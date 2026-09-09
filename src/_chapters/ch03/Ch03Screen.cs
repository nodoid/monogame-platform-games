using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Retro.Engine;

namespace Chapter03;

/// <summary>
/// Chapter 3 - The Game Loop and Fixed Timestep.
///
/// Two balls are thrown with identical physics. The left one integrates with the
/// real elapsed time of each frame; the right one integrates in fixed 1/60
/// slices and keeps the remainder for next frame. Tap to inject a stall - a
/// deliberate 120ms hitch, exactly what a garbage collection or a texture upload
/// does on a phone.
///
/// Watch where they land. The variable-step ball lands somewhere different every
/// time you stall it, because a big dt integrates gravity over a step that is
/// too coarse. The fixed-step ball lands in the same place every single run.
/// That reproducibility is why every number in both games is tuned against a
/// fixed step.
/// </summary>
public sealed class Ch03Screen : IScreen
{
    private const int W = 224, H = 256;
    private const float Gravity = 300f;
    private const float FixedDt = 1f / 60f;

    private readonly RetroGame _game;

    private Vector2 _variablePos, _variableVel;
    private Vector2 _fixedPos, _fixedVel;
    private float _accumulator;
    private int _fixedSteps;

    private float _stall;
    private float _resetTimer;
    private int _variableLandings, _fixedLandings;
    private float _lastVariableX, _lastFixedX;
    private readonly float[] _graph = new float[112];
    private int _graphIndex;

    public Ch03Screen(RetroGame game) => _game = game;

    public void Enter() => Throw();

    public void Leave() { }

    private void Throw()
    {
        _variablePos = _fixedPos = new Vector2(24, 96);
        _variableVel = _fixedVel = new Vector2(58f, -70f);
        _accumulator = 0f;
    }

    public void Update(float dt)
    {
        if (_game.Input.Pressed(Btn.Confirm) || _game.Input.Pressed(Btn.Jump))
            _stall = 0.12f;

        // Simulate the hitch by pretending the frame took much longer.
        if (_stall > 0f)
        {
            dt += _stall;
            _stall = 0f;
        }

        _graph[_graphIndex] = dt * 1000f;
        _graphIndex = (_graphIndex + 1) % _graph.Length;

        // ---- variable step: integrate the whole frame in one go -------------
        _variableVel.Y += Gravity * dt;
        _variablePos += _variableVel * dt;
        if (_variablePos.Y >= 200f)
        {
            _lastVariableX = _variablePos.X;
            _variableLandings++;
            _variablePos.Y = 200f;
            _variableVel = Vector2.Zero;
        }

        // ---- fixed step: consume the frame in equal slices -------------------
        _accumulator += Math.Min(dt, 0.25f);   // never spiral on a huge stall
        _fixedSteps = 0;
        while (_accumulator >= FixedDt)
        {
            _accumulator -= FixedDt;
            _fixedSteps++;
            if (_fixedPos.Y < 200f)
            {
                _fixedVel.Y += Gravity * FixedDt;
                _fixedPos += _fixedVel * FixedDt;
                if (_fixedPos.Y >= 200f)
                {
                    _lastFixedX = _fixedPos.X;
                    _fixedLandings++;
                    _fixedPos.Y = 200f;
                    _fixedVel = Vector2.Zero;
                }
            }
        }

        _resetTimer += dt;
        if (_resetTimer > 3.5f) { _resetTimer = 0f; Throw(); }
    }

    public void Draw(SpriteBatch batch)
    {
        var f = _game.Font;
        var px = _game.Assets.Pixel;

        f.DrawCentred(batch, "FIXED VS VARIABLE", W / 2, 8, Theme.Accent);
        f.DrawCentred(batch, "TAP TO STALL 120MS", W / 2, 20, Theme.InkDim);

        batch.Draw(px, new Rectangle(0, 204, W, 2), Theme.InkFaint);

        // Landing markers accumulate, so the spread is visible after a few runs.
        if (_lastVariableX > 0)
            batch.Draw(px, new Rectangle((int)_lastVariableX, 206, 2, 6), Theme.Warn);
        if (_lastFixedX > 0)
            batch.Draw(px, new Rectangle((int)_lastFixedX, 214, 2, 6), Theme.Good);

        batch.Draw(px, new Rectangle((int)_variablePos.X - 3, (int)_variablePos.Y - 3, 6, 6), Theme.Warn);
        batch.Draw(px, new Rectangle((int)_fixedPos.X - 3, (int)_fixedPos.Y - 3, 6, 6), Theme.Good);

        f.Draw(batch, "VARIABLE", new Vector2(8, 224), Theme.Warn);
        f.Draw(batch, "X " + _lastVariableX.ToString("000.0"), new Vector2(8, 234), Theme.Ink);
        f.Draw(batch, "FIXED", new Vector2(124, 224), Theme.Good);
        f.Draw(batch, "X " + _lastFixedX.ToString("000.0"), new Vector2(124, 234), Theme.Ink);

        f.Draw(batch, "STEPS THIS FRAME " + _fixedSteps, new Vector2(8, 40), Theme.InkDim);
        f.Draw(batch, "CARRY " + (_accumulator * 1000f).ToString("00.00") + " MS",
               new Vector2(8, 50), Theme.InkDim);

        // Frame-time bars: the stall shows as a single tall spike.
        int gy = 62, gh = 26;
        batch.Draw(px, new Rectangle(8, gy, _graph.Length, gh), Theme.Panel * Theme.PanelAlpha);
        for (int i = 0; i < _graph.Length; i++)
        {
            int idx = (_graphIndex + i) % _graph.Length;
            int barH = (int)Math.Min(gh, _graph[idx] * gh / 60f);
            batch.Draw(px, new Rectangle(8 + i, gy + gh - barH, 1, barH),
                       _graph[idx] > 25f ? Theme.Warn : Theme.Info);
        }
    }
}
