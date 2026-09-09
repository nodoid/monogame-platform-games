using Microsoft.Xna.Framework;
using Retro.Engine;
using Retro.Hunch;

namespace Chapter28;

/// <summary>Chapter 28 - Flick-Screen versus Scrolling.</summary>
public class Ch28Game : Retro.Hunch.HunchSandboxGame
{
    public Ch28Game(IPlatformService platform)
        : base(platform, g => new HunchCameraScreen(g))
    {
        Dbg.Reset();
        Dbg.Caption = "CH28";
    }
}
