using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Retro.Engine;

/// <summary>
/// An 8x8 fixed-cell bitmap font covering ASCII 32..95 (space through underscore).
///
/// Why not SpriteFont? A SpriteFont is rasterised from a TrueType face at build
/// time and needs the content pipeline. A single PNG grid gives us hard pixel
/// edges at any integer scale, tints for free, and exactly the arcade look we
/// want. Everything is upper case by design - the original machines had no
/// lower case in their character ROM either.
/// </summary>
public sealed class BitmapFont
{
    public const int Cell = 8;
    public const int Advance = 6;   // glyphs are 5 wide plus one blank column
    private const int First = 32, Columns = 16;

    private readonly Texture2D _sheet;

    public BitmapFont(Texture2D sheet) => _sheet = sheet;

    public int Measure(string text, int scale = 1) => (text?.Length ?? 0) * Advance * scale;

    public void Draw(SpriteBatch batch, string text, Vector2 pos, Color colour, int scale = 1)
    {
        if (string.IsNullOrEmpty(text)) return;
        float x = pos.X;
        foreach (char raw in text)
        {
            char c = char.ToUpperInvariant(raw);
            if (c >= First && c <= 95)
            {
                int i = c - First;
                var src = new Rectangle((i % Columns) * Cell, (i / Columns) * Cell, Cell, Cell);
                batch.Draw(_sheet, new Rectangle((int)x, (int)pos.Y, Cell * scale, Cell * scale),
                           src, colour);
            }
            x += Advance * scale;
        }
    }

    public void DrawCentred(SpriteBatch batch, string text, int centreX, int y, Color colour, int scale = 1)
        => Draw(batch, text, new Vector2(centreX - Measure(text, scale) / 2f, y), colour, scale);

    /// <summary>Right-aligned, which is what a score readout wants.</summary>
    public void DrawRight(SpriteBatch batch, string text, int rightX, int y, Color colour, int scale = 1)
        => Draw(batch, text, new Vector2(rightX - Measure(text, scale), y), colour, scale);
}
