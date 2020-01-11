using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using TX11Shared;
using TX11Shared.Graphics;
using TX11Shared.Keyboard;

namespace TX11Business.UIDependent
{
    internal class ScreenView : IXScreenObserver
    {
        private readonly XServer xServer;
        private readonly int rootId;
        private readonly Window rootWindow;
        private Colormap defaultColormap;
        private readonly List<Colormap> installedColormaps;
        private readonly float pixelsPerMillimeter;

        private Cursor currentCursor;
        private int currentCursorX;
        private int currentCursorY;
        private Cursor drawnCursor;
        private int drawnCursorX;
        private int drawnCursorY;
        private Window motionWindow;
        private int motionX;
        private int motionY;
        private int buttons;
        private bool isBlanked;
        private readonly IXPaint paint;

        private Client grabPointerClient;
        private Window grabPointerWindow;
        private int grabPointerTime;
        private bool grabPointerOwnerEvents;
        private bool grabPointerSynchronous;
        private bool grabPointerPassive;
        private bool grabPointerAutomatic;
        private Client grabKeyboardClient;
        private Window grabKeyboardWindow;
        private int grabKeyboardTime;
        private bool grabKeyboardOwnerEvents;
        private bool grabKeyboardSynchronous;
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
            installedColormaps = new List<Colormap>();
            this.pixelsPerMillimeter = pixelsPerMillimeter;
            this.pixelsPerMillimeter = 3.779527f;
            paint = Util.GetPaint();

            rootWindow = new Window(this.rootId, this.xServer, null, this, null, 0, 0, GetWidth(), GetHeight(), 0,
                                    false, true);
            this.xServer.AddResource(rootWindow);

            currentCursor = rootWindow.GetCursor();
            currentCursorX = GetWidth() / 2;
            currentCursorY = GetHeight() / 2;
            motionWindow = rootWindow;
            focusWindow = rootWindow;
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
            return rootWindow;
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
            return defaultColormap;
        }

        /**
	 * Return the current cursor.
	 *
	 * @return	The current cursor.
	 */
        internal Cursor GetCurrentCursor()
        {
            return currentCursor;
        }

        /**
	 * Return the current pointer X coordinate.
	 *
	 * @return	The current pointer X coordinate.
	 */
        internal int GetPointerX()
        {
            return currentCursorX;
        }

        /**
	 * Return the current pointer Y coordinate.
	 *
	 * @return	The current pointer Y coordinate.
	 */
        internal int GetPointerY()
        {
            return currentCursorY;
        }

        /**
	 * Return a mask indicating the current state of the pointer and
	 * modifier buttons.
	 *
	 * @return	A mask indicating the current state of the buttons.
	 */
        internal int GetButtons()
        {
            return buttons;
        }

        /**
	 * Return the window that has input focus. Can be null.
	 *
	 * @return	The window that has input focus.
	 */
        internal Window GetFocusWindow()
        {
            return focusWindow;
        }

        /**
	 * Blank/unblank the screen.
	 *
	 * @param flag	If true, blank the screen. Otherwise unblank it.
	 */
        internal void Blank(bool flag)
        {
            if (isBlanked == flag)
                return;

            isBlanked = flag;
            PostInvalidate();

            if (!isBlanked)
                xServer.ResetScreenSaver();
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
            Util.Add(installedColormaps, cmap);
            if (defaultColormap == null)
            {
                defaultColormap = cmap;
                rootWindow?.SetColormap(cmap); //fix for  fvwm
            }
        }

        /**
	 * Remove an installed colormap.
	 *
	 * @param cmap	The colormap to remove.
	 */
        internal void RemoveInstalledColormap(Colormap cmap)
        {
            Util.Remove(installedColormaps, cmap);
            if (defaultColormap == cmap)
            {
                if (installedColormaps.Size() == 0)
                    defaultColormap = null;
                else
                    defaultColormap = installedColormaps.FirstOrDefault();
            }
        }

        /**
	 * Remove all colormaps except the default one.
	 */
        internal void RemoveNonDefaultColormaps()
        {
            if (installedColormaps.Size() < 2)
                return;

            Util.Clear(installedColormaps);
            if (defaultColormap != null)
                Util.Add(installedColormaps, defaultColormap);
        }

        /**
	 * Called when a window is deleted, usually due to a client disconnecting.
	 * Removes all references to the window.
	 *
	 * @param w	The window being deleted.
	 */
        internal void DeleteWindow(Window w)
        {
            if (grabPointerWindow == w || grabConfineWindow == w)
            {
                grabPointerClient = null;
                grabPointerWindow = null;
                grabCursor = null;
                grabConfineWindow = null;
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
            if (w == grabKeyboardWindow)
            {
                var pw = rootWindow.WindowAtPoint(motionX, motionY);

                Window.FocusInOutNotify(grabKeyboardWindow, focusWindow, pw, rootWindow, 2);
                grabKeyboardClient = null;
                grabKeyboardWindow = null;
            }

            if (w == focusWindow)
            {
                var pw = rootWindow.WindowAtPoint(motionX, motionY);

                if (focusRevertTo == 0)
                {
                    focusWindow = null;
                }
                else if (focusRevertTo == 1)
                {
                    focusWindow = rootWindow;
                }
                else
                {
                    focusWindow = w.GetParent();
                    while (!focusWindow.IsViewable())
                        focusWindow = focusWindow.GetParent();
                }

                focusRevertTo = 0;
                Window.FocusInOutNotify(w, focusWindow, pw, rootWindow, grabKeyboardWindow == null ? 0 : 3);
            }
        }

        /**
	 * Called when the view needs drawing.
	 *
	 * @param canvas	The canvas on which the view will be drawn.
	 */
        protected void OnDraw(IXCanvas canvas)
        {
            if (rootWindow == null)
            {
                return;
            }

            lock (xServer)
            {
                if (isBlanked)
                {
                    canvas.DrawColor(0xff000000);
                    return;
                }

                paint.Reset();
                rootWindow.Draw(canvas, paint);
                canvas.DrawBitmap(currentCursor.GetBitmap(), currentCursorX - currentCursor.GetHotspotX(),
                                  currentCursorY - currentCursor.GetHotspotY(), null);

                drawnCursor = currentCursor;
                drawnCursorX = currentCursorX;
                drawnCursorY = currentCursorY;
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
            drawnCursorX = currentCursorX;
            drawnCursorY = currentCursorY;
            motionX = currentCursorX;
            motionY = currentCursorY;
            motionWindow = rootWindow;
            focusWindow = rootWindow;
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
            if (drawnCursor != null)
            {
                var left = drawnCursorX - drawnCursor.GetHotspotX();
                var top = drawnCursorY - drawnCursor.GetHotspotY();
                var bm = drawnCursor.GetBitmap();

                PostInvalidate(left, top, left + bm.Width, top + bm.Height);
                drawnCursor = null;
            }

            currentCursor = cursor;
            currentCursorX = x;
            currentCursorY = y;

            {
                var left = x - cursor.GetHotspotX();
                var top = y - cursor.GetHotspotY();
                var bm = cursor.GetBitmap();

                PostInvalidate(left, top, left + bm.Width, top + bm.Height);
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

            if (grabConfineWindow != null)
            {
                var rect = grabConfineWindow.GetIRect();

                if (x < rect.Left)
                    x = rect.Left;
                else if (x >= rect.Right)
                    x = rect.Right - 1;

                if (y < rect.Top)
                    y = rect.Top;
                else if (y >= rect.Bottom)
                    y = rect.Bottom - 1;
            }

            if (grabPointerWindow != null)
                w = grabPointerWindow;
            else
                w = rootWindow.WindowAtPoint(x, y);

            if (grabCursor != null)
                c = grabCursor;
            else
                c = w.GetCursor();

            if (c != currentCursor || x != currentCursorX || y != currentCursorY)
                MovePointer(x, y, c);

            if (w != motionWindow)
            {
                motionWindow.LeaveEnterNotify(x, y, w, mode);
                motionWindow = w;
                motionX = x;
                motionY = y;
            }
            else if (x != motionX || y != motionY)
            {
                if (grabPointerWindow == null)
                {
                    w.MotionNotify(x, y, buttons & 0xff00, null);
                }
                else if (!grabPointerSynchronous)
                {
                    w.GrabMotionNotify(x, y, buttons & 0xff00, grabEventMask, grabPointerClient,
                                       grabPointerOwnerEvents);
                } // Else need to queue the events for later.

                motionX = x;
                motionY = y;
            }
        }

        /**
	 * Update the pointer in case its glyph has changed.
	 *
	 * @param mode	0=Normal, 1=Grab, 2=Ungrab
	 */
        internal void UpdatePointer(int mode)
        {
            UpdatePointerPosition(currentCursorX, currentCursorY, mode);
        }

        /**
	 * Called when a pointer button is pressed/released.
	 *
	 * @param button	The button that was pressed/released.
	 * @param pressed	True if the button was pressed.
	 */
        internal void UpdatePointerButtons(int button, bool pressed)
        {
            var p = xServer.GetPointer();

            button = p.MapButton(button);
            if (button == 0)
                return;

            var mask = 0x80 << button;

            if (pressed)
            {
                if ((buttons & mask) != 0)
                    return;

                buttons |= mask;
            }
            else
            {
                if ((buttons & mask) == 0)
                    return;

                buttons &= ~mask;
            }

            if (grabPointerWindow == null)
            {
                var w = rootWindow.WindowAtPoint(motionX, motionY);
                PassiveButtonGrab pbg = null;

                if (pressed)
                    pbg = w.FindPassiveButtonGrab(buttons, null);

                if (pbg != null)
                {
                    grabPointerClient = pbg.GetGrabClient();
                    grabPointerWindow = pbg.GetGrabWindow();
                    grabPointerPassive = true;
                    grabPointerAutomatic = false;
                    grabPointerTime = xServer.GetTimestamp();
                    grabConfineWindow = pbg.GetConfineWindow();
                    grabEventMask = pbg.GetEventMask();
                    grabPointerOwnerEvents = pbg.GetOwnerEvents();
                    grabPointerSynchronous = pbg.GetPointerSynchronous();
                    grabKeyboardSynchronous = pbg.GetKeyboardSynchronous();

                    grabCursor = pbg.GetCursor();
                    if (grabCursor == null)
                        grabCursor = grabPointerWindow.GetCursor();

                    UpdatePointer(1);
                }
                else
                {
                    var ew = w.ButtonNotify(pressed, motionX, motionY, button, null);
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

                        grabPointerClient = c;
                        grabPointerWindow = ew;
                        grabPointerPassive = false;
                        grabPointerAutomatic = true;
                        grabPointerTime = xServer.GetTimestamp();
                        grabCursor = ew.GetCursor();
                        grabConfineWindow = null;
                        grabEventMask = em & EventCode.MaskAllPointer;
                        grabPointerOwnerEvents = (em & EventCode.MaskOwnerGrabButton) != 0;
                        grabPointerSynchronous = false;
                        grabKeyboardSynchronous = false;
                        UpdatePointer(1);
                    }
                }
            }
            else
            {
                if (!grabPointerSynchronous)
                {
                    grabPointerWindow.GrabButtonNotify(pressed, motionX, motionY, button, grabEventMask,
                                                       grabPointerClient, grabPointerOwnerEvents);
                } // Else need to queue the events for later.

                if (grabPointerAutomatic && !pressed && (buttons & 0xff00) == 0)
                {
                    grabPointerClient = null;
                    grabPointerWindow = null;
                    grabCursor = null;
                    grabConfineWindow = null;
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
        private void UpdateModifiers(bool pressed, int state)
        {
            var mask = 0;

            //if ((state & XKeyEvent.META_SHIFT_ON) != 0)
            //    mask |= 1; // Shift.
            //if ((state & XKeyEvent.META_ALT_ON) != 0)
            //    mask |= 8; // Mod1.

            buttons = (buttons & 0xff00) | mask;
        }

        /**
	 * Called when a key is pressed or released.
	 *
	 * @param keycode	Keycode of the key.
	 * @param pressed	True if pressed, false if released.
	 */
        internal void NotifyKeyPressedReleased(int keycode, bool pressed)
        {
            if (grabKeyboardWindow == null && focusWindow == null)
                return;

            var kb = xServer.GetKeyboard();

            keycode = kb.TranslateToXKeycode(keycode);

            if (pressed && grabKeyboardWindow == null)
            {
                var pkg = focusWindow.FindPassiveKeyGrab(keycode, buttons & 0xff, null);

                if (pkg == null)
                {
                    var w = rootWindow.WindowAtPoint(motionX, motionY);

                    if (w.IsAncestor(focusWindow))
                        pkg = w.FindPassiveKeyGrab(keycode, buttons & 0xff, null);
                }

                if (pkg != null)
                {
                    grabKeyboardPassiveGrab = pkg;
                    grabKeyboardClient = pkg.GetGrabClient();
                    grabKeyboardWindow = pkg.GetGrabWindow();
                    grabKeyboardTime = xServer.GetTimestamp();
                    grabKeyboardOwnerEvents = pkg.GetOwnerEvents();
                    grabPointerSynchronous = pkg.GetPointerSynchronous();
                    grabKeyboardSynchronous = pkg.GetKeyboardSynchronous();
                }
            }

            if (grabKeyboardWindow == null)
            {
                var w = rootWindow.WindowAtPoint(motionX, motionY);

                if (w.IsAncestor(focusWindow))
                    w.KeyNotify(pressed, motionX, motionY, keycode, null);
                else
                    focusWindow.KeyNotify(pressed, motionX, motionY, keycode, null);
            }
            else if (!grabKeyboardSynchronous)
            {
                grabKeyboardWindow.GrabKeyNotify(pressed, motionX, motionY, keycode, grabKeyboardClient,
                                                 grabKeyboardOwnerEvents);
            } // Else need to queue keyboard events.

            kb.UpdateKeymap(keycode, pressed);

            if (!pressed && grabKeyboardPassiveGrab != null)
            {
                int rk = grabKeyboardPassiveGrab.GetKey();

                if (rk == 0 || rk == keycode)
                {
                    grabKeyboardPassiveGrab = null;
                    grabKeyboardClient = null;
                    grabKeyboardWindow = null;
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
            lock (xServer)
            {
                if (rootWindow == null)
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
            lock (xServer)
            {
                if (rootWindow == null)
                    return false;

                Blank(false); // Reset the screen saver.

                var sendEvent = false;

                switch (keycode)
                {
                    case XKeyEvent.AttrKeycodeBack:
                    case XKeyEvent.AttrKeycodeMenu:
                        return false;
                    case XKeyEvent.AttrKeycodeDpadLeft:
                    case XKeyEvent.AttrKeycodeDpadCenter:
                    case XKeyEvent.AttrKeycodeVolumeUp:
                    case XKeyEvent.AttrKeycodeLeftClickDown:
                        UpdatePointerButtons(1, true);
                        break;
                    case XKeyEvent.AttrKeycodeDpadUp:
                    case XKeyEvent.AttrKeycodeDpadDown:
                        UpdatePointerButtons(2, true);
                        break;
                    case XKeyEvent.AttrKeycodeDpadRight:
                    case XKeyEvent.AttrKeycodeVolumeDown:
                    case XKeyEvent.AttrKeycodeRightClickDown:
                        UpdatePointerButtons(3, true);
                        break;
                    case XKeyEvent.AttrKeycodeShiftLeft:
                    case XKeyEvent.AttrKeycodeShiftRight:
                    case XKeyEvent.AttrKeycodeAltLeft:
                    case XKeyEvent.AttrKeycodeAltRight:
                        //updateModifiers(true, e.getMetaState());
                        sendEvent = true;
                        break;
                    default:
                        sendEvent = true;
                        break;
                }

                if (sendEvent)
                    NotifyKeyPressedReleased(keycode, true);
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
            lock (xServer)
            {
                if (rootWindow == null)
                    return false;

                Blank(false); // Reset the screen saver.

                var sendEvent = false;

                switch (keycode)
                {
                    case XKeyEvent.AttrKeycodeBack:
                    case XKeyEvent.AttrKeycodeMenu:
                        return false;
                    case XKeyEvent.AttrKeycodeDpadLeft:
                    case XKeyEvent.AttrKeycodeDpadCenter:
                    case XKeyEvent.AttrKeycodeVolumeUp:
                    case XKeyEvent.AttrKeycodeLeftClickUp:
                        UpdatePointerButtons(1, false);
                        break;
                    case XKeyEvent.AttrKeycodeDpadUp:
                    case XKeyEvent.AttrKeycodeDpadDown:
                        UpdatePointerButtons(2, false);
                        break;
                    case XKeyEvent.AttrKeycodeDpadRight:
                    case XKeyEvent.AttrKeycodeVolumeDown:
                    case XKeyEvent.AttrKeycodeRightClickUp:
                        UpdatePointerButtons(3, false);
                        break;
                    case XKeyEvent.AttrKeycodeShiftLeft:
                    case XKeyEvent.AttrKeycodeShiftRight:
                    case XKeyEvent.AttrKeycodeAltLeft:
                    case XKeyEvent.AttrKeycodeAltRight:
                        //updateModifiers(false, e.getMetaState());
                        sendEvent = true;
                        break;
                    default:
                        sendEvent = true;
                        break;
                }

                if (sendEvent)
                    NotifyKeyPressedReleased(keycode, false);
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
            var vis = xServer.GetRootVisual();

            io.WriteInt(rootWindow.GetId()); // Root window ID.
            io.WriteInt(defaultColormap.GetId()); // Default colormap ID.
            io.WriteInt(defaultColormap.GetWhitePixel()); // White pixel.
            io.WriteInt(defaultColormap.GetBlackPixel()); // Black pixel.
            io.WriteInt(0); // Current input masks.
            io.WriteShort((short) GetWidth()); // Width in pixels.
            io.WriteShort((short) GetHeight()); // Height in pixels.
            io.WriteShort((short) (GetWidth() / pixelsPerMillimeter)); // Width in millimeters.
            io.WriteShort((short) (GetHeight() / pixelsPerMillimeter)); // Height in millimeters.
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
            var n = installedColormaps.Size();

            lock (io)
            {
                Util.WriteReplyHeader(client, (byte) 0);
                io.WriteInt(n); // Reply length.
                io.WriteShort((short) n); // Number of colormaps.
                io.WritePadBytes(22); // Unused.

                foreach (var cmap in installedColormaps)
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

                        if (time >= grabPointerTime && time <= now && grabPointerClient == client)
                        {
                            grabPointerClient = null;
                            grabPointerWindow = null;
                            grabCursor = null;
                            grabConfineWindow = null;
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

                        if (grabPointerWindow != null && !grabPointerPassive && grabPointerClient == client &&
                            time >= grabPointerTime && time <= now && (cid == 0 || c != null))
                        {
                            grabEventMask = mask;
                            if (c != null)
                                grabCursor = c;
                            else
                                grabCursor = grabPointerWindow.GetCursor();
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

                        if (time >= grabKeyboardTime && time <= now)
                        {
                            var pw = rootWindow.WindowAtPoint(motionX, motionY);

                            Window.FocusInOutNotify(grabKeyboardWindow, focusWindow, pw, rootWindow, 2);
                            grabKeyboardClient = null;
                            grabKeyboardWindow = null;
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

                        if (time <= now && time >= grabPointerTime && time >= grabKeyboardTime)
                        {
                            // Release queued events.
                        }
                    }

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

                        if (focusWindow == null)
                            wid = 0;
                        else if (focusWindow == rootWindow)
                            wid = 1;
                        else
                            wid = focusWindow.GetId();

                        lock (io)
                        {
                            Util.WriteReplyHeader(client, focusRevertTo);
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
                w = rootWindow.WindowAtPoint(motionX, motionY);
            }
            else if (wid == 1)
            {
                // Input focus.
                if (focusWindow == null)
                {
                    ErrorCode.Write(client, ErrorCode.Window, RequestCode.SendEvent, wid);
                    return;
                }

                var pw = rootWindow.WindowAtPoint(motionX, motionY);

                if (pw.IsAncestor(focusWindow))
                    w = pw;
                else
                    w = focusWindow;
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
                    if (wid == 1 && w == focusWindow)
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

            if (time < grabPointerTime || time > now)
            {
                status = 2; // Invalid time.
            }
            else if (grabPointerWindow != null && grabPointerClient != client)
            {
                status = 1; // Already grabbed.
            }
            else
            {
                grabPointerClient = client;
                grabPointerWindow = w;
                grabPointerPassive = false;
                grabPointerAutomatic = false;
                grabPointerTime = time;
                grabCursor = c;
                grabConfineWindow = cw;
                grabEventMask = mask;
                grabPointerOwnerEvents = ownerEvents;
                grabPointerSynchronous = psync;
                grabKeyboardSynchronous = ksync;
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

            if (time < grabKeyboardTime || time > now)
            {
                status = 2; // Invalid time.
            }
            else if (grabKeyboardWindow != null)
            {
                status = 1; // Already grabbed.
            }
            else
            {
                grabKeyboardClient = client;
                grabKeyboardWindow = w;
                grabKeyboardTime = time;
                grabKeyboardOwnerEvents = ownerEvents;
                grabPointerSynchronous = psync;
                grabKeyboardSynchronous = ksync;
            }

            lock (io)
            {
                Util.WriteReplyHeader(client, status);
                io.WriteInt(0); // Reply length.
                io.WritePadBytes(24); // Unused.
            }

            io.Flush();

            if (status == 0)
                Window.FocusInOutNotify(focusWindow, w, rootWindow.WindowAtPoint(motionX, motionY), rootWindow, 1);
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
                w = rootWindow;
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

            if (time < focusLastChangeTime || time > now)
                return;

            Window.FocusInOutNotify(focusWindow, w, rootWindow.WindowAtPoint(motionX, motionY), rootWindow,
                                    grabKeyboardWindow == null ? 0 : 3);

            focusWindow = w;
            focusRevertTo = revertTo;
            focusLastChangeTime = time;
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
    }
}