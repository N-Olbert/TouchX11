namespace TX11Business
{
    /// <summary>
    /// Class containing details about a bitmap/pixmap format
    /// </summary>
    internal class Format
    {
        private readonly byte depth;
        private readonly byte bitsPerPixel;
        private readonly byte scanlinePad;

        /// <summary>
        /// Initializes a new instance of the <see cref="Format"/> class.
        /// </summary>
        /// <param name="depth">The depth in bits.</param>
        /// <param name="bitsPerPixel">Number of bits per pixel.</param>
        /// <param name="scanlinePad">Number of bits to pad each scan line.</param>
        internal Format(byte depth, byte bitsPerPixel, byte scanlinePad)
        {
            this.depth = depth;
            this.bitsPerPixel = bitsPerPixel;
            this.scanlinePad = scanlinePad;
        }

        /**
         * Write details of the format.
         *
         * @param io	The input/output stream.

         */
        internal void Write(InputOutput io)
        {
            io.WriteByte(depth); // Depth.
            io.WriteByte(bitsPerPixel); // Bits per pixel.
            io.WriteByte(scanlinePad); // Scanline pad.
            io.WritePadBytes(5); // Unused.
        }
    }
}