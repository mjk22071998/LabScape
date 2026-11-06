using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LabScape.Core.Collections;
using LabScape.ViewModels.Commands;
using LabScape.ViewModels.Items;

namespace LabScape.ViewModels.Layers;

public partial class LayersViewModel : ObservableObject
{
    private CanvasViewModel? _canvas;
    private readonly List<LayerItemViewModel> _allLayers = new();

    public ObservableRangeCollection<LayerItemViewModel> Layers { get; } = new();

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private LayerItemViewModel? _selectedLayer;

    [ObservableProperty]
    private int _totalLayersCount;

    [ObservableProperty]
    private int _lockedLayersCount;

    [ObservableProperty]
    private int _hiddenLayersCount;

    public CanvasViewModel? Canvas => _canvas;

    public LayersViewModel()
    {
    }

    public void AttachCanvas(CanvasViewModel? canvas)
    {
        if (_canvas != null)
        {
            _canvas.Items.CollectionChanged -= Canvas_Items_CollectionChanged;
            _canvas.SelectedItems.CollectionChanged -= Canvas_SelectedItems_CollectionChanged;
        }

        _canvas = canvas;
        RebuildLayers();

        if (_canvas != null)
        {
            _canvas.Items.CollectionChanged += Canvas_Items_CollectionChanged;
            _canvas.SelectedItems.CollectionChanged += Canvas_SelectedItems_CollectionChanged;
        }

        OnPropertyChanged(nameof(Canvas));
    }

    private void Canvas_Items_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        RebuildLayers();
    }

    private void Canvas_SelectedItems_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        SyncSelectionFromCanvas();
    }

    private void SyncSelectionFromCanvas()
    {
        if (_canvas == null) return;
        var firstSelected = _canvas.SelectedItems.FirstOrDefault();
        SelectedLayer = _allLayers.FirstOrDefault(l => l.Item == firstSelected);
        UpdateCounters();
    }

    public void RebuildLayers()
    {
        foreach (var l in _allLayers)
        {
            l.PropertyChanged -= Layer_PropertyChanged;
            l.Dispose();
        }
        _allLayers.Clear();

        if (_canvas != null)
        {
            foreach (var item in _canvas.Items)
            {
                var layer = new LayerItemViewModel(
                    item,
                    onRename: HandleRename,
                    onToggleLock: HandleToggleLock,
                    onToggleVisibility: HandleToggleVisibility);
                layer.PropertyChanged += Layer_PropertyChanged;
                _allLayers.Add(layer);
            }
        }

        RefreshVisibleLayers();
        SyncSelectionFromCanvas();
        UpdateCounters();
    }

    private void Layer_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(LayerItemViewModel.Z))
        {
            RefreshVisibleLayers();
        }
        else if (e.PropertyName is nameof(LayerItemViewModel.IsLocked) or nameof(LayerItemViewModel.IsVisible))
        {
            UpdateCounters();
        }
    }

    private void HandleRename(ItemViewModel item, string oldName, string newName)
    {
        if (_canvas != null)
        {
            _canvas.UndoRedoService.ExecuteCommand(new RenameItemCommand(item, oldName, newName));
        }
        else
        {
            item.Name = newName;
        }
    }

    private void HandleToggleLock(ItemViewModel item, bool isLocked)
    {
        if (_canvas != null)
        {
            _canvas.UndoRedoService.ExecuteCommand(new ToggleItemLockCommand(item, isLocked));
        }
        else
        {
            item.IsLocked = isLocked;
        }
        UpdateCounters();
    }

    private void HandleToggleVisibility(ItemViewModel item, bool isVisible)
    {
        if (_canvas != null)
        {
            _canvas.UndoRedoService.ExecuteCommand(new ToggleItemVisibilityCommand(item, isVisible));
        }
        else
        {
            item.IsVisible = isVisible;
        }
        UpdateCounters();
    }

    public void RefreshVisibleLayers()
    {
        var filtered = _allLayers
            .Where(FilterLayer)
            .OrderByDescending(l => l.Z) // Topmost element (highest Z) at the top of list
            .ToList();

        Layers.ReplaceAll(filtered);
    }

    public void UpdateCounters()
    {
        TotalLayersCount = _allLayers.Count;
        LockedLayersCount = _allLayers.Count(l => l.IsLocked);
        HiddenLayersCount = _allLayers.Count(l => !l.IsVisible);
    }

    partial void OnSearchTextChanged(string value)
    {
        RefreshVisibleLayers();
    }

    private bool FilterLayer(LayerItemViewModel layer)
    {
        if (string.IsNullOrWhiteSpace(SearchText)) return true;
        return layer.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase)
            || layer.ItemType.Contains(SearchText, StringComparison.OrdinalIgnoreCase)
            || layer.Subtitle.Contains(SearchText, StringComparison.OrdinalIgnoreCase);
    }

    // Selection helper from UI
    public void SelectLayer(LayerItemViewModel? layer, bool isMultiSelect = false)
    {
        if (_canvas == null || layer == null) return;
        _canvas.SelectItem(layer.Item, addToExisting: isMultiSelect);
        SelectedLayer = layer;
    }

    // Z-Order Canva/Photoshop style commands
    [RelayCommand]
    public void BringToFront()
    {
        _canvas?.BringToFront();
        RefreshVisibleLayers();
    }

    [RelayCommand]
    public void BringForward()
    {
        _canvas?.MoveForward();
        RefreshVisibleLayers();
    }

    [RelayCommand]
    public void SendBackward()
    {
        _canvas?.SendBackward();
        RefreshVisibleLayers();
    }

    [RelayCommand]
    public void SendToBack()
    {
        _canvas?.SendToBack();
        RefreshVisibleLayers();
    }

    // Bulk actions
    [RelayCommand]
    public void ToggleLockAll()
    {
        if (_canvas == null || _allLayers.Count == 0) return;
        bool anyUnlocked = _allLayers.Any(l => !l.IsLocked);
        _canvas.UndoRedoService.ExecuteCommand(new ToggleItemLockCommand(_allLayers.Select(l => l.Item), anyUnlocked));
        UpdateCounters();
    }

    [RelayCommand]
    public void ToggleVisibilityAll()
    {
        if (_canvas == null || _allLayers.Count == 0) return;
        bool anyHidden = _allLayers.Any(l => !l.IsVisible);
        _canvas.UndoRedoService.ExecuteCommand(new ToggleItemVisibilityCommand(_allLayers.Select(l => l.Item), anyHidden));
        UpdateCounters();
    }

    [RelayCommand]
    public void DeleteSelected()
    {
        _canvas?.DeleteSelected();
    }

    [RelayCommand]
    public void DuplicateSelected()
    {
        _canvas?.DuplicateSelected();
    }

    [RelayCommand]
    public void GroupSelected()
    {
        _canvas?.GroupSelected();
    }

    [RelayCommand]
    public void UngroupSelected()
    {
        _canvas?.UngroupSelected();
    }

    [RelayCommand]
    public void SelectAll()
    {
        if (_canvas == null) return;
        _canvas.SetSelection(_canvas.Items.Where(i => i.IsVisible));
    }

    // Move layer in list by old and new visible indices
    // Index 0 is the topmost layer (highest Z).
    public void MoveLayerInList(int oldIndex, int newIndex)
    {
        if (_canvas == null || oldIndex == newIndex) return;
        if (oldIndex < 0 || oldIndex >= Layers.Count || newIndex < 0 || newIndex >= Layers.Count) return;

        var list = Layers.ToList();
        var target = list[oldIndex];
        list.RemoveAt(oldIndex);
        list.Insert(newIndex, target);

        // Normalize Z indices from top to bottom
        // Topmost (index 0) gets highest Z
        int count = list.Count;
        var changes = new List<(ItemViewModel Item, int OldZ, int NewZ)>();
        for (int i = 0; i < count; i++)
        {
            int assignedZ = count - i;
            if (list[i].Item.Z != assignedZ)
            {
                changes.Add((list[i].Item, list[i].Item.Z, assignedZ));
            }
        }

        if (changes.Count > 0)
        {
            _canvas.UndoRedoService.ExecuteCommand(new ReorderLayerZIndexCommand(changes, "Reorder Layers"));
            RefreshVisibleLayers();
        }
    }
}
