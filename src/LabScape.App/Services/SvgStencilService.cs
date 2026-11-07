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
        // Extract secondary accent color from fillBrush if solid
        Brush? accentBrush = null;
        if (fillBrush is SolidColorBrush scb)
        {
            var c = scb.Color;
            float luminance = (0.299f * c.R + 0.587f * c.G + 0.114f * c.B) / 255f;
            Color accentColor;
            if (luminance > 0.5f)
            {
                accentColor = Color.FromRgb(
                    (byte)Math.Max(0, c.R * 0.8),
                    (byte)Math.Max(0, c.G * 0.8),
                    (byte)Math.Max(0, c.B * 0.8));
            }
            else
            {
                accentColor = Color.FromRgb(
                    (byte)Math.Min(255, c.R * 1.3 + 25),
                    (byte)Math.Min(255, c.G * 1.3 + 25),
                    (byte)Math.Min(255, c.B * 1.3 + 25));
            }
            var acc = new SolidColorBrush(accentColor);
            acc.Freeze();
            accentBrush = acc;
        }
        else if (fillBrush is LinearGradientBrush lgb && lgb.GradientStops.Count > 1)
        {
            var acc = new SolidColorBrush(lgb.GradientStops[1].Color);
            acc.Freeze();
            accentBrush = acc;
        }

        int geometryIndex = 0;
        foreach (var drawing in dg.Children)
        {
            if (drawing is GeometryDrawing gd)
            {
                if (fillBrush != null && gd.Brush != null)
                {
                    if (geometryIndex == 0)
                    {
                        gd.Brush = fillBrush;
                    }
                    else
                    {
                        gd.Brush = accentBrush ?? fillBrush;
                    }
                }

                if (strokeBrush != null && gd.Pen != null)
                {
                    gd.Pen.Brush = strokeBrush;
                }

                if (thickness > 0 && gd.Pen != null)
                {
                    gd.Pen.Thickness = thickness;
                }

                geometryIndex++;
            }
            else if (drawing is DrawingGroup childGroup)
            {
                RecolorDrawingGroup(childGroup, fillBrush, strokeBrush, thickness);
            }
        }
    }
}
