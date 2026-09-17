using Android.App;
using Android.Content.PM;
using Android.OS;

namespace FinAPP
{
    [Activity(
        Theme = "@style/Maui.SplashTheme",
        Label = "FinAPP",
        MainLauncher = true,
        ScreenOrientation = ScreenOrientation.Portrait,
        LaunchMode = LaunchMode.SingleTop,
        ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
    public class MainActivity : MauiAppCompatActivity
    {
        protected override void OnCreate(Bundle? savedInstanceState)
        {
            AndroidX.AppCompat.App.AppCompatDelegate.DefaultNightMode = AndroidX.AppCompat.App.AppCompatDelegate.ModeNightNo;
            base.OnCreate(savedInstanceState);
        }
    }
}
