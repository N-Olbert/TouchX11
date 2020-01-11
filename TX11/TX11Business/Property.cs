using System;
using System.Collections.Generic;
using TX11Business.Compatibility;
using TX11Business.UIDependent;

namespace TX11Business
{
    internal class Property
    {
        private readonly int id;
        private int type;
        private byte format;
        private byte[] data;

        /**
         * Constructor.
         *
         * @param id	The property's ID.
         * @param type	The ID of the property's type atom.
         * @param format	Data format = 8, 16, or 32.
         */
        internal Property(int id, int type, byte format)
        {
            this.id = id;
            this.type = type;
            this.format = format;
        }

        /**
         * Constructor.
         *
         * @param p	The property to copy.
         */
        private Property(Property p)
        {
            id = p.id;
            type = p.type;
            format = p.format;
            data = p.data;
        }

        /**
         * Return the property's atom ID.
         *
         * @return	The property's atom ID.
         */
        internal int GetId()
        {
            return id;
        }

        /**
         * Process an X request relating to properties.
         *
         * @param xServer	The X server.
         * @param client	The remote client.
         * @param arg	Optional first argument.
         * @param opcode	The request's opcode.
         * @param bytesRemaining	Bytes yet to be read in the request.
         * @param w	The window containing the properties.
         * @param properties	Hash table of the window's properties.

         */
        internal static void ProcessRequest(XServer xServer, Client client, byte arg, byte opcode, int bytesRemaining,
            Window w, Dictionary<int, Property> properties)
        {
            switch (opcode)
            {
                case RequestCode.ChangeProperty:
                    ProcessChangePropertyRequest(xServer, client, arg, bytesRemaining, w, properties);
                    break;
                case RequestCode.GetProperty:
                    ProcessGetPropertyRequest(xServer, client, arg == 1, bytesRemaining, w, properties);
                    break;
                case RequestCode.RotateProperties:
                    ProcessRotatePropertiesRequest(xServer, client, bytesRemaining, w, properties);
                    break;
                default:
                    var io = client.GetInputOutput();

                    io.ReadSkip(bytesRemaining);
                    ErrorCode.Write(client, ErrorCode.Implementation, opcode, 0);
                    break;
            }
        }

        /**
         * Process a ChangeProperty request.
         * Change the owner of the specified selection.
         *
         * @param xServer	The X server.
         * @param client	The remote client.
         * @param mode	0=Replace 1=Prepend 2=Append.
         * @param bytesRemaining	Bytes yet to be read in the request.
         * @param w	The window containing the properties.
         * @param properties	Hash table of the window's properties.

         */
        internal static void ProcessChangePropertyRequest(XServer xServer, Client client, byte mode, int bytesRemaining,
            Window w, Dictionary<int, Property> properties)
        {
            var io = client.GetInputOutput();

            if (bytesRemaining < 16)
            {
                io.ReadSkip(bytesRemaining);
                ErrorCode.Write(client, ErrorCode.Length, RequestCode.ChangeProperty, 0);
                return;
            }

            var pid = io.ReadInt(); // Property atom.
            var tid = io.ReadInt(); // Type atom.
            var format = (byte) io.ReadByte(); // Format.

            io.ReadSkip(3); // Unused.

            var length = io.ReadInt(); // Length of data.
            int n, pad;

            if (format == 8)
                n = length;
            else if (format == 16)
                n = length * 2;
            else
                n = length * 4;

            pad = -n & 3;

            bytesRemaining -= 16;
            if (bytesRemaining != n + pad)
            {
                io.ReadSkip(bytesRemaining);
                ErrorCode.Write(client, ErrorCode.Length, RequestCode.ChangeProperty, 0);
                return;
            }

            var data = new byte[n];

            io.ReadBytes(data, 0, n);
            io.ReadSkip(pad); // Unused.

            var property = xServer.GetAtom(pid);

            if (property == null)
            {
                ErrorCode.Write(client, ErrorCode.Atom, RequestCode.ChangeProperty, pid);
                return;
            }

            if (!xServer.AtomExists(tid))
            {
                ErrorCode.Write(client, ErrorCode.Atom, RequestCode.ChangeProperty, tid);
                return;
            }

            Property p;

            if (Util.ContainsKey(properties, pid))
            {
                p = properties.Get(pid);
            }
            else
            {
                p = new Property(pid, tid, format);
                properties.Put(pid, p);
            }

            if (mode == 0)
            {
                // Replace.
                p.type = tid;
                p.format = format;
                p.data = data;
            }
            else
            {
                if (tid != p.type || format != p.format)
                {
                    ErrorCode.Write(client, ErrorCode.Match, RequestCode.ChangeProperty, 0);
                    return;
                }

                if (p.data == null)
                {
                    p.data = data;
                }
                else
                {
                    byte[] d1, d2;

                    if (mode == 1)
                    {
                        // Prepend.
                        d1 = data;
                        d2 = p.data;
                    }
                    else
                    {
                        // Append.
                        d1 = p.data;
                        d2 = data;
                    }

                    p.data = new byte[d1.Length + d2.Length];
                    Array.Copy(d1, 0, p.data, 0, d1.Length);
                    Array.Copy(d2, 0, p.data, d1.Length, d2.Length);
                }
            }

            List<Client> sc;

            if ((sc = w.GetSelectingClients(EventCode.MaskPropertyChange)) != null)
            {
                foreach (var c in sc)
                    EventCode.SendPropertyNotify(c, w, property, xServer.GetTimestamp(), 0);
            }
        }

        /**
         * Process a GetProperty request.
         *
         * @param xServer	The X server.
         * @param client	The remote client.
         * @param delete	Delete flag.
         * @param bytesRemaining	Bytes yet to be read in the request.
         * @param w	The window containing the properties.
         * @param properties	Hash table of the window's properties.

         */
        internal static void ProcessGetPropertyRequest(XServer xServer, Client client, bool delete, int bytesRemaining,
            Window w, Dictionary<int, Property> properties)
        {
            var io = client.GetInputOutput();

            if (bytesRemaining != 16)
            {
                io.ReadSkip(bytesRemaining);
                ErrorCode.Write(client, ErrorCode.Length, RequestCode.GetProperty, 0);
                return;
            }

            var pid = io.ReadInt(); // Property.
            var tid = io.ReadInt(); // Type.
            var longOffset = io.ReadInt(); // Long offset.
            var longLength = io.ReadInt(); // Long length.
            var property = xServer.GetAtom(pid);

            if (property == null)
            {
                ErrorCode.Write(client, ErrorCode.Atom, RequestCode.GetProperty, pid);
                return;
            }
            else if (tid != 0 && !xServer.AtomExists(tid))
            {
                ErrorCode.Write(client, ErrorCode.Atom, RequestCode.GetProperty, tid);
                return;
            }

            byte format = 0;
            var bytesAfter = 0;
            byte[] value = null;
            var generateNotify = false;

            if (Util.ContainsKey(properties, pid))
            {
                var p = properties.Get(pid);

                tid = p.type;
                format = p.format;

                if (tid != 0 && tid != p.type)
                {
                    bytesAfter = (p.data == null) ? 0 : p.data.Length;
                }
                else
                {
                    int n, i, t, l;

                    n = (p.data == null) ? 0 : p.data.Length;
                    i = 4 * longOffset;
                    t = n - i;

                    if (longLength < 0 || longLength > 536870911)
                        longLength = 536870911; // Prevent overflow.

                    if (t < longLength * 4)
                        l = t;
                    else
                        l = longLength * 4;

                    bytesAfter = n - (i + l);

                    if (l < 0)
                    {
                        ErrorCode.Write(client, ErrorCode.Value, RequestCode.GetProperty, 0);
                        return;
                    }

                    if (l > 0)
                    {
                        value = new byte[l];
                        Array.Copy(p.data, i, value, 0, l);
                    }

                    if (delete && bytesAfter == 0)
                    {
                        Util.Remove(properties, pid);
                        generateNotify = true;
                    }
                }
            }
            else
            {
                tid = 0;
            }

            var length = (value == null) ? 0 : value.Length;
            var pad = -length & 3;
            int valueLength;

            if (format == 8)
                valueLength = length;
            else if (format == 16)
                valueLength = length / 2;
            else if (format == 32)
                valueLength = length / 4;
            else
                valueLength = 0;

            lock (io)
            {
                Util.WriteReplyHeader(client, format);
                io.WriteInt((length + pad) / 4); // Reply length.
                io.WriteInt(tid); // Type.
                io.WriteInt(bytesAfter); // Bytes after.
                io.WriteInt(valueLength); // Value length.
                io.WritePadBytes(12); // Unused.

                if (value != null)
                {
                    io.WriteBytes(value, 0, value.Length); // Value.
                    io.WritePadBytes(pad); // Unused.
                }
            }

            io.Flush();

            if (generateNotify)
            {
                List<Client> sc;

                if ((sc = w.GetSelectingClients(EventCode.MaskPropertyChange)) != null)
                {
                    foreach (var c in sc)
                        EventCode.SendPropertyNotify(c, w, property, xServer.GetTimestamp(), 1);
                }
            }
        }

        /**
         * Process a RotateProperties request.
         *
         * @param xServer	The X server.
         * @param client	The remote client.
         * @param bytesRemaining	Bytes yet to be read in the request.
         * @param w	The window containing the properties.
         * @param properties	Hash table of the window's properties.

         */
        internal static void ProcessRotatePropertiesRequest(XServer xServer, Client client, int bytesRemaining,
            Window w, Dictionary<int, Property> properties)
        {
            var io = client.GetInputOutput();

            if (bytesRemaining < 4)
            {
                io.ReadSkip(bytesRemaining);
                ErrorCode.Write(client, ErrorCode.Length, RequestCode.RotateProperties, 0);
                return;
            }

            var n = io.ReadShort(); // Num properties.
            var delta = io.ReadShort(); // Delta.

            bytesRemaining -= 4;
            if (bytesRemaining != n * 4)
            {
                io.ReadSkip(bytesRemaining);
                ErrorCode.Write(client, ErrorCode.Length, RequestCode.RotateProperties, 0);
                return;
            }

            if (n == 0 || (delta % n) == 0)
                return;

            var aids = new int[n];
            var props = new Property[n];
            var pcopy = new Property[n];

            for (var i = 0; i < n; i++)
                aids[i] = io.ReadInt();

            for (var i = 0; i < n; i++)
            {
                if (!xServer.AtomExists(aids[i]))
                {
                    ErrorCode.Write(client, ErrorCode.Atom, RequestCode.RotateProperties, aids[i]);
                    return;
                }
                else if (!Util.ContainsKey(properties, aids[i]))
                {
                    ErrorCode.Write(client, ErrorCode.Match, RequestCode.RotateProperties, aids[i]);
                    return;
                }
                else
                {
                    props[i] = properties.Get(aids[i]);
                    pcopy[i] = new Property(props[i]);
                }
            }

            for (var i = 0; i < n; i++)
            {
                var p = props[i];
                var pc = pcopy[(i + delta) % n];

                p.type = pc.type;
                p.format = pc.format;
                p.data = pc.data;
            }

            List<Client> sc;

            if ((sc = w.GetSelectingClients(EventCode.MaskPropertyChange)) != null)
            {
                for (var i = 0; i < n; i++)
                {
                    foreach (var c in sc)
                        EventCode.SendPropertyNotify(c, w, xServer.GetAtom(aids[i]), xServer.GetTimestamp(), 0);
                }
            }
        }
    }
}