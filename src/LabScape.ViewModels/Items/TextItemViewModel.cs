using CommunityToolkit.Mvvm.ComponentModel;
using LabScape.Core.Models;

namespace LabScape.ViewModels.Items;

public partial class TextItemViewModel : ItemViewModel
{
    public TextItem TextModel => (TextItem)Model;

    [ObservableProperty]
    private string _text;

    [ObservableProperty]
    private double _fontSize;

    [ObservableProperty]
    private string _fontFamily;

    [ObservableProperty]
    private string _fontWeight; // "Normal" | "Bold"

    [ObservableProperty]
    private string _fontStyle; // "Normal" | "Italic"

    [ObservableProperty]
    private string _textAlignment; // "Left" | "Center" | "Right"

    [ObservableProperty]
    private string _textColor;

    [ObservableProperty]
    private string _background;

    [ObservableProperty]
    private string _borderColor;

    [ObservableProperty]
    private double _borderThickness;

    [ObservableProperty]
    private bool _isEditing;

    public TextItemViewModel(TextItem model) : base(model)
    {
        _text = model.Text;
        _fontSize = model.FontSize;
        _fontFamily = model.FontFamily;
        _fontWeight = model.FontWeight;
        _fontStyle = model.FontStyle;
        _textAlignment = model.TextAlignment;
        _textColor = model.TextColor;
        _background = model.Background;
        _borderColor = model.BorderColor;
        _borderThickness = model.BorderThickness;
    }

    partial void OnTextChanged(string value) => TextModel.Text = value;
    partial void OnFontSizeChanged(double value) => TextModel.FontSize = value;
    partial void OnFontFamilyChanged(string value) => TextModel.FontFamily = value;
    partial void OnFontWeightChanged(string value) => TextModel.FontWeight = value;
    partial void OnFontStyleChanged(string value) => TextModel.FontStyle = value;
    partial void OnTextAlignmentChanged(string value) => TextModel.TextAlignment = value;
    partial void OnTextColorChanged(string value) => TextModel.TextColor = value;
    partial void OnBackgroundChanged(string value) => TextModel.Background = value;
    partial void OnBorderColorChanged(string value) => TextModel.BorderColor = value;
    partial void OnBorderThicknessChanged(double value) => TextModel.BorderThickness = value;

    public void BeginEdit() => IsEditing = true;
    public void EndEdit() => IsEditing = false;
}
