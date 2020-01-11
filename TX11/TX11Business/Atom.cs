using System;
using JetBrains.Annotations;

namespace TX11Business
{
    /// <summary>
    /// An X Atom
    /// </summary>
    internal class Atom
    {
        [NotNull, ItemNotNull]
        private static readonly string[] PredefinedAtoms =
        {
            "PRIMARY",
            "SECONDARY",
            "ARC",
            "ATOM",
            "BITMAP",
            "CARDINAL",
            "COLORMAP",
            "CURSOR",
            "CUT_BUFFER0",
            "CUT_BUFFER1",
            "CUT_BUFFER2",
            "CUT_BUFFER3",
            "CUT_BUFFER4",
            "CUT_BUFFER5",
            "CUT_BUFFER6",
            "CUT_BUFFER7",
            "DRAWABLE",
            "FONT",
            "INTEGER",
            "PIXMAP",
            "POINT",
            "RECTANGLE",
            "RESOURCE_MANAGER",
            "RGB_COLOR_MAP",
            "RGB_BEST_MAP",
            "RGB_BLUE_MAP",
            "RGB_DEFAULT_MAP",
            "RGB_GRAY_MAP",
            "RGB_GREEN_MAP",
            "RGB_RED_MAP",
            "STRING",
            "VISUALID",
            "WINDOW",
            "WM_COMMAND",
            "WM_HINTS",
            "WM_CLIENT_MACHINE",
            "WM_ICON_NAME",
            "WM_ICON_SIZE",
            "WM_NAME",
            "WM_NORMAL_HINTS",
            "WM_SIZE_HINTS",
            "WM_ZOOM_HINTS",
            "MIN_SPACE",
            "NORM_SPACE",
            "MAX_SPACE",
            "END_SPACE",
            "SUPERSCRIPT_X",
            "SUPERSCRIPT_Y",
            "SUBSCRIPT_X",
            "SUBSCRIPT_Y",
            "UNDERLINE_POSITION",
            "UNDERLINE_THICKNESS",
            "STRIKEOUT_ASCENT",
            "STRIKEOUT_DESCENT",
            "ITALIC_ANGLE",
            "X_HEIGHT",
            "QUAD_WIDTH",
            "WEIGHT",
            "POINT_SIZE",
            "RESOLUTION",
            "COPYRIGHT",
            "NOTICE",
            "FONT_NAME",
            "FAMILY_NAME",
            "FULL_NAME",
            "CAP_HEIGHT",
            "WM_CLASS",
            "WM_TRANSIENT_FOR"
        };

        private readonly int id;
        private readonly string name;

        /**
         * Constructor.
         *
         * @param id		The atom's ID.
         */
        internal Atom(int id, string name)
        {
            this.id = id;
            this.name = name;
        }

        /**
         * Register the predefined atoms with the X server.
         *
         * @param xServer
         */
        internal static void RegisterPredefinedAtoms(XServer xServer)
        {
            for (var i = 0; i < PredefinedAtoms.Length; i++)
                xServer.AddAtom(new Atom(i + 1, PredefinedAtoms[i]));
        }

        /**
         * Return the number of predefined atoms.
         * @return	The number of predefined atoms.
         */
        internal static int NumPredefinedAtoms()
        {
            return PredefinedAtoms.Length;
        }

        /**
         * Return the atom's ID.
         * @return	The atom's ID.
         */
        internal int GetId()
        {
            return id;
        }

        /**
         * Return the atom's name.
         * @return	The atom's name.
         */
        internal string GetName()
        {
            return name;
        }

        /**
         * Process a GetAtomName request.
         *
         * @param xServer	The X server.
         * @param client	The remote client.
         * @param bytesRemaining	Bytes yet to be read in the request.

         */
        internal static void ProcessGetAtomNameRequest(XServer xServer, Client client, int bytesRemaining)
        {
            var io = client.GetInputOutput();

            if (bytesRemaining != 4)
            {
                io.ReadSkip(bytesRemaining);
                ErrorCode.Write(client, ErrorCode.Length, RequestCode.GetAtomName, 0);
                return;
            }

            var id = io.ReadInt();
            var a = xServer.GetAtom(id);

            if (a == null)
            {
                ErrorCode.Write(client, ErrorCode.Atom, RequestCode.GetAtomName, id);
                return;
            }

            var bytes = a.name.GetBytes();
            var length = bytes.Length;
            var pad = -length & 3;

            lock (io)
            {
                Util.WriteReplyHeader(client, (byte) 0);
                io.WriteInt((length + pad) / 4); // Reply length.
                io.WriteShort((short) length); // Name length.
                io.WritePadBytes(22); // Unused.
                io.WriteBytes(bytes, 0, length); // Name.
                io.WritePadBytes(pad); // Unused.
            }

            io.Flush();
        }

        /**
         * Process an InternAtom request.
         * Return or create an atom with the specified name.
         *
         * @param xServer	The X server.
         * @param client	The remote client.
         * @param arg	Optional first argument.
         * @param bytesRemaining	Bytes yet to be read in the request.

         */
        internal static void ProcessInternAtomRequest(XServer xServer, Client client, byte arg, int bytesRemaining)
        {
            var io = client.GetInputOutput();

            if (bytesRemaining < 4)
            {
                io.ReadSkip(bytesRemaining);
                ErrorCode.Write(client, ErrorCode.Length, RequestCode.InternAtom, 0);
                return;
            }

            var onlyIfExists = (arg != 0);
            var n = io.ReadShort(); // Length of name.
            var pad = -n & 3;

            io.ReadSkip(2); // Unused.
            bytesRemaining -= 4;

            if (bytesRemaining != n + pad)
            {
                io.ReadSkip(bytesRemaining);
                ErrorCode.Write(client, ErrorCode.Length, RequestCode.InternAtom, 0);
                return;
            }

            var name = new byte[n];

            io.ReadBytes(name, 0, n); // The atom name.
            io.ReadSkip(pad); // Unused.

            var id = 0;
            var s = name.GetString();
            var a = xServer.FindAtom(s);

            if (a != null)
            {
                id = a.GetId();
            }
            else if (!onlyIfExists)
            {
                a = new Atom(xServer.NextFreeAtomId(), s);
                xServer.AddAtom(a);
                id = a.GetId();
            }

            lock (io)
            {
                Util.WriteReplyHeader(client, (byte) 0);
                io.WriteInt(0); // Reply length.
                io.WriteInt(id); // The atom ID.
                io.WritePadBytes(20); // Unused.
            }

            io.Flush();
        }
    }
}