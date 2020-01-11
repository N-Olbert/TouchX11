using System;
using TX11Business.UIDependent;

namespace TX11Business
{
    internal class Selection
    {
        private readonly int id;
        private Client owner;
        private Window ownerWindow;
        private int lastChangeTime;

        /**
         * Constructor.
         *
         * @param id		The selection's ID.
         */
        internal Selection(int id)
        {
            this.id = id;
        }

        /**
         * Return the selection's atom ID.
         *
         * @return	The selection's atom ID.
         */
        internal int GetId()
        {
            return id;
        }

        /**
         * If the selection is owned by the client, clear it.
         * This occurs when a client disconnects.
         *
         * @param client	The disconnecting client.
         */
        internal void ClearClient(Client client)
        {
            if (owner == client)
            {
                owner = null;
                ownerWindow = null;
            }
        }

        /**
         * Process an X request relating to selections.
         *
         * @param xServer	The X server.
         * @param client	The client issuing the request.
         * @param opcode	The request's opcode.
         * @param bytesRemaining	Bytes yet to be read in the request.

         */
        internal static void ProcessRequest(XServer xServer, Client client, byte opcode, int bytesRemaining)
        {
            var io = client.GetInputOutput();

            switch (opcode)
            {
                case RequestCode.SetSelectionOwner:
                    ProcessSetSelectionOwnerRequest(xServer, client, bytesRemaining);
                    break;
                case RequestCode.GetSelectionOwner:
                    if (bytesRemaining != 4)
                    {
                        io.ReadSkip(bytesRemaining);
                        ErrorCode.Write(client, ErrorCode.Length, opcode, 0);
                    }
                    else
                    {
                        var aid = io.ReadInt(); // Selection atom.

                        if (!xServer.AtomExists(aid))
                        {
                            ErrorCode.Write(client, ErrorCode.Atom, RequestCode.SetSelectionOwner, aid);
                        }
                        else
                        {
                            var wid = 0;
                            var sel = xServer.GetSelection(aid);

                            if (sel != null && sel.ownerWindow != null)
                                wid = sel.ownerWindow.GetId();

                            lock (io)
                            {
                                Util.WriteReplyHeader(client, (byte) 0);
                                io.WriteInt(0); // Reply length.
                                io.WriteInt(wid); // Owner.
                                io.WritePadBytes(20); // Unused.
                            }

                            io.Flush();
                        }
                    }

                    break;
                case RequestCode.ConvertSelection:
                    ProcessConvertSelectionRequest(xServer, client, bytesRemaining);
                    break;
                default:
                    io.ReadSkip(bytesRemaining);
                    ErrorCode.Write(client, ErrorCode.Implementation, opcode, 0);
                    break;
            }
        }

        /**
         * Process a SetSelectionOwner request.
         * Change the owner of the specified selection.
         *
         * @param xServer	The X server.
         * @param client	The client issuing the request.
         * @param bytesRemaining	Bytes yet to be read in the request.

         */
        internal static void ProcessSetSelectionOwnerRequest(XServer xServer, Client client, int bytesRemaining)
        {
            var io = client.GetInputOutput();

            if (bytesRemaining != 12)
            {
                io.ReadSkip(bytesRemaining);
                ErrorCode.Write(client, ErrorCode.Length, RequestCode.SetSelectionOwner, 0);
                return;
            }

            var wid = io.ReadInt(); // Owner window.
            var aid = io.ReadInt(); // Selection atom.
            var time = io.ReadInt(); // Timestamp.
            Window w = null;

            if (wid != 0)
            {
                var r = xServer.GetResource(wid);

                if (r == null || r.GetRessourceType() != Resource.AttrWindow)
                {
                    ErrorCode.Write(client, ErrorCode.Window, RequestCode.SetSelectionOwner, wid);
                    return;
                }

                w = (Window) r;
            }

            var a = xServer.GetAtom(aid);

            if (a == null)
            {
                ErrorCode.Write(client, ErrorCode.Atom, RequestCode.SetSelectionOwner, aid);
                return;
            }

            var sel = xServer.GetSelection(aid);

            if (sel == null)
            {
                sel = new Selection(aid);
                xServer.AddSelection(sel);
            }

            var now = xServer.GetTimestamp();

            if (time != 0)
            {
                if (time < sel.lastChangeTime || time >= now)
                    return;
            }
            else
            {
                time = now;
            }

            sel.lastChangeTime = time;
            sel.ownerWindow = w;

            if (sel.owner != null && sel.owner != client)
                EventCode.SendSelectionClear(sel.owner, time, w, a);

            sel.owner = (w != null) ? client : null;
        }

        /**
         * Process a ConvertSelection request.
         *
         * @param xServer	The X server.
         * @param client	The client issuing the request.
         * @param bytesRemaining	Bytes yet to be read in the request.

         */
        internal static void ProcessConvertSelectionRequest(XServer xServer, Client client, int bytesRemaining)
        {
            var io = client.GetInputOutput();

            if (bytesRemaining != 20)
            {
                io.ReadSkip(bytesRemaining);
                ErrorCode.Write(client, ErrorCode.Length, RequestCode.ConvertSelection, 0);
                return;
            }

            var wid = io.ReadInt(); // Requestor.
            var sid = io.ReadInt(); // Selection.
            var tid = io.ReadInt(); // Target.
            var pid = io.ReadInt(); // Property.
            var time = io.ReadInt(); // Time.
            var r = xServer.GetResource(wid);
            Window w;
            Atom selectionAtom, targetAtom, propertyAtom;

            if (r == null || r.GetRessourceType() != Resource.AttrWindow)
            {
                ErrorCode.Write(client, ErrorCode.Window, RequestCode.ConvertSelection, wid);
                return;
            }
            else
            {
                w = (Window) r;
            }

            selectionAtom = xServer.GetAtom(sid);
            if (selectionAtom == null)
            {
                ErrorCode.Write(client, ErrorCode.Atom, RequestCode.ConvertSelection, sid);
                return;
            }

            targetAtom = xServer.GetAtom(tid);
            if (targetAtom == null)
            {
                ErrorCode.Write(client, ErrorCode.Atom, RequestCode.ConvertSelection, tid);
                return;
            }

            propertyAtom = null;
            if (pid != 0 && (propertyAtom = xServer.GetAtom(pid)) == null)
            {
                ErrorCode.Write(client, ErrorCode.Atom, RequestCode.ConvertSelection, pid);
                return;
            }

            Client owner = null;
            var sel = xServer.GetSelection(sid);

            if (sel != null)
                owner = sel.owner;

            if (owner != null)
            {
                try
                {
                    EventCode.SendSelectionRequest(owner, time, sel.ownerWindow, w, selectionAtom, targetAtom,
                                                   propertyAtom);
                }
                catch (Exception)
                {
                }
            }
            else
            {
                EventCode.SendSelectionNotify(client, time, w, selectionAtom, targetAtom, propertyAtom);
            }
        }
    }
}