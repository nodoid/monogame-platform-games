"""Emits HunchScreens.cs - fifteen flick-screens, each exactly 32x24 characters.

The design rule, taken straight from the Ocean original: one idea per screen.
A screen introduces a hazard, the next tests it, the one after combines it with
something already learned. Nothing is ever introduced in a screen that also
demands precision with it."""
COLS, ROWS = 32, 24
GROUND = 20          # top row of the walkway
HUD = 2


def blank():
    return [['.'] * COLS for _ in range(ROWS)]


def floor(g, c0, c1, row=GROUND):
    for c in range(c0, c1 + 1):
        g[row][c] = '='
        for r in range(row + 1, ROWS):
            g[r][c] = '#'


def put(g, r, c, ch):
    if 0 <= r < ROWS and 0 <= c < COLS:
        g[r][c] = ch


def render(g):
    return [''.join(r) for r in g]


def base_screen(gaps=(), row=GROUND):
    g = blank()
    spans, start = [], 0
    for a, b in gaps:
        if a > start:
            spans.append((start, a - 1))
        start = b + 1
    if start <= COLS - 1:
        spans.append((start, COLS - 1))
    for a, b in spans:
        floor(g, a, b, row)
    return g


def finish(g, bell_col=29):
    put(g, GROUND - 2, bell_col, 'B')
    return render(g)


def s01():
    """Teach: run right, ring the bell. Nothing can kill you."""
    g = base_screen()
    put(g, GROUND - 1, 2, 'P')
    return finish(g)


def s02():
    """Teach: the jump, over a two-tile pit."""
    g = base_screen(gaps=[(13, 14)])
    put(g, GROUND - 1, 2, 'P')
    return finish(g)


def s03():
    """Test: three pits of increasing width."""
    g = base_screen(gaps=[(8, 9), (16, 18), (23, 25)])
    put(g, GROUND - 1, 2, 'P')
    return finish(g)


def s04():
    """Teach: the rope. One pit too wide to jump."""
    g = base_screen(gaps=[(12, 19)])
    put(g, GROUND - 1, 2, 'P')
    put(g, 6, 15, 'r')
    return finish(g)


def s05():
    """Test: two ropes, chained."""
    g = base_screen(gaps=[(9, 14), (19, 25)])
    put(g, GROUND - 1, 2, 'P')
    put(g, 6, 11, 'r')
    put(g, 6, 22, 'r')
    return finish(g)


def s06():
    """Teach: arrows. Flat ground, so the only problem is timing."""
    g = base_screen()
    put(g, GROUND - 1, 2, 'P')
    put(g, GROUND - 2, 31, 'a')
    put(g, GROUND - 5, 31, 'a')
    return finish(g, 28)


def s07():
    """Combine: arrows over pits."""
    g = base_screen(gaps=[(11, 13), (20, 22)])
    put(g, GROUND - 1, 2, 'P')
    put(g, GROUND - 2, 31, 'a')
    return finish(g, 28)


def s08():
    """Teach: fire pits - a hazard you cannot pass, only jump."""
    g = base_screen()
    put(g, GROUND - 1, 2, 'P')
    put(g, GROUND - 2, 10, 'F')
    put(g, GROUND - 2, 18, 'F')
    return finish(g)


def s11():
    """Teach: the knight. He paces; you must pick your moment."""
    g = base_screen()
    put(g, GROUND - 1, 2, 'P')
    put(g, GROUND - 1, 16, 'g')
    return finish(g)


def s12():
    """Combine: knight plus pit plus arrow."""
    g = base_screen(gaps=[(12, 14)])
    put(g, GROUND - 1, 2, 'P')
    put(g, GROUND - 1, 21, 'g')
    put(g, GROUND - 2, 31, 'a')
    return finish(g, 28)


def s09():
    """Teach: the bouncing fireball. Flat ground and one source, so the only
    problem is reading the arc."""
    g = base_screen()
    put(g, GROUND - 1, 2, 'P')
    put(g, GROUND - 1, 31, 'x')
    return finish(g, 28)


def s10():
    """Test: fireballs over two pits. The bounce and the gap have to agree."""
    g = base_screen(gaps=[(10, 12), (19, 21)])
    put(g, GROUND - 1, 2, 'P')
    put(g, GROUND - 1, 31, 'x')
    return finish(g, 28)


def s13():
    """Raised walkway: the route is up and along."""
    g = base_screen(gaps=[(10, 21)])
    floor(g, 10, 21, GROUND - 5)
    put(g, GROUND - 1, 2, 'P')
    put(g, GROUND - 6, 14, 'F')
    put(g, GROUND - 2, 31, 'a')
    return finish(g, 28)


def s14():
    """Rope over fire, with a knight waiting on the far side."""
    g = base_screen(gaps=[(11, 20)])
    put(g, GROUND - 1, 2, 'P')
    put(g, 6, 13, 'r')
    put(g, 6, 19, 'r')
    put(g, GROUND - 1, 26, 'g')
    return finish(g)


def s15():
    """The parapet: two knights and an arrow slit across three gaps."""
    g = base_screen(gaps=[(9, 11), (17, 19)])
    put(g, GROUND - 1, 2, 'P')
    put(g, GROUND - 1, 14, 'g')
    put(g, GROUND - 1, 25, 'g')
    put(g, GROUND - 2, 31, 'a')
    return finish(g, 28)


def s16():
    """The last screen. Everything at once, with no room at all."""
    g = base_screen(gaps=[(7, 9), (13, 15), (19, 24)])
    put(g, GROUND - 1, 2, 'P')
    put(g, 6, 21, 'r')
    put(g, GROUND - 2, 11, 'F')
    put(g, GROUND - 1, 27, 'g')
    put(g, GROUND - 1, 31, 'x')
    put(g, GROUND - 5, 31, 'a')
    return finish(g, 30)


SCREENS = [
    ("THE COURTYARD", 60, s01()), ("FIRST GAP", 60, s02()),
    ("THE BROKEN WALK", 58, s03()), ("THE LONG DROP", 58, s04()),
    ("TWO ROPES", 56, s05()), ("ARROW SLIT", 56, s06()),
    ("CROSSFIRE", 54, s07()), ("THE BRAZIERS", 54, s08()),
    ("ROLLING FIRE", 54, s09()), ("FIRE AND GAPS", 52, s10()),
    ("THE SENTRY", 52, s11()), ("THE NARROW PASS", 50, s12()),
    ("HIGH WALK", 50, s13()), ("OVER THE FIRE", 48, s14()),
    ("THE PARAPET", 46, s15()), ("THE BELL TOWER", 44, s16()),
]


def emit(path):
    o = ["// <auto-generated> Screen layouts. See tools/gen_hunch_screens.py.",
         "namespace Retro.Hunch;", "",
         "/// <summary>",
         "/// Fifteen flick-screens, 32x24 characters each:",
         "///   '=' walkway  '#' stone fill  'r' rope anchor  'F' fire pit  '^' spikes",
         "///   'a' arrow slit  'g' knight  'x' fireball source  'B' bell  'P' player start",
         "/// </summary>",
         "public static class HunchScreens", "{",
         "    public static readonly (string Name, int Time, string[] Rows)[] All =", "    {"]
    for name, t, rows in SCREENS:
        o.append(f'        ("{name}", {t}, new[]')
        o.append("        {")
        for r in rows:
            assert len(r) == COLS, (name, len(r))
            o.append(f'            "{r}",')
        o.append("        }),")
    o += ["    };", "",
          "    public static int Count => All.Length;", "",
          "    public static HunchLevel Load(int index)",
          "    {",
          "        var s = All[System.Math.Clamp(index, 0, All.Length - 1)];",
          "        return HunchLevel.Parse(s.Name, s.Time, s.Rows);",
          "    }", "}"]
    open(path, "w").write("\n".join(o) + "\n")
    return path


if __name__ == "__main__":
    import sys
    print(emit(sys.argv[1]))
