using System.Windows;
using System.Windows.Media;
using LabScape.Core.Enums;
using LabScape.ViewModels.Items;

namespace LabScape.App.Helpers;

public static class ShapeGeometryHelper
{
    public static Geometry CreateGeometry(ShapeItemViewModel shape)
    {
        double w = Math.Max(1, shape.Width);
        double h = Math.Max(1, shape.Height);

        switch (shape.ShapeType)
        {
            case ShapeType.Circle:
                return new EllipseGeometry(new Point(w / 2.0, h / 2.0), w / 2.0, h / 2.0);

            case ShapeType.Rectangle:
                return new RectangleGeometry(new Rect(0, 0, w, h));

            case ShapeType.RoundedRectangle:
            {
                double cr = Math.Min(shape.CornerRadius, Math.Min(w, h) / 2.0);
                return new RectangleGeometry(new Rect(0, 0, w, h), cr, cr);
            }

            case ShapeType.SemiCircle:
            {
                var fig = new PathFigure { StartPoint = new Point(0, h), IsClosed = true, IsFilled = true };
                fig.Segments.Add(new ArcSegment(new Point(w, h), new Size(w / 2.0, h), 0, false, SweepDirection.Clockwise, true));
                var geom = new PathGeometry();
                geom.Figures.Add(fig);
                return geom;
            }

            case ShapeType.Triangle:
            {
                var fig = new PathFigure { StartPoint = new Point(w / 2.0, 0), IsClosed = true, IsFilled = true };
                fig.Segments.Add(new LineSegment(new Point(w, h), true));
                fig.Segments.Add(new LineSegment(new Point(0, h), true));
                var geom = new PathGeometry();
                geom.Figures.Add(fig);
                return geom;
            }

            case ShapeType.RoundedCornerTriangle:
            {
                double r = Math.Min(shape.CornerRadius, Math.Min(w, h) / 4.0);
                var fig = new PathFigure { StartPoint = new Point(w / 2.0 - r, r * 1.5), IsClosed = true, IsFilled = true };
                fig.Segments.Add(new QuadraticBezierSegment(new Point(w / 2.0, 0), new Point(w / 2.0 + r, r * 1.5), true));
                fig.Segments.Add(new LineSegment(new Point(w - r, h - r), true));
                fig.Segments.Add(new QuadraticBezierSegment(new Point(w, h), new Point(w - r * 1.5, h), true));
                fig.Segments.Add(new LineSegment(new Point(r * 1.5, h), true));
                fig.Segments.Add(new QuadraticBezierSegment(new Point(0, h), new Point(r, h - r), true));
                var geom = new PathGeometry();
                geom.Figures.Add(fig);
                return geom;
            }

            case ShapeType.Donut:
            {
                double outerRx = w / 2.0;
                double outerRy = h / 2.0;
                double innerRatio = Math.Clamp(shape.InnerRadius / 50.0, 0.1, 0.9);
                double innerRx = outerRx * innerRatio;
                double innerRy = outerRy * innerRatio;

                var outer = new EllipseGeometry(new Point(outerRx, outerRy), outerRx, outerRy);
                var inner = new EllipseGeometry(new Point(outerRx, outerRy), innerRx, innerRy);

                return new CombinedGeometry(GeometryCombineMode.Exclude, outer, inner);
            }

            case ShapeType.Line:
                return new LineGeometry(new Point(0, h / 2.0), new Point(w, h / 2.0));

            case ShapeType.Bezier:
            {
                var fig = new PathFigure
                {
                    StartPoint = new Point(shape.StartX, shape.StartY),
                    IsClosed = false,
                    IsFilled = false
                };
                Point p1 = new(shape.ControlPoint1X, shape.ControlPoint1Y);
                Point p2 = new(shape.ControlPoint2X, shape.ControlPoint2Y);
                Point pEnd = new(shape.EndX, shape.EndY);
                fig.Segments.Add(new BezierSegment(p1, p2, pEnd, true));
                var geom = new PathGeometry();
                geom.Figures.Add(fig);
                return geom;
            }

            default:
                return new RectangleGeometry(new Rect(0, 0, w, h));
        }
    }
}
