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

        Loaded += (s, e) => EnsureDataContext();
    }

    private void EnsureDataContext()
    {
        if (DataContext == null && Application.Current?.MainWindow is MainWindow mw && mw.ViewModel?.RoomTemplate != null)
        {
            DataContext = mw.ViewModel.RoomTemplate;
        }
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
            EnsureDataContext();
            if (DataContext is RoomTemplateViewModel vm)
            {
                var options = vm.BuildOptions();
                DragDrop.DoDragDrop(this, options, DragDropEffects.Copy);
            }
        }
    }

    private void BtnGenerateRoom_Click(object sender, RoutedEventArgs e)
    {
        EnsureDataContext();
        if (DataContext is RoomTemplateViewModel vm)
        {
            vm.GenerateRoom();
        }
        else if (Application.Current?.MainWindow is MainWindow mw && mw.ViewModel?.ActiveCanvas != null)
        {
            var fallbackOptions = new RoomTemplateOptions
            {
                Sides = 4,
                PcCount = 12,
                IncludeDesks = true,
                IncludeChairs = true,
                GroupElements = true
            };
            mw.ViewModel.ActiveCanvas.AddRoomTemplate(fallbackOptions);
        }
    }

    private void BtnResetDefaults_Click(object sender, RoutedEventArgs e)
    {
        EnsureDataContext();
        if (DataContext is RoomTemplateViewModel vm)
        {
            vm.ResetDefaults();
        }
    }
}
