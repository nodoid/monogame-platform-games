using Microsoft.Xna.Framework;
using Retro.Engine;
using Retro.Climber;

namespace Chapter13;

/// <summary>Chapter 13 - Anatomy of a Single-Screen Climber.</summary>
public class Ch13Game : Retro.Climber.ClimberSandboxGame
{
    public Ch13Game(IPlatformService platform)
        : base(platform, g => new ClimberLevelViewer(g))
    {
        Dbg.Reset();
        Dbg.Caption = "CH13";
    }
}
