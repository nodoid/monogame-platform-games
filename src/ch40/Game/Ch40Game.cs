using Microsoft.Xna.Framework;
using Retro.Engine;

namespace Chapter40;

/// <summary>
/// Chapter 40 - Sound Effects for the Runner.
///
/// The complete game, with this chapter's debug lens switched on so the app
/// shows what the chapter is about. Turn the lens off and it is the shipping
/// build.
/// </summary>
public class Ch40Game : Retro.Hunch.HunchGame
{
    public Ch40Game(IPlatformService platform) : base(platform)
    {
        Dbg.Reset();
        Dbg.Caption = "CH40 SOUND";
        Dbg.AudioMeter = true;
    }
}
