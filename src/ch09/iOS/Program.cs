using System;
using System.Linq;
using Foundation;
using Microsoft.Xna.Framework;
using Retro.Engine;
using UIKit;

namespace Chapter09;

public static class Application
{
    private static void Main(string[] args) => UIApplication.Main(args, null, typeof(AppDelegate));
}

/// <summary>
/// The iOS entry point, and the platform service.
///
/// Orientation on iOS is the awkward half of this book's mobile chapter. UIKit
/// asks the application delegate which orientations are allowed *right now*, so
/// changing the answer and then telling UIKit to ask again is the whole trick.
/// How you tell it to ask again changed in iOS 16:
///
///   before 16: UIViewController.AttemptRotationToDeviceOrientation()
///   16 and up: the window scene's RequestGeometryUpdate, plus
///              SetNeedsUpdateOfSupportedInterfaceOrientations on the root
///              controller so it re-reads the mask.
///
/// Both paths are here. The Info.plist must list every orientation the app will
/// ever ask for, because the delegate can only narrow that list, never widen it.
/// </summary>
[Register("AppDelegate")]
public class AppDelegate : UIApplicationDelegate, IPlatformService
{
    private Chapter09.Ch09Game _game;
    private UIInterfaceOrientationMask _mask = UIInterfaceOrientationMask.Portrait;

    public override UIWindow Window { get; set; }

    public OrientationMode Orientation { get; private set; } = OrientationMode.Portrait;

    public override bool FinishedLaunching(UIApplication application, NSDictionary launchOptions)
    {
        _game = new Chapter09.Ch09Game(this);
        _game.Run();
        return true;
    }

    public override UIInterfaceOrientationMask GetSupportedInterfaceOrientations(
        UIApplication application, UIWindow forWindow) => _mask;

    public void SetOrientation(OrientationMode mode)
    {
        Orientation = mode;
        _mask = mode switch
        {
            OrientationMode.Portrait => UIInterfaceOrientationMask.Portrait,
            OrientationMode.Landscape => UIInterfaceOrientationMask.Landscape,
            _ => UIInterfaceOrientationMask.All
        };
        ApplyOrientation();
    }

    private void ApplyOrientation()
    {
        var scene = UIApplication.SharedApplication.ConnectedScenes
            .ToArray()
            .OfType<UIWindowScene>()
            .FirstOrDefault(s => s.ActivationState == UISceneActivationState.ForegroundActive)
            ?? UIApplication.SharedApplication.ConnectedScenes.ToArray().OfType<UIWindowScene>().FirstOrDefault();

        var root = scene?.Windows?.FirstOrDefault(w => w.IsKeyWindow)?.RootViewController
                   ?? Window?.RootViewController;

        if (OperatingSystem.IsIOSVersionAtLeast(16))
        {
            root?.SetNeedsUpdateOfSupportedInterfaceOrientations();
            if (scene != null)
            {
                var prefs = new UIWindowSceneGeometryPreferencesIOS { InterfaceOrientations = _mask };
                scene.RequestGeometryUpdate(prefs, err =>
                {
                    // A refused rotation is not fatal - the game letterboxes into
                    // whatever it is given - so log it and carry on.
                    Console.WriteLine($"Orientation request refused: {err?.LocalizedDescription}");
                });
            }
        }
        else
        {
#pragma warning disable CA1422
            UIViewController.AttemptRotationToDeviceOrientation();
#pragma warning restore CA1422
        }
    }

    public (int Left, int Top, int Right, int Bottom) SafeInsets
    {
        get
        {
            var window = UIApplication.SharedApplication.ConnectedScenes.ToArray()
                .OfType<UIWindowScene>().FirstOrDefault()?.Windows?.FirstOrDefault()
                ?? Window;
            if (window == null) return (0, 0, 0, 0);
            var g = window.SafeAreaInsets;
            var s = (float)window.ContentScaleFactor;
            return ((int)(g.Left * s), (int)(g.Top * s), (int)(g.Right * s), (int)(g.Bottom * s));
        }
    }
}
