using System.Diagnostics;
using System.Windows;
using System.Windows.Media;
using LabScape.Infrastructure.Repositories;
using LabScape.Infrastructure.Services;
using Npgsql;

namespace LabScape.App.Dialogs;

public partial class DatabaseSettingsDialog : Window
{
    public DatabaseConnectionProfile Profile { get; private set; }

    public DatabaseSettingsDialog()
    {
        InitializeComponent();
        Profile = DpapiCredentialVault.LoadProfile();

        TxtHost.Text = Profile.Host;
        TxtPort.Text = Profile.Port.ToString();
        TxtDatabase.Text = Profile.Database;
        TxtUsername.Text = Profile.Username;
        TxtPassword.Password = Profile.Password;
        ChkUseMock.IsChecked = Profile.UseMock;

        UpdateFormState();

        // Wire event handlers safely after component initialization
        ChkUseMock.Checked += (s, e) => UpdateFormState();
        ChkUseMock.Unchecked += (s, e) => UpdateFormState();
    }

    private void UpdateFormState()
    {
        if (ChkUseMock == null || TxtHost == null || TxtPort == null || TxtDatabase == null || TxtUsername == null || TxtPassword == null)
            return;

        bool useMock = ChkUseMock.IsChecked == true;
        TxtHost.IsEnabled = !useMock;
        TxtPort.IsEnabled = !useMock;
        TxtDatabase.IsEnabled = !useMock;
        TxtUsername.IsEnabled = !useMock;
        TxtPassword.IsEnabled = !useMock;
    }

    private async void BtnTest_Click(object sender, RoutedEventArgs e)
    {
        BtnTest.IsEnabled = false;
        DiagnosticBanner.Visibility = Visibility.Visible;
        DiagnosticBanner.Background = new SolidColorBrush(Color.FromArgb(40, 245, 158, 11)); // Amber tint
        DiagnosticBanner.BorderBrush = new SolidColorBrush(Color.FromRgb(245, 158, 11));
        TxtDiagnosticResult.Foreground = new SolidColorBrush(Color.FromRgb(180, 83, 9));
        TxtDiagnosticResult.Text = "Testing connection and querying inventory schema...";

        bool useMock = ChkUseMock.IsChecked == true;

        if (useMock)
        {
            var mockRepo = new MockDeviceRepository();
            var latency = await mockRepo.MeasureLatencyAsync("");
            DiagnosticBanner.Background = new SolidColorBrush(Color.FromArgb(40, 16, 185, 129)); // Green tint
            DiagnosticBanner.BorderBrush = new SolidColorBrush(Color.FromRgb(16, 185, 129));
            TxtDiagnosticResult.Foreground = new SolidColorBrush(Color.FromRgb(4, 120, 87));
            TxtDiagnosticResult.Text = $"✅ Simulated Lab Workstation Engine ready! Latency: {latency.TotalMilliseconds:F1} ms (25 simulated devices available).";
            BtnTest.IsEnabled = true;
            return;
        }

        UpdateProfileFromUi();
        var connStr = Profile.BuildConnectionString();

        try
        {
            var sw = Stopwatch.StartNew();
            string pgVersion = "PostgreSQL";
            await using (var conn = new NpgsqlConnection(connStr))
            {
                await conn.OpenAsync();
                sw.Stop();

                try
                {
                    await using var verCmd = new NpgsqlCommand("SELECT version();", conn);
                    var verObj = await verCmd.ExecuteScalarAsync();
                    if (verObj != null)
                    {
                        pgVersion = verObj.ToString()?.Split('\n')[0] ?? "PostgreSQL";
                    }
                }
                catch { }
            }

            // Test device retrieval
            var pgRepo = new PostgreSqlDeviceRepository();
            var devices = await pgRepo.GetDevicesAsync(connStr);

            DiagnosticBanner.Background = new SolidColorBrush(Color.FromArgb(40, 16, 185, 129)); // Green tint
            DiagnosticBanner.BorderBrush = new SolidColorBrush(Color.FromRgb(16, 185, 129));
            TxtDiagnosticResult.Foreground = new SolidColorBrush(Color.FromRgb(4, 120, 87));
            TxtDiagnosticResult.Text = $"✅ Connection successful! Latency: {sw.ElapsedMilliseconds} ms.\nDatabase: {Profile.Database} on {Profile.Host}:{Profile.Port}\nServer: {pgVersion}\nFound {devices.Count} device(s) ready in inventory.";
        }
        catch (Exception ex)
        {
            DiagnosticBanner.Background = new SolidColorBrush(Color.FromArgb(40, 239, 68, 68)); // Red tint
            DiagnosticBanner.BorderBrush = new SolidColorBrush(Color.FromRgb(239, 68, 68));
            TxtDiagnosticResult.Foreground = new SolidColorBrush(Color.FromRgb(185, 28, 28));
            TxtDiagnosticResult.Text = $"❌ Connection Failed:\n{ex.Message}";
        }
        finally
        {
            BtnTest.IsEnabled = true;
        }
    }

    private void UpdateProfileFromUi()
    {
        Profile.UseMock = ChkUseMock.IsChecked == true;
        Profile.Host = TxtHost.Text.Trim();
        if (int.TryParse(TxtPort.Text.Trim(), out int port)) Profile.Port = port;
        Profile.Database = TxtDatabase.Text.Trim();
        Profile.Username = TxtUsername.Text.Trim();
        Profile.Password = TxtPassword.Password;
    }

    private void BtnSave_Click(object sender, RoutedEventArgs e)
    {
        UpdateProfileFromUi();
        DpapiCredentialVault.SaveProfile(Profile);
        DialogResult = true;
        Close();
    }
}
