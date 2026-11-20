using System.IO;
using System.Windows.Media.Imaging;
using System.Windows;
using System.Windows.Media;
using LabScape.App.Helpers;

namespace LabScape.App.Services;

/// <summary>
/// Direct3D GPU-accelerated stencil recoloring service.
/// Provides dynamic Fill, Stroke, and StrokeThickness customization for all SVG stencils (Devices, Furniture, Shapes).
/// </summary>
public static class SvgStencilService
{
    private static readonly Dictionary<string, DrawingImage> _defaultCache = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<string, DrawingImage> _customCache = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<string, ImageSource> _customImageCache = new(StringComparer.OrdinalIgnoreCase);

    public static ImageSource? GetItemImage(string? stencilType, string? fill = null, string? stroke = null, double? strokeThickness = null, string? customPath = null, string? customData = null, IDictionary<string, string>? colorMap = null)
    {
        if (!string.IsNullOrWhiteSpace(customData) || !string.IsNullOrWhiteSpace(customPath))
        {
            var custom = LoadCustomImage(customPath, customData, colorMap);
            if (custom != null)
                return custom;
        }

        return GetStencilImage(stencilType, fill, stroke, strokeThickness, colorMap);
    }

    public static ImageSource? LoadCustomImage(string? customPath, string? customData, IDictionary<string, string>? colorMap = null)
    {
        string colorMapKey = (colorMap != null && colorMap.Count > 0)
            ? string.Join(";", colorMap.OrderBy(k => k.Key).Select(k => $"{k.Key}->{k.Value}"))
            : "";

        if (!string.IsNullOrWhiteSpace(customData))
        {
            string cacheKey;
            using (var sha = System.Security.Cryptography.SHA256.Create())
            {
                byte[] hash = sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(customData + "|" + colorMapKey));
                cacheKey = Convert.ToHexString(hash);
            }

            if (_customImageCache.TryGetValue(cacheKey, out var cachedDataImg))
                return cachedDataImg;

            try
            {
                string text = customData;
                if (text.StartsWith("data:image/svg+xml;base64,", StringComparison.OrdinalIgnoreCase))
                {
                    byte[] raw = Convert.FromBase64String(text.Substring("data:image/svg+xml;base64,".Length));
                    text = System.Text.Encoding.UTF8.GetString(raw);
                }
                else if (text.StartsWith("data:image/svg+xml;utf8,", StringComparison.OrdinalIgnoreCase))
                {
                    text = Uri.UnescapeDataString(text.Substring("data:image/svg+xml;utf8,".Length));
                }

                if (text.TrimStart().StartsWith("<svg", StringComparison.OrdinalIgnoreCase) || text.Contains("<svg "))
                {
                    var dg = ParseSvg(text);
                    if (dg != null)
                    {
                        if (colorMap != null && colorMap.Count > 0)
                        {
                            RecolorDrawingGroup(dg, null, null, 0, colorMap);
                        }
                        var di = new DrawingImage(dg);
                        if (di.CanFreeze && !di.IsFrozen) di.Freeze();
                        _customImageCache[cacheKey] = di;
                        return di;
                    }
                }

                string base64 = customData;
                int commaIdx = base64.IndexOf(',');
                if (commaIdx >= 0)
                {
                    base64 = base64.Substring(commaIdx + 1);
                }

                byte[] bytes = System.Convert.FromBase64String(base64);
                using var ms = new MemoryStream(bytes);
                var bi = new BitmapImage();
                bi.BeginInit();
                bi.CacheOption = BitmapCacheOption.OnLoad;
                bi.StreamSource = ms;
                bi.EndInit();
                if (bi.CanFreeze && !bi.IsFrozen) bi.Freeze();

                _customImageCache[cacheKey] = bi;
                return bi;
            }
            catch
            {
                // Fallback to path or default stencil
            }
        }

        if (!string.IsNullOrWhiteSpace(customPath) && File.Exists(customPath))
        {
            string cacheKey = "file:" + customPath + "|" + colorMapKey;
            if (_customImageCache.TryGetValue(cacheKey, out var cachedFileImg))
                return cachedFileImg;

            try
            {
                if (customPath.EndsWith(".svg", StringComparison.OrdinalIgnoreCase))
                {
                    string svgContent = File.ReadAllText(customPath);
                    var dg = ParseSvg(svgContent);
                    if (dg != null)
                    {
                        if (colorMap != null && colorMap.Count > 0)
                        {
                            RecolorDrawingGroup(dg, null, null, 0, colorMap);
                        }
                        var di = new DrawingImage(dg);
                        if (di.CanFreeze && !di.IsFrozen) di.Freeze();
                        _customImageCache[cacheKey] = di;
                        return di;
                    }
                }

                var bi = new BitmapImage();
                bi.BeginInit();
                bi.CacheOption = BitmapCacheOption.OnLoad;
                bi.UriSource = new Uri(customPath, UriKind.Absolute);
                bi.EndInit();
                if (bi.CanFreeze && !bi.IsFrozen) bi.Freeze();

                _customImageCache[cacheKey] = bi;
                return bi;
            }
            catch
            {
                // Fallback to default stencil
            }
        }

        return null;
    }

    public static string ResolveResourceKey(string? rawType)
    {
        if (string.IsNullOrWhiteSpace(rawType)) return "Stencil_DesktopPC";

        return rawType.ToLowerInvariant() switch
        {
            "desktop_pc" or "desktop" or "pc" => "Stencil_DesktopPC",
            "laptop" => "Stencil_Laptop",
            "server_rack" or "server" => "Stencil_ServerRack",
            "switch" or "network_switch" => "Stencil_NetworkSwitch",
            "printer" => "Stencil_Printer",
            "screen" or "monitor" or "display" => "Stencil_Screen",
            "desk" => "Stencil_Desk",
            "office_chair" or "chair" => "Stencil_OfficeChair",
            "conference_table_8" or "conference_table" or "table" => "Stencil_ConferenceTable8",
            "sofa_3_seater" or "sofa" => "Stencil_Sofa3Seater",
            "bookcase" => "Stencil_Bookcase",
            "plant" => "Stencil_Plant",
            _ => rawType.StartsWith("Stencil_", StringComparison.OrdinalIgnoreCase) ? rawType : "Stencil_DesktopPC"
        };
    }

    public static IReadOnlyList<string> GetStencilColors(string? stencilType)
    {
        string resKey = ResolveResourceKey(stencilType);
        if (Application.Current?.TryFindResource(resKey) is not DrawingGroup dg)
            return Array.Empty<string>();

        var colors = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        CollectColorsFromDrawingGroup(dg, colors, seen);
        return colors;
    }

    public static IReadOnlyList<string> GetItemColors(string? stencilType, string? customPath = null, string? customData = null)
    {
        var colors = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (!string.IsNullOrWhiteSpace(customPath) && File.Exists(customPath) && customPath.EndsWith(".svg", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                string svg = File.ReadAllText(customPath);
                var dg = ParseSvg(svg);
                if (dg != null)
                {
                    CollectColorsFromDrawingGroup(dg, colors, seen);
                    return colors;
                }
            }
            catch { }
        }

        if (!string.IsNullOrWhiteSpace(customData) && (customData.Contains("<svg") || customData.Contains("svg+xml")))
        {
            try
            {
                string text = customData;
                if (text.StartsWith("data:image/svg+xml;base64,", StringComparison.OrdinalIgnoreCase))
                {
                    byte[] raw = Convert.FromBase64String(text.Substring("data:image/svg+xml;base64,".Length));
                    text = System.Text.Encoding.UTF8.GetString(raw);
                }
                else if (text.StartsWith("data:image/svg+xml;utf8,", StringComparison.OrdinalIgnoreCase))
                {
                    text = Uri.UnescapeDataString(text.Substring("data:image/svg+xml;utf8,".Length));
                }

                var dg = ParseSvg(text);
                if (dg != null)
                {
                    CollectColorsFromDrawingGroup(dg, colors, seen);
                    return colors;
                }
            }
            catch { }
        }

        return GetStencilColors(stencilType);
    }

    private static void CollectColorsFromDrawingGroup(DrawingGroup dg, List<string> colors, HashSet<string> seen)
    {
        foreach (var drawing in dg.Children)
        {
            if (drawing is GeometryDrawing gd)
            {
                if (gd.Brush is SolidColorBrush scb && scb.Color.A > 0)
                {
                    string hex = $"#{scb.Color.R:X2}{scb.Color.G:X2}{scb.Color.B:X2}";
                    if (seen.Add(hex)) colors.Add(hex);
                }
                if (gd.Pen?.Brush is SolidColorBrush pscb && pscb.Color.A > 0)
                {
                    string phex = $"#{pscb.Color.R:X2}{pscb.Color.G:X2}{pscb.Color.B:X2}";
                    if (seen.Add(phex)) colors.Add(phex);
                }
            }
            else if (drawing is DrawingGroup childGroup)
            {
                CollectColorsFromDrawingGroup(childGroup, colors, seen);
            }
        }
    }

    public static DrawingImage? GetStencilImage(string? stencilType, string? fill = null, string? stroke = null, double? strokeThickness = null, IDictionary<string, string>? colorMap = null)
    {
        string resKey = ResolveResourceKey(stencilType);

        bool isDefaultFill = string.IsNullOrWhiteSpace(fill);
        bool isDefaultStroke = string.IsNullOrWhiteSpace(stroke);
        bool isDefaultThickness = !strokeThickness.HasValue || strokeThickness.Value <= 0 || Math.Abs(strokeThickness.Value - 2.0) < 0.1;
        bool hasColorMap = colorMap != null && colorMap.Count > 0;

        if (isDefaultFill && isDefaultStroke && isDefaultThickness && !hasColorMap)
        {
            if (_defaultCache.TryGetValue(resKey, out var defImg))
                return defImg;

            if (Application.Current?.TryFindResource(resKey) is DrawingGroup dg)
            {
                var img = new DrawingImage(dg);
                if (img.CanFreeze && !img.IsFrozen) img.Freeze();
                _defaultCache[resKey] = img;
                return img;
            }
            return null;
        }

        string colorMapKey = hasColorMap
            ? string.Join(";", colorMap!.OrderBy(k => k.Key).Select(k => $"{k.Key}->{k.Value}"))
            : "";
        string cacheKey = $"{resKey}|{fill ?? ""}|{stroke ?? ""}|{(strokeThickness ?? 2.0):F1}|{colorMapKey}";
        if (_customCache.TryGetValue(cacheKey, out var customImg))
            return customImg;

        if (Application.Current?.TryFindResource(resKey) is not DrawingGroup baseDg)
            return null;

        var clonedDg = baseDg.Clone();

        Brush? fillBrush = !string.IsNullOrWhiteSpace(fill) ? BrushHelper.ParseBrush(fill) : null;
        Brush? strokeBrush = !string.IsNullOrWhiteSpace(stroke) ? BrushHelper.ParseBrush(stroke) : null;
        double thickness = strokeThickness.GetValueOrDefault(2.0);

        RecolorDrawingGroup(clonedDg, fillBrush, strokeBrush, thickness, colorMap);

        var finalImg = new DrawingImage(clonedDg);
        if (finalImg.CanFreeze && !finalImg.IsFrozen)
            finalImg.Freeze();

        _customCache[cacheKey] = finalImg;
        return finalImg;
    }

    private static void RecolorDrawingGroup(DrawingGroup dg, Brush? fillBrush, Brush? strokeBrush, double thickness, IDictionary<string, string>? colorMap = null)
    {
        Brush? primaryBrush = fillBrush;
        Brush? tintBrush = null;
        Brush? shadeBrush = null;
        Brush? screenBrush = null;

        if (fillBrush is SolidColorBrush scb)
        {
            var c = scb.Color;
            float lum = (0.299f * c.R + 0.587f * c.G + 0.114f * c.B) / 255f;

            Color tintColor = lum > 0.5f
                ? Color.FromRgb(
                    (byte)Math.Max(0, c.R * 0.94),
                    (byte)Math.Max(0, c.G * 0.94),
                    (byte)Math.Max(0, c.B * 0.94))
                : Color.FromRgb(
                    (byte)Math.Min(255, c.R + (255 - c.R) * 0.28),
                    (byte)Math.Min(255, c.G + (255 - c.G) * 0.28),
                    (byte)Math.Min(255, c.B + (255 - c.B) * 0.28));

            Color shadeColor = lum > 0.5f
                ? Color.FromRgb(
                    (byte)Math.Max(0, c.R * 0.82),
                    (byte)Math.Max(0, c.G * 0.82),
                    (byte)Math.Max(0, c.B * 0.82))
                : Color.FromRgb(
                    (byte)Math.Max(0, c.R * 0.65),
                    (byte)Math.Max(0, c.G * 0.65),
                    (byte)Math.Max(0, c.B * 0.65));

            Color screenColor = lum > 0.5f
                ? Color.FromRgb(15, 23, 42) // Deep tech screen glass (#0F172A)
                : Color.FromRgb(
                    (byte)Math.Min(255, c.R * 1.35 + 20),
                    (byte)Math.Min(255, c.G * 1.35 + 20),
                    (byte)Math.Min(255, c.B * 1.35 + 30));

            var tb = new SolidColorBrush(tintColor); if (tb.CanFreeze) tb.Freeze(); tintBrush = tb;
            var sb = new SolidColorBrush(shadeColor); if (sb.CanFreeze) sb.Freeze(); shadeBrush = sb;
            var sc = new SolidColorBrush(screenColor); if (sc.CanFreeze) sc.Freeze(); screenBrush = sc;
        }
        else if (fillBrush is LinearGradientBrush lgb && lgb.GradientStops.Count > 1)
        {
            var acc = new SolidColorBrush(lgb.GradientStops[1].Color);
            if (acc.CanFreeze) acc.Freeze();
            tintBrush = acc;
            shadeBrush = acc;
        }

        foreach (var drawing in dg.Children)
        {
            if (drawing is GeometryDrawing gd)
            {
                // 1. Process Fill: Check individual colorMap first, fallback to fillBrush tinting
                if (gd.Brush is SolidColorBrush origScb)
                {
                    var origC = origScb.Color;
                    string origHex = $"#{origC.R:X2}{origC.G:X2}{origC.B:X2}";

                    if (colorMap != null && TryGetMappedColor(colorMap, origHex, out var mappedHex))
                    {
                        var mappedBrush = BrushHelper.ParseBrush(mappedHex);
                        if (mappedBrush != null)
                        {
                            if (mappedBrush.CanFreeze && !mappedBrush.IsFrozen) mappedBrush.Freeze();
                            gd.Brush = mappedBrush;
                        }
                    }
                    else if (fillBrush != null)
                    {
                        bool isStatusLed = origC.B > 220 && origC.R < 100 && origC.G > 150;
                        if (!isStatusLed)
                        {
                            float origLum = (0.299f * origC.R + 0.587f * origC.G + 0.114f * origC.B) / 255f;
                            if (origLum > 0.75f)
                            {
                                gd.Brush = primaryBrush;
                            }
                            else if (origLum >= 0.35f)
                            {
                                gd.Brush = tintBrush ?? primaryBrush;
                            }
                            else
                            {
                                bool isScreenShape = gd.Geometry is PathGeometry ||
                                                    (gd.Geometry is RectangleGeometry rg && rg.Rect.Height > 25 && rg.Rect.Width > 50);
                                gd.Brush = isScreenShape ? (screenBrush ?? shadeBrush ?? primaryBrush) : (shadeBrush ?? primaryBrush);
                            }
                        }
                    }
                }
                else if (fillBrush != null && gd.Brush != null)
                {
                    gd.Brush = primaryBrush;
                }

                // 2. Process Stroke: Check individual colorMap first, fallback to strokeBrush
                if (gd.Pen != null)
                {
                    if (gd.Pen.Brush is SolidColorBrush origPenScb)
                    {
                        var pc = origPenScb.Color;
                        string penHex = $"#{pc.R:X2}{pc.G:X2}{pc.B:X2}";

                        if (colorMap != null && TryGetMappedColor(colorMap, penHex, out var mappedPenHex))
                        {
                            var mappedPenBrush = BrushHelper.ParseBrush(mappedPenHex);
                            if (mappedPenBrush != null)
                            {
                                if (mappedPenBrush.CanFreeze && !mappedPenBrush.IsFrozen) mappedPenBrush.Freeze();
                                gd.Pen.Brush = mappedPenBrush;
                            }
                        }
                        else if (strokeBrush != null)
                        {
                            gd.Pen.Brush = strokeBrush;
                        }
                    }
                    else if (strokeBrush != null)
                    {
                        gd.Pen.Brush = strokeBrush;
                    }

                    if (thickness > 0)
                    {
                        var bounds = gd.Geometry != null ? gd.Geometry.Bounds : Rect.Empty;
                        bool isVeryThin = !bounds.IsEmpty && (bounds.Height <= 6.0 || bounds.Width <= 6.0);
                        gd.Pen.Thickness = isVeryThin ? Math.Min(thickness, 1.25) : thickness;
                    }
                }
            }
            else if (drawing is DrawingGroup childGroup)
            {
                RecolorDrawingGroup(childGroup, fillBrush, strokeBrush, thickness, colorMap);
            }
        }
    }

    private static bool TryGetMappedColor(IDictionary<string, string> map, string hex, out string mappedHex)
    {
        if (map.TryGetValue(hex, out mappedHex!)) return true;
        string norm = hex.StartsWith('#') ? hex.Substring(1) : hex;
        foreach (var kv in map)
        {
            string kNorm = kv.Key.StartsWith('#') ? kv.Key.Substring(1) : kv.Key;
            if (string.Equals(norm, kNorm, StringComparison.OrdinalIgnoreCase))
            {
                mappedHex = kv.Value;
                return true;
            }
        }
        mappedHex = string.Empty;
        return false;
    }

    public static DrawingGroup? ParseSvg(string svgXml)
    {
        if (string.IsNullOrWhiteSpace(svgXml)) return null;
        try
        {
            var doc = System.Xml.Linq.XDocument.Parse(svgXml);
            var root = doc.Root;
            if (root == null) return null;

            var dg = new DrawingGroup();
            ParseSvgElement(root, dg);
            return dg;
        }
        catch
        {
            return null;
        }
    }

    private static void ParseSvgElement(System.Xml.Linq.XElement el, DrawingGroup dg)
    {
        string name = el.Name.LocalName.ToLowerInvariant();
        switch (name)
        {
            case "rect":
                {
                    double x = GetDoubleAttr(el, "x");
                    double y = GetDoubleAttr(el, "y");
                    double w = GetDoubleAttr(el, "width");
                    double h = GetDoubleAttr(el, "height");
                    double rx = GetDoubleAttr(el, "rx");
                    double ry = GetDoubleAttr(el, "ry", rx);
                    if (w > 0 && h > 0)
                    {
                        var geom = new RectangleGeometry(new Rect(x, y, w, h), rx, ry);
                        dg.Children.Add(new GeometryDrawing(ParseSvgBrush(GetAttr(el, "fill")), ParseSvgPen(el), geom));
                    }
                    break;
                }
            case "circle":
                {
                    double cx = GetDoubleAttr(el, "cx");
                    double cy = GetDoubleAttr(el, "cy");
                    double r = GetDoubleAttr(el, "r");
                    if (r > 0)
                    {
                        var geom = new EllipseGeometry(new Point(cx, cy), r, r);
                        dg.Children.Add(new GeometryDrawing(ParseSvgBrush(GetAttr(el, "fill")), ParseSvgPen(el), geom));
                    }
                    break;
                }
            case "ellipse":
                {
                    double cx = GetDoubleAttr(el, "cx");
                    double cy = GetDoubleAttr(el, "cy");
                    double rx = GetDoubleAttr(el, "rx");
                    double ry = GetDoubleAttr(el, "ry");
                    if (rx > 0 && ry > 0)
                    {
                        var geom = new EllipseGeometry(new Point(cx, cy), rx, ry);
                        dg.Children.Add(new GeometryDrawing(ParseSvgBrush(GetAttr(el, "fill")), ParseSvgPen(el), geom));
                    }
                    break;
                }
            case "line":
                {
                    double x1 = GetDoubleAttr(el, "x1");
                    double y1 = GetDoubleAttr(el, "y1");
                    double x2 = GetDoubleAttr(el, "x2");
                    double y2 = GetDoubleAttr(el, "y2");
                    var geom = new LineGeometry(new Point(x1, y1), new Point(x2, y2));
                    dg.Children.Add(new GeometryDrawing(null, ParseSvgPen(el) ?? new Pen(Brushes.Black, 1.0), geom));
                    break;
                }
            case "path":
                {
                    string? d = GetAttr(el, "d");
                    if (!string.IsNullOrWhiteSpace(d))
                    {
                        try
                        {
                            var geom = Geometry.Parse(d);
                            dg.Children.Add(new GeometryDrawing(ParseSvgBrush(GetAttr(el, "fill")), ParseSvgPen(el), geom));
                        }
                        catch { }
                    }
                    break;
                }
            case "polygon":
            case "polyline":
                {
                    string? pts = GetAttr(el, "points");
                    if (!string.IsNullOrWhiteSpace(pts))
                    {
                        try
                        {
                            string d = "M " + pts + (name == "polygon" ? " Z" : "");
                            var geom = Geometry.Parse(d);
                            dg.Children.Add(new GeometryDrawing(ParseSvgBrush(GetAttr(el, "fill")), ParseSvgPen(el), geom));
                        }
                        catch { }
                    }
                    break;
                }
        }

        foreach (var child in el.Elements())
        {
            ParseSvgElement(child, dg);
        }
    }

    private static string? GetAttr(System.Xml.Linq.XElement el, string name)
    {
        return el.Attribute(name)?.Value ?? el.Attribute(System.Xml.Linq.XName.Get(name, el.Name.NamespaceName))?.Value;
    }

    private static double GetDoubleAttr(System.Xml.Linq.XElement el, string name, double fallback = 0.0)
    {
        string? val = GetAttr(el, name);
        if (string.IsNullOrWhiteSpace(val)) return fallback;
        val = val.TrimEnd('p', 'x', 'P', 'X');
        return double.TryParse(val, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double d) ? d : fallback;
    }

    private static Brush? ParseSvgBrush(string? fill)
    {
        if (string.IsNullOrWhiteSpace(fill) || fill.Equals("none", StringComparison.OrdinalIgnoreCase))
            return null;
        return BrushHelper.ParseBrush(fill);
    }

    private static Pen? ParseSvgPen(System.Xml.Linq.XElement el)
    {
        string? stroke = GetAttr(el, "stroke");
        if (string.IsNullOrWhiteSpace(stroke) || stroke.Equals("none", StringComparison.OrdinalIgnoreCase))
            return null;

        var brush = BrushHelper.ParseBrush(stroke);
        if (brush == null) return null;

        double width = GetDoubleAttr(el, "stroke-width", 1.0);
        var pen = new Pen(brush, width);
        if (pen.CanFreeze && !pen.IsFrozen) pen.Freeze();
        return pen;
    }
}
