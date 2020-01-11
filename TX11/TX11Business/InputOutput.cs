using System;
using System.Buffers.Binary;
using System.Diagnostics;
using JetBrains.Annotations;
using System.IO;
using System.Net.Sockets;
using System.Runtime.CompilerServices;

namespace TX11Business
{
    internal class InputOutput
    {
        [NotNull]
        private readonly BufferedStream inStream;

        [NotNull]
        private readonly BinaryReader inReader;

        [NotNull]
        private readonly BufferedStream outStream;

        [NotNull]
        private readonly BinaryWriter outWriter;

        [NotNull]
        private static readonly byte[] PadBytes =
        {
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0
        };

        internal bool SwapEndianness { get; set; }

        /**
	 * Constructor.
	 *
	 * @param socket	Communicate via this socket.
	
	 */
        internal InputOutput(TcpClient socket)
        {
            inStream = new BufferedStream(socket.GetStream(), 16 * 1024);
            inReader = new BinaryReader(inStream);
            outStream = new BufferedStream(socket.GetStream(), 16 * 1024);
            outWriter = new BinaryWriter(outStream);
        }

        [DebuggerStepThrough]
        internal int ReadByte()
        {
            //2020-01-05 Note: an exception here does most likely NOT indicate an error!
            //Lots of small programms doesnt disconnect properly and simply close the stream from their side,
            //which results in an exception here (Examples are xlsatoms, xlsfonts,...)
            var n = inReader.ReadByte();
            return n;
        }

        internal void ReadBytes(byte[] ba, int offset, int length)
        {
            while (length > 0)
            {
                var n = inReader.Read(ba, offset, length);
                if (n < 0)
                {
                    throw new InvalidOperationException();
                }
                else
                {
                    length = length - n;
                    offset = offset + n;
                }
            }
        }

        /**
	 * Read bits from the input stream as an array of booleans.
	 *
	 * @param bits	The array to store the bits to.
	 * @param offset	The start position in the array to store the bits.
	 * @param length	The maximum number of bits to store.
	
	 */
        internal void ReadBits(bool[] bits, int offset, int length)
        {
            for (var i = 0; i < length; i += 8)
            {
                var x = ReadByte();
                var n = length - i;

                if (n > 8)
                    n = 8;

                for (var j = 0; j < n; j++)
                    bits[offset + i + j] = ((x & (1 << j)) != 0);
            }
        }

        /**
         * Read a shape mask from the input stream as an array of booleans.
         *
         * @param bits	The array to store the mask to.
         * @param width	Width of the pixmap.
         * @param height	Height of the pixmap.

         */
        internal void ReadShapeMask(bool[] bits, int width, int height)
        {
            var count = 0;
            var bytesPerRow = (width + 1) / 2;

            for (var row = 0; row < height; row++)
            {
                var col = 0;

                for (var i = 0; i < bytesPerRow; i++)
                {
                    var b = ReadByte();
                    var mask = 0x80;

                    for (var j = 0; j < 8; j++)
                    {
                        bits[count++] = ((b & mask) != 0);
                        mask >>= 1;

                        if (++col == width)
                            break;
                    }

                    if (col == width)
                    {
                        ReadSkip(bytesPerRow - i - 1);
                        break;
                    }
                }
            }
        }

        internal int ReadShort()
        {
            var x = inReader.ReadInt16();
            return SwapEndianness ? BinaryPrimitives.ReverseEndianness(x) : x;
        }

        internal int ReadInt()
        {
            var x = inReader.ReadInt32();
            return SwapEndianness ? BinaryPrimitives.ReverseEndianness(x) : x;
        }

        internal void ReadSkip(int n)
        {
            if (n > 0)
            {
                inReader.ReadBytes(n);
            }
        }

        internal void WriteBytes(byte[] ba, int offset, int length)
        {
            outWriter.Write(ba, offset, length);
        }

        internal void WriteShort(short n)
        {
            var x = SwapEndianness ? BinaryPrimitives.ReverseEndianness(n) : n;
            outWriter.Write(x);
        }

        internal void WriteInt(int n)
        {
            var x = SwapEndianness ? BinaryPrimitives.ReverseEndianness(n) : n;
            outWriter.Write(x);
        }

        /// <summary>
        /// Writes a byte to the output stream.
        /// </summary>
        /// <param name="b">The byte to write.</param>
        internal void WriteByte(byte b)
        {
            this.outWriter.Write(b);
        }

        /// <summary>
        /// Writes n padding bytes to the output stream.
        /// </summary>
        /// <param name="n">The number of requested padding bytes.</param>
        internal void WritePadBytes(int n)
        {
            var max = PadBytes.Length;

            while (n > max)
            {
                outWriter.Write(PadBytes, 0, max);
                n -= max;
            }

            if (n > 0)
                outWriter.Write(PadBytes, 0, n);
        }

        /// <summary>
        /// Flushes all unwritten output.
        /// </summary>
        [MethodImpl(MethodImplOptions.Synchronized)]
        internal void Flush()
        {
            this.outWriter.Flush();
        }

        /// <summary>
        /// Closes this instance.
        /// <para>Therefore closes the input and output stream.</para>
        /// </summary>
        internal void Close()
        {
            //ToDO: IDisposable
            this.inReader.Close();
            this.outWriter.Close();
        }
    }
}