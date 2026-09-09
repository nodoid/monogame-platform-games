using Microsoft.Xna.Framework;

namespace Retro.Engine;

/// <summary>
/// One palette for every screen in both games.
///
/// Colours are named by *role* rather than by hue - Ink, Accent, Warn - so a
/// screen never says "draw this in yellow", it says "draw this as a heading".
/// That is what makes a theme change one file instead of forty, and it is worth
/// doing from the first screen rather than retrofitting it later.
///
/// The light theme assumes a pale ground and dark text. Sprite tints stay
/// Color.White throughout: a sprite is drawn in its own colours and multiplying
/// it by anything else is a deliberate effect, not a theme decision.
/// </summary>
public static class Theme
{
    /// <summary>Behind the play field, inside the virtual screen.</summary>
    public static Color Background = new(238, 238, 234);

    /// <summary>The letterbox outside the virtual screen, on the device.</summary>
    public static Color Bars = new(206, 208, 204);

    /// <summary>What a screen transition fades through.</summary>
    public static Color Fade = new(246, 246, 243);

    /// <summary>Primary text.</summary>
    public static Color Ink = new(26, 28, 34);

    /// <summary>Secondary text: instructions, labels, ranks.</summary>
    public static Color InkDim = new(112, 116, 124);

    /// <summary>Very quiet text and inactive elements.</summary>
    public static Color InkFaint = new(168, 172, 178);

    /// <summary>Headings and the currently selected thing.</summary>
    public static Color Accent = new(164, 84, 0);

    /// <summary>Warnings, danger, the last few seconds.</summary>
    public static Color Warn = new(184, 30, 24);

    /// <summary>Success and confirmation.</summary>
    public static Color Good = new(22, 116, 52);

    /// <summary>Data readouts and diagnostics.</summary>
    public static Color Info = new(28, 88, 168);

    /// <summary>A wash over a frozen screen, drawn with the 1x1 pixel.</summary>
    public static Color Overlay = new(238, 238, 234);
    public const float OverlayAlpha = 0.78f;

    /// <summary>A filled panel behind text drawn over the play field.</summary>
    public static Color Panel = new(238, 238, 234);
    public const float PanelAlpha = 0.82f;

    /// <summary>A selected row in a list.</summary>
    public static Color Highlight = new(216, 214, 200);

    /// <summary>The on-screen pad, drawn in device space over everything.</summary>
    public static Color PadInk = new(48, 52, 62);
    public const float PadAlpha = 0.30f;

    /// <summary>Grid and box colours for the debug lens.</summary>
    public static Color DbgGrid = new(150, 154, 170);
    public static Color DbgSurface = new(20, 120, 40);
    public static Color DbgBody = new(20, 90, 170);
    public static Color DbgHazard = new(190, 30, 30);
    public static Color DbgReward = new(20, 130, 60);
    public static Color DbgPath = new(170, 110, 0);
    public static Color DbgArcHigh = new(24, 96, 180);
    public static Color DbgArcLow = new(160, 40, 140);

    // ------------------------------------------------------------ presets ---
    // The fields above are the light preset, which is what both games ship.
    // A preset is just an assignment to each of them, so adding one is cheap -
    // and having two proves the roles above are real rather than a rename of
    // the colours that happened to be there first.

    public static void ApplyLight()
    {
        Background = new Color(238, 238, 234);
        Bars       = new Color(206, 208, 204);
        Fade       = new Color(246, 246, 243);
        Ink        = new Color(26, 28, 34);
        InkDim     = new Color(112, 116, 124);
        InkFaint   = new Color(168, 172, 178);
        Accent     = new Color(164, 84, 0);
        Warn       = new Color(184, 30, 24);
        Good       = new Color(22, 116, 52);
        Info       = new Color(28, 88, 168);
        Overlay    = new Color(238, 238, 234);
        Panel      = new Color(238, 238, 234);
        Highlight  = new Color(216, 214, 200);
        PadInk     = new Color(48, 52, 62);
        DbgGrid    = new Color(150, 154, 170);
        DbgSurface = new Color(20, 120, 40);
        DbgBody    = new Color(20, 90, 170);
        DbgHazard  = new Color(190, 30, 30);
        DbgReward  = new Color(20, 130, 60);
        DbgPath    = new Color(170, 110, 0);
        DbgArcHigh = new Color(24, 96, 180);
        DbgArcLow  = new Color(160, 40, 140);
    }

    /// <summary>
    /// The dark preset. Note that switching to it does *not* give you the
    /// original night-time look: chapter 39's backdrop art is drawn for
    /// daylight, and art has to be looked at rather than swapped. This changes
    /// the interface, which is the part a palette can own.
    /// </summary>
    public static void ApplyDark()
    {
        Background = new Color(14, 16, 24);
        Bars       = new Color(8, 9, 14);
        Fade       = new Color(6, 7, 11);
        Ink        = new Color(238, 238, 234);
        InkDim     = new Color(140, 146, 158);
        InkFaint   = new Color(78, 82, 94);
        Accent     = new Color(248, 208, 64);
        Warn       = new Color(240, 84, 60);
        Good       = new Color(112, 220, 132);
        Info       = new Color(120, 190, 250);
        Overlay    = new Color(6, 7, 11);
        Panel      = new Color(6, 7, 11);
        Highlight  = new Color(48, 52, 78);
        PadInk     = new Color(236, 238, 244);
        DbgGrid    = new Color(90, 96, 130);
        DbgSurface = new Color(90, 240, 110);
        DbgBody    = new Color(110, 220, 250);
        DbgHazard  = new Color(250, 90, 90);
        DbgReward  = new Color(120, 240, 150);
        DbgPath    = new Color(250, 210, 90);
        DbgArcHigh = new Color(120, 210, 250);
        DbgArcLow  = new Color(240, 140, 230);
    }
}
