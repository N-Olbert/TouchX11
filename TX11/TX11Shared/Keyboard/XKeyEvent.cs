using System;

namespace TX11Shared.Keyboard
{
    public class XKeyEvent : EventArgs
    {
        public int KeyCode { get; }

        public XKeyEvent(int keyCode)
        {
            KeyCode = keyCode;
        }

        //Copied form Android source: https://cs.android.com/android/platform/superproject/+/master:frameworks/base/core/java/android/view/KeyEvent.java;l=86?q=KeyEvent&sq=

        /** Key code constant: Back key. */
        public const int AttrKeycodeBack = 4;

        /** Key code constant: Directional Pad Up key.
         * May also be synthesized from trackball motions. */
        public const int AttrKeycodeDpadUp = 19;

        /** Key code constant: Directional Pad Down key.
         * May also be synthesized from trackball motions. */
        public const int AttrKeycodeDpadDown = 20;

        /** Key code constant: Directional Pad Left key.
         * May also be synthesized from trackball motions. */
        public const int AttrKeycodeDpadLeft = 21;

        /** Key code constant: Directional Pad Right key.
         * May also be synthesized from trackball motions. */
        public const int AttrKeycodeDpadRight = 22;

        /** Key code constant: Directional Pad Center key.
         * May also be synthesized from trackball motions. */
        public const int AttrKeycodeDpadCenter = 23;

        /** Key code constant: Volume Up key.
         * Adjusts the speaker volume up. */
        public const int AttrKeycodeVolumeUp = 24;

        /** Key code constant: Volume Down key.
         * Adjusts the speaker volume down. */
        public const int AttrKeycodeVolumeDown = 25;

        /** Key code constant: Left Alt modifier key. */
        public const int AttrKeycodeAltLeft = 57;

        /** Key code constant: Right Alt modifier key. */
        public const int AttrKeycodeAltRight = 58;

        /** Key code constant: Left Shift modifier key. */
        public const int AttrKeycodeShiftLeft = 59;

        /** Key code constant: Right Shift modifier key. */
        public const int AttrKeycodeShiftRight = 60;

        /** Key code constant: Backspace key.
         * Deletes characters before the insertion point, unlike {@link #KEYCODE_FORWARD_DEL}. */
        public const int AttrKeycodeDel = 67;

        /** Key code constant: Menu key. */
        public const int AttrKeycodeMenu = 82;
        public const int AttrKeycodeLeftClickDown = int.MaxValue - 1;

        public const int AttrKeycodeLeftClickUp = int.MaxValue - 2;

        public const int AttrKeycodeRightClickDown = int.MaxValue - 3;

        public const int AttrKeycodeRightClickUp = int.MaxValue - 4;

        public int GetMetaState()
        {
            return 0;
            //throw new NotImplementedException();
        }
    }
}