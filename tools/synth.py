"""A very small chiptune synthesiser: square, triangle, saw and noise voices
written straight to 16-bit mono PCM WAV. No dependencies beyond the stdlib."""
import math, random, struct, wave

RATE = 22050

NOTES = {'C': 0, 'C#': 1, 'D': 2, 'D#': 3, 'E': 4, 'F': 5, 'F#': 6,
         'G': 7, 'G#': 8, 'A': 9, 'A#': 10, 'B': 11}


def freq(name):
    """'A4' -> 440.0. 'R' is a rest."""
    if name in ('R', '-', None):
        return 0.0
    octave = int(name[-1])
    semis = NOTES[name[:-1]] + (octave - 4) * 12 - 9
    return 440.0 * (2 ** (semis / 12.0))


def env(n, attack=0.01, decay=0.0, sustain=1.0, release=0.15):
    """Per-sample ADSR multiplier list of length n."""
    a, d, r = int(attack * RATE), int(decay * RATE), int(release * RATE)
    a, d, r = min(a, n), min(d, n), min(r, n)
    s = max(0, n - a - d - r)
    out = []
    for i in range(a):
        out.append(i / max(1, a))
    for i in range(d):
        out.append(1.0 + (sustain - 1.0) * (i / max(1, d)))
    out += [sustain] * s
    for i in range(r):
        out.append(sustain * (1.0 - i / max(1, r)))
    return (out + [0.0] * n)[:n]


def osc(shape, f, n, phase=0.0, duty=0.5, sweep=0.0):
    """Generate n samples. `sweep` bends the pitch over the note (in semitones)."""
    out = []
    ph = phase
    for i in range(n):
        ff = f * (2 ** (sweep * (i / max(1, n)) / 12.0))
        ph += ff / RATE
        t = ph % 1.0
        if shape == 'square':
            v = 1.0 if t < duty else -1.0
        elif shape == 'tri':
            v = 4.0 * abs(t - 0.5) - 1.0
        elif shape == 'saw':
            v = 2.0 * t - 1.0
        elif shape == 'sine':
            v = math.sin(2 * math.pi * t)
        else:  # noise
            v = random.uniform(-1.0, 1.0)
        out.append(v)
    return out


def mix(*tracks):
    n = max((len(t) for t in tracks), default=0)
    out = [0.0] * n
    for t in tracks:
        for i, v in enumerate(t):
            out[i] += v
    return out


def apply_env(sig, e):
    return [s * e[i] for i, s in enumerate(sig[:len(e)])]


def lowpass(sig, alpha=0.25):
    out, prev = [], 0.0
    for s in sig:
        prev = prev + alpha * (s - prev)
        out.append(prev)
    return out


def note(shape, name, dur, vol=0.5, duty=0.5, sweep=0.0, atk=0.005, rel=0.06, sus=0.8):
    n = int(dur * RATE)
    sig = osc(shape, freq(name), n, duty=duty, sweep=sweep)
    return [v * vol for v in apply_env(sig, env(n, atk, 0.02, sus, rel))]


def seq(shape, pattern, tempo=140, vol=0.5, duty=0.5):
    """pattern: list of (note, beats)."""
    beat = 60.0 / tempo
    out = []
    for name, beats in pattern:
        out += note(shape, name, beat * beats, vol, duty)
    return out


def silence(dur):
    return [0.0] * int(dur * RATE)


def write(path, sig, normalise=0.85):
    peak = max((abs(s) for s in sig), default=1.0) or 1.0
    g = normalise / peak if peak > normalise else 1.0
    data = b''.join(struct.pack('<h', max(-32767, min(32767, int(s * g * 32767)))) for s in sig)
    with wave.open(path, 'wb') as w:
        w.setnchannels(1)
        w.setsampwidth(2)
        w.setframerate(RATE)
        w.writeframes(data)
    return path
