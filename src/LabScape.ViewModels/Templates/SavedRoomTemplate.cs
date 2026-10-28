using System.Text.Json.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;

namespace LabScape.ViewModels.Templates;

public partial class SavedRoomTemplate : ObservableObject
{
    [ObservableProperty]
    private string _id = Guid.NewGuid().ToString("N");

    [ObservableProperty]
    private string _name = "Room Template";

    [ObservableProperty]
    private string _description = "";

    [ObservableProperty]
    private bool _isBuiltIn = false;

    [ObservableProperty]
    private RoomTemplateOptions _options = new();

    [ObservableProperty]
    private DateTime _createdAt = DateTime.UtcNow;

    [JsonIgnore]
    public string SummaryBadge => $"{Options.PcCount} PCs • {Options.Sides} Sides";

    [JsonIgnore]
    public string SummaryDimensions
    {
        get
        {
            var res = RoomTemplateGenerator.Generate(Options);
            return $"{res.RoomWidth:0} × {res.RoomHeight:0} px";
        }
    }

    [JsonIgnore]
    public string SummaryWalls
    {
        get
        {
            var res = RoomTemplateGenerator.Generate(Options);
            return $"{res.Walls.Count} Line segments";
        }
    }
}
