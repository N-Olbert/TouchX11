using System;
using System.Diagnostics;
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
            //var test = "Hello\udc00";
            //var w = Paint.GetGlyphWidths(test);
            //if (test.Length != w.Length)
            //{
            //    throw new InvalidOperationException("Strange");
            //}

            if (s.Length > 40000)
            {
                // Debugger.Break(); //Here is a bug...
                var portion1 = s.Substring(0, 40000);
                var portion2 = s.Substring(40000);
                var res = new float[s.Length];
                var widths1 = Paint.GetGlyphWidths(portion1);
                var widths2 = Paint.GetGlyphWidths(portion2);
                Array.Copy(widths1, res, widths1.Length);
                Array.Copy(widths2, 0, res, 40000, widths2.Length);
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