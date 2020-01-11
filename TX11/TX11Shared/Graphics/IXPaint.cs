using System;

namespace TX11Shared.Graphics
{
    public interface IXPaint : IDisposable
    {
        XPaintStyle Style { set; }

        XStrokeCap StrokeCap { set; }

        XStrokeJoin StrokeJoin { set; }

        int TextSize { get; set; }

        int Color { get; set; }

        int StrokeWidth { set; }

        XPixelXferMode XferMode { set; }

        XTypeface Typeface { get; set; }

        float[] GetTextWidths(string s);

        XFontMetrics GetFontMetrics();

        float MeasureText(string s);

        void GetTextBounds(string s, int i, int i1, Rect bounds);

        void Reset();
    }
}