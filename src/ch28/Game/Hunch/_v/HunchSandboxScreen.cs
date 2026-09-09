using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Retro.Engine;

namespace Retro.Hunch;

/// <summary>
/// The playable sandbox for chapters 28 to 31: one screen of rampart, the
/// runner, and nothing that can kill you.
///
/// Left and right pick a screen from the fifteen, the pause button cycles the
/// debug view. Chapters 30 and 31 compile different versions of
/// <see cref="HunchRunner"/> into it so you can feel the jump being added.
/// </summary>
public sealed class HunchSandboxScreen : IScreen
{
    private const int W = 256, H = 192;
    private enum View { None, Tiles, Speed }

    private readonly RetroGame _game;
    private readonly string _caption;

    private HunchLevel _level;
    private HunchRunner _runner;
    private Parallax _parallax;
    private int _index;
    private View _view;
    private float _t;
    private float _peakSpeed;

    public HunchSandboxScreen(RetroGame game, string caption)
    {
        _game = game;
        _caption = caption;
    }

    public void Enter()
    {
        _parallax = new Parallax(_game.Assets);
        Reload();
    }

    public void Leave() { }

    private void Reload()
    {
        _level = HunchScreens.Load(_index);
        _runner = new HunchRunner(_level, _game.Assets);
        _peakSpeed = 0f;
    }

    public void Update(float dt)
    {
        _t += dt;
        if (_game.Input.Pressed(Btn.Up)) { _index = (_index + 1) % HunchScreens.Count; Reload(); }
        if (_game.Input.Pressed(Btn.Down))
        {
            _index = (_index + HunchScreens.Count - 1) % HunchScreens.Count;
            Reload();
        }
        if (_game.Input.Pressed(Btn.Pause)) _view = (View)(((int)_view + 1) % 3);

        _runner.Update(dt, _game.Input, out _, out _, out _);
        _peakSpeed = MathF.Max(_peakSpeed, MathF.Abs(_runner.Velocity.X));
        if (_runner.State == RunState.Dying) Reload();
    }

    public void Draw(SpriteBatch batch)
    {
        var f = _game.Font;
        var px = _game.Assets.Pixel;

        _parallax.Draw(batch, _index, _runner.Position.X, W, H);
        _level.Draw(batch, _game.Assets.Texture("tiles"));

        if (_view == View.Tiles)
        {
            Dbg.Grid(batch, px, W, H, HunchLevel.Tile8, Theme.DbgGrid * 0.35f);
            for (int r = HunchLevel.HudRows; r < HunchLevel.Rows; r++)
                for (int c = 0; c < HunchLevel.Cols; c++)
                    if (_level.IsSolid(c, r) && !_level.IsSolid(c, r - 1))
                        batch.Draw(px, new Rectangle(c * 8, r * 8, 8, 1), Theme.DbgSurface);
            Dbg.Box(batch, px, _runner.Bounds, Theme.DbgBody);
        }

        _runner.Draw(batch);

        batch.Draw(px, new Rectangle(0, 0, W, 16), Theme.Panel * Theme.PanelAlpha);
        f.Draw(batch, _caption, new Vector2(4, 1), Theme.Accent);
        f.Draw(batch, "SCREEN " + (_index + 1) + " " + _level.Name, new Vector2(4, 9), Theme.Ink);

        if (_view == View.Speed)
        {
            f.DrawRight(batch, "VX " + _runner.Velocity.X.ToString("+000;-000;0000"), W - 4, 1, Theme.DbgSurface);
            f.DrawRight(batch, "PEAK " + _peakSpeed.ToString("000"), W - 4, 9, Theme.DbgSurface);
            // A speed bar: watch it fill on a press and drain on a release.
            int bar = (int)(MathF.Abs(_runner.Velocity.X) / HunchRunner.MaxRun * 90f);
            batch.Draw(px, new Rectangle(W - 100, 20, 90, 4), Theme.Panel * Theme.PanelAlpha);
            batch.Draw(px, new Rectangle(W - 100, 20, Math.Clamp(bar, 0, 90), 4), Theme.Info);
        }
        else
        {
            f.DrawRight(batch, "U D SCREEN", W - 4, 1, Theme.InkDim);
            f.DrawRight(batch, "PAUSE VIEW", W - 4, 9, Theme.InkDim);
        }
    }
}

/// <summary>Host for chapters 27 to 31: landscape, no menus, no clock.</summary>
public class HunchSandboxGame : RetroGame
{
    public const int VW = 256, VH = 192;

    private readonly Func<RetroGame, IScreen> _factory;

    public HunchSandboxGame(IPlatformService platform, Func<RetroGame, IScreen> factory)
        : base(platform, VW, VH)
    {
        _factory = factory;
    }

    protected override bool PadReservesSpace => false;

    protected override void OnLoaded()
    {
        LockOrientation(OrientationMode.Landscape);
        Pad.ShowDpadVertical = true;
        Screens.Replace(_factory(this), fade: false);
    }
}
