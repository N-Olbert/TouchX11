using System;
using SkiaSharp;
using TX11Shared.Graphics;

namespace TX11Frontend.UIConnectorWrapper
{
    public static class EnumConverter
    {
        public static SKPaintStyle ToSKEnum(this XPaintStyle s)
        {
            switch (s)
            {
                case XPaintStyle.Fill:
                    return SKPaintStyle.Fill;
                case XPaintStyle.Stroke:
                    return SKPaintStyle.Stroke;
                case XPaintStyle.StrokeAndFill:
                    return SKPaintStyle.StrokeAndFill;
                default:
                    throw new ArgumentOutOfRangeException(nameof(s), s, null);
            }
        }

        public static SKStrokeCap ToSKEnum(this XStrokeCap c)
        {
            switch (c)
            {
                case XStrokeCap.Butt:
                    return SKStrokeCap.Butt;
                case XStrokeCap.Round:
                    return SKStrokeCap.Round;
                case XStrokeCap.Square:
                    return SKStrokeCap.Square;
                default:
                    throw new ArgumentOutOfRangeException(nameof(c), c, null);
            }
        }

        public static SKStrokeJoin ToSKEnum(this XStrokeJoin j)
        {
            switch (j)
            {
                case XStrokeJoin.Bevel:
                    return SKStrokeJoin.Bevel;
                case XStrokeJoin.Round:
                    return SKStrokeJoin.Round;
                case XStrokeJoin.Miter:
                    return SKStrokeJoin.Miter;
                default:
                    throw new ArgumentOutOfRangeException(nameof(j), j, null);
            }
        }

        public static SKPathFillType ToSKEnum(this XPathFillType t)
        {
            switch (t)
            {
                case XPathFillType.Winding:
                    return SKPathFillType.Winding;
                case XPathFillType.EvenOdd:
                    return SKPathFillType.EvenOdd;
                default:
                    throw new ArgumentOutOfRangeException(nameof(t), t, null);
            }
        }

        public static XPaintStyle ToXEnum(this SKPaintStyle s)
        {
            switch (s)
            {
                case SKPaintStyle.Fill:
                    return XPaintStyle.Fill;
                case SKPaintStyle.Stroke:
                    return XPaintStyle.Stroke;
                case SKPaintStyle.StrokeAndFill:
                    return XPaintStyle.StrokeAndFill;
                default:
                    throw new ArgumentOutOfRangeException(nameof(s), s, null);
            }
        }

        public static XStrokeCap ToXEnum(this SKStrokeCap c)
        {
            switch (c)
            {
                case SKStrokeCap.Butt:
                    return XStrokeCap.Butt;
                case SKStrokeCap.Round:
                    return XStrokeCap.Round;
                case SKStrokeCap.Square:
                    return XStrokeCap.Square;
                default:
                    throw new ArgumentOutOfRangeException(nameof(c), c, null);
            }
        }
    }
}