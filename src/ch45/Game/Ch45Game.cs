using Microsoft.Xna.Framework;
using Retro.Engine;

namespace Chapter45;

/// <summary>
/// Chapter 45 - Packaging and Release.
///
/// Generated host: sets the orientation, sizes the virtual screen and opens this
/// chapter's demo. See tools/gen_projects.py.
/// </summary>
public class Ch45Game : RetroGame
{
    public Ch45Game(IPlatformService platform) : base(platform, 224, 256) { }

    protected override void OnLoaded()
    {
        LockOrientation(OrientationMode.Portrait);
        Dbg.Reset();
        Dbg.Caption = "CH45 RELEASE CHECK";
        Screens.Replace(new Ch45Screen(this), fade: false);
    }
}
