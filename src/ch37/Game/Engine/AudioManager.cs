using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Audio;

namespace Retro.Engine;

/// <summary>
/// A tiny mixer sitting between the game and MonoGame's audio.
///
/// The problems it exists to solve, all of which bite in a barrel-heavy screen:
///  - Twelve barrels asking to play a roll every frame will happily open twelve
///    hundred voices and blow past the platform limit. Effects are throttled by
///    a minimum retrigger gap.
///  - Some sounds matter more than others. A death jingle must not be lost
///    because four points pickups got there first, so each play carries a
///    priority and low-priority requests are dropped when the voice budget is full.
///  - Music and effects need independent volume, both for a settings screen and
///    for ducking music under a fanfare.
/// </summary>
public sealed class AudioManager : IDisposable
{
    private const int MaxVoices = 12;

    private sealed class Voice
    {
        public SoundEffectInstance Instance;
        public int Priority;
        public string Name;
    }

    private readonly Assets _assets;
    private readonly List<Voice> _voices = new();
    private readonly Dictionary<string, double> _lastPlayed = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, SoundEffectInstance> _loops = new(StringComparer.OrdinalIgnoreCase);

    private SoundEffectInstance _music;
    private string _musicName;
    private double _clock;
    private float _duck = 1f, _duckTarget = 1f;

    public float MusicVolume { get; set; } = 0.7f;
    public float EffectVolume { get; set; } = 0.9f;
    public bool Muted { get; set; }

    public AudioManager(Assets assets) => _assets = assets;

    public void Update(float dt)
    {
        _clock += dt;

        // Ease the music duck rather than stepping it; a hard drop is audible.
        _duck += (_duckTarget - _duck) * MathF.Min(1f, dt * 6f);
        if (_music != null && !_music.IsDisposed)
            _music.Volume = Muted ? 0f : MusicVolume * _duck;

        for (int i = _voices.Count - 1; i >= 0; i--)
        {
            var v = _voices[i];
            if (v.Instance.IsDisposed || v.Instance.State == SoundState.Stopped)
            {
                if (!v.Instance.IsDisposed) v.Instance.Dispose();
                _voices.RemoveAt(i);
            }
        }
    }

    /// <param name="minGap">Refuse to retrigger this sound within this many seconds.</param>
    public void Play(string name, float volume = 1f, float pitch = 0f, float pan = 0f,
                     int priority = 0, float minGap = 0.03f)
    {
        if (Muted) return;
        if (_lastPlayed.TryGetValue(name, out var last) && _clock - last < minGap) return;

        if (_voices.Count >= MaxVoices)
        {
            int worst = -1;
            for (int i = 0; i < _voices.Count; i++)
                if (worst < 0 || _voices[i].Priority < _voices[worst].Priority) worst = i;
            if (worst < 0 || _voices[worst].Priority >= priority) return;  // nothing cheaper to evict
            _voices[worst].Instance.Stop();
            _voices[worst].Instance.Dispose();
            _voices.RemoveAt(worst);
        }

        var inst = _assets.Sound(name).CreateInstance();
        inst.Volume = Math.Clamp(volume * EffectVolume, 0f, 1f);
        inst.Pitch = Math.Clamp(pitch, -1f, 1f);
        inst.Pan = Math.Clamp(pan, -1f, 1f);
        inst.Play();
        _voices.Add(new Voice { Instance = inst, Priority = priority, Name = name });
        _lastPlayed[name] = _clock;
    }

    /// <summary>A sound that runs until stopped - a rolling barrel, a fire pit.</summary>
    public void Loop(string name, float volume = 1f, float pitch = 0f, float pan = 0f)
    {
        if (Muted) { StopLoop(name); return; }
        if (!_loops.TryGetValue(name, out var inst) || inst.IsDisposed)
        {
            inst = _assets.Sound(name).CreateInstance();
            inst.IsLooped = true;
            inst.Play();
            _loops[name] = inst;
        }
        inst.Volume = Math.Clamp(volume * EffectVolume, 0f, 1f);
        inst.Pitch = Math.Clamp(pitch, -1f, 1f);
        inst.Pan = Math.Clamp(pan, -1f, 1f);
    }

    public void StopLoop(string name)
    {
        if (_loops.TryGetValue(name, out var inst) && !inst.IsDisposed)
        {
            inst.Stop();
            inst.Dispose();
        }
        _loops.Remove(name);
    }

    public void StopAllLoops()
    {
        foreach (var k in new List<string>(_loops.Keys)) StopLoop(k);
    }

    public void PlayMusic(string name, float pitch = 0f)
    {
        if (_musicName == name && _music != null && !_music.IsDisposed)
        {
            _music.Pitch = Math.Clamp(pitch, -1f, 1f);
            return;
        }
        StopMusic();
        _musicName = name;
        _music = _assets.Sound(name).CreateInstance();
        _music.IsLooped = true;
        _music.Volume = Muted ? 0f : MusicVolume * _duck;
        _music.Pitch = Math.Clamp(pitch, -1f, 1f);
        _music.Play();
    }

    /// <summary>Bend the music tempo. Used to wind the player up as a timer drains.</summary>
    public void SetMusicPitch(float pitch)
    {
        if (_music != null && !_music.IsDisposed) _music.Pitch = Math.Clamp(pitch, -1f, 1f);
    }

    public void StopMusic()
    {
        if (_music != null && !_music.IsDisposed) { _music.Stop(); _music.Dispose(); }
        _music = null;
        _musicName = null;
    }

    /// <summary>Pull the music down so a jingle can be heard over it.</summary>
    public void Duck(float amount = 0.25f) => _duckTarget = Math.Clamp(amount, 0f, 1f);
    public void Unduck() => _duckTarget = 1f;

    public void Dispose()
    {
        StopMusic();
        StopAllLoops();
        foreach (var v in _voices) if (!v.Instance.IsDisposed) v.Instance.Dispose();
        _voices.Clear();
    }
}
