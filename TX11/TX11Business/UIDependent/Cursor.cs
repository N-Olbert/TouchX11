using JetBrains.Annotations;
using TX11Business.Compatibility;
using TX11Shared.Graphics;

namespace TX11Business.UIDependent
{
    internal sealed class Cursor : Resource
    {
        private readonly int hotspotX;
        private readonly int hotspotY;
        private IXBitmap bitmap;
        private int foregroundColor;
        private int backgroundColor;

        [NotNull, ItemNotNull]
        private static readonly object[][] Glyphs = new[]
        {
            new object[] {"xc_x_cursor", 7, 7},
            new object[] {"xc_arrow", 14, 1},
            new object[] {"xc_based_arrow_down", 4, 10},
            new object[] {"xc_based_arrow_up", 4, 10},
            new object[] {"xc_boat", 14, 4},
            new object[] {"xc_bogosity", 7, 7},
            new object[] {"xc_bottom_left_corner", 1, 14},
            new object[] {"xc_bottom_right_corner", 14, 14},
            new object[] {"xc_bottom_side", 7, 14},
            new object[] {"xc_bottom_tee", 8, 10},
            new object[] {"xc_box_spiral", 8, 8},
            new object[] {"xc_center_ptr", 5, 1},
            new object[] {"xc_circle", 8, 8},
            new object[] {"xc_clock", 6, 3},
            new object[] {"xc_coffee_mug", 7, 9},
            new object[] {"xc_cross", 7, 7},
            new object[] {"xc_cross_reverse", 7, 7},
            new object[] {"xc_crosshair", 7, 7},
            new object[] {"xc_diamond_cross", 7, 7},
            new object[] {"xc_dot", 6, 6},
            new object[] {"xc_dotbox", 7, 6},
            new object[] {"xc_double_arrow", 6, 8},
            new object[] {"xc_draft_large", 14, 0},
            new object[] {"xc_draft_small", 14, 0},
            new object[] {"xc_draped_box", 7, 6},
            new object[] {"xc_exchange", 7, 7},
            new object[] {"xc_fleur", 8, 8},
            new object[] {"xc_gobbler", 14, 3},
            new object[] {"xc_gumby", 2, 0},
            new object[] {"xc_hand1", 12, 0},
            new object[] {"xc_hand2", 0, 1},
            new object[] {"xc_heart", 6, 8},
            new object[] {"xc_icon", 8, 8},
            new object[] {"xc_iron_cross", 8, 7},
            new object[] {"xc_left_ptr", 1, 1},
            new object[] {"xc_left_side", 1, 7},
            new object[] {"xc_left_tee", 1, 8},
            new object[] {"xc_leftbutton", 8, 8},
            new object[] {"xc_ll_angle", 1, 10},
            new object[] {"xc_lr_angle", 10, 10},
            new object[] {"xc_man", 14, 5},
            new object[] {"xc_middlebutton", 8, 8},
            new object[] {"xc_mouse", 4, 1},
            new object[] {"xc_pencil", 11, 15},
            new object[] {"xc_pirate", 7, 12},
            new object[] {"xc_plus", 5, 6},
            new object[] {"xc_question_arrow", 5, 8},
            new object[] {"xc_right_ptr", 8, 1},
            new object[] {"xc_right_side", 14, 7},
            new object[] {"xc_right_tee", 10, 8},
            new object[] {"xc_rightbutton", 8, 8},
            new object[] {"xc_rtl_logo", 7, 7},
            new object[] {"xc_sailboat", 8, 0},
            new object[] {"xc_sb_down_arrow", 4, 15},
            new object[] {"xc_sb_h_double_arrow", 7, 4},
            new object[] {"xc_sb_left_arrow", 0, 4},
            new object[] {"xc_sb_right_arrow", 15, 4},
            new object[] {"xc_sb_up_arrow", 4, 0},
            new object[] {"xc_sb_v_double_arrow", 4, 7},
            new object[] {"xc_shuttle", 11, 0},
            new object[] {"xc_sizing", 8, 8},
            new object[] {"xc_spider", 6, 7},
            new object[] {"xc_spraycan", 10, 2},
            new object[] {"xc_star", 7, 7},
            new object[] {"xc_target", 7, 7},
            new object[] {"xc_tcross", 7, 7},
            new object[] {"xc_top_left_arrow", 1, 1},
            new object[] {"xc_top_left_corner", 1, 1},
            new object[] {"xc_top_right_corner", 14, 1},
            new object[] {"xc_top_side", 7, 1},
            new object[] {"xc_top_tee", 8, 1},
            new object[] {"xc_trek", 4, 0},
            new object[] {"xc_ul_angle", 1, 1},
            new object[] {"xc_umbrella", 8, 2},
            new object[] {"xc_ur_angle", 10, 1},
            new object[] {"xc_watch", 15, 9},
            new object[] {"xc_xterm", 4, 8}
        };

        /**
         * Constructor for a pixmap cursor.
         *
         * @param id	The server cursor ID.
         * @param xServer	The X server.
         * @param client	The client issuing the request.
         * @param p	Cursor pixmap.
         * @param mp	Mask pixmap. May be null.
         * @param x	Hotspot X coordinate.
         * @param y	Hotspot Y coordinate.
         * @param foregroundColor	Foreground color of the cursor.
         * @param backgroundColor	Foreground color of the cursor.
         */
        internal Cursor(int id, XServer xServer, Client client, Pixmap p, Pixmap mp, int x, int y, int foregroundColor,
            int backgroundColor) : base(AttrCursor, id, xServer, client)
        {
            hotspotX = x;
            hotspotY = y;
            this.foregroundColor = foregroundColor;
            this.backgroundColor = backgroundColor;

            var bm = p.GetDrawable().GetBitmap();
            var width = bm.Width;
            var height = bm.Height;
            var pixels = new int[width * height];

            bm.GetPixels(pixels, 0, width, 0, 0, width, height);
            if (mp == null)
            {
                for (var i = 0; i < pixels.Length; i++)
                {
                    if (pixels[i] == 0xffffffff.AsInt())
                        pixels[i] = foregroundColor;
                    else
                        pixels[i] = backgroundColor;
                }
            }
            else
            {
                var mbm = mp.GetDrawable().GetBitmap();
                var mask = new int[width * height];

                mbm.GetPixels(mask, 0, width, 0, 0, width, height);
                for (var i = 0; i < pixels.Length; i++)
                {
                    if (mask[i] != 0xffffffff.AsInt())
                        pixels[i] = 0;
                    else if (pixels[i] == 0xffffffff.AsInt())
                        pixels[i] = foregroundColor;
                    else
                        pixels[i] = backgroundColor;
                }
            }

            bitmap = Util.BitmapFactory.CreateBitmap(pixels, width, height);
        }

        /**
         * Constructor for a glyph cursor.
         * This functions just assumes the caller wants one of the 77 predefined
         * cursors from the "cursor" font, so the sourceFont, maskFont, and
         * maskChar are all ignored.
         *
         * @param id	The server cursor ID.
         * @param xServer	The X server.
         * @param client	The client issuing the request.
         * @param sourceFont	Font to use for the cursor character.
         * @param maskFont	Font for the mask character. May be null.
         * @param sourceChar	Character to use as the cursor.
         * @param maskChar	Character to use as the mask.
         * @param foregroundColor	Foreground color of the cursor.
         * @param backgroundColor	Foreground color of the cursor.
         */
        internal Cursor(int id, XServer xServer, Client client, Font sourceFont, Font maskFont, int sourceChar,
            int maskChar, int foregroundColor, int backgroundColor) : base(AttrCursor, id, xServer, client)
        {
            sourceChar /= 2;
            if (sourceChar < 0 || sourceChar >= Glyphs.Length)
                sourceChar = 0;

            if (maskChar == 32)
            {
                bitmap = Util.BitmapFactory.CreateBitmap(16, 16);
                bitmap.EraseColor(0);
            }
            else
            {
                bitmap = Util.BitmapFactory.DecodeResource("TX11Ressources.Images." + Glyphs[sourceChar][0] + ".png");
            }

            this.foregroundColor = 0xff000000.AsInt();
            this.backgroundColor = 0xffffffff.AsInt();
            SetColor(foregroundColor, backgroundColor);

            hotspotX = (int) Glyphs[sourceChar][1];
            hotspotY = (int) Glyphs[sourceChar][2];
        }

        /**
         * Return the cursor's bitmap.
         *
         * @return	The cursor's bitmap.
         */
        internal IXBitmap GetBitmap()
        {
            return bitmap;
        }

        /**
         * Return the X coordinate of the cursor's hotspot.
         *
         * @return	The X coordinate of the cursor's hotspot.
         */
        internal int GetHotspotX()
        {
            return hotspotX;
        }

        /**
         * Return the Y coordinate of the cursor's hotspot.
         *
         * @return	The Y coordinate of the cursor's hotspot.
         */
        internal int GetHotspotY()
        {
            return hotspotY;
        }

        /**
         * Set the foreground and background colors of the cursor.
         *
         * @param fg	Foreground color.
         * @param bg	Background color.
         */
        private void SetColor(int fg, int bg)
        {
            if (fg == foregroundColor && bg == backgroundColor)
                return;

            var width = bitmap.Width;
            var height = bitmap.Height;
            var pixels = new int[width * height];

            bitmap.GetPixels(pixels, 0, width, 0, 0, width, height);
            for (var i = 0; i < pixels.Length; i++)
            {
                var pix = pixels[i];

                if (pix == foregroundColor)
                    pixels[i] = fg;
                else if (pix == backgroundColor)
                    pixels[i] = bg;
            }

            bitmap = Util.BitmapFactory.CreateBitmap(pixels, width, height);
            foregroundColor = fg;
            backgroundColor = bg;
        }

        /**
         * Process an X request relating to this cursor.
         *
         * @param client	The remote client.
         * @param opcode	The request's opcode.
         * @param arg		Optional first argument.
         * @param bytesRemaining	Bytes yet to be read in the request.

         */
        internal override void ProcessRequest(Client client, byte opcode, byte arg, int bytesRemaining)
        {
            var io = client.GetInputOutput();

            switch (opcode)
            {
                case RequestCode.FreeCursor:
                    if (bytesRemaining != 0)
                    {
                        io.ReadSkip(bytesRemaining);
                        ErrorCode.Write(client, ErrorCode.Length, opcode, 0);
                    }
                    else
                    {
                        XServer.FreeResource(Id);
                        if (Client != null)
                            Client.FreeResource(this);
                    }

                    break;
                case RequestCode.RecolorCursor:
                    if (bytesRemaining != 12)
                    {
                        io.ReadSkip(bytesRemaining);
                        ErrorCode.Write(client, ErrorCode.Length, opcode, 0);
                    }
                    else
                    {
                        var fgRed = io.ReadShort();
                        var fgGreen = io.ReadShort();
                        var fgBlue = io.ReadShort();
                        var bgRed = io.ReadShort();
                        var bgGreen = io.ReadShort();
                        var bgBlue = io.ReadShort();

                        SetColor(Colormap.FromParts16(fgRed, fgGreen, fgBlue),
                                 Colormap.FromParts16(bgRed, bgGreen, bgBlue));
                    }

                    break;
                default:
                    io.ReadSkip(bytesRemaining);
                    ErrorCode.Write(client, ErrorCode.Implementation, opcode, Id);
                    break;
            }
        }

        /// <inheritdoc />
        public override void Dispose()
        {
            bitmap?.Dispose();
        }

        /**
         * Process a create request.
         *
         * @param xServer	The X server.
         * @param client	The client issuing the request.
         * @param opcode	The request opcode.
         * @param id	The ID of the cursor to create.
         * @param bytesRemaining	Bytes yet to be read in the request.

         */
        internal static void ProcessCreateRequest(XServer xServer, Client client, byte opcode, int id,
            int bytesRemaining)
        {
            var io = client.GetInputOutput();

            if (opcode == RequestCode.CreateCursor)
            {
                var sid = io.ReadInt(); // Source pixmap ID.
                var mid = io.ReadInt(); // Mask pixmap ID.
                var fgRed = io.ReadShort();
                var fgGreen = io.ReadShort();
                var fgBlue = io.ReadShort();
                var bgRed = io.ReadShort();
                var bgGreen = io.ReadShort();
                var bgBlue = io.ReadShort();
                var x = (short) io.ReadShort();
                var y = (short) io.ReadShort();
                var r = xServer.GetResource(sid);
                Resource mr = null;

                if (r == null || r.GetRessourceType() != Resource.AttrPixmap)
                {
                    ErrorCode.Write(client, ErrorCode.Pixmap, opcode, sid);
                    return;
                }
                else if (mid != 0)
                {
                    mr = xServer.GetResource(mid);
                    if (mr == null || mr.GetRessourceType() != Resource.AttrPixmap)
                    {
                        ErrorCode.Write(client, ErrorCode.Pixmap, opcode, mid);
                        return;
                    }
                }

                var p = (Pixmap) r;
                var mp = (Pixmap) mr;

                if (p.GetDepth() != 1)
                {
                    ErrorCode.Write(client, ErrorCode.Match, opcode, sid);
                    return;
                }
                else if (mp != null)
                {
                    if (mp.GetDepth() != 1)
                    {
                        ErrorCode.Write(client, ErrorCode.Match, opcode, mid);
                        return;
                    }

                    var bm1 = p.GetDrawable().GetBitmap();
                    var bm2 = mp.GetDrawable().GetBitmap();

                    if (bm1.Width != bm2.Width || bm1.Height != bm2.Height)
                    {
                        ErrorCode.Write(client, ErrorCode.Match, opcode, mid);
                        return;
                    }
                }

                var fg = Colormap.FromParts16(fgRed, fgGreen, fgBlue);
                var bg = Colormap.FromParts16(bgRed, bgGreen, bgBlue);
                var c = new Cursor(id, xServer, client, p, mp, x, y, fg, bg);

                xServer.AddResource(c);
                client.AddResource(c);
            }
            else if (opcode == RequestCode.CreateGlyphCursor)
            {
                var sid = io.ReadInt(); // Source font ID.
                var mid = io.ReadInt(); // Mask font ID.
                var sourceChar = io.ReadShort(); // Source char.
                var maskChar = io.ReadShort(); // Mask char.
                var fgRed = io.ReadShort();
                var fgGreen = io.ReadShort();
                var fgBlue = io.ReadShort();
                var bgRed = io.ReadShort();
                var bgGreen = io.ReadShort();
                var bgBlue = io.ReadShort();
                var r = xServer.GetResource(sid);
                Resource mr = null;

                if (r == null || r.GetRessourceType() != Resource.AttrFont)
                {
                    ErrorCode.Write(client, ErrorCode.Font, opcode, sid);
                    return;
                }
                else if (mid != 0)
                {
                    mr = xServer.GetResource(mid);
                    if (mr == null || mr.GetRessourceType() != Resource.AttrFont)
                    {
                        ErrorCode.Write(client, ErrorCode.Font, opcode, mid);
                        return;
                    }
                }

                var fg = Colormap.FromParts16(fgRed, fgGreen, fgBlue);
                var bg = Colormap.FromParts16(bgRed, bgGreen, bgBlue);
                var c = new Cursor(id, xServer, client, (Font) r, (Font) mr, sourceChar, maskChar, fg, bg);

                xServer.AddResource(c);
                client.AddResource(c);
            }
        }
    }
}