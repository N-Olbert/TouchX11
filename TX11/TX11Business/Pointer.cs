using TX11Business.UIDependent;

namespace TX11Business
{
    internal class Pointer
    {
        private readonly byte[] buttonMap = {1, 2, 3};

        /**
         * Return the virtual button that a physical button has been mapped to.
         * Zero indicates the button has been disabled.
         *
         * @param button	The physical button: 1, 2, or 3.
         * @return	The virtual button, or 0 for disabled.
         */
        internal int MapButton(int button)
        {
            if (button < 1 || button > buttonMap.Length)
                return 0;

            return buttonMap[button - 1];
        }

        /**
         * Process a WarpPointer request.
         *
         * @param xServer	The X server.
         * @param client	The remote client.
         * @
         */
        internal void ProcessWarpPointer(XServer xServer, Client client)
        {
            var io = client.GetInputOutput();
            var swin = io.ReadInt(); // Source window.
            var dwin = io.ReadInt(); // Destination window.
            var sx = io.ReadShort(); // Source X.
            var sy = io.ReadShort(); // Source Y.
            var width = io.ReadShort(); // Source width.
            var height = io.ReadShort(); // Source height.
            var dx = io.ReadShort(); // Destination X.
            var dy = io.ReadShort(); // Destination Y.
            var screen = xServer.GetScreen();
            var ok = true;
            int x, y;

            if (dwin == 0)
            {
                x = screen.GetPointerX() + dx;
                y = screen.GetPointerY() + dy;
            }
            else
            {
                var r = xServer.GetResource(dwin);

                if (r == null || r.GetRessourceType() != Resource.AttrWindow)
                {
                    ErrorCode.Write(client, ErrorCode.Window, RequestCode.WarpPointer, dwin);
                    ok = false;
                }

                var rect = ((Window) r).GetIRect();

                x = rect.Left + dx;
                y = rect.Top + dy;
            }

            if (swin != 0)
            {
                var r = xServer.GetResource(swin);

                if (r == null || r.GetRessourceType() != Resource.AttrWindow)
                {
                    ErrorCode.Write(client, ErrorCode.Window, RequestCode.WarpPointer, swin);
                    ok = false;
                }
                else
                {
                    var w = (Window) r;
                    var rect = w.GetIRect();

                    sx += rect.Left;
                    sy += rect.Top;

                    if (width == 0)
                        width = rect.Right - sx;
                    if (height == 0)
                        height = rect.Bottom - sy;

                    if (x < sx || x >= sx + width || y < sy || y >= sy + height)
                        ok = false;
                }
            }

            if (ok)
                screen.UpdatePointerPosition(x, y, 0);
        }

        /**
         * Process an X request relating to the pointers.
         *
         * @param xServer	The X server.
         * @param client	The remote client.
         * @param opcode	The request's opcode.
         * @param arg		Optional first argument.
         * @param bytesRemaining	Bytes yet to be read in the request.
         * @
         */
        internal void ProcessRequest(XServer xServer, Client client, byte opcode, byte arg, int bytesRemaining)
        {
            var io = client.GetInputOutput();

            switch (opcode)
            {
                case RequestCode.WarpPointer:
                    if (bytesRemaining != 20)
                    {
                        io.ReadSkip(bytesRemaining);
                        ErrorCode.Write(client, ErrorCode.Length, opcode, 0);
                    }
                    else
                    {
                        ProcessWarpPointer(xServer, client);
                    }

                    break;
                case RequestCode.ChangePointerControl:
                    if (bytesRemaining != 8)
                        ErrorCode.Write(client, ErrorCode.Length, opcode, 0);
                    io.ReadSkip(bytesRemaining);
                    break; // Do nothing.
                case RequestCode.GetPointerControl:
                    if (bytesRemaining != 0)
                    {
                        io.ReadSkip(bytesRemaining);
                        ErrorCode.Write(client, ErrorCode.Length, opcode, 0);
                    }
                    else
                    {
                        lock (io)
                        {
                            Util.WriteReplyHeader(client, (byte) 0);
                            io.WriteInt(0); // Reply length.
                            io.WriteShort((short) 1); // Acceleration numerator.
                            io.WriteShort((short) 1); // Acceleration denom.
                            io.WriteShort((short) 1); // Threshold.
                            io.WritePadBytes(18); // Unused.
                        }

                        io.Flush();
                    }

                    break;
                case RequestCode.SetPointerMapping:
                    if (bytesRemaining != arg + (-arg & 3))
                    {
                        io.ReadSkip(bytesRemaining);
                        ErrorCode.Write(client, ErrorCode.Length, opcode, 0);
                    }
                    else if (arg != buttonMap.Length)
                    {
                        ErrorCode.Write(client, ErrorCode.Value, opcode, 0);
                    }
                    else
                    {
                        io.ReadBytes(buttonMap, 0, arg);
                        io.ReadSkip(-arg & 3); // Unused.

                        lock (io)
                        {
                            Util.WriteReplyHeader(client, (byte) 0);
                            io.WriteInt(0); // Reply length.
                            io.WritePadBytes(24); // Unused.
                        }

                        io.Flush();

                        xServer.SendMappingNotify(2, 0, 0);
                    }

                    break;
                case RequestCode.GetPointerMapping:
                    if (bytesRemaining != 0)
                    {
                        io.ReadSkip(bytesRemaining);
                        ErrorCode.Write(client, ErrorCode.Length, opcode, 0);
                    }
                    else
                    {
                        var n = (byte) buttonMap.Length;
                        var pad = -n & 3;

                        lock (io)
                        {
                            Util.WriteReplyHeader(client, n);
                            io.WriteInt((n + pad) / 4); // Reply length.
                            io.WritePadBytes(24); // Unused.

                            io.WriteBytes(buttonMap, 0, n); // Map.
                            io.WritePadBytes(pad); // Unused.
                        }

                        io.Flush();
                    }

                    break;
                default:
                    io.ReadSkip(bytesRemaining);
                    ErrorCode.Write(client, ErrorCode.Implementation, opcode, 0);
                    break;
            }
        }
    }
}