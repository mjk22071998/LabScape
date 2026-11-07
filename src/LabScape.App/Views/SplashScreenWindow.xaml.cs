using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Animation;

namespace LabScape.App.Views;

public partial class SplashScreenWindow : Window
{
    public SplashScreenWindow()
    {
        InitializeComponent();
        Services.GpuRenderingService.ApplyGpuAcceleration(this);
    }

    public void UpdateStatus(string message, double progressPercent)
    {
        Dispatcher.Invoke(() =>
        {
            TxtStatus.Text = message;
            TxtPercent.Text = $"{(int)progressPercent}%";

            double trackWidth = ProgressBarTrack.ActualWidth;
            if (trackWidth <= 0)
            {
                trackWidth = 472; // default fallback width before layout pass
            }

            double targetWidth = Math.Max(4, Math.Min(trackWidth, trackWidth * (progressPercent / 100.0)));
            ProgressBarIndicator.Width = targetWidth;
        });
    }

    public async Task FadeOutAndCloseAsync(int durationMs = 250)
    {
        var tcs = new TaskCompletionSource<bool>();

        var anim = new DoubleAnimation
        {
            From = 1.0,
            To = 0.0,
            Duration = TimeSpan.FromMilliseconds(durationMs),
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };

        anim.Completed += (s, e) =>
        {
            try
            {
                Close();
            }
            catch
            {
                // Ignore if already closing
            }
            tcs.TrySetResult(true);
        };

        BeginAnimation(OpacityProperty, anim);
        await tcs.Task;
    }
}
