using Microsoft.Xna.Framework;
using Retro.Engine;

namespace Chapter03;

/// <summary>
/// Chapter 3 - The Game Loop and Fixed Timestep.
///
/// Generated host: sets the orientation, sizes the virtual screen and opens this
/// chapter's demo. See tools/gen_projects.py.
/// </summary>
public class Ch03Game : RetroGame
{
    public Ch03Game(IPlatformService platform) : base(platform, 224, 256) { }

    protected override void OnLoaded()
    {
        LockOrientation(OrientationMode.Portrait);
        Dbg.Reset();
        Dbg.Caption = "CH03";
        Screens.Replace(new Ch03Screen(this), fade: false);
    }
}
