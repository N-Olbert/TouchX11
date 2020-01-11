using System.Runtime.CompilerServices;

namespace TX11Business.Compatibility
{
    internal static class CompatibilityExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int AsInt(this uint u)
        {
            return unchecked((int) u);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int AsInt(this long l)
        {
            return unchecked((int) l);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static char AsChar(this int i)
        {
            return unchecked((char) i);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static byte AsByte(this int i)
        {
            return unchecked((byte) i);
        }
    }
}