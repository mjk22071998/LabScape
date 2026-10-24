using System.Windows.Controls;
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
}
