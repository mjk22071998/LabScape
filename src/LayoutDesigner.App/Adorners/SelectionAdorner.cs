using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using LayoutDesigner.Core.Enums;
using LayoutDesigner.ViewModels.Items;

namespace LayoutDesigner.App.Adorners;

public class SelectionAdorner : Adorner
{
    private readonly ItemViewModel _item;
    private readonly Action _onModified;

    private static readonly Pen BoxPen = new(new SolidColorBrush(Color.FromArgb(200, 37, 99, 235)), 1.5)
    {
        DashStyle = DashStyles.Dash
    };
    private static readonly Brush HandleBrush = Brushes.White;
    private static readonly Pen HandlePen = new(new SolidColorBrush(Color.FromRgb(37, 99, 235)), 1.5);

    private static readonly Brush CornerRadiusBrush = new SolidColorBrush(Color.FromRgb(245, 158, 11)); // Amber
    private static readonly Pen CornerRadiusPen = new(new SolidColorBrush(Color.FromRgb(180, 83, 9)), 1.5);

    private static readonly Brush DonutBrush = new SolidColorBrush(Color.FromRgb(239, 68, 68)); // Red-Orange
    private static readonly Pen DonutPen = new(new SolidColorBrush(Color.FromRgb(185, 28, 28)), 1.5);

    private const double HandleSize = 8.0;
    private const double SpecialHandleSize = 10.0;

    private enum DragMode
    {
        None,
        Move,
        ResizeNW, ResizeN, ResizeNE, ResizeE, ResizeSE, ResizeS, ResizeSW, ResizeW,
        Rotate,
        CornerRadius,
        DonutInnerRadius,
        BezierCP1, BezierCP2
    }

    private DragMode _activeDrag = DragMode.None;
    private Point _dragStartPoint;
    private double _initialX, _initialY, _initialW, _initialH;
    private double _initialCornerRadius, _initialInnerRadius;

    static SelectionAdorner()
    {
        BoxPen.Freeze();
        HandlePen.Freeze();
        CornerRadiusBrush.Freeze();
        CornerRadiusPen.Freeze();
        DonutBrush.Freeze();
        DonutPen.Freeze();
    }

    public SelectionAdorner(UIElement adornedElement, ItemViewModel item, Action onModified)
        : base(adornedElement)
    {
        _item = item;
        _onModified = onModified;
        IsHitTestVisible = true;
    }

    protected override void OnRender(DrawingContext dc)
    {
        base.OnRender(dc);

        double w = _item.Width;
        double h = _item.Height;

        // Draw bounding selection rectangle
        dc.DrawRectangle(null, BoxPen, new Rect(-2, -2, w + 4, h + 4));

        // Draw standard 8 resize handles
        DrawHandle(dc, new Point(-2, -2));         // NW
        DrawHandle(dc, new Point(w / 2, -2));      // N
        DrawHandle(dc, new Point(w + 2, -2));      // NE
        DrawHandle(dc, new Point(w + 2, h / 2));   // E
        DrawHandle(dc, new Point(w + 2, h + 2));   // SE
        DrawHandle(dc, new Point(w / 2, h + 2));   // S
        DrawHandle(dc, new Point(-2, h + 2));      // SW
        DrawHandle(dc, new Point(-2, h / 2));      // W

        // Rotate Handle (stalk above N)
        dc.DrawLine(BoxPen, new Point(w / 2, -2), new Point(w / 2, -18));
        dc.DrawEllipse(Brushes.White, HandlePen, new Point(w / 2, -18), HandleSize / 2, HandleSize / 2);

        // Special interactive handles
        if (_item is ShapeItemViewModel shape)
        {
            if (shape.ShapeType == ShapeType.RoundedRectangle || shape.ShapeType == ShapeType.RoundedCornerTriangle)
            {
                // Corner radius handle on top-left edge
                double crOffset = Math.Min(shape.CornerRadius, Math.Min(w, h) / 2);
                Point crPoint = new(crOffset, 4);
                dc.DrawEllipse(CornerRadiusBrush, CornerRadiusPen, crPoint, SpecialHandleSize / 2, SpecialHandleSize / 2);
            }
            else if (shape.ShapeType == ShapeType.Donut)
            {
                // Inner radius handle inside donut hole
                double innerRatio = Math.Clamp(shape.InnerRadius / 50.0, 0.1, 0.9);
                double innerRx = (w / 2.0) * innerRatio;
                Point donutPoint = new(w / 2.0 + innerRx, h / 2.0);
                dc.DrawEllipse(DonutBrush, DonutPen, donutPoint, SpecialHandleSize / 2, SpecialHandleSize / 2);
            }
        }
    }

    private void DrawHandle(DrawingContext dc, Point center)
    {
        dc.DrawRectangle(HandleBrush, HandlePen, new Rect(center.X - HandleSize / 2, center.Y - HandleSize / 2, HandleSize, HandleSize));
    }

    protected override void OnMouseDown(MouseButtonEventArgs e)
    {
        base.OnMouseDown(e);
        if (e.LeftButton != MouseButtonState.Pressed) return;

        Point p = e.GetPosition(this);
        double w = _item.Width;
        double h = _item.Height;

        _initialX = _item.X;
        _initialY = _item.Y;
        _initialW = _item.Width;
        _initialH = _item.Height;
        _dragStartPoint = PointToScreen(p);

        // Check special handles first
        if (_item is ShapeItemViewModel shape)
        {
            if (shape.ShapeType == ShapeType.RoundedRectangle || shape.ShapeType == ShapeType.RoundedCornerTriangle)
            {
                double crOffset = Math.Min(shape.CornerRadius, Math.Min(w, h) / 2);
                if (IsHit(p, new Point(crOffset, 4), SpecialHandleSize))
                {
                    _activeDrag = DragMode.CornerRadius;
                    _initialCornerRadius = shape.CornerRadius;
                    CaptureMouse();
                    e.Handled = true;
                    return;
                }
            }
            else if (shape.ShapeType == ShapeType.Donut)
            {
                double innerRatio = Math.Clamp(shape.InnerRadius / 50.0, 0.1, 0.9);
                double innerRx = (w / 2.0) * innerRatio;
                if (IsHit(p, new Point(w / 2.0 + innerRx, h / 2.0), SpecialHandleSize))
                {
                    _activeDrag = DragMode.DonutInnerRadius;
                    _initialInnerRadius = shape.InnerRadius;
                    CaptureMouse();
                    e.Handled = true;
                    return;
                }
            }
        }

        // Check rotate handle
        if (IsHit(p, new Point(w / 2, -18), HandleSize))
        {
            _activeDrag = DragMode.Rotate;
            CaptureMouse();
            e.Handled = true;
            return;
        }

        // Check resize handles
        if (IsHit(p, new Point(-2, -2), HandleSize)) _activeDrag = DragMode.ResizeNW;
        else if (IsHit(p, new Point(w / 2, -2), HandleSize)) _activeDrag = DragMode.ResizeN;
        else if (IsHit(p, new Point(w + 2, -2), HandleSize)) _activeDrag = DragMode.ResizeNE;
        else if (IsHit(p, new Point(w + 2, h / 2), HandleSize)) _activeDrag = DragMode.ResizeE;
        else if (IsHit(p, new Point(w + 2, h + 2), HandleSize)) _activeDrag = DragMode.ResizeSE;
        else if (IsHit(p, new Point(w / 2, h + 2), HandleSize)) _activeDrag = DragMode.ResizeS;
        else if (IsHit(p, new Point(-2, h + 2), HandleSize)) _activeDrag = DragMode.ResizeSW;
        else if (IsHit(p, new Point(-2, h / 2), HandleSize)) _activeDrag = DragMode.ResizeW;

        if (_activeDrag != DragMode.None)
        {
            CaptureMouse();
            e.Handled = true;
        }
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        if (_activeDrag == DragMode.None) return;

        Point currentScreen = PointToScreen(e.GetPosition(this));
        double deltaX = currentScreen.X - _dragStartPoint.X;
        double deltaY = currentScreen.Y - _dragStartPoint.Y;

        switch (_activeDrag)
        {
            case DragMode.ResizeSE:
                _item.Width = Math.Max(20, _initialW + deltaX);
                _item.Height = Math.Max(20, _initialH + deltaY);
                break;

            case DragMode.ResizeE:
                _item.Width = Math.Max(20, _initialW + deltaX);
                break;

            case DragMode.ResizeS:
                _item.Height = Math.Max(20, _initialH + deltaY);
                break;

            case DragMode.ResizeNW:
                double nwW = Math.Max(20, _initialW - deltaX);
                double nwH = Math.Max(20, _initialH - deltaY);
                _item.X = _initialX + (_initialW - nwW);
                _item.Y = _initialY + (_initialH - nwH);
                _item.Width = nwW;
                _item.Height = nwH;
                break;

            case DragMode.CornerRadius:
                if (_item is ShapeItemViewModel shapeCr)
                {
                    double newCr = Math.Clamp(_initialCornerRadius + deltaX, 0, Math.Min(_item.Width, _item.Height) / 2);
                    shapeCr.CornerRadius = Math.Round(newCr);
                }
                break;

            case DragMode.DonutInnerRadius:
                if (_item is ShapeItemViewModel shapeDonut)
                {
                    double newInner = Math.Clamp(_initialInnerRadius + deltaX / 2.0, 5, 45);
                    shapeDonut.InnerRadius = Math.Round(newInner);
                }
                break;
        }

        InvalidateVisual();
        _onModified?.Invoke();
    }

    protected override void OnMouseUp(MouseButtonEventArgs e)
    {
        base.OnMouseUp(e);
        if (_activeDrag != DragMode.None)
        {
            _activeDrag = DragMode.None;
            ReleaseMouseCapture();
            _onModified?.Invoke();
        }
    }

    private static bool IsHit(Point test, Point center, double radius)
    {
        return (Math.Abs(test.X - center.X) <= radius) && (Math.Abs(test.Y - center.Y) <= radius);
    }
}
