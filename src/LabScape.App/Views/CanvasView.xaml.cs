using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
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

    private SelectionAdorner? _currentAdorner;
    private FrameworkElement? _adornedElement;

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

        if (e.LeftButton == MouseButtonState.Pressed && e.OriginalSource == sender)
        {
            // Clicked empty canvas space
            ViewModel?.ClearSelection();
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

        if (_isPanning && ViewModel != null)
        {
            Point current = e.GetPosition(this);
            ViewModel.PanX = _initialPanX + (current.X - _panStart.X);
            ViewModel.PanY = _initialPanY + (current.Y - _panStart.Y);
            return;
        }

        if (_isDraggingItem && ViewModel != null)
        {
            double deltaX = (canvasPos.X - _itemDragStart.X);
            double deltaY = (canvasPos.Y - _itemDragStart.Y);

            foreach (var (item, startPos) in _initialItemPositions)
            {
                double targetX = startPos.X + deltaX;
                double targetY = startPos.Y + deltaY;

                if (ViewModel.SnapToGrid)
                {
                    targetX = Math.Round(targetX / ViewModel.GridSize) * ViewModel.GridSize;
                    targetY = Math.Round(targetY / ViewModel.GridSize) * ViewModel.GridSize;
                }

                item.X = Math.Max(0, targetX);
                item.Y = Math.Max(0, targetY);
            }
        }
    }

    private void Viewport_MouseUp(object sender, MouseButtonEventArgs e)
    {
        if (_isPanning)
        {
            _isPanning = false;
            Mouse.Capture(null);
            Cursor = Cursors.Arrow;
        }

        if (_isDraggingItem)
        {
            _isDraggingItem = false;
            Mouse.Capture(null);
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
            // Zoom centering at cursor:
            // mouseScreen = mouseCanvas * newZoom + pan
            ViewModel.PanX = mouseScreen.X - mouseCanvas.X * newZoom;
            ViewModel.PanY = mouseScreen.Y - mouseCanvas.Y * newZoom;
            ViewModel.ZoomLevel = newZoom;
        }

        e.Handled = true;
    }

    private void Item_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed) return;
        if (sender is not FrameworkElement elem || elem.DataContext is not ItemViewModel itemVm) return;
        if (ViewModel == null) return;

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

        // Start Dragging
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

        // Find visual element
        var presenter = ItemsHost.ItemContainerGenerator.ContainerFromItem(selectedItem) as FrameworkElement;
        if (presenter == null) return;

        var layer = AdornerLayer.GetAdornerLayer(presenter);
        if (layer != null)
        {
            _adornedElement = presenter;
            _currentAdorner = new SelectionAdorner(presenter, selectedItem, () =>
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
            e.Handled = true;
        }
    }

    private void Viewport_Drop(object sender, DragEventArgs e)
    {
        if (ViewModel == null) return;
        if (e.Data.GetData(typeof(StencilItem)) is StencilItem stencil)
        {
            Point p = e.GetPosition(CanvasSurface);
            var newItem = stencil.CreateItem();
            newItem.X = Math.Max(0, p.X - newItem.Width / 2.0);
            newItem.Y = Math.Max(0, p.Y - newItem.Height / 2.0);
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
