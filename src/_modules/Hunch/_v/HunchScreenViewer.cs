using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Retro.Engine;

namespace Retro.Hunch;

/// <summary>
/// Chapter 27 - Anatomy of a Run-and-Jump.
///
/// All fifteen screens, one at a time, annotated with what each one is *for*.
/// Up and down move through them; the pause button shows the jump reach against
/// each gap.
///
/// Reading them in order is the chapter's argument: screen two teaches the jump
/// with a gap you cannot fail, screen three tests it three times, screen four
/// makes a gap deliberately too wide so the rope has something to be the answer
/// to. Nothing is introduced in a screen that also demands precision with it.
/// </summary>
public sealed class HunchScreenViewer : IScreen
{
    private const int W = 256, H = 192;

    private static readonly string[] Purpose =
    {
        "TEACH: RUN AND RING",       "TEACH: THE JUMP",
        "TEST: THREE GAPS",          "TEACH: THE ROPE",
        "TEST: TWO ROPES",           "TEACH: ARROWS",
        "COMBINE: ARROWS + GAPS",    "TEACH: FIRE PITS",
        "TEACH: BOUNCING FIRE",      "COMBINE: FIRE + GAPS",
        "TEACH: THE KNIGHT",         "COMBINE: KNIGHT + GAP",
        "TEACH: HIGH WALKWAY",       "COMBINE: ROPE OVER FIRE",
        "TEST: TWO KNIGHTS",         "TEST: EVERYTHING",
    };

    private readonly RetroGame _game;
    private HunchLevel _level;
    private Parallax _parallax;
    private int _index;
    private bool _reach;
    private float _t;

    public HunchScreenViewer(RetroGame game) => _game = game;

    public void Enter()
    {
        _parallax = new Parallax(_game.Assets);
        _level = HunchScreens.Load(_index);
    }

    public void Leave() { }

    public void Update(float dt)
    {
        _t += dt;
        if (_game.Input.Pressed(Btn.Up) || _game.Input.Pressed(Btn.Right))
        { _index = (_index + 1) % HunchScreens.Count; Enter(); }
        if (_game.Input.Pressed(Btn.Down) || _game.Input.Pressed(Btn.Left))
        { _index = (_index + HunchScreens.Count - 1) % HunchScreens.Count; Enter(); }
        if (_game.Input.Pressed(Btn.Pause) || _game.Input.Pressed(Btn.Jump)) _reach = !_reach;
    }

    public void Draw(SpriteBatch batch)
    {
        var f = _game.Font;
        var px = _game.Assets.Pixel;

        _parallax.Draw(batch, _index, 0f, W, H);
        _level.Draw(batch, _game.Assets.Texture("tiles"));

        foreach (var r in _level.Ropes)
            for (int y = 0; y < (int)r.Length; y += 8)
                batch.Draw(_game.Assets.Texture("rope"),
                           new Rectangle((int)r.Top.X - 2, (int)r.Top.Y + y, 4, 8), Color.White);

        if (_reach)
        {
            // How far one jump goes from a standing start at top speed.
            float airtime = 2f * MathF.Abs(HunchRunner.JumpVelocity) / HunchRunner.Gravity;
            float reach = HunchRunner.MaxRun * airtime;
            var start = _level.PlayerStart;
            Dbg.Arc(batch, px, start, new Vector2(HunchRunner.MaxRun, HunchRunner.JumpVelocity),
                    HunchRunner.Gravity, Theme.DbgBody, 1.4f);
            f.Draw(batch, "REACH " + reach.ToString("00") + " PX  (" + (reach / 8f).ToString("0.0") + " TILES)",
                   new Vector2(4, 24), Theme.DbgBody);
        }

        batch.Draw(px, new Rectangle(0, 0, W, 16), Theme.Panel * Theme.PanelAlpha);
        f.Draw(batch, (_index + 1).ToString("D2") + "/" + HunchScreens.Count + "  " + _level.Name,
               new Vector2(4, 1), Theme.Accent);
        f.Draw(batch, Purpose[Math.Min(_index, Purpose.Length - 1)], new Vector2(4, 9), Theme.Ink);

        batch.Draw(px, new Rectangle(0, H - 11, W, 11), Theme.Panel * Theme.PanelAlpha);
        f.Draw(batch, "U D SCREEN   FIRE REACH   TIME " + _level.TimeLimit + "S",
               new Vector2(4, H - 9), Theme.InkDim);
    }
}
