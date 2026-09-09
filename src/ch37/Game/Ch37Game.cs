using Microsoft.Xna.Framework;
using Retro.Engine;

namespace Chapter37;

/// <summary>
/// Chapter 37 - Scoring, Bonuses and the High Score Table.
///
/// The complete game, with this chapter's debug lens switched on so the app
/// shows what the chapter is about. Turn the lens off and it is the shipping
/// build.
/// </summary>
public class Ch37Game : Retro.Hunch.HunchGame
{
    public Ch37Game(IPlatformService platform) : base(platform)
    {
        Dbg.Reset();
        Dbg.Caption = "CH37 SCORE AND BONUS";
    }
}
