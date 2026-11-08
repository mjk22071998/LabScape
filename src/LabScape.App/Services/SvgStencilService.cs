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

    public static ImageSource? GetItemImage(string? stencilType, string? fill = null, string? stroke = null, double? strokeThickness = null, string? customPath = null, string? customData = null)
    {
        if (!string.IsNullOrWhiteSpace(customData) || !string.IsNullOrWhiteSpace(customPath))
        {
            var custom = LoadCustomImage(customPath, customData);
            if (custom != null)
                return custom;
        }

        return GetStencilImage(stencilType, fill, stroke, strokeThickness);
    }

    public static ImageSource? LoadCustomImage(string? customPath, string? customData)
    {
        if (!string.IsNullOrWhiteSpace(customData))
        {
            string cacheKey;
            using (var sha = System.Security.Cryptography.SHA256.Create())
            {
                byte[] hash = sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(customData));
                cacheKey = Convert.ToHexString(hash);
            }

            if (_customImageCache.TryGetValue(cacheKey, out var cachedDataImg))
                return cachedDataImg;

            try
            {
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
            string cacheKey = "file:" + customPath;
            if (_customImageCache.TryGetValue(cacheKey, out var cachedFileImg))
                return cachedFileImg;

            try
            {
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

    public static DrawingImage? GetStencilImage(string? stencilType, string? fill = null, string? stroke = null, double? strokeThickness = null)
    {
        string resKey = ResolveResourceKey(stencilType);

        bool isDefaultFill = string.IsNullOrWhiteSpace(fill);
        bool isDefaultStroke = string.IsNullOrWhiteSpace(stroke);
        bool isDefaultThickness = !strokeThickness.HasValue || strokeThickness.Value <= 0 || Math.Abs(strokeThickness.Value - 2.0) < 0.1;

        if (isDefaultFill && isDefaultStroke && isDefaultThickness)
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

        string cacheKey = $"{resKey}|{fill ?? ""}|{stroke ?? ""}|{(strokeThickness ?? 2.0):F1}";
        if (_customCache.TryGetValue(cacheKey, out var customImg))
            return customImg;

        if (Application.Current?.TryFindResource(resKey) is not DrawingGroup baseDg)
            return null;

        var clonedDg = baseDg.Clone();

        Brush? fillBrush = !string.IsNullOrWhiteSpace(fill) ? BrushHelper.ParseBrush(fill) : null;
        Brush? strokeBrush = !string.IsNullOrWhiteSpace(stroke) ? BrushHelper.ParseBrush(stroke) : null;
        double thickness = strokeThickness.GetValueOrDefault(2.0);

        RecolorDrawingGroup(clonedDg, fillBrush, strokeBrush, thickness);

        var finalImg = new DrawingImage(clonedDg);
        if (finalImg.CanFreeze && !finalImg.IsFrozen)
            finalImg.Freeze();

        _customCache[cacheKey] = finalImg;
        return finalImg;
    }

    private static void RecolorDrawingGroup(DrawingGroup dg, Brush? fillBrush, Brush? strokeBrush, double thickness)
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
                if (fillBrush != null && gd.Brush != null)
                {
                    if (gd.Brush is SolidColorBrush origScb)
                    {
                        var origC = origScb.Color;
                        // Preserve small illuminated status LEDs (e.g. cyan/green LEDs)
                        bool isStatusLed = origC.B > 220 && origC.R < 100 && origC.G > 150;

                        if (!isStatusLed)
                        {
                            float origLum = (0.299f * origC.R + 0.587f * origC.G + 0.114f * origC.B) / 255f;

                            if (origLum > 0.75f)
                            {
                                // Main body surface / keyboard / table top / chair cushion
                                gd.Brush = primaryBrush;
                            }
                            else if (origLum >= 0.35f)
                            {
                                // Secondary panel, drawer, stand neck
                                gd.Brush = tintBrush ?? primaryBrush;
                            }
                            else
                            {
                                // Screen or chassis shadow
                                bool isScreenShape = gd.Geometry is PathGeometry || 
                                                    (gd.Geometry is RectangleGeometry rg && rg.Rect.Height > 25 && rg.Rect.Width > 50);

                                gd.Brush = isScreenShape ? (screenBrush ?? shadeBrush ?? primaryBrush) : (shadeBrush ?? primaryBrush);
                            }
                        }
                    }
                    else
                    {
                        gd.Brush = primaryBrush;
                    }
                }

                if (strokeBrush != null && gd.Pen != null)
                {
                    gd.Pen.Brush = strokeBrush;
                }

                if (thickness > 0 && gd.Pen != null)
                {
                    var bounds = gd.Geometry != null ? gd.Geometry.Bounds : Rect.Empty;
                    bool isVeryThin = !bounds.IsEmpty && (bounds.Height <= 6.0 || bounds.Width <= 6.0);
                    gd.Pen.Thickness = isVeryThin ? Math.Min(thickness, 1.25) : thickness;
                }
            }
            else if (drawing is DrawingGroup childGroup)
            {
                RecolorDrawingGroup(childGroup, fillBrush, strokeBrush, thickness);
            }
        }
    }
}
