using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using LabScape.Core.Enums;
using LabScape.Core.Interfaces;
using LabScape.ViewModels.Commands;
using LabScape.ViewModels.Items;

namespace LabScape.App.Adorners;

public class SelectionAdorner : Adorner
{
    private readonly ItemViewModel _item;
    private readonly IUndoRedoService _undoRedoService;
    private readonly Action _onModified;

    private static readonly Pen BoxPen = new(new SolidColorBrush(Color.FromArgb(220, 37, 99, 235)), 1.5)
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
        ResizeNW, ResizeN, ResizeNE, ResizeE, ResizeSE, ResizeS, ResizeSW, ResizeW,
        Rotate,
        CornerRadius,
        DonutInnerRadius
    }

    private DragMode _activeDrag = DragMode.None;
    private Point _dragStartPoint;
    private Point _dragStartLocal;
    private double _initialAngleOffset;
    private double _initialX, _initialY, _initialW, _initialH, _initialRot;
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

    public SelectionAdorner(UIElement adornedElement, ItemViewModel item, IUndoRedoService undoRedoService, Action onModified)
        : base(adornedElement)
    {
        _item = item;
        _undoRedoService = undoRedoService;
        _onModified = onModified;
        IsHitTestVisible = true;

        _item.PropertyChanged += OnItemPropertyChanged;
        Unloaded += (s, e) => _item.PropertyChanged -= OnItemPropertyChanged;
    }

    private void OnItemPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(ItemViewModel.Rotation) or nameof(ItemViewModel.Width) or nameof(ItemViewModel.Height) or nameof(ItemViewModel.X) or nameof(ItemViewModel.Y))
        {
            InvalidateAdorner();
        }
    }

    public void InvalidateAdorner()
    {
        InvalidateVisual();
        AdornerLayer.GetAdornerLayer(AdornedElement)?.Update(AdornedElement);
    }

    public override GeneralTransform GetDesiredTransform(GeneralTransform transform)
    {
        var group = new GeneralTransformGroup();

        // 1. Rotate in local item space around item center (Width / 2, Height / 2)
        if (Math.Abs(_item.Rotation) > 0.001)
        {
            group.Children.Add(new RotateTransform(_item.Rotation, _item.Width / 2.0, _item.Height / 2.0));
        }

        // 2. Translate to item canvas coordinates (X, Y)
        var baseTransform = base.GetDesiredTransform(transform);
        if (baseTransform != null)
        {
            group.Children.Add(baseTransform);
        }

        return group;
    }

    protected override void OnRender(DrawingContext dc)
    {
        base.OnRender(dc);

        double w = _item.Width;
        double h = _item.Height;

        // Bounding selection rectangle
        dc.DrawRectangle(null, BoxPen, new Rect(-2, -2, w + 4, h + 4));

        // 8 standard resize handles
        DrawHandle(dc, new Point(-2, -2));         // NW
        DrawHandle(dc, new Point(w / 2, -2));      // N
        DrawHandle(dc, new Point(w + 2, -2));      // NE
        DrawHandle(dc, new Point(w + 2, h / 2));   // E
        DrawHandle(dc, new Point(w + 2, h + 2));   // SE
        DrawHandle(dc, new Point(w / 2, h + 2));   // S
        DrawHandle(dc, new Point(-2, h + 2));      // SW
        DrawHandle(dc, new Point(-2, h / 2));      // W

        // Rotate Handle (stalk above N handle)
        dc.DrawLine(BoxPen, new Point(w / 2, -2), new Point(w / 2, -20));
        dc.DrawEllipse(Brushes.White, HandlePen, new Point(w / 2, -20), HandleSize / 2, HandleSize / 2);

        // Special interactive handles
        if (_item is ShapeItemViewModel shape)
        {
            if (shape.ShapeType == ShapeType.RoundedRectangle || shape.ShapeType == ShapeType.RoundedCornerTriangle)
            {
                // Corner radius handle on top edge
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
        _initialRot = _item.Rotation;
        _dragStartPoint = PointToScreen(p);
        _dragStartLocal = p;

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
        if (IsHit(p, new Point(w / 2, -20), HandleSize))
        {
            _activeDrag = DragMode.Rotate;
            Point mouseInAdorned = e.GetPosition(AdornedElement);
            double clickDx = mouseInAdorned.X - (w / 2.0);
            double clickDy = mouseInAdorned.Y - (h / 2.0);
            double clickAngleRad = Math.Atan2(clickDy, clickDx);
            double clickAngleDeg = (clickAngleRad * 180.0 / Math.PI) + 90.0;
            if (clickAngleDeg < 0) clickAngleDeg += 360.0;

            _initialAngleOffset = _initialRot - clickAngleDeg;
            CaptureMouse();
            e.Handled = true;
            return;
        }

        // Check 8 resize handles
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
        Point currentLocal = e.GetPosition(this);

        if (_activeDrag == DragMode.None)
        {
            UpdateCursor(currentLocal);
            return;
        }

        Point currentScreen = PointToScreen(currentLocal);
        double deltaX = currentLocal.X - _dragStartLocal.X;
        double deltaY = currentLocal.Y - _dragStartLocal.Y;

        bool shiftHeld = Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift);
        bool altHeld = Keyboard.IsKeyDown(Key.LeftAlt) || Keyboard.IsKeyDown(Key.RightAlt);

        double ratio = _initialH > 0 ? _initialW / _initialH : 1.0;

        switch (_activeDrag)
        {
            case DragMode.ResizeSE:
            {
                double targetW = Math.Max(20, _initialW + deltaX);
                double targetH = Math.Max(20, _initialH + deltaY);
                if (shiftHeld) targetH = targetW / ratio;

                if (altHeld)
                {
                    double dw = targetW - _initialW;
                    double dh = targetH - _initialH;
                    _item.X = _initialX - dw;
                    _item.Y = _initialY - dh;
                    _item.Width = Math.Max(20, _initialW + 2 * dw);
                    _item.Height = Math.Max(20, _initialH + 2 * dh);
                }
                else
                {
                    _item.Width = targetW;
                    _item.Height = targetH;
                }
                break;
            }

            case DragMode.ResizeE:
            {
                double targetW = Math.Max(20, _initialW + deltaX);
                if (altHeld)
                {
                    double dw = targetW - _initialW;
                    _item.X = _initialX - dw;
                    _item.Width = Math.Max(20, _initialW + 2 * dw);
                }
                else
                {
                    _item.Width = targetW;
                }
                if (shiftHeld) _item.Height = _item.Width / ratio;
                break;
            }

            case DragMode.ResizeS:
            {
                double targetH = Math.Max(20, _initialH + deltaY);
                if (altHeld)
                {
                    double dh = targetH - _initialH;
                    _item.Y = _initialY - dh;
                    _item.Height = Math.Max(20, _initialH + 2 * dh);
                }
                else
                {
                    _item.Height = targetH;
                }
                if (shiftHeld) _item.Width = _item.Height * ratio;
                break;
            }

            case DragMode.ResizeNW:
            {
                double nwW = Math.Max(20, _initialW - deltaX);
                double nwH = Math.Max(20, _initialH - deltaY);
                if (shiftHeld) nwH = nwW / ratio;

                _item.X = _initialX + (_initialW - nwW);
                _item.Y = _initialY + (_initialH - nwH);
                _item.Width = nwW;
                _item.Height = nwH;
                break;
            }

            case DragMode.ResizeN:
            {
                double nH = Math.Max(20, _initialH - deltaY);
                _item.Y = _initialY + (_initialH - nH);
                _item.Height = nH;
                if (shiftHeld) _item.Width = _item.Height * ratio;
                break;
            }

            case DragMode.ResizeNE:
            {
                double neW = Math.Max(20, _initialW + deltaX);
                double neH = Math.Max(20, _initialH - deltaY);
                if (shiftHeld) neH = neW / ratio;

                _item.Y = _initialY + (_initialH - neH);
                _item.Width = neW;
                _item.Height = neH;
                break;
            }

            case DragMode.ResizeSW:
            {
                double swW = Math.Max(20, _initialW - deltaX);
                double swH = Math.Max(20, _initialH + deltaY);
                if (shiftHeld) swH = swW / ratio;

                _item.X = _initialX + (_initialW - swW);
                _item.Width = swW;
                _item.Height = swH;
                break;
            }

            case DragMode.ResizeW:
            {
                double wW = Math.Max(20, _initialW - deltaX);
                _item.X = _initialX + (_initialW - wW);
                _item.Width = wW;
                if (shiftHeld) _item.Height = _item.Width / ratio;
                break;
            }

            case DragMode.Rotate:
            {
                // Calculate rotation angle relative to item center in unrotated AdornedElement coordinates
                Point mouseInAdorned = e.GetPosition(AdornedElement);
                double dx = mouseInAdorned.X - (_initialW / 2.0);
                double dy = mouseInAdorned.Y - (_initialH / 2.0);
                double rad = Math.Atan2(dy, dx);
                double deg = (rad * 180.0 / Math.PI) + 90.0;
                if (deg < 0) deg += 360.0;

                double targetRot = deg + _initialAngleOffset;
                while (targetRot < 0) targetRot += 360.0;
                while (targetRot >= 360.0) targetRot -= 360.0;

                // Shift key snaps to 15-degree steps
                if (shiftHeld)
                {
                    targetRot = Math.Round(targetRot / 15.0) * 15.0;
                    if (targetRot >= 360.0) targetRot = 0.0;
                }

                _item.Rotation = Math.Round(targetRot, 1);
                InvalidateAdorner();
                break;
            }

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

        InvalidateAdorner();
        _onModified?.Invoke();
    }

    protected override void OnMouseUp(MouseButtonEventArgs e)
    {
        base.OnMouseUp(e);
        if (_activeDrag != DragMode.None)
        {
            // Record Undoable Command if bounds or rotation changed
            bool hasChanged = Math.Abs(_item.X - _initialX) > 0.1 ||
                              Math.Abs(_item.Y - _initialY) > 0.1 ||
                              Math.Abs(_item.Width - _initialW) > 0.1 ||
                              Math.Abs(_item.Height - _initialH) > 0.1 ||
                              Math.Abs(_item.Rotation - _initialRot) > 0.1;

            if (hasChanged)
            {
                _undoRedoService.ExecuteCommand(new TransformItemCommand(
                    _item,
                    _initialX, _initialY, _initialW, _initialH, _initialRot,
                    _item.X, _item.Y, _item.Width, _item.Height, _item.Rotation));
            }

            _activeDrag = DragMode.None;
            ReleaseMouseCapture();
            Cursor = Cursors.Arrow;
            _onModified?.Invoke();
        }
    }

    private void UpdateCursor(Point p)
    {
        double w = _item.Width;
        double h = _item.Height;

        if (IsHit(p, new Point(-2, -2), HandleSize) || IsHit(p, new Point(w + 2, h + 2), HandleSize))
            Cursor = Cursors.SizeNWSE;
        else if (IsHit(p, new Point(w + 2, -2), HandleSize) || IsHit(p, new Point(-2, h + 2), HandleSize))
            Cursor = Cursors.SizeNESW;
        else if (IsHit(p, new Point(w / 2, -2), HandleSize) || IsHit(p, new Point(w / 2, h + 2), HandleSize))
            Cursor = Cursors.SizeNS;
        else if (IsHit(p, new Point(-2, h / 2), HandleSize) || IsHit(p, new Point(w + 2, h / 2), HandleSize))
            Cursor = Cursors.SizeWE;
        else if (IsHit(p, new Point(w / 2, -20), HandleSize))
            Cursor = Cursors.Hand;
        else
            Cursor = Cursors.Arrow;
    }

    protected override HitTestResult? HitTestCore(PointHitTestParameters hitTestParameters)
    {
        Point p = hitTestParameters.HitPoint;
        double w = _item.Width;
        double h = _item.Height;

        // Check rotate handle
        if (IsHit(p, new Point(w / 2, -20), HandleSize + 6))
            return new PointHitTestResult(this, p);

        // Check 8 resize handles
        if (IsHit(p, new Point(-2, -2), HandleSize + 4) ||
            IsHit(p, new Point(w / 2, -2), HandleSize + 4) ||
            IsHit(p, new Point(w + 2, -2), HandleSize + 4) ||
            IsHit(p, new Point(w + 2, h / 2), HandleSize + 4) ||
            IsHit(p, new Point(w + 2, h + 2), HandleSize + 4) ||
            IsHit(p, new Point(w / 2, h + 2), HandleSize + 4) ||
            IsHit(p, new Point(-2, h + 2), HandleSize + 4) ||
            IsHit(p, new Point(-2, h / 2), HandleSize + 4))
        {
            return new PointHitTestResult(this, p);
        }

        // Check special handles
        if (_item is ShapeItemViewModel shape)
        {
            if (shape.ShapeType == ShapeType.RoundedRectangle || shape.ShapeType == ShapeType.RoundedCornerTriangle)
            {
                double crOffset = Math.Min(shape.CornerRadius, Math.Min(w, h) / 2);
                if (IsHit(p, new Point(crOffset, 4), SpecialHandleSize + 4))
                    return new PointHitTestResult(this, p);
            }
            else if (shape.ShapeType == ShapeType.Donut)
            {
                double innerRatio = Math.Clamp(shape.InnerRadius / 50.0, 0.1, 0.9);
                double innerRx = (w / 2.0) * innerRatio;
                if (IsHit(p, new Point(w / 2.0 + innerRx, h / 2.0), SpecialHandleSize + 4))
                    return new PointHitTestResult(this, p);
            }
        }

        return null;
    }

    private static bool IsHit(Point test, Point center, double radius)
    {
        return (Math.Abs(test.X - center.X) <= radius) && (Math.Abs(test.Y - center.Y) <= radius);
    }
}
