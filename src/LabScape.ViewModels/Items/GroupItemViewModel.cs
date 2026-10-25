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

    private bool _isInternalUpdating;

    protected override void OnPositionChanged(double deltaX, double deltaY)
    {
        if (_isInternalUpdating) return;
        foreach (var child in Children)
        {
            child.X += deltaX;
            child.Y += deltaY;
        }
    }

    public void RecalculateBounds()
    {
        if (Children.Count == 0) return;

        double minX = double.MaxValue;
        double minY = double.MaxValue;
        double maxX = double.MinValue;
        double maxY = double.MinValue;

        foreach (var c in Children)
        {
            var (bMinX, bMinY, bMaxX, bMaxY) = GetVisualBounds(c);
            if (bMinX < minX) minX = bMinX;
            if (bMinY < minY) minY = bMinY;
            if (bMaxX > maxX) maxX = bMaxX;
            if (bMaxY > maxY) maxY = bMaxY;
        }

        _isInternalUpdating = true;
        try
        {
            X = Math.Round(minX, 1);
            Y = Math.Round(minY, 1);
            Width = Math.Round(Math.Max(10, maxX - minX), 1);
            Height = Math.Round(Math.Max(10, maxY - minY), 1);
        }
        finally
        {
            _isInternalUpdating = false;
        }
    }

    public static (double MinX, double MinY, double MaxX, double MaxY) GetVisualBounds(ItemViewModel item)
    {
        double w = item.Width;
        double h = item.Height;

        if (Math.Abs(item.Rotation) < 0.001)
        {
            return (item.X, item.Y, item.X + w, item.Y + h);
        }

        double rad = item.Rotation * Math.PI / 180.0;
        double cos = Math.Abs(Math.Cos(rad));
        double sin = Math.Abs(Math.Sin(rad));

        double cx = item.X + w / 2.0;
        double cy = item.Y + h / 2.0;

        double extX = (w / 2.0) * cos + (h / 2.0) * sin;
        double extY = (w / 2.0) * sin + (h / 2.0) * cos;

        return (cx - extX, cy - extY, cx + extX, cy + extY);
    }
}
