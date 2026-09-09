"""Generates every graphic for the runner (Hunchback style). Landscape, 256x192 virtual."""
import os, math, random
from artlib import canvas, from_art, flip, sheet, draw, save

O = (0, 0, 0, 255)
PAL = {
    'o': O,
    's': (236, 188, 144, 255),   # skin
    'n': (108, 68, 28, 255),     # hair / boots
    't': (72, 148, 64, 255),     # tunic
    'T': (40, 96, 40, 255),      # hump / shadow side
    'b': (150, 92, 24, 255),     # belt
    'r': (200, 40, 40, 255),
    'y': (248, 208, 64, 255),
    'W': (255, 255, 255, 255),
    'g': (150, 150, 165, 255),
}

HEAD = ["......nnnn....",
        ".....nnnnnn...",
        "....nnssss....",
        "....nssooss...",
        "....ssssssss..",
        ".....ssssss..."]

BODY = ["..TTTtttttt...",
        ".TTTTtttttts..",
        ".TTTTttttttss.",
        "..TTttttttt...",
        "...bbbbbbb....",
        "...ttttttt...."]

RUN1 = HEAD + BODY + ["...tt...tt....", "...nn...nn....", "..ooo...ooo...", "..ooo...ooo..."]
RUN2 = HEAD + BODY + ["....tttt......", "....nnnn......", "...oooo.......", "...oooo......."]
RUN3 = HEAD + BODY + [".tt.......tt..", ".nn.......nn..", "ooo.......ooo.", "ooo.......ooo."]
RUN4 = HEAD + BODY + ["....tttt......", "....nnnn......", "....oooo......", "....oooo......"]

JUMP = HEAD + ["..TTTtttttt.s.", ".TTTTttttttss.", ".TTTTtttttts..", "..TTttttttt...",
               "...bbbbbbb....", "..tttt..tttt..", "..nn......nn..", ".ooo......ooo.",
               ".ooo......ooo.", ".............."][:10]

SWING1 = ["....s.........", "....s.........", "....nnnn......", "...nnssss.....",
          "...nssooss....", "...ssssssss...", "..TTTtttttt...", ".TTTTttttttss.",
          "..TTttttttt...", "...bbbbbbb....", "...tttttt.....", "...tt..tt.....",
          "...nn..nn.....", "..ooo..ooo....", "..............", ".............."]
SWING2 = ["....s.........", "....s.........", "....nnnn......", "...nnssss.....",
          "...nssooss....", "...ssssssss...", "..TTTtttttt...", ".TTTTttttttss.",
          "..TTttttttt...", "...bbbbbbb....", "....tttttt....", ".....tttt.....",
          ".....nnnn.....", "....oooooo....", "..............", ".............."]

DEAD = ["..............", "..............", "..............", "..............",
        "....nnnn......", "...nnssss.....", "...ssssssss...", "..TTTtttttt...",
        ".TTTTttttttss.", "..TTttttttt...", "...bbbbbbb....", "..tt.....tt...",
        "..nn.....nn...", ".ooo.....ooo..", "..............", ".............."]


def bell_frames():
    out = []
    for f in range(3):
        img = canvas(16, 16)
        d = draw(img)
        tilt = (0, -1, 1)[f]
        gold, dark = (248, 208, 64, 255), (168, 128, 16, 255)
        d.line([8, 0, 8, 2], fill=(90, 90, 100, 255))
        d.polygon([(4 + tilt, 12), (11 + tilt, 12), (10 + tilt, 4), (5 + tilt, 4)], fill=gold, outline=O)
        d.rectangle([3 + tilt, 12, 12 + tilt, 13], fill=gold, outline=O)
        d.line([6 + tilt, 6, 6 + tilt, 11], fill=dark)
        d.rectangle([7 + tilt * 2, 13, 8 + tilt * 2, 15], fill=dark, outline=O)   # clapper
        out.append(img)
    return out


def rope_tile():
    img = canvas(4, 8)
    d = draw(img)
    d.line([1, 0, 1, 7], fill=(180, 140, 80, 255))
    d.line([2, 0, 2, 7], fill=(120, 88, 40, 255))
    for y in (1, 4, 7):
        d.point([(1, y)], fill=(220, 190, 130, 255))
    return img


def arrow_sprite():
    img = canvas(16, 6)
    d = draw(img)
    d.line([2, 3, 13, 3], fill=(140, 100, 50, 255))
    d.polygon([(13, 0), (15, 3), (13, 5)], fill=(120, 124, 140, 255), outline=O)
    d.line([2, 1, 4, 3], fill=(96, 100, 116, 255))
    d.line([2, 5, 4, 3], fill=(96, 100, 116, 255))
    return img


def guard_frames():
    out = []
    for f in range(2):
        img = canvas(16, 16)
        d = draw(img)
        d.ellipse([4, 0, 11, 7], fill=(132, 134, 156, 255), outline=O)     # helmet
        d.rectangle([5, 4, 10, 6], fill=(40, 40, 50, 255))                 # visor
        d.rectangle([4, 7, 11, 12], fill=(120, 40, 40, 255), outline=O)    # tabard
        d.line([7, 8, 8, 8], fill=(230, 200, 90, 255))
        step = f * 2
        d.rectangle([5, 12, 6, 15], fill=(60, 60, 70, 255))
        d.rectangle([9 - step, 12, 10 - step, 15], fill=(60, 60, 70, 255))
        d.line([12, 2, 12, 15], fill=(140, 100, 50, 255))                  # pike
        d.polygon([(11, 3), (13, 3), (12, 0)], fill=(140, 142, 162, 255), outline=O)
        d.line([10, 9, 12, 9], fill=(236, 188, 144, 255))                  # gauntlet grip
        out.append(img)
    return out


def fireball_frames():
    """The runner's bouncing fireball. Two frames of flicker with trailing
    sparks, small enough that the player reads the arc rather than the sprite."""
    out = []
    for f in range(2):
        img = canvas(12, 12)
        d = draw(img)
        wob = 1 if f else 0
        d.ellipse([1, 2 + wob, 10, 11], fill=(214, 72, 16, 255), outline=O)
        d.ellipse([3, 4 + wob, 8, 9], fill=(246, 156, 30, 255))
        d.ellipse([4, 5 + wob, 7, 7], fill=(255, 236, 130, 255))
        for x in (2, 6, 9):
            d.line([x, 2 + wob, x + (1 if f else -1), 0], fill=(240, 190, 50, 255))
        out.append(img)
    return out


def firepit_frames():
    out = []
    for f in range(3):
        img = canvas(16, 16)
        d = draw(img)
        d.rectangle([0, 12, 15, 15], fill=(70, 60, 50, 255), outline=O)
        random.seed(f)
        for i in range(6):
            x = 2 + i * 2
            h = 4 + ((f + i) % 3) * 3
            d.polygon([(x, 12), (x + 2, 12), (x + 1, 12 - h)], fill=(232, 96, 24, 255))
            d.polygon([(x, 12), (x + 2, 12), (x + 1, 12 - h // 2)], fill=(250, 200, 60, 255))
        out.append(img)
    return out


def spikes_tile():
    img = canvas(16, 8)
    d = draw(img)
    for i in range(4):
        x = i * 4
        d.polygon([(x, 7), (x + 4, 7), (x + 2, 0)], fill=(128, 130, 148, 255), outline=O)
    return img


def tile(kind, frame=0):
    img = canvas(8, 8)
    d = draw(img)
    stone, dark, light = (128, 122, 110, 255), (78, 74, 66, 255), (168, 162, 148, 255)
    if kind == 'stone':
        d.rectangle([0, 0, 7, 7], fill=stone)
        d.line([0, 0, 7, 0], fill=light); d.line([0, 7, 7, 7], fill=dark)
        d.line([0, 3, 7, 3], fill=dark); d.line([3, 0, 3, 3], fill=dark)
        d.line([5, 4, 5, 7], fill=dark)
    elif kind == 'battlement':
        d.rectangle([0, 2, 7, 7], fill=stone)
        d.rectangle([0, 0, 2, 2], fill=stone)
        d.rectangle([5, 0, 7, 2], fill=stone)
        d.line([0, 2, 7, 2], fill=light)
    elif kind == 'walkway':
        d.rectangle([0, 0, 7, 7], fill=(148, 132, 104, 255))
        d.line([0, 0, 7, 0], fill=(196, 180, 150, 255))
        d.line([0, 4, 7, 4], fill=(104, 92, 72, 255))
    elif kind == 'pit':
        d.rectangle([0, 0, 7, 7], fill=(18, 14, 22, 255))
    return img


def parallax_layers(w=512):
    """Two silhouette strips that scroll at different rates behind the ramparts."""
    far = canvas(w, 64)
    d = draw(far)
    random.seed(7)
    x = 0
    while x < w:
        tw = random.choice([24, 32, 40])
        th = random.randint(20, 44)
        d.rectangle([x, 64 - th, x + tw, 63], fill=(178, 190, 206, 255))
        d.polygon([(x - 2, 64 - th), (x + tw + 2, 64 - th), (x + tw // 2, 64 - th - 10)],
                  fill=(164, 176, 194, 255))
        for wy in range(64 - th + 6, 60, 8):
            for wx in range(x + 4, x + tw - 3, 8):
                d.rectangle([wx, wy, wx + 2, wy + 3], fill=(126, 138, 158, 255))
        x += tw + random.randint(4, 14)

    near = canvas(w, 48)
    d = draw(near)
    random.seed(11)
    for i in range(0, w, 16):
        h = 18 + int(10 * math.sin(i * 0.05)) + random.randint(0, 6)
        d.rectangle([i, 48 - h, i + 16, 47], fill=(140, 156, 152, 255))
    return far, near


def sky(w=256, h=192):
    """Daylight sky for the light theme: pale blue overhead fading to a warm
    haze at the horizon, with a low sun. Distance is *lighter* in daylight,
    which is the opposite of the night version and is why the parallax layers
    below are hazed towards the sky colour rather than darkened."""
    img = canvas(w, h)
    d = draw(img)
    for y in range(h):
        t = y / h
        d.line([0, y, w, y], fill=(int(150 + 96 * t), int(184 + 58 * t), int(216 + 22 * t), 255))
    # A few soft clouds, drawn as overlapping pale ellipses.
    random.seed(3)
    for _ in range(7):
        cx, cy = random.randrange(w), random.randrange(20, h // 3)
        cw, ch = random.randrange(26, 48), random.randrange(7, 12)
        d.ellipse([cx, cy, cx + cw, cy + ch], fill=(248, 249, 250, 255))
        d.ellipse([cx + cw // 4, cy - ch // 2, cx + cw - cw // 5, cy + ch // 2],
                  fill=(252, 252, 253, 255))
    d.ellipse([w - 50, 16, w - 24, 42], fill=(255, 246, 214, 255))
    d.ellipse([w - 46, 20, w - 28, 38], fill=(255, 252, 236, 255))
    return img


def life_icon():
    img = canvas(8, 8)
    d = draw(img)
    d.ellipse([2, 0, 5, 3], fill=PAL['n'])
    d.polygon([(1, 7), (6, 7), (5, 3), (2, 3)], fill=PAL['t'])
    return img


def build(outdir):
    os.makedirs(outdir, exist_ok=True)
    p = lambda n: os.path.join(outdir, n)
    art = lambda rows: from_art(rows, PAL)

    save(sheet([art(RUN1), art(RUN2), art(RUN3), art(RUN4)], 14, 16), p("quasi_run.png"))
    save(sheet([art(JUMP)], 14, 16), p("quasi_jump.png"))
    save(sheet([art(SWING1), art(SWING2)], 14, 16), p("quasi_swing.png"))
    save(sheet([art(DEAD)], 14, 16), p("quasi_dead.png"))
    save(sheet(bell_frames(), 16, 16), p("bell.png"))
    save(rope_tile(), p("rope.png"))
    save(arrow_sprite(), p("arrow.png"))
    save(sheet(guard_frames(), 16, 16), p("guard.png"))
    save(sheet(fireball_frames(), 12, 12), p("fireball_h.png"))
    save(sheet(firepit_frames(), 16, 16), p("firepit.png"))
    save(spikes_tile(), p("spikes.png"))
    save(sheet([tile('stone'), tile('battlement'), tile('walkway'), tile('pit')], 8, 8), p("tiles.png"))
    far, near = parallax_layers()
    save(far, p("bg_far.png"))
    save(near, p("bg_near.png"))
    save(sky(), p("sky.png"))
    save(life_icon(), p("life.png"))
    return outdir


if __name__ == "__main__":
    import sys
    print(build(sys.argv[1] if len(sys.argv) > 1 else "out"))
