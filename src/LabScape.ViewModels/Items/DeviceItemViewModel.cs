using CommunityToolkit.Mvvm.ComponentModel;
using LabScape.Core.Enums;
using LabScape.Core.Models;

namespace LabScape.ViewModels.Items;

public partial class DeviceItemViewModel : ItemViewModel, IStylableItemViewModel
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

    [ObservableProperty]
    private bool _isManualIp = true;

    [ObservableProperty]
    private string? _fill;

    [ObservableProperty]
    private string? _stroke;

    [ObservableProperty]
    private double _strokeThickness = 2.0;

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
        _isManualIp = model.IsManualIp;
        _fill = model.Fill;
        _stroke = model.Stroke;
        _strokeThickness = model.StrokeThickness ?? 2.0;
    }

    partial void OnDeviceIdChanged(string? value) => DeviceModel.DeviceId = value;
    partial void OnIpAddressChanged(string? value) => DeviceModel.IpAddress = value;
    partial void OnMacAddressChanged(string? value) => DeviceModel.MacAddress = value;
    partial void OnDeviceTypeChanged(string value) => DeviceModel.DeviceType = value;
    partial void OnValidationStatusChanged(ValidationStatus value) => DeviceModel.ValidationStatus = value;
    partial void OnIsManualIpChanged(bool value) => DeviceModel.IsManualIp = value;
    partial void OnFillChanged(string? value) => DeviceModel.Fill = value;
    partial void OnStrokeChanged(string? value) => DeviceModel.Stroke = value;
    partial void OnStrokeThicknessChanged(double value) => DeviceModel.StrokeThickness = value;
}
