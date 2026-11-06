using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using LabScape.Core.Enums;
using LabScape.Core.Interfaces;
using LabScape.ViewModels;
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

    // Tangent dotted lines connecting Bezier control points to originating endpoints
    private static readonly Pen DottedTangentPen = new(new SolidColorBrush(Color.FromRgb(99, 102, 241)), 1.5)
    {
        DashStyle = DashStyles.Dot
    };
    private static readonly Brush ControlPointBrush = new SolidColorBrush(Color.FromRgb(99, 102, 241)); // Indigo accent
    private static readonly Pen ControlPointPen = new(Brushes.White, 1.5);

    private static readonly Brush CornerRadiusBrush = new SolidColorBrush(Color.FromRgb(245, 158, 11)); // Amber
    private static readonly Pen CornerRadiusPen = new(new SolidColorBrush(Color.FromRgb(180, 83, 9)), 1.5);

    private static readonly Brush DonutBrush = new SolidColorBrush(Color.FromRgb(239, 68, 68)); // Red-Orange
    private static readonly Pen DonutPen = new(new SolidColorBrush(Color.FromRgb(185, 28, 28)), 1.5);

    public const double BaseHandleSize = 8.0;          // 8 screen pixels
    public const double BaseSpecialHandleSize = 10.0;   // 10 screen pixels
    public const double BaseRotStalkHeight = 22.0;      // 22 screen pixels
    public const double BaseOffset = 2.0;               // 2 screen pixels
    public const double SmallItemThreshold = 28.0;      // In screen pixels: below this, 8 resize handles reduce to 4 corner handles

    private readonly Func<double>? _getZoomLevel;

    public bool ShowHandles { get; set; } = true;

    private enum DragMode
    {
        None,
        ResizeNW, ResizeN, ResizeNE, ResizeE, ResizeSE, ResizeS, ResizeSW, ResizeW,
        Rotate,
        CornerRadius,
        DonutInnerRadius,
        LineStart, LineEnd,
        BezierStart, BezierEnd,
        BezierControlPoint1, BezierControlPoint2
    }

    private DragMode _activeDrag = DragMode.None;
    private Point _dragStartPoint;
    private Point _dragStartLocal;
    private Point _dragStartCanvas;
    private Point _initialCenter;
    private double _initialAngleOffset;
    private double _initialX, _initialY, _initialW, _initialH, _initialRot;
    private double _initialCornerRadius, _initialInnerRadius;
    private double _initialCP1X, _initialCP1Y, _initialCP2X, _initialCP2Y;
    private double _initialStartX, _initialStartY, _initialEndX, _initialEndY;
    private Point _initialCanvasStart, _initialCanvasEnd;

    static SelectionAdorner()
    {
        BoxPen.Freeze();
        HandlePen.Freeze();
        DottedTangentPen.Freeze();
        ControlPointBrush.Freeze();
        ControlPointPen.Freeze();
        CornerRadiusBrush.Freeze();
        CornerRadiusPen.Freeze();
        DonutBrush.Freeze();
        DonutPen.Freeze();
    }

    public SelectionAdorner(UIElement adornedElement, ItemViewModel item, IUndoRedoService undoRedoService, Action onModified)
        : this(adornedElement, item, undoRedoService, onModified, null)
    {
    }

    public SelectionAdorner(UIElement adornedElement, ItemViewModel item, IUndoRedoService undoRedoService, Action onModified, Func<double>? getZoomLevel)
        : base(adornedElement)
    {
        _item = item;
        _undoRedoService = undoRedoService;
        _onModified = onModified;
        _getZoomLevel = getZoomLevel;
        IsHitTestVisible = true;
        ClipToBounds = false;

        _item.PropertyChanged += OnItemPropertyChanged;
        Unloaded += (s, e) => Detach();
    }

    public void Detach()
    {
        _item.PropertyChanged -= OnItemPropertyChanged;
        HideVerticalGuideAction?.Invoke();
        HideHorizontalGuideAction?.Invoke();
    }

    public Action<double, double, double>? ShowVerticalGuideAction { get; set; }
    public Action<double, double, double>? ShowHorizontalGuideAction { get; set; }
    public Action? HideVerticalGuideAction { get; set; }
    public Action? HideHorizontalGuideAction { get; set; }
    public Func<IEnumerable<ItemViewModel>>? GetOtherItems { get; set; }

    public double GetCurrentZoom()
    {
        if (_getZoomLevel != null)
        {
            double z = _getZoomLevel();
            if (z > 0.001) return z;
        }

        if (AdornedElement != null)
        {
            if (AdornedElement is FrameworkElement fe)
            {
                DependencyObject? current = fe;
                while (current != null)
                {
                    if (current is FrameworkElement pfe && pfe.DataContext is CanvasViewModel cvm)
                    {
                        if (cvm.ZoomLevel > 0.001) return cvm.ZoomLevel;
                    }
                    current = VisualTreeHelper.GetParent(current);
                }
            }

            try
            {
                var window = Window.GetWindow(AdornedElement);
                if (window != null)
                {
                    var transform = AdornedElement.TransformToAncestor(window);
                    Point p0 = transform.Transform(new Point(0, 0));
                    Point p1 = transform.Transform(new Point(100, 0));
                    double dist = Math.Sqrt((p1.X - p0.X) * (p1.X - p0.X) + (p1.Y - p0.Y) * (p1.Y - p0.Y));
                    double scale = dist / 100.0;
                    if (scale > 0.001) return scale;
                }
            }
            catch { }
        }

        return 1.0;
    }

    public static double CalculateHandleSize(double zoom) => BaseHandleSize / Math.Max(0.05, zoom);
    public static double CalculateSpecialHandleSize(double zoom) => BaseSpecialHandleSize / Math.Max(0.05, zoom);
    public static double CalculateRotStalkHeight(double zoom) => BaseRotStalkHeight / Math.Max(0.05, zoom);
    public static double CalculateOffset(double zoom) => BaseOffset / Math.Max(0.05, zoom);

    public static bool ShouldUse4CornerHandles(double width, double height, double zoom)
    {
        double screenW = width * Math.Max(0.05, zoom);
        double screenH = height * Math.Max(0.05, zoom);
        return screenW < SmallItemThreshold || screenH < SmallItemThreshold;
    }

    private void OnItemPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (string.IsNullOrEmpty(e.PropertyName) ||
            e.PropertyName is nameof(ItemViewModel.Rotation) or nameof(ItemViewModel.Width) or nameof(ItemViewModel.Height) or nameof(ItemViewModel.X) or nameof(ItemViewModel.Y)
            or nameof(ShapeItemViewModel.ControlPoint1X) or nameof(ShapeItemViewModel.ControlPoint1Y)
            or nameof(ShapeItemViewModel.ControlPoint2X) or nameof(ShapeItemViewModel.ControlPoint2Y)
            or nameof(ShapeItemViewModel.StartX) or nameof(ShapeItemViewModel.StartY)
            or nameof(ShapeItemViewModel.EndX) or nameof(ShapeItemViewModel.EndY)
            or nameof(ShapeItemViewModel.CornerRadius) or nameof(ShapeItemViewModel.InnerRadius)
            or nameof(ShapeItemViewModel.ShapeType) or nameof(TextItemViewModel.IsEditing))
        {
            InvalidateAdorner();
        }
    }

    public void InvalidateAdorner()
    {
        InvalidateVisual();
        try
        {
            var layer = AdornerLayer.GetAdornerLayer(AdornedElement);
            if (layer != null && layer.GetAdorners(AdornedElement) != null)
            {
                layer.Update(AdornedElement);
            }
        }
        catch
        {
            // Suppress WPF AdornerLayer update exceptions when element is no longer in adorner layer
        }
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

    protected override void OnMouseLeave(MouseEventArgs e)
    {
        base.OnMouseLeave(e);
        if (_activeDrag == DragMode.None)
        {
            HideVerticalGuideAction?.Invoke();
            HideHorizontalGuideAction?.Invoke();
        }
    }

    protected override void OnRender(DrawingContext dc)
    {
        base.OnRender(dc);

        if (_item is TextItemViewModel textItem && textItem.IsEditing)
        {
            // Hide adorner handles while user is in active inline text editing mode
            return;
        }

        double w = _item.Width;
        double h = _item.Height;
        double zoom = GetCurrentZoom();

        if (_item.IsLocked)
        {
            // Locked items show bounding box outline but no interactive handles
            double lockOffset = CalculateOffset(zoom);
            Rect lockRect = new(-lockOffset, -lockOffset, w + 2 * lockOffset, h + 2 * lockOffset);
            var lockPen = new Pen(new SolidColorBrush(Color.FromRgb(245, 158, 11)), 1.5)
            {
                DashStyle = DashStyles.Dash
            };
            lockPen.Freeze();
            dc.DrawRectangle(null, lockPen, lockRect);
            return;
        }

        double hs = CalculateHandleSize(zoom);
        double shs = CalculateSpecialHandleSize(zoom);
        double rotOffset = CalculateRotStalkHeight(zoom);
        double offset = CalculateOffset(zoom);
        bool use4Handles = ShouldUse4CornerHandles(w, h, zoom);

        // 1. Line Shape: Exactly 3 handles (Start, End, Rotation) - no outer bounding box
        if (_item is ShapeItemViewModel shapeLine && shapeLine.ShapeType == ShapeType.Line)
        {
            Point pStart = new(0, h / 2.0);
            Point pEnd = new(w, h / 2.0);
            Point pMid = new(w / 2.0, h / 2.0);
            Point pRot = new(w / 2.0, h / 2.0 - rotOffset);

            // Selection guide line
            dc.DrawLine(BoxPen, pStart, pEnd);

            if (!ShowHandles) return;

            // Rotation stalk
            dc.DrawLine(BoxPen, pMid, pRot);

            // 1 & 2: Start and End handles
            DrawHandle(dc, pStart, hs);
            DrawHandle(dc, pEnd, hs);

            // 3: Rotation handle
            dc.DrawEllipse(Brushes.White, HandlePen, pRot, hs / 2, hs / 2);
            return;
        }

        // 2. Bezier Curve Shape: Exactly 5 handles (Start, End, 2 Control Points with dotted originating lines, Rotation)
        if (_item is ShapeItemViewModel shapeBezier && shapeBezier.ShapeType == ShapeType.Bezier)
        {
            Point pStart = new(shapeBezier.StartX, shapeBezier.StartY);
            Point pEnd = new(shapeBezier.EndX, shapeBezier.EndY);
            Point cp1 = new(shapeBezier.ControlPoint1X, shapeBezier.ControlPoint1Y);
            Point cp2 = new(shapeBezier.ControlPoint2X, shapeBezier.ControlPoint2Y);

            double midX = (pStart.X + pEnd.X) / 2.0;
            double topY = Math.Min(Math.Min(pStart.Y, pEnd.Y), Math.Min(cp1.Y, cp2.Y));
            Point pRot = new(midX, topY - rotOffset);

            if (!ShowHandles)
            {
                dc.DrawLine(BoxPen, pStart, pEnd);
                return;
            }

            // Dotted lines connecting control points to their respective originating anchor points
            dc.DrawLine(DottedTangentPen, pStart, cp1);
            dc.DrawLine(DottedTangentPen, pEnd, cp2);

            // Rotation stalk
            dc.DrawLine(BoxPen, new Point(midX, topY), pRot);

            // 1 & 2: Start and End anchor handles
            DrawHandle(dc, pStart, hs);
            DrawHandle(dc, pEnd, hs);

            // 3 & 4: Control points connected to originating points
            dc.DrawEllipse(ControlPointBrush, ControlPointPen, cp1, shs / 2, shs / 2);
            dc.DrawEllipse(ControlPointBrush, ControlPointPen, cp2, shs / 2, shs / 2);

            // 5: Rotation handle
            dc.DrawEllipse(Brushes.White, HandlePen, pRot, hs / 2, hs / 2);
            return;
        }

        // 3. Standard Bounding Box for other shapes & items
        dc.DrawRectangle(null, BoxPen, new Rect(-offset, -offset, w + offset * 2, h + offset * 2));

        if (!ShowHandles) return;

        // 4 corner handles (always present: NW, NE, SE, SW)
        DrawHandle(dc, new Point(-offset, -offset), hs);         // NW
        DrawHandle(dc, new Point(w + offset, -offset), hs);      // NE
        DrawHandle(dc, new Point(w + offset, h + offset), hs);   // SE
        DrawHandle(dc, new Point(-offset, h + offset), hs);      // SW

        // 4 edge midpoint handles (reduced from 8 to 4 corner handles when item is small)
        if (!use4Handles)
        {
            DrawHandle(dc, new Point(w / 2, -offset), hs);       // N
            DrawHandle(dc, new Point(w + offset, h / 2), hs);    // E
            DrawHandle(dc, new Point(w / 2, h + offset), hs);    // S
            DrawHandle(dc, new Point(-offset, h / 2), hs);       // W
        }

        // Rotate Handle (stalk above top edge)
        Point rotPoint = new(w / 2, -offset - rotOffset);
        dc.DrawLine(BoxPen, new Point(w / 2, -offset), rotPoint);
        dc.DrawEllipse(Brushes.White, HandlePen, rotPoint, hs / 2, hs / 2);

        // Special interactive handles
        if (_item is ShapeItemViewModel shape)
        {
            if (shape.ShapeType == ShapeType.RoundedRectangle || shape.ShapeType == ShapeType.RoundedCornerTriangle)
            {
                // Corner radius handle on top edge
                double crOffset = Math.Min(shape.CornerRadius, Math.Min(w, h) / 2);
                Point crPoint = new(crOffset, Math.Max(2.0, 4.0 / zoom));
                dc.DrawEllipse(CornerRadiusBrush, CornerRadiusPen, crPoint, shs / 2, shs / 2);
            }
            else if (shape.ShapeType == ShapeType.Donut)
            {
                // Inner radius handle inside donut hole
                double innerRatio = Math.Clamp(shape.InnerRadius / 50.0, 0.1, 0.9);
                double innerRx = (w / 2.0) * innerRatio;
                Point donutPoint = new(w / 2.0 + innerRx, h / 2.0);
                dc.DrawEllipse(DonutBrush, DonutPen, donutPoint, shs / 2, shs / 2);
            }
        }
    }

    private void DrawHandle(DrawingContext dc, Point center)
    {
        DrawHandle(dc, center, CalculateHandleSize(GetCurrentZoom()));
    }

    private void DrawHandle(DrawingContext dc, Point center, double size)
    {
        dc.DrawRectangle(HandleBrush, HandlePen, new Rect(center.X - size / 2, center.Y - size / 2, size, size));
    }

    protected override void OnMouseDown(MouseButtonEventArgs e)
    {
        base.OnMouseDown(e);
        if (!ShowHandles) return;
        if (_item is TextItemViewModel textItem && textItem.IsEditing) return;
        if (e.LeftButton != MouseButtonState.Pressed) return;

        Point p = e.GetPosition(this);
        double w = _item.Width;
        double h = _item.Height;
        double zoom = GetCurrentZoom();

        double hs = CalculateHandleSize(zoom);
        double shs = CalculateSpecialHandleSize(zoom);
        double rotOffset = CalculateRotStalkHeight(zoom);
        double offset = CalculateOffset(zoom);
        double hitRadius = (BaseHandleSize / 2.0 + 3.0) / Math.Max(0.05, zoom);
        double specialHitRadius = (BaseSpecialHandleSize / 2.0 + 3.0) / Math.Max(0.05, zoom);
        double rotHitRadius = (BaseHandleSize / 2.0 + 4.0) / Math.Max(0.05, zoom);
        bool use4Handles = ShouldUse4CornerHandles(w, h, zoom);

        _initialX = _item.X;
        _initialY = _item.Y;
        _initialW = _item.Width;
        _initialH = _item.Height;
        _initialRot = _item.Rotation;
        _dragStartPoint = PointToScreen(p);
        _dragStartLocal = p;
        _dragStartCanvas = GetCanvasPoint(e);
        _initialCenter = new Point(_initialX + _initialW / 2.0, _initialY + _initialH / 2.0);

        // Line-specific handle hit testing (Exactly 3 handles)
        if (_item is ShapeItemViewModel shapeLine && shapeLine.ShapeType == ShapeType.Line)
        {
            Point pStart = new(0, h / 2.0);
            Point pEnd = new(w, h / 2.0);
            Point pRot = new(w / 2.0, h / 2.0 - rotOffset);

            if (IsHit(p, pRot, rotHitRadius))
            {
                _activeDrag = DragMode.Rotate;
                SetupRotateDrag(e, w, h);
                CaptureMouse();
                e.Handled = true;
                return;
            }

            double rad = _initialRot * Math.PI / 180.0;
            double cos = Math.Cos(rad);
            double sin = Math.Sin(rad);
            Point center = new(_initialX + _initialW / 2.0, _initialY + _initialH / 2.0);

            _initialCanvasStart = new Point(center.X - cos * (_initialW / 2.0), center.Y - sin * (_initialW / 2.0));
            _initialCanvasEnd = new Point(center.X + cos * (_initialW / 2.0), center.Y + sin * (_initialW / 2.0));

            if (IsHit(p, pStart, hitRadius))
            {
                _activeDrag = DragMode.LineStart;
                CaptureMouse();
                e.Handled = true;
                return;
            }
            else if (IsHit(p, pEnd, hitRadius))
            {
                _activeDrag = DragMode.LineEnd;
                CaptureMouse();
                e.Handled = true;
                return;
            }

            return; // Never trigger 8-box resize handles for Line
        }

        // Bezier-specific handle hit testing (Exactly 5 handles)
        if (_item is ShapeItemViewModel shapeBezier && shapeBezier.ShapeType == ShapeType.Bezier)
        {
            Point pStart = new(shapeBezier.StartX, shapeBezier.StartY);
            Point pEnd = new(shapeBezier.EndX, shapeBezier.EndY);
            Point cp1 = new(shapeBezier.ControlPoint1X, shapeBezier.ControlPoint1Y);
            Point cp2 = new(shapeBezier.ControlPoint2X, shapeBezier.ControlPoint2Y);

            double midX = (pStart.X + pEnd.X) / 2.0;
            double topY = Math.Min(Math.Min(pStart.Y, pEnd.Y), Math.Min(cp1.Y, cp2.Y));
            Point pRot = new(midX, topY - rotOffset);

            _initialCP1X = shapeBezier.ControlPoint1X;
            _initialCP1Y = shapeBezier.ControlPoint1Y;
            _initialCP2X = shapeBezier.ControlPoint2X;
            _initialCP2Y = shapeBezier.ControlPoint2Y;
            _initialStartX = shapeBezier.StartX;
            _initialStartY = shapeBezier.StartY;
            _initialEndX = shapeBezier.EndX;
            _initialEndY = shapeBezier.EndY;

            if (IsHit(p, cp1, specialHitRadius))
            {
                _activeDrag = DragMode.BezierControlPoint1;
                CaptureMouse();
                e.Handled = true;
                return;
            }
            else if (IsHit(p, cp2, specialHitRadius))
            {
                _activeDrag = DragMode.BezierControlPoint2;
                CaptureMouse();
                e.Handled = true;
                return;
            }
            else if (IsHit(p, pRot, rotHitRadius))
            {
                _activeDrag = DragMode.Rotate;
                SetupRotateDrag(e, w, h);
                CaptureMouse();
                e.Handled = true;
                return;
            }
            else if (IsHit(p, pStart, hitRadius))
            {
                _activeDrag = DragMode.BezierStart;
                CaptureMouse();
                e.Handled = true;
                return;
            }
            else if (IsHit(p, pEnd, hitRadius))
            {
                _activeDrag = DragMode.BezierEnd;
                CaptureMouse();
                e.Handled = true;
                return;
            }

            return; // Never trigger 8-box resize handles for Bezier
        }

        // Check special handles for RoundedRectangle & Donut
        if (_item is ShapeItemViewModel shapeSpecial)
        {
            if (shapeSpecial.ShapeType == ShapeType.RoundedRectangle || shapeSpecial.ShapeType == ShapeType.RoundedCornerTriangle)
            {
                double crOffset = Math.Min(shapeSpecial.CornerRadius, Math.Min(w, h) / 2);
                if (IsHit(p, new Point(crOffset, Math.Max(2.0, 4.0 / zoom)), specialHitRadius))
                {
                    _activeDrag = DragMode.CornerRadius;
                    _initialCornerRadius = shapeSpecial.CornerRadius;
                    CaptureMouse();
                    e.Handled = true;
                    return;
                }
            }
            else if (shapeSpecial.ShapeType == ShapeType.Donut)
            {
                double innerRatio = Math.Clamp(shapeSpecial.InnerRadius / 50.0, 0.1, 0.9);
                double innerRx = (w / 2.0) * innerRatio;
                if (IsHit(p, new Point(w / 2.0 + innerRx, h / 2.0), specialHitRadius))
                {
                    _activeDrag = DragMode.DonutInnerRadius;
                    _initialInnerRadius = shapeSpecial.InnerRadius;
                    CaptureMouse();
                    e.Handled = true;
                    return;
                }
            }
        }

        // Check rotate handle for standard items
        Point pRotStandard = new(w / 2, -offset - rotOffset);
        if (IsHit(p, pRotStandard, rotHitRadius))
        {
            _activeDrag = DragMode.Rotate;
            SetupRotateDrag(e, w, h);
            CaptureMouse();
            e.Handled = true;
            return;
        }

        // Check 4 corner resize handles (always available)
        if (IsHit(p, new Point(-offset, -offset), hitRadius)) _activeDrag = DragMode.ResizeNW;
        else if (IsHit(p, new Point(w + offset, -offset), hitRadius)) _activeDrag = DragMode.ResizeNE;
        else if (IsHit(p, new Point(w + offset, h + offset), hitRadius)) _activeDrag = DragMode.ResizeSE;
        else if (IsHit(p, new Point(-offset, h + offset), hitRadius)) _activeDrag = DragMode.ResizeSW;
        // Check 4 edge midpoint resize handles (only when NOT in 4-corner mode)
        else if (!use4Handles)
        {
            if (IsHit(p, new Point(w / 2, -offset), hitRadius)) _activeDrag = DragMode.ResizeN;
            else if (IsHit(p, new Point(w + offset, h / 2), hitRadius)) _activeDrag = DragMode.ResizeE;
            else if (IsHit(p, new Point(w / 2, h + offset), hitRadius)) _activeDrag = DragMode.ResizeS;
            else if (IsHit(p, new Point(-offset, h / 2), hitRadius)) _activeDrag = DragMode.ResizeW;
        }

        if (_activeDrag != DragMode.None)
        {
            CaptureMouse();
            e.Handled = true;
        }
    }

    private UIElement? FindCanvas()
    {
        DependencyObject current = AdornedElement;
        UIElement? found = null;
        while (current != null)
        {
            if (current is Canvas c)
            {
                found = c;
                if (c.Name == "CanvasSurface")
                    return c;
            }
            current = VisualTreeHelper.GetParent(current);
        }
        return found;
    }

    private Point GetMouseOnCanvas(MouseEventArgs e)
    {
        var canvas = FindCanvas();
        if (canvas != null)
        {
            return e.GetPosition(canvas);
        }
        return e.GetPosition(AdornedElement);
    }

    private Point GetCanvasPoint(MouseEventArgs e) => GetMouseOnCanvas(e);

    private void SetupRotateDrag(MouseButtonEventArgs e, double w, double h)
    {
        Point mouseCanvas = GetMouseOnCanvas(e);
        Point center = new(_initialX + _initialW / 2.0, _initialY + _initialH / 2.0);
        double clickDx = mouseCanvas.X - center.X;
        double clickDy = mouseCanvas.Y - center.Y;
        double clickAngleRad = Math.Atan2(clickDy, clickDx);
        double clickAngleDeg = (clickAngleRad * 180.0 / Math.PI) + 90.0;
        if (clickAngleDeg < 0) clickAngleDeg += 360.0;

        _initialAngleOffset = _initialRot - clickAngleDeg;
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

        Point currentCanvas = GetMouseOnCanvas(e);
        double deltaCanvasX = currentCanvas.X - _dragStartCanvas.X;
        double deltaCanvasY = currentCanvas.Y - _dragStartCanvas.Y;

        double rad = _initialRot * Math.PI / 180.0;
        double cos = Math.Cos(rad);
        double sin = Math.Sin(rad);

        // Project invariant canvas mouse movement into item's unrotated local axes
        double localDX = deltaCanvasX * cos + deltaCanvasY * sin;
        double localDY = -deltaCanvasX * sin + deltaCanvasY * cos;

        bool shiftHeld = Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift);
        bool altHeld = Keyboard.IsKeyDown(Key.LeftAlt) || Keyboard.IsKeyDown(Key.RightAlt);

        double ratio = _initialH > 0 ? _initialW / _initialH : 1.0;
        double w = _initialW;
        double h = _initialH;
        double shiftLocalX = 0;
        double shiftLocalY = 0;

        switch (_activeDrag)
        {
            case DragMode.LineEnd:
            {
                Point mouseCanvas = currentCanvas;
                ApplyLineEndpointAlignmentSnapping(ref mouseCanvas, _initialCanvasStart);
                double vx = mouseCanvas.X - _initialCanvasStart.X;
                double vy = mouseCanvas.Y - _initialCanvasStart.Y;
                double len = Math.Max(15.0, Math.Sqrt(vx * vx + vy * vy));
                double angleDeg = Math.Atan2(vy, vx) * 180.0 / Math.PI;
                if (angleDeg < 0) angleDeg += 360.0;

                if (shiftHeld)
                {
                    angleDeg = Math.Round(angleDeg / 15.0) * 15.0;
                    if (angleDeg >= 360.0) angleDeg = 0.0;
                }

                double lineRad = angleDeg * Math.PI / 180.0;
                double lineCos = Math.Cos(lineRad);
                double lineSin = Math.Sin(lineRad);
                Point newCenter = new(_initialCanvasStart.X + lineCos * (len / 2.0), _initialCanvasStart.Y + lineSin * (len / 2.0));

                _item.X = Math.Round(newCenter.X - len / 2.0, 1);
                _item.Y = Math.Round(newCenter.Y - _initialH / 2.0, 1);
                _item.Width = Math.Round(len, 1);
                _item.Rotation = Math.Round(angleDeg, 1);
                break;
            }

            case DragMode.LineStart:
            {
                Point mouseCanvas = currentCanvas;
                ApplyLineEndpointAlignmentSnapping(ref mouseCanvas, _initialCanvasEnd);
                double vx = _initialCanvasEnd.X - mouseCanvas.X;
                double vy = _initialCanvasEnd.Y - mouseCanvas.Y;
                double len = Math.Max(15.0, Math.Sqrt(vx * vx + vy * vy));
                double angleDeg = Math.Atan2(vy, vx) * 180.0 / Math.PI;
                if (angleDeg < 0) angleDeg += 360.0;

                if (shiftHeld)
                {
                    angleDeg = Math.Round(angleDeg / 15.0) * 15.0;
                    if (angleDeg >= 360.0) angleDeg = 0.0;
                }

                double lineRad = angleDeg * Math.PI / 180.0;
                double lineCos = Math.Cos(lineRad);
                double lineSin = Math.Sin(lineRad);
                Point newCenter = new(_initialCanvasEnd.X - lineCos * (len / 2.0), _initialCanvasEnd.Y - lineSin * (len / 2.0));

                _item.X = Math.Round(newCenter.X - len / 2.0, 1);
                _item.Y = Math.Round(newCenter.Y - _initialH / 2.0, 1);
                _item.Width = Math.Round(len, 1);
                _item.Rotation = Math.Round(angleDeg, 1);
                break;
            }

            case DragMode.BezierControlPoint1:
                if (_item is ShapeItemViewModel shapeCp1)
                {
                    shapeCp1.ControlPoint1X = Math.Round(currentLocal.X, 1);
                    shapeCp1.ControlPoint1Y = Math.Round(currentLocal.Y, 1);
                    UpdateBezierDimensions(shapeCp1);
                }
                break;

            case DragMode.BezierControlPoint2:
                if (_item is ShapeItemViewModel shapeCp2)
                {
                    shapeCp2.ControlPoint2X = Math.Round(currentLocal.X, 1);
                    shapeCp2.ControlPoint2Y = Math.Round(currentLocal.Y, 1);
                    UpdateBezierDimensions(shapeCp2);
                }
                break;

            case DragMode.BezierStart:
                if (_item is ShapeItemViewModel shapeStart)
                {
                    shapeStart.StartX = Math.Round(currentLocal.X, 1);
                    shapeStart.StartY = Math.Round(currentLocal.Y, 1);
                    UpdateBezierDimensions(shapeStart);
                }
                break;

            case DragMode.BezierEnd:
                if (_item is ShapeItemViewModel shapeEnd)
                {
                    shapeEnd.EndX = Math.Round(currentLocal.X, 1);
                    shapeEnd.EndY = Math.Round(currentLocal.Y, 1);
                    UpdateBezierDimensions(shapeEnd);
                }
                break;

            case DragMode.ResizeE:
            {
                if (altHeld)
                {
                    w = Math.Max(20, _initialW + 2 * localDX);
                    h = _initialH;
                    if (shiftHeld) h = Math.Max(20, w / ratio);
                    shiftLocalX = 0;
                    shiftLocalY = 0;
                }
                else
                {
                    w = Math.Max(20, _initialW + localDX);
                    h = _initialH;
                    if (shiftHeld) h = Math.Max(20, w / ratio);
                    shiftLocalX = (w - _initialW) / 2.0;
                    shiftLocalY = shiftHeld ? (h - _initialH) / 2.0 : 0;
                }
                break;
            }

            case DragMode.ResizeW:
            {
                if (altHeld)
                {
                    w = Math.Max(20, _initialW - 2 * localDX);
                    h = _initialH;
                    if (shiftHeld) h = Math.Max(20, w / ratio);
                    shiftLocalX = 0;
                    shiftLocalY = 0;
                }
                else
                {
                    w = Math.Max(20, _initialW - localDX);
                    h = _initialH;
                    if (shiftHeld) h = Math.Max(20, w / ratio);
                    shiftLocalX = -(w - _initialW) / 2.0;
                    shiftLocalY = shiftHeld ? (h - _initialH) / 2.0 : 0;
                }
                break;
            }

            case DragMode.ResizeS:
            {
                if (altHeld)
                {
                    h = Math.Max(20, _initialH + 2 * localDY);
                    w = _initialW;
                    if (shiftHeld) w = Math.Max(20, h * ratio);
                    shiftLocalX = 0;
                    shiftLocalY = 0;
                }
                else
                {
                    h = Math.Max(20, _initialH + localDY);
                    w = _initialW;
                    if (shiftHeld) w = Math.Max(20, h * ratio);
                    shiftLocalX = shiftHeld ? (w - _initialW) / 2.0 : 0;
                    shiftLocalY = (h - _initialH) / 2.0;
                }
                break;
            }

            case DragMode.ResizeN:
            {
                if (altHeld)
                {
                    h = Math.Max(20, _initialH - 2 * localDY);
                    w = _initialW;
                    if (shiftHeld) w = Math.Max(20, h * ratio);
                    shiftLocalX = 0;
                    shiftLocalY = 0;
                }
                else
                {
                    h = Math.Max(20, _initialH - localDY);
                    w = _initialW;
                    if (shiftHeld) w = Math.Max(20, h * ratio);
                    shiftLocalX = shiftHeld ? (w - _initialW) / 2.0 : 0;
                    shiftLocalY = -(h - _initialH) / 2.0;
                }
                break;
            }

            case DragMode.ResizeSE:
            {
                w = Math.Max(20, _initialW + localDX);
                h = Math.Max(20, _initialH + localDY);
                if (shiftHeld)
                {
                    double scale = Math.Max(w / _initialW, h / _initialH);
                    w = Math.Max(20, _initialW * scale);
                    h = Math.Max(20, _initialH * scale);
                }
                if (altHeld)
                {
                    w = Math.Max(20, _initialW + 2 * localDX);
                    h = Math.Max(20, _initialH + 2 * localDY);
                    if (shiftHeld)
                    {
                        double scale = Math.Max(w / _initialW, h / _initialH);
                        w = Math.Max(20, _initialW * scale);
                        h = Math.Max(20, _initialH * scale);
                    }
                    shiftLocalX = 0;
                    shiftLocalY = 0;
                }
                else
                {
                    shiftLocalX = (w - _initialW) / 2.0;
                    shiftLocalY = (h - _initialH) / 2.0;
                }
                break;
            }

            case DragMode.ResizeNW:
            {
                w = Math.Max(20, _initialW - localDX);
                h = Math.Max(20, _initialH - localDY);
                if (shiftHeld)
                {
                    double scale = Math.Max(w / _initialW, h / _initialH);
                    w = Math.Max(20, _initialW * scale);
                    h = Math.Max(20, _initialH * scale);
                }
                if (altHeld)
                {
                    w = Math.Max(20, _initialW - 2 * localDX);
                    h = Math.Max(20, _initialH - 2 * localDY);
                    if (shiftHeld)
                    {
                        double scale = Math.Max(w / _initialW, h / _initialH);
                        w = Math.Max(20, _initialW * scale);
                        h = Math.Max(20, _initialH * scale);
                    }
                    shiftLocalX = 0;
                    shiftLocalY = 0;
                }
                else
                {
                    shiftLocalX = -(w - _initialW) / 2.0;
                    shiftLocalY = -(h - _initialH) / 2.0;
                }
                break;
            }

            case DragMode.ResizeNE:
            {
                w = Math.Max(20, _initialW + localDX);
                h = Math.Max(20, _initialH - localDY);
                if (shiftHeld)
                {
                    double scale = Math.Max(w / _initialW, h / _initialH);
                    w = Math.Max(20, _initialW * scale);
                    h = Math.Max(20, _initialH * scale);
                }
                if (altHeld)
                {
                    w = Math.Max(20, _initialW + 2 * localDX);
                    h = Math.Max(20, _initialH - 2 * localDY);
                    if (shiftHeld)
                    {
                        double scale = Math.Max(w / _initialW, h / _initialH);
                        w = Math.Max(20, _initialW * scale);
                        h = Math.Max(20, _initialH * scale);
                    }
                    shiftLocalX = 0;
                    shiftLocalY = 0;
                }
                else
                {
                    shiftLocalX = (w - _initialW) / 2.0;
                    shiftLocalY = -(h - _initialH) / 2.0;
                }
                break;
            }

            case DragMode.ResizeSW:
            {
                w = Math.Max(20, _initialW - localDX);
                h = Math.Max(20, _initialH + localDY);
                if (shiftHeld)
                {
                    double scale = Math.Max(w / _initialW, h / _initialH);
                    w = Math.Max(20, _initialW * scale);
                    h = Math.Max(20, _initialH * scale);
                }
                if (altHeld)
                {
                    w = Math.Max(20, _initialW - 2 * localDX);
                    h = Math.Max(20, _initialH + 2 * localDY);
                    if (shiftHeld)
                    {
                        double scale = Math.Max(w / _initialW, h / _initialH);
                        w = Math.Max(20, _initialW * scale);
                        h = Math.Max(20, _initialH * scale);
                    }
                    shiftLocalX = 0;
                    shiftLocalY = 0;
                }
                else
                {
                    shiftLocalX = -(w - _initialW) / 2.0;
                    shiftLocalY = (h - _initialH) / 2.0;
                }
                break;
            }

            case DragMode.Rotate:
            {
                Point center = _initialCenter;
                double dx = currentCanvas.X - center.X;
                double dy = currentCanvas.Y - center.Y;
                double radM = Math.Atan2(dy, dx);
                double deg = (radM * 180.0 / Math.PI) + 90.0;
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
                    double localX = (currentCanvas.X - _initialCenter.X) * cos + (currentCanvas.Y - _initialCenter.Y) * sin + _initialW / 2.0;
                    double maxCr = Math.Min(_item.Width, _item.Height) / 2.0;
                    double newCr = Math.Clamp(localX, 0, maxCr);
                    shapeCr.CornerRadius = Math.Round(newCr);
                }
                break;

            case DragMode.DonutInnerRadius:
                if (_item is ShapeItemViewModel shapeDonut)
                {
                    double localX = (currentCanvas.X - _initialCenter.X) * cos + (currentCanvas.Y - _initialCenter.Y) * sin + _initialW / 2.0;
                    double dist = Math.Abs(localX - (_item.Width / 2.0));
                    double maxRx = _item.Width / 2.0;
                    double donutRatio = maxRx > 0 ? dist / maxRx : 0.5;
                    double newInner = Math.Clamp(donutRatio * 50.0, 5, 45);
                    shapeDonut.InnerRadius = Math.Round(newInner);
                }
                break;
        }

        if (_activeDrag is DragMode.ResizeNW or DragMode.ResizeN or DragMode.ResizeNE or DragMode.ResizeE
            or DragMode.ResizeSE or DragMode.ResizeS or DragMode.ResizeSW or DragMode.ResizeW)
        {
            ApplyResizeAlignmentSnapping(ref w, ref h, ref shiftLocalX, ref shiftLocalY, altHeld, shiftHeld, ratio);

            double worldShiftX = shiftLocalX * cos - shiftLocalY * sin;
            double worldShiftY = shiftLocalX * sin + shiftLocalY * cos;

            Point newCenter = new(_initialCenter.X + worldShiftX, _initialCenter.Y + worldShiftY);
            _item.X = Math.Round(newCenter.X - w / 2.0, 1);
            _item.Y = Math.Round(newCenter.Y - h / 2.0, 1);
            _item.Width = Math.Round(w, 1);
            _item.Height = Math.Round(h, 1);
        }
        else if (_activeDrag is not DragMode.LineStart and not DragMode.LineEnd)
        {
            HideVerticalGuideAction?.Invoke();
            HideHorizontalGuideAction?.Invoke();
        }

        InvalidateAdorner();
        _onModified?.Invoke();
    }

    protected override void OnMouseUp(MouseButtonEventArgs e)
    {
        base.OnMouseUp(e);
        HideVerticalGuideAction?.Invoke();
        HideHorizontalGuideAction?.Invoke();
        if (_activeDrag != DragMode.None)
        {
            if (_activeDrag == DragMode.CornerRadius)
            {
                if (_item is ShapeItemViewModel shape && Math.Abs(shape.CornerRadius - _initialCornerRadius) > 0.1)
                {
                    _undoRedoService.ExecuteCommand(new ModifyCornerRadiusCommand(shape, _initialCornerRadius, shape.CornerRadius));
                }
            }
            else if (_activeDrag == DragMode.DonutInnerRadius)
            {
                if (_item is ShapeItemViewModel shape && Math.Abs(shape.InnerRadius - _initialInnerRadius) > 0.1)
                {
                    _undoRedoService.ExecuteCommand(new ModifyInnerRadiusCommand(shape, _initialInnerRadius, shape.InnerRadius));
                }
            }
            else if (_activeDrag is DragMode.BezierStart or DragMode.BezierEnd or DragMode.BezierControlPoint1 or DragMode.BezierControlPoint2)
            {
                if (_item is ShapeItemViewModel shape)
                {
                    bool changed = Math.Abs(shape.ControlPoint1X - _initialCP1X) > 0.1 ||
                                  Math.Abs(shape.ControlPoint1Y - _initialCP1Y) > 0.1 ||
                                  Math.Abs(shape.ControlPoint2X - _initialCP2X) > 0.1 ||
                                  Math.Abs(shape.ControlPoint2Y - _initialCP2Y) > 0.1 ||
                                  Math.Abs(shape.StartX - _initialStartX) > 0.1 ||
                                  Math.Abs(shape.StartY - _initialStartY) > 0.1 ||
                                  Math.Abs(shape.EndX - _initialEndX) > 0.1 ||
                                  Math.Abs(shape.EndY - _initialEndY) > 0.1;

                    if (changed)
                    {
                        _undoRedoService.ExecuteCommand(new ModifyBezierControlPointsCommand(
                            shape,
                            _initialStartX, _initialStartY, _initialEndX, _initialEndY,
                            _initialCP1X, _initialCP1Y, _initialCP2X, _initialCP2Y,
                            shape.StartX, shape.StartY, shape.EndX, shape.EndY,
                            shape.ControlPoint1X, shape.ControlPoint1Y, shape.ControlPoint2X, shape.ControlPoint2Y));
                    }
                }
            }
            else
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
        double zoom = GetCurrentZoom();

        double rotOffset = CalculateRotStalkHeight(zoom);
        double offset = CalculateOffset(zoom);
        double hitRadius = (BaseHandleSize / 2.0 + 3.0) / Math.Max(0.05, zoom);
        double specialHitRadius = (BaseSpecialHandleSize / 2.0 + 3.0) / Math.Max(0.05, zoom);
        double rotHitRadius = (BaseHandleSize / 2.0 + 4.0) / Math.Max(0.05, zoom);
        bool use4Handles = ShouldUse4CornerHandles(w, h, zoom);

        if (_item is ShapeItemViewModel shapeLine && shapeLine.ShapeType == ShapeType.Line)
        {
            Point pStart = new(0, h / 2.0);
            Point pEnd = new(w, h / 2.0);
            Point pRot = new(w / 2.0, h / 2.0 - rotOffset);

            if (IsHit(p, pRot, rotHitRadius))
                Cursor = Cursors.Hand;
            else if (IsHit(p, pStart, hitRadius) || IsHit(p, pEnd, hitRadius))
                Cursor = Cursors.Cross;
            else
                Cursor = Cursors.Arrow;
            return;
        }

        if (_item is ShapeItemViewModel shapeBezier && shapeBezier.ShapeType == ShapeType.Bezier)
        {
            Point pStart = new(shapeBezier.StartX, shapeBezier.StartY);
            Point pEnd = new(shapeBezier.EndX, shapeBezier.EndY);
            Point cp1 = new(shapeBezier.ControlPoint1X, shapeBezier.ControlPoint1Y);
            Point cp2 = new(shapeBezier.ControlPoint2X, shapeBezier.ControlPoint2Y);

            double midX = (pStart.X + pEnd.X) / 2.0;
            double topY = Math.Min(Math.Min(pStart.Y, pEnd.Y), Math.Min(cp1.Y, cp2.Y));
            Point pRot = new(midX, topY - rotOffset);

            if (IsHit(p, pRot, rotHitRadius) || IsHit(p, cp1, specialHitRadius) || IsHit(p, cp2, specialHitRadius))
                Cursor = Cursors.Hand;
            else if (IsHit(p, pStart, hitRadius) || IsHit(p, pEnd, hitRadius))
                Cursor = Cursors.Cross;
            else
                Cursor = Cursors.Arrow;
            return;
        }

        // Check special handles for RoundedRectangle & Donut
        if (_item is ShapeItemViewModel shapeSpecial)
        {
            if (shapeSpecial.ShapeType == ShapeType.RoundedRectangle || shapeSpecial.ShapeType == ShapeType.RoundedCornerTriangle)
            {
                double crOffset = Math.Min(shapeSpecial.CornerRadius, Math.Min(w, h) / 2);
                if (IsHit(p, new Point(crOffset, Math.Max(2.0, 4.0 / zoom)), specialHitRadius))
                {
                    Cursor = Cursors.Hand;
                    return;
                }
            }
            else if (shapeSpecial.ShapeType == ShapeType.Donut)
            {
                double innerRatio = Math.Clamp(shapeSpecial.InnerRadius / 50.0, 0.1, 0.9);
                double innerRx = (w / 2.0) * innerRatio;
                if (IsHit(p, new Point(w / 2.0 + innerRx, h / 2.0), specialHitRadius))
                {
                    Cursor = Cursors.Hand;
                    return;
                }
            }
        }

        Point pRotStandard = new(w / 2, -offset - rotOffset);
        if (IsHit(p, pRotStandard, rotHitRadius))
        {
            Cursor = Cursors.Hand;
            return;
        }

        // Check 4 corner handles
        if (IsHit(p, new Point(-offset, -offset), hitRadius) || IsHit(p, new Point(w + offset, h + offset), hitRadius))
            Cursor = Cursors.SizeNWSE;
        else if (IsHit(p, new Point(w + offset, -offset), hitRadius) || IsHit(p, new Point(-offset, h + offset), hitRadius))
            Cursor = Cursors.SizeNESW;
        // Check 4 midpoint handles (only when NOT in 4-corner mode)
        else if (!use4Handles && (IsHit(p, new Point(w / 2, -offset), hitRadius) || IsHit(p, new Point(w / 2, h + offset), hitRadius)))
            Cursor = Cursors.SizeNS;
        else if (!use4Handles && (IsHit(p, new Point(-offset, h / 2), hitRadius) || IsHit(p, new Point(w + offset, h / 2), hitRadius)))
            Cursor = Cursors.SizeWE;
        else
            Cursor = Cursors.Arrow;
    }

    protected override HitTestResult? HitTestCore(PointHitTestParameters hitTestParameters)
    {
        if (_item.IsLocked) return null;

        Point p = hitTestParameters.HitPoint;
        double w = _item.Width;
        double h = _item.Height;
        double zoom = GetCurrentZoom();

        double rotOffset = CalculateRotStalkHeight(zoom);
        double offset = CalculateOffset(zoom);
        double hitRadius = (BaseHandleSize / 2.0 + 3.0) / Math.Max(0.05, zoom);
        double specialHitRadius = (BaseSpecialHandleSize / 2.0 + 3.0) / Math.Max(0.05, zoom);
        double rotHitRadius = (BaseHandleSize / 2.0 + 4.0) / Math.Max(0.05, zoom);
        bool use4Handles = ShouldUse4CornerHandles(w, h, zoom);

        // Line item has only 3 handles (Start, End, Rotation)
        if (_item is ShapeItemViewModel shapeLine && shapeLine.ShapeType == ShapeType.Line)
        {
            Point pStart = new(0, h / 2.0);
            Point pEnd = new(w, h / 2.0);
            Point pRot = new(w / 2.0, h / 2.0 - rotOffset);

            if (IsHit(p, pRot, rotHitRadius) ||
                IsHit(p, pStart, hitRadius) ||
                IsHit(p, pEnd, hitRadius))
            {
                return new PointHitTestResult(this, p);
            }
            return null;
        }

        // Bezier curve has only 5 handles (Start, End, CP1, CP2, Rotation)
        if (_item is ShapeItemViewModel shapeBezier && shapeBezier.ShapeType == ShapeType.Bezier)
        {
            Point pStart = new(shapeBezier.StartX, shapeBezier.StartY);
            Point pEnd = new(shapeBezier.EndX, shapeBezier.EndY);
            Point cp1 = new(shapeBezier.ControlPoint1X, shapeBezier.ControlPoint1Y);
            Point cp2 = new(shapeBezier.ControlPoint2X, shapeBezier.ControlPoint2Y);

            double midX = (pStart.X + pEnd.X) / 2.0;
            double topY = Math.Min(Math.Min(pStart.Y, pEnd.Y), Math.Min(cp1.Y, cp2.Y));
            Point pRot = new(midX, topY - rotOffset);

            if (IsHit(p, pRot, rotHitRadius) ||
                IsHit(p, cp1, specialHitRadius) ||
                IsHit(p, cp2, specialHitRadius) ||
                IsHit(p, pStart, hitRadius) ||
                IsHit(p, pEnd, hitRadius))
            {
                return new PointHitTestResult(this, p);
            }
            return null;
        }

        // Check special handles for RoundedRectangle & Donut FIRST (so they take precedence over resize/rotate)
        if (_item is ShapeItemViewModel shapeSpecial)
        {
            if (shapeSpecial.ShapeType == ShapeType.RoundedRectangle || shapeSpecial.ShapeType == ShapeType.RoundedCornerTriangle)
            {
                double crOffset = Math.Min(shapeSpecial.CornerRadius, Math.Min(w, h) / 2);
                if (IsHit(p, new Point(crOffset, Math.Max(2.0, 4.0 / zoom)), specialHitRadius))
                    return new PointHitTestResult(this, p);
            }
            else if (shapeSpecial.ShapeType == ShapeType.Donut)
            {
                double innerRatio = Math.Clamp(shapeSpecial.InnerRadius / 50.0, 0.1, 0.9);
                double innerRx = (w / 2.0) * innerRatio;
                if (IsHit(p, new Point(w / 2.0 + innerRx, h / 2.0), specialHitRadius))
                    return new PointHitTestResult(this, p);
            }
        }

        // Check rotate handle for standard items
        Point pRotStandard = new(w / 2, -offset - rotOffset);
        if (IsHit(p, pRotStandard, rotHitRadius))
            return new PointHitTestResult(this, p);

        // Check 4 corner resize handles
        if (IsHit(p, new Point(-offset, -offset), hitRadius) ||
            IsHit(p, new Point(w + offset, -offset), hitRadius) ||
            IsHit(p, new Point(w + offset, h + offset), hitRadius) ||
            IsHit(p, new Point(-offset, h + offset), hitRadius))
        {
            return new PointHitTestResult(this, p);
        }

        // Check 4 midpoint handles if NOT in 4-handle mode
        if (!use4Handles)
        {
            if (IsHit(p, new Point(w / 2, -offset), hitRadius) ||
                IsHit(p, new Point(w + offset, h / 2), hitRadius) ||
                IsHit(p, new Point(w / 2, h + offset), hitRadius) ||
                IsHit(p, new Point(-offset, h / 2), hitRadius))
            {
                return new PointHitTestResult(this, p);
            }
        }

        return null;
    }

    private static void UpdateBezierDimensions(ShapeItemViewModel shape)
    {
        double minX = Math.Min(Math.Min(shape.StartX, shape.EndX), Math.Min(shape.ControlPoint1X, shape.ControlPoint2X));
        double maxX = Math.Max(Math.Max(shape.StartX, shape.EndX), Math.Max(shape.ControlPoint1X, shape.ControlPoint2X));
        double minY = Math.Min(Math.Min(shape.StartY, shape.EndY), Math.Min(shape.ControlPoint1Y, shape.ControlPoint2Y));
        double maxY = Math.Max(Math.Max(shape.StartY, shape.EndY), Math.Max(shape.ControlPoint1Y, shape.ControlPoint2Y));

        double spanX = minX < 0 ? maxX - minX : maxX;
        double spanY = minY < 0 ? maxY - minY : maxY;

        shape.Width = Math.Max(20.0, Math.Round(spanX, 1));
        shape.Height = Math.Max(20.0, Math.Round(spanY, 1));
    }

    private static bool IsHit(Point test, Point center, double radius)
    {
        return (Math.Abs(test.X - center.X) <= radius) && (Math.Abs(test.Y - center.Y) <= radius);
    }

    private void ApplyResizeAlignmentSnapping(
        ref double w, ref double h,
        ref double shiftLocalX, ref double shiftLocalY,
        bool altHeld, bool shiftHeld, double ratio)
    {
        if (Math.Abs(_initialRot) > 0.01)
        {
            HideVerticalGuideAction?.Invoke();
            HideHorizontalGuideAction?.Invoke();
            return;
        }

        IEnumerable<ItemViewModel>? others = GetOtherItems?.Invoke();
        if (others == null)
        {
            var canvas = FindCanvas();
            if (canvas is FrameworkElement fe && fe.DataContext is CanvasViewModel cvm)
            {
                others = cvm.Items;
            }
        }

        if (others == null)
        {
            HideVerticalGuideAction?.Invoke();
            HideHorizontalGuideAction?.Invoke();
            return;
        }

        const double snapTolerance = 6.0;
        bool snapXFound = false;
        bool snapYFound = false;

        bool movesRight = _activeDrag is DragMode.ResizeE or DragMode.ResizeSE or DragMode.ResizeNE;
        bool movesLeft = _activeDrag is DragMode.ResizeW or DragMode.ResizeSW or DragMode.ResizeNW;
        bool movesBottom = _activeDrag is DragMode.ResizeS or DragMode.ResizeSE or DragMode.ResizeSW;
        bool movesTop = _activeDrag is DragMode.ResizeN or DragMode.ResizeNE or DragMode.ResizeNW;

        double fixedLeft = _initialX;
        double fixedRight = _initialX + _initialW;
        double fixedTop = _initialY;
        double fixedBottom = _initialY + _initialH;

        double curLeft = altHeld ? (_initialCenter.X - w / 2.0) : (_initialCenter.X + shiftLocalX - w / 2.0);
        double curTop = altHeld ? (_initialCenter.Y - h / 2.0) : (_initialCenter.Y + shiftLocalY - h / 2.0);
        double curRight = curLeft + w;
        double curBottom = curTop + h;

        ItemViewModel? bestOtherX = null;
        double bestTargetX = 0;
        double bestDistX = double.MaxValue;

        ItemViewModel? bestOtherY = null;
        double bestTargetY = 0;
        double bestDistY = double.MaxValue;

        // X-axis alignment scan
        if (movesRight || movesLeft)
        {
            double candX = movesRight ? curRight : curLeft;

            foreach (var other in others)
            {
                if (other == _item) continue;
                if (Math.Abs(other.X - candX) > 600 && Math.Abs(other.Y - curTop) > 600) continue;

                double oLeft = other.X;
                double oCenter = other.X + other.Width / 2.0;
                double oRight = other.X + other.Width;

                double dLeft = Math.Abs(candX - oLeft);
                if (dLeft <= snapTolerance && dLeft < bestDistX) { bestDistX = dLeft; bestTargetX = oLeft; bestOtherX = other; }

                double dCenter = Math.Abs(candX - oCenter);
                if (dCenter <= snapTolerance && dCenter < bestDistX) { bestDistX = dCenter; bestTargetX = oCenter; bestOtherX = other; }

                double dRight = Math.Abs(candX - oRight);
                if (dRight <= snapTolerance && dRight < bestDistX) { bestDistX = dRight; bestTargetX = oRight; bestOtherX = other; }
            }

            if (bestOtherX != null)
            {
                if (movesRight)
                {
                    if (altHeld)
                    {
                        w = Math.Max(20.0, 2.0 * (bestTargetX - _initialCenter.X));
                        shiftLocalX = 0;
                    }
                    else
                    {
                        w = Math.Max(20.0, bestTargetX - fixedLeft);
                        shiftLocalX = (w - _initialW) / 2.0;
                    }
                }
                else // movesLeft
                {
                    if (altHeld)
                    {
                        w = Math.Max(20.0, 2.0 * (_initialCenter.X - bestTargetX));
                        shiftLocalX = 0;
                    }
                    else
                    {
                        w = Math.Max(20.0, fixedRight - bestTargetX);
                        shiftLocalX = -(w - _initialW) / 2.0;
                    }
                }

                snapXFound = true;
            }
        }

        // Y-axis alignment scan
        if (movesBottom || movesTop)
        {
            double candY = movesBottom ? curBottom : curTop;

            foreach (var other in others)
            {
                if (other == _item) continue;
                if (Math.Abs(other.Y - candY) > 600 && Math.Abs(other.X - curLeft) > 600) continue;

                double oTop = other.Y;
                double oMiddle = other.Y + other.Height / 2.0;
                double oBottom = other.Y + other.Height;

                double dTop = Math.Abs(candY - oTop);
                if (dTop <= snapTolerance && dTop < bestDistY) { bestDistY = dTop; bestTargetY = oTop; bestOtherY = other; }

                double dMiddle = Math.Abs(candY - oMiddle);
                if (dMiddle <= snapTolerance && dMiddle < bestDistY) { bestDistY = dMiddle; bestTargetY = oMiddle; bestOtherY = other; }

                double dBottom = Math.Abs(candY - oBottom);
                if (dBottom <= snapTolerance && dBottom < bestDistY) { bestDistY = dBottom; bestTargetY = oBottom; bestOtherY = other; }
            }

            if (bestOtherY != null)
            {
                if (movesBottom)
                {
                    if (altHeld)
                    {
                        h = Math.Max(20.0, 2.0 * (bestTargetY - _initialCenter.Y));
                        shiftLocalY = 0;
                    }
                    else
                    {
                        h = Math.Max(20.0, bestTargetY - fixedTop);
                        shiftLocalY = (h - _initialH) / 2.0;
                    }
                }
                else // movesTop
                {
                    if (altHeld)
                    {
                        h = Math.Max(20.0, 2.0 * (_initialCenter.Y - bestTargetY));
                        shiftLocalY = 0;
                    }
                    else
                    {
                        h = Math.Max(20.0, fixedBottom - bestTargetY);
                        shiftLocalY = -(h - _initialH) / 2.0;
                    }
                }

                snapYFound = true;
            }
        }

        // Proportional lock when Shift is held
        if (shiftHeld)
        {
            if (snapXFound && !snapYFound)
            {
                h = Math.Max(20.0, w / ratio);
                if (!altHeld)
                {
                    if (movesBottom) shiftLocalY = (h - _initialH) / 2.0;
                    else if (movesTop) shiftLocalY = -(h - _initialH) / 2.0;
                }
            }
            else if (snapYFound && !snapXFound)
            {
                w = Math.Max(20.0, h * ratio);
                if (!altHeld)
                {
                    if (movesRight) shiftLocalX = (w - _initialW) / 2.0;
                    else if (movesLeft) shiftLocalX = -(w - _initialW) / 2.0;
                }
            }
        }

        // Recalculate final edges after snapping for guide line extents
        curLeft = altHeld ? (_initialCenter.X - w / 2.0) : (_initialCenter.X + shiftLocalX - w / 2.0);
        curTop = altHeld ? (_initialCenter.Y - h / 2.0) : (_initialCenter.Y + shiftLocalY - h / 2.0);
        curRight = curLeft + w;
        curBottom = curTop + h;

        if (snapXFound && bestOtherX != null)
        {
            ShowVerticalGuideAction?.Invoke(
                bestTargetX,
                Math.Min(curTop, bestOtherX.Y),
                Math.Max(curBottom, bestOtherX.Y + bestOtherX.Height));
        }
        else
        {
            HideVerticalGuideAction?.Invoke();
        }

        if (snapYFound && bestOtherY != null)
        {
            ShowHorizontalGuideAction?.Invoke(
                bestTargetY,
                Math.Min(curLeft, bestOtherY.X),
                Math.Max(curRight, bestOtherY.X + bestOtherY.Width));
        }
        else
        {
            HideHorizontalGuideAction?.Invoke();
        }
    }

    private void ApplyLineEndpointAlignmentSnapping(ref Point mouseCanvas, Point fixedPoint)
    {
        IEnumerable<ItemViewModel>? others = GetOtherItems?.Invoke();
        if (others == null)
        {
            var canvas = FindCanvas();
            if (canvas is FrameworkElement fe && fe.DataContext is CanvasViewModel cvm)
            {
                others = cvm.Items;
            }
        }

        if (others == null)
        {
            HideVerticalGuideAction?.Invoke();
            HideHorizontalGuideAction?.Invoke();
            return;
        }

        const double snapTolerance = 6.0;
        bool snapXFound = false;
        bool snapYFound = false;

        double bestDistX = double.MaxValue;
        double bestTargetX = 0;
        ItemViewModel? bestOtherX = null;

        double bestDistY = double.MaxValue;
        double bestTargetY = 0;
        ItemViewModel? bestOtherY = null;

        foreach (var other in others)
        {
            if (other == _item) continue;
            if (Math.Abs(other.X - mouseCanvas.X) > 600 && Math.Abs(other.Y - mouseCanvas.Y) > 600) continue;

            double oLeft = other.X;
            double oCenter = other.X + other.Width / 2.0;
            double oRight = other.X + other.Width;

            double dLeft = Math.Abs(mouseCanvas.X - oLeft);
            if (dLeft <= snapTolerance && dLeft < bestDistX) { bestDistX = dLeft; bestTargetX = oLeft; bestOtherX = other; }

            double dCenter = Math.Abs(mouseCanvas.X - oCenter);
            if (dCenter <= snapTolerance && dCenter < bestDistX) { bestDistX = dCenter; bestTargetX = oCenter; bestOtherX = other; }

            double dRight = Math.Abs(mouseCanvas.X - oRight);
            if (dRight <= snapTolerance && dRight < bestDistX) { bestDistX = dRight; bestTargetX = oRight; bestOtherX = other; }

            double oTop = other.Y;
            double oMiddle = other.Y + other.Height / 2.0;
            double oBottom = other.Y + other.Height;

            double dTop = Math.Abs(mouseCanvas.Y - oTop);
            if (dTop <= snapTolerance && dTop < bestDistY) { bestDistY = dTop; bestTargetY = oTop; bestOtherY = other; }

            double dMiddle = Math.Abs(mouseCanvas.Y - oMiddle);
            if (dMiddle <= snapTolerance && dMiddle < bestDistY) { bestDistY = dMiddle; bestTargetY = oMiddle; bestOtherY = other; }

            double dBottom = Math.Abs(mouseCanvas.Y - oBottom);
            if (dBottom <= snapTolerance && dBottom < bestDistY) { bestDistY = dBottom; bestTargetY = oBottom; bestOtherY = other; }
        }

        if (bestOtherX != null)
        {
            mouseCanvas = new Point(bestTargetX, mouseCanvas.Y);
            ShowVerticalGuideAction?.Invoke(
                bestTargetX,
                Math.Min(fixedPoint.Y, bestOtherX.Y),
                Math.Max(fixedPoint.Y, bestOtherX.Y + bestOtherX.Height));
            snapXFound = true;
        }

        if (bestOtherY != null)
        {
            mouseCanvas = new Point(mouseCanvas.X, bestTargetY);
            ShowHorizontalGuideAction?.Invoke(
                bestTargetY,
                Math.Min(fixedPoint.X, bestOtherY.X),
                Math.Max(fixedPoint.X, bestOtherY.X + bestOtherY.Width));
            snapYFound = true;
        }

        if (!snapXFound) HideVerticalGuideAction?.Invoke();
        if (!snapYFound) HideHorizontalGuideAction?.Invoke();
    }

}