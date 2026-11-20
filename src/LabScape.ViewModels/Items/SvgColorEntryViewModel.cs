using CommunityToolkit.Mvvm.ComponentModel;

namespace LabScape.ViewModels.Items;

/// <summary>
/// Represents a single color entry extracted from an SVG vector stencil,
/// displaying the original color, current customized replacement, label, and customization state.
/// </summary>
public partial class SvgColorEntryViewModel : ObservableObject
{
    [ObservableProperty]
    private string _originalColor;

    [ObservableProperty]
    private string _currentColor;

    [ObservableProperty]
    private string _label;

    public bool IsCustomized => !string.Equals(OriginalColor, CurrentColor, StringComparison.OrdinalIgnoreCase);

    public SvgColorEntryViewModel(string originalColor, string currentColor, string label = "")
    {
        _originalColor = originalColor;
        _currentColor = currentColor;
        _label = label;
    }

    partial void OnCurrentColorChanged(string value)
    {
        OnPropertyChanged(nameof(IsCustomized));
    }
}
