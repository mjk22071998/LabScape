using System.Text.Json.Serialization;
using LabScape.Core.Enums;

namespace LabScape.Core.Models;

public class TextItem : LayoutItemBase
{
    [JsonIgnore]
    public override ItemType Type => ItemType.Text;

    [JsonPropertyName("text")]
    public string Text { get; set; } = "Text";

    [JsonPropertyName("font_size")]
    public double FontSize { get; set; } = 16.0;

    [JsonPropertyName("font_family")]
    public string FontFamily { get; set; } = "Segoe UI";

    [JsonPropertyName("font_weight")]
    public string FontWeight { get; set; } = "Normal"; // "Normal" | "Bold"

    [JsonPropertyName("font_style")]
    public string FontStyle { get; set; } = "Normal"; // "Normal" | "Italic"

    [JsonPropertyName("text_alignment")]
    public string TextAlignment { get; set; } = "Left"; // "Left" | "Center" | "Right"

    [JsonPropertyName("text_color")]
    public string TextColor { get; set; } = "#111827";

    [JsonPropertyName("background")]
    public string Background { get; set; } = "Transparent";

    [JsonPropertyName("border_color")]
    public string BorderColor { get; set; } = "Transparent";

    [JsonPropertyName("border_thickness")]
    public double BorderThickness { get; set; } = 0.0;

    public TextItem()
    {
        Name = "Text";
        Width = 140;
        Height = 40;
    }
}
