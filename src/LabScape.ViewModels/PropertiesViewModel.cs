using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using LabScape.Core.Enums;
using LabScape.Core.Models;
using LabScape.ViewModels.Items;

namespace LabScape.ViewModels;

public partial class PropertiesViewModel : ObservableObject
{
    private readonly CanvasViewModel _canvasViewModel;

    [ObservableProperty]
    private ItemViewModel? _selectedItem;

    [ObservableProperty]
    private bool _hasSelection;

    [ObservableProperty]
    private bool _isDevice;

    [ObservableProperty]
    private bool _isFurniture;

    [ObservableProperty]
    private bool _isShape;

    [ObservableProperty]
    private bool _isGroup;

    [ObservableProperty]
    private bool _isRoundedShape;

    [ObservableProperty]
    private bool _isDonut;

    [ObservableProperty]
    private string _ipSearchQuery = string.Empty;

    // Available inventory devices for searchable IP configuration
    public ObservableCollection<DatabaseDevice> AvailableDatabaseDevices { get; } = new();
    public ObservableCollection<DatabaseDevice> FilteredDatabaseDevices { get; } = new();

    public PropertiesViewModel(CanvasViewModel canvasViewModel)
    {
        _canvasViewModel = canvasViewModel;
        _canvasViewModel.SelectedItems.CollectionChanged += (s, e) => UpdateSelection();
    }

    private void UpdateSelection()
    {
        if (_canvasViewModel.SelectedItems.Count == 1)
        {
            SelectedItem = _canvasViewModel.SelectedItems[0];
            HasSelection = true;
            IsDevice = SelectedItem is DeviceItemViewModel;
            IsFurniture = SelectedItem is FurnitureItemViewModel;
            IsShape = SelectedItem is ShapeItemViewModel;
            IsGroup = SelectedItem is GroupItemViewModel;

            if (SelectedItem is ShapeItemViewModel shapeVm)
            {
                IsRoundedShape = shapeVm.ShapeType == ShapeType.RoundedRectangle ||
                                 shapeVm.ShapeType == ShapeType.RoundedCornerTriangle;
                IsDonut = shapeVm.ShapeType == ShapeType.Donut;
            }
            else
            {
                IsRoundedShape = false;
                IsDonut = false;
            }
        }
        else
        {
            SelectedItem = null;
            HasSelection = false;
            IsDevice = false;
            IsFurniture = false;
            IsShape = false;
            IsGroup = false;
            IsRoundedShape = false;
            IsDonut = false;
        }
    }

    partial void OnIpSearchQueryChanged(string value)
    {
        FilterDatabaseDevices();
    }

    public void SetInventoryDevices(IEnumerable<DatabaseDevice> devices)
    {
        AvailableDatabaseDevices.Clear();
        foreach (var dev in devices)
        {
            AvailableDatabaseDevices.Add(dev);
        }
        FilterDatabaseDevices();
    }

    private void FilterDatabaseDevices()
    {
        FilteredDatabaseDevices.Clear();
        var q = IpSearchQuery?.Trim().ToLowerInvariant() ?? string.Empty;
        var query = string.IsNullOrEmpty(q)
            ? AvailableDatabaseDevices
            : AvailableDatabaseDevices.Where(d =>
                d.PrimaryIp.ToLowerInvariant().Contains(q) ||
                d.DisplayName.ToLowerInvariant().Contains(q) ||
                d.MacAddress.ToLowerInvariant().Contains(q));

        foreach (var dev in query)
        {
            FilteredDatabaseDevices.Add(dev);
        }
    }

    public void AssignIpToSelectedDevice(DatabaseDevice dbDevice)
    {
        if (SelectedItem is DeviceItemViewModel dev)
        {
            dev.DeviceId = dbDevice.DeviceId;
            dev.IpAddress = dbDevice.PrimaryIp;
            dev.MacAddress = dbDevice.MacAddress;
            if (string.IsNullOrWhiteSpace(dev.Name) || dev.Name.StartsWith("Desktop-") || dev.Name.StartsWith("Laptop-"))
            {
                dev.Name = dbDevice.DisplayName;
            }
            _canvasViewModel.RecalculateValidation();
        }
    }
}
