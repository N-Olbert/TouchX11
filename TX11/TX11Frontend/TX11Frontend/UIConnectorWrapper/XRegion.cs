using System;
using JetBrains.Annotations;
using SkiaSharp;
using TX11Shared;
using TX11Shared.Graphics;

namespace TX11Frontend.UIConnectorWrapper
{
    public sealed class XRegion : IXRegion
    {
        [NotNull]
        public SKRegion region;

        public XRegion()
        {
            region = new SKRegion();
        }

        /** Return a region set to the specified rectangle
   */
        public XRegion(Rect r)
        {
            region = new SKRegion(r.ToSkRectI());
        }

        ~XRegion()
        {
            Dispose(false);
        }

        /**
     * Return true if the region contains the specified point
     */
        public bool Contains(int x, int y)
        {
            return region.Contains(x, y);
        }

        /**
 * Return true if this region is empty
 */
        public bool IsEmpty()
        {
            return region.Bounds.IsEmpty;
        }

        /**
         * Return true if the region contains a single rectangle
         */
        public bool IsRect()
        {
            //True as SK doesnt support complex mode (yet)
            return true;
        }

        /**
  * Return true if the region is empty, or if the specified rectangle does
  * not intersect the region. Returning false is not a guarantee that they
  * intersect, but returning true is a guarantee that they do not.
  */
        public bool QuickReject(Rect r)
        {
            return IsEmpty() || !region.Intersects(r.ToSkRectI());
        }

        /**
    * Return a new Rect set to the bounds of the region. If the region is
    * empty, the Rect will be set to [0, 0, 0, 0]
    */
        public Rect GetBounds()
        {
            return region.Bounds.ToRect();
        }

        /**
 * Perform the specified Op on this region and the specified rect. Return
 * true if the result of the op is not empty.
 */
        public bool Op(Rect r, XRegionOperation op)
        {
            return region.Op(r.ToSkRectI(), op.ToSKRegionOp());
        }

        /**
     * Translate the region by [dx, dy]. If the region is empty, do nothing.
     */
        public void Translate(int dx, int dy)
        {
            if (!this.region.Bounds.IsEmpty)
            {
                var b = this.region.Bounds;
                var translated = new SKRectI(b.Left + dx, b.Top + dy, b.Right + dx, b.Left + dy);
                this.region.SetRect(translated);
            }
        }

        /**
         * Set the dst region to the result of translating this region by [dx, dy].
         * If this region is empty, then dst will be set to empty.
         */
        public void Translate(int dx, int dy, IXRegion dst)
        {
            //TODO: Review
            if (!this.region.Bounds.IsEmpty)
            {
                var b = this.region.Bounds;
                var translated = new SKRectI(b.Left + dx, b.Top + dy, b.Right + dx, b.Bottom + dy);
                this.region.SetRect(translated);
                ((XRegion) dst).region.SetRect(translated);
            }
            else
            {
                ((XRegion) dst).region.SetRect(SKRectI.Empty);
            }
        }

        /**
 * Perform the specified Op on this region and the specified region. Return
 * true if the result of the op is not empty.
 */
        public bool Op(IXRegion region, XRegionOperation op)
        {
            return this.region.Op(((XRegion) region).region, op.ToSKRegionOp());
        }

        private void ReleaseUnmanagedResources()
        {
            region.Dispose();
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
    }
}