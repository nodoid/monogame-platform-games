using Microsoft.Xna.Framework;
using Retro.Engine;

namespace Chapter08;

/// <summary>
/// Chapter 8 - Game States and Screen Flow.
///
/// Generated host: sets the orientation, sizes the virtual screen and opens this
/// chapter's demo. See tools/gen_projects.py.
/// </summary>
public class Ch08Game : RetroGame
{
    public Ch08Game(IPlatformService platform) : base(platform, 224, 256) { }

    protected override void OnLoaded()
    {
        LockOrientation(OrientationMode.Portrait);
        Dbg.Reset();
        Dbg.Caption = "CH08";
        Screens.Replace(new Ch08Screen(this), fade: false);
    }
}
