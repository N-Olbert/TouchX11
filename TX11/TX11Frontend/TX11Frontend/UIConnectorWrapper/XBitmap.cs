using System;
using System.Diagnostics;
using JetBrains.Annotations;
using SkiaSharp;
using TX11Shared.Graphics;

namespace TX11Frontend.UIConnectorWrapper
{
    public sealed class XBitmap : IXBitmap
    {
        /// <summary>
        /// The underlaying bitmap
        /// </summary>
        [NotNull]
        public SKBitmap Bitmap { get; }

        public int Width => Bitmap.Width;

        public int Height => Bitmap.Height;

        public bool IsRecycled { get; private set; }

        internal XBitmap() : this(new SKBitmap())
        {
        }

        internal XBitmap([NotNull] SKBitmap bitmap)
        {
            Bitmap = bitmap;
        }

        ~XBitmap()
        {
            Dispose(false);
        }

        public void GetPixels(int[] pixels, int offset, int stride, int x, int y, int width, int height)
        {

            var currentPixels = Bitmap.Pixels;
            var rowSize = Bitmap.Info.Width;
            unsafe
            {
                fixed (SKColor* color = currentPixels)
                {
                    fixed (int* ptr = pixels)
                    {
                        var bytesPerPow = width * Bitmap.BytesPerPixel;
                        for (int row = 0; row < height; row++)
                        {
                            var innerindex = row * width;
                            var realIndex = (row + y) * rowSize + (x);
                            Buffer.MemoryCopy(color + realIndex, ptr + innerindex, bytesPerPow, bytesPerPow);
                        }
                    }
                }
            }
        }

        public void EraseColor(int backgroundColor)
        {
            //Equal methods:
            // https://skia-doc.commondatastorage.googleapis.com/doxygen/doxygen/html/classSkBitmap.html#a35f53aa9e91280892da7e38b101f4f31
            // Fill the entire bitmap with the specified color.
            // -> same as https://developer.android.com/reference/android/graphics/Bitmap#eraseColor(int)
            Bitmap.Erase(backgroundColor.ToSkColor());
        }

        public void Recycle()
        {
            IsRecycled = true;
        }

        #region IDisposable

        private void ReleaseUnmanagedResources()
        {
            Bitmap.Dispose();
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