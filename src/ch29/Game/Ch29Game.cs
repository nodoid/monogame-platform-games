using Microsoft.Xna.Framework;
using Retro.Engine;
using Retro.Hunch;

namespace Chapter29;

/// <summary>Chapter 29 - Level Data and the Tile Map.</summary>
public class Ch29Game : Retro.Hunch.HunchSandboxGame
{
    public Ch29Game(IPlatformService platform)
        : base(platform, g => new HunchSandboxScreen(g, "CH29 TILE MAP"))
    {
        Dbg.Reset();
        Dbg.Caption = "CH29";
    }
}
