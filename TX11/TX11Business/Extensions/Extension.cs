namespace TX11Business.Extensions
{
    /// <summary>
    /// This class handles requests relating to extensions.
    /// </summary>
    internal class Extensions
    {
        internal const byte AttrXge = unchecked((byte) -128);
        internal const byte AttrXtest = unchecked((byte) -127);
        internal const byte BigRequests = unchecked((byte) -126);
        internal const byte Shape = unchecked((byte) -125);

        /**
         * Process a request relating to an X extension.
         *
         * @param xServer	The X server.
         * @param client	The remote client.
         * @param opcode	The request's opcode.
         * @param arg	Optional first argument.
         * @param bytesRemaining	Bytes yet to be read in the request.

         */
        internal static void ProcessRequest(XServer xServer, Client client, byte opcode, byte arg, int bytesRemaining)
        {
            var io = client.GetInputOutput();

            switch (opcode)
            {
                case AttrXge:
                    if (bytesRemaining != 4)
                    {
                        io.ReadSkip(bytesRemaining);
                        ErrorCode.Write(client, ErrorCode.Length, opcode, 0);
                    }
                    else
                    {
                        // Assume arg == 0 (GEQueryVersion).
                        var xgeMajor = (short) io.ReadShort();
                        var xgeMinor = (short) io.ReadShort();

                        lock (io)
                        {
                            Util.WriteReplyHeader(client, arg);
                            io.WriteInt(0); // Reply length.
                            io.WriteShort(xgeMajor);
                            io.WriteShort(xgeMinor);
                            io.WritePadBytes(20);
                        }

                        io.Flush();
                    }

                    break;
                case AttrXtest:
                    XTest.ProcessRequest(xServer, client, opcode, arg, bytesRemaining);
                    break;
                case BigRequests:
                    if (bytesRemaining != 0)
                    {
                        io.ReadSkip(bytesRemaining);
                        ErrorCode.Write(client, ErrorCode.Length, opcode, 0);
                    }
                    else
                    {
                        // Assume arg == 0 (BigReqEnable).
                        lock (io)
                        {
                            Util.WriteReplyHeader(client, arg);
                            io.WriteInt(0);
                            io.WriteInt(int.MaxValue);
                            io.WritePadBytes(20);
                        }

                        io.Flush();
                    }

                    break;
                case Shape:
                    XShape.ProcessRequest(xServer, client, opcode, arg, bytesRemaining);
                    break;
                default:
                    io.ReadSkip(bytesRemaining); // Not implemented.
                    ErrorCode.Write(client, ErrorCode.Implementation, opcode, 0);
                    break;
            }
        }
    }
}