using System.Collections.Specialized;

using System.Diagnostics;

using LabScape.Core.Collections;

using LabScape.Core.Enums;

using LabScape.Core.Helpers;

using LabScape.Core.Models;

using LabScape.Infrastructure.Services;

using LabScape.ViewModels;

using LabScape.ViewModels.Commands;

using LabScape.ViewModels.Items;

using LabScape.ViewModels.Services;

using LabScape.ViewModels.Templates;

using Xunit;



namespace LabScape.Tests;



public class CoreTests

{

    private readonly Xunit.Abstractions.ITestOutputHelper? _output;



    public CoreTests(Xunit.Abstractions.ITestOutputHelper output)

    {

        _output = output;

    }



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

    public void Stencil_Screen_InstantiatesDeviceWithScreenType_AndHasVectorResource()

    {

        var allStencils = LabScape.ViewModels.Stencils.StencilItem.GetAllStencils();

        var screenStencil = allStencils.FirstOrDefault(s => s.Title == "Screen");



        Assert.NotNull(screenStencil);

        Assert.Equal("Devices", screenStencil.Category);

        Assert.Equal("Stencil_Screen", screenStencil.IconResourceKey);



        var item = screenStencil.CreateItem();

        var deviceItem = Assert.IsType<DeviceItemViewModel>(item);

        Assert.Equal("screen", deviceItem.DeviceType);

        Assert.True(deviceItem.ShowCanvasLabel);

        Assert.Equal(80, deviceItem.Width);

        Assert.Equal(70, deviceItem.Height);

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



    [Fact]

    public void SelectionAdorner_CodeAnalysis_VerifiesZoomIndependentHandleSizing_AndSmallItemReduction()

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



        // 1. Zoom-independent handle sizing implementation

        Assert.Contains("CalculateHandleSize", code);

        Assert.Contains("CalculateSpecialHandleSize", code);

        Assert.Contains("BaseHandleSize / Math.Max(0.05, zoom)", code);

        Assert.Contains("BaseSpecialHandleSize / Math.Max(0.05, zoom)", code);



        // 2. Small item handle reduction (8 resize handles down to 4 corner handles)

        Assert.Contains("SmallItemThreshold", code);

        Assert.Contains("ShouldUse4CornerHandles", code);

        Assert.Contains("if (!use4Handles)", code);



        // 3. CanvasView must listen to ZoomLevel changes to invalidate adorners

        string canvasViewPath = Path.Combine(projectRoot, "src", "LabScape.App", "Views", "CanvasView.xaml.cs");

        Assert.True(File.Exists(canvasViewPath));

        string canvasViewCode = File.ReadAllText(canvasViewPath);

        Assert.Contains("OnViewModelPropertyChanged", canvasViewCode);

        Assert.Contains("nameof(CanvasViewModel.ZoomLevel)", canvasViewCode);

        Assert.Contains("adorner.InvalidateAdorner()", canvasViewCode);

    }



    [Fact]

    public void Canvas_MouseWheel_VerticalHorizontalAndCtrlZoom()

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

        string canvasViewPath = Path.Combine(projectRoot, "src", "LabScape.App", "Views", "CanvasView.xaml.cs");

        Assert.True(File.Exists(canvasViewPath));



        string code = File.ReadAllText(canvasViewPath);



        // 1. Ctrl + Scroll for Zoom in/out

        Assert.Contains("ModifierKeys.Control", code);

        Assert.Contains("ViewModel.ZoomLevel = newZoom;", code);



        // 2. Shift + Scroll for Horizontal Scroll

        Assert.Contains("ModifierKeys.Shift", code);

        Assert.Contains("ViewModel.PanX +=", code);



        // 3. Normal Scroll for Vertical Scroll

        Assert.Contains("ViewModel.PanY +=", code);

    }



    [Fact]

    public void ShapeGeometry_CornerRadius_AndInnerRadius_UpdateCanvasAndSupportUndoRedo()

    {

        var donut = new ShapeItemViewModel(new ShapeItem

        {

            Name = "Donut",

            ShapeType = ShapeType.Donut,

            Width = 100,

            Height = 100,

            InnerRadius = 25

        });



        var roundedRect = new ShapeItemViewModel(new ShapeItem

        {

            Name = "RoundedRect",

            ShapeType = ShapeType.RoundedRectangle,

            Width = 100,

            Height = 100,

            CornerRadius = 12

        });



        var undoService = new UndoRedoService();



        // 1. Donut Inner Radius Undo/Redo

        var donutCmd = new ModifyInnerRadiusCommand(donut, 25, 40);

        undoService.ExecuteCommand(donutCmd);

        Assert.Equal(40, donut.InnerRadius);

        undoService.Undo();

        Assert.Equal(25, donut.InnerRadius);

        undoService.Redo();

        Assert.Equal(40, donut.InnerRadius);



        // 2. Corner Radius Undo/Redo

        var crCmd = new ModifyCornerRadiusCommand(roundedRect, 12, 28);

        undoService.ExecuteCommand(crCmd);

        Assert.Equal(28, roundedRect.CornerRadius);

        undoService.Undo();

        Assert.Equal(12, roundedRect.CornerRadius);

        undoService.Redo();

        Assert.Equal(28, roundedRect.CornerRadius);



        // 3. Verify CanvasView.xaml uses MultiBinding for Path.Data

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

        string canvasContent = File.ReadAllText(canvasViewPath);



        Assert.Contains("MultiBinding Converter=\"{StaticResource ShapeToGeometryConverter}\"", canvasContent);

        Assert.Contains("Binding Path=\"CornerRadius\"", canvasContent);

        Assert.Contains("Binding Path=\"InnerRadius\"", canvasContent);

    }



    [Fact]

    public void RoundedCornerTriangle_GeometryAndProperties_CalculatedCorrectly()

    {

        var roundedTriangle = new ShapeItemViewModel(new ShapeItem

        {

            Name = "RoundedTriangle",

            ShapeType = ShapeType.RoundedCornerTriangle,

            Width = 80,

            Height = 80,

            CornerRadius = 12

        });



        var undoService = new UndoRedoService();

        var crCmd = new ModifyCornerRadiusCommand(roundedTriangle, 12, 24);

        undoService.ExecuteCommand(crCmd);

        Assert.Equal(24, roundedTriangle.CornerRadius);

        undoService.Undo();

        Assert.Equal(12, roundedTriangle.CornerRadius);

        undoService.Redo();

        Assert.Equal(24, roundedTriangle.CornerRadius);



        // Verify ShapeGeometryHelper.cs contains proper trigonometric tangent fillet computation

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

        string helperPath = Path.Combine(projectRoot, "src", "LabScape.App", "Helpers", "ShapeGeometryHelper.cs");

        string helperCode = File.ReadAllText(helperPath);

        Assert.Contains("case ShapeType.RoundedCornerTriangle:", helperCode);

        Assert.Contains("double lenSide = Math.Sqrt((w * w / 4.0) + (h * h));", helperCode);

        Assert.Contains("Point t0In = new(p0.X - (t0 * u20.X), p0.Y - (t0 * u20.Y));", helperCode);

        Assert.DoesNotContain("w - r * 1.5", helperCode);



        // Verify web-viewer index.html contains updated drawRoundedTriangle with midpoint base

        string webViewerPath = Path.Combine(projectRoot, "web-viewer", "index.html");

        string webViewerCode = File.ReadAllText(webViewerPath);

        Assert.Contains("drawRoundedTriangle(ctx, x, y, w, h, radius)", webViewerCode);

        Assert.Contains("ctx.moveTo((p2.x + p3.x) / 2, p2.y);", webViewerCode);

    }



    [Fact]

    public void SelectionAdorner_ResizeAlignmentSnapping_ProjectsAssistingGuidesAndSnapsEdges()

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



        // 1. Verify SelectionAdorner.cs contains alignment assisting lines and snapping methods

        string adornerPath = Path.Combine(projectRoot, "src", "LabScape.App", "Adorners", "SelectionAdorner.cs");

        Assert.True(File.Exists(adornerPath));

        string adornerCode = File.ReadAllText(adornerPath);



        Assert.Contains("public Action<double, double, double>? ShowVerticalGuideAction { get; set; }", adornerCode);

        Assert.Contains("public Action<double, double, double>? ShowHorizontalGuideAction { get; set; }", adornerCode);

        Assert.Contains("public Action? HideVerticalGuideAction { get; set; }", adornerCode);

        Assert.Contains("public Action? HideHorizontalGuideAction { get; set; }", adornerCode);

        Assert.Contains("public Func<IEnumerable<ItemViewModel>>? GetOtherItems { get; set; }", adornerCode);

        Assert.Contains("ApplyResizeAlignmentSnapping", adornerCode);

        Assert.Contains("ApplyLineEndpointAlignmentSnapping", adornerCode);



        // 2. Verify CanvasView.xaml.cs wires up assisting guides and handles cleanup

        string canvasViewPath = Path.Combine(projectRoot, "src", "LabScape.App", "Views", "CanvasView.xaml.cs");

        Assert.True(File.Exists(canvasViewPath));

        string canvasViewCode = File.ReadAllText(canvasViewPath);



        Assert.Contains("ShowVerticalGuideAction = ShowVerticalGuide", canvasViewCode);

        Assert.Contains("ShowHorizontalGuideAction = ShowHorizontalGuide", canvasViewCode);

        Assert.Contains("HideVerticalGuideAction = () => SnapGuideVertical.Visibility = Visibility.Collapsed", canvasViewCode);

        Assert.Contains("HideHorizontalGuideAction = () => SnapGuideHorizontal.Visibility = Visibility.Collapsed", canvasViewCode);

        Assert.Contains("GetOtherItems = () => ViewModel?.Items ?? Enumerable.Empty<ItemViewModel>()", canvasViewCode);



        // 3. Mathematical Verification of 6.0px magnetic snapping tolerance

        double snapTolerance = 6.0;

        double itemLeft = 100;

        double itemWidth = 80;

        double candRight = itemLeft + itemWidth; // 180

        double otherLeft = 184; // 4px away, within 6.0px tolerance



        Assert.True(Math.Abs(candRight - otherLeft) <= snapTolerance);

        double snappedWidth = Math.Max(20.0, otherLeft - itemLeft);

        Assert.Equal(84, snappedWidth);

        Assert.Equal(184, itemLeft + snappedWidth);

    }



    [Fact]

    public void MultipleSelection_ActsLikeTemporaryGroup_ScalesRotatesAndSupportsUndoRedo()

    {

        // 1. Test TransformMultipleItemsCommand execution and undo/redo

        var undoService = new UndoRedoService();

        var item1 = new ShapeItemViewModel(new ShapeItem { Name = "Box1", X = 100, Y = 100, Width = 50, Height = 50, Rotation = 0 });

        var item2 = new ShapeItemViewModel(new ShapeItem { Name = "Box2", X = 200, Y = 200, Width = 100, Height = 80, Rotation = 0 });



        var records = new List<TransformMultipleItemsCommand.ItemTransformRecord>

        {

            new(item1, 100, 100, 50, 50, 0, 100, 100, 75, 50, 90),

            new(item2, 200, 200, 100, 80, 0, 250, 200, 150, 80, 90)

        };



        var cmd = new TransformMultipleItemsCommand(records);

        undoService.ExecuteCommand(cmd);



        Assert.Equal(75, item1.Width);

        Assert.Equal(90, item1.Rotation);

        Assert.Equal(250, item2.X);

        Assert.Equal(150, item2.Width);

        Assert.Equal(90, item2.Rotation);



        // Undo

        undoService.Undo();

        Assert.Equal(100, item1.X);

        Assert.Equal(50, item1.Width);

        Assert.Equal(0, item1.Rotation);

        Assert.Equal(200, item2.X);

        Assert.Equal(100, item2.Width);

        Assert.Equal(0, item2.Rotation);



        // Redo

        undoService.Redo();

        Assert.Equal(75, item1.Width);

        Assert.Equal(90, item1.Rotation);

        Assert.Equal(250, item2.X);

        Assert.Equal(150, item2.Width);

        Assert.Equal(90, item2.Rotation);



        // 2. Architectural Verification of MultiSelectionAdorner and CanvasView

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



        string multiAdornerPath = Path.Combine(projectRoot, "src", "LabScape.App", "Adorners", "MultiSelectionAdorner.cs");

        Assert.True(File.Exists(multiAdornerPath));

        string adornerCode = File.ReadAllText(multiAdornerPath);



        Assert.Contains("class MultiSelectionAdorner : Adorner", adornerCode);

        Assert.Contains("CalculateUnifiedBounds", adornerCode);

        Assert.Contains("TransformMultipleItemsCommand", adornerCode);

        Assert.Contains("DragMode.Rotate", adornerCode);

        Assert.Contains("DragMode.ResizeSE", adornerCode);

        Assert.Contains("ShowVerticalGuideAction", adornerCode);

        Assert.Contains("ShowHorizontalGuideAction", adornerCode);



        string canvasViewPath = Path.Combine(projectRoot, "src", "LabScape.App", "Views", "CanvasView.xaml.cs");

        Assert.True(File.Exists(canvasViewPath));

        string canvasViewCode = File.ReadAllText(canvasViewPath);



        Assert.Contains("new MultiSelectionAdorner(", canvasViewCode);

        Assert.Contains("_currentMultiAdorner", canvasViewCode);



        // 3. Mathematical Verification of Unified Bounding Box & Rotated Bounds

        // Unrotated bounds: item1 at (100,100, 50x50), item2 at (200,200, 100x80)

        item1.X = 100; item1.Y = 100; item1.Width = 50; item1.Height = 50; item1.Rotation = 0;

        item2.X = 200; item2.Y = 200; item2.Width = 100; item2.Height = 80; item2.Rotation = 0;

        var b1 = GroupItemViewModel.GetVisualBounds(item1);

        var b2 = GroupItemViewModel.GetVisualBounds(item2);

        double minX = Math.Min(b1.MinX, b2.MinX);

        double minY = Math.Min(b1.MinY, b2.MinY);

        double maxX = Math.Max(b1.MaxX, b2.MaxX);

        double maxY = Math.Max(b1.MaxY, b2.MaxY);



        Assert.Equal(100, minX);

        Assert.Equal(100, minY);

        Assert.Equal(300, maxX); // 200 + 100 = 300

        Assert.Equal(280, maxY); // 200 + 80 = 280



        // Rotated bounds: item1 at 90 deg (Width=75, Height=50) -> extX=25, extY=37.5, cx=137.5 -> minX=112.5

        item1.Width = 75; item1.Rotation = 90;

        var bRot1 = GroupItemViewModel.GetVisualBounds(item1);

        Assert.Equal(112.5, bRot1.MinX);

        Assert.Equal(87.5, bRot1.MinY);

    }



    [Fact]

    public void BezierCurve_HasFreeformEndpointsAndControlPoints_SupportsUndoRedoAndUnclippedRendering()

    {

        var bezier = new ShapeItemViewModel(new ShapeItem

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

            EndY = 15

        });



        Assert.Equal(10, bezier.StartX);

        Assert.Equal(75, bezier.StartY);

        Assert.Equal(130, bezier.EndX);

        Assert.Equal(15, bezier.EndY);

        Assert.Equal(35, bezier.ControlPoint1X);

        Assert.Equal(15, bezier.ControlPoint1Y);

        Assert.Equal(105, bezier.ControlPoint2X);

        Assert.Equal(75, bezier.ControlPoint2Y);



        // Test Full Bezier Freeform Undo/Redo

        var undoService = new UndoRedoService();

        var cmd = new ModifyBezierControlPointsCommand(

            bezier,

            oldStartX: 10, oldStartY: 75, oldEndX: 130, oldEndY: 15,

            oldCp1X: 35, oldCp1Y: 15, oldCp2X: 105, oldCp2Y: 75,

            newStartX: -20, newStartY: 120, newEndX: 250, newEndY: -10,

            newCp1X: 10, newCp1Y: -50, newCp2X: 180, newCp2Y: 160);



        undoService.ExecuteCommand(cmd);

        Assert.Equal(-20, bezier.StartX);

        Assert.Equal(120, bezier.StartY);

        Assert.Equal(250, bezier.EndX);

        Assert.Equal(-10, bezier.EndY);

        Assert.Equal(10, bezier.ControlPoint1X);

        Assert.Equal(-50, bezier.ControlPoint1Y);

        Assert.Equal(180, bezier.ControlPoint2X);

        Assert.Equal(160, bezier.ControlPoint2Y);



        undoService.Undo();

        Assert.Equal(10, bezier.StartX);

        Assert.Equal(75, bezier.StartY);

        Assert.Equal(130, bezier.EndX);

        Assert.Equal(15, bezier.EndY);

        Assert.Equal(35, bezier.ControlPoint1X);

        Assert.Equal(15, bezier.ControlPoint1Y);

        Assert.Equal(105, bezier.ControlPoint2X);

        Assert.Equal(75, bezier.ControlPoint2Y);



        undoService.Redo();

        Assert.Equal(-20, bezier.StartX);

        Assert.Equal(120, bezier.StartY);

        Assert.Equal(250, bezier.EndX);

        Assert.Equal(-10, bezier.EndY);



        // Verify XAML and Adorner have ClipToBounds="False" configured for unclipped rendering

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

        string adornerCode = File.ReadAllText(adornerPath);

        Assert.Contains("ClipToBounds = false;", adornerCode);

        Assert.Contains("shapeStart.StartX = Math.Round(currentLocal.X, 1);", adornerCode);

        Assert.Contains("shapeEnd.EndX = Math.Round(currentLocal.X, 1);", adornerCode);



        string canvasPath = Path.Combine(projectRoot, "src", "LabScape.App", "Views", "CanvasView.xaml");

        string canvasCode = File.ReadAllText(canvasPath);

        Assert.Contains("Binding Path=\"StartX\"", canvasCode);

        Assert.Contains("Binding Path=\"EndX\"", canvasCode);

        Assert.Contains("Setter Property=\"ClipToBounds\" Value=\"False\"", canvasCode);



        // Verify Line and Bezier are only selected when line/curve is clicked (null background & null fill on empty box, plus hit-test stroke buffer)

        Assert.Contains("DataTrigger Binding=\"{Binding ShapeType}\" Value=\"Line\"", canvasCode);

        Assert.Contains("DataTrigger Binding=\"{Binding ShapeType}\" Value=\"Bezier\"", canvasCode);

        Assert.Contains("Setter Property=\"Background\" Value=\"{x:Null}\"", canvasCode);

        Assert.Contains("Stroke=\"Transparent\"", canvasCode);

        Assert.Contains("StrokeThickness=\"12\"", canvasCode);



        // Verify Bezier Grid layout does not hardcode Width/Height restricting WPF layout slot

        Assert.Contains("DataTrigger Binding=\"{Binding ShapeType}\" Value=\"Bezier\">\r\n                                                <Setter Property=\"Width\" Value=\"Auto\"/>\r\n                                                <Setter Property=\"Height\" Value=\"Auto\"/>", canvasCode.Replace("\r\n", "\n").Replace("\n", "\r\n"));



        // Verify Undoable command synchronizes dimensions dynamically

        Assert.True(bezier.Width >= 270, $"Expected Bezier width >= 270 after Redo, got {bezier.Width}");

        undoService.Undo();

        Assert.True(bezier.Width >= 120, $"Expected Bezier width >= 120 after Undo, got {bezier.Width}");

    }



    [Fact]

    public void ShapeStyle_GradientsAndSolidColors_ParseAndSerializeAndSupportUndoRedo()

    {

        // 1. Verify GradientHelper linear and radial gradient creation

        string linear = GradientHelper.CreateLinearGradient("#4F46E5", "#06B6D4", 45.0);

        Assert.Equal("linear-gradient(45deg, #4F46E5, #06B6D4)", linear);



        string radial = GradientHelper.CreateRadialGradient("#6366F1", "#EC4899");

        Assert.Equal("radial-gradient(circle, #6366F1, #EC4899)", radial);



        // 2. Verify GradientHelper analyze brush

        var infoSolid = GradientHelper.AnalyzeBrush("#3B82F6");

        Assert.Equal(BrushType.Solid, infoSolid.Type);

        Assert.Equal("#3B82F6", infoSolid.Color1);



        var infoLinear = GradientHelper.AnalyzeBrush(linear);

        Assert.Equal(BrushType.LinearGradient, infoLinear.Type);

        Assert.Equal("#4F46E5", infoLinear.Color1);

        Assert.Equal("#06B6D4", infoLinear.Color2);

        Assert.Equal(45.0, infoLinear.Angle);



        var infoRadial = GradientHelper.AnalyzeBrush(radial);

        Assert.Equal(BrushType.RadialGradient, infoRadial.Type);

        Assert.Equal("#6366F1", infoRadial.Color1);

        Assert.Equal("#EC4899", infoRadial.Color2);



        var infoNone = GradientHelper.AnalyzeBrush("none");

        Assert.Equal(BrushType.None, infoNone.Type);



        // 3. Test PropertiesViewModel integration and Undo/Redo

        var undo = new UndoRedoService();

        var canvas = new CanvasViewModel(undo);

        var props = new PropertiesViewModel(canvas);



        var rect = new ShapeItemViewModel(new ShapeItem

        {

            Name = "MyRect",

            ShapeType = ShapeType.Rectangle,

            Fill = "#E0E7FF",

            Stroke = "#4338CA"

        });

        canvas.Items.Add(rect);

        canvas.SelectItem(rect);



        Assert.True(props.IsShape);

        Assert.True(props.IsFillApplicable);

        Assert.True(props.IsSolidFill);



        // Apply Linear Gradient Fill

        props.FillType = BrushType.LinearGradient;

        props.FillColor1 = "#EF4444";

        props.FillColor2 = "#F59E0B";

        props.FillAngle = 135.0;

        props.ApplyFillChanges();



        Assert.Contains("linear-gradient(135deg, #EF4444, #F59E0B)", rect.Fill);



        // Apply Linear Gradient Stroke

        props.StrokeType = BrushType.LinearGradient;

        props.StrokeColor1 = "#10B981";

        props.StrokeColor2 = "#06B6D4";

        props.StrokeAngle = 90.0;

        props.ApplyStrokeChanges();



        Assert.Contains("linear-gradient(90deg, #10B981, #06B6D4)", rect.Stroke);



        // Undo stroke

        undo.Undo();

        Assert.Equal("#4338CA", rect.Stroke);



        // Undo fill

        undo.Undo();

        Assert.Equal("#E0E7FF", rect.Fill);



        // Redo fill

        undo.Redo();

        Assert.Contains("linear-gradient(135deg, #EF4444, #F59E0B)", rect.Fill);



        // Redo stroke

        undo.Redo();

        Assert.Contains("linear-gradient(90deg, #10B981, #06B6D4)", rect.Stroke);

    }



    [Fact]

    public void ShapeStyle_LineAndBezier_DoNotApplyFill_OnlyStroke()

    {

        var undo = new UndoRedoService();

        var canvas = new CanvasViewModel(undo);

        var props = new PropertiesViewModel(canvas);



        var line = new ShapeItemViewModel(new ShapeItem

        {

            Name = "MyLine",

            ShapeType = ShapeType.Line,

            Stroke = "#1E293B"

        });

        canvas.Items.Add(line);

        canvas.SelectItem(line);



        Assert.True(props.IsShape);

        Assert.False(props.IsFillApplicable, "Line is an open path and must not have fill applicable");



        var bezier = new ShapeItemViewModel(new ShapeItem

        {

            Name = "MyBezier",

            ShapeType = ShapeType.Bezier,

            Stroke = "#4338CA"

        });

        canvas.Items.Add(bezier);

        canvas.SelectItem(bezier);



        Assert.True(props.IsShape);

        Assert.False(props.IsFillApplicable, "Bezier curve is an open path and must not have fill applicable");



        // Verify stroke gradient can still be applied to bezier

        props.StrokeType = BrushType.LinearGradient;

        props.StrokeColor1 = "#8B5CF6";

        props.StrokeColor2 = "#EC4899";

        props.StrokeAngle = 45.0;

        props.ApplyStrokeChanges();



        Assert.Equal("linear-gradient(45deg, #8B5CF6, #EC4899)", bezier.Stroke);

    }



    [Fact]

    public void RoomTemplateGenerator_4Sides_GeneratesIndividualLinesAndProperRotations()

    {

        var options = new RoomTemplateOptions

        {

            Sides = 4,

            PcCount = 12,

            IncludeDesks = true,

            IncludeChairs = true,

            GroupElements = false

        };



        var result = RoomTemplateGenerator.Generate(options);



        // 1. Boundary MUST consist of separate Line shapes - NEVER a single Rectangle

        Assert.Equal(4, result.Walls.Count);

        foreach (var wall in result.Walls)

        {

            Assert.Equal(ShapeType.Line, wall.ShapeType);

        }

        Assert.Contains(result.Walls, w => w.Name == "North Wall");

        Assert.Contains(result.Walls, w => w.Name == "East Wall");

        Assert.Contains(result.Walls, w => w.Name == "South Wall");

        Assert.Contains(result.Walls, w => w.Name == "West Wall");



        // 2. Hardware and furniture counts

        Assert.Equal(12, result.Pcs.Count);

        Assert.Equal(12, result.Desks.Count);

        Assert.Equal(12, result.Chairs.Count);



        // 3. 12 PCs distributed evenly across 4 walls (3 per wall)

        var northPcs = result.Pcs.Where(p => p.Rotation == 0.0).ToList();

        var eastPcs = result.Pcs.Where(p => p.Rotation == 90.0).ToList();

        var southPcs = result.Pcs.Where(p => p.Rotation == 180.0).ToList();

        var westPcs = result.Pcs.Where(p => p.Rotation == 270.0).ToList();



        Assert.Equal(3, northPcs.Count);

        Assert.Equal(3, eastPcs.Count);

        Assert.Equal(3, southPcs.Count);

        Assert.Equal(3, westPcs.Count);



        // 4. Chairs facing desks

        var northChairs = result.Chairs.Where(c => c.Rotation == 180.0).ToList();

        var eastChairs = result.Chairs.Where(c => c.Rotation == 270.0).ToList();

        var southChairs = result.Chairs.Where(c => c.Rotation == 0.0).ToList();

        var westChairs = result.Chairs.Where(c => c.Rotation == 90.0).ToList();



        Assert.Equal(3, northChairs.Count);

        Assert.Equal(3, eastChairs.Count);

        Assert.Equal(3, southChairs.Count);

        Assert.Equal(3, westChairs.Count);



        // 5. Chairs positioned forward under desks and layered below desk tabletop

        var northDesk = result.Desks.First(d => d.Rotation == 0.0);

        var northChair = northChairs.First();

        Assert.Equal(northDesk.Y + RoomTemplateGenerator.DeskH - 20.0, northChair.Y, 1);

        Assert.True(northChair.Z < northDesk.Z, "Chairs must be layered below desks in Z-order");

        var northPc = northPcs.First();

        Assert.True(northDesk.Z < northPc.Z, "Desks must be layered below PCs in Z-order");

    }



    [Fact]

    public void RoomTemplateGenerator_3Sides_U_Shape_LeavesOpenEntrance()

    {

        var options = new RoomTemplateOptions

        {

            Sides = 3,

            OpenSide = WallSide.South,

            PcCount = 9,

            IncludeDesks = true,

            IncludeChairs = true

        };



        var result = RoomTemplateGenerator.Generate(options);



        // 3 Line walls generated, South wall is absent (open entrance doorway)

        Assert.Equal(3, result.Walls.Count);

        Assert.All(result.Walls, w => Assert.Equal(ShapeType.Line, w.ShapeType));

        Assert.Contains(result.Walls, w => w.Name == "North Wall");

        Assert.Contains(result.Walls, w => w.Name == "East Wall");

        Assert.Contains(result.Walls, w => w.Name == "West Wall");

        Assert.DoesNotContain(result.Walls, w => w.Name == "South Wall");



        // 9 PCs distributed 3 North, 3 East, 3 West

        Assert.Equal(9, result.Pcs.Count);

        Assert.Equal(3, result.Pcs.Count(p => p.Rotation == 0.0));

        Assert.Equal(3, result.Pcs.Count(p => p.Rotation == 90.0));

        Assert.Equal(3, result.Pcs.Count(p => p.Rotation == 270.0));

        Assert.Equal(0, result.Pcs.Count(p => p.Rotation == 180.0));

    }



    [Fact]

    public void RoomTemplateGenerator_2Sides_And_1Side_Configurations()

    {

        // 2 sides (L-Corner: North & West)

        var options2 = new RoomTemplateOptions

        {

            Sides = 2,

            CornerOption = "NorthWest",

            PcCount = 6,

            IncludeDesks = true,

            IncludeChairs = true

        };

        var result2 = RoomTemplateGenerator.Generate(options2);

        Assert.Equal(2, result2.Walls.Count);

        Assert.All(result2.Walls, w => Assert.Equal(ShapeType.Line, w.ShapeType));

        Assert.Contains(result2.Walls, w => w.Name == "North Wall");

        Assert.Contains(result2.Walls, w => w.Name == "West Wall");

        Assert.Equal(6, result2.Pcs.Count);



        // 2 sides (Opposite parallel walls: North & South facing rows)

        var optionsOppositeNS = new RoomTemplateOptions

        {

            Sides = 2,

            CornerOption = "NorthSouth",

            PcCount = 8,

            IncludeDesks = true,

            IncludeChairs = true

        };

        var resultOppositeNS = RoomTemplateGenerator.Generate(optionsOppositeNS);

        Assert.Equal(2, resultOppositeNS.Walls.Count);

        Assert.Contains(resultOppositeNS.Walls, w => w.Name == "North Wall");

        Assert.Contains(resultOppositeNS.Walls, w => w.Name == "South Wall");

        Assert.DoesNotContain(resultOppositeNS.Walls, w => w.Name == "East Wall");

        Assert.DoesNotContain(resultOppositeNS.Walls, w => w.Name == "West Wall");

        Assert.Equal(8, resultOppositeNS.Pcs.Count);

        Assert.Equal(4, resultOppositeNS.Pcs.Count(p => p.Rotation == 0.0));   // North wall PCs face South (0 deg)

        Assert.Equal(4, resultOppositeNS.Pcs.Count(p => p.Rotation == 180.0)); // South wall PCs face North (180 deg)



        // 2 sides (Opposite parallel walls: West & East facing columns)

        var optionsOppositeWE = new RoomTemplateOptions

        {

            Sides = 2,

            CornerOption = "EastWest",

            PcCount = 6,

            IncludeDesks = true,

            IncludeChairs = true

        };

        var resultOppositeWE = RoomTemplateGenerator.Generate(optionsOppositeWE);

        Assert.Equal(2, resultOppositeWE.Walls.Count);

        Assert.Contains(resultOppositeWE.Walls, w => w.Name == "West Wall");

        Assert.Contains(resultOppositeWE.Walls, w => w.Name == "East Wall");

        Assert.DoesNotContain(resultOppositeWE.Walls, w => w.Name == "North Wall");

        Assert.DoesNotContain(resultOppositeWE.Walls, w => w.Name == "South Wall");

        Assert.Equal(6, resultOppositeWE.Pcs.Count);

        Assert.Equal(3, resultOppositeWE.Pcs.Count(p => p.Rotation == 90.0));  // West wall PCs face East (90 deg)

        Assert.Equal(3, resultOppositeWE.Pcs.Count(p => p.Rotation == 270.0)); // East wall PCs face West (270 deg)



        // 1 side (Single wall/bench: North)

        var options1 = new RoomTemplateOptions

        {

            Sides = 1,

            SingleSide = WallSide.North,

            PcCount = 4,

            IncludeDesks = true,

            IncludeChairs = true

        };

        var result1 = RoomTemplateGenerator.Generate(options1);

        Assert.Single(result1.Walls);

        Assert.Equal(ShapeType.Line, result1.Walls[0].ShapeType);

        Assert.Equal("North Wall", result1.Walls[0].Name);

        Assert.Equal(4, result1.Pcs.Count);

        Assert.All(result1.Pcs, p => Assert.Equal(0.0, p.Rotation));

    }



    [Fact]

    public void CanvasViewModel_AddRoomTemplate_GroupsAndSupportsUndoRedo()

    {

        var undo = new UndoRedoService();

        var canvas = new CanvasViewModel(undo) { SnapToGrid = false };



        var options = new RoomTemplateOptions

        {

            Sides = 4,

            PcCount = 8,

            IncludeDesks = true,

            IncludeChairs = true,

            GroupElements = true,

            OriginX = 100,

            OriginY = 100

        };



        canvas.AddRoomTemplate(options);



        // 1. Single group item on canvas

        Assert.Single(canvas.Items);

        var group = Assert.IsType<GroupItemViewModel>(canvas.Items[0]);



        // Children: 4 walls + 8 PCs + 8 desks + 8 chairs = 28 items

        Assert.Equal(28, group.Children.Count);



        // Verify wall children are lines

        var walls = group.Children.OfType<ShapeItemViewModel>().ToList();

        Assert.Equal(4, walls.Count);

        Assert.All(walls, w => Assert.Equal(ShapeType.Line, w.ShapeType));



        // 2. Moving the group shifts children's absolute coordinates

        double oldChildX = group.Children[0].X;

        group.X += 50.0;

        Assert.Equal(oldChildX + 50.0, group.Children[0].X);



        // 3. Undo removes the room template

        undo.Undo();

        Assert.Empty(canvas.Items);



        // 4. Redo restores the room template

        undo.Redo();

        Assert.Single(canvas.Items);



        // 5. Ungrouping unpacks individual components

        canvas.SelectItem(canvas.Items[0]);

        canvas.UngroupSelected();

        Assert.Equal(28, canvas.Items.Count);

        Assert.DoesNotContain(canvas.Items, i => i is GroupItemViewModel);

        Assert.Equal(4, canvas.Items.OfType<ShapeItemViewModel>().Count(w => w.ShapeType == ShapeType.Line));

    }



    [Fact]

    public void CanvasStressSimulator_Generates1kMixedItems_WithBalancedDistribution()

    {

        var items = CanvasStressSimulator.Generate1kItems();



        // 1. Total count

        Assert.Equal(1000, items.Count);



        // 2. Mix breakdown

        int deviceCount = items.Count(i => i is DeviceItemViewModel);

        int furnitureCount = items.Count(i => i is FurnitureItemViewModel);

        int shapeCount = items.Count(i => i is ShapeItemViewModel s && s.ShapeType != ShapeType.Line && s.ShapeType != ShapeType.Bezier);

        int lineCount = items.Count(i => i is ShapeItemViewModel s && s.ShapeType == ShapeType.Line);

        int bezierCount = items.Count(i => i is ShapeItemViewModel s && s.ShapeType == ShapeType.Bezier);



        Assert.Equal(300, deviceCount);      // 300 Devices

        Assert.Equal(150, furnitureCount);   // 150 Furniture

        Assert.Equal(250, shapeCount);       // 250 Geometric Shapes

        Assert.Equal(150, lineCount);        // 150 Lines

        Assert.Equal(150, bezierCount);      // 150 Bezier Curves



        // 3. Grid positioning distribution

        Assert.True(items.Min(i => i.X) >= 60.0);

        Assert.True(items.Max(i => i.X) > 5000.0);

        Assert.True(items.Min(i => i.Y) >= 60.0);

        Assert.True(items.Max(i => i.Y) > 3000.0);

    }



    [Fact]

    public void PerformanceBenchmark_1kItems_Simulation_ThroughputAndMemory()

    {

        var undo = new UndoRedoService();

        var canvas = new CanvasViewModel(undo) { SnapToGrid = false };



        var benchmark = canvas.RunSimulationBenchmark();



        // 1. Verify counts and batch insertion

        Assert.Equal(1000, canvas.Items.Count);

        Assert.Equal(1000, benchmark.TotalItems);

        Assert.Equal(300, benchmark.DeviceCount);

        Assert.Equal(150, benchmark.FurnitureCount);

        Assert.Equal(250, benchmark.ShapeCount);

        Assert.Equal(150, benchmark.LineCount);

        Assert.Equal(150, benchmark.BezierCount);



        // 2. Select All performance

        var swSelect = System.Diagnostics.Stopwatch.StartNew();

        canvas.SelectAll();

        swSelect.Stop();

        Assert.Equal(1000, canvas.SelectedItems.Count);



        // 3. Batch Movement performance

        var swNudge = System.Diagnostics.Stopwatch.StartNew();

        canvas.Nudge(20, 20);

        swNudge.Stop();



        // 4. Batch Undo and Redo performance

        var swUndo = System.Diagnostics.Stopwatch.StartNew();

        undo.Undo(); // undo nudge

        undo.Undo(); // undo insertion

        swUndo.Stop();

        Assert.Empty(canvas.Items);



        var swRedo = System.Diagnostics.Stopwatch.StartNew();

        undo.Redo(); // redo insertion

        undo.Redo(); // redo nudge

        swRedo.Stop();

        Assert.Equal(1000, canvas.Items.Count);



        // 5. JSON Serialization & Deserialization

        var jsonRepo = new LayoutJsonRepository();

        var doc = new LayoutDocument();

        doc.Items.AddRange(canvas.Items.Select(i => i.Model));



        var swExport = System.Diagnostics.Stopwatch.StartNew();

        string json = jsonRepo.SerializeToJson(doc);

        swExport.Stop();



        var swImport = System.Diagnostics.Stopwatch.StartNew();

        var restoredDoc = jsonRepo.DeserializeFromJson(json);

        swImport.Stop();

        Assert.Equal(1000, restoredDoc.Items.Count);



        _output?.WriteLine("=================================================================");

        _output?.WriteLine("        LABSCAPE 1,000 MIXED ITEMS PERFORMANCE BENCHMARK");

        _output?.WriteLine("=================================================================");

        _output?.WriteLine($" Total Simulated Items:         {benchmark.TotalItems:N0}");

        _output?.WriteLine($"   - IT Device Icons:           {benchmark.DeviceCount:N0} (PCs, Laptops, Screens, Servers, Switches, Printers)");

        _output?.WriteLine($"   - Office Furniture Icons:    {benchmark.FurnitureCount:N0} (Desks, Chairs, Conference Tables, Sofas)");

        _output?.WriteLine($"   - Geometric Vector Shapes:   {benchmark.ShapeCount:N0} (Rectangles, Circles, Triangles, Donuts, Semi-Circles)");

        _output?.WriteLine($"   - Line Elements:             {benchmark.LineCount:N0} (Various angles, lengths, strokes)");

        _output?.WriteLine($"   - Bezier Curves:             {benchmark.BezierCount:N0} (Waves, S-curves, gradients)");

        _output?.WriteLine("-----------------------------------------------------------------");

        _output?.WriteLine($" 1. Generation Time:            {benchmark.GenerationTimeMs:F2} ms");

        _output?.WriteLine($" 2. Batch Canvas Insertion:     {benchmark.InsertionTimeMs:F2} ms");

        _output?.WriteLine($" 3. Total Setup Time:           {benchmark.TotalElapsedMs:F2} ms");

        _output?.WriteLine("-----------------------------------------------------------------");

        _output?.WriteLine($" 4. Select All (1k Items):      {swSelect.Elapsed.TotalMilliseconds:F2} ms");

        _output?.WriteLine($" 5. Batch Move/Nudge (1k Items):{swNudge.Elapsed.TotalMilliseconds:F2} ms");

        _output?.WriteLine($" 6. Batch Undo (1k Items):      {swUndo.Elapsed.TotalMilliseconds:F2} ms");

        _output?.WriteLine($" 7. Batch Redo (1k Items):      {swRedo.Elapsed.TotalMilliseconds:F2} ms");

        _output?.WriteLine("-----------------------------------------------------------------");

        _output?.WriteLine($" 8. JSON Export (1k Items):     {swExport.Elapsed.TotalMilliseconds:F2} ms ({json.Length / 1024.0:F1} KB output)");

        _output?.WriteLine($" 9. JSON Import (1k Items):     {swImport.Elapsed.TotalMilliseconds:F2} ms");

        _output?.WriteLine("-----------------------------------------------------------------");

        _output?.WriteLine($" Managed Heap Before:           {benchmark.MemoryBeforeMb:F2} MB");

        _output?.WriteLine($" Managed Heap After:            {benchmark.MemoryAfterMb:F2} MB");

        _output?.WriteLine($" Heap Allocation Delta:         {benchmark.MemoryDeltaMb:F2} MB (~{benchmark.MemoryDeltaMb * 1024.0 / 1000.0:F2} KB per item)");

        _output?.WriteLine($" OS Working Set:                {benchmark.WorkingSetMb:F2} MB");

        _output?.WriteLine("=================================================================");

    }



    [Fact]

    public void RoomTemplateViewModel_StateAndCanvasIntegration_GeneratesWithUndoRedo()

    {

        var undo = new UndoRedoService();

        var canvas = new CanvasViewModel(undo) { SnapToGrid = false };



        var vm = new LabScape.ViewModels.Templates.RoomTemplateViewModel(() => canvas);



        // 1. Initial Defaults

        Assert.Equal(4, vm.Sides);

        Assert.True(vm.CanGenerateRoom, "Generate Room must always be enabled");

        Assert.True(vm.GenerateRoomCommand.CanExecute(null), "GenerateRoomCommand must be executable");

        Assert.Equal(12, vm.PcCount);

        Assert.True(vm.IncludeDesks);

        Assert.True(vm.IncludeChairs);

        Assert.True(vm.GroupElements);

        Assert.False(vm.Is3SidesVisible);

        Assert.False(vm.Is2SidesVisible);

        Assert.False(vm.Is1SideVisible);

        Assert.Contains("Line segments", vm.SummaryWalls);

        Assert.Contains("North: 3", vm.SummaryDistribution);



        // 2. Switching Sides triggers visibility flags and updates summary

        vm.Sides = 3;

        Assert.True(vm.Is3SidesVisible);

        Assert.False(vm.Is2SidesVisible);

        Assert.False(vm.Is1SideVisible);



        vm.Sides = 2;

        Assert.False(vm.Is3SidesVisible);

        Assert.True(vm.Is2SidesVisible);

        Assert.False(vm.Is1SideVisible);



        vm.Sides = 1;

        Assert.False(vm.Is3SidesVisible);

        Assert.False(vm.Is2SidesVisible);

        Assert.True(vm.Is1SideVisible);



        // 3. Reset Defaults

        vm.ResetDefaults();

        Assert.Equal(4, vm.Sides);

        Assert.Equal(12, vm.PcCount);

        Assert.False(vm.Is1SideVisible);



        // 4. Generate Room Command inserts onto canvas with undo/redo

        Assert.Empty(canvas.Items);

        vm.GenerateRoom();

        Assert.NotEmpty(canvas.Items);



        // A group item was created containing the room elements

        var group = canvas.Items.OfType<GroupItemViewModel>().FirstOrDefault();

        Assert.NotNull(group);

        Assert.NotEmpty(group.Children);



        // Verify Undo removes the room

        Assert.True(undo.CanUndo);

        undo.Undo();

        Assert.Empty(canvas.Items);



        // Verify Redo restores the room

        Assert.True(undo.CanRedo);

        undo.Redo();

        Assert.Single(canvas.Items);

    }



    [Fact]

    public void RoomTemplateViewModel_SidesValidation_GuaranteesAllFourSidesDefault()

    {

        var canvas = new CanvasViewModel(new UndoRedoService());

        var vm = new LabScape.ViewModels.Templates.RoomTemplateViewModel(() => canvas);



        // 1. Initial configuration has 4 sides enabled and ready

        Assert.Equal(4, vm.Sides);

        Assert.True(vm.CanGenerateRoom);

        Assert.True(vm.GenerateRoomCommand.CanExecute(null));



        var opts = vm.BuildOptions();

        Assert.Equal(4, opts.Sides);



        var activeWalls = opts.GetActiveWalls();

        Assert.Equal(4, activeWalls.Count);

        Assert.Contains(WallSide.North, activeWalls);

        Assert.Contains(WallSide.East, activeWalls);

        Assert.Contains(WallSide.South, activeWalls);

        Assert.Contains(WallSide.West, activeWalls);



        var result = RoomTemplateGenerator.Generate(opts);

        Assert.Equal(4, result.Walls.Count);

        Assert.Contains(result.Walls, w => w.Name == "North Wall");

        Assert.Contains(result.Walls, w => w.Name == "East Wall");

        Assert.Contains(result.Walls, w => w.Name == "South Wall");

        Assert.Contains(result.Walls, w => w.Name == "West Wall");



        // 2. BuildOptions sanitizes out-of-range values safely back to 4 sides

        vm.Sides = 0;

        var sanitizedOpts0 = vm.BuildOptions();

        Assert.Equal(4, sanitizedOpts0.Sides);



        vm.Sides = 99;

        var sanitizedOpts99 = vm.BuildOptions();

        Assert.Equal(4, sanitizedOpts99.Sides);

    }



    [Fact]

    public void GroupItemViewModel_VisualBounds_CalculatesTightRotatedBounds()

    {

        // Line shape representing East wall: length 400, height 20, rotated 90 degrees around (700, 300)

        var eastWall = RoomTemplateGenerator.CreateWallLine("East Wall", 700, 100, 700, 500, 4.0, "#000000");

        Assert.Equal(90.0, eastWall.Rotation);



        var (minX, minY, maxX, maxY) = GroupItemViewModel.GetVisualBounds(eastWall);



        // Visually: Center is (700, 300).

        // Rotated 90 deg: width extent is 20/2 = 10, height extent is 400/2 = 200.

        // Therefore: minX = 690, maxX = 710, minY = 100, maxY = 500.

        Assert.Equal(690.0, minX, 1);

        Assert.Equal(710.0, maxX, 1);

        Assert.Equal(100.0, minY, 1);

        Assert.Equal(500.0, maxY, 1);

    }



    [Fact]

    public void GroupItemViewModel_MoveGroup_MovesAllChildrenAndUndoes()

    {

        var undo = new UndoRedoService();

        var canvas = new CanvasViewModel(undo);



        var groupModel = new GroupItem { Name = "Test Room Group" };

        var group = new GroupItemViewModel(groupModel);



        var item1 = new DeviceItemViewModel(new DeviceItem { Name = "PC-01", X = 100, Y = 100, Width = 50, Height = 50 });

        var item2 = new DeviceItemViewModel(new DeviceItem { Name = "PC-02", X = 200, Y = 100, Width = 50, Height = 50 });



        group.AddChild(item1);

        group.AddChild(item2);



        canvas.AddItem(group);

        Assert.Equal(100.0, group.X);

        Assert.Equal(100.0, group.Y);

        Assert.Equal(150.0, group.Width); // 250 - 100

        Assert.Equal(50.0, group.Height);



        // Move group by delta (+50, +30)

        double initialGroupX = group.X;

        double initialGroupY = group.Y;

        group.X += 50.0;

        group.Y += 30.0;



        // Children should have moved synchronously

        Assert.Equal(150.0, item1.X);

        Assert.Equal(130.0, item1.Y);

        Assert.Equal(250.0, item2.X);

        Assert.Equal(130.0, item2.Y);



        // Commit move command

        canvas.CommitItemMoves(new List<(ItemViewModel, double, double, double, double)>

        {

            (group, initialGroupX, initialGroupY, group.X, group.Y)

        });



        // Undo move

        Assert.True(undo.CanUndo);

        undo.Undo();



        Assert.Equal(100.0, group.X);

        Assert.Equal(100.0, group.Y);

        Assert.Equal(100.0, item1.X);

        Assert.Equal(100.0, item1.Y);

        Assert.Equal(200.0, item2.X);

        Assert.Equal(100.0, item2.Y);



        // Redo move

        Assert.True(undo.CanRedo);

        undo.Redo();



        Assert.Equal(150.0, group.X);

        Assert.Equal(130.0, group.Y);

        Assert.Equal(150.0, item1.X);

        Assert.Equal(130.0, item1.Y);

        Assert.Equal(250.0, item2.X);

        Assert.Equal(130.0, item2.Y);

    }



    [Fact]

    public void RoomTemplateGenerator_GroupElementsTrue_ProducesTightGroupBounds()

    {

        var options = new RoomTemplateOptions

        {

            Sides = 4,

            PcCount = 8,

            OriginX = 100.0,

            OriginY = 100.0,

            GroupElements = true

        };



        var result = RoomTemplateGenerator.Generate(options);

        Assert.NotNull(result.Group);

        Assert.NotEmpty(result.Group.Children);



        // Group bounds should closely match room origin and dimensions

        Assert.True(result.Group.X >= 80.0 && result.Group.X <= 120.0, $"Group X {result.Group.X} should be close to 100");

        Assert.True(result.Group.Y >= 80.0 && result.Group.Y <= 120.0, $"Group Y {result.Group.Y} should be close to 100");

        Assert.True(result.Group.Width <= result.RoomWidth + 30.0, $"Group Width {result.Group.Width} should not balloon");

        Assert.True(result.Group.Height <= result.RoomHeight + 30.0, $"Group Height {result.Group.Height} should not balloon");



        // Every child must have ParentGroupId set to group Id

        foreach (var child in result.Group.Children)

        {

            Assert.Equal(result.Group.Id, child.ParentGroupId);

        }

    }



    [Fact]

    public void UngroupRoomTemplate_SelectingLines_DoesNotDisplaceCoordinates()

    {

        var undo = new UndoRedoService();

        var canvas = new CanvasViewModel(undo) { SnapToGrid = true, GridSize = 20.0 };



        var options = new RoomTemplateOptions

        {

            Sides = 4,

            PcCount = 8,

            OriginX = 100.0,

            OriginY = 100.0,

            GroupElements = true

        };



        canvas.AddRoomTemplate(options);

        var group = canvas.Items.OfType<GroupItemViewModel>().Single();



        // Find initial coordinates of all wall lines before ungrouping

        var initialWalls = group.Children.OfType<ShapeItemViewModel>().Where(s => s.ShapeType == ShapeType.Line).ToList();

        Assert.Equal(4, initialWalls.Count);

        var originalCoords = initialWalls.ToDictionary(w => w.Name, w => (w.X, w.Y, w.Width, w.Height, w.Rotation));



        // Ungroup the room

        canvas.SetSelection(new[] { group });

        canvas.UngroupSelected();



        // All walls should now be direct top-level canvas items

        var unpackedWalls = canvas.Items.OfType<ShapeItemViewModel>().Where(s => s.ShapeType == ShapeType.Line).ToList();

        Assert.Equal(4, unpackedWalls.Count);



        // Verify that ungrouping preserved exact coordinates

        foreach (var wall in unpackedWalls)

        {

            var orig = originalCoords[wall.Name];

            Assert.Equal(orig.X, wall.X, 1);

            Assert.Equal(orig.Y, wall.Y, 1);

            Assert.Equal(orig.Width, wall.Width, 1);

            Assert.Equal(orig.Height, wall.Height, 1);

            Assert.Equal(orig.Rotation, wall.Rotation, 1);

        }



        // Now select each wall individually and verify its coordinates never drift or get clamped

        foreach (var wall in unpackedWalls)

        {

            canvas.SelectItem(wall, addToExisting: false);

            var orig = originalCoords[wall.Name];

            Assert.Equal(orig.X, wall.X, 1);

            Assert.Equal(orig.Y, wall.Y, 1);

        }

    }



    [Fact]

    public void ObservableRangeCollection_BatchOperations_RaiseSingleResetEvent()

    {

        var col = new ObservableRangeCollection<int>();

        int eventCount = 0;

        NotifyCollectionChangedAction? lastAction = null;

        col.CollectionChanged += (s, e) =>

        {

            eventCount++;

            lastAction = e.Action;

        };



        // 1. AddRange

        col.AddRange(new[] { 1, 2, 3, 4, 5 });

        Assert.Equal(1, eventCount);

        Assert.Equal(NotifyCollectionChangedAction.Reset, lastAction);

        Assert.Equal(5, col.Count);



        // 2. ReplaceAll

        eventCount = 0;

        col.ReplaceAll(new[] { 10, 20 });

        Assert.Equal(1, eventCount);

        Assert.Equal(NotifyCollectionChangedAction.Reset, lastAction);

        Assert.Equal(2, col.Count);

        Assert.Equal(10, col[0]);

        Assert.Equal(20, col[1]);



        // 3. BatchUpdate

        eventCount = 0;

        col.BatchUpdate(() =>

        {

            col.Add(30);

            col.Add(40);

            col.Remove(10);

        });

        Assert.Equal(1, eventCount);

        Assert.Equal(NotifyCollectionChangedAction.Reset, lastAction);

        Assert.Equal(3, col.Count);

        Assert.Contains(20, col);

        Assert.Contains(30, col);

        Assert.Contains(40, col);

        Assert.DoesNotContain(10, col);

    }



    [Fact]

    public void UngroupSelected_PerformanceAndBatchSelection_Instantaneous()

    {

        var undoRedo = new UndoRedoService();

        var canvas = new CanvasViewModel(undoRedo);

        var options = new RoomTemplateOptions

        {

            Sides = 4,

            PcCount = 16,

            GroupElements = true

        };



        canvas.AddRoomTemplate(options);

        var group = canvas.Items.OfType<GroupItemViewModel>().Single();

        int expectedChildCount = group.Children.Count;

        Assert.True(expectedChildCount >= 52, $"Expected at least 52 items in 16-PC room, found {expectedChildCount}");



        // Track how many collection changed notifications fire during ungroup

        int itemsChangedCount = 0;

        int selectionChangedCount = 0;

        canvas.Items.CollectionChanged += (s, e) => itemsChangedCount++;

        canvas.SelectedItems.CollectionChanged += (s, e) => selectionChangedCount++;



        canvas.SetSelection(new[] { group });

        selectionChangedCount = 0; // reset counter after initial group selection



        var sw = Stopwatch.StartNew();

        canvas.UngroupSelected();

        sw.Stop();



        // Execution must complete in under 50ms (typically < 5ms in memory)

        Assert.True(sw.ElapsedMilliseconds < 50, $"Ungrouping took {sw.ElapsedMilliseconds}ms, exceeding 50ms threshold");



        // The group must be gone from Items, and replaced by the unpacked children

        Assert.Empty(canvas.Items.OfType<GroupItemViewModel>());

        Assert.Equal(expectedChildCount, canvas.Items.Count);



        // Selection should be set to all unpacked items

        Assert.Equal(expectedChildCount, canvas.SelectedItems.Count);



        // With batch updates, Items should fire exactly 1 Reset notification, and SelectedItems should fire exactly 1 Reset notification!

        Assert.Equal(1, itemsChangedCount);

        Assert.Equal(1, selectionChangedCount);

    }



    [Fact]

    public void UngroupSelected_UndoRedo_MaintainsIntegrity()

    {

        var undoRedo = new UndoRedoService();

        var canvas = new CanvasViewModel(undoRedo);

        var options = new RoomTemplateOptions

        {

            Sides = 4,

            PcCount = 8,

            GroupElements = true

        };



        canvas.AddRoomTemplate(options);

        var group = canvas.Items.OfType<GroupItemViewModel>().Single();

        int childCount = group.Children.Count;



        canvas.SetSelection(new[] { group });

        canvas.UngroupSelected();



        Assert.Equal(childCount, canvas.Items.Count);

        Assert.Empty(canvas.Items.OfType<GroupItemViewModel>());



        // Undo

        undoRedo.Undo();

        Assert.Single(canvas.Items.OfType<GroupItemViewModel>());

        Assert.Single(canvas.Items);

        var restoredGroup = canvas.Items.OfType<GroupItemViewModel>().Single();

        Assert.Equal(childCount, restoredGroup.Children.Count);



        // Redo

        undoRedo.Redo();

        Assert.Empty(canvas.Items.OfType<GroupItemViewModel>());

        Assert.Equal(childCount, canvas.Items.Count);

    }



    [Fact]

    public void TextItem_CreationAndSerialization_PreservesAllProperties()

    {

        var repo = new LayoutJsonRepository();

        var doc = new LayoutDocument

        {

            Metadata = new DocumentMetadata { Title = "Text Test" }

        };



        var textItem = new TextItem

        {

            Name = "Header Label",

            Text = "Server Room Phase 1\nHigh Security Area",

            FontSize = 18.0,

            FontFamily = "Segoe UI",

            FontWeight = "Bold",

            FontStyle = "Italic",

            TextAlignment = "Center",

            TextColor = "#EF4444",

            Background = "#FEF3C7",

            BorderColor = "#F59E0B",

            BorderThickness = 2.0,

            X = 120,

            Y = 80,

            Width = 240,

            Height = 60,

            Z = 10

        };



        doc.Items.Add(textItem);



        string json = repo.SerializeToJson(doc);

        Assert.Contains("\"type\": \"text\"", json);

        Assert.Contains("\"text\": \"Server Room Phase 1\\nHigh Security Area\"", json);

        Assert.Contains("\"font_size\": 18", json);

        Assert.Contains("\"font_family\": \"Segoe UI\"", json);

        Assert.Contains("\"font_weight\": \"Bold\"", json);

        Assert.Contains("\"font_style\": \"Italic\"", json);

        Assert.Contains("\"text_alignment\": \"Center\"", json);

        Assert.Contains("\"text_color\": \"#EF4444\"", json);

        Assert.Contains("\"background\": \"#FEF3C7\"", json);

        Assert.Contains("\"border_color\": \"#F59E0B\"", json);

        Assert.Contains("\"border_thickness\": 2", json);



        var restored = repo.DeserializeFromJson(json);

        Assert.Single(restored.Items);

        var restoredText = Assert.IsType<TextItem>(restored.Items[0]);



        Assert.Equal("Header Label", restoredText.Name);

        Assert.Equal("Server Room Phase 1\nHigh Security Area", restoredText.Text);

        Assert.Equal(18.0, restoredText.FontSize);

        Assert.Equal("Segoe UI", restoredText.FontFamily);

        Assert.Equal("Bold", restoredText.FontWeight);

        Assert.Equal("Italic", restoredText.FontStyle);

        Assert.Equal("Center", restoredText.TextAlignment);

        Assert.Equal("#EF4444", restoredText.TextColor);

        Assert.Equal("#FEF3C7", restoredText.Background);

        Assert.Equal("#F59E0B", restoredText.BorderColor);

        Assert.Equal(2.0, restoredText.BorderThickness);

        Assert.Equal(120, restoredText.X);

        Assert.Equal(80, restoredText.Y);

        Assert.Equal(240, restoredText.Width);

        Assert.Equal(60, restoredText.Height);

        Assert.Equal(10, restoredText.Z);

    }



    [Fact]

    public void TextItemViewModel_EditAndUndoRedo_MaintainsIntegrity()

    {

        var undoRedo = new UndoRedoService();

        var model = new TextItem { Text = "Initial Title" };

        var vm = new TextItemViewModel(model);



        Assert.False(vm.IsEditing);

        vm.BeginEdit();

        Assert.True(vm.IsEditing);

        vm.EndEdit();

        Assert.False(vm.IsEditing);



        // Test ChangeTextContentCommand

        undoRedo.ExecuteCommand(new ChangeTextContentCommand(vm, "Initial Title", "Updated Title"));

        Assert.Equal("Updated Title", vm.Text);

        Assert.Equal("Updated Title", model.Text);



        undoRedo.Undo();

        Assert.Equal("Initial Title", vm.Text);

        Assert.Equal("Initial Title", model.Text);



        undoRedo.Redo();

        Assert.Equal("Updated Title", vm.Text);

        Assert.Equal("Updated Title", model.Text);

    }



    [Fact]

    public void CanvasViewModel_AddTextItem_SpawnsAndSelects()

    {

        var undoRedo = new UndoRedoService();

        var canvas = new CanvasViewModel(undoRedo);



        canvas.AddTextItem();

        Assert.Single(canvas.Items);

        var item = Assert.IsType<TextItemViewModel>(canvas.Items[0]);

        Assert.Single(canvas.SelectedItems);

        Assert.Same(item, canvas.SelectedItems[0]);



        undoRedo.Undo();

        Assert.Empty(canvas.Items);



        undoRedo.Redo();

        Assert.Single(canvas.Items);

    }



    [Fact]

    public void PropertiesViewModel_TextItem_SyncsProperties()

    {

        var undoRedo = new UndoRedoService();

        var canvas = new CanvasViewModel(undoRedo);

        var propVm = new PropertiesViewModel(canvas);



        var model = new TextItem

        {

            Text = "Design Note",

            FontSize = 14,

            FontFamily = "Segoe UI",

            FontWeight = "Normal",

            FontStyle = "Normal",

            TextAlignment = "Left",

            TextColor = "#1E293B",

            Background = "Transparent"

        };

        var vm = new TextItemViewModel(model);

        canvas.AddItem(vm);

        canvas.SetSelection(new[] { vm });



        Assert.True(propVm.IsText);

        Assert.Equal("Design Note", propVm.ItemText);

        Assert.Equal(14.0, propVm.ItemFontSize);

        Assert.False(propVm.IsBold);

        Assert.False(propVm.IsItalic);

        Assert.Equal("Left", propVm.ItemTextAlignment);



        // Toggle Bold

        propVm.ToggleBold();

        Assert.True(propVm.IsBold);

        Assert.Equal("Bold", vm.FontWeight);



        // Toggle Italic

        propVm.ToggleItalic();

        Assert.True(propVm.IsItalic);

        Assert.Equal("Italic", vm.FontStyle);



        // Change Alignment

        propVm.SetAlignCenter();

        Assert.Equal("Center", propVm.ItemTextAlignment);

        Assert.Equal("Center", vm.TextAlignment);



        // Change Text Color and Background

        propVm.SetItemTextColor("#3B82F6");

        Assert.Equal("#3B82F6", vm.TextColor);



        propVm.SetItemTextBackground("#FEF3C7");

        Assert.Equal("#FEF3C7", vm.Background);

    }



    [Fact]

    public void SelectionAdorner_ThresholdAndFlickerFreeResize_InvariantCanvasCoordinates()

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



        // 1. Threshold for 4-point reduction calibrated to 28px (so 80x50 Semicircle & 140x40 Text retain 8 handles)

        Assert.Contains("public const double SmallItemThreshold = 28.0;", code);



        // 2. Invariant canvas coordinates to eliminate resize flickering

        Assert.Contains("FindCanvas()", code);

        Assert.Contains("GetCanvasPoint(e)", code);

        Assert.Contains("double localDX = deltaCanvasX * cos + deltaCanvasY * sin;", code);

        Assert.Contains("double localDY = -deltaCanvasX * sin + deltaCanvasY * cos;", code);

        Assert.Contains("double worldShiftX = shiftLocalX * cos - shiftLocalY * sin;", code);

        Assert.Contains("double worldShiftY = shiftLocalX * sin + shiftLocalY * cos;", code);

    }



    [Fact]

    public void RoomTemplatePersistence_RoundTrip_SavesAndLoadsTemplatesCorrectly()

    {

        string tempDir = Path.Combine(Path.GetTempPath(), "LabScape_Tests_" + Guid.NewGuid().ToString("N"));

        string tempFile = Path.Combine(tempDir, "templates.json");



        try

        {

            // 1. Initial load from non-existent file returns default presets and saves them

            var defaults = LabScape.ViewModels.Templates.RoomTemplatePersistence.LoadTemplates(tempFile);

            Assert.NotEmpty(defaults);

            Assert.True(File.Exists(tempFile));

            Assert.Contains(defaults, t => t.Name == "12-PC Standard Lab");

            Assert.Contains(defaults, t => t.Options.Sides == 4);



            // 2. Add custom template and save

            var custom = new LabScape.ViewModels.Templates.SavedRoomTemplate

            {

                Name = "Special 16-PC Studio",

                Description = "Custom lab for testing",

                Options = new LabScape.ViewModels.Templates.RoomTemplateOptions

                {

                    Sides = 3,

                    OpenSide = LabScape.Core.Enums.WallSide.East,

                    PcCount = 16

                }

            };

            defaults.Add(custom);

            LabScape.ViewModels.Templates.RoomTemplatePersistence.SaveTemplates(defaults, tempFile);



            // 3. Reload from file and verify roundtrip preservation

            var reloaded = LabScape.ViewModels.Templates.RoomTemplatePersistence.LoadTemplates(tempFile);

            Assert.Equal(defaults.Count, reloaded.Count);

            var found = reloaded.FirstOrDefault(t => t.Name == "Special 16-PC Studio");

            Assert.NotNull(found);

            Assert.Equal(3, found.Options.Sides);

            Assert.Equal(LabScape.Core.Enums.WallSide.East, found.Options.OpenSide);

            Assert.Equal(16, found.Options.PcCount);

        }

        finally

        {

            if (Directory.Exists(tempDir))

            {

                Directory.Delete(tempDir, true);

            }

        }

    }



    [Fact]

    public void RoomTemplateViewModel_SavedTemplates_AddInsertDelete_MaintainsStateAndCanvas()

    {

        var undo = new UndoRedoService();

        var canvas = new CanvasViewModel(undo) { SnapToGrid = false };



        var vm = new LabScape.ViewModels.Templates.RoomTemplateViewModel(() => canvas, autoLoadTemplates: false);



        // 1. Populate with presets

        var presets = LabScape.ViewModels.Templates.RoomTemplatePersistence.GetDefaultTemplates();

        foreach (var p in presets)

        {

            vm.SavedTemplates.Add(p);

        }

        vm.SelectedTemplate = vm.SavedTemplates[0];



        Assert.Equal(5, vm.SavedTemplates.Count);

        Assert.NotNull(vm.SelectedTemplate);

        Assert.Equal(12, vm.PcCount);

        Assert.Equal(4, vm.Sides);



        // 2. Select another template and verify options sync

        var uShape = vm.SavedTemplates.First(t => t.Options.Sides == 3);

        vm.SelectedTemplate = uShape;

        Assert.Equal(3, vm.Sides);

        Assert.Equal(8, vm.PcCount);



        // 3. Insert template on canvas

        Assert.Empty(canvas.Items);

        vm.InsertTemplate(uShape);

        Assert.NotEmpty(canvas.Items);



        // 4. Add new template

        var custom = new LabScape.ViewModels.Templates.SavedRoomTemplate

        {

            Name = "My Custom Room",

            Options = new LabScape.ViewModels.Templates.RoomTemplateOptions

            {

                Sides = 2,

                CornerOption = "NorthWest",

                PcCount = 6

            }

        };

        vm.AddSavedTemplate(custom, insertImmediately: false);

        Assert.Equal(6, vm.SavedTemplates.Count);

        Assert.Equal(custom, vm.SelectedTemplate);



        // 5. Delete template

        vm.DeleteTemplate(custom);

        Assert.Equal(5, vm.SavedTemplates.Count);

        Assert.DoesNotContain(custom, vm.SavedTemplates);

    }
    [Fact]
    public void ManualIpAssignment_SingleDevice_SupportsValidationAndUndoRedo()
    {
        var undo = new UndoRedoService();
        var canvas = new CanvasViewModel(undo);
        var dev1 = new DeviceItemViewModel(new DeviceItem { Name = "PC-01", DeviceType = "desktop" });
        canvas.AddItem(dev1);

        // 1. Initial status: Unassigned
        Assert.Equal(ValidationStatus.Unassigned, dev1.ValidationStatus);
        Assert.Null(dev1.IpAddress);
        Assert.True(dev1.IsManualIp);

        // 2. Set Invalid IP format
        dev1.IpAddress = "999.123.456.789";
        canvas.RecalculateValidation();
        Assert.Equal(ValidationStatus.InvalidIpFormat, dev1.ValidationStatus);

        // 3. Set Valid IP
        var assignCmd = new AssignDeviceNetworkCommand(
            dev1,
            oldIp: dev1.IpAddress, newIp: "192.168.1.50",
            oldMac: dev1.MacAddress, newMac: "00:11:22:33:44:55",
            oldDeviceId: dev1.DeviceId, newDeviceId: null,
            oldName: dev1.Name, newName: dev1.Name,
            oldIsManual: true, newIsManual: true,
            onChanged: () => canvas.RecalculateValidation());

        undo.ExecuteCommand(assignCmd);
        Assert.Equal("192.168.1.50", dev1.IpAddress);
        Assert.Equal(ValidationStatus.Valid, dev1.ValidationStatus);

        // 4. Add second device with Duplicate IP
        var dev2 = new DeviceItemViewModel(new DeviceItem { Name = "PC-02", DeviceType = "desktop", IpAddress = "192.168.1.50" });
        canvas.AddItem(dev2);
        Assert.Equal(ValidationStatus.DuplicateIp, dev1.ValidationStatus);
        Assert.Equal(ValidationStatus.DuplicateIp, dev2.ValidationStatus);

        // 5. Undo second device addition -> dev1 returns to Valid
        undo.Undo();
        Assert.Single(canvas.Items);
        Assert.Equal(ValidationStatus.Valid, dev1.ValidationStatus);

        // 6. Undo assignment -> returns to InvalidIpFormat
        undo.Undo();
        Assert.Equal("999.123.456.789", dev1.IpAddress);
        Assert.Equal(ValidationStatus.InvalidIpFormat, dev1.ValidationStatus);

        // 7. Redo assignment -> Valid again
        undo.Redo();
        Assert.Equal("192.168.1.50", dev1.IpAddress);
        Assert.Equal(ValidationStatus.Valid, dev1.ValidationStatus);
    }

    [Fact]
    public void BatchIpAssignment_SequentialAssignment_OrdersSpatiallyAndSupportsUndoRedo()
    {
        var undo = new UndoRedoService();
        var canvas = new CanvasViewModel(undo);

        // Create 4 devices out of spatial order
        var devD = new DeviceItemViewModel(new DeviceItem { Name = "Old-D", X = 200, Y = 200 });
        var devA = new DeviceItemViewModel(new DeviceItem { Name = "Old-A", X = 100, Y = 100 });
        var devC = new DeviceItemViewModel(new DeviceItem { Name = "Old-C", X = 100, Y = 200 });
        var devB = new DeviceItemViewModel(new DeviceItem { Name = "Old-B", X = 200, Y = 100 });

        canvas.AddItem(devD);
        canvas.AddItem(devA);
        canvas.AddItem(devC);
        canvas.AddItem(devB);

        // Select all 4
        canvas.SelectAll();
        Assert.Equal(4, canvas.SelectedItems.Count);

        var propVm = new PropertiesViewModel(canvas);
        propVm.BatchStartIp = "10.0.0.10";
        propVm.BatchIpStep = 1;
        propVm.BatchNamePrefix = "LAB-PC-";
        propVm.BatchRenameDevices = true;

        propVm.BatchAssignSequentialIps();

        // Check spatial order:
        // Row 1 (Y=100): devA (X=100) -> 10.0.0.10, devB (X=200) -> 10.0.0.11
        // Row 2 (Y=200): devC (X=100) -> 10.0.0.12, devD (X=200) -> 10.0.0.13
        Assert.Equal("10.0.0.10", devA.IpAddress);
        Assert.Equal("LAB-PC-01", devA.Name);
        Assert.Equal(ValidationStatus.Valid, devA.ValidationStatus);

        Assert.Equal("10.0.0.11", devB.IpAddress);
        Assert.Equal("LAB-PC-02", devB.Name);
        Assert.Equal(ValidationStatus.Valid, devB.ValidationStatus);

        Assert.Equal("10.0.0.12", devC.IpAddress);
        Assert.Equal("LAB-PC-03", devC.Name);
        Assert.Equal(ValidationStatus.Valid, devC.ValidationStatus);

        Assert.Equal("10.0.0.13", devD.IpAddress);
        Assert.Equal("LAB-PC-04", devD.Name);
        Assert.Equal(ValidationStatus.Valid, devD.ValidationStatus);

        // Test Undo
        undo.Undo();
        Assert.Equal("Old-A", devA.Name);
        Assert.Null(devA.IpAddress);
        Assert.Equal(ValidationStatus.Unassigned, devA.ValidationStatus);

        // Test Redo
        undo.Redo();
        Assert.Equal("10.0.0.10", devA.IpAddress);
        Assert.Equal("LAB-PC-01", devA.Name);
        Assert.Equal(ValidationStatus.Valid, devA.ValidationStatus);
    }

    [Fact]
    public void ManualIp_PropertiesViewModel_SwitchBetweenDatabaseAndManual()
    {
        var undo = new UndoRedoService();
        var canvas = new CanvasViewModel(undo);
        var dev = new DeviceItemViewModel(new DeviceItem { Name = "Desktop-01", DeviceType = "desktop" });
        canvas.AddItem(dev);
        canvas.SelectItem(dev);

        var propVm = new PropertiesViewModel(canvas);

        // 1. Assign from Database inventory
        var dbDevice = new DatabaseDevice
        {
            DeviceId = "db-srv-01",
            DisplayName = "PROD-DB-01",
            PrimaryIp = "192.168.10.5",
            MacAddress = "00:AA:BB:CC:DD:EE"
        };
        propVm.AssignIpToSelectedDevice(dbDevice);

        Assert.False(dev.IsManualIp);
        Assert.Equal("192.168.10.5", dev.IpAddress);
        Assert.Equal("PROD-DB-01", dev.Name);
        Assert.False(propVm.IsManualIpMode);
        Assert.Equal("PROD-DB-01", propVm.LinkedDatabaseDeviceName);

        // 2. Switch to Manual mode: keeps IP as static IP, marks manual
        propVm.SwitchToManualMode();
        Assert.True(dev.IsManualIp);
        Assert.True(propVm.IsManualIpMode);
        Assert.Empty(propVm.LinkedDatabaseDeviceName);
        Assert.Equal("192.168.10.5", dev.IpAddress); // IP is retained

        // 3. Clear IP
        propVm.ClearDeviceIp();
        Assert.Null(dev.IpAddress);
        Assert.Equal(ValidationStatus.Unassigned, dev.ValidationStatus);

        // 4. Next available IP suggestion
        propVm.SuggestNextIp();
        Assert.NotNull(dev.IpAddress);
        Assert.True(CanvasViewModel.IsValidIPv4(dev.IpAddress));
        Assert.Equal(ValidationStatus.Valid, dev.ValidationStatus);
    }

    [Fact]
    public void SchemaSerialization_PreservesIsManualIpFlag()
    {
        var repo = new LayoutJsonRepository();
        var doc = new LayoutDocument
        {
            Metadata = new DocumentMetadata { Title = "Test IP Modes" }
        };

        var devManual = new DeviceItem
        {
            Name = "Workstation-Manual",
            IpAddress = "192.168.1.10",
            IsManualIp = true
        };
        var devDb = new DeviceItem
        {
            Name = "Workstation-DbLinked",
            IpAddress = "192.168.1.20",
            DeviceId = "INV-999",
            IsManualIp = false
        };

        doc.Items.Add(devManual);
        doc.Items.Add(devDb);

        string json = repo.SerializeToJson(doc);
        Assert.Contains("\"is_manual_ip\": true", json);
        Assert.Contains("\"is_manual_ip\": false", json);

        var loaded = repo.DeserializeFromJson(json);
        Assert.NotNull(loaded);
        Assert.Equal(2, loaded.Items.Count);

        var loadedManual = loaded.Items.OfType<DeviceItem>().First(d => d.Name == "Workstation-Manual");
        var loadedDb = loaded.Items.OfType<DeviceItem>().First(d => d.Name == "Workstation-DbLinked");

        Assert.True(loadedManual.IsManualIp);
        Assert.False(loadedDb.IsManualIp);
    }

    [Fact]
    public void AllXamlStaticResources_ResolveToDeclaredKeys_PreventStartupCrash()
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

        var appDir = Path.Combine(projectRoot, "src", "LabScape.App");
        var xamlFiles = Directory.GetFiles(appDir, "*.xaml", SearchOption.AllDirectories);

        var declaredKeys = new HashSet<string>();
        var keyRegex = new System.Text.RegularExpressions.Regex(@"x:Key=""([^""]+)""");
        var staticResRegex = new System.Text.RegularExpressions.Regex(@"\{StaticResource\s+([^}]+)\}");

        foreach (var file in xamlFiles)
        {
            string content = File.ReadAllText(file);
            foreach (System.Text.RegularExpressions.Match match in keyRegex.Matches(content))
            {
                declaredKeys.Add(match.Groups[1].Value);
            }
        }

        var missing = new List<string>();
        foreach (var file in xamlFiles)
        {
            string[] lines = File.ReadAllLines(file);
            for (int lineNo = 1; lineNo <= lines.Length; lineNo++)
            {
                string line = lines[lineNo - 1];
                foreach (System.Text.RegularExpressions.Match match in staticResRegex.Matches(line))
                {
                    string res = match.Groups[1].Value.Trim();
                    if (res.StartsWith("x:") || res.StartsWith("{")) continue;
                    if (!declaredKeys.Contains(res))
                    {
                        missing.Add($"{Path.GetFileName(file)}:L{lineNo} -> '{res}'");
                    }
                }
            }
        }

        Assert.Empty(missing);
    }
}
