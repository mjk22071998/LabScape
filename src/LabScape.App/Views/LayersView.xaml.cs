using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using LabScape.App.Adorners;
using LabScape.ViewModels.Layers;
using LabScape.ViewModels.Stencils;

namespace LabScape.App.Views;

public partial class LayersView : UserControl
{
    private Point _dragStartPoint;
    private LayerItemViewModel? _draggedItem;
    private ListBoxItem? _dragSourceContainer;
    private bool _isSyncingSelectionFromCanvas;

    // Visual drop insertion indicator adorner
    private LayerDropIndicatorAdorner? _dropIndicatorAdorner;
    private ListBoxItem? _adornedItem;

    public LayersViewModel? ViewModel => DataContext as LayersViewModel;

    public LayersView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.OldValue is LayersViewModel oldVm)
        {
            oldVm.PropertyChanged -= ViewModel_PropertyChanged;
        }
        if (e.NewValue is LayersViewModel newVm)
        {
            newVm.PropertyChanged += ViewModel_PropertyChanged;
            SyncSelectionToUI();
        }
    }

    private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(LayersViewModel.SelectedLayer))
        {
            SyncSelectionToUI();
        }
    }

    private void SyncSelectionToUI()
    {
        if (ViewModel?.Canvas == null) return;
        _isSyncingSelectionFromCanvas = true;
        try
        {
            var selectedSet = new HashSet<object>(
                ViewModel.Layers.Cast<LayerItemViewModel>().Where(l => l.IsSelected));

            LayersListBox.SelectedItems.Clear();
            foreach (var item in selectedSet)
            {
                LayersListBox.SelectedItems.Add(item);
            }
        }
        finally
        {
            _isSyncingSelectionFromCanvas = false;
        }
    }

    private void LayersListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isSyncingSelectionFromCanvas || ViewModel?.Canvas == null) return;

        var selectedLayers = LayersListBox.SelectedItems.OfType<LayerItemViewModel>().ToList();
        ViewModel.Canvas.SetSelection(selectedLayers.Select(l => l.Item));
    }

    private void ClearSearch_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel != null)
        {
            ViewModel.SearchText = string.Empty;
        }
    }

    private void LayerName_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2 && sender is FrameworkElement elem && elem.DataContext is LayerItemViewModel layer)
        {
            layer.BeginEdit();
            e.Handled = true;
        }
    }

    private void InlineEditBox_Loaded(object sender, RoutedEventArgs e)
    {
        if (sender is TextBox tb)
        {
            tb.Focus();
            tb.SelectAll();
        }
    }

    private void InlineEditBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (sender is TextBox tb && tb.DataContext is LayerItemViewModel layer)
        {
            if (e.Key == Key.Enter)
            {
                layer.CommitEdit();
                e.Handled = true;
            }
            else if (e.Key == Key.Escape)
            {
                layer.CancelEdit();
                e.Handled = true;
            }
        }
    }

    private void InlineEditBox_LostFocus(object sender, RoutedEventArgs e)
    {
        if (sender is TextBox tb && tb.DataContext is LayerItemViewModel layer)
        {
            layer.CommitEdit();
        }
    }

    // --- Drag-and-Drop Reordering for Z-Index (Canva/Photoshop Style) ---

    private void LayersListBox_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        // Don't initiate drag if clicking on interactive child buttons or inline text boxes
        if (FindVisualParent<Button>(e.OriginalSource as DependencyObject) != null ||
            FindVisualParent<TextBox>(e.OriginalSource as DependencyObject) != null)
        {
            _draggedItem = null;
            _dragSourceContainer = null;
            return;
        }

        _dragStartPoint = e.GetPosition(this);
        _dragSourceContainer = FindVisualParent<ListBoxItem>(e.OriginalSource as DependencyObject);
        _draggedItem = _dragSourceContainer?.DataContext as LayerItemViewModel
                       ?? FindParentDataContext<LayerItemViewModel>(e.OriginalSource as DependencyObject);
    }

    private void LayersListBox_PreviewMouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed || _draggedItem == null) return;

        Point currentPos = e.GetPosition(this);
        Vector diff = _dragStartPoint - currentPos;

        if (Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance ||
            Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance)
        {
            var container = _dragSourceContainer ??
                            (LayersListBox.ItemContainerGenerator.ContainerFromItem(_draggedItem) as ListBoxItem);
            if (container != null)
            {
                try
                {
                    // If multiple layers are selected and user dragged one of them, move all selected layers together
                    var selectedLayers = LayersListBox.SelectedItems.OfType<LayerItemViewModel>().ToList();
                    List<LayerItemViewModel> itemsToDrag = (selectedLayers.Contains(_draggedItem) && selectedLayers.Count > 1)
                        ? selectedLayers
                        : new List<LayerItemViewModel> { _draggedItem };

                    var dragData = new DataObject();
                    dragData.SetData(typeof(LayerItemViewModel), _draggedItem);
                    dragData.SetData(typeof(List<LayerItemViewModel>), itemsToDrag);

                    DragDrop.DoDragDrop(container, dragData, DragDropEffects.Move);
                }
                finally
                {
                    RemoveDropIndicator();
                    _draggedItem = null;
                    _dragSourceContainer = null;
                }
            }
        }
    }

    private void LayersListBox_DragEnter(object sender, DragEventArgs e)
    {
        HandleDragOverOrEnter(e);
    }

    private void LayersListBox_DragOver(object sender, DragEventArgs e)
    {
        HandleDragOverOrEnter(e);
    }

    private void HandleDragOverOrEnter(DragEventArgs e)
    {
        if (ViewModel == null)
        {
            e.Effects = DragDropEffects.None;
            RemoveDropIndicator();
            return;
        }

        if (e.Data.GetDataPresent(typeof(LayerItemViewModel)) || e.Data.GetDataPresent(typeof(List<LayerItemViewModel>)))
        {
            var target = GetDropTarget(e);
            if (target != null)
            {
                e.Effects = DragDropEffects.Move;
                UpdateDropIndicator(target.Value.Container, target.Value.IsUpperHalf);
                e.Handled = true;
                return;
            }
        }
        else if (e.Data.GetDataPresent(typeof(StencilItem)))
        {
            var target = GetDropTarget(e);
            if (target != null)
            {
                e.Effects = DragDropEffects.Copy;
                UpdateDropIndicator(target.Value.Container, target.Value.IsUpperHalf);
                e.Handled = true;
                return;
            }
        }

        e.Effects = DragDropEffects.None;
        RemoveDropIndicator();
        e.Handled = true;
    }

    private void LayersListBox_DragLeave(object sender, DragEventArgs e)
    {
        Point p = e.GetPosition(LayersListBox);
        if (p.X < 0 || p.Y < 0 || p.X >= LayersListBox.ActualWidth || p.Y >= LayersListBox.ActualHeight)
        {
            RemoveDropIndicator();
        }
    }

    private void LayersListBox_Drop(object sender, DragEventArgs e)
    {
        RemoveDropIndicator();
        if (ViewModel == null) return;

        var target = GetDropTarget(e);
        if (target == null) return;

        int targetIndex = target.Value.TargetIndex;
        if (targetIndex < 0 || targetIndex >= ViewModel.Layers.Count) return;

        // 1. Reordering layers within the stack
        if (e.Data.GetDataPresent(typeof(List<LayerItemViewModel>)) || e.Data.GetDataPresent(typeof(LayerItemViewModel)))
        {
            List<LayerItemViewModel> movingLayers;
            if (e.Data.GetData(typeof(List<LayerItemViewModel>)) is List<LayerItemViewModel> list && list.Count > 0)
            {
                movingLayers = list;
            }
            else if (e.Data.GetData(typeof(LayerItemViewModel)) is LayerItemViewModel single)
            {
                movingLayers = new List<LayerItemViewModel> { single };
            }
            else
            {
                return;
            }

            ViewModel.MoveLayersInList(movingLayers, targetIndex, target.Value.IsUpperHalf);
            e.Handled = true;
            return;
        }

        // 2. Inserting stencil into canvas at target layer Z-index position
        if (e.Data.GetData(typeof(StencilItem)) is StencilItem stencil && ViewModel.Canvas != null)
        {
            var newItem = stencil.CreateItem();
            newItem.X = Math.Max(0, ViewModel.Canvas.PanX + 200);
            newItem.Y = Math.Max(0, ViewModel.Canvas.PanY + 150);
            ViewModel.Canvas.AddItem(newItem);

            var createdLayer = ViewModel.Layers.FirstOrDefault(l => l.Item == newItem);
            if (createdLayer != null)
            {
                ViewModel.MoveLayersInList(new[] { createdLayer }, targetIndex, target.Value.IsUpperHalf);
            }

            e.Handled = true;
        }
    }

    private (LayerItemViewModel TargetLayer, ListBoxItem Container, int TargetIndex, bool IsUpperHalf)? GetDropTarget(DragEventArgs e)
    {
        if (ViewModel == null || ViewModel.Layers.Count == 0) return null;

        Point p = e.GetPosition(LayersListBox);
        var hit = VisualTreeHelper.HitTest(LayersListBox, p);
        var listBoxItem = FindVisualParent<ListBoxItem>(hit?.VisualHit);

        if (listBoxItem != null && listBoxItem.DataContext is LayerItemViewModel targetLayer)
        {
            int index = ViewModel.Layers.IndexOf(targetLayer);
            if (index >= 0)
            {
                Point itemP = e.GetPosition(listBoxItem);
                bool isUpper = itemP.Y < (listBoxItem.ActualHeight / 2.0);
                return (targetLayer, listBoxItem, index, isUpper);
            }
        }

        // Boundary checks for empty whitespace above or below items in the scrollable view
        int count = ViewModel.Layers.Count;
        if (count > 0)
        {
            var lastLayer = ViewModel.Layers[count - 1];
            if (LayersListBox.ItemContainerGenerator.ContainerFromItem(lastLayer) is ListBoxItem lastContainer)
            {
                Point lastP = e.GetPosition(lastContainer);
                if (lastP.Y > lastContainer.ActualHeight / 2.0)
                {
                    return (lastLayer, lastContainer, count - 1, false);
                }
            }

            var firstLayer = ViewModel.Layers[0];
            if (LayersListBox.ItemContainerGenerator.ContainerFromItem(firstLayer) is ListBoxItem firstContainer)
            {
                Point firstP = e.GetPosition(firstContainer);
                if (firstP.Y < firstContainer.ActualHeight / 2.0)
                {
                    return (firstLayer, firstContainer, 0, true);
                }
            }
        }

        return null;
    }

    private void UpdateDropIndicator(ListBoxItem targetItem, bool isUpperHalf)
    {
        if (_adornedItem == targetItem && _dropIndicatorAdorner != null)
        {
            _dropIndicatorAdorner.IsUpperHalf = isUpperHalf;
            return;
        }

        RemoveDropIndicator();

        var adornerLayer = AdornerLayer.GetAdornerLayer(targetItem);
        if (adornerLayer != null)
        {
            _adornedItem = targetItem;
            _dropIndicatorAdorner = new LayerDropIndicatorAdorner(targetItem, isUpperHalf);
            adornerLayer.Add(_dropIndicatorAdorner);
        }
    }

    private void RemoveDropIndicator()
    {
        if (_dropIndicatorAdorner != null && _adornedItem != null)
        {
            var adornerLayer = AdornerLayer.GetAdornerLayer(_adornedItem);
            adornerLayer?.Remove(_dropIndicatorAdorner);
            _dropIndicatorAdorner = null;
            _adornedItem = null;
        }
    }

    private static T? FindVisualParent<T>(DependencyObject? child) where T : DependencyObject
    {
        while (child != null)
        {
            if (child is T parent) return parent;
            child = VisualTreeHelper.GetParent(child);
        }
        return null;
    }

    private static T? FindParentDataContext<T>(DependencyObject? child) where T : class
    {
        while (child != null)
        {
            if (child is FrameworkElement fe && fe.DataContext is T dc) return dc;
            child = VisualTreeHelper.GetParent(child);
        }
        return null;
    }
}
