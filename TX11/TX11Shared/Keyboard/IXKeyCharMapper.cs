namespace TX11Shared.Keyboard
{
    public interface IXKeyCharMapper
    {
        byte KeycodeShiftLeft { get; }

        byte KeycodeShiftRight { get; }

        byte KeycodeAltLeft { get; }

        byte KeycodeAltRight { get; }

        byte KeycodeCtrlLeft { get; }

        byte KeycodeCtrlRight { get; }

        int DeleteKeyCode { get; }

        int GetMappedChar(XKeyEvent keyEvent);
    }
}