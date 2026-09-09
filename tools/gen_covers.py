"""Generates the book's front and back covers as print-resolution JPEGs.

The layout follows the house style of a technical publisher's covers - a dark
field, a category line, a large light title, a rule, a subtitle, artwork in the
lower half and the author's name at the foot - with no publisher branding of any
kind. The artwork is not stock: it is two of the book's own captured figures,
the finished climber and the finished runner, photographed on device.
"""
import os
import sys

from PIL import Image, ImageDraw, ImageFilter, ImageFont

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), ".."))
FIGS = os.path.join(ROOT, "book", "figures")
OUT = os.path.join(ROOT, "book")

# 7.5 x 9.25 inches at 300 dpi - a common trade paperback trim.
W, H = 2250, 2775
MARGIN = 170
CONTENT = W - MARGIN * 2

INK = (255, 255, 255)
DIM = (183, 205, 218)
FAINT = (140, 166, 182)
ACCENT = (242, 164, 19)
PANEL = (20, 54, 76)

TITLE = "Using MonoGame to Create Platform Games"
SUBTITLE = ("Build two complete arcade platform games in C# and ship them "
            "to Android and iOS")
AUTHOR = "Paul F. Johnson"
CATEGORY = "GAME DEVELOPMENT"

FONT = "/System/Library/Fonts/HelveticaNeue.ttc"
BOLD, REGULAR, LIGHT, MEDIUM = 1, 0, 7, 10


def font(size, weight=REGULAR):
    return ImageFont.truetype(FONT, size, index=weight)


def background():
    """A vertical gradient, deep at the top and a little warmer at the foot."""
    top, bottom = (9, 28, 43), (17, 62, 84)
    im = Image.new("RGB", (W, H))
    d = ImageDraw.Draw(im)
    for y in range(H):
        t = y / (H - 1)
        d.line([(0, y), (W, y)],
               fill=tuple(int(top[i] + (bottom[i] - top[i]) * t) for i in range(3)))
    d.rectangle([0, 0, W, 22], fill=ACCENT)
    return im


def wrap(draw, text, f, width):
    lines, line = [], ""
    for word in text.split():
        trial = f"{line} {word}".strip()
        if draw.textlength(trial, font=f) <= width:
            line = trial
        else:
            if line:
                lines.append(line)
            line = word
    if line:
        lines.append(line)
    return lines


def block(draw, text, f, colour, x, y, width, leading=1.35, spacing=0):
    """Draw wrapped text and return the y just below it."""
    lh = int(f.size * leading)
    for line in wrap(draw, text, f, width):
        if spacing:
            cx = x
            for ch in line:
                draw.text((cx, y), ch, font=f, fill=colour)
                cx += draw.textlength(ch, font=f) + spacing
        else:
            draw.text((x, y), line, font=f, fill=colour)
        y += lh
    return y


def shadowed(im, art, box, border=12):
    """Paste artwork in a white keyline with a soft shadow under it."""
    x, y, w, h = box
    art = art.convert("RGB")
    art.thumbnail((w, h), Image.LANCZOS)
    aw, ah = art.size

    shadow = Image.new("RGBA", (aw + border * 2 + 90, ah + border * 2 + 90), (0, 0, 0, 0))
    ImageDraw.Draw(shadow).rectangle(
        [45, 52, 45 + aw + border * 2, 52 + ah + border * 2], fill=(0, 0, 0, 150))
    shadow = shadow.filter(ImageFilter.GaussianBlur(24))
    im.paste(shadow, (x - 45, y - 45), shadow)

    card = Image.new("RGB", (aw + border * 2, ah + border * 2), (255, 255, 255))
    card.paste(art, (border, border))
    im.paste(card, (x, y))
    return aw + border * 2, ah + border * 2


# ------------------------------------------------------------------- front ---
def front():
    im = background()
    d = ImageDraw.Draw(im)

    y = 300
    block(d, CATEGORY, font(40, MEDIUM), ACCENT, MARGIN, y, CONTENT, spacing=9)

    y = 400
    f = font(146, BOLD)
    for line in wrap(d, TITLE, f, CONTENT):
        d.text((MARGIN, y), line, font=f, fill=INK)
        y += int(f.size * 1.12)

    y += 34
    d.rectangle([MARGIN, y, MARGIN + 360, y + 8], fill=ACCENT)

    y += 74
    y = block(d, SUBTITLE, font(54, LIGHT), DIM, MARGIN, y, CONTENT - 120, 1.42)

    # The two finished games, as they were photographed on device.
    art_y = 1330
    climber = Image.open(os.path.join(FIGS, "ch26.png"))
    runner = Image.open(os.path.join(FIGS, "ch42.png"))
    cw, chh = shadowed(im, climber, (MARGIN, art_y, 770, 880))
    ry = art_y + (chh - 720) // 2
    rw, rh = shadowed(im, runner, (MARGIN + cw + 70, ry, 1010, 720))

    d.text((MARGIN, art_y + chh + 46), "MONKEY CLIMBER", font=font(34, MEDIUM), fill=FAINT)
    print("front: artwork bottom", art_y + chh + 46 + 34, "rule at", H - 330)
    d.text((MARGIN + cw + 70, ry + rh + 46), "RUN AND JUMP",
           font=font(34, MEDIUM), fill=FAINT)

    d.rectangle([MARGIN, H - 330, MARGIN + CONTENT, H - 326], fill=(46, 88, 112))
    d.text((MARGIN, H - 268), AUTHOR, font=font(72, BOLD), fill=INK)
    return im


# -------------------------------------------------------------------- back ---
BLURB = [
    "Two games teach you far more than one. This book builds a single-screen "
    "climber and a flick-screen run-and-jump in C# and MonoGame, and ships both "
    "of them to Android and iOS from a single codebase.",

    "Every one of the forty-six chapters ends in an application you can run - "
    "ninety-two projects in all, each complete on both platforms. Nothing is "
    "left as an exercise, and nothing is sourced from elsewhere: every sprite, "
    "tile, icon, sound effect and music loop is generated by a script the book "
    "walks you through.",

    "The chapters explain why, not only how. Why a fixed timestep. Why collision "
    "is resolved one axis at a time. Why a committed jump suits one game and a "
    "variable-height jump the other. And why a default that is harmless on a "
    "desktop can quietly draw your game at a third of its size on a phone.",
]

LEARN = [
    "Structure a MonoGame project that targets Android and iOS from one codebase",
    "Write a fixed-timestep loop and a virtual screen that scales to any panel",
    "Build two different movement and collision models, and know which to reach for",
    "Generate sprites, tiles, icons, splash screens and chiptune audio from code",
    "Design levels that teach an idea, test it, then combine it with the last one",
    "Add scoring, lives, high score tables and saves that survive a reinstall",
    "Profile, package and release, with a pre-flight check that runs on the device",
]

FOR_WHOM = ("C# developers who want to write games, and game developers new to "
            "MonoGame or to mobile. You should be comfortable with C# and object "
            "orientation; no graphics, audio or mobile experience is assumed.")


def back():
    im = background()
    d = ImageDraw.Draw(im)

    y = 300
    f = font(76, BOLD)
    for line in wrap(d, TITLE, f, CONTENT):
        d.text((MARGIN, y), line, font=f, fill=INK)
        y += int(f.size * 1.14)

    y += 20
    d.rectangle([MARGIN, y, MARGIN + 250, y + 6], fill=ACCENT)
    y += 78

    for para in BLURB:
        y = block(d, para, font(44, LIGHT), DIM, MARGIN, y, CONTENT, 1.46)
        y += 34

    y += 26
    panel_top = y
    d.rectangle([MARGIN - 44, panel_top, W - MARGIN + 44, panel_top + 4], fill=(46, 88, 112))
    y += 60
    d.text((MARGIN, y), "What you will learn", font=font(52, BOLD), fill=ACCENT)
    y += 96

    bullet = font(40, LIGHT)
    for item in LEARN:
        d.ellipse([MARGIN + 6, y + 16, MARGIN + 22, y + 32], fill=ACCENT)
        y = block(d, item, bullet, DIM, MARGIN + 56, y, CONTENT - 56, 1.34)
        y += 16

    y += 40
    d.text((MARGIN, y), "Who this book is for", font=font(52, BOLD), fill=ACCENT)
    y += 96
    y = block(d, FOR_WHOM, font(40, LIGHT), DIM, MARGIN, y, CONTENT, 1.40)

    d.rectangle([MARGIN, H - 330, MARGIN + CONTENT, H - 326], fill=(46, 88, 112))
    d.text((MARGIN, H - 268), AUTHOR, font=font(60, BOLD), fill=INK)
    print("back cover text ends at y =", y, "of", H - 330)
    return im


def main():
    os.makedirs(OUT, exist_ok=True)
    for name, im in (("cover_front", front()), ("cover_back", back())):
        p = os.path.join(OUT, name + ".jpg")
        im.save(p, "JPEG", quality=94, subsampling=0, dpi=(300, 300))
        print(p, im.size)


if __name__ == "__main__":
    main()
