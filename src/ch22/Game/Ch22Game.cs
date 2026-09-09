using Microsoft.Xna.Framework;
using Retro.Engine;

namespace Chapter22;

/// <summary>
/// Chapter 22 - Music and the Bonus Timer.
///
/// The complete game, with this chapter's debug lens switched on so the app
/// shows what the chapter is about. Turn the lens off and it is the shipping
/// build.
/// </summary>
public class Ch22Game : Retro.Climber.ClimberGame
{
    public Ch22Game(IPlatformService platform) : base(platform)
    {
        Dbg.Reset();
        Dbg.Caption = "CH22 MUSIC TEMPO";
        Dbg.AudioMeter = true;
        Dbg.Timing = true;
    }
}
