using System.Windows;
using System.Windows.Input;
using SkiaSharp;
using SkiaSharp.Views.Forms;
using TX11Frontend.PlatformSpecific;
using TX11Frontend.WPF;
using TX11Shared;
using TX11Shared.Keyboard;
using Xamarin.Forms.Platform.WPF;

[assembly: ExportRenderer(typeof(XCanvasView), typeof(XCanvasViewRenderer))]

namespace TX11Frontend.WPF
{
    public class XCanvasViewRenderer : SKCanvasViewRenderer
    {
        protected override void OnElementChanged(ElementChangedEventArgs<SKCanvasView> e)
        {
            // clean up
            if (Control != null)
            {
                Control.MouseMove -= OnMoved;
                Control.MouseDown -= ControlOnMouseDown;
                Control.MouseUp -= ControlOnMouseUp;
                Control.Loaded -= ControlOnLoaded;
                Control.IsVisibleChanged -= OnControlOnIsVisibleChanged;
                Control.KeyDown -= ControlOnKeyDown;
                Control.KeyUp -= ControlOnKeyUp;
            }

            base.OnElementChanged(e);

            // set up
            if (Control != null)
            {
                Control.Focusable = true;
                Control.MouseMove += OnMoved;
                Control.MouseDown += ControlOnMouseDown;
                Control.MouseUp += ControlOnMouseUp;
                Control.Loaded += ControlOnLoaded;
                Control.IsVisibleChanged += OnControlOnIsVisibleChanged;
                Control.KeyDown +=ControlOnKeyDown;
                Control.KeyUp += ControlOnKeyUp;
            }
        }

        private void ControlOnKeyUp(object sender, KeyEventArgs e)
        {
            var isShift = e.Key == Key.LeftShift || e.Key == Key.RightShift;
            var isAlt = e.Key == Key.LeftAlt || e.Key == Key.RightAlt;
            var isCtrl = e.Key == Key.LeftCtrl || e.Key == Key.RightCtrl;
            if (Element is IXCanvasViewController touchController)
            {
                touchController.OnKeyUp(new XKeyEvent((char) e.Key, isShift || IsShiftPressed(),
                                                      isAlt || IsAltPressed(), isCtrl || IsCtrlPressed()));
            }
        }

        private void ControlOnKeyDown(object sender, KeyEventArgs e)
        {
            KeyConverter x = new KeyConverter();
            var isShift = e.Key == Key.LeftShift || e.Key == Key.RightShift;
            var isAlt = e.Key == Key.LeftAlt || e.Key == Key.RightAlt;
            var isCtrl = e.Key == Key.LeftCtrl || e.Key == Key.RightCtrl;
            if (Element is IXCanvasViewController touchController)
            {
                touchController.OnKeyDown(new XKeyEvent((char) e.Key, isShift || IsShiftPressed(),
                                                        isAlt || IsAltPressed(), isCtrl || IsCtrlPressed()));
            }
        }

        private void OnControlOnIsVisibleChanged(object sender, DependencyPropertyChangedEventArgs args)
        {
            Control.Focus();
        }

        private void ControlOnMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (Element is IXCanvasViewController touchController && e != null)
            {
                switch (e.ChangedButton)
                {
                    case MouseButton.Left:
                        touchController.OnKeyUp(new XKeyEvent(XKeyEvent.LeftClickUpFakeKeyCode, IsShiftPressed(), IsAltPressed(), IsCtrlPressed()));
                        break;
                    case MouseButton.Right:
                        touchController.OnKeyUp(new XKeyEvent(XKeyEvent.RightClickUpFakeKeyCode, IsShiftPressed(), IsAltPressed(), IsCtrlPressed()));
                        break;
                }
            }
        }

        private void ControlOnLoaded(object sender, RoutedEventArgs e)
        {
            if (Element is IXCanvasViewController touchController && e != null)
            {
                PresentationSource source = PresentationSource.FromVisual(Control);
                double dpiX, dpiY;
                if (source != null)
                {
                    dpiX = 96.0 * source.CompositionTarget.TransformToDevice.M11;
                    dpiY = 96.0 * source.CompositionTarget.TransformToDevice.M22;
                    touchController.SetDensity(dpiX);
                }
            }

            Control.Focus();
        }

        private void ControlOnMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (Element is IXCanvasViewController touchController && e != null)
            {
                switch (e.ChangedButton)
                {
                    case MouseButton.Left:
                        touchController.OnKeyDown(new XKeyEvent(XKeyEvent.LeftClickDownFakeKeyCode, IsShiftPressed(), IsAltPressed(), IsCtrlPressed()));
                        break;
                    case MouseButton.Right:
                        touchController.OnKeyDown(new XKeyEvent(XKeyEvent.RightClickDownFakeKeyCode, IsShiftPressed(), IsAltPressed(), IsCtrlPressed()));
                        break;
                }
            }
        }

        private void OnMoved(object sender, MouseEventArgs mouseEventArgs)
        {
            if (Element is IXCanvasViewController touchController)
            {
                var current = mouseEventArgs.GetPosition(Control);
                var point = new SKPoint((float) current.X, (float) current.Y);
                touchController.OnTouch(point);
            }
        }

        private bool IsShiftPressed() => Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift);

        private bool IsAltPressed() => Keyboard.IsKeyDown(Key.LeftAlt) || Keyboard.IsKeyDown(Key.RightAlt);

        private bool IsCtrlPressed() => Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl);
    }
}