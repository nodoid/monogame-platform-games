using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;

namespace Retro.Engine;

/// <summary>
/// Loads PNG and WAV files straight out of the app bundle.
///
/// Why not the MonoGame content pipeline? On mobile the pipeline adds a build
/// step that has to run on the developer's machine, produces .xnb files that are
/// awkward to inspect, and needs platform-specific profiles. Our assets are
/// small PNGs and short WAVs; Texture2D.FromStream and SoundEffect.FromStream
/// read them directly, and TitleContainer.OpenStream resolves the path
/// identically on Android (assets/) and iOS (bundle root). One code path, no
/// tooling, and the raw files stay editable right up to shipping.
/// </summary>
public sealed class Assets : IDisposable
{
    private readonly GraphicsDevice _device;
    private readonly string _root;
    private readonly Dictionary<string, Texture2D> _textures = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, SoundEffect> _sounds = new(StringComparer.OrdinalIgnoreCase);

    public Assets(GraphicsDevice device, string root = "Content")
    {
        _device = device;
        _root = root;
    }

    public Texture2D Texture(string name)
    {
        if (_textures.TryGetValue(name, out var t)) return t;
        using var s = TitleContainer.OpenStream($"{_root}/{name}.png");
        t = Texture2D.FromStream(_device, s);
        _textures[name] = t;
        return t;
    }

    public SoundEffect Sound(string name)
    {
        if (_sounds.TryGetValue(name, out var s)) return s;
        using var stream = TitleContainer.OpenStream($"{_root}/{name}.wav");
        s = SoundEffect.FromStream(stream);
        _sounds[name] = s;
        return s;
    }

    /// <summary>A 1x1 white texture, handy for bars, boxes and letterbox fills.</summary>
    public Texture2D Pixel
    {
        get
        {
            if (_textures.TryGetValue("__pixel", out var p)) return p;
            p = new Texture2D(_device, 1, 1);
            p.SetData(new[] { Microsoft.Xna.Framework.Color.White });
            _textures["__pixel"] = p;
            return p;
        }
    }

    public void Dispose()
    {
        foreach (var t in _textures.Values) t.Dispose();
        foreach (var s in _sounds.Values) s.Dispose();
        _textures.Clear();
        _sounds.Clear();
    }
}
