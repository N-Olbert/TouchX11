using TX11Frontend.iOS.PlatformSpecific;
using TX11Shared.Keyboard;
using Xamarin.Forms;

[assembly: Dependency(typeof(IOSKeyCharMapper))]
namespace TX11Frontend.iOS.PlatformSpecific
{
    public class IOSKeyCharMapper : IXKeyCharMapper
    {
        //Randomly choosen numbers below 32 (ctrl keys)

        public byte KeycodeShiftLeft => 14;

        public byte KeycodeShiftRight => 15;

        public byte KeycodeAltLeft => 1;

        public byte KeycodeAltRight => 2;

        public byte KeycodeCtrlLeft => 3;

        public byte KeycodeCtrlRight => 4;

        public int DeleteKeyCode => 8;

        public int GetMappedChar(XKeyEvent keyEvent)
        {
            return (char) keyEvent.KeyCode;
        }
    }
}