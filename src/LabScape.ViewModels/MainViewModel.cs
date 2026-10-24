using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LabScape.Core.Enums;
using LabScape.Core.Interfaces;
using LabScape.Core.Models;
using LabScape.ViewModels.Items;

namespace LabScape.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly ILayoutRepository _layoutRepository;
    private readonly IDatabaseService _databaseService;
    private readonly IUndoRedoService _undoRedoService;

    public CanvasViewModel Canvas { get; }
    public StencilsViewModel Stencils { get; }
    public PropertiesViewModel Properties { get; }

    [ObservableProperty]
    private string _currentFilePath = string.Empty;

    [ObservableProperty]
    private string _documentTitle = "Untitled Floorplan";

    [ObservableProperty]
    private bool _isDatabaseConnected;

    [ObservableProperty]
    private string _databaseStatusText = "Database: Disconnected";

    [ObservableProperty]
    private string _activeTool = "Select"; // "Select", "Pan"

    [ObservableProperty]
    private bool _canUndo;

    [ObservableProperty]
    private bool _canRedo;

    [ObservableProperty]
    private int _validationWarningCount;

    [ObservableProperty]
    private int _validationErrorCount;

    public MainViewModel(
        ILayoutRepository layoutRepository,
        IDatabaseService databaseService,
        IUndoRedoService undoRedoService)
    {
        _layoutRepository = layoutRepository;
        _databaseService = databaseService;
        _undoRedoService = undoRedoService;

        Canvas = new CanvasViewModel(_undoRedoService);
        Stencils = new StencilsViewModel();
        Properties = new PropertiesViewModel(Canvas);

        _undoRedoService.StateChanged += (s, e) =>
        {
            CanUndo = _undoRedoService.CanUndo;
            CanRedo = _undoRedoService.CanRedo;
            UpdateValidationCounters();
        };

        Canvas.Items.CollectionChanged += (s, e) => UpdateValidationCounters();
        UpdateValidationCounters();

        // Sample initial devices for searchable dropdown
        Properties.SetInventoryDevices(new List<DatabaseDevice>
        {
            new DatabaseDevice { DeviceId = "DEV-101", DisplayName = "CAD-Workstation-01", PrimaryIp = "192.168.10.101", MacAddress = "00:1A:2B:3C:4D:5E" },
            new DatabaseDevice { DeviceId = "DEV-102", DisplayName = "CAD-Workstation-02", PrimaryIp = "192.168.10.102", MacAddress = "00:1A:2B:3C:4D:5F" },
            new DatabaseDevice { DeviceId = "DEV-103", DisplayName = "Dev-Laptop-Engineering", PrimaryIp = "192.168.10.103", MacAddress = "00:1A:2B:3C:4D:60" },
            new DatabaseDevice { DeviceId = "DEV-104", DisplayName = "Rack-Server-Primary", PrimaryIp = "192.168.10.104", MacAddress = "00:1A:2B:3C:4D:61" },
            new DatabaseDevice { DeviceId = "DEV-105", DisplayName = "Rack-Server-Backup", PrimaryIp = "192.168.10.105", MacAddress = "00:1A:2B:3C:4D:62" },
            new DatabaseDevice { DeviceId = "DEV-106", DisplayName = "Reception-PC", PrimaryIp = "192.168.10.106", MacAddress = "00:1A:2B:3C:4D:63" },
            new DatabaseDevice { DeviceId = "DEV-107", DisplayName = "Conference-AV-Unit", PrimaryIp = "192.168.10.107", MacAddress = "00:1A:2B:3C:4D:64" }
        });
    }

    private void UpdateValidationCounters()
    {
        Canvas.RecalculateValidation();
        var allDevices = GetAllDeviceItems(Canvas.Items).ToList();
        ValidationWarningCount = allDevices.Count(d => d.ValidationStatus == ValidationStatus.Unassigned);
        ValidationErrorCount = allDevices.Count(d => d.ValidationStatus == ValidationStatus.DuplicateIp);
    }

    private IEnumerable<DeviceItemViewModel> GetAllDeviceItems(IEnumerable<ItemViewModel> items)
    {
        foreach (var itm in items)
        {
            if (itm is DeviceItemViewModel dev)
                yield return dev;
            else if (itm is GroupItemViewModel grp)
            {
                foreach (var childDev in GetAllDeviceItems(grp.Children))
                    yield return childDev;
            }
        }
    }

    [RelayCommand]
    public void Undo()
    {
        _undoRedoService.Undo();
    }

    [RelayCommand]
    public void Redo()
    {
        _undoRedoService.Redo();
    }

    [RelayCommand]
    public void NewDocument()
    {
        Canvas.Items.Clear();
        Canvas.ClearSelection();
        _undoRedoService.Clear();
        CurrentFilePath = string.Empty;
        DocumentTitle = "Untitled Floorplan";
    }

    [RelayCommand]
    public void SelectTool(string toolName)
    {
        ActiveTool = toolName;
    }

    public LayoutDocument BuildDocumentModel()
    {
        var doc = new LayoutDocument
        {
            Metadata = new DocumentMetadata
            {
                Title = DocumentTitle,
                CanvasWidth = Canvas.CanvasWidth,
                CanvasHeight = Canvas.CanvasHeight,
                GridSize = Canvas.GridSize
            }
        };

        foreach (var itemVm in Canvas.Items)
        {
            doc.Items.Add(itemVm.Model);
        }

        return doc;
    }

    public void LoadDocumentModel(LayoutDocument doc)
    {
        Canvas.Items.Clear();
        Canvas.ClearSelection();
        _undoRedoService.Clear();

        Canvas.CanvasWidth = doc.Metadata.CanvasWidth;
        Canvas.CanvasHeight = doc.Metadata.CanvasHeight;
        Canvas.GridSize = doc.Metadata.GridSize;
        DocumentTitle = doc.Metadata.Title;

        foreach (var itemModel in doc.Items)
        {
            Canvas.Items.Add(ItemViewModel.FromModel(itemModel));
        }

        UpdateValidationCounters();
    }

    public string ExportJsonString()
    {
        var doc = BuildDocumentModel();
        return _layoutRepository.SerializeToJson(doc);
    }
}
