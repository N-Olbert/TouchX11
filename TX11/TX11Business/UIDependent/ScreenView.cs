using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Threading;
using JetBrains.Annotations;
using TX11Business.BusinessObjects;
using TX11Business.BusinessObjects.Events;
using TX11Shared;
using TX11Shared.Graphics;
using TX11Shared.Keyboard;

namespace TX11Business.UIDependent
{
    internal class ScreenView : IXScreenObserver
    {
        [NotNull]
        private readonly XServer xServer;
        [NotNull]
        private readonly Window rootWindow;
        [NotNull]
        private readonly List<Colormap> installedColormaps;
        [NotNull]
        private readonly IXPaint paint;
        [NotNull, ItemNotNull]
        private readonly Queue<PendingPointerEvent> pendingPointerEvents;
        [NotNull, ItemNotNull]
        private readonly Queue<PendingKeyboardEvent> pendingKeyboardEvents;
        private readonly float pixelsPerMillimeter;
        private readonly int rootId;

        private Colormap defaultColormap;
        private Cursor currentCursor;
        private int currentCursorX;
        private int currentCursorY;
        private Cursor drawnCursor;
        private Window motionWindow;
        private int motionX;
        private int motionY;
        private int buttons;
        private bool isBlanked;
        

        private Client grabPointerClient;
        private Window grabPointerWindow;
        private int grabPointerTime;
        private bool grabPointerOwnerEvents;
        private bool grabPointerSynchronous;
        private bool grabPointerPassive;
        private bool grabPointerAutomatic;
        private bool _grabPointerFreezeNextEvent = false;
        private Client grabKeyboardClient;
        private Window grabKeyboardWindow;
        private int grabKeyboardTime;
        private bool grabKeyboardOwnerEvents;
        private bool grabKeyboardSynchronous;
        private bool _grabKeyboardFreezeNextEvent = false;
        private Cursor grabCursor;
        private Window grabConfineWindow;
        private int grabEventMask;
        private PassiveKeyGrab grabKeyboardPassiveGrab;

        private Window focusWindow;
        private byte focusRevertTo; // 0=None, 1=Root, 2=Parent.
        private int focusLastChangeTime;


        /**
	 * Constructor.
	 *
	 * @param c	The application context.
	 * @param xServer	The X server.
	 * @param rootId	The ID of the root window, to be created later.
	 * @param pixelsPerMillimeter	Screen resolution.
	 */
        internal ScreenView(XServer xServer, int rootId, float pixelsPerMillimeter)
        {
            this.xServer = xServer;
            this.rootId = rootId;
            this.installedColormaps = new List<Colormap>();
            this.pixelsPerMillimeter = pixelsPerMillimeter;
            this.pixelsPerMillimeter = 3.779527f;
            this.paint = Util.GetPaint();

            this.pendingPointerEvents = new Queue<PendingPointerEvent>();
            this.pendingKeyboardEvents = new Queue<PendingKeyboardEvent>();

            this.rootWindow = new Window(this.rootId, this.xServer, null, this, null, 0, 0, GetWidth(), GetHeight(), 0,
                                         false, true);
            this.xServer.AddResource(this.rootWindow);

            this.currentCursor = this.rootWindow.GetCursor();
            this.currentCursorX = GetWidth() / 2;
            this.currentCursorY = GetHeight() / 2;
            this.motionWindow = this.rootWindow;
            this.focusWindow = this.rootWindow;
        }

        ///**
        // * Placeholder constructor to prevent a compiler warning.
        // * @param c
        // */
        //internal ScreenView(
        //	Context c
        //)
        //{
        //	super(c);

        //	_xServer = null;
        //	_rootId = 0;
        //	_installedColormaps = null;
        //	_pixelsPerMillimeter = 0;
        //}
        /**
	 * Return the screen's root window.
	 *
	 * @return	The screen's root window.
	 */
        internal Window GetRootWindow()
        {
            return this.rootWindow;
        }

        internal short GetHeight()
        {
            var screen = XConnector.Resolve<IXScreen>();
            return (short) screen.RealHeight;
        }

        internal short GetWidth()
        {
            var screen = XConnector.Resolve<IXScreen>();
            return (short) screen.RealWidth;
        }

        /**
		 * Return the screen's default colormap.
		 *
		 * @return	The screen's default colormap.
		 */
        internal Colormap GetDefaultColormap()
        {
            return this.defaultColormap;
        }

        /**
	 * Return the current cursor.
	 *
	 * @return	The current cursor.
	 */
        internal Cursor GetCurrentCursor()
        {
            return this.currentCursor;
        }

        /**
	 * Return the current pointer X coordinate.
	 *
	 * @return	The current pointer X coordinate.
	 */
        internal int GetPointerX()
        {
            return this.currentCursorX;
        }

        /**
	 * Return the current pointer Y coordinate.
	 *
	 * @return	The current pointer Y coordinate.
	 */
        internal int GetPointerY()
        {
            return this.currentCursorY;
        }

        /**
	 * Return a mask indicating the current state of the pointer and
	 * modifier buttons.
	 *
	 * @return	A mask indicating the current state of the buttons.
	 */
        internal int GetButtons()
        {
            return this.buttons;
        }

        /**
	 * Return the window that has input focus. Can be null.
	 *
	 * @return	The window that has input focus.
	 */
        internal Window GetFocusWindow()
        {
            return this.focusWindow;
        }

        /**
	 * Blank/unblank the screen.
	 *
	 * @param flag	If true, blank the screen. Otherwise unblank it.
	 */
        internal void Blank(bool flag)
        {
            if (this.isBlanked == flag)
                return;

            this.isBlanked = flag;
            PostInvalidate();

            if (!this.isBlanked)
                this.xServer.ResetScreenSaver();
        }

        internal void PostInvalidate(int left, int top, int v1, int v2)
        {
            XConnector.Resolve<IXScreen>().Invalidate();
        }

        internal void PostInvalidate()
        {
            XConnector.Resolve<IXScreen>().Invalidate();
        }

        /**
		 * Add an installed colormap.
		 *
		 * @param cmap	The colormap to add.
		 */
        internal void AddInstalledColormap(Colormap cmap)
        {
            Util.Add(this.installedColormaps, cmap);
            if (this.defaultColormap == null)
            {
                this.defaultColormap = cmap;
                this.rootWindow?.SetColormap(cmap); //fix for  fvwm
            }
        }

        /**
	 * Remove an installed colormap.
	 *
	 * @param cmap	The colormap to remove.
	 */
        internal void RemoveInstalledColormap(Colormap cmap)
        {
            Util.Remove(this.installedColormaps, cmap);
            if (this.defaultColormap == cmap)
            {
                if (this.installedColormaps.Size() == 0)
                    this.defaultColormap = null;
                else
                    this.defaultColormap = this.installedColormaps.FirstOrDefault();
            }
        }

        /**
	 * Remove all colormaps except the default one.
	 */
        internal void RemoveNonDefaultColormaps()
        {
            if (this.installedColormaps.Size() < 2)
                return;

            Util.Clear(this.installedColormaps);
            if (this.defaultColormap != null)
                Util.Add(this.installedColormaps, this.defaultColormap);
        }

        /**
	 * Called when a window is deleted, usually due to a client disconnecting.
	 * Removes all references to the window.
	 *
	 * @param w	The window being deleted.
	 */
        internal void DeleteWindow(Window w)
        {
            if (this.grabPointerWindow == w || this.grabConfineWindow == w)
            {
                this.grabPointerClient = null;
                this.grabPointerWindow = null;
                this.grabCursor = null;
                this.grabConfineWindow = null;
                UpdatePointer(2);
            }
            else
            {
                UpdatePointer(0);
            }

            RevertFocus(w);
        }

        /**
	 * Called when the window is unmapped.
	 * If the window had keyboard focus, update the focus window.
	 *
	 * @param w
	 */
        internal void RevertFocus(Window w)
        {
            if (w == this.grabKeyboardWindow)
            {
                var pw = this.rootWindow.WindowAtPoint(this.motionX, this.motionY);

                Window.FocusInOutNotify(this.grabKeyboardWindow, this.focusWindow, pw, this.rootWindow, 2);
                this.grabKeyboardClient = null;
                this.grabKeyboardWindow = null;
            }

            if (w == this.focusWindow)
            {
                var pw = this.rootWindow.WindowAtPoint(this.motionX, this.motionY);

                if (this.focusRevertTo == 0)
                {
                    this.focusWindow = null;
                }
                else if (this.focusRevertTo == 1)
                {
                    this.focusWindow = this.rootWindow;
                }
                else
                {
                    this.focusWindow = w.GetParent();
                    while (!this.focusWindow.IsViewable())
                        this.focusWindow = this.focusWindow.GetParent();
                }

                this.focusRevertTo = 0;
                Window.FocusInOutNotify(w, this.focusWindow, pw, this.rootWindow,
                                        this.grabKeyboardWindow == null ? 0 : 3);
            }
        }

        /**
	 * Called when the view needs drawing.
	 *
	 * @param canvas	The canvas on which the view will be drawn.
	 */
        protected void OnDraw(IXCanvas canvas)
        {
            if (this.rootWindow == null)
            {
                return;
            }

            lock (this.xServer)
            {
                if (this.isBlanked)
                {
                    canvas.DrawColor(0xff000000);
                    return;
                }

                this.paint.Reset();
                this.rootWindow.Draw(canvas, this.paint);
                canvas.DrawBitmap(this.currentCursor.GetBitmap(),
                                  this.currentCursorX - this.currentCursor.GetHotspotX(),
                                  this.currentCursorY - this.currentCursor.GetHotspotY(), null);

                this.drawnCursor = this.currentCursor;
            }
        }

        /**
	 * Called when the size changes.
	 * Create the root window.
	 *
	 * @param width	The new width.
	 * @param height	The new height.
	 * @param oldWidth	The old width.
	 * @param oldHeight	The old height.
	 */
        protected void OnSizeChanged(int width, int height, int oldWidth, int oldHeight)
        {
            this.motionX = this.currentCursorX;
            this.motionY = this.currentCursorY;
            this.motionWindow = this.rootWindow;
            this.focusWindow = this.rootWindow;
        }

        /**
	 * Move the pointer on the screen.
	 *
	 * @param x	New X coordinate.
	 * @param y	New Y coordinate.
	 * @param cursor	The cursor to draw.
	 */
        private void MovePointer(int x, int y, Cursor cursor)
        {
            if (this.drawnCursor != null)
            {
                PostInvalidate();
                this.drawnCursor = null;
            }

            this.currentCursor = cursor;
            this.currentCursorX = x;
            this.currentCursorY = y;

            {
                PostInvalidate();
            }
        }

        /**
	 * Update the location of the pointer.
	 *
	 * @param x	New X coordinate.
	 * @param y	New Y coordinate.
	 * @param mode	0=Normal, 1=Grab, 2=Ungrab
	 */
        internal void UpdatePointerPosition(int x, int y, int mode)
        {
            Window w;
            Cursor c;

            if (this.grabConfineWindow != null)
            {
                var rect = this.grabConfineWindow.GetIRect();

                if (x < rect.Left)
                    x = rect.Left;
                else if (x >= rect.Right)
                    x = rect.Right - 1;

                if (y < rect.Top)
                    y = rect.Top;
                else if (y >= rect.Bottom)
                    y = rect.Bottom - 1;
            }

            if (this.grabPointerWindow != null)
                w = this.grabPointerWindow;
            else
                w = this.rootWindow.WindowAtPoint(x, y);

            if (this.grabCursor != null)
                c = this.grabCursor;
            else
                c = w.GetCursor();

            if (c != this.currentCursor || x != this.currentCursorX || y != this.currentCursorY)
                MovePointer(x, y, c);

            if (w != this.motionWindow)
            {
                this.motionWindow.LeaveEnterNotify(x, y, w, mode);
                this.motionWindow = w;
                this.motionX = x;
                this.motionY = y;
            }
            else if (x != this.motionX || y != this.motionY)
            {
                if (this.grabPointerWindow == null)
                {
                    w.MotionNotify(x, y, this.buttons & 0xff00, null);
                }
                else if (!this.grabPointerSynchronous)
                {
                    CallGrabMotionNotify(w, x, y, buttons, grabEventMask, grabPointerClient, grabPointerOwnerEvents);
                }
                else
                {
                    var e = new PendingGrabMotionNotifyEvent(w, x, y, buttons, grabEventMask, grabPointerClient,
                                                        grabPointerOwnerEvents);
                    this.pendingPointerEvents.Enqueue(e);
                }

                this.motionX = x;
                this.motionY = y;
            }
        }

        /**
	 * Update the pointer in case its glyph has changed.
	 *
	 * @param mode	0=Normal, 1=Grab, 2=Ungrab
	 */
        internal void UpdatePointer(int mode)
        {
            UpdatePointerPosition(this.currentCursorX, this.currentCursorY, mode);
        }

        /**
	 * Called when a pointer button is pressed/released.
	 *
	 * @param button	The button that was pressed/released.
	 * @param pressed	True if the button was pressed.
	 */
        internal void UpdatePointerButtons(int button, bool pressed)
        {
            var p = this.xServer.GetPointer();

            button = p.MapButton(button);
            if (button == 0)
                return;

            var mask = 0x80 << button;

            if (pressed)
            {
                if ((this.buttons & mask) != 0)
                    return;

                this.buttons |= mask;
            }
            else
            {
                if ((this.buttons & mask) == 0)
                    return;

                this.buttons &= ~mask;
            }

            if (this.grabPointerWindow == null)
            {
                var w = this.rootWindow.WindowAtPoint(this.motionX, this.motionY);
                PassiveButtonGrab pbg = null;

                if (pressed)
                    pbg = w.FindPassiveButtonGrab(this.buttons, null);

                if (pbg != null)
                {
                    this.grabPointerClient = pbg.GetGrabClient();
                    this.grabPointerWindow = pbg.GetGrabWindow();
                    this.grabPointerPassive = true;
                    this.grabPointerAutomatic = false;
                    this.grabPointerTime = this.xServer.GetTimestamp();
                    this.grabConfineWindow = pbg.GetConfineWindow();
                    this.grabEventMask = pbg.GetEventMask();
                    this.grabPointerOwnerEvents = pbg.GetOwnerEvents();
                    this.grabPointerSynchronous = pbg.GetPointerSynchronous();
                    this.grabKeyboardSynchronous = pbg.GetKeyboardSynchronous();

                    this.grabCursor = pbg.GetCursor();
                    if (this.grabCursor == null)
                        this.grabCursor = this.grabPointerWindow.GetCursor();

                    UpdatePointer(1);
                }
                else
                {
                    var ew = w.ButtonNotify(pressed, this.motionX, this.motionY, button, null);
                    ConsiderPointerFreezeNextEvent();

                    Client c = null;

                    if (pressed && ew != null)
                    {
                        List<Client> sc;

                        sc = ew.GetSelectingClients(EventCode.MaskButtonPress);
                        if (sc != null)
                            c = sc.First();
                    }

                    // Start an automatic key grab.
                    if (c != null)
                    {
                        var em = ew.GetClientEventMask(c);

                        this.grabPointerClient = c;
                        this.grabPointerWindow = ew;
                        this.grabPointerPassive = false;
                        this.grabPointerAutomatic = true;
                        this.grabPointerTime = this.xServer.GetTimestamp();
                        this.grabCursor = ew.GetCursor();
                        this.grabConfineWindow = null;
                        this.grabEventMask = em & EventCode.MaskAllPointer;
                        this.grabPointerOwnerEvents = (em & EventCode.MaskOwnerGrabButton) != 0;
                        this.grabPointerSynchronous = false;
                        this.grabKeyboardSynchronous = false;
                        UpdatePointer(1);
                    }
                }
            }
            else
            {
                if (!grabPointerSynchronous)
                {
                    CallGrabButtonNotify(grabPointerWindow, pressed, motionX, motionY, button, grabEventMask,
                                         grabPointerClient, grabPointerOwnerEvents);

                }
                else
                {
                    var e = new PendingGrabButtonNotifyEvent(grabPointerWindow, pressed, motionX, motionY, button,
                                                        grabEventMask, grabPointerClient, grabPointerOwnerEvents);

                    this.pendingPointerEvents.Enqueue(e);
                }
                
                if (this.grabPointerAutomatic && !pressed && (this.buttons & 0xff00) == 0)
                {
                    this.grabPointerClient = null;
                    this.grabPointerWindow = null;
                    this.grabCursor = null;
                    this.grabConfineWindow = null;
                    UpdatePointer(2);
                }
            }
        }

        /**
	 * Called when shift/alt keys are pressed/released.
	 *
	 * @param pressed	True if pressed, false if released.
	 * @param state	Current state of the modifier keys.
	 */
        private void UpdateModifiers(XKeyEvent keyEvent, bool pressed)
        {
            var modifierMask = this.xServer.GetKeyboard().GetModifierMask(keyEvent, pressed);
            this.buttons = (this.buttons & 0xff00) | modifierMask;
        }

        /**
	 * Called when a key is pressed or released.
	 *
	 * @param keycode	Keycode of the key.
	 * @param pressed	True if pressed, false if released.
	 */
        internal void NotifyKeyPressedReleased(int keycode, bool pressed)
        {
            if (this.grabKeyboardWindow == null && this.focusWindow == null)
                return;

            var kb = this.xServer.GetKeyboard();

            keycode = kb.TranslateToXKeycode(keycode);

            if (pressed && this.grabKeyboardWindow == null)
            {
                var pkg = this.focusWindow.FindPassiveKeyGrab(keycode, this.buttons & 0xff, null);

                if (pkg == null)
                {
                    var w = this.rootWindow.WindowAtPoint(this.motionX, this.motionY);

                    if (w.IsAncestor(this.focusWindow))
                        pkg = w.FindPassiveKeyGrab(keycode, this.buttons & 0xff, null);
                }

                if (pkg != null)
                {
                    this.grabKeyboardPassiveGrab = pkg;
                    this.grabKeyboardClient = pkg.GetGrabClient();
                    this.grabKeyboardWindow = pkg.GetGrabWindow();
                    this.grabKeyboardTime = this.xServer.GetTimestamp();
                    this.grabKeyboardOwnerEvents = pkg.GetOwnerEvents();
                    this.grabPointerSynchronous = pkg.GetPointerSynchronous();
                    this.grabKeyboardSynchronous = pkg.GetKeyboardSynchronous();
                }
            }

            if (this.grabKeyboardWindow == null)
            {
                var w = this.rootWindow.WindowAtPoint(this.motionX, this.motionY);

                if (w.IsAncestor(this.focusWindow))
                    w.KeyNotify(pressed, this.motionX, this.motionY, keycode, null);
                else
                    this.focusWindow.KeyNotify(pressed, this.motionX, this.motionY, keycode, null);

                ConsiderKeyboardFreezeNextEvent();
            }
            else if (!this.grabKeyboardSynchronous)
            {
                CallGrabKeyNotify(grabKeyboardWindow, pressed, motionX, motionY, keycode, grabKeyboardClient,
                                  grabKeyboardOwnerEvents);
            }
            else
            {
                var e = new PendingGrabKeyNotifyEvent(grabKeyboardWindow, pressed, motionX, motionY, keycode,
                                                 grabKeyboardClient, grabKeyboardOwnerEvents);
                this.pendingKeyboardEvents.Enqueue(e);
            }

            kb.UpdateKeymap(keycode, pressed);

            if (!pressed && this.grabKeyboardPassiveGrab != null)
            {
                int rk = this.grabKeyboardPassiveGrab.GetKey();

                if (rk == 0 || rk == keycode)
                {
                    this.grabKeyboardPassiveGrab = null;
                    this.grabKeyboardClient = null;
                    this.grabKeyboardWindow = null;
                }
            }
        }

        /**
	 * Called when there is a touch event.
	 *
	 * @param event	The touch event.
	 * @return	True if the event was handled.
	 */
        internal bool OnTouchEvent(int x, int y)
        {
            lock (this.xServer)
            {
                if (this.rootWindow == null)
                    return false;

                Blank(false); // Reset the screen saver.
                UpdatePointerPosition(x, y, 0);
            }

            return true;
        }

        /**
	 * Called when there is a key down event.
	 *
	 * @param keycode	The value in event.getKeyCode().
	 * @param event	The key event.
	 * @return	True if the event was handled.
	 */
        internal bool OnKeyDown(int keycode, XKeyEvent e)
        {
            lock (this.xServer)
            {
                Blank(false); // Reset the screen saver.

                if (e.IsLeftClick)
                {
                    UpdatePointerButtons(1, true);
                }
                else if (e.IsRightClick)
                {
                    UpdatePointerButtons(3, true);
                }
                else if (e.IsAltPressed || e.IsShiftPressed || e.IsControlPressed)
                {
                    UpdateModifiers(e, true);
                    NotifyKeyPressedReleased(keycode, true);
                }
                else
                {
                    NotifyKeyPressedReleased(keycode, true);
                }
            }

            return true;
        }

        /**
	 * Called when there is a key up event.
	 *
	 * @param keycode	The value in event.getKeyCode().
	 * @param event	The key event.
	 * @return	True if the event was handled.
	 */
        internal bool OnKeyUp(int keycode, XKeyEvent e)
        {
            lock (this.xServer)
            {
                Blank(false); // Reset the screen saver.
                if (e.IsLeftClick)
                {
                    UpdatePointerButtons(1, false);
                }
                else if (e.IsRightClick)
                {
                    UpdatePointerButtons(3, false);
                }
                else if (e.IsAltPressed || e.IsShiftPressed || e.IsControlPressed)
                {
                    UpdateModifiers(e, false);
                    NotifyKeyPressedReleased(keycode, false);
                }
                else
                {
                    NotifyKeyPressedReleased(keycode, false);
                }
            }

            return true;
        }

        /**
	 * Write details of the screen.
	 *
	 * @param io	The input/output stream.
	
	 */
        internal void Write(InputOutput io)
        {
            var vis = this.xServer.GetRootVisual();

            io.WriteInt(this.rootWindow.GetId()); // Root window ID.
            io.WriteInt(this.defaultColormap.GetId()); // Default colormap ID.
            io.WriteInt(this.defaultColormap.GetWhitePixel()); // White pixel.
            io.WriteInt(this.defaultColormap.GetBlackPixel()); // Black pixel.
            io.WriteInt(0); // Current input masks.
            io.WriteShort((short) GetWidth()); // Width in pixels.
            io.WriteShort((short) GetHeight()); // Height in pixels.
            io.WriteShort((short) (GetWidth() / this.pixelsPerMillimeter)); // Width in millimeters.
            io.WriteShort((short) (GetHeight() / this.pixelsPerMillimeter)); // Height in millimeters.
            io.WriteShort((short) 1); // Minimum installed maps.
            io.WriteShort((short) 1); // Maximum installed maps.
            io.WriteInt(vis.GetId()); // Root visual ID.
            io.WriteByte(vis.GetBackingStoreInfo());
            io.WriteByte((byte) (vis.GetSaveUnder() ? 1 : 0));
            io.WriteByte((byte) vis.GetDepth()); // Root depth.
            io.WriteByte((byte) 1); // Number of allowed depths.

            // Write the only allowed depth.
            io.WriteByte((byte) vis.GetDepth()); // Depth.
            io.WriteByte((byte) 0); // Unused.
            io.WriteShort((short) 1); // Number of visuals with this depth.
            io.WritePadBytes(4); // Unused.
            vis.Write(io); // The visual at this depth.
        }

        /**
	 * Write the screen's installed colormaps.
	 *
	 * @param client	The remote client.
	
	 */
        internal void WriteInstalledColormaps(Client client)
        {
            var io = client.GetInputOutput();
            var n = this.installedColormaps.Size();

            lock (io)
            {
                Util.WriteReplyHeader(client, (byte) 0);
                io.WriteInt(n); // Reply length.
                io.WriteShort((short) n); // Number of colormaps.
                io.WritePadBytes(22); // Unused.

                foreach (var cmap in this.installedColormaps)
                    io.WriteInt(cmap.GetId());
            }

            io.Flush();
        }

        /**
	 * Process a screen-related request.
	 *
	 * @param xServer	The X server.
	 * @param client	The remote client.
	 * @param opcode	The request's opcode.
	 * @param arg	Optional first argument.
	 * @param bytesRemaining	Bytes yet to be read in the request.
	
	 */
        internal void ProcessRequest(XServer xServer, Client client, byte opcode, byte arg, int bytesRemaining)
        {
            var io = client.GetInputOutput();

            switch (opcode)
            {
                case RequestCode.SendEvent:
                    if (bytesRemaining != 40)
                    {
                        io.ReadSkip(bytesRemaining);
                        ErrorCode.Write(client, ErrorCode.Length, opcode, 0);
                    }
                    else
                    {
                        ProcessSendEventRequest(this.xServer, client, arg == 1);
                    }

                    break;
                case RequestCode.GrabPointer:
                    if (bytesRemaining != 20)
                    {
                        io.ReadSkip(bytesRemaining);
                        ErrorCode.Write(client, ErrorCode.Length, opcode, 0);
                    }
                    else
                    {
                        ProcessGrabPointerRequest(this.xServer, client, arg == 1);
                    }

                    break;
                case RequestCode.UngrabPointer:
                    if (bytesRemaining != 4)
                    {
                        io.ReadSkip(bytesRemaining);
                        ErrorCode.Write(client, ErrorCode.Length, opcode, 0);
                    }
                    else
                    {
                        var time = io.ReadInt(); // Time.
                        var now = this.xServer.GetTimestamp();

                        if (time == 0)
                            time = now;

                        if (time >= this.grabPointerTime && time <= now && this.grabPointerClient == client)
                        {
                            this.grabPointerClient = null;
                            this.grabPointerWindow = null;
                            this.grabCursor = null;
                            this.grabConfineWindow = null;
                            UpdatePointer(2);
                        }
                    }

                    break;
                case RequestCode.GrabButton:
                    if (bytesRemaining != 20)
                    {
                        io.ReadSkip(bytesRemaining);
                        ErrorCode.Write(client, ErrorCode.Length, opcode, 0);
                    }
                    else
                    {
                        ProcessGrabButtonRequest(this.xServer, client, arg == 1);
                    }

                    break;
                case RequestCode.UngrabButton:
                    if (bytesRemaining != 8)
                    {
                        io.ReadSkip(bytesRemaining);
                        ErrorCode.Write(client, ErrorCode.Length, opcode, 0);
                    }
                    else
                    {
                        var wid = io.ReadInt(); // Grab window.
                        var modifiers = io.ReadShort(); // Modifiers.
                        var r = this.xServer.GetResource(wid);

                        io.ReadSkip(2); // Unused.

                        if (r == null || r.GetRessourceType() != Resource.AttrWindow)
                        {
                            ErrorCode.Write(client, ErrorCode.Window, opcode, wid);
                        }
                        else
                        {
                            var w = (Window) r;

                            w.RemovePassiveButtonGrab(arg, modifiers);
                        }
                    }

                    break;
                case RequestCode.ChangeActivePointerGrab:
                    if (bytesRemaining != 12)
                    {
                        io.ReadSkip(bytesRemaining);
                        ErrorCode.Write(client, ErrorCode.Length, opcode, 0);
                    }
                    else
                    {
                        var cid = io.ReadInt(); // Cursor.
                        var time = io.ReadInt(); // Time.
                        var mask = io.ReadShort(); // Event mask.
                        Cursor c = null;

                        io.ReadSkip(2); // Unused.

                        if (cid != 0)
                        {
                            var r = this.xServer.GetResource(cid);

                            if (r == null || r.GetRessourceType() != Resource.AttrCursor)
                                ErrorCode.Write(client, ErrorCode.Cursor, opcode, 0);
                            else
                                c = (Cursor) r;
                        }

                        var now = this.xServer.GetTimestamp();

                        if (time == 0)
                            time = now;

                        if (this.grabPointerWindow != null && !this.grabPointerPassive &&
                            this.grabPointerClient == client && time >= this.grabPointerTime && time <= now &&
                            (cid == 0 || c != null))
                        {
                            this.grabEventMask = mask;
                            if (c != null)
                                this.grabCursor = c;
                            else
                                this.grabCursor = this.grabPointerWindow.GetCursor();
                        }
                    }

                    break;
                case RequestCode.GrabKeyboard:
                    if (bytesRemaining != 12)
                    {
                        io.ReadSkip(bytesRemaining);
                        ErrorCode.Write(client, ErrorCode.Length, opcode, 0);
                    }
                    else
                    {
                        ProcessGrabKeyboardRequest(this.xServer, client, arg == 1);
                    }

                    break;
                case RequestCode.UngrabKeyboard:
                    if (bytesRemaining != 4)
                    {
                        io.ReadSkip(bytesRemaining);
                        ErrorCode.Write(client, ErrorCode.Length, opcode, 0);
                    }
                    else
                    {
                        var time = io.ReadInt(); // Time.
                        var now = this.xServer.GetTimestamp();

                        if (time == 0)
                            time = now;

                        if (time >= this.grabKeyboardTime && time <= now)
                        {
                            var pw = this.rootWindow.WindowAtPoint(this.motionX, this.motionY);

                            Window.FocusInOutNotify(this.grabKeyboardWindow, this.focusWindow, pw, this.rootWindow, 2);
                            this.grabKeyboardClient = null;
                            this.grabKeyboardWindow = null;
                        }
                    }

                    break;
                case RequestCode.GrabKey:
                    if (bytesRemaining != 12)
                    {
                        io.ReadSkip(bytesRemaining);
                        ErrorCode.Write(client, ErrorCode.Length, opcode, 0);
                    }
                    else
                    {
                        ProcessGrabKeyRequest(this.xServer, client, arg == 1);
                    }

                    break;
                case RequestCode.UngrabKey:
                    if (bytesRemaining != 8)
                    {
                        io.ReadSkip(bytesRemaining);
                        ErrorCode.Write(client, ErrorCode.Length, opcode, 0);
                    }
                    else
                    {
                        var wid = io.ReadInt(); // Grab window.
                        var modifiers = io.ReadShort(); // Modifiers.
                        var r = this.xServer.GetResource(wid);

                        io.ReadSkip(2); // Unused.

                        if (r == null || r.GetRessourceType() != Resource.AttrWindow)
                        {
                            ErrorCode.Write(client, ErrorCode.Window, opcode, wid);
                        }
                        else
                        {
                            var w = (Window) r;

                            w.RemovePassiveKeyGrab(arg, modifiers);
                        }
                    }

                    break;
                case RequestCode.AllowEvents:
                    ProcessAllowEvents(client, opcode, io, bytesRemaining, arg);
                    break;
                case RequestCode.SetInputFocus:
                    if (bytesRemaining != 8)
                    {
                        io.ReadSkip(bytesRemaining);
                        ErrorCode.Write(client, ErrorCode.Length, opcode, 0);
                    }
                    else
                    {
                        ProcessSetInputFocusRequest(this.xServer, client, arg);
                    }

                    break;
                case RequestCode.GetInputFocus:
                    if (bytesRemaining != 0)
                    {
                        io.ReadSkip(bytesRemaining);
                        ErrorCode.Write(client, ErrorCode.Length, opcode, 0);
                    }
                    else
                    {
                        int wid;

                        if (this.focusWindow == null)
                            wid = 0;
                        else if (this.focusWindow == this.rootWindow)
                            wid = 1;
                        else
                            wid = this.focusWindow.GetId();

                        lock (io)
                        {
                            Util.WriteReplyHeader(client, this.focusRevertTo);
                            io.WriteInt(0); // Reply length.
                            io.WriteInt(wid); // Focus window.
                            io.WritePadBytes(20); // Unused.
                        }

                        io.Flush();
                    }

                    break;
            }
        }

        /**
	 * Process a SendEvent request.
	 *
	 * @param xServer	The X server.
	 * @param client	The remote client.
	 * @param propagate	Propagate flag.
	
	 */
        private void ProcessSendEventRequest(XServer xServer, Client client, bool propagate)
        {
            var io = client.GetInputOutput();
            var wid = io.ReadInt(); // Destination window.
            var mask = io.ReadInt(); // Event mask.
            var eventBytes = new byte[32];
            Window w;

            io.ReadBytes(eventBytes, 0, 32); // Event.

            if (wid == 0)
            {
                // Pointer window.
                w = this.rootWindow.WindowAtPoint(this.motionX, this.motionY);
            }
            else if (wid == 1)
            {
                // Input focus.
                if (this.focusWindow == null)
                {
                    ErrorCode.Write(client, ErrorCode.Window, RequestCode.SendEvent, wid);
                    return;
                }

                var pw = this.rootWindow.WindowAtPoint(this.motionX, this.motionY);

                if (pw.IsAncestor(this.focusWindow))
                    w = pw;
                else
                    w = this.focusWindow;
            }
            else
            {
                var r = this.xServer.GetResource(wid);

                if (r == null || r.GetRessourceType() != Resource.AttrWindow)
                {
                    ErrorCode.Write(client, ErrorCode.Window, RequestCode.SendEvent, wid);
                    return;
                }
                else
                    w = (Window) r;
            }

            List<Client> dc = null;

            if (mask == 0)
            {
                dc = new List<Client>();
                Util.Add(dc, w.GetClient());
            }
            else if (!propagate)
            {
                dc = w.GetSelectingClients(mask);
            }
            else
            {
                for (;;)
                {
                    if ((dc = w.GetSelectingClients(mask)) != null)
                        break;

                    mask &= ~w.GetDoNotPropagateMask();
                    if (mask == 0)
                        break;

                    w = w.GetParent();
                    if (w == null)
                        break;
                    if (wid == 1 && w == this.focusWindow)
                        break;
                }
            }

            if (dc == null)
                return;

            foreach (var c in dc)
            {
                var dio = c.GetInputOutput();

                lock (dio)
                {
                    dio.WriteByte((byte) (eventBytes[0] | 128));

                    if (eventBytes[0] == EventCode.KeymapNotify)
                    {
                        dio.WriteBytes(eventBytes, 1, 31);
                    }
                    else
                    {
                        dio.WriteByte(eventBytes[1]);
                        dio.WriteShort((short) (c.GetSequenceNumber() & 0xffff));
                        dio.WriteBytes(eventBytes, 4, 28);
                    }
                }

                dio.Flush();
            }
        }

        /**
	 * Process a GrabPointer request.
	 *
	 * @param xServer	The X server.
	 * @param client	The remote client.
	 * @param ownerEvents	Owner-events flag.
	
	 */
        private void ProcessGrabPointerRequest(XServer xServer, Client client, bool ownerEvents)
        {
            var io = client.GetInputOutput();
            var wid = io.ReadInt(); // Grab window.
            var mask = io.ReadShort(); // Event mask.
            var psync = (io.ReadByte() == 0); // Pointer mode.
            var ksync = (io.ReadByte() == 0); // Keyboard mode.
            var cwid = io.ReadInt(); // Confine-to.
            var cid = io.ReadInt(); // Cursor.
            var time = io.ReadInt(); // Time.
            var r = this.xServer.GetResource(wid);

            if (r == null || r.GetRessourceType() != Resource.AttrWindow)
            {
                ErrorCode.Write(client, ErrorCode.Window, RequestCode.GrabPointer, wid);
                return;
            }

            var w = (Window) r;
            Cursor c = null;
            Window cw = null;

            if (cwid != 0)
            {
                r = this.xServer.GetResource(cwid);

                if (r == null || r.GetRessourceType() != Resource.AttrWindow)
                {
                    ErrorCode.Write(client, ErrorCode.Window, RequestCode.GrabPointer, cwid);
                    return;
                }

                cw = (Window) r;
            }

            if (cid != 0)
            {
                r = this.xServer.GetResource(cid);
                if (r != null && r.GetRessourceType() != Resource.AttrCursor)
                {
                    ErrorCode.Write(client, ErrorCode.Cursor, RequestCode.GrabPointer, cid);
                    return;
                }

                c = (Cursor) r;
            }

            if (c == null)
                c = w.GetCursor();

            byte status = 0; // Success.
            var now = this.xServer.GetTimestamp();

            if (time == 0)
                time = now;

            if (time < this.grabPointerTime || time > now)
            {
                status = 2; // Invalid time.
            }
            else if (this.grabPointerWindow != null && this.grabPointerClient != client)
            {
                status = 1; // Already grabbed.
            }
            else
            {
                this.grabPointerClient = client;
                this.grabPointerWindow = w;
                this.grabPointerPassive = false;
                this.grabPointerAutomatic = false;
                this.grabPointerTime = time;
                this.grabCursor = c;
                this.grabConfineWindow = cw;
                this.grabEventMask = mask;
                this.grabPointerOwnerEvents = ownerEvents;
                this.grabPointerSynchronous = psync;
                this.grabKeyboardSynchronous = ksync;
            }

            lock (io)
            {
                Util.WriteReplyHeader(client, status);
                io.WriteInt(0); // Reply length.
                io.WritePadBytes(24); // Unused.
            }

            io.Flush();

            if (status == 0)
                UpdatePointer(1);
        }

        /**
 * Process a GrabButton request.
 *
 * @param xServer	The X server.
 * @param client	The remote client.
 * @param ownerEvents	Owner-events flag.

 */
        private void ProcessGrabButtonRequest(XServer xServer, Client client, bool ownerEvents)
        {
            var io = client.GetInputOutput();
            var wid = io.ReadInt(); // Grab window.
            var mask = io.ReadShort(); // Event mask.
            var psync = (io.ReadByte() == 0); // Pointer mode.
            var ksync = (io.ReadByte() == 0); // Keyboard mode.
            var cwid = io.ReadInt(); // Confine-to.
            var cid = io.ReadInt(); // Cursor.
            var button = (byte) io.ReadByte(); // Button.
            int modifiers;
            var r = this.xServer.GetResource(wid);

            io.ReadSkip(1); // Unused.
            modifiers = io.ReadShort(); // Modifiers.

            if (r == null || r.GetRessourceType() != Resource.AttrWindow)
            {
                ErrorCode.Write(client, ErrorCode.Window, RequestCode.GrabPointer, wid);
                return;
            }

            var w = (Window) r;
            Cursor c = null;
            Window cw = null;

            if (cwid != 0)
            {
                r = this.xServer.GetResource(cwid);

                if (r == null || r.GetRessourceType() != Resource.AttrWindow)
                {
                    ErrorCode.Write(client, ErrorCode.Window, RequestCode.GrabPointer, cwid);
                    return;
                }

                cw = (Window) r;
            }

            if (cid != 0)
            {
                r = this.xServer.GetResource(cid);

                if (r != null && r.GetRessourceType() != Resource.AttrCursor)
                {
                    ErrorCode.Write(client, ErrorCode.Cursor, RequestCode.GrabPointer, cid);
                    return;
                }

                c = (Cursor) r;
            }

            w.AddPassiveButtonGrab(new PassiveButtonGrab(client, w, button, modifiers, ownerEvents, mask, psync, ksync,
                                                         cw, c));
        }

        /**
	 * Process a GrabKeyboard request.
	 *
	 * @param xServer	The X server.
	 * @param client	The remote client.
	 * @param ownerEvents	Owner-events flag.
	
	 */
        private void ProcessGrabKeyboardRequest(XServer xServer, Client client, bool ownerEvents)
        {
            var io = client.GetInputOutput();
            var wid = io.ReadInt(); // Grab window.
            var time = io.ReadInt(); // Time.
            var psync = (io.ReadByte() == 0); // Pointer mode.
            var ksync = (io.ReadByte() == 0); // Keyboard mode.
            var r = this.xServer.GetResource(wid);

            io.ReadSkip(2); // Unused.

            if (r == null || r.GetRessourceType() != Resource.AttrWindow)
            {
                ErrorCode.Write(client, ErrorCode.Window, RequestCode.GrabKeyboard, wid);
                return;
            }

            var w = (Window) r;
            byte status = 0; // Success.
            var now = this.xServer.GetTimestamp();

            if (time == 0)
                time = now;

            if (time < this.grabKeyboardTime || time > now)
            {
                status = 2; // Invalid time.
            }
            else if (this.grabKeyboardWindow != null && (this.grabKeyboardClient != client))
            {
                status = 1; // Already grabbed.
            }
            else
            {
                this.grabKeyboardClient = client;
                this.grabKeyboardWindow = w;
                this.grabKeyboardTime = time;
                this.grabKeyboardOwnerEvents = ownerEvents;
                this.grabPointerSynchronous = psync;
                this.grabKeyboardSynchronous = ksync;
            }

            lock (io)
            {
                Util.WriteReplyHeader(client, status);
                io.WriteInt(0); // Reply length.
                io.WritePadBytes(24); // Unused.
            }

            io.Flush();

            if (status == 0)
                Window.FocusInOutNotify(this.focusWindow, w, this.rootWindow.WindowAtPoint(this.motionX, this.motionY),
                                        this.rootWindow, 1);
        }

        /**
 * Process a GrabKey request.
 *
 * @param xServer	The X server.
 * @param client	The remote client.
 * @param ownerEvents	Owner-events flag.

 */
        private void ProcessGrabKeyRequest(XServer xServer, Client client, bool ownerEvents)
        {
            var io = client.GetInputOutput();
            var wid = io.ReadInt(); // Grab window.
            var modifiers = io.ReadShort(); // Modifiers.
            var keycode = (byte) io.ReadByte(); // Key.
            var psync = (io.ReadByte() == 0); // Pointer mode.
            var ksync = (io.ReadByte() == 0); // Keyboard mode.
            var r = this.xServer.GetResource(wid);

            io.ReadSkip(3); // Unused.

            if (r == null || r.GetRessourceType() != Resource.AttrWindow)
            {
                ErrorCode.Write(client, ErrorCode.Window, RequestCode.GrabPointer, wid);
                return;
            }

            var w = (Window) r;

            w.AddPassiveKeyGrab(new PassiveKeyGrab(client, w, keycode, modifiers, ownerEvents, psync, ksync));
        }

        /**
	 * Process a SetInputFocus request.
	 *
	 * @param xServer	The X server.
	 * @param client	The remote client.
	 * @param revertTo	0=None, 1=Root, 2=Parent.

	
	 */
        private void ProcessSetInputFocusRequest(XServer xServer, Client client, byte revertTo)
        {
            var io = client.GetInputOutput();
            var wid = io.ReadInt(); // Focus window.
            var time = io.ReadInt(); // Time.
            Window w;

            if (wid == 0)
            {
                w = null;
                revertTo = 0;
            }
            else if (wid == 1)
            {
                w = this.rootWindow;
                revertTo = 0;
            }
            else
            {
                var r = xServer.GetResource(wid);

                if (r == null || r.GetRessourceType() != Resource.AttrWindow)
                {
                    ErrorCode.Write(client, ErrorCode.Window, RequestCode.GrabPointer, wid);
                    return;
                }

                w = (Window) r;
            }

            var now = xServer.GetTimestamp();

            if (time == 0)
                time = now;

            if (time < this.focusLastChangeTime || time > now)
                return;

            Window.FocusInOutNotify(this.focusWindow, w, this.rootWindow.WindowAtPoint(this.motionX, this.motionY),
                                    this.rootWindow, this.grabKeyboardWindow == null ? 0 : 3);

            this.focusWindow = w;
            this.focusRevertTo = revertTo;
            this.focusLastChangeTime = time;
        }

        public void OnSizeChanged(IXScreen source, Size newSize, Size oldSize)
        {
            OnSizeChanged(newSize.Width, newSize.Height, oldSize.Width, oldSize.Height);
        }

        public void OnTouch(IXScreen source, int x, int y)
        {
            OnTouchEvent(x, y);
        }

        public void OnDraw(IXScreen source, IXCanvas canvas)
        {
            OnDraw(canvas);
        }

        public void OnKeyDown(IXScreen source, XKeyEvent e)
        {
            OnKeyDown(e.KeyCode, e);
        }

        public void OnKeyUp(IXScreen source, XKeyEvent e)
        {
            OnKeyUp(e.KeyCode, e);
        }

        private void ProcessAllowEvents(Client client, byte opcode, 
                                        [NotNull] InputOutput io, int bytesRemaining, byte mode)
        {
            if (bytesRemaining != 4)
            {
                io.ReadSkip(bytesRemaining);
                ErrorCode.Write(client, ErrorCode.Length, opcode, 0);
                return;
            }
            
            var t = io.ReadInt();
            var now = this.xServer.GetTimestamp();
            var time = t == 0 ? now : t;
            if ((now < time) || (time < this.grabPointerTime) || (time < this.grabKeyboardTime))
            {
                return;
            }

            switch ((AllowEventsMode)mode)
            {
                case AllowEventsMode.AsyncPointer:
                    FlushPendingPointerEvents();
                    this.grabPointerSynchronous = false;
                    this._grabPointerFreezeNextEvent = false;
                    break;
                case AllowEventsMode.SyncPointer:
                    FlushPendingPointerEvents();
                    this.grabPointerSynchronous = false;
                    this._grabPointerFreezeNextEvent = true;
                    break;
                case AllowEventsMode.AsyncKeyboard:
                    FlushPendingKeyboardEvents();
                    this.grabKeyboardSynchronous = false;
                    this._grabKeyboardFreezeNextEvent = false;
                    break;
                case AllowEventsMode.SyncKeyboard:
                    FlushPendingKeyboardEvents();
                    this.grabKeyboardSynchronous = false;
                    this._grabKeyboardFreezeNextEvent = true;
                    break;
                case AllowEventsMode.AsyncBoth:
                case AllowEventsMode.SyncBoth:
                case AllowEventsMode.ReplayPointer:
                case AllowEventsMode.ReplayKeyboard:
                    Debug.WriteLine($"Unsupported AllowEvents mode: {mode}");
                    ErrorCode.Write(client, ErrorCode.Implementation, opcode, 0);
                    break;
                default:
                    ErrorCode.Write(client, ErrorCode.Value, opcode, 0);
                    break;
            }
        }

        private void ConsiderPointerFreezeNextEvent()
        {
            this.grabPointerSynchronous = this._grabPointerFreezeNextEvent;
        }

        private void ConsiderKeyboardFreezeNextEvent()
        {
            this.grabKeyboardSynchronous = this._grabKeyboardFreezeNextEvent;
        }

        public void CallGrabButtonNotify([NotNull] Window w, bool pressed, int motionX, int motionY, int button,
            int grabEventMask, Client grabPointerClient, bool grabPointerOwnerEvents)
        {
            w.GrabButtonNotify(pressed, motionX, motionY, button, grabEventMask, grabPointerClient,
                               grabPointerOwnerEvents);
            ConsiderPointerFreezeNextEvent();
        }

        internal void CallGrabMotionNotify([NotNull] Window w, int x, int y, int buttons, int grabEventMask,
            Client grabPointerClient, bool grabPointerOwnerEvents)
        {
            w.GrabMotionNotify(x, y, buttons & 0xff00, grabEventMask, grabPointerClient, grabPointerOwnerEvents);
        }

        internal void CallGrabKeyNotify([NotNull] Window w, bool pressed, int motionX, int motionY, int keycode,
            Client grabKeyboardClient, bool grabKeyboardOwnerEvents)
        {
            w.GrabKeyNotify(pressed, motionX, motionY, keycode, grabKeyboardClient, grabKeyboardOwnerEvents);
            ConsiderKeyboardFreezeNextEvent();
        }

        private void FlushPendingPointerEvents()
        {
            PendingEvent.FlushEvents(this.pendingPointerEvents, this);
        }

        private void FlushPendingKeyboardEvents()
        {
            PendingEvent.FlushEvents(this.pendingKeyboardEvents, this);
        }
    }
}