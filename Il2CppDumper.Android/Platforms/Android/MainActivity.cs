using Android.App;
using Android.Content.PM;
using Microsoft.Maui;

namespace Il2CppDumper.Mobile
{
    [Activity(
        Theme = "@style/Maui.MainTheme.NoActionBar",
        MainLauncher = true,
        LaunchMode = LaunchMode.SingleTop,
        ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode |
                               ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
    public class MainActivity : MauiAppCompatActivity
    {
    }
}
