using System;
using System.IO;
using System.Windows;
using System.Windows.Threading;
using ControlzEx.Theming;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using LabScape.ViewModels;
using LabScape.ViewModels.Items;
using LabScape.ViewModels.Templates;
using LabScape.Core.Models;
using LabScape.Core.Enums;

namespace LabScape.App;

public partial class App : Application
{
    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        try
        {
            // Mandate Direct3D hardware acceleration across the process
            Services.GpuRenderingService.Initialize();
            Services.ThemeService.Initialize();
        }
        catch
        {
            // Fallback if theme manager fails
        }

        DispatcherUnhandledException += App_DispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
        TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;

        // Check for CLI screenshot export flag
        string? screenshotPath = null;
        bool isLight = false;
        bool isSampleLab = false;
        for (int i = 0; i < e.Args.Length; i++)
        {
            if (e.Args[i] == "--screenshot" && i + 1 < e.Args.Length)
            {
                screenshotPath = e.Args[i + 1];
            }
            if (e.Args[i] == "--theme" && i + 1 < e.Args.Length && e.Args[i + 1].Equals("light", StringComparison.OrdinalIgnoreCase))
            {
                isLight = true;
            }
            if (e.Args[i] == "--sample-lab")
            {
                isSampleLab = true;
            }
        }

        if (screenshotPath != null)
        {
            Services.ThemeService.ApplyTheme(!isLight, savePreference: false);

            var shotWindow = new MainWindow();
            MainWindow = shotWindow;
            shotWindow.Width = 1440;
            shotWindow.Height = 900;
            shotWindow.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            shotWindow.Show();

            if (isSampleLab)
            {
                PopulateSampleLab(shotWindow);
            }

            if (e.Args.Contains("--tab-templates"))
            {
                shotWindow.RoomTemplateAnchorable.IsActive = true;
                shotWindow.RoomTemplateAnchorable.IsSelected = true;
            }

            if (e.Args.Contains("--size-assist"))
            {
                PopulateSizeAssistDemo(shotWindow);
            }

            await Task.Delay(1200);
            shotWindow.UpdateLayout();

            int w = (int)shotWindow.ActualWidth;
            int h = (int)shotWindow.ActualHeight;
            if (w <= 0) w = 1440;
            if (h <= 0) h = 900;

            var rtb = new RenderTargetBitmap(w, h, 96, 96, System.Windows.Media.PixelFormats.Pbgra32);
            rtb.Render(shotWindow);

            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(rtb));

            var dir = Path.GetDirectoryName(screenshotPath);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

            using (var fs = File.Create(screenshotPath))
            {
                encoder.Save(fs);
            }

            Shutdown();
            return;
        }

        // 1. Show high-performance branded Splash Screen immediately
        var splash = new Views.SplashScreenWindow();
        Services.GpuRenderingService.ApplyGpuAcceleration(splash);
        splash.Show();

        // 2. Progressive startup milestones with real-time feedback
        splash.UpdateStatus("Accelerating GPU Direct3D Engine...", 20);
        await Task.Delay(80);

        splash.UpdateStatus("Loading Stencil & Component Libraries...", 45);
        await Task.Delay(80);

        splash.UpdateStatus("Configuring Workspace & Docking Panels...", 70);
        await Task.Delay(80);

        // 3. Instantiate MainWindow (loads Ribbon, AvalonDock, ViewModels)
        splash.UpdateStatus("Preparing Canvas Workspace & Themes...", 90);
        var mainWindow = new MainWindow();
        Services.GpuRenderingService.ApplyGpuAcceleration(mainWindow);
        MainWindow = mainWindow;

        splash.UpdateStatus("Ready", 100);
        await Task.Delay(100);

        // 4. Reveal MainWindow and smoothly dissolve Splash Screen
        mainWindow.Show();
        await splash.FadeOutAndCloseAsync(250);
    }

    private void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        LogAndShowException("UI Thread Exception", e.Exception);
        e.Handled = true;
    }

    private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception ex)
        {
            LogAndShowException("Fatal Domain Exception", ex);
        }
    }

    private void TaskScheduler_UnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        LogAndShowException("Background Task Exception", e.Exception);
        e.SetObserved();
    }

    private static void LogAndShowException(string title, Exception ex)
    {
        try
        {
            string logDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "LabScape", "Logs");
            if (!Directory.Exists(logDir))
            {
                Directory.CreateDirectory(logDir);
            }
            string logFile = Path.Combine(logDir, "crash.log");
            File.AppendAllText(logFile, $"[{DateTime.UtcNow:O}] {title}:\n{ex}\n\n");
        }
        catch
        {
            // Ignore logging failures
        }

        MessageBox.Show(
            $"An unexpected error occurred in LabScape:\n\n{ex.Message}\n\nDetails:\n{ex.StackTrace}",
            $"LabScape Error — {title}",
            MessageBoxButton.OK,
            MessageBoxImage.Error);
    }

    private static void PopulateSampleLab(MainWindow window)
    {
        if (window.DataContext is not MainViewModel mainVm) return;
        var canvas = mainVm.Canvas;
        if (canvas == null) return;

        // Generate a 4-sided room with workstations
        canvas.AddRoomTemplate(new RoomTemplateOptions
        {
            Sides = 4,
            PcCount = 8,
            OriginX = 120,
            OriginY = 80
        });

        // Add Server Rack
        var rack = new DeviceItemViewModel(new DeviceItem
        {
            DeviceType = "server_rack_48u",
            Name = "Server-Rack-01",
            X = 880,
            Y = 120,
            Width = 90,
            Height = 140
        });
        canvas.Items.Add(rack);

        // Add Network Switch
        var sw = new DeviceItemViewModel(new DeviceItem
        {
            DeviceType = "network_switch",
            Name = "Core-Switch-48P",
            X = 880,
            Y = 280,
            Width = 90,
            Height = 50
        });
        canvas.Items.Add(sw);

        // Add 8-Person Conference Table
        var table = new FurnitureItemViewModel(new FurnitureItem
        {
            FurnitureType = "conference_table_8p",
            Name = "Conference Table",
            X = 360,
            Y = 200,
            Width = 220,
            Height = 110
        });
        canvas.Items.Add(table);

        // Select the Server Rack
        canvas.SelectItem(rack, addToExisting: false);
    }

    private static void PopulateSizeAssistDemo(MainWindow window)
    {
        if (window.DataContext is not MainViewModel mainVm) return;
        var canvas = mainVm.Canvas;
        if (canvas == null) return;

        // Two identical 120x80 shapes demonstrating Equal Size assist
        var desk1 = new DeviceItemViewModel(new DeviceItem
        {
            DeviceType = "desktop_pc",
            Name = "Workstation-A",
            X = 260,
            Y = 220,
            Width = 120,
            Height = 80
        });
        var desk2 = new DeviceItemViewModel(new DeviceItem
        {
            DeviceType = "desktop_pc",
            Name = "Workstation-B",
            X = 540,
            Y = 220,
            Width = 120,
            Height = 80
        });
        canvas.Items.Add(desk1);
        canvas.Items.Add(desk2);
        canvas.SelectItem(desk2, addToExisting: false);

        // Find CanvasView and trigger size guides
        var canvasView = FindVisualChild<Views.CanvasView>(window);
        if (canvasView != null)
        {
            canvasView.ShowEqualWidthGuide(new Rect(540, 220, 120, 80), new Rect(260, 220, 120, 80), 120);
            canvasView.ShowEqualHeightGuide(new Rect(540, 220, 120, 80), new Rect(260, 220, 120, 80), 80);
            canvasView.ShowHorizontalGuide(220, 260, 660);
        }
    }

    private static T? FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
    {
        for (int i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = System.Windows.Media.VisualTreeHelper.GetChild(parent, i);
            if (child is T typedChild) return typedChild;
            var descendant = FindVisualChild<T>(child);
            if (descendant != null) return descendant;
        }
        return null;
    }
}
