using System.Collections.Generic;
using JetBrains.Annotations;
using TX11Business.UIDependent;

namespace TX11Business.BusinessObjects.Events
{
    internal abstract class PendingEvent
    {
        protected Window Window { get; }

        protected int X { get; }

        protected int Y { get; }

        protected Client GrabbingClient { get; }

        protected bool GrabOwnerEvents { get; }

        protected PendingEvent(Window w, int motionX, int motionY, Client grabKeyboardClient,
            bool grabKeyboardOwnerEvents)
        {
            Window = w;
            X = motionX;
            Y = motionY;
            GrabbingClient = grabKeyboardClient;
            GrabOwnerEvents = grabKeyboardOwnerEvents;
        }

        internal abstract void ExecuteEvent([NotNull]ScreenView view);

        internal static void FlushEvents<T>([NotNull]Queue<T> q, ScreenView screen) where T : PendingEvent
        {
            while (q.Count > 0)
            {
                q.Dequeue()?.ExecuteEvent(screen);
            }
        }
    }

    internal abstract class PendingPointerEvent : PendingEvent
    {
        protected PendingPointerEvent(Window w, int motionX, int motionY, Client grabKeyboardClient,
                                      bool grabKeyboardOwnerEvents) 
            : base(w, motionX, motionY, grabKeyboardClient, grabKeyboardOwnerEvents)
        {
        }
    }

    internal abstract class PendingKeyboardEvent : PendingEvent
    {
        protected PendingKeyboardEvent(Window w, int motionX, int motionY, Client grabKeyboardClient,
                                       bool grabKeyboardOwnerEvents) 
            : base(w, motionX, motionY, grabKeyboardClient, grabKeyboardOwnerEvents)
        {
        }
    }
}