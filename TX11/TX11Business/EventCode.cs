using TX11Business.UIDependent;

namespace TX11Business
{
    internal class EventCode
    {
        internal const byte KeyPress = 2;
        internal const byte KeyRelease = 3;
        internal const byte ButtonPress = 4;
        internal const byte ButtonRelease = 5;
        internal const byte MotionNotify = 6;
        internal const byte EnterNotify = 7;
        internal const byte LeaveNotify = 8;
        internal const byte FocusIn = 9;
        internal const byte FocusOut = 10;
        internal const byte KeymapNotify = 11;
        internal const byte Expose = 12;
        internal const byte GraphicsExposure = 13;
        internal const byte NoExposure = 14;
        internal const byte VisibilityNotify = 15;
        internal const byte CreateNotify = 16;
        internal const byte DestroyNotify = 17;
        internal const byte UnmapNotify = 18;
        internal const byte MapNotify = 19;
        internal const byte MapRequest = 20;
        internal const byte ReparentNotify = 21;
        internal const byte ConfigureNotify = 22;
        internal const byte ConfigureRequest = 23;
        internal const byte GravityNotify = 24;
        internal const byte ResizeRequest = 25;
        internal const byte CirculateNotify = 26;
        internal const byte CirculateRequest = 27;
        internal const byte PropertyNotify = 28;
        internal const byte SelectionClear = 29;
        internal const byte SelectionRequest = 30;
        internal const byte SelectionNotify = 31;
        internal const byte ColormapNotify = 32;
        internal const byte ClientMessage = 33;
        internal const byte MappingNotify = 34;

        internal const int MaskKeyPress = 0x00000001;
        internal const int MaskKeyRelease = 0x00000002;
        internal const int MaskButtonPress = 0x00000004;
        internal const int MaskButtonRelease = 0x00000008;
        internal const int MaskEnterWindow = 0x00000010;
        internal const int MaskLeaveWindow = 0x00000020;
        internal const int MaskPointerMotion = 0x00000040;
        internal const int MaskPointerMotionHint = 0x00000080;
        internal const int MaskButton1Motion = 0x00000100;
        internal const int MaskButton2Motion = 0x00000200;
        internal const int MaskButton3Motion = 0x00000400;
        internal const int MaskButton4Motion = 0x00000800;
        internal const int MaskButton5Motion = 0x00001000;
        internal const int MaskButtonMotion = 0x00002000;
        internal const int MaskKeymapState = 0x00004000;
        internal const int MaskExposure = 0x00008000;
        internal const int MaskVisibilityChange = 0x00010000;
        internal const int MaskStructureNotify = 0x00020000;
        internal const int MaskResizeRedirect = 0x00040000;
        internal const int MaskSubstructureNotify = 0x00080000;
        internal const int MaskSubstructureRedirect = 0x00100000;
        internal const int MaskFocusChange = 0x00200000;
        internal const int MaskPropertyChange = 0x00400000;
        internal const int MaskColormapChange = 0x00800000;
        internal const int MaskOwnerGrabButton = 0x01000000;

        internal const int MaskAllPointer = MaskButtonPress | MaskButtonRelease | MaskPointerMotion |
                                            MaskPointerMotionHint | MaskButton1Motion | MaskButton2Motion |
                                            MaskButton3Motion | MaskButton4Motion | MaskButton5Motion |
                                            MaskButtonMotion;

        /**
         * Write an event header.
         * 
         * @param client	The client to write to.
         * @param code	The event code.
         * @param arg	Optional first argument.

         */
        private static void WriteHeader(Client client, byte code, int arg)
        {
            var io = client.GetInputOutput();

            io.WriteByte((byte) code);
            io.WriteByte((byte) arg);
            io.WriteShort((short) (client.GetSequenceNumber() & 0xffff));
        }

        /**
         * Send a key press event.
         *
         * @param client	The client to write to.
         * @param timestamp	Time in milliseconds since last server reset.
         * @param keycode	The code of the key that was pressed.
         * @param root	The root window of the event window.
         * @param eventWindow	The window interested in the event.
         * @param child	Child of event window, ancestor of source. Can be null.
         * @param rootX	Pointer root X coordinate at the time of the event.
         * @param rootY	Pointer root Y coordinate at the time of the event.
         * @param eventX	Pointer X coordinate relative to event window.
         * @param eventY	Pointer Y coordinate relative to event window.
         * @param state	Bitmask of the buttons and modifier keys.

         */
        internal static void SendKeyPress(Client client, int timestamp, int keycode, Window root, Window eventWindow,
            Window child, int rootX, int rootY, int eventX, int eventY, int state)
        {
            var io = client.GetInputOutput();

            lock (io)
            {
                WriteHeader(client, KeyPress, keycode);
                io.WriteInt(timestamp); // Time.
                io.WriteInt(root.GetId()); // Root.
                io.WriteInt(eventWindow.GetId()); // Event.
                io.WriteInt(child == null ? 0 : child.GetId()); // Child.
                io.WriteShort((short) rootX); // Root X.
                io.WriteShort((short) rootY); // Root Y.
                io.WriteShort((short) eventX); // Event X.
                io.WriteShort((short) eventY); // Event Y.
                io.WriteShort((short) state); // State.
                io.WriteByte((byte) 1); // Same screen.
                io.WritePadBytes(1); // Unused.
            }

            io.Flush();
        }

        /**
         * Send a key release event.
         *
         * @param client	The client to write to.
         * @param timestamp	Time in milliseconds since last server reset.
         * @param keycode	The code of the key that was released.
         * @param root	The root window of the event window.
         * @param eventWindow	The window interested in the event.
         * @param child	Child of event window, ancestor of source. Can be null.
         * @param rootX	Pointer root X coordinate at the time of the event.
         * @param rootY	Pointer root Y coordinate at the time of the event.
         * @param eventX	Pointer X coordinate relative to event window.
         * @param eventY	Pointer Y coordinate relative to event window.
         * @param state	Bitmask of the buttons and modifier keys.

         */
        internal static void SendKeyRelease(Client client, int timestamp, int keycode, Window root, Window eventWindow,
            Window child, int rootX, int rootY, int eventX, int eventY, int state)
        {
            var io = client.GetInputOutput();

            lock (io)
            {
                WriteHeader(client, KeyRelease, keycode);
                io.WriteInt(timestamp); // Time.
                io.WriteInt(root.GetId()); // Root.
                io.WriteInt(eventWindow.GetId()); // Event.
                io.WriteInt(child == null ? 0 : child.GetId()); // Child.
                io.WriteShort((short) rootX); // Root X.
                io.WriteShort((short) rootY); // Root Y.
                io.WriteShort((short) eventX); // Event X.
                io.WriteShort((short) eventY); // Event Y.
                io.WriteShort((short) state); // State.
                io.WriteByte((byte) 1); // Same screen.
                io.WritePadBytes(1); // Unused.
            }

            io.Flush();
        }

        /**
         * Send a button press event.
         *
         * @param client	The client to write to.
         * @param timestamp	Time in milliseconds since last server reset.
         * @param button	The button that was pressed.
         * @param root	The root window of the event window.
         * @param eventWindow	The window interested in the event.
         * @param child	Child of event window, ancestor of source. Can be null.
         * @param rootX	Pointer root X coordinate at the time of the event.
         * @param rootY	Pointer root Y coordinate at the time of the event.
         * @param eventX	Pointer X coordinate relative to event window.
         * @param eventY	Pointer Y coordinate relative to event window.
         * @param state	Bitmask of the buttons and modifier keys.

         */
        internal static void SendButtonPress(Client client, int timestamp, int button, Window root, Window eventWindow,
            Window child, int rootX, int rootY, int eventX, int eventY, int state)
        {
            var io = client.GetInputOutput();

            lock (io)
            {
                WriteHeader(client, ButtonPress, button);
                io.WriteInt(timestamp); // Time.
                io.WriteInt(root.GetId()); // Root.
                io.WriteInt(eventWindow.GetId()); // Event.
                io.WriteInt(child == null ? 0 : child.GetId()); // Child.
                io.WriteShort((short) rootX); // Root X.
                io.WriteShort((short) rootY); // Root Y.
                io.WriteShort((short) eventX); // Event X.
                io.WriteShort((short) eventY); // Event Y.
                io.WriteShort((short) state); // State.
                io.WriteByte((byte) 1); // Same screen.
                io.WritePadBytes(1); // Unused.
            }

            io.Flush();
        }

        /**
         * Send a button release event.
         *
         * @param client	The client to write to.
         * @param timestamp	Time in milliseconds since last server reset.
         * @param button	The button that was released.
         * @param root	The root window of the event window.
         * @param eventWindow	The window interested in the event.
         * @param child	Child of event window, ancestor of source. Can be null.
         * @param rootX	Pointer root X coordinate at the time of the event.
         * @param rootY	Pointer root Y coordinate at the time of the event.
         * @param eventX	Pointer X coordinate relative to event window.
         * @param eventY	Pointer Y coordinate relative to event window.
         * @param state	Bitmask of the buttons and modifier keys.

         */
        internal static void SendButtonRelease(Client client, int timestamp, int button, Window root,
            Window eventWindow, Window child, int rootX, int rootY, int eventX, int eventY, int state)
        {
            var io = client.GetInputOutput();

            lock (io)
            {
                WriteHeader(client, ButtonRelease, button);
                io.WriteInt(timestamp); // Time.
                io.WriteInt(root.GetId()); // Root.
                io.WriteInt(eventWindow.GetId()); // Event.
                io.WriteInt(child == null ? 0 : child.GetId()); // Child.
                io.WriteShort((short) rootX); // Root X.
                io.WriteShort((short) rootY); // Root Y.
                io.WriteShort((short) eventX); // Event X.
                io.WriteShort((short) eventY); // Event Y.
                io.WriteShort((short) state); // State.
                io.WriteByte((byte) 1); // Same screen.
                io.WritePadBytes(1); // Unused.
            }

            io.Flush();
        }

        /**
         * Send a motion notify event.
         *
         * @param client	The client to write to.
         * @param timestamp	Time in milliseconds since last server reset.
         * @param detail	0=Normal, 1=Hint.
         * @param root	The root window of the event window.
         * @param eventWindow	The window interested in the event.
         * @param child	Child of event window, ancestor of source. Can be null.
         * @param rootX	Pointer root X coordinate at the time of the event.
         * @param rootY	Pointer root Y coordinate at the time of the event.
         * @param eventX	Pointer X coordinate relative to event window.
         * @param eventY	Pointer Y coordinate relative to event window.
         * @param state	Bitmask of the buttons and modifier keys.

         */
        internal static void SendMotionNotify(Client client, int timestamp, int detail, Window root, Window eventWindow,
            Window child, int rootX, int rootY, int eventX, int eventY, int state)
        {
            var io = client.GetInputOutput();

            lock (io)
            {
                WriteHeader(client, MotionNotify, detail);
                io.WriteInt(timestamp); // Time.
                io.WriteInt(root.GetId()); // Root.
                io.WriteInt(eventWindow.GetId()); // Event.
                io.WriteInt(child == null ? 0 : child.GetId()); // Child.
                io.WriteShort((short) rootX); // Root X.
                io.WriteShort((short) rootY); // Root Y.
                io.WriteShort((short) eventX); // Event X.
                io.WriteShort((short) eventY); // Event Y.
                io.WriteShort((short) state); // State.
                io.WriteByte((byte) 1); // Same screen.
                io.WritePadBytes(1); // Unused.
            }

            io.Flush();
        }

        /**
         * Send an enter notify event.
         *
         * @param client	The client to write to.
         * @param timestamp	Time in milliseconds since last server reset.
         * @param detail	0=Ancestor, 1=Virtual, 2=Inferior, 3=Nonlinear, 4=NonlinearVirtual.
         * @param root	The root window of the event window.
         * @param eventWindow	The window interested in the event.
         * @param child	Child of event window, ancestor of source. Can be null.
         * @param rootX	Pointer root X coordinate at the time of the event.
         * @param rootY	Pointer root Y coordinate at the time of the event.
         * @param eventX	Pointer X coordinate relative to event window.
         * @param eventY	Pointer Y coordinate relative to event window.
         * @param state	Bitmask of the buttons and modifier keys.
         * @param mode	0=Normal, 1=Grab, 2=Ungrab.
         * @param focus	Is the event window the focus window or an inferior of it?

         */
        internal static void SendEnterNotify(Client client, int timestamp, int detail, Window root, Window eventWindow,
            Window child, int rootX, int rootY, int eventX, int eventY, int state, int mode, bool focus)
        {
            var io = client.GetInputOutput();

            lock (io)
            {
                WriteHeader(client, EnterNotify, detail);
                io.WriteInt(timestamp); // Time.
                io.WriteInt(root.GetId()); // Root.
                io.WriteInt(eventWindow.GetId()); // Event.
                io.WriteInt(child == null ? 0 : child.GetId()); // Child.
                io.WriteShort((short) rootX); // Root X.
                io.WriteShort((short) rootY); // Root Y.
                io.WriteShort((short) eventX); // Event X.
                io.WriteShort((short) eventY); // Event Y.
                io.WriteShort((short) state); // State.
                io.WriteByte((byte) mode); // Mode.
                io.WriteByte((byte) (focus ? 3 : 2)); // Same screen, focus.
            }

            io.Flush();
        }

        /**
         * Send a leave notify event.
         *
         * @param client	The client to write to.
         * @param timestamp	Time in milliseconds since last server reset.
         * @param detail	0=Ancestor, 1=Virtual, 2=Inferior, 3=Nonlinear,
         *												4=NonlinearVirtual.
         * @param root	The root window of the event window.
         * @param eventWindow	The window interested in the event.
         * @param child	Child of event window, ancestor of source. Can be null.
         * @param rootX	Pointer root X coordinate at the time of the event.
         * @param rootY	Pointer root Y coordinate at the time of the event.
         * @param eventX	Pointer X coordinate relative to event window.
         * @param eventY	Pointer Y coordinate relative to event window.
         * @param state	Bitmask of the buttons and modifier keys.
         * @param mode	0=Normal, 1=Grab, 2=Ungrab.
         * @param focus	Is the event window the focus window or an inferior of it?

         */
        internal static void SendLeaveNotify(Client client, int timestamp, int detail, Window root, Window eventWindow,
            Window child, int rootX, int rootY, int eventX, int eventY, int state, int mode, bool focus)
        {
            var io = client.GetInputOutput();

            lock (io)
            {
                WriteHeader(client, LeaveNotify, detail);
                io.WriteInt(timestamp); // Time.
                io.WriteInt(root.GetId()); // Root.
                io.WriteInt(eventWindow.GetId()); // Event.
                io.WriteInt(child == null ? 0 : child.GetId()); // Child.
                io.WriteShort((short) rootX); // Root X.
                io.WriteShort((short) rootY); // Root Y.
                io.WriteShort((short) eventX); // Event X.
                io.WriteShort((short) eventY); // Event Y.
                io.WriteShort((short) state); // State.
                io.WriteByte((byte) mode); // Mode.
                io.WriteByte((byte) (focus ? 3 : 2)); // Same screen, focus.
            }

            io.Flush();
        }

        /**
         * Send a focus in event.
         *
         * @param client	The client to write to.
         * @param timestamp	Time in milliseconds since last server reset.
         * @param detail	0=Ancestor, 1=Virtual, 2=Inferior, 3=Nonlinear,
         *					4=NonlinearVirtual, 5=Pointer, 6=PinterRoot, 7=None.
         * @param eventWindow	The window interested in the event.
         * @param mode	0=Normal, 1=Grab, 2=Ungrab, 3=WhileGrabbed.

         */
        internal static void SendFocusIn(Client client, int timestamp, int detail, Window eventWindow, int mode)
        {
            var io = client.GetInputOutput();

            lock (io)
            {
                WriteHeader(client, FocusIn, detail);
                io.WriteInt(eventWindow.GetId()); // Event.
                io.WriteByte((byte) mode); // Mode.
                io.WritePadBytes(23); // Unused.
            }

            io.Flush();
        }

        /**
         * Send a focus out event.
         *
         * @param client	The client to write to.
         * @param timestamp	Time in milliseconds since last server reset.
         * @param detail	0=Ancestor, 1=Virtual, 2=Inferior, 3=Nonlinear,
         *					4=NonlinearVirtual, 5=Pointer, 6=PinterRoot, 7=None.
         * @param eventWindow	The window interested in the event.
         * @param mode	0=Normal, 1=Grab, 2=Ungrab, 3=WhileGrabbed.

         */
        internal static void SendFocusOut(Client client, int timestamp, int detail, Window eventWindow, int mode)
        {
            var io = client.GetInputOutput();

            lock (io)
            {
                WriteHeader(client, FocusOut, detail);
                io.WriteInt(eventWindow.GetId()); // Event.
                io.WriteByte((byte) mode); // Mode.
                io.WritePadBytes(23); // Unused.
            }

            io.Flush();
        }

        /**
         * Send a keymap notify event.
         *
         * @param client	The client to write to.
         * @param keys	A bit vector of the logical state of the keyboard.

         */
        internal static void SendKeymapNotify(Client client, byte[] keys)
        {
            var io = client.GetInputOutput();

            lock (io)
            {
                io.WriteByte(KeymapNotify);
                io.WriteBytes(keys, 0, 31); // Keys.
            }

            io.Flush();
        }

        /**
         * Send an expose event.
         *
         * @param client	The client to write to.
         * @param window	The window where the exposure occurred.
         * @param x	Left of the exposed rectangle.
         * @param y	Top of the exposed rectangle.
         * @param width	Width of the exposed rectangle.
         * @param height	Height of the exposed rectangle.
         * @param count	Number of rectangles remaining in the exposed region.

         */
        internal static void SendExpose(Client client, Window window, int x, int y, int width, int height, int count)
        {
            var io = client.GetInputOutput();

            lock (io)
            {
                WriteHeader(client, Expose, 0);
                io.WriteInt(window.GetId()); // Event.
                io.WriteShort((short) x); // X.
                io.WriteShort((short) y); // Y.
                io.WriteShort((short) width); // Width.
                io.WriteShort((short) height); // Height.
                io.WriteShort((short) count); // Count.
                io.WritePadBytes(14); // Unused.
            }

            io.Flush();
        }

        /**
         * Send a graphics exposure event.
         *
         * @param client	The client to write to.
         * @param drawable	The drawable where the exposure occurred.
         * @param majorOpcode	The graphics request that caused the event.
         *						Either CopyArea or CopyPlane.
         * @param x	Left of the exposed rectangle.
         * @param y	Top of the exposed rectangle.
         * @param width	Width of the exposed rectangle.
         * @param height	Height of the exposed rectangle.
         * @param count	Number of rectangles remaining in the exposed region.

         */
        internal static void SendGraphicsExposure(Client client, Resource drawable, byte majorOpcode, int x, int y,
            int width, int height, int count)
        {
            var io = client.GetInputOutput();

            lock (io)
            {
                WriteHeader(client, GraphicsExposure, 0);
                io.WriteInt(drawable.GetId()); // Drawable.
                io.WriteShort((short) x); // X.
                io.WriteShort((short) y); // Y.
                io.WriteShort((short) width); // Width.
                io.WriteShort((short) height); // Height.
                io.WriteShort((short) 0); // Minor opcode.
                io.WriteShort((short) count); // Count.
                io.WriteByte((byte) majorOpcode); // Major opcode.
                io.WritePadBytes(11); // Unused.
            }

            io.Flush();
        }

        /**
         * Send a no exposure event.
         *
         * @param client	The client to write to.
         * @param drawable	The drawable where no exposure exposure occurred.
         * @param majorOpcode	The graphics request that caused the event.
         *						Either CopyArea or CopyPlane.

         */
        internal static void SendNoExposure(Client client, Resource drawable, byte majorOpcode)
        {
            var io = client.GetInputOutput();

            lock (io)
            {
                WriteHeader(client, NoExposure, 0);
                io.WriteInt(drawable.GetId()); // Drawable.
                io.WriteShort((short) 0); // Minor opcode.
                io.WriteByte((byte) majorOpcode); // Major opcode.
                io.WritePadBytes(21); // Unused.
            }

            io.Flush();
        }

        /**
         * Send a visibility notify event.
         *
         * @param client	The client to write to.
         * @param window	The window where the exposure occurred.
         * @param state	0=Unobscured, 1=PartiallyObscured, 2=FullyObscured.

         */
        internal static void SendVisibilityNotify(Client client, Window window, int state)
        {
            var io = client.GetInputOutput();

            lock (io)
            {
                WriteHeader(client, VisibilityNotify, 0);
                io.WriteInt(window.GetId()); // Event.
                io.WriteByte((byte) state); // State.
                io.WritePadBytes(23); // Unused.
            }

            io.Flush();
        }

        /**
         * Send a create notify event.
         *
         * @param client	The client to write to.
         * @param parent	The parent of the window that was created.
         * @param window	The window that was created.
         * @param x	X position of the created window.
         * @param y	Y position of the created window.
         * @param width	Width of the created window.
         * @param height	Height of the created window.
         * @param borderWidth	Border width of the created window.
         * @param overrideRedirect	Does the created window use override redirect?

         */
        internal static void SendCreateNotify(Client client, Window parent, Window window, int x, int y, int width,
            int height, int borderWidth, bool overrideRedirect)
        {
            var io = client.GetInputOutput();

            lock (io)
            {
                WriteHeader(client, CreateNotify, 0);
                io.WriteInt(parent.GetId()); // Parent.
                io.WriteInt(window.GetId()); // Window.
                io.WriteShort((short) x); // X.
                io.WriteShort((short) y); // Y.
                io.WriteShort((short) width); // Width.
                io.WriteShort((short) height); // Height.
                io.WriteShort((short) borderWidth); // Border width.
                io.WriteByte((byte) (overrideRedirect ? 1 : 0));
                io.WritePadBytes(9); // Unused.
            }

            io.Flush();
        }

        /**
         * Send a destroy notify event.
         *
         * @param client	The client to write to.
         * @param eventWindow	The window where the event was generated.
         * @param window	The window that was destroyed.

         */
        internal static void SendDestroyNotify(Client client, Window eventWindow, Window window)
        {
            var io = client.GetInputOutput();

            lock (io)
            {
                WriteHeader(client, DestroyNotify, 0);
                io.WriteInt(eventWindow.GetId()); // Event.
                io.WriteInt(window.GetId()); // Window.
                io.WritePadBytes(20); // Unused.
            }

            io.Flush();
        }

        /**
         * Send an unmap notify event.
         *
         * @param client	The client to write to.
         * @param eventWindow	The window where the event was generated.
         * @param window	The window that was unmapped.
         * @param fromConfigure	True if event was caused by parent being resized.

         */
        internal static void SendUnmapNotify(Client client, Window eventWindow, Window window, bool fromConfigure)
        {
            var io = client.GetInputOutput();

            lock (io)
            {
                WriteHeader(client, UnmapNotify, 0);
                io.WriteInt(eventWindow.GetId()); // Event.
                io.WriteInt(window.GetId()); // Window.
                io.WriteByte((byte) (fromConfigure ? 1 : 0)); // From configure.
                io.WritePadBytes(19); // Unused.
            }

            io.Flush();
        }

        /**
         * Send a map notify event.
         *
         * @param client	The client to write to.
         * @param eventWindow	The window where the event was generated.
         * @param window	The window that was mapped.
         * @param overrideRedirect	True if the window uses override redirect.

         */
        internal static void SendMapNotify(Client client, Window eventWindow, Window window, bool overrideRedirect)
        {
            var io = client.GetInputOutput();

            lock (io)
            {
                WriteHeader(client, MapNotify, 0);
                io.WriteInt(eventWindow.GetId()); // Event.
                io.WriteInt(window.GetId()); // Window.
                io.WriteByte((byte) (overrideRedirect ? 1 : 0));
                io.WritePadBytes(19); // Unused.
            }

            io.Flush();
        }

        /**
         * Send a map request event.
         *
         * @param client	The client to write to.
         * @param eventWindow	The window where the event was generated.
         * @param window	The window that was mapped.

         */
        internal static void SendMapRequest(Client client, Window eventWindow, Window window)
        {
            var io = client.GetInputOutput();

            lock (io)
            {
                WriteHeader(client, MapRequest, 0);
                io.WriteInt(eventWindow.GetId()); // Event.
                io.WriteInt(window.GetId()); // Window.
                io.WritePadBytes(20); // Unused.
            }

            io.Flush();
        }

        /**
         * Send a reparent notify event.
         *
         * @param client	The client to write to.
         * @param eventWindow	The window where the event was generated.
         * @param window	The window that has been rerooted.
         * @param parent	The window's new parent.
         * @param x	X position of the window relative to the new parent.
         * @param y	Y position of the window relative to the new parent.
         * @param overrideRedirect	Does the window use override redirect?

         */
        internal static void SendReparentNotify(Client client, Window eventWindow, Window window, Window parent, int x,
            int y, bool overrideRedirect)
        {
            var io = client.GetInputOutput();

            lock (io)
            {
                WriteHeader(client, ReparentNotify, 0);
                io.WriteInt(eventWindow.GetId()); // Event.
                io.WriteInt(window.GetId()); // Window.
                io.WriteInt(parent.GetId()); // Parent.
                io.WriteShort((short) x); // X.
                io.WriteShort((short) y); // Y.
                io.WriteByte((byte) (overrideRedirect ? 1 : 0));
                io.WritePadBytes(11); // Unused.
            }

            io.Flush();
        }

        /**
         * Send a configure notify event.
         *
         * @param client	The client to write to.
         * @param eventWindow	The window where the event was generated.
         * @param window	The window that was changed.
         * @param aboveSibling	The sibling window beneath it. May be null.
         * @param x	X position of the window relative to its parent.
         * @param y	Y position of the window relative to its parent.
         * @param width	Width of the window.
         * @param height	Height of the window.
         * @param borderWidth	Border width of the window.
         * @param overrideRedirect	Does the window use override redirect?

         */
        internal static void SendConfigureNotify(Client client, Window eventWindow, Window window, Window aboveSibling,
            int x, int y, int width, int height, int borderWidth, bool overrideRedirect)
        {
            var io = client.GetInputOutput();

            lock (io)
            {
                WriteHeader(client, ConfigureNotify, 0);
                io.WriteInt(eventWindow.GetId()); // Event.
                io.WriteInt(window.GetId()); // Window.
                io.WriteInt(aboveSibling == null ? 0 : aboveSibling.GetId());
                io.WriteShort((short) x); // X.
                io.WriteShort((short) y); // Y.
                io.WriteShort((short) width); // Width.
                io.WriteShort((short) height); // Height.
                io.WriteShort((short) borderWidth); // Border width.
                io.WriteByte((byte) (overrideRedirect ? 1 : 0));
                io.WritePadBytes(5); // Unused.
            }

            io.Flush();
        }

        /**
         * Send a configure request event.
         *
         * @param client	The client to write to.
         * @param stackMode	0=Above, 1=Below, 2=TopIf, 3=BottomIf, 4=Opposite
         * @param parent	The parent of the window.
         * @param window	The window that the ConfigureWindow was issued to.
         * @param sibling	The sibling window beneath it. May be null.
         * @param x	X position of the window.
         * @param y	Y position of the window.
         * @param width	Width of the window.
         * @param height	Height of the window.
         * @param borderWidth	Border width of the window.
         * @param valueMask	Components specified in the ConfigureWindow request.

         */
        internal static void SendConfigureRequest(Client client, int stackMode, Window parent, Window window,
            Window sibling, int x, int y, int width, int height, int borderWidth, int valueMask)
        {
            var io = client.GetInputOutput();

            lock (io)
            {
                WriteHeader(client, ConfigureRequest, stackMode);
                io.WriteInt(parent.GetId()); // Parent.
                io.WriteInt(window.GetId()); // Window.
                io.WriteInt(sibling == null ? 0 : sibling.GetId()); // Sibling.
                io.WriteShort((short) x); // X.
                io.WriteShort((short) y); // Y.
                io.WriteShort((short) width); // Width.
                io.WriteShort((short) height); // Height.
                io.WriteShort((short) borderWidth); // Border width.
                io.WriteShort((short) valueMask); // Value mask.
                io.WritePadBytes(4); // Unused.
            }

            io.Flush();
        }

        /**
         * Send a gravity notify event.
         *
         * @param client	The client to write to.
         * @param eventWindow	The window where the event was generated.
         * @param window	The window that was moved because parent changed size.
         * @param x	X position of the window relative to the parent.
         * @param y	Y position of the window relative to the parent.

         */
        internal static void SendGravityNotify(Client client, Window eventWindow, Window window, int x, int y)
        {
            var io = client.GetInputOutput();

            lock (io)
            {
                WriteHeader(client, GravityNotify, 0);
                io.WriteInt(eventWindow.GetId()); // Event.
                io.WriteInt(window.GetId()); // Window.
                io.WriteShort((short) x); // X.
                io.WriteShort((short) y); // Y.
                io.WritePadBytes(16); // Unused.
            }

            io.Flush();
        }

        /**
         * Send a resize request event.
         *
         * @param client	The client to write to.
         * @param window	A client is attempting to resize this window.
         * @param width	Requested width of the window.
         * @param height	Requested height of the window.

         */
        internal static void SendResizeRequest(Client client, Window window, int width, int height)
        {
            var io = client.GetInputOutput();

            lock (io)
            {
                WriteHeader(client, ResizeRequest, 0);
                io.WriteInt(window.GetId()); // Window.
                io.WriteShort((short) width); // Width.
                io.WriteShort((short) height); // Height.
                io.WritePadBytes(20); // Unused.
            }

            io.Flush();
        }

        /**
         * Send a circulate notify event.
         *
         * @param client	The client to write to.
         * @param eventWindow	The window where the event was generated.
         * @param window	The window that was restacked.
         * @param place	0=Top, 1=Bottom.

         */
        internal static void SendCirculateNotify(Client client, Window eventWindow, Window window, int place)
        {
            var io = client.GetInputOutput();

            lock (io)
            {
                WriteHeader(client, CirculateNotify, 0);
                io.WriteInt(eventWindow.GetId()); // Event.
                io.WriteInt(window.GetId()); // Window.
                io.WritePadBytes(4); // Unused.
                io.WriteByte((byte) place); // Place.
                io.WritePadBytes(15); // Unused.
            }

            io.Flush();
        }

        /**
         * Send a circulate request event.
         *
         * @param client	The client to write to.
         * @param parent	The parent of the window.
         * @param window	The window that needs to be restacked.
         * @param place	0=Top, 1=Bottom.

         */
        internal static void SendCirculateRequest(Client client, Window parent, Window window, int place)
        {
            var io = client.GetInputOutput();

            lock (io)
            {
                WriteHeader(client, CirculateRequest, 0);
                io.WriteInt(parent.GetId()); // Parent.
                io.WriteInt(window.GetId()); // Window.
                io.WritePadBytes(4); // Unused.
                io.WriteByte((byte) place); // Place.
                io.WritePadBytes(15); // Unused.
            }

            io.Flush();
        }

        /**
         * Send a property notify event.
         *
         * @param client	The client to write to.
         * @param window	The window being changed
         * @param atom	The property being changed.
         * @param timestamp	Time in milliseconds when the property was changed.
         * @param state	0=NewValue, 1=Deleted.

         */
        internal static void SendPropertyNotify(Client client, Window window, Atom atom, int timestamp, int state)
        {
            var io = client.GetInputOutput();

            lock (io)
            {
                WriteHeader(client, PropertyNotify, 0);
                io.WriteInt(window.GetId()); // Window.
                io.WriteInt(atom.GetId()); // Atom.
                io.WriteInt(timestamp); // Time.
                io.WriteByte((byte) state); // State.
                io.WritePadBytes(15); // Unused.
            }

            io.Flush();
        }

        /**
         * Send a selection clear event.
         *
         * @param client	The client to write to.
         * @param timestamp	Last-change time for the selection.
         * @param window	Previous owner of the selection.
         * @param atom	The selection.

         */
        internal static void SendSelectionClear(Client client, int timestamp, Window window, Atom atom)
        {
            var io = client.GetInputOutput();

            lock (io)
            {
                WriteHeader(client, SelectionClear, 0);
                io.WriteInt(timestamp); // Time.
                io.WriteInt(window.GetId()); // Owner.
                io.WriteInt(atom.GetId()); // Selection.
                io.WritePadBytes(16); // Unused.
            }

            io.Flush();
        }

        /**
         * Send a selection request event.
         *
         * @param client	The client to write to.
         * @param timestamp	Last-change time for the selection.
         * @param owner	Current owner of the selection.
         * @param requestor	Window requesting change of selection.
         * @param selection	The selection whose owner changed.
         * @param target	Target atom.
         * @param property	Property atom. May be null.

         */
        internal static void SendSelectionRequest(Client client, int timestamp, Window owner, Window requestor,
            Atom selection, Atom target, Atom property)
        {
            var io = client.GetInputOutput();

            lock (io)
            {
                WriteHeader(client, SelectionRequest, 0);
                io.WriteInt(timestamp); // Time.
                io.WriteInt(owner.GetId()); // Owner.
                io.WriteInt(requestor.GetId()); // Requestor.
                io.WriteInt(selection.GetId()); // Selection.
                io.WriteInt(target.GetId()); // Target.
                io.WriteInt(property == null ? 0 : property.GetId());
                io.WritePadBytes(4); // Unused.
            }

            io.Flush();
        }

        /**
         * Send a selection notify event.
         *
         * @param client	The client to write to.
         * @param timestamp	Last-change time for the selection.
         * @param requestor	Window requesting change of selection.
         * @param selection	The selection whose owner changed.
         * @param target	Target atom.
         * @param property	Property atom. May be null.

         */
        internal static void SendSelectionNotify(Client client, int timestamp, Window requestor, Atom selection,
            Atom target, Atom property)
        {
            var io = client.GetInputOutput();

            lock (io)
            {
                WriteHeader(client, SelectionNotify, 0);
                io.WriteInt(timestamp); // Time.
                io.WriteInt(requestor.GetId()); // Requestor.
                io.WriteInt(selection.GetId()); // Selection.
                io.WriteInt(target.GetId()); // Target.
                io.WriteInt(property == null ? 0 : property.GetId());
                io.WritePadBytes(8); // Unused.
            }

            io.Flush();
        }

        /**
         * Send a colormap notify event.
         *
         * @param client	The client to write to.
         * @param window	The window the colormap belongs to.
         * @param cmap	The colormap. May be null.
         * @param isNew	True=colormap changed, False=colormap un/installed.
         * @param state	0=Uninstalled, 1=Installed.

         */
        internal static void SendColormapNotify(Client client, Window window, Colormap cmap, bool isNew, int state)
        {
            var io = client.GetInputOutput();

            lock (io)
            {
                WriteHeader(client, ColormapNotify, 0);
                io.WriteInt(window.GetId()); // Window.
                io.WriteInt(cmap == null ? 0 : cmap.GetId()); // Colormap.
                io.WriteByte((byte) (isNew ? 1 : 0)); // New.
                io.WriteByte((byte) state); // State.
                io.WritePadBytes(18); // Unused.
            }

            io.Flush();
        }

        /**
         * Send a mapping notify event.
         *
         * @param client	The client to write to.
         * @param request	0=Modifier, 1=Keyboard, 2=Pointer.
         * @param firstKeycode	Start of altered keycodes if request=Keyboard.
         * @param count	Size of altered keycode range if request=Keyboard.

         */
        internal static void SendMappingNotify(Client client, int request, int firstKeycode, int count)
        {
            var io = client.GetInputOutput();

            lock (io)
            {
                WriteHeader(client, MappingNotify, 0);
                io.WriteByte((byte) request); // Request.
                io.WriteByte((byte) firstKeycode); // First keycode.
                io.WriteByte((byte) count); // Count.
                io.WritePadBytes(25); // Unused.
            }

            io.Flush();
        }
    }
}