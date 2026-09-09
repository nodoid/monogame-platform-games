using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Retro.Engine;

namespace Retro.Hunch;

/// <summary>
/// Chapter 28 - Flick-Screen versus Scrolling.
///
/// The same three screens of rampart, presented both ways, with the runner under
/// your control. The pause button switches mode.
///
///   FLICK   the camera does not exist. Reach the right edge and the whole
///           picture is replaced. Every jump is composed by the designer,
///           because they know exactly what you can see when you commit to it.
///   SCROLL  a camera follows you through a dead zone with look-ahead. The world
///           is continuous, but the designer no longer controls the framing, and
///           a gap can appear from off-screen with no time to read it.
///
/// Neither is better. The original chose flick-screen, and this book keeps it,
/// because a game of exact single jumps needs the player to see the whole
/// problem before they start solving it. The scrolling code is here so you can
/// take the other road with your own game.
/// </summary>
public sealed class HunchCameraScreen : IScreen
{
    private const int W = 256, H = 192;
    private const int WorldScreens = 3;
    private const int WorldWidth = W * WorldScreens;

    // Dead zone: the camera only moves once the runner leaves this middle band.
    private const float DeadZone = 48f;
    private const float LookAhead = 40f;
    private const float CameraLag = 4.5f;

    private readonly RetroGame _game;
    private readonly HunchLevel[] _levels = new HunchLevel[WorldScreens];
    private Parallax _parallax;
    private HunchRunner _runner;

    private bool _scrolling;
    private int _flickIndex;
    private float _camX, _camTarget;
    private float _t;

    public HunchCameraScreen(RetroGame game) => _game = game;

    public void Enter()
    {
        _parallax = new Parallax(_game.Assets);
        for (int i = 0; i < WorldScreens; i++) _levels[i] = HunchScreens.Load(i + 1);
        _runner = new HunchRunner(_levels[0], _game.Assets);
    }

    public void Leave() { }

    public void Update(float dt)
    {
        _t += dt;
        if (_game.Input.Pressed(Btn.Pause))
        {
            _scrolling = !_scrolling;
            _flickIndex = 0;
            _camX = 0f;
            _runner = new HunchRunner(_levels[0], _game.Assets);
        }

        _runner.Update(dt, _game.Input, out _, out _, out _);

        if (_scrolling)
        {
            // The runner is simulated against the screen it is standing over, so
            // three flick-screens read as one continuous world.
            float worldX = _camX + _runner.Position.X;
            int over = Math.Clamp((int)(worldX / W), 0, WorldScreens - 1);
            if (_levels[over] != null && _runner.Position.X > W - 8 && over < WorldScreens - 1)
            {
                // Hand the runner to the next screen without moving it visually.
                _camX += W;
                _runner.Position.X -= W;
            }

            float focus = _runner.Position.X + (_runner.FacingLeft ? -LookAhead : LookAhead);
            if (focus > W / 2f + DeadZone) _camTarget += focus - (W / 2f + DeadZone);
            else if (focus < W / 2f - DeadZone) _camTarget += focus - (W / 2f - DeadZone);
            _camTarget = Math.Clamp(_camTarget, 0f, WorldWidth - W);
            _camX += (_camTarget - _camX) * MathF.Min(1f, dt * CameraLag);
        }
        else
        {
            if (_runner.Position.X > W - 6 && _flickIndex < WorldScreens - 1)
            {
                _flickIndex++;
                _runner = new HunchRunner(_levels[_flickIndex], _game.Assets);
                _runner.Position = new Vector2(8, _runner.Position.Y);
            }
            else if (_runner.Position.X < 6 && _flickIndex > 0)
            {
                _flickIndex--;
                _runner = new HunchRunner(_levels[_flickIndex], _game.Assets);
                _runner.Position = new Vector2(W - 10, _runner.Position.Y);
            }
        }

        if (_runner.State == RunState.Dying)
            _runner = new HunchRunner(_levels[_scrolling ? 0 : _flickIndex], _game.Assets);
    }

    public void Draw(SpriteBatch batch)
    {
        var f = _game.Font;
        var px = _game.Assets.Pixel;

        if (_scrolling)
        {
            _parallax.Draw(batch, 0, _camX, W, H);
            // Draw each screen offset by the camera. In a real scrolling game the
            // level would be one wide tile map; three stitched screens make the
            // seam visible, which is the point.
            batch.End();
            for (int i = 0; i < WorldScreens; i++)
            {
                int ox = i * W - (int)_camX;
                if (ox > W || ox < -W) continue;
                batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp,
                            null, null, null,
                            Matrix.CreateTranslation(ox, 0, 0));
                _levels[i].Draw(batch, _game.Assets.Texture("tiles"));
                batch.End();
            }
            batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp);

            // Dead zone markers.
            batch.Draw(px, new Rectangle((int)(W / 2f - DeadZone), 16, 1, H - 32), Theme.Accent * 0.4f);
            batch.Draw(px, new Rectangle((int)(W / 2f + DeadZone), 16, 1, H - 32), Theme.Accent * 0.4f);
        }
        else
        {
            _parallax.Draw(batch, _flickIndex, 0f, W, H);
            _levels[_flickIndex].Draw(batch, _game.Assets.Texture("tiles"));
        }

        _runner.Draw(batch);

        batch.Draw(px, new Rectangle(0, 0, W, 16), Theme.Panel * Theme.PanelAlpha);
        f.Draw(batch, _scrolling ? "SCROLLING CAMERA" : "FLICK SCREEN", new Vector2(4, 1),
               _scrolling ? Theme.Info : Theme.Accent);
        f.Draw(batch, _scrolling
                   ? "CAM " + ((int)_camX).ToString("D4") + "  DEAD ZONE " + (int)(DeadZone * 2)
                   : "SCREEN " + (_flickIndex + 1) + " OF " + WorldScreens,
               new Vector2(4, 9), Color.White);
        f.DrawRight(batch, "PAUSE SWITCHES", W - 4, 5, Theme.InkDim);
    }
}
