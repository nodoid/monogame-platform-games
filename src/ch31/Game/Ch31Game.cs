using Microsoft.Xna.Framework;
using Retro.Engine;
using Retro.Hunch;

namespace Chapter31;

/// <summary>Chapter 31 - Precision Jumping.</summary>
public class Ch31Game : Retro.Hunch.HunchSandboxGame
{
    public Ch31Game(IPlatformService platform)
        : base(platform, g => new HunchSandboxScreen(g, "CH31 JUMPING"))
    {
        Dbg.Reset();
        Dbg.Caption = "CH31";
    }
}
