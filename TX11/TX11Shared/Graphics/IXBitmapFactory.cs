namespace TX11Shared.Graphics
{
    public interface IXBitmapFactory
    {
        IXBitmap CreateBitmap(IXBitmap bitmap, int sx, int sy, int width, int height);

        IXBitmap CreateBitmap(int width, int height);

        IXBitmap CreateBitmap(int[] pixels, int width, int height);

        IXBitmap DecodeResource(string resourceName);
    }
}