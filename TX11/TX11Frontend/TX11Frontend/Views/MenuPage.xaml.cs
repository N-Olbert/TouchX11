using System;
using TX11Frontend.Models;
using System.Collections.Generic;
using System.ComponentModel;
using TX11Ressources.Localization;
using Xamarin.Forms;

namespace TX11Frontend.Views
{
    // Learn more about making custom code visible in the Xamarin.Forms previewer
    // by visiting https://aka.ms/xamarinforms-previewer
    [DesignTimeVisible(false)]
    public partial class MenuPage : ContentPage
    {
        internal MainPage RootPage => Application.Current.MainPage as MainPage;

        public MenuPage()
        {
            InitializeComponent();

            var menuItems = new List<HomeMenuItem>
            {
                new HomeMenuItem {Id = MenuItemType.Canvas, Title = Strings.CanvasPageTitle},
                new HomeMenuItem {Id = MenuItemType.About, Title = Strings.AboutPageTitle},
                new HomeMenuItem {Id = MenuItemType.Settings, Title = Strings.SettingsPageTitle}
            };

            ListViewMenu.ItemsSource = menuItems;
            ListViewMenu.SelectedItem = menuItems[0];
            ListViewMenu.ItemSelected += async (sender, e) =>
            {
                if (e.SelectedItem == null)
                    return;

                var id = (int) ((HomeMenuItem) e.SelectedItem).Id;
                await RootPage.NavigateFromMenu(id);
            };
        }
    }
}