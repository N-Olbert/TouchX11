using System.Windows;
using System.Windows.Input;
using SkiaSharp;
using SkiaSharp.Views.Forms;
using TX11Frontend.PlatformSpecific;
using TX11Frontend.WPF;
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
                Control.PreviewTextInput -= ControlOnTextInput;
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
                Control.PreviewTextInput += ControlOnTextInput;
            }
        }

        private void OnControlOnIsVisibleChanged(object sender, DependencyPropertyChangedEventArgs args)
        {
            Control.Focus();
        }

        private void ControlOnTextInput(object sender, TextCompositionEventArgs e)
        {
            var text = (!string.IsNullOrEmpty(e.Text) ? e.Text : null) ??
                       (!string.IsNullOrEmpty(e.ControlText) ? e.ControlText : null) ?? e.SystemText;
            if (Element is IXCanvasViewController touchController && !string.IsNullOrEmpty(text) && text.Length == 1)
            {
                var keyCode = text[0];
                touchController.OnKeyDown(new XKeyEvent(keyCode));
                touchController.OnKeyUp(new XKeyEvent(keyCode));
            }

            e.Handled = true;
        }

        private void ControlOnMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (Element is IXCanvasViewController touchController && e != null)
            {
                switch (e.ChangedButton)
                {
                    case MouseButton.Left:
                        touchController.OnKeyUp(new XKeyEvent(XKeyEvent.AttrKeycodeLeftClickUp));
                        break;
                    case MouseButton.Right:
                        touchController.OnKeyUp(new XKeyEvent(XKeyEvent.AttrKeycodeRightClickUp));
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
                        touchController.OnKeyDown(new XKeyEvent(XKeyEvent.AttrKeycodeLeftClickDown));
                        break;
                    case MouseButton.Right:
                        touchController.OnKeyDown(new XKeyEvent(XKeyEvent.AttrKeycodeRightClickDown));
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
    }
}