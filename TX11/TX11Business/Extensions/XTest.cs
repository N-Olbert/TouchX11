using System.Threading;
using TX11Business.UIDependent;

namespace TX11Business.Extensions
{
    /// <summary>
    /// Handles requests related to the XTEST extension.
    /// </summary>
    internal class XTest
    {
        private const byte XTestGetVersion = 0;
        private const byte XTestCompareCursor = 1;
        private const byte XTestFakeInput = 2;
        private const byte XTestGrabControl = 3;

        private const byte KeyPress = 2;
        private const byte KeyRelease = 3;
        private const byte ButtonPress = 4;
        private const byte ButtonRelease = 5;
        private const byte MotionNotify = 6;

        /**
         * Process a request relating to the X SHAPE extension.
         *
         * @param xServer	The X server.
         * @param client	The remote client.
         * @param opcode	The request's opcode.
         * @param arg	Optional first argument.
         * @param bytesRemaining	Bytes yet to be read in the request.
         * @param sequenceNumber	Request sequence number.

         */
        internal static void ProcessRequest(XServer xServer, Client client, byte opcode, byte arg, int bytesRemaining)
        {
            var io = client.GetInputOutput();

            switch (arg)
            {
                case XTestGetVersion:
                    if (bytesRemaining != 4)
                    {
                        io.ReadSkip(bytesRemaining);
                        ErrorCode.WriteWithMinorOpcode(client, ErrorCode.Length, arg, opcode, 0);
                    }
                    else
                    {
                        io.ReadSkip(4); // Skip client major/minor version.

                        byte serverMajorVersion = 2;
                        var serverMinorVersion = 1;

                        lock (io)
                        {
                            Util.WriteReplyHeader(client, serverMajorVersion);
                            io.WriteInt(0); // Reply length.
                            io.WriteShort((short) serverMinorVersion);
                            io.WritePadBytes(22);
                        }

                        io.Flush();
                    }

                    break;
                case XTestCompareCursor:
                    if (bytesRemaining != 8)
                    {
                        io.ReadSkip(bytesRemaining);
                        ErrorCode.WriteWithMinorOpcode(client, ErrorCode.Length, arg, opcode, 0);
                    }
                    else
                    {
                        var wid = io.ReadInt();
                        var cid = io.ReadInt();
                        var r = xServer.GetResource(wid);

                        if (r == null || r.GetRessourceType() != Resource.AttrWindow)
                        {
                            ErrorCode.WriteWithMinorOpcode(client, ErrorCode.Window, arg, opcode, wid);
                            return;
                        }

                        var w = (Window) r;
                        var c = w.GetCursor();
                        bool same;

                        if (cid == 0) // No cursor.
                            same = (c == null);
                        else if (cid == 1) // CurrentCursor.
                            same = (c == xServer.GetScreen().GetCurrentCursor());
                        else
                            same = (cid == c.GetId());

                        lock (io)
                        {
                            Util.WriteReplyHeader(client, (byte) (same ? 1 : 0));
                            io.WriteInt(0); // Reply length.
                            io.WritePadBytes(24);
                        }

                        io.Flush();
                    }

                    break;
                case XTestFakeInput:
                    if (bytesRemaining != 32)
                    {
                        io.ReadSkip(bytesRemaining);
                        ErrorCode.WriteWithMinorOpcode(client, ErrorCode.Length, arg, opcode, 0);
                    }
                    else
                    {
                        var type = (byte) io.ReadByte();
                        var detail = io.ReadByte();

                        io.ReadSkip(2);

                        var delay = io.ReadInt();
                        var wid = io.ReadInt();

                        io.ReadSkip(8);

                        var x = io.ReadShort();
                        var y = io.ReadShort();

                        io.ReadSkip(8);

                        Window root;

                        if (wid == 0)
                        {
                            root = xServer.GetScreen().GetRootWindow();
                        }
                        else
                        {
                            Resource r = (Window) xServer.GetResource(wid);

                            if (r == null || r.GetRessourceType() != Resource.AttrWindow)
                            {
                                ErrorCode.WriteWithMinorOpcode(client, ErrorCode.Window, arg, opcode, wid);
                                return;
                            }
                            else
                            {
                                root = (Window) r;
                            }
                        }

                        TestFakeInput(xServer, client, opcode, arg, type, detail, delay, root, x, y);
                    }

                    break;
                case XTestGrabControl:
                    if (bytesRemaining != 4)
                    {
                        io.ReadSkip(bytesRemaining);
                        ErrorCode.WriteWithMinorOpcode(client, ErrorCode.Length, arg, opcode, 0);
                    }
                    else
                    {
                        var impervious = (io.ReadByte() != 0);

                        io.ReadSkip(3);
                        client.SetImperviousToServerGrabs(impervious);
                    }

                    break;
                default:
                    io.ReadSkip(bytesRemaining);
                    ErrorCode.Write(client, ErrorCode.Implementation, opcode, 0);
                    break;
            }
        }

        /**
         * Inject a fake event.
         *
         * @param xServer	The X server.
         * @param client	The remote client.
         * @param opcode	The request opcode, used for error reporting.
         * @param minorOpcode	The minor opcode, used for error reporting.
         * @param type	The event type.
         * @param detail	Meaning depends on event type.
         * @param delay	Millisecond delay.
         * @param root	The root window in which motion takes place.
         * @param x	The X coordinate of a motion event.
         * @param y	The Y coordinate of a motion event.
         * @
         */
        private static void TestFakeInput(XServer xServer, Client client, byte opcode, byte minorOpcode, byte type,
            int detail, int delay, Window root, int x, int y)
        {
            if (delay != 0)
            {
                Thread.Sleep(delay);
            }

            var sv = xServer.GetScreen();

            switch (type)
            {
                case KeyPress:
                    sv.NotifyKeyPressedReleased(detail, true);
                    break;
                case KeyRelease:
                    sv.NotifyKeyPressedReleased(detail, false);
                    break;
                case ButtonPress:
                    sv.UpdatePointerButtons(detail, true);
                    break;
                case ButtonRelease:
                    sv.UpdatePointerButtons(detail, false);
                    break;
                case MotionNotify:
                    if (root != null)
                        sv = root.GetScreen();

                    if (detail != 0)
                    {
                        // Relative position.
                        x += sv.GetPointerX();
                        y += sv.GetPointerY();
                    }

                    if (x < 0)
                        x = 0;
                    else if (x >= sv.GetWidth())
                        x = sv.GetWidth() - 1;

                    if (y < 0)
                        y = 0;
                    else if (y >= sv.GetHeight())
                        y = sv.GetHeight() - 1;

                    sv.UpdatePointerPosition(x, y, 0);
                    break;
                default:
                    ErrorCode.WriteWithMinorOpcode(client, ErrorCode.Value, minorOpcode, opcode, type);
                    break;
            }
        }
    }
}