using System.Windows;
using System.Windows.Media;
using LabScape.Infrastructure.Repositories;
using LabScape.Infrastructure.Services;

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
    }

    private void ChkUseMock_Checked(object sender, RoutedEventArgs e) => UpdateFormState();
    private void ChkUseMock_Unchecked(object sender, RoutedEventArgs e) => UpdateFormState();

    private void UpdateFormState()
    {
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
        DiagnosticBanner.Background = new SolidColorBrush(Color.FromRgb(254, 243, 199)); // Amber
        TxtDiagnosticResult.Foreground = new SolidColorBrush(Color.FromRgb(146, 64, 14));
        TxtDiagnosticResult.Text = "Testing connection...";

        bool useMock = ChkUseMock.IsChecked == true;

        if (useMock)
        {
            var mockRepo = new MockDeviceRepository();
            var latency = await mockRepo.MeasureLatencyAsync("");
            DiagnosticBanner.Background = new SolidColorBrush(Color.FromRgb(220, 252, 231)); // Green
            TxtDiagnosticResult.Foreground = new SolidColorBrush(Color.FromRgb(22, 101, 52));
            TxtDiagnosticResult.Text = $"✅ Simulated Lab Workstation Engine ready! Latency: {latency.TotalMilliseconds:F1} ms (25 devices available)";
            BtnTest.IsEnabled = true;
            return;
        }

        UpdateProfileFromUi();
        var pgRepo = new PostgreSqlDeviceRepository();
        var connStr = Profile.BuildConnectionString();
        var reachable = await pgRepo.TestConnectionAsync(connStr);

        if (reachable)
        {
            var latency = await pgRepo.MeasureLatencyAsync(connStr);
            var devices = await pgRepo.GetDevicesAsync(connStr);
            DiagnosticBanner.Background = new SolidColorBrush(Color.FromRgb(220, 252, 231));
            TxtDiagnosticResult.Foreground = new SolidColorBrush(Color.FromRgb(22, 101, 52));
            TxtDiagnosticResult.Text = $"✅ Connected to PostgreSQL! Round-trip latency: {latency.TotalMilliseconds:F1} ms. Found {devices.Count} inventory devices.";
        }
        else
        {
            DiagnosticBanner.Background = new SolidColorBrush(Color.FromRgb(254, 226, 226)); // Red
            TxtDiagnosticResult.Foreground = new SolidColorBrush(Color.FromRgb(153, 27, 27));
            TxtDiagnosticResult.Text = "❌ Connection failed: Unable to connect to host or authenticate credentials. Verify host, port, and credentials.";
        }

        BtnTest.IsEnabled = true;
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
