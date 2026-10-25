using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using LabScape.App.Helpers;
using LabScape.Core.Enums;
using LabScape.Core.Helpers;
using LabScape.ViewModels.Items;

namespace LabScape.App.Converters;

public class HalfValueConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is double d) return d / 2.0;
        return 0.0;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
}

public class HexToBrushConverter : IValueConverter
{
    private static readonly Dictionary<string, Brush?> _cache = new(StringComparer.OrdinalIgnoreCase);

    public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string hex && !string.IsNullOrWhiteSpace(hex))
        {
            if (_cache.TryGetValue(hex, out var cached))
                return cached;

            var brush = BrushHelper.ParseBrush(hex);
            if (brush != null && brush.CanFreeze && !brush.IsFrozen)
                brush.Freeze();

            _cache[hex] = brush;
            return brush;
        }
        return Brushes.Transparent;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
}

public class ValidationBadgeVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is ValidationStatus status)
        {
            return status != ValidationStatus.Valid ? Visibility.Visible : Visibility.Collapsed;
        }
        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
}

public class StringNotEmptyToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return !string.IsNullOrWhiteSpace(value as string) ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
}

public class DeviceTypeToImageConverter : IValueConverter
{
    private static readonly Dictionary<string, DrawingImage> _cache = new(StringComparer.OrdinalIgnoreCase);

    public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        string key = (value as string)?.ToLowerInvariant() switch
        {
            "laptop" => "Stencil_Laptop",
            "server_rack" => "Stencil_ServerRack",
            "switch" => "Stencil_NetworkSwitch",
            "printer" => "Stencil_Printer",
            "screen" or "monitor" or "display" => "Stencil_Screen",
            _ => "Stencil_DesktopPC"
        };

        if (_cache.TryGetValue(key, out var cached))
            return cached;

        if (Application.Current?.TryFindResource(key) is DrawingGroup drawing)
        {
            var img = new DrawingImage(drawing);
            if (img.CanFreeze && !img.IsFrozen)
                img.Freeze();
            _cache[key] = img;
            return img;
        }
        return null;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
}

public class FurnitureTypeToImageConverter : IValueConverter
{
    private static readonly Dictionary<string, DrawingImage> _cache = new(StringComparer.OrdinalIgnoreCase);

    public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        string key = (value as string) switch
        {
            "office_chair" => "Stencil_OfficeChair",
            "conference_table_8" => "Stencil_ConferenceTable8",
            "sofa_3_seater" => "Stencil_Sofa3Seater",
            "bookcase" => "Stencil_Bookcase",
            "plant" => "Stencil_Plant",
            _ => "Stencil_Desk"
        };

        if (_cache.TryGetValue(key, out var cached))
            return cached;

        if (Application.Current?.TryFindResource(key) is DrawingGroup drawing)
        {
            var img = new DrawingImage(drawing);
            if (img.CanFreeze && !img.IsFrozen)
                img.Freeze();
            _cache[key] = img;
            return img;
        }
        return null;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
}

public class ShapeToGeometryConverter : IValueConverter, IMultiValueConverter
{
    public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is ShapeItemViewModel shape)
        {
            var geom = ShapeGeometryHelper.CreateGeometry(shape);
            if (geom != null && geom.CanFreeze && !geom.IsFrozen)
                geom.Freeze();
            return geom;
        }
        return null;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();

    public object? Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length > 0 && values[0] is ShapeItemViewModel shape)
        {
            var geom = ShapeGeometryHelper.CreateGeometry(shape);
            if (geom != null && geom.CanFreeze && !geom.IsFrozen)
                geom.Freeze();
            return geom;
        }
        return null;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) => throw new NotImplementedException();
}

public class ResourceKeyToImageConverter : IValueConverter
{
    public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string key && Application.Current?.TryFindResource(key) is DrawingGroup drawing)
        {
            return new DrawingImage(drawing);
        }
        return null;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
}

public class IntToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is int count)
        {
            return count > 0 ? Visibility.Visible : Visibility.Collapsed;
        }
        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
}

public class DeviceLabelWrapConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not string text || string.IsNullOrEmpty(text))
            return value;

        return TextWrapHelper.InsertSoftBreakOpportunities(text);
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string s)
        {
            return TextWrapHelper.StripSoftBreaks(s);
        }
        return value ?? string.Empty;
    }
}

public class RelativeOffsetConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length >= 2 && values[0] is double childCoord && values[1] is double groupCoord)
        {
            return childCoord - groupCoord;
        }
        return 0.0;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) => throw new NotImplementedException();
}


