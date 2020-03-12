using System.ComponentModel;
using System.Globalization;
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
            this.ScaleEntry.Text = 1.00.ToString(CultureInfo.CurrentCulture);
        }
    }
}