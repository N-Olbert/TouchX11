using TX11Business.UIDependent;

namespace TX11Business.BusinessObjects.Events
{
    internal class PendingGrabButtonNotifyEvent : PendingPointerEvent
    {
        private readonly int mButton;
        private readonly int mGrabEventMask;
        private readonly bool pressed;

        public PendingGrabButtonNotifyEvent(Window w, bool pressed, int motionX, int motionY, int button,
                                            int grabEventMask, Client grabPointerClient, bool grabPointerOwnerEvents)
        :base(w, motionX, motionY, grabPointerClient, grabPointerOwnerEvents)
        {
            this.mButton = button;
            this.mGrabEventMask = grabEventMask;
            this.pressed = pressed;
        }

        internal override void ExecuteEvent(ScreenView view)
        {
            view.CallGrabButtonNotify(Window, this.pressed, X, Y, this.mButton, this.mGrabEventMask, 
                                      GrabbingClient, GrabOwnerEvents);
        }
    }
}