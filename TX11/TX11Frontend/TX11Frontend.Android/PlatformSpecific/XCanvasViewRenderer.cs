﻿using Android.Content;
using Android.Views;
using SkiaSharp;
using SkiaSharp.Views.Forms;
using TX11Frontend.Droid;
using TX11Frontend.PlatformSpecific;
using TX11Shared.Keyboard;
using Xamarin.Forms;

[assembly: ExportRenderer(typeof(XCanvasView), typeof(XCanvasViewRenderer))]

namespace TX11Frontend.Droid
{
    public class XCanvasViewRenderer : SKCanvasViewRenderer
    {
        //ugly
        internal static XCanvasViewRenderer current;

        public XCanvasViewRenderer(Context context) : base(context)
        {
            current = this;
        }

        public override void OnWindowFocusChanged(bool hasWindowFocus)
        {
            if (Element is IXCanvasViewController touchController)
            {
                touchController.SetDensity((int) Android.App.Application.Context.Resources.DisplayMetrics.DensityDpi);
            }

            base.OnWindowFocusChanged(hasWindowFocus);
        }

        public override bool OnKeyDown(Keycode keyCode, KeyEvent e)
        {
            if (e != null && Element is IXCanvasViewController touchController)
            {
                touchController.OnKeyDown(new XKeyEvent((int) keyCode, e.IsShiftPressed, e.IsAltPressed, e.IsCtrlPressed));
            }

            return true;
        }

        public override bool OnKeyUp(Keycode keyCode, KeyEvent e)
        {
            if (e != null && Element is IXCanvasViewController touchController)
            {
                touchController.OnKeyUp(new XKeyEvent((int) keyCode, e.IsShiftPressed, e.IsAltPressed, e.IsCtrlPressed));
            }

            return true;
        }

        public override bool OnTouchEvent(MotionEvent e)
        {
            if (e != null && Element is IXCanvasViewController touchController)
            {
                var point = new SKPoint(e.GetX(), e.GetY());
                touchController.OnTouch(point);

                //Touch is click
                touchController.OnKeyDown(new XKeyEvent(XKeyEvent.LeftClickDownFakeKeyCode, false, false, false));
                touchController.OnKeyUp(new XKeyEvent(XKeyEvent.LeftClickDownFakeKeyCode, false, false, false));
            }

            return base.OnTouchEvent(e);
        }
    }
}