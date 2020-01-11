namespace TX11Frontend.UWP
{
    public sealed partial class MainPage
    {
        public MainPage()
        {
            this.InitializeComponent();

            var app = new TX11Frontend.App();
            LoadApplication(app);
        }
    }
}