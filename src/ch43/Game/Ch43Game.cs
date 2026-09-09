using Microsoft.Xna.Framework;
using Retro.Engine;

namespace Chapter43;

/// <summary>
/// Chapter 43 - Performance and Optimisation.
///
/// The complete game, with this chapter's debug lens switched on so the app
/// shows what the chapter is about. Turn the lens off and it is the shipping
/// build.
/// </summary>
public class Ch43Game : Retro.Climber.ClimberGame
{
    public Ch43Game(IPlatformService platform) : base(platform)
    {
        Dbg.Reset();
        Dbg.Caption = "CH43 PERFORMANCE";
        Dbg.AudioMeter = true;
        Dbg.Timing = true;
    }
}
