using LabScape.Core.Enums;

namespace LabScape.ViewModels.Templates;

public class RoomTemplateOptions
{
    public int Sides { get; set; } = 4;
    public WallSide OpenSide { get; set; } = WallSide.South;
    public string CornerOption { get; set; } = "NorthWest";
    public WallSide SingleSide { get; set; } = WallSide.North;

    public int PcCount { get; set; } = 12;
    public bool IncludeDesks { get; set; } = true;
    public bool IncludeChairs { get; set; } = true;
    public bool GroupElements { get; set; } = true;

    public double OriginX { get; set; } = 100.0;
    public double OriginY { get; set; } = 100.0;
    public double DeskSpacing { get; set; } = 20.0;
    public string WallStroke { get; set; } = "#374151";
    public double WallThickness { get; set; } = 4.0;

    public List<WallSide> GetActiveWalls()
    {
        return Sides switch
        {
            1 => new List<WallSide> { SingleSide },
            2 => CornerOption switch
            {
                "NorthEast" => new List<WallSide> { WallSide.North, WallSide.East },
                "SouthWest" => new List<WallSide> { WallSide.South, WallSide.West },
                "SouthEast" => new List<WallSide> { WallSide.South, WallSide.East },
                "NorthSouth" or "SouthNorth" => new List<WallSide> { WallSide.North, WallSide.South },
                "EastWest" or "WestEast" => new List<WallSide> { WallSide.West, WallSide.East },
                _ => new List<WallSide> { WallSide.North, WallSide.West }
            },
            3 => OpenSide switch
            {
                WallSide.North => new List<WallSide> { WallSide.East, WallSide.South, WallSide.West },
                WallSide.East => new List<WallSide> { WallSide.North, WallSide.South, WallSide.West },
                WallSide.West => new List<WallSide> { WallSide.North, WallSide.East, WallSide.South },
                _ => new List<WallSide> { WallSide.North, WallSide.East, WallSide.West }
            },
            _ => new List<WallSide> { WallSide.North, WallSide.East, WallSide.South, WallSide.West }
        };
    }

    public Dictionary<WallSide, int> CalculatePcDistribution()
    {
        var walls = GetActiveWalls();
        var result = new Dictionary<WallSide, int>();
        if (walls.Count == 0) return result;

        int baseCount = PcCount / walls.Count;
        int remainder = PcCount % walls.Count;

        for (int i = 0; i < walls.Count; i++)
        {
            result[walls[i]] = baseCount + (i < remainder ? 1 : 0);
        }

        return result;
    }
}
