using Microsoft.Xna.Framework;
using Retro.Engine;

namespace Chapter17;

/// <summary>
/// Chapter 17 - Barrels and Rolling Hazards.
///
/// The complete game, with this chapter's debug lens switched on so the app
/// shows what the chapter is about. Turn the lens off and it is the shipping
/// build.
/// </summary>
public class Ch17Game : Retro.Climber.ClimberGame
{
    public Ch17Game(IPlatformService platform) : base(platform)
    {
        Dbg.Reset();
        Dbg.Caption = "CH17 BARREL PATHS";
        Dbg.Ladders = true;
        Dbg.Paths = true;
    }
}
