using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Retro.Engine;

namespace Retro.Climber;

public sealed class ClimberPlayScreen : IScreen
{
    private enum Phase { Ready, Playing, Dying, Cleared }

    private readonly ClimberGame _game;
    private readonly ClimberSession _session;
    private readonly Random _rng = new();

    private ClimberLevel _level;
    private ClimberPlayer _player;
    private GorillaActor _gorilla;
    private Princess _princess;

    private readonly List<Barrel> _barrels = new();
    private readonly List<Fireball> _fireballs = new();
    private readonly List<Pie> _pies = new();
    private readonly List<Spring> _springs = new();
    private readonly List<Elevator> _elevators = new();
    private readonly List<Pickup> _hammers = new();
    private readonly List<Vector2> _rivets = new();
    private readonly List<(string Text, Vector2 Pos, float Life)> _popups = new();

    private Phase _phase;
    private float _phaseTimer;
    private float _bonusTimer;
    private float _conveyorPhase;
    private float _pieTimer, _springTimer;
    private float _shake;

    // Set the moment this screen hands over to another. The screen manager
    // fades before it swaps and keeps updating us meanwhile, so without this
    // the hand-over would repeat on every frame of the fade.
    private bool _handedOver;

    public ClimberPlayScreen(ClimberGame game, ClimberSession session)
    {
        _game = game;
        _session = session;
    }

    private bool IsRivetStage => _level.Rivets.Count > 0;
    private bool HasBarrels => _level.BarrelSpawn != Vector2.Zero;

    public void Enter()
    {
        _level = ClimberStages.Load(_session.StageIndex);
        _player = new ClimberPlayer(_level, _game.Assets);
        _gorilla = new GorillaActor(_game.Assets, _level.GorillaPos);
        _princess = new Princess(_game.Assets, _level.PrincessPos);

        _barrels.Clear(); _fireballs.Clear(); _pies.Clear(); _springs.Clear();
        _elevators.Clear(); _hammers.Clear(); _rivets.Clear(); _popups.Clear();

        foreach (var f in _level.FireballSpots) _fireballs.Add(new Fireball(_level, _game.Assets, f));
        foreach (var h in _level.HammerSpots)
            _hammers.Add(new Pickup(_game.Assets.Texture("hammer"), h));
        foreach (var e in _level.Elevators) _elevators.Add(new Elevator(e.Bottom, e.Height, e.Speed));
        _rivets.AddRange(_level.Rivets);

        _session.Bonus = _level.StartBonus;
        _bonusTimer = 0f;
        _pieTimer = 1.5f;
        _springTimer = 2.0f;
        _phase = Phase.Ready;
        _phaseTimer = 0f;

        // Warm the texture cache during the ready phase rather than mid-play -
        // chapter 43's one optimisation that is genuinely worth doing.
        foreach (var name in new[] { "barrel", "fireball", "hammer", "pie", "spring", "gorilla" })
            _game.Assets.Texture(name);

        _game.Audio.StopAllLoops();
        _game.Audio.PlayMusic("music_stage");
    }

    public void Leave()
    {
        _game.Audio.StopAllLoops();
        _game.Audio.StopMusic();
    }

    public void Update(float dt)
    {
        Dbg.Tick(dt);
        _conveyorPhase += dt * 8f;
        _phaseTimer += dt;
        if (_shake > 0f) _shake = MathF.Max(0f, _shake - dt * 3f);

        for (int i = _popups.Count - 1; i >= 0; i--)
        {
            var p = _popups[i];
            p.Life -= dt; p.Pos.Y -= 14f * dt;
            if (p.Life <= 0f) _popups.RemoveAt(i); else _popups[i] = p;
        }

        // Elevators publish their beams before anything asks about the ground.
        _level.Dynamic.Clear();
        foreach (var e in _elevators) { e.Update(dt); _level.Dynamic.Add(e.AsBeam()); }

        switch (_phase)
        {
            case Phase.Ready:
                if (_phaseTimer > 1.6f) { _phase = Phase.Playing; _phaseTimer = 0f; }
                break;
            case Phase.Playing: UpdatePlaying(dt); break;
            case Phase.Dying:
                _player.Update(dt, _game.Input, out _, out _, out _);
                if (_phaseTimer > 2.0f) AfterDeath();
                break;
            case Phase.Cleared: UpdateCleared(); break;
        }
    }

    private void UpdatePlaying(float dt)
    {
        if (_game.Input.Pressed(Btn.Pause))
        {
            _game.Screens.Push(new PauseScreen(_game, () => _game.ReturnToTitle()));
            return;
        }

        // ---- bonus timer: the soft clock that pushes the player upward -------
        _bonusTimer += dt;
        while (_bonusTimer >= 1.0f)
        {
            _bonusTimer -= 1.0f;
            _session.Bonus = Math.Max(0, _session.Bonus - 100);
            if (_session.Bonus == 0) { KillPlayer(); return; }
            if (_session.Bonus <= 1000 && _session.Bonus % 200 == 0)
                _game.Audio.Play("timer_warn", 0.6f, priority: 3);
        }
        float urgency = 1f - _session.Bonus / (float)Math.Max(1, _level.StartBonus);
        _game.Audio.SetMusicPitch(urgency * 0.25f);

        // ---- player ---------------------------------------------------------
        _player.Update(dt, _game.Input, out bool step, out bool jumped, out bool landed);
        if (step) _game.Audio.Play("step", 0.35f, priority: 0, minGap: 0.12f);
        if (jumped) _game.Audio.Play("jump", 0.7f, priority: 2);
        if (landed) _game.Audio.Play("land", 0.5f, priority: 1);
        if (_player.State == JackState.Dying) { BeginDeath(); return; }

        // ---- the gorilla and his barrels -------------------------------------------
        if (HasBarrels && _gorilla.Update(dt, _session.BarrelInterval))
        {
            var spawn = _level.BarrelSpawn;
            int dir = _level.TryGetGround(spawn.X, spawn.Y, out var b, out _) ? b.RollDirection : 1;
            _barrels.Add(new Barrel(_level, _game.Assets, spawn, dir));
        }
        else if (!HasBarrels) _gorilla.Update(dt, 999f);

        // ---- pies (50m) ------------------------------------------------------
        if (_level.PieSpawns.Count > 0)
        {
            _pieTimer -= dt;
            if (_pieTimer <= 0f)
            {
                _pieTimer = MathF.Max(1.1f, 2.4f - _session.Loop * 0.3f);
                var at = _level.PieSpawns[_rng.Next(_level.PieSpawns.Count)];
                int dir = at.X < ClimberLevel.PixelWidth / 2f ? 1 : -1;
                _pies.Add(new Pie(_level, _game.Assets, at, dir));
            }
        }

        // ---- springs (75m) ---------------------------------------------------
        if (_level.SpringSpawns.Count > 0)
        {
            _springTimer -= dt;
            if (_springTimer <= 0f)
            {
                _springTimer = MathF.Max(1.4f, 2.8f - _session.Loop * 0.3f);
                _springs.Add(new Spring(_level, _game.Assets,
                                        _level.SpringSpawns[_rng.Next(_level.SpringSpawns.Count)]));
            }
        }

        if (UpdateHazards(dt)) return;

        // ---- hammers ---------------------------------------------------------
        foreach (var h in _hammers)
        {
            h.Update(dt);
            if (!h.Taken && h.Bounds.Intersects(_player.Bounds))
            {
                h.Taken = true;
                _player.GiveHammer();
                _game.Audio.Play("hammer_get", 0.9f, priority: 5);
            }
        }

        // ---- the goal --------------------------------------------------------
        if (IsRivetStage)
        {
            for (int i = _rivets.Count - 1; i >= 0; i--)
            {
                var r = _rivets[i];
                var box = new Rectangle((int)r.X - 2, (int)r.Y - 10, 12, 12);
                if (box.Intersects(_player.Bounds))
                {
                    _rivets.RemoveAt(i);
                    Award(150, r);
                    _game.Audio.Play("point", 0.8f, priority: 3);
                }
            }
            if (_rivets.Count == 0) Clear();
        }
        else if (_player.Bounds.Intersects(_princess.Bounds)) Clear();

        _princess.Update(dt);
    }

    /// <summary>Every moving hazard. Returns true if the player died.</summary>
    private bool UpdateHazards(float dt)
    {
        float loudest = 0f;
        for (int i = _barrels.Count - 1; i >= 0; i--)
        {
            var b = _barrels[i];
            b.Update(dt, _rng, _player.Position.X, _player.Position.Y, _session.LadderChance);
            if (!b.Alive) { _barrels.RemoveAt(i); continue; }

            if (!b.ScoredJump && _player.State == JackState.Jump &&
                Math.Abs(b.Position.X - _player.Position.X) < 12 &&
                _player.Position.Y < b.Position.Y - 6)
            {
                b.ScoredJump = true;
                Award(100, _player.Position);
            }

            var hammer = _player.HammerBounds;
            if (hammer.HasValue && hammer.Value.Intersects(b.Bounds))
            {
                _barrels.RemoveAt(i);
                Smash(b.Wild ? 800 : 500, b.Position);
                continue;
            }
            if (b.Bounds.Intersects(_player.Bounds)) { KillPlayer(); return true; }

            float d = Vector2.Distance(b.Position, _player.Position);
            loudest = MathF.Max(loudest, 1f - MathF.Min(1f, d / 130f));
        }

        if (_barrels.Count > 0 && loudest > 0.05f)
        {
            float pan = Math.Clamp((AverageBarrelX() - _player.Position.X) / 90f, -1f, 1f);
            _game.Audio.Loop("barrel", 0.30f * loudest, pitch: -0.15f + loudest * 0.3f, pan: pan);
        }
        else _game.Audio.StopLoop("barrel");

        for (int i = _fireballs.Count - 1; i >= 0; i--)
        {
            var f = _fireballs[i];
            f.Update(dt, _rng, _player.Position);
            var hammer = _player.HammerBounds;
            if (hammer.HasValue && hammer.Value.Intersects(f.Bounds))
            {
                _fireballs.RemoveAt(i); Smash(800, f.Position); continue;
            }
            if (f.Bounds.Intersects(_player.Bounds)) { KillPlayer(); return true; }
        }

        for (int i = _pies.Count - 1; i >= 0; i--)
        {
            var p = _pies[i];
            p.Update(dt);
            if (!p.Alive) { _pies.RemoveAt(i); continue; }
            var hammer = _player.HammerBounds;
            if (hammer.HasValue && hammer.Value.Intersects(p.Bounds))
            {
                _pies.RemoveAt(i); Smash(500, p.Position); continue;
            }
            if (p.Bounds.Intersects(_player.Bounds)) { KillPlayer(); return true; }
        }

        for (int i = _springs.Count - 1; i >= 0; i--)
        {
            var s = _springs[i];
            s.Update(dt);
            if (!s.Alive) { _springs.RemoveAt(i); continue; }
            // Springs cannot be hammered. Nothing in the original could stop one.
            if (s.Bounds.Intersects(_player.Bounds)) { KillPlayer(); return true; }
        }

        return false;
    }

    private void UpdateCleared()
    {
        if (_session.Bonus > 0 && _phaseTimer > 0.7f)
        {
            int stepPts = Math.Min(100, _session.Bonus);
            _session.Bonus -= stepPts;
            if (_session.Award(stepPts)) _game.Audio.Play("extra_life", 0.9f, priority: 5);
            _game.Audio.Play("point", 0.5f, priority: 1, minGap: 0.02f);
        }
        else if (_session.Bonus <= 0 && _phaseTimer > 2.6f)
        {
            if (_handedOver) return;
            _handedOver = true;
            _session.AdvanceStage();
            _game.Screens.Replace(new ClimberPlayScreen(_game, _session));
        }
    }

    private float AverageBarrelX()
    {
        float sum = 0f;
        foreach (var b in _barrels) sum += b.Position.X;
        return _barrels.Count == 0 ? 0f : sum / _barrels.Count;
    }

    private void Award(int points, Vector2 where)
    {
        if (_session.Award(points)) _game.Audio.Play("extra_life", 0.9f, priority: 6);
        else _game.Audio.Play("point", 0.6f, priority: 2);
        _popups.Add((points.ToString(), where + new Vector2(-6, -18), 0.9f));
    }

    private void Smash(int points, Vector2 where)
    {
        Award(points, where);
        _game.Audio.Play("hammer_hit", 0.9f, priority: 4);
        _shake = 0.25f;
    }

    private void KillPlayer() { _player.Kill(); BeginDeath(); }

    private void BeginDeath()
    {
        if (_phase == Phase.Dying) return;
        _phase = Phase.Dying;
        _phaseTimer = 0f;
        _game.Audio.StopAllLoops();
        _game.Audio.StopMusic();
        _game.Audio.Play("death", 1f, priority: 10);
        _shake = 0.4f;
    }

    private void AfterDeath()
    {
        if (_handedOver) return;
        _handedOver = true;
        _session.Lives--;
        if (_session.Lives <= 0) _game.EndRun(_session);
        else _game.Screens.Replace(new ClimberPlayScreen(_game, _session));
    }

    private void Clear()
    {
        if (_phase == Phase.Cleared) return;
        _phase = Phase.Cleared;
        _phaseTimer = 0f;
        _player.State = JackState.Won;
        _game.Audio.StopAllLoops();
        _game.Audio.StopMusic();
        _game.Audio.Play("stage_clear", 1f, priority: 10);
    }

    // ------------------------------------------------------------------ draw --
    public void Draw(SpriteBatch batch)
    {
        var tiles = _game.Assets.Texture("tiles");
        _level.Draw(batch, tiles, _conveyorPhase);

        foreach (var e in _elevators) e.Draw(batch, tiles);
        if (_level.HasOilDrum)
            batch.Draw(tiles, new Rectangle((int)_level.OilDrum.X, (int)_level.OilDrum.Y, 8, 8),
                       new Rectangle(6 * 8, 0, 8, 8), Color.White);

        foreach (var h in _hammers) h.Draw(batch);
        _princess.Draw(batch);
        _gorilla.Draw(batch);
        foreach (var f in _fireballs) f.Draw(batch);
        foreach (var p in _pies) p.Draw(batch);
        foreach (var s in _springs) s.Draw(batch);
        foreach (var b in _barrels) b.Draw(batch);
        _player.Draw(batch);

        foreach (var p in _popups)
            _game.Font.Draw(batch, p.Text, p.Pos, Theme.Ink * MathF.Min(1f, p.Life * 2f));

        DrawDebug(batch);
        DrawHud(batch);
        Dbg.Banner(batch, _game.Font, _game.Assets.Pixel, ClimberLevel.PixelWidth, ClimberLevel.PixelHeight);

        if (_phase == Phase.Ready)
        {
            _game.Font.DrawCentred(batch, _level.Name, ClimberLevel.PixelWidth / 2, 120, Theme.Accent);
            _game.Font.DrawCentred(batch, "GET READY", ClimberLevel.PixelWidth / 2, 140, Theme.Warn);
        }
        else if (_phase == Phase.Cleared)
        {
            _game.Font.DrawCentred(batch, "STAGE CLEAR", ClimberLevel.PixelWidth / 2, 120, Theme.Accent);
        }
    }

    private void DrawDebug(SpriteBatch batch)
    {
        if (!Dbg.Any) return;
        var px = _game.Assets.Pixel;

        if (Dbg.TileGrid || Dbg.Paths)
        {
            foreach (var b in _level.Beams)
            {
                for (float x = b.X0; x <= b.X1; x += 2)
                    batch.Draw(px, new Rectangle((int)x, (int)b.SurfaceAt(x) - 1, 2, 1),
                               Theme.DbgSurface * 0.9f);
                if (Dbg.Paths)
                {
                    int dir = b.RollDirection;
                    for (float x = b.X0 + 6; x < b.X1; x += 14)
                    {
                        float y = b.SurfaceAt(x) - 6;
                        batch.Draw(px, new Rectangle((int)x, (int)y, 5, 1), Theme.DbgPath);
                        batch.Draw(px, new Rectangle((int)(dir > 0 ? x + 5 : x - 1), (int)y - 1, 1, 3),
                                   Theme.DbgPath);
                    }
                }
            }
        }

        if (Dbg.Ladders)
        {
            for (int r = ClimberLevel.HudRows; r < ClimberLevel.Rows; r++)
                for (int c = 0; c < ClimberLevel.Cols; c++)
                    if (_level.IsLadder(c, r))
                        batch.Draw(px, new Rectangle(c * 8 + 2, r * 8, 4, 8), Theme.DbgReward * 0.45f);
            _game.Font.Draw(batch, "LADDER ODDS " + (_session.LadderChance * 100f).ToString("00") + "%",
                            new Vector2(6, 26), Theme.DbgSurface);
        }

        if (Dbg.JumpArc)
        {
            float dirX = _player.FacingLeft ? -ClimberPlayer.WalkSpeed : ClimberPlayer.WalkSpeed;
            Dbg.Arc(batch, px, _player.Position, new Vector2(dirX, ClimberPlayer.JumpVelocity),
                    ClimberPlayer.Gravity, Theme.DbgArcHigh * 0.9f);
            Dbg.Arc(batch, px, _player.Position, new Vector2(0f, ClimberPlayer.JumpVelocity),
                    ClimberPlayer.Gravity, Theme.DbgArcLow * 0.8f);
        }

        if (Dbg.Hitboxes)
        {
            Dbg.Box(batch, px, _player.Bounds, Theme.DbgBody);
            var h = _player.HammerBounds;
            if (h.HasValue) Dbg.Box(batch, px, h.Value, Theme.DbgPath);
            foreach (var b in _barrels) Dbg.Box(batch, px, b.Bounds, Theme.DbgHazard);
            foreach (var fb in _fireballs) Dbg.Box(batch, px, fb.Bounds, Theme.DbgHazard);
            foreach (var p in _pies) Dbg.Box(batch, px, p.Bounds, Theme.DbgHazard);
            foreach (var s in _springs) Dbg.Box(batch, px, s.Bounds, Theme.DbgHazard);
            foreach (var hm in _hammers) if (!hm.Taken) Dbg.Box(batch, px, hm.Bounds, Theme.DbgPath);
            if (!IsRivetStage) Dbg.Box(batch, px, _princess.Bounds, Theme.DbgReward);
        }

        if (Dbg.AudioMeter)
        {
            _game.Font.Draw(batch, "BARRELS " + _barrels.Count, new Vector2(6, 26), Theme.DbgSurface);
            _game.Font.Draw(batch, "FIRE    " + _fireballs.Count, new Vector2(6, 36), Theme.DbgSurface);
            _game.Font.Draw(batch, "PIES    " + _pies.Count, new Vector2(6, 46), Theme.DbgSurface);
        }
    }

    private void DrawHud(SpriteBatch batch)
    {
        var f = _game.Font;
        int best = Math.Max(_session.Table.Best, _session.Score);

        f.Draw(batch, "1UP", new Vector2(8, 0), Theme.Warn);
        f.Draw(batch, _session.Score.ToString("D6"), new Vector2(8, 8), Theme.Ink);

        f.Draw(batch, "HIGH SCORE", new Vector2(84, 0), Theme.Warn);
        f.Draw(batch, best.ToString("D6"), new Vector2(102, 8), Theme.Ink);

        var life = _game.Assets.Texture("life");
        for (int i = 0; i < Math.Min(_session.Lives, 6); i++)
            batch.Draw(life, new Rectangle(8 + i * 10, 16, 8, 8), Color.White);

        f.DrawRight(batch, _level.Metres + "M", 152, 16, Theme.InkDim);
        f.DrawRight(batch, "BONUS " + _session.Bonus.ToString("D4"), 216, 16,
                    _session.Bonus <= 1000 ? Theme.Warn : Theme.Accent);
    }
}
