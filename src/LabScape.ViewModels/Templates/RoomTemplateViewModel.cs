using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LabScape.Core.Enums;

namespace LabScape.ViewModels.Templates;

public partial class RoomTemplateViewModel : ObservableObject
{
    private readonly Func<CanvasViewModel?> _getActiveCanvas;

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

    public RoomTemplateViewModel(Func<CanvasViewModel?> getActiveCanvas)
    {
        _getActiveCanvas = getActiveCanvas;
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
        return new RoomTemplateOptions
        {
            Sides = Sides,
            OpenSide = OpenSide,
            CornerOption = CornerOption,
            SingleSide = SingleSide,
            PcCount = PcCount,
            IncludeDesks = IncludeDesks,
            IncludeChairs = IncludeChairs,
            GroupElements = GroupElements,
            DeskSpacing = DeskSpacing,
            OriginX = originX ?? 100.0,
            OriginY = originY ?? 100.0
        };
    }

    [RelayCommand]
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
