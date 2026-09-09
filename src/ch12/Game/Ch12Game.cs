using Microsoft.Xna.Framework;
using Retro.Engine;

namespace Chapter12;

/// <summary>
/// Chapter 12 - Persisting High Scores.
///
/// Generated host: sets the orientation, sizes the virtual screen and opens this
/// chapter's demo. See tools/gen_projects.py.
/// </summary>
public class Ch12Game : RetroGame
{
    public Ch12Game(IPlatformService platform) : base(platform, 224, 256) { }

    protected override void OnLoaded()
    {
        LockOrientation(OrientationMode.Portrait);
        Dbg.Reset();
        Dbg.Caption = "CH12";
        Screens.Replace(new Ch12Screen(this), fade: false);
    }
}
