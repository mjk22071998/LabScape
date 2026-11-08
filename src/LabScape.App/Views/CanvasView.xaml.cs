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
    private MultiSelectionAdorner? _currentMultiAdorner;
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
            _currentMultiAdorner?.InvalidateVisual();
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

            // 1. Calculate collective union bounding box of all dragged items (accurately handles groups and multi-selection)
            double initMinX = double.MaxValue;
            double initMinY = double.MaxValue;
            double initMaxX = double.MinValue;
            double initMaxY = double.MinValue;

            foreach (var (item, startPos) in _initialItemPositions)
            {
                if (item is GroupItemViewModel grp)
                {
                    grp.RecalculateBounds();
                }

                var (bMinX, bMinY, bMaxX, bMaxY) = GetItemBoundsAt(item, startPos.X, startPos.Y);
                initMinX = Math.Min(initMinX, bMinX);
                initMinY = Math.Min(initMinY, bMinY);
                initMaxX = Math.Max(initMaxX, bMaxX);
                initMaxY = Math.Max(initMaxY, bMaxY);
            }

            double pWidth = initMaxX - initMinX;
            double pHeight = initMaxY - initMinY;

            double snapDeltaX;
            double snapDeltaY;

            if (ViewModel.SnapToGrid)
            {
                if (_initialItemPositions.Count == 1 && _initialItemPositions.Keys.First() is ShapeItemViewModel { ShapeType: ShapeType.Line } line)
                {
                    var lineStart = _initialItemPositions[line];
                    double primaryCenterX = lineStart.X + line.Width / 2.0 + deltaX;
                    double primaryCenterY = lineStart.Y + line.Height / 2.0 + deltaY;
                    double snappedCenterX = Math.Round(primaryCenterX / ViewModel.GridSize) * ViewModel.GridSize;
                    double snappedCenterY = Math.Round(primaryCenterY / ViewModel.GridSize) * ViewModel.GridSize;
                    snapDeltaX = snappedCenterX - line.Width / 2.0 - lineStart.X;
                    snapDeltaY = snappedCenterY - line.Height / 2.0 - lineStart.Y;
                }
                else
                {
                    double snappedX = Math.Round((initMinX + deltaX) / ViewModel.GridSize) * ViewModel.GridSize;
                    double snappedY = Math.Round((initMinY + deltaY) / ViewModel.GridSize) * ViewModel.GridSize;
                    snapDeltaX = snappedX - initMinX;
                    snapDeltaY = snappedY - initMinY;
                }
            }
            else
            {
                snapDeltaX = deltaX;
                snapDeltaY = deltaY;
            }

            double pLeft = initMinX + snapDeltaX;
            double pRight = pLeft + pWidth;
            double pCenter = (pLeft + pRight) / 2.0;

            double pTop = initMinY + snapDeltaY;
            double pBottom = pTop + pHeight;
            double pMiddle = (pTop + pBottom) / 2.0;

            double targetPLeft = pLeft;
            double targetPTop = pTop;

            double effectiveTolerance = SnapTolerance / Math.Max(0.2, ViewModel.ZoomLevel);

            bool snapXFound = false;
            double bestDistX = double.MaxValue;
            double bestGuideX = 0;
            double bestGuideY1 = 0;
            double bestGuideY2 = 0;

            bool snapYFound = false;
            double bestDistY = double.MaxValue;
            double bestGuideY = 0;
            double bestGuideX1 = 0;
            double bestGuideX2 = 0;

            foreach (var other in ViewModel.Items)
            {
                if (_initialItemPositions.ContainsKey(other)) continue;
                if (!other.IsVisible) continue;

                if (other is GroupItemViewModel otherGrp && (otherGrp.Width <= 10 || otherGrp.Height <= 10))
                {
                    otherGrp.RecalculateBounds();
                }

                var (oLeft, oTop, oRight, oBottom) = GetItemBoundsAt(other, other.X, other.Y);
                double oCenter = (oLeft + oRight) / 2.0;
                double oMiddle = (oTop + oBottom) / 2.0;

                // Box-to-box distance check: avoids skipping large groups while filtering out distant items
                double boxDistX = Math.Max(0.0, Math.Max(pLeft - oRight, oLeft - pRight));
                double boxDistY = Math.Max(0.0, Math.Max(pTop - oBottom, oTop - pBottom));
                if (boxDistX > 1000 && boxDistY > 1000) continue;

                // X-axis alignment snaps (all 9 combinations: Left, Center, Right vs Left, Center, Right of group/item)
                var xSnaps = new (double PCoord, double OCoord, double SnappedLeft, double GuideX)[]
                {
                    (pLeft, oLeft, oLeft, oLeft),
                    (pLeft, oCenter, oCenter, oCenter),
                    (pLeft, oRight, oRight, oRight),
                    (pCenter, oLeft, oLeft - pWidth / 2.0, oLeft),
                    (pCenter, oCenter, oCenter - pWidth / 2.0, oCenter),
                    (pCenter, oRight, oRight - pWidth / 2.0, oRight),
                    (pRight, oLeft, oLeft - pWidth, oLeft),
                    (pRight, oCenter, oCenter - pWidth, oCenter),
                    (pRight, oRight, oRight - pWidth, oRight),
                };

                foreach (var (pCoord, oCoord, snappedLeft, guideX) in xSnaps)
                {
                    double d = Math.Abs(pCoord - oCoord);
                    if (d <= effectiveTolerance && d < bestDistX)
                    {
                        bestDistX = d;
                        targetPLeft = snappedLeft;
                        bestGuideX = guideX;
                        bestGuideY1 = Math.Min(pTop, oTop);
                        bestGuideY2 = Math.Max(pBottom, oBottom);
                        snapXFound = true;
                    }
                }

                // Y-axis alignment snaps (all 9 combinations: Top, Middle, Bottom vs Top, Middle, Bottom of group/item)
                var ySnaps = new (double PCoord, double OCoord, double SnappedTop, double GuideY)[]
                {
                    (pTop, oTop, oTop, oTop),
                    (pTop, oMiddle, oMiddle, oMiddle),
                    (pTop, oBottom, oBottom, oBottom),
                    (pMiddle, oTop, oTop - pHeight / 2.0, oTop),
                    (pMiddle, oMiddle, oMiddle - pHeight / 2.0, oMiddle),
                    (pMiddle, oBottom, oBottom - pHeight / 2.0, oBottom),
                    (pBottom, oTop, oTop - pHeight, oTop),
                    (pBottom, oMiddle, oMiddle - pHeight, oMiddle),
                    (pBottom, oBottom, oBottom - pHeight, oBottom),
                };

                foreach (var (pCoord, oCoord, snappedTop, guideY) in ySnaps)
                {
                    double d = Math.Abs(pCoord - oCoord);
                    if (d <= effectiveTolerance && d < bestDistY)
                    {
                        bestDistY = d;
                        targetPTop = snappedTop;
                        bestGuideY = guideY;
                        bestGuideX1 = Math.Min(pLeft, oLeft);
                        bestGuideX2 = Math.Max(pRight, oRight);
                        snapYFound = true;
                    }
                }
            }

            if (snapXFound)
            {
                ShowVerticalGuide(bestGuideX, bestGuideY1, bestGuideY2);
            }
            else
            {
                SnapGuideVertical.Visibility = Visibility.Collapsed;
            }

            if (snapYFound)
            {
                ShowHorizontalGuide(bestGuideY, bestGuideX1, bestGuideX2);
            }
            else
            {
                SnapGuideHorizontal.Visibility = Visibility.Collapsed;
            }

            // Apply calculated offset to all selected items uniformly
            double finalDeltaX = targetPLeft - initMinX;
            double finalDeltaY = targetPTop - initMinY;

            foreach (var (item, startPos) in _initialItemPositions)
            {
                item.X = startPos.X + finalDeltaX;
                item.Y = startPos.Y + finalDeltaY;
            }

            // Visio-style live page expansion during drag
            if (ViewModel.AutoSize && _initialItemPositions.Count > 0)
            {
                double maxDragX = _initialItemPositions.Keys.Max(i => i.X + i.Width);
                double maxDragY = _initialItemPositions.Keys.Max(i => i.Y + i.Height);
                if (maxDragX > ViewModel.CanvasWidth || maxDragY > ViewModel.CanvasHeight)
                {
                    ViewModel.ExpandPageGridLive(maxDragX, maxDragY);
                }
            }
        }
    }

    private static (double MinX, double MinY, double MaxX, double MaxY) GetItemBoundsAt(ItemViewModel item, double startX, double startY)
    {
        double w = item.Width;
        double h = item.Height;
        if (Math.Abs(item.Rotation) < 0.001)
        {
            return (startX, startY, startX + w, startY + h);
        }

        double rad = item.Rotation * Math.PI / 180.0;
        double cos = Math.Abs(Math.Cos(rad));
        double sin = Math.Abs(Math.Sin(rad));

        double cx = startX + w / 2.0;
        double cy = startY + h / 2.0;

        double extX = (w / 2.0) * cos + (h / 2.0) * sin;
        double extY = (w / 2.0) * sin + (h / 2.0) * cos;

        return (cx - extX, cy - extY, cx + extX, cy + extY);
    }

    private void ShowVerticalGuide(double x, double y1, double y2)
    {
        SnapGuideVertical.X1 = x;
        SnapGuideVertical.Y1 = y1 - 20;
        SnapGuideVertical.X2 = x;
        SnapGuideVertical.Y2 = y2 + 20;
        SnapGuideVertical.Visibility = Visibility.Visible;
    }

    private void ShowHorizontalGuide(double y, double x1, double x2)
    {
        SnapGuideHorizontal.X1 = x1 - 20;
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
                else
                {
                    ViewModel.UpdatePageGrid();
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
        if (targetItem is GroupItemViewModel targetGrp)
        {
            targetGrp.RecalculateBounds();
        }

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
        if (targetItem.IsLocked || ViewModel.SelectedItems.Any(s => s.IsLocked))
        {
            _isPotentialDrag = false;
            _isDraggingItem = false;
            _initialItemPositions.Clear();
        }
        else
        {
            _isPotentialDrag = true;
            _isDraggingItem = false;
            _itemDragStart = e.GetPosition(CanvasSurface);
            _initialItemPositions.Clear();
            foreach (var sel in ViewModel.SelectedItems)
            {
                _initialItemPositions[sel] = new Point(sel.X, sel.Y);
            }
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
                layer.ClipToBounds = false;
                var adorner = new SelectionAdorner(presenter, selectedItem, ViewModel.UndoRedoService, () =>
                {
                    if (selectedItem is GroupItemViewModel grp && Math.Abs(grp.Rotation) < 0.001) grp.RecalculateBounds();
                }, () => ViewModel?.ZoomLevel ?? 1.0)
                {
                    ShowHandles = isSingle,
                    ShowVerticalGuideAction = ShowVerticalGuide,
                    ShowHorizontalGuideAction = ShowHorizontalGuide,
                    HideVerticalGuideAction = () => SnapGuideVertical.Visibility = Visibility.Collapsed,
                    HideHorizontalGuideAction = () => SnapGuideHorizontal.Visibility = Visibility.Collapsed,
                    GetOtherItems = () => ViewModel?.Items ?? Enumerable.Empty<ItemViewModel>()
                };
                layer.Add(adorner);
                _currentAdorners.Add(adorner);
                _adornedElements.Add(presenter);
            }
        }

        // Multiple selection acts like a temporary group: project unified bounding box with handles
        if (!isSingle && ItemsHost != null)
        {
            var hostLayer = AdornerLayer.GetAdornerLayer(ItemsHost);
            if (hostLayer != null)
            {
                hostLayer.ClipToBounds = false;
                double? initialGroupRot = null;
                if (_currentMultiAdorner != null && _currentMultiAdorner.Items.SequenceEqual(ViewModel.SelectedItems))
                {
                    initialGroupRot = _currentMultiAdorner.GroupRotation;
                }
                else if (ViewModel.SelectedItems.All(i => Math.Abs(i.Rotation - ViewModel.SelectedItems[0].Rotation) < 0.1))
                {
                    initialGroupRot = ViewModel.SelectedItems[0].Rotation;
                }

                var multiAdorner = new MultiSelectionAdorner(
                    ItemsHost,
                    ViewModel.SelectedItems,
                    ViewModel.UndoRedoService,
                    () => ViewModel?.ZoomLevel ?? 1.0,
                    initialGroupRot)
                {
                    ShowVerticalGuideAction = ShowVerticalGuide,
                    ShowHorizontalGuideAction = ShowHorizontalGuide,
                    HideVerticalGuideAction = () => SnapGuideVertical.Visibility = Visibility.Collapsed,
                    HideHorizontalGuideAction = () => SnapGuideHorizontal.Visibility = Visibility.Collapsed,
                    GetOtherItems = () => ViewModel?.Items != null
                        ? ViewModel.Items.Except(ViewModel.SelectedItems)
                        : Enumerable.Empty<ItemViewModel>()
                };
                hostLayer.Add(multiAdorner);
                _currentMultiAdorner = multiAdorner;
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

        if (_currentMultiAdorner != null && ItemsHost != null)
        {
            _currentMultiAdorner.Detach();
            var hostLayer = AdornerLayer.GetAdornerLayer(ItemsHost);
            hostLayer?.Remove(_currentMultiAdorner);
            _currentMultiAdorner = null;
        }

        SnapGuideVertical.Visibility = Visibility.Collapsed;
        SnapGuideHorizontal.Visibility = Visibility.Collapsed;
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
                double gx = p.X - preview.RoomWidth / 2.0;
                double gy = p.Y - preview.RoomHeight / 2.0;

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

        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            e.Effects = DragDropEffects.Copy;
            e.Handled = true;
            return;
        }

        if (e.Data.GetDataPresent(typeof(StencilItem)))
        {
            e.Effects = DragDropEffects.Copy;

            if (e.Data.GetData(typeof(StencilItem)) is StencilItem stencil)
            {
                Point p = e.GetPosition(CanvasSurface);
                double gx = p.X - stencil.DefaultWidth / 2.0;
                double gy = p.Y - stencil.DefaultHeight / 2.0;

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
            double dropX = p.X - preview.RoomWidth / 2.0;
            double dropY = p.Y - preview.RoomHeight / 2.0;

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

        if (e.Data.GetDataPresent(DataFormats.FileDrop) &&
            e.Data.GetData(DataFormats.FileDrop) is string[] files &&
            files.Length > 0)
        {
            string firstFile = files[0];
            string ext = System.IO.Path.GetExtension(firstFile).ToLowerInvariant();
            string[] supported = { ".png", ".jpg", ".jpeg", ".bmp", ".gif", ".webp" };
            if (supported.Contains(ext))
            {
                Point p = e.GetPosition(CanvasSurface);
                if (Window.GetWindow(this) is MainWindow mw)
                {
                    mw.InsertCustomImageFromFile(firstFile, p);
                }
                e.Handled = true;
                return;
            }
        }

        if (e.Data.GetData(typeof(StencilItem)) is StencilItem stencil)
        {
            Point p = e.GetPosition(CanvasSurface);
            var newItem = stencil.CreateItem();
            double dropX = p.X - newItem.Width / 2.0;
            double dropY = p.Y - newItem.Height / 2.0;

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

    private void CanvasContextMenu_InsertImage_Click(object sender, RoutedEventArgs e)
    {
        if (Window.GetWindow(this) is MainWindow mw)
        {
            var ofd = new Microsoft.Win32.OpenFileDialog
            {
                Title = "Insert Custom Image",
                Filter = "Image Files (*.png;*.jpg;*.jpeg;*.bmp;*.gif;*.webp)|*.png;*.jpg;*.jpeg;*.bmp;*.gif;*.webp|All Files (*.*)|*.*",
                CheckFileExists = true
            };

            if (ofd.ShowDialog(mw) == true)
            {
                Point mousePos = Mouse.GetPosition(CanvasSurface);
                mw.InsertCustomImageFromFile(ofd.FileName, mousePos);
            }
        }
    }

    private void ItemContextMenu_ChangeImage_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel?.SelectedItems.Count > 0 && Window.GetWindow(this) is MainWindow mw)
        {
            var item = ViewModel.SelectedItems[0];
            var ofd = new Microsoft.Win32.OpenFileDialog
            {
                Title = "Change Custom Image",
                Filter = "Image Files (*.png;*.jpg;*.jpeg;*.bmp;*.gif;*.webp)|*.png;*.jpg;*.jpeg;*.bmp;*.gif;*.webp|All Files (*.*)|*.*",
                CheckFileExists = true
            };

            if (ofd.ShowDialog(mw) == true)
            {
                try
                {
                    byte[] bytes = System.IO.File.ReadAllBytes(ofd.FileName);
                    item.CustomImagePath = ofd.FileName;
                    item.CustomImageData = Convert.ToBase64String(bytes);
                }
                catch { }
            }
        }
    }

    private void ItemContextMenu_RemoveImage_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel?.SelectedItems != null)
        {
            foreach (var item in ViewModel.SelectedItems)
            {
                item.CustomImagePath = null;
                item.CustomImageData = null;
            }
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

    private void ItemContextMenu_AssignIp_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel == null) return;
        var dlg = new Dialogs.BatchIpAssignmentDialog(ViewModel, ViewModel.SelectedItems.OfType<DeviceItemViewModel>())
        {
            Owner = Window.GetWindow(this)
        };
        dlg.ShowDialog();
    }

    private void CanvasContextMenu_BatchAssignIp_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel == null) return;
        var dlg = new Dialogs.BatchIpAssignmentDialog(ViewModel)
        {
            Owner = Window.GetWindow(this)
        };
        dlg.ShowDialog();
    }
}
