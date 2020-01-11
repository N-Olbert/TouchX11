namespace TX11Business.BusinessObjects.Events
{
    public enum AllowEventsMode : byte
    {
        AsyncPointer = 0,
        SyncPointer = 1,
        ReplayPointer = 2,
        AsyncKeyboard = 3,
        SyncKeyboard = 4,
        ReplayKeyboard = 5,
        AsyncBoth = 6,
        SyncBoth = 7,
    }
}