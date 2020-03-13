using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using SkiaSharp;
using SkiaSharp.Views.Forms;
using TX11Business.Startup;
using TX11Frontend.PlatformSpecific;
using TX11Shared;
using Xamarin.Forms;
using TX11Frontend.UIConnectorWrapper;
using TX11Frontend.ViewModels;
using TX11Ressources.Localization;
using TX11Shared.Graphics;
using TX11Shared.Keyboard;
using TX11Frontend.Models;

namespace TX11Frontend.Views
{
    // Learn more about making custom code visible in the Xamarin.Forms previewer
    // by visiting https://aka.ms/xamarinforms-previewer
    [DesignTimeVisible(false)]
    public partial class CanvasPage : ContentPage, IXScreen
    {
        [NotNull]
        private readonly CanvasViewModel viewModel;

        private System.Drawing.Size oldSize = System.Drawing.Size.Empty;
        private SKSize screenSize;

        /// <summary>
        /// The currently active scaling factor.
        /// Note: Scaling is constant during the life time of the server
        /// </summary>
        private float scaling;
        private int invalidationPending;
        private bool started;

        public CanvasPage()
        {
            InitializeComponent();
            BindingContext = viewModel = new CanvasViewModel();
            // ReSharper disable once PossibleNullReferenceException
            ShowKeyBoard.Text = Strings.ShowKeyboardTitle;


            //Last action: Register
            XConnector.Register(typeof(IXScreen), this);
            CanvasView.Focus();
        }

        void OnShowKeyboardClicked(object sender, EventArgs e)
        {
            DependencyService.Get<IXKeyboardController>()?.ShowKeyboard();
        }

        private void OnPaintSurface(object sender, SKPaintSurfaceEventArgs e)
        {
            if (started)
            {
                var lockObj = StartupHelper.InvalidationLockObject;
                if (lockObj != null)
                {
                    lock (lockObj)
                    {
                        Interlocked.Exchange(ref this.invalidationPending, 0);
                        var factory = XConnector.GetInstanceOf<IXBitmapFactory>();
                        using (var screenSizeBitmap = factory.CreateBitmap((int)screenSize.Width, (int)screenSize.Height))
                        {
                            var canvasFactory = XConnector.GetInstanceOf<IXCanvasFactory>();
                            using (var canvas = canvasFactory.CreateCanvas(screenSizeBitmap))
                            {
                                XConnector.Resolve<IXScreenObserver>()?.OnDraw(this, canvas);
                                var drawBitmap = ((XCanvas)canvas).Bitmap;

                                using (var scaledBitMap = drawBitmap.Resize(e.Info, SKFilterQuality.High))
                                {
                                    e.Surface.Canvas.DrawBitmap(scaledBitMap, 0, 0);
                                }
                            }
                        }
                    }
                }
            }
        }

        private void OnSizeChanged(object sender, EventArgs e)
        {
            var newSize = new System.Drawing.Size((int) this.Width, (int) this.Height);
            if (started)
            {
                XConnector.Resolve<IXScreenObserver>()?.OnSizeChanged(this, newSize, this.oldSize);
                this.oldSize = newSize;
            }
        }

        public double Dpi
        {
            get { return CanvasView.Density; }
        }

        public double RealWidth => screenSize.Width;

        public double RealHeight => screenSize.Height;

        public void Invalidate()
        {
            if (Interlocked.CompareExchange(ref invalidationPending, 1, 0) == 0)
            {
                if (Dispatcher.IsInvokeRequired)
                {
                    Dispatcher.BeginInvokeOnMainThread(() => CanvasView.InvalidateSurface());
                }
                else
                {
                    CanvasView.InvalidateSurface();
                }
            }
        }

        private void CanvasView_OnTouched(SKPoint point)
        {
            if (started)
            {
                var scaling = 1 / this.scaling;
                XConnector.Resolve<IXScreenObserver>()?.OnTouch(this, (int)(point.X * scaling), (int)(point.Y * scaling));
            }
        }

        private void CanvasView_OnKeyDown(XKeyEvent obj)
        {
            if (started)
            {
                XConnector.Resolve<IXScreenObserver>()?.OnKeyDown(this, obj);
            }
        }

        private void CanvasView_OnKeyUp(XKeyEvent obj)
        {
            if (started)
            {
                XConnector.Resolve<IXScreenObserver>()?.OnKeyUp(this, obj);
            }
        }

        private async void StartButton_OnClicked(object sender, EventArgs e)
        {
            if (!started)
            {
                StartButton.IsVisible = false;
                CanvasView.IsVisible = false;
                CanvasView.IsVisible = true;
                CanvasView.InvalidateSurface();
                await Task.Delay(100);

                var scale = (float)ScalingHelper.ScalingFactor;
                var width = CanvasView.CanvasSize.Width / scale;
                var height = CanvasView.CanvasSize.Height / scale;
                this.scaling = scale;
                screenSize = new SKSize(width, height);
                StartupHelper.PrepareXServer();
                await Task.Delay(100);
                started = true;

                CanvasView.Focus();
            }
        }

        private void VisualElement_OnFocused(object sender, FocusEventArgs e)
        {
            this.CanvasView?.Focus();
        }
    }
}