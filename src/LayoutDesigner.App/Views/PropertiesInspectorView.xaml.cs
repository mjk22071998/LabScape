using System.Windows.Controls;
using LayoutDesigner.Core.Models;
using LayoutDesigner.ViewModels;

namespace LayoutDesigner.App.Views;

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
