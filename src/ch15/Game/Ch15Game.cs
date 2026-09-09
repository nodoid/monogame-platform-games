using Microsoft.Xna.Framework;
using Retro.Engine;
using Retro.Climber;

namespace Chapter15;

/// <summary>Chapter 15 - Climbing Mechanics.</summary>
public class Ch15Game : Retro.Climber.ClimberSandboxGame
{
    public Ch15Game(IPlatformService platform)
        : base(platform, g => new ClimberSandboxScreen(g, "CH15 CLIMBING"))
    {
        Dbg.Reset();
        Dbg.Caption = "CH15";
    }
}
