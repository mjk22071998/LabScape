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
