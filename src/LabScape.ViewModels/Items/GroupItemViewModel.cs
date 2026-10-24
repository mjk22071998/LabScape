using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using LabScape.Core.Models;

namespace LabScape.ViewModels.Items;

public partial class GroupItemViewModel : ItemViewModel
{
    public GroupItem GroupModel => (GroupItem)Model;

    public ObservableCollection<ItemViewModel> Children { get; } = new();

    /// <summary>
    /// Requirement: Group names are configurable in properties panel and saved in schema,
    /// but NEVER displayed on the layout canvas.
    /// </summary>
    public override bool ShowCanvasLabel => false;

    public GroupItemViewModel(GroupItem model) : base(model)
    {
        foreach (var child in model.Children)
        {
            var childVm = FromModel(child);
            childVm.ParentGroupId = model.Id;
            Children.Add(childVm);
        }
        RecalculateBounds();
    }

    public void AddChild(ItemViewModel item)
    {
        item.ParentGroupId = Id;
        if (!GroupModel.Children.Contains(item.Model))
        {
            GroupModel.Children.Add(item.Model);
        }
        if (!Children.Contains(item))
        {
            Children.Add(item);
        }
        RecalculateBounds();
    }

    public void RemoveChild(ItemViewModel item)
    {
        item.ParentGroupId = null;
        GroupModel.Children.Remove(item.Model);
        Children.Remove(item);
        RecalculateBounds();
    }

    public void RecalculateBounds()
    {
        if (Children.Count == 0) return;

        double minX = Children.Min(c => c.X);
        double minY = Children.Min(c => c.Y);
        double maxX = Children.Max(c => c.X + c.Width);
        double maxY = Children.Max(c => c.Y + c.Height);

        X = minX;
        Y = minY;
        Width = Math.Max(10, maxX - minX);
        Height = Math.Max(10, maxY - minY);
    }
}
