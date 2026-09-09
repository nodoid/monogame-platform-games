using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Retro.Engine;

namespace Retro.Hunch;

/// <summary>
/// RUN AND JUMP - a flick-screen run-and-jump.
///
/// The game plays in landscape at 256x192, because a runner is about the ground
/// ahead of you and a tall screen hides it. Everything that is *read* rather
/// than played - the title, the high score table, the name entry - flips to
/// portrait at 192x256, which is how a phone is held when nobody is running.
/// Both switches go through <see cref="IPlatformService"/> and a virtual
/// resolution swap; Chapter 22 works through why that is two changes and not one.
/// </summary>
public class HunchGame : RetroGame
{
    public const int LandscapeW = 256, LandscapeH = 192;
    public const int PortraitW = 192, PortraitH = 256;

    public HighScoreTable Table { get; } = new(maxNameLength: 8);
    public HighScoreStore Store { get; } = new("bellringer.scores");

    public HunchGame(IPlatformService platform) : base(platform, LandscapeW, LandscapeH) { }

    // In landscape the pad sits over the picture; in portrait it gets its own strip.
    protected override bool PadReservesSpace => VirtualHeight > VirtualWidth;

    protected override void OnLoaded()
    {
        // The window is portrait for the whole life of the app. Gameplay is
        // presented turned a quarter turn, so the player turns the device -
        // see VirtualScreen.Rotated for why the window itself never moves.
        LockOrientation(OrientationMode.Portrait);

        if (!Store.Load(Table))
            Table.SeedDefaults(new[] { "QUASIMODO", "ESMERALDA", "CLOPIN", "GRINGOIRE",
                                       "PHOEBUS", "FROLLO", "DJALI", "PIERRE" },
                               18000, 1800);
        Audio.MusicVolume = Store.MusicVolume;
        Audio.EffectVolume = Store.EffectVolume;

        EnterPortrait();
        Screens.Replace(new HunchTitleScreen(this), fade: false);
    }

    public void EnterLandscape()
    {
        SetOrientation(OrientationMode.Landscape);
        SetVirtualSize(LandscapeW, LandscapeH);
        // The runner never needs up or down, so the pad loses two thirds of its
        // area and the remaining buttons get bigger.
        Pad.ShowDpadVertical = false;
    }

    public void EnterPortrait()
    {
        SetOrientation(OrientationMode.Portrait);
        SetVirtualSize(PortraitW, PortraitH);
        Pad.ShowDpadVertical = true;
    }

    public void StartRun()
    {
        var session = new HunchSession(Table, Store)
        {
            ScreenIndex = Dbg.StartLevel % HunchScreens.Count
        };
        Screens.Replace(new HunchPlayScreen(this, session));
    }

    public void ReturnToTitle()
    {
        EnterPortrait();
        Screens.Replace(new HunchTitleScreen(this));
    }

    public void EndRun(HunchSession session)
    {
        // Rotate before the table is shown, not after - the rotation is part of
        // the transition, and doing it late shows one frame of squashed layout.
        EnterPortrait();

        if (Table.Qualifies(session.Score))
        {
            Screens.Replace(new NameEntryScreen(this, Table, Store, session.Score,
                session.ScreenNumber,
                rank => Screens.Replace(new HighScoreScreen(this, Table, "BEST RUNNERS",
                    ReturnToTitle, rank))));
        }
        else
        {
            Screens.Replace(new HunchGameOverScreen(this, session));
        }
    }
}

public sealed class HunchTitleScreen : IScreen
{
    private readonly HunchGame _game;
    private float _t;
    private Animation _run, _bell;

    public HunchTitleScreen(HunchGame game) => _game = game;

    public void Enter()
    {
        _t = 0f;
        _game.EnterPortrait();
        _run = new Animation(_game.Assets.Texture("quasi_run"), 14, 16, 10f);
        _bell = new Animation(_game.Assets.Texture("bell"), 16, 16, 4f);
        _game.Audio.PlayMusic("music_title");
        _game.Pad.Enabled = false;
    }

    public void Leave() { _game.Pad.Enabled = true; }

    public void Update(float dt)
    {
        _t += dt;
        _run.Update(dt);
        _bell.Update(dt);

        if ((_t > 0.5f && (_game.Input.Pressed(Btn.Confirm) || _game.Input.Pressed(Btn.Jump)))
            || (Dbg.AutoPlay && _t > 0.4f))
        {
            _game.Audio.Play("select", 0.9f, priority: 8);
            _game.StartRun();
            return;
        }
        if (_t > 12f)
            _game.Screens.Replace(new HighScoreScreen(_game, _game.Table, "BEST RUNNERS",
                () => _game.Screens.Replace(new HunchTitleScreen(_game)), -1, 9f));
    }

    public void Draw(SpriteBatch batch)
    {
        int w = HunchGame.PortraitW;
        var f = _game.Font;
        var tiles = _game.Assets.Texture("tiles");

        batch.Draw(_game.Assets.Texture("sky"), new Rectangle(0, 0, w, HunchGame.PortraitH),
                   new Rectangle(0, 0, 192, 192), Color.White);

        int groundY = 190;
        for (int x = 0; x < w; x += 8)
        {
            batch.Draw(tiles, new Rectangle(x, groundY, 8, 8), new Rectangle(16, 0, 8, 8), Color.White);
            for (int y = groundY + 8; y < HunchGame.PortraitH; y += 8)
                batch.Draw(tiles, new Rectangle(x, y, 8, 8), new Rectangle(0, 0, 8, 8), Color.White);
            if ((x / 8) % 3 == 0)
                batch.Draw(tiles, new Rectangle(x, groundY - 8, 8, 8), new Rectangle(8, 0, 8, 8), Color.White);
        }

        int rx = 20 + (int)((_t * 46f) % (w + 40)) - 20;
        _run.Draw(batch, new Vector2(rx, groundY), false, Color.White);
        _bell.DrawAt(batch, new Vector2(w - 40, groundY - 40), false, Color.White);

        f.DrawCentred(batch, "RUN", w / 2, 30, Theme.Accent, 2);
        f.DrawCentred(batch, "AND JUMP", w / 2, 52, Theme.Accent, 2);
        f.DrawCentred(batch, "RING EVERY BELL", w / 2, 80, Theme.Good);
        f.DrawCentred(batch, "HIGH SCORE", w / 2, 104, Theme.InkDim);
        f.DrawCentred(batch, _game.Table.Best.ToString("D6"), w / 2, 116, Theme.Ink);

        if (((int)(_t * 2) & 1) == 0)
            f.DrawCentred(batch, "TAP TO START", w / 2, 144, Theme.Ink);

        // Above the wall, not on it: grey ink on grey stone is unreadable, which
        // is invisible in the source and obvious in a screenshot.
        f.DrawCentred(batch, "3 LIVES", w / 2, 162, Theme.Ink);
        f.DrawCentred(batch, "16 SCREENS - BEAT THE CLOCK", w / 2, 174, Theme.Ink);
    }
}

public sealed class HunchGameOverScreen : IScreen
{
    private readonly HunchGame _game;
    private readonly HunchSession _session;
    private float _t;

    public HunchGameOverScreen(HunchGame game, HunchSession session)
    {
        _game = game;
        _session = session;
    }

    public void Enter() { _t = 0f; _game.EnterPortrait(); _game.Audio.StopMusic(); }
    public void Leave() { }

    public void Update(float dt)
    {
        _t += dt;
        if ((_t > 1.2f && _game.Input.Pressed(Btn.Confirm)) || _t > 6f)
            _game.Screens.Replace(new HighScoreScreen(_game, _game.Table, "BEST RUNNERS",
                _game.ReturnToTitle));
    }

    public void Draw(SpriteBatch batch)
    {
        int w = HunchGame.PortraitW;
        var f = _game.Font;
        f.DrawCentred(batch, "GAME OVER", w / 2, 96, Theme.Warn, 2);
        f.DrawCentred(batch, "SCORE " + _session.Score.ToString("D6"), w / 2, 130, Theme.Ink);
        f.DrawCentred(batch, "REACHED BELL " + _session.ScreenNumber, w / 2, 146, Theme.InkDim);
    }
}
