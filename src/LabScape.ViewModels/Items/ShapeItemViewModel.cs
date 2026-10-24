using CommunityToolkit.Mvvm.ComponentModel;
using LabScape.Core.Enums;
using LabScape.Core.Models;

namespace LabScape.ViewModels.Items;

public partial class ShapeItemViewModel : ItemViewModel
{
    public ShapeItem ShapeModel => (ShapeItem)Model;

    [ObservableProperty]
    private ShapeType _shapeType;

    [ObservableProperty]
    private double _cornerRadius;

    [ObservableProperty]
    private double _innerRadius;

    [ObservableProperty]
    private string _fill;

    [ObservableProperty]
    private string _stroke;

    [ObservableProperty]
    private double _strokeThickness;

    [ObservableProperty]
    private double _controlPoint1X;

    [ObservableProperty]
    private double _controlPoint1Y;

    [ObservableProperty]
    private double _controlPoint2X;

    [ObservableProperty]
    private double _controlPoint2Y;

    public override bool ShowCanvasLabel => false;

    public ShapeItemViewModel(ShapeItem model) : base(model)
    {
        _shapeType = model.ShapeType;
        _cornerRadius = model.CornerRadius;
        _innerRadius = model.InnerRadius;
        _fill = model.Fill;
        _stroke = model.Stroke;
        _strokeThickness = model.StrokeThickness;
        _controlPoint1X = model.ControlPoint1X;
        _controlPoint1Y = model.ControlPoint1Y;
        _controlPoint2X = model.ControlPoint2X;
        _controlPoint2Y = model.ControlPoint2Y;
    }

    partial void OnShapeTypeChanged(ShapeType value) => ShapeModel.ShapeType = value;
    partial void OnCornerRadiusChanged(double value) => ShapeModel.CornerRadius = value;
    partial void OnInnerRadiusChanged(double value) => ShapeModel.InnerRadius = value;
    partial void OnFillChanged(string value) => ShapeModel.Fill = value;
    partial void OnStrokeChanged(string value) => ShapeModel.Stroke = value;
    partial void OnStrokeThicknessChanged(double value) => ShapeModel.StrokeThickness = value;
    partial void OnControlPoint1XChanged(double value) => ShapeModel.ControlPoint1X = value;
    partial void OnControlPoint1YChanged(double value) => ShapeModel.ControlPoint1Y = value;
    partial void OnControlPoint2XChanged(double value) => ShapeModel.ControlPoint2X = value;
    partial void OnControlPoint2YChanged(double value) => ShapeModel.ControlPoint2Y = value;
}
