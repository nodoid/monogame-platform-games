"""Launcher icons and splash screens for both games, at every density each platform wants."""
import os, math
from PIL import Image
from artlib import canvas, draw, save, from_art
import pixelfont

ANDROID_ICON = {'mdpi': 48, 'hdpi': 72, 'xhdpi': 96, 'xxhdpi': 144, 'xxxhdpi': 192}
ANDROID_SPLASH = {'mdpi': (320, 480), 'hdpi': (480, 800), 'xhdpi': (720, 1280),
                  'xxhdpi': (1080, 1920), 'xxxhdpi': (1440, 2560)}


def text(img, s, x, y, colour, scale=1):
    """Draw with the 5x7 game font so the splash matches the in-game HUD."""
    d = draw(img)
    for i, ch in enumerate(s.upper()):
        g = pixelfont.GLYPHS.get(ch)
        if not g:
            continue
        for gy, row in enumerate(g):
            for gx, c in enumerate(row):
                if c == '1':
                    d.rectangle([x + (i * 6 + gx) * scale, y + gy * scale,
                                 x + (i * 6 + gx) * scale + scale - 1,
                                 y + gy * scale + scale - 1], fill=colour)
    return len(s) * 6 * scale


def text_w(s, scale=1):
    return len(s) * 6 * scale


# ------------------------------------------------------------ climber icon ---
def climber_icon(size):
    """Ladders. Cyan ladders climbing red girders on the theme's pale ground -
    the silhouette that says 'climbing game' at 48 pixels."""
    S = 48
    img = canvas(S, S, (238, 238, 234, 255))
    d = draw(img)
    girder, hi, lo = (216, 40, 88, 255), (255, 150, 180, 255), (120, 12, 40, 255)
    for gy in (10, 22, 34, 45):
        d.rectangle([0, gy, S - 1, gy + 3], fill=girder)
        d.line([0, gy, S - 1, gy], fill=hi)
        d.line([0, gy + 3, S - 1, gy + 3], fill=lo)
        for rx in range(3, S, 8):
            d.point([(rx, gy + 2)], fill=(255, 210, 220, 255))
    rail, rung = (20, 110, 148, 255), (52, 158, 196, 255)
    for lx, top, bot in ((8, 12, 24), (30, 24, 36), (14, 36, 47)):
        d.rectangle([lx, top, lx + 1, bot], fill=rail)
        d.rectangle([lx + 9, top, lx + 10, bot], fill=rail)
        for ry in range(top + 2, bot, 4):
            d.rectangle([lx, ry, lx + 10, ry], fill=rung)
    # a barrel rolling on the top girder anchors the theme
    d.ellipse([34, 3, 44, 10], fill=(192, 96, 24, 255), outline=(0, 0, 0, 255))
    d.line([36, 5, 42, 5], fill=(232, 176, 64, 255))
    d.line([36, 8, 42, 8], fill=(232, 176, 64, 255))
    return img.resize((size, size), Image.NEAREST)


# ------------------------------------------------------------ hunch icon -----
def hunch_icon(size):
    """A jumping character, mid-leap over a battlement gap, against a daylight sky."""
    S = 48
    img = canvas(S, S, (186, 212, 230, 255))
    d = draw(img)
    d.ellipse([2, 2, 12, 12], fill=(255, 240, 190, 255))       # sun, clear of the figure
    d.ellipse([4, 4, 10, 10], fill=(255, 252, 232, 255))
    for cx, cy, cw in ((30, 3, 14), (36, 12, 10), (26, 19, 9)):  # clouds, upper right
        d.ellipse([cx, cy, cx + cw, cy + 5], fill=(250, 251, 252, 255))
    stone, light, dark = (128, 122, 110, 255), (172, 166, 152, 255), (74, 70, 62, 255)
    for x0, x1 in ((0, 15), (33, 47)):                          # two ledges, gap between
        d.rectangle([x0, 36, x1, 47], fill=stone)
        d.line([x0, 36, x1, 36], fill=light)
        for bx in range(x0, x1, 6):                             # battlement teeth
            d.rectangle([bx, 32, bx + 3, 36], fill=stone)
            d.line([bx, 32, bx + 3, 32], fill=light)
        for by in range(40, 48, 4):
            d.line([x0, by, x1, by], fill=dark)
    from gen_hunch_art import JUMP, PAL
    hero = from_art(JUMP, PAL)                                  # reuse the real sprite
    hero = hero.resize((hero.width * 2, hero.height * 2), Image.NEAREST)
    img.alpha_composite(hero, (11, 6))
    return img.resize((size, size), Image.NEAREST)


# ----------------------------------------------------------------- splash ----
def climber_splash(w, h):
    img = canvas(w, h, (238, 238, 234, 255))
    art = climber_icon(min(w, h) // 2)
    img.alpha_composite(art, ((w - art.width) // 2, h // 6))
    s = max(1, w // 90)
    t1, t2 = "MONKEY CLIMBER", "CLIMB OR DIE"
    text(img, t1, (w - text_w(t1, s)) // 2, h // 6 + art.height + h // 20, (26, 28, 34, 255), s)
    s2 = max(1, s // 2)
    text(img, t2, (w - text_w(t2, s2)) // 2, h // 6 + art.height + h // 20 + 10 * s,
         (164, 84, 0, 255), s2)
    return img


def hunch_splash(w, h):
    img = canvas(w, h, (240, 244, 246, 255))
    art = hunch_icon(min(w, h) // 2)
    img.alpha_composite(art, ((w - art.width) // 2, h // 6))
    s = max(1, w // 90)
    t1, t2 = "RUN AND JUMP", "RING EVERY BELL"
    text(img, t1, (w - text_w(t1, s)) // 2, h // 6 + art.height + h // 20, (26, 28, 34, 255), s)
    s2 = max(1, s // 2)
    text(img, t2, (w - text_w(t2, s2)) // 2, h // 6 + art.height + h // 20 + 10 * s,
         (22, 116, 52, 255), s2)
    return img


def font_sheet():
    """ASCII 32..95 as white 8x8 cells; the games tint it at draw time."""
    cols, cell = pixelfont.COLS, pixelfont.CELL
    rows = (pixelfont.LAST - pixelfont.FIRST + cols) // cols
    img = canvas(cols * cell, rows * cell)
    d = draw(img)
    for ch, ox, oy in pixelfont.sheet_layout():
        g = pixelfont.GLYPHS.get(ch)
        if not g:
            continue
        for gy, row in enumerate(g):
            for gx, c in enumerate(row):
                if c == '1':
                    d.point([(ox + gx, oy + gy)], fill=(255, 255, 255, 255))
    return img


def build(base):
    for game, icon_fn, splash_fn in (("climber", climber_icon, climber_splash),
                                     ("hunch", hunch_icon, hunch_splash)):
        out = os.path.join(base, game)
        os.makedirs(out, exist_ok=True)
        for dpi, px in ANDROID_ICON.items():
            os.makedirs(os.path.join(out, "icon"), exist_ok=True)
            save(icon_fn(px), os.path.join(out, "icon", f"ic_launcher_{dpi}.png"))
        save(icon_fn(1024), os.path.join(out, "icon", "ios_appicon_1024.png"))
        for dpi, (w, h) in ANDROID_SPLASH.items():
            os.makedirs(os.path.join(out, "splash"), exist_ok=True)
            save(splash_fn(w, h), os.path.join(out, "splash", f"splash_{dpi}.png"))
        for tag, (w, h) in (("1x", (640, 960)), ("2x", (1280, 1920)), ("3x", (1920, 2880))):
            save(splash_fn(w, h), os.path.join(out, "splash", f"ios_splash@{tag}.png"))
    shared = os.path.join(base, "shared")
    os.makedirs(shared, exist_ok=True)
    save(font_sheet(), os.path.join(shared, "font.png"))
    return base


if __name__ == "__main__":
    import sys
    print(build(sys.argv[1] if len(sys.argv) > 1 else "out"))
