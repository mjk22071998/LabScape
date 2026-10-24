namespace LabScape.Core.Models;

public class DatabaseDevice
{
    public string DeviceId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string PrimaryIp { get; set; } = string.Empty;
    public string MacAddress { get; set; } = string.Empty;
    public bool IsAssigned { get; set; }
}
