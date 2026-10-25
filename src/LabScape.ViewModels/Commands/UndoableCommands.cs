using System.Collections.ObjectModel;
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
        foreach (var item in _items)
        {
            if (!_collection.Contains(item))
                _collection.Add(item);
        }
    }

    public void Unexecute()
    {
        foreach (var item in _items)
        {
            _collection.Remove(item);
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
        _indices.Clear();
        foreach (var item in _items)
        {
            _indices.Add(_collection.IndexOf(item));
            _collection.Remove(item);
        }
    }

    public void Unexecute()
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
