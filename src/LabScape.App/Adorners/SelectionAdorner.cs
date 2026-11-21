using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using LabScape.App.Helpers;
using LabScape.Core.Enums;
using LabScape.Core.Interfaces;
using LabScape.Core.Models;
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

    // draw.io-style midpoint handle between consecutive points on Bezier curve
    private static readonly Brush MidpointHandleBrush = new SolidColorBrush(Color.FromArgb(200, 14, 165, 233));
    private static readonly Pen MidpointHandlePen = new(Brushes.White, 1.2);
    private static readonly Pen MidpointPlusPen = new(Brushes.White, 1.0);

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
        BezierControlPoint
    }

    private DragMode _activeDrag = DragMode.None;
    private int _activeBezierControlPointIndex = -1;
    private List<ControlPoint> _initialControlPoints = new();
    private Point _dragStartPoint;
    private Point _dragStartLocal;
    private Point _dragStartCanvas;
    private Point _initialCenter;
    private double _initialAngleOffset;
    private double _initialX, _initialY, _initialW, _initialH, _initialRot;
    private double _initialCornerRadius, _initialInnerRadius;
    private double _initialStartX, _initialStartY, _initialEndX, _initialEndY;
    private Point _initialCanvasStart, _initialCanvasEnd;

    static SelectionAdorner()
    {
        BoxPen.Freeze();
        HandlePen.Freeze();
        DottedTangentPen.Freeze();
        ControlPointBrush.Freeze();
        ControlPointPen.Freeze();
        MidpointHandleBrush.Freeze();
        MidpointHandlePen.Freeze();
        MidpointPlusPen.Freeze();
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
        HideSizeGuidesAction?.Invoke();
    }

    public Action<double, double, double>? ShowVerticalGuideAction { get; set; }
    public Action<double, double, double>? ShowHorizontalGuideAction { get; set; }
    public Action? HideVerticalGuideAction { get; set; }
    public Action? HideHorizontalGuideAction { get; set; }
    public Action<Rect, Rect, double>? ShowEqualWidthGuideAction { get; set; }
    public Action<Rect, Rect, double>? ShowEqualHeightGuideAction { get; set; }
    public Action? HideEqualWidthGuideAction { get; set; }
    public Action? HideEqualHeightGuideAction { get; set; }
    public Action? HideSizeGuidesAction { get; set; }
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

        // 1. Line Shape: Exactly 2 handles (Start, End) - no rotation handle, no outer bounding box
        if (_item is ShapeItemViewModel shapeLine && shapeLine.ShapeType == ShapeType.Line)
        {
            Point pStart = new(0, h / 2.0);
            Point pEnd = new(w, h / 2.0);

            // Selection guide line
            dc.DrawLine(BoxPen, pStart, pEnd);

            if (!ShowHandles) return;

            // 1 & 2: Start and End handles
            DrawHandle(dc, pStart, hs);
            DrawHandle(dc, pEnd, hs);
            return;
        }

        // 2. Bezier Curve Shape: draw.io-style multi-control-point editing (Start, End, all Control Points, Midpoints, NO rotation handle)
        if (_item is ShapeItemViewModel shapeBezier && shapeBezier.ShapeType == ShapeType.Bezier)
        {
            var cpList = shapeBezier.GetControlPoints();
            Point pStart = new(shapeBezier.StartX, shapeBezier.StartY);
            Point pEnd = new(shapeBezier.EndX, shapeBezier.EndY);

            if (!ShowHandles)
            {
                dc.DrawLine(BoxPen, pStart, pEnd);
                return;
            }

            var nodes = new List<Point>(cpList.Count + 2) { pStart };
            foreach (var cp in cpList)
            {
                nodes.Add(new Point(cp.X, cp.Y));
            }
            nodes.Add(pEnd);

            // Dotted tangent lines connecting consecutive points on control polygon & Midpoint '+' handles
            for (int i = 0; i < nodes.Count - 1; i++)
            {
                dc.DrawLine(DottedTangentPen, nodes[i], nodes[i + 1]);
                Point mid = new((nodes[i].X + nodes[i + 1].X) / 2.0, (nodes[i].Y + nodes[i + 1].Y) / 2.0);
                DrawMidpointHandle(dc, mid, hs * 0.85);
            }

            // Start and End anchor handles
            DrawHandle(dc, pStart, hs);
            DrawHandle(dc, pEnd, hs);

            // Circular control point handles
            foreach (var cp in cpList)
            {
                dc.DrawEllipse(ControlPointBrush, ControlPointPen, new Point(cp.X, cp.Y), shs / 2, shs / 2);
            }
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

    private static void DrawMidpointHandle(DrawingContext dc, Point center, double size)
    {
        double half = size / 2.0;
        dc.DrawEllipse(MidpointHandleBrush, MidpointHandlePen, center, half, half);
        double arm = half * 0.55;
        dc.DrawLine(MidpointPlusPen, new Point(center.X - arm, center.Y), new Point(center.X + arm, center.Y));
        dc.DrawLine(MidpointPlusPen, new Point(center.X, center.Y - arm), new Point(center.X, center.Y + arm));
    }

    protected override void OnMouseDown(MouseButtonEventArgs e)
    {
        base.OnMouseDown(e);
        if (!ShowHandles) return;
        if (_item is TextItemViewModel textItem && textItem.IsEditing) return;
        if (e.LeftButton != MouseButtonState.Pressed && e.RightButton != MouseButtonState.Pressed) return;

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

        // Line-specific handle hit testing (Only Start and End handles, no rotation)
        if (_item is ShapeItemViewModel shapeLine && shapeLine.ShapeType == ShapeType.Line)
        {
            Point pStart = new(0, h / 2.0);
            Point pEnd = new(w, h / 2.0);

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

        // Bezier-specific handle hit testing (Endpoints, multi-control-points, and midpoints, no rotation)
        if (_item is ShapeItemViewModel shapeBezier && shapeBezier.ShapeType == ShapeType.Bezier)
        {
            var cpList = shapeBezier.GetControlPoints();
            Point pStart = new(shapeBezier.StartX, shapeBezier.StartY);
            Point pEnd = new(shapeBezier.EndX, shapeBezier.EndY);

            _initialStartX = shapeBezier.StartX;
            _initialStartY = shapeBezier.StartY;
            _initialEndX = shapeBezier.EndX;
            _initialEndY = shapeBezier.EndY;
            _initialControlPoints = cpList.ToList();

            // 1. Check existing control points
            for (int i = 0; i < cpList.Count; i++)
            {
                Point cp = new(cpList[i].X, cpList[i].Y);
                if (IsHit(p, cp, specialHitRadius))
                {
                    if (e.ChangedButton == MouseButton.Right)
                    {
                        // Right-click removes this control point if more than 1 point remains
                        if (cpList.Count > 1)
                        {
                            var oldPoints = cpList.ToList();
                            shapeBezier.RemoveControlPoint(i);
                            var newPoints = shapeBezier.GetControlPoints().ToList();
                            UpdateBezierDimensions(shapeBezier);
                            _undoRedoService.ExecuteCommand(new ModifyBezierControlPointsCommand(
                                shapeBezier,
                                shapeBezier.StartX, shapeBezier.StartY, shapeBezier.EndX, shapeBezier.EndY,
                                oldPoints,
                                shapeBezier.StartX, shapeBezier.StartY, shapeBezier.EndX, shapeBezier.EndY,
                                newPoints));
                            InvalidateAdorner();
                            _onModified?.Invoke();
                            e.Handled = true;
                            return;
                        }
                    }
                    else if (e.ChangedButton == MouseButton.Left)
                    {
                        _activeDrag = DragMode.BezierControlPoint;
                        _activeBezierControlPointIndex = i;
                        CaptureMouse();
                        e.Handled = true;
                        return;
                    }
                }
            }

            // 2. Check midpoint handles for adding a new control point
            var nodes = new List<Point>(cpList.Count + 2) { pStart };
            foreach (var cp in cpList) nodes.Add(new Point(cp.X, cp.Y));
            nodes.Add(pEnd);

            if (e.ChangedButton == MouseButton.Left)
            {
                for (int i = 0; i < nodes.Count - 1; i++)
                {
                    Point mid = new((nodes[i].X + nodes[i + 1].X) / 2.0, (nodes[i].Y + nodes[i + 1].Y) / 2.0);
                    if (IsHit(p, mid, specialHitRadius))
                    {
                        shapeBezier.AddControlPoint(Math.Round(mid.X, 1), Math.Round(mid.Y, 1), i);
                        UpdateBezierDimensions(shapeBezier);
                        _activeDrag = DragMode.BezierControlPoint;
                        _activeBezierControlPointIndex = i;
                        CaptureMouse();
                        e.Handled = true;
                        return;
                    }
                }
            }

            // 3. Check Start and End anchor handles
            if (IsHit(p, pStart, hitRadius))
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
            Cursor = CursorHelper.GetRotationCursor(_item.Rotation);
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

            case DragMode.BezierControlPoint:
                if (_item is ShapeItemViewModel shapeCp && _activeBezierControlPointIndex >= 0)
                {
                    shapeCp.SetControlPoint(_activeBezierControlPointIndex, Math.Round(currentLocal.X, 1), Math.Round(currentLocal.Y, 1));
                    UpdateBezierDimensions(shapeCp);
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
                    Cursor = CursorHelper.GetRotationCursor(_item.Rotation);
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
            HideSizeGuidesAction?.Invoke();
        }

        InvalidateAdorner();
        _onModified?.Invoke();
    }

    protected override void OnMouseUp(MouseButtonEventArgs e)
    {
        base.OnMouseUp(e);
        HideVerticalGuideAction?.Invoke();
        HideHorizontalGuideAction?.Invoke();
        HideSizeGuidesAction?.Invoke();
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
            else if (_activeDrag is DragMode.BezierStart or DragMode.BezierEnd or DragMode.BezierControlPoint)
            {
                if (_item is ShapeItemViewModel shape)
                {
                    var currentPoints = shape.GetControlPoints().ToList();
                    bool pointsChanged = _initialControlPoints.Count != currentPoints.Count;
                    if (!pointsChanged)
                    {
                        for (int i = 0; i < currentPoints.Count; i++)
                        {
                            if (Math.Abs(currentPoints[i].X - _initialControlPoints[i].X) > 0.1 ||
                                Math.Abs(currentPoints[i].Y - _initialControlPoints[i].Y) > 0.1)
                            {
                                pointsChanged = true;
                                break;
                            }
                        }
                    }

                    bool changed = pointsChanged ||
                                  Math.Abs(shape.StartX - _initialStartX) > 0.1 ||
                                  Math.Abs(shape.StartY - _initialStartY) > 0.1 ||
                                  Math.Abs(shape.EndX - _initialEndX) > 0.1 ||
                                  Math.Abs(shape.EndY - _initialEndY) > 0.1;

                    if (changed)
                    {
                        _undoRedoService.ExecuteCommand(new ModifyBezierControlPointsCommand(
                            shape,
                            _initialStartX, _initialStartY, _initialEndX, _initialEndY,
                            _initialControlPoints,
                            shape.StartX, shape.StartY, shape.EndX, shape.EndY,
                            currentPoints));
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

            if (IsHit(p, pStart, hitRadius) || IsHit(p, pEnd, hitRadius))
                Cursor = Cursors.Cross;
            else
                Cursor = Cursors.Arrow;
            return;
        }

        if (_item is ShapeItemViewModel shapeBezier && shapeBezier.ShapeType == ShapeType.Bezier)
        {
            var cpList = shapeBezier.GetControlPoints();
            Point pStart = new(shapeBezier.StartX, shapeBezier.StartY);
            Point pEnd = new(shapeBezier.EndX, shapeBezier.EndY);

            if (IsHit(p, pStart, hitRadius) || IsHit(p, pEnd, hitRadius))
            {
                Cursor = Cursors.Cross;
                return;
            }

            foreach (var cp in cpList)
            {
                if (IsHit(p, new Point(cp.X, cp.Y), specialHitRadius))
                {
                    Cursor = Cursors.Hand;
                    return;
                }
            }

            var nodes = new List<Point>(cpList.Count + 2) { pStart };
            foreach (var cp in cpList) nodes.Add(new Point(cp.X, cp.Y));
            nodes.Add(pEnd);

            for (int i = 0; i < nodes.Count - 1; i++)
            {
                Point mid = new((nodes[i].X + nodes[i + 1].X) / 2.0, (nodes[i].Y + nodes[i + 1].Y) / 2.0);
                if (IsHit(p, mid, specialHitRadius))
                {
                    Cursor = Cursors.Hand;
                    return;
                }
            }

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
            Cursor = CursorHelper.GetRotationCursor(_item.Rotation);
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

        // Line item has only 2 handles (Start, End) - no rotation handle
        if (_item is ShapeItemViewModel shapeLine && shapeLine.ShapeType == ShapeType.Line)
        {
            Point pStart = new(0, h / 2.0);
            Point pEnd = new(w, h / 2.0);

            if (IsHit(p, pStart, hitRadius) ||
                IsHit(p, pEnd, hitRadius))
            {
                return new PointHitTestResult(this, p);
            }
            return null;
        }

        // Bezier curve has endpoints, all control points, and midpoints - no rotation handle
        if (_item is ShapeItemViewModel shapeBezier && shapeBezier.ShapeType == ShapeType.Bezier)
        {
            var cpList = shapeBezier.GetControlPoints();
            Point pStart = new(shapeBezier.StartX, shapeBezier.StartY);
            Point pEnd = new(shapeBezier.EndX, shapeBezier.EndY);

            if (IsHit(p, pStart, hitRadius) || IsHit(p, pEnd, hitRadius))
            {
                return new PointHitTestResult(this, p);
            }

            foreach (var cp in cpList)
            {
                if (IsHit(p, new Point(cp.X, cp.Y), specialHitRadius))
                {
                    return new PointHitTestResult(this, p);
                }
            }

            var nodes = new List<Point>(cpList.Count + 2) { pStart };
            foreach (var cp in cpList) nodes.Add(new Point(cp.X, cp.Y));
            nodes.Add(pEnd);

            for (int i = 0; i < nodes.Count - 1; i++)
            {
                Point mid = new((nodes[i].X + nodes[i + 1].X) / 2.0, (nodes[i].Y + nodes[i + 1].Y) / 2.0);
                if (IsHit(p, mid, specialHitRadius))
                {
                    return new PointHitTestResult(this, p);
                }
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
        double minX = Math.Min(shape.StartX, shape.EndX);
        double maxX = Math.Max(shape.StartX, shape.EndX);
        double minY = Math.Min(shape.StartY, shape.EndY);
        double maxY = Math.Max(shape.StartY, shape.EndY);

        foreach (var cp in shape.GetControlPoints())
        {
            if (cp.X < minX) minX = cp.X;
            if (cp.X > maxX) maxX = cp.X;
            if (cp.Y < minY) minY = cp.Y;
            if (cp.Y > maxY) maxY = cp.Y;
        }

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
            HideSizeGuidesAction?.Invoke();
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
            HideSizeGuidesAction?.Invoke();
            return;
        }

        const double snapTolerance = 6.0;
        bool snapXFound = false;
        bool snapYFound = false;
        bool snapSizeWFound = false;
        bool snapSizeHFound = false;

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

        ItemViewModel? bestOtherSizeW = null;
        Rect bestOtherSizeWRect = Rect.Empty;
        double bestSizeTargetW = 0;
        double bestDistSizeW = double.MaxValue;

        ItemViewModel? bestOtherSizeH = null;
        Rect bestOtherSizeHRect = Rect.Empty;
        double bestSizeTargetH = 0;
        double bestDistSizeH = double.MaxValue;

        foreach (var other in others)
        {
            if (other == _item) continue;

            double oLeft = other.X;
            double oRight = other.X + other.Width;
            double oTop = other.Y;
            double oBottom = other.Y + other.Height;
            double oW = other.Width;
            double oH = other.Height;

            if (other is GroupItemViewModel grp && (grp.Width <= 10 || grp.Height <= 10))
            {
                grp.RecalculateBounds();
                oLeft = grp.X; oRight = grp.X + grp.Width;
                oTop = grp.Y; oBottom = grp.Y + grp.Height;
                oW = grp.Width; oH = grp.Height;
            }

            if (Math.Abs(other.Rotation) > 0.001)
            {
                var (bMinX, bMinY, bMaxX, bMaxY) = GroupItemViewModel.GetVisualBounds(other);
                oLeft = bMinX; oTop = bMinY; oRight = bMaxX; oBottom = bMaxY;
                oW = bMaxX - bMinX; oH = bMaxY - bMinY;
            }

            // X-axis alignment & size scan
            if (movesRight || movesLeft)
            {
                double candX = movesRight ? curRight : curLeft;
                double boxDistX = Math.Max(0.0, Math.Max(candX - oRight, oLeft - candX));
                double boxDistY = Math.Max(0.0, Math.Max(curTop - oBottom, oTop - curBottom));

                if (boxDistX <= 1200 || boxDistY <= 1200)
                {
                    double oCenter = (oLeft + oRight) / 2.0;

                    double dLeft = Math.Abs(candX - oLeft);
                    if (dLeft <= snapTolerance && dLeft < bestDistX) { bestDistX = dLeft; bestTargetX = oLeft; bestOtherX = other; }

                    double dCenter = Math.Abs(candX - oCenter);
                    if (dCenter <= snapTolerance && dCenter < bestDistX) { bestDistX = dCenter; bestTargetX = oCenter; bestOtherX = other; }

                    double dRight = Math.Abs(candX - oRight);
                    if (dRight <= snapTolerance && dRight < bestDistX) { bestDistX = dRight; bestTargetX = oRight; bestOtherX = other; }

                    // Equal Width check
                    double dSizeW = Math.Abs(w - oW);
                    if (dSizeW <= snapTolerance && dSizeW < bestDistSizeW)
                    {
                        bestDistSizeW = dSizeW;
                        bestSizeTargetW = oW;
                        bestOtherSizeW = other;
                        bestOtherSizeWRect = new Rect(oLeft, oTop, oW, oH);
                    }
                }
            }

            // Y-axis alignment & size scan
            if (movesBottom || movesTop)
            {
                double candY = movesBottom ? curBottom : curTop;
                double boxDistX = Math.Max(0.0, Math.Max(curLeft - oRight, oLeft - curRight));
                double boxDistY = Math.Max(0.0, Math.Max(candY - oBottom, oTop - candY));

                if (boxDistX <= 1200 || boxDistY <= 1200)
                {
                    double oMiddle = (oTop + oBottom) / 2.0;

                    double dTop = Math.Abs(candY - oTop);
                    if (dTop <= snapTolerance && dTop < bestDistY) { bestDistY = dTop; bestTargetY = oTop; bestOtherY = other; }

                    double dMiddle = Math.Abs(candY - oMiddle);
                    if (dMiddle <= snapTolerance && dMiddle < bestDistY) { bestDistY = dMiddle; bestTargetY = oMiddle; bestOtherY = other; }

                    double dBottom = Math.Abs(candY - oBottom);
                    if (dBottom <= snapTolerance && dBottom < bestDistY) { bestDistY = dBottom; bestTargetY = oBottom; bestOtherY = other; }

                    // Equal Height check
                    double dSizeH = Math.Abs(h - oH);
                    if (dSizeH <= snapTolerance && dSizeH < bestDistSizeH)
                    {
                        bestDistSizeH = dSizeH;
                        bestSizeTargetH = oH;
                        bestOtherSizeH = other;
                        bestOtherSizeHRect = new Rect(oLeft, oTop, oW, oH);
                    }
                }
            }
        }

        // Apply X snapping (edge alignment has priority if closer, otherwise equal size)
        if (movesRight || movesLeft)
        {
            if (bestDistX <= snapTolerance && bestDistX <= bestDistSizeW && bestOtherX != null)
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
            else if (bestDistSizeW <= snapTolerance && bestOtherSizeW != null)
            {
                w = Math.Max(20.0, bestSizeTargetW);
                if (movesRight)
                {
                    shiftLocalX = altHeld ? 0 : (w - _initialW) / 2.0;
                }
                else // movesLeft
                {
                    shiftLocalX = altHeld ? 0 : -(w - _initialW) / 2.0;
                }
                snapSizeWFound = true;
            }
        }

        // Apply Y snapping (edge alignment has priority if closer, otherwise equal size)
        if (movesBottom || movesTop)
        {
            if (bestDistY <= snapTolerance && bestDistY <= bestDistSizeH && bestOtherY != null)
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
            else if (bestDistSizeH <= snapTolerance && bestOtherSizeH != null)
            {
                h = Math.Max(20.0, bestSizeTargetH);
                if (movesBottom)
                {
                    shiftLocalY = altHeld ? 0 : (h - _initialH) / 2.0;
                }
                else // movesTop
                {
                    shiftLocalY = altHeld ? 0 : -(h - _initialH) / 2.0;
                }
                snapSizeHFound = true;
            }
        }

        // Proportional lock when Shift is held
        if (shiftHeld)
        {
            if ((snapXFound || snapSizeWFound) && !snapYFound && !snapSizeHFound)
            {
                h = Math.Max(20.0, w / ratio);
                if (!altHeld)
                {
                    if (movesBottom) shiftLocalY = (h - _initialH) / 2.0;
                    else if (movesTop) shiftLocalY = -(h - _initialH) / 2.0;
                }
            }
            else if ((snapYFound || snapSizeHFound) && !snapXFound && !snapSizeWFound)
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
        Rect currentResizingRect = new Rect(curLeft, curTop, w, h);

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

        if (snapSizeWFound && bestOtherSizeW != null)
        {
            ShowEqualWidthGuideAction?.Invoke(currentResizingRect, bestOtherSizeWRect, w);
        }
        else
        {
            HideEqualWidthGuideAction?.Invoke();
        }

        if (snapSizeHFound && bestOtherSizeH != null)
        {
            ShowEqualHeightGuideAction?.Invoke(currentResizingRect, bestOtherSizeHRect, h);
        }
        else
        {
            HideEqualHeightGuideAction?.Invoke();
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