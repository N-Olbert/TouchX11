using System.ComponentModel;
using System.Globalization;
using TX11Frontend.Models;
using TX11Frontend.PlatformSpecific;
using TX11Ressources.Localization;
using Xamarin.Forms;

namespace TX11Frontend.Views
{
    // Learn more about making custom code visible in the Xamarin.Forms previewer
    // by visiting https://aka.ms/xamarinforms-previewer
    [DesignTimeVisible(false)]
    public partial class SettingsPage : ContentPage
    {
        public SettingsPage()
        {
            InitializeComponent();
            this.ScaleLabel.Text = Strings.ScaleLabelText;
            var defaultValue = 1.00d;
            var provider = DependencyService.Get<IXScaleDefaultValueProvider>();
            if (provider != null)
            {
                defaultValue = provider.Scale;
            }

            this.ScaleEntry.Text = defaultValue.ToString(CultureInfo.CurrentCulture);
        }

        void ScaleEntry_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (double.TryParse(e.NewTextValue, out double result))
            {
                ScalingHelper.SetScalingFactor(result);
            }
        }
    }
}