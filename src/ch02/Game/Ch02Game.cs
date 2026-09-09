using System;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Retro.Engine;

namespace Chapter02;

/// <summary>
/// Chapter 2 - Setting Up the Project.
///
/// A device report. Every number on this screen is one you will need later and
/// cannot guess: the real back buffer size, the aspect ratio, the safe-area
/// insets carved out by a notch or a home bar, and what the frame time actually
/// is on this hardware rather than what the target says it is.
///
/// Run it on the oldest phone you can find before tuning anything.
/// </summary>
public class Ch02Game : Game
{
    private readonly GraphicsDeviceManager _graphics;
    private readonly IPlatformService _platform;

    private SpriteBatch _batch;
    private Assets _assets;
    private BitmapFont _font;

    private readonly float[] _frameMs = new float[120];
    private int _frameIndex;
    private float _worst, _average;

    public Ch02Game(IPlatformService platform)
    {
        _platform = platform ?? new NullPlatformService();
        _graphics = new GraphicsDeviceManager(this)
        {
            IsFullScreen = true,
            SynchronizeWithVerticalRetrace = true
        };
        IsFixedTimeStep = true;
        TargetElapsedTime = TimeSpan.FromTicks(TimeSpan.TicksPerSecond / 60);
        Content.RootDirectory = "Content";
        IsMouseVisible = false;
    }

    protected override void LoadContent()
    {
        Theme.ApplyLight();
        _batch = new SpriteBatch(GraphicsDevice);
        _assets = new Assets(GraphicsDevice);
        _font = new BitmapFont(_assets.Texture("font"));
    }

    protected override void Update(GameTime gameTime)
    {
        float ms = (float)gameTime.ElapsedGameTime.TotalMilliseconds;
        _frameMs[_frameIndex] = ms;
        _frameIndex = (_frameIndex + 1) % _frameMs.Length;

        float sum = 0f, worst = 0f;
        foreach (var f in _frameMs) { sum += f; if (f > worst) worst = f; }
        _average = sum / _frameMs.Length;
        _worst = worst;

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Theme.Background);
        var pp = GraphicsDevice.PresentationParameters;
        int sw = pp.BackBufferWidth, sh = pp.BackBufferHeight;
        int scale = Math.Max(1, Math.Min(sw, sh) / 260);

        _batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp);

        var insets = _platform.SafeInsets;
        var lines = new[]
        {
            ("DEVICE REPORT", Theme.Accent),
            ("", Color.White),
            ($"BACK BUFFER {sw} X {sh}", Color.White),
            ($"ASPECT      {sw / (float)sh:0.000}", Color.White),
            ($"ORIENTATION {_platform.Orientation.ToString().ToUpperInvariant()}", Color.White),
            ($"SAFE L{insets.Left} T{insets.Top} R{insets.Right} B{insets.Bottom}", Color.White),
            ("", Color.White),
            ($"TARGET   {TargetElapsedTime.TotalMilliseconds:0.00} MS", Theme.InkDim),
            ($"AVERAGE  {_average:0.00} MS", _average > 17.5f ? Theme.Warn : Theme.Good),
            ($"WORST    {_worst:0.00} MS", _worst > 25f ? Theme.Warn : Theme.Good),
            ("", Color.White),
            ($"VSYNC    {_graphics.SynchronizeWithVerticalRetrace}", Theme.InkDim),
            ($"FIXED    {IsFixedTimeStep}", Theme.InkDim),
        };

        int y = 20 + insets.Top;
        foreach (var (text, colour) in lines)
        {
            if (text.Length > 0) _font.Draw(_batch, text, new Vector2(16 + insets.Left, y), colour, scale);
            y += 10 * scale;
        }

        // A frame-time graph. A flat line is what you want; a comb means the
        // device is fighting you and no amount of gameplay tuning will fix it.
        int gx = 16 + insets.Left, gy = sh - 90 - insets.Bottom, gw = sw - 32 - insets.Left - insets.Right, gh = 60;
        _batch.Draw(_assets.Pixel, new Rectangle(gx, gy, gw, gh), new Color(0, 0, 0, 26));
        _batch.Draw(_assets.Pixel, new Rectangle(gx, gy + gh - (int)(gh * 16.67f / 40f), gw, 1),
                    Theme.Good * 0.6f);
        for (int i = 0; i < _frameMs.Length; i++)
        {
            int idx = (_frameIndex + i) % _frameMs.Length;
            float v = Math.Min(_frameMs[idx], 40f);
            int barH = (int)(gh * v / 40f);
            int bx = gx + i * gw / _frameMs.Length;
            _batch.Draw(_assets.Pixel, new Rectangle(bx, gy + gh - barH, Math.Max(1, gw / _frameMs.Length), barH),
                        v > 20f ? Theme.Warn : Theme.Info);
        }
        _font.Draw(_batch, "FRAME TIME 0-40MS", new Vector2(gx, gy - 10 * scale), Theme.InkDim, scale);

        _batch.End();
        base.Draw(gameTime);
    }
}
