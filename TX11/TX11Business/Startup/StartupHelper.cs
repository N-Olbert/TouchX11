using System.Threading;
using TX11Shared;

namespace TX11Business.Startup
{
    public class StartupHelper
    {
        private static XServer server;
        public static void PrepareXServer()
        {
            if (server == null)
            {
                var t = new Thread((() =>
                {
                    server = new XServer(6001, null);
                    var rootScreen = server.GetScreen();
                    XConnector.Register(typeof(IXScreenObserver), rootScreen);

                    server.Start();
                }));

                t.Start();
            }
        }

        /// <summary>
        /// Gets an object which should be locked during a screen invalidation.
        /// Acquiring a lock on this object stops the execution of client requests.
        /// </summary>
        public static object InvalidationLockObject => server;
    }
}