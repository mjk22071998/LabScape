using System.Collections.Concurrent;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace LabScape.App.Helpers;

/// <summary>
/// Provides vector-rendered, high-contrast custom cursors (such as rotation cursors)
/// adhering to graphic design standards with 32-bit alpha transparency and dark/light mode visibility.
/// </summary>
public static class CursorHelper
{
    // Standard SVG two-way circular rotation arrow (radius 8.5, centered at 16, 16)
    private const string RotationPathData = "M 8,16 A 8,8 0 0,1 23,10 M 23,4 L 23,11 L 16,11 M 24,16 A 8,8 0 0,1 9,22 M 9,28 L 9,21 L 16,21";

    private static readonly ConcurrentDictionary<int, Cursor> s_rotationCursorCache = new();
    private static Cursor? s_defaultRotationCursor;

    /// <summary>
    /// Gets the standard circular dual-arrow rotation cursor.
    /// </summary>
    public static Cursor RotationCursor => s_defaultRotationCursor ??= GetRotationCursor(0);

    /// <summary>
    /// Gets a rotation cursor oriented to the specified angle in degrees (quantized to 15-degree steps for optimal caching).
    /// </summary>
    public static Cursor GetRotationCursor(double angleDegrees = 0)
    {
        if (double.IsNaN(angleDegrees) || double.IsInfinity(angleDegrees))
        {
            angleDegrees = 0;
        }

        // Snap to nearest 15-degree step [0, 345] to bound cache to 24 instances
        int snapped = ((int)Math.Round(angleDegrees / 15.0) * 15) % 360;
        if (snapped < 0) snapped += 360;

        return s_rotationCursorCache.GetOrAdd(snapped, CreateRotationCursor);
    }

    private static Cursor CreateRotationCursor(int angleDegrees)
    {
        try
        {
            var dv = new DrawingVisual();
            using (var dc = dv.RenderOpen())
            {
                if (angleDegrees != 0)
                {
                    dc.PushTransform(new RotateTransform(angleDegrees, 16, 16));
                }

                // Dual-tone high-contrast styling: rich dark halo for light backgrounds + crisp white core for dark backgrounds
                var penBlack = new Pen(Brushes.Black, 3.4)
                {
                    StartLineCap = PenLineCap.Round,
                    EndLineCap = PenLineCap.Round,
                    LineJoin = PenLineJoin.Round
                };
                penBlack.Freeze();

                var penWhite = new Pen(Brushes.White, 1.8)
                {
                    StartLineCap = PenLineCap.Round,
                    EndLineCap = PenLineCap.Round,
                    LineJoin = PenLineJoin.Round
                };
                penWhite.Freeze();

                var geom = Geometry.Parse(RotationPathData);
                geom.Freeze();

                dc.DrawGeometry(null, penBlack, geom);
                dc.DrawGeometry(null, penWhite, geom);

                if (angleDegrees != 0)
                {
                    dc.Pop();
                }
            }

            var rtb = new RenderTargetBitmap(32, 32, 96, 96, PixelFormats.Pbgra32);
            rtb.Render(dv);

            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(rtb));
            byte[] pngBytes;
            using (var pngStream = new MemoryStream())
            {
                encoder.Save(pngStream);
                pngBytes = pngStream.ToArray();
            }

            using var curStream = new MemoryStream();
            using (var writer = new BinaryWriter(curStream, Encoding.UTF8, leaveOpen: true))
            {
                writer.Write((ushort)0); // idReserved
                writer.Write((ushort)2); // idType = 2 (Cursor)
                writer.Write((ushort)1); // idCount = 1 image
                writer.Write((byte)32);  // bWidth = 32
                writer.Write((byte)32);  // bHeight = 32
                writer.Write((byte)0);   // bColorCount = 0 (32-bit ARGB)
                writer.Write((byte)0);   // bReserved = 0
                writer.Write((ushort)16); // wXHotspot = 16 (centered)
                writer.Write((ushort)16); // wYHotspot = 16 (centered)
                writer.Write((uint)pngBytes.Length); // dwBytesInRes
                writer.Write((uint)22);  // dwImageOffset = 22 (6 header + 16 entry)
                writer.Write(pngBytes);
                writer.Flush();
            }

            curStream.Position = 0;
            return new Cursor(curStream);
        }
        catch
        {
            // Fallback in headless or restricted environments
            return Cursors.SizeAll;
        }
    }
}
