using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using LabScape.ViewModels.Layers;

namespace LabScape.App.Views;

public partial class LayersView : UserControl
{
    private Point _dragStartPoint;
    private LayerItemViewModel? _draggedItem;
    private bool _isSyncingSelectionFromCanvas;

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

    // Drag-and-Drop Reordering for Z-Index
    private void LayersListBox_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _dragStartPoint = e.GetPosition(null);
        _draggedItem = FindParentDataContext<LayerItemViewModel>(e.OriginalSource as DependencyObject);
    }

    private void LayersListBox_PreviewMouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed || _draggedItem == null) return;

        Point currentPos = e.GetPosition(null);
        Vector diff = _dragStartPoint - currentPos;

        if (Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance ||
            Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance)
        {
            var listBoxItem = FindVisualParent<ListBoxItem>(e.OriginalSource as DependencyObject);
            if (listBoxItem != null)
            {
                DragDrop.DoDragDrop(listBoxItem, _draggedItem, DragDropEffects.Move);
                _draggedItem = null;
            }
        }
    }

    private void LayersListBox_DragOver(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(typeof(LayerItemViewModel)))
        {
            e.Effects = DragDropEffects.Move;
            e.Handled = true;
        }
    }

    private void LayersListBox_Drop(object sender, DragEventArgs e)
    {
        if (ViewModel == null) return;

        if (e.Data.GetData(typeof(LayerItemViewModel)) is LayerItemViewModel droppedLayer)
        {
            var targetListBoxItem = FindVisualParent<ListBoxItem>(e.OriginalSource as DependencyObject);
            if (targetListBoxItem?.DataContext is LayerItemViewModel targetLayer)
            {
                int oldIndex = ViewModel.Layers.IndexOf(droppedLayer);
                int newIndex = ViewModel.Layers.IndexOf(targetLayer);

                if (oldIndex >= 0 && newIndex >= 0 && oldIndex != newIndex)
                {
                    ViewModel.MoveLayerInList(oldIndex, newIndex);
                }
            }
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
