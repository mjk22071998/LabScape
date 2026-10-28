using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LabScape.Core.Enums;

namespace LabScape.ViewModels.Templates;

public partial class RoomTemplateViewModel : ObservableObject
{
    private readonly Func<CanvasViewModel?> _getActiveCanvas;

    public ObservableCollection<SavedRoomTemplate> SavedTemplates { get; } = new();

    [ObservableProperty]
    private SavedRoomTemplate? _selectedTemplate;

    [ObservableProperty]
    private int _sides = 4;

    [ObservableProperty]
    private WallSide _openSide = WallSide.South;

    [ObservableProperty]
    private string _cornerOption = "NorthWest";

    [ObservableProperty]
    private WallSide _singleSide = WallSide.North;

    [ObservableProperty]
    private int _pcCount = 12;

    [ObservableProperty]
    private bool _includeDesks = true;

    [ObservableProperty]
    private bool _includeChairs = true;

    [ObservableProperty]
    private bool _groupElements = true;

    [ObservableProperty]
    private double _deskSpacing = 20.0;

    [ObservableProperty]
    private string _summaryDimensions = "580 × 580 px";

    [ObservableProperty]
    private string _summaryWalls = "4 Line segments (open autonomy)";

    [ObservableProperty]
    private string _summaryDistribution = "North: 3 | East: 3 | South: 3 | West: 3";

    [ObservableProperty]
    private bool _is3SidesVisible = false;

    [ObservableProperty]
    private bool _is2SidesVisible = false;

    [ObservableProperty]
    private bool _is1SideVisible = false;

    public event Action? RequestCreateNewTemplate;

    public RoomTemplateViewModel(Func<CanvasViewModel?> getActiveCanvas, bool autoLoadTemplates = true)
    {
        _getActiveCanvas = getActiveCanvas;

        if (autoLoadTemplates)
        {
            LoadTemplates();
        }

        UpdateSummary();
    }

    public void LoadTemplates()
    {
        SavedTemplates.Clear();
        var templates = RoomTemplatePersistence.LoadTemplates();
        foreach (var t in templates)
        {
            SavedTemplates.Add(t);
        }

        if (SelectedTemplate == null && SavedTemplates.Count > 0)
        {
            SelectedTemplate = SavedTemplates[0];
        }
    }

    public void SaveTemplates()
    {
        RoomTemplatePersistence.SaveTemplates(SavedTemplates);
    }

    partial void OnSelectedTemplateChanged(SavedRoomTemplate? value)
    {
        if (value == null) return;

        var opt = value.Options;
        Sides = opt.Sides;
        OpenSide = opt.OpenSide;
        CornerOption = opt.CornerOption;
        SingleSide = opt.SingleSide;
        PcCount = opt.PcCount;
        IncludeDesks = opt.IncludeDesks;
        IncludeChairs = opt.IncludeChairs;
        GroupElements = opt.GroupElements;
        DeskSpacing = opt.DeskSpacing;

        UpdateSummary();
    }

    partial void OnSidesChanged(int value)
    {
        Is3SidesVisible = value == 3;
        Is2SidesVisible = value == 2;
        Is1SideVisible = value == 1;
        UpdateSummary();
    }

    partial void OnOpenSideChanged(WallSide value) => UpdateSummary();
    partial void OnCornerOptionChanged(string value) => UpdateSummary();
    partial void OnSingleSideChanged(WallSide value) => UpdateSummary();
    partial void OnPcCountChanged(int value) => UpdateSummary();
    partial void OnIncludeDesksChanged(bool value) => UpdateSummary();
    partial void OnIncludeChairsChanged(bool value) => UpdateSummary();
    partial void OnGroupElementsChanged(bool value) => UpdateSummary();
    partial void OnDeskSpacingChanged(double value) => UpdateSummary();

    public void UpdateSummary()
    {
        var currentOpts = BuildOptions(0, 0);
        var result = RoomTemplateGenerator.Generate(currentOpts);

        SummaryDimensions = $"{result.RoomWidth:0} × {result.RoomHeight:0} px";
        SummaryWalls = $"{result.Walls.Count} Line segments (open autonomy)";

        var dist = currentOpts.CalculatePcDistribution();
        var parts = new List<string>();
        foreach (var (side, count) in dist)
        {
            parts.Add($"{side}: {count}");
        }
        SummaryDistribution = parts.Count > 0 ? string.Join(" | ", parts) : "None";
    }

    public RoomTemplateOptions BuildOptions(double? originX = null, double? originY = null)
    {
        int sides = Sides;
        if (sides < 1 || sides > 4) sides = 4;

        return new RoomTemplateOptions
        {
            Sides = sides,
            OpenSide = OpenSide,
            CornerOption = CornerOption,
            SingleSide = SingleSide,
            PcCount = Math.Max(1, PcCount),
            IncludeDesks = IncludeDesks,
            IncludeChairs = IncludeChairs,
            GroupElements = GroupElements,
            DeskSpacing = DeskSpacing,
            OriginX = originX ?? 100.0,
            OriginY = originY ?? 100.0
        };
    }

    public bool CanGenerateRoom => true;

    [RelayCommand(CanExecute = nameof(CanGenerateRoom))]
    public void GenerateRoom()
    {
        var canvas = _getActiveCanvas();
        if (canvas == null) return;

        double originX = Math.Max(60.0, -canvas.PanX + 120.0);
        double originY = Math.Max(60.0, -canvas.PanY + 120.0);
        if (canvas.SnapToGrid)
        {
            originX = Math.Round(originX / canvas.GridSize) * canvas.GridSize;
            originY = Math.Round(originY / canvas.GridSize) * canvas.GridSize;
        }

        var options = BuildOptions(originX, originY);
        canvas.AddRoomTemplate(options);
    }

    [RelayCommand]
    public void InsertTemplate(SavedRoomTemplate? template)
    {
        var target = template ?? SelectedTemplate;
        if (target == null) return;

        var canvas = _getActiveCanvas();
        if (canvas == null) return;

        double originX = Math.Max(60.0, -canvas.PanX + 120.0);
        double originY = Math.Max(60.0, -canvas.PanY + 120.0);
        if (canvas.SnapToGrid)
        {
            originX = Math.Round(originX / canvas.GridSize) * canvas.GridSize;
            originY = Math.Round(originY / canvas.GridSize) * canvas.GridSize;
        }

        var opts = new RoomTemplateOptions
        {
            Sides = target.Options.Sides,
            OpenSide = target.Options.OpenSide,
            CornerOption = target.Options.CornerOption,
            SingleSide = target.Options.SingleSide,
            PcCount = target.Options.PcCount,
            IncludeDesks = target.Options.IncludeDesks,
            IncludeChairs = target.Options.IncludeChairs,
            GroupElements = target.Options.GroupElements,
            DeskSpacing = target.Options.DeskSpacing,
            OriginX = originX,
            OriginY = originY
        };

        canvas.AddRoomTemplate(opts);
    }

    [RelayCommand]
    public void CreateNewTemplate()
    {
        RequestCreateNewTemplate?.Invoke();
    }

    public void AddSavedTemplate(SavedRoomTemplate template, bool insertImmediately = false)
    {
        SavedTemplates.Add(template);
        SelectedTemplate = template;
        SaveTemplates();

        if (insertImmediately)
        {
            InsertTemplate(template);
        }
    }

    [RelayCommand]
    public void DeleteTemplate(SavedRoomTemplate? template)
    {
        if (template == null) return;

        SavedTemplates.Remove(template);
        if (SelectedTemplate == template)
        {
            SelectedTemplate = SavedTemplates.FirstOrDefault();
        }
        SaveTemplates();
    }

    [RelayCommand]
    public void ResetDefaults()
    {
        Sides = 4;
        OpenSide = WallSide.South;
        CornerOption = "NorthWest";
        SingleSide = WallSide.North;
        PcCount = 12;
        IncludeDesks = true;
        IncludeChairs = true;
        GroupElements = true;
        DeskSpacing = 20.0;
        UpdateSummary();
    }
}
