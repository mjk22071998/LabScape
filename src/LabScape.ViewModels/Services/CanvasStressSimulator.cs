using System.Diagnostics;
using LabScape.Core.Enums;
using LabScape.Core.Models;
using LabScape.ViewModels.Commands;
using LabScape.ViewModels.Items;

namespace LabScape.ViewModels.Services;

public class SimulationBenchmarkResult
{
    public int TotalItems { get; set; }
    public int DeviceCount { get; set; }
    public int FurnitureCount { get; set; }
    public int ShapeCount { get; set; }
    public int LineCount { get; set; }
    public int BezierCount { get; set; }

    public double GenerationTimeMs { get; set; }
    public double InsertionTimeMs { get; set; }
    public double TotalElapsedMs => GenerationTimeMs + InsertionTimeMs;

    public double MemoryBeforeMb { get; set; }
    public double MemoryAfterMb { get; set; }
    public double MemoryDeltaMb => Math.Max(0, MemoryAfterMb - MemoryBeforeMb);
    public double WorkingSetMb { get; set; }
}

public static class CanvasStressSimulator
{
    private static readonly string[] DeviceTypes = { "desktop_pc", "laptop", "screen", "server_rack", "switch", "printer" };
    private static readonly string[] FurnitureTypes = { "desk", "office_chair", "conference_table_8", "sofa_3_seater", "bookcase" };
    private static readonly ShapeType[] GeometricShapes = { ShapeType.Rectangle, ShapeType.RoundedRectangle, ShapeType.Circle, ShapeType.Triangle, ShapeType.RoundedCornerTriangle, ShapeType.SemiCircle, ShapeType.Donut };

    private static readonly string[] ColorPalette =
    {
        "#2563EB", "#3B82F6", "#60A5FA", // Blue
        "#4F46E5", "#6366F1", "#818CF8", // Indigo
        "#7C3AED", "#8B5CF6", "#A78BFA", // Purple
        "#059669", "#10B981", "#34D399", // Emerald
        "#D97706", "#F59E0B", "#FBBF24", // Amber
        "#DC2626", "#EF4444", "#F87171", // Red
        "#475569", "#64748B", "#94A3B8"  // Slate
    };

    private static readonly string[] GradientPalette =
    {
        "linear-gradient(45deg, #2563EB, #7C3AED)",
        "linear-gradient(90deg, #4F46E5, #EC4899)",
        "linear-gradient(135deg, #059669, #10B981)",
        "linear-gradient(0deg, #D97706, #EF4444)",
        "linear-gradient(60deg, #6366F1, #38BDF8)",
        "radial-gradient(circle, #7C3AED, #3B82F6)",
        "radial-gradient(circle, #EC4899, #F59E0B)"
    };

    /// <summary>
    /// Generates exactly 1,000 items with an authentic mix of:
    /// - 300 Devices (PCs, Laptops, Screens, Servers, Switches, Printers)
    /// - 150 Furniture (Desks, Chairs, Tables, Sofas, Bookcases)
    /// - 250 Shapes (Rectangles, Rounded Rectangles, Circles, Triangles, Rounded Triangles, Semi-Circles, Donuts)
    /// - 150 Lines (Horizontal, Vertical, Diagonals with varied strokes)
    /// - 150 Bezier Curves (Wave curves, S-curves, loops with gradients and solid colors)
    /// </summary>
    public static List<ItemViewModel> Generate1kItems(double startX = 60, double startY = 60, int cols = 40, int rows = 25)
    {
        var items = new List<ItemViewModel>(1000);
        const double cellW = 145.0;
        const double cellH = 145.0;

        int totalSlots = cols * rows; // 1,000 slots
        int zIndex = 1;

        for (int i = 0; i < totalSlots; i++)
        {
            int col = i % cols;
            int row = i / cols;
            double x = Math.Round(startX + col * cellW, 1);
            double y = Math.Round(startY + row * cellH, 1);

            ItemViewModel itemVm;

            // 1. Devices (0 - 299: 300 items)
            if (i < 300)
            {
                string devType = DeviceTypes[i % DeviceTypes.Length];
                double w = devType == "switch" ? 90 : (devType == "printer" ? 70 : 80);
                double h = devType == "switch" ? 45 : (devType == "server_rack" ? 100 : (devType == "screen" ? 70 : 80));

                var dev = new DeviceItem
                {
                    Name = $"{FormatDevicePrefix(devType)}-{i + 1:D3}",
                    DeviceType = devType,
                    Width = w,
                    Height = h,
                    X = x + (100 - w) / 2.0,
                    Y = y + (100 - h) / 2.0,
                    Rotation = (i % 4) * 90.0,
                    ValidationStatus = (i % 5 == 0) ? ValidationStatus.Valid : ValidationStatus.Unassigned,
                    IpAddress = (i % 5 == 0) ? $"192.168.1.{(i % 250) + 1}" : null
                };
                itemVm = new DeviceItemViewModel(dev);
            }
            // 2. Furniture (300 - 449: 150 items)
            else if (i < 450)
            {
                int furnIdx = i - 300;
                string furnType = FurnitureTypes[furnIdx % FurnitureTypes.Length];
                double w = furnType == "desk" ? 120 : (furnType == "conference_table_8" ? 140 : (furnType == "sofa_3_seater" ? 130 : 60));
                double h = furnType == "desk" ? 70 : (furnType == "conference_table_8" ? 70 : (furnType == "sofa_3_seater" ? 65 : 60));

                var furn = new FurnitureItem
                {
                    Name = $"{furnType}-{i + 1:D3}",
                    FurnitureType = furnType,
                    Width = w,
                    Height = h,
                    X = x + (120 - w) / 2.0,
                    Y = y + (80 - h) / 2.0,
                    Rotation = (furnIdx % 4) * 90.0
                };
                itemVm = new FurnitureItemViewModel(furn);
            }
            // 3. Shapes (450 - 699: 250 items)
            else if (i < 700)
            {
                int shapeIdx = i - 450;
                ShapeType sType = GeometricShapes[shapeIdx % GeometricShapes.Length];
                string fill = (shapeIdx % 3 == 0) ? GradientPalette[shapeIdx % GradientPalette.Length] : ColorPalette[shapeIdx % ColorPalette.Length];
                string stroke = ColorPalette[(shapeIdx + 3) % ColorPalette.Length];

                var shp = new ShapeItem
                {
                    Name = $"Shape-{sType}-{i + 1:D3}",
                    ShapeType = sType,
                    Width = 85.0,
                    Height = 85.0,
                    X = x + 15,
                    Y = y + 15,
                    Fill = fill,
                    Stroke = stroke,
                    StrokeThickness = 2.0 + (shapeIdx % 3),
                    CornerRadius = sType == ShapeType.RoundedRectangle ? 18.0 : (sType == ShapeType.RoundedCornerTriangle ? 12.0 : 0.0),
                    InnerRadius = sType == ShapeType.Donut ? 28.0 : 0.0,
                    Rotation = (shapeIdx % 8) * 45.0
                };
                itemVm = new ShapeItemViewModel(shp);
            }
            // 4. Lines (700 - 849: 150 items)
            else if (i < 850)
            {
                int lineIdx = i - 700;
                double angleDeg = (lineIdx % 8) * 45.0;
                double len = 70.0 + (lineIdx % 4) * 20.0;
                string stroke = ColorPalette[lineIdx % ColorPalette.Length];
                double thick = 2.0 + (lineIdx % 4);

                var line = new ShapeItem
                {
                    Name = $"Line-{i + 1:D3}",
                    ShapeType = ShapeType.Line,
                    Width = len,
                    Height = 20.0,
                    X = x + 10,
                    Y = y + 50,
                    Rotation = angleDeg,
                    Stroke = stroke,
                    StrokeThickness = thick
                };
                itemVm = new ShapeItemViewModel(line);
            }
            // 5. Bezier Curves (850 - 999: 150 items)
            else
            {
                int bezIdx = i - 850;
                string stroke = (bezIdx % 2 == 0) ? GradientPalette[bezIdx % GradientPalette.Length] : ColorPalette[bezIdx % ColorPalette.Length];
                double thick = 2.5 + (bezIdx % 3);

                // Diverse Bezier curvature profiles (S-curves, arcs, crests)
                double startLocalX = 10.0;
                double startLocalY = 60.0;
                double endLocalX = 100.0;
                double endLocalY = (bezIdx % 2 == 0) ? 60.0 : 20.0;

                double cp1X = 30.0;
                double cp1Y = (bezIdx % 3 == 0) ? 10.0 : (bezIdx % 3 == 1 ? 80.0 : 20.0);
                double cp2X = 80.0;
                double cp2Y = (bezIdx % 3 == 0) ? 90.0 : (bezIdx % 3 == 1 ? 20.0 : 80.0);

                var bez = new ShapeItem
                {
                    Name = $"Bezier-{i + 1:D3}",
                    ShapeType = ShapeType.Bezier,
                    Width = 110.0,
                    Height = 90.0,
                    X = x + 10,
                    Y = y + 10,
                    StartX = startLocalX,
                    StartY = startLocalY,
                    EndX = endLocalX,
                    EndY = endLocalY,
                    ControlPoint1X = cp1X,
                    ControlPoint1Y = cp1Y,
                    ControlPoint2X = cp2X,
                    ControlPoint2Y = cp2Y,
                    Stroke = stroke,
                    StrokeThickness = thick,
                    Rotation = (bezIdx % 4) * 45.0
                };
                itemVm = new ShapeItemViewModel(bez);
            }

            itemVm.Z = zIndex++;
            items.Add(itemVm);
        }

        return items;
    }

    /// <summary>
    /// Executes the 1K items simulation onto the CanvasViewModel and returns detailed diagnostic metrics.
    /// </summary>
    public static SimulationBenchmarkResult Simulate1kItemsOnCanvas(CanvasViewModel canvas)
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        long memBefore = GC.GetTotalMemory(true);

        var sw = Stopwatch.StartNew();
        var items = Generate1kItems();
        sw.Stop();
        double genTimeMs = sw.Elapsed.TotalMilliseconds;

        // Ensure canvas bounds accommodate the 40x25 item grid
        canvas.CanvasWidth = Math.Max(canvas.CanvasWidth, 6200);
        canvas.CanvasHeight = Math.Max(canvas.CanvasHeight, 4000);

        var swInsert = Stopwatch.StartNew();
        canvas.UndoRedoService.ExecuteCommand(new AddItemsBatchCommand(canvas.Items, items));
        swInsert.Stop();
        double insertTimeMs = swInsert.Elapsed.TotalMilliseconds;

        long memAfter = GC.GetTotalMemory(false);
        long ws = Process.GetCurrentProcess().WorkingSet64;

        return new SimulationBenchmarkResult
        {
            TotalItems = items.Count,
            DeviceCount = items.Count(i => i is DeviceItemViewModel),
            FurnitureCount = items.Count(i => i is FurnitureItemViewModel),
            ShapeCount = items.Count(i => i is ShapeItemViewModel s && s.ShapeType != ShapeType.Line && s.ShapeType != ShapeType.Bezier),
            LineCount = items.Count(i => i is ShapeItemViewModel s && s.ShapeType == ShapeType.Line),
            BezierCount = items.Count(i => i is ShapeItemViewModel s && s.ShapeType == ShapeType.Bezier),
            GenerationTimeMs = genTimeMs,
            InsertionTimeMs = insertTimeMs,
            MemoryBeforeMb = memBefore / (1024.0 * 1024.0),
            MemoryAfterMb = memAfter / (1024.0 * 1024.0),
            WorkingSetMb = ws / (1024.0 * 1024.0)
        };
    }

    private static string FormatDevicePrefix(string deviceType) => deviceType switch
    {
        "desktop_pc" => "PC",
        "laptop" => "LAPTOP",
        "screen" => "SCREEN",
        "server_rack" => "RACK",
        "switch" => "SW",
        "printer" => "PRINTER",
        _ => "DEV"
    };
}
