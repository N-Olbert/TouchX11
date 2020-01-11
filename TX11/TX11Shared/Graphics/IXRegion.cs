using System;

namespace TX11Shared.Graphics
{
    public interface IXRegion : IDisposable
    {
        /**
         * Return true if the region contains the specified point
         */
        bool Contains(int x, int y);

        /**
         * Return true if this region is empty
         */
        bool IsEmpty();

        /**
         * Return true if the region contains a single rectangle
         */
        bool IsRect();

        /**
          * Return true if the region is empty, or if the specified rectangle does
          * not intersect the region. Returning false is not a guarantee that they
          * intersect, but returning true is a guarantee that they do not.
          */
        bool QuickReject(Rect r);

        /**
            * Return a new Rect set to the bounds of the region. If the region is
            * empty, the Rect will be set to [0, 0, 0, 0]
            */
        Rect GetBounds();

        /**
         * Perform the specified Op on this region and the specified rect. Return
         * true if the result of the op is not empty.
         */
        bool Op(Rect r, XRegionOperation op);

        /**
         * Translate the region by [dx, dy]. If the region is empty, do nothing.
         */
        void Translate(int dx, int dy);

        /**
         * Set the dst region to the result of translating this region by [dx, dy].
         * If this region is empty, then dst will be set to empty.
         */
        void Translate(int dx, int dy, IXRegion dst);

        /**
         * Perform the specified Op on this region and the specified region. Return
         * true if the result of the op is not empty.
         */
        bool Op(IXRegion region, XRegionOperation op);
    }
}