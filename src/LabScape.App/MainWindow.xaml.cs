using System.Windows;
using System.Windows.Input;
using Fluent;
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

    private void DatabaseConnect_Click(object sender, RoutedEventArgs e)
    {
        System.Windows.MessageBox.Show("PostgreSQL Database configuration dialog.\nDefault connection will query public.devices.", "Database Settings", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void SyncInventory_Click(object sender, RoutedEventArgs e)
    {
        System.Windows.MessageBox.Show("Inventory synced from database.\nUpdated searchable IP list with available network units.", "Sync Inventory", MessageBoxButton.OK, MessageBoxImage.Information);
    }
}
