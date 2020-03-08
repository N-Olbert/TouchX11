namespace TX11Shared.Keyboard
{
    public struct XKeyEvent
    {
        public const int LeftClickDownFakeKeyCode = int.MaxValue - 1;

        public const int LeftClickUpFakeKeyCode = int.MaxValue - 2;

        public const int RightClickDownFakeKeyCode = int.MaxValue - 3;

        public const int RightClickUpFakeKeyCode = int.MaxValue - 4;

        public const int ControlFakeKeyCode = int.MaxValue - 5;

        public int KeyCode { get; }

        public bool IsShiftPressed { get; }

        public bool IsAltPressed { get; }

        public bool IsControlPressed { get; }

        public bool IsLeftClick => KeyCode == LeftClickDownFakeKeyCode || KeyCode == LeftClickUpFakeKeyCode;

        public bool IsRightClick => KeyCode == RightClickDownFakeKeyCode || KeyCode == RightClickUpFakeKeyCode;

        public XKeyEvent(int keyCode, bool isShiftPressed, bool isAltPressed, bool isControlPressed)
        {
            KeyCode = keyCode;
            IsShiftPressed = isShiftPressed;
            IsAltPressed = isAltPressed;
            IsControlPressed = isControlPressed;
        }
    }
}