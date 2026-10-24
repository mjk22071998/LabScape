using CommunityToolkit.Mvvm.ComponentModel;
using LabScape.Core.Enums;
using LabScape.Core.Models;

namespace LabScape.ViewModels.Items;

public partial class DeviceItemViewModel : ItemViewModel
{
    public DeviceItem DeviceModel => (DeviceItem)Model;

    [ObservableProperty]
    private string? _deviceId;

    [ObservableProperty]
    private string? _ipAddress;

    [ObservableProperty]
    private string? _macAddress;

    [ObservableProperty]
    private string _deviceType;

    [ObservableProperty]
    private ValidationStatus _validationStatus;

    /// <summary>
    /// Requirement: ONLY device items show their name below the icon on the canvas.
    /// </summary>
    public override bool ShowCanvasLabel => true;

    public DeviceItemViewModel(DeviceItem model) : base(model)
    {
        _deviceId = model.DeviceId;
        _ipAddress = model.IpAddress;
        _macAddress = model.MacAddress;
        _deviceType = model.DeviceType;
        _validationStatus = model.ValidationStatus;
    }

    partial void OnDeviceIdChanged(string? value) => DeviceModel.DeviceId = value;
    partial void OnIpAddressChanged(string? value) => DeviceModel.IpAddress = value;
    partial void OnMacAddressChanged(string? value) => DeviceModel.MacAddress = value;
    partial void OnDeviceTypeChanged(string value) => DeviceModel.DeviceType = value;
    partial void OnValidationStatusChanged(ValidationStatus value) => DeviceModel.ValidationStatus = value;
}
