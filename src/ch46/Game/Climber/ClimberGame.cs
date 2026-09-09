using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Retro.Engine;

namespace Retro.Climber;

/// <summary>
/// MONKEY CLIMBER - a single-screen climber.
///
/// 224x256 virtual pixels, locked to portrait. Chapter 1 argues that this is a
/// design decision before it is a technical one: a climber is a game about
/// vertical distance, and the shape of the screen is what makes the top of the
/// structure feel far away.
///
/// Four stages in the original's order - 25m barrels, 50m conveyors, 75m
/// elevators, 100m rivets - cycling with rising difficulty.
/// </summary>
public class ClimberGame : RetroGame
{
    public const int VW = 224, VH = 256;

    public HighScoreTable Table { get; } = new(maxNameLength: 3);
    public HighScoreStore Store { get; } = new("monkeyclimber.scores");

    public ClimberGame(IPlatformService platform) : base(platform, VW, VH) { }

    protected override void OnLoaded()
    {
        LockOrientation(OrientationMode.Portrait);

        if (!Store.Load(Table))
            Table.SeedDefaults(new[] { "JMP", "APE", "ACE", "BAR", "TOP", "RUN", "BOB", "AAA" },
                               12000, 1200);
        Audio.MusicVolume = Store.MusicVolume;
        Audio.EffectVolume = Store.EffectVolume;

        Screens.Replace(new ClimberTitleScreen(this), fade: false);
    }

    /// <summary>A run starts with the intro, which is where the girders get their slope.</summary>
    public void StartRun()
    {
        var session = new ClimberSession(Table, Store) { StageIndex = Dbg.StartLevel % ClimberStages.Count };
        Screens.Replace(new ClimberIntroScreen(this, session));
    }

    public void ReturnToTitle() => Screens.Replace(new ClimberTitleScreen(this));

    public void EndRun(ClimberSession session)
    {
        if (Table.Qualifies(session.Score))
        {
            Screens.Replace(new NameEntryScreen(this, Table, Store, session.Score,
                session.StageNumber,
                rank => Screens.Replace(new HighScoreScreen(this, Table, "HIGH SCORES",
                    ReturnToTitle, rank))));
        }
        else
        {
            Screens.Replace(new ClimberGameOverScreen(this, session));
        }
    }
}

/// <summary>
/// The opening. The gorilla carries the girl up the left-hand side of the structure and
/// then stamps on it, and each stamp tilts the girders a little further until
/// they are at their full slope.
///
/// This is not decoration. The stage is *built* flat and the intro is what gives
/// it its geometry, so the first thing a player sees is the reason barrels are
/// going to roll downhill at them. Chapter 25 argues that presentation is
/// information delivered when the player has nothing else to do; this is the
/// clearest example in either game.
/// </summary>
public sealed class ClimberIntroScreen : IScreen
{
    private const int Stomps = 5;
    private const float ClimbSeconds = 2.4f;
    private const float StompInterval = 0.55f;

    private readonly ClimberGame _game;
    private readonly ClimberSession _session;

    private ClimberLevel _level;
    private Animation _gorilla, _girl;
    private float _t;
    private int _stomps;
    private float _nextStomp;
    private float _shake;

    public ClimberIntroScreen(ClimberGame game, ClimberSession session)
    {
        _game = game;
        _session = session;
    }

    public void Enter()
    {
        _level = ClimberStages.Load(_session.StageIndex);
        _gorilla = new Animation(_game.Assets.Texture("gorilla"), 32, 32, 6f);
        _girl = new Animation(_game.Assets.Texture("princess"), 16, 16, 3f);
        _t = 0f;
        _stomps = 0;
        _nextStomp = ClimbSeconds;
        _game.Audio.PlayMusic("music_title");
    }

    public void Leave() { }

    /// <summary>0 while the girders are flat, 1 once every stamp has landed.</summary>
    private float SlopeScale => Stomps == 0 ? 1f : _stomps / (float)Stomps;

    public void Update(float dt)
    {
        _t += dt;
        _gorilla.Update(dt);
        _girl.Update(dt);
        if (_shake > 0f) _shake = MathF.Max(0f, _shake - dt * 4f);

        if (_t >= _nextStomp && _stomps < Stomps)
        {
            _stomps++;
            _nextStomp = _t + StompInterval;
            _shake = 0.28f;
            _game.Audio.Play("land", 1f, pitch: -0.4f, priority: 8);
        }

        bool done = _stomps >= Stomps && _t > _nextStomp + 0.9f;
        if (done || _game.Input.Pressed(Btn.Confirm) || _game.Input.Pressed(Btn.Jump)
            || (Dbg.AutoPlay && _t > 0.6f))
            _game.Screens.Replace(new ClimberPlayScreen(_game, _session));
    }

    public void Draw(SpriteBatch batch)
    {
        var f = _game.Font;
        var tiles = _game.Assets.Texture("tiles");

        _level.Draw(batch, tiles, 0f, SlopeScale);

        // The gorilla climbs the left-hand side, then stands on the top girder.
        float climb = MathF.Min(1f, _t / ClimbSeconds);
        float kx = _level.GorillaPos.X;
        float ky = MathF.Round(232f + (_level.GorillaPos.Y - 232f) * climb);
        if (_shake > 0f) ky += 1f;

        _gorilla.DrawAt(batch, new Vector2(kx, ky), false, Color.White);
        _girl.DrawAt(batch, new Vector2(kx + 8f, ky - 14f), false, Color.White);

        if (_stomps < Stomps && climb >= 1f)
            f.DrawCentred(batch, "!", (int)kx + 16, (int)ky - 26, Theme.Warn, 2);

        f.DrawCentred(batch, "HOW HIGH CAN YOU GET", ClimberGame.VW / 2, 200, Theme.Ink);
        f.DrawCentred(batch, _level.Metres + "M", ClimberGame.VW / 2, 216, Theme.Accent, 2);
        if (((int)(_t * 2) & 1) == 0)
            f.DrawCentred(batch, "TAP TO SKIP", ClimberGame.VW / 2, 240, Theme.InkDim);
    }
}

public sealed class ClimberTitleScreen : IScreen
{
    private readonly ClimberGame _game;
    private float _t;
    private Animation _gorilla, _barrel;

    public ClimberTitleScreen(ClimberGame game) => _game = game;

    public void Enter()
    {
        _t = 0f;
        _gorilla = new Animation(_game.Assets.Texture("gorilla"), 32, 32, 2f);
        _barrel = new Animation(_game.Assets.Texture("barrel"), 16, 16, 10f);
        _game.Audio.PlayMusic("music_title");
        _game.Pad.Enabled = false;     // the whole screen is the button here
    }

    public void Leave() { _game.Pad.Enabled = true; }

    public void Update(float dt)
    {
        _t += dt;
        _gorilla.Update(dt);
        _barrel.Update(dt);

        if ((_t > 0.5f && (_game.Input.Pressed(Btn.Confirm) || _game.Input.Pressed(Btn.Jump)))
            || (Dbg.AutoPlay && _t > 0.4f))
        {
            _game.Audio.Play("select", 0.9f, priority: 8);
            _game.StartRun();
            return;
        }
        if (_t > 12f)
            _game.Screens.Replace(new HighScoreScreen(_game, _game.Table, "HIGH SCORES",
                () => _game.Screens.Replace(new ClimberTitleScreen(_game)), -1, 9f));
    }

    public void Draw(SpriteBatch batch)
    {
        var f = _game.Font;
        var tiles = _game.Assets.Texture("tiles");

        // Sloping girders drawn from the game's own tile, so the title screen
        // shows the game rather than a logo.
        for (int row = 0; row < 4; row++)
        {
            int y = 150 + row * 22;
            int x0 = row % 2 == 0 ? 12 : 36;
            int drop = row % 2 == 0 ? 1 : -1;
            int n = 0;
            for (int x = x0; x < ClimberGame.VW - 12; x += 8, n++)
                batch.Draw(tiles, new Rectangle(x, y + drop * (n / 4), 8, 8),
                           new Rectangle(0, 0, 8, 8), Color.White);
            batch.Draw(tiles, new Rectangle(x0 + 40, y - 16, 8, 8), new Rectangle(8, 0, 8, 8), Color.White);
            batch.Draw(tiles, new Rectangle(x0 + 40, y - 8, 8, 8), new Rectangle(8, 0, 8, 8), Color.White);
        }

        _gorilla.DrawAt(batch, new Vector2(26, 118), false, Color.White);
        _barrel.DrawAt(batch, new Vector2(70 + (int)((_t * 40) % 120), 134), false, Color.White);

        f.DrawCentred(batch, "MONKEY", ClimberGame.VW / 2, 24, Theme.Accent, 2);
        f.DrawCentred(batch, "CLIMBER", ClimberGame.VW / 2, 44, Theme.Accent, 2);
        f.DrawCentred(batch, "CLIMB OR DIE", ClimberGame.VW / 2, 68, Theme.Warn);
        f.DrawCentred(batch, "HIGH SCORE " + _game.Table.Best.ToString("D6"),
                      ClimberGame.VW / 2, 86, Theme.Ink);

        if (((int)(_t * 2) & 1) == 0)
            f.DrawCentred(batch, "TAP TO START", ClimberGame.VW / 2, 108, Theme.Ink);

        f.DrawCentred(batch, "3 LIVES  -  BEAT THE CLOCK", ClimberGame.VW / 2, 244, Theme.InkDim);
    }
}

public sealed class ClimberGameOverScreen : IScreen
{
    private readonly ClimberGame _game;
    private readonly ClimberSession _session;
    private float _t;

    public ClimberGameOverScreen(ClimberGame game, ClimberSession session)
    {
        _game = game;
        _session = session;
    }

    public void Enter() { _t = 0f; _game.Audio.StopMusic(); }
    public void Leave() { }

    public void Update(float dt)
    {
        _t += dt;
        if ((_t > 1.2f && _game.Input.Pressed(Btn.Confirm)) || _t > 6f)
            _game.Screens.Replace(new HighScoreScreen(_game, _game.Table, "HIGH SCORES",
                _game.ReturnToTitle));
    }

    public void Draw(SpriteBatch batch)
    {
        var f = _game.Font;
        f.DrawCentred(batch, "GAME OVER", ClimberGame.VW / 2, 100, Theme.Warn, 2);
        f.DrawCentred(batch, "SCORE " + _session.Score.ToString("D6"), ClimberGame.VW / 2, 132, Theme.Ink);
        f.DrawCentred(batch, "STAGE " + _session.StageNumber, ClimberGame.VW / 2, 148, Theme.InkDim);
    }
}
