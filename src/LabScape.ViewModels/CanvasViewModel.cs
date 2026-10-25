using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LabScape.Core.Collections;
using LabScape.Core.Enums;
using LabScape.Core.Interfaces;
using LabScape.ViewModels.Commands;
using LabScape.ViewModels.Items;
using LabScape.ViewModels.Services;
using LabScape.ViewModels.Templates;

namespace LabScape.ViewModels;

public partial class CanvasViewModel : ObservableObject
{
    private readonly IUndoRedoService _undoRedoService;

    public ObservableRangeCollection<ItemViewModel> Items { get; } = new();
    public ObservableRangeCollection<ItemViewModel> SelectedItems { get; } = new();

    [ObservableProperty]
    private double _zoomLevel = 1.0;

    [ObservableProperty]
    private double _panX = 0;

    [ObservableProperty]
    private double _panY = 0;

    [ObservableProperty]
    private double _canvasWidth = 3000;

    [ObservableProperty]
    private double _canvasHeight = 2000;

    [ObservableProperty]
    private double _gridSize = 20.0;

    [ObservableProperty]
    private bool _showGrid = true;

    [ObservableProperty]
    private bool _snapToGrid = true;

    [ObservableProperty]
    private bool _showRulers = true;

    [ObservableProperty]
    private double _cursorX = 0;

    [ObservableProperty]
    private double _cursorY = 0;

    [ObservableProperty]
    private SimulationBenchmarkResult? _lastBenchmarkResult;

    public IUndoRedoService UndoRedoService => _undoRedoService;

    public CanvasViewModel(IUndoRedoService undoRedoService)
    {
        _undoRedoService = undoRedoService;
        Items.CollectionChanged += (s, e) => RecalculateValidation();
    }

    public void CommitItemMoves(List<(ItemViewModel Item, double OldX, double OldY, double NewX, double NewY)> moves)
    {
        if (moves.Count == 0) return;
        var significantMoves = moves.Where(m => Math.Abs(m.OldX - m.NewX) > 0.1 || Math.Abs(m.OldY - m.NewY) > 0.1).ToList();
        if (significantMoves.Count > 0)
        {
            _undoRedoService.ExecuteCommand(new MoveItemsCommand(significantMoves));
        }
    }

    public void AddItem(ItemViewModel item)
    {
        // Calculate next Z-index
        int maxZ = Items.Count > 0 ? Items.Max(i => i.Z) : 0;
        item.Z = maxZ + 1;

        if (SnapToGrid)
        {
            item.X = Math.Round(item.X / GridSize) * GridSize;
            item.Y = Math.Round(item.Y / GridSize) * GridSize;
        }

        _undoRedoService.ExecuteCommand(new AddItemCommand(Items, item));
        SetSelection(new[] { item });
    }

    public void AddRoomTemplate(RoomTemplateOptions options)
    {
        var result = RoomTemplateGenerator.Generate(options);
        if (options.GroupElements && result.Group != null)
        {
            AddItem(result.Group);
        }
        else
        {
            int maxZ = Items.Count > 0 ? Items.Max(i => i.Z) : 0;
            foreach (var item in result.AllItems)
            {
                item.Z = ++maxZ;
            }
            _undoRedoService.ExecuteCommand(new AddItemsBatchCommand(Items, result.AllItems));
            SetSelection(result.AllItems);
        }
    }

    public void SelectItem(ItemViewModel item, bool addToExisting = false)
    {
        if (!addToExisting)
        {
            ClearSelection();
        }
        item.IsSelected = true;
        if (!SelectedItems.Contains(item))
        {
            SelectedItems.Add(item);
        }
    }

    public void DeselectItem(ItemViewModel item)
    {
        item.IsSelected = false;
        SelectedItems.Remove(item);
    }

    public void ClearSelection()
    {
        if (SelectedItems.Count == 0) return;
        foreach (var item in SelectedItems)
        {
            item.IsSelected = false;
        }
        SelectedItems.Clear();
    }

    public void SetSelection(IEnumerable<ItemViewModel> items)
    {
        var targetList = items.ToList();

        // 1. Unselect items that are no longer selected
        var targetSet = new HashSet<ItemViewModel>(targetList);
        foreach (var item in SelectedItems)
        {
            if (!targetSet.Contains(item))
            {
                item.IsSelected = false;
            }
        }

        // 2. Select items
        foreach (var itm in targetList)
        {
            itm.IsSelected = true;
        }

        // 3. Batch replace with a single Reset notification
        SelectedItems.ReplaceAll(targetList);
    }

    [RelayCommand]
    public void DeleteSelected()
    {
        if (SelectedItems.Count == 0) return;
        _undoRedoService.ExecuteCommand(new RemoveItemCommand(Items, SelectedItems.ToList()));
        ClearSelection();
    }

    [RelayCommand]
    public void ClearAll()
    {
        if (Items.Count == 0) return;
        _undoRedoService.ExecuteCommand(new RemoveItemCommand(Items, Items.ToList()));
        ClearSelection();
    }

    [RelayCommand]
    public void Simulate1kStressTest()
    {
        LastBenchmarkResult = CanvasStressSimulator.Simulate1kItemsOnCanvas(this);
    }

    public SimulationBenchmarkResult RunSimulationBenchmark()
    {
        var result = CanvasStressSimulator.Simulate1kItemsOnCanvas(this);
        LastBenchmarkResult = result;
        return result;
    }

    [RelayCommand]
    public void SelectAll()
    {
        SetSelection(Items);
    }

    [RelayCommand]
    public void CutSelected()
    {
        if (SelectedItems.Count == 0) return;
        LayoutClipboard.Copy(SelectedItems);
        _undoRedoService.ExecuteCommand(new RemoveItemCommand(Items, SelectedItems.ToList()));
        ClearSelection();
    }

    [RelayCommand]
    public void CopySelected()
    {
        if (SelectedItems.Count == 0) return;
        LayoutClipboard.Copy(SelectedItems);
    }

    [RelayCommand]
    public void Paste()
    {
        if (!LayoutClipboard.HasContent) return;
        var newItems = LayoutClipboard.Paste(GridSize, GridSize);
        if (newItems.Count == 0) return;

        // Ensure unique Z
        int maxZ = Items.Count > 0 ? Items.Max(i => i.Z) : 0;
        foreach (var item in newItems)
        {
            item.Z = ++maxZ;
        }

        _undoRedoService.ExecuteCommand(new AddItemsBatchCommand(Items, newItems));
        SetSelection(newItems);
    }

    [RelayCommand]
    public void DuplicateSelected()
    {
        if (SelectedItems.Count == 0) return;
        var duplicates = LayoutClipboard.Duplicate(SelectedItems, GridSize, GridSize);
        if (duplicates.Count == 0) return;

        int maxZ = Items.Count > 0 ? Items.Max(i => i.Z) : 0;
        foreach (var item in duplicates)
        {
            item.Z = ++maxZ;
        }

        _undoRedoService.ExecuteCommand(new AddItemsBatchCommand(Items, duplicates));
        SetSelection(duplicates);
    }

    public void Nudge(double deltaX, double deltaY)
    {
        if (SelectedItems.Count == 0) return;
        var moves = SelectedItems.Select(i => (i, i.X, i.Y, i.X + deltaX, i.Y + deltaY)).ToList();
        _undoRedoService.ExecuteCommand(new MoveItemsCommand(moves));
    }

    [RelayCommand]
    public void GroupSelected()
    {
        // Need at least 2 items to form a group
        if (SelectedItems.Count < 2) return;
        _undoRedoService.ExecuteCommand(new GroupItemsCommand(Items, SelectedItems.ToList(), SetSelection));
    }

    [RelayCommand]
    public void UngroupSelected()
    {
        var groups = SelectedItems.OfType<GroupItemViewModel>().ToList();
        if (groups.Count == 0) return;
        _undoRedoService.ExecuteCommand(new UngroupItemsCommand(Items, groups, SetSelection));
    }

    [RelayCommand]
    public void MoveToFront()
    {
        if (SelectedItems.Count == 0 || Items.Count == 0) return;
        var nonSelected = Items.Except(SelectedItems).ToList();
        int nonSelectedMax = nonSelected.Count > 0 ? nonSelected.Max(i => i.Z) : 0;
        var orderedSelected = SelectedItems.OrderBy(i => i.Z).ToList();
        int nextZ = Math.Max(nonSelectedMax + 1, orderedSelected.First().Z);
        var changes = new List<(ItemViewModel Item, int OldZ, int NewZ)>();
        for (int i = 0; i < orderedSelected.Count; i++)
        {
            changes.Add((orderedSelected[i], orderedSelected[i].Z, nextZ + i));
        }
        _undoRedoService.ExecuteCommand(new ReorderLayerZIndexCommand(changes, "Move to Front"));
    }

    [RelayCommand]
    public void BringToFront() => MoveToFront();

    [RelayCommand]
    public void SendToBack()
    {
        if (SelectedItems.Count == 0 || Items.Count == 0) return;
        var nonSelected = Items.Except(SelectedItems).ToList();
        int nonSelectedMin = nonSelected.Count > 0 ? nonSelected.Min(i => i.Z) : 0;
        var orderedSelected = SelectedItems.OrderBy(i => i.Z).ToList();
        int baseZ = nonSelectedMin - orderedSelected.Count;
        var changes = new List<(ItemViewModel Item, int OldZ, int NewZ)>();
        for (int i = 0; i < orderedSelected.Count; i++)
        {
            changes.Add((orderedSelected[i], orderedSelected[i].Z, baseZ + i));
        }
        _undoRedoService.ExecuteCommand(new ReorderLayerZIndexCommand(changes, "Send to Back"));
    }

    [RelayCommand]
    public void MoveForward()
    {
        if (SelectedItems.Count == 0 || Items.Count == 0) return;
        var changes = new List<(ItemViewModel Item, int OldZ, int NewZ)>();
        foreach (var item in SelectedItems.OrderByDescending(i => i.Z))
        {
            var above = Items.Where(i => !SelectedItems.Contains(i) && i.Z >= item.Z).OrderBy(i => i.Z).FirstOrDefault();
            int newZ = above != null ? above.Z + 1 : item.Z + 1;
            changes.Add((item, item.Z, newZ));
        }
        _undoRedoService.ExecuteCommand(new ReorderLayerZIndexCommand(changes, "Move Forward"));
    }

    [RelayCommand]
    public void BringForward() => MoveForward();

    [RelayCommand]
    public void SendBackward()
    {
        if (SelectedItems.Count == 0 || Items.Count == 0) return;
        var changes = new List<(ItemViewModel Item, int OldZ, int NewZ)>();
        foreach (var item in SelectedItems.OrderBy(i => i.Z))
        {
            var below = Items.Where(i => !SelectedItems.Contains(i) && i.Z <= item.Z).OrderByDescending(i => i.Z).FirstOrDefault();
            int newZ = below != null ? below.Z - 1 : item.Z - 1;
            changes.Add((item, item.Z, newZ));
        }
        _undoRedoService.ExecuteCommand(new ReorderLayerZIndexCommand(changes, "Send Backward"));
    }

    [RelayCommand]
    public void AlignLeft()
    {
        if (SelectedItems.Count < 2) return;
        double minX = SelectedItems.Min(i => i.X);
        var moves = SelectedItems.Select(i => (i, i.X, i.Y, minX, i.Y)).ToList();
        _undoRedoService.ExecuteCommand(new MoveItemsCommand(moves));
    }

    [RelayCommand]
    public void AlignCenter()
    {
        if (SelectedItems.Count < 2) return;
        double avgCenter = SelectedItems.Average(i => i.X + i.Width / 2.0);
        var moves = SelectedItems.Select(i => (i, i.X, i.Y, avgCenter - i.Width / 2.0, i.Y)).ToList();
        _undoRedoService.ExecuteCommand(new MoveItemsCommand(moves));
    }

    [RelayCommand]
    public void AlignRight()
    {
        if (SelectedItems.Count < 2) return;
        double maxRight = SelectedItems.Max(i => i.X + i.Width);
        var moves = SelectedItems.Select(i => (i, i.X, i.Y, maxRight - i.Width, i.Y)).ToList();
        _undoRedoService.ExecuteCommand(new MoveItemsCommand(moves));
    }

    [RelayCommand]
    public void AlignTop()
    {
        if (SelectedItems.Count < 2) return;
        double minY = SelectedItems.Min(i => i.Y);
        var moves = SelectedItems.Select(i => (i, i.X, i.Y, i.X, minY)).ToList();
        _undoRedoService.ExecuteCommand(new MoveItemsCommand(moves));
    }

    [RelayCommand]
    public void AlignMiddle()
    {
        if (SelectedItems.Count < 2) return;
        double avgMiddle = SelectedItems.Average(i => i.Y + i.Height / 2.0);
        var moves = SelectedItems.Select(i => (i, i.X, i.Y, i.X, avgMiddle - i.Height / 2.0)).ToList();
        _undoRedoService.ExecuteCommand(new MoveItemsCommand(moves));
    }

    [RelayCommand]
    public void AlignBottom()
    {
        if (SelectedItems.Count < 2) return;
        double maxBottom = SelectedItems.Max(i => i.Y + i.Height);
        var moves = SelectedItems.Select(i => (i, i.X, i.Y, i.X, maxBottom - i.Height)).ToList();
        _undoRedoService.ExecuteCommand(new MoveItemsCommand(moves));
    }

    [RelayCommand]
    public void ZoomIn()
    {
        ZoomLevel = Math.Min(10.0, Math.Round((ZoomLevel + 0.15) * 100) / 100);
    }

    [RelayCommand]
    public void ZoomOut()
    {
        ZoomLevel = Math.Max(0.10, Math.Round((ZoomLevel - 0.15) * 100) / 100);
    }

    [RelayCommand]
    public void ResetZoom()
    {
        ZoomLevel = 1.0;
        PanX = 0;
        PanY = 0;
    }

    [RelayCommand]
    public void Undo() => _undoRedoService.Undo();

    [RelayCommand]
    public void Redo() => _undoRedoService.Redo();

    public bool CanUndo => _undoRedoService.CanUndo;
    public bool CanRedo => _undoRedoService.CanRedo;

    public void RecalculateValidation()
    {
        var devices = GetAllDevices(Items).ToList();
        var ipGroups = devices
            .Where(d => !string.IsNullOrWhiteSpace(d.IpAddress))
            .GroupBy(d => d.IpAddress!.Trim())
            .ToDictionary(g => g.Key, g => g.ToList());

        foreach (var dev in devices)
        {
            if (string.IsNullOrWhiteSpace(dev.IpAddress))
            {
                dev.ValidationStatus = ValidationStatus.Unassigned;
            }
            else if (ipGroups.TryGetValue(dev.IpAddress.Trim(), out var matches) && matches.Count > 1)
            {
                dev.ValidationStatus = ValidationStatus.DuplicateIp;
            }
            else
            {
                dev.ValidationStatus = ValidationStatus.Valid;
            }
        }
    }

    private IEnumerable<DeviceItemViewModel> GetAllDevices(IEnumerable<ItemViewModel> list)
    {
        foreach (var itm in list)
        {
            if (itm is DeviceItemViewModel dev)
            {
                yield return dev;
            }
            else if (itm is GroupItemViewModel grp)
            {
                foreach (var childDev in GetAllDevices(grp.Children))
                {
                    yield return childDev;
                }
            }
        }
    }
}
