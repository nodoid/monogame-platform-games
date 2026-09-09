using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Retro.Engine;

namespace Retro.Climber;

/// <summary>
/// Chapter 13 - Anatomy of a Single-Screen Climber.
///
/// A stage viewer for all four stages. Left and right change stage; up and down
/// cycle four overlays:
///
///   NONE      the stage as the player sees it
///   BEAMS     every girder's surface line and its gradient
///   ROLL      the direction a hazard travels, which on a slope is downhill
///   REACH     the jump apex line above each girder
///
/// The BEAMS overlay is the one to study. The original's girders tilt, and this
/// book models them as beams with a linear surface rather than as tiles, so a
/// barrel accelerates downhill instead of rolling at a constant speed along a
/// staircase. The gradient printed against each beam is what the barrel reads.
/// </summary>
public sealed class ClimberLevelViewer : IScreen
{
    private const int W = 224, H = 256;
    private enum Overlay { None, Beams, Roll, Reach }

    private readonly RetroGame _game;
    private ClimberLevel _level;
    private int _stage;
    private Overlay _overlay = Overlay.None;
    private float _t;

    public ClimberLevelViewer(RetroGame game) => _game = game;

    public void Enter() => _level = ClimberStages.Load(_stage);
    public void Leave() { }

    public void Update(float dt)
    {
        _t += dt;
        if (_game.Input.Pressed(Btn.Right)) { _stage = (_stage + 1) % ClimberStages.Count; Enter(); }
        if (_game.Input.Pressed(Btn.Left)) { _stage = (_stage + 3) % ClimberStages.Count; Enter(); }
        if (_game.Input.Pressed(Btn.Up)) _overlay = (Overlay)(((int)_overlay + 1) % 4);
        if (_game.Input.Pressed(Btn.Down)) _overlay = (Overlay)(((int)_overlay + 3) % 4);
    }

    public void Draw(SpriteBatch batch)
    {
        var f = _game.Font;
        var px = _game.Assets.Pixel;

        _level.Draw(batch, _game.Assets.Texture("tiles"), _t * 8f);

        switch (_overlay)
        {
            case Overlay.Beams: DrawBeams(batch, px, f); break;
            case Overlay.Roll: DrawRoll(batch, px); break;
            case Overlay.Reach: DrawReach(batch, px, f); break;
        }

        batch.Draw(px, new Rectangle(0, 0, W, 24), Theme.Panel * Theme.PanelAlpha);
        f.Draw(batch, _level.Name, new Vector2(6, 2), Theme.Accent);
        f.Draw(batch, "OVERLAY " + _overlay.ToString().ToUpperInvariant(), new Vector2(6, 13), Theme.Ink);

        batch.Draw(px, new Rectangle(0, H - 22, W, 22), Theme.Panel * Theme.PanelAlpha);
        f.Draw(batch, "L R STAGE   U D OVERLAY", new Vector2(6, H - 16), Theme.InkDim);
    }

    private void DrawBeams(SpriteBatch batch, Texture2D px, BitmapFont f)
    {
        foreach (var b in _level.Beams)
        {
            for (float x = b.X0; x <= b.X1; x += 1f)
                batch.Draw(px, new Rectangle((int)x, (int)b.SurfaceAt(x) - 1, 1, 1), Theme.DbgSurface);
            float g = b.Gradient;
            if (MathF.Abs(g) > 0.001f)
                f.Draw(batch, (g * 100f).ToString("+00;-00"),
                       new Vector2(b.X0 + 2, b.SurfaceAt(b.X0) - 11), Theme.Info);
        }
    }

    private void DrawRoll(SpriteBatch batch, Texture2D px)
    {
        foreach (var b in _level.Beams)
        {
            int dir = b.RollDirection;
            int phase = (int)(_t * 30) % 16;
            for (float x = b.X0 + 4; x < b.X1 - 4; x += 16)
            {
                float ax = dir > 0 ? x + phase : x - phase;
                if (ax < b.X0 || ax > b.X1) continue;
                float y = b.SurfaceAt(ax) - 6;
                batch.Draw(px, new Rectangle((int)ax, (int)y, 5, 1), Theme.DbgPath);
                batch.Draw(px, new Rectangle((int)(dir > 0 ? ax + 5 : ax - 1), (int)y - 1, 1, 3),
                           Theme.DbgPath);
            }
            // The low end is where a hazard leaves the girder.
            float lowX = b.Y1 > b.Y0 ? b.X1 : b.X0;
            batch.Draw(px, new Rectangle((int)lowX - 2, (int)b.SurfaceAt(lowX), 4, 20),
                       Theme.DbgHazard * 0.35f);
        }
    }

    private void DrawReach(SpriteBatch batch, Texture2D px, BitmapFont f)
    {
        // Apex of the fixed jump: v^2 / 2g, derived in ClimberTuning.
        float apex = ClimberTuning.JumpApex;
        foreach (var b in _level.Beams)
            for (float x = b.X0; x <= b.X1; x += 2f)
                batch.Draw(px, new Rectangle((int)x, (int)(b.SurfaceAt(x) - apex), 1, 1),
                           Theme.DbgArcHigh * 0.8f);

        f.Draw(batch, "JUMP APEX " + apex.ToString("00.0") + " PX", new Vector2(6, 30), Theme.Info);
        f.Draw(batch, "GIRDER GAP 32 PX", new Vector2(6, 41), Theme.Warn);
        f.Draw(batch, "SO YOU CANNOT JUMP UP", new Vector2(6, 52), Theme.Ink);
        f.Draw(batch, "A LEVEL - ONLY ACROSS", new Vector2(6, 63), Theme.Ink);
    }
}
