using System.Globalization;
using System.Windows;
using System.Windows.Media;

namespace LayoutDesigner.App.Controls;

public enum RulerOrientation
{
    Horizontal,
    Vertical
}

public class RulerControl : FrameworkElement
{
    public static readonly DependencyProperty OrientationProperty =
        DependencyProperty.Register(nameof(Orientation), typeof(RulerOrientation), typeof(RulerControl),
            new FrameworkPropertyMetadata(RulerOrientation.Horizontal, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty ZoomLevelProperty =
        DependencyProperty.Register(nameof(ZoomLevel), typeof(double), typeof(RulerControl),
            new FrameworkPropertyMetadata(1.0, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty OffsetProperty =
        DependencyProperty.Register(nameof(Offset), typeof(double), typeof(RulerControl),
            new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty CursorPositionProperty =
        DependencyProperty.Register(nameof(CursorPosition), typeof(double), typeof(RulerControl),
            new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsRender));

    public RulerOrientation Orientation
    {
        get => (RulerOrientation)GetValue(OrientationProperty);
        set => SetValue(OrientationProperty, value);
    }

    public double ZoomLevel
    {
        get => (double)GetValue(ZoomLevelProperty);
        set => SetValue(ZoomLevelProperty, value);
    }

    public double Offset
    {
        get => (double)GetValue(OffsetProperty);
        set => SetValue(OffsetProperty, value);
    }

    public double CursorPosition
    {
        get => (double)GetValue(CursorPositionProperty);
        set => SetValue(CursorPositionProperty, value);
    }

    private static readonly Brush BackgroundBrush = new SolidColorBrush(Color.FromRgb(248, 249, 250));
    private static readonly Pen BorderPen = new(new SolidColorBrush(Color.FromRgb(222, 226, 230)), 1);
    private static readonly Pen TickPen = new(new SolidColorBrush(Color.FromRgb(173, 181, 189)), 1);
    private static readonly Pen CursorPen = new(new SolidColorBrush(Color.FromRgb(220, 53, 69)), 1.5);
    private static readonly Typeface TextTypeface = new("Segoe UI");

    static RulerControl()
    {
        BackgroundBrush.Freeze();
        BorderPen.Freeze();
        TickPen.Freeze();
        CursorPen.Freeze();
    }

    protected override void OnRender(DrawingContext dc)
    {
        base.OnRender(dc);

        double width = ActualWidth;
        double height = ActualHeight;
        if (width <= 0 || height <= 0) return;

        // Draw background & border
        dc.DrawRectangle(BackgroundBrush, null, new Rect(0, 0, width, height));

        if (Orientation == RulerOrientation.Horizontal)
        {
            dc.DrawLine(BorderPen, new Point(0, height - 1), new Point(width, height - 1));
            DrawHorizontalTicks(dc, width, height);
        }
        else
        {
            dc.DrawLine(BorderPen, new Point(width - 1, 0), new Point(width - 1, height));
            DrawVerticalTicks(dc, width, height);
        }
    }

    private void DrawHorizontalTicks(DrawingContext dc, double width, double height)
    {
        double step = 100 * ZoomLevel;
        if (step < 40) step *= 2;
        if (step > 200) step /= 2;

        double startX = Offset % step;
        double startValue = Math.Floor(-Offset / ZoomLevel / 100) * 100;

        for (double x = startX; x < width; x += step)
        {
            double value = Math.Round((x - Offset) / ZoomLevel);
            if (value < 0) continue;

            // Major tick
            dc.DrawLine(TickPen, new Point(x, height - 10), new Point(x, height));

            var formatted = new FormattedText(
                value.ToString(CultureInfo.InvariantCulture),
                CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                TextTypeface,
                9,
                Brushes.DimGray,
                VisualTreeHelper.GetDpi(this).PixelsPerDip);

            dc.DrawText(formatted, new Point(x + 2, 2));

            // Medium & minor ticks
            double subStep = step / 10;
            for (int i = 1; i < 10; i++)
            {
                double sx = x + i * subStep;
                if (sx >= width) break;
                double tickLen = (i == 5) ? 6 : 3;
                dc.DrawLine(TickPen, new Point(sx, height - tickLen), new Point(sx, height));
            }
        }

        // Draw cursor indicator
        double cursorScreen = CursorPosition * ZoomLevel + Offset;
        if (cursorScreen >= 0 && cursorScreen <= width)
        {
            dc.DrawLine(CursorPen, new Point(cursorScreen, 0), new Point(cursorScreen, height));
        }
    }

    private void DrawVerticalTicks(DrawingContext dc, double width, double height)
    {
        double step = 100 * ZoomLevel;
        if (step < 40) step *= 2;
        if (step > 200) step /= 2;

        double startY = Offset % step;

        for (double y = startY; y < height; y += step)
        {
            double value = Math.Round((y - Offset) / ZoomLevel);
            if (value < 0) continue;

            // Major tick
            dc.DrawLine(TickPen, new Point(width - 10, y), new Point(width, y));

            var formatted = new FormattedText(
                value.ToString(CultureInfo.InvariantCulture),
                CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                TextTypeface,
                8,
                Brushes.DimGray,
                VisualTreeHelper.GetDpi(this).PixelsPerDip);

            // Rotate text for vertical ruler
            dc.PushTransform(new RotateTransform(-90, width - 12, y + 2));
            dc.DrawText(formatted, new Point(width - 12, y + 2));
            dc.Pop();

            // Medium & minor ticks
            double subStep = step / 10;
            for (int i = 1; i < 10; i++)
            {
                double sy = y + i * subStep;
                if (sy >= height) break;
                double tickLen = (i == 5) ? 6 : 3;
                dc.DrawLine(TickPen, new Point(width - tickLen, sy), new Point(width, sy));
            }
        }

        // Draw cursor indicator
        double cursorScreen = CursorPosition * ZoomLevel + Offset;
        if (cursorScreen >= 0 && cursorScreen <= height)
        {
            dc.DrawLine(CursorPen, new Point(0, cursorScreen), new Point(width, cursorScreen));
        }
    }
}
