using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using LabScape.ViewModels.Templates;

namespace LabScape.App.Views;

public partial class RoomTemplateView : UserControl
{
    private Point _dragStart;

    public RoomTemplateView()
    {
        InitializeComponent();
    }

    private void DragCard_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _dragStart = e.GetPosition(this);
    }

    private void DragCard_MouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed) return;

        Point current = e.GetPosition(this);
        Vector diff = _dragStart - current;

        if (Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance ||
            Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance)
        {
            if (DataContext is RoomTemplateViewModel vm)
            {
                var options = vm.BuildOptions();
                DragDrop.DoDragDrop(this, options, DragDropEffects.Copy);
            }
        }
    }
}
