using System;
using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Views;
using Microsoft.Xna.Framework;
using Retro.Engine;

namespace Chapter12;

/// <summary>
/// The Android entry point.
///
/// Three things here are not obvious and matter:
///
///  - ConfigurationChanges lists everything we want to handle ourselves. Without
///    it Android destroys and recreates the Activity when the device rotates or
///    the keyboard appears, which throws away the running game.
///  - Theme.Splash sets the window background to the splash drawable. It is on
///    screen from the moment the process starts until the game's first frame is
///    presented, which on a cold start is most of a second. Leave it off and the
///    app opens on a white rectangle.
///  - The Activity is itself the IPlatformService. Orientation on Android is a
///    property of the Activity, so the object that owns it is the natural place
///    to implement the interface the shared code calls.
/// </summary>
[Activity(
    Label = "12 Saving",
    MainLauncher = true,
    Icon = "@mipmap/ic_launcher",
    RoundIcon = "@mipmap/ic_launcher",
    Theme = "@style/Theme.Splash",
    AlwaysRetainTaskState = true,
    LaunchMode = LaunchMode.SingleInstance,
    ScreenOrientation = ScreenOrientation.SensorPortrait,
    ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.Keyboard |
                           ConfigChanges.KeyboardHidden | ConfigChanges.ScreenSize |
                           ConfigChanges.ScreenLayout | ConfigChanges.UiMode |
                           ConfigChanges.SmallestScreenSize)]
public class MainActivity : AndroidGameActivity, IPlatformService
{
    private Chapter12.Ch12Game _game;
    private View _view;

    public OrientationMode Orientation { get; private set; } = OrientationMode.Portrait;

    protected override void OnCreate(Bundle savedInstanceState)
    {
        base.OnCreate(savedInstanceState);

        _game = new Chapter12.Ch12Game(this);
        _view = _game.Services.GetService(typeof(View)) as View;
        SetContentView(_view);

        // Draw behind the status and navigation bars, then hide them. A game
        // that leaves the system bars up loses 10% of a phone screen.
        //
        // This has to come *after* SetContentView. Window.InsetsController
        // reads the decor view, which does not exist until the window has
        // content - calling it first throws a NullReferenceException inside
        // the platform, and the app dies before drawing a frame.
        GoFullScreen();

        _game.Run();
    }

    private void GoFullScreen()
    {
        var decor = Window?.DecorView;
        if (decor == null) return;

        if (OperatingSystem.IsAndroidVersionAtLeast(30))
        {
            Window.SetDecorFitsSystemWindows(false);
            // Ask the decor view rather than the window: the window's accessor
            // assumes a decor view it does not check for.
            var controller = decor.WindowInsetsController;
            if (controller != null)
            {
                controller.Hide(WindowInsets.Type.SystemBars());
                controller.SystemBarsBehavior =
                    (int)WindowInsetsControllerBehavior.ShowTransientBarsBySwipe;
            }
        }
        else
        {
#pragma warning disable CA1422
            decor.SystemUiVisibility =
                (StatusBarVisibility)(SystemUiFlags.LayoutStable |
                                      SystemUiFlags.LayoutHideNavigation |
                                      SystemUiFlags.LayoutFullscreen |
                                      SystemUiFlags.HideNavigation |
                                      SystemUiFlags.Fullscreen |
                                      SystemUiFlags.ImmersiveSticky);
#pragma warning restore CA1422
        }
    }

    public void SetOrientation(OrientationMode mode)
    {
        Orientation = mode;
        RequestedOrientation = mode switch
        {
            // SensorPortrait / SensorLandscape rather than the plain values, so a
            // tablet held upside down still works. Locking to one exact rotation
            // is a common and very annoying bug.
            OrientationMode.Portrait => ScreenOrientation.SensorPortrait,
            OrientationMode.Landscape => ScreenOrientation.SensorLandscape,
            _ => ScreenOrientation.FullSensor
        };
    }

    public (int Left, int Top, int Right, int Bottom) SafeInsets
    {
        get
        {
            var insets = Window?.DecorView?.RootWindowInsets;
            if (insets == null) return (0, 0, 0, 0);
            if (OperatingSystem.IsAndroidVersionAtLeast(30))
            {
                var i = insets.GetInsets(WindowInsets.Type.DisplayCutout());
                return (i.Left, i.Top, i.Right, i.Bottom);
            }
#pragma warning disable CA1422
            var cutout = insets.DisplayCutout;
            return cutout == null ? (0, 0, 0, 0)
                : (cutout.SafeInsetLeft, cutout.SafeInsetTop, cutout.SafeInsetRight, cutout.SafeInsetBottom);
#pragma warning restore CA1422
        }
    }

    public override void OnWindowFocusChanged(bool hasFocus)
    {
        base.OnWindowFocusChanged(hasFocus);
        if (hasFocus) GoFullScreen();      // the bars come back after a swipe
    }
}
