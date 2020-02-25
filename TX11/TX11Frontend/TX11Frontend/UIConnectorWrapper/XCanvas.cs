using System;
using SkiaSharp;
using JetBrains.Annotations;
using TX11Shared;
using TX11Shared.Graphics;

namespace TX11Frontend.UIConnectorWrapper
{
    public sealed class XCanvas : IXCanvas
    {
        private bool ownsBitmap;

        [NotNull]
        internal SKCanvas Canvas { get; }

        [NotNull]
        internal SKBitmap Bitmap { get; }

        public XCanvas(SKBitmap bitmap)
        {
            Canvas = new SKCanvas(bitmap);
            Bitmap = bitmap;
            ownsBitmap = false;
        }

        ~XCanvas()
        {
            //Ensure disposal of SKObjects
            Dispose(false);
        }

        public void DrawBitmap(IXBitmap backgroundBitmap, float x, float y, IXPaint paint)
        {
            Canvas.DrawBitmap(((XBitmap) backgroundBitmap).Bitmap, x, y, ((XPaint) paint)?.Paint);
        }

        public void DrawRect(Rect rectangle, IXPaint paint)
        {
            Canvas.DrawRect(rectangle.ToSkRectI(), ((XPaint) paint).Paint);
        }

        public void Save()
        {
            Canvas.Save();
        }

        public bool ClipRect(Rect rectangle)
        {
            Canvas.ClipRect(rectangle.ToSkRectI());

            //https://developer.android.com/reference/android/graphics/Canvas.html#clipRect(android.graphics.Rect)
            //Returns
            //boolean true if the resulting clip is non - empty
            Canvas.GetDeviceClipBounds(out var x);
            return !x.IsEmpty;
        }

        public void Restore()
        {
            Canvas.Restore();
        }

        public void DrawBitmap(int[] colors, float x, float y, int width, int height, IXPaint xPaint)
        {
            using (var bm = new XBitmapFactory().CreateBitmap(colors, width, height))
            {
                Canvas.DrawBitmap(((XBitmap) bm).Bitmap, x, y, ((XPaint) xPaint).Paint);
            }
        }

        public void DrawPoints(float[] points, IXPaint paint)
        {
            for (int i = 0; i < points.Length; i++)
            {
                Canvas.DrawPoint(points[i], points[++i], ((XPaint) paint).Paint);
            }
        }

        public void DrawPath(IXPath path, IXPaint paint)
        {
            Canvas.DrawPath(((XPath) path).Path, ((XPaint) paint).Paint);
        }

        public void DrawLines(float[] points, IXPaint paint)
        {
            //For each segment, this request draws a line between[x1, y1] and[x2, y2]
            for (int i = 0; i < points.Length; i++)
            {
                Canvas.DrawLine(points[i], points[++i], points[++i], points[++i], ((XPaint) paint).Paint);
            }
        }

        public void DrawRect(float x, float y, float width, float height, IXPaint paint)
        {
            Canvas.DrawRect(x, y, width, height, ((XPaint) paint).Paint);
        }

        public void DrawArc(Rect r, float startAngle, float sweepAngle, bool useCenter, IXPaint paint)
        {
            Canvas.DrawArc(r.ToSkRectI(), startAngle, sweepAngle, useCenter, ((XPaint)paint).Paint);
        }

        public void DrawText(string s, float x, float y, IXPaint paint)
        {
            Canvas.DrawText(s, x, y, ((XPaint) paint).Paint);
        }

        public void ClipRect(int v1, int v2, int v3, int v4)
        {
            Canvas.ClipRect(new SKRect(v1, v2, v3, v4));
        }

        public void ClipRect(Rect r, XRegionOperation union)
        {
            var bounds = GetClipBounds();
            using (var region = new SKRegion(bounds.ToSkRectI()))
            {
                region.Op(r.ToSkRectI(), union.ToSKRegionOp());
                Canvas.ClipRegion(region);
            }
        }

        public void DrawColor(uint color)
        {
            Canvas.DrawColor(color.ToSkColor());
        }

        public Rect GetClipBounds()
        {
            Canvas.GetDeviceClipBounds(out var x);
            return new Rect((int) x.Location.X, (int) x.Location.Y, (int) x.Width, (int) x.Height);
        }

        public void SaveToDisk()
        {
            Bitmap.SaveToDisk();
        }

        public void DrawPoint(float point, float f, IXPaint paint)
        {
            Canvas.DrawPoint(point, f, ((XPaint) paint).Paint);
        }

        public bool ClipRegion(IXRegion boundingShapeRegion)
        {
            Canvas.ClipRegion(((XRegion) boundingShapeRegion).region);
            Canvas.GetDeviceClipBounds(out var x);
            return !x.IsEmpty;
            return true;
        }

        #region IDisposable

        private void ReleaseUnmanagedResources()
        {
            Canvas.Dispose();
            if (ownsBitmap)
            {
                Bitmap.Dispose();
            }
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