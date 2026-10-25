using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using LabScape.Core.Enums;
using LabScape.Core.Models;

namespace LabScape.ViewModels.Items;

public partial class ItemViewModel : ObservableObject
{
    public LayoutItemBase Model { get; }

    [ObservableProperty]
    private string _id;

    [ObservableProperty]
    private string _name;

    [ObservableProperty]
    private double _x;

    [ObservableProperty]
    private double _y;

    [ObservableProperty]
    private int _z;

    [ObservableProperty]
    private double _width;

    [ObservableProperty]
    private double _height;

    [ObservableProperty]
    private double _rotation;

    [ObservableProperty]
    private bool _isSelected;

    [ObservableProperty]
    private string? _parentGroupId;

    public ItemType ItemType => Model.Type;

    /// <summary>
    /// Requirement: ONLY device items show their name below the icon on the canvas.
    /// Shapes, furniture, and groups NEVER show text on the layout canvas.
    /// </summary>
    public virtual bool ShowCanvasLabel => false;

    public ItemViewModel(LayoutItemBase model)
    {
        Model = model;
        _id = model.Id;
        _name = model.Name;
        _x = model.X;
        _y = model.Y;
        _z = model.Z;
        _width = model.Width;
        _height = model.Height;
        _rotation = model.Rotation;
        _parentGroupId = model.ParentGroupId;
    }

    partial void OnNameChanged(string value) => Model.Name = value;
    partial void OnXChanged(double oldValue, double newValue)
    {
        Model.X = newValue;
        OnPositionChanged(newValue - oldValue, 0);
    }
    partial void OnYChanged(double oldValue, double newValue)
    {
        Model.Y = newValue;
        OnPositionChanged(0, newValue - oldValue);
    }
    partial void OnZChanged(int value) => Model.Z = value;
    partial void OnWidthChanged(double value) => Model.Width = value;
    partial void OnHeightChanged(double value) => Model.Height = value;
    partial void OnRotationChanged(double value) => Model.Rotation = value;
    partial void OnParentGroupIdChanged(string? value) => Model.ParentGroupId = value;

    protected virtual void OnPositionChanged(double deltaX, double deltaY) { }

    public static ItemViewModel FromModel(LayoutItemBase model)
    {
        return model switch
        {
            DeviceItem dev => new DeviceItemViewModel(dev),
            FurnitureItem furn => new FurnitureItemViewModel(furn),
            ShapeItem shp => new ShapeItemViewModel(shp),
            GroupItem grp => new GroupItemViewModel(grp),
            _ => new ItemViewModel(model)
        };
    }
}
