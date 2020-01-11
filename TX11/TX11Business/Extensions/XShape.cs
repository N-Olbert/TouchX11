using System.Collections.Generic;
using TX11Business.Compatibility;
using TX11Business.UIDependent;
using TX11Shared;
using TX11Shared.Graphics;

namespace TX11Business.Extensions
{
    /// <summary>
    /// Handles requests related to the X SHAPE extension.
    /// </summary>
    internal class XShape
    {
        internal const byte EventBase = 76;
        internal const byte KindBounding = 0;
        internal const byte KindClip = 1;
        internal const byte KindInput = 2;

        private const byte ShapeQueryVersion = 0;
        private const byte ShapeRectangles = 1;
        private const byte ShapeMask = 2;
        private const byte ShapeCombine = 3;
        private const byte ShapeOffset = 4;
        private const byte ShapeQueryExtents = 5;
        private const byte ShapeSelectInput = 6;
        private const byte ShapeInputSelected = 7;
        private const byte ShapeGetRectangles = 8;

        private const byte OpSet = 0;
        private const byte OpUnion = 1;
        private const byte OpIntersect = 2;
        private const byte OpSubtract = 3;
        private const byte OpInvert = 4;

        /**
         * Process a request relating to the X SHAPE extension.
         *
         * @param xServer	The X server.
         * @param client	The remote client.
         * @param opcode	The request's opcode.
         * @param arg	Optional first argument.
         * @param bytesRemaining	Bytes yet to be read in the request.
         * @param sequenceNumber	Request sequence number.

         */
        internal static void ProcessRequest(XServer xServer, Client client, byte opcode, byte arg, int bytesRemaining)
        {
            var io = client.GetInputOutput();

            switch (arg)
            {
                case ShapeQueryVersion:
                    if (bytesRemaining != 0)
                    {
                        io.ReadSkip(bytesRemaining);
                        ErrorCode.WriteWithMinorOpcode(client, ErrorCode.Length, arg, opcode, 0);
                    }
                    else
                    {
                        lock (io)
                        {
                            Util.WriteReplyHeader(client, arg);
                            io.WriteInt(0); // Reply length.
                            io.WriteShort((short) 1); // Shape major.
                            io.WriteShort((short) 1); // Shape minor.
                            io.WritePadBytes(20);
                        }

                        io.Flush();
                    }

                    break;
                case ShapeRectangles:
                    if (bytesRemaining < 12)
                    {
                        io.ReadSkip(bytesRemaining);
                        ErrorCode.WriteWithMinorOpcode(client, ErrorCode.Length, arg, opcode, 0);
                    }
                    else
                    {
                        var shapeOp = (byte) io.ReadByte();
                        var shapeKind = (byte) io.ReadByte();

                        io.ReadByte(); // Ordering.
                        io.ReadSkip(1);

                        var wid = io.ReadInt();
                        var x = io.ReadShort();
                        var y = io.ReadShort();
                        var w = (Window) xServer.GetResource(wid);

                        bytesRemaining -= 12;

                        var nr = bytesRemaining / 8;
                        var r = (nr == 0) ? null : Util.RegionFactory.GetRegion();

                        for (var i = 0; i < nr; i++)
                        {
                            var rx = io.ReadShort();
                            var ry = io.ReadShort();
                            var rw = io.ReadShort();
                            var rh = io.ReadShort();

                            r.Op(new Rect(rx, ry, rx + rw, ry + rh), XRegionOperation.Union);
                            bytesRemaining -= 8;
                        }

                        if (bytesRemaining != 0) // Oops!
                            io.ReadSkip(bytesRemaining);

                        RegionOperate(w, shapeKind, r, shapeOp, x, y);
                        if (shapeKind != KindInput && w.IsViewable())
                            w.Invalidate();
                    }

                    break;
                case ShapeMask:
                    if (bytesRemaining != 16)
                    {
                        io.ReadSkip(bytesRemaining);
                        ErrorCode.WriteWithMinorOpcode(client, ErrorCode.Length, arg, opcode, 0);
                    }
                    else
                    {
                        var shapeOp = (byte) io.ReadByte();
                        var shapeKind = (byte) io.ReadByte();

                        io.ReadSkip(2);

                        var wid = io.ReadInt();
                        var x = io.ReadShort();
                        var y = io.ReadShort();
                        var pid = io.ReadInt(); // Pixmap ID.
                        var w = (Window) xServer.GetResource(wid);
                        var p = (pid == 0) ? null : (Pixmap) xServer.GetResource(pid);
                        var r = (p == null) ? null : CreateRegion(p);

                        RegionOperate(w, shapeKind, r, shapeOp, x, y);
                        if (shapeKind != KindInput && w.IsViewable())
                            w.Invalidate();
                    }

                    break;
                case ShapeCombine:
                    if (bytesRemaining != 16)
                    {
                        io.ReadSkip(bytesRemaining);
                        ErrorCode.WriteWithMinorOpcode(client, ErrorCode.Length, arg, opcode, 0);
                    }
                    else
                    {
                        var shapeOp = (byte) io.ReadByte();
                        var dstKind = (byte) io.ReadByte();
                        var srcKind = (byte) io.ReadByte();

                        io.ReadSkip(1);

                        var dwid = io.ReadInt();
                        var x = io.ReadShort();
                        var y = io.ReadShort();
                        var swid = io.ReadInt();
                        var sw = (Window) xServer.GetResource(swid);
                        var dw = (Window) xServer.GetResource(dwid);
                        var sr = sw.GetShapeRegion(srcKind);
                        var irect = sw.GetIRect();

                        x -= irect.Left; // Make region coordinates relative.
                        y -= irect.Top;

                        RegionOperate(dw, dstKind, sr, shapeOp, x, y);
                        if (dstKind != KindInput && dw.IsViewable())
                            dw.Invalidate();
                    }

                    break;
                case ShapeOffset:
                    if (bytesRemaining != 12)
                    {
                        io.ReadSkip(bytesRemaining);
                        ErrorCode.WriteWithMinorOpcode(client, ErrorCode.Length, arg, opcode, 0);
                    }
                    else
                    {
                        var shapeKind = (byte) io.ReadByte();

                        io.ReadSkip(3);

                        var wid = io.ReadInt();
                        var x = io.ReadShort();
                        var y = io.ReadShort();
                        var w = (Window) xServer.GetResource(wid);
                        var r = w.GetShapeRegion(shapeKind);

                        if (r != null && (x != 0 || y != 0))
                        {
                            r.Translate(x, y);
                            w.SendShapeNotify(shapeKind);
                            if (shapeKind != KindInput && w.IsViewable())
                                w.Invalidate();
                        }
                    }

                    break;
                case ShapeQueryExtents:
                    if (bytesRemaining != 4)
                    {
                        io.ReadSkip(bytesRemaining);
                        ErrorCode.WriteWithMinorOpcode(client, ErrorCode.Length, arg, opcode, 0);
                    }
                    else
                    {
                        var wid = io.ReadInt();
                        var w = (Window) xServer.GetResource(wid);
                        var bs = w.IsBoundingShaped();
                        var cs = w.IsClipShaped();
                        Rect orect;
                        Rect irect;

                        if (bs)
                            orect = w.GetShapeRegion(KindBounding).GetBounds();
                        else
                            orect = w.GetORect();

                        if (cs)
                            irect = w.GetShapeRegion(KindClip).GetBounds();
                        else
                            irect = w.GetIRect();

                        lock (io)
                        {
                            Util.WriteReplyHeader(client, arg);
                            io.WriteInt(0);
                            io.WriteByte((byte) (bs ? 1 : 0)); // Bounding shaped?
                            io.WriteByte((byte) (cs ? 1 : 0)); // Clip shaped?
                            io.WritePadBytes(2);
                            io.WriteShort((short) orect.Left);
                            io.WriteShort((short) orect.Top);
                            io.WriteShort((short) orect.Width());
                            io.WriteShort((short) orect.Height());
                            io.WriteShort((short) irect.Left);
                            io.WriteShort((short) irect.Top);
                            io.WriteShort((short) irect.Width());
                            io.WriteShort((short) irect.Height());
                            io.WritePadBytes(4);
                        }

                        io.Flush();
                    }

                    break;
                case ShapeSelectInput:
                    if (bytesRemaining != 8)
                    {
                        io.ReadSkip(bytesRemaining);
                        ErrorCode.WriteWithMinorOpcode(client, ErrorCode.Length, arg, opcode, 0);
                    }
                    else
                    {
                        var wid = io.ReadInt();
                        var enable = (io.ReadByte() == 1);

                        io.ReadSkip(3);

                        var w = (Window) xServer.GetResource(wid);

                        if (enable)
                            w.AddShapeSelectInput(client);
                        else
                            w.RemoveShapeSelectInput(client);
                    }

                    break;
                case ShapeInputSelected:
                    if (bytesRemaining != 4)
                    {
                        io.ReadSkip(bytesRemaining);
                        ErrorCode.WriteWithMinorOpcode(client, ErrorCode.Length, arg, opcode, 0);
                    }
                    else
                    {
                        var wid = io.ReadInt();
                        var w = (Window) xServer.GetResource(wid);
                        var enabled = w.ShapeSelectInputEnabled(client);

                        lock (io)
                        {
                            Util.WriteReplyHeader(client, (byte) (enabled ? 1 : 0));
                            io.WriteInt(0); // Reply length.
                            io.WritePadBytes(24);
                        }

                        io.Flush();
                    }

                    break;
                case ShapeGetRectangles:
                    if (bytesRemaining != 8)
                    {
                        io.ReadSkip(bytesRemaining);
                        ErrorCode.WriteWithMinorOpcode(client, ErrorCode.Length, arg, opcode, 0);
                    }
                    else
                    {
                        var wid = io.ReadInt();
                        var shapeKind = (byte) io.ReadByte();

                        io.ReadSkip(3);

                        var w = (Window) xServer.GetResource(wid);
                        var r = w.GetShapeRegion(shapeKind);
                        var irect = w.GetIRect();
                        byte ordering = 0; // Unsorted.
                        var rectangles = RectanglesFromRegion(r);
                        var nr = rectangles.Size();

                        lock (io)
                        {
                            Util.WriteReplyHeader(client, ordering);
                            io.WriteInt(2 * nr); // Reply length.
                            io.WriteInt(nr);
                            io.WritePadBytes(20);

                            foreach (var rect in rectangles)
                            {
                                io.WriteShort((short) (rect.Left - irect.Left));
                                io.WriteShort((short) (rect.Top - irect.Top));
                                io.WriteShort((short) rect.Width());
                                io.WriteShort((short) rect.Height());
                            }
                        }

                        io.Flush();
                    }

                    break;
                default:
                    io.ReadSkip(bytesRemaining);
                    ErrorCode.Write(client, ErrorCode.Implementation, opcode, 0);
                    break;
            }
        }

        /**
         * Carry out a shape operation on a region.
         *
         * @param w	The destination window to operate on.
         * @param shapeKind	The type of shape in the destination window.
         * @param sr	Source region.
         * @param shapeOp	Operation to carry out on the regions.
         * @param x	X offset to apply to the source region.
         * @param y	Y offset to apply to the source region.
         */
        private static void RegionOperate(Window w, byte shapeKind, IXRegion sr, byte shapeOp, int x, int y)
        {
            if (sr != null)
            {
                // Apply (x, y) offset.
                var r = Util.RegionFactory.GetRegion();
                var irect = w.GetIRect();

                sr.Translate(x + irect.Left, y + irect.Top, r);
                sr = r;
            }

            var dr = w.GetShapeRegion(shapeKind);

            switch (shapeOp)
            {
                case OpSet:
                    break;
                case OpUnion:
                    if (sr == null || dr == null)
                        sr = null;
                    else
                        sr.Op(dr, XRegionOperation.Union);
                    break;
                case OpIntersect:
                    if (sr == null)
                        sr = dr;
                    else if (dr != null)
                        sr.Op(dr, XRegionOperation.Intersect);
                    break;
                case OpSubtract: // Subtract source region from dest region.
                    if (sr == null)
                        sr = Util.RegionFactory.GetRegion(); // Empty region.
                    else if (dr == null)
                        sr.Op(w.GetORect(), XRegionOperation.Difference);
                    else
                        sr.Op(dr, XRegionOperation.Difference);
                    break;
                case OpInvert: // Subtract dest region from source region.
                    if (dr == null)
                    {
                        sr = Util.RegionFactory.GetRegion(); // Empty region.
                    }
                    else if (sr == null)
                    {
                        sr = Util.RegionFactory.GetRegion(w.GetORect());
                        sr.Op(dr, XRegionOperation.ReverseDifference);
                    }
                    else
                    {
                        sr.Op(dr, XRegionOperation.ReverseDifference);
                    }

                    break;
                default:
                    return;
            }

            w.SetShapeRegion(shapeKind, sr);
            w.SendShapeNotify(shapeKind);
        }

        /**
         * Return a list of rectangles that when combined make up the region.
         *
         * @param r	The region.
         * @return	
         */
        private static List<Rect> RectanglesFromRegion(IXRegion r)
        {
            var rl = new List<Rect>();

            if (r != null && !r.IsEmpty())
            {
                if (r.IsRect())
                    Util.Add(rl, r.GetBounds());
                else
                    ExtractRectangles(r, r.GetBounds(), rl);
            }

            return rl;
        }

        /**
         * Recursively break the region contained in the rectangle into
         * rectangles entirely contained in the rectangle.
         *
         * @param r	Region being broken into rectangles.
         * @param rect	Part of the region being analyzed..
         * @param rl	Return list of rectangles.
         */
        private static void ExtractRectangles(IXRegion r, Rect rect, List<Rect> rl)
        {
            var rs = RegionRectIntersection(r, rect);

            if (rs == 0) // No intersection with rect.
                return;

            if (rs == 1)
            {
                // Full intersection with rect.
                Util.Add(rl, rect);
                return;
            }

            var rw = rect.Width();
            var rh = rect.Height();

            if (rw > rh)
            {
                // Split the rectangle horizontally.
                var cx = rect.Left + rw / 2;

                ExtractRectangles(r, new Rect(rect.Left, rect.Top, cx, rect.Bottom), rl);
                ExtractRectangles(r, new Rect(cx, rect.Top, rect.Right, rect.Bottom), rl);
            }
            else
            {
                // Split it vertically.
                var cy = rect.Top + rh / 2;

                ExtractRectangles(r, new Rect(rect.Left, rect.Top, rect.Right, cy), rl);
                ExtractRectangles(r, new Rect(rect.Left, cy, rect.Right, rect.Bottom), rl);
            }
        }

        /**
         * Check how a region intersects with a rectangle.
         *
         * @param r	The region.
         * @param rect	The rectangle.
         * @return	0 = no overlap; 1 = full overlap; -1 = partial overlap.
         */
        private static int RegionRectIntersection(IXRegion r, Rect rect)
        {
            if (r.QuickReject(rect))
                return 0;

            var icount = 0;
            var ocount = 0;

            for (var y = rect.Top; y < rect.Bottom; y++)
            {
                for (var x = rect.Left; x < rect.Right; x++)
                    if (r.Contains(x, y))
                        icount++;
                    else
                        ocount++;

                if (icount > 0 && ocount > 0)
                    return -1;
            }

            if (icount == 0)
                return 0;
            else if (ocount == 0)
                return 1;

            return -1;
        }

        /**
         * Create a region using the non-zero pixels in the pixmap.
         *
         * @param p	The pixmap.
         * @return	A region equivalent to the non-zero pixels.
         */
        private static IXRegion CreateRegion(Pixmap p)
        {
            var d = p.GetDrawable();
            var r = Util.RegionFactory.GetRegion();

            ExtractRegion(r, d.GetBitmap(), new Rect(0, 0, d.GetWidth(), d.GetHeight()));

            return r;
        }

        /**
         * Recursively break the image contained in the rectangle into
         * rectangles containing non-zero pixels.
         *
         * @param region	Returned region.
         * @param bitmap	Bitmap where the pixels appear.
         * @param rect	Rectangle containing the pixels.
         */
        private static void ExtractRegion(IXRegion region, IXBitmap bitmap, Rect rect)
        {
            var nzp = CheckNonZeroPixels(bitmap, rect);

            if (nzp == 1) // Empty.
                return;

            var rw = rect.Width();
            var rh = rect.Height();

            if (nzp == 2)
            {
                // All non-zero. We have a rectangle.
                region.Op(rect, XRegionOperation.Union);
                return;
            }

            if (rw > rh)
            {
                // Split the rectangle horizontally.
                var cx = rect.Left + rw / 2;

                ExtractRegion(region, bitmap, new Rect(rect.Left, rect.Top, cx, rect.Bottom));
                ExtractRegion(region, bitmap, new Rect(cx, rect.Top, rect.Right, rect.Bottom));
            }
            else
            {
                // Split it vertically.
                var cy = rect.Top + rh / 2;

                ExtractRegion(region, bitmap, new Rect(rect.Left, rect.Top, rect.Right, cy));
                ExtractRegion(region, bitmap, new Rect(rect.Left, cy, rect.Right, rect.Bottom));
            }
        }

        /**
         * Check the number of non-zero pixels contained in the rectangle.
         * Return a bit mask indicating whether all the pixels are non-zero,
         * none of them, or a mix.
         *
         * @param bitmap	The bitmap containing the pixels.
         * @param rect	The rectangle.
         * @return	1 = no pixels set; 2 = all pixels set; 0 = some pixels set
         */
        private static int CheckNonZeroPixels(IXBitmap bitmap, Rect rect)
        {
            var width = rect.Width();
            var height = rect.Height();
            var pixels = new int[width];
            var mask = 3;

            for (var i = 0; i < height; i++)
            {
                bitmap.GetPixels(pixels, 0, width, rect.Left, rect.Top + i, width, 1);

                foreach (var p in pixels)
                {
                    mask &= (p != 0xff000000.AsInt()) ? 2 : 1;
                    if (mask == 0)
                        return 0;
                }
            }

            return mask;
        }
    }
}