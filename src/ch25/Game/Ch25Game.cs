using Microsoft.Xna.Framework;
using Retro.Engine;

namespace Chapter25;

/// <summary>
/// Chapter 25 - Presentation: Cutscenes and Attract Mode.
///
/// The complete game, with this chapter's debug lens switched on so the app
/// shows what the chapter is about. Turn the lens off and it is the shipping
/// build.
/// </summary>
public class Ch25Game : Retro.Climber.ClimberGame
{
    public Ch25Game(IPlatformService platform) : base(platform)
    {
        Dbg.Reset();
        Dbg.Caption = "CH25 ATTRACT MODE";
    }
}
