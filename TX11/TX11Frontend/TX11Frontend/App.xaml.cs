﻿using System;
using TX11Frontend.PlatformSpecific;
using Xamarin.Forms;
using TX11Frontend.UIConnectorWrapper;
using TX11Frontend.Views;
using TX11Shared;
using TX11Shared.Graphics;
using TX11Shared.Keyboard;

namespace TX11Frontend
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
            MainPage = new MainPage();
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
            XConnector.RegisterFactoryMethodFor<IXKeyCharMapper>(() => DependencyService.Get<IXKeyCharMapper>());
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