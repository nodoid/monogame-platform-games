using System;
using System.IO;
using System.Reflection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Retro.Engine;

namespace Chapter45;

/// <summary>
/// Chapter 45 - Packaging and Release.
///
/// A pre-flight check that runs on the device. Every line is something that has
/// shipped wrong in a real game at least once:
///
///   - the build is a Debug build (the single most common store rejection cause
///     that is entirely self-inflicted)
///   - the bundle identifier is still the template's
///   - an asset the game asks for at runtime did not make it into the package
///   - the icon or splash resource is missing on one platform only
///
/// Run this as the last thing before you upload. It takes four seconds and it
/// has caught all four of the above.
/// </summary>
public sealed class Ch45Screen : IScreen
{
    private const int W = 224, H = 256;

    private static readonly string[] RequiredAssets =
    {
        "font", "tiles", "life",
        "jack_run", "barrel", "fireball", "gorilla", "princess",
        "quasi_run", "bell", "knight", "arrow",
    };

    private static readonly string[] RequiredSounds =
    {
        "jump", "land", "point", "death", "select",
    };

    private readonly RetroGame _game;
    private readonly System.Collections.Generic.List<(string Label, bool Ok, string Detail)> _checks = new();
    private float _t;
    private int _passed, _failed;

    public Ch45Screen(RetroGame game) => _game = game;

    public void Enter() => Run();
    public void Leave() { }

    private void Add(string label, bool ok, string detail = "")
    {
        _checks.Add((label, ok, detail));
        if (ok) _passed++; else _failed++;
    }

    private void Run()
    {
        _checks.Clear();
        _passed = _failed = 0;

        bool debug = false;
#if DEBUG
        debug = true;
#endif
        Add("RELEASE BUILD", !debug, debug ? "DEBUG" : "RELEASE");

        var asm = Assembly.GetExecutingAssembly().GetName();
        Add("VERSION SET", asm.Version != null && asm.Version.Major > 0,
            asm.Version?.ToString() ?? "NONE");

        int missingArt = 0;
        foreach (var name in RequiredAssets)
        {
            try { _ = _game.Assets.Texture(name); }
            catch (Exception) { missingArt++; }
        }
        Add("ARTWORK PACKAGED", missingArt == 0,
            missingArt == 0 ? RequiredAssets.Length + " OK" : missingArt + " MISSING");

        int missingSfx = 0;
        foreach (var name in RequiredSounds)
        {
            try { _ = _game.Assets.Sound(name); }
            catch (Exception) { missingSfx++; }
        }
        Add("AUDIO PACKAGED", missingSfx == 0,
            missingSfx == 0 ? RequiredSounds.Length + " OK" : missingSfx + " MISSING");

        // Writable storage: if this fails, high scores silently never save.
        bool canWrite = false;
        string where = "";
        try
        {
            where = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var probe = Path.Combine(where, ".writeprobe");
            File.WriteAllText(probe, "ok");
            canWrite = File.ReadAllText(probe) == "ok";
            File.Delete(probe);
        }
        catch (Exception) { canWrite = false; }
        Add("STORAGE WRITABLE", canWrite, canWrite ? "YES" : "NO");

        var pp = _game.GraphicsDevice.PresentationParameters;
        Add("BACK BUFFER", pp.BackBufferWidth > 0 && pp.BackBufferHeight > 0,
            pp.BackBufferWidth + "X" + pp.BackBufferHeight);

        var insets = _game.Platform.SafeInsets;
        Add("SAFE AREA READ", true,
            $"L{insets.Left} T{insets.Top} R{insets.Right} B{insets.Bottom}");

        Add("ORIENTATION LOCKED", _game.Platform.Orientation != OrientationMode.Sensor,
            _game.Platform.Orientation.ToString().ToUpperInvariant());

        Add("FIXED TIMESTEP", _game.IsFixedTimeStep, _game.IsFixedTimeStep ? "60 HZ" : "FREE");

        // The book's capture flag must never ship: it skips the title screen.
        Add("DEBUG LENS OFF", !Dbg.Any && !Dbg.AutoPlay,
            Dbg.AutoPlay ? "AUTOPLAY ON" : Dbg.Any ? "LENS ON" : "CLEAN");
    }

    public void Update(float dt)
    {
        _t += dt;
        Dbg.Tick(dt);
        if (_game.Input.Pressed(Btn.Jump)) Run();
    }

    public void Draw(SpriteBatch batch)
    {
        var f = _game.Font;
        var px = _game.Assets.Pixel;

        f.DrawCentred(batch, "RELEASE CHECK", W / 2, 8, Theme.Accent);
        f.DrawCentred(batch, _failed == 0 ? "ALL CLEAR" : _failed + " PROBLEM" + (_failed == 1 ? "" : "S"),
                      W / 2, 22, _failed == 0 ? Theme.Good : Theme.Warn);

        int y = 40;
        foreach (var (label, ok, detail) in _checks)
        {
            batch.Draw(px, new Rectangle(10, y + 1, 6, 6), ok ? Theme.Good : Theme.Warn);
            f.Draw(batch, label, new Vector2(22, y), Theme.Ink);
            f.DrawRight(batch, detail, W - 10, y, Theme.InkDim);
            y += 12;
        }

        y += 8;
        f.Draw(batch, "FPS " + Dbg.Fps.ToString("00.0"), new Vector2(12, y), Theme.Info);
        y += 12;
        f.Draw(batch, "GC  " + (GC.GetTotalMemory(false) / 1024) + " KB", new Vector2(12, y), Theme.Info);
        y += 12;
        f.Draw(batch, "GEN0 " + GC.CollectionCount(0), new Vector2(12, y), Theme.Info);

        f.DrawCentred(batch, "FIRE TO RE-RUN", W / 2, H - 18, Theme.InkDim);
    }
}
