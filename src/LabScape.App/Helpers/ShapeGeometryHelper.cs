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
                if (w < 1e-3 || h < 1e-3)
                {
                    return Geometry.Empty;
                }

                double cr = Math.Max(0.0, shape.CornerRadius);
                double lenSide = Math.Sqrt((w * w / 4.0) + (h * h));
                double lenBase = w;

                if (lenSide < 1e-3 || cr <= 0.5)
                {
                    var sharpFig = new PathFigure { StartPoint = new Point(w / 2.0, 0), IsClosed = true, IsFilled = true };
                    sharpFig.Segments.Add(new LineSegment(new Point(w, h), true));
                    sharpFig.Segments.Add(new LineSegment(new Point(0, h), true));
                    var sharpGeom = new PathGeometry();
                    sharpGeom.Figures.Add(sharpFig);
                    return sharpGeom;
                }

                // Tangent ratios from trigonometry:
                // Apex (P0): tan(alpha0) = (w/2) / h => k0 = 1 / tan0 = 2h / w
                double k0 = (2.0 * h) / w;

                // Base corners (P1, P2): cos(beta) = (w/2) / lenSide => k1 = (lenSide + w/2) / h
                double k1 = (lenSide + (w / 2.0)) / h;

                // Maximum tangent distance along edges to prevent overlap
                double t0Max = lenSide * 0.45;
                double t1Max = Math.Min(lenSide * 0.45, lenBase * 0.45);

                double t0 = Math.Min(cr * k0, t0Max);
                double t1 = Math.Min(cr * k1, t1Max);
                double t2 = t1;

                Point p0 = new(w / 2.0, 0.0);
                Point p1 = new(w, h);
                Point p2 = new(0.0, h);

                Vector u01 = new(w / (2.0 * lenSide), h / lenSide);
                Vector u12 = new(-1.0, 0.0);
                Vector u20 = new(w / (2.0 * lenSide), -h / lenSide);

                Point t0In = new(p0.X - (t0 * u20.X), p0.Y - (t0 * u20.Y));
                Point t0Out = new(p0.X + (t0 * u01.X), p0.Y + (t0 * u01.Y));

                Point t1In = new(p1.X - (t1 * u01.X), p1.Y - (t1 * u01.Y));
                Point t1Out = new(p1.X + (t1 * u12.X), p1.Y + (t1 * u12.Y));

                Point t2In = new(p2.X - (t2 * u12.X), p2.Y - (t2 * u12.Y));
                Point t2Out = new(p2.X + (t2 * u20.X), p2.Y + (t2 * u20.Y));

                var fig = new PathFigure { StartPoint = t0In, IsClosed = true, IsFilled = true };
                fig.Segments.Add(new QuadraticBezierSegment(p0, t0Out, true));
                fig.Segments.Add(new LineSegment(t1In, true));
                fig.Segments.Add(new QuadraticBezierSegment(p1, t1Out, true));
                fig.Segments.Add(new LineSegment(t2In, true));
                fig.Segments.Add(new QuadraticBezierSegment(p2, t2Out, true));
                fig.Segments.Add(new LineSegment(t0In, true));

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
