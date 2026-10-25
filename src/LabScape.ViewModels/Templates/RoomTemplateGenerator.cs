using LabScape.Core.Enums;
using LabScape.Core.Models;
using LabScape.ViewModels.Items;

namespace LabScape.ViewModels.Templates;

public class RoomTemplateResult
{
    public List<ShapeItemViewModel> Walls { get; } = new();
    public List<FurnitureItemViewModel> Desks { get; } = new();
    public List<DeviceItemViewModel> Pcs { get; } = new();
    public List<FurnitureItemViewModel> Chairs { get; } = new();
    public List<ItemViewModel> AllItems { get; } = new();
    public GroupItemViewModel? Group { get; set; }
    public double RoomWidth { get; set; }
    public double RoomHeight { get; set; }
}

public static class RoomTemplateGenerator
{
    public const double DeskW = 120.0;
    public const double DeskH = 70.0;
    public const double PcW = 80.0;
    public const double PcH = 80.0;
    public const double ChairW = 60.0;
    public const double ChairH = 60.0;
    public const double CornerMargin = 90.0;
    public const double WallOffset = 10.0;

    public static RoomTemplateResult Generate(RoomTemplateOptions options)
    {
        var result = new RoomTemplateResult();
        var activeWalls = options.GetActiveWalls();
        var distribution = options.CalculatePcDistribution();

        int northCount = distribution.GetValueOrDefault(WallSide.North, 0);
        int southCount = distribution.GetValueOrDefault(WallSide.South, 0);
        int eastCount = distribution.GetValueOrDefault(WallSide.East, 0);
        int westCount = distribution.GetValueOrDefault(WallSide.West, 0);

        int maxHorizCount = Math.Max(northCount, southCount);
        int maxVertCount = Math.Max(eastCount, westCount);

        double gap = Math.Max(0.0, options.DeskSpacing);

        double horizSpan = maxHorizCount > 0 ? (maxHorizCount * DeskW + (maxHorizCount - 1) * gap) : 0;
        double vertSpan = maxVertCount > 0 ? (maxVertCount * DeskW + (maxVertCount - 1) * gap) : 0;

        // Calculate minimum room dimensions based on wall layout and open aisles
        double minWidth = (activeWalls.Contains(WallSide.West) && activeWalls.Contains(WallSide.East)) ? 460.0 : 360.0;
        double minHeight = (activeWalls.Contains(WallSide.North) && activeWalls.Contains(WallSide.South)) ? 460.0 : 320.0;

        double calculatedW = Math.Max(minWidth, horizSpan + (activeWalls.Count > 1 ? CornerMargin * 2 : 40.0));
        double calculatedH = Math.Max(minHeight, vertSpan + (activeWalls.Count > 1 ? CornerMargin * 2 : 40.0));

        // Snap room dimensions to 20px grid
        double roomWidth = Math.Ceiling(calculatedW / 20.0) * 20.0;
        double roomHeight = Math.Ceiling(calculatedH / 20.0) * 20.0;

        result.RoomWidth = roomWidth;
        result.RoomHeight = roomHeight;

        double x0 = options.OriginX;
        double y0 = options.OriginY;

        // 1. Generate Boundary Walls (Individual Line items - NEVER a rectangle, giving full autonomy for doors/openings)
        if (activeWalls.Contains(WallSide.North))
        {
            var northWall = CreateWallLine("North Wall", x0, y0, x0 + roomWidth, y0, options.WallThickness, options.WallStroke);
            result.Walls.Add(northWall);
        }

        if (activeWalls.Contains(WallSide.East))
        {
            var eastWall = CreateWallLine("East Wall", x0 + roomWidth, y0, x0 + roomWidth, y0 + roomHeight, options.WallThickness, options.WallStroke);
            result.Walls.Add(eastWall);
        }

        if (activeWalls.Contains(WallSide.South))
        {
            var southWall = CreateWallLine("South Wall", x0, y0 + roomHeight, x0 + roomWidth, y0 + roomHeight, options.WallThickness, options.WallStroke);
            result.Walls.Add(southWall);
        }

        if (activeWalls.Contains(WallSide.West))
        {
            var westWall = CreateWallLine("West Wall", x0, y0, x0, y0 + roomHeight, options.WallThickness, options.WallStroke);
            result.Walls.Add(westWall);
        }

        int pcIndex = 1;

        // 2. Populate North Wall
        if (activeWalls.Contains(WallSide.North) && northCount > 0)
        {
            double span = northCount * DeskW + (northCount - 1) * gap;
            double startX = x0 + (roomWidth - span) / 2.0;

            for (int i = 0; i < northCount; i++)
            {
                double deskX = Math.Round(startX + i * (DeskW + gap), 1);
                double deskY = Math.Round(y0 + WallOffset, 1);

                if (options.IncludeDesks)
                {
                    var desk = CreateDesk($"Desk-{pcIndex:D2}", deskX, deskY, 0.0);
                    result.Desks.Add(desk);
                }

                double pcX = Math.Round(deskX + (DeskW - PcW) / 2.0, 1);
                double pcY = Math.Round(deskY + (DeskH - PcH) / 2.0, 1);
                var pc = CreatePc($"PC-{pcIndex:D2}", pcX, pcY, 0.0);
                result.Pcs.Add(pc);

                if (options.IncludeChairs)
                {
                    double chairX = Math.Round(deskX + (DeskW - ChairW) / 2.0, 1);
                    double chairY = Math.Round(deskY + DeskH - 20.0, 1);
                    var chair = CreateChair($"Chair-{pcIndex:D2}", chairX, chairY, 180.0);
                    result.Chairs.Add(chair);
                }

                pcIndex++;
            }
        }

        // 3. Populate East Wall
        if (activeWalls.Contains(WallSide.East) && eastCount > 0)
        {
            double span = eastCount * DeskW + (eastCount - 1) * gap;
            double startY = y0 + (roomHeight - span) / 2.0;

            for (int i = 0; i < eastCount; i++)
            {
                double centerY = startY + i * (DeskW + gap) + DeskW / 2.0;
                double centerX = x0 + roomWidth - WallOffset - DeskH / 2.0;

                double deskX = Math.Round(centerX - DeskW / 2.0, 1);
                double deskY = Math.Round(centerY - DeskH / 2.0, 1);

                if (options.IncludeDesks)
                {
                    var desk = CreateDesk($"Desk-{pcIndex:D2}", deskX, deskY, 90.0);
                    result.Desks.Add(desk);
                }

                double pcX = Math.Round(centerX - PcW / 2.0, 1);
                double pcY = Math.Round(centerY - PcH / 2.0, 1);
                var pc = CreatePc($"PC-{pcIndex:D2}", pcX, pcY, 90.0);
                result.Pcs.Add(pc);

                if (options.IncludeChairs)
                {
                    double chairVisualRight = x0 + roomWidth - WallOffset - DeskH + 20.0;
                    double chairX = Math.Round(chairVisualRight - ChairW, 1);
                    double chairY = Math.Round(centerY - ChairH / 2.0, 1);
                    var chair = CreateChair($"Chair-{pcIndex:D2}", chairX, chairY, 270.0);
                    result.Chairs.Add(chair);
                }

                pcIndex++;
            }
        }

        // 4. Populate South Wall
        if (activeWalls.Contains(WallSide.South) && southCount > 0)
        {
            double span = southCount * DeskW + (southCount - 1) * gap;
            double startX = x0 + (roomWidth - span) / 2.0;

            for (int i = 0; i < southCount; i++)
            {
                double deskX = Math.Round(startX + i * (DeskW + gap), 1);
                double deskY = Math.Round(y0 + roomHeight - WallOffset - DeskH, 1);

                if (options.IncludeDesks)
                {
                    var desk = CreateDesk($"Desk-{pcIndex:D2}", deskX, deskY, 180.0);
                    result.Desks.Add(desk);
                }

                double pcX = Math.Round(deskX + (DeskW - PcW) / 2.0, 1);
                double pcY = Math.Round(deskY + (DeskH - PcH) / 2.0, 1);
                var pc = CreatePc($"PC-{pcIndex:D2}", pcX, pcY, 180.0);
                result.Pcs.Add(pc);

                if (options.IncludeChairs)
                {
                    double chairX = Math.Round(deskX + (DeskW - ChairW) / 2.0, 1);
                    double chairY = Math.Round(deskY - ChairH + 20.0, 1);
                    var chair = CreateChair($"Chair-{pcIndex:D2}", chairX, chairY, 0.0);
                    result.Chairs.Add(chair);
                }

                pcIndex++;
            }
        }

        // 5. Populate West Wall
        if (activeWalls.Contains(WallSide.West) && westCount > 0)
        {
            double span = westCount * DeskW + (westCount - 1) * gap;
            double startY = y0 + (roomHeight - span) / 2.0;

            for (int i = 0; i < westCount; i++)
            {
                double centerY = startY + i * (DeskW + gap) + DeskW / 2.0;
                double centerX = x0 + WallOffset + DeskH / 2.0;

                double deskX = Math.Round(centerX - DeskW / 2.0, 1);
                double deskY = Math.Round(centerY - DeskH / 2.0, 1);

                if (options.IncludeDesks)
                {
                    var desk = CreateDesk($"Desk-{pcIndex:D2}", deskX, deskY, 270.0);
                    result.Desks.Add(desk);
                }

                double pcX = Math.Round(centerX - PcW / 2.0, 1);
                double pcY = Math.Round(centerY - PcH / 2.0, 1);
                var pc = CreatePc($"PC-{pcIndex:D2}", pcX, pcY, 270.0);
                result.Pcs.Add(pc);

                if (options.IncludeChairs)
                {
                    double chairVisualLeft = x0 + WallOffset + DeskH - 20.0;
                    double chairX = Math.Round(chairVisualLeft, 1);
                    double chairY = Math.Round(centerY - ChairH / 2.0, 1);
                    var chair = CreateChair($"Chair-{pcIndex:D2}", chairX, chairY, 90.0);
                    result.Chairs.Add(chair);
                }

                pcIndex++;
            }
        }

        // Order elements cleanly: Walls, Chairs, Desks, PCs
        result.AllItems.AddRange(result.Walls);
        result.AllItems.AddRange(result.Chairs);
        result.AllItems.AddRange(result.Desks);
        result.AllItems.AddRange(result.Pcs);

        // Assign incremental Z indices
        int z = 1;
        foreach (var wall in result.Walls) wall.Z = z++;
        foreach (var chair in result.Chairs) chair.Z = z++;
        foreach (var desk in result.Desks) desk.Z = z++;
        foreach (var pc in result.Pcs) pc.Z = z++;

        // 6. Optional Grouping
        if (options.GroupElements && result.AllItems.Count > 0)
        {
            var groupModel = new GroupItem
            {
                Name = $"Room Template ({options.Sides} Sides, {options.PcCount} PCs)"
            };
            var groupVm = new GroupItemViewModel(groupModel);
            foreach (var item in result.AllItems)
            {
                groupVm.AddChild(item);
            }
            groupVm.Z = z++;
            result.Group = groupVm;
        }

        return result;
    }

    public static ShapeItemViewModel CreateWallLine(string name, double x0, double y0, double x1, double y1, double thickness, string stroke)
    {
        double dx = x1 - x0;
        double dy = y1 - y0;
        double len = Math.Max(10.0, Math.Sqrt(dx * dx + dy * dy));
        double angleDeg = Math.Atan2(dy, dx) * 180.0 / Math.PI;
        if (angleDeg < 0) angleDeg += 360.0;

        double centerX = (x0 + x1) / 2.0;
        double centerY = (y0 + y1) / 2.0;
        const double h = 20.0;

        var shape = new ShapeItem
        {
            Name = name,
            ShapeType = ShapeType.Line,
            Width = Math.Round(len, 1),
            Height = h,
            X = Math.Round(centerX - len / 2.0, 1),
            Y = Math.Round(centerY - h / 2.0, 1),
            Rotation = Math.Round(angleDeg, 1),
            Stroke = stroke,
            StrokeThickness = thickness
        };
        return new ShapeItemViewModel(shape);
    }

    public static FurnitureItemViewModel CreateDesk(string name, double x, double y, double rotation)
    {
        var item = new FurnitureItem
        {
            Name = name,
            FurnitureType = "desk",
            Width = DeskW,
            Height = DeskH,
            X = x,
            Y = y,
            Rotation = rotation
        };
        return new FurnitureItemViewModel(item);
    }

    public static DeviceItemViewModel CreatePc(string name, double x, double y, double rotation)
    {
        var item = new DeviceItem
        {
            Name = name,
            DeviceType = "desktop_pc",
            Width = PcW,
            Height = PcH,
            X = x,
            Y = y,
            Rotation = rotation,
            ValidationStatus = ValidationStatus.Unassigned
        };
        return new DeviceItemViewModel(item);
    }

    public static FurnitureItemViewModel CreateChair(string name, double x, double y, double rotation)
    {
        var item = new FurnitureItem
        {
            Name = name,
            FurnitureType = "office_chair",
            Width = ChairW,
            Height = ChairH,
            X = x,
            Y = y,
            Rotation = rotation
        };
        return new FurnitureItemViewModel(item);
    }
}
