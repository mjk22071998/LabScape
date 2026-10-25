using System.Collections.ObjectModel;
using LabScape.Core.Collections;
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
        void PerformExecute()
        {
            foreach (var item in _itemsToGroup)
            {
                _collection.Remove(item);
            }
            _collection.Add(_groupVm);
        }

        if (_collection is ObservableRangeCollection<ItemViewModel> rangeCol)
        {
            rangeCol.BatchUpdate(PerformExecute);
        }
        else
        {
            PerformExecute();
        }

        _setSelection(new[] { _groupVm });
    }

    public void Unexecute()
    {
        void PerformUnexecute()
        {
            _collection.Remove(_groupVm);
            foreach (var item in _itemsToGroup)
            {
                item.ParentGroupId = null;
                _collection.Add(item);
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

        void PerformExecute()
        {
            var existingItems = new HashSet<ItemViewModel>(_collection);
            foreach (var group in _groupsToUngroup)
            {
                _collection.Remove(group);
                existingItems.Remove(group);

                foreach (var child in _groupChildren[group])
                {
                    child.ParentGroupId = null;
                    if (!existingItems.Contains(child))
                    {
                        _collection.Add(child);
                        existingItems.Add(child);
                    }
                    unpacked.Add(child);
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

        _setSelection(unpacked);
    }

    public void Unexecute()
    {
        void PerformUnexecute()
        {
            var existingItems = new HashSet<ItemViewModel>(_collection);
            foreach (var group in _groupsToUngroup)
            {
                foreach (var child in _groupChildren[group])
                {
                    child.ParentGroupId = group.Id;
                    _collection.Remove(child);
                    existingItems.Remove(child);
                }
                if (!existingItems.Contains(group))
                {
                    _collection.Add(group);
                    existingItems.Add(group);
                }
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

        _setSelection(_groupsToUngroup);
    }
}
