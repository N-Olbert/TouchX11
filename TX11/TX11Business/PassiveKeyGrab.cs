using TX11Business.UIDependent;

namespace TX11Business
{
    internal class PassiveKeyGrab
    {
        private readonly Client grabClient;
        private readonly Window grabWindow;
        private readonly byte key;
        private readonly int modifiers;
        private readonly bool ownerEvents;
        private readonly bool pointerSynchronous;
        private readonly bool keyboardSynchronous;

        /**
         * Constructor.
         *
         * @param grabClient	The grabbing client.
         * @param grabWindow	The grab window.
         * @param key	The key being grabbed, or 0 for any.
         * @param modifiers	The modifier mask, or 0x8000 for any.
         * @param ownerEvents	Owner-events flag.
         * @param pointerSynchronous	Are pointer events synchronous?
         * @param keyboardSynchronous	Are keyboard events synchronous?
         */
        internal PassiveKeyGrab(Client grabClient, Window grabWindow, byte key, int modifiers, bool ownerEvents,
            bool pointerSynchronous, bool keyboardSynchronous)
        {
            this.grabClient = grabClient;
            this.grabWindow = grabWindow;
            this.key = key;
            this.modifiers = modifiers;
            this.ownerEvents = ownerEvents;
            this.pointerSynchronous = pointerSynchronous;
            this.keyboardSynchronous = keyboardSynchronous;
        }

        /**
         * Does the event trigger the passive grab?
         *
         * @param key	The key that was pressed.
         * @param modifiers	The current state of the modifiers.
         * @return	True if the event matches.
         */
        internal bool MatchesEvent(int key, int modifiers)
        {
            if (this.key != 0 && this.key != key)
                return false;

            if (this.modifiers != 0x8000 && this.modifiers != modifiers)
                return false;

            return true;
        }

        /**
         * Does this match the parameters of the grab?
         *
         * @param key	The key being grabbed, or 0 for any.
         * @param modifiers	The modifier mask, or 0x8000 for any.
         * @return	True if it matches the parameters.
         */
        internal bool MatchesGrab(int key, int modifiers)
        {
            if (key != 0 && this.key != 0 && key != this.key)
                return false;

            if (modifiers != 0x8000 && this.modifiers != 0x8000 && modifiers != this.modifiers)
                return false;

            return true;
        }

        /**
         * Return the key code.
         *
         * @return	The key code.
         */
        internal byte GetKey()
        {
            return key;
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
    }
}