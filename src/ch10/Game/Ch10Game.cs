using Microsoft.Xna.Framework;
using Retro.Engine;

namespace Chapter10;

/// <summary>
/// Chapter 10 - Sourcing and Making Sound Effects.
///
/// Generated host: sets the orientation, sizes the virtual screen and opens this
/// chapter's demo. See tools/gen_projects.py.
/// </summary>
public class Ch10Game : RetroGame
{
    public Ch10Game(IPlatformService platform) : base(platform, 224, 256) { }

    protected override void OnLoaded()
    {
        LockOrientation(OrientationMode.Portrait);
        Dbg.Reset();
        Dbg.Caption = "CH10";
        Screens.Replace(new Ch10Screen(this), fade: false);
    }
}
