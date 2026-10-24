using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LabScape.Core.Enums;
using LabScape.Core.Models;
using LabScape.ViewModels.Items;

namespace LabScape.ViewModels;

public partial class PropertiesViewModel : ObservableObject
{
    private CanvasViewModel? _canvasViewModel;
    private NotifyCollectionChangedEventHandler? _selectionHandler;
    private bool _isInternalPropertyUpdate;

    [RelayCommand]
    public void MoveToFront() => _canvasViewModel?.MoveToFront();

    [RelayCommand]
    public void MoveForward() => _canvasViewModel?.MoveForward();

    [RelayCommand]
    public void SendBackward() => _canvasViewModel?.SendBackward();

    [RelayCommand]
    public void SendToBack() => _canvasViewModel?.SendToBack();

    [ObservableProperty]
    private ItemViewModel? _selectedItem;

    [ObservableProperty]
    private bool _hasSelection;

    [ObservableProperty]
    private bool _isMultiSelection;

    [ObservableProperty]
    private bool _isSingleSelection;

    [ObservableProperty]
    private int _selectedCount;

    [ObservableProperty]
    private bool _canEditCoordinates;

    [ObservableProperty]
    private string _headerTitle = string.Empty;

    [ObservableProperty]
    private string _headerSubtitle = string.Empty;

    [ObservableProperty]
    private string _displayName = string.Empty;

    [ObservableProperty]
    private string _displayX = "0";

    [ObservableProperty]
    private string _displayY = "0";

    [ObservableProperty]
    private string _coordinatesToolTip = "Item coordinates";

    [ObservableProperty]
    private string _commonWidth = "0";

    [ObservableProperty]
    private string _commonHeight = "0";

    [ObservableProperty]
    private string _commonZ = "0";

    [ObservableProperty]
    private string _commonRotation = "0";

    [ObservableProperty]
    private double _commonStrokeThickness = 2.0;

    [ObservableProperty]
    private double _commonCornerRadius = 10.0;

    [ObservableProperty]
    private double _commonInnerRadius = 25.0;

    [ObservableProperty]
    private bool _isDevice;

    [ObservableProperty]
    private bool _isSingleDevice;

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

    public PropertiesViewModel(CanvasViewModel? canvasViewModel = null)
    {
        if (canvasViewModel != null)
        {
            AttachCanvas(canvasViewModel);
        }
    }

    public void AttachCanvas(CanvasViewModel canvas)
    {
        if (_canvasViewModel != null && _selectionHandler != null)
        {
            _canvasViewModel.SelectedItems.CollectionChanged -= _selectionHandler;
        }

        _canvasViewModel = canvas;
        _selectionHandler = (s, e) => UpdateSelection();
        _canvasViewModel.SelectedItems.CollectionChanged += _selectionHandler;
        UpdateSelection();
    }

    private void UpdateSelection()
    {
        if (SelectedItem != null)
        {
            SelectedItem.PropertyChanged -= OnSelectedItemPropertyChanged;
        }

        if (_canvasViewModel == null || _canvasViewModel.SelectedItems.Count == 0)
        {
            SelectedItem = null;
            HasSelection = false;
            IsMultiSelection = false;
            IsSingleSelection = false;
            SelectedCount = 0;
            CanEditCoordinates = false;
            HeaderTitle = string.Empty;
            HeaderSubtitle = string.Empty;
            DisplayName = string.Empty;
            IsDevice = false;
            IsSingleDevice = false;
            IsFurniture = false;
            IsShape = false;
            IsGroup = false;
            IsRoundedShape = false;
            IsDonut = false;
            DisplayX = "0";
            DisplayY = "0";
            CommonWidth = "0";
            CommonHeight = "0";
            CommonZ = "0";
            CommonRotation = "0";
            CoordinatesToolTip = "No item selected";
            return;
        }

        HasSelection = true;
        SelectedCount = _canvasViewModel.SelectedItems.Count;
        IsMultiSelection = SelectedCount > 1;
        IsSingleSelection = SelectedCount == 1;
        CanEditCoordinates = !IsMultiSelection;

        SelectedItem = _canvasViewModel.SelectedItems[0];
        SelectedItem.PropertyChanged += OnSelectedItemPropertyChanged;

        if (IsSingleSelection)
        {
            HeaderTitle = $"ITEM TYPE: {SelectedItem.ItemType.ToString().ToUpperInvariant()}";
            HeaderSubtitle = SelectedItem.Name;
            DisplayName = SelectedItem.Name;
            CoordinatesToolTip = "Canvas coordinate position (px)";

            IsDevice = SelectedItem is DeviceItemViewModel;
            IsSingleDevice = IsDevice;
            IsFurniture = SelectedItem is FurnitureItemViewModel;
            IsShape = SelectedItem is ShapeItemViewModel;
            IsGroup = SelectedItem is GroupItemViewModel;

            if (SelectedItem is ShapeItemViewModel shapeVm)
            {
                IsRoundedShape = shapeVm.ShapeType == ShapeType.RoundedRectangle ||
                                 shapeVm.ShapeType == ShapeType.RoundedCornerTriangle;
                IsDonut = shapeVm.ShapeType == ShapeType.Donut;
                CommonCornerRadius = shapeVm.CornerRadius;
                CommonInnerRadius = shapeVm.InnerRadius;
                CommonStrokeThickness = shapeVm.StrokeThickness;
            }
            else
            {
                IsRoundedShape = false;
                IsDonut = false;
            }
        }
        else
        {
            HeaderTitle = "MULTIPLE ITEMS SELECTED";
            HeaderSubtitle = $"{SelectedCount} items selected (Common properties editable below)";
            DisplayName = "(Multiple Items Selected)";
            CoordinatesToolTip = "X/Y coordinate editing is disabled when multiple items are selected. Move items directly on canvas or use alignment tools.";

            IsDevice = false;
            IsSingleDevice = false;
            IsFurniture = false;
            IsGroup = false;
            IsShape = _canvasViewModel.SelectedItems.Any(i => i is ShapeItemViewModel);
            IsRoundedShape = _canvasViewModel.SelectedItems.All(i => i is ShapeItemViewModel s && (s.ShapeType == ShapeType.RoundedRectangle || s.ShapeType == ShapeType.RoundedCornerTriangle));
            IsDonut = _canvasViewModel.SelectedItems.All(i => i is ShapeItemViewModel s && s.ShapeType == ShapeType.Donut);

            var firstShape = _canvasViewModel.SelectedItems.OfType<ShapeItemViewModel>().FirstOrDefault();
            if (firstShape != null)
            {
                CommonStrokeThickness = firstShape.StrokeThickness;
                CommonCornerRadius = firstShape.CornerRadius;
                CommonInnerRadius = firstShape.InnerRadius;
            }
        }

        SyncFieldsFromSelection();
    }

    private void OnSelectedItemPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (_isInternalPropertyUpdate) return;

        if (e.PropertyName is nameof(ItemViewModel.X) or nameof(ItemViewModel.Y) or
            nameof(ItemViewModel.Width) or nameof(ItemViewModel.Height) or
            nameof(ItemViewModel.Z) or nameof(ItemViewModel.Rotation) or
            nameof(ItemViewModel.Name))
        {
            SyncFieldsFromSelection();
        }
    }

    private void SyncFieldsFromSelection()
    {
        if (_canvasViewModel == null || _canvasViewModel.SelectedItems.Count == 0) return;

        _isInternalPropertyUpdate = true;
        try
        {
            if (IsSingleSelection && SelectedItem != null)
            {
                DisplayName = SelectedItem.Name;
                HeaderSubtitle = SelectedItem.Name;
                DisplayX = SelectedItem.X.ToString("F1");
                DisplayY = SelectedItem.Y.ToString("F1");
                CommonWidth = SelectedItem.Width.ToString("F1");
                CommonHeight = SelectedItem.Height.ToString("F1");
                CommonZ = SelectedItem.Z.ToString();
                CommonRotation = SelectedItem.Rotation.ToString("F1");
            }
            else
            {
                // Multi-selection: X and Y are non-editable
                DisplayX = "(Multiple)";
                DisplayY = "(Multiple)";

                var first = _canvasViewModel.SelectedItems[0];

                CommonWidth = _canvasViewModel.SelectedItems.All(i => Math.Abs(i.Width - first.Width) < 0.1)
                    ? first.Width.ToString("F1")
                    : _canvasViewModel.SelectedItems.Average(i => i.Width).ToString("F1");

                CommonHeight = _canvasViewModel.SelectedItems.All(i => Math.Abs(i.Height - first.Height) < 0.1)
                    ? first.Height.ToString("F1")
                    : _canvasViewModel.SelectedItems.Average(i => i.Height).ToString("F1");

                CommonZ = _canvasViewModel.SelectedItems.All(i => i.Z == first.Z)
                    ? first.Z.ToString()
                    : first.Z.ToString();

                CommonRotation = _canvasViewModel.SelectedItems.All(i => Math.Abs(i.Rotation - first.Rotation) < 0.1)
                    ? first.Rotation.ToString("F1")
                    : "0";
            }
        }
        finally
        {
            _isInternalPropertyUpdate = false;
        }
    }

    partial void OnDisplayNameChanged(string value)
    {
        if (_isInternalPropertyUpdate || !IsSingleSelection || SelectedItem == null) return;
        if (!string.IsNullOrWhiteSpace(value) && value != "(Multiple Items Selected)")
        {
            SelectedItem.Name = value;
            HeaderSubtitle = value;
        }
    }

    partial void OnDisplayXChanged(string value)
    {
        if (_isInternalPropertyUpdate || !IsSingleSelection || SelectedItem == null) return;
        if (double.TryParse(value, out double x))
        {
            SelectedItem.X = Math.Max(0, x);
        }
    }

    partial void OnDisplayYChanged(string value)
    {
        if (_isInternalPropertyUpdate || !IsSingleSelection || SelectedItem == null) return;
        if (double.TryParse(value, out double y))
        {
            SelectedItem.Y = Math.Max(0, y);
        }
    }

    partial void OnCommonWidthChanged(string value)
    {
        if (_isInternalPropertyUpdate || _canvasViewModel == null) return;
        if (double.TryParse(value, out double w) && w >= 10)
        {
            foreach (var item in _canvasViewModel.SelectedItems)
            {
                item.Width = w;
            }
        }
    }

    partial void OnCommonHeightChanged(string value)
    {
        if (_isInternalPropertyUpdate || _canvasViewModel == null) return;
        if (double.TryParse(value, out double h) && h >= 10)
        {
            foreach (var item in _canvasViewModel.SelectedItems)
            {
                item.Height = h;
            }
        }
    }

    partial void OnCommonZChanged(string value)
    {
        if (_isInternalPropertyUpdate || _canvasViewModel == null) return;
        if (int.TryParse(value, out int z))
        {
            foreach (var item in _canvasViewModel.SelectedItems)
            {
                item.Z = z;
            }
        }
    }

    partial void OnCommonRotationChanged(string value)
    {
        if (_isInternalPropertyUpdate || _canvasViewModel == null) return;
        if (double.TryParse(value, out double rot))
        {
            foreach (var item in _canvasViewModel.SelectedItems)
            {
                item.Rotation = rot;
            }
        }
    }

    partial void OnCommonStrokeThicknessChanged(double value)
    {
        if (_isInternalPropertyUpdate || _canvasViewModel == null) return;
        foreach (var shape in _canvasViewModel.SelectedItems.OfType<ShapeItemViewModel>())
        {
            shape.StrokeThickness = value;
        }
    }

    partial void OnCommonCornerRadiusChanged(double value)
    {
        if (_isInternalPropertyUpdate || _canvasViewModel == null) return;
        foreach (var shape in _canvasViewModel.SelectedItems.OfType<ShapeItemViewModel>())
        {
            shape.CornerRadius = value;
        }
    }

    partial void OnCommonInnerRadiusChanged(double value)
    {
        if (_isInternalPropertyUpdate || _canvasViewModel == null) return;
        foreach (var shape in _canvasViewModel.SelectedItems.OfType<ShapeItemViewModel>())
        {
            shape.InnerRadius = value;
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
            _canvasViewModel?.RecalculateValidation();
        }
    }
}
