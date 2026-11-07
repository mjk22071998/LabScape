using System;

namespace LabScape.Core.Enums;

public enum PageSizePreset
{
    A4,
    A3,
    A2,
    A1,
    Letter,
    Legal,
    Tabloid,
    Screen1080p,
    Custom
}

public enum PageOrientation
{
    Landscape,
    Portrait
}

public static class PageDimensions
{
    // Standard dimensions in 96 DPI device-independent pixels (1 in = 96 px, 1 mm = 3.779527559 px)
    // A4: 297 x 210 mm -> 1123 x 794 px
    // A3: 420 x 297 mm -> 1587 x 1123 px (Default enterprise lab layout standard)
    // A2: 594 x 420 mm -> 2245 x 1587 px
    // A1: 841 x 594 mm -> 3179 x 2245 px
    // Letter: 11 x 8.5 in -> 1056 x 816 px
    // Legal: 14 x 8.5 in -> 1344 x 816 px
    // Tabloid: 17 x 11 in -> 1632 x 1056 px
    // Screen1080p: 1920 x 1080 px

    public static (double width, double height) GetDimensions(PageSizePreset preset, PageOrientation orientation)
    {
        (double w, double h) = preset switch
        {
            PageSizePreset.A4 => (1123, 794),
            PageSizePreset.A3 => (1587, 1123),
            PageSizePreset.A2 => (2245, 1587),
            PageSizePreset.A1 => (3179, 2245),
            PageSizePreset.Letter => (1056, 816),
            PageSizePreset.Legal => (1344, 816),
            PageSizePreset.Tabloid => (1632, 1056),
            PageSizePreset.Screen1080p => (1920, 1080),
            _ => (1587, 1123)
        };

        if (orientation == PageOrientation.Portrait)
        {
            return (Math.Min(w, h), Math.Max(w, h));
        }
        else
        {
            return (Math.Max(w, h), Math.Min(w, h));
        }
    }

    public static string GetDisplayName(PageSizePreset preset) => preset switch
    {
        PageSizePreset.A4 => "A4 (297 × 210 mm)",
        PageSizePreset.A3 => "A3 (420 × 297 mm)",
        PageSizePreset.A2 => "A2 (594 × 420 mm)",
        PageSizePreset.A1 => "A1 (841 × 594 mm)",
        PageSizePreset.Letter => "US Letter (8.5 × 11 in)",
        PageSizePreset.Legal => "US Legal (8.5 × 14 in)",
        PageSizePreset.Tabloid => "US Tabloid / Ledger (11 × 17 in)",
        PageSizePreset.Screen1080p => "Full HD 1080p (1920 × 1080)",
        PageSizePreset.Custom => "Custom Size",
        _ => preset.ToString()
    };

    public static PageSizePreset DetectPreset(double width, double height, out PageOrientation orientation)
    {
        orientation = width >= height ? PageOrientation.Landscape : PageOrientation.Portrait;
        double maxDim = Math.Max(width, height);
        double minDim = Math.Min(width, height);

        foreach (PageSizePreset p in Enum.GetValues<PageSizePreset>())
        {
            if (p == PageSizePreset.Custom) continue;
            var (pw, ph) = GetDimensions(p, PageOrientation.Landscape);
            if (Math.Abs(maxDim - pw) <= 3 && Math.Abs(minDim - ph) <= 3)
            {
                return p;
            }
        }

        return PageSizePreset.Custom;
    }
}
