using Microsoft.Xna.Framework;
using Retro.Engine;

namespace Chapter44;

/// <summary>
/// Chapter 44 - Settings, Saves and Shared Score Data.
///
/// Generated host: sets the orientation, sizes the virtual screen and opens this
/// chapter's demo. See tools/gen_projects.py.
/// </summary>
public class Ch44Game : RetroGame
{
    public Ch44Game(IPlatformService platform) : base(platform, 224, 256) { }

    protected override void OnLoaded()
    {
        LockOrientation(OrientationMode.Portrait);
        Dbg.Reset();
        Dbg.Caption = "CH44 SETTINGS";
        Screens.Replace(new Ch44Screen(this), fade: false);
    }
}
