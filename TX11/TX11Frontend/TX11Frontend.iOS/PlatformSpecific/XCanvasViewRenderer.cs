using Foundation;
using SkiaSharp;
using SkiaSharp.Views.Forms;
using TX11Frontend.iOS.PlatformSpecific;
using TX11Frontend.PlatformSpecific;
using TX11Shared.Keyboard;
using UIKit;
using Xamarin.Forms;
using Xamarin.Forms.Platform.iOS;

[assembly: ExportRenderer(typeof(XCanvasView), typeof(XCanvasViewRenderer))]
namespace TX11Frontend.iOS.PlatformSpecific
{
    public class XCanvasViewRenderer : SKCanvasViewRenderer
    {
        public static SKCanvasView CanvasInstance { get; private set; }

        protected override void OnElementChanged(ElementChangedEventArgs<SKCanvasView> e)
        {
            CanvasInstance = e?.NewElement;
            if (e?.NewElement is IXCanvasViewController touchController)
            {
                //https://stackoverflow.com/questions/3860305/get-ppi-of-iphone-ipad-ipod-touch-at-runtime/7922666
                //Not entirely correct, but
                //a) nearly no X11 app uses this value
                //b) experience shows that bigger values arent really bad for usability (on iPad)
                touchController.SetDensity(UIScreen.MainScreen.Scale * 160);
            }

            base.OnElementChanged(e);
        }

        public override void TouchesMoved(NSSet touches, UIEvent evt)
        {
            HandleTouch(touches);
            base.TouchesMoved(touches, evt);
        }

        public override void TouchesEnded(NSSet touches, UIEvent evt)
        {
            HandleTouch(touches);
            if (Element is IXCanvasViewController touchController)
            {
                //Perform click
                touchController.OnKeyDown(new XKeyEvent(XKeyEvent.LeftClickDownFakeKeyCode, false, false, false));
                touchController.OnKeyUp(new XKeyEvent(XKeyEvent.LeftClickDownFakeKeyCode, false, false, false));
            }
        }

        public override void TouchesBegan(NSSet touches, UIEvent evt)
        {
            HandleTouch(touches);
        }

        private void HandleTouch(NSSet touches)
        {
            var touch = (UITouch) touches?.AnyObject;
            if (touch != null && UIScreen.MainScreen != null)
            {
                var point = touch.GetPreciseLocation((UIView) Self);
                if (Element is IXCanvasViewController touchController)
                {
                    var s = (float)UIScreen.MainScreen.NativeScale;
                    touchController.OnTouch(new SKPoint((float) point.X * s, (float) point.Y * s));
                }
            }
        }
    }
}