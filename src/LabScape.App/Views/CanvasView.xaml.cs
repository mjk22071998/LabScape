using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Shapes;
using LabScape.App.Adorners;
using LabScape.ViewModels;
using LabScape.ViewModels.Items;
using LabScape.ViewModels.Stencils;

namespace LabScape.App.Views;

public partial class CanvasView : UserControl
{
    private bool _isPanning;
    private Point _panStart;
    private double _initialPanX, _initialPanY;

    private bool _isDraggingItem;
    private Point _itemDragStart;
    private readonly Dictionary<ItemViewModel, Point> _initialItemPositions = new();

    // Marquee / Rubberband selection state
    private bool _isSelectingMarquee;
    private Point _marqueeStart;

    private SelectionAdorner? _currentAdorner;
    private FrameworkElement? _adornedElement;

    private const double SnapTolerance = 6.0;

    public CanvasView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is CanvasViewModel vm)
        {
            vm.SelectedItems.CollectionChanged += (s, ev) => UpdateAdorner();
        }
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

        // 2. Click on empty canvas surface
        if (e.RightButton == MouseButtonState.Pressed && (e.OriginalSource == sender || e.OriginalSource == CanvasSurface))
        {
            ViewModel?.ClearSelection();
        }

        if (e.LeftButton == MouseButtonState.Pressed && (e.OriginalSource == sender || e.OriginalSource == CanvasSurface))
        {
            bool isCtrl = (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control;
            if (!isCtrl)
            {
                ViewModel?.ClearSelection();
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
        if (_isDraggingItem && ViewModel != null && _initialItemPositions.Count > 0)
        {
            double deltaX = canvasPos.X - _itemDragStart.X;
            double deltaY = canvasPos.Y - _itemDragStart.Y;

            // Pick the primary dragged item for alignment snapping
            var primary = _initialItemPositions.Keys.First();
            var primaryStart = _initialItemPositions[primary];

            double targetPrimaryX = primaryStart.X + deltaX;
            double targetPrimaryY = primaryStart.Y + deltaY;

            // Standard Snap to Grid
            if (ViewModel.SnapToGrid)
            {
                targetPrimaryX = Math.Round(targetPrimaryX / ViewModel.GridSize) * ViewModel.GridSize;
                targetPrimaryY = Math.Round(targetPrimaryY / ViewModel.GridSize) * ViewModel.GridSize;
            }

            // Magnetic Alignment Snapping against other canvas items
            var otherItems = ViewModel.Items.Where(i => !_initialItemPositions.ContainsKey(i)).ToList();
            bool snapXFound = false;
            bool snapYFound = false;

            double pLeft = targetPrimaryX;
            double pCenter = targetPrimaryX + primary.Width / 2.0;
            double pRight = targetPrimaryX + primary.Width;

            double pTop = targetPrimaryY;
            double pMiddle = targetPrimaryY + primary.Height / 2.0;
            double pBottom = targetPrimaryY + primary.Height;

            foreach (var other in otherItems)
            {
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
                item.X = Math.Max(0, startPos.X + finalDeltaX);
                item.Y = Math.Max(0, startPos.Y + finalDeltaY);
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

                if (matched.Count > 0)
                {
                    ViewModel.SetSelection(matched);
                }
            }
        }

        if (_isDraggingItem && ViewModel != null)
        {
            _isDraggingItem = false;
            Mouse.Capture(null);
            SnapGuideVertical.Visibility = Visibility.Collapsed;
            SnapGuideHorizontal.Visibility = Visibility.Collapsed;

            // Commit atomic undoable Move Command
            var moves = _initialItemPositions.Select(kv => (kv.Key, kv.Value.X, kv.Value.Y, kv.Key.X, kv.Key.Y)).ToList();
            ViewModel.CommitItemMoves(moves);
            _initialItemPositions.Clear();
        }
    }

    private void Viewport_MouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (ViewModel == null) return;

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

        e.Handled = true;
    }

    private void Item_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is not FrameworkElement elem || elem.DataContext is not ItemViewModel itemVm) return;
        if (ViewModel == null) return;

        if (e.RightButton == MouseButtonState.Pressed)
        {
            if (!itemVm.IsSelected)
            {
                ViewModel.SelectItem(itemVm, addToExisting: false);
            }
            return;
        }

        if (e.LeftButton != MouseButtonState.Pressed) return;

        bool isCtrl = (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control;
        if (isCtrl)
        {
            if (itemVm.IsSelected)
                ViewModel.DeselectItem(itemVm);
            else
                ViewModel.SelectItem(itemVm, addToExisting: true);
        }
        else
        {
            if (!itemVm.IsSelected)
            {
                ViewModel.SelectItem(itemVm, addToExisting: false);
            }
        }

        // Start Dragging items
        _isDraggingItem = true;
        _itemDragStart = e.GetPosition(CanvasSurface);
        _initialItemPositions.Clear();
        foreach (var sel in ViewModel.SelectedItems)
        {
            _initialItemPositions[sel] = new Point(sel.X, sel.Y);
        }

        Mouse.Capture((IInputElement)sender);
        e.Handled = true;
    }

    private void UpdateAdorner()
    {
        RemoveCurrentAdorner();

        if (ViewModel == null || ViewModel.SelectedItems.Count != 1) return;
        var selectedItem = ViewModel.SelectedItems[0];

        // Find visual element container
        var presenter = ItemsHost.ItemContainerGenerator.ContainerFromItem(selectedItem) as FrameworkElement;
        if (presenter == null) return;

        var layer = AdornerLayer.GetAdornerLayer(presenter);
        if (layer != null)
        {
            _adornedElement = presenter;
            _currentAdorner = new SelectionAdorner(presenter, selectedItem, ViewModel.UndoRedoService, () =>
            {
                if (selectedItem is GroupItemViewModel grp) grp.RecalculateBounds();
            });
            layer.Add(_currentAdorner);
        }
    }

    private void RemoveCurrentAdorner()
    {
        if (_currentAdorner != null && _adornedElement != null)
        {
            var layer = AdornerLayer.GetAdornerLayer(_adornedElement);
            layer?.Remove(_currentAdorner);
            _currentAdorner = null;
            _adornedElement = null;
        }
    }

    private void Viewport_DragOver(object sender, DragEventArgs e)
    {
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

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (ViewModel == null) return;

        bool isCtrl = (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control;
        bool isShift = (Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift;

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
    }
}
