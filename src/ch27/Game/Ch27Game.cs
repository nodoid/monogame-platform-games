using Microsoft.Xna.Framework;
using Retro.Engine;
using Retro.Hunch;

namespace Chapter27;

/// <summary>Chapter 27 - Anatomy of a Run-and-Jump.</summary>
public class Ch27Game : Retro.Hunch.HunchSandboxGame
{
    public Ch27Game(IPlatformService platform)
        : base(platform, g => new HunchScreenViewer(g))
    {
        Dbg.Reset();
        Dbg.Caption = "CH27";
    }
}
