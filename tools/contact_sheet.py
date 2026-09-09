"""Lays every captured figure out on one page, so they can be reviewed at once.

A figure that came out as GET READY, a game over screen or a blank fade is
obvious on a contact sheet and invisible one file at a time."""
import os
import sys

from PIL import Image, ImageDraw

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import chapters

FIGS = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "book", "figures")
CELL = 300


def main(out="/tmp/figures.png", cols=8):
    cells = []
    for ch in chapters.ALL:
        p = os.path.join(FIGS, ch["id"] + ".png")
        cell = Image.new("RGB", (CELL, CELL), (24, 24, 28))
        if os.path.exists(p):
            im = Image.open(p).convert("RGB")
            im.thumbnail((CELL - 6, CELL - 24))
            cell.paste(im, ((CELL - im.width) // 2, 20 + (CELL - 24 - im.height) // 2))
        ImageDraw.Draw(cell).text((6, 5), f"{ch['id']} {ch['short']}", (255, 210, 90))
        cells.append(cell)
    rows = (len(cells) + cols - 1) // cols
    sheet = Image.new("RGB", (CELL * cols, CELL * rows), (12, 12, 14))
    for i, c in enumerate(cells):
        sheet.paste(c, (CELL * (i % cols), CELL * (i // cols)))
    sheet.save(out)
    print(out, sheet.size)


if __name__ == "__main__":
    args = sys.argv[1:]
    main(args[0] if args else "/tmp/figures.png",
         int(args[1]) if len(args) > 1 else 8)
