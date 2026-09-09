using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Retro.Engine;

/// <summary>
/// The book's debug lens.
///
/// Each chapter's app switches on the overlay that shows what that chapter is
/// about - hit boxes, the tile grid the collision code actually sees, the jump
/// arc, the audio voice count. They are all off in a shipping build and cost
/// nothing when off, but during development the ability to *see* the numbers is
/// worth more than any amount of stepping through a debugger: a platform bug is
/// nearly always a bug about where something is, and a picture answers that
/// immediately.
///
/// The flags are static because they are a development tool, not game state.
/// </summary>
public static class Dbg
{
    public static string Caption;          // shown in the corner: which chapter this is
    public static bool Hitboxes;
    public static bool TileGrid;
    public static bool JumpArc;
    public static bool AudioMeter;
    public static bool Timing;
    public static bool SafeArea;
    public static bool Paths;              // hazard travel directions
    public static bool Ladders;            // ladder decision points
    public static bool Camera;             // parallax layer offsets

    /// <summary>
    /// Skip the title screen and the opening and go straight into play.
    ///
    /// This exists so the figures in this book can be captured from the real
    /// game on a real device rather than mocked up: a screenshot tool cannot
    /// press "start" on an iOS simulator, and a game that needs a human to
    /// reach its interesting screen cannot be photographed automatically.
    /// It is never set in a shipping build, and chapter 45's pre-flight check
    /// asserts as much.
    /// </summary>
    public static bool AutoPlay;

    /// <summary>
    /// Start a run at this stage or screen rather than the first.
    ///
    /// Chapter 24 argues that a difficulty curve you cannot skip into is a
    /// difficulty curve that never gets tested, and the same is true of level
    /// twelve of sixteen. This is that skip, and it is also how the figures in
    /// this book are captured on the screen each chapter is about.
    /// </summary>
    public static int StartLevel;

    /// <summary>
    /// Capture builds only: outline the virtual screen's destination rectangle
    /// in a colour no artwork uses, so the tool that photographs the app knows
    /// exactly which pixels are the game and which are the letterbox.
    ///
    /// Guessing from the letterbox colour does not survive the light theme,
    /// where a panel inside the game can be the same grey as the bars around
    /// it and the figure comes out clipped.
    /// </summary>
#if CAPTURE
    public static bool CaptureFrame = true;
#else
    public static bool CaptureFrame;
#endif

    /// <summary>The marker colour. Nothing in either game's palette is magenta.</summary>
    public static readonly Color CaptureMark = new(255, 0, 255);

    public static bool Any => Hitboxes || TileGrid || JumpArc || AudioMeter ||
                              Timing || SafeArea || Paths || Ladders || Camera;

    public static void Reset()
    {
        Hitboxes = TileGrid = JumpArc = AudioMeter = Timing = SafeArea = Paths = Ladders = Camera = false;
        AutoPlay = false;
        StartLevel = 0;
        // CaptureFrame is deliberately not reset: it is a property of the build,
        // not of the lens, and every generated host calls Reset on startup.
    }

    // ------------------------------------------------------------- drawing ---
    public static void Box(SpriteBatch b, Texture2D px, Rectangle r, Color c)
    {
        b.Draw(px, new Rectangle(r.X, r.Y, r.Width, 1), c);
        b.Draw(px, new Rectangle(r.X, r.Bottom - 1, r.Width, 1), c);
        b.Draw(px, new Rectangle(r.X, r.Y, 1, r.Height), c);
        b.Draw(px, new Rectangle(r.Right - 1, r.Y, 1, r.Height), c);
    }

    public static void Grid(SpriteBatch b, Texture2D px, int width, int height, int tile, Color c)
    {
        for (int x = 0; x <= width; x += tile) b.Draw(px, new Rectangle(x, 0, 1, height), c);
        for (int y = 0; y <= height; y += tile) b.Draw(px, new Rectangle(0, y, width, 1), c);
    }

    /// <summary>Plot a ballistic arc from a launch point - the shape of a jump.</summary>
    public static void Arc(SpriteBatch b, Texture2D px, Vector2 from, Vector2 velocity,
                           float gravity, Color c, float seconds = 1.2f)
    {
        var p = from;
        var v = velocity;
        const float step = 1f / 60f;
        for (float t = 0; t < seconds; t += step)
        {
            v.Y += gravity * step;
            p += v * step;
            if (p.Y > 400 || p.X < -20 || p.X > 400) break;
            b.Draw(px, new Rectangle((int)p.X, (int)p.Y, 1, 1), c);
        }
    }

    private static readonly Queue<float> Frames = new();
    private static float _fps;

    public static void Tick(float dt)
    {
        Frames.Enqueue(dt);
        while (Frames.Count > 60) Frames.Dequeue();
        float sum = 0f;
        foreach (var f in Frames) sum += f;
        _fps = sum > 0f ? Frames.Count / sum : 0f;
    }

    public static float Fps => _fps;

    public static void Banner(SpriteBatch b, BitmapFont font, Texture2D px, int width, int height)
    {
        if (string.IsNullOrEmpty(Caption)) return;
        int y = height - 10;
        b.Draw(px, new Rectangle(0, y - 1, width, 10), Theme.Panel * Theme.PanelAlpha);
        font.Draw(b, Caption, new Vector2(3, y), Theme.Good);
        if (Timing) font.DrawRight(b, _fps.ToString("00") + " FPS", width - 3, y, Theme.Good);
    }
}
