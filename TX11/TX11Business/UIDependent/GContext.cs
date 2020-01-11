using TX11Business.Compatibility;
using TX11Shared;
using TX11Shared.Graphics;

namespace TX11Business.UIDependent
{
    /// <summary>
    /// A Graphics Context, that is:
    /// "Various information for graphics output is stored in a graphics context such as foreground pixel,
    /// background pixel, line width, clipping region, and so on.
    /// A graphics context can only be used with drawables that have the same root and the same depth
    /// as the graphics context. "
    /// <para>https://www.x.org/releases/X11R7.7/doc/xproto/x11protocol.html#glossary:Graphics_context</para>
    /// </summary>
    /// <seealso cref="TX11Business.Resource" />
    internal sealed class GContext : Resource
    {
        private readonly IXPaint paint;
        private Font font;
        private XPathFillType fillType;
        private readonly int[] attributes;
        private Rect[] clipRectangles;
        private int foregroundColor = 0xff000000.AsInt();
        private int backgroundColor = 0xffffffff.AsInt();

        private const int Function = 0;
        private const int PlaneMask = 1;
        private const int Foreground = 2;
        private const int Background = 3;
        private const int LineWidth = 4;
        private const int LineStyle = 5;
        private const int CapStyle = 6;
        private const int JoinStyle = 7;
        private const int FillStyle = 8;
        private const int FillRule = 9;
        private const int Tile = 10;
        private const int Stipple = 11;
        private const int TileStippleXOrigin = 12;
        private const int TileStippleYOrigin = 13;
        private const int Font = 14;
        private const int SubwindowMode = 15;
        private const int GraphicsExposures = 16;
        private const int ClipXOrigin = 17;
        private const int ClipYOrigin = 18;
        private const int ClipMask = 19;
        private const int DashOffset = 20;
        private const int Dashes = 21;
        private const int ArcMode = 22;

        /**
         * Constructor.
         *
         * @param id		The graphics context's ID.
         * @param xServer	The X server.
         * @param client	The client issuing the request.
         */
        internal GContext(int id, XServer xServer, Client client) : base(AttrGcontext, id, xServer, client)
        {
            this.paint = Util.GetPaint();
            this.attributes = new int[]
            {
                3, // function = Copy
                0xffffffff.AsInt(), // plane-mask = all ones
                0, // foreground = 0
                1, // background = 1
                0, // line-width = 0
                0, // line-style = Solid
                1, // cap-style = Butt
                0, // join-style = Miter
                0, // fill-style = Solid
                0, // fill-rule = EvenOdd
                0, // tile = foreground-filled pixmap
                0, // stipple = pixmap filled with ones
                0, // tile-stipple-x-origin = 0
                0, // tile-stipple-y-origin = 0
                0, // font = server-dependent
                0, // subwindow-mode = ClipByChildren
                1, // graphics-exposures = True
                0, // clip-x-origin = 0
                0, // clip-y-origin = 0
                0, // clip-mask = None
                0, // dash-offset = 0
                4, // dashes = 4 (i.e. the list [4,4])
                1 // arc-mode = PieSlice
            };
        }

        /**
         * Return the GContext's Paint handle.
         *
         * @return	The GContext's Paint handle.
         */
        internal IXPaint GetPaint()
        {
            return this.paint;
        }

        /**
         * Return the GContext's background color.
         *
         * @return	The GContext's background color.
         */
        internal int GetBackgroundColor()
        {
            return this.backgroundColor;
        }

        /**
         * Return the GContext's foreground color.
         *
         * @return	The GContext's foreground color.
         */
        internal int GetForegroundColor()
        {
            return this.foregroundColor;
        }

        /**
         * Return the fill type.
         *
         * @return	The fill type.
         */
        internal XPathFillType GetFillType()
        {
            return this.fillType;
        }

        /**
         * Return the arc mode.
         * 0 = chord, 1 = pie slice.
         *
         * @return	The arc mode.
         */
        internal int GetArcMode()
        {
            return this.attributes[ArcMode];
        }

        /**
         * Return whether to generate graphics exposure events.
         *
         * @return	Whether to generate graphics exposure events.
         */
        internal bool GetGraphicsExposure()
        {
            return (this.attributes[GraphicsExposures] != 0);
        }

        /**
         * Return the GContext's font.
         *
         * @return	The GContext's font.
         */
        internal Font GetFont()
        {
            return this.font;
        }

        /**
         * Set the GContext's font.
         *
         * @param id	The ID of the font.
         * @return	True if the ID refers to a valid font.
         */
        internal bool SetFont(int id)
        {
            var r = this.XServer.GetResource(id);

            if (r == null || r.GetRessourceType() != Resource.AttrFont)
                return false;

            this.font = (Font) r;
            this.paint.Typeface = (this.font.GetTypeface());
            this.paint.TextSize = (this.font.GetSize());

            return true;
        }

        /**
         * Apply the GContext's clip rectangles to the canvas.
         *
         * @param canvas	The canvas to apply the rectangles to.
         */
        internal void ApplyClipRectangles(IXCanvas canvas)
        {
            if (this.clipRectangles == null)
                return;

            if (this.clipRectangles.Length == 0)
                canvas.ClipRect(0, 0, 0, 0);
            else
                foreach (var r in this.clipRectangles)
                    canvas.ClipRect(r, XRegionOperation.Union);
        }

        /**
         * Process an X request relating to this graphics context.
         *
         * @param client	The remote client.
         * @param opcode	The request's opcode.
         * @param arg		Optional first argument.
         * @param bytesRemaining	Bytes yet to be read in the request.
         * @
         */
        internal override void ProcessRequest(Client client, byte opcode, byte arg, int bytesRemaining)
        {
            var io = client.GetInputOutput();

            switch (opcode)
            {
                case RequestCode.QueryFont:
                case RequestCode.QueryTextExtents:
                    this.font.ProcessRequest(client, opcode, arg, bytesRemaining);
                    return;
                case RequestCode.AttrChangeGc:
                    ProcessValues(client, opcode, bytesRemaining);
                    break;
                case RequestCode.AttrCopyGc:
                    if (bytesRemaining != 8)
                    {
                        io.ReadSkip(bytesRemaining);
                        ErrorCode.Write(client, ErrorCode.Length, opcode, 0);
                    }
                    else
                    {
                        var id = io.ReadInt(); // Destination GContext.
                        var mask = io.ReadInt(); // Value mask.
                        var r = this.XServer.GetResource(id);

                        if (r == null || r.GetRessourceType() != Resource.AttrGcontext)
                        {
                            ErrorCode.Write(client, ErrorCode.GContext, opcode, id);
                        }
                        else
                        {
                            var gc = (GContext) r;

                            for (var i = 0; i < 23; i++)
                                if ((mask & (1 << i)) != 0)
                                    gc.attributes[i] = this.attributes[i];

                            gc.ApplyValues(null, opcode);
                        }
                    }

                    break;
                case RequestCode.SetDashes:
                    if (bytesRemaining < 4)
                    {
                        io.ReadSkip(bytesRemaining);
                        ErrorCode.Write(client, ErrorCode.Length, opcode, 0);
                    }
                    else
                    {
                        io.ReadShort(); // Dash offset.

                        var n = io.ReadShort(); // Length of dashes.
                        var pad = -n & 3;

                        bytesRemaining -= 4;
                        if (bytesRemaining != n + pad)
                            ErrorCode.Write(client, ErrorCode.Length, opcode, 0);

                        io.ReadSkip(n + pad); // Ignore the dash information.
                    }

                    break; //Original code had fallthrough, seems like a bug
                case RequestCode.SetClipRectangles:
                    if (bytesRemaining < 4)
                    {
                        io.ReadSkip(bytesRemaining);
                        ErrorCode.Write(client, ErrorCode.Length, opcode, 0);
                    }
                    else
                    {
                        int clipXOrigin = (short) io.ReadShort();
                        int clipYOrigin = (short) io.ReadShort();

                        bytesRemaining -= 4;
                        if ((bytesRemaining & 7) != 0)
                        {
                            io.ReadSkip(bytesRemaining);
                            ErrorCode.Write(client, ErrorCode.Length, opcode, 0);
                        }
                        else
                        {
                            var i = 0;

                            this.clipRectangles = new Rect[bytesRemaining / 8];
                            while (bytesRemaining > 0)
                            {
                                int x = (short) io.ReadShort();
                                int y = (short) io.ReadShort();
                                var width = io.ReadShort();
                                var height = io.ReadShort();

                                bytesRemaining -= 8;
                                this.clipRectangles[i++] = new Rect(x + clipXOrigin, y + clipYOrigin,
                                                               x + clipXOrigin + width, y + clipYOrigin + height);
                            }
                        }
                    }

                    break;
                case RequestCode.AttrFreeGc:
                    if (bytesRemaining != 0)
                    {
                        io.ReadSkip(bytesRemaining);
                        ErrorCode.Write(client, ErrorCode.Length, opcode, 0);
                    }
                    else
                    {
                        this.XServer.FreeResource(this.Id);
                        if (this.Client != null)
                            this.Client.FreeResource(this);
                    }

                    break; //Original code had fallthrough, seems like a bug
                default:
                    io.ReadSkip(bytesRemaining);
                    ErrorCode.Write(client, ErrorCode.Implementation, opcode, 0);
                    break;
            }
        }

        /// <inheritdoc />
        public override void Dispose()
        {
            this.paint?.Dispose();
        }

        /**
         * Process a CreateGC request.
         *
         * @param xServer	The X server.
         * @param client	The client issuing the request.
         * @param id	The ID of the GContext to create.
         * @param bytesRemaining	Bytes yet to be read in the request.
         * @
         */
        internal static void ProcessCreateGcRequest(XServer xServer, Client client, int id, int bytesRemaining)
        {
            var gc = new GContext(id, xServer, client);

            if (gc.ProcessValues(client, RequestCode.AttrCreateGc, bytesRemaining))
            {
                xServer.AddResource(gc);
                client.AddResource(gc);
            }
        }

        /**
         * Process a list of GContext attribute values.
         *
         * @param client	The remote client.
         * @param opcode	The opcode being processed.
         * @param bytesRemaining	Bytes yet to be read in the request.
         * @return	True if the values are all valid.
         * @
         */
        private bool ProcessValues(Client client, byte opcode, int bytesRemaining)
        {
            var io = client.GetInputOutput();

            if (bytesRemaining < 4)
            {
                io.ReadSkip(bytesRemaining);
                ErrorCode.Write(client, ErrorCode.Length, opcode, 0);
                return false;
            }

            var valueMask = io.ReadInt(); // Value mask.
            var n = Util.Bitcount(valueMask);

            bytesRemaining -= 4;
            if (bytesRemaining != n * 4)
            {
                io.ReadSkip(bytesRemaining);
                ErrorCode.Write(client, ErrorCode.Length, opcode, 0);
                return false;
            }

            for (var i = 0; i < 23; i++)
                if ((valueMask & (1 << i)) != 0)
                    ProcessValue(io, i);

            return ApplyValues(client, opcode);
        }

        /**
         * Process a single GContext attribute value.
         *
         * @param io	The input/output stream.
         * @param maskBit	The mask bit of the attribute.
         * @
         */
        private void ProcessValue(InputOutput io, int maskBit)
        {
            switch (maskBit)
            {
                case Function:
                case LineStyle:
                case CapStyle:
                case JoinStyle:
                case FillStyle:
                case FillRule:
                case SubwindowMode:
                case GraphicsExposures:
                case Dashes:
                case ArcMode:
                    this.attributes[maskBit] = io.ReadByte();
                    io.ReadSkip(3);
                    break;
                case PlaneMask:
                case Foreground:
                case Background:
                case Tile:
                case Stipple:
                case Font:
                case ClipMask:
                    this.attributes[maskBit] = io.ReadInt();
                    break;
                case LineWidth:
                case DashOffset:
                    this.attributes[maskBit] = io.ReadShort();
                    io.ReadSkip(2);
                    break;
                case TileStippleXOrigin:
                case TileStippleYOrigin:
                case ClipXOrigin:
                case ClipYOrigin:
                    this.attributes[maskBit] = (short) io.ReadShort();
                    io.ReadSkip(2);
                    break;
            }
        }

        /**
         * Apply the attribute values to the Paint.
         *
         * @param client	The remote client.
         * @param opcode	The opcode being processed.
         * @return	True if the values are all valid.
         * @
         */
        private bool ApplyValues(Client client, byte opcode)
        {
            var ok = true;

            this.foregroundColor = (this.attributes[Foreground] | 0xff000000).AsInt();
            this.backgroundColor = (this.attributes[Background] | 0xff000000).AsInt();

            this.paint.Color = (this.foregroundColor);
            this.paint.StrokeWidth = (this.attributes[LineWidth]);

            if (this.attributes[Function] == 6) // XOR.
            {
                //_paint.setXfermode (new PixelXorXfermode(0xffffffff));
                this.paint.XferMode = XPixelXferMode.XOr;
            }
            else
            {
                this.paint.XferMode = XPixelXferMode.None;
            }

            switch (this.attributes[CapStyle])
            {
                case 0: // NotLast
                case 1: // Butt
                    this.paint.StrokeCap = XStrokeCap.Butt;
                    break;
                case 2: // Round
                    this.paint.StrokeCap = XStrokeCap.Round;
                    break;
                case 3: // Projecting
                    this.paint.StrokeCap = XStrokeCap.Square;
                    break;
            }

            switch (this.attributes[JoinStyle])
            {
                case 0: // Miter
                    this.paint.StrokeJoin = XStrokeJoin.Miter;
                    break;
                case 1: // Round
                    this.paint.StrokeJoin = XStrokeJoin.Round;
                    break;
                case 2: // Bevel
                    this.paint.StrokeJoin = XStrokeJoin.Bevel;
                    break;
            }

            if (this.attributes[FillRule] == 1) // Winding.
                this.fillType = XPathFillType.Winding;
            else // Defaults to even-odd.
                this.fillType = XPathFillType.EvenOdd;

            var fid = this.attributes[Font];

            if (this.font == null || fid == 0)
                this.font = this.XServer.GetDefaultFont();

            if (fid != 0 && !SetFont(fid))
            {
                ok = false;
                ErrorCode.Write(client, ErrorCode.Font, opcode, fid);
            }

            return ok;
        }
    }
}