using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Retro.Engine;

/// <summary>
/// Maps a small fixed "virtual" resolution onto whatever physical screen the
/// device happens to have, using an integer scale wherever one fits.
///
/// Why: both games are built from 8-pixel tiles. If we drew at the device's
/// native resolution the art would have to be authored at a dozen sizes, and
/// non-integer scaling would smear the pixels. Instead we pretend the screen is
/// always (say) 224x256, draw everything there, and blit that up with point
/// sampling. Every phone then shows identical gameplay geometry - a jump that
/// clears a gap on one device clears it on all of them.
/// </summary>
public sealed class VirtualScreen
{
    public int Width { get; }
    public int Height { get; }

    private readonly GraphicsDevice _device;
    private RenderTarget2D _target;
    private Rectangle _destination;
    private float _scale = 1f;

    public VirtualScreen(GraphicsDevice device, int width, int height)
    {
        _device = device;
        Width = width;
        Height = height;
        _target = new RenderTarget2D(device, width, height, false,
            SurfaceFormat.Color, DepthFormat.None, 0, RenderTargetUsage.PreserveContents);
        Recalculate();
    }

    /// <summary>The letterboxed rectangle the virtual screen occupies on the device.</summary>
    public Rectangle Destination => _destination;

    /// <summary>Pixels of black bar, useful when laying out safe-area HUD elements.</summary>
    public Point Letterbox => new(_destination.X, _destination.Y);

    /// <summary>
    /// Device pixels to keep clear at the bottom for the on-screen pad. The game
    /// picture is fitted into what is left and pushed to the top of it, so a
    /// thumb never covers the action.
    /// </summary>
    public int ReserveBottom { get; set; }

    /// <summary>
    /// Present the virtual screen turned a quarter turn.
    ///
    /// The runner plays in landscape and shows its menus in portrait, and the
    /// obvious way to do that is to ask the operating system to rotate the
    /// window. That does not work reliably: on iOS the scene rotates and the
    /// graphics back buffer does not follow it, so the picture ends up drawn
    /// sideways at a fraction of the scale in a corner. It is also two
    /// different APIs on the two platforms and asynchronous on both.
    ///
    /// So the app locks its window to one orientation and the engine turns the
    /// picture itself. The player turns the device; the window never moves.
    /// One code path, immediate, and it cannot be half-applied.
    /// </summary>
    public bool Rotated { get; set; }

    /// <summary>
    /// The real render surface. `PresentationParameters` goes stale on iOS after
    /// a programmatic orientation change - it keeps reporting the pre-rotation
    /// size, which lays the picture out at a fraction of the scale in the wrong
    /// corner. The viewport is what the device is actually drawing into.
    /// </summary>
    public static (int W, int H) SurfaceOf(GraphicsDevice device)
    {
        int w = device.Viewport.Width, h = device.Viewport.Height;
        if (w <= 1 || h <= 1)
        {
            var pp = device.PresentationParameters;
            w = pp.BackBufferWidth;
            h = pp.BackBufferHeight;
        }
        return (w, h);
    }

    public void Recalculate()
    {
        var (bw, bh) = SurfaceOf(_device);
        int sw = bw, sh = Math.Max(1, bh - ReserveBottom);

        // A turned picture occupies the swapped shape on screen.
        int vw = Rotated ? Height : Width;
        int vh = Rotated ? Width : Height;

        // Prefer a whole-number scale: 3x looks crisp, 3.4x does not.
        float raw = MathF.Min(sw / (float)vw, sh / (float)vh);
        float scale = raw >= 1f ? MathF.Floor(raw) : raw;

        // On very short screens flooring can waste a lot of space. If more than
        // an eighth of the available area would be lost, take the fractional
        // scale instead - a slightly soft image beats a tiny one.
        if (raw >= 1f && (scale / raw) < 0.875f)
            scale = raw;

        _scale = scale;
        int w = (int)(vw * scale), h = (int)(vh * scale);
        // Horizontally centred, but biased towards the top of the usable area so
        // the reserved control strip eats the empty space rather than the game.
        int y = ReserveBottom > 0 ? (sh - h) / 4 : (sh - h) / 2;
        _destination = new Rectangle((sw - w) / 2, y, w, h);
    }

    public void Begin()
    {
        _device.SetRenderTarget(_target);
        _device.Clear(Theme.Background);
    }

    public void End(SpriteBatch batch, Color? bars = null)
    {
        _device.SetRenderTarget(null);
        _device.Clear(bars ?? Theme.Bars);
        batch.Begin(SpriteSortMode.Deferred, BlendState.Opaque, SamplerState.PointClamp);
        if (!Rotated)
        {
            batch.Draw(_target, _destination, Color.White);
        }
        else
        {
            // Turn about the destination's centre, with the origin at the
            // target's centre, so the swapped shape lands exactly on it.
            var centre = new Vector2(_destination.X + _destination.Width / 2f,
                                     _destination.Y + _destination.Height / 2f);
            batch.Draw(_target, centre, null, Color.White, MathHelper.PiOver2,
                       new Vector2(Width / 2f, Height / 2f), _scale, SpriteEffects.None, 0f);
        }
        batch.End();
    }

    /// <summary>Convert a touch/mouse point on the device into virtual-screen space.</summary>
    public Vector2 ToVirtual(Vector2 screenPoint)
    {
        if (_destination.Width == 0 || _destination.Height == 0 || _scale <= 0f)
            return Vector2.Zero;

        var centre = new Vector2(_destination.X + _destination.Width / 2f,
                                 _destination.Y + _destination.Height / 2f);
        var d = (screenPoint - centre) / _scale;
        // Inverse of the quarter turn applied in End().
        if (Rotated) d = new Vector2(d.Y, -d.X);
        return d + new Vector2(Width / 2f, Height / 2f);
    }

    public void Dispose() => _target?.Dispose();
}
