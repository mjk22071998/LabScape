using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using LayoutDesigner.ViewModels.Stencils;

namespace LayoutDesigner.App.Views;

public partial class StencilsPaletteView : UserControl
{
    private Point _dragStart;

    public StencilsPaletteView()
    {
        InitializeComponent();
    }

    private void Stencil_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _dragStart = e.GetPosition(this);
    }

    private void Stencil_MouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed) return;

        Point current = e.GetPosition(this);
        Vector diff = _dragStart - current;

        if (Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance ||
            Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance)
        {
            if (sender is FrameworkElement elem && elem.DataContext is StencilItem stencil)
            {
                DragDrop.DoDragDrop(elem, stencil, DragDropEffects.Copy);
            }
        }
    }
}
