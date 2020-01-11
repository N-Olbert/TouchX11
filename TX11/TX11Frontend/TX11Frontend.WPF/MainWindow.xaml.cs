using System;
using System.Diagnostics;
using Xamarin.Forms;
using Xamarin.Forms.Platform.WPF;

namespace TX11Frontend.WPF
{
    /// <summary>
    /// Interaktionslogik für MainWindow.xaml
    /// </summary>
    public partial class MainWindow : FormsApplicationPage
    {
        public MainWindow()
        {
            InitializeComponent();

            Forms.Init();
            LoadApplication(new TX11Frontend.App());
        }

        #region Overrides of Window

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);

            if (Debugger.IsAttached)
            {
                //Hotfix for Visual Studio 2019 which wont let the process exit (for whatever reason)
                Process.GetCurrentProcess().Kill();
            }
        }

        #endregion
    }
}