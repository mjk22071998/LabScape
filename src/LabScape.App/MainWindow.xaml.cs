using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Fluent;
using LabScape.Infrastructure.Repositories;
using LabScape.Infrastructure.Services;
using LabScape.ViewModels;
using Microsoft.Win32;

namespace LabScape.App;

public partial class MainWindow : RibbonWindow
{
    private readonly LayoutJsonRepository _jsonRepo;
    public MainViewModel ViewModel { get; }

    public MainWindow()
    {
        InitializeComponent();

        _jsonRepo = new LayoutJsonRepository();
        var dbService = new PostgresDatabaseService();
        var initialUndo = new UndoRedoService();

        ViewModel = new MainViewModel(
            _jsonRepo,
            dbService,
            initialUndo,
            undoRedoFactory: () => new UndoRedoService());

        DataContext = ViewModel;

        Loaded += (s, e) =>
        {
            UpdateThemeToggleState();
            Services.ThemeService.ThemeChanged += OnThemeChanged;

            // 1. Instant Cache Restoration (Zero-wait offline-first startup)
            var cachedDevices = DpapiCredentialVault.LoadInventoryCache();
            if (cachedDevices != null && cachedDevices.Count > 0)
            {
                ViewModel.Properties.SetInventoryDevices(cachedDevices);
                ViewModel.IsDatabaseConnected = true;
                ViewModel.DatabaseStatusText = $"Database: PostgreSQL ({cachedDevices.Count} devices - Cached)";
            }

            // 2. Background Reconnect & Live Sync
            try
            {
                var profile = DpapiCredentialVault.LoadProfile();
                if (profile.IsConfigured || (!profile.UseMock && !string.IsNullOrWhiteSpace(profile.Host) && !string.IsNullOrWhiteSpace(profile.Database)))
                {
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            Core.Interfaces.IDeviceRepository repo = profile.UseMock
                                ? new MockDeviceRepository()
                                : new PostgreSqlDeviceRepository();

                            var connStr = profile.BuildConnectionString();
                            var reachable = await repo.TestConnectionAsync(connStr);
                            if (reachable)
                            {
                                var devices = await repo.GetDevicesAsync(connStr);
                                DpapiCredentialVault.SaveInventoryCache(devices);
                                profile.LastConnected = DateTime.UtcNow;
                                profile.IsConfigured = true;
                                DpapiCredentialVault.SaveProfile(profile);

                                Dispatcher.Invoke(() =>
                                {
                                    ViewModel.Properties.SetInventoryDevices(devices);
                                    ViewModel.IsDatabaseConnected = true;
                                    ViewModel.DatabaseStatusText = profile.UseMock
                                        ? $"Database: Simulated ({devices.Count} devices)"
                                        : $"Database: PostgreSQL ({devices.Count} devices)";
                                });
                            }
                            else if (cachedDevices == null || cachedDevices.Count == 0)
                            {
                                Dispatcher.Invoke(() =>
                                {
                                    ViewModel.IsDatabaseConnected = false;
                                    ViewModel.DatabaseStatusText = "Database: Disconnected (Click to Connect)";
                                });
                            }
                        }
                        catch
                        {
                            if (cachedDevices == null || cachedDevices.Count == 0)
                            {
                                Dispatcher.Invoke(() =>
                                {
                                    ViewModel.IsDatabaseConnected = false;
                                    ViewModel.DatabaseStatusText = "Database: Disconnected (Click to Connect)";
                                });
                            }
                        }
                    });
                }
                else if (cachedDevices == null || cachedDevices.Count == 0)
                {
                    ViewModel.IsDatabaseConnected = false;
                    ViewModel.DatabaseStatusText = "Database: Disconnected (Click to Connect)";
                }
            }
            catch
            {
                if (cachedDevices == null || cachedDevices.Count == 0)
                {
                    ViewModel.IsDatabaseConnected = false;
                    ViewModel.DatabaseStatusText = "Database: Disconnected (Click to Connect)";
                }
            }
        };
        Unloaded += (s, e) =>
        {
            Services.ThemeService.ThemeChanged -= OnThemeChanged;
        };
    }

    // ========================================================
    // Comprehensive Global Standard Shortcut Keys Handling
    // ========================================================
    private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        // Don't intercept typing in text boxes or combo boxes
        if (e.OriginalSource is System.Windows.Controls.TextBox ||
            e.OriginalSource is System.Windows.Controls.ComboBox ||
            e.OriginalSource is Fluent.TextBox ||
            e.OriginalSource is Fluent.ComboBox)
        {
            return;
        }

        // Theme Shortcut: F8
        if (e.Key == Key.F8)
        {
            Services.ThemeService.ToggleTheme();
            e.Handled = true;
            return;
        }

        var isCtrl = (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control;
        var isShift = (Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift;
        var isAlt = (Keyboard.Modifiers & ModifierKeys.Alt) == ModifierKeys.Alt;

        var canvas = ViewModel.ActiveCanvas;

        // 1. Tab & File Shortcuts
        if (isCtrl && !isShift && !isAlt && e.Key == Key.N)
        {
            ViewModel.NewTab();
            e.Handled = true;
            return;
        }
        if (isCtrl && !isShift && !isAlt && e.Key == Key.O)
        {
            OpenJsonDocument();
            e.Handled = true;
            return;
        }
        if (isCtrl && !isShift && !isAlt && e.Key == Key.S)
        {
            SaveJsonDocument();
            e.Handled = true;
            return;
        }
        if (isCtrl && isShift && !isAlt && e.Key == Key.S)
        {
            SaveAsJsonDocument();
            e.Handled = true;
            return;
        }
        if (isCtrl && !isShift && !isAlt && e.Key == Key.W)
        {
            ViewModel.CloseActiveTab();
            e.Handled = true;
            return;
        }
        if (isCtrl && !isShift && !isAlt && e.Key == Key.Tab)
        {
            ViewModel.NextTab();
            e.Handled = true;
            return;
        }
        if (isCtrl && isShift && !isAlt && e.Key == Key.Tab)
        {
            ViewModel.PreviousTab();
            e.Handled = true;
            return;
        }

        // 2. Undo / Redo Shortcuts (Ctrl+Z, Ctrl+Y, Ctrl+Shift+Z)
        if (isCtrl && !isShift && !isAlt && e.Key == Key.Z)
        {
            if (ViewModel.CanUndo) ViewModel.Undo();
            e.Handled = true;
            return;
        }
        if ((isCtrl && !isShift && !isAlt && e.Key == Key.Y) ||
            (isCtrl && isShift && !isAlt && e.Key == Key.Z))
        {
            if (ViewModel.CanRedo) ViewModel.Redo();
            e.Handled = true;
            return;
        }

        // 3. Selection & Clipboard Shortcuts
        if (canvas != null)
        {
            if (isCtrl && !isShift && !isAlt && e.Key == Key.A)
            {
                canvas.SelectAll();
                e.Handled = true;
                return;
            }
            if (isCtrl && !isShift && !isAlt && e.Key == Key.X)
            {
                canvas.CutSelected();
                e.Handled = true;
                return;
            }
            if (isCtrl && !isShift && !isAlt && e.Key == Key.C)
            {
                canvas.CopySelected();
                e.Handled = true;
                return;
            }
            if (isCtrl && !isShift && !isAlt && e.Key == Key.V)
            {
                canvas.Paste();
                e.Handled = true;
                return;
            }
            if (isCtrl && !isShift && !isAlt && e.Key == Key.D)
            {
                canvas.DuplicateSelected();
                e.Handled = true;
                return;
            }

            // 4. Delete & Clear Selection
            if (!isCtrl && !isShift && !isAlt && (e.Key == Key.Delete || e.Key == Key.Back))
            {
                canvas.DeleteSelected();
                e.Handled = true;
                return;
            }
            if (!isCtrl && !isShift && !isAlt && e.Key == Key.Escape)
            {
                canvas.ClearSelection();
                e.Handled = true;
                return;
            }

            // 5. Grouping Shortcuts (Ctrl+G, Ctrl+Shift+G)
            if (isCtrl && !isShift && !isAlt && e.Key == Key.G)
            {
                canvas.GroupSelected();
                e.Handled = true;
                return;
            }
            if (isCtrl && isShift && !isAlt && e.Key == Key.G)
            {
                canvas.UngroupSelected();
                e.Handled = true;
                return;
            }

            // 6. Layering Shortcuts (Send Backward, Send to Back, Move Forward, Move to Front)
            // Move to Front: Ctrl+Shift+] or Shift+PageUp
            if ((isCtrl && isShift && e.Key == Key.OemCloseBrackets) || (isShift && e.Key == Key.PageUp))
            {
                canvas.MoveToFront();
                e.Handled = true;
                return;
            }
            // Move Forward: Ctrl+] or PageUp
            if ((isCtrl && !isShift && e.Key == Key.OemCloseBrackets) || (!isShift && e.Key == Key.PageUp))
            {
                canvas.MoveForward();
                e.Handled = true;
                return;
            }
            // Send to Back: Ctrl+Shift+[ or Shift+PageDown
            if ((isCtrl && isShift && e.Key == Key.OemOpenBrackets) || (isShift && e.Key == Key.PageDown))
            {
                canvas.SendToBack();
                e.Handled = true;
                return;
            }
            // Send Backward: Ctrl+[ or PageDown
            if ((isCtrl && !isShift && e.Key == Key.OemOpenBrackets) || (!isShift && e.Key == Key.PageDown))
            {
                canvas.SendBackward();
                e.Handled = true;
                return;
            }

            // 7. Zoom Shortcuts (Ctrl++, Ctrl+-, Ctrl+0)
            if (isCtrl && (e.Key == Key.OemPlus || e.Key == Key.Add))
            {
                canvas.ZoomIn();
                e.Handled = true;
                return;
            }
            if (isCtrl && (e.Key == Key.OemMinus || e.Key == Key.Subtract))
            {
                canvas.ZoomOut();
                e.Handled = true;
                return;
            }
            if (isCtrl && (e.Key == Key.D0 || e.Key == Key.NumPad0))
            {
                canvas.ResetZoom();
                e.Handled = true;
                return;
            }

            // 8. Arrow Keys Nudging (1px normal, GridSize with Shift)
            if (!isCtrl && !isAlt && (e.Key == Key.Up || e.Key == Key.Down || e.Key == Key.Left || e.Key == Key.Right))
            {
                if (canvas.SelectedItems.Count > 0)
                {
                    double step = isShift ? canvas.GridSize : 1.0;
                    double dx = 0, dy = 0;
                    if (e.Key == Key.Left) dx = -step;
                    else if (e.Key == Key.Right) dx = step;
                    else if (e.Key == Key.Up) dy = -step;
                    else if (e.Key == Key.Down) dy = step;

                    canvas.Nudge(dx, dy);
                    e.Handled = true;
                    return;
                }
            }
        }
    }

    // ========================================================
    // Browser-Style Tab Interactions
    // ========================================================

    private void TabItem_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement elem && elem.DataContext is LayoutTabViewModel tab)
        {
            ViewModel.SelectTab(tab);
        }
    }

    private void TabClose_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement elem && elem.DataContext is LayoutTabViewModel tab)
        {
            ViewModel.CloseTab(tab);
        }
    }

    // ========================================================
    // Unified JSON File Operations (Open, Save, Save As, Import)
    // ========================================================

    private void SaveJson_Click(object sender, RoutedEventArgs e)
    {
        SaveJsonDocument();
    }

    private void SaveAsJson_Click(object sender, RoutedEventArgs e)
    {
        SaveAsJsonDocument();
    }

    private void SaveJsonDocument()
    {
        var activeTab = ViewModel.ActiveTab;
        if (activeTab == null) return;

        if (string.IsNullOrWhiteSpace(activeTab.FilePath))
        {
            SaveAsJsonDocument();
            return;
        }

        try
        {
            var doc = activeTab.BuildDocumentModel();
            var json = _jsonRepo.SerializeToJson(doc);
            File.WriteAllText(activeTab.FilePath, json);
            activeTab.IsModified = false;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error saving floorplan: {ex.Message}", "Save Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void SaveAsJsonDocument()
    {
        var activeTab = ViewModel.ActiveTab;
        if (activeTab == null) return;

        var defaultName = string.IsNullOrWhiteSpace(activeTab.Title) ? "floorplan.json" : $"{activeTab.Title.Replace("*", "").Trim()}.json";
        if (!defaultName.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
        {
            defaultName += ".json";
        }

        var sfd = new SaveFileDialog
        {
            Filter = "Layout JSON (*.json)|*.json|All Files (*.*)|*.*",
            FileName = defaultName,
            Title = "Save Floorplan Layout as JSON"
        };

        if (sfd.ShowDialog() == true)
        {
            try
            {
                activeTab.FilePath = sfd.FileName;
                activeTab.Title = Path.GetFileNameWithoutExtension(sfd.FileName);
                var doc = activeTab.BuildDocumentModel();
                var json = _jsonRepo.SerializeToJson(doc);
                File.WriteAllText(sfd.FileName, json);
                activeTab.IsModified = false;
                MessageBox.Show($"Floorplan saved successfully to:\n{sfd.FileName}", "Save Complete", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving floorplan: {ex.Message}", "Save Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private void OpenJson_Click(object sender, RoutedEventArgs e)
    {
        OpenJsonDocument();
    }

    private async void OpenJsonDocument()
    {
        var ofd = new OpenFileDialog
        {
            Filter = "Layout JSON (*.json)|*.json|All Files (*.*)|*.*",
            Title = "Open / Import Floorplan Layout JSON"
        };

        if (ofd.ShowDialog() == true)
        {
            try
            {
                var doc = await _jsonRepo.LoadFromFileAsync(ofd.FileName);
                var fileName = Path.GetFileNameWithoutExtension(ofd.FileName);
                var loadedTab = ViewModel.LoadDocumentIntoTab(doc, ofd.FileName, fileName);
                MessageBox.Show($"Opened floorplan '{loadedTab.Title}' with {doc.Items.Count} item(s) in tab.", "File Opened", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error reading floorplan JSON: {ex.Message}", "Open Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private async void ImportIntoActive_Click(object sender, RoutedEventArgs e)
    {
        var ofd = new OpenFileDialog
        {
            Filter = "Layout JSON (*.json)|*.json|All Files (*.*)|*.*",
            Title = "Import and Merge Items from JSON into Active Canvas"
        };

        if (ofd.ShowDialog() == true)
        {
            try
            {
                var doc = await _jsonRepo.LoadFromFileAsync(ofd.FileName);
                ViewModel.ImportItemsIntoActiveCanvas(doc);
                MessageBox.Show($"Imported {doc.Items.Count} item(s) into the active canvas with undo support.", "Import Complete", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error importing JSON: {ex.Message}", "Import Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private void ExportJson_Click(object sender, RoutedEventArgs e)
    {
        // 1. Export Gatekeeper: Duplicate IP Conflict Blocker
        if (ViewModel.ValidationErrorCount > 0)
        {
            MessageBox.Show(
                $"Cannot export layout: {ViewModel.ValidationErrorCount} duplicate IP conflict(s) detected!\n\nPlease resolve all duplicate IP addresses before exporting.",
                "Export Blocked — IP Conflict",
                MessageBoxButton.OK,
                MessageBoxImage.Stop);
            return;
        }

        // 2. Export Gatekeeper: Unassigned Devices Warning
        if (ViewModel.ValidationWarningCount > 0)
        {
            var promptResult = MessageBox.Show(
                $"Warning: There are {ViewModel.ValidationWarningCount} unassigned device(s) without an IP address on the active canvas.\n\nDo you want to proceed with exporting anyway?",
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
                File.WriteAllText(sfd.FileName, json);
                MessageBox.Show($"Floorplan exported successfully to:\n{sfd.FileName}", "Export Complete", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving export file: {ex.Message}", "Export Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
                Path.Combine(baseDir, "web-viewer", "index.html"),
                Path.Combine(baseDir, "..", "..", "..", "..", "web-viewer", "index.html"),
                Path.Combine(Directory.GetCurrentDirectory(), "web-viewer", "index.html")
            };

            string? htmlPath = candidates.FirstOrDefault(File.Exists);
            if (htmlPath == null)
            {
                MessageBox.Show("Web viewer (index.html) could not be located.", "Viewer Not Found", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var fullPath = Path.GetFullPath(htmlPath);
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = fullPath,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error opening Web Viewer: {ex.Message}", "Launch Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
        try
        {
            Core.Interfaces.IDeviceRepository repo = profile.UseMock
                ? new MockDeviceRepository()
                : new PostgreSqlDeviceRepository();

            var connStr = profile.BuildConnectionString();
            var devices = await repo.GetDevicesAsync(connStr);

            // Persist profile & cached devices immediately
            profile.IsConfigured = true;
            profile.LastConnected = DateTime.UtcNow;
            DpapiCredentialVault.SaveProfile(profile);
            DpapiCredentialVault.SaveInventoryCache(devices);

            ViewModel.Properties.SetInventoryDevices(devices);
            ViewModel.IsDatabaseConnected = true;
            ViewModel.DatabaseStatusText = profile.UseMock
                ? $"Database: Simulated ({devices.Count} devices)"
                : $"Database: PostgreSQL ({devices.Count} devices)";

            MessageBox.Show(
                $"Successfully retrieved {devices.Count} device(s) from {(profile.UseMock ? "Simulated Lab Inventory" : "PostgreSQL database")}.\n\nDatabase connection and inventory cache have been persisted securely. You will not need to reconnect next time.",
                "Inventory Synced",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            ViewModel.IsDatabaseConnected = false;
            ViewModel.DatabaseStatusText = "Database: Connection Failed";
            MessageBox.Show(
                $"Failed to retrieve device information from database:\n\n{ex.Message}\n\nPlease check your Database Connection Settings in the Database tab.",
                "Database Connection Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private void OnThemeChanged(bool isDark)
    {
        UpdateThemeToggleState();
    }

    private void UpdateThemeToggleState()
    {
        bool isDark = Services.ThemeService.IsDarkTheme;
        if (BtnDarkTheme != null)
        {
            BtnDarkTheme.IsChecked = isDark;
            BtnDarkTheme.Header = isDark ? "Dark Theme" : "Light Theme";
            var iconResource = isDark ? "IconThemeDark" : "IconThemeLight";
            if (TryFindResource(iconResource) is System.Windows.Media.ImageSource img)
            {
                BtnDarkTheme.Icon = img;
                BtnDarkTheme.LargeIcon = img;
            }
        }
    }

    private void ThemeToggle_Click(object sender, RoutedEventArgs e)
    {
        Services.ThemeService.ToggleTheme();
    }
}
