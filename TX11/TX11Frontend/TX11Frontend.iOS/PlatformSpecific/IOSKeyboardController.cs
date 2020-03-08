using TX11Frontend.iOS.PlatformSpecific;
using TX11Frontend.PlatformSpecific;
using UIKit;
using Xamarin.Forms;
using Xamarin.Forms.Internals;

[assembly: Dependency(typeof(IOSKeyboardController))]
namespace TX11Frontend.iOS.PlatformSpecific
{
    public class IOSKeyboardController : IXKeyboardController
    {
        public void ShowKeyboard()
        {
            IOSKeyboardHelperEditorRenderer.KeyboardHelperEditor?.BecomeFirstResponder();
        }

        public void HideKeyboard()
        {
            IOSKeyboardHelperEditorRenderer.KeyboardHelperEditor?.EndEditing(true);
        }
    }
}