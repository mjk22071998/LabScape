using System.Windows;
using System.Windows.Input;
using Fluent;
using LabScape.Infrastructure.Repositories;
using LabScape.Infrastructure.Services;
using LabScape.ViewModels;
using Microsoft.Win32;

namespace LabScape.App;

public partial class MainWindow : RibbonWindow
{
    public MainViewModel ViewModel { get; }

    public MainWindow()
    {
        InitializeComponent();

        var jsonRepo = new LayoutJsonRepository();
        var dbService = new PostgresDatabaseService();
        var undoService = new UndoRedoService();

        ViewModel = new MainViewModel(jsonRepo, dbService, undoService);
        DataContext = ViewModel;

        // Register Global Window KeyBindings for Grouping (Ctrl+G, Ctrl+Shift+G)
        CommandBindings.Add(new CommandBinding(
            new RoutedCommand("GroupCmd", typeof(MainWindow), new InputGestureCollection { new KeyGesture(Key.G, ModifierKeys.Control) }),
            (s, e) => ViewModel.Canvas.GroupSelected()));

        CommandBindings.Add(new CommandBinding(
            new RoutedCommand("UngroupCmd", typeof(MainWindow), new InputGestureCollection { new KeyGesture(Key.G, ModifierKeys.Control | ModifierKeys.Shift) }),
            (s, e) => ViewModel.Canvas.UngroupSelected()));
    }

    private void ExportJson_Click(object sender, RoutedEventArgs e)
    {
        // 1. Export Gatekeeper: Duplicate IP Conflict Blocker
        if (ViewModel.ValidationErrorCount > 0)
        {
            System.Windows.MessageBox.Show(
                $"Cannot export layout: {ViewModel.ValidationErrorCount} duplicate IP conflict(s) detected!\n\nPlease resolve all duplicate IP addresses before exporting.",
                "Export Blocked — IP Conflict",
                MessageBoxButton.OK,
                MessageBoxImage.Stop);
            return;
        }

        // 2. Export Gatekeeper: Unassigned Devices Warning
        if (ViewModel.ValidationWarningCount > 0)
        {
            var promptResult = System.Windows.MessageBox.Show(
                $"Warning: There are {ViewModel.ValidationWarningCount} unassigned device(s) without an IP address on the canvas.\n\nDo you want to proceed with exporting anyway?",
                "Unassigned Devices Warning",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (promptResult != MessageBoxResult.Yes)
                return;
        }

        var sfd = new SaveFileDialog
        {
            Filter = "Layout JSON (*.json)|*.json|All Files (*.*)|*.*",
            FileName = "floorplan_export.json",
            Title = "Export Layout Document to JSON"
        };

        if (sfd.ShowDialog() == true)
        {
            try
            {
                var json = ViewModel.ExportJsonString();
                System.IO.File.WriteAllText(sfd.FileName, json);
                System.Windows.MessageBox.Show($"Floorplan exported successfully to:\n{sfd.FileName}", "Export Complete", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error saving file: {ex.Message}", "Save Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private void PreviewWebViewer_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            var candidates = new[]
            {
                System.IO.Path.Combine(baseDir, "web-viewer", "index.html"),
                System.IO.Path.Combine(baseDir, "..", "..", "..", "..", "web-viewer", "index.html"),
                System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), "web-viewer", "index.html")
            };

            string? htmlPath = candidates.FirstOrDefault(System.IO.File.Exists);
            if (htmlPath == null)
            {
                System.Windows.MessageBox.Show("Web viewer (index.html) could not be located.", "Viewer Not Found", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var fullPath = System.IO.Path.GetFullPath(htmlPath);
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = fullPath,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Error opening Web Viewer: {ex.Message}", "Launch Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void OpenJson_Click(object sender, RoutedEventArgs e)
    {
        var ofd = new OpenFileDialog
        {
            Filter = "Layout JSON (*.json)|*.json|All Files (*.*)|*.*",
            Title = "Open Layout Document JSON"
        };

        if (ofd.ShowDialog() == true)
        {
            try
            {
                var repo = new LayoutJsonRepository();
                var doc = await repo.LoadFromFileAsync(ofd.FileName);
                ViewModel.LoadDocumentModel(doc);
                System.Windows.MessageBox.Show($"Loaded floorplan with {doc.Items.Count} top-level items.", "Load Complete", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error reading file: {ex.Message}", "Load Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private void Exit_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private async void DatabaseConnect_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new Dialogs.DatabaseSettingsDialog { Owner = this };
        if (dlg.ShowDialog() == true)
        {
            await SyncInventoryWithProfileAsync(dlg.Profile);
        }
    }

    private async void SyncInventory_Click(object sender, RoutedEventArgs e)
    {
        var profile = DpapiCredentialVault.LoadProfile();
        await SyncInventoryWithProfileAsync(profile);
    }

    private async Task SyncInventoryWithProfileAsync(DatabaseConnectionProfile profile)
    {
        Core.Interfaces.IDeviceRepository repo = profile.UseMock
            ? new MockDeviceRepository()
            : new PostgreSqlDeviceRepository();

        var connStr = profile.BuildConnectionString();
        var devices = await repo.GetDevicesAsync(connStr);
        ViewModel.Properties.SetInventoryDevices(devices);
        ViewModel.IsDatabaseConnected = true;
        ViewModel.DatabaseStatusText = profile.UseMock
            ? $"Database: Simulated ({devices.Count} devices)"
            : $"Database: PostgreSQL ({devices.Count} devices)";

        System.Windows.MessageBox.Show(
            $"Successfully synced {devices.Count} devices from {(profile.UseMock ? "Simulated Lab Inventory" : "PostgreSQL public.devices")}.\nSearchable IP dropdown updated in Properties panel.",
            "Inventory Synced",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }
}
