using System;
using SkiaSharp;
using SkiaSharp.Views.Forms;
using TX11Shared.Keyboard;
using Xamarin.Forms;

namespace TX11Frontend.PlatformSpecific
{
    public sealed class XCanvasView : SKCanvasView, IXCanvasViewController
    {
        public double Density { get; private set; }

        public event Action<SKPoint> Touched;

        public event Action<SKPoint, SKTouchAction> PerformClick;

        public event Action<XKeyEvent> KeyDown;

        public event Action<XKeyEvent> KeyUp;

        public void OnTouch(SKPoint point)
        {
            Touched?.Invoke(point);
        }

        public void OnPerformClick(SKPoint point, SKTouchAction clickAction)
        {
            PerformClick?.Invoke(point, clickAction);
        }

        public void OnKeyDown(XKeyEvent e)
        {
            KeyDown?.Invoke(e);
        }

        public void OnKeyUp(XKeyEvent e)
        {
            KeyUp?.Invoke(e);
        }

        public void SetDensity(double density)
        {
            Density = density;
        }
    }

    public interface IXCanvasViewController : IViewController
    {
        void OnTouch(SKPoint point);

        void OnPerformClick(SKPoint point, SKTouchAction clickAction);

        void OnKeyDown(XKeyEvent e);

        void OnKeyUp(XKeyEvent e);

        void SetDensity(double density);
    }
}