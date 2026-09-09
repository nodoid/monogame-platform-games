using Microsoft.Xna.Framework;
using Retro.Engine;

namespace Chapter24;

/// <summary>
/// Chapter 24 - Stage Progression and Difficulty.
///
/// The complete game, with this chapter's debug lens switched on so the app
/// shows what the chapter is about. Turn the lens off and it is the shipping
/// build.
/// </summary>
public class Ch24Game : Retro.Climber.ClimberGame
{
    public Ch24Game(IPlatformService platform) : base(platform)
    {
        Dbg.Reset();
        Dbg.Caption = "CH24 DIFFICULTY";
        Dbg.Ladders = true;
        Dbg.Timing = true;
    }
}
