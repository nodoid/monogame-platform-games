using Microsoft.Xna.Framework;
using Retro.Engine;
using Retro.Climber;

namespace Chapter16;

/// <summary>Chapter 16 - The Jump Arc.</summary>
public class Ch16Game : Retro.Climber.ClimberSandboxGame
{
    public Ch16Game(IPlatformService platform)
        : base(platform, g => new ClimberSandboxScreen(g, "CH16 JUMPING"))
    {
        Dbg.Reset();
        Dbg.Caption = "CH16";
    }
}
