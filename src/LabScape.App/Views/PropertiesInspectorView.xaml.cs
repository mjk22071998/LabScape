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
}
