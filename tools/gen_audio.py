"""Generates every sound effect and music loop for both games."""
import os, random, math
from synth import (RATE, note, seq, osc, env, apply_env, mix, lowpass,
                   silence, write, freq)


# ---------------------------------------------------------------- shared -----
def sfx_jump():
    return note('square', 'C4', 0.16, 0.5, duty=0.25, sweep=14, rel=0.05)


def sfx_land():
    n = int(0.09 * RATE)
    thud = apply_env(lowpass(osc('noise', 0, n), 0.06), env(n, 0.001, 0.01, 0.4, 0.07))
    body = note('tri', 'C2', 0.09, 0.5, sweep=-6)
    return [v * 0.7 for v in mix(thud, body)]


def sfx_step():
    n = int(0.035 * RATE)
    return [v * 0.28 for v in apply_env(lowpass(osc('noise', 0, n), 0.35),
                                        env(n, 0.001, 0.005, 0.3, 0.028))]


def sfx_point():
    return note('square', 'E5', 0.05, 0.45, duty=0.5) + note('square', 'B5', 0.13, 0.45)


def sfx_extra_life():
    return seq('square', [('C5', 1), ('E5', 1), ('G5', 1), ('C6', 2)], tempo=260, vol=0.42)


def sfx_blip():
    return note('square', 'A4', 0.045, 0.35, duty=0.25)


def sfx_select():
    return note('square', 'C5', 0.05, 0.4) + note('square', 'G5', 0.09, 0.4)


def sfx_timer_warn():
    return note('square', 'C6', 0.07, 0.32, duty=0.125) + silence(0.05) + \
           note('square', 'C6', 0.07, 0.32, duty=0.125)


# ------------------------------------------------------------ the climber ----
def sfx_barrel_roll():
    """One second of loopable rumble; the game pitches it by distance."""
    n = int(1.0 * RATE)
    rumble = lowpass(osc('noise', 0, n), 0.02)
    wobble = osc('tri', 42, n)
    sig = [rumble[i] * (0.6 + 0.4 * wobble[i]) * 0.5 for i in range(n)]
    # taper both ends so the loop point is inaudible
    fade = int(0.02 * RATE)
    for i in range(fade):
        sig[i] *= i / fade
        sig[n - 1 - i] *= i / fade
    return sig


def sfx_hammer_get():
    return seq('square', [('G4', 1), ('C5', 1), ('E5', 1), ('G5', 1)], tempo=340, vol=0.45)


def sfx_hammer_hit():
    n = int(0.14 * RATE)
    crack = apply_env(osc('noise', 0, n), env(n, 0.001, 0.02, 0.25, 0.11))
    ring = note('square', 'A5', 0.14, 0.3, duty=0.125, sweep=-10)
    return [v * 0.8 for v in mix(crack, ring)]


def sfx_death_climber():
    body = seq('square', [('G4', 1), ('F#4', 1), ('F4', 1), ('E4', 1),
                          ('D#4', 1), ('D4', 1), ('C#4', 1), ('C4', 3)],
               tempo=200, vol=0.42, duty=0.5)
    return body + note('tri', 'C3', 0.5, 0.4, sweep=-24)


def sfx_stage_clear():
    lead = seq('square', [('C5', 1), ('C5', 1), ('C5', 1), ('C5', 2), ('G4', 1),
                          ('A4', 1), ('C5', 1), ('A4', 1), ('C5', 3)], tempo=250, vol=0.4)
    bass = seq('tri', [('C3', 2), ('C3', 2), ('G2', 2), ('C3', 4)], tempo=250, vol=0.32)
    return mix(lead, bass)


def music_climber_stage():
    """Marching 8-bar loop. Tempo is nudged at runtime as the bonus timer drains."""
    lead = seq('square', [('E4', 1), ('G4', 1), ('C5', 1), ('G4', 1),
                          ('E4', 1), ('G4', 1), ('C5', 2),
                          ('F4', 1), ('A4', 1), ('C5', 1), ('A4', 1),
                          ('F4', 1), ('A4', 1), ('C5', 2),
                          ('D4', 1), ('F4', 1), ('B4', 1), ('F4', 1),
                          ('D4', 1), ('F4', 1), ('B4', 2),
                          ('C4', 1), ('E4', 1), ('G4', 1), ('C5', 1),
                          ('G4', 1), ('E4', 1), ('C4', 2)],
               tempo=170, vol=0.3, duty=0.25)
    bass = seq('tri', [('C3', 2), ('C3', 2), ('G2', 2), ('C3', 2),
                       ('F2', 2), ('F2', 2), ('C3', 2), ('F2', 2),
                       ('G2', 2), ('G2', 2), ('D3', 2), ('G2', 2),
                       ('C3', 2), ('G2', 2), ('C3', 4)],
               tempo=170, vol=0.26)
    return mix(lead, bass)


def music_climber_title():
    lead = seq('square', [('C5', 2), ('G4', 1), ('E4', 1), ('G4', 2), ('C5', 2),
                          ('D5', 2), ('C5', 1), ('B4', 1), ('C5', 4)],
               tempo=200, vol=0.34, duty=0.5)
    return lead


# ------------------------------------------------------------- the runner ----
def sfx_bell():
    """Struck bell: a few inharmonic partials with a long tail."""
    n = int(1.4 * RATE)
    parts = []
    for mult, amp in ((1.0, 1.0), (2.76, 0.55), (5.4, 0.3), (8.9, 0.16)):
        f = 523.25 * mult
        parts.append([v * amp for v in apply_env(osc('sine', f, n),
                                                 env(n, 0.002, 0.05, 0.55, 1.2))])
    strike_n = int(0.02 * RATE)
    strike = apply_env(osc('noise', 0, strike_n), env(strike_n, 0.001, 0.004, 0.3, 0.014))
    return [v * 0.55 for v in mix(mix(*parts), strike)]


def sfx_arrow():
    n = int(0.30 * RATE)
    air = lowpass(osc('noise', 0, n), 0.5)
    whistle = osc('sine', 1400, n, sweep=-18)
    sig = [air[i] * 0.35 + whistle[i] * 0.30 for i in range(n)]
    return apply_env(sig, env(n, 0.03, 0.05, 0.7, 0.2))


def sfx_rope_grab():
    n = int(0.22 * RATE)
    creak = osc('saw', 180, n, sweep=5)
    return [v * 0.3 for v in apply_env(lowpass(creak, 0.15), env(n, 0.02, 0.04, 0.5, 0.15))]


def sfx_swing():
    n = int(0.35 * RATE)
    return [v * 0.25 for v in apply_env(lowpass(osc('noise', 0, n), 0.08),
                                        env(n, 0.12, 0.05, 0.7, 0.18))]


def sfx_death_hunch():
    fall = note('square', 'G4', 0.55, 0.42, duty=0.25, sweep=-30)
    thud = sfx_land()
    return fall + silence(0.05) + [v * 1.2 for v in thud]


def sfx_screen_clear():
    return mix(seq('square', [('G4', 1), ('C5', 1), ('E5', 1), ('G5', 2)], tempo=280, vol=0.4),
               seq('tri', [('C3', 2), ('G3', 3)], tempo=280, vol=0.3))


def music_hunch_run():
    lead = seq('square', [('A4', 1), ('A4', 1), ('C5', 1), ('A4', 1),
                          ('D5', 2), ('C5', 2),
                          ('A4', 1), ('A4', 1), ('C5', 1), ('D5', 1),
                          ('E5', 2), ('D5', 2),
                          ('C5', 1), ('C5', 1), ('E5', 1), ('C5', 1),
                          ('G5', 2), ('E5', 2),
                          ('D5', 1), ('C5', 1), ('B4', 1), ('A4', 1),
                          ('A4', 4)],
               tempo=190, vol=0.28, duty=0.25)
    bass = seq('tri', [('A2', 2), ('A2', 2), ('D3', 2), ('D3', 2),
                       ('A2', 2), ('A2', 2), ('E3', 2), ('E3', 2),
                       ('F3', 2), ('F3', 2), ('C3', 2), ('C3', 2),
                       ('G2', 2), ('E2', 2), ('A2', 4)],
               tempo=190, vol=0.26)
    return mix(lead, bass)


def music_hunch_title():
    return mix(seq('square', [('D5', 2), ('A4', 1), ('D5', 1), ('F5', 2), ('E5', 2),
                              ('D5', 2), ('C5', 1), ('A4', 1), ('D5', 4)],
                   tempo=180, vol=0.32),
               seq('tri', [('D3', 4), ('F3', 4), ('A2', 4), ('D3', 4)], tempo=180, vol=0.28))


CLIMBER = {
    'jump': sfx_jump, 'land': sfx_land, 'step': sfx_step, 'point': sfx_point,
    'extra_life': sfx_extra_life, 'blip': sfx_blip, 'select': sfx_select,
    'timer_warn': sfx_timer_warn, 'barrel': sfx_barrel_roll,
    'hammer_get': sfx_hammer_get, 'hammer_hit': sfx_hammer_hit,
    'death': sfx_death_climber, 'stage_clear': sfx_stage_clear,
    'music_stage': music_climber_stage, 'music_title': music_climber_title,
}

HUNCH = {
    'jump': sfx_jump, 'land': sfx_land, 'step': sfx_step, 'point': sfx_point,
    'extra_life': sfx_extra_life, 'blip': sfx_blip, 'select': sfx_select,
    'timer_warn': sfx_timer_warn, 'bell': sfx_bell, 'arrow': sfx_arrow,
    'rope_grab': sfx_rope_grab, 'swing': sfx_swing, 'death': sfx_death_hunch,
    'screen_clear': sfx_screen_clear, 'music_run': music_hunch_run,
    'music_title': music_hunch_title,
}


def build(outdir, table):
    os.makedirs(outdir, exist_ok=True)
    random.seed(1234)
    for name, fn in table.items():
        write(os.path.join(outdir, name + '.wav'), fn())
    return outdir


if __name__ == '__main__':
    import sys
    base = sys.argv[1] if len(sys.argv) > 1 else 'out'
    print(build(os.path.join(base, 'climber'), CLIMBER))
    print(build(os.path.join(base, 'hunch'), HUNCH))
