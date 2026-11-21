using CommunityToolkit.Mvvm.ComponentModel;
using LabScape.Core.Enums;
using LabScape.Core.Models;

namespace LabScape.ViewModels.Items;

public partial class ShapeItemViewModel : ItemViewModel, IStylableItemViewModel
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

    public IReadOnlyList<ControlPoint> GetControlPoints() => ShapeModel.ControlPoints.AsReadOnly();

    public int ControlPointCount => ShapeModel.ControlPoints.Count;

    public void AddControlPoint(double x, double y, int? index = null)
    {
        var cp = new ControlPoint(x, y);
        if (index.HasValue && index.Value >= 0 && index.Value <= ShapeModel.ControlPoints.Count)
        {
            ShapeModel.ControlPoints.Insert(index.Value, cp);
        }
        else
        {
            ShapeModel.ControlPoints.Add(cp);
        }

        SyncFromModel();
        OnPropertyChanged(nameof(ControlPointCount));
        OnPropertyChanged(string.Empty);
    }

    public bool RemoveControlPoint(int index)
    {
        if (ShapeModel.ControlPoints.Count <= 1 || index < 0 || index >= ShapeModel.ControlPoints.Count)
        {
            return false;
        }

        ShapeModel.ControlPoints.RemoveAt(index);
        SyncFromModel();
        OnPropertyChanged(nameof(ControlPointCount));
        OnPropertyChanged(string.Empty);
        return true;
    }

    public void SetControlPoint(int index, double x, double y)
    {
        if (index >= 0 && index < ShapeModel.ControlPoints.Count)
        {
            ShapeModel.ControlPoints[index].X = x;
            ShapeModel.ControlPoints[index].Y = y;
            SyncFromModel();
            OnPropertyChanged(string.Empty);
        }
    }

    public void SetAllControlPoints(IEnumerable<ControlPoint> points)
    {
        ShapeModel.ControlPoints = points.Select(p => new ControlPoint(p.X, p.Y)).ToList();
        SyncFromModel();
        OnPropertyChanged(nameof(ControlPointCount));
        OnPropertyChanged(string.Empty);
    }

    private void SyncFromModel()
    {
        ShapeModel.SyncLegacyControlPoints();
        ControlPoint1X = ShapeModel.ControlPoint1X;
        ControlPoint1Y = ShapeModel.ControlPoint1Y;
        ControlPoint2X = ShapeModel.ControlPoint2X;
        ControlPoint2Y = ShapeModel.ControlPoint2Y;
    }

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
