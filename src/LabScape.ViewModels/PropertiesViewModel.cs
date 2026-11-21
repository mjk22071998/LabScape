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



    public CanvasViewModel? Canvas => _canvasViewModel;



    public PageSizePreset CanvasPagePreset

    {

        get => _canvasViewModel?.PageSize ?? PageSizePreset.A3;

        set

        {

            if (_canvasViewModel != null && _canvasViewModel.PageSize != value)

            {

                _canvasViewModel.SetPageSize(value, _canvasViewModel.PageOrientation);

                NotifyCanvasPropertiesChanged();

            }

        }

    }



    public PageOrientation CanvasPageOrientation

    {

        get => _canvasViewModel?.PageOrientation ?? PageOrientation.Landscape;

        set

        {

            if (_canvasViewModel != null && _canvasViewModel.PageOrientation != value)

            {

                _canvasViewModel.SetPageSize(_canvasViewModel.PageSize, value);

                NotifyCanvasPropertiesChanged();

            }

        }

    }



    public bool IsLandscape => CanvasPageOrientation == PageOrientation.Landscape;

    public bool IsPortrait => CanvasPageOrientation == PageOrientation.Portrait;

    public bool IsCustomPageSize => CanvasPagePreset == PageSizePreset.Custom;



    public bool AutoSize

    {

        get => _canvasViewModel?.AutoSize ?? true;

        set

        {

            if (_canvasViewModel != null && _canvasViewModel.AutoSize != value)

            {

                _canvasViewModel.AutoSize = value;

                NotifyCanvasPropertiesChanged();

            }

        }

    }



    public int TotalPages => _canvasViewModel?.TotalPages ?? 1;

    public int PageColumns => _canvasViewModel?.PageColumns ?? 1;

    public int PageRows => _canvasViewModel?.PageRows ?? 1;

    public string PageSizeDisplayText => _canvasViewModel?.PageSizeDisplayText ?? string.Empty;

    public string PageGridSummary => TotalPages <= 1

        ? "1 Single Page (1 × 1)"

        : $"{TotalPages} Pages ({PageColumns} Columns × {PageRows} Rows)";



    public record PagePresetOption(PageSizePreset Preset, string DisplayName);



    public IReadOnlyList<PagePresetOption> AvailablePresets { get; } = Enum.GetValues<PageSizePreset>()

        .Select(p => new PagePresetOption(p, PageDimensions.GetDisplayName(p)))

        .ToList();



    public PagePresetOption? SelectedPresetOption

    {

        get => AvailablePresets.FirstOrDefault(p => p.Preset == CanvasPagePreset);

        set

        {

            if (value != null && value.Preset != CanvasPagePreset)

            {

                CanvasPagePreset = value.Preset;

            }

        }

    }



    public string CanvasWidthText

    {

        get => _canvasViewModel?.CanvasWidth.ToString("F0") ?? "1587";

        set

        {

            if (_canvasViewModel != null && double.TryParse(value, out double w) && w >= 200)

            {

                _canvasViewModel.SetPageSize(PageSizePreset.Custom, _canvasViewModel.PageOrientation, customWidth: w);

                NotifyCanvasPropertiesChanged();

            }

        }

    }



    public string CanvasHeightText

    {

        get => _canvasViewModel?.CanvasHeight.ToString("F0") ?? "1123";

        set

        {

            if (_canvasViewModel != null && double.TryParse(value, out double h) && h >= 200)

            {

                _canvasViewModel.SetPageSize(PageSizePreset.Custom, _canvasViewModel.PageOrientation, customHeight: h);

                NotifyCanvasPropertiesChanged();

            }

        }

    }



    [RelayCommand]

    public void SetPagePreset(PageSizePreset preset)

    {

        CanvasPagePreset = preset;

    }



    [RelayCommand]

    public void SetOrientation(PageOrientation orientation)

    {

        CanvasPageOrientation = orientation;

    }



    [RelayCommand]

    public void TogglePageOrientation()

    {

        CanvasPageOrientation = CanvasPageOrientation == PageOrientation.Landscape ? PageOrientation.Portrait : PageOrientation.Landscape;

    }



    [RelayCommand]

    public void FitPage()

    {

        _canvasViewModel?.FitToPage();

    }



    private void OnCanvasPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)

    {

        if (e.PropertyName is nameof(CanvasViewModel.CanvasWidth) or nameof(CanvasViewModel.CanvasHeight) or nameof(CanvasViewModel.PageSize) or nameof(CanvasViewModel.PageOrientation) or nameof(CanvasViewModel.AutoSize) or nameof(CanvasViewModel.PageColumns) or nameof(CanvasViewModel.PageRows) or nameof(CanvasViewModel.TotalPages) or nameof(CanvasViewModel.PageSizeDisplayText))

        {

            NotifyCanvasPropertiesChanged();

        }

    }



    private void NotifyCanvasPropertiesChanged()

    {

        OnPropertyChanged(nameof(Canvas));

        OnPropertyChanged(nameof(CanvasPagePreset));

        OnPropertyChanged(nameof(CanvasPageOrientation));

        OnPropertyChanged(nameof(IsLandscape));

        OnPropertyChanged(nameof(IsPortrait));

        OnPropertyChanged(nameof(IsCustomPageSize));

        OnPropertyChanged(nameof(CanvasWidthText));

        OnPropertyChanged(nameof(CanvasHeightText));

        OnPropertyChanged(nameof(SelectedPresetOption));

        OnPropertyChanged(nameof(AutoSize));

        OnPropertyChanged(nameof(TotalPages));

        OnPropertyChanged(nameof(PageColumns));

        OnPropertyChanged(nameof(PageRows));

        OnPropertyChanged(nameof(PageSizeDisplayText));

        OnPropertyChanged(nameof(PageGridSummary));

    }





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

    private bool _isMultipleDevices;



    [ObservableProperty]

    private int _selectedDevicesCount;



    [ObservableProperty]

    private string _deviceIpAddress = string.Empty;



    [ObservableProperty]

    private string _deviceMacAddress = string.Empty;



    [ObservableProperty]

    private string _deviceId = string.Empty;



    [ObservableProperty]

    private bool _isManualIpMode = true;



    [ObservableProperty]

    private string _linkedDatabaseDeviceName = string.Empty;



    [ObservableProperty]

    private ValidationStatus _deviceValidationStatus = ValidationStatus.Unassigned;



    [ObservableProperty]

    private string _deviceValidationFeedback = "Unassigned";



    [ObservableProperty]

    private string _deviceValidationColor = "#F59E0B";



    [ObservableProperty]

    private string _batchStartIp = "192.168.1.101";



    [ObservableProperty]

    private int _batchIpStep = 1;



    [ObservableProperty]

    private string _batchNamePrefix = "PC-";



    [ObservableProperty]

    private bool _batchRenameDevices = true;



    [ObservableProperty]

    private bool _isFurniture;



    [ObservableProperty]

    private bool _isShape;



    [ObservableProperty]

    private bool _isAppearanceApplicable;



    [ObservableProperty]

    private bool _isGroup;



    [ObservableProperty]

    private bool _isRoundedShape;



    [ObservableProperty]

    private bool _isDonut;

    [ObservableProperty]

    private bool _isBezier;

    [ObservableProperty]

    private int _bezierControlPointsCount;

    public bool CanAddBezierControlPoint => IsBezier;

    public bool CanRemoveBezierControlPoint => IsBezier && BezierControlPointsCount > 1;



    [ObservableProperty]

    private bool _isText;



    [ObservableProperty]

    private string _itemText = string.Empty;



    [ObservableProperty]

    private double _itemFontSize = 16.0;



    [ObservableProperty]

    private string _itemFontFamily = "Segoe UI";



    [ObservableProperty]

    private bool _isBold;



    [ObservableProperty]

    private bool _isItalic;



    [ObservableProperty]

    private string _itemTextAlignment = "Left";



    [ObservableProperty]

    private string _itemTextColor = "#111827";



    [ObservableProperty]

    private string _itemTextBackground = "Transparent";



    [ObservableProperty]

    private string _itemBorderColor = "Transparent";



    [ObservableProperty]

    private double _itemBorderThickness = 0.0;



    public static readonly string[] AvailableFontFamilies = new[]

    {

        "Segoe UI", "Arial", "Calibri", "Comic Sans MS", "Consolas",

        "Courier New", "Georgia", "Impact", "Lucida Console", "Tahoma",

        "Times New Roman", "Trebuchet MS", "Verdana"

    };

    public string[] FontFamiliesList => AvailableFontFamilies;



    public static readonly double[] AvailableFontSizes = new double[]

    {

        8, 9, 10, 11, 12, 14, 16, 18, 20, 24, 28, 32, 36, 48, 72

    };

    public double[] FontSizesList => AvailableFontSizes;



    [ObservableProperty]

    private string _ipSearchQuery = string.Empty;



    // Available inventory devices for searchable IP configuration

    public ObservableCollection<DatabaseDevice> AvailableDatabaseDevices { get; } = new();

    public ObservableCollection<DatabaseDevice> FilteredDatabaseDevices { get; } = new();



    public static readonly string[] PresetColors = new[]
    {
        // 1. Grayscale & Monochromes
        "#FFFFFF", "#F8FAFC", "#E2E8F0", "#CBD5E1", "#94A3B8", "#64748B", "#475569", "#334155", "#1E293B", "#0F172A", "#000000",
        // 2. Vivid Standard Hues
        "#EF4444", "#F97316", "#F59E0B", "#EAB308", "#84CC16", "#10B981", "#14B8A6", "#06B6D4", "#0EA5E9", "#3B82F6", "#6366F1", "#8B5CF6", "#A855F7", "#D946EF", "#EC4899", "#F43F5E",
        // 3. Rich Deep Shades
        "#991B1B", "#9A3412", "#92400E", "#854D0E", "#3F6212", "#065F46", "#115E59", "#155E75", "#075985", "#1E40AF", "#3730A3", "#5B21B6", "#6B21A8", "#86198F", "#9D174D", "#9F1239",
        // 4. Soft Pastels & Light Tints
        "#FEE2E2", "#FFEDD5", "#FEF3C7", "#FEF9C3", "#ECFCCB", "#DCFCE7", "#CCFBF1", "#CFFAFE", "#E0F2FE", "#DBEAFE", "#E0E7FF", "#EDE9FE", "#F3E8FF", "#FAE8FF", "#FCE7F3", "#FFE4E6"
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

        new("Left to Right (90°)", 90.0),

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

    public static Func<string?, IReadOnlyList<string>>? StencilColorsProvider { get; set; }

    public static Func<string?, string?, string?, IReadOnlyList<string>>? ItemColorsProvider { get; set; }

    public ObservableCollection<SvgColorEntryViewModel> SvgColors { get; } = new();

    [ObservableProperty]
    private bool _hasSvgColors;



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



        if (_canvasViewModel != null)

        {

            _canvasViewModel.PropertyChanged -= OnCanvasPropertyChanged;

        }



        _canvasViewModel = canvas;

        _selectionHandler = (s, e) => UpdateSelection();

        _canvasViewModel.SelectedItems.CollectionChanged += _selectionHandler;

        _canvasViewModel.PropertyChanged += OnCanvasPropertyChanged;

        UpdateSelection();

        NotifyCanvasPropertiesChanged();

    }



    public void DetachCanvas()

    {

        if (_canvasViewModel != null && _selectionHandler != null)

        {

            _canvasViewModel.SelectedItems.CollectionChanged -= _selectionHandler;

            _selectionHandler = null;

        }



        if (_canvasViewModel != null)

        {

            _canvasViewModel.PropertyChanged -= OnCanvasPropertyChanged;

            _canvasViewModel = null;

        }



        if (SelectedItem != null)

        {

            SelectedItem.PropertyChanged -= OnSelectedItemPropertyChanged;

        }



        UpdateSelection();

        NotifyCanvasPropertiesChanged();

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

            var selectedDevs = _canvasViewModel != null ? _canvasViewModel.SelectedItems.OfType<DeviceItemViewModel>().ToList() : new List<DeviceItemViewModel>();

            SelectedDevicesCount = selectedDevs.Count;

            IsMultipleDevices = selectedDevs.Count > 1;

            IsSingleDevice = false;

            IsDevice = selectedDevs.Count > 0;

            if (IsMultipleDevices && string.IsNullOrWhiteSpace(BatchStartIp))

            {

                BatchStartIp = _canvasViewModel?.SuggestNextAvailableIp() ?? "192.168.1.101";

            }

            IsMultipleDevices = false;

            SelectedDevicesCount = 0;

            DeviceIpAddress = string.Empty;

            DeviceMacAddress = string.Empty;

            DeviceId = string.Empty;

            LinkedDatabaseDeviceName = string.Empty;

            DeviceValidationStatus = ValidationStatus.Unassigned;

            DeviceValidationFeedback = "No device selected";

            IsFurniture = false;

            IsShape = false;

            IsAppearanceApplicable = false;

            IsGroup = false;

            IsRoundedShape = false;

            IsDonut = false;

            IsBezier = false;

            BezierControlPointsCount = 0;

            IsFillApplicable = false;

            SvgColors.Clear();

            HasSvgColors = false;

            IsText = false;

            ItemText = string.Empty;

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

            IsMultipleDevices = false;

            SelectedDevicesCount = IsDevice ? 1 : 0;

            if (SelectedItem is DeviceItemViewModel devSingle)

            {

                _isInternalPropertyUpdate = true;

                try

                {

                    DeviceIpAddress = devSingle.IpAddress ?? string.Empty;

                    DeviceMacAddress = devSingle.MacAddress ?? string.Empty;

                    DeviceId = devSingle.DeviceId ?? string.Empty;

                    IsManualIpMode = devSingle.IsManualIp;

                    LinkedDatabaseDeviceName = devSingle.IsManualIp ? string.Empty : devSingle.Name;

                    UpdateDeviceValidationDisplay(devSingle);

                }

                finally

                {

                    _isInternalPropertyUpdate = false;

                }

            }

            IsFurniture = SelectedItem is FurnitureItemViewModel;

            IsShape = SelectedItem is ShapeItemViewModel;

            IsAppearanceApplicable = SelectedItem is IStylableItemViewModel;

            IsGroup = SelectedItem is GroupItemViewModel;

            IsText = SelectedItem is TextItemViewModel;



            if (SelectedItem is TextItemViewModel textVm)

            {

                _isInternalPropertyUpdate = true;

                try

                {

                    ItemText = textVm.Text;

                    ItemFontSize = textVm.FontSize;

                    ItemFontFamily = textVm.FontFamily;

                    IsBold = string.Equals(textVm.FontWeight, "Bold", StringComparison.OrdinalIgnoreCase);

                    IsItalic = string.Equals(textVm.FontStyle, "Italic", StringComparison.OrdinalIgnoreCase);

                    ItemTextAlignment = textVm.TextAlignment;

                    ItemTextColor = textVm.TextColor;

                    ItemTextBackground = textVm.Background;

                    ItemBorderColor = textVm.BorderColor;

                    ItemBorderThickness = textVm.BorderThickness;

                }

                finally

                {

                    _isInternalPropertyUpdate = false;

                }

            }



            if (SelectedItem is ShapeItemViewModel shapeVm)

            {

                IsRoundedShape = shapeVm.ShapeType == ShapeType.RoundedRectangle ||

                                 shapeVm.ShapeType == ShapeType.RoundedCornerTriangle;

                IsDonut = shapeVm.ShapeType == ShapeType.Donut;

                IsBezier = shapeVm.ShapeType == ShapeType.Bezier;

                BezierControlPointsCount = shapeVm.ShapeType == ShapeType.Bezier ? shapeVm.ControlPointCount : 0;

                CommonCornerRadius = shapeVm.CornerRadius;

                CommonInnerRadius = shapeVm.InnerRadius;

                CommonStrokeThickness = shapeVm.StrokeThickness;

            }

            else

            {

                IsRoundedShape = false;

                IsDonut = false;

            IsBezier = false;

            BezierControlPointsCount = 0;

            }

        }

        else

        {

            HeaderTitle = "MULTIPLE ITEMS SELECTED";

            HeaderSubtitle = $"{SelectedCount} items selected (Temporary Group)";

            DisplayName = "(Multiple Items Selected)";

            CoordinatesToolTip = "X/Y coordinate editing is disabled when multiple items are selected. Move items directly on canvas or use alignment tools.";



            IsDevice = false;

            IsSingleDevice = false;

            IsFurniture = false;

            IsGroup = false;

            IsText = false;

            IsShape = _canvasViewModel.SelectedItems.Any(i => i is ShapeItemViewModel);

            IsAppearanceApplicable = _canvasViewModel.SelectedItems.Any(i => i is IStylableItemViewModel);

            IsRoundedShape = _canvasViewModel.SelectedItems.All(i => i is ShapeItemViewModel s && (s.ShapeType == ShapeType.RoundedRectangle || s.ShapeType == ShapeType.RoundedCornerTriangle));

            IsDonut = _canvasViewModel.SelectedItems.All(i => i is ShapeItemViewModel s && s.ShapeType == ShapeType.Donut);

            IsBezier = _canvasViewModel.SelectedItems.All(i => i is ShapeItemViewModel s && s.ShapeType == ShapeType.Bezier);

            var firstShape = _canvasViewModel.SelectedItems.OfType<ShapeItemViewModel>().FirstOrDefault();

            BezierControlPointsCount = IsBezier && firstShape != null ? firstShape.ControlPointCount : 0;

            if (firstShape != null)

            {

                CommonStrokeThickness = firstShape.StrokeThickness;

                CommonCornerRadius = firstShape.CornerRadius;

                CommonInnerRadius = firstShape.InnerRadius;

            }

        }



        SyncFieldsFromSelection();

        SyncStyleFromSelection();

        SyncSvgColorsFromSelection();

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

        if (e.PropertyName is nameof(ShapeItemViewModel.ControlPointCount))

        {

            if (SelectedItem is ShapeItemViewModel shape && shape.ShapeType == ShapeType.Bezier)

            {

                BezierControlPointsCount = shape.ControlPointCount;

                OnPropertyChanged(nameof(CanAddBezierControlPoint));

                OnPropertyChanged(nameof(CanRemoveBezierControlPoint));

            }

        }

        if (e.PropertyName is nameof(ItemViewModel.SvgColorMap))

        {

            SyncSvgColorsMapState();

        }



        if (e.PropertyName is nameof(TextItemViewModel.Text) or nameof(TextItemViewModel.FontSize) or

            nameof(TextItemViewModel.FontFamily) or nameof(TextItemViewModel.FontWeight) or

            nameof(TextItemViewModel.FontStyle) or nameof(TextItemViewModel.TextAlignment) or

            nameof(TextItemViewModel.TextColor) or nameof(TextItemViewModel.Background))

        {

            SyncTextFromSelection();

        }



        if (e.PropertyName is nameof(DeviceItemViewModel.IpAddress) or

            nameof(DeviceItemViewModel.MacAddress) or

            nameof(DeviceItemViewModel.DeviceId) or

            nameof(DeviceItemViewModel.ValidationStatus) or

            nameof(DeviceItemViewModel.IsManualIp))

        {

            SyncDeviceFromSelection();

        }

    }



    private void SyncTextFromSelection()

    {

        if (SelectedItem is not TextItemViewModel textVm) return;

        _isInternalPropertyUpdate = true;

        try

        {

            ItemText = textVm.Text;

            ItemFontSize = textVm.FontSize;

            ItemFontFamily = textVm.FontFamily;

            IsBold = string.Equals(textVm.FontWeight, "Bold", StringComparison.OrdinalIgnoreCase);

            IsItalic = string.Equals(textVm.FontStyle, "Italic", StringComparison.OrdinalIgnoreCase);

            ItemTextAlignment = textVm.TextAlignment;

            ItemTextColor = textVm.TextColor;

            ItemTextBackground = textVm.Background;

            ItemBorderColor = textVm.BorderColor;

            ItemBorderThickness = textVm.BorderThickness;

        }

        finally

        {

            _isInternalPropertyUpdate = false;

        }

    }



    partial void OnItemTextChanged(string value)

    {

        if (_isInternalPropertyUpdate || SelectedItem is not TextItemViewModel textVm) return;

        if (textVm.Text != value)

        {

            string old = textVm.Text;

            textVm.Text = value;

            _canvasViewModel?.UndoRedoService.ExecuteCommand(new ChangeTextContentCommand(textVm, old, value));

        }

    }



    partial void OnItemFontSizeChanged(double value)

    {

        if (_isInternalPropertyUpdate || SelectedItem is not TextItemViewModel textVm) return;

        textVm.FontSize = value;

    }



    partial void OnItemFontFamilyChanged(string value)

    {

        if (_isInternalPropertyUpdate || SelectedItem is not TextItemViewModel textVm) return;

        textVm.FontFamily = value;

    }



    partial void OnIsBoldChanged(bool value)

    {

        if (_isInternalPropertyUpdate || SelectedItem is not TextItemViewModel textVm) return;

        textVm.FontWeight = value ? "Bold" : "Normal";

    }



    partial void OnIsItalicChanged(bool value)

    {

        if (_isInternalPropertyUpdate || SelectedItem is not TextItemViewModel textVm) return;

        textVm.FontStyle = value ? "Italic" : "Normal";

    }



    partial void OnItemTextAlignmentChanged(string value)

    {

        if (_isInternalPropertyUpdate || SelectedItem is not TextItemViewModel textVm) return;

        textVm.TextAlignment = value;

    }



    partial void OnItemTextColorChanged(string value)

    {

        if (_isInternalPropertyUpdate || SelectedItem is not TextItemViewModel textVm) return;

        textVm.TextColor = value;

    }



    partial void OnItemTextBackgroundChanged(string value)

    {

        if (_isInternalPropertyUpdate || SelectedItem is not TextItemViewModel textVm) return;

        textVm.Background = value;

    }



    partial void OnItemBorderColorChanged(string value)

    {

        if (_isInternalPropertyUpdate || SelectedItem is not TextItemViewModel textVm) return;

        textVm.BorderColor = value;

    }



    partial void OnItemBorderThicknessChanged(double value)

    {

        if (_isInternalPropertyUpdate || SelectedItem is not TextItemViewModel textVm) return;

        textVm.BorderThickness = value;

    }



    [RelayCommand]

    public void SetItemTextColor(string color) => ItemTextColor = color;



    [RelayCommand]

    public void SetItemTextBackground(string color) => ItemTextBackground = color;



    [RelayCommand]

    public void SetItemBorderColor(string color) => ItemBorderColor = color;



    [RelayCommand]

    public void SetAlignLeft() => ItemTextAlignment = "Left";



    [RelayCommand]

    public void SetAlignCenter() => ItemTextAlignment = "Center";



    [RelayCommand]

    public void SetAlignRight() => ItemTextAlignment = "Right";



    [RelayCommand]

    public void ToggleBold() => IsBold = !IsBold;



    [RelayCommand]

    public void ToggleItalic() => IsItalic = !IsItalic;



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

            SelectedItem.X = x;

        }

    }



    partial void OnDisplayYChanged(string value)

    {

        if (_isInternalPropertyUpdate || !IsSingleSelection || SelectedItem == null) return;

        if (double.TryParse(value, out double y))

        {

            SelectedItem.Y = y;

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

        foreach (var item in _canvasViewModel.SelectedItems.OfType<IStylableItemViewModel>())

        {

            item.StrokeThickness = value;

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



    private void SyncDeviceFromSelection()

    {

        if (SelectedItem is not DeviceItemViewModel dev) return;

        _isInternalPropertyUpdate = true;

        try

        {

            DeviceIpAddress = dev.IpAddress ?? string.Empty;

            DeviceMacAddress = dev.MacAddress ?? string.Empty;

            DeviceId = dev.DeviceId ?? string.Empty;

            IsManualIpMode = dev.IsManualIp;

            LinkedDatabaseDeviceName = dev.IsManualIp ? string.Empty : dev.Name;

            UpdateDeviceValidationDisplay(dev);

        }

        finally

        {

            _isInternalPropertyUpdate = false;

        }

    }



    private void UpdateDeviceValidationDisplay(DeviceItemViewModel dev)

    {

        DeviceValidationStatus = dev.ValidationStatus;

        switch (dev.ValidationStatus)

        {

            case ValidationStatus.Valid:

                DeviceValidationFeedback = "Valid IPv4 Address";

                DeviceValidationColor = "#10B981"; // Emerald Green

                break;

            case ValidationStatus.DuplicateIp:

                DeviceValidationFeedback = "Conflict: Duplicate IP on canvas";

                DeviceValidationColor = "#EF4444"; // Red

                break;

            case ValidationStatus.InvalidIpFormat:

                DeviceValidationFeedback = "Invalid IPv4 Format (e.g. 192.168.1.10)";

                DeviceValidationColor = "#EF4444"; // Red

                break;

            case ValidationStatus.Unassigned:

            default:

                DeviceValidationFeedback = "Unassigned (No IP Address)";

                DeviceValidationColor = "#F59E0B"; // Amber

                break;

        }

    }



    partial void OnDeviceIpAddressChanged(string value)

    {

        if (_isInternalPropertyUpdate || SelectedItem is not DeviceItemViewModel dev) return;

        string? trimmed = string.IsNullOrWhiteSpace(value) ? null : value.Trim();

        dev.IpAddress = trimmed;

        dev.IsManualIp = true;

        IsManualIpMode = true;

        _canvasViewModel?.RecalculateValidation();

        UpdateDeviceValidationDisplay(dev);

    }



    partial void OnDeviceMacAddressChanged(string value)

    {

        if (_isInternalPropertyUpdate || SelectedItem is not DeviceItemViewModel dev) return;

        dev.MacAddress = string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    }



    partial void OnDeviceIdChanged(string value)

    {

        if (_isInternalPropertyUpdate || SelectedItem is not DeviceItemViewModel dev) return;

        dev.DeviceId = string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    }



    [RelayCommand]

    public void SetManualMode()

    {

        IsManualIpMode = true;

        if (SelectedItem is DeviceItemViewModel dev)

        {

            dev.IsManualIp = true;

            LinkedDatabaseDeviceName = string.Empty;

        }

    }



    [RelayCommand]

    public void SetDatabaseMode()

    {

        IsManualIpMode = false;

    }



    [RelayCommand]

    public void SwitchToManualMode()

    {

        if (SelectedItem is DeviceItemViewModel dev)

        {

            dev.IsManualIp = true;

            IsManualIpMode = true;

            LinkedDatabaseDeviceName = string.Empty;

        }

    }



    [RelayCommand]

    public void AutoGenerateMac()

    {

        if (SelectedItem is not DeviceItemViewModel dev) return;

        var r = new Random();

        string mac = $"02:{r.Next(256):X2}:{r.Next(256):X2}:{r.Next(256):X2}:{r.Next(256):X2}:{r.Next(256):X2}";

        string? oldMac = dev.MacAddress;

        dev.MacAddress = mac;

        DeviceMacAddress = mac;

        _canvasViewModel?.UndoRedoService.ExecuteCommand(new AssignDeviceNetworkCommand(

            dev,

            oldIp: dev.IpAddress, newIp: dev.IpAddress,

            oldMac: oldMac, newMac: mac,

            oldDeviceId: dev.DeviceId, newDeviceId: dev.DeviceId,

            oldName: dev.Name, newName: dev.Name,

            oldIsManual: dev.IsManualIp, newIsManual: dev.IsManualIp));

    }



    [RelayCommand]

    public void SuggestNextIp()

    {

        if (_canvasViewModel == null || SelectedItem is not DeviceItemViewModel dev) return;

        string nextIp = _canvasViewModel.SuggestNextAvailableIp();

        string? oldIp = dev.IpAddress;

        dev.IpAddress = nextIp;

        dev.IsManualIp = true;

        IsManualIpMode = true;

        DeviceIpAddress = nextIp;

        _canvasViewModel.RecalculateValidation();

        UpdateDeviceValidationDisplay(dev);



        _canvasViewModel.UndoRedoService.ExecuteCommand(new AssignDeviceNetworkCommand(

            dev,

            oldIp: oldIp, newIp: nextIp,

            oldMac: dev.MacAddress, newMac: dev.MacAddress,

            oldDeviceId: dev.DeviceId, newDeviceId: dev.DeviceId,

            oldName: dev.Name, newName: dev.Name,

            oldIsManual: dev.IsManualIp, newIsManual: true,

            onChanged: () =>
            {

                _canvasViewModel?.RecalculateValidation();

                SyncDeviceFromSelection();

            }));

    }



    [RelayCommand]

    public void ClearDeviceIp()

    {

        if (SelectedItem is not DeviceItemViewModel dev) return;

        string? oldIp = dev.IpAddress;

        string? oldId = dev.DeviceId;

        bool oldIsManual = dev.IsManualIp;



        dev.IpAddress = null;

        dev.DeviceId = null;

        dev.IsManualIp = true;

        IsManualIpMode = true;

        LinkedDatabaseDeviceName = string.Empty;

        DeviceIpAddress = string.Empty;

        DeviceId = string.Empty;



        _canvasViewModel?.RecalculateValidation();

        UpdateDeviceValidationDisplay(dev);



        _canvasViewModel?.UndoRedoService.ExecuteCommand(new AssignDeviceNetworkCommand(

            dev,

            oldIp: oldIp, newIp: null,

            oldMac: dev.MacAddress, newMac: dev.MacAddress,

            oldDeviceId: oldId, newDeviceId: null,

            oldName: dev.Name, newName: dev.Name,

            oldIsManual: oldIsManual, newIsManual: true,

            onChanged: () =>
            {

                _canvasViewModel?.RecalculateValidation();

                SyncDeviceFromSelection();

            }));

    }



    public void AssignIpToSelectedDevice(DatabaseDevice dbDevice)

    {

        if (SelectedItem is DeviceItemViewModel dev)

        {

            string? oldIp = dev.IpAddress;

            string? oldMac = dev.MacAddress;

            string? oldId = dev.DeviceId;

            string oldName = dev.Name;

            bool oldIsManual = dev.IsManualIp;



            string newName = dbDevice.DisplayName;



            dev.DeviceId = dbDevice.DeviceId;

            dev.IpAddress = dbDevice.PrimaryIp;

            dev.MacAddress = dbDevice.MacAddress;

            dev.Name = newName;

            dev.IsManualIp = false;



            IsManualIpMode = false;

            LinkedDatabaseDeviceName = dbDevice.DisplayName;

            DeviceIpAddress = dbDevice.PrimaryIp;

            DeviceMacAddress = dbDevice.MacAddress;

            DeviceId = dbDevice.DeviceId;

            DisplayName = newName;



            _canvasViewModel?.RecalculateValidation();

            UpdateDeviceValidationDisplay(dev);



            _canvasViewModel?.UndoRedoService.ExecuteCommand(new AssignDeviceNetworkCommand(

                dev,

                oldIp: oldIp, newIp: dbDevice.PrimaryIp,

                oldMac: oldMac, newMac: dbDevice.MacAddress,

                oldDeviceId: oldId, newDeviceId: dbDevice.DeviceId,

                oldName: oldName, newName: newName,

                oldIsManual: oldIsManual, newIsManual: false,

                onChanged: () =>
                {

                    _canvasViewModel?.RecalculateValidation();

                    SyncDeviceFromSelection();

                }));

        }

    }



    [RelayCommand]

    public void BatchAssignSequentialIps()

    {

        if (_canvasViewModel == null) return;

        var devices = _canvasViewModel.SelectedItems.OfType<DeviceItemViewModel>()

            .OrderBy(d => Math.Round(d.Y / 30.0)) // Row first

            .ThenBy(d => d.X)                    // Then column

            .ToList();



        if (devices.Count == 0) return;



        string startIp = string.IsNullOrWhiteSpace(BatchStartIp) ? "192.168.1.101" : BatchStartIp.Trim();

        if (!CanvasViewModel.IsValidIPv4(startIp))

        {

            startIp = "192.168.1.101";

        }



        var states = new List<BatchAssignDeviceNetworkCommand.DeviceNetworkState>();

        var r = new Random();



        for (int i = 0; i < devices.Count; i++)

        {

            var dev = devices[i];

            TryIncrementIp(startIp, i * Math.Max(1, BatchIpStep), out string assignedIp);



            string? newMac = !string.IsNullOrWhiteSpace(dev.MacAddress)

                ? dev.MacAddress

                : $"02:{r.Next(256):X2}:{r.Next(256):X2}:{r.Next(256):X2}:{r.Next(256):X2}:{r.Next(256):X2}";



            string newName = BatchRenameDevices

                ? $"{BatchNamePrefix.Trim()}{i + 1:D2}"

                : dev.Name;



            states.Add(new BatchAssignDeviceNetworkCommand.DeviceNetworkState(

                dev,

                OldIp: dev.IpAddress, NewIp: assignedIp,

                OldMac: dev.MacAddress, NewMac: newMac,

                OldDeviceId: dev.DeviceId, NewDeviceId: dev.DeviceId,

                OldName: dev.Name, NewName: newName,

                OldIsManual: dev.IsManualIp, NewIsManual: true));

        }



        var batchCmd = new BatchAssignDeviceNetworkCommand(states, onChanged: () =>
        {

            _canvasViewModel?.RecalculateValidation();

            UpdateSelection();

        });



        _canvasViewModel.UndoRedoService.ExecuteCommand(batchCmd);

        _canvasViewModel.RecalculateValidation();

        UpdateSelection();

    }



    [RelayCommand]

    public void BatchClearIps()

    {

        if (_canvasViewModel == null) return;

        var devices = _canvasViewModel.SelectedItems.OfType<DeviceItemViewModel>().ToList();

        if (devices.Count == 0) return;



        var states = devices.Select(dev => new BatchAssignDeviceNetworkCommand.DeviceNetworkState(

            dev,

            OldIp: dev.IpAddress, NewIp: null,

            OldMac: dev.MacAddress, NewMac: dev.MacAddress,

            OldDeviceId: dev.DeviceId, NewDeviceId: null,

            OldName: dev.Name, NewName: dev.Name,

            OldIsManual: dev.IsManualIp, NewIsManual: true)).ToList();



        var batchCmd = new BatchAssignDeviceNetworkCommand(states, onChanged: () =>
        {

            _canvasViewModel?.RecalculateValidation();

            UpdateSelection();

        });



        _canvasViewModel.UndoRedoService.ExecuteCommand(batchCmd);

        _canvasViewModel.RecalculateValidation();

        UpdateSelection();

    }



    public static bool TryIncrementIp(string baseIp, int step, out string nextIp)

    {

        nextIp = baseIp;

        if (!System.Net.IPAddress.TryParse(baseIp, out var ip)) return false;

        var bytes = ip.GetAddressBytes();

        if (bytes.Length != 4) return false;



        uint intAddress = ((uint)bytes[0] << 24) | ((uint)bytes[1] << 16) | ((uint)bytes[2] << 8) | bytes[3];

        intAddress += (uint)step;



        byte b0 = (byte)((intAddress >> 24) & 0xFF);

        byte b1 = (byte)((intAddress >> 16) & 0xFF);

        byte b2 = (byte)((intAddress >> 8) & 0xFF);

        byte b3 = (byte)(intAddress & 0xFF);



        nextIp = $"{b0}.{b1}.{b2}.{b3}";

        return true;

    }



    private string _initialFill = "#E0E7FF";

    private string _initialStroke = "#4338CA";



    private void SyncStyleFromSelection()

    {

        if (_canvasViewModel == null) return;

        var stylableItems = _canvasViewModel.SelectedItems.OfType<IStylableItemViewModel>().ToList();

        if (stylableItems.Count == 0) return;



        var firstItem = stylableItems[0];

        var shapeItems = stylableItems.OfType<ShapeItemViewModel>().ToList();

        bool hasFillable = shapeItems.Count == 0 || shapeItems.Any(s => s.ShapeType != ShapeType.Line && s.ShapeType != ShapeType.Bezier);

        IsFillApplicable = hasFillable;



        string curFill = !string.IsNullOrWhiteSpace(firstItem.Fill)

            ? firstItem.Fill

            : (firstItem is DeviceItemViewModel ? "#3B82F6" : "#F3F4F6");

        string curStroke = !string.IsNullOrWhiteSpace(firstItem.Stroke)

            ? firstItem.Stroke

            : (firstItem is DeviceItemViewModel ? "#1E40AF" : "#374151");



        _initialFill = curFill;

        _initialStroke = curStroke;

        CommonStrokeThickness = firstItem.StrokeThickness;



        _isInternalPropertyUpdate = true;

        try

        {

            var fillInfo = GradientHelper.AnalyzeBrush(curFill, defaultColor1: "#3B82F6", defaultColor2: "#1D4ED8");

            FillType = fillInfo.Type;

            FillColor1 = fillInfo.Color1;

            FillColor2 = fillInfo.Color2;

            FillAngle = fillInfo.Angle;

            FillBrushString = curFill;

            SelectedFillDirection = DirectionOptions.FirstOrDefault(d => Math.Abs(d.Angle - fillInfo.Angle) < 1) ?? DirectionOptions[0];



            var strokeInfo = GradientHelper.AnalyzeBrush(curStroke, defaultColor1: "#1E40AF", defaultColor2: "#06B6D4");

            StrokeType = strokeInfo.Type;

            StrokeColor1 = strokeInfo.Color1;

            StrokeColor2 = strokeInfo.Color2;

            StrokeAngle = strokeInfo.Angle;

            StrokeBrushString = curStroke;

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

    private void SyncSvgColorsFromSelection()
    {
        SvgColors.Clear();
        if (SelectedItem == null)
        {
            HasSvgColors = false;
            return;
        }

        string? stencilType = null;
        if (SelectedItem is DeviceItemViewModel dev)
            stencilType = dev.DeviceType;
        else if (SelectedItem is FurnitureItemViewModel furn)
            stencilType = furn.FurnitureType;

        if (string.IsNullOrWhiteSpace(stencilType) && !SelectedItem.HasCustomImage)
        {
            HasSvgColors = false;
            return;
        }

        var colors = ItemColorsProvider?.Invoke(stencilType, SelectedItem.CustomImagePath, SelectedItem.CustomImageData)
            ?? StencilColorsProvider?.Invoke(stencilType)
            ?? Array.Empty<string>();
        if (colors.Count == 0)
        {
            HasSvgColors = false;
            return;
        }

        var colorMap = SelectedItem.SvgColorMap;
        foreach (var c in colors)
        {
            string current = c;
            if (colorMap != null && TryGetMappedColor(colorMap, c, out var mapped))
            {
                current = mapped;
            }

            string label = GetColorDescription(c);
            SvgColors.Add(new SvgColorEntryViewModel(c, current, label));
        }

        HasSvgColors = SvgColors.Count > 0;
    }

    private void SyncSvgColorsMapState()
    {
        if (SelectedItem == null) return;
        var map = SelectedItem.SvgColorMap;
        foreach (var entry in SvgColors)
        {
            if (map != null && TryGetMappedColor(map, entry.OriginalColor, out var mapped))
            {
                entry.CurrentColor = mapped;
            }
            else
            {
                entry.CurrentColor = entry.OriginalColor;
            }
        }
    }

    public void ChangeSvgColor(string originalColor, string newColor)
    {
        if (SelectedItem == null || string.IsNullOrWhiteSpace(originalColor) || string.IsNullOrWhiteSpace(newColor))
            return;

        var oldMap = SelectedItem.SvgColorMap != null
            ? new Dictionary<string, string>(SelectedItem.SvgColorMap, StringComparer.OrdinalIgnoreCase)
            : null;

        var newMap = oldMap != null
            ? new Dictionary<string, string>(oldMap, StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        if (string.Equals(originalColor, newColor, StringComparison.OrdinalIgnoreCase))
        {
            newMap.Remove(originalColor);
        }
        else
        {
            newMap[originalColor] = newColor;
        }

        var finalNewMap = newMap.Count > 0 ? newMap : null;
        _canvasViewModel?.UndoRedoService.ExecuteCommand(new ModifySvgColorMapCommand(SelectedItem, oldMap, finalNewMap));

        var entry = SvgColors.FirstOrDefault(e => string.Equals(e.OriginalColor, originalColor, StringComparison.OrdinalIgnoreCase));
        if (entry != null)
        {
            entry.CurrentColor = finalNewMap != null && finalNewMap.TryGetValue(originalColor, out var m) ? m : originalColor;
        }
    }

    [RelayCommand]
    public void ResetSvgColor(SvgColorEntryViewModel? entry)
    {
        if (entry == null) return;
        ChangeSvgColor(entry.OriginalColor, entry.OriginalColor);
    }

    [RelayCommand]
    public void ResetAllSvgColors()
    {
        if (SelectedItem == null || SelectedItem.SvgColorMap == null) return;
        var oldMap = new Dictionary<string, string>(SelectedItem.SvgColorMap, StringComparer.OrdinalIgnoreCase);
        _canvasViewModel?.UndoRedoService.ExecuteCommand(new ModifySvgColorMapCommand(SelectedItem, oldMap, null));

        foreach (var entry in SvgColors)
        {
            entry.CurrentColor = entry.OriginalColor;
        }
    }

    private static bool TryGetMappedColor(IDictionary<string, string> map, string hex, out string mappedHex)
    {
        if (map.TryGetValue(hex, out mappedHex!)) return true;
        string norm = hex.StartsWith('#') ? hex.Substring(1) : hex;
        foreach (var kv in map)
        {
            string kNorm = kv.Key.StartsWith('#') ? kv.Key.Substring(1) : kv.Key;
            if (string.Equals(norm, kNorm, StringComparison.OrdinalIgnoreCase))
            {
                mappedHex = kv.Value;
                return true;
            }
        }
        mappedHex = string.Empty;
        return false;
    }

    private static string GetColorDescription(string hex)
    {
        if (string.IsNullOrWhiteSpace(hex)) return "Color";
        return hex.ToUpperInvariant() switch
        {
            "#1F2937" => "Screen Glass / Bezel",
            "#111827" => "Outline / Shadow",
            "#9CA3AF" => "Accents / Stand / Key Rows",
            "#E5E7EB" => "Light Surface / Keys / Cushion",
            "#374151" => "Chassis / Frame / Dark Border",
            "#4B5563" => "Stand Neck / Secondary Panel",
            "#6B7280" => "Grille / Details / Mid Gray",
            "#10B981" => "Status Indicator LED (Emerald)",
            "#06B6D4" => "Port / Tech Indicator (Cyan)",
            "#3B82F6" => "Primary Accent (Blue)",
            "#F59E0B" => "Alert / Amber",
            "#EF4444" => "Alert / Red",
            _ => GetLuminanceDescription(hex)
        };
    }

    private static string GetLuminanceDescription(string hex)
    {
        try
        {
            string clean = hex.TrimStart('#');
            if (clean.Length == 6 &&
                byte.TryParse(clean[..2], System.Globalization.NumberStyles.HexNumber, null, out byte r) &&
                byte.TryParse(clean[2..4], System.Globalization.NumberStyles.HexNumber, null, out byte g) &&
                byte.TryParse(clean[4..6], System.Globalization.NumberStyles.HexNumber, null, out byte b))
            {
                float lum = (0.299f * r + 0.587f * g + 0.114f * b) / 255f;
                if (lum > 0.85f) return $"Light Tone ({hex})";
                if (lum > 0.60f) return $"Mid-Light Tone ({hex})";
                if (lum > 0.35f) return $"Mid-Dark Tone ({hex})";
                if (lum > 0.15f) return $"Dark Tone ({hex})";
                return $"Deep Shadow ({hex})";
            }
        }
        catch { }
        return hex;
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



        var applicableItems = _canvasViewModel.SelectedItems.OfType<IStylableItemViewModel>()

            .Where(s => !(s is ShapeItemViewModel shape && (shape.ShapeType == ShapeType.Line || shape.ShapeType == ShapeType.Bezier)))

            .ToList();



        if (applicableItems.Count == 0) return;



        if (newFill != _initialFill)

        {

            string old = _initialFill;

            var changes = applicableItems

                .Select(s => (Item: s, OldFill: (string?)old, NewFill: (string?)newFill, OldStroke: (string?)null, NewStroke: (string?)null, OldThickness: (double?)null, NewThickness: (double?)null))

                .ToList();



            var cmd = new ModifyItemStyleCommand(changes, "Change Item Fill");

            _canvasViewModel.UndoRedoService.ExecuteCommand(cmd);

            _initialFill = newFill;

        }

    }



    public void ApplyStrokeChanges()

    {

        if (_canvasViewModel == null) return;

        string newStroke = BuildStrokeString();

        StrokeBrushString = newStroke;



        var applicableItems = _canvasViewModel.SelectedItems.OfType<IStylableItemViewModel>().ToList();

        if (applicableItems.Count == 0) return;



        if (newStroke != _initialStroke)

        {

            string old = _initialStroke;

            var changes = applicableItems

                .Select(s => (Item: s, OldFill: (string?)null, NewFill: (string?)null, OldStroke: (string?)old, NewStroke: (string?)newStroke, OldThickness: (double?)null, NewThickness: (double?)null))

                .ToList();



            var cmd = new ModifyItemStyleCommand(changes, "Change Item Stroke");

            _canvasViewModel.UndoRedoService.ExecuteCommand(cmd);

            _initialStroke = newStroke;

        }

    }

    [RelayCommand]
    public void AddBezierControlPoint()
    {
        if (SelectedItem is not ShapeItemViewModel shape || shape.ShapeType != ShapeType.Bezier) return;
        var oldPoints = shape.GetControlPoints().Select(p => new ControlPoint(p.X, p.Y)).ToList();

        double newX, newY;
        if (oldPoints.Count > 0)
        {
            var last = oldPoints[^1];
            newX = Math.Round((last.X + shape.EndX) / 2.0, 1);
            newY = Math.Round((last.Y + shape.EndY) / 2.0, 1);
        }
        else
        {
            newX = Math.Round((shape.StartX + shape.EndX) / 2.0, 1);
            newY = Math.Round((shape.StartY + shape.EndY) / 2.0, 1);
        }

        var newPoints = new List<ControlPoint>(oldPoints) { new ControlPoint(newX, newY) };
        var cmd = new ModifyBezierControlPointsCommand(
            shape,
            shape.StartX, shape.StartY, shape.EndX, shape.EndY, oldPoints,
            shape.StartX, shape.StartY, shape.EndX, shape.EndY, newPoints);
        if (_canvasViewModel?.UndoRedoService != null)
        {
            _canvasViewModel.UndoRedoService.ExecuteCommand(cmd);
        }
        else
        {
            cmd.Execute();
        }
        BezierControlPointsCount = shape.ControlPointCount;
        OnPropertyChanged(nameof(CanAddBezierControlPoint));
        OnPropertyChanged(nameof(CanRemoveBezierControlPoint));
    }

    [RelayCommand]
    public void RemoveBezierControlPoint()
    {
        if (SelectedItem is not ShapeItemViewModel shape || shape.ShapeType != ShapeType.Bezier || shape.ControlPointCount <= 1) return;
        var oldPoints = shape.GetControlPoints().Select(p => new ControlPoint(p.X, p.Y)).ToList();
        var newPoints = oldPoints.Take(oldPoints.Count - 1).ToList();
        var cmd = new ModifyBezierControlPointsCommand(
            shape,
            shape.StartX, shape.StartY, shape.EndX, shape.EndY, oldPoints,
            shape.StartX, shape.StartY, shape.EndX, shape.EndY, newPoints);
        if (_canvasViewModel?.UndoRedoService != null)
        {
            _canvasViewModel.UndoRedoService.ExecuteCommand(cmd);
        }
        else
        {
            cmd.Execute();
        }
        BezierControlPointsCount = shape.ControlPointCount;
        OnPropertyChanged(nameof(CanAddBezierControlPoint));
        OnPropertyChanged(nameof(CanRemoveBezierControlPoint));
    }

}

