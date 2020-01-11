using Android.App;
using Android.Content.PM;
using Android.Runtime;
using Android.Views;
using Android.OS;

namespace TX11Frontend.Droid
{
    [Activity(Label = "TX11Frontend", Icon = "@mipmap/icon", Theme = "@style/MainTheme", MainLauncher = true,
        ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation)]
    public class MainActivity : global::Xamarin.Forms.Platform.Android.FormsAppCompatActivity
    {
        public override bool OnKeyDown(Keycode keyCode, KeyEvent e)
        {
            return XCanvasViewRenderer.current != null
                ? XCanvasViewRenderer.current.OnKeyDown(keyCode, e)
                : base.OnKeyDown(keyCode, e);
        }

        public override bool OnKeyUp(Keycode keyCode, KeyEvent e)
        {
            return XCanvasViewRenderer.current != null
                ? XCanvasViewRenderer.current.OnKeyUp(keyCode, e)
                : base.OnKeyUp(keyCode, e);
        }

        protected override void OnCreate(Bundle savedInstanceState)
        {
            TabLayoutResource = Resource.Layout.Tabbar;
            ToolbarResource = Resource.Layout.Toolbar;

            base.OnCreate(savedInstanceState);

            Xamarin.Essentials.Platform.Init(this, savedInstanceState);
            global::Xamarin.Forms.Forms.Init(this, savedInstanceState);
            var app = new App();
            app.KeyboardController = new KeyboardController();
            LoadApplication(app);
        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions,
            [GeneratedEnum] Android.Content.PM.Permission[] grantResults)
        {
            Xamarin.Essentials.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);

            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }
    }
}