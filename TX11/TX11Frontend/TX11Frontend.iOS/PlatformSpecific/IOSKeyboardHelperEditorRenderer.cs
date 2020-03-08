using System.Globalization;
using Foundation;
using TX11Frontend.iOS.PlatformSpecific;
using TX11Frontend.PlatformSpecific;
using TX11Shared.Keyboard;
using UIKit;
using Xamarin.Forms;
using Xamarin.Forms.Platform.iOS;

[assembly: ExportRenderer(typeof(Editor), typeof(IOSKeyboardHelperEditorRenderer))]
namespace TX11Frontend.iOS.PlatformSpecific
{
    public class IOSKeyboardHelperEditorRenderer : EditorRenderer
    {
        public static UITextView KeyboardHelperEditor { get; private set; }

        public IOSKeyboardHelperEditorRenderer()
        {
            var control = new UITextView();
            SetNativeControl(control);
            KeyboardHelperEditor = control;
            control.ShouldChangeText += ShouldChangeText;
            control.AutocorrectionType = UITextAutocorrectionType.No;
            control.AutocapitalizationType = UITextAutocapitalizationType.None;
        }

        protected override bool ShouldChangeText(UITextView textView, NSRange range, string text)
        {
            if (XCanvasViewRenderer.CanvasInstance is IXCanvasViewController touchController)
            {
                if (!string.IsNullOrEmpty(text))
                {
                    if (range.Location < text.Length)
                    {
                        foreach (var @char in text.Substring((int) range.Location))
                        {
                            var lower = @char.ToString(CultureInfo.CurrentUICulture)
                                             .ToLower(CultureInfo.CurrentUICulture);
                            var isShiftPressed = @char != lower[0];
                            touchController.OnKeyDown(new XKeyEvent(@char, isShiftPressed, false, false));
                            touchController.OnKeyUp(new XKeyEvent(@char, isShiftPressed, false, false));
                        }
                    }
                }
                else
                {
                    touchController.OnKeyDown(new XKeyEvent(new IOSKeyCharMapper().DeleteKeyCode, false, false, false));
                    touchController.OnKeyUp(new XKeyEvent(new IOSKeyCharMapper().DeleteKeyCode, false, false, false));
                }
            }

            return false; //handeled, but bug in iOS 13 https://forums.developer.apple.com/thread/124270
        }
    }
}