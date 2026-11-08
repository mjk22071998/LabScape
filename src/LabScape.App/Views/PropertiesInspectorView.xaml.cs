using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using LabScape.Core.Models;
using LabScape.ViewModels;

namespace LabScape.App.Views;

public partial class PropertiesInspectorView : UserControl
{
    public PropertiesInspectorView()
    {
        InitializeComponent();
    }

    private PropertiesViewModel? ViewModel => DataContext as PropertiesViewModel;

    private void DatabaseDevicesListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is ListBox listBox && listBox.SelectedItem is DatabaseDevice dbDev)
        {
            ViewModel?.AssignIpToSelectedDevice(dbDev);
            listBox.SelectedItem = null; // reset selection so it can be re-clicked
        }
    }

    private void FillTypeComboBox_DropDownClosed(object? sender, EventArgs e)
    {
        ViewModel?.ApplyFillChanges();
    }

    private void StrokeTypeComboBox_DropDownClosed(object? sender, EventArgs e)
    {
        ViewModel?.ApplyStrokeChanges();
    }

    private void FillTextBox_LostFocus(object sender, RoutedEventArgs e)
    {
        ViewModel?.ApplyFillChanges();
    }

    private void FillTextBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            ViewModel?.ApplyFillChanges();
            (sender as TextBox)?.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
        }
    }

    private void StrokeTextBox_LostFocus(object sender, RoutedEventArgs e)
    {
        ViewModel?.ApplyStrokeChanges();
    }

    private void StrokeTextBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            ViewModel?.ApplyStrokeChanges();
            (sender as TextBox)?.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
        }
    }

    private void Properties_ChangeCustomImage_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel?.SelectedItem == null) return;

        var ofd = new Microsoft.Win32.OpenFileDialog
        {
            Title = "Attach Custom Image",
            Filter = "Image Files (*.png;*.jpg;*.jpeg;*.bmp;*.gif;*.webp)|*.png;*.jpg;*.jpeg;*.bmp;*.gif;*.webp|All Files (*.*)|*.*",
            CheckFileExists = true
        };

        if (ofd.ShowDialog() == true)
        {
            try
            {
                byte[] bytes = System.IO.File.ReadAllBytes(ofd.FileName);
                ViewModel.SelectedItem.CustomImagePath = ofd.FileName;
                ViewModel.SelectedItem.CustomImageData = Convert.ToBase64String(bytes);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load image: {ex.Message}", "Image Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private void Properties_ClearCustomImage_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel?.SelectedItem == null) return;
        ViewModel.SelectedItem.CustomImagePath = null;
        ViewModel.SelectedItem.CustomImageData = null;
    }
    private void OpenFillColorPicker_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel == null) return;
        var dlg = new Dialogs.ColorPickerDialog(ViewModel.FillColor1)
        {
            Owner = Window.GetWindow(this)
        };
        if (dlg.ShowDialog() == true)
        {
            ViewModel.FillColor1 = dlg.SelectedColorHex;
            ViewModel.ApplyFillChanges();
        }
    }

    private void OpenStrokeColorPicker_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel == null) return;
        var dlg = new Dialogs.ColorPickerDialog(ViewModel.StrokeColor1)
        {
            Owner = Window.GetWindow(this)
        };
        if (dlg.ShowDialog() == true)
        {
            ViewModel.StrokeColor1 = dlg.SelectedColorHex;
            ViewModel.ApplyStrokeChanges();
        }
    }
}
