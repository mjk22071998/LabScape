using System;
using System.IO;
using System.Windows;
using System.Windows.Threading;
using ControlzEx.Theming;

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
}
