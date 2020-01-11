using TX11Shared.Graphics;

namespace TX11Frontend.UIConnectorWrapper
{
    public class XCanvasFactory : IXCanvasFactory
    {
        public IXCanvas CreateCanvas(IXBitmap bitmap)
        {
            return new XCanvas(((XBitmap) bitmap).Bitmap);
        }
    }
}