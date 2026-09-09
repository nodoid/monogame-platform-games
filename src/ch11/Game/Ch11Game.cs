using Microsoft.Xna.Framework;
using Retro.Engine;

namespace Chapter11;

/// <summary>
/// Chapter 11 - Scoring and the High Score Table.
///
/// Generated host: sets the orientation, sizes the virtual screen and opens this
/// chapter's demo. See tools/gen_projects.py.
/// </summary>
public class Ch11Game : RetroGame
{
    public Ch11Game(IPlatformService platform) : base(platform, 224, 256) { }

    protected override void OnLoaded()
    {
        LockOrientation(OrientationMode.Portrait);
        Dbg.Reset();
        Dbg.Caption = "CH11";
        Screens.Replace(new Ch11Screen(this), fade: false);
    }
}
