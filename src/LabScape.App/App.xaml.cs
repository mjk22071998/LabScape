using System;
using System.IO;
using System.Windows;
using System.Windows.Threading;

namespace LabScape.App;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        DispatcherUnhandledException += App_DispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
        TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;
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
