using System;
using TX11Business.Compatibility;
using TX11Shared;
using TX11Shared.Graphics;

namespace TX11Business.UIDependent
{
    internal class Drawable
    {
        private readonly IXBitmap bitmap;
        private readonly IXCanvas canvas;
        private readonly int depth;
        private IXBitmap backgroundBitmap;
        private int backgroundColor;
        private bool[] shapeMask;

        private const byte BitmapFormat = 0;
        private const byte XyPixmapFormat = 1;
        private const byte ZPixmapFormat = 2;

        /**
         * Constructor.
         *
         * @param width	The drawable width.
         * @param height	The drawable height.
         * @param depth	The drawable depth.
         * @param bgbitmap	Background bitmap. Can be null.
         * @param bgcolor	Background color.
         */
        internal Drawable(int width, int height, int depth, IXBitmap bgbitmap, int bgcolor)
        {
            bitmap = Util.BitmapFactory.CreateBitmap(width, height);
            canvas = Util.CanvasFactory.CreateCanvas(bitmap);
            this.depth = depth;
            backgroundBitmap = bgbitmap;
            backgroundColor = bgcolor;
        }

        /**
         * Return the drawable's width.
         *
         * @return	The drawable's width.
         */
        internal int GetWidth()
        {
            return bitmap.Width;
        }

        /**
         * Return the drawable's height.
         *
         * @return	The drawable's height.
         */
        internal int GetHeight()
        {
            return bitmap.Height;
        }

        /**
         * Return the drawable's depth.
         *
         * @return	The drawable's depth.
         */
        internal int GetDepth()
        {
            return depth;
        }

        /**
         * Return the drawable's bitmap.
         *
         * @return	The drawable's bitmap.
         */
        internal IXBitmap GetBitmap()
        {
            return bitmap;
        }

        /**
         * Set the drawable's background color.
         *
         * @param color	The background color.
         */
        internal void SetBackgroundColor(int color)
        {
            backgroundColor = color;
        }

        /**
         * Set the drawable's background bitmap.
         *
         * @param bitmap	The background bitmap.
         */
        internal void SetBackgroundBitmap(IXBitmap bitmap)
        {
            backgroundBitmap = bitmap;
        }

        /**
         * Process an X request relating to this drawable.
         *
         * @param xServer	The X server.
         * @param client	The remote client.
         * @param id	The ID of the pixmap or window using this drawable.
         * @param opcode	The request's opcode.
         * @param arg		Optional first argument.
         * @param bytesRemaining	Bytes yet to be read in the request.
         * @return	True if the drawable has been changed.

         */
        internal bool ProcessRequest(XServer xServer, Client client, int id, byte opcode, byte arg, int bytesRemaining)
        {
            var changed = false;
            var io = client.GetInputOutput();

            switch (opcode)
            {
                case RequestCode.CopyArea:
                    if (bytesRemaining != 20)
                    {
                        io.ReadSkip(bytesRemaining);
                        ErrorCode.Write(client, ErrorCode.Length, opcode, 0);
                    }
                    else
                    {
                        var did = io.ReadInt(); // Dest drawable.
                        var gcid = io.ReadInt(); // GC.
                        var sx = (short) io.ReadShort(); // Src X.
                        var sy = (short) io.ReadShort(); // Src Y.
                        var dx = (short) io.ReadShort(); // Dst X.
                        var dy = (short) io.ReadShort(); // Dst Y.
                        var width = io.ReadShort(); // Width.
                        var height = io.ReadShort(); // Height.
                        var r1 = xServer.GetResource(did);
                        var r2 = xServer.GetResource(gcid);

                        if (r1 == null || !r1.IsDrawable())
                        {
                            ErrorCode.Write(client, ErrorCode.Drawable, opcode, did);
                        }
                        else if (r2 == null || r2.GetRessourceType() != Resource.AttrGcontext)
                        {
                            ErrorCode.Write(client, ErrorCode.GContext, opcode, gcid);
                        }
                        else if (width > 0 && height > 0)
                        {
                            CopyArea(sx, sy, width, height, r1, dx, dy, (GContext) r2);
                        }
                    }

                    break;
                case RequestCode.CopyPlane:
                    if (bytesRemaining != 24)
                    {
                        io.ReadSkip(bytesRemaining);
                        ErrorCode.Write(client, ErrorCode.Length, opcode, id);
                    }
                    else
                    {
                        var did = io.ReadInt(); // Dest drawable.
                        var gcid = io.ReadInt(); // GC.
                        var sx = (short) io.ReadShort(); // Src X.
                        var sy = (short) io.ReadShort(); // Src Y.
                        var dx = (short) io.ReadShort(); // Dst X.
                        var dy = (short) io.ReadShort(); // Dst Y.
                        var width = io.ReadShort(); // Width.
                        var height = io.ReadShort(); // Height.
                        var bitPlane = io.ReadInt(); // Bit plane.
                        var r1 = xServer.GetResource(did);
                        var r2 = xServer.GetResource(gcid);

                        if (r1 == null || !r1.IsDrawable())
                        {
                            ErrorCode.Write(client, ErrorCode.Drawable, opcode, did);
                        }
                        else if (r2 == null || r2.GetRessourceType() != Resource.AttrGcontext)
                        {
                            ErrorCode.Write(client, ErrorCode.GContext, opcode, gcid);
                        }
                        else
                        {
                            if (depth != 32)
                                CopyPlane(sx, sy, width, height, bitPlane, r1, dx, dy, (GContext) r2);
                            else
                                CopyArea(sx, sy, width, height, r1, dx, dy, (GContext) r2);
                        }
                    }

                    break;
                case RequestCode.GetImage:
                    if (bytesRemaining != 12)
                    {
                        io.ReadSkip(bytesRemaining);
                        ErrorCode.Write(client, ErrorCode.Length, opcode, 0);
                    }
                    else
                    {
                        ProcessGetImageRequest(client, arg);
                    }

                    break;
                case RequestCode.QueryBestSize:
                    if (bytesRemaining != 4)
                    {
                        io.ReadSkip(bytesRemaining);
                        ErrorCode.Write(client, ErrorCode.Length, opcode, 0);
                    }
                    else
                    {
                        var width = io.ReadShort(); // Width.
                        var height = io.ReadShort(); // Height.

                        lock (io)
                        {
                            Util.WriteReplyHeader(client, (byte) 0);
                            io.WriteInt(0); // Reply length.
                            io.WriteShort((short) width); // Width.
                            io.WriteShort((short) height); // Height.
                            io.WritePadBytes(20); // Unused.
                        }

                        io.Flush();
                    }

                    break;
                case RequestCode.PolyPoint:
                case RequestCode.PolyLine:
                case RequestCode.PolySegment:
                case RequestCode.PolyRectangle:
                case RequestCode.PolyArc:
                case RequestCode.FillPoly:
                case RequestCode.PolyFillRectangle:
                case RequestCode.PolyFillArc:
                case RequestCode.PutImage:
                case RequestCode.PolyText8:
                case RequestCode.PolyText16:
                case RequestCode.ImageText8:
                case RequestCode.ImageText16:
                    if (bytesRemaining < 4)
                    {
                        io.ReadSkip(bytesRemaining);
                        ErrorCode.Write(client, ErrorCode.Length, opcode, 0);
                    }
                    else
                    {
                        var gcid = io.ReadInt(); // GContext.
                        var r = xServer.GetResource(gcid);

                        bytesRemaining -= 4;
                        if (r == null || r.GetRessourceType() != Resource.AttrGcontext)
                        {
                            io.ReadSkip(bytesRemaining);
                            ErrorCode.Write(client, ErrorCode.GContext, opcode, 0);
                        }
                        else
                        {
                            changed = ProcessGcRequest(xServer, client, id, (GContext) r, opcode, arg, bytesRemaining);
                        }
                    }

                    break;
                default:
                    io.ReadSkip(bytesRemaining);
                    ErrorCode.Write(client, ErrorCode.Implementation, opcode, 0);
                    break;
            }

            return changed;
        }

        /**
         * Process a GetImage request.
         *
         * @param client	The remote client.
         * @param format	1=XYPixmap, 2=ZPixmap.

         */
        private void ProcessGetImageRequest(Client client, byte format)
        {
            var io = client.GetInputOutput();
            var x = (short) io.ReadShort(); // X.
            var y = (short) io.ReadShort(); // Y.
            var width = io.ReadShort(); // Width.
            var height = io.ReadShort(); // Height.
            var planeMask = io.ReadInt(); // Plane mask.
            var wh = width * height;
            int n, pad;
            int[] pixels;
            byte[] bytes = null;

            if (x < 0 || y < 0 || x + width > bitmap.Width || y + height > bitmap.Height)
            {
                ErrorCode.Write(client, ErrorCode.Match, RequestCode.GetImage, 0);
                return;
            }

            try
            {
                pixels = new int[wh];
            }
            catch (OutOfMemoryException)
            {
                ErrorCode.Write(client, ErrorCode.Alloc, RequestCode.GetImage, 0);
                return;
            }

            bitmap.GetPixels(pixels, 0, width, x, y, width, height);

            if (format == ZPixmapFormat)
            {
                n = wh * 3;
            }
            else
            {
                // XY_PIXMAP_FORMAT is the only other valid value.
                var planes = Util.Bitcount(planeMask);
                var rightPad = -width & 7;
                var xmax = width + rightPad;
                var offset = 0;

                n = planes * height * (width + rightPad) / 8;

                try
                {
                    bytes = new byte[n];
                }
                catch (OutOfMemoryException)
                {
                    ErrorCode.Write(client, ErrorCode.Alloc, RequestCode.GetImage, 0);
                    return;
                }

                for (var plane = 31; plane >= 0; plane--)
                {
                    var bit = 1 << plane;

                    if ((planeMask & bit) == 0)
                        continue;

                    byte b = 0;

                    for (var yi = 0; yi < height; yi++)
                    {
                        for (var xi = 0; xi < xmax; xi++)
                        {
                            b <<= 1;
                            if (xi < width && (pixels[yi * width + xi] & bit) != 0)
                                b |= 1;

                            if ((xi & 7) == 7)
                            {
                                bytes[offset++] = b;
                                b = 0;
                            }
                        }
                    }
                }
            }

            pad = -n & 3;

            lock (io)
            {
                Util.WriteReplyHeader(client, (byte) 32);
                io.WriteInt((n + pad) / 4); // Reply length.
                io.WriteInt(0); // Visual ID.
                io.WritePadBytes(20); // Unused.

                if (format == 2)
                {
                    for (var i = 0; i < wh; i++)
                    {
                        n = pixels[i] & planeMask;
                        io.WriteByte((byte) (n & 0xff));
                        io.WriteByte((byte) ((n >> 8) & 0xff));
                        io.WriteByte((byte) ((n >> 16) & 0xff));
                    }
                }
                else
                {
                    io.WriteBytes(bytes, 0, n);
                }

                io.WritePadBytes(pad); // Unused.
            }

            io.Flush();
        }

        /**
         * Clear the entire drawable.
         */
        internal void Clear()
        {
            if (backgroundBitmap == null || backgroundBitmap.IsRecycled)
            {
                bitmap.EraseColor(backgroundColor);
            }
            else
            {
                var width = bitmap.Width;
                var height = bitmap.Height;
                var dx = backgroundBitmap.Width;
                var dy = backgroundBitmap.Height;

                for (var y = 0; y < height; y += dy)
                for (var x = 0; x < width; x += dx)
                    canvas.DrawBitmap(backgroundBitmap, x, y, null);
            }
        }

        /**
         * Clear a rectangular region of the drawable.
         *
         * @param x	X coordinate of the rectangle.
         * @param y	Y coordinate of the rectangle.
         * @param width	Width of the rectangle.
         * @param height	Height of the rectangle.
         */
        internal void ClearArea(int x, int y, int width, int height)
        {
            var r = new Rect(x, y, x + width, y + height);
            var paint = Util.GetPaint();

            if (backgroundBitmap == null || backgroundBitmap.IsRecycled)
            {
                paint.Color = (backgroundColor);
                paint.Style = (XPaintStyle.Fill);
                canvas.DrawRect(r, paint);
            }
            else
            {
                var bw = bitmap.Width;
                var bh = bitmap.Height;
                var dx = backgroundBitmap.Width;
                var dy = backgroundBitmap.Height;

                canvas.Save();
                canvas.ClipRect(r);

                for (var iy = 0; iy < bh; iy += dy)
                {
                    if (iy >= r.Bottom)
                        break;
                    if (iy + dy < r.Top)
                        continue;

                    for (var ix = 0; ix < bw; ix += dx)
                    {
                        if (ix >= r.Right)
                            break;
                        if (iy + dy < r.Left)
                            continue;

                        canvas.DrawBitmap(backgroundBitmap, ix, iy, null);
                    }
                }

                canvas.Restore();
            }
        }

        /**
         * Copy a rectangle from this drawable to another.
         *
         * @param sx	X coordinate of this rectangle.
         * @param sy	Y coordinate of this rectangle.
         * @param width	Width of the rectangle.
         * @param height	Height of the rectangle.
         * @param dr	The pixmap or window to draw the rectangle in.
         * @param dx	The destination X coordinate.
         * @param dy	The destination Y coordinate.
         * @param gc	The GContext.

         */
        private void CopyArea(int sx, int sy, int width, int height, Resource dr, int dx, int dy, GContext gc)
        {
            Drawable dst;

            if (dr.GetRessourceType() == Resource.AttrPixmap)
                dst = ((Pixmap) dr).GetDrawable();
            else
                dst = ((Window) dr).GetDrawable();

            if (sx < 0)
            {
                width += sx;
                dx -= sx;
                sx = 0;
            }

            if (sy < 0)
            {
                height += sy;
                dy -= sy;
                sy = 0;
            }

            if (sx + width > bitmap.Width)
                width = bitmap.Width - sx;

            if (sy + height > bitmap.Height)
                height = bitmap.Height - sy;

            if (width <= 0 || height <= 0)
                return;

            var bm = Util.BitmapFactory.CreateBitmap(bitmap, sx, sy, width, height);

            dst.canvas.DrawBitmap(bm, dx, dy, gc.GetPaint());

            if (dr.GetRessourceType() == Resource.AttrWindow)
                ((Window) dr).Invalidate(dx, dy, width, height);

            if (gc.GetGraphicsExposure())
                EventCode.SendNoExposure(gc.GetClient(), dr, RequestCode.CopyArea);
        }

        /**
         * Copy a rectangle from a plane of this drawable to another rectangle.
         *
         * @param sx	X coordinate of this rectangle.
         * @param sy	Y coordinate of this rectangle.
         * @param width	Width of the rectangle.
         * @param height	Height of the rectangle.
         * @param bitPlane	The bit plane being copied.
         * @param dr	The pixmap or window to draw the rectangle in.
         * @param dx	The destination X coordinate.
         * @param dy	The destination Y coordinate.
         * @param gc	The GContext.

         */
        private void CopyPlane(int sx, int sy, int width, int height, int bitPlane, Resource dr, int dx, int dy,
            GContext gc)
        {
            Drawable dst;

            if (dr.GetRessourceType() == Resource.AttrPixmap)
                dst = ((Pixmap) dr).GetDrawable();
            else
                dst = ((Window) dr).GetDrawable();

            var fg = (depth == 1) ? 0xffffffff.AsInt() : gc.GetForegroundColor();
            var bg = (depth == 1) ? 0 : gc.GetBackgroundColor();
            var pixels = new int[width * height];

            bitmap.GetPixels(pixels, 0, width, sx, sy, width, height);
            for (var i = 0; i < pixels.Length; i++)
                pixels[i] = ((pixels[i] & bitPlane) != 0) ? fg : bg;

            dst.canvas.DrawBitmap(pixels, 0, width, dx, dy, width, height, true, gc.GetPaint());

            if (dr.GetRessourceType() == Resource.AttrWindow)
                ((Window) dr).Invalidate(dx, dy, width, height);

            if (gc.GetGraphicsExposure())
                EventCode.SendNoExposure(gc.GetClient(), dr, RequestCode.CopyPlane);
        }

        /**
         * Draw text at the specified location, on top of a bounding rectangle
         * drawn in the background color.
         *
         * @param s	The string to write.
         * @param x	X coordinate.
         * @param y	Y coordinate.
         * @param gc	Graphics context for drawing the text.
         */
        private void DrawImageText(string s, int x, int y, GContext gc)
        {
            var paint = gc.GetPaint();
            var font = gc.GetFont();
            var rect = new Rect();

            font.GetTextBounds(s, x, y, rect);
            paint.Color = (gc.GetBackgroundColor());
            paint.Style = (XPaintStyle.Fill);
            canvas.DrawRect(rect, paint);

            paint.Color = (gc.GetForegroundColor());
            canvas.DrawText(s, x, y, paint);
        }

        /**
         * Process an X request relating to this drawable using the
         * GContext provided.
         *
         * @param xServer	The X server.
         * @param client	The remote client.
         * @param id	The ID of the pixmap or window using this drawable.
         * @param gc	The GContext to use for drawing.
         * @param opcode	The request's opcode.
         * @param arg		Optional first argument.
         * @param bytesRemaining	Bytes yet to be read in the request.
         * @return	True if the drawable is modified.

         */
        internal bool ProcessGcRequest(XServer xServer, Client client, int id, GContext gc, byte opcode, byte arg,
            int bytesRemaining)
        {
            var io = client.GetInputOutput();
            var paint = gc.GetPaint();
            var changed = false;
            var originalColor = paint.Color;

            canvas.Save();
            gc.ApplyClipRectangles(canvas);

            var seq = client.GetSequenceNumber();
            switch (opcode)
            {
                case RequestCode.PolyPoint:
                    if ((bytesRemaining & 3) != 0)
                    {
                        io.ReadSkip(bytesRemaining);
                        ErrorCode.Write(client, ErrorCode.Length, opcode, 0);
                    }
                    else
                    {
                        var points = new float[bytesRemaining / 2];
                        var i = 0;

                        while (bytesRemaining > 0)
                        {
                            float p = (short) io.ReadShort();

                            bytesRemaining -= 2;
                            if (arg == 0 || i < 2) // Relative to origin.
                                points[i] = p;
                            else
                                points[i] = points[i - 2] + p; // Rel to previous.
                            i++;
                        }

                        try
                        {
                            canvas.DrawPoints(points, paint);
                        }
                        catch (Exception)
                        {
                            for (i = 0; i < points.Length; i += 2)
                                canvas.DrawPoint(points[i], points[i + 1], paint);
                        }

                        changed = true;
                    }

                    break;
                case RequestCode.PolyLine:
                    if ((bytesRemaining & 3) != 0)
                    {
                        io.ReadSkip(bytesRemaining);
                        ErrorCode.Write(client, ErrorCode.Length, opcode, 0);
                    }
                    else
                    {
                        var path = Util.GetPath();
                        var i = 0;

                        while (bytesRemaining > 0)
                        {
                            float x = (short) io.ReadShort();
                            float y = (short) io.ReadShort();

                            bytesRemaining -= 4;
                            if (i == 0)
                                path.MoveTo(x, y);
                            else if (arg == 0) // Relative to origin.
                                path.LineTo(x, y);
                            else // Relative to previous.
                                path.RLineTo(x, y);
                            i++;
                        }

                        paint.Style = XPaintStyle.Stroke;
                        canvas.DrawPath(path, paint);
                        changed = true;
                    }

                    break;
                case RequestCode.PolySegment:
                    if ((bytesRemaining & 7) != 0)
                    {
                        io.ReadSkip(bytesRemaining);
                        ErrorCode.Write(client, ErrorCode.Length, opcode, 0);
                    }
                    else
                    {
                        var points = new float[bytesRemaining / 2];
                        var i = 0;

                        while (bytesRemaining > 0)
                        {
                            points[i++] = (short) io.ReadShort();
                            bytesRemaining -= 2;
                        }

                        canvas.DrawLines(points, paint);
                        changed = true;
                    }

                    break;
                case RequestCode.PolyRectangle:
                case RequestCode.PolyFillRectangle:
                    if ((bytesRemaining & 7) != 0)
                    {
                        io.ReadSkip(bytesRemaining);
                        ErrorCode.Write(client, ErrorCode.Length, opcode, 0);
                    }
                    else
                    {
                        if (opcode == RequestCode.PolyRectangle)
                            paint.Style = XPaintStyle.Stroke;
                        else
                            paint.Style = XPaintStyle.Fill;

                        while (bytesRemaining > 0)
                        {
                            float x = (short) io.ReadShort();
                            float y = (short) io.ReadShort();
                            float width = io.ReadShort();
                            float height = io.ReadShort();

                            bytesRemaining -= 8;
                            canvas.DrawRect(x, y, width, height, paint);
                            changed = true;
                        }
                    }

                    break;
                case RequestCode.FillPoly:
                    if (bytesRemaining < 4 || (bytesRemaining & 3) != 0)
                    {
                        io.ReadSkip(bytesRemaining);
                        ErrorCode.Write(client, ErrorCode.Length, opcode, 0);
                    }
                    else
                    {
                        io.ReadByte(); // Shape.

                        var mode = io.ReadByte(); // Coordinate mode.
                        var path = Util.GetPath();
                        var i = 0;

                        io.ReadSkip(2); // Unused.
                        bytesRemaining -= 4;

                        while (bytesRemaining > 0)
                        {
                            float x = (short) io.ReadShort();
                            float y = (short) io.ReadShort();

                            bytesRemaining -= 4;
                            if (i == 0)
                                path.MoveTo(x, y);
                            else if (mode == 0) // Relative to origin.
                                path.LineTo(x, y);
                            else // Relative to previous.
                                path.RLineTo(x, y);
                            i++;
                        }

                        path.Close();
                        path.FillType = (gc.GetFillType());
                        paint.Style = XPaintStyle.Fill;
                        canvas.DrawPath(path, paint);
                        changed = true;
                    }

                    break;
                case RequestCode.PolyArc:
                case RequestCode.PolyFillArc:
                    if ((bytesRemaining % 12) != 0)
                    {
                        io.ReadSkip(bytesRemaining);
                        ErrorCode.Write(client, ErrorCode.Length, opcode, 0);
                    }
                    else
                    {
                        var useCenter = false;

                        if (opcode == RequestCode.PolyArc)
                        {
                            paint.Style = XPaintStyle.Stroke;
                        }
                        else
                        {
                            paint.Style = XPaintStyle.Fill;
                            if (gc.GetArcMode() == 1) // Pie slice.
                                useCenter = true;
                        }

                        while (bytesRemaining > 0)
                        {
                            var x = (short) io.ReadShort();
                            var y = (short) io.ReadShort();
                            var width = io.ReadShort();
                            var height = io.ReadShort();
                            var angle1 = (short) io.ReadShort();
                            var angle2 = (short) io.ReadShort();
                            var r = new Rect(x, y, x + width, y + height);

                            bytesRemaining -= 12;
                            canvas.DrawArc(r, angle1 / -64.0f, angle2 / -64.0f, useCenter, paint);
                            changed = true;
                        }
                    }

                    break;
                case RequestCode.PutImage:
                    changed = ProcessPutImage(client, gc, arg, bytesRemaining);
                    break;
                case RequestCode.PolyText8:
                case RequestCode.PolyText16:
                    changed = ProcessPolyText(client, gc, opcode, bytesRemaining);
                    break;
                case RequestCode.ImageText8:
                    if (bytesRemaining != 4 + arg + (-arg & 3))
                    {
                        io.ReadSkip(bytesRemaining);
                        ErrorCode.Write(client, ErrorCode.Length, opcode, 0);
                    }
                    else
                    {
                        int x = (short) io.ReadShort();
                        int y = (short) io.ReadShort();
                        var pad = -arg & 3;
                        var bytes = new byte[arg];

                        io.ReadBytes(bytes, 0, arg);
                        io.ReadSkip(pad);
                        DrawImageText(bytes.GetString(), x, y, gc);
                        changed = true;
                    }

                    break;
                case RequestCode.ImageText16:
                    if (bytesRemaining != 4 + 2 * arg + (-(2 * arg) & 3))
                    {
                        io.ReadSkip(bytesRemaining);
                        ErrorCode.Write(client, ErrorCode.Length, opcode, 0);
                    }
                    else
                    {
                        int x = (short) io.ReadShort();
                        int y = (short) io.ReadShort();
                        var pad = (-2 * arg) & 3;
                        var chars = new char[arg];

                        for (var i = 0; i < arg; i++)
                        {
                            var b1 = io.ReadByte();
                            var b2 = io.ReadByte();

                            chars[i] = (char) ((b1 << 8) | b2);
                        }

                        io.ReadSkip(pad);
                        DrawImageText(new string(chars), x, y, gc);
                        changed = true;
                    }

                    break;
                default:
                    io.ReadSkip(bytesRemaining);
                    ErrorCode.Write(client, ErrorCode.Implementation, opcode, 0);
                    break;
            }

            if (depth == 1)
                paint.Color = (originalColor);

            canvas.Restore(); // Undo any clip rectangles.

            return changed;
        }

        /**
         * Process a PutImage request.
         *
         * @param client	The remote client.
         * @param gc	The GContext to use for drawing.
         * @param bytesRemaining	Bytes yet to be read in the request.
         * @return	True if the drawable is modified.

         */
        private bool ProcessPutImage(Client client, GContext gc, byte format, int bytesRemaining)
        {
            var io = client.GetInputOutput();

            if (bytesRemaining < 12)
            {
                io.ReadSkip(bytesRemaining);
                ErrorCode.Write(client, ErrorCode.Length, RequestCode.PutImage, 0);
                return false;
            }

            var width = io.ReadShort();
            var height = io.ReadShort();
            float dstX = (short) io.ReadShort();
            float dstY = (short) io.ReadShort();
            var leftPad = io.ReadByte();
            var depth = io.ReadByte();
            int n, pad, rightPad;

            io.ReadSkip(2); // Unused.
            bytesRemaining -= 12;

            var badMatch = false;

            if (format == BitmapFormat)
            {
                if (depth != 1)
                    badMatch = true;
            }
            else if (format == XyPixmapFormat)
            {
                if (depth != this.depth)
                    badMatch = true;
            }
            else if (format == ZPixmapFormat)
            {
                if (depth != this.depth || leftPad != 0)
                    badMatch = true;
            }
            else
            {
                // Invalid format.
                badMatch = true;
            }

            if (badMatch)
            {
                io.ReadSkip(bytesRemaining);
                ErrorCode.Write(client, ErrorCode.Match, RequestCode.PutImage, 0);
                return false;
            }

            var isShapeMask = false;

            if (format == ZPixmapFormat)
            {
                rightPad = 0;
                if (depth == 32)
                {
                    n = 3 * width * height;
                }
                else
                {
                    n = (width * height + 7) / 8;
                    if (bytesRemaining != n + (-n & 3))
                    {
                        isShapeMask = true;
                        n = (width + 1) / 2 * height;
                    }
                }
            }
            else
            {
                // XYPixmap or IXBitmap.
                rightPad = -(width + leftPad) & 7;
                n = ((width + leftPad + rightPad) * height * depth + 7) / 8;
            }

            pad = -n & 3;

            if (bytesRemaining != n + pad)
            {
                io.ReadSkip(bytesRemaining);
                ErrorCode.Write(client, ErrorCode.Length, RequestCode.PutImage, 0);
                return false;
            }

            int[] colors;

            try
            {
                colors = new int[width * height];
            }
            catch (OutOfMemoryException)
            {
                ErrorCode.Write(client, ErrorCode.Alloc, RequestCode.PutImage, 0);
                return false;
            }

            if (format == BitmapFormat)
            {
                var fg = gc.GetForegroundColor();
                var bg = gc.GetBackgroundColor();
                var offset = 0;
                var count = 0;
                var x = 0;
                var y = 0;
                var mask = 128;
                var val = 0;

                for (;;)
                {
                    if ((count++ & 7) == 0)
                    {
                        val = io.ReadByte();
                        mask = 128;
                    }

                    if (x >= leftPad && x < leftPad + width)
                        colors[offset++] = ((val & mask) == 0) ? bg : fg;

                    mask >>= 1;
                    if (++x == leftPad + width + rightPad)
                    {
                        x = 0;
                        if (++y == height)
                            break;
                    }
                }
            }
            else if (format == XyPixmapFormat)
            {
                var planeBit = 1 << (depth - 1);

                for (var i = 0; i < depth; i++)
                {
                    var offset = 0;
                    var count = 0;
                    var x = 0;
                    var y = 0;
                    var mask = 128;
                    var val = 0;

                    for (;;)
                    {
                        if ((count++ & 7) == 0)
                        {
                            val = io.ReadByte();
                            mask = 128;
                        }

                        if (x >= leftPad && x < leftPad + width)
                            colors[offset++] |= ((val & mask) == 0) ? 0 : planeBit;

                        mask >>= 1;
                        if (++x == leftPad + width + rightPad)
                        {
                            x = 0;
                            if (++y == height)
                                break;
                        }
                    }

                    planeBit >>= 1;
                }
            }
            else if (depth == 32)
            {
                // 32-bit ZPixmap.
                var useShapeMask = (shapeMask != null && colors.Length == shapeMask.Length);

                for (var i = 0; i < colors.Length; i++)
                {
                    var b = io.ReadByte();
                    var g = io.ReadByte();
                    var r = io.ReadByte();
                    var alpha = (useShapeMask && !shapeMask[i]) ? 0 : 0xff000000.AsInt();

                    colors[i] = alpha | (r << 16) | (g << 8) | b;
                }

                if (useShapeMask)
                    shapeMask = null;
            }
            else if (isShapeMask)
            {
                // ZPixmap, depth = 1, shape mask.
                shapeMask = new bool[colors.Length];
                io.ReadShapeMask(shapeMask, width, height);
                io.ReadSkip(pad);

                return false; // Don't redraw.
            }
            else
            {
                // ZPixmap with depth = 1.
                var fg = gc.GetForegroundColor();
                var bg = gc.GetBackgroundColor();
                var bits = new bool[colors.Length];

                io.ReadBits(bits, 0, colors.Length);

                for (var i = 0; i < colors.Length; i++)
                    colors[i] = bits[i] ? fg : bg;
            }

            io.ReadSkip(pad);
            canvas.DrawBitmap(colors, 0, width, dstX, dstY, width, height, true, gc.GetPaint());

            return true;
        }

        /**
         * Process a PolyText8 or PolyText16 request.
         *
         * @param client	The remote client.
         * @param gc	The GContext to use for drawing.
         * @param opcode	The request's opcode.
         * @param bytesRemaining	Bytes yet to be read in the request.
         * @return	True if the drawable is modified.

         */
        private bool ProcessPolyText(Client client, GContext gc, byte opcode, int bytesRemaining)
        {
            var io = client.GetInputOutput();

            if (bytesRemaining < 4)
            {
                io.ReadSkip(bytesRemaining);
                ErrorCode.Write(client, ErrorCode.Length, opcode, 0);
                return false;
            }

            float x = (short) io.ReadShort();
            float y = (short) io.ReadShort();

            bytesRemaining -= 4;
            while (bytesRemaining > 1)
            {
                var length = io.ReadByte();
                int minBytes;

                bytesRemaining--;
                if (length == 255) // Font change indicator.
                    minBytes = 4;
                else if (opcode == RequestCode.PolyText8)
                    minBytes = 1 + length;
                else
                    minBytes = 1 + length * 2;

                if (bytesRemaining < minBytes)
                {
                    io.ReadSkip(bytesRemaining);
                    ErrorCode.Write(client, ErrorCode.Length, opcode, 0);
                    return false;
                }

                if (length == 255)
                {
                    // Font change indicator.
                    var fid = 0;

                    for (var i = 0; i < 4; i++)
                        fid = (fid << 8) | io.ReadByte();

                    bytesRemaining -= 4;
                    if (!gc.SetFont(fid))
                        ErrorCode.Write(client, ErrorCode.Font, opcode, fid);
                }
                else
                {
                    // It's a string.
                    var delta = io.ReadByte();
                    string s;

                    bytesRemaining--;
                    if (opcode == RequestCode.PolyText8)
                    {
                        var bytes = new byte[length];

                        io.ReadBytes(bytes, 0, length);
                        bytesRemaining -= length;
                        s = bytes.GetString();
                    }
                    else
                    {
                        var chars = new char[length];

                        for (var i = 0; i < length; i++)
                        {
                            var b1 = io.ReadByte();
                            var b2 = io.ReadByte();

                            chars[i] = (char) ((b1 << 8) | b2);
                        }

                        bytesRemaining -= length * 2;
                        s = new string(chars);
                    }

                    var paint = gc.GetPaint();

                    x += delta;
                    canvas.DrawText(s, x, y, paint);
                    x += paint.MeasureText(s);
                }
            }

            io.ReadSkip(bytesRemaining);

            return true;
        }
    }
}