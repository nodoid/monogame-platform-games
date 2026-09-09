using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Retro.Engine;

namespace Retro.Climber;

/// <summary>
/// The playable sandbox used by chapters 14, 15 and 16.
///
/// The stage and the character are real; the hazards are not there yet. Each of
/// those three chapters compiles a different version of <see cref="ClimberPlayer"/>
/// into this same screen, so you can feel exactly what each capability adds:
/// walking a sloped girder, then climbing, then the jump.
///
/// The pause button toggles a debug view showing the beam surface under the
/// character's feet, which is the only reliable way to understand why a platform
/// character is standing where it is standing.
/// </summary>
public sealed class ClimberSandboxScreen : IScreen
{
    private const int W = 224, H = 256;

    private readonly RetroGame _game;
    private readonly string _caption;

    private ClimberLevel _level;
    private ClimberPlayer _player;
    private int _stage;
    private bool _debug;
    private float _t;

    public ClimberSandboxScreen(RetroGame game, string caption)
    {
        _game = game;
        _caption = caption;
    }

    public void Enter() => Reload();
    public void Leave() { }

    private void Reload()
    {
        _level = ClimberStages.Load(_stage);
        _player = new ClimberPlayer(_level, _game.Assets);
    }

    public void Update(float dt)
    {
        _t += dt;
        if (_game.Input.Pressed(Btn.Pause)) _debug = !_debug;
        if (_game.Input.Pressed(Btn.Confirm) && _game.Input.Down(Btn.Up))
        { _stage = (_stage + 1) % ClimberStages.Count; Reload(); return; }

        _player.Update(dt, _game.Input, out _, out _, out _);
        if (_player.State == JackState.Dying) Reload();
    }

    public void Draw(SpriteBatch batch)
    {
        var f = _game.Font;
        var px = _game.Assets.Pixel;

        _level.Draw(batch, _game.Assets.Texture("tiles"), _t * 8f);

        if (_debug)
        {
            foreach (var b in _level.Beams)
                for (float x = b.X0; x <= b.X1; x += 2f)
                    batch.Draw(px, new Rectangle((int)x, (int)b.SurfaceAt(x) - 1, 1, 1),
                               Theme.DbgSurface * 0.8f);

            // Where the feet are, and the beam surface they are seated on.
            int fx = (int)_player.Position.X, fy = (int)_player.Position.Y;
            bool grounded = _level.TryGetGround(fx, fy, out var beam, out float surface);
            batch.Draw(px, new Rectangle(fx - 1, fy - 1, 3, 3),
                       grounded ? Theme.DbgSurface : Theme.DbgHazard);
            if (grounded)
            {
                batch.Draw(px, new Rectangle(fx - 12, (int)surface, 24, 1), Theme.DbgBody);
                f.Draw(batch, "GRAD " + (beam.Gradient * 100f).ToString("+00;-00;000"),
                       new Vector2(6, 26), Theme.Info);
            }
            Dbg.Box(batch, px, _player.Bounds, Theme.DbgBody);
        }

        _player.Draw(batch);

        batch.Draw(px, new Rectangle(0, 0, W, 24), Theme.Panel * Theme.PanelAlpha);
        f.Draw(batch, _caption, new Vector2(6, 2), Theme.Accent);
        f.Draw(batch, "STATE " + _player.State.ToString().ToUpperInvariant(),
               new Vector2(6, 13), Theme.Ink);
        f.DrawRight(batch, _debug ? "DEBUG ON" : "DEBUG OFF", W - 6, 13,
                    _debug ? Theme.DbgSurface : Theme.InkDim);
    }
}
