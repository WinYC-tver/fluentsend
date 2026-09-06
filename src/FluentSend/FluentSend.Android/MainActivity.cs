using System;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Runtime;
using Avalonia;
using Avalonia.Android;

namespace FluentSend.Android;

[Activity(
    Label = "FluentSend",
    Theme = "@style/MyTheme.NoActionBar",
    Icon = "@drawable/Icon",
    MainLauncher = true,
    Exported = true,
    LaunchMode = LaunchMode.SingleTask,
    ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.UiMode | ConfigChanges.KeyboardHidden | ConfigChanges.SmallestScreenSize | ConfigChanges.ScreenLayout)]
[IntentFilter(new[] { Intent.ActionMain }, Categories = new[] { Intent.CategoryLauncher })]
public class MainActivity : AvaloniaMainActivity
{
    private const int RequestPermissionsCode = 0xA1;

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        RequestRuntimePermissions();
    }

    private void RequestRuntimePermissions()
    {
        if (Build.VERSION.SdkInt >= BuildVersionCodes.Tiramisu)
        {
            var perms = new[]
            {
                Manifest.Permission.PostNotifications,
                Manifest.Permission.ReadMediaImages,
                Manifest.Permission.ReadMediaVideo,
                Manifest.Permission.ReadMediaAudio,
            };
            RequestPermissions(perms, RequestPermissionsCode);
        }
        else if (Build.VERSION.SdkInt >= BuildVersionCodes.M)
        {
            RequestPermissions(new[]
            {
                Manifest.Permission.ReadExternalStorage,
                Manifest.Permission.WriteExternalStorage,
            }, RequestPermissionsCode);
        }

        if (Build.VERSION.SdkInt >= BuildVersionCodes.S)
        {
            try
            {
                var wifiManager = (Android.Net.Wifi.WifiManager?)GetSystemService(WifiService);
                var multicastLock = wifiManager?.CreateMulticastLock("fluentsend-discovery");
                multicastLock?.Acquire();
            }
            catch
            {
            }
        }
    }

    protected override AvaloniaView CreateAvaloniaView()
    {
        return new AvaloniaView(this);
    }

    public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Permission[] grantResults)
    {
        base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
    }
}
