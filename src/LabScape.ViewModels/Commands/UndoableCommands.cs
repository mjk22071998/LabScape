using System.Collections.ObjectModel;
using LabScape.Core.Collections;
using LabScape.Core.Interfaces;
using LabScape.ViewModels.Items;

namespace LabScape.ViewModels.Commands;

public class AddItemCommand : IUndoableCommand
{
    private readonly ObservableCollection<ItemViewModel> _collection;
    private readonly ItemViewModel _item;

    public string Description => $"Add {_item.Name}";

    public AddItemCommand(ObservableCollection<ItemViewModel> collection, ItemViewModel item)
    {
        _collection = collection;
        _item = item;
    }

    public void Execute()
    {
        if (!_collection.Contains(_item))
            _collection.Add(_item);
    }

    public void Unexecute()
    {
        _collection.Remove(_item);
    }
}

public class ChangeTextContentCommand : IUndoableCommand
{
    private readonly TextItemViewModel _item;
    private readonly string _oldText;
    private readonly string _newText;

    public string Description => "Edit Text";

    public ChangeTextContentCommand(TextItemViewModel item, string oldText, string newText)
    {
        _item = item;
        _oldText = oldText;
        _newText = newText;
    }

    public void Execute() => _item.Text = _newText;
    public void Unexecute() => _item.Text = _oldText;
}

public class AddItemsBatchCommand : IUndoableCommand
{
    private readonly ObservableCollection<ItemViewModel> _collection;
    private readonly List<ItemViewModel> _items;

    public string Description => $"Add {_items.Count} items";

    public AddItemsBatchCommand(ObservableCollection<ItemViewModel> collection, IEnumerable<ItemViewModel> items)
    {
        _collection = collection;
        _items = items.ToList();
    }

    public void Execute()
    {
        void PerformExecute()
        {
            var existing = new HashSet<ItemViewModel>(_collection);
            foreach (var item in _items)
            {
                if (!existing.Contains(item))
                {
                    _collection.Add(item);
                    existing.Add(item);
                }
            }
        }

        if (_collection is ObservableRangeCollection<ItemViewModel> rangeCol)
        {
            rangeCol.BatchUpdate(PerformExecute);
        }
        else
        {
            PerformExecute();
        }
    }

    public void Unexecute()
    {
        void PerformUnexecute()
        {
            foreach (var item in _items)
            {
                _collection.Remove(item);
            }
        }

        if (_collection is ObservableRangeCollection<ItemViewModel> rangeCol)
        {
            rangeCol.BatchUpdate(PerformUnexecute);
        }
        else
        {
            PerformUnexecute();
        }
    }
}

public class RemoveItemCommand : IUndoableCommand
{
    private readonly ObservableCollection<ItemViewModel> _collection;
    private readonly List<ItemViewModel> _items;
    private readonly List<int> _indices = new();

    public string Description => $"Delete {_items.Count} items";

    public RemoveItemCommand(ObservableCollection<ItemViewModel> collection, IEnumerable<ItemViewModel> items)
    {
        _collection = collection;
        _items = items.ToList();
    }

    public void Execute()
    {
        void PerformExecute()
        {
            _indices.Clear();
            foreach (var item in _items)
            {
                _indices.Add(_collection.IndexOf(item));
                _collection.Remove(item);
            }
        }

        if (_collection is ObservableRangeCollection<ItemViewModel> rangeCol)
        {
            rangeCol.BatchUpdate(PerformExecute);
        }
        else
        {
            PerformExecute();
        }
    }

    public void Unexecute()
    {
        void PerformUnexecute()
        {
            for (int i = 0; i < _items.Count; i++)
            {
                int idx = _indices[i];
                if (idx >= 0 && idx <= _collection.Count)
                    _collection.Insert(idx, _items[i]);
                else
                    _collection.Add(_items[i]);
            }
        }

        if (_collection is ObservableRangeCollection<ItemViewModel> rangeCol)
        {
            rangeCol.BatchUpdate(PerformUnexecute);
        }
        else
        {
            PerformUnexecute();
        }
    }
}

public class MoveItemsCommand : IUndoableCommand
{
    private readonly List<(ItemViewModel Item, double OldX, double OldY, double NewX, double NewY)> _moves;

    public string Description => $"Move {_moves.Count} items";

    public MoveItemsCommand(List<(ItemViewModel Item, double OldX, double OldY, double NewX, double NewY)> moves)
    {
        _moves = moves;
    }

    public void Execute()
    {
        foreach (var m in _moves)
        {
            m.Item.X = m.NewX;
            m.Item.Y = m.NewY;
        }
    }

    public void Unexecute()
    {
        foreach (var m in _moves)
        {
            m.Item.X = m.OldX;
            m.Item.Y = m.OldY;
        }
    }
}

public class TransformItemCommand : IUndoableCommand
{
    private readonly ItemViewModel _item;
    private readonly double _oldX, _oldY, _oldW, _oldH, _oldRot;
    private readonly double _newX, _newY, _newW, _newH, _newRot;

    public string Description => $"Transform {_item.Name}";

    public TransformItemCommand(
        ItemViewModel item,
        double oldX, double oldY, double oldW, double oldH, double oldRot,
        double newX, double newY, double newW, double newH, double newRot)
    {
        _item = item;
        _oldX = oldX;
        _oldY = oldY;
        _oldW = oldW;
        _oldH = oldH;
        _oldRot = oldRot;

        _newX = newX;
        _newY = newY;
        _newW = newW;
        _newH = newH;
        _newRot = newRot;
    }

    public void Execute()
    {
        _item.X = _newX;
        _item.Y = _newY;
        _item.Width = _newW;
        _item.Height = _newH;
        _item.Rotation = _newRot;
    }

    public void Unexecute()
    {
        _item.X = _oldX;
        _item.Y = _oldY;
        _item.Width = _oldW;
        _item.Height = _oldH;
        _item.Rotation = _oldRot;
    }
}

public class TransformMultipleItemsCommand : IUndoableCommand
{
    public record ItemTransformRecord(
        ItemViewModel Item,
        double OldX, double OldY, double OldWidth, double OldHeight, double OldRotation,
        double NewX, double NewY, double NewWidth, double NewHeight, double NewRotation);

    private readonly List<ItemTransformRecord> _records;

    public string Description => $"Transform {_records.Count} items";

    public IReadOnlyList<ItemTransformRecord> Records => _records;

    public TransformMultipleItemsCommand(IEnumerable<ItemTransformRecord> records)
    {
        _records = records.ToList();
    }

    public void Execute()
    {
        foreach (var r in _records)
        {
            r.Item.X = r.NewX;
            r.Item.Y = r.NewY;
            r.Item.Width = r.NewWidth;
            r.Item.Height = r.NewHeight;
            r.Item.Rotation = r.NewRotation;
        }
    }

    public void Unexecute()
    {
        foreach (var r in _records)
        {
            r.Item.X = r.OldX;
            r.Item.Y = r.OldY;
            r.Item.Width = r.OldWidth;
            r.Item.Height = r.OldHeight;
            r.Item.Rotation = r.OldRotation;
        }
    }
}

public class ModifyBezierControlPointsCommand : IUndoableCommand
{
    private readonly ShapeItemViewModel _item;
    private readonly double _oldStartX, _oldStartY, _oldEndX, _oldEndY;
    private readonly double _oldCp1X, _oldCp1Y, _oldCp2X, _oldCp2Y;
    private readonly double _newStartX, _newStartY, _newEndX, _newEndY;
    private readonly double _newCp1X, _newCp1Y, _newCp2X, _newCp2Y;

    public string Description => $"Modify Bezier Curve on {_item.Name}";

    public ModifyBezierControlPointsCommand(
        ShapeItemViewModel item,
        double oldCp1X, double oldCp1Y, double oldCp2X, double oldCp2Y,
        double newCp1X, double newCp1Y, double newCp2X, double newCp2Y)
        : this(item,
               item.StartX, item.StartY, item.EndX, item.EndY,
               oldCp1X, oldCp1Y, oldCp2X, oldCp2Y,
               item.StartX, item.StartY, item.EndX, item.EndY,
               newCp1X, newCp1Y, newCp2X, newCp2Y)
    {
    }

    public ModifyBezierControlPointsCommand(
        ShapeItemViewModel item,
        double oldStartX, double oldStartY, double oldEndX, double oldEndY,
        double oldCp1X, double oldCp1Y, double oldCp2X, double oldCp2Y,
        double newStartX, double newStartY, double newEndX, double newEndY,
        double newCp1X, double newCp1Y, double newCp2X, double newCp2Y)
    {
        _item = item;
        _oldStartX = oldStartX;
        _oldStartY = oldStartY;
        _oldEndX = oldEndX;
        _oldEndY = oldEndY;
        _oldCp1X = oldCp1X;
        _oldCp1Y = oldCp1Y;
        _oldCp2X = oldCp2X;
        _oldCp2Y = oldCp2Y;
        _newStartX = newStartX;
        _newStartY = newStartY;
        _newEndX = newEndX;
        _newEndY = newEndY;
        _newCp1X = newCp1X;
        _newCp1Y = newCp1Y;
        _newCp2X = newCp2X;
        _newCp2Y = newCp2Y;
    }

    public void Execute()
    {
        _item.StartX = _newStartX;
        _item.StartY = _newStartY;
        _item.EndX = _newEndX;
        _item.EndY = _newEndY;
        _item.ControlPoint1X = _newCp1X;
        _item.ControlPoint1Y = _newCp1Y;
        _item.ControlPoint2X = _newCp2X;
        _item.ControlPoint2Y = _newCp2Y;
        UpdateDimensions(_item);
    }

    public void Unexecute()
    {
        _item.StartX = _oldStartX;
        _item.StartY = _oldStartY;
        _item.EndX = _oldEndX;
        _item.EndY = _oldEndY;
        _item.ControlPoint1X = _oldCp1X;
        _item.ControlPoint1Y = _oldCp1Y;
        _item.ControlPoint2X = _oldCp2X;
        _item.ControlPoint2Y = _oldCp2Y;
        UpdateDimensions(_item);
    }

    private static void UpdateDimensions(ShapeItemViewModel shape)
    {
        double minX = Math.Min(Math.Min(shape.StartX, shape.EndX), Math.Min(shape.ControlPoint1X, shape.ControlPoint2X));
        double maxX = Math.Max(Math.Max(shape.StartX, shape.EndX), Math.Max(shape.ControlPoint1X, shape.ControlPoint2X));
        double minY = Math.Min(Math.Min(shape.StartY, shape.EndY), Math.Min(shape.ControlPoint1Y, shape.ControlPoint2Y));
        double maxY = Math.Max(Math.Max(shape.StartY, shape.EndY), Math.Max(shape.ControlPoint1Y, shape.ControlPoint2Y));

        double spanX = minX < 0 ? maxX - minX : maxX;
        double spanY = minY < 0 ? maxY - minY : maxY;

        shape.Width = Math.Max(20.0, Math.Round(spanX, 1));
        shape.Height = Math.Max(20.0, Math.Round(spanY, 1));
    }
}

public class ModifyCornerRadiusCommand : IUndoableCommand
{
    private readonly ShapeItemViewModel _item;
    private readonly double _oldRadius, _newRadius;

    public string Description => $"Change Corner Radius on {_item.Name}";

    public ModifyCornerRadiusCommand(ShapeItemViewModel item, double oldRadius, double newRadius)
    {
        _item = item;
        _oldRadius = oldRadius;
        _newRadius = newRadius;
    }

    public void Execute() => _item.CornerRadius = _newRadius;
    public void Unexecute() => _item.CornerRadius = _oldRadius;
}

public class ModifyInnerRadiusCommand : IUndoableCommand
{
    private readonly ShapeItemViewModel _item;
    private readonly double _oldRadius, _newRadius;

    public string Description => $"Change Inner Radius on {_item.Name}";

    public ModifyInnerRadiusCommand(ShapeItemViewModel item, double oldRadius, double newRadius)
    {
        _item = item;
        _oldRadius = oldRadius;
        _newRadius = newRadius;
    }

    public void Execute() => _item.InnerRadius = _newRadius;
    public void Unexecute() => _item.InnerRadius = _oldRadius;
}

public class ReorderLayerZIndexCommand : IUndoableCommand
{
    private readonly List<(ItemViewModel Item, int OldZ, int NewZ)> _changes;
    private readonly string _description;

    public string Description => _description;

    public ReorderLayerZIndexCommand(List<(ItemViewModel Item, int OldZ, int NewZ)> changes, string description = "Reorder Layer")
    {
        _changes = changes;
        _description = description;
    }

    public void Execute()
    {
        foreach (var c in _changes)
        {
            c.Item.Z = c.NewZ;
        }
    }

    public void Unexecute()
    {
        foreach (var c in _changes)
        {
            c.Item.Z = c.OldZ;
        }
    }
}

public class ModifyShapeStyleCommand : IUndoableCommand
{
    private readonly List<(ShapeItemViewModel Shape, string? OldFill, string? NewFill, string? OldStroke, string? NewStroke, double? OldThickness, double? NewThickness)> _changes;
    private readonly string _description;

    public string Description => _description;

    public ModifyShapeStyleCommand(
        List<(ShapeItemViewModel Shape, string? OldFill, string? NewFill, string? OldStroke, string? NewStroke, double? OldThickness, double? NewThickness)> changes,
        string description = "Modify Shape Appearance")
    {
        _changes = changes;
        _description = description;
    }

    public void Execute()
    {
        foreach (var c in _changes)
        {
            if (c.NewFill != null) c.Shape.Fill = c.NewFill;
            if (c.NewStroke != null) c.Shape.Stroke = c.NewStroke;
            if (c.NewThickness.HasValue) c.Shape.StrokeThickness = c.NewThickness.Value;
        }
    }

    public void Unexecute()
    {
        foreach (var c in _changes)
        {
            if (c.OldFill != null) c.Shape.Fill = c.OldFill;
            if (c.OldStroke != null) c.Shape.Stroke = c.OldStroke;
            if (c.OldThickness.HasValue) c.Shape.StrokeThickness = c.OldThickness.Value;
        }
    }
}


public class AssignDeviceNetworkCommand : IUndoableCommand
{
    private readonly DeviceItemViewModel _device;
    private readonly string? _oldIp;
    private readonly string? _newIp;
    private readonly string? _oldMac;
    private readonly string? _newMac;
    private readonly string? _oldDeviceId;
    private readonly string? _newDeviceId;
    private readonly string? _oldName;
    private readonly string? _newName;
    private readonly bool _oldIsManual;
    private readonly bool _newIsManual;
    private readonly Action? _onChanged;

    public string Description => $"Configure IP for {_device.Name}";

    public AssignDeviceNetworkCommand(
        DeviceItemViewModel device,
        string? oldIp, string? newIp,
        string? oldMac, string? newMac,
        string? oldDeviceId, string? newDeviceId,
        string? oldName, string? newName,
        bool oldIsManual, bool newIsManual,
        Action? onChanged = null)
    {
        _device = device;
        _oldIp = oldIp;
        _newIp = newIp;
        _oldMac = oldMac;
        _newMac = newMac;
        _oldDeviceId = oldDeviceId;
        _newDeviceId = newDeviceId;
        _oldName = oldName;
        _newName = newName;
        _oldIsManual = oldIsManual;
        _newIsManual = newIsManual;
        _onChanged = onChanged;
    }

    public void Execute()
    {
        _device.IpAddress = _newIp;
        _device.MacAddress = _newMac;
        _device.DeviceId = _newDeviceId;
        if (_newName != null) _device.Name = _newName;
        _device.IsManualIp = _newIsManual;
        _onChanged?.Invoke();
    }

    public void Unexecute()
    {
        _device.IpAddress = _oldIp;
        _device.MacAddress = _oldMac;
        _device.DeviceId = _oldDeviceId;
        if (_oldName != null) _device.Name = _oldName;
        _device.IsManualIp = _oldIsManual;
        _onChanged?.Invoke();
    }
}

public class BatchAssignDeviceNetworkCommand : IUndoableCommand
{
    public record DeviceNetworkState(
        DeviceItemViewModel Device,
        string? OldIp, string? NewIp,
        string? OldMac, string? NewMac,
        string? OldDeviceId, string? NewDeviceId,
        string? OldName, string? NewName,
        bool OldIsManual, bool NewIsManual);

    private readonly List<DeviceNetworkState> _entries;
    private readonly Action? _onChanged;

    public string Description => $"Batch assign network settings to {_entries.Count} devices";

    public IReadOnlyList<DeviceNetworkState> Entries => _entries;

    public BatchAssignDeviceNetworkCommand(IEnumerable<DeviceNetworkState> entries, Action? onChanged = null)
    {
        _entries = entries.ToList();
        _onChanged = onChanged;
    }

    public void Execute()
    {
        foreach (var e in _entries)
        {
            e.Device.IpAddress = e.NewIp;
            e.Device.MacAddress = e.NewMac;
            e.Device.DeviceId = e.NewDeviceId;
            if (e.NewName != null) e.Device.Name = e.NewName;
            e.Device.IsManualIp = e.NewIsManual;
        }
        _onChanged?.Invoke();
    }

    public void Unexecute()
    {
        foreach (var e in _entries)
        {
            e.Device.IpAddress = e.OldIp;
            e.Device.MacAddress = e.OldMac;
            e.Device.DeviceId = e.OldDeviceId;
            if (e.OldName != null) e.Device.Name = e.OldName;
            e.Device.IsManualIp = e.OldIsManual;
        }
        _onChanged?.Invoke();
    }
}

public class RenameItemCommand : IUndoableCommand
{
    private readonly ItemViewModel _item;
    private readonly string _oldName;
    private readonly string _newName;

    public string Description => $"Rename '{_oldName}' to '{_newName}'";

    public RenameItemCommand(ItemViewModel item, string oldName, string newName)
    {
        _item = item;
        _oldName = oldName;
        _newName = newName;
    }

    public void Execute()
    {
        _item.Name = _newName;
    }

    public void Unexecute()
    {
        _item.Name = _oldName;
    }
}

public class ToggleItemLockCommand : IUndoableCommand
{
    private readonly List<(ItemViewModel Item, bool OldLocked, bool NewLocked)> _changes;

    public string Description => _changes.Count == 1
        ? $"{(_changes[0].NewLocked ? "Lock" : "Unlock")} {_changes[0].Item.Name}"
        : $"{(_changes.Count > 0 && _changes[0].NewLocked ? "Lock" : "Unlock")} {_changes.Count} items";

    public ToggleItemLockCommand(IEnumerable<ItemViewModel> items, bool isLocked)
    {
        _changes = items.Select(i => (Item: i, OldLocked: i.IsLocked, NewLocked: isLocked)).ToList();
    }

    public ToggleItemLockCommand(ItemViewModel item, bool isLocked)
    {
        _changes = new List<(ItemViewModel, bool, bool)> { (item, item.IsLocked, isLocked) };
    }

    public void Execute()
    {
        foreach (var c in _changes)
        {
            c.Item.IsLocked = c.NewLocked;
        }
    }

    public void Unexecute()
    {
        foreach (var c in _changes)
        {
            c.Item.IsLocked = c.OldLocked;
        }
    }
}

public class ToggleItemVisibilityCommand : IUndoableCommand
{
    private readonly List<(ItemViewModel Item, bool OldVisible, bool NewVisible)> _changes;

    public string Description => _changes.Count == 1
        ? $"{(_changes[0].NewVisible ? "Show" : "Hide")} {_changes[0].Item.Name}"
        : $"{(_changes.Count > 0 && _changes[0].NewVisible ? "Show" : "Hide")} {_changes.Count} items";

    public ToggleItemVisibilityCommand(IEnumerable<ItemViewModel> items, bool isVisible)
    {
        _changes = items.Select(i => (Item: i, OldVisible: i.IsVisible, NewVisible: isVisible)).ToList();
    }

    public ToggleItemVisibilityCommand(ItemViewModel item, bool isVisible)
    {
        _changes = new List<(ItemViewModel, bool, bool)> { (item, item.IsVisible, isVisible) };
    }

    public void Execute()
    {
        foreach (var c in _changes)
        {
            c.Item.IsVisible = c.NewVisible;
        }
    }

    public void Unexecute()
    {
        foreach (var c in _changes)
        {
            c.Item.IsVisible = c.OldVisible;
        }
    }
}
