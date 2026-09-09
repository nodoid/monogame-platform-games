using Microsoft.Xna.Framework;
using Retro.Engine;

namespace Chapter06;

/// <summary>
/// Chapter 6 - Animation Systems.
///
/// Generated host: sets the orientation, sizes the virtual screen and opens this
/// chapter's demo. See tools/gen_projects.py.
/// </summary>
public class Ch06Game : RetroGame
{
    public Ch06Game(IPlatformService platform) : base(platform, 224, 256) { }

    protected override void OnLoaded()
    {
        LockOrientation(OrientationMode.Portrait);
        Dbg.Reset();
        Dbg.Caption = "CH06";
        Screens.Replace(new Ch06Screen(this), fade: false);
    }
}
