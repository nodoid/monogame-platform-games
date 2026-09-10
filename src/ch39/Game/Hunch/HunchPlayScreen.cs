using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Retro.Engine;

namespace Retro.Hunch;

public sealed class HunchPlayScreen : IScreen
{
    private enum Phase { Ready, Playing, Dying, Cleared }

    private readonly HunchGame _game;
    private readonly HunchSession _session;
    private readonly Random _rng = new();

    private HunchLevel _level;
    private HunchRunner _runner;
    private Parallax _parallax;
    private Bell _bell;

    private readonly List<Rope> _ropes = new();
    private readonly List<Arrow> _arrows = new();
    private readonly List<Knight> _knights = new();
    private readonly List<FireBall> _fireballs = new();
    private readonly List<FirePit> _fires = new();
    private readonly List<(string Text, Vector2 Pos, float Life)> _popups = new();

    private Phase _phase;
    private float _phaseTimer;
    private float _arrowTimer, _fireTimer;
    private float _bonusPaid;
    private float _shake;
    private float _lastWarn;

    // Set the moment this screen hands over to another. The screen manager
    // fades before it swaps and keeps updating us meanwhile, so without this
    // the hand-over would repeat on every frame of the fade.
    private bool _handedOver;

    public HunchPlayScreen(HunchGame game, HunchSession session)
    {
        _game = game;
        _session = session;
    }

    public void Enter()
    {
        _game.EnterLandscape();

        _level = HunchScreens.Load(_session.ScreenIndex);
        _runner = new HunchRunner(_level, _game.Assets);
        _parallax = new Parallax(_game.Assets);
        _bell = new Bell(_game.Assets, _level.BellPos);

        _ropes.Clear(); _arrows.Clear(); _knights.Clear(); _fireballs.Clear(); _fires.Clear();
        _popups.Clear();

        foreach (var r in _level.Ropes) _ropes.Add(new Rope(_game.Assets.Texture("rope"), r));
        foreach (var g in _level.Knights) _knights.Add(new Knight(_level, _game.Assets, g));
        foreach (var f in _level.FirePits) _fires.Add(new FirePit(_game.Assets, f));

        _session.TimeLeft = _session.TimeForScreen(_level.TimeLimit);
        _phase = Phase.Ready;
        _phaseTimer = 0f;
        _arrowTimer = 1.4f;
        _fireTimer = 2.2f;
        _bonusPaid = 0f;
        _lastWarn = 99f;

        _game.Audio.StopAllLoops();
        _game.Audio.PlayMusic("music_run");
    }

    public void Leave()
    {
        _game.Audio.StopAllLoops();
        _game.Audio.StopMusic();
    }

    public void Update(float dt)
    {
        Dbg.Tick(dt);
        _phaseTimer += dt;
        if (_shake > 0f) _shake = MathF.Max(0f, _shake - dt * 3f);

        for (int i = _popups.Count - 1; i >= 0; i--)
        {
            var p = _popups[i];
            p.Life -= dt; p.Pos.Y -= 14f * dt;
            if (p.Life <= 0f) _popups.RemoveAt(i); else _popups[i] = p;
        }

        foreach (var r in _ropes) if (!r.Held) r.Update(dt, 0f);
        foreach (var f in _fires) f.Update(dt);
        _bell.Update(dt);

        switch (_phase)
        {
            case Phase.Ready:
                if (_phaseTimer > 1.3f) { _phase = Phase.Playing; _phaseTimer = 0f; }
                break;
            case Phase.Playing: UpdatePlaying(dt); break;
            case Phase.Dying:
                _runner.Update(dt, _game.Input, out _, out _, out _);
                if (_phaseTimer > 1.8f) AfterDeath();
                break;
            case Phase.Cleared: UpdateCleared(dt); break;
        }
    }

    private void UpdatePlaying(float dt)
    {
        if (_game.Input.Pressed(Btn.Pause))
        {
            _game.Screens.Push(new PauseScreen(_game, () => _game.ReturnToTitle()));
            return;
        }

        // ---- the clock, which is the real antagonist -------------------------
        _session.TimeLeft -= dt;
        if (_session.TimeLeft <= 0f) { _session.TimeLeft = 0f; KillRunner(); return; }

        if (_session.TimeLeft <= 10f && MathF.Floor(_session.TimeLeft) != MathF.Floor(_lastWarn))
            _game.Audio.Play("timer_warn", 0.7f, priority: 4);
        _lastWarn = _session.TimeLeft;

        // Music speeds up as the clock runs out - the player hears the danger
        // before they think to look at the number.
        float urgency = 1f - Math.Clamp(_session.TimeLeft / 30f, 0f, 1f);
        _game.Audio.SetMusicPitch(urgency * 0.3f);

        // ---- runner ---------------------------------------------------------
        _runner.Update(dt, _game.Input, out bool step, out bool jumped, out bool landed);
        if (step) _game.Audio.Play("step", 0.3f, priority: 0, minGap: 0.1f);
        if (jumped) _game.Audio.Play("jump", 0.65f, priority: 3);
        if (landed) _game.Audio.Play("land", 0.5f, priority: 2);
        if (_runner.State == RunState.Dying) { BeginDeath(); return; }

        // ---- ropes ----------------------------------------------------------
        foreach (var r in _ropes)
        {
            if (r == _runner.AttachedRope) continue;
            if (_runner.State != RunState.Swing && _runner.Velocity.Y > -20f && _runner.TryGrab(r))
            {
                _game.Audio.Play("rope_grab", 0.7f, priority: 3);
                _game.Audio.Play("swing", 0.5f, priority: 1);
                break;
            }
        }

        // ---- arrows ---------------------------------------------------------
        if (_level.ArrowSlits.Count > 0)
        {
            _arrowTimer -= dt;
            if (_arrowTimer <= 0f)
            {
                _arrowTimer = 2.1f + (float)_rng.NextDouble() * 1.1f - _session.Loop * 0.25f;
                _arrowTimer = MathF.Max(0.8f, _arrowTimer);
                var slit = _level.ArrowSlits[_rng.Next(_level.ArrowSlits.Count)];
                int dir = slit.X > HunchLevel.PixelWidth / 2f ? -1 : 1;
                _arrows.Add(new Arrow(_game.Assets.Texture("arrow"),
                                      new Vector2(slit.X, slit.Y + 2), dir));
                // Pan the whistle to the side it came from: the ear locates it
                // long before the sprite is close enough to see.
                _game.Audio.Play("arrow", 0.7f, pan: dir > 0 ? -0.8f : 0.8f, priority: 5);
            }
        }
        for (int i = _arrows.Count - 1; i >= 0; i--)
        {
            _arrows[i].Update(dt);
            if (!_arrows[i].Alive) { _arrows.RemoveAt(i); continue; }
            if (_arrows[i].Bounds.Intersects(_runner.HurtBox)) { KillRunner(); return; }
        }

        // ---- bouncing fireballs ---------------------------------------------
        if (_level.FireBalls.Count > 0)
        {
            _fireTimer -= dt;
            if (_fireTimer <= 0f)
            {
                _fireTimer = MathF.Max(1.2f, 2.6f + (float)_rng.NextDouble() * 1.0f
                                             - _session.Loop * 0.25f);
                var at = _level.FireBalls[_rng.Next(_level.FireBalls.Count)];
                int dir = at.X > HunchLevel.PixelWidth / 2f ? -1 : 1;
                _fireballs.Add(new FireBall(_level, _game.Assets, at, dir));
            }
        }
        for (int i = _fireballs.Count - 1; i >= 0; i--)
        {
            _fireballs[i].Update(dt);
            if (!_fireballs[i].Alive) { _fireballs.RemoveAt(i); continue; }
            if (_fireballs[i].Bounds.Intersects(_runner.HurtBox)) { KillRunner(); return; }
        }

        // ---- knights and fire -------------------------------------------------
        foreach (var g in _knights)
        {
            g.Update(dt);
            if (g.Bounds.Intersects(_runner.HurtBox)) { KillRunner(); return; }
        }
        foreach (var f in _fires)
            if (f.Bounds.Intersects(_runner.HurtBox)) { KillRunner(); return; }

        if (_level.IsSpikeAtPixel(_runner.Position.X, _runner.Position.Y - 2)) { KillRunner(); return; }

        // ---- the bell --------------------------------------------------------
        if (!_bell.Rung && _bell.Bounds.Intersects(_runner.Bounds))
        {
            _bell.Ring();
            _game.Audio.Duck(0.25f);
            _game.Audio.Play("bell", 1f, priority: 10);
            _game.Audio.Play("screen_clear", 0.8f, priority: 9);
            Award(500, _runner.Position);
            _phase = Phase.Cleared;
            _phaseTimer = 0f;
            _runner.State = RunState.Won;
        }
    }

    private void UpdateCleared(float dt)
    {
        // Pay the remaining time out as bonus, ten points a second, with a tick.
        if (_session.TimeLeft > 0f)
        {
            float take = MathF.Min(_session.TimeLeft, dt * 26f);
            _session.TimeLeft -= take;
            _bonusPaid += take * 10f;
            if (_bonusPaid >= 10f)
            {
                int pts = (int)_bonusPaid;
                _bonusPaid -= pts;
                if (_session.Award(pts)) _game.Audio.Play("extra_life", 0.9f, priority: 8);
                _game.Audio.Play("point", 0.4f, priority: 1, minGap: 0.04f);
            }
        }
        else if (_phaseTimer > 2.2f)
        {
            if (_handedOver) return;
            _handedOver = true;
            _game.Audio.Unduck();
            _session.ScreenIndex++;
            if (_session.ScreenIndex >= HunchScreens.Count)
            {
                _session.ScreenIndex = 0;
                _session.Loop++;
            }
            _game.Screens.Replace(new HunchPlayScreen(_game, _session));
        }
    }

    private void Award(int points, Vector2 where)
    {
        if (_session.Award(points)) _game.Audio.Play("extra_life", 0.9f, priority: 8);
        _popups.Add((points.ToString(), where + new Vector2(-8, -20), 1.0f));
    }

    private void KillRunner() { _runner.Kill(); BeginDeath(); }

    private void BeginDeath()
    {
        if (_phase == Phase.Dying) return;
        _phase = Phase.Dying;
        _phaseTimer = 0f;
        _shake = 0.4f;
        _game.Audio.StopAllLoops();
        _game.Audio.StopMusic();
        _game.Audio.Play("death", 1f, priority: 10);
    }

    private void AfterDeath()
    {
        if (_handedOver) return;
        _handedOver = true;
        _session.Lives--;
        if (_session.Lives <= 0) _game.EndRun(_session);
        else _game.Screens.Replace(new HunchPlayScreen(_game, _session));
    }

    // ------------------------------------------------------------------ draw --
    public void Draw(SpriteBatch batch)
    {
        int w = HunchLevel.PixelWidth, h = HunchLevel.PixelHeight;
        _parallax.Draw(batch, _session.ScreenIndex, _runner.Position.X, w, h);
        _level.Draw(batch, _game.Assets.Texture("tiles"));

        foreach (var r in _ropes) r.Draw(batch);
        foreach (var f in _fires) f.Draw(batch);
        _bell.Draw(batch);
        foreach (var g in _knights) g.Draw(batch);
        foreach (var a in _arrows) a.Draw(batch);
        foreach (var b in _fireballs) b.Draw(batch);
        _runner.Draw(batch);

        foreach (var p in _popups)
            _game.Font.Draw(batch, p.Text, p.Pos, Theme.Ink * MathF.Min(1f, p.Life * 2f));

        DrawDebug(batch);
        DrawHud(batch);
        Dbg.Banner(batch, _game.Font, _game.Assets.Pixel, HunchLevel.PixelWidth, HunchLevel.PixelHeight);

        if (_phase == Phase.Ready)
        {
            _game.Font.DrawCentred(batch, _level.Name, w / 2, 78, Theme.Accent);
            _game.Font.DrawCentred(batch, "SCREEN " + _session.ScreenNumber + " OF " + HunchScreens.Count,
                                   w / 2, 92, Color.White);
        }
        else if (_phase == Phase.Cleared)
        {
            _game.Font.DrawCentred(batch, "BELL RUNG", w / 2, 78, Theme.Accent);
            _game.Font.DrawCentred(batch, "TIME BONUS", w / 2, 92, Theme.Ink);
        }
    }

    private void DrawDebug(SpriteBatch batch)
    {
        if (!Dbg.Any) return;
        var px = _game.Assets.Pixel;

        if (Dbg.TileGrid)
        {
            Dbg.Grid(batch, px, HunchLevel.PixelWidth, HunchLevel.PixelHeight, HunchLevel.Tile8,
                     Theme.DbgGrid * 0.35f);
            for (int r = HunchLevel.HudRows; r < HunchLevel.Rows; r++)
                for (int c = 0; c < HunchLevel.Cols; c++)
                    if (_level.IsSolid(c, r) && !_level.IsSolid(c, r - 1))
                        batch.Draw(px, new Rectangle(c * 8, r * 8, 8, 1), Theme.DbgSurface * 0.8f);
        }

        if (Dbg.JumpArc)
        {
            float vx = _runner.FacingLeft ? -HunchRunner.MaxRun : HunchRunner.MaxRun;
            // Full hold and an early release, so the reachable band is visible.
            Dbg.Arc(batch, px, _runner.Position, new Vector2(vx, HunchRunner.JumpVelocity),
                    HunchRunner.Gravity, Theme.DbgBody * 0.9f, 1.6f);
            Dbg.Arc(batch, px, _runner.Position,
                    new Vector2(vx, HunchRunner.JumpVelocity * HunchRunner.CutMultiplier),
                    HunchRunner.Gravity, Theme.DbgArcLow * 0.8f, 1.6f);
        }

        if (Dbg.Hitboxes)
        {
            Dbg.Box(batch, px, _runner.Bounds, Theme.DbgBody);
            Dbg.Box(batch, px, _runner.HurtBox, Theme.DbgPath);
            foreach (var a in _arrows) Dbg.Box(batch, px, a.Bounds, Theme.Warn);
            foreach (var g in _knights) Dbg.Box(batch, px, g.Bounds, Theme.Warn);
            foreach (var b in _fireballs) Dbg.Box(batch, px, b.Bounds, Theme.DbgHazard);
            foreach (var fp in _fires) Dbg.Box(batch, px, fp.Bounds, Theme.Warn);
            foreach (var r in _ropes) Dbg.Box(batch, px, r.GrabBox, Theme.Accent);
            Dbg.Box(batch, px, _bell.Bounds, Theme.Good);
        }

        if (Dbg.Paths)
        {
            foreach (var r in _ropes)
            {
                // The arc the rope end sweeps through.
                for (float a = -1.25f; a <= 1.25f; a += 0.05f)
                {
                    var p = r.Top + new Vector2(MathF.Sin(a), MathF.Cos(a)) * r.Length;
                    batch.Draw(px, new Rectangle((int)p.X, (int)p.Y, 1, 1), Theme.Accent * 0.6f);
                }
            }
        }

        if (Dbg.Camera)
        {
            _game.Font.Draw(batch, "SCREEN " + _session.ScreenIndex, new Vector2(6, 20), Theme.DbgSurface);
            _game.Font.Draw(batch, "FAR  " + (_session.ScreenIndex * 46), new Vector2(6, 30), Theme.DbgSurface);
            _game.Font.Draw(batch, "NEAR " + (_session.ScreenIndex * 104), new Vector2(6, 40), Theme.DbgSurface);
        }

        if (Dbg.AudioMeter)
        {
            _game.Font.Draw(batch, "ARROWS " + _arrows.Count, new Vector2(6, 20), Theme.DbgSurface);
            _game.Font.Draw(batch, "FIRE   " + _fireballs.Count, new Vector2(6, 30), Theme.DbgSurface);
        }
    }

    private void DrawHud(SpriteBatch batch)
    {
        var f = _game.Font;
        int w = HunchLevel.PixelWidth;

        f.Draw(batch, "SCORE " + _session.Score.ToString("D6"), new Vector2(4, 1), Theme.Ink);

        var life = _game.Assets.Texture("life");
        for (int i = 0; i < Math.Min(_session.Lives, 6); i++)
            batch.Draw(life, new Rectangle(114 + i * 10, 1, 8, 8), Color.White);

        int t = (int)MathF.Ceiling(_session.TimeLeft);
        var timeCol = t <= 10 ? (((int)(_session.TimeLeft * 6) & 1) == 0 ? Theme.Warn : Theme.Accent)
                              : Theme.Accent;
        f.DrawRight(batch, "TIME " + t.ToString("D2"), w - 4, 1, timeCol);

        f.Draw(batch, "HI " + Math.Max(_session.Table.Best, _session.Score).ToString("D6"),
               new Vector2(4, 9), Theme.InkDim);
        f.DrawRight(batch, "BELL " + _session.ScreenNumber.ToString("D2") + "/" + HunchScreens.Count,
                    w - 4, 9, Theme.InkDim);
    }
}
