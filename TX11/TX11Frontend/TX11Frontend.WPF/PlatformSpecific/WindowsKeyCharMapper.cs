using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Input;
using TX11Frontend.WPF.PlatformSpecific;
using TX11Shared.Keyboard;
using Xamarin.Forms;

[assembly:Dependency(typeof(WindowsKeyCharMapper))]
namespace TX11Frontend.WPF.PlatformSpecific
{
    /// <summary>
    /// Windows specific implementation of <see cref="IXKeyCharMapper"/>
    /// </summary>
    /// <seealso cref="TX11Shared.Keyboard.IXKeyCharMapper" />
    public class WindowsKeyCharMapper : IXKeyCharMapper
    {
        ////Constants obtained from https://docs.microsoft.com/en-us/windows/win32/inputdev/virtual-key-codes

        /// <summary>
        /// The virtual key code of the SHIFT key
        /// </summary>
        private const int ShiftVirtualKeyCode = 0x10;

        /// <summary>
        /// The virtual key code of the CTRL key
        /// </summary>
        private const int ControlVirtualKeyCode = 0x11;

        /// <summary>
        /// The virtual key code of the ALT key
        /// </summary>
        private const int AltVirtualKeyCode = 0x12;

        public byte KeycodeShiftLeft => (byte)Key.LeftShift;

        public byte KeycodeShiftRight => (byte)Key.RightShift;

        public byte KeycodeAltLeft => (byte)Key.LeftAlt;

        public byte KeycodeAltRight => (byte)Key.RightAlt;

        public byte KeycodeCtrlLeft => (byte)Key.LeftCtrl;

        public byte KeycodeCtrlRight => (byte)Key.RightCtrl;

        public int DeleteKeyCode => (byte)Key.Delete;

        public int GetMappedChar(XKeyEvent keyEvent)
        {
            if (!Enum.IsDefined(typeof(Key), keyEvent.KeyCode))
            {
                return '\0';
            }

            var keyboardState = new byte[256];
            if (keyEvent.IsShiftPressed)
            {
                keyboardState[ShiftVirtualKeyCode] = byte.MaxValue;
            }

            if (keyEvent.IsAltPressed)
            {
                keyboardState[AltVirtualKeyCode] = byte.MaxValue;
                keyboardState[ControlVirtualKeyCode] = byte.MaxValue; //ALT-GR
            }

            try
            {
                //based upon https://stackoverflow.com/a/5826175 (answer from George)
                var key = (Key) keyEvent.KeyCode;
                var virtualKey = KeyInterop.VirtualKeyFromKey(key);
                var scanCode = Native.MapVirtualKey((uint) virtualKey, 0);
                var stringBuilder = new StringBuilder(2);
                var result = Native.ToUnicode((uint) virtualKey, scanCode, keyboardState, stringBuilder,
                                              stringBuilder.Capacity, 0);
                return result == 1 && stringBuilder.Length > 0 ? stringBuilder[0] : '\0';
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return '\0';
            }
        }

        private static class Native
        {
            [DllImport("user32.dll")]
            public static extern int ToUnicode(uint wVirtKey, uint wScanCode, byte[] lpKeyState,
                                               [Out, MarshalAs(UnmanagedType.LPWStr, SizeParamIndex = 4)]
                                               StringBuilder pwszBuff, int cchBuff, uint wFlags);

            [DllImport("user32.dll")]
            public static extern uint MapVirtualKey(uint uCode, uint uMapType);
        }
    }
}