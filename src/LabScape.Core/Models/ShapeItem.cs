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

    // Bezier control offsets (relative to bounding box 0..1 or absolute)
    [JsonPropertyName("control_point1_x")]
    public double ControlPoint1X { get; set; } = 25.0;

    [JsonPropertyName("control_point1_y")]
    public double ControlPoint1Y { get; set; } = 0.0;

    [JsonPropertyName("control_point2_x")]
    public double ControlPoint2X { get; set; } = 75.0;

    [JsonPropertyName("control_point2_y")]
    public double ControlPoint2Y { get; set; } = 100.0;

    public ShapeItem()
    {
        Name = "New Shape";
        Width = 100;
        Height = 100;
    }
}
