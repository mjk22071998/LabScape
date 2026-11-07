using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

namespace LabScape.App.Adorners;

/// <summary>
/// Renders a Canva/Photoshop-style drop insertion line with glow and circular endpoints
/// between layer items during drag-and-drop reordering.
/// </summary>
public class LayerDropIndicatorAdorner : Adorner
{
    private static readonly Pen IndicatorPen = new(new SolidColorBrush(Color.FromRgb(59, 130, 246)), 2.5)
    {
        StartLineCap = PenLineCap.Round,
        EndLineCap = PenLineCap.Round
    };
    private static readonly Brush IndicatorGlowBrush = new SolidColorBrush(Color.FromArgb(70, 59, 130, 246));
    private static readonly Brush CircleBrush = new SolidColorBrush(Color.FromRgb(59, 130, 246));

    private bool _isUpperHalf;

    public bool IsUpperHalf
    {
        get => _isUpperHalf;
        set
        {
            if (_isUpperHalf != value)
            {
                _isUpperHalf = value;
                InvalidateVisual();
            }
        }
    }

    public LayerDropIndicatorAdorner(UIElement adornedElement, bool isUpperHalf) : base(adornedElement)
    {
        _isUpperHalf = isUpperHalf;
        IsHitTestVisible = false;
    }

    protected override void OnRender(DrawingContext dc)
    {
        base.OnRender(dc);
        double w = AdornedElement.RenderSize.Width;
        double h = AdornedElement.RenderSize.Height;
        double y = _isUpperHalf ? 0 : h;

        // 1. Subtle glow underlay
        var glowPen = new Pen(IndicatorGlowBrush, 5.0)
        {
            StartLineCap = PenLineCap.Round,
            EndLineCap = PenLineCap.Round
        };
        dc.DrawLine(glowPen, new Point(4, y), new Point(w - 4, y));

        // 2. High-contrast accent insertion line
        dc.DrawLine(IndicatorPen, new Point(4, y), new Point(w - 4, y));

        // 3. Circular end markers
        dc.DrawEllipse(CircleBrush, null, new Point(4, y), 3.0, 3.0);
        dc.DrawEllipse(CircleBrush, null, new Point(w - 4, y), 3.0, 3.0);
    }
}
