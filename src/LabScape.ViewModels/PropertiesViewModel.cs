using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LabScape.Core.Enums;
using LabScape.Core.Helpers;
using LabScape.Core.Models;
using LabScape.ViewModels.Commands;
using LabScape.ViewModels.Items;

namespace LabScape.ViewModels;

public class GradientDirectionOption
{
    public string Name { get; set; } = string.Empty;
    public double Angle { get; set; }

    public GradientDirectionOption(string name, double angle)
    {
        Name = name;
        Angle = angle;
    }

    public override string ToString() => Name;
}

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

    public static readonly string[] PresetColors = new[]
    {
        "#FFFFFF", "#F8FAFC", "#E2E8F0", "#94A3B8", "#475569", "#0F172A",
        "#EF4444", "#F97316", "#F59E0B", "#EAB308", "#10B981", "#06B6D4",
        "#3B82F6", "#6366F1", "#8B5CF6", "#A855F7", "#EC4899", "#F43F5E",
        "#FEE2E2", "#FEF3C7", "#DCFCE7", "#E0E7FF", "#EDE9FE", "#FCE7F3"
    };
    public string[] AvailablePresetColors => PresetColors;

    public static readonly BrushType[] AvailableFillTypes = new[]
    {
        BrushType.Solid,
        BrushType.LinearGradient,
        BrushType.RadialGradient,
        BrushType.None
    };
    public BrushType[] AvailableFillTypesList => AvailableFillTypes;

    public static readonly BrushType[] AvailableStrokeTypes = new[]
    {
        BrushType.Solid,
        BrushType.LinearGradient,
        BrushType.None
    };
    public BrushType[] AvailableStrokeTypesList => AvailableStrokeTypes;

    public static readonly List<GradientDirectionOption> DirectionOptions = new()
    {
        new("➡️ Left to Right (90°)", 90.0),
        new("⬇️ Top to Bottom (180°)", 180.0),
        new("↘️ Diagonal Down (135°)", 135.0),
        new("↗️ Diagonal Up (45°)", 45.0),
        new("⬅️ Right to Left (270°)", 270.0),
        new("⬆️ Bottom to Top (0°)", 0.0)
    };
    public List<GradientDirectionOption> AvailableDirectionOptions => DirectionOptions;

    [ObservableProperty]
    private bool _isFillApplicable = true;

    [ObservableProperty]
    private BrushType _fillType = BrushType.Solid;

    [ObservableProperty]
    private string _fillColor1 = "#E0E7FF";

    [ObservableProperty]
    private string _fillColor2 = "#4338CA";

    [ObservableProperty]
    private double _fillAngle = 90.0;

    [ObservableProperty]
    private string _fillBrushString = "#E0E7FF";

    [ObservableProperty]
    private GradientDirectionOption _selectedFillDirection = DirectionOptions[0];

    public bool IsSolidFill => FillType == BrushType.Solid;
    public bool IsLinearGradientFill => FillType == BrushType.LinearGradient;
    public bool IsRadialGradientFill => FillType == BrushType.RadialGradient;
    public bool IsNoFill => FillType == BrushType.None;

    [ObservableProperty]
    private BrushType _strokeType = BrushType.Solid;

    [ObservableProperty]
    private string _strokeColor1 = "#4338CA";

    [ObservableProperty]
    private string _strokeColor2 = "#06B6D4";

    [ObservableProperty]
    private double _strokeAngle = 90.0;

    [ObservableProperty]
    private string _strokeBrushString = "#4338CA";

    [ObservableProperty]
    private GradientDirectionOption _selectedStrokeDirection = DirectionOptions[0];

    public bool IsSolidStroke => StrokeType == BrushType.Solid;
    public bool IsLinearGradientStroke => StrokeType == BrushType.LinearGradient;
    public bool IsNoStroke => StrokeType == BrushType.None;

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
            IsFillApplicable = false;
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
        SyncStyleFromSelection();
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

        if (e.PropertyName is nameof(ShapeItemViewModel.Fill) or nameof(ShapeItemViewModel.Stroke) or nameof(ShapeItemViewModel.StrokeThickness))
        {
            SyncStyleFromSelection();
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

    private string _initialFill = "#E0E7FF";
    private string _initialStroke = "#4338CA";

    private void SyncStyleFromSelection()
    {
        if (_canvasViewModel == null) return;
        var shapeItems = _canvasViewModel.SelectedItems.OfType<ShapeItemViewModel>().ToList();
        if (shapeItems.Count == 0) return;

        var firstShape = shapeItems[0];
        bool hasFillable = shapeItems.Any(s => s.ShapeType != ShapeType.Line && s.ShapeType != ShapeType.Bezier);
        IsFillApplicable = hasFillable;

        _initialFill = firstShape.Fill;
        _initialStroke = firstShape.Stroke;

        _isInternalPropertyUpdate = true;
        try
        {
            var fillInfo = GradientHelper.AnalyzeBrush(firstShape.Fill, defaultColor1: "#E0E7FF", defaultColor2: "#4338CA");
            FillType = fillInfo.Type;
            FillColor1 = fillInfo.Color1;
            FillColor2 = fillInfo.Color2;
            FillAngle = fillInfo.Angle;
            FillBrushString = firstShape.Fill;
            SelectedFillDirection = DirectionOptions.FirstOrDefault(d => Math.Abs(d.Angle - fillInfo.Angle) < 1) ?? DirectionOptions[0];

            var strokeInfo = GradientHelper.AnalyzeBrush(firstShape.Stroke, defaultColor1: "#4338CA", defaultColor2: "#06B6D4");
            StrokeType = strokeInfo.Type;
            StrokeColor1 = strokeInfo.Color1;
            StrokeColor2 = strokeInfo.Color2;
            StrokeAngle = strokeInfo.Angle;
            StrokeBrushString = firstShape.Stroke;
            SelectedStrokeDirection = DirectionOptions.FirstOrDefault(d => Math.Abs(d.Angle - strokeInfo.Angle) < 1) ?? DirectionOptions[0];

            OnPropertyChanged(nameof(IsSolidFill));
            OnPropertyChanged(nameof(IsLinearGradientFill));
            OnPropertyChanged(nameof(IsRadialGradientFill));
            OnPropertyChanged(nameof(IsNoFill));
            OnPropertyChanged(nameof(IsSolidStroke));
            OnPropertyChanged(nameof(IsLinearGradientStroke));
            OnPropertyChanged(nameof(IsNoStroke));
        }
        finally
        {
            _isInternalPropertyUpdate = false;
        }
    }

    private string BuildFillString()
    {
        return FillType switch
        {
            BrushType.None => "none",
            BrushType.Solid => GradientHelper.NormalizeHex(FillColor1, "#E0E7FF"),
            BrushType.LinearGradient => GradientHelper.CreateLinearGradient(FillColor1, FillColor2, FillAngle),
            BrushType.RadialGradient => GradientHelper.CreateRadialGradient(FillColor1, FillColor2),
            _ => GradientHelper.NormalizeHex(FillColor1, "#E0E7FF")
        };
    }

    private string BuildStrokeString()
    {
        return StrokeType switch
        {
            BrushType.None => "none",
            BrushType.Solid => GradientHelper.NormalizeHex(StrokeColor1, "#4338CA"),
            BrushType.LinearGradient => GradientHelper.CreateLinearGradient(StrokeColor1, StrokeColor2, StrokeAngle),
            _ => GradientHelper.NormalizeHex(StrokeColor1, "#4338CA")
        };
    }

    private void UpdateLiveFill()
    {
        if (_canvasViewModel == null) return;
        string newFill = BuildFillString();
        FillBrushString = newFill;

        _isInternalPropertyUpdate = true;
        try
        {
            foreach (var shape in _canvasViewModel.SelectedItems.OfType<ShapeItemViewModel>()
                .Where(s => s.ShapeType != ShapeType.Line && s.ShapeType != ShapeType.Bezier))
            {
                shape.Fill = newFill;
            }
        }
        finally
        {
            _isInternalPropertyUpdate = false;
        }
    }

    private void UpdateLiveStroke()
    {
        if (_canvasViewModel == null) return;
        string newStroke = BuildStrokeString();
        StrokeBrushString = newStroke;

        _isInternalPropertyUpdate = true;
        try
        {
            foreach (var shape in _canvasViewModel.SelectedItems.OfType<ShapeItemViewModel>())
            {
                shape.Stroke = newStroke;
            }
        }
        finally
        {
            _isInternalPropertyUpdate = false;
        }
    }

    partial void OnFillTypeChanged(BrushType value)
    {
        OnPropertyChanged(nameof(IsSolidFill));
        OnPropertyChanged(nameof(IsLinearGradientFill));
        OnPropertyChanged(nameof(IsRadialGradientFill));
        OnPropertyChanged(nameof(IsNoFill));
        if (!_isInternalPropertyUpdate) UpdateLiveFill();
    }

    partial void OnFillColor1Changed(string value)
    {
        if (!_isInternalPropertyUpdate) UpdateLiveFill();
    }

    partial void OnFillColor2Changed(string value)
    {
        if (!_isInternalPropertyUpdate) UpdateLiveFill();
    }

    partial void OnFillAngleChanged(double value)
    {
        if (!_isInternalPropertyUpdate) UpdateLiveFill();
    }

    partial void OnSelectedFillDirectionChanged(GradientDirectionOption value)
    {
        if (value != null && Math.Abs(FillAngle - value.Angle) > 0.1)
        {
            FillAngle = value.Angle;
        }
    }

    partial void OnStrokeTypeChanged(BrushType value)
    {
        OnPropertyChanged(nameof(IsSolidStroke));
        OnPropertyChanged(nameof(IsLinearGradientStroke));
        OnPropertyChanged(nameof(IsNoStroke));
        if (!_isInternalPropertyUpdate) UpdateLiveStroke();
    }

    partial void OnStrokeColor1Changed(string value)
    {
        if (!_isInternalPropertyUpdate) UpdateLiveStroke();
    }

    partial void OnStrokeColor2Changed(string value)
    {
        if (!_isInternalPropertyUpdate) UpdateLiveStroke();
    }

    partial void OnStrokeAngleChanged(double value)
    {
        if (!_isInternalPropertyUpdate) UpdateLiveStroke();
    }

    partial void OnSelectedStrokeDirectionChanged(GradientDirectionOption value)
    {
        if (value != null && Math.Abs(StrokeAngle - value.Angle) > 0.1)
        {
            StrokeAngle = value.Angle;
        }
    }

    [RelayCommand]
    public void SetFillColor1(string hex)
    {
        FillColor1 = hex;
        UpdateLiveFill();
        ApplyFillChanges();
    }

    [RelayCommand]
    public void SetFillColor2(string hex)
    {
        FillColor2 = hex;
        UpdateLiveFill();
        ApplyFillChanges();
    }

    [RelayCommand]
    public void SwapFillColors()
    {
        _isInternalPropertyUpdate = true;
        try
        {
            (FillColor1, FillColor2) = (FillColor2, FillColor1);
        }
        finally
        {
            _isInternalPropertyUpdate = false;
        }
        UpdateLiveFill();
        ApplyFillChanges();
    }

    [RelayCommand]
    public void SetStrokeColor1(string hex)
    {
        StrokeColor1 = hex;
        UpdateLiveStroke();
        ApplyStrokeChanges();
    }

    [RelayCommand]
    public void SetStrokeColor2(string hex)
    {
        StrokeColor2 = hex;
        UpdateLiveStroke();
        ApplyStrokeChanges();
    }

    [RelayCommand]
    public void SwapStrokeColors()
    {
        _isInternalPropertyUpdate = true;
        try
        {
            (StrokeColor1, StrokeColor2) = (StrokeColor2, StrokeColor1);
        }
        finally
        {
            _isInternalPropertyUpdate = false;
        }
        UpdateLiveStroke();
        ApplyStrokeChanges();
    }

    public void ApplyFillChanges()
    {
        if (_canvasViewModel == null) return;
        string newFill = BuildFillString();
        FillBrushString = newFill;

        var applicableShapes = _canvasViewModel.SelectedItems.OfType<ShapeItemViewModel>()
            .Where(s => s.ShapeType != ShapeType.Line && s.ShapeType != ShapeType.Bezier)
            .ToList();

        if (applicableShapes.Count == 0) return;

        if (newFill != _initialFill)
        {
            string old = _initialFill;
            var changes = applicableShapes
                .Select(s => (Shape: s, OldFill: (string?)old, NewFill: (string?)newFill, OldStroke: (string?)null, NewStroke: (string?)null, OldThickness: (double?)null, NewThickness: (double?)null))
                .ToList();

            var cmd = new ModifyShapeStyleCommand(changes, "Change Shape Fill");
            _canvasViewModel.UndoRedoService.ExecuteCommand(cmd);
            _initialFill = newFill;
        }
    }

    public void ApplyStrokeChanges()
    {
        if (_canvasViewModel == null) return;
        string newStroke = BuildStrokeString();
        StrokeBrushString = newStroke;

        var applicableShapes = _canvasViewModel.SelectedItems.OfType<ShapeItemViewModel>().ToList();
        if (applicableShapes.Count == 0) return;

        if (newStroke != _initialStroke)
        {
            string old = _initialStroke;
            var changes = applicableShapes
                .Select(s => (Shape: s, OldFill: (string?)null, NewFill: (string?)null, OldStroke: (string?)old, NewStroke: (string?)newStroke, OldThickness: (double?)null, NewThickness: (double?)null))
                .ToList();

            var cmd = new ModifyShapeStyleCommand(changes, "Change Shape Stroke");
            _canvasViewModel.UndoRedoService.ExecuteCommand(cmd);
            _initialStroke = newStroke;
        }
    }
}
