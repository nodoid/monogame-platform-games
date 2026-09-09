using Microsoft.Xna.Framework;
using Retro.Engine;

namespace Chapter09;

/// <summary>
/// Chapter 9 - Building the Audio Engine.
///
/// Generated host: sets the orientation, sizes the virtual screen and opens this
/// chapter's demo. See tools/gen_projects.py.
/// </summary>
public class Ch09Game : RetroGame
{
    public Ch09Game(IPlatformService platform) : base(platform, 224, 256) { }

    protected override void OnLoaded()
    {
        LockOrientation(OrientationMode.Portrait);
        Dbg.Reset();
        Dbg.Caption = "CH09";
        Screens.Replace(new Ch09Screen(this), fade: false);
    }
}
