using Microsoft.Xna.Framework;
using Retro.Engine;
using Retro.Hunch;

namespace Chapter30;

/// <summary>Chapter 30 - Running Movement.</summary>
public class Ch30Game : Retro.Hunch.HunchSandboxGame
{
    public Ch30Game(IPlatformService platform)
        : base(platform, g => new HunchSandboxScreen(g, "CH30 RUNNING"))
    {
        Dbg.Reset();
        Dbg.Caption = "CH30";
    }
}
