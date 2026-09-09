"""Generates every graphic for the climber. Portrait, 224x256 virtual."""
import os, math, random
from artlib import canvas, from_art, flip, sheet, draw, save, TRANSPARENT

O = (0, 0, 0, 255)
PAL = {
    'o': O,
    's': (252, 188, 140, 255),   # skin
    'h': (224, 40, 24, 255),     # cap
    'w': (232, 232, 240, 255),   # shirt highlight
    'b': (40, 72, 200, 255),     # overalls
    'n': (120, 60, 20, 255),     # hair / timber
    'y': (248, 208, 40, 255),
    'r': (200, 32, 24, 255),
    'g': (60, 200, 90, 255),
    'p': (240, 120, 200, 255),
    'W': (255, 255, 255, 255),
    'd': (90, 90, 100, 255),
}

HEAD = ["....hhhh....",
        "...hhhhhhh..",
        "...sssssss..",
        "..soossosss.",
        "..sssssssss.",
        "...sssssss..",
        "....sssss..."]

TORSO = ["..wwwwwwww..",
         ".wwwbwwbwww.",
         "sswwbwwbwwss",
         "ss.bbbbbb.ss"]

IDLE   = HEAD + TORSO + ["...bbbbbb...", "...bb..bb...", "...bb..bb...", "..ooo..ooo..", "..ooo..ooo.."][:5]
RUN1   = HEAD + TORSO + ["...bbbbbb...", "..bbb.bbb...", ".bbb...bbb..", "ooo.....ooo.", "ooo.....ooo."]
RUN2   = HEAD + TORSO + ["...bbbbbb...", "...bbbbbb...", "....bbbb....", "...ooooo....", "...ooooo...."]
RUN3   = HEAD + TORSO + ["...bbbbbb...", "..bbbb.bb...", "..bbb...bb..", ".ooo...ooo..", ".ooo...ooo.."]
JUMP   = HEAD + ["s.wwwwwwww.s", "sswwwbbwwwss", ".s.wbwwbw.s.", "...bbbbbb..."] + \
         ["..bbbbbbbb..", "..bb....bb..", ".ooo....ooo.", ".ooo....ooo.", "............"]

CLIMB1 = ["..s.hhhh....", "..s.hhhhhh..", "..s.hhhhhh..", "..s.nnnnnn..",
          "..sswwwww...", "...wwwwwwws.", "...wwwwwwss.", "...wwwwww...",
          "...bbbbbb...", "...bbbbbb...", "...bb.bbb...", "...bb..bb...",
          "...bb..bb...", "..ooo..oo...", "..ooo..ooo..", "............"]
CLIMB2 = ["....hhhh.s..", "..hhhhhh.s..", "..hhhhhh.s..", "..nnnnnn.s..",
          "...wwwwwss..", ".swwwwwww...", ".sswwwwww...", "...wwwwww...",
          "...bbbbbb...", "...bbbbbb...", "...bbb.bb...", "...bb..bb...",
          "...bb..bb...", "...oo..ooo..", "..ooo..ooo..", "............"]

# Hammer raised / swung. The hitbox changes with the frame, which the book explains.
HAMMER_UP = ["...nnnn.....", "...nnnn.....", "....nn......", "....hhhh....",
             "...hhhhhhh..", "...sssssss..", "..soossosss.", "..sssssssss.",
             "..wwwwwwww..", ".wwwbwwbwww.", "ss.bbbbbb.ss", "...bbbbbb...",
             "...bb..bb...", "...bb..bb...", "..ooo..ooo..", "..ooo..ooo.."]
HAMMER_DN = ["............", "....hhhh....", "...hhhhhhh..", "...sssssss..",
             "..soossosss.", "..sssssssss.", "..wwwwwwww..", ".wwwbwwbww.n",
             "ss.bbbbbb.nn", "...bbbbbb.nn", "...bb..bb.nn", "...bb..bb...",
             "..ooo..ooo..", "..ooo..ooo..", "............", "............"]

DEAD = ["............", "............", "...sssssss..", "..soossosss.",
        "..sssssssss.", "...sssssss..", "....hhhhh...", "...hhhhhhh..",
        "..wwwwwwww..", ".bbbbbbbbbb.", "ss.bbbbbb.ss", "...bb..bb...",
        "..ooo..ooo..", "..ooo..ooo..", "............", "............"]


def barrel_frames():
    """Four rolling frames: the stave pattern rotates so motion reads even at 16px."""
    out = []
    body, band, dark = (192, 96, 24, 255), (232, 176, 64, 255), (110, 48, 8, 255)
    for f in range(4):
        img = canvas(16, 16)
        d = draw(img)
        d.ellipse([0, 1, 15, 14], fill=body, outline=O)
        d.ellipse([3, 1, 12, 14], fill=dark, outline=None)
        d.ellipse([4, 2, 11, 13], fill=body, outline=None)
        for i in range(3):
            y = 3 + ((f * 3 + i * 4) % 11)
            d.line([1, y, 14, y], fill=band)
        d.ellipse([0, 1, 15, 14], outline=O)
        out.append(img)
    return out


def fireball_frames():
    out = []
    for f in range(2):
        img = canvas(16, 16)
        d = draw(img)
        wob = 1 if f else 0
        d.ellipse([2, 4 + wob, 13, 15], fill=(232, 64, 16, 255), outline=O)
        d.ellipse([4, 6 + wob, 11, 13], fill=(248, 160, 32, 255))
        d.ellipse([6, 8 + wob, 9, 11], fill=(255, 240, 120, 255))
        # eyes make the hazard read as an enemy rather than scenery
        d.point([(6, 7 + wob), (9, 7 + wob)], fill=(255, 255, 255, 255))
        for x in (3, 7, 11):
            d.line([x, 4 + wob, x + (1 if f else -1), 1 + wob], fill=(248, 200, 48, 255))
        out.append(img)
    return out


def hammer_item():
    img = canvas(12, 12)
    d = draw(img)
    d.rectangle([1, 1, 10, 5], fill=(216, 216, 224, 255), outline=O)
    d.rectangle([4, 5, 7, 11], fill=(150, 80, 24, 255), outline=O)
    return img


def gorilla_frames():
    """32x32 antagonist. Two frames: chest-beat left and right."""
    out = []
    fur, dark, face = (140, 72, 24, 255), (86, 40, 8, 255), (232, 184, 136, 255)
    for f in range(2):
        img = canvas(32, 32)
        d = draw(img)
        d.ellipse([6, 0, 25, 13], fill=fur, outline=O)          # head
        d.ellipse([11, 6, 20, 13], fill=face)                    # muzzle
        d.rectangle([12, 7, 13, 8], fill=O); d.rectangle([18, 7, 19, 8], fill=O)
        d.line([13, 11, 18, 11], fill=O)
        d.ellipse([3, 2, 8, 8], fill=dark, outline=O)            # ears
        d.ellipse([23, 2, 28, 8], fill=dark, outline=O)
        d.rectangle([8, 13, 23, 27], fill=fur, outline=O)        # body
        d.rectangle([13, 17, 18, 24], fill=face)
        ax = 2 if f == 0 else 5
        d.rectangle([ax, 14, ax + 5, 24], fill=fur, outline=O)   # arms
        d.rectangle([31 - ax - 5, 14, 31 - ax, 24], fill=fur, outline=O)
        d.rectangle([9, 27, 14, 31], fill=fur, outline=O)        # feet
        d.rectangle([17, 27, 22, 31], fill=fur, outline=O)
        out.append(img)
    return out


def princess_frames():
    out = []
    for f in range(2):
        img = canvas(16, 16)
        d = draw(img)
        d.ellipse([4, 0, 11, 7], fill=(248, 216, 96, 255), outline=O)   # hair
        d.ellipse([5, 2, 10, 8], fill=PAL['s'])
        d.point([(6, 4), (9, 4)], fill=O)
        d.polygon([(3, 15), (12, 15), (10, 8), (5, 8)], fill=(236, 88, 176, 255), outline=O)
        arm = 8 if f == 0 else 6
        d.line([3, arm, 5, 10], fill=PAL['s'])
        d.line([12, arm, 10, 10], fill=PAL['s'])
        out.append(img)
    return out


def tile(kind, frame=0):
    img = canvas(8, 8)
    d = draw(img)
    if kind == 'girder':
        d.rectangle([0, 0, 7, 7], fill=(216, 40, 88, 255))
        d.line([0, 0, 7, 0], fill=(255, 140, 170, 255))
        d.line([0, 7, 7, 7], fill=(120, 12, 40, 255))
        d.point([(2, 3), (5, 5)], fill=(255, 200, 210, 255))       # rivets
    elif kind == 'ladder':
        d.line([1, 0, 1, 7], fill=(20, 110, 148, 255))
        d.line([6, 0, 6, 7], fill=(20, 110, 148, 255))
        d.line([1, 2, 6, 2], fill=(52, 158, 196, 255))
        d.line([1, 6, 6, 6], fill=(52, 158, 196, 255))
    elif kind == 'ladder_broken':
        d.line([1, 0, 1, 3], fill=(20, 110, 148, 255))
        d.line([6, 0, 6, 3], fill=(20, 110, 148, 255))
        d.line([1, 2, 6, 2], fill=(52, 158, 196, 255))
    elif kind == 'conveyor':
        d.rectangle([0, 0, 7, 7], fill=(96, 98, 116, 255))
        for x in range(-8, 8, 4):
            xx = (x + frame * 2) % 8
            d.line([xx, 0, xx, 7], fill=(158, 162, 184, 255))
        d.line([0, 0, 7, 0], fill=(186, 190, 210, 255))
    elif kind == 'rivet':
        d.ellipse([1, 1, 6, 6], fill=(36, 168, 184, 255), outline=(10, 60, 84, 255))
        d.point([(3, 3)], fill=(190, 244, 250, 255))
    elif kind == 'oil':
        d.rectangle([0, 0, 7, 7], fill=(40, 40, 48, 255), outline=O)
    return img


def pie_frames():
    """A cement pie from the 50m factory. Two frames of wobble so it reads as
    something wet being carried rather than a wheel rolling."""
    out = []
    crust, filling, shine = (208, 176, 120, 255), (150, 120, 74, 255), (240, 220, 180, 255)
    for f in range(2):
        img = canvas(16, 12)
        d = draw(img)
        lift = 1 if f else 0
        d.rectangle([1, 8 - lift, 14, 11], fill=(120, 116, 110, 255), outline=O)   # tray
        d.ellipse([2, 2 - lift, 13, 9 - lift], fill=crust, outline=O)
        d.ellipse([4, 3 - lift, 11, 7 - lift], fill=filling)
        d.line([5, 4 - lift, 8, 4 - lift], fill=shine)
        out.append(img)
    return out


def spring_frames():
    """A 75m spring. Extended and compressed, so a bounce reads at 12 pixels.
    The coils must have visible gaps or the sprite is just a grey block - which
    is exactly what the first version of this was."""
    out = []
    coil, hi, dark = (108, 112, 132, 255), (150, 154, 176, 255), (52, 56, 72, 255)
    for f in range(2):
        img = canvas(12, 12)
        d = draw(img)
        if f == 0:
            cap_y, coils = 0, (3, 5, 7)          # extended
        else:
            cap_y, coils = 4, (6, 8)             # compressed
        d.rectangle([1, cap_y, 10, cap_y + 1], fill=hi, outline=O)
        for y in coils:
            d.line([2, y, 9, y], fill=coil)
            d.point([(2, y), (9, y)], fill=dark)
        d.rectangle([1, 10, 10, 11], fill=hi, outline=O)
        out.append(img)
    return out


def life_icon():
    img = canvas(8, 8)
    d = draw(img)
    d.ellipse([2, 0, 5, 3], fill=PAL['h'])
    d.rectangle([2, 3, 5, 6], fill=PAL['b'])
    d.point([(2, 7), (5, 7)], fill=O)
    return img


def build(outdir):
    os.makedirs(outdir, exist_ok=True)
    p = lambda n: os.path.join(outdir, n)
    art = lambda rows: from_art(rows, PAL)

    save(sheet([art(IDLE), art(RUN1), art(RUN2), art(RUN3), art(JUMP)], 12, 16), p("jack_run.png"))
    save(sheet([art(CLIMB1), art(CLIMB2)], 12, 16), p("jack_climb.png"))
    save(sheet([art(HAMMER_UP), art(HAMMER_DN)], 12, 16), p("jack_hammer.png"))
    save(sheet([art(DEAD)], 12, 16), p("jack_dead.png"))
    save(sheet(barrel_frames(), 16, 16), p("barrel.png"))
    save(sheet(fireball_frames(), 16, 16), p("fireball.png"))
    save(hammer_item(), p("hammer.png"))
    save(sheet(gorilla_frames(), 32, 32), p("gorilla.png"))
    save(sheet(princess_frames(), 16, 16), p("princess.png"))
    save(sheet([tile('girder'), tile('ladder'), tile('ladder_broken'),
                tile('conveyor', 0), tile('conveyor', 1), tile('rivet'), tile('oil')], 8, 8),
         p("tiles.png"))
    save(sheet(pie_frames(), 16, 12), p("pie.png"))
    save(sheet(spring_frames(), 12, 12), p("spring.png"))
    save(life_icon(), p("life.png"))
    return outdir


if __name__ == "__main__":
    import sys
    print(build(sys.argv[1] if len(sys.argv) > 1 else "out"))
