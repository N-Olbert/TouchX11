using Android.App;
using Android.Content;
using Android.Views.InputMethods;
using TX11Frontend.Droid.PlatformSpecific;
using TX11Frontend.PlatformSpecific;
using Xamarin.Forms;

[assembly: Dependency(typeof(AndroidKeyboardController))]
namespace TX11Frontend.Droid.PlatformSpecific
{
    class AndroidKeyboardController : IXKeyboardController
    {
        public void ShowKeyboard()
        {
            var context = Android.App.Application.Context;
            var inputMethodManager = context?.GetSystemService(Context.InputMethodService) as InputMethodManager;

            inputMethodManager?.ToggleSoftInput(ShowFlags.Forced, HideSoftInputFlags.ImplicitOnly);
        }

        public void HideKeyboard()
        {
            var context = Android.App.Application.Context;
            var inputMethodManager = context?.GetSystemService(Context.InputMethodService) as InputMethodManager;

            if (inputMethodManager != null && context is Activity activity)
            {
                var token = activity.CurrentFocus?.WindowToken;
                inputMethodManager.HideSoftInputFromWindow(token, HideSoftInputFlags.None);

                activity.Window?.DecorView?.ClearFocus();
            }
        }
    }
}