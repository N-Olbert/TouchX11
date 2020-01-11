using System;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.CompilerServices;
using SkiaSharp;
using TX11Shared;
using TX11Shared.Graphics;

namespace TX11Frontend.UIConnectorWrapper
{
    public static class UIConnectorExtensions
    {
        public static SKRect ToSKRect(this Rectangle r)
        {
            return SKRect.Create(r.X, r.Y, r.Width, r.Height);
        }

        public static SKRectI ToSkRectI(this Rect r)
        {
            return SKRectI.Create(r.X, r.Y, r.Width(), r.Height());
        }

        public static Rect ToRect(this SKRect r)
        {
            return new Rect((int) r.Left, (int) r.Top, (int) r.Right, (int) r.Bottom);
        }

        public static Rect ToRect(this SKRectI r)
        {
            return new Rect(r.Left, r.Top, r.Right, r.Bottom);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static SKColor ToSkColor(this uint value)
        {
            var a = (byte) (value >> 24);
            var r = (byte) (value >> 16);
            var g = (byte) (value >> 8);
            var b = (byte) (value);
            return new SKColor(r, g, b, a);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ToAsIntColor(this SKColor value)
        {
            return (value.Alpha << 24 | value.Red << 16 | value.Green << 8 | value.Blue);
        }

        public static SKColor ToSkColor(this int value)
        {
            return ToSkColor((uint) value);
        }

        public static SKRegionOperation ToSKRegionOp(this XRegionOperation op)
        {
            switch (op)
            {
                case XRegionOperation.Difference:
                    return SKRegionOperation.Difference;
                case XRegionOperation.Intersect:
                    return SKRegionOperation.Intersect;
                case XRegionOperation.Union:
                    return SKRegionOperation.Union;
                case XRegionOperation.Xor:
                    return SKRegionOperation.XOR;
                case XRegionOperation.ReverseDifference:
                    return SKRegionOperation.ReverseDifference;
                case XRegionOperation.Replace:
                    return SKRegionOperation.Replace;
                default:
                    throw new ArgumentOutOfRangeException(nameof(op), op, null);
            }
        }

        [Conditional("DEBUG")]
        public static void SaveToDisk(this SKBitmap bitmap)
        {
            //using (var image = SKImage.FromBitmap(bitmap))
            //{
            //    using (var data = image.Encode(SKEncodedImageFormat.Png, 80))
            //    {
            //        var s = $"C:\\GitHub\\Pics\\{DateTime.UtcNow.Ticks}.png";
            //        using (var stream = File.Create(s))
            //        {
            //            data.SaveTo(stream);
            //        }
            //    }
            //}
        }
    }
}