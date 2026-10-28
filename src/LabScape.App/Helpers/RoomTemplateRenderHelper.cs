using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using LabScape.ViewModels.Templates;

namespace LabScape.App.Helpers;

public static class RoomTemplateRenderHelper
{
    public static void PopulateCanvas(Canvas canvas, RoomTemplateResult result, FrameworkElement resourceHost)
    {
        canvas.Children.Clear();
        if (result.RoomWidth <= 0 || result.RoomHeight <= 0) return;

        canvas.Width = result.RoomWidth;
        canvas.Height = result.RoomHeight;

        // 1. Subtle Inner Room Floor Guideline
        var floorGuide = new System.Windows.Shapes.Rectangle
        {
            Width = result.RoomWidth,
            Height = result.RoomHeight,
            Fill = new SolidColorBrush(Color.FromArgb(14, 2, 132, 199)),
            Stroke = new SolidColorBrush(Color.FromArgb(40, 2, 132, 199)),
            StrokeThickness = 1.2,
            StrokeDashArray = new DoubleCollection { 4, 4 }
        };
        Canvas.SetLeft(floorGuide, 0);
        Canvas.SetTop(floorGuide, 0);
        canvas.Children.Add(floorGuide);

        // 2. Render Perimeter Walls (exact coordinates)
        var wallBrush = FindResourceSafe<Brush>(resourceHost, "AccentPrimaryBrush")
            ?? new SolidColorBrush(Color.FromRgb(2, 132, 199));

        foreach (var wall in result.Walls)
        {
            var line = new Line
            {
                X1 = wall.StartX,
                Y1 = wall.StartY,
                X2 = wall.EndX,
                Y2 = wall.EndY,
                Stroke = wallBrush,
                StrokeThickness = 4.5,
                StrokeStartLineCap = PenLineCap.Round,
                StrokeEndLineCap = PenLineCap.Round
            };
            canvas.Children.Add(line);
        }

        // 3. Render Modular Desks (exact geometry)
        foreach (var desk in result.Desks)
        {
            var deskBorder = new Border
            {
                Width = desk.Width,
                Height = desk.Height,
                Background = new SolidColorBrush(Color.FromArgb(235, 226, 232, 240)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(100, 116, 139)),
                BorderThickness = new Thickness(1.5),
                CornerRadius = new CornerRadius(3)
            };

            var deskLabel = new TextBlock
            {
                Text = "Desk",
                FontSize = 9.5,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(71, 85, 105)),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            deskBorder.Child = deskLabel;

            if (desk.Rotation != 0)
            {
                deskBorder.RenderTransformOrigin = new Point(0.5, 0.5);
                deskBorder.RenderTransform = new RotateTransform(desk.Rotation);
            }

            Canvas.SetLeft(deskBorder, desk.X);
            Canvas.SetTop(deskBorder, desk.Y);
            canvas.Children.Add(deskBorder);
        }

        // 4. Render Ergonomic Swivel Chairs (exact geometry)
        foreach (var chair in result.Chairs)
        {
            var chairContainer = new Grid
            {
                Width = chair.Width,
                Height = chair.Height
            };

            var chairBase = new System.Windows.Shapes.Ellipse
            {
                Width = chair.Width,
                Height = chair.Height,
                Fill = new SolidColorBrush(Color.FromArgb(190, 203, 213, 225)),
                Stroke = new SolidColorBrush(Color.FromRgb(71, 85, 105)),
                StrokeThickness = 1.2
            };
            chairContainer.Children.Add(chairBase);

            // Curved Backrest Arc
            var backrest = new System.Windows.Shapes.Path
            {
                Data = Geometry.Parse("M 15 15 A 15 15 0 0 1 45 15"),
                Stroke = new SolidColorBrush(Color.FromRgb(30, 41, 59)),
                StrokeThickness = 2.5,
                StrokeStartLineCap = PenLineCap.Round,
                StrokeEndLineCap = PenLineCap.Round,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            chairContainer.Children.Add(backrest);

            if (chair.Rotation != 0)
            {
                chairContainer.RenderTransformOrigin = new Point(0.5, 0.5);
                chairContainer.RenderTransform = new RotateTransform(chair.Rotation);
            }

            Canvas.SetLeft(chairContainer, chair.X);
            Canvas.SetTop(chairContainer, chair.Y);
            canvas.Children.Add(chairContainer);
        }

        // 5. Render PC Workstations (exact geometry & screen facing desks)
        var monitorIconSource = FindResourceSafe<ImageSource>(resourceHost, "IconMonitor")
            ?? FindResourceSafe<ImageSource>(resourceHost, "IconScreen");
        var textPrimaryBrush = FindResourceSafe<Brush>(resourceHost, "AppTextPrimaryBrush") ?? Brushes.Black;

        foreach (var pc in result.Pcs)
        {
            var pcContainer = new Grid
            {
                Width = pc.Width,
                Height = pc.Height
            };

            if (monitorIconSource != null)
            {
                var img = new Image
                {
                    Source = monitorIconSource,
                    Width = 46,
                    Height = 46,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };
                pcContainer.Children.Add(img);
            }
            else
            {
                var screenBox = new Border
                {
                    Width = 48,
                    Height = 32,
                    Background = new SolidColorBrush(Color.FromRgb(15, 23, 42)),
                    BorderBrush = new SolidColorBrush(Color.FromRgb(2, 132, 199)),
                    BorderThickness = new Thickness(1.5),
                    CornerRadius = new CornerRadius(2),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };
                pcContainer.Children.Add(screenBox);
            }

            var lbl = new TextBlock
            {
                Text = pc.Name,
                FontSize = 8.5,
                FontWeight = FontWeights.Bold,
                Foreground = textPrimaryBrush,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Bottom,
                Margin = new Thickness(0, 0, 0, 2)
            };
            pcContainer.Children.Add(lbl);

            if (pc.Rotation != 0)
            {
                pcContainer.RenderTransformOrigin = new Point(0.5, 0.5);
                pcContainer.RenderTransform = new RotateTransform(pc.Rotation);
            }

            Canvas.SetLeft(pcContainer, pc.X);
            Canvas.SetTop(pcContainer, pc.Y);
            canvas.Children.Add(pcContainer);
        }
    }

    private static T? FindResourceSafe<T>(FrameworkElement host, string key) where T : class
    {
        return (host.TryFindResource(key) ?? Application.Current?.TryFindResource(key)) as T;
    }
}
