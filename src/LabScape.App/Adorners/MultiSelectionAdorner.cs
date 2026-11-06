using System.ComponentModel;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using LabScape.Core.Interfaces;
using LabScape.ViewModels.Commands;
using LabScape.ViewModels.Items;

namespace LabScape.App.Adorners;

/// <summary>
/// Adorner that encloses multiple selected items in a unified bounding box with 8 resize handles
/// and 1 rotation handle, allowing multiple selection to act like a temporary group.
/// </summary>
public class MultiSelectionAdorner : Adorner
{
    private static readonly Pen BoxPen = new(new SolidColorBrush(Color.FromRgb(59, 130, 246)), 1.5) // Accent primary blue
    {
        DashStyle = new DashStyle(new double[] { 4, 2 }, 0)
    };
    private static readonly Pen HandlePen = new(new SolidColorBrush(Color.FromRgb(37, 99, 235)), 1.5);
    private static readonly Brush HandleBrush = Brushes.White;

    private const double BaseHandleSize = 8.0;
    private const double BaseRotStalkHeight = 24.0;
    private const double BaseOffset = 3.0;

    private enum DragMode
    {
        None,
        ResizeNW, ResizeN, ResizeNE, ResizeE, ResizeSE, ResizeS, ResizeSW, ResizeW,
        Rotate
    }

    private readonly List<ItemViewModel> _items;
    private readonly IUndoRedoService _undoRedoService;
    private readonly Func<double>? _getZoomLevel;

    private DragMode _activeDrag = DragMode.None;
    private Point _dragStartCanvas;
    private (double X, double Y, double Width, double Height) _initialBounds;
    private Point _initialCenter;
    private double _initialAngleOffset;
    private readonly Dictionary<ItemViewModel, (double X, double Y, double Width, double Height, double Rotation)> _initialItemStates = new();

    public Action<double, double, double>? ShowVerticalGuideAction { get; set; }
    public Action<double, double, double>? ShowHorizontalGuideAction { get; set; }
    public Action? HideVerticalGuideAction { get; set; }
    public Action? HideHorizontalGuideAction { get; set; }
    public Func<IEnumerable<ItemViewModel>>? GetOtherItems { get; set; }

    static MultiSelectionAdorner()
    {
        BoxPen.Freeze();
        HandlePen.Freeze();
    }

    public MultiSelectionAdorner(
        UIElement adornedElement,
        IEnumerable<ItemViewModel> items,
        IUndoRedoService undoRedoService,
        Func<double>? getZoomLevel = null)
        : base(adornedElement)
    {
        _items = items.ToList();
        _undoRedoService = undoRedoService;
        _getZoomLevel = getZoomLevel;

        IsHitTestVisible = true;
        ClipToBounds = false;

        foreach (var item in _items)
        {
            item.PropertyChanged += OnItemPropertyChanged;
        }

        Unloaded += (s, e) => Detach();
    }

    public void Detach()
    {
        foreach (var item in _items)
        {
            item.PropertyChanged -= OnItemPropertyChanged;
        }
        HideVerticalGuideAction?.Invoke();
        HideHorizontalGuideAction?.Invoke();
    }

    private void OnItemPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (_activeDrag == DragMode.None)
        {
            InvalidateVisual();
        }
    }

    public double GetCurrentZoom()
    {
        if (_getZoomLevel != null)
        {
            double z = _getZoomLevel();
            if (z > 0.001) return z;
        }
        return 1.0;
    }

    public (double X, double Y, double Width, double Height) CalculateUnifiedBounds()
    {
        if (_items.Count == 0) return (0, 0, 0, 0);

        double minX = double.MaxValue;
        double minY = double.MaxValue;
        double maxX = double.MinValue;
        double maxY = double.MinValue;

        foreach (var item in _items)
        {
            var (bMinX, bMinY, bMaxX, bMaxY) = GroupItemViewModel.GetVisualBounds(item);
            if (bMinX < minX) minX = bMinX;
            if (bMinY < minY) minY = bMinY;
            if (bMaxX > maxX) maxX = bMaxX;
            if (bMaxY > maxY) maxY = bMaxY;
        }

        return (minX, minY, Math.Max(10.0, maxX - minX), Math.Max(10.0, maxY - minY));
    }

    protected override void OnRender(DrawingContext dc)
    {
        base.OnRender(dc);

        if (_items.Count < 2) return;

        var (x, y, w, h) = CalculateUnifiedBounds();
        double zoom = GetCurrentZoom();

        double hs = BaseHandleSize / Math.Max(0.05, zoom);
        double rotOffset = BaseRotStalkHeight / Math.Max(0.05, zoom);
        double offset = BaseOffset / Math.Max(0.05, zoom);

        // 1. Unified outer bounding box
        dc.DrawRectangle(null, BoxPen, new Rect(x - offset, y - offset, w + offset * 2, h + offset * 2));

        // 2. Corner handles (NW, NE, SE, SW)
        DrawSquareHandle(dc, new Point(x - offset, y - offset), hs);
        DrawSquareHandle(dc, new Point(x + w + offset, y - offset), hs);
        DrawSquareHandle(dc, new Point(x + w + offset, y + h + offset), hs);
        DrawSquareHandle(dc, new Point(x - offset, y + h + offset), hs);

        // 3. Edge midpoint handles (N, E, S, W)
        DrawSquareHandle(dc, new Point(x + w / 2.0, y - offset), hs);
        DrawSquareHandle(dc, new Point(x + w + offset, y + h / 2.0), hs);
        DrawSquareHandle(dc, new Point(x + w / 2.0, y + h + offset), hs);
        DrawSquareHandle(dc, new Point(x - offset, y + h / 2.0), hs);

        // 4. Rotate stalk and circular handle
        Point rotPoint = new(x + w / 2.0, y - offset - rotOffset);
        dc.DrawLine(BoxPen, new Point(x + w / 2.0, y - offset), rotPoint);
        dc.DrawEllipse(HandleBrush, HandlePen, rotPoint, hs / 2.0, hs / 2.0);
    }

    private static void DrawSquareHandle(DrawingContext dc, Point center, double size)
    {
        Rect r = new(center.X - size / 2.0, center.Y - size / 2.0, size, size);
        dc.DrawRectangle(HandleBrush, HandlePen, r);
    }

    private static bool IsHit(Point test, Point center, double radius)
    {
        return (Math.Abs(test.X - center.X) <= radius) && (Math.Abs(test.Y - center.Y) <= radius);
    }

    protected override HitTestResult? HitTestCore(PointHitTestParameters hitTestParameters)
    {
        if (_items.Count < 2) return null;

        Point p = hitTestParameters.HitPoint;
        var (x, y, w, h) = CalculateUnifiedBounds();
        double zoom = GetCurrentZoom();

        double rotOffset = BaseRotStalkHeight / Math.Max(0.05, zoom);
        double offset = BaseOffset / Math.Max(0.05, zoom);
        double hitRadius = (BaseHandleSize / 2.0 + 3.0) / Math.Max(0.05, zoom);
        double rotHitRadius = (BaseHandleSize / 2.0 + 4.0) / Math.Max(0.05, zoom);

        // Rotate handle
        Point pRot = new(x + w / 2.0, y - offset - rotOffset);
        if (IsHit(p, pRot, rotHitRadius)) return new PointHitTestResult(this, p);

        // 4 corner handles
        if (IsHit(p, new Point(x - offset, y - offset), hitRadius) ||
            IsHit(p, new Point(x + w + offset, y - offset), hitRadius) ||
            IsHit(p, new Point(x + w + offset, y + h + offset), hitRadius) ||
            IsHit(p, new Point(x - offset, y + h + offset), hitRadius))
        {
            return new PointHitTestResult(this, p);
        }

        // 4 edge midpoint handles
        if (IsHit(p, new Point(x + w / 2.0, y - offset), hitRadius) ||
            IsHit(p, new Point(x + w + offset, y + h / 2.0), hitRadius) ||
            IsHit(p, new Point(x + w / 2.0, y + h + offset), hitRadius) ||
            IsHit(p, new Point(x - offset, y + h / 2.0), hitRadius))
        {
            return new PointHitTestResult(this, p);
        }

        // Non-handle hit: let event pass through to canvas items
        return null;
    }

    protected override void OnMouseDown(MouseButtonEventArgs e)
    {
        base.OnMouseDown(e);
        if (e.LeftButton != MouseButtonState.Pressed || _items.Count < 2) return;

        Point p = e.GetPosition(this);
        var (x, y, w, h) = CalculateUnifiedBounds();
        double zoom = GetCurrentZoom();

        double rotOffset = BaseRotStalkHeight / Math.Max(0.05, zoom);
        double offset = BaseOffset / Math.Max(0.05, zoom);
        double hitRadius = (BaseHandleSize / 2.0 + 3.0) / Math.Max(0.05, zoom);
        double rotHitRadius = (BaseHandleSize / 2.0 + 4.0) / Math.Max(0.05, zoom);

        _initialBounds = (x, y, w, h);
        _initialCenter = new Point(x + w / 2.0, y + h / 2.0);
        _dragStartCanvas = p;

        _initialItemStates.Clear();
        foreach (var item in _items)
        {
            _initialItemStates[item] = (item.X, item.Y, item.Width, item.Height, item.Rotation);
        }

        // 1. Rotate handle
        Point pRot = new(x + w / 2.0, y - offset - rotOffset);
        if (IsHit(p, pRot, rotHitRadius))
        {
            _activeDrag = DragMode.Rotate;
            double clickDx = p.X - _initialCenter.X;
            double clickDy = p.Y - _initialCenter.Y;
            double clickAngleRad = Math.Atan2(clickDy, clickDx);
            double clickAngleDeg = (clickAngleRad * 180.0 / Math.PI) + 90.0;
            if (clickAngleDeg < 0) clickAngleDeg += 360.0;
            _initialAngleOffset = clickAngleDeg;

            CaptureMouse();
            e.Handled = true;
            return;
        }

        // 2. Corner resize handles
        if (IsHit(p, new Point(x - offset, y - offset), hitRadius)) _activeDrag = DragMode.ResizeNW;
        else if (IsHit(p, new Point(x + w + offset, y - offset), hitRadius)) _activeDrag = DragMode.ResizeNE;
        else if (IsHit(p, new Point(x + w + offset, y + h + offset), hitRadius)) _activeDrag = DragMode.ResizeSE;
        else if (IsHit(p, new Point(x - offset, y + h + offset), hitRadius)) _activeDrag = DragMode.ResizeSW;
        // 3. Edge resize handles
        else if (IsHit(p, new Point(x + w / 2.0, y - offset), hitRadius)) _activeDrag = DragMode.ResizeN;
        else if (IsHit(p, new Point(x + w + offset, y + h / 2.0), hitRadius)) _activeDrag = DragMode.ResizeE;
        else if (IsHit(p, new Point(x + w / 2.0, y + h + offset), hitRadius)) _activeDrag = DragMode.ResizeS;
        else if (IsHit(p, new Point(x - offset, y + h / 2.0), hitRadius)) _activeDrag = DragMode.ResizeW;

        if (_activeDrag != DragMode.None)
        {
            CaptureMouse();
            e.Handled = true;
        }
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        Point currentCanvas = e.GetPosition(this);

        if (_activeDrag == DragMode.None)
        {
            UpdateCursor(currentCanvas);
            return;
        }

        bool shiftHeld = Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift);
        bool altHeld = Keyboard.IsKeyDown(Key.LeftAlt) || Keyboard.IsKeyDown(Key.RightAlt);

        double deltaX = currentCanvas.X - _dragStartCanvas.X;
        double deltaY = currentCanvas.Y - _dragStartCanvas.Y;

        double x0 = _initialBounds.X;
        double y0 = _initialBounds.Y;
        double w0 = _initialBounds.Width;
        double h0 = _initialBounds.Height;
        double ratio = h0 > 0 ? w0 / h0 : 1.0;

        if (_activeDrag == DragMode.Rotate)
        {
            double clickDx = currentCanvas.X - _initialCenter.X;
            double clickDy = currentCanvas.Y - _initialCenter.Y;
            double currentAngleRad = Math.Atan2(clickDy, clickDx);
            double currentAngleDeg = (currentAngleRad * 180.0 / Math.PI) + 90.0;
            if (currentAngleDeg < 0) currentAngleDeg += 360.0;

            double deltaAngle = currentAngleDeg - _initialAngleOffset;
            if (shiftHeld)
            {
                deltaAngle = Math.Round(deltaAngle / 15.0) * 15.0;
            }

            double rad = deltaAngle * Math.PI / 180.0;
            double cos = Math.Cos(rad);
            double sin = Math.Sin(rad);

            foreach (var item in _items)
            {
                if (!_initialItemStates.TryGetValue(item, out var init)) continue;

                double initCx = init.X + init.Width / 2.0;
                double initCy = init.Y + init.Height / 2.0;

                double itemDx = initCx - _initialCenter.X;
                double itemDy = initCy - _initialCenter.Y;

                double newCx = _initialCenter.X + itemDx * cos - itemDy * sin;
                double newCy = _initialCenter.Y + itemDx * sin + itemDy * cos;

                item.X = Math.Round(newCx - item.Width / 2.0, 1);
                item.Y = Math.Round(newCy - item.Height / 2.0, 1);

                double newRot = (init.Rotation + deltaAngle) % 360.0;
                if (newRot < 0) newRot += 360.0;
                item.Rotation = Math.Round(newRot, 1);
            }

            InvalidateVisual();
            return;
        }

        // Resize Modes
        double candX = x0;
        double candY = y0;
        double candW = w0;
        double candH = h0;

        switch (_activeDrag)
        {
            case DragMode.ResizeE:
                candW = Math.Max(20.0, altHeld ? w0 + 2 * deltaX : w0 + deltaX);
                candX = altHeld ? _initialCenter.X - candW / 2.0 : x0;
                if (shiftHeld)
                {
                    candH = Math.Max(20.0, candW / ratio);
                    candY = _initialCenter.Y - candH / 2.0;
                }
                break;

            case DragMode.ResizeW:
                candW = Math.Max(20.0, altHeld ? w0 - 2 * deltaX : w0 - deltaX);
                candX = altHeld ? _initialCenter.X - candW / 2.0 : x0 + w0 - candW;
                if (shiftHeld)
                {
                    candH = Math.Max(20.0, candW / ratio);
                    candY = _initialCenter.Y - candH / 2.0;
                }
                break;

            case DragMode.ResizeS:
                candH = Math.Max(20.0, altHeld ? h0 + 2 * deltaY : h0 + deltaY);
                candY = altHeld ? _initialCenter.Y - candH / 2.0 : y0;
                if (shiftHeld)
                {
                    candW = Math.Max(20.0, candH * ratio);
                    candX = _initialCenter.X - candW / 2.0;
                }
                break;

            case DragMode.ResizeN:
                candH = Math.Max(20.0, altHeld ? h0 - 2 * deltaY : h0 - deltaY);
                candY = altHeld ? _initialCenter.Y - candH / 2.0 : y0 + h0 - candH;
                if (shiftHeld)
                {
                    candW = Math.Max(20.0, candH * ratio);
                    candX = _initialCenter.X - candW / 2.0;
                }
                break;

            case DragMode.ResizeSE:
                candW = Math.Max(20.0, altHeld ? w0 + 2 * deltaX : w0 + deltaX);
                candH = Math.Max(20.0, altHeld ? h0 + 2 * deltaY : h0 + deltaY);
                if (shiftHeld)
                {
                    double s = Math.Max(candW / w0, candH / h0);
                    candW = Math.Max(20.0, w0 * s);
                    candH = Math.Max(20.0, h0 * s);
                }
                candX = altHeld ? _initialCenter.X - candW / 2.0 : x0;
                candY = altHeld ? _initialCenter.Y - candH / 2.0 : y0;
                break;

            case DragMode.ResizeNW:
                candW = Math.Max(20.0, altHeld ? w0 - 2 * deltaX : w0 - deltaX);
                candH = Math.Max(20.0, altHeld ? h0 - 2 * deltaY : h0 - deltaY);
                if (shiftHeld)
                {
                    double s = Math.Max(candW / w0, candH / h0);
                    candW = Math.Max(20.0, w0 * s);
                    candH = Math.Max(20.0, h0 * s);
                }
                candX = altHeld ? _initialCenter.X - candW / 2.0 : x0 + w0 - candW;
                candY = altHeld ? _initialCenter.Y - candH / 2.0 : y0 + h0 - candH;
                break;

            case DragMode.ResizeNE:
                candW = Math.Max(20.0, altHeld ? w0 + 2 * deltaX : w0 + deltaX);
                candH = Math.Max(20.0, altHeld ? h0 - 2 * deltaY : h0 - deltaY);
                if (shiftHeld)
                {
                    double s = Math.Max(candW / w0, candH / h0);
                    candW = Math.Max(20.0, w0 * s);
                    candH = Math.Max(20.0, h0 * s);
                }
                candX = altHeld ? _initialCenter.X - candW / 2.0 : x0;
                candY = altHeld ? _initialCenter.Y - candH / 2.0 : y0 + h0 - candH;
                break;

            case DragMode.ResizeSW:
                candW = Math.Max(20.0, altHeld ? w0 - 2 * deltaX : w0 - deltaX);
                candH = Math.Max(20.0, altHeld ? h0 + 2 * deltaY : h0 + deltaY);
                if (shiftHeld)
                {
                    double s = Math.Max(candW / w0, candH / h0);
                    candW = Math.Max(20.0, w0 * s);
                    candH = Math.Max(20.0, h0 * s);
                }
                candX = altHeld ? _initialCenter.X - candW / 2.0 : x0 + w0 - candW;
                candY = altHeld ? _initialCenter.Y - candH / 2.0 : y0;
                break;
        }

        // Apply alignment snapping against outside items
        ApplyResizeSnapping(ref candX, ref candY, ref candW, ref candH, altHeld);

        // Scale and reposition all items inside the temporary group
        double scaleX = w0 > 0 ? candW / w0 : 1.0;
        double scaleY = h0 > 0 ? candH / h0 : 1.0;

        foreach (var item in _items)
        {
            if (!_initialItemStates.TryGetValue(item, out var init)) continue;

            double newX = candX + (init.X - x0) * scaleX;
            double newY = candY + (init.Y - y0) * scaleY;
            double newW = Math.Max(5.0, init.Width * scaleX);
            double newH = Math.Max(5.0, init.Height * scaleY);

            item.X = Math.Round(newX, 1);
            item.Y = Math.Round(newY, 1);
            item.Width = Math.Round(newW, 1);
            item.Height = Math.Round(newH, 1);

            if (item is GroupItemViewModel grp)
            {
                grp.RecalculateBounds();
            }
        }

        InvalidateVisual();
    }

    private void ApplyResizeSnapping(ref double candX, ref double candY, ref double candW, ref double candH, bool altHeld)
    {
        IEnumerable<ItemViewModel>? others = GetOtherItems?.Invoke();
        if (others == null)
        {
            HideVerticalGuideAction?.Invoke();
            HideHorizontalGuideAction?.Invoke();
            return;
        }

        const double snapTolerance = 6.0;
        bool movesRight = _activeDrag is DragMode.ResizeE or DragMode.ResizeSE or DragMode.ResizeNE;
        bool movesLeft = _activeDrag is DragMode.ResizeW or DragMode.ResizeSW or DragMode.ResizeNW;
        bool movesBottom = _activeDrag is DragMode.ResizeS or DragMode.ResizeSE or DragMode.ResizeSW;
        bool movesTop = _activeDrag is DragMode.ResizeN or DragMode.ResizeNE or DragMode.ResizeNW;

        double curLeft = candX;
        double curRight = candX + candW;
        double curTop = candY;
        double curBottom = candY + candH;

        bool snapXFound = false;
        bool snapYFound = false;

        // X Snapping
        if (movesRight || movesLeft)
        {
            double movingX = movesRight ? curRight : curLeft;
            double bestDistX = double.MaxValue;
            double bestTargetX = 0;
            ItemViewModel? bestOtherX = null;

            foreach (var other in others)
            {
                if (_items.Contains(other)) continue;
                if (Math.Abs(other.X - movingX) > 600 && Math.Abs(other.Y - curTop) > 600) continue;

                double oLeft = other.X;
                double oCenter = other.X + other.Width / 2.0;
                double oRight = other.X + other.Width;

                double dLeft = Math.Abs(movingX - oLeft);
                if (dLeft <= snapTolerance && dLeft < bestDistX) { bestDistX = dLeft; bestTargetX = oLeft; bestOtherX = other; }

                double dCenter = Math.Abs(movingX - oCenter);
                if (dCenter <= snapTolerance && dCenter < bestDistX) { bestDistX = dCenter; bestTargetX = oCenter; bestOtherX = other; }

                double dRight = Math.Abs(movingX - oRight);
                if (dRight <= snapTolerance && dRight < bestDistX) { bestDistX = dRight; bestTargetX = oRight; bestOtherX = other; }
            }

            if (bestOtherX != null)
            {
                if (movesRight)
                {
                    if (altHeld)
                    {
                        candW = Math.Max(20.0, 2.0 * (bestTargetX - _initialCenter.X));
                        candX = _initialCenter.X - candW / 2.0;
                    }
                    else
                    {
                        candW = Math.Max(20.0, bestTargetX - _initialBounds.X);
                        candX = _initialBounds.X;
                    }
                }
                else // movesLeft
                {
                    double fixedRight = _initialBounds.X + _initialBounds.Width;
                    if (altHeld)
                    {
                        candW = Math.Max(20.0, 2.0 * (_initialCenter.X - bestTargetX));
                        candX = _initialCenter.X - candW / 2.0;
                    }
                    else
                    {
                        candW = Math.Max(20.0, fixedRight - bestTargetX);
                        candX = fixedRight - candW;
                    }
                }

                ShowVerticalGuideAction?.Invoke(
                    bestTargetX,
                    Math.Min(candY, bestOtherX.Y),
                    Math.Max(candY + candH, bestOtherX.Y + bestOtherX.Height));
                snapXFound = true;
            }
        }

        // Y Snapping
        if (movesBottom || movesTop)
        {
            double movingY = movesBottom ? curBottom : curTop;
            double bestDistY = double.MaxValue;
            double bestTargetY = 0;
            ItemViewModel? bestOtherY = null;

            foreach (var other in others)
            {
                if (_items.Contains(other)) continue;
                if (Math.Abs(other.Y - movingY) > 600 && Math.Abs(other.X - curLeft) > 600) continue;

                double oTop = other.Y;
                double oMiddle = other.Y + other.Height / 2.0;
                double oBottom = other.Y + other.Height;

                double dTop = Math.Abs(movingY - oTop);
                if (dTop <= snapTolerance && dTop < bestDistY) { bestDistY = dTop; bestTargetY = oTop; bestOtherY = other; }

                double dMiddle = Math.Abs(movingY - oMiddle);
                if (dMiddle <= snapTolerance && dMiddle < bestDistY) { bestDistY = dMiddle; bestTargetY = oMiddle; bestOtherY = other; }

                double dBottom = Math.Abs(movingY - oBottom);
                if (dBottom <= snapTolerance && dBottom < bestDistY) { bestDistY = dBottom; bestTargetY = oBottom; bestOtherY = other; }
            }

            if (bestOtherY != null)
            {
                if (movesBottom)
                {
                    if (altHeld)
                    {
                        candH = Math.Max(20.0, 2.0 * (bestTargetY - _initialCenter.Y));
                        candY = _initialCenter.Y - candH / 2.0;
                    }
                    else
                    {
                        candH = Math.Max(20.0, bestTargetY - _initialBounds.Y);
                        candY = _initialBounds.Y;
                    }
                }
                else // movesTop
                {
                    double fixedBottom = _initialBounds.Y + _initialBounds.Height;
                    if (altHeld)
                    {
                        candH = Math.Max(20.0, 2.0 * (_initialCenter.Y - bestTargetY));
                        candY = _initialCenter.Y - candH / 2.0;
                    }
                    else
                    {
                        candH = Math.Max(20.0, fixedBottom - bestTargetY);
                        candY = fixedBottom - candH;
                    }
                }

                ShowHorizontalGuideAction?.Invoke(
                    bestTargetY,
                    Math.Min(candX, bestOtherY.X),
                    Math.Max(candX + candW, bestOtherY.X + bestOtherY.Width));
                snapYFound = true;
            }
        }

        if (!snapXFound) HideVerticalGuideAction?.Invoke();
        if (!snapYFound) HideHorizontalGuideAction?.Invoke();
    }

    protected override void OnMouseUp(MouseButtonEventArgs e)
    {
        base.OnMouseUp(e);
        HideVerticalGuideAction?.Invoke();
        HideHorizontalGuideAction?.Invoke();

        if (_activeDrag != DragMode.None)
        {
            // Record Undoable Command if any items changed
            var records = new List<TransformMultipleItemsCommand.ItemTransformRecord>();
            foreach (var item in _items)
            {
                if (_initialItemStates.TryGetValue(item, out var init))
                {
                    bool changed = Math.Abs(item.X - init.X) > 0.1 ||
                                  Math.Abs(item.Y - init.Y) > 0.1 ||
                                  Math.Abs(item.Width - init.Width) > 0.1 ||
                                  Math.Abs(item.Height - init.Height) > 0.1 ||
                                  Math.Abs(item.Rotation - init.Rotation) > 0.1;
                    if (changed)
                    {
                        records.Add(new TransformMultipleItemsCommand.ItemTransformRecord(
                            item,
                            init.X, init.Y, init.Width, init.Height, init.Rotation,
                            item.X, item.Y, item.Width, item.Height, item.Rotation));
                    }
                }
            }

            if (records.Count > 0)
            {
                _undoRedoService.ExecuteCommand(new TransformMultipleItemsCommand(records));
            }

            _activeDrag = DragMode.None;
            ReleaseMouseCapture();
            InvalidateVisual();
        }
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

    private void UpdateCursor(Point p)
    {
        var (x, y, w, h) = CalculateUnifiedBounds();
        double zoom = GetCurrentZoom();

        double rotOffset = BaseRotStalkHeight / Math.Max(0.05, zoom);
        double offset = BaseOffset / Math.Max(0.05, zoom);
        double hitRadius = (BaseHandleSize / 2.0 + 3.0) / Math.Max(0.05, zoom);
        double rotHitRadius = (BaseHandleSize / 2.0 + 4.0) / Math.Max(0.05, zoom);

        Point pRot = new(x + w / 2.0, y - offset - rotOffset);
        if (IsHit(p, pRot, rotHitRadius)) { Cursor = Cursors.Hand; return; }

        if (IsHit(p, new Point(x - offset, y - offset), hitRadius) ||
            IsHit(p, new Point(x + w + offset, y + h + offset), hitRadius))
        {
            Cursor = Cursors.SizeNWSE;
            return;
        }

        if (IsHit(p, new Point(x + w + offset, y - offset), hitRadius) ||
            IsHit(p, new Point(x - offset, y + h + offset), hitRadius))
        {
            Cursor = Cursors.SizeNESW;
            return;
        }

        if (IsHit(p, new Point(x + w / 2.0, y - offset), hitRadius) ||
            IsHit(p, new Point(x + w / 2.0, y + h + offset), hitRadius))
        {
            Cursor = Cursors.SizeNS;
            return;
        }

        if (IsHit(p, new Point(x + w + offset, y + h / 2.0), hitRadius) ||
            IsHit(p, new Point(x - offset, y + h / 2.0), hitRadius))
        {
            Cursor = Cursors.SizeWE;
            return;
        }

        Cursor = Cursors.Arrow;
    }
}
