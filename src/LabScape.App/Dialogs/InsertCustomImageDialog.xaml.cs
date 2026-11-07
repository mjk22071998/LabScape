using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace LabScape.App.Dialogs;

public partial class InsertCustomImageDialog : Window
{
    public string FilePath { get; }
    public string Base64Data { get; private set; } = string.Empty;
    public bool IsNetworkRelated => RbNetwork.IsChecked == true;
    public string ItemName { get; private set; } = string.Empty;
    public string? IpAddress { get; private set; }
    public double CalculatedWidth { get; private set; } = 80;
    public double CalculatedHeight { get; private set; } = 80;

    public InsertCustomImageDialog(string filePath)
    {
        InitializeComponent();
        FilePath = filePath;

        string fileName = Path.GetFileName(filePath);
        string nameWithoutExt = Path.GetFileNameWithoutExtension(filePath);
        string cleanName = nameWithoutExt.Replace('_', ' ').Replace('-', ' ');

        TxtFileName.Text = fileName;
        TxtFilePath.Text = filePath;
        TxtDeviceName.Text = cleanName;
        TxtFurnitureName.Text = cleanName;

        try
        {
            var bi = new BitmapImage();
            bi.BeginInit();
            bi.CacheOption = BitmapCacheOption.OnLoad;
            bi.UriSource = new Uri(filePath, UriKind.Absolute);
            bi.EndInit();
            if (bi.CanFreeze && !bi.IsFrozen) bi.Freeze();

            ImgThumbnail.Source = bi;

            int pixelW = bi.PixelWidth;
            int pixelH = bi.PixelHeight;

            var fi = new FileInfo(filePath);
            string sizeStr = fi.Length < 1024 * 1024
                ? $"{fi.Length / 1024.0:F1} KB"
                : $"{fi.Length / (1024.0 * 1024.0):F2} MB";

            TxtDimensions.Text = $"{pixelW} × {pixelH} px ({sizeStr})";

            const double maxDimension = 100.0;
            if (pixelW > 0 && pixelH > 0)
            {
                if (pixelW >= pixelH)
                {
                    CalculatedWidth = maxDimension;
                    CalculatedHeight = Math.Max(24.0, Math.Round(maxDimension * ((double)pixelH / pixelW)));
                }
                else
                {
                    CalculatedHeight = maxDimension;
                    CalculatedWidth = Math.Max(24.0, Math.Round(maxDimension * ((double)pixelW / pixelH)));
                }
            }

            byte[] bytes = File.ReadAllBytes(filePath);
            Base64Data = Convert.ToBase64String(bytes);
        }
        catch
        {
            TxtDimensions.Text = "Standard Image";
            CalculatedWidth = 80;
            CalculatedHeight = 80;
        }

        TxtDeviceName.Focus();
        TxtDeviceName.SelectAll();
    }

    private void Role_Changed(object sender, RoutedEventArgs e)
    {
        if (BorderNetwork == null || BorderNonNetwork == null) return;

        var accentBrush = (Brush)FindResource("AccentPrimaryBrush");
        var cardBorderBrush = (Brush)FindResource("CardBorderBrush");

        if (RbNetwork.IsChecked == true)
        {
            BorderNetwork.BorderBrush = accentBrush;
            BorderNetwork.BorderThickness = new Thickness(1.5);
            PanelNetworkFields.IsEnabled = true;
            PanelNetworkFields.Opacity = 1.0;

            BorderNonNetwork.BorderBrush = cardBorderBrush;
            BorderNonNetwork.BorderThickness = new Thickness(1.0);
            PanelNonNetworkFields.IsEnabled = false;
            PanelNonNetworkFields.Opacity = 0.5;
        }
        else
        {
            BorderNetwork.BorderBrush = cardBorderBrush;
            BorderNetwork.BorderThickness = new Thickness(1.0);
            PanelNetworkFields.IsEnabled = false;
            PanelNetworkFields.Opacity = 0.5;

            var tealBrush = new SolidColorBrush(Color.FromRgb(0x0D, 0x94, 0x88));
            BorderNonNetwork.BorderBrush = tealBrush;
            BorderNonNetwork.BorderThickness = new Thickness(1.5);
            PanelNonNetworkFields.IsEnabled = true;
            PanelNonNetworkFields.Opacity = 1.0;
        }
    }

    private void CardNetwork_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.OriginalSource is not TextBox)
        {
            RbNetwork.IsChecked = true;
        }
    }

    private void CardNonNetwork_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.OriginalSource is not TextBox)
        {
            RbNonNetwork.IsChecked = true;
        }
    }

    private void Insert_Click(object sender, RoutedEventArgs e)
    {
        if (IsNetworkRelated)
        {
            ItemName = string.IsNullOrWhiteSpace(TxtDeviceName.Text) ? "Custom Device" : TxtDeviceName.Text.Trim();
            IpAddress = string.IsNullOrWhiteSpace(TxtDeviceIp.Text) ? null : TxtDeviceIp.Text.Trim();
        }
        else
        {
            ItemName = string.IsNullOrWhiteSpace(TxtFurnitureName.Text) ? "Custom Asset" : TxtFurnitureName.Text.Trim();
            IpAddress = null;
        }

        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
