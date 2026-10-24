using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LabScape.Core.Enums;
using LabScape.Core.Interfaces;
using LabScape.ViewModels.Commands;
using LabScape.ViewModels.Items;

namespace LabScape.ViewModels;

public partial class CanvasViewModel : ObservableObject
{
    private readonly IUndoRedoService _undoRedoService;

    public ObservableCollection<ItemViewModel> Items { get; } = new();
    public ObservableCollection<ItemViewModel> SelectedItems { get; } = new();

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

    public CanvasViewModel(IUndoRedoService undoRedoService)
    {
        _undoRedoService = undoRedoService;
        Items.CollectionChanged += (s, e) => RecalculateValidation();
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
        foreach (var item in SelectedItems.ToList())
        {
            item.IsSelected = false;
        }
        SelectedItems.Clear();
    }

    public void SetSelection(IEnumerable<ItemViewModel> items)
    {
        ClearSelection();
        foreach (var itm in items)
        {
            itm.IsSelected = true;
            SelectedItems.Add(itm);
        }
    }

    [RelayCommand]
    public void DeleteSelected()
    {
        if (SelectedItems.Count == 0) return;
        _undoRedoService.ExecuteCommand(new RemoveItemCommand(Items, SelectedItems.ToList()));
        ClearSelection();
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
    public void BringToFront()
    {
        if (SelectedItems.Count == 0 || Items.Count == 0) return;
        int maxZ = Items.Max(i => i.Z);
        foreach (var item in SelectedItems)
        {
            item.Z = ++maxZ;
        }
    }

    [RelayCommand]
    public void SendToBack()
    {
        if (SelectedItems.Count == 0 || Items.Count == 0) return;
        int minZ = Items.Min(i => i.Z);
        foreach (var item in SelectedItems)
        {
            item.Z = --minZ;
        }
    }

    [RelayCommand]
    public void BringForward()
    {
        foreach (var item in SelectedItems)
        {
            item.Z += 1;
        }
    }

    [RelayCommand]
    public void SendBackward()
    {
        foreach (var item in SelectedItems)
        {
            item.Z -= 1;
        }
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
