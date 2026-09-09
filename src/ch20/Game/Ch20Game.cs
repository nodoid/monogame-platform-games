using Microsoft.Xna.Framework;
using Retro.Engine;

namespace Chapter20;

/// <summary>
/// Chapter 20 - Scoring, Lives and the Bonus Timer.
///
/// The complete game, with this chapter's debug lens switched on so the app
/// shows what the chapter is about. Turn the lens off and it is the shipping
/// build.
/// </summary>
public class Ch20Game : Retro.Climber.ClimberGame
{
    public Ch20Game(IPlatformService platform) : base(platform)
    {
        Dbg.Reset();
        Dbg.Caption = "CH20 SCORE AND LIVES";
    }
}
