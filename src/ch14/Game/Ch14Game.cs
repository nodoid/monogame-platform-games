using Microsoft.Xna.Framework;
using Retro.Engine;
using Retro.Climber;

namespace Chapter14;

/// <summary>Chapter 14 - The Static Playfield.</summary>
public class Ch14Game : Retro.Climber.ClimberSandboxGame
{
    public Ch14Game(IPlatformService platform)
        : base(platform, g => new ClimberSandboxScreen(g, "CH14 WALKING"))
    {
        Dbg.Reset();
        Dbg.Caption = "CH14";
    }
}
