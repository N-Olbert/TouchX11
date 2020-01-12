using System;
using System.Diagnostics;
using System.Globalization;
using JetBrains.Annotations;
using SkiaSharp;
using TX11Shared;
using TX11Shared.Graphics;

namespace TX11Frontend.UIConnectorWrapper
{
    public sealed class XPaint : IXPaint
    {
        [NotNull]
        internal SKPaint Paint { get; } = new SKPaint();

        public XPaintStyle Style
        {
            set => Paint.Style = value.ToSKEnum();
        }

        public XStrokeCap StrokeCap
        {
            set => Paint.StrokeCap = value.ToSKEnum();
        }

        public XStrokeJoin StrokeJoin
        {
            set => Paint.StrokeJoin = value.ToSKEnum();
        }

        public int TextSize
        {
            get => (int) Paint.TextSize;
            set => Paint.TextSize = value;
        }

        public int Color
        {
            get { return Paint.Color.ToAsIntColor(); }
            set { Paint.Color = ((uint) value).ToSkColor(); }
        }

        public int StrokeWidth
        {
            set => Paint.StrokeWidth = value;
        }

        public XPixelXferMode XferMode { get; set; }

        public XTypeface Typeface { get; set; }

        ~XPaint()
        {
            Dispose(false);
        }

        public float[] GetTextWidths(string s)
        {
            if (s.Length > 256)
            {
                //Bug within SkiaSharp, see: https://github.com/mono/SkiaSharp/issues/1093
                var res = new float[s.Length];
                for (var index = 0; index < s.Length; index++)
                {
                    var c = s[index];
                    using (var newPaint = new SKPaint())
                    {
                        res[index] = newPaint.GetGlyphWidths(c.ToString(CultureInfo.InvariantCulture))[0];
                    }
                }

                return res;
            }

            return Paint.GetGlyphWidths(s);
        }

        public XFontMetrics GetFontMetrics()
        {
            var m = Paint.FontMetrics;
            return new XFontMetrics(m.Ascent, m.Descent, m.Bottom, m.Top);
        }

        public float MeasureText(string s)
        {
            return Paint.MeasureText(s);
        }

        public void GetTextBounds(string s, int start, int end, Rect bounds)
        {
            SKRect b = new SKRect();
            var portion = s.Substring(start, end - start);
            Paint.MeasureText(portion, ref b);
            bounds.Left = (int) b.Left;
            bounds.Top = (int) b.Top;
            bounds.Bottom = (int) b.Bottom;
            bounds.Right = (int) b.Right;
        }

        public void Reset()
        {
            Paint.Reset();
        }

        #region IDisposable

        private void ReleaseUnmanagedResources()
        {
            Paint.Dispose();
        }

        private void Dispose(bool disposing)
        {
            ReleaseUnmanagedResources();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}