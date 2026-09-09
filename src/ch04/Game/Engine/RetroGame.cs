using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input.Touch;

namespace Retro.Engine;

/// <summary>
/// Everything both games need from MonoGame, wired up once.
///
/// This is the chapter 3 to 8 version. Audio arrives in chapter 9 and is added
/// to this same class there; nothing else about it changes.
///
/// The fixed timestep is the important line here. Arcade physics were written
/// against a known frame rate; a jump tuned at 60Hz behaves differently if the
/// update interval wanders. MonoGame's IsFixedTimeStep gives us a constant dt,
/// so the jump arc, the barrel speed and the bonus countdown are identical on
/// every device - and, just as usefully, a bug reproduces the same way twice.
/// </summary>
public abstract class RetroGame : Game
{
    protected readonly GraphicsDeviceManager Graphics;

    public IPlatformService Platform { get; }
    public VirtualScreen Screen { get; private set; }
    public VirtualPad Pad { get; } = new();
    public InputState Input { get; private set; }
    public Assets Assets { get; private set; }
    public BitmapFont Font { get; private set; }
    public ScreenManager Screens { get; private set; }
    public SpriteBatch Batch { get; private set; }

    public int VirtualWidth { get; private set; }
    public int VirtualHeight { get; private set; }

    private int _lastBackW, _lastBackH;
    private bool _lastPadEnabled = true;

    protected RetroGame(IPlatformService platform, int virtualWidth, int virtualHeight)
    {
        Platform = platform ?? new NullPlatformService();
        VirtualWidth = virtualWidth;
        VirtualHeight = virtualHeight;

        Graphics = new GraphicsDeviceManager(this)
        {
            IsFullScreen = true,
            PreferredBackBufferWidth = virtualWidth * 3,
            PreferredBackBufferHeight = virtualHeight * 3,
            SynchronizeWithVerticalRetrace = true,
            // 16-bit depth is plenty for 2D and saves bandwidth on mobile GPUs.
            PreferredDepthStencilFormat = DepthFormat.None,
        };
        Graphics.HardwareModeSwitch = false;

        IsFixedTimeStep = true;
        TargetElapsedTime = TimeSpan.FromTicks(TimeSpan.TicksPerSecond / 60);
        IsMouseVisible = false;
        Content.RootDirectory = "Content";
    }

    protected override void Initialize()
    {
        // Two simultaneous touches is all either game ever needs: a direction
        // and a jump. Asking for fewer keeps the touch panel cheap.
        TouchPanel.EnabledGestures = GestureType.None;

        // On a phone the window *is* the display, so take the whole of it.
        //
        // The preferred size set in the constructor is a desktop convenience,
        // and on Android MonoGame honours its aspect ratio: it letterboxes the
        // viewport to 224x256 before the engine has letterboxed anything, and
        // then the engine letterboxes again inside that. The game comes out at
        // a third of the size, sitting in the bottom of the screen, with the
        // two sets of bars indistinguishable from each other.
        var display = GraphicsDevice.Adapter.CurrentDisplayMode;
        if (display.Width > 1 && display.Height > 1)
        {
            Graphics.PreferredBackBufferWidth = display.Width;
            Graphics.PreferredBackBufferHeight = display.Height;
            Graphics.ApplyChanges();
        }

        base.Initialize();
    }

    /// <summary>The palette this game uses. Override to ship a dark build.</summary>
    protected virtual void ApplyTheme() => Theme.ApplyLight();

    protected override void LoadContent()
    {
        ApplyTheme();
        Batch = new SpriteBatch(GraphicsDevice);
        Assets = new Assets(GraphicsDevice);
        Font = new BitmapFont(Assets.Texture("font"));
        Screen = new VirtualScreen(GraphicsDevice, VirtualWidth, VirtualHeight);
        Input = new InputState(Pad);
        Screens = new ScreenManager(Assets.Pixel, VirtualWidth, VirtualHeight);

        RelayoutForScreenSize(force: true);
        OnLoaded();
    }

    /// <summary>Called once the engine is up. Build the first screen here.</summary>
    protected abstract void OnLoaded();

    /// <summary>True when the on-screen pad should reserve room at the bottom.</summary>
    protected virtual bool PadReservesSpace => VirtualHeight > VirtualWidth;

    protected void RelayoutForScreenSize(bool force = false)
    {
        var (w, h) = VirtualScreen.SurfaceOf(GraphicsDevice);
        if (!force && w == _lastBackW && h == _lastBackH && Pad.Enabled == _lastPadEnabled) return;
        _lastBackW = w; _lastBackH = h; _lastPadEnabled = Pad.Enabled;

        Pad.Layout(w, h, landscape: w >= h);
        // Only reserve room when the pad is actually on screen. A title screen
        // that hides the pad should get the whole display back, not a 400-pixel
        // grey band where the controls would have been.
        Screen.ReserveBottom = PadReservesSpace && Pad.Enabled ? Pad.ReservedHeight : 0;
        Screen.Recalculate();
    }

    public GraphicsDeviceManager GraphicsManager => Graphics;

    /// <summary>
    /// The one orientation the window is ever in. Everything the game presents
    /// in the other one is turned a quarter turn instead; see
    /// <see cref="VirtualScreen.Rotated"/>.
    /// </summary>
    public OrientationMode PrimaryOrientation { get; private set; } = OrientationMode.Portrait;

    /// <summary>Lock the window to one orientation. Called once, at startup.</summary>
    protected void LockOrientation(OrientationMode mode)
    {
        PrimaryOrientation = mode;
        Graphics.SupportedOrientations = mode == OrientationMode.Landscape
            ? DisplayOrientation.LandscapeLeft | DisplayOrientation.LandscapeRight
            : DisplayOrientation.Portrait;
        Graphics.ApplyChanges();
        Platform.SetOrientation(mode);
        if (Screen != null)
        {
            Screen.Rotated = false;
            RelayoutForScreenSize(force: true);
        }
    }

    /// <summary>
    /// Ask for an orientation.
    ///
    /// If it is not the window's, the *picture* turns rather than the window.
    /// Asking the operating system to rotate at runtime is two different APIs,
    /// asynchronous on both, and on iOS the scene turns while the graphics back
    /// buffer does not follow - which draws the game sideways at a fraction of
    /// the scale in a corner. Turning the presentation ourselves is one code
    /// path, immediate, and cannot be half-applied.
    /// </summary>
    public void SetOrientation(OrientationMode mode)
    {
        if (Screen == null) return;
        Screen.Rotated = mode != OrientationMode.Sensor && mode != PrimaryOrientation;
        RelayoutForScreenSize(force: true);
    }

    /// <summary>
    /// Swap the virtual resolution at runtime. The runner uses this to play at
    /// 256x192 in landscape and then present its high score table at 192x256 in
    /// portrait: the render target is rebuilt, everything else carries on.
    /// </summary>
    public void SetVirtualSize(int width, int height)
    {
        if (width == VirtualWidth && height == VirtualHeight) return;
        VirtualWidth = width;
        VirtualHeight = height;
        Screen?.Dispose();
        Screen = new VirtualScreen(GraphicsDevice, width, height);
        Screens?.SetSize(width, height);
        RelayoutForScreenSize(force: true);
    }

    protected override void Update(GameTime gameTime)
    {
        // The device can rotate, and on Android the surface is recreated when it
        // does; re-fitting every frame is cheap and removes a whole class of
        // "the game is off-centre after rotating" bugs.
        RelayoutForScreenSize();

        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
        Input.Update(dt);
        Screens.Update(dt);
        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        Screen.Begin();
        Batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp);
        Screens.Draw(Batch);
        Batch.End();
        Screen.End(Batch);

        // The pad is drawn last, in device space, outside the virtual screen -
        // so it stays a constant physical size no matter how the game is scaled.
        // Its transform turns with the picture; see VirtualScreen.Rotated.
        Batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp,
                    null, null, null, Pad.DeviceTransform);
        Pad.Draw(Batch, Assets.Pixel);
        Batch.End();

        if (Dbg.CaptureFrame)
        {
            // Sits on the outermost row and column of the picture rather than
            // outside it, so it is never clipped by the edge of the display.
            Batch.Begin(SpriteSortMode.Deferred, BlendState.Opaque, SamplerState.PointClamp);
            Dbg.Box(Batch, Assets.Pixel, Screen.Destination, Dbg.CaptureMark);
            Batch.End();
        }

        base.Draw(gameTime);
    }

    protected override void UnloadContent()
    {
        Assets?.Dispose();
        Screen?.Dispose();
        base.UnloadContent();
    }

}
