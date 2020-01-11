using TX11Shared;
using TX11Shared.Graphics;

namespace TX11Frontend.UIConnectorWrapper
{
    public class XRegionFactory : IXRegionFactory
    {
        public IXRegion GetRegion()
        {
            return new XRegion();
        }

        public IXRegion GetRegion(Rect r)
        {
            return new XRegion(r);
        }
    }
}