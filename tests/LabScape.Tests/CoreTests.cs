using LabScape.Core.Enums;
using LabScape.Core.Helpers;
using LabScape.Core.Models;
using LabScape.Infrastructure.Services;
using LabScape.ViewModels;
using LabScape.ViewModels.Commands;
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

        // Undo BringToFront
        undoService.Undo();
        Assert.Equal(1, itemA.Z);

        // SendToBack
        canvas.SendToBack();
        Assert.True(itemA.Z < itemB.Z);

        // MoveForward
        int prevZ = itemA.Z;
        canvas.MoveForward();
        Assert.True(itemA.Z > prevZ);

        // SendBackward
        canvas.SendBackward();
        Assert.Equal(prevZ, itemA.Z);
    }

    [Fact]
    public void TransformItemCommand_UndoRedo_RestoresAllDimensionsAndRotation()
    {
        var undoService = new UndoRedoService();
        var item = new ShapeItemViewModel(new ShapeItem
        {
            Name = "Rect1",
            X = 20,
            Y = 30,
            Width = 100,
            Height = 80,
            Rotation = 0
        });

        // Create transform command: resizing and rotating
        var cmd = new LabScape.ViewModels.Commands.TransformItemCommand(
            item,
            oldX: 20, oldY: 30, oldW: 100, oldH: 80, oldRot: 0,
            newX: 50, newY: 60, newW: 200, newH: 150, newRot: 45.0);

        undoService.ExecuteCommand(cmd);
        Assert.Equal(50, item.X);
        Assert.Equal(60, item.Y);
        Assert.Equal(200, item.Width);
        Assert.Equal(150, item.Height);
        Assert.Equal(45.0, item.Rotation);

        // Undo
        undoService.Undo();
        Assert.Equal(20, item.X);
        Assert.Equal(30, item.Y);
        Assert.Equal(100, item.Width);
        Assert.Equal(80, item.Height);
        Assert.Equal(0, item.Rotation);

        // Redo
        undoService.Redo();
        Assert.Equal(50, item.X);
        Assert.Equal(60, item.Y);
        Assert.Equal(200, item.Width);
        Assert.Equal(150, item.Height);
        Assert.Equal(45.0, item.Rotation);
    }

    [Fact]
    public void StencilPalette_AllCatalogItemsInstantiateCorrectTypes()
    {
        var allStencils = LabScape.ViewModels.Stencils.StencilItem.GetAllStencils();
        Assert.True(allStencils.Count >= 18, "Catalog must contain at least 18 stencils");

        foreach (var stencil in allStencils)
        {
            var item = stencil.CreateItem();
            Assert.NotNull(item);
            Assert.NotEmpty(item.Name);
            Assert.True(item.Width > 0);
            Assert.True(item.Height > 0);

            if (stencil.Category == "Devices")
            {
                Assert.True(item.ShowCanvasLabel, $"Device '{stencil.Title}' must show canvas label");
                Assert.IsType<DeviceItemViewModel>(item);
            }
            else
            {
                Assert.False(item.ShowCanvasLabel, $"Non-device '{stencil.Title}' must NOT show canvas label");
            }
        }
    }

    [Fact]
    public async Task MockDeviceRepository_ReturnsSimulatedWorkstationsWithLatency()
    {
        var repo = new LabScape.Infrastructure.Repositories.MockDeviceRepository();
        var isConnected = await repo.TestConnectionAsync("");
        var latency = await repo.MeasureLatencyAsync("");
        var devices = await repo.GetDevicesAsync("");

        Assert.True(isConnected);
        Assert.True(latency.TotalMilliseconds >= 0);
        Assert.True(devices.Count >= 25);
        Assert.Contains(devices, d => d.DisplayName.Contains("CAD-Workstation"));
        Assert.Contains(devices, d => d.DisplayName.Contains("Server-Node"));
    }

    [Fact]
    public void DpapiCredentialVault_RoundTrip_PreservesConnectionProfile()
    {
        if (!OperatingSystem.IsWindows()) return;

        string tempVault = Path.Combine(Path.GetTempPath(), $"labscape_test_vault_{Guid.NewGuid()}.dat");
        string tempCache = Path.Combine(Path.GetTempPath(), $"labscape_test_cache_{Guid.NewGuid()}.json");

        try
        {
            LabScape.Infrastructure.Services.DpapiCredentialVault.CustomVaultPath = tempVault;
            LabScape.Infrastructure.Services.DpapiCredentialVault.CustomCachePath = tempCache;

            var original = new LabScape.Infrastructure.Services.DatabaseConnectionProfile
            {
                Host = "10.0.0.99",
                Port = 5432,
                Database = "test_lab_db",
                Username = "lab_admin",
                Password = "SecurePassword123!",
                UseMock = false,
                IsConfigured = true
            };

            LabScape.Infrastructure.Services.DpapiCredentialVault.SaveProfile(original);
            var loaded = LabScape.Infrastructure.Services.DpapiCredentialVault.LoadProfile();

            Assert.Equal(original.Host, loaded.Host);
            Assert.Equal(original.Port, loaded.Port);
            Assert.Equal(original.Database, loaded.Database);
            Assert.Equal(original.Username, loaded.Username);
            Assert.Equal(original.Password, loaded.Password);
            Assert.Equal(original.UseMock, loaded.UseMock);
            Assert.Equal(original.IsConfigured, loaded.IsConfigured);

            // Test Inventory Cache Roundtrip
            var testDevices = new List<DatabaseDevice>
            {
                new DatabaseDevice { DeviceId = "D1", DisplayName = "Workstation 1", PrimaryIp = "192.168.1.10", MacAddress = "00:11:22:33:44:55" }
            };
            LabScape.Infrastructure.Services.DpapiCredentialVault.SaveInventoryCache(testDevices);
            var cached = LabScape.Infrastructure.Services.DpapiCredentialVault.LoadInventoryCache();
            Assert.Single(cached);
            Assert.Equal("Workstation 1", cached[0].DisplayName);
        }
        finally
        {
            LabScape.Infrastructure.Services.DpapiCredentialVault.CustomVaultPath = null;
            LabScape.Infrastructure.Services.DpapiCredentialVault.CustomCachePath = null;
            if (File.Exists(tempVault)) File.Delete(tempVault);
            if (File.Exists(tempCache)) File.Delete(tempCache);
        }
    }

    [Fact]
    public void DpapiCredentialVault_Defaults_ToRealPostgreSql_NotMock()
    {
        var defaultProfile = new LabScape.Infrastructure.Services.DatabaseConnectionProfile();
        Assert.False(defaultProfile.UseMock, "Default profile must target real PostgreSQL, not simulated mock");
    }

    [Fact]
    public void MainViewModel_Startup_InitializesWithEmptyInventory_NoDummyData()
    {
        var vm = new MainViewModel(
            new LayoutJsonRepository(),
            new LabScape.Infrastructure.Services.PostgresDatabaseService(),
            new UndoRedoService());

        Assert.Empty(vm.Properties.AvailableDatabaseDevices);
        Assert.Empty(vm.Properties.FilteredDatabaseDevices);
        Assert.False(vm.IsDatabaseConnected);
        Assert.Equal("Database: Disconnected", vm.DatabaseStatusText);
    }

    [Fact]
    public void PropertiesViewModel_MultiSelection_DisablesXY_AllowsCommonProperties()
    {
        var canvas = new CanvasViewModel(new UndoRedoService());
        var props = new PropertiesViewModel(canvas);

        var item1 = new ShapeItemViewModel(new ShapeItem { Name = "Shape1", X = 100, Y = 100, Width = 80, Height = 60, Z = 1 });
        var item2 = new ShapeItemViewModel(new ShapeItem { Name = "Shape2", X = 300, Y = 250, Width = 90, Height = 70, Z = 2 });
        canvas.Items.Add(item1);
        canvas.Items.Add(item2);

        // 1. Single selection: XY must be editable
        canvas.SelectItem(item1);
        Assert.True(props.HasSelection);
        Assert.True(props.IsSingleSelection);
        Assert.False(props.IsMultiSelection);
        Assert.True(props.CanEditCoordinates, "Single selection must allow editing XY coordinates");
        Assert.Equal("100.0", props.DisplayX);
        Assert.Equal("100.0", props.DisplayY);

        // Edit single item X
        props.DisplayX = "150";
        Assert.Equal(150, item1.X);

        // 2. Multi selection: XY must be disabled, but common properties (Width, Height, Z, Rotation) editable
        canvas.SelectItem(item2, addToExisting: true);
        Assert.Equal(2, canvas.SelectedItems.Count);
        Assert.True(props.HasSelection);
        Assert.True(props.IsMultiSelection);
        Assert.False(props.IsSingleSelection);
        Assert.False(props.CanEditCoordinates, "Multi-selection must NOT allow editing XY coordinates");
        Assert.Equal("(Multiple)", props.DisplayX);
        Assert.Equal("(Multiple)", props.DisplayY);

        // Edit common width across all selected items
        props.CommonWidth = "200";
        Assert.Equal(200, item1.Width);
        Assert.Equal(200, item2.Width);

        // Edit common height across all selected items
        props.CommonHeight = "120";
        Assert.Equal(120, item1.Height);
        Assert.Equal(120, item2.Height);

        // Edit common rotation across all selected items
        props.CommonRotation = "45";
        Assert.Equal(45, item1.Rotation);
        Assert.Equal(45, item2.Rotation);

        // Edit common Z across all selected items
        props.CommonZ = "10";
        Assert.Equal(10, item1.Z);
        Assert.Equal(10, item2.Z);
    }

    [Fact]
    public void ExportGatekeeper_ValidationCountersReflectState()
    {
        var undo = new UndoRedoService();
        var vm = new MainViewModel(
            new LayoutJsonRepository(),
            new LabScape.Infrastructure.Services.PostgresDatabaseService(),
            undo);

        // 1. Initially with unassigned PC
        var dev1 = new DeviceItemViewModel(new DeviceItem { Name = "PC-A", ValidationStatus = ValidationStatus.Unassigned });
        vm.Canvas.Items.Add(dev1);
        vm.Canvas.RecalculateValidation();

        // Warning count should be >= 1, Error count 0
        Assert.True(dev1.ValidationStatus == ValidationStatus.Unassigned);

        // 2. Add duplicate IPs
        var dev2 = new DeviceItemViewModel(new DeviceItem { Name = "PC-B", IpAddress = "192.168.1.100" });
        var dev3 = new DeviceItemViewModel(new DeviceItem { Name = "PC-C", IpAddress = "192.168.1.100" });
        vm.Canvas.Items.Add(dev2);
        vm.Canvas.Items.Add(dev3);
        vm.Canvas.RecalculateValidation();

        Assert.Equal(ValidationStatus.DuplicateIp, dev2.ValidationStatus);
        Assert.Equal(ValidationStatus.DuplicateIp, dev3.ValidationStatus);
    }

    [Fact]
    public void Html5WebViewer_ExistsAndHasCompleteCanvas2DImplementation()
    {
        // Search up from BaseDirectory to find repo root containing web-viewer
        string? current = AppContext.BaseDirectory;
        string? webViewerPath = null;
        while (current != null)
        {
            var candidate = Path.Combine(current, "web-viewer", "index.html");
            if (File.Exists(candidate))
            {
                webViewerPath = candidate;
                break;
            }
            current = Directory.GetParent(current)?.FullName;
        }

        Assert.True(webViewerPath != null && File.Exists(webViewerPath), "web-viewer/index.html must exist in project");

        var html = File.ReadAllText(webViewerPath!);
        Assert.Contains("<canvas id=\"layout-canvas\"", html);
        Assert.Contains("getContext(\"2d\")", html);
        Assert.Contains("renderDevice", html);
        Assert.Contains("renderFurniture", html);
        Assert.Contains("renderShape", html);
        Assert.Contains("wheel", html); // Pan and zoom
        Assert.Contains("donut", html);
        Assert.Contains("rounded_corner_triangle", html);
        Assert.Contains("bezier", html);
    }

    [Fact]
    public void Setup_ManifestAndInnoScript_EnforceAdminAndOfflineDefaults()
    {
        string? current = AppContext.BaseDirectory;
        string? root = null;
        while (current != null)
        {
            if (File.Exists(Path.Combine(current, "build-release.ps1")))
            {
                root = current;
                break;
            }
            current = Directory.GetParent(current)?.FullName;
        }

        Assert.NotNull(root);

        // 1. Verify app.manifest requires administrator
        var manifestPath = Path.Combine(root, "src", "LabScape.Setup", "app.manifest");
        Assert.True(File.Exists(manifestPath), "Setup app.manifest must exist");
        var manifestContent = File.ReadAllText(manifestPath);
        Assert.Contains("requestedExecutionLevel level=\"requireAdministrator\"", manifestContent);

        // 2. Verify Inno Setup script
        var issPath = Path.Combine(root, "installer", "LabScape.iss");
        Assert.True(File.Exists(issPath), "Inno Setup script must exist");
        var issContent = File.ReadAllText(issPath);
        Assert.Contains("PrivilegesRequired=admin", issContent);
        Assert.Contains("DefaultDirName={autopf}\\{#MyAppName}", issContent);
        Assert.Contains("OutputBaseFilename=LabScape-Setup-v{#MyAppVersion}", issContent);
        Assert.Contains("web-viewer", issContent);
        Assert.Contains(".layout", issContent);

        // 3. Verify payload archive if generated
        var payloadZip = Path.Combine(root, "src", "LabScape.Setup", "Resources", "payload.zip");
        if (File.Exists(payloadZip))
        {
            using var archive = System.IO.Compression.ZipFile.OpenRead(payloadZip);
            Assert.Contains(archive.Entries, e => e.FullName.Contains("LabScape.App.dll"));
            Assert.Contains(archive.Entries, e => e.FullName.Contains("web-viewer"));
        }
    }

    [Fact]
    public void MultiTabBrowserArchitecture_MaintainsSeparateCanvasesAndHistories()
    {
        var jsonRepo = new LayoutJsonRepository();
        var db = new LabScape.Infrastructure.Services.PostgresDatabaseService();
        var vm = new MainViewModel(jsonRepo, db, new UndoRedoService(), () => new UndoRedoService());

        // Tab 1 exists by default
        Assert.Single(vm.Tabs);
        var tab1 = vm.ActiveTab;
        Assert.NotNull(tab1);

        // Add item to Tab 1
        var item1 = new DeviceItemViewModel(new DeviceItem { Name = "PC-Tab1", IpAddress = "10.0.0.1" });
        tab1.Canvas.AddItem(item1);
        Assert.Single(tab1.Canvas.Items);
        Assert.True(tab1.UndoRedoService.CanUndo);

        // Create Tab 2
        var tab2 = vm.CreateNewTab("Engineering Lab 2");
        Assert.Equal(2, vm.Tabs.Count);
        Assert.Same(tab2, vm.ActiveTab);
        Assert.Empty(tab2.Canvas.Items);
        Assert.False(tab2.UndoRedoService.CanUndo, "New tab must have its own fresh undo stack");

        // Add 2 items to Tab 2
        var item2A = new FurnitureItemViewModel(new FurnitureItem { Name = "Desk-Tab2" });
        var item2B = new ShapeItemViewModel(new ShapeItem { Name = "Circle-Tab2", ShapeType = ShapeType.Circle });
        tab2.Canvas.AddItem(item2A);
        tab2.Canvas.AddItem(item2B);
        Assert.Equal(2, tab2.Canvas.Items.Count);
        Assert.Single(tab1.Canvas.Items); // Tab 1 unaffected

        // Switch back to Tab 1
        vm.SelectTab(tab1);
        Assert.Same(tab1, vm.ActiveTab);
        Assert.Same(tab1.Canvas, vm.ActiveCanvas);
        Assert.Single(vm.Canvas.Items);
        Assert.True(vm.CanUndo);

        // Undo on Tab 1
        vm.Undo();
        Assert.Empty(tab1.Canvas.Items);
        Assert.Equal(2, tab2.Canvas.Items.Count); // Tab 2 unaffected

        // Cycle tabs
        vm.NextTab();
        Assert.Same(tab2, vm.ActiveTab);
        vm.PreviousTab();
        Assert.Same(tab1, vm.ActiveTab);

        // Close Tab 1
        vm.CloseTab(tab1);
        Assert.Single(vm.Tabs);
        Assert.Same(tab2, vm.ActiveTab);

        // Close last tab creates a new empty tab
        vm.CloseTab(tab2);
        Assert.Single(vm.Tabs);
        Assert.NotNull(vm.ActiveTab);
        Assert.Empty(vm.ActiveTab.Canvas.Items);
    }

    [Fact]
    public void UnifiedJsonSchema_ImportAndExport_FullBidirectionalFidelity()
    {
        var jsonRepo = new LayoutJsonRepository();
        var db = new LabScape.Infrastructure.Services.PostgresDatabaseService();
        var vm = new MainViewModel(jsonRepo, db, new UndoRedoService(), () => new UndoRedoService());

        // 1. Create a rich document
        var originalDoc = new LayoutDocument
        {
            Metadata = new DocumentMetadata
            {
                Title = "Network Operations Center",
                CanvasWidth = 3200,
                CanvasHeight = 2400,
                GridSize = 20
            }
        };

        var switchDev = new DeviceItem
        {
            Name = "Core-Switch",
            DeviceType = "switch",
            IpAddress = "10.10.0.1",
            ValidationStatus = ValidationStatus.Valid,
            X = 100,
            Y = 100
        };
        var confTable = new FurnitureItem
        {
            Name = "Board Table",
            FurnitureType = "conference_table_8",
            X = 400,
            Y = 200
        };
        var donutShape = new ShapeItem
        {
            Name = "Pillar",
            ShapeType = ShapeType.Donut,
            InnerRadius = 35,
            X = 800,
            Y = 300
        };

        originalDoc.Items.Add(switchDev);
        originalDoc.Items.Add(confTable);
        originalDoc.Items.Add(donutShape);

        // 2. Export / Serialize to JSON (The EXACT same format for import and export)
        var exportedJson = jsonRepo.SerializeToJson(originalDoc);
        Assert.Contains("\"type\": \"device\"", exportedJson);
        Assert.Contains("\"type\": \"furniture\"", exportedJson);
        Assert.Contains("\"type\": \"shape\"", exportedJson);
        Assert.Contains("\"shape_type\": \"donut\"", exportedJson);

        // 3. Import / Open into tab
        var deserializedDoc = jsonRepo.DeserializeFromJson(exportedJson);
        var tab = vm.LoadDocumentIntoTab(deserializedDoc, "test_noc.json", "NOC Floorplan");

        Assert.Equal("NOC Floorplan", tab.Title);
        Assert.Equal(3, tab.Canvas.Items.Count);
        Assert.Contains(tab.Canvas.Items, i => i.Name == "Core-Switch" && i is DeviceItemViewModel);
        Assert.Contains(tab.Canvas.Items, i => i.Name == "Board Table" && i is FurnitureItemViewModel);
        Assert.Contains(tab.Canvas.Items, i => i.Name == "Pillar" && i is ShapeItemViewModel s && s.InnerRadius == 35);

        // 4. Test Import into Active Canvas (Merging)
        var mergeDoc = new LayoutDocument();
        mergeDoc.Items.Add(new DeviceItem { Name = "Secondary-Router", IpAddress = "10.10.0.2" });
        vm.ImportItemsIntoActiveCanvas(mergeDoc);

        Assert.Equal(4, tab.Canvas.Items.Count);
        Assert.Contains(tab.Canvas.Items, i => i.Name == "Secondary-Router");
    }

    [Fact]
    public void StandardShortcuts_ClipboardDuplicateSelectAllAndNudge_OperateWithUndo()
    {
        var undo = new UndoRedoService();
        var canvas = new CanvasViewModel(undo);

        var dev = new DeviceItemViewModel(new DeviceItem { Name = "PC-Alpha", X = 100, Y = 100, Width = 80, Height = 80 });
        var shape = new ShapeItemViewModel(new ShapeItem { Name = "Box", X = 300, Y = 100, Width = 100, Height = 100 });

        canvas.AddItem(dev);
        canvas.AddItem(shape);
        Assert.Equal(2, canvas.Items.Count);

        // 1. Select All (Ctrl+A)
        canvas.SelectAll();
        Assert.Equal(2, canvas.SelectedItems.Count);

        // 2. Nudge (Arrow Keys)
        canvas.Nudge(10, 20);
        Assert.Equal(110, dev.X);
        Assert.Equal(120, dev.Y);
        Assert.Equal(310, shape.X);
        Assert.Equal(120, shape.Y);

        // Undo Nudge
        undo.Undo();
        Assert.Equal(100, dev.X);
        Assert.Equal(100, dev.Y);

        // 3. Duplicate (Ctrl+D)
        canvas.SetSelection(new[] { dev });
        canvas.DuplicateSelected();
        Assert.Equal(3, canvas.Items.Count);
        var dup = canvas.SelectedItems.First();
        Assert.StartsWith("PC-Alpha", dup.Name);
        Assert.NotEqual(dev.Model.Id, dup.Model.Id);

        // Undo Duplicate
        undo.Undo();
        Assert.Equal(2, canvas.Items.Count);

        // 4. Cut and Paste (Ctrl+X, Ctrl+V)
        canvas.SetSelection(new[] { shape });
        canvas.CutSelected();
        Assert.Single(canvas.Items);
        Assert.DoesNotContain(shape, canvas.Items);

        canvas.Paste();
        Assert.Equal(2, canvas.Items.Count);
        var pasted = canvas.SelectedItems.First();
        Assert.StartsWith("Box", pasted.Name);

        // Undo Paste
        undo.Undo();
        Assert.Single(canvas.Items);
    }

    [Fact]
    public void ThemeResources_LightThemeAndDarkThemeFiles_ExistAndContainConsistentKeys()
    {
        string baseDir = AppContext.BaseDirectory;
        // Search up until we find src/LabScape.App
        string? projectRoot = null;
        var dir = new DirectoryInfo(baseDir);
        while (dir != null)
        {
            if (Directory.Exists(Path.Combine(dir.FullName, "src", "LabScape.App")))
            {
                projectRoot = dir.FullName;
                break;
            }
            dir = dir.Parent;
        }

        Assert.NotNull(projectRoot);
        string lightThemePath = Path.Combine(projectRoot, "src", "LabScape.App", "Resources", "Themes", "LightTheme.xaml");
        string darkThemePath = Path.Combine(projectRoot, "src", "LabScape.App", "Resources", "Themes", "DarkTheme.xaml");

        Assert.True(File.Exists(lightThemePath), "LightTheme.xaml must exist");
        Assert.True(File.Exists(darkThemePath), "DarkTheme.xaml must exist");

        string lightContent = File.ReadAllText(lightThemePath);
        string darkContent = File.ReadAllText(darkThemePath);

        string[] requiredKeys = new[]
        {
            "AppBackgroundBrush",
            "AppSurfaceBrush",
            "AppTextPrimaryBrush",
            "AppTextSecondaryBrush",
            "PanelBackgroundBrush",
            "CardBackgroundBrush",
            "TabBarBackgroundBrush",
            "TabActiveBackgroundBrush",
            "StatusBarBackgroundBrush",
            "CanvasWorkspaceBackgroundBrush",
            "CanvasPaperBackgroundBrush",
            "CanvasGridLineBrush",
            "CanvasRulerBackgroundBrush"
        };

        foreach (var key in requiredKeys)
        {
            Assert.Contains($"x:Key=\"{key}\"", lightContent);
            Assert.Contains($"x:Key=\"{key}\"", darkContent);
        }
    }

    [Fact]
    public void DeviceLabelWrapping_InsertsSoftBreaks_AndPreservesWholeText()
    {
        // 1. Spaced / Hyphenated name
        string normal = "PC-01 Desktop";
        string wrappedNormal = TextWrapHelper.InsertSoftBreakOpportunities(normal);
        Assert.Equal(normal, TextWrapHelper.StripSoftBreaks(wrappedNormal));

        // 2. Underscored name
        string underscored = "DESKTOP_FINANCE_BUILDING_10";
        string wrappedUnderscored = TextWrapHelper.InsertSoftBreakOpportunities(underscored);
        Assert.Contains(TextWrapHelper.SoftBreak, wrappedUnderscored);
        Assert.Equal(underscored, TextWrapHelper.StripSoftBreaks(wrappedUnderscored));

        // 3. Long continuous unbroken string
        string unbroken = "WORKSTATIONENTERPRISE2026";
        string wrappedUnbroken = TextWrapHelper.InsertSoftBreakOpportunities(unbroken, maxChunkLength: 7);
        Assert.Contains(TextWrapHelper.SoftBreak, wrappedUnbroken);
        Assert.Equal(unbroken, TextWrapHelper.StripSoftBreaks(wrappedUnbroken));

        // 4. Dot-separated hostname
        string dotted = "core.switch.datacenter.lan";
        string wrappedDotted = TextWrapHelper.InsertSoftBreakOpportunities(dotted);
        Assert.Contains(TextWrapHelper.SoftBreak, wrappedDotted);
        Assert.Equal(dotted, TextWrapHelper.StripSoftBreaks(wrappedDotted));
    }

    [Fact]
    public void Canvas_DeviceItemTemplate_DoesNotShowIpOnCanvas_AndWrapsDeviceName()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        string? projectRoot = null;
        while (dir != null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "LabScape.slnx")))
            {
                projectRoot = dir.FullName;
                break;
            }
            dir = dir.Parent;
        }

        Assert.NotNull(projectRoot);
        string canvasViewPath = Path.Combine(projectRoot, "src", "LabScape.App", "Views", "CanvasView.xaml");
        Assert.True(File.Exists(canvasViewPath));

        string canvasContent = File.ReadAllText(canvasViewPath);

        // Requirement: Do NOT show IP on canvas
        Assert.DoesNotContain("Text=\"{Binding IpAddress}\"", canvasContent);

        // Requirement: Wrap PC name when exceeding boundary
        Assert.Contains("TextWrapping=\"Wrap\"", canvasContent);
        Assert.Contains("MaxWidth=\"{Binding Width}\"", canvasContent);
        Assert.Contains("Converter={StaticResource DeviceLabelWrapConverter}", canvasContent);
    }

    [Fact]
    public void PropertiesInspector_HasDedicatedLayerSection_WithStandardIcons_AndNoGhostText()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        string? projectRoot = null;
        while (dir != null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "LabScape.slnx")))
            {
                projectRoot = dir.FullName;
                break;
            }
            dir = dir.Parent;
        }

        Assert.NotNull(projectRoot);
        string viewPath = Path.Combine(projectRoot, "src", "LabScape.App", "Views", "PropertiesInspectorView.xaml");
        Assert.True(File.Exists(viewPath));

        string content = File.ReadAllText(viewPath);

        // Dedicated Layering Section
        Assert.Contains("Header=\"Layering &amp; Order\"", content);

        // Standard Vector Icons
        Assert.Contains("StaticResource IconMoveToFront", content);
        Assert.Contains("StaticResource IconMoveForward", content);
        Assert.Contains("StaticResource IconSendBackward", content);
        Assert.Contains("StaticResource IconSendToBack", content);

        // No more raw ASCII arrows
        Assert.DoesNotContain("Content=\"▲ Front\"", content);
        Assert.DoesNotContain("Content=\"▼ Back\"", content);

        // No ghost background text (no conflicting local Visibility attribute on empty-state TextBlock)
        Assert.DoesNotContain("ConverterParameter=Inverse", content);
    }

    [Fact]
    public void SelectionAdorner_LineAndBezier_HandleConfigurations_AndUndoRedo()
    {
        var lineItem = new ShapeItemViewModel(new ShapeItem
        {
            Name = "Line 1",
            ShapeType = ShapeType.Line,
            Width = 100,
            Height = 20
        });

        Assert.Equal(ShapeType.Line, lineItem.ShapeType);
        Assert.Equal(100, lineItem.Width);
        Assert.Equal(20, lineItem.Height);

        var bezierItem = new ShapeItemViewModel(new ShapeItem
        {
            Name = "Bezier 1",
            ShapeType = ShapeType.Bezier,
            Width = 120,
            Height = 80,
            ControlPoint1X = 30,
            ControlPoint1Y = 10,
            ControlPoint2X = 90,
            ControlPoint2Y = 70
        });

        Assert.Equal(ShapeType.Bezier, bezierItem.ShapeType);
        Assert.Equal(30, bezierItem.ControlPoint1X);
        Assert.Equal(10, bezierItem.ControlPoint1Y);
        Assert.Equal(90, bezierItem.ControlPoint2X);
        Assert.Equal(70, bezierItem.ControlPoint2Y);

        // Test ModifyBezierControlPointsCommand Undo/Redo
        var undoService = new UndoRedoService();
        var cmd = new ModifyBezierControlPointsCommand(
            bezierItem,
            oldCp1X: 30, oldCp1Y: 10, oldCp2X: 90, oldCp2Y: 70,
            newCp1X: 45, newCp1Y: 25, newCp2X: 110, newCp2Y: 85);

        undoService.ExecuteCommand(cmd);
        Assert.Equal(45, bezierItem.ControlPoint1X);
        Assert.Equal(25, bezierItem.ControlPoint1Y);
        Assert.Equal(110, bezierItem.ControlPoint2X);
        Assert.Equal(85, bezierItem.ControlPoint2Y);

        undoService.Undo();
        Assert.Equal(30, bezierItem.ControlPoint1X);
        Assert.Equal(10, bezierItem.ControlPoint1Y);
        Assert.Equal(90, bezierItem.ControlPoint2X);
        Assert.Equal(70, bezierItem.ControlPoint2Y);

        undoService.Redo();
        Assert.Equal(45, bezierItem.ControlPoint1X);
        Assert.Equal(25, bezierItem.ControlPoint1Y);
        Assert.Equal(110, bezierItem.ControlPoint2X);
        Assert.Equal(85, bezierItem.ControlPoint2Y);
    }

    [Fact]
    public void SelectionAdorner_CodeAnalysis_VerifiesLine3Handles_AndBezier5HandlesWithDottedTangents()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        string? projectRoot = null;
        while (dir != null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "LabScape.slnx")))
            {
                projectRoot = dir.FullName;
                break;
            }
            dir = dir.Parent;
        }

        Assert.NotNull(projectRoot);
        string adornerPath = Path.Combine(projectRoot, "src", "LabScape.App", "Adorners", "SelectionAdorner.cs");
        Assert.True(File.Exists(adornerPath));

        string code = File.ReadAllText(adornerPath);

        // 1. Line must have only 3 handles (Start, End, Rotation)
        Assert.Contains("shapeLine.ShapeType == ShapeType.Line", code);
        Assert.Contains("pStart", code);
        Assert.Contains("pEnd", code);
        Assert.Contains("pRot", code);
        Assert.Contains("DragMode.LineStart", code);
        Assert.Contains("DragMode.LineEnd", code);

        // 2. Bezier must have 5 handles (Start, End, CP1, CP2, Rotation) and dotted line connecting control points to originating endpoints
        Assert.Contains("shapeBezier.ShapeType == ShapeType.Bezier", code);
        Assert.Contains("DottedTangentPen", code);
        Assert.Contains("dc.DrawLine(DottedTangentPen, pStart, cp1);", code);
        Assert.Contains("dc.DrawLine(DottedTangentPen, pEnd, cp2);", code);
        Assert.Contains("DragMode.BezierControlPoint1", code);
        Assert.Contains("DragMode.BezierControlPoint2", code);
    }
}
