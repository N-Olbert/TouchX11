using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using TX11Business.Compatibility;
using TX11Business.Extensions;
using TX11Business.UIDependent;
using TX11Shared;

namespace TX11Business
{
    internal class XServer
    {
        internal const short ProtocolMajorVersion = 11;
        internal const short ProtocolMinorVersion = 0;
        internal const string AttrVendor = "Open source";
        internal const int ReleaseNumber = 0;

        private readonly int port;
        private readonly string windowManagerClass;
        private readonly List<Format> formats;
        private readonly Dictionary<int, Resource> resources;

        private readonly List<Client> clients;
        private const int ClientIdBits = 20;
        private const int ClientIdStep = (1 << ClientIdBits);
        private int clientIdBase = ClientIdStep;

        private readonly Dictionary<int, Atom> atoms;
        private readonly Dictionary<string, Atom> atomNames;
        private int maxAtomId;
        private readonly Dictionary<int, Selection> selections;

        private readonly Keyboard keyboard;
        private readonly Pointer pointer;
        private readonly Font defaultFont;
        private readonly Visual rootVisual;
        private readonly ScreenView screen;
        private string[] fontPath;
        private AcceptThread acceptThread;
        private long timestamp;
        private Client grabClient;

        private int screenSaverTimeout;
        private int screenSaverInterval;
        private int preferBlanking = 1;
        private int allowExposures;
        private long screenSaverTime;
        private readonly CountDownTimer screenSaverCountDownTimer = null;

        private bool accessControlEnabled;
        private readonly HashSet<int> accessControlHosts;

        private readonly Dictionary<string, Extension> extensions;

        /**
         * Constructor.
         *
         * @param c	The application context.
         * @param port	The port to listen on. Usually 6000.
         * @param windowManagerClass	Window manager class name. Can be null.
         */
        internal XServer(int port, string windowManagerClass)
        {
            this.port = port;
            this.windowManagerClass = windowManagerClass;
            formats = new List<Format>();
            resources = new Dictionary<int, Resource>();
            clients = new List<Client>();
            atoms = new Dictionary<int, Atom>();
            atomNames = new Dictionary<string, Atom>();
            selections = new Dictionary<int, Selection>();
            accessControlHosts = new HashSet<int>();

            extensions = new Dictionary<string, Extension>();
            extensions.Put("Generic Event Extension", new Extension(Extensions.Extensions.AttrXge, (byte) 0, (byte) 0));
            extensions.Put("XTEST", new Extension(Extensions.Extensions.AttrXtest, (byte) 0, (byte) 0));
            extensions.Put("BIG-REQUESTS", new Extension(Extensions.Extensions.BigRequests, (byte) 0, (byte) 0));
            extensions.Put("SHAPE", new Extension(Extensions.Extensions.Shape, XShape.EventBase, (byte) 0));

            Util.Add(formats, new Format((byte) 32, (byte) 24, (byte) 8));

            keyboard = new Keyboard();
            pointer = new Pointer();

            defaultFont = new Font(1, this, null, null);
            AddResource(defaultFont);
            AddResource(new Cursor(2, this, null, (Font) null, (Font) null, 0, 1, 0xff000000.AsInt(),
                                   0xffffffff.AsInt()));

            screen = new ScreenView(this, 3, PixelsPerMillimeter());

            var cmap = new Colormap(4, this, null, screen);

            cmap.SetInstalled(true);
            AddResource(cmap);

            rootVisual = new Visual(1);
            Atom.RegisterPredefinedAtoms(this);

            timestamp = DateTime.UtcNow.Ticks;
        }

        /**
         * Start the thread that listens on the socket.
         * Also start the window manager if one is specified.
         * 
         * @return	True if the thread is started successfully.
         */
        [MethodImpl(MethodImplOptions.Synchronized)]
        internal bool Start()
        {
            if (acceptThread != null)
                return true; // Already running.

            try
            {
                acceptThread = new AcceptThread(port, this);
                acceptThread.Start();
            }
            catch (Exception)
            {
                return false;
            }

            //if (_windowManagerClass != null)
            //{
            //	int idx = _windowManagerClass.lastIndexOf('.');

            //	if (idx > 0)
            //	{
            //		String pkg = _windowManagerClass.substring(0, idx);
            //		Intent intent = new Intent(Intent.ACTION_MAIN);

            //		intent.setComponent(new ComponentName(pkg,
            //											_windowManagerClass));

            //		try
            //		{
            //			if (_context.startService(intent) == null)
            //				Log.e("XServer",
            //						"Could not start " + _windowManagerClass);
            //		}
            //		catch (SecurityException e)
            //		{
            //			Log.e("XServer", "Could not start " + _windowManagerClass
            //										+ ": " + e.getMessage());
            //		}
            //	}
            //}

            ResetScreenSaver();

            return true;
        }

        /**
         * Stop listening on the socket and terminate all clients.
         */
        [MethodImpl(MethodImplOptions.Synchronized)]
        internal void Stop()
        {
            if (acceptThread != null)
            {
                acceptThread.Cancel();
                acceptThread = null;
            }

            grabClient = null;
            while (clients.Count > 0)
                clients.First().Cancel();
        }

        /**
         * Reset the server.
         * This should be called when the last client disconnects with a
         * close-down mode of Destroy.
         */
        private void Reset()
        {
            // Remove all client-allocated resources.
            foreach (var res in resources.ToList())
            {
                if (res.Key > ClientIdStep)
                    //res.Value.delete(); //TODO?
                    resources.Remove(res.Key);
            }

            screen.RemoveNonDefaultColormaps();

            if (atoms.Size() != Atom.NumPredefinedAtoms())
            {
                Util.Clear(atoms);
                Util.Clear(atomNames);
                Atom.RegisterPredefinedAtoms(this);
            }

            Util.Clear(selections);
            timestamp = DateTime.UtcNow.Ticks;
        }

        /**
         * Return the internet address the server is listening on.
         *
         * @return The internet address the server is listening on.
         */
        internal IPAddress GetInetAddress()
        {
            if (acceptThread == null)
                return null;

            return acceptThread.GetInetAddress();
        }

        /**
         * Return the number of milliseconds since the last reset.
         *
         * @return	The number of milliseconds since the last reset.
         */
        internal int GetTimestamp()
        {
            var diff = (DateTime.UtcNow.Ticks - timestamp) / TimeSpan.TicksPerMillisecond;

            if (diff <= 0)
                return 1;

            return (int) diff;
        }

        /**
         * Remove a client from the list of active clients.
         *
         * @param client	The client to remove.
         */
        internal void RemoveClient(Client client)
        {
            foreach (var sel in selections.Values)
                sel.ClearClient(client);

            Util.Remove(clients, client);
            if (grabClient == client)
                grabClient = null;

            if (client.GetCloseDownMode() == Client.Destroy && clients.Size() == 0)
                Reset();
        }

        /**
         * Disable all clients except this one.
         *
         * @param client	The client issuing the grab.
         */
        internal void GrabServer(Client client)
        {
            grabClient = client;
        }

        /**
         * End the server grab.
         *
         * @param client	The client issuing the grab.
         */
        internal void UngrabServer(Client client)
        {
            if (grabClient == client)
                grabClient = null;
        }

        /**
         * Return true if processing is allowed. This is only false if the
         * server has been grabbed by another client and the checking client
         * is not impervious to server grabs.
         *
         * @param client	The client checking if processing is allowed.
         *
         * @return	True if processing is allowed for the client.
         */
        internal bool ProcessingAllowed(Client client)
        {
            if (grabClient == null || client.GetImperviousToServerGrabs())
                return true;

            return grabClient == client;
        }

        /**
         * Get the X server's keyboard.
         *
         * @return	The keyboard used by the X server.
         */
        [NotNull]
        internal Keyboard GetKeyboard()
        {
            return keyboard;
        }

        /**
         * Get the X server's pointer.
         *
         * @return	The pointer used by the X server.
         */
        internal Pointer GetPointer()
        {
            return pointer;
        }

        /**
         * Get the server's font path.
         *
         * @return	The server's font path.
         */
        internal string[] GetFontPath()
        {
            return fontPath;
        }

        /**
         * Set the server's font path.
         *
         * @param path	The new font path.
         */
        internal void SetFontPath(string[] path)
        {
            fontPath = path;
        }

        /**
         * Return the screen attached to the display.
         *
         * @return	The screen attached to the display.
         */
        internal ScreenView GetScreen()
        {
            return screen;
        }

        /**
         * Return the number of pixels per millimeter on the display.
         *
         * @return	The number of pixels per millimeter on the display.
         */
        private float PixelsPerMillimeter()
        {
            var xScreen = XConnector.Resolve<IXScreen>();
            var dpi = xScreen.Dpi;
            Font.SetDpi((int) dpi);

            return (float) dpi / 25.4f;
        }

        /**
         * Get the number of pixmap formats.
         *
         * @return	The number of pixmap formats.
         */
        internal int GetNumFormats()
        {
            return formats.Size();
        }

        /**
         * Write details of all the pixmap formats.
         *
         * @param io	The input/output stream.

         */
        internal void WriteFormats(InputOutput io)
        {
            foreach (var f in formats)
                f.Write(io);
        }

        /**
         * Return the default font.
         * 
         * @return	The default font.
         */
        internal Font GetDefaultFont()
        {
            return defaultFont;
        }

        /**
         * Return the root visual.
         *
         * @return	The root visual.
         */
        internal Visual GetRootVisual()
        {
            return rootVisual;
        }

        /**
         * Add an atom.
         *
         * @param a		The atom to add.
         */
        internal void AddAtom(Atom a)
        {
            atoms.Put(a.GetId(), a);
            atomNames.Put(a.GetName(), a);

            if (a.GetId() > maxAtomId)
                maxAtomId = a.GetId();
        }

        /**
         * Return the atom with the specified ID.
         *
         * @param id	The atom ID.
         * @return	The specified atom, or null if it doesn't exist.
         */
        internal Atom GetAtom(int id)
        {
            if (!Util.ContainsKey(atoms, id)) // No such atom.
                return null;

            return atoms.Get(id);
        }

        /**
         * Return the atom with the specified name.
         *
         * @param name	The atom's name.
         * @return	The specified atom, or null if it doesn't exist.
         */
        internal Atom FindAtom(string name)
        {
            if (!Util.ContainsKey(atomNames, name))
                return null;

            return atomNames.Get(name);
        }

        /**
         * Does the atom with specified ID exist?
         *
         * @param id	The atom ID.
         * @return	True if an atom with the ID exists.
         */
        internal bool AtomExists(int id)
        {
            return Util.ContainsKey(atoms, id);
        }

        /**
         * Get the ID of the next free atom.
         *
         * @return	The ID of the next free atom.
         */
        internal int NextFreeAtomId()
        {
            return ++maxAtomId;
        }

        /**
         * Return the selection with the specified ID.
         *
         * @param id	The selection ID.
         * @return	The specified selection, or null if it doesn't exist.
         */
        internal Selection GetSelection(int id)
        {
            if (!Util.ContainsKey(selections, id)) // No such selection.
                return null;

            return selections.Get(id);
        }

        /**
         * Add a selection.
         *
         * @param sel	The selection to add.
         */
        internal void AddSelection(Selection sel)
        {
            selections.Put(sel.GetId(), sel);
        }

        /**
         * Add a resource.
         *
         * @param r	The resource to add.
         */
        internal void AddResource(Resource r)
        {
            resources.Put(r.GetId(), r);
        }

        /**
         * Return the resource with the specified ID.
         *
         * @param id	The resource ID.
         * @return	The specified resource, or null if it doesn't exist.
         */
        internal Resource GetResource(int id)
        {
            if (!ResourceExists(id))
                return null;

            return resources.Get(id);
        }

        /**
         * Does the resource with specified ID exist?
         *
         * @param id	The resource ID.
         * @return	True if a resource with the ID exists.
         */
        internal bool ResourceExists(int id)
        {
            return Util.ContainsKey(resources, id);
        }

        /**
         * Free the resource with the specified ID.
         *
         * @param id	The resource ID. 
         */
        internal void FreeResource(int id)
        {
            Util.Remove(resources, id);
        }

        /**
         * If client is null, destroy the resources of all clients that have
         * terminated in RetainTemporary mode. Otherwise destroy all resources
         * associated with the client, which has terminated with mode
         * RetainPermanant or RetainTemporary.
         *
         * @param client	The terminated client, or null.
         */
        [MethodImpl(MethodImplOptions.Synchronized)]
        internal void DestroyClientResources(Client client)
        {
            var rc = resources.Values;
            var dl = new List<Resource>();

            if (client == null)
            {
                foreach (var r in rc)
                {
                    var c = r.GetClient();
                    var disconnected = (c == null || !c.IsConnected());

                    if (disconnected && r.GetCloseDownMode() == Client.RetainTemporary)
                        Util.Add(dl, r);
                }
            }
            else
            {
                foreach (var r in rc)
                    if (r.GetClient() == client)
                        Util.Add(dl, r);
            }

            foreach (var r in dl)
                r.Delete();
        }

        /**
         * Send a MappingNotify to all clients.
         *
         * @param request	0=Modifier, 1=Keyboard, 2=Pointer
         * @param firstKeycode	First keycode in new keyboard map.
         * @param keycodeCount	Number of keycodes in new keyboard map.
         */
        internal void SendMappingNotify(int request, int firstKeycode, int keycodeCount)
        {
            foreach (var c in clients)
            {
                try
                {
                    EventCode.SendMappingNotify(c, request, firstKeycode, keycodeCount);
                }
                catch (Exception)
                {
                }
            }
        }

        /**
         * Process a QueryExtension request.
         *
         * @param client	The remote client.
         * @param bytesRemaining	Bytes yet to be read in the request.

         */
        internal void ProcessQueryExtensionRequest(Client client, int bytesRemaining)
        {
            var io = client.GetInputOutput();

            if (bytesRemaining < 4)
            {
                io.ReadSkip(bytesRemaining);
                ErrorCode.Write(client, ErrorCode.Length, RequestCode.QueryExtension, 0);
                return;
            }

            var length = io.ReadShort(); // Length of name.
            var pad = -length & 3;

            io.ReadSkip(2); // Unused.
            bytesRemaining -= 4;

            if (bytesRemaining != length + pad)
            {
                io.ReadSkip(bytesRemaining);
                ErrorCode.Write(client, ErrorCode.Length, RequestCode.QueryExtension, 0);
                return;
            }

            var bytes = new byte[length];

            io.ReadBytes(bytes, 0, length);
            io.ReadSkip(pad); // Unused.

            var s = bytes.GetString();
            Extension e;

            if (Util.ContainsKey(extensions, s))
                e = extensions.Get(s);
            else
                e = null;

            lock (io)
            {
                Util.WriteReplyHeader(client, (byte) 0);
                io.WriteInt(0); // Reply length.

                if (e == null)
                {
                    io.WriteByte((byte) 0); // Present. 0 = false.
                    io.WriteByte((byte) 0); // Major opcode.
                    io.WriteByte((byte) 0); // First event.
                    io.WriteByte((byte) 0); // First error.
                }
                else
                {
                    io.WriteByte((byte) 1); // Present. 1 = true.
                    io.WriteByte(e.MajorOpcode); // Major opcode.
                    io.WriteByte(e.FirstEvent); // First event.
                    io.WriteByte(e.FirstError); // First error.
                }

                io.WritePadBytes(20); // Unused.
            }

            io.Flush();
        }

        /**
         * Write the list of extensions supported by the server.
         *
         * @param client	The remote client.

         */
        internal void WriteListExtensions(Client client)
        {
            var ss = extensions.Keys;
            var length = 0;

            foreach (var s in ss)
                length += s.Length + 1;

            var pad = -length & 3;
            var io = client.GetInputOutput();

            lock (io)
            {
                Util.WriteReplyHeader(client, (byte) ss.Size());
                io.WriteInt((length + pad) / 4); // Reply length.
                io.WritePadBytes(24); // Unused.

                foreach (var s in ss)
                {
                    var ba = s.GetBytes();

                    io.WriteByte((byte) ba.Length);
                    io.WriteBytes(ba, 0, ba.Length);
                }

                io.WritePadBytes(pad); // Unused.
            }

            io.Flush();
        }

        /**
         * Process a ChangeHosts request.
         *
         * @param client	The remote client.
         * @param mode	Change mode. 0=Insert, 1=Delete.
         * @param bytesRemaining	Bytes yet to be read in the request.

         */
        internal void ProcessChangeHostsRequest(Client client, int mode, int bytesRemaining)
        {
            var io = client.GetInputOutput();

            if (bytesRemaining < 4)
            {
                io.ReadSkip(bytesRemaining);
                ErrorCode.Write(client, ErrorCode.Length, RequestCode.ChangeHosts, 0);
                return;
            }

            var family = io.ReadByte(); // 0=Inet, 1=DECnet, 2=Chaos.

            io.ReadSkip(1); // Unused.

            var length = io.ReadShort(); // Length of address.
            var pad = -length & 3;

            bytesRemaining -= 4;
            if (bytesRemaining != length + pad)
            {
                io.ReadSkip(bytesRemaining);
                ErrorCode.Write(client, ErrorCode.Length, RequestCode.ChangeHosts, 0);
                return;
            }

            if (family != 0 || length != 4)
            {
                io.ReadSkip(bytesRemaining);
                ErrorCode.Write(client, ErrorCode.Value, RequestCode.ChangeHosts, 0);
                return;
            }

            var address = 0;

            for (var i = 0; i < length; i++)
                address = (address << 8) | io.ReadByte();

            io.ReadSkip(pad); // Unused.

            if (mode == 0)
                Util.Add(accessControlHosts, address);
            else
                Util.Remove(accessControlHosts, address);
        }

        /**
         * Reply to a ListHosts request.
         *
         * @param client	The remote client.

         */
        internal void WriteListHosts(Client client)
        {
            var io = client.GetInputOutput();
            var n = accessControlHosts.Size();

            lock (io)
            {
                Util.WriteReplyHeader(client, (byte) (accessControlEnabled ? 1 : 0));
                io.WriteInt(n * 2); // Reply length.
                io.WriteShort((short) n); // Number of hosts.
                io.WritePadBytes(22); // Unused.

                foreach (var addr in accessControlHosts)
                {
                    io.WriteByte((byte) 0); // Family = Internet.
                    io.WritePadBytes(1); // Unused.
                    io.WriteShort((short) 4); // Length of address.
                    io.WriteByte((byte) ((addr >> 24) & 0xff));
                    io.WriteByte((byte) ((addr >> 16) & 0xff));
                    io.WriteByte((byte) ((addr >> 8) & 0xff));
                    io.WriteByte((byte) (addr & 0xff));
                }
            }

            io.Flush();
        }

        /**
         * Enable/disable access control.
         *
         * @param enabled	If true, enable access control.
         */
        internal void SetAccessControl(bool enabled)
        {
            accessControlEnabled = enabled;
        }

        /**
         * Get the list of hosts that are allowed to connect.
         * This can be modified.
         *
         * @return	The set of addresses that are allowed to connect.
         */
        internal HashSet<int> GetAccessControlHosts()
        {
            return accessControlHosts;
        }

        /**
         * Is a client from the specified address allowed to connect?
         *
         * @param address	The client's IP address, MSB format.
         * @return	True if the client is allowed to exist.
         */
        private bool IsAccessAllowed(int address)
        {
            if (!accessControlEnabled)
                return true;

            return Util.Contains(accessControlHosts, address);
        }

        /**
         * Set the screen saver parameters.
         *
         * @param timeout	Timeout period, in seconds. 0=disabled, -1=default.
         * @param interval	Interval in seconds. 0=disabled, -1=default.
         * @param preferBlanking	0=No, 1=Yes, 2=Default.
         * @param allowExposures	0=No, 1=Yes, 2=Default.
         */
        internal void SetScreenSaver(int timeout, int interval, int preferBlanking, int allowExposures)
        {
            if (timeout == -1)
                screenSaverTimeout = 0; // Default timeout.
            else
                screenSaverTimeout = timeout;

            if (interval == -1)
                screenSaverInterval = 0; // Default interval.
            else
                screenSaverInterval = interval;

            this.preferBlanking = preferBlanking;
            this.allowExposures = allowExposures;

            ResetScreenSaver();
        }

        /**
         * Called when we'd potentially want to blank the screen.
         */
        private void CheckScreenBlank()
        {
            if (screenSaverTimeout == 0) // Disabled.
                return;

            var offset = (screenSaverTime + screenSaverTimeout) * 1000 - DateTime.UtcNow.Ticks / 1000;

            if (offset < 1000)
            {
                screen.Blank(true);
                return;
            }

            //Debugger.Break();

            //	_screenSaverCountDownTimer = new CountDownTimer(offset, offset + 1)
            //	{
            //			internal void onTick(long millis) { }
            //	internal void onFinish()
            //	{
            //		_screenSaverCountDownTimer = null;
            //		checkScreenBlank();
            //	}
            //};
            //_screenSaverCountDownTimer.start();
        }

        /**
         * Reset the screen saver timer.
         */
        internal void ResetScreenSaver()
        {
            var now = DateTime.UtcNow.Ticks / 1000;

            if (now == screenSaverTime)
                return;

            screenSaverTime = now;
            if (screenSaverCountDownTimer == null)
                CheckScreenBlank();
        }

        /**
         * Reply to GetScreenSaver request.
         *
         * @param client	The remote client.

         */
        internal void WriteScreenSaver(Client client)
        {
            var io = client.GetInputOutput();

            lock (io)
            {
                Util.WriteReplyHeader(client, (byte) 0);
                io.WriteInt(0); // Reply length.
                io.WriteShort((short) screenSaverTimeout); // Timeout.
                io.WriteShort((short) screenSaverInterval); // Interval.
                io.WriteByte((byte) preferBlanking); // Prefer blanking.
                io.WriteByte((byte) allowExposures); // Allow exposures.
                io.WritePadBytes(18); // Unused.
            }

            io.Flush();
        }

        internal class Extension
        {
            internal readonly byte MajorOpcode;
            internal readonly byte FirstEvent;
            internal readonly byte FirstError;

            /**
             * Constructor.
             *
             * @param pmajorOpcode	Major opcode of the extension, or zero.
             * @param pfirstEvent	Base event type code, or zero.
             * @param pfirstError	Base error code, or zero.
             */
            internal Extension(byte pmajorOpcode, byte pfirstEvent, byte pfirstError)
            {
                MajorOpcode = pmajorOpcode;
                FirstEvent = pfirstEvent;
                FirstError = pfirstError;
            }
        }

        private class AcceptThread : InheritableThread
        {
            private readonly TcpListener serverSocket;
            private readonly XServer xServer;

            /**
             * Constructor.
             *
             * @param port	The port to listen on.
             *
    
             */
            internal AcceptThread(int port, XServer server)
            {
                serverSocket = new TcpListener(IPAddress.Any, port);
                xServer = server;
            }

            /**
             * Return the internet address that is accepting connections.
             * May be null.
             *
             * @return	The internet address that is accepting connections.
             */
            internal IPAddress GetInetAddress()
            {
                var temp = serverSocket.LocalEndpoint as IPEndPoint;
                return temp?.Address;
            }

            /*
             * Run the thread.
             */
            internal override void Run()
            {
                serverSocket.Start();
                while (true)
                {
                    TcpClient socket;

                    try
                    {
                        // This is a blocking call and will only return on a
                        // successful connection or an exception.
                        socket = serverSocket.AcceptTcpClient();
                    }
                    catch (Exception)
                    {
                        break;
                    }

                    var addr = 0;
                    IPEndPoint isa;

                    isa = socket.Client.RemoteEndPoint as IPEndPoint;

                    if (isa != null)
                    {
                        var ia = isa.Address;
                        var bytes = ia.GetAddressBytes();

                        addr = (bytes[0] << 24) | (bytes[1] << 16) | (bytes[2] << 8) | bytes[3];
                    }

                    if (addr != 0 && !xServer.IsAccessAllowed(addr))
                    {
                        try
                        {
                            socket.Close();
                        }
                        catch (Exception)
                        {
                        }

                        continue;
                    }

                    lock (this)
                    {
                        Client c;

                        try
                        {
                            //Todo: ugly
                            c = new Client(xServer, socket, xServer.clientIdBase, ClientIdStep - 1);
                            Util.Add(xServer.clients, c);
                            c.Start();
                            xServer.clientIdBase += ClientIdStep;
                        }
                        catch (Exception)
                        {
                            try
                            {
                                socket.Close();
                            }
                            catch (Exception)
                            {
                            }
                        }
                    }
                }
            }

            /*
             * Cancel the thread.
             */
            internal void Cancel()
            {
                try
                {
                    serverSocket.Stop();
                }
                catch (Exception)
                {
                }
            }
        }
    }
}