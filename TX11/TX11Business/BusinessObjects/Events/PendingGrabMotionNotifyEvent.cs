using TX11Business.UIDependent;

namespace TX11Business.BusinessObjects.Events
{
    internal class PendingGrabMotionNotifyEvent : PendingPointerEvent
    {
        private readonly int buttons;
        private readonly int grabEventMask;

        public PendingGrabMotionNotifyEvent(Window w, int x, int y, int buttons, int grabEventMask, 
                                            Client grabPointerClient, bool grabPointerOwnerEvents)
            : base(w, x, y, grabPointerClient, grabPointerOwnerEvents)
        {
            this.buttons = buttons;
            this.grabEventMask = grabEventMask;
        }

        internal override void ExecuteEvent(ScreenView view)
        {
            view.CallGrabMotionNotify(Window, X, Y, this.buttons, this.grabEventMask, GrabbingClient,
                                      GrabOwnerEvents);
        }
    }
}