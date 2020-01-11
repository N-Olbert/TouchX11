using Windows.UI.Xaml.Input;
using SkiaSharp;
using SkiaSharp.Views.Forms;
using TX11Frontend.PlatformSpecific;
using TX11Frontend.UWP;
using TX11Shared.Keyboard;
using Xamarin.Forms.Platform.UWP;

[assembly: ExportRenderer(typeof(XCanvasView), typeof(XCanvasViewRenderer))]

namespace TX11Frontend.UWP
{
    public class XCanvasViewRenderer : SKCanvasViewRenderer
    {
        protected override void OnElementChanged(ElementChangedEventArgs<SKCanvasView> e)
        {
            // clean up
            if (Control != null)
            {
                Control.PointerMoved -= OnMoved;
                Control.KeyDown -= ControlOnKeyDown;
                Control.KeyUp -= ControlOnKeyUp;
                Control.PointerPressed -= ControlOnMouseDown;
            }

            base.OnElementChanged(e);

            // set up
            if (Control != null)
            {
                Control.PointerMoved += OnMoved;
                Control.KeyDown += ControlOnKeyDown;
                Control.KeyUp += ControlOnKeyUp;
                Control.PointerPressed += ControlOnMouseDown;
                Control.AllowFocusWhenDisabled = Control.AllowFocusOnInteraction = true;
            }
        }

        private void ControlOnMouseDown(object sender, PointerRoutedEventArgs pointerRoutedEventArgs)
        {
            //throw new System.NotImplementedException();
        }

        private void ControlOnKeyUp(object sender, KeyRoutedEventArgs keyRoutedEventArgs)
        {
            if (Element is IXCanvasViewController touchController)
            {
                touchController.OnKeyUp(new XKeyEvent((int) keyRoutedEventArgs.Key));
            }
        }

        private void ControlOnKeyDown(object sender, KeyRoutedEventArgs keyRoutedEventArgs)
        {
            if (Element is IXCanvasViewController touchController)
            {
                touchController.OnKeyDown(new XKeyEvent((int) keyRoutedEventArgs.Key));
            }
        }

        private void OnMoved(object sender, PointerRoutedEventArgs pointerRoutedEventArgs)
        {
            if (Element is IXCanvasViewController touchController)
            {
                var current = pointerRoutedEventArgs.GetCurrentPoint(Control).Position;
                var point = new SKPoint((float) current.X, (float) current.Y);
                touchController.OnTouch(point);
            }
        }
    }
}