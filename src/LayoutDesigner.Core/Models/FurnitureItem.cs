using System.Text.Json.Serialization;
using LayoutDesigner.Core.Enums;

namespace LayoutDesigner.Core.Models;

public class FurnitureItem : LayoutItemBase
{
    [JsonIgnore]
    public override ItemType Type => ItemType.Furniture;

    [JsonPropertyName("furniture_type")]
    public string FurnitureType { get; set; } = "desk";

    [JsonPropertyName("fill")]
    public string Fill { get; set; } = "#F3F4F6";

    [JsonPropertyName("stroke")]
    public string Stroke { get; set; } = "#374151";

    public FurnitureItem()
    {
        Name = "New Furniture";
        Width = 120;
        Height = 70;
    }
}
