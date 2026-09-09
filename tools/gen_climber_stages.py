"""Emits ClimberStages.cs - the climber's four stages, following the original's
25m / 50m / 75m / 100m progression.

Girders are beams with a surface that slopes, so hazards accelerate downhill and
zigzag the way the original's do. Ladders are computed from the beam surfaces
they join, which is the reason this is generated rather than typed: a ladder that
stops two pixels short of a sloped girder is invisible in the source and obvious
in play.
"""

W, H = 224, 256
TILE = 8

# Vertical spacing between walkable levels, and how far a girder tilts across
# its length. The *minimum* clearance is SPACING - SLOPE, and that has to stay
# comfortably above the jump apex of 25 pixels or the player can jump up a
# level and the game stops being a climber. 38 - 6 leaves seven pixels, which is
# the same margin chapter 16 derives for the flat case.
SPACING = 38
SLOPE = 6
GROUND = 248


def level(n):
    """Level 0 is the ground; higher numbers are further up."""
    return GROUND - n * SPACING


class Stage:
    def __init__(self, name, metres, bonus):
        self.name, self.metres, self.bonus = name, metres, bonus
        self.beams = []      # (x0, x1, y0, y1, dir, conveyor, beltdir)
        self.ladders = []    # (col, topY, bottomY, broken)
        self.calls = []      # fluent extras

    # -- geometry ---------------------------------------------------------
    def beam(self, x0, x1, y0, y1=None, direction=1, conveyor=False, belt=0):
        self.beams.append((x0, x1, y0, y0 if y1 is None else y1, direction, conveyor, belt))
        return self

    def girder(self, n, hole=None, tilt=0, direction=1, conveyor=False, belt=0):
        """A full-width girder with an optional hole, tilting `tilt` pixels across
        the screen.

        The hole is where hazards leave. Because the girder below is full width
        apart from its own hole - cut on the opposite side - anything that falls
        through lands exactly one level down, which is what keeps a fall
        survivable and a two-level fall fatal."""
        y = level(n)
        y0, y1 = y - tilt / 2.0, y + tilt / 2.0

        def part(a, b):
            if b - a < 8:
                return
            t0 = y0 + (y1 - y0) * (a / float(W))
            t1 = y0 + (y1 - y0) * (b / float(W))
            self.beam(a, b, t0, t1, direction, conveyor, belt)

        if hole is None:
            part(0, W)
        else:
            h0, h1 = hole
            part(0, h0)
            part(h1, W)
        return self

    def surface(self, x, y_hint):
        """Surface height of the beam under a point - used to seat ladders."""
        best = None
        for (x0, x1, y0, y1, *_rest) in self.beams:
            if not (x0 - 1 <= x <= x1 + 1):
                continue
            t = 0.0 if x1 <= x0 else max(0.0, min(1.0, (x - x0) / (x1 - x0)))
            s = y0 + (y1 - y0) * t
            if s >= y_hint - 0.5 and (best is None or s < best):
                best = s
        return best if best is not None else y_hint

    def ladder(self, x, upper_hint, broken=False):
        """A ladder joining two beams. `upper_hint` is a y at or just above the
        upper beam; the lower end is the next beam down at the same x."""
        # Seat the ladder using the centre of the tile column it will occupy,
        # not the x that was asked for. A ladder whose column centre falls past
        # the end of a beam is the one failure mode this format has, and doing
        # the lookup at the centre removes it by construction.
        col = int(x // TILE)
        cx = col * TILE + TILE / 2.0
        top = self.surface(cx, upper_hint)
        bottom = self.surface(cx, top + 10)
        self.ladders.append((col, top, bottom, broken))
        return self

    # -- markers ----------------------------------------------------------
    def call(self, fn, *args):
        self.calls.append((fn, args))
        return self


def stage_25m():
    """25m - barrels. Five sloping girders and the ground. The gorilla throws barrels
    from the top; they accelerate downhill, fall off the low end and adopt the
    next girder's slope, which is where the zigzag descent comes from."""
    s = Stage("25M BARRELS", 25, 5000)
    s.beam(88, 128, level(5) - 18)                       # the girl's platform
    s.beam(8, 128, level(5))                             # the gorilla's girder, flat
    # Alternating tilts, with the hole cut at the downhill end of each girder.
    s.girder(4, hole=(176, 196), tilt=+SLOPE)
    s.girder(3, hole=(28, 48), tilt=-SLOPE)
    s.girder(2, hole=(176, 196), tilt=+SLOPE)
    s.girder(1, hole=(28, 48), tilt=-SLOPE)
    s.beam(0, W, GROUND, GROUND, +1)                     # the ground, flat

    for x, hint, broken in ((112, level(5) - 4, False),
                            (160, level(4) - 4, False),
                            (64, level(3) - 4, False),
                            (152, level(2) - 4, False),
                            (88, level(2) - 4, True),
                            (72, level(1) - 4, False),
                            (200, level(1) - 4, True)):
        s.ladder(x, hint, broken)

    s.call("Gorilla", 32, level(5) - 32).call("Girl", 100, level(5) - 34)
    s.call("Barrels", 76, level(5) - 4)
    s.call("Player", 24, GROUND)
    s.call("Oil", 8, GROUND - 8).call("Fire", 16, GROUND)
    s.call("Hammer", 72, level(1) + 1).call("Hammer", 188, level(3) - 2)
    return s


def stage_50m():
    """50m - the pie factory. Conveyor belts run in alternating directions and
    carry cement pies. The belts also drag the player, so a ladder that was easy
    to stop at on 25m is not."""
    s = Stage("50M CONVEYORS", 50, 6000)
    s.beam(88, 128, level(5) - 18)
    s.beam(8, 128, level(5))
    s.girder(4, hole=(184, 204), conveyor=True, belt=+1, direction=+1)
    s.girder(3, hole=(20, 40), conveyor=True, belt=-1, direction=-1)
    s.girder(2, hole=(184, 204), conveyor=True, belt=+1, direction=+1)
    s.girder(1, hole=(20, 40), conveyor=True, belt=-1, direction=-1)
    s.beam(0, W, GROUND, GROUND, +1)

    for x, hint in ((112, level(5) - 4), (56, level(4) - 4), (160, level(4) - 4),
                    (72, level(3) - 4), (152, level(3) - 4),
                    (88, level(2) - 4), (144, level(2) - 4),
                    (64, level(1) - 4), (168, level(1) - 4)):
        s.ladder(x, hint)

    s.call("Gorilla", 32, level(5) - 32).call("Girl", 100, level(5) - 34)
    s.call("Player", 16, GROUND)
    s.call("Pies", 4, level(4)).call("Pies", 220, level(3)).call("Pies", 4, level(2))
    s.call("Fire", 200, GROUND).call("Fire", 60, level(1))
    s.call("Hammer", 120, level(4) - 2).call("Hammer", 64, level(2) - 2)
    return s


def stage_75m():
    """75m - elevators. Two shafts of moving platforms, with springs bouncing
    along the top and dropping down the right-hand side. The only stage where
    standing still is genuinely dangerous."""
    s = Stage("75M ELEVATORS", 75, 6000)
    s.beam(88, 128, level(6) - 18)
    s.beam(56, 152, level(5))                            # the gorilla's girder
    s.beam(48, 168, level(4), level(4), -1)              # the spring run
    s.beam(0, 60, level(3), level(3), +1)                # left tower
    s.beam(0, 60, level(2), level(2), +1)
    s.beam(0, 60, level(1), level(1), +1)
    s.beam(148, 224, level(3), level(3), -1)             # right tower
    s.beam(148, 224, level(2), level(2), -1)
    s.beam(148, 224, level(1), level(1), -1)
    s.beam(0, 224, GROUND, GROUND, +1)

    for x, hint in ((16, level(3) - 4), (16, level(2) - 4), (16, level(1) - 4),
                    (208, level(3) - 4), (208, level(2) - 4), (208, level(1) - 4),
                    (96, level(5) - 4), (64, level(4) - 4)):
        s.ladder(x, hint)

    s.call("Gorilla", 88, level(5) - 32).call("Girl", 100, level(5) - 34)
    s.call("Player", 16, GROUND)
    # Two shafts running opposite ways, bridging the gap the towers leave.
    s.call("Elevator", 84, GROUND - 12, 116, 26)
    s.call("Elevator", 124, GROUND - 12, 116, -26)
    s.call("Springs", 162, level(4))
    s.call("Fire", 30, level(2)).call("Fire", 196, level(2))
    s.call("Hammer", 30, level(3) - 2)
    return s


def stage_100m():
    """100m - rivets. Eight rivets set into the ends of four flat girders. Walk
    them all out and the structure gives way. No barrels; the pressure is the
    clock and the fires."""
    s = Stage("100M RIVETS", 100, 6000)
    s.beam(56, 136, level(4) - 20)
    holes = [(184, 204), (20, 40), (184, 204), (20, 40)]
    rows = [4, 3, 2, 1]
    for n, hole in zip(rows, holes):
        s.girder(n, hole=hole)
    s.beam(0, W, GROUND, GROUND, +1)

    for x, hint in ((96, level(4) - 4), (128, level(4) - 4),
                    (72, level(3) - 4), (152, level(3) - 4),
                    (96, level(2) - 4), (128, level(2) - 4),
                    (64, level(1) - 4), (160, level(1) - 4)):
        s.ladder(x, hint)

    for n in rows:
        y = level(n)
        s.call("Rivet", 12, y).call("Rivet", 204, y)

    s.call("Gorilla", 60, level(4) - 52).call("Girl", 116, level(4) - 36)
    s.call("Player", 24, GROUND)
    s.call("Fire", 40, GROUND).call("Fire", 190, GROUND).call("Fire", 100, level(2))
    s.call("Hammer", 112, level(3) - 2)
    return s


STAGES = [stage_25m(), stage_50m(), stage_75m(), stage_100m()]


def emit(path):
    o = ["// <auto-generated> Stage layouts. See tools/gen_climber_stages.py.",
         "using Microsoft.Xna.Framework;", "",
         "namespace Retro.Climber;", "",
         "/// <summary>",
         "/// The four stages, in the original's order: 25m barrels, 50m conveyors,",
         "/// 75m elevators, 100m rivets. Girders are beams with sloping surfaces;",
         "/// ladders are seated on the beams they join, which is computed rather than",
         "/// typed so a ladder cannot stop short of a sloped girder.",
         "/// </summary>",
         "public static class ClimberStages", "{",
         "    public static int Count => 4;", ""]

    for i, s in enumerate(STAGES):
        o.append(f"    private static ClimberLevel Stage{i}()")
        o.append("    {")
        o.append(f'        return ClimberLevel.Create("{s.name}", {s.metres}, {s.bonus})')
        for (x0, x1, y0, y1, d, conv, belt) in s.beams:
            extra = f", conveyor: true, beltDir: {belt}" if conv else ""
            o.append(f"            .Beam_({x0}f, {x1}f, {y0}f, {y1}f, {d}{extra})")
        for (col, top, bottom, broken) in s.ladders:
            b = ", broken: true" if broken else ""
            o.append(f"            .Ladder_({col}, {top:.0f}f, {bottom:.0f}f{b})")
        for (fn, args) in s.calls:
            a = ", ".join(f"{v}f" for v in args)
            o.append(f"            .{fn}({a})")
        o[-1] += ";"
        o.append("    }")
        o.append("")

    o.append("    public static ClimberLevel Load(int index) => (index % 4) switch")
    o.append("    {")
    for i in range(4):
        o.append(f"        {i} => Stage{i}(),")
    o.append("        _ => Stage0(),")
    o.append("    };")
    o.append("}")
    open(path, "w").write("\n".join(o) + "\n")
    return path


if __name__ == "__main__":
    import sys
    print(emit(sys.argv[1]))


# --------------------------------------------------------------------- checks --
def validate(verbose=True):
    """Every stage must satisfy three properties, and none of them is obvious
    from reading the layout:

      1. No two walkable levels are closer than the jump apex, or the player can
         jump up a level and the game stops being a climber.
      2. Walking off the end of any girder drops exactly one level, except into
         the 75m stage's central void, which the elevators bridge and which is
         meant to kill.
      3. Every ladder is seated on the beams it joins at both ends, and every
         marker that should stand on a girder does.

    The goal ledge is exempt from (1): it sits close above the top girder because
    reaching the girl is the point."""
    apex = 25.03
    fatal = 50.0
    voids = {"75M ELEVATORS": (60, 148)}
    ok = True

    def surfaces_at(beams, x):
        out = []
        for (x0, x1, y0, y1, *_r) in beams:
            if x0 - 1 <= x <= x1 + 1:
                t = 0.0 if x1 <= x0 else max(0.0, min(1.0, (x - x0) / (x1 - x0)))
                out.append(y0 + (y1 - y0) * t)
        return sorted(out)

    for st in STAGES:
        play = st.beams[1:]                      # beam 0 is the goal ledge
        void = voids.get(st.name)

        worst = 1e9
        for x in range(0, W + 1, 2):
            ys = surfaces_at(play, x)
            for a, b in zip(ys, ys[1:]):
                if b - a > 1:
                    worst = min(worst, b - a)
        if worst <= apex + 3:
            print(f"  {st.name}: minimum clearance {worst:.1f} is jumpable")
            ok = False

        for (x0, x1, y0, y1, *_r) in play:
            for (ex, ey) in ((x0 - 2, y0), (x1 + 2, y1)):
                if ex < 0 or ex > W: continue
                if void and void[0] - 4 <= ex <= void[1] + 4: continue
                below = [v for v in surfaces_at(st.beams, ex) if v > ey + 1]
                if below and below[0] - ey > fatal:
                    print(f"  {st.name}: walking off ({ex:.0f},{ey:.0f}) drops {below[0]-ey:.0f}")
                    ok = False

        HUD = 24
        for (fn, args) in st.calls:
            if len(args) > 1:
                x, y = args[0], args[1]
                # The gorilla and the girl are placed by their top-left corner; the
                # player, fires and hammers are placed at their feet.
                top = y if fn in ("Gorilla", "Girl") else y - 16
                if top < HUD:
                    print(f"  {st.name}: {fn}({x},{y}) reaches y={top}, under the HUD")
                    ok = False
                if not (0 <= x <= W):
                    print(f"  {st.name}: {fn}({x},{y}) is off screen")
                    ok = False
            if fn not in ("Player", "Fire", "Rivet", "Pies", "Hammer"): continue
            x, y = args[0], args[1]
            near = [v for v in surfaces_at(st.beams, x) if abs(v - y) <= 4]
            if not near:
                print(f"  {st.name}: {fn}({x},{y}) is not seated on a beam")
                ok = False

        for (col, top, bottom, broken) in st.ladders:
            x = col * TILE + 4
            for edge in (top, bottom):
                near = [v for v in surfaces_at(st.beams, x) if abs(v - edge) < 1.5]
                if not near:
                    print(f"  {st.name}: ladder col {col} end {edge:.0f} is not on a beam")
                    ok = False

        if verbose:
            print(f"{st.name:16s} clearance {worst:5.1f}  beams {len(st.beams):2d}  "
                  f"ladders {len(st.ladders)}")
    print("geometry check:", "PASS" if ok else "FAIL")
    return ok
