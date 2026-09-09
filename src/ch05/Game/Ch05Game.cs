using Microsoft.Xna.Framework;
using Retro.Engine;

namespace Chapter05;

/// <summary>
/// Chapter 5 - Input That Feels Right.
///
/// Generated host: sets the orientation, sizes the virtual screen and opens this
/// chapter's demo. See tools/gen_projects.py.
/// </summary>
public class Ch05Game : RetroGame
{
    public Ch05Game(IPlatformService platform) : base(platform, 224, 256) { }

    protected override void OnLoaded()
    {
        LockOrientation(OrientationMode.Portrait);
        Dbg.Reset();
        Dbg.Caption = "CH05";
        Screens.Replace(new Ch05Screen(this), fade: false);
    }
}
