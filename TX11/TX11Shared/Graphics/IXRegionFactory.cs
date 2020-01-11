namespace TX11Shared.Graphics
{
    public interface IXRegionFactory
    {
        IXRegion GetRegion();

        IXRegion GetRegion(Rect r);
    }
}