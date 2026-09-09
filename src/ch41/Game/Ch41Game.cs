using Microsoft.Xna.Framework;
using Retro.Engine;

namespace Chapter41;

/// <summary>
/// Chapter 41 - Music, Feedback and Audio as a Warning.
///
/// The complete game, with this chapter's debug lens switched on so the app
/// shows what the chapter is about. Turn the lens off and it is the shipping
/// build.
/// </summary>
public class Ch41Game : Retro.Hunch.HunchGame
{
    public Ch41Game(IPlatformService platform) : base(platform)
    {
        Dbg.Reset();
        Dbg.Caption = "CH41 AUDIO WARNINGS";
        Dbg.AudioMeter = true;
        Dbg.Timing = true;
    }
}
