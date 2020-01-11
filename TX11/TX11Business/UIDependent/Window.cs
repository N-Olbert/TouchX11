using System;
using System.Collections.Generic;
using TX11Business.Compatibility;
using TX11Business.Extensions;
using TX11Shared;
using TX11Shared.Graphics;

namespace TX11Business.UIDependent
{
    internal class Window : Resource
    {
        private readonly ScreenView screen;
        private Window parent;
        private readonly Rect orect;
        private readonly Rect irect;
        private IXRegion boundingShapeRegion;
        private IXRegion clipShapeRegion;
        private IXRegion inputShapeRegion;
        private readonly List<Client> shapeSelectInput;
        private Drawable drawable;
        private Colormap colormap;
        private Cursor cursor;
        private readonly int[] attributes;
        private int borderWidth;
        private readonly bool inputOnly;
        private bool overrideRedirect;
        private bool hardwareAccelerated;
        private readonly List<Window> children;
        private readonly Dictionary<int, Property> properties;
        private readonly HashSet<PassiveButtonGrab> passiveButtonGrabs;
        private readonly HashSet<PassiveKeyGrab> passiveKeyGrabs;
        private bool isMapped;
        private bool exposed;
        private int visibility = NotViewable;
        private IXBitmap backgroundBitmap;
        private int eventMask;
        private readonly Dictionary<Client, int> clientMasks;

        private const int Unobscured = 0;
        private const int PartiallyObscured = 1;
        private const int FullyObscured = 2;
        private const int NotViewable = 3;

        private const int BackgroundPixmap = 0;
        private const int BackgroundPixel = 1;
        private const int BorderPixmap = 2;
        private const int BorderPixel = 3;
        private const int BitGravity = 4;
        private const int WinGravity = 5;
        private const int BackingStore = 6;
        private const int BackingPlanes = 7;
        private const int BackingPixel = 8;
        private const int OverrideRedirect = 9;
        private const int SaveUnder = 10;
        private const int EventMask = 11;
        private const int DoNotPropagateMask = 12;
        private const int Colormap = 13;
        private const int Cursor = 14;

        private const int WinGravityUnmap = 0;
        private const int WinGravityNorthWest = 1;
        private const int WinGravityNorth = 2;
        private const int WinGravityNorthEast = 3;
        private const int WinGravityWest = 4;
        private const int WinGravityCenter = 5;
        private const int WinGravityEast = 6;
        private const int WinGravitySouthWest = 7;
        private const int WinGravitySouth = 8;
        private const int WinGravitySouthEast = 9;
        private const int WinGravityStatic = 10;

        /**
         * Constructor.
         *
         * @param id		The window's ID.
         * @param xServer	The X server.
         * @param client	The client issuing the request.
         * @param screen	The window's screen.
         * @param parent	The window's parent.
         * @param x	X position relative to parent.
         * @param y	Y position relative to parent.
         * @param width	Width of the window.
         * @param height	Height of the window.
         * @param borderWidth	Width of the window's border.
         * @param inputOnly	Is this an InputOnly window?
         * @param isRoot	Is this the root window?
         */
        internal Window(int id, XServer xServer, Client client, ScreenView screen, Window parent, int x, int y,
            int width, int height, int borderWidth, bool inputOnly, bool isRoot) : base(AttrWindow, id, xServer, client)
        {
            this.screen = screen;
            this.parent = parent;
            this.borderWidth = borderWidth;
            colormap = this.screen.GetDefaultColormap();
            this.inputOnly = inputOnly;

            if (isRoot)
            {
                orect = new Rect(0, 0, width, height);
                irect = new Rect(0, 0, width, height);
            }
            else
            {
                var left = this.parent.orect.Left + this.parent.borderWidth + x;
                var top = this.parent.orect.Top + this.parent.borderWidth + y;
                var border = 2 * borderWidth;

                orect = new Rect(left, top, left + width + border, top + height + border);
                if (this.borderWidth == 0)
                    irect = new Rect(orect);
                else
                    irect = new Rect(left + borderWidth, top + borderWidth, orect.Right - borderWidth,
                                     orect.Bottom - borderWidth);
            }

            attributes = new int[]
            {
                0, // background-pixmap = None
                0, // background-pixel = zero
                0, // border-pixmap = CopyFromParent
                0, // border-pixel = zero
                0, // bit-gravity = Forget
                WinGravityNorthWest, // win-gravity = NorthWest
                0, // backing-store = NotUseful
                0xffffffff.AsInt(), // backing-planes = all ones
                0, // backing-pixel = zero
                0, // override-redirect = False
                0, // save-under = False
                0, // event-mask = empty set
                0, // do-not-propogate-mask = empty set
                0, // colormap = CopyFromParent
                0 // cursor = None
            };

            if (isRoot)
            {
                attributes[BackgroundPixel] = 0xffc0c0c0.AsInt();
                isMapped = true;
                cursor = (Cursor) XServer.GetResource(2); // X cursor.
                drawable = new Drawable(width, height, 32, null, attributes[BackgroundPixel]);
                drawable.Clear();
            }
            else
            {
                attributes[BackgroundPixel] = 0xff000000.AsInt();
                drawable = new Drawable(width, height, 32, null, attributes[BackgroundPixel]);
            }

            children = new List<Window>();
            properties = new Dictionary<int, Property>();
            passiveButtonGrabs = new HashSet<PassiveButtonGrab>();
            passiveKeyGrabs = new HashSet<PassiveKeyGrab>();
            clientMasks = new Dictionary<Client, int>();
            shapeSelectInput = new List<Client>();
        }

        /**
         * Is the clip region shaped?
         *
         * @return	True if the clip region is shaped.
         */
        internal bool IsClipShaped()
        {
            return clipShapeRegion != null;
        }

        internal void SetColormap(Colormap cm)
        {
            colormap = cm; //Fix fo  fvwm
        }

        /**
         * Is the bounding region shaped?
         *
         * @return	True if the bounding region is shaped.
         */
        internal bool IsBoundingShaped()
        {
            return boundingShapeRegion != null;
        }

        /**
         * Send a shape notify to all interested clients.
         *
         * @param shapeKind	The kind of shape.
         */
        internal void SendShapeNotify(byte shapeKind)
        {
            var r = GetShapeRegion(shapeKind);
            var shaped = (r != null);
            Rect rect;

            if (r != null)
                rect = r.GetBounds();
            else if (shapeKind == XShape.KindClip)
                rect = irect;
            else
                rect = orect;

            foreach (var client in shapeSelectInput)
            {
                try
                {
                    var io = client.GetInputOutput();

                    lock (io)
                    {
                        io.WriteByte(XShape.EventBase);
                        io.WriteByte((byte) shapeKind);
                        io.WriteShort((short) (client.GetSequenceNumber() & 0xffff));
                        io.WriteInt(Id);
                        io.WriteShort((short) (rect.Left - irect.Left));
                        io.WriteShort((short) (rect.Top - irect.Left));
                        io.WriteShort((short) rect.Width());
                        io.WriteShort((short) rect.Height());
                        io.WriteInt(1);
                        io.WriteByte((byte) (shaped ? 1 : 0));
                        io.WritePadBytes(11);
                    }

                    io.Flush();
                }
                catch (Exception)
                {
                }
            }
        }

        /**
         * Add a client to the shape select input.
         *
         * @param client	The client to add.
         */
        internal void AddShapeSelectInput(Client client)
        {
            Util.Add(shapeSelectInput, client);
        }

        /**
         * Remove a client from the shape select input.
         *
         * @param client	The client to remove.
         */
        internal void RemoveShapeSelectInput(Client client)
        {
            Util.Remove(shapeSelectInput, client);
        }

        /**
         * Does the client is the shape select input list?
         *
         * @param client	The client to check.
         * @return	True if the client is in the shape select input list.
         */
        internal bool ShapeSelectInputEnabled(Client client)
        {
            return Util.Contains(shapeSelectInput, client);
        }

        /**
         * Return a shape region.
         *
         * @param shapeKind	The kind of shape to return.
         * @return	The shape region.
         */
        internal IXRegion GetShapeRegion(byte shapeKind)
        {
            switch (shapeKind)
            {
                case XShape.KindBounding:
                    return boundingShapeRegion;
                case XShape.KindClip:
                    return clipShapeRegion;
                case XShape.KindInput:
                    return inputShapeRegion;
            }

            return null;
        }

        /**
         * Set a shape region.
         *
         * @param shapeKind	The kind of shape to set.
         * @param sr	The shape region.
         */
        internal void SetShapeRegion(byte shapeKind, IXRegion r)
        {
            switch (shapeKind)
            {
                case XShape.KindBounding:
                    boundingShapeRegion = r;
                    break;
                case XShape.KindClip:
                    clipShapeRegion = r;
                    break;
                case XShape.KindInput:
                    inputShapeRegion = r;
                    break;
            }
        }

        /**
         * Return the window's parent.
         *
         * @return	The window's parent.
         */
        internal Window GetParent()
        {
            return parent;
        }

        /**
         * Return the window's screen.
         *
         * @return	The window's screen.
         */
        internal ScreenView GetScreen()
        {
            return screen;
        }

        /**
         * Return the window's drawable.
         *
         * @return	The window's drawable.
         */
        internal Drawable GetDrawable()
        {
            return drawable;
        }

        /**
         * Return the window's cursor.
         *
         * @return	The window's cursor.
         */
        internal Cursor GetCursor()
        {
            if (cursor == null)
                return parent.GetCursor();
            else
                return cursor;
        }

        /**
         * Return the window's inner rectangle.
         *
         * @return	The window's inner rectangle.
         */
        internal Rect GetIRect()
        {
            return irect;
        }

        /**
         * Return the window's outer rectangle.
         *
         * @return	The window's outer rectangle.
         */
        internal Rect GetORect()
        {
            return orect;
        }

        /**
         * Return the window's cumulative event mask.
         *
         * @return	The window's event mask.
         */
        internal int GetEventMask()
        {
            return eventMask;
        }

        /**
         * Return the list of clients selecting on the events.
         *
         * @param mask	The event mask.
         * @return	List of clients, or null if none selecting.
         */
        internal List<Client> GetSelectingClients(int mask)
        {
            if ((mask & eventMask) == 0)
                return null;

            var rc = new List<Client>();
            var sc = clientMasks.Keys;

            foreach (var c in sc)
                if ((clientMasks.Get(c) & mask) != 0)
                    Util.Add(rc, c);

            return rc;
        }

        /**
         * Remove a client from the event selection list.
         * Usually occurs after an I/O error on the client.
         *
         * @param client	The client to remove.
         */
        private void RemoveSelectingClient(Client client)
        {
            Util.Remove(clientMasks, client);

            var sc = clientMasks.Keys;

            eventMask = 0;
            foreach (var c in sc)
                eventMask |= clientMasks.Get(c);
        }

        /**
         * Return the event mask that the client is selecting on.
         *
         * @param client	The client selecting on the events.
         * @return	The event mask, or zero if the client is selecting.
         */
        internal int GetClientEventMask(Client client)
        {
            if (Util.ContainsKey(clientMasks, client))
                return clientMasks.Get(client);
            else
                return 0;
        }

        /**
         * Return the window's do-not-propagate mask.
         *
         * @return	The window's do-not-propagate mask.
         */
        internal int GetDoNotPropagateMask()
        {
            return attributes[DoNotPropagateMask];
        }

        /**
         * Is the window an inferior of this window?
         *
         * @param w	The window being tested.
         *
         * @return	True if the window is a inferior of this window.
         */
        internal bool IsInferior(Window w)
        {
            for (;;)
            {
                if (w.parent == this)
                    return true;
                else if (w.parent == null)
                    return false;
                else
                    w = w.parent;
            }
        }

        /**
         * Is the window an ancestor of this window?
         *
         * @param w	The window being tested.
         *
         * @return	True if the window is an ancestor of this window.
         */
        internal bool IsAncestor(Window w)
        {
            return w.IsInferior(this);
        }

        /**
         * Is the window viewable? It and all its ancestors must be mapped.
         *
         * @return	True if the window is viewable.
         */
        internal bool IsViewable()
        {
            for (var w = this; w != null; w = w.parent)
                if (!w.isMapped)
                    return false;

            return true;
        }

        /**
         * Draw the window and its mapped children.
         * 
         * @param canvas	The canvas to draw to.
         * @param paint		A paint to draw with.
         */
        internal void Draw(IXCanvas canvas, IXPaint paint)
        {
            if (!isMapped)
                return;

            if (boundingShapeRegion != null)
            {
                canvas.Save();

                if (!hardwareAccelerated)
                {
                    try
                    {
                        canvas.ClipRegion(boundingShapeRegion);
                    }
                    catch (Exception)
                    {
                        hardwareAccelerated = true;
                    }
                }

                paint.Color = (attributes[BorderPixel] | 0xff000000).AsInt();
                paint.Style = XPaintStyle.Fill;
                canvas.DrawRect(orect, paint);
            }
            else if (borderWidth != 0)
            {
                if (!Rect.Intersects(orect, canvas.GetClipBounds()))
                    return;

                var hbw = (int) (0.5f * borderWidth);

                paint.Color = (attributes[BorderPixel] | 0xff000000).AsInt();
                paint.StrokeWidth = (borderWidth);
                paint.Style = XPaintStyle.Stroke;

                var r = new Rect(orect.Left + hbw, orect.Top + hbw, orect.Right - hbw, orect.Bottom - hbw);
                canvas.DrawRect(r.X, r.Y, r.Width(), r.Height(), paint);
            }

            canvas.Save();

            bool clipIntersect;

            if (clipShapeRegion != null && !hardwareAccelerated)
            {
                try
                {
                    clipIntersect = canvas.ClipRegion(clipShapeRegion);
                }
                catch (Exception)
                {
                    hardwareAccelerated = true;
                    clipIntersect = canvas.ClipRect(irect);
                }
            }
            else
            {
                clipIntersect = canvas.ClipRect(irect);
            }

            if (clipIntersect)
            {
                if (!inputOnly)
                    canvas.DrawBitmap(drawable.GetBitmap(), irect.Left, irect.Top, paint);
                foreach (var w in children)
                    w.Draw(canvas, paint);
            }

            canvas.Restore();
            if (boundingShapeRegion != null)
                canvas.Restore();
        }

        /**
         * Return the mapped window whose input area contains the specified point.
         *
         * @param x	X coordinate of the point.
         * @param y	Y coordinate of the point.
         * @return	The mapped window containing the point.
         */
        internal Window WindowAtPoint(int x, int y)
        {
            for (var i = children.Size() - 1; i >= 0; i--)
            {
                var w = children.ElementAt(i);

                if (!w.isMapped)
                    continue;

                if (w.inputShapeRegion != null)
                {
                    if (w.inputShapeRegion.Contains(x, y))
                        return w.WindowAtPoint(x, y);
                }
                else if (w.orect.Contains(x, y))
                {
                    return w.WindowAtPoint(x, y);
                }
            }

            return this;
        }

        /**
         * Find a passive button grab on this window or its ancestors.
         *
         * @param buttons	The pointer buttons and modifiers currently pressed.
         * @param highestPbg	Highest pointer grab found so far.
         * @return	The passive pointer grab from the highest ancestor.
         */
        internal PassiveButtonGrab FindPassiveButtonGrab(int buttons, PassiveButtonGrab highestPbg)
        {
            foreach (var pbg in passiveButtonGrabs)
            {
                if (pbg.MatchesEvent(buttons))
                {
                    highestPbg = pbg;
                    break;
                }
            }

            if (parent == null)
                return highestPbg;
            else
                return parent.FindPassiveButtonGrab(buttons, highestPbg);
        }

        /**
         * Add a passive button grab.
         *
         * @param pbg	The passive button grab.
         */
        internal void AddPassiveButtonGrab(PassiveButtonGrab pbg)
        {
            RemovePassiveButtonGrab(pbg.GetButton(), pbg.GetModifiers());
            Util.Add(passiveButtonGrabs, pbg);
        }

        /**
         * Remove all passive button grabs that match the button and modifiers
         * combination.
         *
         * @param button	The button, or 0 for any button.
         * @param modifiers	The modifier mask, or 0x8000 for any.
         */
        internal void RemovePassiveButtonGrab(byte button, int modifiers)
        {
            //Iterator<PassiveButtonGrab> it = _passiveButtonGrabs.iterator();

            //while (it.hasNext())
            //{
            //	PassiveButtonGrab pbg = it.next();

            //	if (pbg.matchesGrab(button, modifiers))
            //		it.remove();
            //}

            passiveButtonGrabs.RemoveWhere(pbg => pbg.MatchesGrab(button, modifiers));
        }

        /**
         * Find a passive key grab on this window or its ancestors.
         *
         * @param key	The key that was pressed.
         * @param modifiers	The modifiers currently pressed.
         * @param highestPkg	Highest key grab found so far.
         * @return	The passive key grab from the highest ancestor.
         */
        internal PassiveKeyGrab FindPassiveKeyGrab(int key, int modifiers, PassiveKeyGrab highestPkg)
        {
            foreach (var pkg in passiveKeyGrabs)
            {
                if (pkg.MatchesEvent(key, modifiers))
                {
                    highestPkg = pkg;
                    break;
                }
            }

            if (parent == null)
                return highestPkg;
            else
                return parent.FindPassiveKeyGrab(key, modifiers, highestPkg);
        }

        /**
         * Add a passive key grab.
         *
         * @param pkg	The passive key grab.
         */
        internal void AddPassiveKeyGrab(PassiveKeyGrab pkg)
        {
            RemovePassiveKeyGrab(pkg.GetKey(), pkg.GetModifiers());
            Util.Add(passiveKeyGrabs, pkg);
        }

        /**
         * Remove all passive key grabs that match the key and modifiers
         * combination.
         *
         * @param key	The key, or 0 for any key.
         * @param modifiers	The modifier mask, or 0x8000 for any.
         */
        internal void RemovePassiveKeyGrab(byte key, int modifiers)
        {
            //Iterator<PassiveKeyGrab> it = _passiveKeyGrabs.iterator();

            //while (it.hasNext())
            //{
            //	PassiveKeyGrab pkg = it.next();

            //	if (pkg.matchesGrab(key, modifiers))
            //		it.remove();
            //}

            passiveKeyGrabs.RemoveWhere(pkg => pkg.MatchesGrab(key, modifiers));
        }

        /**
         * Process a CreateWindow.
         * Create a window with the specified ID, with this window as parent.
         *
         * @param io	The input/output stream.
         * @param client	The client issuing the request.
         * @param sequenceNumber	The request sequence number.
         * @param id	The ID of the window to create.
         * @param depth		The window depth.
         * @param bytesRemaining	Bytes yet to be read in the request.
         * @return	True if the window was created successfully.

         */
        internal bool ProcessCreateWindowRequest(InputOutput io, Client client, int sequenceNumber, int id, int depth,
            int bytesRemaining)
        {
            int x = (short) io.ReadShort(); // X position.
            int y = (short) io.ReadShort(); // Y position.
            var width = io.ReadShort(); // Window width.
            var height = io.ReadShort(); // Window height.
            var borderWidth = io.ReadShort(); // Border width.
            var wclass = io.ReadShort(); // Window class.
            Window w;
            bool inputOnly;

            io.ReadInt(); // Visual.
            bytesRemaining -= 16;

            if (wclass == 0) // Copy from parent.
                inputOnly = this.inputOnly;
            else if (wclass == 1) // Input/output.
                inputOnly = false;
            else
                inputOnly = true;

            try
            {
                w = new Window(id, XServer, client, screen, this, x, y, width, height, borderWidth, inputOnly, false);
            }
            catch (OutOfMemoryException)
            {
                io.ReadSkip(bytesRemaining);
                ErrorCode.Write(client, ErrorCode.Alloc, RequestCode.CreateWindow, 0);
                return false;
            }

            if (!w.ProcessWindowAttributes(client, RequestCode.CreateWindow, bytesRemaining))
                return false;

            w.drawable.Clear();

            XServer.AddResource(w);
            client.AddResource(w);
            Util.Add(children, w);

            List<Client> sc;

            if ((sc = GetSelectingClients(EventCode.MaskSubstructureNotify)) != null)
            {
                foreach (var c in sc)
                    EventCode.SendCreateNotify(c, this, w, x, y, width, height, borderWidth, overrideRedirect);
            }

            return true;
        }

        /**
         * Request a redraw of the window.
         */
        internal void Invalidate()
        {
            screen.PostInvalidate(orect.Left, orect.Top, orect.Right, orect.Bottom);
        }

        /**
         * Request a redraw of a region of the window.
         * 
         * @param x	X coordinate of the region.
         * @param y	Y coordinate of the region.
         * @param width	Width of the region.
         * @param height	Height of the region.
         */
        internal void Invalidate(int x, int y, int width, int height)
        {
            screen.PostInvalidate(irect.Left + x, irect.Top + y, irect.Left + x + width, irect.Top + y + height);
        }

        /**
         * Delete this window from its parent.
         * Used when a client disconnects.
         */
        internal override void Delete()
        {
            List<Client> psc, sc;

            // Send unmap and destroy notification to any other clients that
            // are listening.
            RemoveSelectingClient(Client);
            parent.RemoveSelectingClient(Client);

            sc = GetSelectingClients(EventCode.MaskStructureNotify);
            psc = parent.GetSelectingClients(EventCode.MaskSubstructureNotify);

            if (isMapped)
            {
                screen.RevertFocus(this);
                isMapped = false;

                if (sc != null)
                {
                    foreach (var c in sc)
                    {
                        try
                        {
                            EventCode.SendUnmapNotify(c, this, this, false);
                        }
                        catch (Exception)
                        {
                            RemoveSelectingClient(c);
                        }
                    }
                }

                if (psc != null)
                {
                    foreach (var c in psc)
                    {
                        try
                        {
                            EventCode.SendUnmapNotify(c, parent, this, false);
                        }
                        catch (Exception)
                        {
                            RemoveSelectingClient(c);
                        }
                    }
                }

                UpdateAffectedVisibility();
                Invalidate();
            }

            if (sc != null)
            {
                foreach (var c in sc)
                {
                    try
                    {
                        EventCode.SendDestroyNotify(c, this, this);
                    }
                    catch (Exception)
                    {
                        RemoveSelectingClient(c);
                    }
                }
            }

            if (psc != null)
            {
                foreach (var c in psc)
                {
                    try
                    {
                        EventCode.SendDestroyNotify(c, parent, this);
                    }
                    catch (Exception)
                    {
                        RemoveSelectingClient(c);
                    }
                }
            }

            screen.DeleteWindow(this);

            if (parent != null)
                Util.Remove(parent.children, this);

            base.Delete();
        }

        /**
         * Process a list of window attributes.
         *
         * @param client	The remote client.
         * @param opcode	The opcode being processed.
         * @param bytesRemaining	Bytes yet to be read in the request.
         * @return	True if the window is successfully created.

         */
        private bool ProcessWindowAttributes(Client client, byte opcode, int bytesRemaining)
        {
            var io = client.GetInputOutput();

            if (bytesRemaining < 4)
            {
                io.ReadSkip(bytesRemaining);
                ErrorCode.Write(client, ErrorCode.Length, opcode, 0);
                return false;
            }

            var valueMask = io.ReadInt(); // Value mask.
            var n = Util.Bitcount(valueMask);

            bytesRemaining -= 4;
            if (bytesRemaining != n * 4)
            {
                io.ReadSkip(bytesRemaining);
                ErrorCode.Write(client, ErrorCode.Length, opcode, 0);
                return false;
            }

            for (var i = 0; i < 15; i++)
                if ((valueMask & (1 << i)) != 0)
                    ProcessValue(io, i);

            if (opcode == RequestCode.CreateWindow) // Apply all values on create.
                valueMask = 0xffffffff.AsInt();

            return ApplyValues(client, opcode, valueMask);
        }

        /**
         * Process a single window attribute value.
         *
         * @param io	The input/output stream.
         * @param maskBit	The mask bit of the attribute.

         */
        private void ProcessValue(InputOutput io, int maskBit)
        {
            switch (maskBit)
            {
                case BackgroundPixmap:
                case BackgroundPixel:
                case BorderPixmap:
                case BorderPixel:
                case BackingPlanes:
                case BackingPixel:
                case EventMask:
                case DoNotPropagateMask:
                case Colormap:
                case Cursor:
                    attributes[maskBit] = io.ReadInt();
                    break;
                case BitGravity:
                case WinGravity:
                case BackingStore:
                case OverrideRedirect:
                case SaveUnder:
                    attributes[maskBit] = io.ReadByte();
                    io.ReadSkip(3);
                    break;
            }
        }

        /**
         * Apply the attribute values to the window.
         *
         * @param client	The remote client.
         * @param opcode	The opcode being processed.
         * @param mask	Bit mask of the attributes that have changed.
         * @return	True if the values are all valid.

         */
        private bool ApplyValues(Client client, byte opcode, int mask)
        {
            var ok = true;

            if ((mask & (1 << BackgroundPixmap)) != 0)
            {
                var pmid = attributes[BackgroundPixmap];

                if (pmid == 0)
                {
                    // None.
                    backgroundBitmap = null;
                    drawable.SetBackgroundBitmap(null);
                }
                else if (pmid == 1)
                {
                    // ParentRelative.
                    backgroundBitmap = parent.backgroundBitmap;
                    attributes[BackgroundPixel] = parent.attributes[BackgroundPixel];
                    drawable.SetBackgroundBitmap(backgroundBitmap);
                    drawable.SetBackgroundColor((attributes[BackgroundPixel] | 0xff000000).AsInt());
                }
                else
                {
                    var r = XServer.GetResource(pmid);

                    if (r != null && r.GetRessourceType() == Resource.AttrPixmap)
                    {
                        var p = (Pixmap) r;
                        var d = p.GetDrawable();

                        backgroundBitmap = d.GetBitmap();
                        drawable.SetBackgroundBitmap(backgroundBitmap);
                    }
                    else
                    {
                        ErrorCode.Write(client, ErrorCode.Colormap, opcode, pmid);
                        ok = false;
                    }
                }
            }

            if ((mask & (1 << BackgroundPixel)) != 0)
                drawable.SetBackgroundColor((attributes[BackgroundPixel] | 0xff000000).AsInt());

            if ((mask & (1 << Colormap)) != 0)
            {
                var cid = attributes[Colormap];

                if (cid != 0)
                {
                    var r = XServer.GetResource(cid);

                    if (r != null && r.GetRessourceType() == Resource.AttrColormap)
                    {
                        colormap = (Colormap) r;
                    }
                    else
                    {
                        ErrorCode.Write(client, ErrorCode.Colormap, opcode, cid);
                        ok = false;
                    }
                }
                else if (parent != null && parent.colormap != null)
                {
                    colormap = parent.colormap;
                }
            }

            if ((mask & (1 << EventMask)) != 0)
            {
                clientMasks.Put(client, attributes[EventMask]);

                var sc = clientMasks.Keys;

                eventMask = 0;
                foreach (var c in sc)
                    eventMask |= clientMasks.Get(c);
            }

            if ((mask & (1 << OverrideRedirect)) != 0)
                overrideRedirect = (attributes[OverrideRedirect] == 1);

            if ((mask & (1 << Cursor)) != 0)
            {
                var cid = attributes[Cursor];

                if (cid != 0)
                {
                    var r = XServer.GetResource(cid);

                    if (r != null && r.GetRessourceType() == Resource.AttrCursor)
                    {
                        cursor = (Cursor) r;
                    }
                    else
                    {
                        ErrorCode.Write(client, ErrorCode.Cursor, opcode, cid);
                        ok = false;
                    }
                }
                else
                {
                    cursor = null;
                }
            }

            return ok;
        }

        /**
         * Notify that the pointer has entered this window.
         *
         * @param x	Pointer X coordinate.
         * @param y	Pointer Y coordinate.
         * @param detail	0=Ancestor, 1=Virtual, 2=Inferior, 3=Nonlinear,
         * 					4=NonlinearVirtual.
         * @param toWindow	Window containing pointer.
         * @param mode	0=Normal, 1=Grab, 2=Ungrab.
         */
        private void EnterNotify(int x, int y, int detail, Window toWindow, int mode)
        {
            if (!isMapped)
                return;

            List<Client> sc;

            if ((sc = GetSelectingClients(EventCode.MaskEnterWindow)) == null)
                return;

            var child = (toWindow.parent == this) ? toWindow : null;
            var fw = screen.GetFocusWindow();
            var focus = false;

            if (fw != null)
                focus = (fw == this) || IsAncestor(fw);

            foreach (var c in sc)
            {
                try
                {
                    EventCode.SendEnterNotify(c, XServer.GetTimestamp(), detail, screen.GetRootWindow(), this, child, x,
                                              y, x - irect.Left, y - irect.Top, screen.GetButtons(), mode, focus);
                }
                catch (Exception)
                {
                    RemoveSelectingClient(c);
                }
            }

            sc = GetSelectingClients(EventCode.MaskKeymapState);
            if (sc != null)
            {
                var kb = XServer.GetKeyboard();

                foreach (var c in sc)
                {
                    try
                    {
                        EventCode.SendKeymapNotify(c, kb.GetKeymap());
                    }
                    catch (Exception)
                    {
                        RemoveSelectingClient(c);
                    }
                }
            }
        }

        /**
         * Notify that the pointer has left this window.
         *
         * @param x	Pointer X coordinate.
         * @param y	Pointer Y coordinate.
         * @param detail	0=Ancestor, 1=Virtual, 2=Inferior, 3=Nonlinear, 4=NonlinearVirtual.
         * @param fromWindow	Window previously containing pointer.
         * @param mode	0=Normal, 1=Grab, 2=Ungrab.
         */
        private void LeaveNotify(int x, int y, int detail, Window fromWindow, int mode)
        {
            if (!isMapped)
                return;

            List<Client> sc;

            if ((sc = GetSelectingClients(EventCode.MaskLeaveWindow)) == null)
                return;

            var child = (fromWindow.parent == this) ? fromWindow : null;
            var fw = screen.GetFocusWindow();
            var focus = false;

            if (fw != null)
                focus = (fw == this) || IsAncestor(fw);

            foreach (var c in sc)
            {
                try
                {
                    EventCode.SendLeaveNotify(c, XServer.GetTimestamp(), detail, screen.GetRootWindow(), this, child, x,
                                              y, x - irect.Left, y - irect.Top, screen.GetButtons(), mode, focus);
                }
                catch (Exception)
                {
                    RemoveSelectingClient(c);
                }
            }
        }

        /**
         * Called when the pointer leaves this window and enters another.
         *
         * @param x	Pointer X coordinate.
         * @param y	Pointer Y coordinate.
         * @param ew	The window being entered.
         * @param mode	0=Normal, 1=Grab, 2=Ungrab.
         */
        internal void LeaveEnterNotify(int x, int y, Window ew, int mode)
        {
            if (ew.IsInferior(this))
            {
                LeaveNotify(x, y, 0, this, mode);

                for (var w = parent; w != ew; w = w.parent)
                    w.LeaveNotify(x, y, 1, this, 0);

                ew.EnterNotify(x, y, 2, ew, mode);
            }
            else if (IsInferior(ew))
            {
                LeaveNotify(x, y, 2, this, mode);

                var stack = new Stack<Window>();

                for (var w = ew.parent; w != this; w = w.parent)
                    stack.Push(w);

                while (stack.Count > 0)
                {
                    var w = stack.Pop();

                    w.EnterNotify(x, y, 1, ew, mode);
                }

                ew.EnterNotify(x, y, 0, ew, mode);
            }
            else
            {
                LeaveNotify(x, y, 3, this, 0);

                Window lca = null;
                var stack = new Stack<Window>();

                for (var w = parent; w != ew; w = w.parent)
                {
                    if (w.IsInferior(ew))
                    {
                        lca = w;
                        break;
                    }
                    else
                    {
                        w.LeaveNotify(x, y, 4, this, mode);
                    }
                }

                for (var w = ew.parent; w != lca; w = w.parent)
                    stack.Push(w);

                while (stack.Count > 0)
                {
                    var w = stack.Pop();

                    w.EnterNotify(x, y, 4, ew, mode);
                }

                ew.EnterNotify(x, y, 3, ew, mode);
            }
        }

        /**
         * Notify that this window has gained keyboard focus.
         *
         * @param detail	0=Ancestor, 1=Virtual, 2=Inferior, 3=Nonlinear,
         *					4=NonlinearVirtual, 5=Pointer, 6=PointerRoot, 7=None.
         * @param mode	0=Normal, 1=Grab, 2=Ungrab, 3=WhileGrabbed.
         */
        private void FocusInNotify(int detail, int mode)
        {
            if (!isMapped)
                return;

            List<Client> sc;

            if ((sc = GetSelectingClients(EventCode.MaskFocusChange)) == null)
                return;

            foreach (var c in sc)
            {
                try
                {
                    EventCode.SendFocusIn(c, XServer.GetTimestamp(), detail, this, mode);
                }
                catch (Exception)
                {
                    RemoveSelectingClient(c);
                }
            }

            sc = GetSelectingClients(EventCode.MaskKeymapState);
            if (sc != null)
            {
                var kb = XServer.GetKeyboard();

                foreach (var c in sc)
                {
                    try
                    {
                        EventCode.SendKeymapNotify(c, kb.GetKeymap());
                    }
                    catch (Exception)
                    {
                        RemoveSelectingClient(c);
                    }
                }
            }
        }

        /**
         * Notify that this window has lost keyboard focus.
         *
         * @param detail	0=Ancestor, 1=Virtual, 2=Inferior, 3=Nonlinear,
         *					4=NonlinearVirtual, 5=Pointer, 6=PointerRoot, 7=None.
         * @param mode	0=Normal, 1=Grab, 2=Ungrab, 3=WhileGrabbed.
         */
        private void FocusOutNotify(int detail, int mode)
        {
            if (!isMapped)
                return;

            List<Client> sc;

            if ((sc = GetSelectingClients(EventCode.MaskFocusChange)) == null)
                return;

            foreach (var c in sc)
            {
                try
                {
                    EventCode.SendFocusOut(c, XServer.GetTimestamp(), detail, this, mode);
                }
                catch (Exception)
                {
                    RemoveSelectingClient(c);
                }
            }
        }

        /**
         * Called when keyboard focus changes from one window to another.
         * Handles the FocusIn and FocusOut events.
         *
         * @param wlose	The window that is losing focus.
         * @param wgain	The window that is gaining focus.
         * @param wp	The window containing the pointer.
         * @param wroot	The root window.
         * @param mode	0=Normal, 1=Grab, 2=Ungrab, 3=WhileGrabbed.
         */
        internal static void FocusInOutNotify(Window wlose, Window wgain, Window wp, Window wroot, int mode)
        {
            if (wlose == wgain)
                return;

            if (wlose == null)
            {
                wroot.FocusOutNotify(7, mode);

                if (wgain == wroot)
                {
                    wroot.FocusInNotify(6, mode);

                    var stack = new Stack<Window>();

                    for (var w = wp; w != null; w = w.parent)
                        stack.Push(w);

                    while (!stack.Empty())
                    {
                        var w = stack.Pop();

                        w.FocusInNotify(5, mode);
                    }
                }
                else
                {
                    var stack = new Stack<Window>();

                    for (var w = wgain.parent; w != null; w = w.parent)
                        stack.Push(w);

                    while (!stack.Empty())
                    {
                        var w = stack.Pop();

                        w.FocusInNotify(4, mode);
                    }

                    wgain.FocusInNotify(3, mode);

                    if (wgain.IsInferior(wp))
                    {
                        for (var w = wp; w != wgain; w = w.parent)
                            stack.Push(w);

                        while (!stack.Empty())
                        {
                            var w = stack.Pop();

                            w.FocusInNotify(5, mode);
                        }
                    }
                }
            }
            else if (wlose == wroot)
            {
                for (var w = wp; w != null; w = w.parent)
                    w.FocusOutNotify(5, mode);

                wroot.FocusOutNotify(6, mode);

                if (wgain == null)
                {
                    wroot.FocusInNotify(7, mode);
                }
                else
                {
                    var stack = new Stack<Window>();

                    for (var w = wgain.parent; w != null; w = w.parent)
                        stack.Push(w);

                    while (!stack.Empty())
                    {
                        var w = stack.Pop();

                        w.FocusInNotify(4, mode);
                    }

                    wgain.FocusInNotify(3, mode);

                    if (wgain.IsInferior(wp))
                    {
                        for (var w = wp; w != wgain; w = w.parent)
                            stack.Push(w);

                        while (!stack.Empty())
                        {
                            var w = stack.Pop();

                            w.FocusInNotify(5, mode);
                        }
                    }
                }
            }
            else if (wgain == null)
            {
                if (wlose.IsInferior(wp))
                    for (var w = wp; w != wlose; w = w.parent)
                        w.FocusOutNotify(5, mode);

                wlose.FocusOutNotify(3, mode);
                for (var w = wlose.parent; w != null; w = w.parent)
                    w.FocusOutNotify(4, mode);
                wroot.FocusInNotify(7, mode);
            }
            else if (wgain == wroot)
            {
                if (wlose.IsInferior(wp))
                    for (var w = wp; w != wlose; w = w.parent)
                        w.FocusOutNotify(5, mode);

                wlose.FocusOutNotify(3, mode);
                for (var w = wlose.parent; w != null; w = w.parent)
                    w.FocusOutNotify(4, mode);
                wroot.FocusInNotify(6, mode);

                var stack = new Stack<Window>();

                for (var w = wp; w != null; w = w.parent)
                    stack.Push(w);

                while (!stack.Empty())
                {
                    var w = stack.Pop();

                    w.FocusInNotify(5, mode);
                }
            }
            else if (wgain.IsInferior(wlose))
            {
                wlose.FocusOutNotify(0, mode);

                for (var w = wlose.parent; w != wgain; w = w.parent)
                    w.FocusOutNotify(1, mode);

                wgain.FocusInNotify(2, mode);

                if (wgain.IsInferior(wp) && (wp != wlose && !wp.IsInferior(wlose) && !wp.IsAncestor(wlose)))
                {
                    var stack = new Stack<Window>();

                    for (var w = wp; w != wgain; w = w.parent)
                        stack.Push(w);

                    while (!stack.Empty())
                    {
                        var w = stack.Pop();

                        w.FocusInNotify(5, mode);
                    }
                }
            }
            else if (wlose.IsInferior(wgain))
            {
                if (wlose.IsInferior(wp) && (wp != wgain && !wp.IsInferior(wgain) && !wp.IsAncestor(wgain)))
                {
                    for (var w = wp; w != wlose; w = w.parent)
                        w.FocusOutNotify(5, mode);
                }

                wlose.FocusOutNotify(2, mode);

                var stack = new Stack<Window>();

                for (var w = wgain.parent; w != wlose; w = w.parent)
                    stack.Push(w);

                while (!stack.Empty())
                {
                    var w = stack.Pop();

                    w.FocusInNotify(1, mode);
                }

                wgain.FocusInNotify(0, mode);
            }
            else
            {
                if (wlose.IsInferior(wp))
                    for (var w = wp; w != wlose; w = w.parent)
                        w.FocusOutNotify(5, mode);

                wlose.FocusOutNotify(3, 0);

                Window lca = null;
                var stack = new Stack<Window>();

                for (var w = wlose.parent; w != wgain; w = w.parent)
                {
                    if (w.IsInferior(wgain))
                    {
                        lca = w;
                        break;
                    }
                    else
                    {
                        w.FocusOutNotify(4, mode);
                    }
                }

                for (var w = wgain.parent; w != lca; w = w.parent)
                    stack.Push(w);

                while (!stack.Empty())
                {
                    var w = stack.Pop();

                    w.FocusInNotify(4, mode);
                }

                wgain.FocusInNotify(3, mode);

                if (wgain.IsInferior(wp))
                {
                    for (var w = wp; w != wgain; w = w.parent)
                        stack.Push(w);

                    while (!stack.Empty())
                    {
                        var w = stack.Pop();

                        w.FocusInNotify(5, mode);
                    }
                }
            }
        }

        /**
         * Called when a button is pressed or released in this window.
         *
         * @param pressed	Whether the button was pressed or released.
         * @param x	Pointer X coordinate.
         * @param y	Pointer Y coordinate.
         * @param button	Button that was pressed/released.
         * @param grabClient	Only send to this client if it isn't null.
         *
         * @return	The window it was sent to, or null if not sent.
         */
        internal Window ButtonNotify(bool pressed, int x, int y, int button, Client grabClient)
        {
            var evw = this;
            Window child = null;
            var mask = pressed ? EventCode.MaskButtonPress : EventCode.MaskButtonRelease;
            List<Client> sc;

            for (;;)
            {
                if (evw.isMapped)
                {
                    sc = evw.GetSelectingClients(mask);
                    if (sc != null)
                        break;
                }

                if (evw.parent == null)
                    return null;

                if ((evw.attributes[DoNotPropagateMask] & mask) != 0)
                    return null;

                child = evw;
                evw = evw.parent;
            }

            Window sentWindow = null;

            foreach (var c in sc)
            {
                if (grabClient != null && grabClient != c)
                    continue;

                try
                {
                    if (pressed)
                        EventCode.SendButtonPress(c, XServer.GetTimestamp(), button, screen.GetRootWindow(), evw, child,
                                                  x, y, x - evw.irect.Left, y - evw.irect.Top, screen.GetButtons());
                    else
                        EventCode.SendButtonRelease(c, XServer.GetTimestamp(), button, screen.GetRootWindow(), evw,
                                                    child, x, y, x - evw.irect.Left, y - evw.irect.Top,
                                                    screen.GetButtons());
                    sentWindow = evw;
                }
                catch (Exception)
                {
                    evw.RemoveSelectingClient(c);
                }
            }

            return sentWindow;
        }

        /**
         * Called when a button is pressed or released while grabbed by this
         * window.
         *
         * @param pressed	Whether the button was pressed or released.
         * @param x	Pointer X coordinate.
         * @param y	Pointer Y coordinate.
         * @param button	Button that was pressed/released.
         * @param eventMask	The events the window is interested in.
         * @param grabClient	The grabbing client.
         * @param ownerEvents	Owner-events flag.
         */
        internal void GrabButtonNotify(bool pressed, int x, int y, int button, int eventMask, Client grabClient,
            bool ownerEvents)
        {
            if (ownerEvents)
            {
                var w = screen.GetRootWindow().WindowAtPoint(x, y);

                if (w.ButtonNotify(pressed, x, y, button, grabClient) != null)
                    return;
            }

            var mask = pressed ? EventCode.MaskButtonPress : EventCode.MaskButtonRelease;

            if ((eventMask & mask) == 0)
                return;

            try
            {
                if (pressed)
                    EventCode.SendButtonPress(grabClient, XServer.GetTimestamp(), button, screen.GetRootWindow(), this,
                                              null, x, y, x - irect.Left, y - irect.Top, screen.GetButtons());
                else
                    EventCode.SendButtonRelease(grabClient, XServer.GetTimestamp(), button, screen.GetRootWindow(),
                                                this, null, x, y, x - irect.Left, y - irect.Top, screen.GetButtons());
            }
            catch (Exception)
            {
                RemoveSelectingClient(grabClient);
            }
        }

        /**
         * Called when a key is pressed or released in this window.
         *
         * @param pressed	Whether the key was pressed or released.
         * @param x	Pointer X coordinate.
         * @param y	Pointer Y coordinate.
         * @param keycode	Keycode of the key.
         * @param grabClient	Only notify this client if it isn't null.
         *
         * @return	True if an event is sent.
         */
        internal bool KeyNotify(bool pressed, int x, int y, int keycode, Client grabClient)
        {
            var evw = this;
            Window child = null;
            var mask = pressed ? EventCode.MaskKeyPress : EventCode.MaskKeyRelease;
            List<Client> sc;

            for (;;)
            {
                if (evw.isMapped)
                {
                    sc = evw.GetSelectingClients(mask);
                    if (sc != null)
                        break;
                }

                if (evw.parent == null)
                    return false;

                if ((evw.attributes[DoNotPropagateMask] & mask) != 0)
                    return false;

                child = evw;
                evw = evw.parent;
            }

            var sent = false;

            foreach (var c in sc)
            {
                if (grabClient != null && grabClient != c)
                    continue;

                try
                {
                    if (pressed)
                        EventCode.SendKeyPress(c, XServer.GetTimestamp(), keycode, screen.GetRootWindow(), evw, child,
                                               x, y, x - evw.irect.Left, y - evw.irect.Top, screen.GetButtons());
                    else
                        EventCode.SendKeyRelease(c, XServer.GetTimestamp(), keycode, screen.GetRootWindow(), evw, child,
                                                 x, y, x - evw.irect.Left, y - evw.irect.Top, screen.GetButtons());
                    sent = true;
                }
                catch (Exception)
                {
                    evw.RemoveSelectingClient(c);
                }
            }

            return sent;
        }

        /**
         * Called when a key is pressed or released while grabbed by this window.
         *
         * @param pressed	Whether the key was pressed or released.
         * @param x	Pointer X coordinate.
         * @param y	Pointer Y coordinate.
         * @param keycode	Keycode of the key.
         * @param grabClient	The grabbing client.
         * @param ownerEvents	Owner-events flag.
         */
        internal void GrabKeyNotify(bool pressed, int x, int y, int keycode, Client grabClient, bool ownerEvents)
        {
            if (ownerEvents)
            {
                var w = screen.GetRootWindow().WindowAtPoint(x, y);

                if (w.KeyNotify(pressed, x, y, keycode, grabClient))
                    return;
            }

            try
            {
                if (pressed)
                    EventCode.SendKeyPress(grabClient, XServer.GetTimestamp(), keycode, screen.GetRootWindow(), this,
                                           null, x, y, x - irect.Left, y - irect.Top, screen.GetButtons());
                else
                    EventCode.SendKeyRelease(grabClient, XServer.GetTimestamp(), keycode, screen.GetRootWindow(), this,
                                             null, x, y, x - irect.Left, y - irect.Top, screen.GetButtons());
            }
            catch (Exception)
            {
                RemoveSelectingClient(grabClient);
            }
        }

        /**
         * Return the event mask that would select on the buttons.
         *
         * @param buttonMask	Currently pressed pointer buttons.
         *
         * @return	The event mask that would select on the buttons.
         */
        private int ButtonEventMask(int buttonMask)
        {
            var mask = EventCode.MaskPointerMotion | EventCode.MaskPointerMotionHint;

            if ((buttonMask & 0x700) == 0)
                return mask;

            mask |= EventCode.MaskButtonMotion;
            if ((buttonMask & 0x100) != 0)
                mask |= EventCode.MaskButton1Motion;
            if ((buttonMask & 0x200) != 0)
                mask |= EventCode.MaskButton2Motion;
            if ((buttonMask & 0x400) != 0)
                mask |= EventCode.MaskButton3Motion;

            return mask;
        }

        /**
         * Called when the pointer moves within this window.
         *
         * @param x	Pointer X coordinate.
         * @param y	Pointer Y coordinate.
         * @param buttonMask	Currently pressed pointer buttons.
         * @param grabClient	Only send notify this client if it isn't null.
         *
         * @return	True if an event is sent.
         */
        internal bool MotionNotify(int x, int y, int buttonMask, Client grabClient)
        {
            var evw = this;
            Window child = null;
            var mask = ButtonEventMask(buttonMask);
            List<Client> sc;

            for (;;)
            {
                if (evw.isMapped)
                {
                    sc = evw.GetSelectingClients(mask);
                    if (sc != null)
                        break;
                }

                if (evw.parent == null)
                    return false;

                if ((evw.attributes[DoNotPropagateMask] & EventCode.MaskPointerMotion) != 0)
                    return false;

                child = evw;
                evw = evw.parent;
            }

            var sent = false;

            foreach (var c in sc)
            {
                if (grabClient != null && grabClient != c)
                    continue;

                var detail = 0; // Normal.
                var em = evw.GetClientEventMask(c);

                if ((em & EventCode.MaskPointerMotionHint) != 0 && (em & EventCode.MaskPointerMotion) == 0)
                    detail = 1; // Hint.

                try
                {
                    EventCode.SendMotionNotify(c, XServer.GetTimestamp(), detail, screen.GetRootWindow(), evw, child, x,
                                               y, x - evw.irect.Left, y - evw.irect.Top, buttonMask);
                }
                catch (Exception)
                {
                    evw.RemoveSelectingClient(c);
                }
            }

            return sent;
        }

        /**
         * Called when the pointer moves while grabbed by this window.
         *
         * @param x	Pointer X coordinate.
         * @param y	Pointer Y coordinate.
         * @param buttonMask	Currently pressed pointer buttons.
         * @param eventMask	The events the window is interested in.
         * @param grabClient	The grabbing client.
         * @param ownerEvents	Owner-events flag.
         */
        internal void GrabMotionNotify(int x, int y, int buttonMask, int eventMask, Client grabClient, bool ownerEvents)
        {
            if (ownerEvents)
            {
                var w = screen.GetRootWindow().WindowAtPoint(x, y);

                if (w.MotionNotify(x, y, buttonMask, grabClient))
                    return;
            }

            var em = ButtonEventMask(buttonMask) & eventMask;

            if (em != 0)
            {
                var detail = 0; // Normal.

                if ((em & EventCode.MaskPointerMotionHint) != 0 && (em & EventCode.MaskPointerMotion) == 0)
                    detail = 1; // Hint.

                try
                {
                    EventCode.SendMotionNotify(grabClient, XServer.GetTimestamp(), detail, screen.GetRootWindow(), this,
                                               null, x, y, x - irect.Left, y - irect.Top, buttonMask);
                }
                catch (Exception)
                {
                    RemoveSelectingClient(grabClient);
                }
            }
        }

        /**
         * Map the window.
         *
         * @param client	The remote client.

         */
        private void Map(Client client)
        {
            if (isMapped)
                return;

            List<Client> sc;

            if (!overrideRedirect)
            {
                sc = parent.GetSelectingClients(EventCode.MaskSubstructureRedirect);
                if (sc != null)
                {
                    foreach (var c in sc)
                    {
                        if (c != client)
                        {
                            EventCode.SendMapRequest(c, parent, this);
                            return;
                        }
                    }
                }
            }

            isMapped = true;

            sc = GetSelectingClients(EventCode.MaskStructureNotify);
            if (sc != null)
            {
                foreach (var c in sc)
                {
                    try
                    {
                        EventCode.SendMapNotify(c, this, this, overrideRedirect);
                    }
                    catch (Exception)
                    {
                        RemoveSelectingClient(c);
                    }
                }
            }

            sc = parent.GetSelectingClients(EventCode.MaskSubstructureNotify);
            if (sc != null)
            {
                foreach (var c in sc)
                {
                    try
                    {
                        EventCode.SendMapNotify(c, parent, this, overrideRedirect);
                    }
                    catch (Exception)
                    {
                        RemoveSelectingClient(c);
                    }
                }
            }

            UpdateAffectedVisibility();

            if (!exposed)
            {
                sc = GetSelectingClients(EventCode.MaskExposure);
                if (sc != null)
                {
                    foreach (var c in sc)
                    {
                        try
                        {
                            EventCode.SendExpose(c, this, 0, 0, drawable.GetWidth(), drawable.GetHeight(), 0);
                        }
                        catch (Exception)
                        {
                            RemoveSelectingClient(c);
                        }
                    }
                }

                exposed = true;
            }
        }

        /**
         * Map the children of this window.
         *
         * @param client	The remote client.

         */
        private void MapSubwindows(Client client)
        {
            foreach (var w in children)
            {
                w.Map(client);
                w.MapSubwindows(client);
            }
        }

        /**
         * Unmap the window.
         *

         */
        private void Unmap()
        {
            if (!isMapped)
                return;

            isMapped = false;

            List<Client> sc;

            sc = GetSelectingClients(EventCode.MaskStructureNotify);
            if (sc != null)
            {
                foreach (var c in sc)
                {
                    try
                    {
                        EventCode.SendUnmapNotify(c, this, this, false);
                    }
                    catch (Exception)
                    {
                        RemoveSelectingClient(c);
                    }
                }
            }

            sc = parent.GetSelectingClients(EventCode.MaskSubstructureNotify);
            if (sc != null)
            {
                foreach (var c in sc)
                {
                    try
                    {
                        EventCode.SendUnmapNotify(c, parent, this, false);
                    }
                    catch (Exception)
                    {
                        RemoveSelectingClient(c);
                    }
                }
            }

            UpdateAffectedVisibility();
            screen.RevertFocus(this);
        }

        /**
         * Unmap the children of this window.
         *

         */
        private void UnmapSubwindows()
        {
            foreach (var w in children)
            {
                w.Unmap();
                w.UnmapSubwindows();
            }
        }

        /**
         * Destroy the window and all its children.
         *
         * @param removeFromParent	If true, remove it from its parent.

         */
        private void Destroy(bool removeFromParent)
        {
            if (parent == null) // No effect on root window.
                return;

            XServer.FreeResource(Id);
            if (isMapped)
                Unmap();

            foreach (var w in children)
                w.Destroy(false);

            Util.Clear(children);

            if (removeFromParent)
                Util.Remove(parent.children, this);

            List<Client> sc;

            sc = GetSelectingClients(EventCode.MaskStructureNotify);
            if (sc != null)
            {
                foreach (var c in sc)
                {
                    try
                    {
                        EventCode.SendDestroyNotify(c, this, this);
                    }
                    catch (Exception)
                    {
                        RemoveSelectingClient(c);
                    }
                }
            }

            sc = parent.GetSelectingClients(EventCode.MaskSubstructureNotify);
            if (sc != null)
            {
                foreach (var c in sc)
                {
                    try
                    {
                        EventCode.SendDestroyNotify(c, parent, this);
                    }
                    catch (Exception)
                    {
                        RemoveSelectingClient(c);
                    }
                }
            }

            drawable.GetBitmap().Recycle();
        }

        /**
         * Change the window's parent.
         *
         * @param client	The remote client.
         * @param parent	New parent.
         * @param x	New X position relative to new parent.
         * @param y	New Y position relative to new parent.

         */
        private void Reparent(Client client, Window parent, int x, int y)
        {
            var mapped = isMapped;

            if (mapped)
                Unmap();

            var orig = new Rect(orect);
            var dx = parent.irect.Left + x - orect.Left;
            var dy = parent.irect.Top + y - orect.Top;

            orect.Left += dx;
            orect.Top += dy;
            orect.Right += dx;
            orect.Bottom += dy;
            irect.Left += dx;
            irect.Top += dy;
            irect.Right += dx;
            irect.Bottom += dy;

            Util.Remove(this.parent.children, this);
            Util.Add(parent.children, this);

            if (dx != 0 || dy != 0)
                foreach (var w in children)
                    w.Move(dx, dy, 0, 0);

            List<Client> sc;

            sc = GetSelectingClients(EventCode.MaskStructureNotify);
            if (sc != null)
            {
                foreach (var c in sc)
                {
                    try
                    {
                        EventCode.SendReparentNotify(c, this, this, parent, x, y, overrideRedirect);
                    }
                    catch (Exception)
                    {
                        RemoveSelectingClient(c);
                    }
                }
            }

            sc = this.parent.GetSelectingClients(EventCode.MaskSubstructureNotify);
            if (sc != null)
            {
                foreach (var c in sc)
                {
                    try
                    {
                        EventCode.SendReparentNotify(c, this.parent, this, parent, x, y, overrideRedirect);
                    }
                    catch (Exception)
                    {
                        RemoveSelectingClient(c);
                    }
                }
            }

            sc = parent.GetSelectingClients(EventCode.MaskSubstructureNotify);
            if (sc != null)
            {
                foreach (var c in sc)
                {
                    try
                    {
                        EventCode.SendReparentNotify(c, parent, this, parent, x, y, overrideRedirect);
                    }
                    catch (Exception)
                    {
                        RemoveSelectingClient(c);
                    }
                }
            }

            this.parent = parent;
            if (mapped)
            {
                Map(client);
                if (!inputOnly)
                    screen.PostInvalidate(orig.Left, orig.Top, orig.Right, orig.Bottom);
            }
        }

        /**
         * Circulate occluded windows.
         *
         * @param client	The remote client.
         * @param direction	0=RaiseLowest, 1=LowerHighest.
         * @return	True if a window is restacked.

         */
        private bool Circulate(Client client, int direction)
        {
            Window sw = null;

            if (direction == 0)
            {
                // Raise lowest occluded.
                foreach (var w in children)
                {
                    if (Occludes(null, w))
                    {
                        sw = w;
                        break;
                    }
                }
            }
            else
            {
                // Lower highest occluding.
                for (var i = children.Size() - 1; i >= 0; i--)
                {
                    var w = children.ElementAt(i);

                    if (Occludes(w, null))
                    {
                        sw = w;
                        break;
                    }
                }
            }

            if (sw == null)
                return false;

            List<Client> sc;

            sc = GetSelectingClients(EventCode.MaskSubstructureRedirect);
            if (sc != null)
            {
                foreach (var c in sc)
                {
                    if (c != client)
                    {
                        try
                        {
                            EventCode.SendCirculateRequest(c, this, sw, direction);
                            return false;
                        }
                        catch (Exception)
                        {
                            RemoveSelectingClient(c);
                        }
                    }
                }
            }

            if (direction == 0)
            {
                Util.Remove(children, sw);
                Util.Add(children, sw);
            }
            else
            {
                Util.Remove(children, sw);
                children.Add(0, sw);
            }

            sc = GetSelectingClients(EventCode.MaskStructureNotify);
            if (sc != null)
            {
                foreach (var c in sc)
                {
                    try
                    {
                        EventCode.SendCirculateNotify(c, this, sw, direction);
                    }
                    catch (Exception)
                    {
                        RemoveSelectingClient(c);
                    }
                }
            }

            sc = parent.GetSelectingClients(EventCode.MaskSubstructureNotify);
            if (sc != null)
            {
                foreach (var c in sc)
                {
                    try
                    {
                        EventCode.SendCirculateNotify(c, parent, sw, direction);
                    }
                    catch (Exception)
                    {
                        RemoveSelectingClient(c);
                    }
                }
            }

            UpdateAffectedVisibility();

            return true;
        }

        /**
         * Does the first window occlude the second?
         * If the first window is null, does any window occlude w2?
         * If the second window is null, is any window occluded by w1?
         *
         * @param w1	First window.
         * @param w2	Second window.
         * @return	True if occlusion occurs.
         */
        private bool Occludes(Window w1, Window w2)
        {
            if (w1 == null)
            {
                if (w2 == null || !w2.isMapped)
                    return false;

                // Does anything occlude w2?
                var r = w2.orect;
                var above = false;

                foreach (var w in children)
                {
                    if (above)
                    {
                        if (w.isMapped && Rect.Intersects(w.orect, r))
                            return true;
                    }
                    else
                    {
                        if (w == w2)
                            above = true;
                    }
                }
            }
            else
            {
                if (w2 == null)
                {
                    // Does w1 occlude anything?
                    if (!w1.isMapped)
                        return false;

                    var r = w1.orect;

                    foreach (var w in children)
                    {
                        if (w == w1)
                            return false;
                        else if (w.isMapped && Rect.Intersects(w.orect, r))
                            return true;
                    }
                }
                else
                {
                    // Does w1 occlude w2?
                    if (!w1.isMapped || !w2.isMapped)
                        return false;
                    if (!Rect.Intersects(w1.orect, w2.orect))
                        return false;

                    return Util.IndexOf(children, w1) > Util.IndexOf(children, w2);
                }
            }

            return false;
        }

        /**
         * Move the window and its children.
         *
         * @param dx	X distance to move.
         * @param dy	Y distance to move.
         * @param dw	The change in the parent's width.
         * @param dh	The change in the parent's height.

         */
        private void Move(int dx, int dy, int dw, int dh)
        {
            if (dw != 0 || dh != 0)
            {
                switch (attributes[WinGravity])
                {
                    case WinGravityUnmap:
                        Unmap();
                        break;
                    case WinGravityNorthWest:
                        break; // No change.
                    case WinGravityNorth:
                        dx += dw / 2;
                        break;
                    case WinGravityNorthEast:
                        dx += dw;
                        break;
                    case WinGravityWest:
                        dy += dh / 2;
                        break;
                    case WinGravityCenter:
                        dx += dw / 2;
                        dy += dh / 2;
                        break;
                    case WinGravityEast:
                        dx += dw;
                        dy += dh / 2;
                        break;
                    case WinGravitySouthWest:
                        dy += dh;
                        break;
                    case WinGravitySouth:
                        dx += dw / 2;
                        dy += dh;
                        break;
                    case WinGravitySouthEast:
                        dx += dw;
                        dy += dh;
                        break;
                    case WinGravityStatic:
                        dx = 0;
                        dy = 0;
                        break;
                }

                List<Client> sc;

                sc = GetSelectingClients(EventCode.MaskStructureNotify);
                if (sc != null)
                {
                    foreach (var c in sc)
                    {
                        try
                        {
                            EventCode.SendGravityNotify(c, this, this, orect.Left + dx - parent.irect.Left,
                                                        orect.Top + dy - parent.irect.Top);
                        }
                        catch (Exception)
                        {
                            RemoveSelectingClient(c);
                        }
                    }
                }

                sc = parent.GetSelectingClients(EventCode.MaskSubstructureNotify);
                if (sc != null)
                {
                    foreach (var c in sc)
                    {
                        try
                        {
                            EventCode.SendGravityNotify(c, parent, this, orect.Left + dx - parent.irect.Left,
                                                        orect.Top + dy - parent.irect.Top);
                        }
                        catch (Exception)
                        {
                            RemoveSelectingClient(c);
                        }
                    }
                }
            }

            if (dx == 0 && dy == 0)
                return;

            irect.Left += dx;
            irect.Right += dx;
            irect.Top += dy;
            irect.Bottom += dy;
            orect.Left += dx;
            orect.Right += dx;
            orect.Top += dy;
            orect.Bottom += dy;

            foreach (var w in children)
                w.Move(dx, dy, 0, 0);
        }

        /**
         * Process a ConfigureWindow request.
         *
         * @param client	The remote client.
         * @param opcode	The opcode being processed.
         * @param bytesRemaining	Bytes yet to be read in the request.
         * @return	True if the window needs to be redrawn.

         */
        private bool ProcessConfigureWindow(Client client, int bytesRemaining)
        {
            var io = client.GetInputOutput();

            if (bytesRemaining < 4)
            {
                io.ReadSkip(bytesRemaining);
                ErrorCode.Write(client, ErrorCode.Length, RequestCode.ConfigureWindow, 0);
                return false;
            }

            var mask = io.ReadShort(); // Value mask.
            var n = Util.Bitcount(mask);

            io.ReadSkip(2); // Unused.
            bytesRemaining -= 4;
            if (bytesRemaining != 4 * n)
            {
                io.ReadSkip(bytesRemaining);
                ErrorCode.Write(client, ErrorCode.Length, RequestCode.ConfigureWindow, 0);
                return false;
            }
            else if (parent == null)
            {
                // No effect on root window.
                io.ReadSkip(bytesRemaining);
                return false;
            }

            var oldLeft = irect.Left;
            var oldTop = irect.Top;
            var oldWidth = irect.Right - irect.Left;
            var oldHeight = irect.Bottom - irect.Top;
            var oldX = orect.Left - parent.irect.Left;
            var oldY = orect.Top - parent.irect.Top;
            var width = oldWidth;
            var height = oldHeight;
            var x = oldX;
            var y = oldY;
            var borderWidth = this.borderWidth;
            var stackMode = 0;
            var changed = false;
            Window sibling = null;
            Rect dirty = null;

            if ((mask & 0x01) != 0)
            {
                x = (short) io.ReadShort(); // X.
                io.ReadSkip(2); // Unused.
            }

            if ((mask & 0x02) != 0)
            {
                y = (short) io.ReadShort(); // Y.
                io.ReadSkip(2); // Unused.
            }

            if ((mask & 0x04) != 0)
            {
                width = (short) io.ReadShort(); // Width.
                io.ReadSkip(2); // Unused.
            }

            if ((mask & 0x08) != 0)
            {
                height = (short) io.ReadShort(); // Height.
                io.ReadSkip(2); // Unused.
            }

            if ((mask & 0x10) != 0)
            {
                borderWidth = (short) io.ReadShort(); // Border width.
                io.ReadSkip(2); // Unused.
            }

            if ((mask & 0x20) != 0)
            {
                var id = io.ReadInt(); // Sibling.
                var r = XServer.GetResource(id);

                if (r == null || r.GetRessourceType() != Resource.AttrWindow)
                {
                    ErrorCode.Write(client, ErrorCode.Window, RequestCode.ConfigureWindow, id);
                    io.ReadSkip(bytesRemaining);
                    return false;
                }
                else
                {
                    sibling = (Window) r;
                }
            }

            if ((mask & 0x40) != 0)
            {
                stackMode = io.ReadByte(); // Stack mode.
                io.ReadSkip(3); // Unused.
            }

            if (!overrideRedirect)
            {
                List<Client> sc;

                sc = parent.GetSelectingClients(EventCode.MaskSubstructureRedirect);
                if (sc != null)
                {
                    foreach (var c in sc)
                    {
                        if (c != client)
                        {
                            EventCode.SendConfigureRequest(c, stackMode, parent, this, sibling, x, y, width, height,
                                                           borderWidth, mask);
                            return false;
                        }
                    }
                }
            }

            if (width != oldWidth || height != oldHeight)
            {
                if (width <= 0 || height <= 0)
                {
                    ErrorCode.Write(client, ErrorCode.Value, RequestCode.ConfigureWindow, 0);
                    return false;
                }

                List<Client> sc;

                sc = GetSelectingClients(EventCode.MaskResizeRedirect);
                if (sc != null)
                {
                    foreach (var c in sc)
                    {
                        if (c != client)
                        {
                            EventCode.SendResizeRequest(c, this, width, height);
                            width = oldWidth;
                            height = oldHeight;
                            break;
                        }
                    }
                }
            }

            if (x != oldX || y != oldY || width != oldWidth || height != oldHeight || borderWidth != this.borderWidth)
            {
                if (width != oldWidth || height != oldHeight)
                {
                    try
                    {
                        drawable = new Drawable(width, height, 32, backgroundBitmap,
                                                (attributes[BackgroundPixel] | 0xff000000).AsInt());
                    }
                    catch (OutOfMemoryException)
                    {
                        ErrorCode.Write(client, ErrorCode.Alloc, RequestCode.ConfigureWindow, 0);
                        return false;
                    }

                    drawable.Clear();
                    exposed = false;
                }

                dirty = new Rect(orect);
                this.borderWidth = borderWidth;
                orect.Left = parent.irect.Left + x;
                orect.Top = parent.irect.Top + y;
                orect.Right = orect.Left + width + 2 * borderWidth;
                orect.Bottom = orect.Top + height + 2 * borderWidth;
                irect.Left = orect.Left + borderWidth;
                irect.Top = orect.Top + borderWidth;
                irect.Right = orect.Right - borderWidth;
                irect.Bottom = orect.Bottom - borderWidth;
                changed = true;
            }

            if ((mask & 0x60) != 0)
            {
                if (sibling != null && sibling.parent != parent)
                {
                    ErrorCode.Write(client, ErrorCode.Match, RequestCode.ConfigureWindow, 0);
                    return false;
                }

                if (sibling == null)
                {
                    switch (stackMode)
                    {
                        case 0: // Above.
                            Util.Remove(parent.children, this);
                            Util.Add(parent.children, this);
                            changed = true;
                            break;
                        case 1: // Below.
                            Util.Remove(parent.children, this);
                            parent.children.Add(0, this);
                            changed = true;
                            break;
                        case 2: // TopIf.
                            if (parent.Occludes(null, this))
                            {
                                Util.Remove(parent.children, this);
                                Util.Add(parent.children, this);
                                changed = true;
                            }

                            break;
                        case 3: // BottomIf.
                            if (parent.Occludes(this, null))
                            {
                                Util.Remove(parent.children, this);
                                parent.children.Add(0, this);
                                changed = true;
                            }

                            break;
                        case 4: // Opposite.
                            if (parent.Occludes(null, this))
                            {
                                Util.Remove(parent.children, this);
                                Util.Add(parent.children, this);
                                changed = true;
                            }
                            else if (parent.Occludes(this, null))
                            {
                                Util.Remove(parent.children, this);
                                parent.children.Add(0, this);
                                changed = true;
                            }

                            break;
                    }
                }
                else
                {
                    int pos;

                    switch (stackMode)
                    {
                        case 0: // Above.
                            Util.Remove(parent.children, this);
                            pos = Util.IndexOf(parent.children, sibling);
                            parent.children.Add(pos + 1, this);
                            changed = true;
                            break;
                        case 1: // Below.
                            Util.Remove(parent.children, this);
                            pos = Util.IndexOf(parent.children, sibling);
                            parent.children.Add(pos, this);
                            changed = true;
                            break;
                        case 2: // TopIf.
                            if (parent.Occludes(sibling, this))
                            {
                                Util.Remove(parent.children, this);
                                Util.Add(parent.children, this);
                                changed = true;
                            }

                            break;
                        case 3: // BottomIf.
                            if (parent.Occludes(this, sibling))
                            {
                                Util.Remove(parent.children, this);
                                parent.children.Add(0, this);
                                changed = true;
                            }

                            break;
                        case 4: // Opposite.
                            if (parent.Occludes(sibling, this))
                            {
                                Util.Remove(parent.children, this);
                                Util.Add(parent.children, this);
                                changed = true;
                            }
                            else if (parent.Occludes(this, sibling))
                            {
                                Util.Remove(parent.children, this);
                                parent.children.Add(0, this);
                                changed = true;
                            }

                            break;
                    }
                }
            }

            if (changed)
            {
                List<Client> sc;

                sc = GetSelectingClients(EventCode.MaskStructureNotify);
                if (sc != null)
                {
                    foreach (var c in sc)
                        EventCode.SendConfigureNotify(c, this, this, null, x, y, width, height, this.borderWidth,
                                                      overrideRedirect);
                }

                sc = parent.GetSelectingClients(EventCode.MaskSubstructureNotify);
                if (sc != null)
                {
                    foreach (var c in sc)
                        EventCode.SendConfigureNotify(c, parent, this, null, x, y, width, height, this.borderWidth,
                                                      overrideRedirect);
                }

                if (irect.Left != oldLeft || irect.Top != oldTop || width != oldWidth || height != oldHeight)
                    foreach (var w in children)
                        w.Move(irect.Left - oldLeft, irect.Top - oldTop, width - oldWidth, height - oldHeight);

                UpdateAffectedVisibility();
            }

            if (!exposed)
            {
                List<Client> sc;

                if ((sc = GetSelectingClients(EventCode.MaskExposure)) != null)
                {
                    foreach (var c in sc)
                        EventCode.SendExpose(c, this, 0, 0, drawable.GetWidth(), drawable.GetHeight(), 0);
                }

                exposed = true;
            }

            if (dirty != null && isMapped && !inputOnly)
                screen.PostInvalidate(dirty.Left, dirty.Top, dirty.Right, dirty.Bottom);

            return changed;
        }

        /**
         * Process an X request relating to this window.
         *
         * @param client	The remote client.
         * @param opcode	The request's opcode.
         * @param arg		Optional first argument.
         * @param bytesRemaining	Bytes yet to be read in the request.

         */
        internal override void ProcessRequest(Client client, byte opcode, byte arg, int bytesRemaining)
        {
            var redraw = false;
            var updatePointer = false;
            var io = client.GetInputOutput();

            switch (opcode)
            {
                case RequestCode.ChangeWindowAttributes:
                    redraw = ProcessWindowAttributes(client, RequestCode.ChangeWindowAttributes, bytesRemaining);
                    updatePointer = true;
                    break;
                case RequestCode.GetWindowAttributes:
                    if (bytesRemaining != 0)
                    {
                        io.ReadSkip(bytesRemaining);
                        ErrorCode.Write(client, ErrorCode.Length, opcode, 0);
                    }
                    else
                    {
                        var vid = XServer.GetRootVisual().GetId();
                        var mapState = (byte) (isMapped ? 0 : 2);

                        lock (io)
                        {
                            Util.WriteReplyHeader(client, (byte) 2);
                            io.WriteInt(3); // Reply length.
                            io.WriteInt(vid); // Visual.
                            io.WriteShort((short) (inputOnly ? 2 : 1)); // Class.
                            io.WriteByte((byte) attributes[BitGravity]);
                            io.WriteByte((byte) attributes[WinGravity]);
                            io.WriteInt(attributes[BackingPlanes]);
                            io.WriteInt(attributes[BackingPixel]);
                            io.WriteByte((byte) attributes[SaveUnder]);
                            io.WriteByte((byte) 1); // Map is installed.
                            io.WriteByte(mapState); // Map-state.
                            io.WriteByte((byte) (overrideRedirect ? 1 : 0));
                            io.WriteInt(colormap.GetId()); // Colormap.
                            io.WriteInt(attributes[EventMask]);
                            io.WriteInt(attributes[EventMask]);
                            io.WriteShort((short) attributes[DoNotPropagateMask]);
                            io.WritePadBytes(2); // Unused.
                        }

                        io.Flush();
                    }

                    break;
                case RequestCode.DestroyWindow:
                    if (bytesRemaining != 0)
                    {
                        io.ReadSkip(bytesRemaining);
                        ErrorCode.Write(client, ErrorCode.Length, opcode, 0);
                    }
                    else
                    {
                        Destroy(true);
                        redraw = true;
                        updatePointer = true;
                    }

                    break;
                case RequestCode.DestroySubwindows:
                    if (bytesRemaining != 0)
                    {
                        io.ReadSkip(bytesRemaining);
                        ErrorCode.Write(client, ErrorCode.Length, opcode, 0);
                    }
                    else
                    {
                        foreach (var w in children)
                            w.Destroy(false);
                        Util.Clear(children);
                        redraw = true;
                        updatePointer = true;
                    }

                    break;
                case RequestCode.ChangeSaveSet:
                    if (bytesRemaining != 0)
                    {
                        io.ReadSkip(bytesRemaining);
                        ErrorCode.Write(client, ErrorCode.Length, opcode, 0);
                    }
                    else
                    {
                        // Do nothing.
                    }

                    break;
                case RequestCode.ReparentWindow:
                    if (bytesRemaining != 8)
                    {
                        io.ReadSkip(bytesRemaining);
                        ErrorCode.Write(client, ErrorCode.Length, opcode, 0);
                    }
                    else
                    {
                        var id = io.ReadInt(); // Parent.
                        int x = (short) io.ReadShort(); // X.
                        int y = (short) io.ReadShort(); // Y.
                        var r = XServer.GetResource(id);

                        if (r == null || r.GetRessourceType() != Resource.AttrWindow)
                        {
                            ErrorCode.Write(client, ErrorCode.Window, opcode, id);
                        }
                        else
                        {
                            Reparent(client, (Window) r, x, y);
                            redraw = true;
                            updatePointer = true;
                        }
                    }

                    break;
                case RequestCode.MapWindow:
                    if (bytesRemaining != 0)
                    {
                        io.ReadSkip(bytesRemaining);
                        ErrorCode.Write(client, ErrorCode.Length, opcode, 0);
                    }
                    else
                    {
                        Map(client);
                        redraw = true;
                        updatePointer = true;
                    }

                    break;
                case RequestCode.MapSubwindows:
                    if (bytesRemaining != 0)
                    {
                        io.ReadSkip(bytesRemaining);
                        ErrorCode.Write(client, ErrorCode.Length, opcode, 0);
                    }
                    else
                    {
                        MapSubwindows(client);
                        redraw = true;
                        updatePointer = true;
                    }

                    break;
                case RequestCode.UnmapWindow:
                    if (bytesRemaining != 0)
                    {
                        io.ReadSkip(bytesRemaining);
                        ErrorCode.Write(client, ErrorCode.Length, opcode, 0);
                    }
                    else
                    {
                        Unmap();
                        redraw = true;
                        updatePointer = true;
                    }

                    break;
                case RequestCode.UnmapSubwindows:
                    if (bytesRemaining != 0)
                    {
                        io.ReadSkip(bytesRemaining);
                        ErrorCode.Write(client, ErrorCode.Length, opcode, 0);
                    }
                    else
                    {
                        UnmapSubwindows();
                        redraw = true;
                        updatePointer = true;
                    }

                    break;
                case RequestCode.ConfigureWindow:
                    redraw = ProcessConfigureWindow(client, bytesRemaining);
                    updatePointer = true;
                    break;
                case RequestCode.CirculateWindow:
                    if (bytesRemaining != 0)
                    {
                        io.ReadSkip(bytesRemaining);
                        ErrorCode.Write(client, ErrorCode.Length, opcode, 0);
                    }
                    else
                    {
                        redraw = Circulate(client, arg);
                        updatePointer = true;
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
                        var rid = screen.GetRootWindow().GetId();
                        var depth = XServer.GetRootVisual().GetDepth();
                        int x, y;
                        var width = irect.Right - irect.Left;
                        var height = irect.Bottom - irect.Top;

                        if (parent == null)
                        {
                            x = orect.Left;
                            y = orect.Top;
                        }
                        else
                        {
                            x = orect.Left - parent.irect.Left;
                            y = orect.Top - parent.irect.Top;
                        }

                        lock (io)
                        {
                            Util.WriteReplyHeader(client, depth);
                            io.WriteInt(0); // Reply length.
                            io.WriteInt(rid); // Root.
                            io.WriteShort((short) x); // X.
                            io.WriteShort((short) y); // Y.
                            io.WriteShort((short) width); // Width.
                            io.WriteShort((short) height); // Height.
                            io.WriteShort((short) borderWidth); // Border wid.
                            io.WritePadBytes(10); // Unused.
                        }

                        io.Flush();
                    }

                    break;
                case RequestCode.QueryTree:
                    if (bytesRemaining != 0)
                    {
                        io.ReadSkip(bytesRemaining);
                        ErrorCode.Write(client, ErrorCode.Length, opcode, 0);
                    }
                    else
                    {
                        var rid = screen.GetRootWindow().GetId();
                        var pid = (parent == null) ? 0 : parent.GetId();

                        lock (io)
                        {
                            Util.WriteReplyHeader(client, (byte) 0);
                            io.WriteInt(children.Size()); // Reply length.
                            io.WriteInt(rid); // Root.
                            io.WriteInt(pid); // Parent.
                            // Number of children.
                            io.WriteShort((short) children.Size());
                            io.WritePadBytes(14); // Unused.

                            foreach (var w in children)
                                io.WriteInt(w.GetId());
                        }

                        io.Flush();
                    }

                    break;
                case RequestCode.ChangeProperty:
                case RequestCode.GetProperty:
                case RequestCode.RotateProperties:
                    Property.ProcessRequest(XServer, client, arg, opcode, bytesRemaining, this, properties);
                    break;
                case RequestCode.DeleteProperty:
                    if (bytesRemaining != 4)
                    {
                        io.ReadSkip(bytesRemaining);
                        ErrorCode.Write(client, ErrorCode.Length, opcode, 0);
                    }
                    else
                    {
                        var id = io.ReadInt(); // Property.
                        var a = XServer.GetAtom(id);

                        if (a == null)
                        {
                            ErrorCode.Write(client, ErrorCode.Atom, opcode, id);
                        }
                        else if (Util.ContainsKey(properties, id))
                        {
                            var sc = GetSelectingClients(EventCode.MaskPropertyChange);

                            Util.Remove(properties, id);
                            if (sc != null)
                            {
                                foreach (var c in sc)
                                {
                                    try
                                    {
                                        EventCode.SendPropertyNotify(c, this, a, XServer.GetTimestamp(), 1);
                                    }
                                    catch (Exception)
                                    {
                                        RemoveSelectingClient(c);
                                    }
                                }
                            }
                        }
                    }

                    break;
                case RequestCode.ListProperties:
                    if (bytesRemaining != 0)
                    {
                        io.ReadSkip(bytesRemaining);
                        ErrorCode.Write(client, ErrorCode.Length, opcode, 0);
                    }
                    else
                    {
                        var n = properties.Size();

                        lock (io)
                        {
                            Util.WriteReplyHeader(client, (byte) 0);
                            io.WriteInt(n); // Reply length.
                            io.WriteShort((short) n); // Num atoms.
                            io.WritePadBytes(22); // Unused.

                            foreach (var p in properties.Values)
                                io.WriteInt(p.GetId());
                        }

                        io.Flush();
                    }

                    break;
                case RequestCode.QueryPointer:
                    if (bytesRemaining != 0)
                    {
                        io.ReadSkip(bytesRemaining);
                        ErrorCode.Write(client, ErrorCode.Length, opcode, 0);
                    }
                    else
                    {
                        var rid = screen.GetRootWindow().GetId();
                        var rx = screen.GetPointerX();
                        var ry = screen.GetPointerY();
                        var mask = screen.GetButtons();
                        var wx = rx - irect.Left;
                        var wy = ry - irect.Top;
                        var w = WindowAtPoint(rx, ry);
                        var cid = 0;

                        if (w.parent == this)
                            cid = w.GetId();

                        lock (io)
                        {
                            Util.WriteReplyHeader(client, (byte) 1);
                            io.WriteInt(0); // Reply length.
                            io.WriteInt(rid); // Root.
                            io.WriteInt(cid); // Child.
                            io.WriteShort((short) rx); // Root X.
                            io.WriteShort((short) ry); // Root Y.
                            io.WriteShort((short) wx); // Win X.
                            io.WriteShort((short) wy); // Win Y.
                            io.WriteShort((short) mask); // Mask.
                            io.WritePadBytes(6); // Unused.
                        }

                        io.Flush();
                    }

                    break;
                case RequestCode.GetMotionEvents:
                    if (bytesRemaining != 8)
                    {
                        io.ReadSkip(bytesRemaining);
                        ErrorCode.Write(client, ErrorCode.Length, opcode, 0);
                    }
                    else
                    {
                        var numEvents = 0; // Do nothing.

                        io.ReadInt(); // Start time.
                        io.ReadInt(); // Stop time.

                        lock (io)
                        {
                            Util.WriteReplyHeader(client, (byte) 0);
                            io.WriteInt(numEvents * 2); // Reply length.
                            io.WriteInt(numEvents); // Number of events.
                            io.WritePadBytes(20); // Unused.
                        }

                        io.Flush();
                    }

                    break;
                case RequestCode.TranslateCoordinates:
                    if (bytesRemaining != 8)
                    {
                        io.ReadSkip(bytesRemaining);
                        ErrorCode.Write(client, ErrorCode.Length, opcode, 0);
                    }
                    else
                    {
                        var id = io.ReadInt(); // Destination window.
                        int x = (short) io.ReadShort(); // Source X.
                        int y = (short) io.ReadShort(); // Source Y.
                        var r = XServer.GetResource(id);

                        if (r == null || r.GetRessourceType() != Resource.AttrWindow)
                        {
                            ErrorCode.Write(client, ErrorCode.Window, opcode, id);
                        }
                        else
                        {
                            var w = (Window) r;
                            var dx = irect.Left + x - w.irect.Left;
                            var dy = irect.Top + y - w.irect.Top;
                            var child = 0;

                            foreach (var c in w.children)
                                if (c.isMapped && c.irect.Contains(x, y))
                                    child = c.Id;

                            lock (io)
                            {
                                Util.WriteReplyHeader(client, (byte) 1);
                                io.WriteInt(0); // Reply length.
                                io.WriteInt(child); // Child.
                                io.WriteShort((short) dx); // Dest X.
                                io.WriteShort((short) dy); // Dest Y.
                                io.WritePadBytes(16); // Unused.
                            }

                            io.Flush();
                        }
                    }

                    break;
                case RequestCode.ClearArea:
                    if (bytesRemaining != 8)
                    {
                        io.ReadSkip(bytesRemaining);
                        ErrorCode.Write(client, ErrorCode.Length, opcode, 0);
                    }
                    else
                    {
                        int x = (short) io.ReadShort(); // Source X.
                        int y = (short) io.ReadShort(); // Source Y.
                        var width = io.ReadShort(); // Width.
                        var height = io.ReadShort(); // Height.

                        if (width == 0)
                            width = drawable.GetWidth() - x;
                        if (height == 0)
                            height = drawable.GetHeight() - y;
                        drawable.ClearArea(x, y, width, height);
                        Invalidate(x, y, width, height);

                        if (arg == 1)
                        {
                            List<Client> sc;

                            sc = GetSelectingClients(EventCode.MaskExposure);
                            if (sc != null)
                                foreach (var c in sc)
                                    EventCode.SendExpose(c, this, x, y, width, height, 0);
                        }
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
                    redraw = drawable.ProcessRequest(XServer, client, Id, opcode, arg, bytesRemaining);
                    break;
                case RequestCode.ListInstalledColormaps:
                    if (bytesRemaining != 0)
                    {
                        io.ReadSkip(bytesRemaining);
                        ErrorCode.Write(client, ErrorCode.Length, opcode, 0);
                    }
                    else
                    {
                        screen.WriteInstalledColormaps(client);
                    }

                    break;
                default:
                    io.ReadSkip(bytesRemaining);
                    ErrorCode.Write(client, ErrorCode.Implementation, opcode, 0);
                    break;
            }

            if (redraw)
            {
                Invalidate();
                if (updatePointer)
                    screen.UpdatePointer(0);
            }
        }

        /**
         * Calculate a window's visibility.
         *
         * @return	The window's visibility.
         */
        private int CalculateVisibility()
        {
            if (inputOnly)
                return NotViewable;

            var result = Unobscured;

            for (var aw = this; aw.parent != null; aw = aw.parent)
            {
                if (!isMapped)
                    return NotViewable; // All ancestors must be mapped.

                if (result == FullyObscured)
                    continue; // Keep checking in case ancestor is unmapped.

                var above = false;

                foreach (var w in aw.parent.children)
                {
                    if (!w.isMapped)
                        continue;

                    if (above)
                    {
                        if (Rect.Intersects(w.orect, orect))
                        {
                            if (w.orect.Contains(orect))
                            {
                                result = FullyObscured;
                                break;
                            }

                            result = PartiallyObscured;
                        }
                    }
                    else if (w == aw)
                    {
                        above = true;
                    }
                }
            }

            return result;
        }

        /**
         * Update the visibility of this window and its children.
         */
        private void UpdateVisibility()
        {
            var sc = GetSelectingClients(EventCode.MaskVisibilityChange);

            if (sc != null)
            {
                var visibility = CalculateVisibility();

                if (visibility != this.visibility)
                {
                    this.visibility = visibility;
                    if (visibility != NotViewable)
                    {
                        foreach (var c in sc)
                        {
                            try
                            {
                                EventCode.SendVisibilityNotify(c, this, visibility);
                            }
                            catch (Exception)
                            {
                                RemoveSelectingClient(c);
                            }
                        }
                    }
                }
            }

            foreach (var w in children)
                w.UpdateVisibility();
        }

        /**
         * Update the visibility of all the windows that might have been
         * affected by changes to this window.
         */
        private void UpdateAffectedVisibility()
        {
            if (parent == null)
            {
                UpdateVisibility();
                return;
            }

            foreach (var w in parent.children)
            {
                w.UpdateVisibility();
                if (w == this)
                    break;
            }
        }
    }
}