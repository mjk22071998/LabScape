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
    private double _startX;

    [ObservableProperty]
    private double _startY;

    [ObservableProperty]
    private double _endX;

    [ObservableProperty]
    private double _endY;

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
        _startX = model.StartX;
        _startY = model.StartY;
        _endX = model.EndX;
        _endY = model.EndY;
        _controlPoint1X = model.ControlPoint1X;
        _controlPoint1Y = model.ControlPoint1Y;
        _controlPoint2X = model.ControlPoint2X;
        _controlPoint2Y = model.ControlPoint2Y;
    }

    partial void OnShapeTypeChanged(ShapeType value)
    {
        ShapeModel.ShapeType = value;
        OnPropertyChanged(string.Empty);
    }

    partial void OnCornerRadiusChanged(double value)
    {
        ShapeModel.CornerRadius = value;
        OnPropertyChanged(string.Empty);
    }

    partial void OnInnerRadiusChanged(double value)
    {
        ShapeModel.InnerRadius = value;
        OnPropertyChanged(string.Empty);
    }

    partial void OnFillChanged(string value) => ShapeModel.Fill = value;
    partial void OnStrokeChanged(string value) => ShapeModel.Stroke = value;
    partial void OnStrokeThicknessChanged(double value) => ShapeModel.StrokeThickness = value;

    partial void OnStartXChanged(double value)
    {
        ShapeModel.StartX = value;
        OnPropertyChanged(string.Empty);
    }

    partial void OnStartYChanged(double value)
    {
        ShapeModel.StartY = value;
        OnPropertyChanged(string.Empty);
    }

    partial void OnEndXChanged(double value)
    {
        ShapeModel.EndX = value;
        OnPropertyChanged(string.Empty);
    }

    partial void OnEndYChanged(double value)
    {
        ShapeModel.EndY = value;
        OnPropertyChanged(string.Empty);
    }

    partial void OnControlPoint1XChanged(double value)
    {
        ShapeModel.ControlPoint1X = value;
        OnPropertyChanged(string.Empty);
    }

    partial void OnControlPoint1YChanged(double value)
    {
        ShapeModel.ControlPoint1Y = value;
        OnPropertyChanged(string.Empty);
    }

    partial void OnControlPoint2XChanged(double value)
    {
        ShapeModel.ControlPoint2X = value;
        OnPropertyChanged(string.Empty);
    }

    partial void OnControlPoint2YChanged(double value)
    {
        ShapeModel.ControlPoint2Y = value;
        OnPropertyChanged(string.Empty);
    }

    protected override void OnPropertyChanged(System.ComponentModel.PropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);
        if (e.PropertyName is nameof(Width) or nameof(Height) or nameof(StartX) or nameof(StartY) or nameof(EndX) or nameof(EndY))
        {
            OnPropertyChanged(string.Empty);
        }
    }
}
