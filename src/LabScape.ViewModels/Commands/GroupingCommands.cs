using System.Collections.ObjectModel;
using LabScape.Core.Interfaces;
using LabScape.Core.Models;
using LabScape.ViewModels.Items;

namespace LabScape.ViewModels.Commands;

public class GroupItemsCommand : IUndoableCommand
{
    private readonly ObservableCollection<ItemViewModel> _collection;
    private readonly List<ItemViewModel> _itemsToGroup;
    private readonly GroupItemViewModel _groupVm;
    private readonly Action<IEnumerable<ItemViewModel>> _setSelection;

    public string Description => "Group items (Ctrl+G)";

    public GroupItemsCommand(
        ObservableCollection<ItemViewModel> collection,
        IEnumerable<ItemViewModel> itemsToGroup,
        Action<IEnumerable<ItemViewModel>> setSelection)
    {
        _collection = collection;
        _itemsToGroup = itemsToGroup.ToList();
        _setSelection = setSelection;

        var groupModel = new GroupItem { Name = "Group" };
        _groupVm = new GroupItemViewModel(groupModel);
        foreach (var item in _itemsToGroup)
        {
            _groupVm.AddChild(item);
        }
        _groupVm.Z = _itemsToGroup.Max(i => i.Z);
    }

    public void Execute()
    {
        foreach (var item in _itemsToGroup)
        {
            _collection.Remove(item);
        }
        _collection.Add(_groupVm);
        _setSelection(new[] { _groupVm });
    }

    public void Unexecute()
    {
        _collection.Remove(_groupVm);
        foreach (var item in _itemsToGroup)
        {
            item.ParentGroupId = null;
            _collection.Add(item);
        }
        _setSelection(_itemsToGroup);
    }
}

public class UngroupItemsCommand : IUndoableCommand
{
    private readonly ObservableCollection<ItemViewModel> _collection;
    private readonly List<GroupItemViewModel> _groupsToUngroup;
    private readonly Dictionary<GroupItemViewModel, List<ItemViewModel>> _groupChildren = new();
    private readonly Action<IEnumerable<ItemViewModel>> _setSelection;

    public string Description => "Ungroup items (Ctrl+Shift+G)";

    public UngroupItemsCommand(
        ObservableCollection<ItemViewModel> collection,
        IEnumerable<GroupItemViewModel> groups,
        Action<IEnumerable<ItemViewModel>> setSelection)
    {
        _collection = collection;
        _groupsToUngroup = groups.ToList();
        _setSelection = setSelection;

        foreach (var g in _groupsToUngroup)
        {
            _groupChildren[g] = g.Children.ToList();
        }
    }

    public void Execute()
    {
        var unpacked = new List<ItemViewModel>();
        foreach (var group in _groupsToUngroup)
        {
            _collection.Remove(group);
            foreach (var child in _groupChildren[group])
            {
                child.ParentGroupId = null;
                if (!_collection.Contains(child))
                {
                    _collection.Add(child);
                }
                unpacked.Add(child);
            }
        }
        _setSelection(unpacked);
    }

    public void Unexecute()
    {
        foreach (var group in _groupsToUngroup)
        {
            foreach (var child in _groupChildren[group])
            {
                child.ParentGroupId = group.Id;
                _collection.Remove(child);
            }
            if (!_collection.Contains(group))
            {
                _collection.Add(group);
            }
        }
        _setSelection(_groupsToUngroup);
    }
}
