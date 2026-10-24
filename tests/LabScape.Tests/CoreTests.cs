using LabScape.Core.Enums;
using LabScape.Core.Models;
using LabScape.Infrastructure.Services;
using LabScape.ViewModels;
using LabScape.ViewModels.Items;
using Xunit;

namespace LabScape.Tests;

public class CoreTests
{
    [Fact]
    public void LabelVisibilityRule_OnlyDevicesShowCanvasLabel()
    {
        var device = new DeviceItemViewModel(new DeviceItem { Name = "PC-01" });
        var furniture = new FurnitureItemViewModel(new FurnitureItem { Name = "Desk" });
        var shape = new ShapeItemViewModel(new ShapeItem { Name = "Circle" });
        var group = new GroupItemViewModel(new GroupItem { Name = "MyGroup" });

        Assert.True(device.ShowCanvasLabel, "Device MUST show label on layout canvas");
        Assert.False(furniture.ShowCanvasLabel, "Furniture must NOT show label on layout canvas");
        Assert.False(shape.ShowCanvasLabel, "Shape must NOT show label on layout canvas");
        Assert.False(group.ShowCanvasLabel, "Group must NOT show label on layout canvas");
    }

    [Fact]
    public void SchemaSerialization_RoundTrip_PreservesAllPropertiesAndHierarchy()
    {
        var repo = new LayoutJsonRepository();
        var doc = new LayoutDocument
        {
            Metadata = new DocumentMetadata
            {
                Title = "Engineering Lab",
                CanvasWidth = 2400,
                CanvasHeight = 1600,
                GridSize = 25.0
            }
        };

        // Add Device
        var dev = new DeviceItem
        {
            Name = "CAD-01",
            DeviceType = "desktop_pc",
            IpAddress = "192.168.1.50",
            MacAddress = "AA:BB:CC:DD:EE:FF",
            X = 100,
            Y = 150,
            Z = 1,
            Width = 80,
            Height = 80
        };
        doc.Items.Add(dev);

        // Add Furniture
        var furn = new FurnitureItem
        {
            Name = "Main Table",
            FurnitureType = "conference_table_8",
            X = 300,
            Y = 300,
            Z = 2,
            Width = 220,
            Height = 100
        };
        doc.Items.Add(furn);

        // Add Shape with CornerRadius
        var shape = new ShapeItem
        {
            Name = "Rounded Corner",
            ShapeType = ShapeType.RoundedRectangle,
            CornerRadius = 16.0,
            X = 600,
            Y = 200,
            Z = 3,
            Width = 120,
            Height = 80
        };
        doc.Items.Add(shape);

        // Add Donut Shape with InnerRadius
        var donut = new ShapeItem
        {
            Name = "Donut Pillar",
            ShapeType = ShapeType.Donut,
            InnerRadius = 30.0,
            X = 800,
            Y = 200,
            Z = 4,
            Width = 90,
            Height = 90
        };
        doc.Items.Add(donut);

        // Add Group with Recursive Children
        var group = new GroupItem
        {
            Name = "Workstation Cluster",
            X = 1000,
            Y = 500,
            Z = 5,
            Width = 200,
            Height = 150
        };
        group.Children.Add(new DeviceItem { Name = "Nested-PC", IpAddress = "192.168.1.51", X = 1010, Y = 510, Z = 6 });
        group.Children.Add(new FurnitureItem { Name = "Nested-Chair", FurnitureType = "office_chair", X = 1050, Y = 550, Z = 7 });
        doc.Items.Add(group);

        // Serialize to JSON
        string json = repo.SerializeToJson(doc);
        Assert.NotEmpty(json);
        Assert.Contains("\"type\": \"device\"", json);
        Assert.Contains("\"type\": \"furniture\"", json);
        Assert.Contains("\"type\": \"shape\"", json);
        Assert.Contains("\"type\": \"group\"", json);
        Assert.Contains("\"corner_radius\": 16", json);
        Assert.Contains("\"inner_radius\": 30", json);
        Assert.Contains("\"children\":", json);

        // Deserialize back
        var deserialized = repo.DeserializeFromJson(json);
        Assert.Equal(doc.Metadata.Title, deserialized.Metadata.Title);
        Assert.Equal(5, deserialized.Items.Count);

        var restoredDev = Assert.IsType<DeviceItem>(deserialized.Items[0]);
        Assert.Equal("CAD-01", restoredDev.Name);
        Assert.Equal("192.168.1.50", restoredDev.IpAddress);
        Assert.Equal("AA:BB:CC:DD:EE:FF", restoredDev.MacAddress);

        var restoredShape = Assert.IsType<ShapeItem>(deserialized.Items[2]);
        Assert.Equal(ShapeType.RoundedRectangle, restoredShape.ShapeType);
        Assert.Equal(16.0, restoredShape.CornerRadius);

        var restoredDonut = Assert.IsType<ShapeItem>(deserialized.Items[3]);
        Assert.Equal(ShapeType.Donut, restoredDonut.ShapeType);
        Assert.Equal(30.0, restoredDonut.InnerRadius);

        var restoredGroup = Assert.IsType<GroupItem>(deserialized.Items[4]);
        Assert.Equal("Workstation Cluster", restoredGroup.Name);
        Assert.Equal(2, restoredGroup.Children.Count);
        Assert.IsType<DeviceItem>(restoredGroup.Children[0]);
        Assert.IsType<FurnitureItem>(restoredGroup.Children[1]);
    }

    [Fact]
    public void ValidationEngine_DetectsUnassignedAndDuplicateIps()
    {
        var undoService = new UndoRedoService();
        var canvas = new CanvasViewModel(undoService);

        var dev1 = new DeviceItemViewModel(new DeviceItem { Name = "PC-01", IpAddress = null });
        var dev2 = new DeviceItemViewModel(new DeviceItem { Name = "PC-02", IpAddress = "10.0.0.1" });
        var dev3 = new DeviceItemViewModel(new DeviceItem { Name = "PC-03", IpAddress = "10.0.0.1" }); // Duplicate!
        var dev4 = new DeviceItemViewModel(new DeviceItem { Name = "PC-04", IpAddress = "10.0.0.2" }); // Unique

        canvas.AddItem(dev1);
        canvas.AddItem(dev2);
        canvas.AddItem(dev3);
        canvas.AddItem(dev4);

        canvas.RecalculateValidation();

        Assert.Equal(ValidationStatus.Unassigned, dev1.ValidationStatus);
        Assert.Equal(ValidationStatus.DuplicateIp, dev2.ValidationStatus);
        Assert.Equal(ValidationStatus.DuplicateIp, dev3.ValidationStatus);
        Assert.Equal(ValidationStatus.Valid, dev4.ValidationStatus);
    }

    [Fact]
    public void GroupAndUngroup_WithUndoRedo_MaintainsIntegrity()
    {
        var undoService = new UndoRedoService();
        var canvas = new CanvasViewModel(undoService) { SnapToGrid = false };

        var item1 = new DeviceItemViewModel(new DeviceItem { Name = "Dev1", X = 10, Y = 10, Width = 50, Height = 50, Z = 1 });
        var item2 = new FurnitureItemViewModel(new FurnitureItem { Name = "Desk1", X = 80, Y = 10, Width = 100, Height = 50, Z = 2 });

        canvas.AddItem(item1);
        canvas.AddItem(item2);
        Assert.Equal(2, canvas.Items.Count);

        // Select both items
        canvas.SelectItem(item1, addToExisting: false);
        canvas.SelectItem(item2, addToExisting: true);
        Assert.Equal(2, canvas.SelectedItems.Count);

        // Group (Ctrl+G)
        canvas.GroupSelected();
        Assert.Single(canvas.Items);
        var group = Assert.IsType<GroupItemViewModel>(canvas.Items[0]);
        Assert.Equal(2, group.Children.Count);
        Assert.Equal("Group", group.Name);
        Assert.Equal(10, group.X);
        Assert.Equal(10, group.Y);
        Assert.Equal(170, group.Width); // max(80+100) - min(10)

        // Undo Grouping
        undoService.Undo();
        Assert.Equal(2, canvas.Items.Count);
        Assert.Null(item1.ParentGroupId);
        Assert.Null(item2.ParentGroupId);

        // Redo Grouping
        undoService.Redo();
        Assert.Single(canvas.Items);

        // Ungroup (Ctrl+Shift+G)
        canvas.SelectItem(canvas.Items[0]);
        canvas.UngroupSelected();
        Assert.Equal(2, canvas.Items.Count);
        Assert.DoesNotContain(canvas.Items, i => i is GroupItemViewModel);
    }

    [Fact]
    public void LayeringAndAlignment_ModifiesCoordinatesProperly()
    {
        var undoService = new UndoRedoService();
        var canvas = new CanvasViewModel(undoService) { SnapToGrid = false };

        var itemA = new ShapeItemViewModel(new ShapeItem { Name = "A", X = 50, Y = 10, Width = 100, Height = 50, Z = 1 });
        var itemB = new ShapeItemViewModel(new ShapeItem { Name = "B", X = 150, Y = 80, Width = 60, Height = 40, Z = 2 });

        canvas.AddItem(itemA);
        canvas.AddItem(itemB);

        // Select both
        canvas.SelectItem(itemA);
        canvas.SelectItem(itemB, addToExisting: true);

        // Align Left: Both should have X = min(50, 150) = 50
        canvas.AlignLeft();
        Assert.Equal(50, itemA.X);
        Assert.Equal(50, itemB.X);

        // Undo
        undoService.Undo();
        Assert.Equal(50, itemA.X);
        Assert.Equal(150, itemB.X);

        // Bring to front on itemA
        canvas.ClearSelection();
        canvas.SelectItem(itemA);
        canvas.BringToFront();
        Assert.True(itemA.Z > itemB.Z);
    }
}
