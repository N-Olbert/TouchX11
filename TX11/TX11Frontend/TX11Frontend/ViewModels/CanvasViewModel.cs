using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Xamarin.Forms;
using TX11Ressources.Localization;

namespace TX11Frontend.ViewModels
{
    public class CanvasViewModel : BaseViewModel
    {
        public Command LoadItemsCommand { get; set; }

        public CanvasViewModel()
        {
            Title = Strings.CanvasPageTitle;
            LoadItemsCommand = new Command(async () => await ExecuteLoadItemsCommand());
        }

        async Task ExecuteLoadItemsCommand()
        {
            if (IsBusy)
                return;

            IsBusy = true;

            try
            {
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}