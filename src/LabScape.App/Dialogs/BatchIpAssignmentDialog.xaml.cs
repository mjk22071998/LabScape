using System.Windows;
using System.Windows.Controls;
using LabScape.ViewModels;
using LabScape.ViewModels.Commands;
using LabScape.ViewModels.Items;

namespace LabScape.App.Dialogs;

public partial class BatchIpAssignmentDialog : Window
{
    private readonly CanvasViewModel _canvas;
    private readonly List<DeviceItemViewModel> _selectedDevices;
    private readonly List<DeviceItemViewModel> _allDevices;
    private readonly List<DeviceItemViewModel> _unassignedDevices;

    public class PreviewItem
    {
        public string CurrentName { get; set; } = string.Empty;
        public string NewName { get; set; } = string.Empty;
        public string AssignedIp { get; set; } = string.Empty;
        public string MacAddress { get; set; } = string.Empty;
        public DeviceItemViewModel Device { get; set; } = null!;
    }

    public BatchIpAssignmentDialog(CanvasViewModel canvas, IEnumerable<DeviceItemViewModel>? initialSelection = null)
    {
        InitializeComponent();
        _canvas = canvas;

        var allItems = GetAllDevices(_canvas.Items).ToList();
        _allDevices = allItems;
        _unassignedDevices = allItems.Where(d => string.IsNullOrWhiteSpace(d.IpAddress)).ToList();

        var initialList = initialSelection?.ToList() ?? new List<DeviceItemViewModel>();
        if (initialList.Count == 0 && _canvas.SelectedItems.Count > 0)
        {
            initialList = _canvas.SelectedItems.OfType<DeviceItemViewModel>().ToList();
        }
        _selectedDevices = initialList;

        RbSelected.Content = $"Selected workstations only ({_selectedDevices.Count} devices)";
        RbUnassigned.Content = $"All unassigned workstations on canvas ({_unassignedDevices.Count} devices)";
        RbAll.Content = $"All workstations on active canvas ({_allDevices.Count} devices)";

        if (_selectedDevices.Count > 0)
        {
            RbSelected.IsChecked = true;
        }
        else if (_unassignedDevices.Count > 0)
        {
            RbUnassigned.IsChecked = true;
        }
        else
        {
            RbAll.IsChecked = true;
        }

        TxtStartIp.Text = _canvas.SuggestNextAvailableIp();
        UpdatePreview();
    }

    private IEnumerable<DeviceItemViewModel> GetAllDevices(IEnumerable<ItemViewModel> list)
    {
        foreach (var itm in list)
        {
            if (itm is DeviceItemViewModel dev)
            {
                yield return dev;
            }
            else if (itm is GroupItemViewModel grp)
            {
                foreach (var childDev in GetAllDevices(grp.Children))
                {
                    yield return childDev;
                }
            }
        }
    }

    private List<DeviceItemViewModel> GetCurrentTargetDevices()
    {
        List<DeviceItemViewModel> source = RbSelected.IsChecked == true
            ? _selectedDevices
            : (RbUnassigned.IsChecked == true ? _unassignedDevices : _allDevices);

        return source
            .OrderBy(d => Math.Round(d.Y / 30.0))
            .ThenBy(d => d.X)
            .ToList();
    }

    private void Scope_Changed(object sender, RoutedEventArgs e)
    {
        if (IsLoaded) UpdatePreview();
    }

    private void Parameters_Changed(object sender, RoutedEventArgs e)
    {
        if (IsLoaded) UpdatePreview();
    }

    private void UpdatePreview()
    {
        var targets = GetCurrentTargetDevices();
        TxtPreviewCount.Text = $"{targets.Count} devices targetted";
        BtnAssign.IsEnabled = targets.Count > 0;

        string startIp = string.IsNullOrWhiteSpace(TxtStartIp.Text) ? "192.168.1.101" : TxtStartIp.Text.Trim();
        if (!CanvasViewModel.IsValidIPv4(startIp))
        {
            startIp = "192.168.1.101";
        }

        int step = int.TryParse(TxtStep.Text, out int s) && s > 0 ? s : 1;
        bool rename = ChkRename.IsChecked == true;
        bool genMac = ChkGenerateMac.IsChecked == true;
        string prefix = TxtPrefix.Text ?? "PC-";

        var previewList = new List<PreviewItem>();
        var r = new Random(12345);

        for (int i = 0; i < targets.Count; i++)
        {
            var dev = targets[i];
            PropertiesViewModel.TryIncrementIp(startIp, i * step, out string assignedIp);

            string mac = !string.IsNullOrWhiteSpace(dev.MacAddress)
                ? dev.MacAddress
                : (genMac ? $"02:{r.Next(256):X2}:{r.Next(256):X2}:{r.Next(256):X2}:{r.Next(256):X2}:{r.Next(256):X2}" : "(None)");

            string newName = rename ? $"{prefix.Trim()}{i + 1:D2}" : dev.Name;

            previewList.Add(new PreviewItem
            {
                CurrentName = dev.Name,
                NewName = newName,
                AssignedIp = assignedIp,
                MacAddress = mac,
                Device = dev
            });
        }

        LvPreview.ItemsSource = previewList;
    }

    private void Assign_Click(object sender, RoutedEventArgs e)
    {
        var preview = LvPreview.ItemsSource as List<PreviewItem>;
        if (preview == null || preview.Count == 0) return;

        var states = new List<BatchAssignDeviceNetworkCommand.DeviceNetworkState>();
        var r = new Random();

        foreach (var item in preview)
        {
            string? mac = item.MacAddress != "(None)"
                ? item.MacAddress
                : item.Device.MacAddress;

            if (string.IsNullOrWhiteSpace(mac) && ChkGenerateMac.IsChecked == true)
            {
                mac = $"02:{r.Next(256):X2}:{r.Next(256):X2}:{r.Next(256):X2}:{r.Next(256):X2}:{r.Next(256):X2}";
            }

            states.Add(new BatchAssignDeviceNetworkCommand.DeviceNetworkState(
                item.Device,
                OldIp: item.Device.IpAddress, NewIp: item.AssignedIp,
                OldMac: item.Device.MacAddress, NewMac: mac,
                OldDeviceId: item.Device.DeviceId, NewDeviceId: item.Device.DeviceId,
                OldName: item.Device.Name, NewName: item.NewName,
                OldIsManual: item.Device.IsManualIp, NewIsManual: true));
        }

        var cmd = new BatchAssignDeviceNetworkCommand(states, onChanged: () => {
            _canvas.RecalculateValidation();
        });

        _canvas.UndoRedoService.ExecuteCommand(cmd);
        _canvas.RecalculateValidation();

        DialogResult = true;
        Close();
    }

    private void Clear_Click(object sender, RoutedEventArgs e)
    {
        var targets = GetCurrentTargetDevices();
        if (targets.Count == 0) return;

        var states = targets.Select(dev => new BatchAssignDeviceNetworkCommand.DeviceNetworkState(
            dev,
            OldIp: dev.IpAddress, NewIp: null,
            OldMac: dev.MacAddress, NewMac: dev.MacAddress,
            OldDeviceId: dev.DeviceId, NewDeviceId: null,
            OldName: dev.Name, NewName: dev.Name,
            OldIsManual: dev.IsManualIp, NewIsManual: true)).ToList();

        var cmd = new BatchAssignDeviceNetworkCommand(states, onChanged: () => {
            _canvas.RecalculateValidation();
        });

        _canvas.UndoRedoService.ExecuteCommand(cmd);
        _canvas.RecalculateValidation();

        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
