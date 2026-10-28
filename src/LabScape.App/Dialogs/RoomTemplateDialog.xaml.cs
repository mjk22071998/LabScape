using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using LabScape.Core.Enums;
using LabScape.ViewModels.Templates;

namespace LabScape.App.Dialogs;

public partial class RoomTemplateDialog : Window
{
    private bool _isInitializing = true;
    private bool _userCustomizedName = false;

    public SavedRoomTemplate? ResultTemplate { get; private set; }
    public bool InsertImmediately { get; private set; }
    public RoomTemplateOptions Options { get; private set; }

    public RoomTemplateDialog(RoomTemplateOptions? initialOptions = null)
    {
        InitializeComponent();

        Options = initialOptions ?? new RoomTemplateOptions();

        TxtTemplateName.TextChanged += (s, e) =>
        {
            if (!_isInitializing) _userCustomizedName = true;
        };

        // Initialize UI with initial options
        SliderPcCount.Value = Options.PcCount;
        TxtPcCount.Text = Options.PcCount.ToString();
        ChkIncludeDesks.IsChecked = Options.IncludeDesks;
        ChkIncludeChairs.IsChecked = Options.IncludeChairs;
        ChkGroupElements.IsChecked = Options.GroupElements;

        switch (Options.Sides)
        {
            case 1:
                Rb1Side.IsChecked = true;
                break;
            case 2:
                Rb2Sides.IsChecked = true;
                break;
            case 3:
                Rb3Sides.IsChecked = true;
                break;
            default:
                Rb4Sides.IsChecked = true;
                break;
        }

        _isInitializing = false;
        UpdatePanelsVisibility();
        UpdateLivePreview();
    }

    private void UpdatePanelsVisibility()
    {
        if (Panel3Sides == null || Panel2Sides == null || Panel1Side == null) return;

        Panel3Sides.Visibility = Rb3Sides.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
        Panel2Sides.Visibility = Rb2Sides.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
        Panel1Side.Visibility = Rb1Side.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
    }

    private void OptionChanged(object sender, RoutedEventArgs e)
    {
        if (_isInitializing) return;
        UpdatePanelsVisibility();
        UpdateLivePreview();
    }

    private void SliderPcCount_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_isInitializing) return;
        if (TxtPcCount != null)
        {
            TxtPcCount.Text = ((int)SliderPcCount.Value).ToString();
        }
        UpdateLivePreview();
    }

    private void TxtPcCount_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_isInitializing) return;
        if (int.TryParse(TxtPcCount.Text, out int val))
        {
            val = Math.Clamp(val, 1, 48);
            if ((int)SliderPcCount.Value != val)
            {
                SliderPcCount.Value = val;
            }
            UpdateLivePreview();
        }
    }

    public RoomTemplateOptions BuildCurrentOptions()
    {
        var opts = new RoomTemplateOptions
        {
            OriginX = 0,
            OriginY = 0,
            IncludeDesks = ChkIncludeDesks.IsChecked == true,
            IncludeChairs = ChkIncludeChairs.IsChecked == true,
            GroupElements = ChkGroupElements.IsChecked == true,
            PcCount = (int)SliderPcCount.Value
        };

        if (Rb1Side.IsChecked == true)
        {
            opts.Sides = 1;
            var item = CmbSingleSide.SelectedItem as ComboBoxItem;
            opts.SingleSide = (item?.Tag as string) switch
            {
                "South" => WallSide.South,
                "East" => WallSide.East,
                "West" => WallSide.West,
                _ => WallSide.North
            };
        }
        else if (Rb2Sides.IsChecked == true)
        {
            opts.Sides = 2;
            var item = CmbCorner.SelectedItem as ComboBoxItem;
            opts.CornerOption = (item?.Tag as string) ?? "NorthWest";
        }
        else if (Rb3Sides.IsChecked == true)
        {
            opts.Sides = 3;
            var item = CmbOpenSide.SelectedItem as ComboBoxItem;
            opts.OpenSide = (item?.Tag as string) switch
            {
                "North" => WallSide.North,
                "East" => WallSide.East,
                "West" => WallSide.West,
                _ => WallSide.South
            };
        }
        else
        {
            opts.Sides = 4;
        }

        return opts;
    }

    private void UpdateLivePreview()
    {
        if (_isInitializing || PreviewCanvas == null) return;

        var currentOpts = BuildCurrentOptions();
        var result = RoomTemplateGenerator.Generate(currentOpts);

        // Update default auto name if user hasn't explicitly customized it
        if (!_userCustomizedName && TxtTemplateName != null)
        {
            string sideDescriptor = currentOpts.Sides switch
            {
                1 => "Single Bench",
                2 => "Corner Lab",
                3 => "U-Shape Studio",
                _ => "Standard Lab"
            };
            TxtTemplateName.Text = $"{currentOpts.PcCount}-PC {sideDescriptor}";
        }

        // Summary readouts
        TxtOverlayDimensions.Text = $"{result.RoomWidth:0} × {result.RoomHeight:0} px";

        var dist = currentOpts.CalculatePcDistribution();
        var parts = new List<string>();
        foreach (var (side, count) in dist)
        {
            parts.Add($"{side}: {count}");
        }
        TxtSummaryDistribution.Text = parts.Count > 0 ? string.Join(" | ", parts) : "None";

        // Render EXACT visual representation into PreviewCanvas
        PreviewCanvas.Width = result.RoomWidth;
        PreviewCanvas.Height = result.RoomHeight;
        PreviewCanvas.Children.Clear();

        // 1. Subtle Inner Room Floor Guideline
        var floorGuide = new System.Windows.Shapes.Rectangle
        {
            Width = result.RoomWidth,
            Height = result.RoomHeight,
            Fill = new SolidColorBrush(Color.FromArgb(12, 2, 132, 199)),
            Stroke = new SolidColorBrush(Color.FromArgb(40, 2, 132, 199)),
            StrokeThickness = 1,
            StrokeDashArray = new DoubleCollection { 4, 4 }
        };
        Canvas.SetLeft(floorGuide, 0);
        Canvas.SetTop(floorGuide, 0);
        PreviewCanvas.Children.Add(floorGuide);

        // 2. Render Perimeter Walls (exact coordinates)
        var wallBrush = TryFindResource("AccentPrimaryBrush") as Brush ?? new SolidColorBrush(Color.FromRgb(2, 132, 199));
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
            PreviewCanvas.Children.Add(line);
        }

        // 3. Render Modular Desks (exact geometry)
        foreach (var desk in result.Desks)
        {
            var deskBorder = new Border
            {
                Width = desk.Width,
                Height = desk.Height,
                Background = new SolidColorBrush(Color.FromArgb(230, 226, 232, 240)),
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
            PreviewCanvas.Children.Add(deskBorder);
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
                Fill = new SolidColorBrush(Color.FromArgb(180, 203, 213, 225)),
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
            PreviewCanvas.Children.Add(chairContainer);
        }

        // 5. Render PC Workstations (exact geometry & screen facing desks)
        var monitorIconSource = TryFindResource("IconMonitor") as ImageSource ?? TryFindResource("IconScreen") as ImageSource;
        foreach (var pc in result.Pcs)
        {
            var pcContainer = new Grid
            {
                Width = pc.Width,
                Height = pc.Height
            };

            // Workstation monitor image / glyph
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

            // PC Name label
            var lbl = new TextBlock
            {
                Text = pc.Name,
                FontSize = 8.5,
                FontWeight = FontWeights.Bold,
                Foreground = TryFindResource("AppTextPrimaryBrush") as Brush ?? Brushes.Black,
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
            PreviewCanvas.Children.Add(pcContainer);
        }
    }

    private void BtnSaveOnly_Click(object sender, RoutedEventArgs e)
    {
        Options = BuildCurrentOptions();
        string name = string.IsNullOrWhiteSpace(TxtTemplateName.Text) ? "Custom Lab Template" : TxtTemplateName.Text.Trim();
        ResultTemplate = new SavedRoomTemplate
        {
            Name = name,
            Description = $"{Options.PcCount} PCs across {Options.Sides} boundary walls with modular layout.",
            Options = Options
        };
        InsertImmediately = false;
        DialogResult = true;
        Close();
    }

    private void BtnSaveAndInsert_Click(object sender, RoutedEventArgs e)
    {
        Options = BuildCurrentOptions();
        string name = string.IsNullOrWhiteSpace(TxtTemplateName.Text) ? "Custom Lab Template" : TxtTemplateName.Text.Trim();
        ResultTemplate = new SavedRoomTemplate
        {
            Name = name,
            Description = $"{Options.PcCount} PCs across {Options.Sides} boundary walls with modular layout.",
            Options = Options
        };
        InsertImmediately = true;
        DialogResult = true;
        Close();
    }
}
