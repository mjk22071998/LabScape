using LabScape.Core.Enums;

namespace LabScape.Core.Helpers;

public class GradientInfo
{
    public BrushType Type { get; set; } = BrushType.Solid;
    public string Color1 { get; set; } = "#E0E7FF";
    public string Color2 { get; set; } = "#4338CA";
    public double Angle { get; set; } = 90.0;
}

public static class GradientHelper
{
    public static string CreateLinearGradient(string startHex, string endHex, double angleDeg = 90.0)
    {
        startHex = NormalizeHex(startHex, "#E0E7FF");
        endHex = NormalizeHex(endHex, "#4338CA");
        return $"linear-gradient({Math.Round(angleDeg):F0}deg, {startHex}, {endHex})";
    }

    public static string CreateRadialGradient(string centerHex, string outerHex)
    {
        centerHex = NormalizeHex(centerHex, "#E0E7FF");
        outerHex = NormalizeHex(outerHex, "#4338CA");
        return $"radial-gradient(circle, {centerHex}, {outerHex})";
    }

    public static GradientInfo AnalyzeBrush(string? brushStr, string defaultColor1 = "#E0E7FF", string defaultColor2 = "#4338CA")
    {
        var info = new GradientInfo
        {
            Type = BrushType.Solid,
            Color1 = defaultColor1,
            Color2 = defaultColor2,
            Angle = 90.0
        };

        if (string.IsNullOrWhiteSpace(brushStr))
        {
            info.Type = BrushType.Solid;
            return info;
        }

        brushStr = brushStr.Trim();
        if (brushStr.Equals("none", StringComparison.OrdinalIgnoreCase) ||
            brushStr.Equals("transparent", StringComparison.OrdinalIgnoreCase))
        {
            info.Type = BrushType.None;
            return info;
        }

        if (brushStr.StartsWith("linear-gradient", StringComparison.OrdinalIgnoreCase))
        {
            info.Type = BrushType.LinearGradient;
            ParseLinearGradientContent(brushStr, info);
            return info;
        }

        if (brushStr.StartsWith("radial-gradient", StringComparison.OrdinalIgnoreCase))
        {
            info.Type = BrushType.RadialGradient;
            ParseRadialGradientContent(brushStr, info);
            return info;
        }

        info.Type = BrushType.Solid;
        info.Color1 = brushStr;
        info.Color2 = defaultColor2;
        return info;
    }

    public static string NormalizeHex(string? hex, string fallback)
    {
        if (string.IsNullOrWhiteSpace(hex)) return fallback;
        hex = hex.Trim();
        if (!hex.StartsWith("#") && (hex.Length == 3 || hex.Length == 6 || hex.Length == 8))
        {
            hex = "#" + hex;
        }
        return hex;
    }

    private static void ParseLinearGradientContent(string value, GradientInfo info)
    {
        int startIdx = value.IndexOf('(');
        int endIdx = value.LastIndexOf(')');
        if (startIdx < 0 || endIdx <= startIdx) return;

        string content = value.Substring(startIdx + 1, endIdx - startIdx - 1).Trim();
        var parts = SplitArgs(content);
        if (parts.Count == 0) return;

        int colorIdx = 0;
        string first = parts[0].Trim();
        if (TryParseAngle(first, out double angle))
        {
            info.Angle = angle;
            colorIdx = 1;
        }

        if (colorIdx < parts.Count)
        {
            info.Color1 = ExtractColor(parts[colorIdx]);
        }
        if (colorIdx + 1 < parts.Count)
        {
            info.Color2 = ExtractColor(parts[colorIdx + 1]);
        }
        else
        {
            info.Color2 = info.Color1;
        }
    }

    private static void ParseRadialGradientContent(string value, GradientInfo info)
    {
        int startIdx = value.IndexOf('(');
        int endIdx = value.LastIndexOf(')');
        if (startIdx < 0 || endIdx <= startIdx) return;

        string content = value.Substring(startIdx + 1, endIdx - startIdx - 1).Trim();
        var parts = SplitArgs(content);
        if (parts.Count == 0) return;

        int colorIdx = 0;
        if (parts[0].Trim().Equals("circle", StringComparison.OrdinalIgnoreCase))
        {
            colorIdx = 1;
        }

        if (colorIdx < parts.Count)
        {
            info.Color1 = ExtractColor(parts[colorIdx]);
        }
        if (colorIdx + 1 < parts.Count)
        {
            info.Color2 = ExtractColor(parts[colorIdx + 1]);
        }
        else
        {
            info.Color2 = info.Color1;
        }
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

    private static string ExtractColor(string part)
    {
        part = part.Trim();
        int spaceIdx = part.IndexOf(' ');
        if (spaceIdx > 0)
        {
            return part.Substring(0, spaceIdx).Trim();
        }
        return part;
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
