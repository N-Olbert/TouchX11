using TX11Business.UIDependent;

namespace TX11Business.BusinessObjects.Events
{
    internal class PendingGrabKeyNotifyEvent : PendingKeyboardEvent
    {
        private readonly int keycode;
        private readonly bool pressed;

        public PendingGrabKeyNotifyEvent(Window w, bool pressed, int motionX, int motionY, int keycode,
                                         Client grabKeyboardClient, bool grabKeyboardOwnerEvents) 
            : base(w, motionX, motionY, grabKeyboardClient,grabKeyboardOwnerEvents)
        {
            this.pressed = pressed;
            this.keycode = keycode;
        }

        internal override void ExecuteEvent(ScreenView view)
        {
            view.CallGrabKeyNotify(Window, this.pressed, X, Y, this.keycode, GrabbingClient, GrabOwnerEvents);
        }
    }
}