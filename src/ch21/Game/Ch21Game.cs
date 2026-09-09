using Microsoft.Xna.Framework;
using Retro.Engine;

namespace Chapter21;

/// <summary>
/// Chapter 21 - Sound Effects for the Climber.
///
/// The complete game, with this chapter's debug lens switched on so the app
/// shows what the chapter is about. Turn the lens off and it is the shipping
/// build.
/// </summary>
public class Ch21Game : Retro.Climber.ClimberGame
{
    public Ch21Game(IPlatformService platform) : base(platform)
    {
        Dbg.Reset();
        Dbg.Caption = "CH21 SOUND";
        Dbg.AudioMeter = true;
    }
}
