using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Retro.Engine;

namespace Chapter08;

/// <summary>
/// Chapter 8 - Game States and Screen Flow.
///
/// Three screens and a stack. Fire pushes the next screen, the pause button pops
/// one, and the overlay screen demonstrates why a stack beats a single "current
/// screen" variable: the menu below it keeps drawing and keeps its state, but
/// stops updating.
/// </summary>
public sealed class Ch08Screen : IScreen
{
    private readonly RetroGame _game;
    private readonly string _label;
    private readonly Color _colour;
    private readonly int _depth;
    private float _t;
    private int _ticks;

    public Ch08Screen(RetroGame game, string label = "TITLE", Color? colour = null, int depth = 0)
    {
        _game = game;
        _label = label;
        _colour = colour ?? Theme.Accent;
        _depth = depth;
    }

    public void Enter() => _t = 0f;
    public void Leave() { }

    public void Update(float dt)
    {
        _t += dt;
        _ticks++;

        if (_game.Screens.Busy) return;

        if (_game.Input.Pressed(Btn.Jump) || _game.Input.Pressed(Btn.Confirm))
        {
            switch (_depth)
            {
                case 0:
                    _game.Screens.Push(new Ch08Screen(_game, "GAMEPLAY", Theme.Good, 1));
                    break;
                case 1:
                    _game.Screens.Push(new Ch08Overlay(_game, this));
                    break;
            }
        }
        else if (_game.Input.Pressed(Btn.Pause) && _depth > 0)
        {
            _game.Screens.Pop();
        }
    }

    /// <summary>Exposed so the overlay can prove the screen below stopped updating.</summary>
    public int Ticks => _ticks;

    public void Draw(SpriteBatch batch)
    {
        int w = _game.VirtualWidth, h = _game.VirtualHeight;
        var f = _game.Font;

        batch.Draw(_game.Assets.Pixel, new Rectangle(0, 0, w, h), Theme.Background);

        // Something moving, so a frozen screen is obvious at a glance.
        int bx = (int)((MathF.Sin(_t * 1.6f) * 0.5f + 0.5f) * (w - 24)) + 4;
        batch.Draw(_game.Assets.Pixel, new Rectangle(bx, h / 2 - 30, 16, 16), _colour);

        f.DrawCentred(batch, _label, w / 2, 30, _colour, 2);
        f.DrawCentred(batch, "STACK DEPTH " + _game.Screens.Depth, w / 2, 60, Theme.Ink);
        f.DrawCentred(batch, "UPDATES " + _ticks, w / 2, 74, Theme.InkDim);

        f.DrawCentred(batch, _depth < 2 ? "FIRE  PUSH NEXT" : "", w / 2, h - 60, Theme.InkDim);
        if (_depth > 0) f.DrawCentred(batch, "PAUSE  POP", w / 2, h - 46, Theme.InkDim);
    }
}

/// <summary>
/// Marked as an overlay, so the screen manager keeps drawing what is underneath.
/// </summary>
public sealed class Ch08Overlay : IScreen, IOverlayScreen
{
    private readonly RetroGame _game;
    private readonly Ch08Screen _below;
    private readonly int _ticksAtEntry;
    private float _t;

    public Ch08Overlay(RetroGame game, Ch08Screen below)
    {
        _game = game;
        _below = below;
        _ticksAtEntry = below.Ticks;
    }

    public void Enter() => _t = 0f;
    public void Leave() { }

    public void Update(float dt)
    {
        _t += dt;
        if (_t > 0.3f && (_game.Input.Pressed(Btn.Pause) || _game.Input.Pressed(Btn.Jump)))
            _game.Screens.Pop();
    }

    public void Draw(SpriteBatch batch)
    {
        int w = _game.VirtualWidth, h = _game.VirtualHeight;
        var f = _game.Font;
        batch.Draw(_game.Assets.Pixel, new Rectangle(0, 0, w, h), Theme.Panel * Theme.PanelAlpha);
        f.DrawCentred(batch, "PAUSED", w / 2, h / 2 - 24, Theme.Ink, 2);
        f.DrawCentred(batch, "THE SCREEN BELOW", w / 2, h / 2 + 6, Theme.InkDim);
        f.DrawCentred(batch, "IS STILL DRAWN", w / 2, h / 2 + 18, Theme.InkDim);
        f.DrawCentred(batch, "FROZEN AT " + _ticksAtEntry, w / 2, h / 2 + 36, Theme.Accent);
        f.DrawCentred(batch, "NOW " + _below.Ticks, w / 2, h / 2 + 48, Theme.Accent);
        f.DrawCentred(batch, "TAP TO RESUME", w / 2, h - 50, Theme.Ink);
    }
}
