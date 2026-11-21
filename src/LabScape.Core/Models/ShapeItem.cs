using System.Text.Json.Serialization;
using LabScape.Core.Enums;

namespace LabScape.Core.Models;

public class ShapeItem : LayoutItemBase, IJsonOnDeserialized
{
    [JsonIgnore]
    public override ItemType Type => ItemType.Shape;

    [JsonPropertyName("shape_type")]
    public ShapeType ShapeType { get; set; } = ShapeType.Rectangle;

    [JsonPropertyName("corner_radius")]
    public double CornerRadius { get; set; } = 12.0;

    [JsonPropertyName("inner_radius")]
    public double InnerRadius { get; set; } = 25.0;

    [JsonPropertyName("fill")]
    public string Fill { get; set; } = "#E0E7FF";

    [JsonPropertyName("stroke")]
    public string Stroke { get; set; } = "#4338CA";

    [JsonPropertyName("stroke_thickness")]
    public double StrokeThickness { get; set; } = 2.0;

    // Bezier curve endpoints and control offsets (relative to item origin)
    [JsonPropertyName("start_x")]
    public double StartX { get; set; } = 10.0;

    [JsonPropertyName("start_y")]
    public double StartY { get; set; } = 75.0;

    [JsonPropertyName("end_x")]
    public double EndX { get; set; } = 130.0;

    [JsonPropertyName("end_y")]
    public double EndY { get; set; } = 15.0;

    private double _cp1X = 35.0;
    private double _cp1Y = 15.0;
    private double _cp2X = 105.0;
    private double _cp2Y = 75.0;
    private List<ControlPoint> _controlPoints = new();
    private bool _controlPointsInitialized;

    [JsonPropertyName("control_point1_x")]
    public double ControlPoint1X
    {
        get => _controlPoints.Count > 0 ? _controlPoints[0].X : _cp1X;
        set
        {
            _cp1X = value;
            if (_controlPoints.Count > 0) _controlPoints[0].X = value;
        }
    }

    [JsonPropertyName("control_point1_y")]
    public double ControlPoint1Y
    {
        get => _controlPoints.Count > 0 ? _controlPoints[0].Y : _cp1Y;
        set
        {
            _cp1Y = value;
            if (_controlPoints.Count > 0) _controlPoints[0].Y = value;
        }
    }

    [JsonPropertyName("control_point2_x")]
    public double ControlPoint2X
    {
        get => _controlPoints.Count > 1 ? _controlPoints[1].X : _cp2X;
        set
        {
            _cp2X = value;
            if (_controlPoints.Count > 1) _controlPoints[1].X = value;
        }
    }

    [JsonPropertyName("control_point2_y")]
    public double ControlPoint2Y
    {
        get => _controlPoints.Count > 1 ? _controlPoints[1].Y : _cp2Y;
        set
        {
            _cp2Y = value;
            if (_controlPoints.Count > 1) _controlPoints[1].Y = value;
        }
    }

    [JsonPropertyName("control_points")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<ControlPoint> ControlPoints
    {
        get
        {
            EnsureControlPoints();
            return _controlPoints;
        }
        set
        {
            _controlPointsInitialized = true;
            _controlPoints = value ?? new List<ControlPoint>();
            SyncLegacyControlPoints();
        }
    }

    public void EnsureControlPoints()
    {
        if (!_controlPointsInitialized)
        {
            _controlPointsInitialized = true;
            if (_controlPoints.Count == 0)
            {
                _controlPoints.Add(new ControlPoint(_cp1X, _cp1Y));
                _controlPoints.Add(new ControlPoint(_cp2X, _cp2Y));
            }
        }
    }

    public void SyncLegacyControlPoints()
    {
        if (_controlPoints.Count > 0)
        {
            _cp1X = _controlPoints[0].X;
            _cp1Y = _controlPoints[0].Y;
        }
        if (_controlPoints.Count > 1)
        {
            _cp2X = _controlPoints[1].X;
            _cp2Y = _controlPoints[1].Y;
        }
    }

    void IJsonOnDeserialized.OnDeserialized()
    {
        EnsureControlPoints();
    }

    public ShapeItem()
    {
        Name = "New Shape";
        Width = 100;
        Height = 100;
        EnsureControlPoints();
    }
}
