namespace TX11Business.UIDependent
{
    internal class Visual
    {
        internal const byte BackingStoreNever = 0;
        internal const byte BackingStoreWhenMapped = 1;
        internal const byte BackingStoreAlways = 2;

        internal const byte StaticGray = 0;
        internal const byte GrayScale = 1;
        internal const byte StaticColor = 2;
        internal const byte PseudoColor = 3;
        internal const byte TrueColor = 4;
        internal const byte DirectColor = 5;

        private readonly int id;

        /**
         * Constructor.
         *
         * @param id	The visual ID.
         */
        internal Visual(int id)
        {
            this.id = id;
        }

        /**
         * Return the visual's ID.
         *
         * @return	The visual's ID.
         */
        internal int GetId()
        {
            return id;
        }

        /**
         * Return whether the visual supports a backing store.
         *
         * @return	Whether a backing store is supported.
         */
        internal byte GetBackingStoreInfo()
        {
            return BackingStoreAlways;
        }

        /**
         * Return whether the visual supports save-under.
         *
         * @return	Whether save-under is supported.
         */
        internal bool GetSaveUnder()
        {
            return false;
        }

        /**
         * Return the depth of the visual.
         * Under Android this is always 32.
         *
         * @return	The depth of the visual, in bits.
         */
        internal byte GetDepth()
        {
            return 32;
        }

        /**
         * Write details of the visual.
         *
         * @param io	The input/output stream.

         */
        internal void Write(InputOutput io)
        {
            io.WriteInt(id); // Visual ID.
            io.WriteByte(TrueColor); // Class.
            io.WriteByte((byte) 8); // Bits per RGB value.
            io.WriteShort((short) (1 << 8)); // Colormap entries.
            io.WriteInt(0x00ff0000); // Red mask.
            io.WriteInt(0x0000ff00); // Green mask.
            io.WriteInt(0x000000ff); // Blue mask.
            io.WritePadBytes(4); // Unused.
        }
    }
}