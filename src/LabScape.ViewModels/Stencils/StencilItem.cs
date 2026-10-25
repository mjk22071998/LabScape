using LabScape.Core.Enums;
using LabScape.Core.Models;
using LabScape.ViewModels.Items;

namespace LabScape.ViewModels.Stencils;

public class StencilItem
{
    public string Category { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string IconResourceKey { get; set; } = string.Empty;
    public double DefaultWidth { get; set; } = 80;
    public double DefaultHeight { get; set; } = 80;
    public Func<ItemViewModel> CreateItem { get; set; } = null!;

    public static List<StencilItem> GetAllStencils()
    {
        return new List<StencilItem>
        {
            // === 1. IT Devices ===
            new StencilItem
            {
                Category = "Devices",
                Title = "Desktop PC",
                IconResourceKey = "Stencil_DesktopPC",
                DefaultWidth = 80,
                DefaultHeight = 80,
                CreateItem = () => new DeviceItemViewModel(new DeviceItem
                {
                    Name = "Desktop-01",
                    DeviceType = "desktop_pc",
                    Width = 80,
                    Height = 80,
                    ValidationStatus = ValidationStatus.Unassigned
                })
            },
            new StencilItem
            {
                Category = "Devices",
                Title = "Laptop",
                IconResourceKey = "Stencil_Laptop",
                DefaultWidth = 80,
                DefaultHeight = 80,
                CreateItem = () => new DeviceItemViewModel(new DeviceItem
                {
                    Name = "Laptop-01",
                    DeviceType = "laptop",
                    Width = 80,
                    Height = 80,
                    ValidationStatus = ValidationStatus.Unassigned
                })
            },
            new StencilItem
            {
                Category = "Devices",
                Title = "Screen",
                IconResourceKey = "Stencil_Screen",
                DefaultWidth = 80,
                DefaultHeight = 70,
                CreateItem = () => new DeviceItemViewModel(new DeviceItem
                {
                    Name = "Screen-01",
                    DeviceType = "screen",
                    Width = 80,
                    Height = 70,
                    ValidationStatus = ValidationStatus.Unassigned
                })
            },
            new StencilItem
            {
                Category = "Devices",
                Title = "Server Rack",
                IconResourceKey = "Stencil_ServerRack",
                DefaultWidth = 80,
                DefaultHeight = 100,
                CreateItem = () => new DeviceItemViewModel(new DeviceItem
                {
                    Name = "Server-Rack-01",
                    DeviceType = "server_rack",
                    Width = 80,
                    Height = 100,
                    ValidationStatus = ValidationStatus.Unassigned
                })
            },
            new StencilItem
            {
                Category = "Devices",
                Title = "Network Switch",
                IconResourceKey = "Stencil_NetworkSwitch",
                DefaultWidth = 90,
                DefaultHeight = 45,
                CreateItem = () => new DeviceItemViewModel(new DeviceItem
                {
                    Name = "Switch-01",
                    DeviceType = "switch",
                    Width = 90,
                    Height = 45,
                    ValidationStatus = ValidationStatus.Unassigned
                })
            },
            new StencilItem
            {
                Category = "Devices",
                Title = "Printer / Copier",
                IconResourceKey = "Stencil_Printer",
                DefaultWidth = 70,
                DefaultHeight = 70,
                CreateItem = () => new DeviceItemViewModel(new DeviceItem
                {
                    Name = "Printer-01",
                    DeviceType = "printer",
                    Width = 70,
                    Height = 70,
                    ValidationStatus = ValidationStatus.Unassigned
                })
            },

            // === 2. Furniture ===
            new StencilItem
            {
                Category = "Furniture",
                Title = "Desk",
                IconResourceKey = "Stencil_Desk",
                DefaultWidth = 120,
                DefaultHeight = 70,
                CreateItem = () => new FurnitureItemViewModel(new FurnitureItem
                {
                    Name = "Office Desk",
                    FurnitureType = "desk",
                    Width = 120,
                    Height = 70
                })
            },
            new StencilItem
            {
                Category = "Furniture",
                Title = "Office Swivel Chair",
                IconResourceKey = "Stencil_OfficeChair",
                DefaultWidth = 60,
                DefaultHeight = 60,
                CreateItem = () => new FurnitureItemViewModel(new FurnitureItem
                {
                    Name = "Office Chair",
                    FurnitureType = "office_chair",
                    Width = 60,
                    Height = 60
                })
            },
            new StencilItem
            {
                Category = "Furniture",
                Title = "Conference Table 8-Seater",
                IconResourceKey = "Stencil_ConferenceTable8",
                DefaultWidth = 220,
                DefaultHeight = 100,
                CreateItem = () => new FurnitureItemViewModel(new FurnitureItem
                {
                    Name = "Conference Table",
                    FurnitureType = "conference_table_8",
                    Width = 220,
                    Height = 100
                })
            },
            new StencilItem
            {
                Category = "Furniture",
                Title = "3-Seater Sofa",
                IconResourceKey = "Stencil_Sofa3Seater",
                DefaultWidth = 160,
                DefaultHeight = 80,
                CreateItem = () => new FurnitureItemViewModel(new FurnitureItem
                {
                    Name = "Sofa",
                    FurnitureType = "sofa_3_seater",
                    Width = 160,
                    Height = 80
                })
            },
            new StencilItem
            {
                Category = "Furniture",
                Title = "Office Bookcase",
                IconResourceKey = "Stencil_Bookcase",
                DefaultWidth = 120,
                DefaultHeight = 40,
                CreateItem = () => new FurnitureItemViewModel(new FurnitureItem
                {
                    Name = "Bookcase",
                    FurnitureType = "bookcase",
                    Width = 120,
                    Height = 40
                })
            },
            new StencilItem
            {
                Category = "Furniture",
                Title = "Floor Plant",
                IconResourceKey = "Stencil_Plant",
                DefaultWidth = 60,
                DefaultHeight = 60,
                CreateItem = () => new FurnitureItemViewModel(new FurnitureItem
                {
                    Name = "Plant",
                    FurnitureType = "plant",
                    Width = 60,
                    Height = 60
                })
            },

            // === 3. Shapes ===
            new StencilItem
            {
                Category = "Shapes",
                Title = "Rectangle",
                IconResourceKey = "Shape_Rectangle",
                DefaultWidth = 100,
                DefaultHeight = 60,
                CreateItem = () => new ShapeItemViewModel(new ShapeItem
                {
                    Name = "Rectangle",
                    ShapeType = ShapeType.Rectangle,
                    Width = 100,
                    Height = 60
                })
            },
            new StencilItem
            {
                Category = "Shapes",
                Title = "Rounded Rectangle",
                IconResourceKey = "Shape_RoundedRectangle",
                DefaultWidth = 100,
                DefaultHeight = 60,
                CreateItem = () => new ShapeItemViewModel(new ShapeItem
                {
                    Name = "Rounded Rectangle",
                    ShapeType = ShapeType.RoundedRectangle,
                    CornerRadius = 15.0,
                    Width = 100,
                    Height = 60
                })
            },
            new StencilItem
            {
                Category = "Shapes",
                Title = "Circle",
                IconResourceKey = "Shape_Circle",
                DefaultWidth = 80,
                DefaultHeight = 80,
                CreateItem = () => new ShapeItemViewModel(new ShapeItem
                {
                    Name = "Circle",
                    ShapeType = ShapeType.Circle,
                    Width = 80,
                    Height = 80
                })
            },
            new StencilItem
            {
                Category = "Shapes",
                Title = "Semi Circle",
                IconResourceKey = "Shape_SemiCircle",
                DefaultWidth = 80,
                DefaultHeight = 50,
                CreateItem = () => new ShapeItemViewModel(new ShapeItem
                {
                    Name = "Semi Circle",
                    ShapeType = ShapeType.SemiCircle,
                    Width = 80,
                    Height = 50
                })
            },
            new StencilItem
            {
                Category = "Shapes",
                Title = "Triangle",
                IconResourceKey = "Shape_Triangle",
                DefaultWidth = 80,
                DefaultHeight = 80,
                CreateItem = () => new ShapeItemViewModel(new ShapeItem
                {
                    Name = "Triangle",
                    ShapeType = ShapeType.Triangle,
                    Width = 80,
                    Height = 80
                })
            },
            new StencilItem
            {
                Category = "Shapes",
                Title = "Rounded Corner Triangle",
                IconResourceKey = "Shape_RoundedTriangle",
                DefaultWidth = 80,
                DefaultHeight = 80,
                CreateItem = () => new ShapeItemViewModel(new ShapeItem
                {
                    Name = "Rounded Triangle",
                    ShapeType = ShapeType.RoundedCornerTriangle,
                    CornerRadius = 12.0,
                    Width = 80,
                    Height = 80
                })
            },
            new StencilItem
            {
                Category = "Shapes",
                Title = "Donut",
                IconResourceKey = "Shape_Donut",
                DefaultWidth = 90,
                DefaultHeight = 90,
                CreateItem = () => new ShapeItemViewModel(new ShapeItem
                {
                    Name = "Donut",
                    ShapeType = ShapeType.Donut,
                    InnerRadius = 25.0,
                    Width = 90,
                    Height = 90
                })
            },
            new StencilItem
            {
                Category = "Shapes",
                Title = "Line",
                IconResourceKey = "Shape_Line",
                DefaultWidth = 100,
                DefaultHeight = 20,
                CreateItem = () => new ShapeItemViewModel(new ShapeItem
                {
                    Name = "Line",
                    ShapeType = ShapeType.Line,
                    Width = 100,
                    Height = 20,
                    StrokeThickness = 2.0
                })
            },
            new StencilItem
            {
                Category = "Shapes",
                Title = "Bezier Curve",
                IconResourceKey = "Shape_Bezier",
                DefaultWidth = 140,
                DefaultHeight = 90,
                CreateItem = () => new ShapeItemViewModel(new ShapeItem
                {
                    Name = "Bezier",
                    ShapeType = ShapeType.Bezier,
                    Width = 140,
                    Height = 90,
                    StartX = 10,
                    StartY = 75,
                    ControlPoint1X = 35,
                    ControlPoint1Y = 15,
                    ControlPoint2X = 105,
                    ControlPoint2Y = 75,
                    EndX = 130,
                    EndY = 15,
                    StrokeThickness = 2.0
                })
            }
        };
    }
}
