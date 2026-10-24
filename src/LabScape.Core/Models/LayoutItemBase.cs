using System.Text.Json.Serialization;
using LabScape.Core.Enums;

namespace LabScape.Core.Models;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(DeviceItem), typeDiscriminator: "device")]
[JsonDerivedType(typeof(FurnitureItem), typeDiscriminator: "furniture")]
[JsonDerivedType(typeof(ShapeItem), typeDiscriminator: "shape")]
[JsonDerivedType(typeof(GroupItem), typeDiscriminator: "group")]
public abstract class LayoutItemBase
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [JsonIgnore]
    public abstract ItemType Type { get; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("x")]
    public double X { get; set; }

    [JsonPropertyName("y")]
    public double Y { get; set; }

    [JsonPropertyName("z")]
    public int Z { get; set; }

    [JsonPropertyName("width")]
    public double Width { get; set; } = 80;

    [JsonPropertyName("height")]
    public double Height { get; set; } = 80;

    [JsonPropertyName("rotation")]
    public double Rotation { get; set; } = 0.0;

    [JsonIgnore]
    public string? ParentGroupId { get; set; }

    [JsonIgnore]
    public bool IsSelected { get; set; }
}
