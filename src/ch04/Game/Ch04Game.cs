using Microsoft.Xna.Framework;
using Retro.Engine;

namespace Chapter04;

/// <summary>
/// Chapter 4 - Sprites, Sheets and the Content Pipeline.
///
/// Generated host: sets the orientation, sizes the virtual screen and opens this
/// chapter's demo. See tools/gen_projects.py.
/// </summary>
public class Ch04Game : RetroGame
{
    public Ch04Game(IPlatformService platform) : base(platform, 224, 256) { }

    protected override void OnLoaded()
    {
        LockOrientation(OrientationMode.Portrait);
        Dbg.Reset();
        Dbg.Caption = "CH04";
        Screens.Replace(new Ch04Screen(this), fade: false);
    }
}
