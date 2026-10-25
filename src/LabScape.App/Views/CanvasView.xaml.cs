using System.Collections.Specialized;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using LabScape.App.Adorners;
using LabScape.Core.Enums;
using LabScape.Core.Models;
using LabScape.ViewModels;
using LabScape.ViewModels.Commands;
using LabScape.ViewModels.Items;
using LabScape.ViewModels.Stencils;
using LabScape.ViewModels.Templates;

namespace LabScape.App.Views;

public partial class CanvasView : UserControl
{
    private bool _isPanning;
    private Point _panStart;
    private double _initialPanX, _initialPanY;

    private bool _isDraggingItem;
    private bool _isPotentialDrag;
    private ItemViewModel? _pendingSingleSelection;
    private Point _itemDragStart;
    private readonly Dictionary<ItemViewModel, Point> _initialItemPositions = new();

    // Marquee / Rubberband selection state
    private bool _isSelectingMarquee;
    private Point _marqueeStart;

    private readonly List<SelectionAdorner> _currentAdorners = new();
    private readonly List<FrameworkElement> _adornedElements = new();
    private CanvasViewModel? _subscribedVm;

    private const double SnapTolerance = 6.0;

    public CanvasView()
    {
        InitializeComponent();
        DataContextChanged += CanvasView_DataContextChanged;
        Loaded += CanvasView_Loaded;
        Unloaded += CanvasView_Unloaded;
    }

    private void CanvasView_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        HookViewModel();
    }

    private void CanvasView_Loaded(object sender, RoutedEventArgs e)
    {
        HookViewModel();
    }

    private void CanvasView_Unloaded(object sender, RoutedEventArgs e)
    {
        UnhookViewModel();
    }

    private void HookViewModel()
    {
        if (_subscribedVm != null)
        {
            _subscribedVm.SelectedItems.CollectionChanged -= OnSelectedItemsChanged;
            _subscribedVm.PropertyChanged -= OnViewModelPropertyChanged;
            _subscribedVm = null;
        }

        if (DataContext is CanvasViewModel vm)
        {
            _subscribedVm = vm;
            _subscribedVm.SelectedItems.CollectionChanged += OnSelectedItemsChanged;
            _subscribedVm.PropertyChanged += OnViewModelPropertyChanged;
            UpdateAdorner();
        }
        else
        {
            RemoveAllAdorners();
        }
    }

    private void UnhookViewModel()
    {
        if (_subscribedVm != null)
        {
            _subscribedVm.SelectedItems.CollectionChanged -= OnSelectedItemsChanged;
            _subscribedVm.PropertyChanged -= OnViewModelPropertyChanged;
            _subscribedVm = null;
        }
        RemoveAllAdorners();
    }

    private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(CanvasViewModel.ZoomLevel))
        {
            foreach (var adorner in _currentAdorners)
            {
                adorner.InvalidateAdorner();
            }
        }
    }

    private void OnSelectedItemsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        UpdateAdorner();
    }

    private CanvasViewModel? ViewModel => DataContext as CanvasViewModel;

    private void Viewport_MouseDown(object sender, MouseButtonEventArgs e)
    {
        Focus();

        // 1. Middle-drag or Space + Left-drag: Viewport Panning
        if (e.MiddleButton == MouseButtonState.Pressed ||
            (Keyboard.IsKeyDown(Key.Space) && e.LeftButton == MouseButtonState.Pressed))
        {
            _isPanning = true;
            _panStart = e.GetPosition(this);
            if (ViewModel != null)
            {
                _initialPanX = ViewModel.PanX;
                _initialPanY = ViewModel.PanY;
            }
            Mouse.Capture((IInputElement)sender);
            Cursor = Cursors.Hand;
            e.Handled = true;
            return;
        }

        // 2. Click on empty canvas surface (start marquee or clear selection)
        bool isCanvasClick = e.OriginalSource == sender || 
                             e.OriginalSource == CanvasSurface || 
                             (e.OriginalSource as FrameworkElement)?.DataContext is not ItemViewModel;

        // Double-click on empty canvas creates a text item at clicked point
        if (e.ClickCount == 2 && isCanvasClick && e.LeftButton == MouseButtonState.Pressed && ViewModel != null)
        {
            Point p = e.GetPosition(CanvasSurface);
            if (ViewModel.SnapToGrid)
            {
                p.X = Math.Round(p.X / ViewModel.GridSize) * ViewModel.GridSize;
                p.Y = Math.Round(p.Y / ViewModel.GridSize) * ViewModel.GridSize;
            }
            var textItem = new TextItem
            {
                Name = "Text",
                Text = "Double-click to edit text",
                X = p.X,
                Y = p.Y,
                Width = 140,
                Height = 40
            };
            var vm = new TextItemViewModel(textItem);
            ViewModel.AddItem(vm);
            vm.BeginEdit();
            e.Handled = true;
            return;
        }

        if (e.RightButton == MouseButtonState.Pressed && isCanvasClick)
        {
            ViewModel?.ClearSelection();
            UpdateAdorner();
        }

        if (e.LeftButton == MouseButtonState.Pressed && isCanvasClick)
        {
            bool isCtrl = (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control;
            if (!isCtrl)
            {
                ViewModel?.ClearSelection();
                UpdateAdorner();
            }

            _isSelectingMarquee = true;
            _marqueeStart = e.GetPosition(CanvasSurface);

            Canvas.SetLeft(RubberbandRect, _marqueeStart.X);
            Canvas.SetTop(RubberbandRect, _marqueeStart.Y);
            RubberbandRect.Width = 0;
            RubberbandRect.Height = 0;
            RubberbandRect.Visibility = Visibility.Visible;

            Mouse.Capture((IInputElement)sender);
            e.Handled = true;
        }
    }

    private void Viewport_MouseMove(object sender, MouseEventArgs e)
    {
        Point canvasPos = e.GetPosition(CanvasSurface);
        if (ViewModel != null)
        {
            ViewModel.CursorX = Math.Round(canvasPos.X);
            ViewModel.CursorY = Math.Round(canvasPos.Y);
        }

        // 1. Pan Viewport
        if (_isPanning && ViewModel != null)
        {
            Point current = e.GetPosition(this);
            ViewModel.PanX = _initialPanX + (current.X - _panStart.X);
            ViewModel.PanY = _initialPanY + (current.Y - _panStart.Y);
            return;
        }

        // 2. Marquee Selection
        if (_isSelectingMarquee)
        {
            double x = Math.Min(_marqueeStart.X, canvasPos.X);
            double y = Math.Min(_marqueeStart.Y, canvasPos.Y);
            double w = Math.Abs(canvasPos.X - _marqueeStart.X);
            double h = Math.Abs(canvasPos.Y - _marqueeStart.Y);

            Canvas.SetLeft(RubberbandRect, x);
            Canvas.SetTop(RubberbandRect, y);
            RubberbandRect.Width = w;
            RubberbandRect.Height = h;
            return;
        }

        // 3. Dragging Items with Smart Magnetic Alignment Guides
        if (_isPotentialDrag && !_isDraggingItem)
        {
            Point curPos = e.GetPosition(CanvasSurface);
            if (Math.Abs(curPos.X - _itemDragStart.X) >= SystemParameters.MinimumHorizontalDragDistance ||
                Math.Abs(curPos.Y - _itemDragStart.Y) >= SystemParameters.MinimumVerticalDragDistance)
            {
                _isDraggingItem = true;
                _pendingSingleSelection = null; // User actively dragged, keep all selected items
            }
        }

        if (_isDraggingItem && ViewModel != null && _initialItemPositions.Count > 0)
        {
            double deltaX = canvasPos.X - _itemDragStart.X;
            double deltaY = canvasPos.Y - _itemDragStart.Y;

            // Pick the primary dragged item for alignment snapping
            var primary = _initialItemPositions.Keys.First();
            var primaryStart = _initialItemPositions[primary];

            double targetPrimaryX;
            double targetPrimaryY;

            if (ViewModel.SnapToGrid)
            {
                // For lines, the center is aligned to grid, while X/Y has an offset of Width/2 or Height/2.
                // Snapping the center preserves accurate grid alignment for both horizontal and rotated lines:
                if (primary is ShapeItemViewModel { ShapeType: ShapeType.Line })
                {
                    double primaryCenterX = primaryStart.X + primary.Width / 2.0 + deltaX;
                    double primaryCenterY = primaryStart.Y + primary.Height / 2.0 + deltaY;
                    double snappedCenterX = Math.Round(primaryCenterX / ViewModel.GridSize) * ViewModel.GridSize;
                    double snappedCenterY = Math.Round(primaryCenterY / ViewModel.GridSize) * ViewModel.GridSize;
                    targetPrimaryX = snappedCenterX - primary.Width / 2.0;
                    targetPrimaryY = snappedCenterY - primary.Height / 2.0;
                }
                else
                {
                    targetPrimaryX = Math.Round((primaryStart.X + deltaX) / ViewModel.GridSize) * ViewModel.GridSize;
                    targetPrimaryY = Math.Round((primaryStart.Y + deltaY) / ViewModel.GridSize) * ViewModel.GridSize;
                }
            }
            else
            {
                targetPrimaryX = primaryStart.X + deltaX;
                targetPrimaryY = primaryStart.Y + deltaY;
            }

            // Magnetic Alignment Snapping against other canvas items (zero-allocation fast scan)
            bool snapXFound = false;
            bool snapYFound = false;

            double pLeft = targetPrimaryX;
            double pCenter = targetPrimaryX + primary.Width / 2.0;
            double pRight = targetPrimaryX + primary.Width;

            double pTop = targetPrimaryY;
            double pMiddle = targetPrimaryY + primary.Height / 2.0;
            double pBottom = targetPrimaryY + primary.Height;

            foreach (var other in ViewModel.Items)
            {
                if (_initialItemPositions.ContainsKey(other)) continue;
                if (Math.Abs(other.X - targetPrimaryX) > 400 && Math.Abs(other.Y - targetPrimaryY) > 400) continue;
                double oLeft = other.X;
                double oCenter = other.X + other.Width / 2.0;
                double oRight = other.X + other.Width;

                // X-axis alignment snaps
                if (!snapXFound)
                {
                    if (Math.Abs(pLeft - oLeft) <= SnapTolerance)
                    {
                        targetPrimaryX = oLeft;
                        ShowVerticalGuide(oLeft, Math.Min(targetPrimaryY, other.Y), Math.Max(pBottom, other.Y + other.Height));
                        snapXFound = true;
                    }
                    else if (Math.Abs(pCenter - oCenter) <= SnapTolerance)
                    {
                        targetPrimaryX = oCenter - primary.Width / 2.0;
                        ShowVerticalGuide(oCenter, Math.Min(targetPrimaryY, other.Y), Math.Max(pBottom, other.Y + other.Height));
                        snapXFound = true;
                    }
                    else if (Math.Abs(pRight - oRight) <= SnapTolerance)
                    {
                        targetPrimaryX = oRight - primary.Width;
                        ShowVerticalGuide(oRight, Math.Min(targetPrimaryY, other.Y), Math.Max(pBottom, other.Y + other.Height));
                        snapXFound = true;
                    }
                }

                // Y-axis alignment snaps
                double oTop = other.Y;
                double oMiddle = other.Y + other.Height / 2.0;
                double oBottom = other.Y + other.Height;

                if (!snapYFound)
                {
                    if (Math.Abs(pTop - oTop) <= SnapTolerance)
                    {
                        targetPrimaryY = oTop;
                        ShowHorizontalGuide(oTop, Math.Min(targetPrimaryX, other.X), Math.Max(pRight, other.X + other.Width));
                        snapYFound = true;
                    }
                    else if (Math.Abs(pMiddle - oMiddle) <= SnapTolerance)
                    {
                        targetPrimaryY = oMiddle - primary.Height / 2.0;
                        ShowHorizontalGuide(oMiddle, Math.Min(targetPrimaryX, other.X), Math.Max(pRight, other.X + other.Width));
                        snapYFound = true;
                    }
                    else if (Math.Abs(pBottom - oBottom) <= SnapTolerance)
                    {
                        targetPrimaryY = oBottom - primary.Height;
                        ShowHorizontalGuide(oBottom, Math.Min(targetPrimaryX, other.X), Math.Max(pRight, other.X + other.Width));
                        snapYFound = true;
                    }
                }
            }

            if (!snapXFound) SnapGuideVertical.Visibility = Visibility.Collapsed;
            if (!snapYFound) SnapGuideHorizontal.Visibility = Visibility.Collapsed;

            // Apply calculated offset to all selected items
            double finalDeltaX = targetPrimaryX - primaryStart.X;
            double finalDeltaY = targetPrimaryY - primaryStart.Y;

            foreach (var (item, startPos) in _initialItemPositions)
            {
                double newX = startPos.X + finalDeltaX;
                double newY = startPos.Y + finalDeltaY;

                // For items with startPos.X >= 0, clamp to >= 0
                // For rotated items with negative X (e.g. vertical line rotated 90 deg where X = centerX - len/2):
                // Ensure visual center X >= 0:
                if (startPos.X >= 0)
                {
                    newX = Math.Max(0, newX);
                }
                else
                {
                    double minAllowedX = -item.Width / 2.0;
                    newX = Math.Max(minAllowedX, newX);
                }

                if (startPos.Y >= 0)
                {
                    newY = Math.Max(0, newY);
                }
                else
                {
                    double minAllowedY = -item.Height / 2.0;
                    newY = Math.Max(minAllowedY, newY);
                }

                item.X = newX;
                item.Y = newY;
            }
        }
    }

    private void ShowVerticalGuide(double x, double y1, double y2)
    {
        SnapGuideVertical.X1 = x;
        SnapGuideVertical.Y1 = Math.Max(0, y1 - 20);
        SnapGuideVertical.X2 = x;
        SnapGuideVertical.Y2 = y2 + 20;
        SnapGuideVertical.Visibility = Visibility.Visible;
    }

    private void ShowHorizontalGuide(double y, double x1, double x2)
    {
        SnapGuideHorizontal.X1 = Math.Max(0, x1 - 20);
        SnapGuideHorizontal.Y1 = y;
        SnapGuideHorizontal.X2 = x2 + 20;
        SnapGuideHorizontal.Y2 = y;
        SnapGuideHorizontal.Visibility = Visibility.Visible;
    }

    private void Viewport_MouseUp(object sender, MouseButtonEventArgs e)
    {
        if (_isPanning)
        {
            _isPanning = false;
            Mouse.Capture(null);
            Cursor = Cursors.Arrow;
        }

        if (_isSelectingMarquee && ViewModel != null)
        {
            _isSelectingMarquee = false;
            Mouse.Capture(null);
            RubberbandRect.Visibility = Visibility.Collapsed;

            double mx = Canvas.GetLeft(RubberbandRect);
            double my = Canvas.GetTop(RubberbandRect);
            double mw = RubberbandRect.Width;
            double mh = RubberbandRect.Height;

            if (mw > 4 && mh > 4)
            {
                Rect marquee = new(mx, my, mw, mh);
                var matched = ViewModel.Items.Where(itm =>
                {
                    Rect itemRect = new(itm.X, itm.Y, itm.Width, itm.Height);
                    return marquee.IntersectsWith(itemRect);
                }).ToList();

                bool isCtrl = (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control;
                if (matched.Count > 0)
                {
                    if (isCtrl)
                    {
                        var combined = ViewModel.SelectedItems.Union(matched).ToList();
                        ViewModel.SetSelection(combined);
                    }
                    else
                    {
                        ViewModel.SetSelection(matched);
                    }
                }
                else if (!isCtrl)
                {
                    ViewModel.ClearSelection();
                }
            }
            UpdateAdorner();
        }

        if (_isPotentialDrag)
        {
            _isPotentialDrag = false;
            Mouse.Capture(null);

            if (_isDraggingItem && ViewModel != null)
            {
                _isDraggingItem = false;
                SnapGuideVertical.Visibility = Visibility.Collapsed;
                SnapGuideHorizontal.Visibility = Visibility.Collapsed;

                // Commit atomic undoable Move Command only if positions actually changed
                var moves = _initialItemPositions
                    .Where(kv => Math.Abs(kv.Value.X - kv.Key.X) > 0.01 || Math.Abs(kv.Value.Y - kv.Key.Y) > 0.01)
                    .Select(kv => (kv.Key, kv.Value.X, kv.Value.Y, kv.Key.X, kv.Key.Y))
                    .ToList();
                if (moves.Count > 0)
                {
                    ViewModel.CommitItemMoves(moves);
                }
            }
            else if (_pendingSingleSelection != null && ViewModel != null)
            {
                ViewModel.SelectItem(_pendingSingleSelection, addToExisting: false);
                UpdateAdorner();
            }

            _pendingSingleSelection = null;
            _initialItemPositions.Clear();
        }
    }

    private void Viewport_MouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (ViewModel == null) return;

        bool isCtrl = (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control;
        bool isShift = (Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift;

        if (isCtrl)
        {
            // 1. Ctrl + Scroll: Zoom in and out centered around mouse cursor
            Point mouseScreen = e.GetPosition((UIElement)sender);
            Point mouseCanvas = e.GetPosition(CanvasSurface);

            double oldZoom = ViewModel.ZoomLevel;
            double factor = e.Delta > 0 ? 1.15 : (1.0 / 1.15);
            double newZoom = Math.Clamp(Math.Round(oldZoom * factor * 100) / 100, 0.10, 10.0);

            if (Math.Abs(newZoom - oldZoom) > 0.001)
            {
                // Zoom centering invariant: mouseScreen = mouseCanvas * newZoom + pan
                ViewModel.PanX = mouseScreen.X - mouseCanvas.X * newZoom;
                ViewModel.PanY = mouseScreen.Y - mouseCanvas.Y * newZoom;
                ViewModel.ZoomLevel = newZoom;
            }
        }
        else if (isShift)
        {
            // 2. Shift + Scroll: Scroll horizontally
            double scrollStep = 48.0;
            double deltaUnits = (e.Delta / 120.0) * scrollStep;
            ViewModel.PanX += deltaUnits;
        }
        else
        {
            // 3. Normal Scroll: Scroll vertically
            double scrollStep = 48.0;
            double deltaUnits = (e.Delta / 120.0) * scrollStep;
            ViewModel.PanY += deltaUnits;
        }

        e.Handled = true;
    }

    private ItemViewModel FindRootItem(ItemViewModel item)
    {
        if (ViewModel == null || ViewModel.Items.Contains(item))
        {
            return item;
        }

        foreach (var topItem in ViewModel.Items)
        {
            if (topItem is GroupItemViewModel group && ContainsItemRecursive(group, item))
            {
                return group;
            }
        }

        return item;
    }

    private static bool ContainsItemRecursive(GroupItemViewModel group, ItemViewModel target)
    {
        foreach (var child in group.Children)
        {
            if (child == target || child.Id == target.Id) return true;
            if (child is GroupItemViewModel childGroup && ContainsItemRecursive(childGroup, target))
            {
                return true;
            }
        }
        return false;
    }

    private FrameworkElement? FindContainerForItem(ItemViewModel item)
    {
        if (ItemsHost == null) return null;

        // 1. Direct container from top-level ItemsHost
        if (ItemsHost.ItemContainerGenerator.ContainerFromItem(item) is FrameworkElement topPresenter)
        {
            return topPresenter;
        }

        // 2. If item is an inner child inside a group
        if (item.ParentGroupId != null && ViewModel != null)
        {
            foreach (var topItem in ViewModel.Items.OfType<GroupItemViewModel>())
            {
                if (topItem == item) continue;
                if (ContainsItemRecursive(topItem, item))
                {
                    if (ItemsHost.ItemContainerGenerator.ContainerFromItem(topItem) is DependencyObject groupContainer)
                    {
                        var childItemsControl = FindVisualChild<ItemsControl>(groupContainer);
                        if (childItemsControl?.ItemContainerGenerator.ContainerFromItem(item) is FrameworkElement childPresenter)
                        {
                            return childPresenter;
                        }
                    }
                    break;
                }
            }
        }

        return null;
    }

    private static T? FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
    {
        int count = VisualTreeHelper.GetChildrenCount(parent);
        for (int i = 0; i < count; i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is T typedChild) return typedChild;
            var desc = FindVisualChild<T>(child);
            if (desc != null) return desc;
        }
        return null;
    }

    private void Item_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is not FrameworkElement elem || elem.DataContext is not ItemViewModel itemVm) return;
        if (ViewModel == null) return;

        ItemViewModel targetItem = FindRootItem(itemVm);

        if (e.RightButton == MouseButtonState.Pressed)
        {
            if (!targetItem.IsSelected)
            {
                ViewModel.SelectItem(targetItem, addToExisting: false);
                UpdateAdorner();
            }
            return;
        }

        if (e.LeftButton != MouseButtonState.Pressed) return;

        // Double-click on a text item enters inline editing mode
        if (e.ClickCount == 2 && targetItem is TextItemViewModel textItem)
        {
            if (!targetItem.IsSelected)
            {
                ViewModel.SelectItem(targetItem, addToExisting: false);
            }
            textItem.BeginEdit();
            e.Handled = true;
            return;
        }

        _pendingSingleSelection = null;
        bool isCtrl = (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control;
        if (isCtrl)
        {
            if (targetItem.IsSelected)
                ViewModel.DeselectItem(targetItem);
            else
                ViewModel.SelectItem(targetItem, addToExisting: true);
        }
        else
        {
            if (!targetItem.IsSelected)
            {
                ViewModel.SelectItem(targetItem, addToExisting: false);
            }
            else if (ViewModel.SelectedItems.Count > 1)
            {
                // Already part of a multi-selection: if user releases mouse without dragging,
                // reduce selection to this single item.
                _pendingSingleSelection = targetItem;
            }
        }

        UpdateAdorner();

        // Prepare for potential drag (do NOT set _isDraggingItem = true yet to avoid displacement on simple click)
        _isPotentialDrag = true;
        _isDraggingItem = false;
        _itemDragStart = e.GetPosition(CanvasSurface);
        _initialItemPositions.Clear();
        foreach (var sel in ViewModel.SelectedItems)
        {
            _initialItemPositions[sel] = new Point(sel.X, sel.Y);
        }

        Mouse.Capture((IInputElement)sender);
        e.Handled = true;
    }

    private bool _adornerUpdatePending = false;

    private void UpdateAdorner()
    {
        if (ViewModel == null || ViewModel.SelectedItems.Count == 0)
        {
            RemoveAllAdorners();
            return;
        }

        // Fast-path: if containers are already generated and ready, update immediately
        if (AreContainersReadyForSelection())
        {
            UpdateAdornerInternal();
            return;
        }

        // Defer until layout pass completes container generation
        if (_adornerUpdatePending) return;
        _adornerUpdatePending = true;

        Dispatcher.InvokeAsync(() =>
        {
            _adornerUpdatePending = false;
            UpdateAdornerInternal();
        }, System.Windows.Threading.DispatcherPriority.Loaded);
    }

    private bool AreContainersReadyForSelection()
    {
        if (ViewModel == null || ViewModel.SelectedItems.Count == 0 || ItemsHost == null) return true;
        if (ItemsHost.ItemContainerGenerator.Status != System.Windows.Controls.Primitives.GeneratorStatus.ContainersGenerated)
            return false;

        foreach (var item in ViewModel.SelectedItems)
        {
            if (FindContainerForItem(item) == null)
                return false;
        }
        return true;
    }

    private void UpdateAdornerInternal()
    {
        RemoveAllAdorners();

        if (ViewModel == null || ViewModel.SelectedItems.Count == 0) return;

        bool isSingle = ViewModel.SelectedItems.Count == 1;
        bool hasMissingContainers = false;

        foreach (var selectedItem in ViewModel.SelectedItems)
        {
            var presenter = FindContainerForItem(selectedItem);
            if (presenter == null)
            {
                hasMissingContainers = true;
                continue;
            }

            var layer = AdornerLayer.GetAdornerLayer(presenter);
            if (layer != null)
            {
                var adorner = new SelectionAdorner(presenter, selectedItem, ViewModel.UndoRedoService, () =>
                {
                    if (selectedItem is GroupItemViewModel grp) grp.RecalculateBounds();
                }, () => ViewModel?.ZoomLevel ?? 1.0)
                {
                    ShowHandles = isSingle
                };
                layer.Add(adorner);
                _currentAdorners.Add(adorner);
                _adornedElements.Add(presenter);
            }
        }

        if (hasMissingContainers && ViewModel.SelectedItems.Count > 0 && !_adornerUpdatePending)
        {
            _adornerUpdatePending = true;
            Dispatcher.InvokeAsync(() =>
            {
                _adornerUpdatePending = false;
                UpdateAdornerInternal();
            }, System.Windows.Threading.DispatcherPriority.Loaded);
        }
    }

    private void RemoveAllAdorners()
    {
        for (int i = 0; i < _currentAdorners.Count && i < _adornedElements.Count; i++)
        {
            var adorner = _currentAdorners[i];
            adorner.Detach();
            var layer = AdornerLayer.GetAdornerLayer(_adornedElements[i]);
            layer?.Remove(adorner);
        }
        _currentAdorners.Clear();
        _adornedElements.Clear();
    }

    private void Viewport_DragOver(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(typeof(RoomTemplateOptions)))
        {
            e.Effects = DragDropEffects.Copy;
            if (e.Data.GetData(typeof(RoomTemplateOptions)) is RoomTemplateOptions options)
            {
                var preview = RoomTemplateGenerator.Generate(options);
                Point p = e.GetPosition(CanvasSurface);
                double gx = Math.Max(0, p.X - preview.RoomWidth / 2.0);
                double gy = Math.Max(0, p.Y - preview.RoomHeight / 2.0);

                if (ViewModel?.SnapToGrid == true)
                {
                    gx = Math.Round(gx / ViewModel.GridSize) * ViewModel.GridSize;
                    gy = Math.Round(gy / ViewModel.GridSize) * ViewModel.GridSize;
                }

                Canvas.SetLeft(DragGhost, gx);
                Canvas.SetTop(DragGhost, gy);
                DragGhost.Width = preview.RoomWidth;
                DragGhost.Height = preview.RoomHeight;
                DragGhost.Visibility = Visibility.Visible;
            }
            e.Handled = true;
            return;
        }

        if (e.Data.GetDataPresent(typeof(StencilItem)))
        {
            e.Effects = DragDropEffects.Copy;

            if (e.Data.GetData(typeof(StencilItem)) is StencilItem stencil)
            {
                Point p = e.GetPosition(CanvasSurface);
                double gx = Math.Max(0, p.X - stencil.DefaultWidth / 2.0);
                double gy = Math.Max(0, p.Y - stencil.DefaultHeight / 2.0);

                if (ViewModel?.SnapToGrid == true)
                {
                    gx = Math.Round(gx / ViewModel.GridSize) * ViewModel.GridSize;
                    gy = Math.Round(gy / ViewModel.GridSize) * ViewModel.GridSize;
                }

                Canvas.SetLeft(DragGhost, gx);
                Canvas.SetTop(DragGhost, gy);
                DragGhost.Width = stencil.DefaultWidth;
                DragGhost.Height = stencil.DefaultHeight;
                DragGhost.Visibility = Visibility.Visible;
            }

            e.Handled = true;
        }
    }

    private void Viewport_DragLeave(object sender, DragEventArgs e)
    {
        DragGhost.Visibility = Visibility.Collapsed;
    }

    private void Viewport_Drop(object sender, DragEventArgs e)
    {
        DragGhost.Visibility = Visibility.Collapsed;

        if (ViewModel == null) return;

        if (e.Data.GetDataPresent(typeof(RoomTemplateOptions)) &&
            e.Data.GetData(typeof(RoomTemplateOptions)) is RoomTemplateOptions options)
        {
            Point p = e.GetPosition(CanvasSurface);
            var preview = RoomTemplateGenerator.Generate(options);
            double dropX = Math.Max(0, p.X - preview.RoomWidth / 2.0);
            double dropY = Math.Max(0, p.Y - preview.RoomHeight / 2.0);

            if (ViewModel.SnapToGrid)
            {
                dropX = Math.Round(dropX / ViewModel.GridSize) * ViewModel.GridSize;
                dropY = Math.Round(dropY / ViewModel.GridSize) * ViewModel.GridSize;
            }

            options.OriginX = dropX;
            options.OriginY = dropY;
            ViewModel.AddRoomTemplate(options);
            e.Handled = true;
            return;
        }

        if (e.Data.GetData(typeof(StencilItem)) is StencilItem stencil)
        {
            Point p = e.GetPosition(CanvasSurface);
            var newItem = stencil.CreateItem();
            double dropX = Math.Max(0, p.X - newItem.Width / 2.0);
            double dropY = Math.Max(0, p.Y - newItem.Height / 2.0);

            if (ViewModel.SnapToGrid)
            {
                dropX = Math.Round(dropX / ViewModel.GridSize) * ViewModel.GridSize;
                dropY = Math.Round(dropY / ViewModel.GridSize) * ViewModel.GridSize;
            }

            newItem.X = dropX;
            newItem.Y = dropY;
            ViewModel.AddItem(newItem);
            e.Handled = true;
        }
    }

    private string? _editingInitialText;

    private void InPlaceTextBox_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (sender is TextBox tb && tb.Visibility == Visibility.Visible)
        {
            if (tb.DataContext is TextItemViewModel textItem)
            {
                _editingInitialText = textItem.Text;
            }
            Dispatcher.InvokeAsync(() =>
            {
                tb.Focus();
                tb.SelectAll();
            }, System.Windows.Threading.DispatcherPriority.Input);
        }
    }

    private void InPlaceTextBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (sender is not TextBox tb || tb.DataContext is not TextItemViewModel textItem) return;

        if (e.Key == Key.Escape)
        {
            if (_editingInitialText != null)
            {
                textItem.Text = _editingInitialText;
            }
            textItem.EndEdit();
            e.Handled = true;
        }
        else if (e.Key == Key.Enter)
        {
            bool isShift = (Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift;
            bool isCtrl = (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control;

            if (isShift)
            {
                int caret = tb.CaretIndex;
                tb.Text = tb.Text.Insert(caret, Environment.NewLine);
                tb.CaretIndex = caret + Environment.NewLine.Length;
                e.Handled = true;
            }
            else
            {
                CommitTextEdit(textItem);
                e.Handled = true;
            }
        }
    }

    private void InPlaceTextBox_LostFocus(object sender, RoutedEventArgs e)
    {
        if (sender is TextBox tb && tb.DataContext is TextItemViewModel textItem)
        {
            CommitTextEdit(textItem);
        }
    }

    private void CommitTextEdit(TextItemViewModel textItem)
    {
        if (!textItem.IsEditing) return;

        if (_editingInitialText != null && _editingInitialText != textItem.Text && ViewModel != null)
        {
            ViewModel.UndoRedoService.ExecuteCommand(new ChangeTextContentCommand(textItem, _editingInitialText, textItem.Text));
        }
        _editingInitialText = null;
        textItem.EndEdit();
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (ViewModel == null) return;

        bool isCtrl = (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control;
        bool isShift = (Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift;
        bool isAlt = (Keyboard.Modifiers & ModifierKeys.Alt) == ModifierKeys.Alt;

        if (isCtrl && isShift && e.Key == Key.G)
        {
            ViewModel.UngroupSelected();
            e.Handled = true;
        }
        else if (isCtrl && !isShift && e.Key == Key.G)
        {
            ViewModel.GroupSelected();
            e.Handled = true;
        }
        else if (e.Key == Key.Delete)
        {
            ViewModel.DeleteSelected();
            e.Handled = true;
        }
        else if (!isCtrl && !isShift && !isAlt && e.Key == Key.T && FocusManager.GetFocusedElement(this) is not TextBox)
        {
            ViewModel.AddTextItem();
            e.Handled = true;
        }
    }
}
