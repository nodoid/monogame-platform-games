"""Tiny drawing helpers shared by the asset generators."""
from PIL import Image, ImageDraw

TRANSPARENT = (0, 0, 0, 0)


def canvas(w, h, fill=TRANSPARENT):
    return Image.new("RGBA", (w, h), fill)


def from_art(rows, palette, scale=1):
    """Build an image from a list of equal-length strings and a char->RGBA map."""
    h = len(rows)
    w = max(len(r) for r in rows)
    img = canvas(w, h)
    px = img.load()
    for y, row in enumerate(rows):
        for x, ch in enumerate(row):
            col = palette.get(ch)
            if col:
                px[x, y] = col
    if scale != 1:
        img = img.resize((w * scale, h * scale), Image.NEAREST)
    return img


def flip(img):
    return img.transpose(Image.FLIP_LEFT_RIGHT)


def sheet(frames, cell_w=None, cell_h=None, cols=None):
    """Pack frames left-to-right into one strip (or grid) of uniform cells."""
    cw = cell_w or max(f.width for f in frames)
    ch = cell_h or max(f.height for f in frames)
    cols = cols or len(frames)
    rows = (len(frames) + cols - 1) // cols
    out = canvas(cw * cols, ch * rows)
    for i, f in enumerate(frames):
        cx, cy = (i % cols) * cw, (i // cols) * ch
        out.alpha_composite(f, (cx + (cw - f.width) // 2, cy + (ch - f.height)))
    return out


def outline(img, colour=(0, 0, 0, 255)):
    """Add a 1px silhouette outline; keeps sprites readable on busy backgrounds."""
    w, h = img.size
    out = canvas(w + 2, h + 2)
    src = img.load()
    dst = out.load()
    for y in range(h):
        for x in range(w):
            if src[x, y][3] > 0:
                for dy in (-1, 0, 1):
                    for dx in (-1, 0, 1):
                        dst[x + 1 + dx, y + 1 + dy] = colour
    out.alpha_composite(img, (1, 1))
    return out


def draw(img):
    return ImageDraw.Draw(img)


def save(img, path):
    img.save(path, "PNG", optimize=True)
    return path
