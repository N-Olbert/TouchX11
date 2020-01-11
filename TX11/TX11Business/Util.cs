using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using TX11Shared;
using TX11Shared.Graphics;

namespace TX11Business
{
    internal static class Util
    {
        [NotNull]
        internal static IXBitmapFactory BitmapFactory => GetBitmapFactory();

        [NotNull]
        internal static IXCanvasFactory CanvasFactory => GetCanvasFactory();

        [NotNull]
        internal static IXRegionFactory RegionFactory => GetRegionFactory();

        internal static int Bitcount(int n)
        {
            var c = 0;
            while ((n != 0))
            {
                c = (c + (n & 1));
                n >>= 1;
            }

            return c;
        }

        internal static void WriteReplyHeader(Client client, int arg)
        {
            var io = client.GetInputOutput();
            var sn = ((short) ((client.GetSequenceNumber() & 65535)));
            io.WriteByte(1);
            //  Reply.
            io.WriteByte(((byte) (arg)));
            io.WriteShort(sn);
        }

        internal static IXPaint GetPaint()
        {
            return XConnector.GetInstanceOf<IXPaint>();
        }

        internal static IXPath GetPath()
        {
            return XConnector.GetInstanceOf<IXPath>();
        }

        internal static IXCanvasFactory GetCanvasFactory()
        {
            return XConnector.GetInstanceOf<IXCanvasFactory>();
        }

        internal static IXRegionFactory GetRegionFactory()
        {
            return XConnector.GetInstanceOf<IXRegionFactory>();
        }

        internal static IXBitmapFactory GetBitmapFactory()
        {
            return XConnector.GetInstanceOf<IXBitmapFactory>();
        }

        internal static byte[] GetBytes(this string str)
        {
            return System.Text.Encoding.UTF8.GetBytes(str);
        }

        internal static string GetString(this byte[] bytes)
        {
            return System.Text.Encoding.UTF8.GetString(bytes);
        }

        internal static bool EqualsIgnoreCase(this string s1, string s2)
        {
            return string.Equals(s1, s2, StringComparison.OrdinalIgnoreCase);
        }

        internal static string[] Split(this string toSplit, string seperator)
        {
            return toSplit.Split(new[] {seperator}, StringSplitOptions.None);
        }

        internal static bool ContainsKey<T1, T2>(this Dictionary<T1, T2> dict, T1 key)
        {
            return dict.ContainsKey(key);
        }

        internal static T2 Get<T1, T2>(this Dictionary<T1, T2> dict, T1 key)
        {
            return dict[key];
        }

        internal static void Put<T1, T2>(this Dictionary<T1, T2> dict, T1 key, T2 value)
        {
            dict[key] = value;
        }

        internal static void Remove<T1, T2>(this Dictionary<T1, T2> dict, T1 key)
        {
            dict.Remove(key);
        }

        internal static void Add<T1>(this ICollection<T1> list, T1 value)
        {
            list.Add(value);
        }

        internal static void Add<T1>(this List<T1> list, int pos, T1 value)
        {
            list.Insert(pos, value);
        }

        internal static void Remove<T1>(this ICollection<T1> list, T1 value)
        {
            list.Remove(value);
        }

        internal static int Size<T1>(this IEnumerable<T1> list)
        {
            return list.Count();
        }

        internal static void Clear<T1>(this ICollection<T1> list)
        {
            list.Clear();
        }

        internal static int IndexOf<T1>(this IList<T1> list, T1 elem)
        {
            return list.IndexOf(elem);
        }

        internal static bool Contains<T1>(this IEnumerable<T1> list, T1 elem)
        {
            return Enumerable.Contains(list, elem);
        }

        internal static bool Empty<T1>(this Stack<T1> list)
        {
            return list.Count == 0;
        }

        internal static T1 ElementAt<T1>(this IEnumerable<T1> list, int pos)
        {
            return Enumerable.ElementAt(list, pos);
        }
    }
}