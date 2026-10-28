using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using LabScape.App.Helpers;
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
        RoomTemplateRenderHelper.PopulateCanvas(PreviewCanvas, result, this);
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
