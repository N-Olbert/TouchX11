using System;

namespace TX11Shared.Graphics
{
    public interface IXBitmap : IDisposable
    {
        int Width { get; }

        int Height { get; }

        void GetPixels(int[] pixels, int offset, int stride, int x, int y, int width, int height);

        void EraseColor(int backgroundColor);

        bool IsRecycled { get; }

        void Recycle();
    }
}