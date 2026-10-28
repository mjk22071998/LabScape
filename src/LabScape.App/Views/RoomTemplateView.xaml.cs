using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using LabScape.App.Dialogs;
using LabScape.ViewModels.Templates;

namespace LabScape.App.Views;

public partial class RoomTemplateView : UserControl
{
    private Point _dragStart;
    private SavedRoomTemplate? _draggedTemplate;

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

    private void BtnCreateNew_Click(object sender, RoutedEventArgs e)
    {
        EnsureDataContext();
        var vm = DataContext as RoomTemplateViewModel;

        var initialOpts = vm?.BuildOptions();
        var dlg = new RoomTemplateDialog(initialOpts)
        {
            Owner = Window.GetWindow(this)
        };

        if (dlg.ShowDialog() == true && dlg.ResultTemplate != null)
        {
            if (vm != null)
            {
                vm.AddSavedTemplate(dlg.ResultTemplate, dlg.InsertImmediately);
            }
            else if (Application.Current?.MainWindow is MainWindow mw && mw.ViewModel?.ActiveCanvas != null)
            {
                if (dlg.InsertImmediately)
                {
                    mw.ViewModel.ActiveCanvas.AddRoomTemplate(dlg.ResultTemplate.Options);
                }
            }
        }
    }

    private void TemplateCard_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _dragStart = e.GetPosition(this);
        if (sender is FrameworkElement elem && elem.DataContext is SavedRoomTemplate template)
        {
            _draggedTemplate = template;
            EnsureDataContext();
            if (DataContext is RoomTemplateViewModel vm)
            {
                vm.SelectedTemplate = template;
            }
        }
    }

    private void TemplateCard_MouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed || _draggedTemplate == null) return;

        Point current = e.GetPosition(this);
        Vector diff = _dragStart - current;

        if (Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance ||
            Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance)
        {
            var options = new RoomTemplateOptions
            {
                Sides = _draggedTemplate.Options.Sides,
                OpenSide = _draggedTemplate.Options.OpenSide,
                CornerOption = _draggedTemplate.Options.CornerOption,
                SingleSide = _draggedTemplate.Options.SingleSide,
                PcCount = _draggedTemplate.Options.PcCount,
                IncludeDesks = _draggedTemplate.Options.IncludeDesks,
                IncludeChairs = _draggedTemplate.Options.IncludeChairs,
                GroupElements = _draggedTemplate.Options.GroupElements,
                DeskSpacing = _draggedTemplate.Options.DeskSpacing
            };

            DragDrop.DoDragDrop(this, options, DragDropEffects.Copy);
            _draggedTemplate = null;
        }
    }

    private void BtnInsertTemplate_Click(object sender, RoutedEventArgs e)
    {
        EnsureDataContext();
        if (sender is FrameworkElement elem && elem.Tag is SavedRoomTemplate template)
        {
            if (DataContext is RoomTemplateViewModel vm)
            {
                vm.InsertTemplate(template);
            }
            else if (Application.Current?.MainWindow is MainWindow mw && mw.ViewModel?.ActiveCanvas != null)
            {
                mw.ViewModel.ActiveCanvas.AddRoomTemplate(template.Options);
            }
        }
    }

    private void BtnDeleteTemplate_Click(object sender, RoutedEventArgs e)
    {
        EnsureDataContext();
        if (sender is FrameworkElement elem && elem.Tag is SavedRoomTemplate template)
        {
            if (DataContext is RoomTemplateViewModel vm)
            {
                vm.DeleteTemplate(template);
            }
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
            vm.SavedTemplates.Clear();
            var defaults = RoomTemplatePersistence.GetDefaultTemplates();
            foreach (var d in defaults)
            {
                vm.SavedTemplates.Add(d);
            }
            vm.SelectedTemplate = vm.SavedTemplates.FirstOrDefault();
            vm.SaveTemplates();
            vm.ResetDefaults();
        }
    }
}
