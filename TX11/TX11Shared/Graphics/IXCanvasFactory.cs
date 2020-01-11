namespace TX11Shared.Graphics
{
    public interface IXCanvasFactory
    {
        IXCanvas CreateCanvas(IXBitmap bitmap);
    }
}