using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LabScape.Core.Enums;
using LabScape.Core.Interfaces;
using LabScape.Core.Models;
using LabScape.ViewModels.Commands;
using LabScape.ViewModels.Items;

namespace LabScape.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly ILayoutRepository _layoutRepository;
    private readonly IDatabaseService _databaseService;
    private readonly IUndoRedoService _undoRedoService;
    private readonly Func<IUndoRedoService> _undoRedoFactory;
    private readonly CanvasViewModel _fallbackCanvas;

    public ObservableCollection<LayoutTabViewModel> Tabs { get; } = new();

    [ObservableProperty]
    private LayoutTabViewModel? _activeTab;

    public CanvasViewModel Canvas => ActiveTab?.Canvas ?? _fallbackCanvas;
    public CanvasViewModel? ActiveCanvas => ActiveTab?.Canvas;
    public IUndoRedoService? ActiveUndoRedo => ActiveTab?.UndoRedoService;

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
        IUndoRedoService undoRedoService,
        Func<IUndoRedoService>? undoRedoFactory = null)
    {
        _layoutRepository = layoutRepository;
        _databaseService = databaseService;
        _undoRedoService = undoRedoService;
        _undoRedoFactory = undoRedoFactory ?? (() => undoRedoService);

        _fallbackCanvas = new CanvasViewModel(_undoRedoService);
        Stencils = new StencilsViewModel();
        Properties = new PropertiesViewModel(_fallbackCanvas);

        // Initial default tab
        var initialTab = new LayoutTabViewModel("Untitled 1", _undoRedoFactory());
        Tabs.Add(initialTab);
        ActiveTab = initialTab;

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

    partial void OnActiveTabChanged(LayoutTabViewModel? oldValue, LayoutTabViewModel? newValue)
    {
        if (oldValue != null)
        {
            oldValue.IsActive = false;
            oldValue.UndoRedoService.StateChanged -= ActiveTab_StateChanged;
            oldValue.Canvas.Items.CollectionChanged -= ActiveTab_ItemsChanged;
        }

        if (newValue != null)
        {
            newValue.IsActive = true;
            newValue.UndoRedoService.StateChanged += ActiveTab_StateChanged;
            newValue.Canvas.Items.CollectionChanged += ActiveTab_ItemsChanged;

            Properties.AttachCanvas(newValue.Canvas);
            OnPropertyChanged(nameof(Canvas));
            OnPropertyChanged(nameof(ActiveCanvas));
            OnPropertyChanged(nameof(ActiveUndoRedo));

            SyncActiveTabState();
        }
    }

    private void ActiveTab_StateChanged(object? sender, EventArgs e)
    {
        SyncActiveTabState();
    }

    private void ActiveTab_ItemsChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        SyncActiveTabState();
    }

    private void SyncActiveTabState()
    {
        if (ActiveTab == null) return;

        CanUndo = ActiveTab.UndoRedoService.CanUndo;
        CanRedo = ActiveTab.UndoRedoService.CanRedo;
        DocumentTitle = ActiveTab.Title;
        CurrentFilePath = ActiveTab.FilePath;
        ValidationWarningCount = ActiveTab.ValidationWarningCount;
        ValidationErrorCount = ActiveTab.ValidationErrorCount;
    }

    // ==========================================
    // Tab Management Commands
    // ==========================================

    [RelayCommand]
    public void NewTab()
    {
        CreateNewTab();
    }

    public LayoutTabViewModel CreateNewTab(string title = "")
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            int nextNum = Tabs.Count + 1;
            title = $"Untitled {nextNum}";
        }

        var tab = new LayoutTabViewModel(title, _undoRedoFactory());
        Tabs.Add(tab);
        ActiveTab = tab;
        return tab;
    }

    [RelayCommand]
    public void CloseTab(LayoutTabViewModel? tab)
    {
        tab ??= ActiveTab;
        if (tab == null) return;

        int index = Tabs.IndexOf(tab);
        Tabs.Remove(tab);

        if (Tabs.Count == 0)
        {
            NewTab();
        }
        else if (ActiveTab == tab)
        {
            int newIndex = Math.Clamp(index, 0, Tabs.Count - 1);
            ActiveTab = Tabs[newIndex];
        }
    }

    [RelayCommand]
    public void CloseActiveTab()
    {
        CloseTab(ActiveTab);
    }

    [RelayCommand]
    public void CloseOtherTabs(LayoutTabViewModel? tab)
    {
        tab ??= ActiveTab;
        if (tab == null) return;
        var others = Tabs.Where(t => t != tab).ToList();
        foreach (var other in others)
        {
            Tabs.Remove(other);
        }
        ActiveTab = tab;
    }

    [RelayCommand]
    public void CloseAllTabs()
    {
        Tabs.Clear();
        NewTab();
    }

    [RelayCommand]
    public void NextTab()
    {
        if (Tabs.Count <= 1 || ActiveTab == null) return;
        int idx = Tabs.IndexOf(ActiveTab);
        ActiveTab = Tabs[(idx + 1) % Tabs.Count];
    }

    [RelayCommand]
    public void PreviousTab()
    {
        if (Tabs.Count <= 1 || ActiveTab == null) return;
        int idx = Tabs.IndexOf(ActiveTab);
        ActiveTab = Tabs[(idx - 1 + Tabs.Count) % Tabs.Count];
    }

    [RelayCommand]
    public void SelectTab(LayoutTabViewModel tab)
    {
        if (Tabs.Contains(tab))
        {
            ActiveTab = tab;
        }
    }

    // ==========================================
    // History & Tool Commands
    // ==========================================

    [RelayCommand]
    public void Undo()
    {
        ActiveTab?.UndoRedoService.Undo();
    }

    [RelayCommand]
    public void Redo()
    {
        ActiveTab?.UndoRedoService.Redo();
    }

    [RelayCommand]
    public void NewDocument()
    {
        NewTab();
    }

    [RelayCommand]
    public void SelectTool(string toolName)
    {
        ActiveTool = toolName;
    }

    // ==========================================
    // Document Serialization & Interchange
    // ==========================================

    public LayoutDocument BuildDocumentModel()
    {
        return ActiveTab?.BuildDocumentModel() ?? new LayoutDocument();
    }

    public void LoadDocumentModel(LayoutDocument doc)
    {
        LoadDocumentIntoTab(doc, string.Empty, doc.Metadata.Title);
    }

    public LayoutTabViewModel LoadDocumentIntoTab(LayoutDocument doc, string filePath = "", string title = "")
    {
        LayoutTabViewModel targetTab;

        // If current active tab is blank, reuse it
        if (ActiveTab != null && ActiveTab.Canvas.Items.Count == 0 && !ActiveTab.IsModified && string.IsNullOrEmpty(ActiveTab.FilePath))
        {
            targetTab = ActiveTab;
        }
        else
        {
            string tabTitle = !string.IsNullOrWhiteSpace(title)
                ? title
                : (!string.IsNullOrWhiteSpace(doc.Metadata.Title) ? doc.Metadata.Title : "Loaded Floorplan");
            targetTab = CreateNewTab(tabTitle);
        }

        targetTab.FilePath = filePath;
        targetTab.LoadDocumentModel(doc);
        if (!string.IsNullOrWhiteSpace(title))
        {
            targetTab.Title = title;
        }
        ActiveTab = targetTab;
        return targetTab;
    }

    public void ImportItemsIntoActiveCanvas(LayoutDocument doc)
    {
        if (ActiveTab == null) return;

        var itemsToAdd = new List<ItemViewModel>();
        int maxZ = ActiveTab.Canvas.Items.Count > 0 ? ActiveTab.Canvas.Items.Max(i => i.Z) : 0;

        foreach (var itemModel in doc.Items)
        {
            itemModel.Id = Guid.NewGuid().ToString();
            itemModel.X += 40;
            itemModel.Y += 40;
            itemModel.Z = ++maxZ;
            itemsToAdd.Add(ItemViewModel.FromModel(itemModel));
        }

        ActiveTab.Canvas.UndoRedoService.ExecuteCommand(new AddItemsBatchCommand(ActiveTab.Canvas.Items, itemsToAdd));
        ActiveTab.Canvas.SetSelection(itemsToAdd);
    }

    public string ExportJsonString()
    {
        var doc = BuildDocumentModel();
        return _layoutRepository.SerializeToJson(doc);
    }
}
