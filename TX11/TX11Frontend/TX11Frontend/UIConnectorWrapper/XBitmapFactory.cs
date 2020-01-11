using System;
using System.Diagnostics;
using System.IO;
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
            if (pixels.Length != width * height)
            {
                throw new InvalidOperationException();
            }

            var bm = new SKBitmap(width, height, SKImageInfo.PlatformColorType, SKAlphaType.Unpremul);
            var colors = new SKColor[width * height];
            for (int row = 0; row < height; row++)
            {
                for (int col = 0; col < width; col++)
                {
                    var index = row * width + col;
                    var value = pixels[index];
                    colors[index] = value.ToSkColor();
                }
            }

            bm.Pixels = colors;
            return new XBitmap(bm);
        }

        public IXBitmap createBitmap(int[] pixels, int offset, int width, int height)
        {
            var bm = new SKBitmap(width, height, SKImageInfo.PlatformColorType, SKAlphaType.Unpremul);
            var colors = new SKColor[width * height];
            for (int row = 0; row < height; row++)
            {
                for (int col = 0; col < width; col++)
                {
                    var index = row * width + col;
                    var value = pixels[index];
                    colors[index + offset] = value.ToSkColor();
                }
            }

            bm.Pixels = colors;
            return new XBitmap(bm);
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