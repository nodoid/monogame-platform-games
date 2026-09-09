using Microsoft.Xna.Framework;
using Retro.Engine;

namespace Chapter23;

/// <summary>
/// Chapter 23 - The High Score Table and Initial Entry.
///
/// The complete game, with this chapter's debug lens switched on so the app
/// shows what the chapter is about. Turn the lens off and it is the shipping
/// build.
/// </summary>
public class Ch23Game : Retro.Climber.ClimberGame
{
    public Ch23Game(IPlatformService platform) : base(platform)
    {
        Dbg.Reset();
        Dbg.Caption = "CH23 HIGH SCORES";
    }
}
