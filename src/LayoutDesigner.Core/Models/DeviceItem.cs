using System.Text.Json.Serialization;
using LayoutDesigner.Core.Enums;

namespace LayoutDesigner.Core.Models;

public class DeviceItem : LayoutItemBase
{
    [JsonIgnore]
    public override ItemType Type => ItemType.Device;

    [JsonPropertyName("device_id")]
    public string? DeviceId { get; set; }

    [JsonPropertyName("ip_address")]
    public string? IpAddress { get; set; }

    [JsonPropertyName("mac_address")]
    public string? MacAddress { get; set; }

    [JsonPropertyName("device_type")]
    public string DeviceType { get; set; } = "desktop_pc";

    [JsonPropertyName("validation_status")]
    public ValidationStatus ValidationStatus { get; set; } = ValidationStatus.Unassigned;

    public DeviceItem()
    {
        Name = "New Device";
        Width = 80;
        Height = 80;
    }
}
