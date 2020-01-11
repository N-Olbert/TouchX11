using System;
using TX11Frontend.PlatformSpecific;
using Xamarin.Forms;
using TX11Frontend.UIConnectorWrapper;
using TX11Frontend.Views;
using TX11Shared;
using TX11Shared.Graphics;

namespace TX11Frontend
{
    public partial class App : Application
    {
        /// <summary>
        /// Gets the app instance (singleton).
        /// </summary>
        public static App Instance { get; private set; }

        public IKeyboardController KeyboardController { get; set; }

        public App()
        {
            if (Instance != null)
            {
                throw new InvalidOperationException();
            }

            InitializeComponent();
            MainPage = new MainPage();
            Instance = this;
            PrepareStartup();
        }

        public static void PrepareStartup()
        {
            XConnector.RegisterFactoryMethodFor<IXPaint>(() => new XPaint());
            XConnector.RegisterFactoryMethodFor<IXPath>(() => new XPath());
            XConnector.RegisterFactoryMethodFor<IXBitmap>(() => new XBitmap());
            XConnector.RegisterFactoryMethodFor<IXBitmapFactory>(() => new XBitmapFactory());
            XConnector.RegisterFactoryMethodFor<IXCanvasFactory>(() => new XCanvasFactory());
            XConnector.RegisterFactoryMethodFor<IXRegionFactory>(() => new XRegionFactory());
        }

        protected override void OnStart()
        {
        }

        protected override void OnSleep()
        {
        }

        protected override void OnResume()
        {
        }
    }
}