using TX11Frontend.iOS.PlatformSpecific;
using TX11Frontend.PlatformSpecific;
using UIKit;
using Xamarin.Forms;

[assembly: Dependency(typeof(IOSScaleDefaultValueProvider))]
namespace TX11Frontend.iOS.PlatformSpecific
{
    public class IOSScaleDefaultValueProvider : IXScaleDefaultValueProvider
    {
        public double Scale => (double)UIScreen.MainScreen.NativeScale;
    }
}
