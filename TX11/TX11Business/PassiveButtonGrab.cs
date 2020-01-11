using TX11Business.UIDependent;

namespace TX11Business
{
    internal class PassiveButtonGrab
    {
        private readonly Client grabClient;
        private readonly Window grabWindow;
        private readonly byte button;
        private readonly int modifiers;
        private readonly bool ownerEvents;
        private readonly int eventMask;
        private readonly bool pointerSynchronous;
        private readonly bool keyboardSynchronous;
        private readonly Window confineWindow;
        private readonly Cursor cursor;

        /**
         * Constructor.
         *
         * @param grabClient	The grabbing client.
         * @param grabWindow	The grab window.
         * @param button	The button being grabbed, or 0 for any.
         * @param modifiers	The modifier mask, or 0x8000 for any.
         * @param ownerEvents	Owner-events flag.
         * @param eventMask	Selected pointer events.
         * @param pointerSynchronous	Are pointer events synchronous?
         * @param keyboardSynchronous	Are keyboard events synchronous?
         * @param confineWindow	Confine the cursor to this window. Can be null.
         * @param cursor	The cursor to use during the grab. Can be null.
         */
        internal PassiveButtonGrab(Client grabClient, Window grabWindow, byte button, int modifiers, bool ownerEvents,
            int eventMask, bool pointerSynchronous, bool keyboardSynchronous, Window confineWindow, Cursor cursor)
        {
            this.grabClient = grabClient;
            this.grabWindow = grabWindow;
            this.button = button;
            this.modifiers = modifiers;
            this.ownerEvents = ownerEvents;
            this.eventMask = eventMask;
            this.pointerSynchronous = pointerSynchronous;
            this.keyboardSynchronous = keyboardSynchronous;
            this.confineWindow = confineWindow;
            this.cursor = cursor;
        }

        /**
         * Does the event trigger the passive grab?
         *
         * @param button	Currently-pressed buttons and modifiers.
         * @return	True if the event matches.
         */
        internal bool MatchesEvent(int buttons)
        {
            if (button != 0 && (buttons & 0xff00) != (0x80 << button))
                return false;

            if (modifiers != 0x8000 && (buttons & 0xff) != modifiers)
                return false;

            return true;
        }

        /**
         * Does this match the parameters of the grab?
         *
         * @param button	The button being grabbed, or 0 for any.
         * @param modifiers	The modifier mask, or 0x8000 for any.
         * @return	True if it matches the parameters.
         */
        internal bool MatchesGrab(int buttonId, int buttonModifiers)
        {
            if (buttonId != 0 && this.button != 0 && buttonId != this.button)
                return false;

            if (buttonModifiers != 0x8000 && this.modifiers != 0x8000 && buttonModifiers != this.modifiers)
                return false;

            return true;
        }

        /**
         * Return the button.
         *
         * @return	The button.
         */
        internal byte GetButton()
        {
            return button;
        }

        /**
         * Return the modifier mask.
         *
         * @return	The modifier mask.
         */
        internal int GetModifiers()
        {
            return modifiers;
        }

        /**
         * Return the grab client.
         *
         * @return	The grab client.
         */
        internal Client GetGrabClient()
        {
            return grabClient;
        }

        /**
         * Return the grab window.
         *
         * @return	The grab window.
         */
        internal Window GetGrabWindow()
        {
            return grabWindow;
        }

        /**
         * Return the owner-events flag.
         *
         * @return	The owner-events flag.
         */
        internal bool GetOwnerEvents()
        {
            return ownerEvents;
        }

        /**
         * Return the pointer events mask.
         *
         * @return	The pointer events mask.
         */
        internal int GetEventMask()
        {
            return eventMask;
        }

        /**
         * Return whether pointer events are synchronous.
         *
         * @return	Whether pointer events are synchronous.
         */
        internal bool GetPointerSynchronous()
        {
            return pointerSynchronous;
        }

        /**
         * Return whether pointer events are synchronous.
         *
         * @return	Whether pointer events are synchronous.
         */
        internal bool GetKeyboardSynchronous()
        {
            return keyboardSynchronous;
        }

        /**
         * Return the confine window.
         *
         * @return	The confine window.
         */
        internal Window GetConfineWindow()
        {
            return confineWindow;
        }

        /**
         * Return the cursor.
         *
         * @return	The cursor.
         */
        internal Cursor GetCursor()
        {
            return cursor;
        }
    }
}