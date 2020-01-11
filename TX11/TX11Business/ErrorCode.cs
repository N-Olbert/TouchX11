namespace TX11Business
{
    /// <summary>
    /// Class which handles all possible X11 error messages
    /// </summary>
    internal static class ErrorCode
    {
        internal const byte None = 0;
        internal const byte Request = 1;
        internal const byte Value = 2;
        internal const byte Window = 3;
        internal const byte Pixmap = 4;
        internal const byte Atom = 5;
        internal const byte Cursor = 6;
        internal const byte Font = 7;
        internal const byte Match = 8;
        internal const byte Drawable = 9;
        internal const byte Access = 10;
        internal const byte Alloc = 11;
        internal const byte Colormap = 12;
        internal const byte GContext = 13;
        internal const byte AttrIdChoice = 14;
        internal const byte Name = 15;
        internal const byte Length = 16;
        internal const byte Implementation = 17;

        /**
         * Write an X error.
         *
         * @param client	The remote client.
         * @param error	The error code.
         * @param opcode	The opcode of the error request.
         * @param resourceId	The (optional) resource ID that caused the error.

         */
        internal static void Write(Client client, byte error, byte opcode, int resourceId)
        {
            WriteWithMinorOpcode(client, error, (short) 0, opcode, resourceId);
        }

        /**
         * Write an X error with a minor opcode specified.
         *
         * @param client	The remote client.
         * @param error	The error code.
         * @param minorOpcode	The minor opcode of the error request.
         * @param opcode	The major opcode of the error request.
         * @param resourceId	The (optional) resource ID that caused the error.

         */
        internal static void WriteWithMinorOpcode(Client client, byte error, short minorOpcode, byte opcode,
            int resourceId)
        {
            var io = client.GetInputOutput();
            var sn = (short) (client.GetSequenceNumber() & 0xffff);

            lock (io)
            {
                io.WriteByte((byte) 0); // Indicates an error.
                io.WriteByte(error); // Error code.
                io.WriteShort(sn); // Sequence number.
                io.WriteInt(resourceId); // Bad resource ID.
                io.WriteShort(minorOpcode); // Minor opcode.
                io.WriteByte(opcode); // Major opcode.
                io.WritePadBytes(21); // Unused.
            }

            io.Flush();
        }
    }
}