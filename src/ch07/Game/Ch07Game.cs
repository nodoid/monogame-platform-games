using Microsoft.Xna.Framework;
using Retro.Engine;

namespace Chapter07;

/// <summary>
/// Chapter 7 - Collision Fundamentals.
///
/// Generated host: sets the orientation, sizes the virtual screen and opens this
/// chapter's demo. See tools/gen_projects.py.
/// </summary>
public class Ch07Game : RetroGame
{
    public Ch07Game(IPlatformService platform) : base(platform, 224, 256) { }

    protected override void OnLoaded()
    {
        LockOrientation(OrientationMode.Portrait);
        Dbg.Reset();
        Dbg.Caption = "CH07";
        Screens.Replace(new Ch07Screen(this), fade: false);
    }
}
