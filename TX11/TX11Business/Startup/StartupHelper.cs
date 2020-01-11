using System.Threading;
using TX11Shared;

namespace TX11Business.Startup
{
    public class StartupHelper
    {
        public static void PrepareXServer()
        {
            var t = new Thread((() =>
            {
                var server = new XServer(6001, null);
                var rootScreen = server.GetScreen();
                XConnector.Register(typeof(IXScreenObserver), rootScreen);

                server.Start();
            }));

            t.Start();
        }
    }
}