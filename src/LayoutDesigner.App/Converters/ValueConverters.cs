using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using LayoutDesigner.App.Helpers;
using LayoutDesigner.Core.Enums;
using LayoutDesigner.ViewModels.Items;

namespace LayoutDesigner.App.Converters;

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
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string hex && !string.IsNullOrWhiteSpace(hex))
        {
            try
            {
                var color = (Color)ColorConverter.ConvertFromString(hex);
                return new SolidColorBrush(color);
            }
            catch
            {
                // fallback
            }
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
    public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        string key = (value as string) switch
        {
            "laptop" => "Stencil_Laptop",
            "server_rack" => "Stencil_ServerRack",
            _ => "Stencil_DesktopPC"
        };

        if (Application.Current?.TryFindResource(key) is DrawingGroup drawing)
        {
            return new DrawingImage(drawing);
        }
        return null;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
}

public class FurnitureTypeToImageConverter : IValueConverter
{
    public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        string key = (value as string) switch
        {
            "office_chair" => "Stencil_OfficeChair",
            "conference_table_8" => "Stencil_ConferenceTable8",
            "sofa_3_seater" => "Stencil_Sofa3Seater",
            _ => "Stencil_Desk"
        };

        if (Application.Current?.TryFindResource(key) is DrawingGroup drawing)
        {
            return new DrawingImage(drawing);
        }
        return null;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
}

public class ShapeToGeometryConverter : IValueConverter
{
    public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is ShapeItemViewModel shape)
        {
            return ShapeGeometryHelper.CreateGeometry(shape);
        }
        return null;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
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

