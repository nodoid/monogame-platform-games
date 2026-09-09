using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Retro.Engine;

namespace Chapter06;

/// <summary>
/// Chapter 6 - Animation Systems.
///
/// The same run cycle played at four different rates side by side, plus a
/// controllable one. Up and down change the frame rate of the big sprite; left
/// and right change which sheet is playing; fire freezes it so you can step.
///
/// The lesson is that a walk cycle has a *correct* speed, and it is not a
/// matter of taste: the feet must appear to be planted. Run the cycle too slow
/// against the movement speed and the character moonwalks; too fast and it
/// scurries. Chapter 24 pins the runner's cycle to its actual velocity.
/// </summary>
public sealed class Ch06Screen : IScreen
{
    private const int W = 224, H = 256;

    private static readonly (string Sheet, int FW, int FH, int First, int Last)[] Clips =
    {
        ("jack_run", 12, 16, 1, 3),
        ("jack_climb", 12, 16, 0, 1),
        ("barrel", 16, 16, 0, 3),
        ("fireball", 16, 16, 0, 1),
        ("gorilla", 32, 32, 0, 1),
    };

    private readonly RetroGame _game;
    private readonly Animation[] _row = new Animation[4];
    private static readonly float[] RowFps = { 3f, 6f, 12f, 24f };

    private Animation _big;
    private int _clip;
    private float _fps = 12f;
    private bool _paused;
    private float _stepHeld;

    public Ch06Screen(RetroGame game) => _game = game;

    public void Enter() => Rebuild();

    public void Leave() { }

    private void Rebuild()
    {
        var (sheet, fw, fh, first, last) = Clips[_clip];
        var tex = _game.Assets.Texture(sheet);
        _big = new Animation(tex, fw, fh, _fps).SetRange(first, last);
        for (int i = 0; i < _row.Length; i++)
            _row[i] = new Animation(tex, fw, fh, RowFps[i]).SetRange(first, last);
    }

    public void Update(float dt)
    {
        if (_game.Input.Pressed(Btn.Right)) { _clip = (_clip + 1) % Clips.Length; Rebuild(); }
        if (_game.Input.Pressed(Btn.Left)) { _clip = (_clip + Clips.Length - 1) % Clips.Length; Rebuild(); }
        if (_game.Input.Pressed(Btn.Up)) { _fps = Math.Min(30f, _fps + 1f); Rebuild(); }
        if (_game.Input.Pressed(Btn.Down)) { _fps = Math.Max(1f, _fps - 1f); Rebuild(); }
        if (_game.Input.Pressed(Btn.Jump)) _paused = !_paused;

        if (!_paused) _big.Update(dt);
        else
        {
            // While paused, holding fire steps one frame every 250ms.
            _stepHeld = _game.Input.Down(Btn.Jump) ? _stepHeld + dt : 0f;
        }
        foreach (var a in _row) a.Update(dt);
    }

    public void Draw(SpriteBatch batch)
    {
        var f = _game.Font;
        var (sheet, fw, fh, first, last) = Clips[_clip];
        var tex = _game.Assets.Texture(sheet);

        f.DrawCentred(batch, "ANIMATION LAB", W / 2, 6, Theme.Accent);
        f.DrawCentred(batch, sheet.ToUpperInvariant(), W / 2, 18, Theme.Ink);

        int scale = 4;
        int cx = W / 2, cy = 100;
        var dest = new Rectangle(cx - fw * scale / 2, cy - fh * scale / 2, fw * scale, fh * scale);
        batch.Draw(tex, dest, _big.Source, Color.White);

        f.DrawCentred(batch, _fps.ToString("00") + " FPS", cx, cy + fh * scale / 2 + 8,
                      _paused ? Theme.Warn : Theme.Good);
        f.DrawCentred(batch, "FRAME " + _big.Frame + " OF " + first + "-" + last,
                      cx, cy + fh * scale / 2 + 20, Theme.InkDim);

        // The comparison row.
        f.Draw(batch, "SAME CLIP, FOUR RATES", new Vector2(10, 168), Theme.InkDim);
        for (int i = 0; i < _row.Length; i++)
        {
            int x = 22 + i * 48;
            batch.Draw(tex, new Rectangle(x - fw, 184, fw * 2, fh * 2), _row[i].Source, Color.White);
            f.Draw(batch, RowFps[i].ToString("00"), new Vector2(x - 6, 184 + fh * 2 + 4), Theme.InkDim);
        }

        f.Draw(batch, "L R  CLIP", new Vector2(10, H - 40), Theme.InkDim);
        f.Draw(batch, "U D  RATE", new Vector2(10, H - 30), Theme.InkDim);
        f.Draw(batch, "FIRE " + (_paused ? "RESUME" : "PAUSE"), new Vector2(10, H - 20), Theme.InkDim);
    }
}
