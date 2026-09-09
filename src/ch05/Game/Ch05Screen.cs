using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input.Touch;
using Retro.Engine;

namespace Chapter05;

/// <summary>
/// Chapter 5 - Input That Feels Right.
///
/// An input probe. Every logical button shows three lamps: DOWN (held), PRESSED
/// (the edge this frame) and BUFFERED (pressed within the last 120ms).
///
/// The buffer lamp is the interesting one. Hold the phone normally and tap jump
/// while watching: PRESSED is lit for a single frame - one sixtieth of a second
/// - and if the game only ever asked that question, every tap that arrived one
/// frame before landing would be thrown away. BUFFERED stays lit long enough for
/// the game to notice. Chapters 16 and 31 both spend the difference.
/// </summary>
public sealed class Ch05Screen : IScreen
{
    private const int W = 224, H = 256;
    private static readonly Btn[] Buttons =
        { Btn.Left, Btn.Right, Btn.Up, Btn.Down, Btn.Jump, Btn.Pause, Btn.Confirm };

    private readonly RetroGame _game;
    private float _lastJumpAge = 99f;
    private int _taps;

    public Ch05Screen(RetroGame game) => _game = game;

    public void Enter() { }
    public void Leave() { }

    public void Update(float dt)
    {
        _lastJumpAge += dt;
        if (_game.Input.Pressed(Btn.Jump)) { _lastJumpAge = 0f; _taps++; }
    }

    public void Draw(SpriteBatch batch)
    {
        var f = _game.Font;
        var px = _game.Assets.Pixel;

        f.DrawCentred(batch, "INPUT PROBE", W / 2, 6, Theme.Accent);
        f.Draw(batch, "BUTTON   DOWN PRESS BUF", new Vector2(10, 24), Theme.InkDim);

        int y = 38;
        foreach (var b in Buttons)
        {
            f.Draw(batch, b.ToString().ToUpperInvariant(), new Vector2(10, y), Theme.Ink);
            Lamp(batch, px, 118, y, _game.Input.Down(b), Theme.Good);
            Lamp(batch, px, 152, y, _game.Input.Pressed(b), Theme.Accent);
            Lamp(batch, px, 186, y, _game.Input.PressedRecently(b), Theme.Warn);
            y += 14;
        }

        // The buffer window, drawn as a draining bar.
        y += 8;
        f.Draw(batch, "JUMP BUFFER 120MS", new Vector2(10, y), Theme.InkDim);
        float frac = Math.Clamp(1f - _lastJumpAge / 0.12f, 0f, 1f);
        batch.Draw(px, new Rectangle(10, y + 12, 200, 8), Theme.Panel * Theme.PanelAlpha);
        batch.Draw(px, new Rectangle(10, y + 12, (int)(200 * frac), 8),
                   frac > 0f ? Theme.Warn : Theme.InkDim);
        f.Draw(batch, "TAPS " + _taps, new Vector2(10, y + 26), Theme.Ink);

        // Raw touches, so you can see the pad hit areas being hit.
        f.Draw(batch, "TOUCHES " + _game.Input.Touches.Count, new Vector2(10, y + 44), Theme.InkDim);
        int i = 0;
        foreach (var t in _game.Input.Touches)
        {
            var v = _game.Screen.ToVirtual(t.Position);
            f.Draw(batch, $"{(int)v.X:000},{(int)v.Y:000} {t.State.ToString().ToUpperInvariant()}",
                   new Vector2(10, y + 56 + i * 10), Theme.Info);
            if (++i >= 3) break;
        }
    }

    private static void Lamp(SpriteBatch b, Texture2D px, int x, int y, bool on, Color c)
        => b.Draw(px, new Rectangle(x, y, 8, 8), on ? c : Theme.InkFaint);
}
