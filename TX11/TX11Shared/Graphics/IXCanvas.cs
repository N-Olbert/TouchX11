using System;

namespace TX11Shared.Graphics
{
    public interface IXCanvas : IDisposable
    {
        void DrawBitmap(IXBitmap backgroundBitmap, float x, float y, IXPaint paint);

        void DrawRect(Rect rectangle, IXPaint paint);

        void Save();

        bool ClipRect(Rect rectangle);

        void Restore();

        void DrawBitmap(int[] colors, float x, float y, int width, int height, IXPaint xPaint);

        void DrawPoints(float[] points, IXPaint paint);

        void DrawPath(IXPath path, IXPaint paint);

        void DrawLines(float[] points, IXPaint paint);

        void DrawRect(float x, float y, float width, float height, IXPaint paint);

        void DrawArc(Rect r, float startAngle, float sweepAngle, bool useCenter, IXPaint paint);

        void DrawText(string s, float x, float y, IXPaint paint);

        void ClipRect(int v1, int v2, int v3, int v4);

        void ClipRect(Rect r, XRegionOperation union);

        void DrawColor(uint color);

        Rect GetClipBounds();

        void DrawPoint(float point, float f, IXPaint paint);

        bool ClipRegion(IXRegion boundingShapeRegion);
    }
}