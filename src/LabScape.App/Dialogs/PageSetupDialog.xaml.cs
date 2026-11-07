using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using LabScape.Core.Enums;
using LabScape.ViewModels;

namespace LabScape.App.Dialogs;

public partial class PageSetupDialog : Window
{
    private readonly CanvasViewModel _canvas;
    private bool _isInternalUpdate;

    public PageSetupDialog(CanvasViewModel canvas)
    {
        InitializeComponent();
        Services.GpuRenderingService.ApplyGpuAcceleration(this);
        _canvas = canvas ?? throw new ArgumentNullException(nameof(canvas));

        LoadFromCanvas();
    }

    private void LoadFromCanvas()
    {
        _isInternalUpdate = true;
        try
        {
            // Populate Presets
            CmbPreset.ItemsSource = Enum.GetValues<PageSizePreset>()
                .Select(p => new PagePresetDisplayItem(p, PageDimensions.GetDisplayName(p)))
                .ToList();

            var currentPreset = _canvas.PageSize;
            var match = (CmbPreset.ItemsSource as System.Collections.Generic.IEnumerable<PagePresetDisplayItem>)
                ?.FirstOrDefault(p => p.Preset == currentPreset);
            CmbPreset.SelectedItem = match;

            TxtWidth.Text = _canvas.BasePageWidth.ToString("F0");
            TxtHeight.Text = _canvas.BasePageHeight.ToString("F0");

            if (_canvas.PageOrientation == PageOrientation.Portrait)
            {
                RbPortrait.IsChecked = true;
                UpdatePreview(isLandscape: false);
            }
            else
            {
                RbLandscape.IsChecked = true;
                UpdatePreview(isLandscape: true);
            }

            ChkAutoSize.IsChecked = _canvas.AutoSize;
            ChkShowGrid.IsChecked = _canvas.ShowGrid;
            ChkSnapToGrid.IsChecked = _canvas.SnapToGrid;
            ChkShowRulers.IsChecked = _canvas.ShowRulers;
            TxtGridSize.Text = _canvas.GridSize.ToString("F0");
        }
        finally
        {
            _isInternalUpdate = false;
        }
    }

    private void CmbPreset_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isInternalUpdate) return;
        if (CmbPreset.SelectedItem is PagePresetDisplayItem item)
        {
            if (item.Preset != PageSizePreset.Custom)
            {
                var orient = RbPortrait.IsChecked == true ? PageOrientation.Portrait : PageOrientation.Landscape;
                var (w, h) = PageDimensions.GetDimensions(item.Preset, orient);
                _isInternalUpdate = true;
                TxtWidth.Text = w.ToString("F0");
                TxtHeight.Text = h.ToString("F0");
                _isInternalUpdate = false;
            }
        }
    }

    private void Dimensions_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_isInternalUpdate) return;
        // If user manually edits dimensions, switch preset to Custom
        if (double.TryParse(TxtWidth.Text, out double w) && double.TryParse(TxtHeight.Text, out double h))
        {
            var detected = PageDimensions.DetectPreset(w, h, out var orient);
            _isInternalUpdate = true;
            var match = (CmbPreset.ItemsSource as System.Collections.Generic.IEnumerable<PagePresetDisplayItem>)
                ?.FirstOrDefault(p => p.Preset == detected);
            if (match != null) CmbPreset.SelectedItem = match;
            _isInternalUpdate = false;
        }
    }

    private void Orientation_Checked(object sender, RoutedEventArgs e)
    {
        if (_isInternalUpdate) return;
        bool isLandscape = RbLandscape.IsChecked == true;
        UpdatePreview(isLandscape);

        if (double.TryParse(TxtWidth.Text, out double w) && double.TryParse(TxtHeight.Text, out double h))
        {
            double maxDim = Math.Max(w, h);
            double minDim = Math.Min(w, h);
            _isInternalUpdate = true;
            if (isLandscape)
            {
                TxtWidth.Text = maxDim.ToString("F0");
                TxtHeight.Text = minDim.ToString("F0");
            }
            else
            {
                TxtWidth.Text = minDim.ToString("F0");
                TxtHeight.Text = maxDim.ToString("F0");
            }
            _isInternalUpdate = false;
        }
    }

    private void UpdatePreview(bool isLandscape)
    {
        if (SheetPreview == null) return;
        if (isLandscape)
        {
            SheetPreview.Width = 60;
            SheetPreview.Height = 42;
        }
        else
        {
            SheetPreview.Width = 42;
            SheetPreview.Height = 60;
        }
    }

    private void Apply()
    {
        var preset = (CmbPreset.SelectedItem as PagePresetDisplayItem)?.Preset ?? PageSizePreset.A3;
        var orient = RbPortrait.IsChecked == true ? PageOrientation.Portrait : PageOrientation.Landscape;

        double customW = 1587;
        double customH = 1123;
        if (double.TryParse(TxtWidth.Text, out double w) && w >= 200) customW = w;
        if (double.TryParse(TxtHeight.Text, out double h) && h >= 200) customH = h;

        _canvas.SetPageSize(preset, orient, customW, customH);
        _canvas.AutoSize = ChkAutoSize.IsChecked == true;
        _canvas.ShowGrid = ChkShowGrid.IsChecked == true;
        _canvas.SnapToGrid = ChkSnapToGrid.IsChecked == true;
        _canvas.ShowRulers = ChkShowRulers.IsChecked == true;

        if (double.TryParse(TxtGridSize.Text, out double g) && g >= 5 && g <= 100)
        {
            _canvas.GridSize = g;
        }

        _canvas.UpdatePageGrid();
    }

    private void Apply_Click(object sender, RoutedEventArgs e)
    {
        Apply();
    }

    private void Ok_Click(object sender, RoutedEventArgs e)
    {
        Apply();
        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}

public record PagePresetDisplayItem(PageSizePreset Preset, string DisplayName)
{
    public override string ToString() => DisplayName;
}
