using Microsoft.Xna.Framework;
using Retro.Engine;

namespace Chapter46;

/// <summary>
/// Chapter 46 - Where to Take These Engines Next.
///
/// Generated host: sets the orientation, sizes the virtual screen and opens this
/// chapter's demo. See tools/gen_projects.py.
/// </summary>
public class Ch46Game : RetroGame
{
    public Ch46Game(IPlatformService platform) : base(platform, 224, 256) { }

    protected override void OnLoaded()
    {
        LockOrientation(OrientationMode.Portrait);
        Dbg.Reset();
        Dbg.Caption = "CH46 BOTH GAMES";
        Screens.Replace(new Ch46Screen(this), fade: false);
    }
}
