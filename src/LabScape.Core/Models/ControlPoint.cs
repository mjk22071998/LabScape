using System.Text.Json.Serialization;

namespace LabScape.Core.Models;

public class ControlPoint
{
    [JsonPropertyName("x")]
    public double X { get; set; }

    [JsonPropertyName("y")]
    public double Y { get; set; }

    public ControlPoint()
    {
    }

    public ControlPoint(double x, double y)
    {
        X = x;
        Y = y;
    }
}
