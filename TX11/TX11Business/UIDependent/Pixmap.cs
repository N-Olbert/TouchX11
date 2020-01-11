using System;
using TX11Business.Compatibility;

namespace TX11Business.UIDependent
{
    internal class Pixmap : Resource
    {
        private readonly Drawable drawable;
        private readonly ScreenView screen;

        /**
         * Constructor.
         *
         * @param id	The pixmap's ID.
         * @param xServer	The X server.
         * @param client	The client issuing the request.
         * @param screen	The screen.
         * @param width	The pixmap width.
         * @param height	The pixmap height.
         * @param depth	The pixmap depth.
         */
        internal Pixmap(int id, XServer xServer, Client client, ScreenView screen, int width, int height, int depth) :
            base(AttrPixmap, id, xServer, client)
        {
            drawable = new Drawable(width, height, depth, null, 0xff000000.AsInt());
            this.screen = screen;
        }

        /**
         * Return the pixmap's screen.
         *
         * @return	The pixmap's screen.
         */
        internal ScreenView GetScreen()
        {
            return screen;
        }

        /**
         * Return the pixmap's drawable.
         *
         * @return	The pixmap's drawable.
         */
        internal Drawable GetDrawable()
        {
            return drawable;
        }

        /**
         * Return the pixmap's depth.
         *
         * @return	The pixmap's depth.
         */
        internal int GetDepth()
        {
            return drawable.GetDepth();
        }

        /**
         * Process an X request relating to this pixmap.
         *
         * @param client	The remote client.
         * @param opcode	The request's opcode.
         * @param arg		Optional first argument.
         * @param bytesRemaining	Bytes yet to be read in the request.
         * @
         */
        internal override void ProcessRequest(Client client, byte opcode, byte arg, int bytesRemaining)
        {
            var io = client.GetInputOutput();

            switch (opcode)
            {
                case RequestCode.FreePixmap:
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
                        drawable.GetBitmap().Recycle();
                    }

                    break;
                case RequestCode.GetGeometry:
                    if (bytesRemaining != 0)
                    {
                        io.ReadSkip(bytesRemaining);
                        ErrorCode.Write(client, ErrorCode.Length, opcode, 0);
                    }
                    else
                    {
                        WriteGeometry(client);
                    }

                    break;
                case RequestCode.CopyArea:
                case RequestCode.CopyPlane:
                case RequestCode.PolyPoint:
                case RequestCode.PolyLine:
                case RequestCode.PolySegment:
                case RequestCode.PolyRectangle:
                case RequestCode.PolyArc:
                case RequestCode.FillPoly:
                case RequestCode.PolyFillRectangle:
                case RequestCode.PolyFillArc:
                case RequestCode.PutImage:
                case RequestCode.GetImage:
                case RequestCode.PolyText8:
                case RequestCode.PolyText16:
                case RequestCode.ImageText8:
                case RequestCode.ImageText16:
                case RequestCode.QueryBestSize:
                    drawable.ProcessRequest(XServer, client, Id, opcode, arg, bytesRemaining);
                    return;
                default:
                    io.ReadSkip(bytesRemaining);
                    bytesRemaining = 0;
                    ErrorCode.Write(client, ErrorCode.Implementation, opcode, 0);
                    break;
            }
        }

        /**
         * Write details of the pixmap's geometry in response to a GetGeometry
         * request.
         *
         * @param client	The remote client.
         * @
         */
        private void WriteGeometry(Client client)
        {
            var io = client.GetInputOutput();

            lock (io)
            {
                Util.WriteReplyHeader(client, (byte) 32);
                io.WriteInt(0); // Reply length.
                io.WriteInt(screen.GetRootWindow().GetId()); // Root window.
                io.WriteShort((short) 0); // X.
                io.WriteShort((short) 0); // Y.
                io.WriteShort((short) drawable.GetWidth()); // Width.
                io.WriteShort((short) drawable.GetHeight()); // Height.
                io.WriteShort((short) 0); // Border width.
                io.WritePadBytes(10); // Unused.
            }

            io.Flush();
        }

        /**
         * Process a CreatePixmap request.
         *
         * @param xServer	The X server.
         * @param client	The client issuing the request.
         * @param id	The ID of the pixmap to create.
         * @param depth		The depth of the pixmap.
         * @param drawable	The drawable whose depth it must match.
         * @
         */
        internal static void ProcessCreatePixmapRequest(XServer xServer, Client client, int id, int width, int height,
            int depth, Resource drawable)
        {
            ScreenView screen;
            Pixmap p;

            if (drawable.GetRessourceType() == Resource.AttrPixmap)
                screen = ((Pixmap) drawable).GetScreen();
            else
                screen = ((Window) drawable).GetScreen();

            try
            {
                p = new Pixmap(id, xServer, client, screen, width, height, depth);
            }
            catch (OutOfMemoryException)
            {
                ErrorCode.Write(client, ErrorCode.Alloc, RequestCode.CreatePixmap, 0);
                return;
            }

            xServer.AddResource(p);
            client.AddResource(p);
        }
    }
}