using System;
using Android.Views;
using TX11Frontend.Droid.PlatformSpecific;
using TX11Shared.Keyboard;
using Xamarin.Forms;
// ReSharper disable BitwiseOperatorOnEnumWithoutFlags

[assembly: Dependency(typeof(AndroidKeyCharMapper))]

namespace TX11Frontend.Droid.PlatformSpecific
{
    class AndroidKeyCharMapper : IXKeyCharMapper
    {
        public byte KeycodeShiftLeft => (byte)Keycode.ShiftLeft;

        public byte KeycodeShiftRight => (byte)Keycode.ShiftRight;

        public byte KeycodeAltLeft => (byte)Keycode.AltLeft;

        public byte KeycodeAltRight => (byte)Keycode.AltRight;

        public byte KeycodeCtrlLeft => (byte)Keycode.CtrlLeft;

        public byte KeycodeCtrlRight => (byte)Keycode.CtrlRight;

        public int DeleteKeyCode => (byte)Keycode.Del;

        public int GetMappedChar(XKeyEvent keyEvent)
        {
            if (Enum.IsDefined(typeof(Keycode), keyEvent.KeyCode))
            {
                using (var mapper = KeyCharacterMap.Load((int) KeyboardType.BuiltInKeyboard))
                {
                    if (mapper != null)
                    {
                        MetaKeyStates metaState = MetaKeyStates.None;
                        if (keyEvent.IsAltPressed)
                        {
                            metaState |= MetaKeyStates.AltOn;
                        }

                        if (keyEvent.IsControlPressed)
                        {
                            metaState |= MetaKeyStates.CtrlOn;
                        }

                        if (keyEvent.IsShiftPressed)
                        {
                            metaState |= MetaKeyStates.ShiftOn;
                        }

                        return mapper.Get((Keycode) keyEvent.KeyCode, metaState);
                    }
                }
            }

            return 0;
        }
    }
}