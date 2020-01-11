using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Threading;
using JetBrains.Annotations;
using TX11Business.Compatibility;
using TX11Business.UIDependent;

namespace TX11Business
{
    internal class Client : InheritableThread
    {
        internal const int Destroy = 0;
        internal const int RetainPermanent = 1;
        internal const int RetainTemporary = 2;

        [NotNull]
        private readonly XServer xServer;

        private readonly TcpClient socket;

        [NotNull]
        private readonly InputOutput inputOutput;

        private readonly int resourceIdBase;
        private readonly int resourceIdMask;
        private readonly List<Resource> resources;
        private int sequenceNumber;
        private bool closeConnection;
        private bool isConnected = true;
        private int closeDownMode = Destroy;
        private bool imperviousToServerGrabs;

        /**
         * Constructor.
         *
         * @param xserver	The X Server.
         * @param socket	The communications socket.
         * @param resourceIdBase	The lowest resource ID the client can use.
         * @param resourceIdMask	The range of resource IDs the client can use.

         */
        internal Client(XServer xserver, TcpClient socket, int resourceIdBase, int resourceIdMask)
        {
            xServer = xserver;
            this.socket = socket;
            inputOutput = new InputOutput(socket);
            this.resourceIdBase = resourceIdBase;
            this.resourceIdMask = resourceIdMask;
            resources = new List<Resource>();
        }

        /**
         * Get the client's close down mode.
         *
         * @return	The client's close down mode.
         */
        internal int GetCloseDownMode()
        {
            return closeDownMode;
        }

        /**
         * Return the input/output handle.
         *
         * @return	The input/output handle.
         */
        [NotNull]
        internal InputOutput GetInputOutput()
        {
            return inputOutput;
        }

        /**
         * Get the sequence number of the latest request sent by the client.
         *
         * @return	The last-used sequence number.
         */
        internal int GetSequenceNumber()
        {
            return sequenceNumber;
        }

        /**
         * Return whether the client is connected.
         *
         * @return	True if the client is connected.
         */
        internal bool IsConnected()
        {
            return isConnected;
        }

        /**
         * Return whether the client is impervious to server grabs.
         *
         * @return	True if impervious.
         */
        internal bool GetImperviousToServerGrabs()
        {
            return imperviousToServerGrabs;
        }

        /**
         * Set whether the client is impervious to server grabs.
         *
         * @param impervious	If true, the client is impervious.
         */
        internal void SetImperviousToServerGrabs(bool impervious)
        {
            imperviousToServerGrabs = impervious;
        }

        /**
         * Add to the client's list of resources.
         *
         * @param r	The resource to add.
         */
        [MethodImpl(MethodImplOptions.Synchronized)]
        internal void AddResource(Resource r)
        {
            Util.Add(resources, r);
        }

        /**
         * Remove a resource from the client's list.
         *
         * @param id	The resource ID.
         */
        [MethodImpl(MethodImplOptions.Synchronized)]
        internal void FreeResource(Resource r)
        {
            Util.Remove(resources, r);
        }

        /**
         * Run the communications thread.
         */
        internal override void Run()
        {
            try
            {
                DoComms();
            }
            catch (Exception)
            {
            }

            lock (xServer)
            {
                Close();
            }
        }

        /**
         * Cancel the communications thread.
         */
        internal void Cancel()
        {
            closeConnection = true;
            Close();
        }

        /**
         * Close the communications thread and free resources.
         */
        private void Close()
        {
            if (!isConnected)
                return;

            isConnected = false;

            try
            {
                inputOutput.Close();
                socket.Close();
            }
            catch (Exception)
            {
            }

            // Clear the resources associated with this client.
            if (closeDownMode == Destroy)
                foreach (var r in resources)
                    r.Delete();

            resources.Clear();
            xServer.RemoveClient(this);
        }

        /**
         * Handle communications with the client.

         */
        private void DoComms()
        {
            // Read the connection setup.
            var byteOrder = inputOutput.ReadByte();

            if (byteOrder == 0x42)
                inputOutput.SwapEndianness = BitConverter.IsLittleEndian;
            else if (byteOrder == 0x6c)
                inputOutput.SwapEndianness = !BitConverter.IsLittleEndian;
            else
                return;

            inputOutput.ReadByte(); // Unused.
            inputOutput.ReadShort(); // Protocol major version.
            inputOutput.ReadShort(); // Protocol minor version.

            var nameLength = inputOutput.ReadShort();
            var dataLength = inputOutput.ReadShort();

            inputOutput.ReadShort(); // Unused.

            if (nameLength > 0)
            {
                inputOutput.ReadSkip(nameLength); // Authorization protocol name.
                inputOutput.ReadSkip(-nameLength & 3); // Padding.
            }

            if (dataLength > 0)
            {
                inputOutput.ReadSkip(dataLength); // Authorization protocol data.
                inputOutput.ReadSkip(-dataLength & 3); // Padding.
            }

            // Complete the setup.
            var vendor = XServer.AttrVendor.GetBytes();
            var pad = -vendor.Length & 3;
            var extra = 26 + 2 * xServer.GetNumFormats() + (vendor.Length + pad) / 4;
            var kb = xServer.GetKeyboard();

            lock (inputOutput)
            {
                inputOutput.WriteByte((byte) 1); // Success.
                inputOutput.WriteByte((byte) 0); // Unused.
                inputOutput.WriteShort(XServer.ProtocolMajorVersion);
                inputOutput.WriteShort(XServer.ProtocolMinorVersion);
                inputOutput.WriteShort((short) extra); // Length of data.
                inputOutput.WriteInt(XServer.ReleaseNumber); // Release number.
                inputOutput.WriteInt(resourceIdBase);
                inputOutput.WriteInt(resourceIdMask);
                inputOutput.WriteInt(0); // Motion buffer size.
                inputOutput.WriteShort((short) vendor.Length); // Vendor length.
                inputOutput.WriteShort((short) 0x7fff); // Max request length.
                inputOutput.WriteByte((byte) 1); // Number of screens.
                inputOutput.WriteByte((byte) xServer.GetNumFormats());
                inputOutput.WriteByte((byte) 0); // Image byte order (0=LSB, 1=MSB).
                inputOutput.WriteByte((byte) 1); // Bitmap bit order (0=LSB, 1=MSB).
                inputOutput.WriteByte((byte) 8); // Bitmap format scanline unit.
                inputOutput.WriteByte((byte) 8); // Bitmap format scanline pad.
                inputOutput.WriteByte((byte) kb.GetMinimumKeycode());
                inputOutput.WriteByte((byte) kb.GetMaximumKeycode());
                inputOutput.WritePadBytes(4); // Unused.

                if (vendor.Length > 0)
                {
                    // Write padded vendor string.
                    inputOutput.WriteBytes(vendor, 0, vendor.Length);
                    inputOutput.WritePadBytes(pad);
                }

                xServer.WriteFormats(inputOutput);
                xServer.GetScreen().Write(inputOutput);
            }

            inputOutput.Flush();

            while (!closeConnection)
            {
                var opcode = (byte) inputOutput.ReadByte();
                var arg = (byte) inputOutput.ReadByte();
                var requestLength = inputOutput.ReadShort();
                int bytesRemaining;

                if (requestLength == 0)
                {
                    // Handle big requests.
                    requestLength = inputOutput.ReadInt();
                    if (requestLength > 2)
                        bytesRemaining = requestLength * 4 - 8;
                    else
                        bytesRemaining = 0;
                }
                else
                {
                    bytesRemaining = requestLength * 4 - 4;
                }

                // Deal with server grabs.
                while (!xServer.ProcessingAllowed(this))
                {
                    Thread.Sleep(100);
                }

                lock (xServer)
                {
                    ProcessRequest(opcode, arg, bytesRemaining);
                }
            }
        }

        /**
         * Is it OK to create a resource with the specified ID?
         *
         * @param id	The resource ID.
         * @return	True if it is OK to create a resource with the ID.
         */
        private bool ValidResourceId(int id)
        {
            return ((id & ~resourceIdMask) == resourceIdBase && !xServer.ResourceExists(id));
        }

        /**
         * Process a single request from the client.
         *
         * @param opcode	The request's opcode.
         * @param arg	Optional first argument.
         * @param bytesRemaining	Bytes yet to be read in the request.

         */
        private void ProcessRequest(byte opcode, byte arg, int bytesRemaining)
        {
#if DEBUG
            var watch = Stopwatch.StartNew();
#endif
            sequenceNumber++;
            switch (opcode)
            {
                case RequestCode.CreateWindow:
                    if (bytesRemaining < 28)
                    {
                        inputOutput.ReadSkip(bytesRemaining);
                        ErrorCode.Write(this, ErrorCode.Length, opcode, 0);
                    }
                    else
                    {
                        var id = inputOutput.ReadInt(); // Window ID.
                        var parent = inputOutput.ReadInt(); // Parent.
                        var r = xServer.GetResource(parent);

                        bytesRemaining -= 8;
                        if (!ValidResourceId(id))
                        {
                            inputOutput.ReadSkip(bytesRemaining);
                            ErrorCode.Write(this, ErrorCode.AttrIdChoice, opcode, id);
                        }
                        else if (r == null || r.GetRessourceType() != Resource.AttrWindow)
                        {
                            inputOutput.ReadSkip(bytesRemaining);
                            ErrorCode.Write(this, ErrorCode.Window, opcode, parent);
                        }
                        else
                        {
                            var w = (Window) r;

                            w.ProcessCreateWindowRequest(inputOutput, this, sequenceNumber, id, arg, bytesRemaining);
                        }
                    }

                    break;
                case RequestCode.ChangeWindowAttributes:
                case RequestCode.GetWindowAttributes:
                case RequestCode.DestroyWindow:
                case RequestCode.DestroySubwindows:
                case RequestCode.ChangeSaveSet:
                case RequestCode.ReparentWindow:
                case RequestCode.MapWindow:
                case RequestCode.MapSubwindows:
                case RequestCode.UnmapWindow:
                case RequestCode.UnmapSubwindows:
                case RequestCode.ConfigureWindow:
                case RequestCode.CirculateWindow:
                case RequestCode.QueryTree:
                case RequestCode.ChangeProperty:
                case RequestCode.DeleteProperty:
                case RequestCode.GetProperty:
                case RequestCode.ListProperties:
                case RequestCode.QueryPointer:
                case RequestCode.GetMotionEvents:
                case RequestCode.TranslateCoordinates:
                case RequestCode.ClearArea:
                case RequestCode.ListInstalledColormaps:
                case RequestCode.RotateProperties:
                    if (bytesRemaining < 4)
                    {
                        ErrorCode.Write(this, ErrorCode.Length, opcode, 0);
                    }
                    else
                    {
                        var id = inputOutput.ReadInt();
                        var r = xServer.GetResource(id);

                        bytesRemaining -= 4;
                        if (r == null || r.GetRessourceType() != Resource.AttrWindow)
                        {
                            inputOutput.ReadSkip(bytesRemaining);
                            ErrorCode.Write(this, ErrorCode.Window, opcode, id);
                        }
                        else
                        {
                            r.ProcessRequest(this, opcode, arg, bytesRemaining);
                        }
                    }

                    break;
                case RequestCode.GetGeometry:
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
                    if (bytesRemaining < 4)
                    {
                        ErrorCode.Write(this, ErrorCode.Length, opcode, 0);
                    }
                    else
                    {
                        var id = inputOutput.ReadInt();
                        var r = xServer.GetResource(id);

                        bytesRemaining -= 4;
                        if (r == null || !r.IsDrawable())
                        {
                            inputOutput.ReadSkip(bytesRemaining);
                            ErrorCode.Write(this, ErrorCode.Drawable, opcode, id);
                        }
                        else
                        {
                            r.ProcessRequest(this, opcode, arg, bytesRemaining);
                        }
                    }

                    break;
                case RequestCode.InternAtom:
                    Atom.ProcessInternAtomRequest(xServer, this, arg, bytesRemaining);
                    break;
                case RequestCode.GetAtomName:
                    Atom.ProcessGetAtomNameRequest(xServer, this, bytesRemaining);
                    break;
                case RequestCode.GetSelectionOwner:
                case RequestCode.SetSelectionOwner:
                case RequestCode.ConvertSelection:
                    Selection.ProcessRequest(xServer, this, opcode, bytesRemaining);
                    break;
                case RequestCode.SendEvent:
                case RequestCode.GrabPointer:
                case RequestCode.UngrabPointer:
                case RequestCode.GrabButton:
                case RequestCode.UngrabButton:
                case RequestCode.ChangeActivePointerGrab:
                case RequestCode.GrabKeyboard:
                case RequestCode.UngrabKeyboard:
                case RequestCode.GrabKey:
                case RequestCode.UngrabKey:
                case RequestCode.AllowEvents:
                case RequestCode.SetInputFocus:
                case RequestCode.GetInputFocus:
                    xServer.GetScreen().ProcessRequest(xServer, this, opcode, arg, bytesRemaining);
                    break;
                case RequestCode.GrabServer:
                    if (bytesRemaining != 0)
                    {
                        inputOutput.ReadSkip(bytesRemaining);
                        ErrorCode.Write(this, ErrorCode.Length, opcode, 0);
                    }
                    else
                    {
                        xServer.GrabServer(this);
                    }

                    break;
                case RequestCode.UngrabServer:
                    if (bytesRemaining != 0)
                    {
                        inputOutput.ReadSkip(bytesRemaining);
                        ErrorCode.Write(this, ErrorCode.Length, opcode, 0);
                    }
                    else
                    {
                        xServer.UngrabServer(this);
                    }

                    break;
                case RequestCode.WarpPointer:
                case RequestCode.ChangePointerControl:
                case RequestCode.GetPointerControl:
                case RequestCode.SetPointerMapping:
                case RequestCode.GetPointerMapping:
                    xServer.GetPointer().ProcessRequest(xServer, this, opcode, arg, bytesRemaining);
                    break;
                case RequestCode.OpenFont:
                    if (bytesRemaining < 8)
                    {
                        inputOutput.ReadSkip(bytesRemaining);
                        ErrorCode.Write(this, ErrorCode.Length, opcode, 0);
                    }
                    else
                    {
                        var id = inputOutput.ReadInt(); // Font ID.

                        bytesRemaining -= 4;
                        if (!ValidResourceId(id))
                        {
                            inputOutput.ReadSkip(bytesRemaining);
                            ErrorCode.Write(this, ErrorCode.AttrIdChoice, opcode, id);
                        }
                        else
                        {
                            Font.ProcessOpenFontRequest(xServer, this, id, bytesRemaining);
                        }
                    }

                    break;
                case RequestCode.CloseFont:
                    if (bytesRemaining != 4)
                    {
                        inputOutput.ReadSkip(bytesRemaining);
                        ErrorCode.Write(this, ErrorCode.Length, opcode, 0);
                    }
                    else
                    {
                        var id = inputOutput.ReadInt();
                        var r = xServer.GetResource(id);

                        bytesRemaining -= 4;
                        if (r == null || r.GetRessourceType() != Resource.AttrFont)
                            ErrorCode.Write(this, ErrorCode.Font, opcode, id);
                        else
                            r.ProcessRequest(this, opcode, arg, bytesRemaining);
                    }

                    break;
                case RequestCode.QueryFont:
                case RequestCode.QueryTextExtents:
                    if (bytesRemaining != 4)
                    {
                        inputOutput.ReadSkip(bytesRemaining);
                        ErrorCode.Write(this, ErrorCode.Length, opcode, 0);
                    }
                    else
                    {
                        var id = inputOutput.ReadInt();
                        var r = xServer.GetResource(id);

                        bytesRemaining -= 4;
                        if (r == null || !r.IsFontable())
                        {
                            inputOutput.ReadSkip(bytesRemaining);
                            ErrorCode.Write(this, ErrorCode.Font, opcode, id);
                        }
                        else
                        {
                            r.ProcessRequest(this, opcode, arg, bytesRemaining);
                        }
                    }

                    break;
                case RequestCode.ListFonts:
                case RequestCode.ListFontsWithInfo:
                    Font.ProcessListFonts(this, opcode, bytesRemaining);
                    break;
                case RequestCode.SetFontPath:
                    Font.ProcessSetFontPath(xServer, this, bytesRemaining);
                    break;
                case RequestCode.GetFontPath:
                    if (bytesRemaining != 0)
                    {
                        inputOutput.ReadSkip(bytesRemaining);
                        ErrorCode.Write(this, ErrorCode.Length, opcode, 0);
                    }
                    else
                    {
                        Font.ProcessGetFontPath(xServer, this);
                    }

                    break;
                case RequestCode.CreatePixmap:
                    if (bytesRemaining != 12)
                    {
                        inputOutput.ReadSkip(bytesRemaining);
                        ErrorCode.Write(this, ErrorCode.Length, opcode, 0);
                    }
                    else
                    {
                        var id = inputOutput.ReadInt(); // Pixmap ID.
                        var did = inputOutput.ReadInt(); // Drawable ID.
                        var width = inputOutput.ReadShort(); // Width.
                        var height = inputOutput.ReadShort(); // Height.
                        var r = xServer.GetResource(did);

                        if (!ValidResourceId(id))
                        {
                            ErrorCode.Write(this, ErrorCode.AttrIdChoice, opcode, id);
                        }
                        else if (r == null || !r.IsDrawable())
                        {
                            ErrorCode.Write(this, ErrorCode.Drawable, opcode, did);
                        }
                        else
                        {
                            try
                            {
                                Pixmap.ProcessCreatePixmapRequest(xServer, this, id, width, height, arg, r);
                            }
                            catch (OutOfMemoryException)
                            {
                                ErrorCode.Write(this, ErrorCode.Alloc, opcode, 0);
                            }
                        }
                    }

                    break;
                case RequestCode.FreePixmap:
                    if (bytesRemaining != 4)
                    {
                        inputOutput.ReadSkip(bytesRemaining);
                        ErrorCode.Write(this, ErrorCode.Length, opcode, 0);
                    }
                    else
                    {
                        var id = inputOutput.ReadInt();
                        var r = xServer.GetResource(id);

                        bytesRemaining -= 4;
                        if (r == null || r.GetRessourceType() != Resource.AttrPixmap)
                            ErrorCode.Write(this, ErrorCode.Pixmap, opcode, id);
                        else
                            r.ProcessRequest(this, opcode, arg, bytesRemaining);
                    }

                    break;
                case RequestCode.AttrCreateGc:
                    if (bytesRemaining < 12)
                    {
                        inputOutput.ReadSkip(bytesRemaining);
                        ErrorCode.Write(this, ErrorCode.Length, opcode, 0);
                    }
                    else
                    {
                        var id = inputOutput.ReadInt(); // GContext ID.
                        var d = inputOutput.ReadInt(); // Drawable ID.
                        var r = xServer.GetResource(d);

                        bytesRemaining -= 8;
                        if (!ValidResourceId(id))
                        {
                            inputOutput.ReadSkip(bytesRemaining);
                            ErrorCode.Write(this, ErrorCode.AttrIdChoice, opcode, id);
                        }
                        else if (r == null || !r.IsDrawable())
                        {
                            inputOutput.ReadSkip(bytesRemaining);
                            ErrorCode.Write(this, ErrorCode.Drawable, opcode, d);
                        }
                        else
                        {
                            GContext.ProcessCreateGcRequest(xServer, this, id, bytesRemaining);
                        }
                    }

                    break;
                case RequestCode.AttrChangeGc:
                case RequestCode.AttrCopyGc:
                case RequestCode.SetDashes:
                case RequestCode.SetClipRectangles:
                case RequestCode.AttrFreeGc:
                    if (bytesRemaining < 4)
                    {
                        ErrorCode.Write(this, ErrorCode.Length, opcode, 0);
                    }
                    else
                    {
                        var id = inputOutput.ReadInt();
                        var r = xServer.GetResource(id);

                        bytesRemaining -= 4;
                        if (r == null || r.GetRessourceType() != Resource.AttrGcontext)
                        {
                            inputOutput.ReadSkip(bytesRemaining);
                            ErrorCode.Write(this, ErrorCode.GContext, opcode, id);
                        }
                        else
                        {
                            r.ProcessRequest(this, opcode, arg, bytesRemaining);
                        }
                    }

                    break;
                case RequestCode.CreateColormap:
                    if (bytesRemaining != 12)
                    {
                        inputOutput.ReadSkip(bytesRemaining);
                        ErrorCode.Write(this, ErrorCode.Length, opcode, 0);
                    }
                    else
                    {
                        var id = inputOutput.ReadInt(); // Colormap ID.

                        bytesRemaining -= 4;
                        if (!ValidResourceId(id))
                        {
                            inputOutput.ReadSkip(bytesRemaining);
                            ErrorCode.Write(this, ErrorCode.AttrIdChoice, opcode, id);
                        }
                        else
                        {
                            Colormap.ProcessCreateColormapRequest(xServer, this, id, arg);
                        }
                    }

                    break;
                case RequestCode.CopyColormapAndFree:
                    if (bytesRemaining != 8)
                    {
                        ErrorCode.Write(this, ErrorCode.Length, opcode, 0);
                    }
                    else
                    {
                        var id1 = inputOutput.ReadInt();
                        var id2 = inputOutput.ReadInt();
                        var r = xServer.GetResource(id2);

                        if (r == null || r.GetRessourceType() != Resource.AttrColormap)
                            ErrorCode.Write(this, ErrorCode.Colormap, opcode, id2);
                        else if (!ValidResourceId(id1))
                            ErrorCode.Write(this, ErrorCode.AttrIdChoice, opcode, id1);
                        else
                            ((Colormap) r).ProcessCopyColormapAndFree(this, id1);
                    }

                    break;
                case RequestCode.FreeColormap:
                case RequestCode.InstallColormap:
                case RequestCode.UninstallColormap:
                case RequestCode.AllocColor:
                case RequestCode.AllocNamedColor:
                case RequestCode.AllocColorCells:
                case RequestCode.AllocColorPlanes:
                case RequestCode.FreeColors:
                case RequestCode.StoreColors:
                case RequestCode.StoreNamedColor:
                case RequestCode.QueryColors:
                case RequestCode.LookupColor:
                    if (bytesRemaining < 4)
                    {
                        ErrorCode.Write(this, ErrorCode.Length, opcode, 0);
                    }
                    else
                    {
                        var id = inputOutput.ReadInt();
                        var r = xServer.GetResource(id);

                        bytesRemaining -= 4;
                        if (r == null || r.GetRessourceType() != Resource.AttrColormap)
                        {
                            inputOutput.ReadSkip(bytesRemaining);
                            ErrorCode.Write(this, ErrorCode.Colormap, opcode, id);
                        }
                        else
                        {
                            r.ProcessRequest(this, opcode, arg, bytesRemaining);
                        }
                    }

                    break;
                case RequestCode.CreateCursor:
                case RequestCode.CreateGlyphCursor:
                    if (bytesRemaining != 28)
                    {
                        inputOutput.ReadSkip(bytesRemaining);
                        ErrorCode.Write(this, ErrorCode.Length, opcode, 0);
                    }
                    else
                    {
                        var id = inputOutput.ReadInt(); // Cursor ID.

                        bytesRemaining -= 4;
                        if (!ValidResourceId(id))
                        {
                            inputOutput.ReadSkip(bytesRemaining);
                            ErrorCode.Write(this, ErrorCode.AttrIdChoice, opcode, id);
                        }
                        else
                        {
                            Cursor.ProcessCreateRequest(xServer, this, opcode, id, bytesRemaining);
                        }
                    }

                    break;
                case RequestCode.FreeCursor:
                case RequestCode.RecolorCursor:
                    if (bytesRemaining < 4)
                    {
                        ErrorCode.Write(this, ErrorCode.Length, opcode, 0);
                    }
                    else
                    {
                        var id = inputOutput.ReadInt();
                        var r = xServer.GetResource(id);

                        bytesRemaining -= 4;
                        if (r == null || r.GetRessourceType() != Resource.AttrCursor)
                        {
                            inputOutput.ReadSkip(bytesRemaining);
                            ErrorCode.Write(this, ErrorCode.Colormap, opcode, id);
                        }
                        else
                        {
                            r.ProcessRequest(this, opcode, arg, bytesRemaining);
                        }
                    }

                    break;
                case RequestCode.QueryExtension:
                    xServer.ProcessQueryExtensionRequest(this, bytesRemaining);
                    break;
                case RequestCode.ListExtensions:
                    if (bytesRemaining != 0)
                    {
                        inputOutput.ReadSkip(bytesRemaining);
                        ErrorCode.Write(this, ErrorCode.Length, opcode, 0);
                    }
                    else
                    {
                        xServer.WriteListExtensions(this);
                    }

                    break;
                case RequestCode.QueryKeymap:
                case RequestCode.ChangeKeyboardMapping:
                case RequestCode.GetKeyboardMapping:
                case RequestCode.ChangeKeyboardControl:
                case RequestCode.SetModifierMapping:
                case RequestCode.GetModifierMapping:
                case RequestCode.GetKeyboardControl:
                case RequestCode.Bell:
                    xServer.GetKeyboard().ProcessRequest(xServer, this, opcode, arg, bytesRemaining);
                    break;
                case RequestCode.SetScreenSaver:
                    if (bytesRemaining != 8)
                    {
                        inputOutput.ReadSkip(bytesRemaining);
                        ErrorCode.Write(this, ErrorCode.Length, opcode, 0);
                    }
                    else
                    {
                        var timeout = inputOutput.ReadShort(); // Timeout.
                        var interval = inputOutput.ReadShort(); // Interval
                        var pb = inputOutput.ReadByte(); // Prefer-blanking.
                        var ae = inputOutput.ReadByte(); // Allow-exposures.

                        inputOutput.ReadSkip(2); // Unused.
                        xServer.SetScreenSaver(timeout, interval, pb, ae);
                    }

                    break;
                case RequestCode.GetScreenSaver:
                    if (bytesRemaining != 0)
                    {
                        inputOutput.ReadSkip(bytesRemaining);
                        ErrorCode.Write(this, ErrorCode.Length, opcode, 0);
                    }
                    else
                    {
                        xServer.WriteScreenSaver(this);
                    }

                    break;
                case RequestCode.ChangeHosts:
                    xServer.ProcessChangeHostsRequest(this, arg, bytesRemaining);
                    break;
                case RequestCode.ListHosts:
                    if (bytesRemaining != 0)
                    {
                        inputOutput.ReadSkip(bytesRemaining);
                        ErrorCode.Write(this, ErrorCode.Length, opcode, 0);
                    }
                    else
                    {
                        xServer.WriteListHosts(this);
                    }

                    break;
                case RequestCode.SetAccessControl:
                    if (bytesRemaining != 0)
                    {
                        inputOutput.ReadSkip(bytesRemaining);
                        ErrorCode.Write(this, ErrorCode.Length, opcode, 0);
                    }
                    else
                    {
                        xServer.SetAccessControl(arg == 1);
                    }

                    break;
                case RequestCode.SetCloseDownMode:
                    ProcessSetCloseDownModeRequest(arg, bytesRemaining);
                    break;
                case RequestCode.KillClient:
                    ProcessKillClientRequest(bytesRemaining);
                    break;
                case RequestCode.ForceScreenSaver:
                    if (bytesRemaining != 0)
                    {
                        inputOutput.ReadSkip(bytesRemaining);
                        ErrorCode.Write(this, ErrorCode.Length, opcode, 0);
                    }
                    else
                    {
                        xServer.GetScreen().Blank(arg == 1);
                    }

                    break;
                case RequestCode.NoOperation:
                    inputOutput.ReadSkip(bytesRemaining);
                    break;
                default: // Opcode not implemented.
                    if ((sbyte) opcode < 0)
                    {
                        Extensions.Extensions.ProcessRequest(xServer, this, opcode, arg, bytesRemaining);
                    }
                    else
                    {
                        inputOutput.ReadSkip(bytesRemaining);
                        ErrorCode.Write(this, ErrorCode.Implementation, opcode, 0);
                    }

                    break;
            }
#if DEBUG
            if (watch.ElapsedMilliseconds > 1)
            {
                Debug.WriteLine($"Op {opcode} took {watch.ElapsedMilliseconds}");
            }
#endif
        }

        /**
         * Process a SetCloseDownMode request.
         *
         * @param mode	The close down mode.
         * @param bytesRemaining	Bytes yet to be read in the request.

         */
        internal void ProcessSetCloseDownModeRequest(int mode, int bytesRemaining)
        {
            if (bytesRemaining != 0)
            {
                inputOutput.ReadSkip(bytesRemaining);
                ErrorCode.Write(this, ErrorCode.Length, RequestCode.SetCloseDownMode, 0);
                return;
            }

            closeDownMode = mode;
            foreach (var r in resources)
            {
                r.SetCloseDownMode(mode);
            }
        }

        /**
         * Process a KillClient request.
         *
         * @param bytesRemaining	Bytes yet to be read in the request.

         */
        internal void ProcessKillClientRequest(int bytesRemaining)
        {
            if (bytesRemaining != 4)
            {
                inputOutput.ReadSkip(bytesRemaining);
                ErrorCode.Write(this, ErrorCode.Length, RequestCode.KillClient, 0);
                return;
            }

            var id = inputOutput.ReadInt();
            Client client = null;

            if (id != 0)
            {
                var r = xServer.GetResource(id);

                if (r == null)
                {
                    ErrorCode.Write(this, ErrorCode.Length, RequestCode.KillClient, 0);
                    return;
                }

                client = r.GetClient();
            }

            if (client != null && client.isConnected)
                client.closeConnection = true;
            else if (client == null || client.closeDownMode != Destroy)
                xServer.DestroyClientResources(client);
        }
    }
}