namespace TX11Business
{
    internal abstract class Resource
    {
        internal const int AttrWindow = 1;
        internal const int AttrPixmap = 2;
        internal const int AttrCursor = 3;
        internal const int AttrFont = 4;
        internal const int AttrGcontext = 5;
        internal const int AttrColormap = 6;

        private readonly int type;
        protected int Id;
        protected XServer XServer;
        protected Client Client;
        private int closeDownMode = Client.Destroy;

        /**
         * Constructor.
         *
         * @param type	The resource type.
         * @param id	The resource ID.
         * @param xServer	The X server.
         * @param client	The client issuing the request.
         */
        protected Resource(int type, int id, XServer xServer, Client client)
        {
            this.type = type;
            Id = id;
            XServer = xServer;
            Client = client;
        }

        /**
         * Return the resource type.
         * 
         * @return	The resource type.
         */
        internal int GetRessourceType()
        {
            return type;
        }

        /**
         * Return the resource ID.
         *
         * @return	The resource ID.
         */
        internal int GetId()
        {
            return Id;
        }

        /**
         * Return the client that created the resource.
         *
         * @return	The client that created the resource.
         */
        internal Client GetClient()
        {
            return Client;
        }

        /**
         * Return the resource's close down mode.
         *
         * @return	The resource's close down mode.
         */
        internal int GetCloseDownMode()
        {
            return closeDownMode;
        }

        /**
         * Set the close down mode of the resource.
         *
         * @param mode	The mode used to destroy the resource.
         */
        internal void SetCloseDownMode(int mode)
        {
            closeDownMode = mode;
        }

        /**
         * Is the resource a drawable? (Window or Pixmap)
         *
         * @return	Whether the resource is a drawable.
         */
        internal bool IsDrawable()
        {
            return (type == AttrWindow || type == AttrPixmap);
        }

        /**
         * Is the resource a fontable? (Font or GContext)
         *
         * @return Whether the resource is a fontable.
         */
        internal bool IsFontable()
        {
            return (type == AttrFont || type == AttrGcontext);
        }

        /**
         * Destroy the resource.
         * Remove it from the X server's resources, and override this function
         * to handle object-specific removals.
         */
        internal virtual void Delete()
        {
            XServer.FreeResource(Id);
        }

        /**
         * Process an X request relating to this resource.
         * This is a fallback function that does nothing.
         *
         * @param client	The remote client.
         * @param opcode	The request's opcode.
         * @param arg		Optional first argument.
         * @param bytesRemaining	Bytes yet to be read in the request.

         */
        internal abstract void ProcessRequest(Client client, byte opcode, byte arg, int bytesRemaining);
    }
}