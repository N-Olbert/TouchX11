using System;
using System.Collections.Generic;
using TX11Business.Compatibility;
using TX11Shared;
using TX11Shared.Graphics;

namespace TX11Business.UIDependent
{
    internal class Font : Resource
    {
        private static int _dpi = 250;

        private readonly IXPaint paint;
        private readonly float minWidth;
        private readonly float maxWidth;
        private readonly short ascent;
        private readonly short descent;
        private readonly short maxAscent;
        private readonly short maxDescent;
        private readonly char maxChar;
        private Atom nameAtom;

        private static readonly string[] AllFonts =
        {
            "-platformDefault-default-medium-r-normal--0-0-0-0-p-0-iso8859-1",
            "-platformDefault-default-bold-r-normal--0-0-0-0-p-0-iso8859-1",
            "-platformDefault-default-medium-i-normal--0-0-0-0-p-0-iso8859-1",
            "-platformDefault-default-bold-i-normal--0-0-0-0-p-0-iso8859-1",
            "-platformDefault-default-medium-r-normal--0-0-0-0-p-0-iso10646-1",
            "-platformDefault-default-bold-r-normal--0-0-0-0-p-0-iso10646-1",
            "-platformDefault-default-medium-i-normal--0-0-0-0-p-0-iso10646-1",
            "-platformDefault-default-bold-i-normal--0-0-0-0-p-0-iso10646-1",
            "-platformDefault-monospace-medium-r-normal--0-0-0-0-m-0-iso8859-1",
            "-platformDefault-monospace-bold-r-normal--0-0-0-0-m-0-iso8859-1",
            "-platformDefault-monospace-medium-i-normal--0-0-0-0-m-0-iso8859-1",
            "-platformDefault-monospace-bold-i-normal--0-0-0-0-m-0-iso8859-1",
            "-platformDefault-monospace-medium-r-normal--0-0-0-0-m-0-iso10646-1",
            "-platformDefault-monospace-bold-r-normal--0-0-0-0-m-0-iso10646-1",
            "-platformDefault-monospace-medium-i-normal--0-0-0-0-m-0-iso10646-1",
            "-platformDefault-monospace-bold-i-normal--0-0-0-0-m-0-iso10646-1",
            "-platformDefault-serif-medium-r-normal--0-0-0-0-p-0-iso8859-1",
            "-platformDefault-serif-bold-r-normal--0-0-0-0-p-0-iso8859-1",
            "-platformDefault-serif-medium-i-normal--0-0-0-0-p-0-iso8859-1",
            "-platformDefault-serif-bold-i-normal--0-0-0-0-p-0-iso8859-1",
            "-platformDefault-serif-medium-r-normal--0-0-0-0-p-0-iso10646-1",
            "-platformDefault-serif-bold-r-normal--0-0-0-0-p-0-iso10646-1",
            "-platformDefault-serif-medium-i-normal--0-0-0-0-p-0-iso10646-1",
            "-platformDefault-serif-bold-i-normal--0-0-0-0-p-0-iso10646-1",
            "-platformDefault-sans serif-medium-r-normal--0-0-0-0-p-0-iso8859-1",
            "-platformDefault-sans serif-bold-r-normal--0-0-0-0-p-0-iso8859-1",
            "-platformDefault-sans serif-medium-i-normal--0-0-0-0-p-0-iso8859-1",
            "-platformDefault-sans serif-bold-i-normal--0-0-0-0-p-0-iso8859-1",
            "-platformDefault-sans serif-medium-r-normal--0-0-0-0-p-0-iso10646-1",
            "-platformDefault-sans serif-bold-r-normal--0-0-0-0-p-0-iso10646-1",
            "-platformDefault-sans serif-medium-i-normal--0-0-0-0-p-0-iso10646-1",
            "-platformDefault-sans serif-bold-i-normal--0-0-0-0-p-0-iso10646-1",
            "fixed",
            "cursor"
        };

        private static string[][] _allFontFields;

        /**
         * Set the dots-per-inch resolution at which fonts will be displayed.
         *
         * @param dpi	The dots-per-inch resolution.
         */
        internal static void SetDpi(int dpi)
        {
            _dpi = dpi;
        }

        /**
         * Constructor.
         *
         * @param id		The server font ID.
         * @param xserver	The X server.
         * @param client	The client issuing the request.
         * @param name		The name of the font. May be null.
         */
        internal Font(int id, XServer xServer, Client client, string name) : base(AttrFont, id, xServer, client)
        {
            var maxChar = 255.AsChar();

            paint = Util.GetPaint();
            if (name == null || name.EqualsIgnoreCase("cursor"))
            {
                paint.Typeface = XTypeface.Default;
            }
            else if (name.EqualsIgnoreCase("fixed"))
            {
                paint.Typeface = XTypeface.MonoSpace;
            }
            else
            {
                var fields = name.Split("-");
                var baseTypeSet = XTypeface.Default;
                var style = XTypeface.Normal;

                if (fields.Length == 15)
                {
                    if (fields[3].EqualsIgnoreCase("bold"))
                        style |= XTypeface.Bold;
                    if (fields[4].EqualsIgnoreCase("i"))
                        style |= XTypeface.Italic;

                    if (int.TryParse(fields[7], out var n) && n > 0)
                    {
                        paint.TextSize = n;
                    }

                    if (!fields[11].EqualsIgnoreCase("p"))
                        baseTypeSet = XTypeface.MonoSpace;
                    else if (fields[2].EqualsIgnoreCase("default"))
                        baseTypeSet = XTypeface.Default;
                    else if (fields[2].EqualsIgnoreCase("serif"))
                        baseTypeSet = XTypeface.Serif;
                    else if (fields[2].EqualsIgnoreCase("sans serif"))
                        baseTypeSet = XTypeface.SansSerif;
                    else
                    {
                        baseTypeSet = XTypeface.Default;
                        //baseTypeSet = XTypeface.create(fields[2], style); //No support atm
                    }

                    if (fields[13].EqualsIgnoreCase("iso10646"))
                        maxChar = 65534.AsChar();
                }

                paint.Typeface = baseTypeSet | style;
            }

            this.maxChar = maxChar;

            // Calculate the minimum and maximum widths.
            var bytes = new byte[126 - 32 + 1];
            var widths = new float[bytes.Length];

            for (var i = 0; i < bytes.Length; i++)
                bytes[i] = (byte) (i + 32);

            widths = paint.GetTextWidths(bytes.GetString());

            var minw = widths[0];
            var maxw = widths[0];

            foreach (var width in widths)
            {
                if (width < minw)
                    minw = width;
                if (width > maxw)
                    maxw = width;
            }

            minWidth = minw;
            maxWidth = maxw;

            var metrics = paint.GetFontMetrics();

            ascent = (short) -metrics.Ascent;
            descent = (short) metrics.Descent;
            maxAscent = (short) -metrics.Top;
            maxDescent = (short) metrics.Bottom;
        }

        /**
         * Return the font's typeface.
         *
         * @return	The font's typeface.
         */
        internal XTypeface GetTypeface()
        {
            return paint.Typeface;
        }

        /**
         * Return the font's size.
         *
         * @return	The font's size.
         */
        internal int GetSize()
        {
            return (int) paint.TextSize;
        }

        /**
         * Calculate the bounding rectangle for text drawn at a location.
         *
         * @param s	The text.
         * @param x	X coordinate.
         * @param y	Y coordinate.
         * @param rect	Return value. The bounding rectangle.
         */
        internal void GetTextBounds(string s, int x, int y, Rect rect)
        {
            rect.Left = x;
            rect.Right = x + (int) paint.MeasureText(s);
            rect.Top = y - ascent;
            rect.Bottom = y + descent;
        }

        /**
         * Process an X request relating to this font.
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
                case RequestCode.CloseFont:
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
                case RequestCode.QueryFont:
                    if (bytesRemaining != 0)
                    {
                        io.ReadSkip(bytesRemaining);
                        ErrorCode.Write(client, ErrorCode.Length, opcode, 0);
                    }
                    else
                    {
                        ProcessQueryFontRequest(client);
                    }

                    break;
                case RequestCode.QueryTextExtents:
                    if (bytesRemaining < 4 || (bytesRemaining & 3) != 0)
                    {
                        io.ReadSkip(bytesRemaining);
                        ErrorCode.Write(client, ErrorCode.Length, opcode, 0);
                    }
                    else
                    {
                        var pad = (arg == 0) ? 0 : 2;
                        var length = (bytesRemaining - pad) / 2;
                        var chars = new char[length];

                        for (var i = 0; i < length; i++)
                        {
                            var b1 = io.ReadByte();
                            var b2 = io.ReadByte();

                            chars[i] = (char) ((b1 << 8) | b2);
                        }

                        io.ReadSkip(pad);
                        ProcessQueryTextExtentsRequest(client, new string(chars));
                    }

                    break;
                default:
                    io.ReadSkip(bytesRemaining);
                    ErrorCode.Write(client, ErrorCode.Implementation, opcode, 0);
                    break;
            }
        }

        /**
         * Process an OpenFont request.
         *
         * @param xServer	The X server.
         * @param client	The client issuing the request.
         * @param id	The ID of the font to create.
         * @param bytesRemaining	Bytes yet to be read in the request.

         */
        internal static void ProcessOpenFontRequest(XServer xServer, Client client, int id, int bytesRemaining)
        {
            var io = client.GetInputOutput();
            var length = io.ReadShort(); // Length of name.
            var pad = -length & 3;

            io.ReadSkip(2); // Unused.
            bytesRemaining -= 4;
            if (bytesRemaining != length + pad)
            {
                io.ReadSkip(bytesRemaining);
                ErrorCode.Write(client, ErrorCode.Length, RequestCode.OpenFont, 0);
                return;
            }

            var nameBytes = new byte[length];

            io.ReadBytes(nameBytes, 0, length);
            io.ReadSkip(pad);

            var name = nameBytes.GetString();
            var f = new Font(id, xServer, client, name);

            xServer.AddResource(f);
            client.AddResource(f);

            // Create an atom containing the font name.
            var a = xServer.FindAtom(name);

            if (a == null)
            {
                a = new Atom(xServer.NextFreeAtomId(), name);
                xServer.AddAtom(a);
            }

            f.nameAtom = a;
        }

        /**
         * Process a QueryFont request.
         *
         * @param client	The client issuing the request.

         */
        private void ProcessQueryFontRequest(Client client)
        {
            var io = client.GetInputOutput();
            var numFontProperties = (nameAtom == null) ? 0 : 1;
            var numCharInfos = maxChar - 31;
            var chars = new char[numCharInfos];

            for (var c = 32.AsChar(); c <= maxChar; c++)
                chars[c - 32] = c;

            var s = new string(chars);
            var bounds = new Rect();
            var widths = new float[numCharInfos];

            widths = paint.GetTextWidths(s);

            lock (io)
            {
                Util.WriteReplyHeader(client, (byte) 0);
                // Reply length.
                io.WriteInt(7 + numFontProperties * 2 + numCharInfos * 3);

                // Min bounds.
                io.WriteShort((short) 0); // Left side bearing.
                io.WriteShort((short) 0); // Right side bearing.
                io.WriteShort((short) minWidth); // Character width.
                io.WriteShort((short) 0); // Ascent.
                io.WriteShort((short) 0); // Descent.
                io.WriteShort((short) 0); // Attributes.
                io.WritePadBytes(4); // Unused.

                // Max bounds.
                io.WriteShort((short) 0); // Left side bearing.
                io.WriteShort((short) maxWidth); // Right side bearing.
                io.WriteShort((short) maxWidth); // Character width.
                io.WriteShort(maxAscent); // Ascent.
                io.WriteShort(maxDescent); // Descent.
                io.WriteShort((short) 0); // Attributes.
                io.WritePadBytes(4); // Unused.

                io.WriteShort((short) 32); // Min char or byte2.
                io.WriteShort((short) maxChar); // Max char or byte2.
                io.WriteShort((short) 32); // Default char.
                io.WriteShort((short) numFontProperties);
                io.WriteByte((byte) 0); // Draw direction = left-to-right.
                io.WriteByte((byte) 0); // Min byte 1.
                io.WriteByte((byte) 0); // Max byte 1.
                io.WriteByte((byte) 0); // All chars exist = false.
                io.WriteShort(ascent); // Font ascent.
                io.WriteShort(descent); // Font descent.
                io.WriteInt(numCharInfos);

                // If name atom is specified, write the FONT property.
                if (nameAtom != null)
                {
                    var a = XServer.FindAtom("FONT");

                    io.WriteInt(a.GetId()); // Name.
                    io.WriteInt(nameAtom.GetId()); // Value.
                }

                for (var i = 0; i < numCharInfos; i++)
                {
                    paint.GetTextBounds(s, i, i + 1, bounds);
                    io.WriteShort((short) bounds.Left); // Left side bearing.
                    io.WriteShort((short) bounds.Right); // Right side bearing.
                    io.WriteShort((short) widths[i]); // Character width.
                    io.WriteShort((short) -bounds.Top); // Ascent.
                    io.WriteShort((short) bounds.Bottom); // Descent.
                    io.WriteShort((short) 0); // Attributes.
                }
            }

            io.Flush();
        }

        /**
         * Process a QueryTextExtents request.
         *
         * @param client	The remote client.
         * @param s		The string whose extents are being queried.

         */
        private void ProcessQueryTextExtentsRequest(Client client, string s)
        {
            var io = client.GetInputOutput();
            var width = (int) paint.MeasureText(s);
            var bounds = new Rect();

            paint.GetTextBounds(s, 0, s.Length, bounds);

            lock (io)
            {
                Util.WriteReplyHeader(client, (byte) 0);
                io.WriteInt(0); // Reply length.
                io.WriteShort(ascent); // Font ascent.
                io.WriteShort(descent); // Font descent.
                io.WriteShort((short) -bounds.Top); // Overall ascent.
                io.WriteShort((short) bounds.Bottom); // Overall descent.
                io.WriteInt(width); // Overall width.
                io.WriteInt(bounds.Left); // Overall left.
                io.WriteInt(bounds.Right); // Overall right.
                io.WritePadBytes(4); // Unused.
            }

            io.Flush();
        }

        /**
         * Process a GetFontPath request.
         *
         * @param xServer	The X server.
         * @param client	The remote client.

         */
        internal static void ProcessGetFontPath(XServer xServer, Client client)
        {
            var io = client.GetInputOutput();
            var fontPaths = xServer.GetFontPath();
            var numPaths = 0;
            var length = 0;

            if (fontPaths != null)
                numPaths = fontPaths.Length;

            for (var i = 0; i < numPaths; i++)
                length += fontPaths[i].Length + 1;

            var pad = -length & 3;

            lock (io)
            {
                Util.WriteReplyHeader(client, (byte) 0);
                io.WriteInt((length + pad) / 4); // Reply length.
                io.WriteShort((short) numPaths); // Number of STRs in path.
                io.WritePadBytes(22); // Unused.

                for (var i = 0; i < numPaths; i++)
                {
                    var ba = fontPaths[i].GetBytes();

                    io.WriteByte((byte) ba.Length);
                    io.WriteBytes(ba, 0, ba.Length);
                }

                io.WritePadBytes(pad); // Unused.
            }

            io.Flush();
        }

        /**
         * Process a SetFontPath request.
         *
         * @param xServer	The X server.
         * @param client	The remote client.
         * @param bytesRemaining	Bytes yet to be read in the request.

         */
        internal static void ProcessSetFontPath(XServer xServer, Client client, int bytesRemaining)
        {
            var io = client.GetInputOutput();

            if (bytesRemaining < 4)
            {
                io.ReadSkip(bytesRemaining);
                ErrorCode.Write(client, ErrorCode.Length, RequestCode.SetFontPath, 0);
                return;
            }

            var numPaths = io.ReadShort(); // Number of STRs in path.
            var fontPaths = (numPaths > 0) ? new string[numPaths] : null;
            var lengthError = false;

            io.ReadSkip(2); // Unused.
            bytesRemaining -= 4;

            for (var i = 0; i < numPaths; i++)
            {
                if (bytesRemaining < 1)
                {
                    lengthError = true;
                    break;
                }

                var length = io.ReadByte();
                var ba = new byte[length];

                bytesRemaining--;
                if (bytesRemaining < length)
                {
                    lengthError = true;
                    break;
                }

                io.ReadBytes(ba, 0, length);
                bytesRemaining -= length + 1;
                fontPaths[i] = ba.GetString();
            }

            if (bytesRemaining >= 4)
                lengthError = true;

            io.ReadSkip(bytesRemaining);
            if (lengthError)
                ErrorCode.Write(client, ErrorCode.Length, RequestCode.SetFontPath, 0);
            else
                xServer.SetFontPath(fontPaths);
        }

        /**
         * Does the font name match the pattern?
         *
         * @param idx	The index of the font being matched.
         * @param pattern	The pattern being matched.
         * @param pfields	The pattern, broken into its components.
         * @return	The name of the matching font, or null if it doesn't match.
         */
        private static string FontMatchesPattern(int idx, string pattern, string[] pfields)
        {
            var font = AllFonts[idx];

            if (pattern.Equals("*"))
                return font;

            string[] fields;

            if (_allFontFields == null)
                _allFontFields = new string[AllFonts.Length][];

            if (_allFontFields[idx] == null)
                fields = _allFontFields[idx] = font.Split("-");
            else
                fields = _allFontFields[idx];

            if (fields.Length < pfields.Length)
                return null;

            if (fields.Length == 1)
                return pattern.EqualsIgnoreCase(font) ? font : null;

            var offset = 0;
            var rescale = false;

            if (pfields[0].Equals("*"))
                offset = fields.Length - pfields.Length;

            for (var i = 0; i < pfields.Length; i++)
            {
                if (pfields[i].Equals("*"))
                    continue;

                var foff = offset + i;

                if (foff == 0 || foff == 9 || foff == 10)
                    continue; // First field not used. And ignore resolution.
                else if (fields[foff].EqualsIgnoreCase(pfields[i]))
                    continue;
                // else if (fields[foff].matches (pfields[i]))
                // continue;	// Pattern matching.
                else if (foff >= 7 && foff <= 8) // Pixel and point size.
                    rescale = true;
                else
                    return null;
            }

            if (rescale)
            {
                var pixels = 0;
                var points = 0;

                if (offset <= 7)
                {
                    if (int.TryParse(pfields[7 - offset], out var n))
                    {
                        pixels = n;
                    }
                }

                if (offset <= 8)
                {
                    if (int.TryParse(pfields[8 - offset], out var n))
                    {
                        points = n;
                    }
                }

                if (pixels == 0 && points == 0)
                    return font;
                else if (pixels == 0 && points != 0)
                    pixels = (int) Math.Round(points * _dpi / 722.7);
                else if (pixels != 0 && points == 0)
                    points = (int) Math.Round(pixels * 722.7 / _dpi);

                return "-" + fields[1] + "-" + fields[2] + "-" + fields[3] + "-" + fields[4] + "-" + fields[5] + "-" +
                       fields[6] + "-" + pixels + "-" + points + "-" + _dpi + "-" + _dpi + "-" + fields[11] + "-" +
                       fields[12] + "-" + fields[13] + "-" + fields[14];
            }

            return font;
        }

        /**
         * Process a ListFonts or ListFontsWithInfo request.
         *
         * @param client	The remote client.
         * @param opcode	The request's opcode.
         * @param bytesRemaining	Bytes yet to be read in the request.

         */
        internal static void ProcessListFonts(Client client, byte opcode, int bytesRemaining)
        {
            var io = client.GetInputOutput();

            if (bytesRemaining < 4)
            {
                io.ReadSkip(bytesRemaining);
                ErrorCode.Write(client, ErrorCode.Length, opcode, 0);
                return;
            }

            var maxNames = io.ReadShort(); // Max names.
            var length = io.ReadShort(); // Length of pattern.
            var pad = -length & 3;

            bytesRemaining -= 4;
            if (bytesRemaining != length + pad)
            {
                io.ReadSkip(bytesRemaining);
                ErrorCode.Write(client, ErrorCode.Length, opcode, 0);
                return;
            }

            var bytes = new byte[length];

            io.ReadBytes(bytes, 0, length); // Pattern.
            io.ReadSkip(pad); // Unused.

            var pattern = bytes.GetString();
            var pfields = pattern.Split("-");
            var fonts = new List<string>();

            for (var i = 0; i < AllFonts.Length; i++)
            {
                var f = FontMatchesPattern(i, pattern, pfields);

                if (f != null)
                {
                    Util.Add(fonts, f);
                    if (fonts.Count >= maxNames)
                        break;
                }
            }

            if (opcode == RequestCode.ListFonts)
            {
                length = 0;
                foreach (var s in fonts)
                    length += s.Length + 1;

                pad = -length & 3;

                lock (io)
                {
                    Util.WriteReplyHeader(client, (byte) 0);
                    io.WriteInt((length + pad) / 4); // Reply length.
                    io.WriteShort((short) fonts.Size()); // Number of names.
                    io.WritePadBytes(22); // Unused.

                    foreach (var s in fonts)
                    {
                        var ba = s.GetBytes();

                        io.WriteByte((byte) ba.Length);
                        io.WriteBytes(ba, 0, ba.Length);
                    }

                    io.WritePadBytes(pad); // Unused.
                }

                io.Flush();
            }
            else
            {
                var remaining = fonts.Size();

                foreach (var s in fonts)
                    WriteFontWithInfo(client, s, remaining--);

                // Last in series indicator.
                lock (io)
                {
                    Util.WriteReplyHeader(client, (byte) 0);
                    io.WriteInt(7); // Reply length.
                    io.WritePadBytes(52); // Unused.
                }

                io.Flush();
            }
        }

        /**
         * Write information about a named font.
         * This is one of multiple replies to a ListFontsWithInfo request.
         *
         * @param client	The remote client.
         * @param name	The name of the font.
         * @param fontsRemaining	Number of replies before request is complete.

         */
        private static void WriteFontWithInfo(Client client, string name, int fontsRemaining)
        {
            var io = client.GetInputOutput();
            var font = new Font(0, null, null, name);
            var numFontProperties = 0;
            var nameLength = (byte) name.Length;
            var pad = -nameLength & 3;
            var metrics = font.paint.GetFontMetrics();

            lock (io)
            {
                Util.WriteReplyHeader(client, nameLength);
                // Reply length.
                io.WriteInt(7 + numFontProperties * 2 + (nameLength + pad) / 4);

                // Min bounds.
                io.WriteShort((short) 0); // Left side bearing.
                io.WriteShort((short) 0); // Right side bearing.
                io.WriteShort((short) font.minWidth); // Character width.
                io.WriteShort((short) 0); // Ascent.
                io.WriteShort((short) 0); // Descent.
                io.WriteShort((short) 0); // Attributes.
                io.WritePadBytes(4); // Unused.

                // Max bounds.
                io.WriteShort((short) 0); // Left side bearing.
                io.WriteShort((short) font.maxWidth); // Right side bearing.
                io.WriteShort((short) font.maxWidth); // Character width.
                io.WriteShort((short) -metrics.Top); // Ascent.
                io.WriteShort((short) metrics.Bottom); // Descent.
                io.WriteShort((short) 0); // Attributes.
                io.WritePadBytes(4); // Unused.

                io.WriteShort((short) 32); // Min char or byte2.
                io.WriteShort((short) font.maxChar); // Max char or byte2.
                io.WriteShort((short) 32); // Default char.
                io.WriteShort((short) numFontProperties);
                io.WriteByte((byte) 0); // Draw direction = left-to-right.
                io.WriteByte((byte) 0); // Min byte 1.
                io.WriteByte((byte) 0); // Max byte 1.
                io.WriteByte((byte) 0); // All chars exist = false.
                io.WriteShort((short) -metrics.Ascent); // Font ascent.
                io.WriteShort((short) metrics.Descent); // Font descent.
                io.WriteInt(fontsRemaining); // Replies hint.
                // No font properties.
                io.WriteBytes(name.GetBytes(), 0, nameLength); // Name.
                io.WritePadBytes(pad); // Unused.
            }

            io.Flush();
        }
    }
}