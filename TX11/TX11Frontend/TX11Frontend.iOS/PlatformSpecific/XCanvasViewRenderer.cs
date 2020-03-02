using CoreGraphics;
using Foundation;
using SkiaSharp;
using SkiaSharp.Views.Forms;
using TX11Frontend.iOS.PlatformSpecific;
using TX11Frontend.PlatformSpecific;
using UIKit;
using Xamarin.Forms;
using Xamarin.Forms.Platform.iOS;

[assembly: ExportRenderer(typeof(XCanvasView), typeof(XCanvasViewRenderer))]
namespace TX11Frontend.iOS.PlatformSpecific
{
    public class XCanvasViewRenderer : SKCanvasViewRenderer
    {
        public override void TouchesMoved(NSSet touches, UIEvent evt)
        {
            HandleTouch(touches);
            base.TouchesMoved(touches, evt);
        }

        public override void TouchesEnded(NSSet touches, UIEvent evt)
        {
            HandleTouch(touches);
            base.TouchesEnded(touches, evt);
        }

        public override void TouchesBegan(NSSet touches, UIEvent evt)
        {
            HandleTouch(touches);
            base.TouchesBegan(touches, evt);
        }

        private void HandleTouch(NSSet touches)
        {
            if (touches != null)
            {
                var touch = (UITouch) touches.AnyObject;
                if (touch != null)
                {
                    var point = touch.GetPreciseLocation((UIView) Self);
                    if (Element is IXCanvasViewController touchController)
                    {
                        touchController.OnTouch(new SKPoint((float) point.X * 2, (float) point.Y * 2));
                    }
                }
            }
        }
    }
}