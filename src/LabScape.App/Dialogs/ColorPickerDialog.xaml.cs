using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace LabScape.App.Dialogs;

public partial class ColorPickerDialog : Window
{
    private double _hue = 0;          // 0 - 360
    private double _saturation = 1;   // 0 - 1
    private double _value = 1;        // 0 - 1
    private bool _isUpdatingInternally = false;
    private bool _isDraggingCanvas = false;

    public string SelectedColorHex { get; private set; } = "#FF0000";

    public static readonly string[] DefaultPalette = new[]
    {
        "#FFFFFF", "#F8FAFC", "#E2E8F0", "#94A3B8", "#475569", "#0F172A",
        "#EF4444", "#F97316", "#F59E0B", "#EAB308", "#84CC16", "#10B981",
        "#06B6D4", "#3B82F6", "#6366F1", "#8B5CF6", "#EC4899", "#F43F5E",
        "#FEE2E2", "#FEF3C7", "#DCFCE7", "#CFFAFE", "#E0E7FF", "#FCE7F3"
    };

    public ColorPickerDialog(string? initialHex = null)
    {
        InitializeComponent();

        BuildQuickSwatches();

        string startColor = !string.IsNullOrWhiteSpace(initialHex) ? initialHex : "#3B82F6";
        SelectedColorHex = NormalizeHex(startColor);

        // Set original color preview
        try
        {
            var oldColor = (Color)ColorConverter.ConvertFromString(SelectedColorHex);
            OldColorBox.Background = new SolidColorBrush(oldColor);
        }
        catch
        {
            OldColorBox.Background = Brushes.White;
        }

        SetFromHex(SelectedColorHex);
    }

    private void BuildQuickSwatches()
    {
        QuickSwatchesPanel.Children.Clear();
        foreach (var hex in DefaultPalette)
        {
            try
            {
                var color = (Color)ColorConverter.ConvertFromString(hex);
                var btn = new Button
                {
                    Width = 16,
                    Height = 16,
                    Margin = new Thickness(1.5),
                    Padding = new Thickness(0),
                    Cursor = Cursors.Hand,
                    ToolTip = hex,
                    Background = new SolidColorBrush(color),
                    BorderBrush = new SolidColorBrush(Color.FromArgb(80, 0, 0, 0)),
                    BorderThickness = new Thickness(1),
                    Tag = hex
                };

                // Rounded visual template
                var template = new ControlTemplate(typeof(Button));
                var borderFactory = new FrameworkElementFactory(typeof(Border));
                borderFactory.SetValue(Border.CornerRadiusProperty, new CornerRadius(3));
                borderFactory.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(Button.BackgroundProperty));
                borderFactory.SetValue(Border.BorderBrushProperty, new TemplateBindingExtension(Button.BorderBrushProperty));
                borderFactory.SetValue(Border.BorderThicknessProperty, new TemplateBindingExtension(Button.BorderThicknessProperty));
                template.VisualTree = borderFactory;
                btn.Template = template;

                btn.Click += (s, e) =>
                {
                    if (s is Button b && b.Tag is string clickedHex)
                    {
                        SetFromHex(clickedHex);
                    }
                };

                QuickSwatchesPanel.Children.Add(btn);
            }
            catch
            {
                // Ignore invalid palette hex
            }
        }
    }

    private void SetFromHex(string hex)
    {
        try
        {
            string cleanHex = NormalizeHex(hex);
            var color = (Color)ColorConverter.ConvertFromString(cleanHex);
            var (h, s, v) = RgbToHsv(color.R, color.G, color.B);
            _hue = h;
            _saturation = s;
            _value = v;

            UpdateUi();
        }
        catch
        {
            // Keep current if invalid
        }
    }

    private void UpdateUi()
    {
        _isUpdatingInternally = true;
        try
        {
            // 1. Pure hue color for 2D base canvas
            var pureHueColor = HsvToRgb(_hue, 1.0, 1.0);
            HueBaseRect.Fill = new SolidColorBrush(pureHueColor);

            // 2. Position crosshair on 2D canvas
            double thumbX = _saturation * SpectrumCanvas.Width;
            double thumbY = (1.0 - _value) * SpectrumCanvas.Height;

            thumbX = Math.Clamp(thumbX, 0, SpectrumCanvas.Width);
            thumbY = Math.Clamp(thumbY, 0, SpectrumCanvas.Height);

            Canvas.SetLeft(CrosshairThumb, thumbX - (CrosshairThumb.Width / 2.0));
            Canvas.SetTop(CrosshairThumb, thumbY - (CrosshairThumb.Height / 2.0));

            // 3. Final active color
            var activeColor = HsvToRgb(_hue, _saturation, _value);
            SelectedColorHex = $"#{activeColor.R:X2}{activeColor.G:X2}{activeColor.B:X2}";

            // 4. Update preview and inputs
            NewColorBox.Background = new SolidColorBrush(activeColor);
            HexTextBox.Text = SelectedColorHex;

            HueSlider.Value = _hue;

            RedSlider.Value = activeColor.R;
            GreenSlider.Value = activeColor.G;
            BlueSlider.Value = activeColor.B;

            RedTextBox.Text = activeColor.R.ToString();
            GreenTextBox.Text = activeColor.G.ToString();
            BlueTextBox.Text = activeColor.B.ToString();
        }
        finally
        {
            _isUpdatingInternally = false;
        }
    }

    private void SpectrumCanvas_MouseDown(object sender, MouseButtonEventArgs e)
    {
        _isDraggingCanvas = true;
        SpectrumCanvas.CaptureMouse();
        UpdateCanvasHsv(e.GetPosition(SpectrumCanvas));
    }

    private void SpectrumCanvas_MouseMove(object sender, MouseEventArgs e)
    {
        if (_isDraggingCanvas)
        {
            UpdateCanvasHsv(e.GetPosition(SpectrumCanvas));
        }
    }

    private void SpectrumCanvas_MouseUp(object sender, MouseButtonEventArgs e)
    {
        if (_isDraggingCanvas)
        {
            _isDraggingCanvas = false;
            SpectrumCanvas.ReleaseMouseCapture();
        }
    }

    private void UpdateCanvasHsv(Point pos)
    {
        double x = Math.Clamp(pos.X, 0, SpectrumCanvas.Width);
        double y = Math.Clamp(pos.Y, 0, SpectrumCanvas.Height);

        _saturation = x / SpectrumCanvas.Width;
        _value = 1.0 - (y / SpectrumCanvas.Height);

        UpdateUi();
    }

    private void HueSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_isUpdatingInternally) return;

        _hue = HueSlider.Value;
        UpdateUi();
    }

    private void RgbSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_isUpdatingInternally) return;

        byte r = (byte)Math.Clamp(Math.Round(RedSlider.Value), 0, 255);
        byte g = (byte)Math.Clamp(Math.Round(GreenSlider.Value), 0, 255);
        byte b = (byte)Math.Clamp(Math.Round(BlueSlider.Value), 0, 255);

        var (h, s, v) = RgbToHsv(r, g, b);
        _hue = h;
        _saturation = s;
        _value = v;

        UpdateUi();
    }

    private void RgbTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_isUpdatingInternally) return;

        if (byte.TryParse(RedTextBox.Text, out byte r) &&
            byte.TryParse(GreenTextBox.Text, out byte g) &&
            byte.TryParse(BlueTextBox.Text, out byte b))
        {
            var (h, s, v) = RgbToHsv(r, g, b);
            _hue = h;
            _saturation = s;
            _value = v;
            UpdateUi();
        }
    }

    private void HexTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_isUpdatingInternally) return;
        string text = HexTextBox.Text.Trim();
        if (text.StartsWith("#") && (text.Length == 7 || text.Length == 9))
        {
            SetFromHex(text);
        }
    }

    private void HexTextBox_LostFocus(object sender, RoutedEventArgs e)
    {
        SetFromHex(HexTextBox.Text);
    }

    private void HexTextBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            SetFromHex(HexTextBox.Text);
            HexTextBox.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
        }
    }

    private void ApplyButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private static string NormalizeHex(string hex)
    {
        string clean = hex.Trim();
        if (!clean.StartsWith("#"))
            clean = "#" + clean;
        return clean;
    }

    private static (double h, double s, double v) RgbToHsv(byte r, byte g, byte b)
    {
        double rd = r / 255.0;
        double gd = g / 255.0;
        double bd = b / 255.0;

        double max = Math.Max(rd, Math.Max(gd, bd));
        double min = Math.Min(rd, Math.Min(gd, bd));
        double delta = max - min;

        double h = 0;
        if (delta > 0)
        {
            if (Math.Abs(max - rd) < 0.0001)
                h = 60 * (((gd - bd) / delta) % 6);
            else if (Math.Abs(max - gd) < 0.0001)
                h = 60 * (((bd - rd) / delta) + 2);
            else
                h = 60 * (((rd - gd) / delta) + 4);

            if (h < 0) h += 360;
        }

        double s = max == 0 ? 0 : delta / max;
        double v = max;

        return (h, s, v);
    }

    private static Color HsvToRgb(double h, double s, double v)
    {
        h = Math.Clamp(h, 0, 360);
        s = Math.Clamp(s, 0, 1);
        v = Math.Clamp(v, 0, 1);

        double c = v * s;
        double x = c * (1 - Math.Abs((h / 60) % 2 - 1));
        double m = v - c;

        double r = 0, g = 0, b = 0;
        if (h < 60) { r = c; g = x; }
        else if (h < 120) { r = x; g = c; }
        else if (h < 180) { g = c; b = x; }
        else if (h < 240) { g = x; b = c; }
        else if (h < 300) { r = x; b = c; }
        else { r = c; b = x; }

        return Color.FromRgb(
            (byte)Math.Clamp(Math.Round((r + m) * 255), 0, 255),
            (byte)Math.Clamp(Math.Round((g + m) * 255), 0, 255),
            (byte)Math.Clamp(Math.Round((b + m) * 255), 0, 255));
    }
}
