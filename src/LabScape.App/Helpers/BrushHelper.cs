using System.Windows;
using System.Windows.Media;
using LabScape.Core.Helpers;

namespace LabScape.App.Helpers;

public static class BrushHelper
{
    public static Brush? ParseBrush(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Equals("none", StringComparison.OrdinalIgnoreCase))
            return null;

        value = value.Trim();

        // 1. Linear Gradient
        if (value.StartsWith("linear-gradient", StringComparison.OrdinalIgnoreCase))
        {
            return ParseLinearGradient(value);
        }

        // 2. Radial Gradient
        if (value.StartsWith("radial-gradient", StringComparison.OrdinalIgnoreCase))
        {
            return ParseRadialGradient(value);
        }

        // 3. Solid Color Hex or Name
        try
        {
            string hex = GradientHelper.NormalizeHex(value, value);
            var color = (Color)ColorConverter.ConvertFromString(hex);
            var brush = new SolidColorBrush(color);
            brush.Freeze();
            return brush;
        }
        catch
        {
            return Brushes.Transparent;
        }
    }

    public static LinearGradientBrush ParseLinearGradient(string value)
    {
        int startIdx = value.IndexOf('(');
        int endIdx = value.LastIndexOf(')');
        if (startIdx < 0 || endIdx <= startIdx)
        {
            return CreateDefaultLinearGradient();
        }

        string content = value.Substring(startIdx + 1, endIdx - startIdx - 1).Trim();
        var parts = SplitArgs(content);
        if (parts.Count == 0) return CreateDefaultLinearGradient();

        double angleDeg = 90.0;
        int colorStartIdx = 0;

        string first = parts[0].Trim();
        if (TryParseAngle(first, out double parsedAngle))
        {
            angleDeg = parsedAngle;
            colorStartIdx = 1;
        }

        var stops = new List<(Color Color, double Offset)>();
        int totalColors = parts.Count - colorStartIdx;

        for (int i = colorStartIdx; i < parts.Count; i++)
        {
            string part = parts[i].Trim();
            ParseColorStop(part, i - colorStartIdx, totalColors, out Color col, out double offset);
            stops.Add((col, offset));
        }

        if (stops.Count == 0)
        {
            stops.Add((Color.FromRgb(79, 70, 229), 0.0));
            stops.Add((Color.FromRgb(6, 182, 212), 1.0));
        }
        else if (stops.Count == 1)
        {
            stops.Add((stops[0].Color, 1.0));
        }

        var (startPoint, endPoint) = CalculatePointsForAngle(angleDeg);
        var brush = new LinearGradientBrush
        {
            StartPoint = startPoint,
            EndPoint = endPoint
        };

        foreach (var stop in stops)
        {
            brush.GradientStops.Add(new GradientStop(stop.Color, stop.Offset));
        }

        brush.Freeze();
        return brush;
    }

    public static RadialGradientBrush ParseRadialGradient(string value)
    {
        int startIdx = value.IndexOf('(');
        int endIdx = value.LastIndexOf(')');
        if (startIdx < 0 || endIdx <= startIdx)
        {
            return CreateDefaultRadialGradient();
        }

        string content = value.Substring(startIdx + 1, endIdx - startIdx - 1).Trim();
        var parts = SplitArgs(content);
        int colorStartIdx = 0;
        if (parts.Count > 0 && parts[0].Trim().Equals("circle", StringComparison.OrdinalIgnoreCase))
        {
            colorStartIdx = 1;
        }

        var stops = new List<(Color Color, double Offset)>();
        int totalColors = parts.Count - colorStartIdx;

        for (int i = colorStartIdx; i < parts.Count; i++)
        {
            string part = parts[i].Trim();
            ParseColorStop(part, i - colorStartIdx, totalColors, out Color col, out double offset);
            stops.Add((col, offset));
        }

        if (stops.Count == 0)
        {
            stops.Add((Color.FromRgb(99, 102, 241), 0.0));
            stops.Add((Color.FromRgb(236, 72, 153), 1.0));
        }
        else if (stops.Count == 1)
        {
            stops.Add((stops[0].Color, 1.0));
        }

        var brush = new RadialGradientBrush
        {
            Center = new Point(0.5, 0.5),
            GradientOrigin = new Point(0.5, 0.5),
            RadiusX = 0.5,
            RadiusY = 0.5
        };

        foreach (var stop in stops)
        {
            brush.GradientStops.Add(new GradientStop(stop.Color, stop.Offset));
        }

        brush.Freeze();
        return brush;
    }

    public static (Point Start, Point End) CalculatePointsForAngle(double deg)
    {
        deg = (deg % 360 + 360) % 360;

        if (Math.Abs(deg - 0) < 1 || Math.Abs(deg - 360) < 1) return (new Point(0.5, 1), new Point(0.5, 0));
        if (Math.Abs(deg - 45) < 1) return (new Point(0, 1), new Point(1, 0));
        if (Math.Abs(deg - 90) < 1) return (new Point(0, 0.5), new Point(1, 0.5));
        if (Math.Abs(deg - 135) < 1) return (new Point(0, 0), new Point(1, 1));
        if (Math.Abs(deg - 180) < 1) return (new Point(0.5, 0), new Point(0.5, 1));
        if (Math.Abs(deg - 225) < 1) return (new Point(1, 0), new Point(0, 1));
        if (Math.Abs(deg - 270) < 1) return (new Point(1, 0.5), new Point(0, 0.5));
        if (Math.Abs(deg - 315) < 1) return (new Point(1, 1), new Point(0, 0));

        double rad = (deg - 90.0) * Math.PI / 180.0;
        double cos = Math.Cos(rad);
        double sin = Math.Sin(rad);

        return (
            new Point(Math.Clamp(0.5 - 0.5 * cos, 0, 1), Math.Clamp(0.5 - 0.5 * sin, 0, 1)),
            new Point(Math.Clamp(0.5 + 0.5 * cos, 0, 1), Math.Clamp(0.5 + 0.5 * sin, 0, 1))
        );
    }

    private static LinearGradientBrush CreateDefaultLinearGradient()
    {
        var brush = new LinearGradientBrush(Color.FromRgb(79, 70, 229), Color.FromRgb(6, 182, 212), new Point(0, 0.5), new Point(1, 0.5));
        brush.Freeze();
        return brush;
    }

    private static RadialGradientBrush CreateDefaultRadialGradient()
    {
        var brush = new RadialGradientBrush(Color.FromRgb(99, 102, 241), Color.FromRgb(236, 72, 153));
        brush.Freeze();
        return brush;
    }

    private static bool TryParseAngle(string text, out double angle)
    {
        angle = 90.0;
        text = text.Trim().ToLowerInvariant();
        if (text.EndsWith("deg") && double.TryParse(text.Substring(0, text.Length - 3).Trim(), out angle))
        {
            return true;
        }
        switch (text)
        {
            case "to top": angle = 0; return true;
            case "to top right": case "to right top": angle = 45; return true;
            case "to right": angle = 90; return true;
            case "to bottom right": case "to right bottom": angle = 135; return true;
            case "to bottom": angle = 180; return true;
            case "to bottom left": case "to left bottom": angle = 225; return true;
            case "to left": angle = 270; return true;
            case "to top left": case "to left top": angle = 315; return true;
            default: return false;
        }
    }

    private static void ParseColorStop(string part, int index, int total, out Color color, out double offset)
    {
        color = Colors.Transparent;
        offset = total <= 1 ? 0.0 : (double)index / (total - 1);

        string[] tokens = part.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (tokens.Length == 0) return;

        try
        {
            string hex = GradientHelper.NormalizeHex(tokens[0], tokens[0]);
            color = (Color)ColorConverter.ConvertFromString(hex);
        }
        catch
        {
            color = Colors.Transparent;
        }

        if (tokens.Length > 1)
        {
            string offsetStr = tokens[1].Trim();
            if (offsetStr.EndsWith('%') && double.TryParse(offsetStr.TrimEnd('%'), out double pct))
            {
                offset = Math.Clamp(pct / 100.0, 0.0, 1.0);
            }
            else if (double.TryParse(offsetStr, out double num))
            {
                offset = Math.Clamp(num, 0.0, 1.0);
            }
        }
    }

    private static List<string> SplitArgs(string content)
    {
        var list = new List<string>();
        int depth = 0;
        int last = 0;
        for (int i = 0; i < content.Length; i++)
        {
            char c = content[i];
            if (c == '(') depth++;
            else if (c == ')') depth--;
            else if (c == ',' && depth == 0)
            {
                list.Add(content.Substring(last, i - last).Trim());
                last = i + 1;
            }
        }
        if (last < content.Length)
        {
            list.Add(content.Substring(last).Trim());
        }
        return list;
    }
}
