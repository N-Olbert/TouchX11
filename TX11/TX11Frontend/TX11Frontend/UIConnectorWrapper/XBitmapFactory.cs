using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using SkiaSharp;
using TX11Ressources;
using TX11Shared.Graphics;

namespace TX11Frontend.UIConnectorWrapper
{
    public class XBitmapFactory : IXBitmapFactory
    {
        public IXBitmap CreateBitmap(IXBitmap bitmap, int sx, int sy, int width, int height)
        {
            SKBitmap result;
            using (var temp = new SKBitmap(width, height, SKImageInfo.PlatformColorType, SKAlphaType.Unpremul))
            {
                using (var canvas = new SKCanvas(temp))
                {
                    canvas.DrawBitmap(((XBitmap) bitmap).Bitmap, SKRect.Create(sx, sy, width, height),
                                      SKRect.Create(width, height));
                    result = temp.Copy();
                }
            }

            return new XBitmap(result);
        }

        public IXBitmap CreateBitmap(int width, int height)
        {
            return new XBitmap(new SKBitmap(width, height, SKImageInfo.PlatformColorType, SKAlphaType.Unpremul));
        }

        public IXBitmap CreateBitmap(int[] pixels, int width, int height)
        {
            if (pixels == null || pixels.Length != width * height)
            {
                throw new InvalidOperationException();
            }

            var h = GCHandle.Alloc(pixels, GCHandleType.Pinned);
            try
            {
                var bitmap = new SKBitmap(width, height, SKImageInfo.PlatformColorType, SKAlphaType.Unpremul);
                bitmap.InstallPixels(bitmap.Info, h.AddrOfPinnedObject(), bitmap.RowBytes, delegate { h.Free(); }, null);
                return new XBitmap(bitmap);
            }
            catch (Exception)
            {
                h.Free();
                throw;
            }
        }

        public IXBitmap DecodeResource(string resourceName)
        {
            using (Stream stream = ResourceManager.Assembly.GetManifestResourceStream(resourceName))
            {
                using (var codec = SKCodec.Create(stream))
                {
                    var newInfo = new SKImageInfo(codec.Info.Width, codec.Info.Height, SKImageInfo.PlatformColorType,
                                                  SKAlphaType.Unpremul);
                    var resourceBitmap = SKBitmap.Decode(codec, newInfo);
                    return new XBitmap(resourceBitmap);
                }
            }
        }

        public IXBitmap DecodeFile(string fileName)
        {
            using (Stream stream = File.OpenRead(fileName))
            {
                using (var codec = SKCodec.Create(stream))
                {
                    var newInfo = new SKImageInfo(codec.Info.Width, codec.Info.Height, SKImageInfo.PlatformColorType,
                                                  SKAlphaType.Unpremul);
                    var resourceBitmap = SKBitmap.Decode(codec, newInfo);
                    return new XBitmap(resourceBitmap);
                }
            }
        }
    }
}