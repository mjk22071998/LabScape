using System.Text.Json.Serialization;
using LabScape.Core.Enums;

namespace LabScape.Core.Models;

public class ShapeItem : LayoutItemBase
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

    [JsonPropertyName("control_point1_x")]
    public double ControlPoint1X { get; set; } = 35.0;

    [JsonPropertyName("control_point1_y")]
    public double ControlPoint1Y { get; set; } = 15.0;

    [JsonPropertyName("control_point2_x")]
    public double ControlPoint2X { get; set; } = 105.0;

    [JsonPropertyName("control_point2_y")]
    public double ControlPoint2Y { get; set; } = 75.0;

    public ShapeItem()
    {
        Name = "New Shape";
        Width = 100;
        Height = 100;
    }
}
