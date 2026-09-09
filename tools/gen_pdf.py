"""Builds the whole book as one PDF, cover to cover.

The forty-six chapter sources become a single ODF document - title page, part
dividers, every chapter starting on a fresh page - which LibreOffice paginates
and converts. The front and back covers are then rendered as pages of their own
and merged either side of it, so the PDF opens on the cover and ends on the
blurb, the way the printed book would.
"""
import os
import re
import subprocess
import sys
import tempfile

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import chapters
import gen_book
import odt

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), ".."))
BOOK = os.path.join(ROOT, "book")
SRC = os.path.join(BOOK, "src")
FIGS = os.path.join(BOOK, "figures")

TITLE = "Using MonoGame to Create Platform Games"
SUBTITLE = "Build two complete arcade platform games in C# and ship them to Android and iOS"
AUTHOR = "Paul F. Johnson"

# US Letter, which is what the template's page master says.
PAGE_W_IN, PAGE_H_IN, DPI = 8.5, 11.0, 200


def front_matter():
    """A title page, then a page break before Part Zero opens."""
    p = odt.para
    return ("".join([
        p("title", TITLE),
        p("normal", SUBTITLE),
        p("normal", ""),
        p("normal", AUTHOR),
        p("normal", ""),
        p("normal", "Two games, forty-six chapters, ninety-two runnable apps."),
        p("normal", "Source: https://github.com/nodoid/monogame-platform-games"),
    ]))


def chapter_body(name, images):
    """One chapter, with its first heading forced onto a new page."""
    text = open(os.path.join(SRC, name + ".txt"), encoding="utf-8").read()
    shot = os.path.join(FIGS, name + ".png")
    queue = []
    if os.path.exists(shot):
        from PIL import Image
        images[name + ".png"] = shot
        queue = [(name + ".png", Image.open(shot).size)]

    body = gen_book.convert(text, list(queue))
    # The chapter's own Heading 1 becomes the page break, so no blank paragraph
    # is needed and the heading keeps its template spacing.
    return body.replace(f'text:style-name="{odt.encode_style("Heading 1")}"',
                        'text:style-name="chapbreak"', 1)


def running_head(styles, text):
    """The template's master page carries a static header reading "Introduction".

    That is right for the section the template was cut from and wrong across five
    hundred pages of chapters, so the combined build swaps it for the book's
    title. The per-chapter documents keep the template's own header untouched."""
    return styles.replace(
        '<style:header><text:p text:style-name="MP1">Introduction</text:p>',
        f'<style:header><text:p text:style-name="MP1">{text}</text:p>', 1)


def combined_odt(path):
    styles = running_head(gen_book.template_styles(), TITLE)
    images, parts = {}, []
    parts.append(front_matter())
    for part, name, first, last in chapters.PARTS:
        parts.append(odt.para("h1", f"Part {part}: {name}").replace(
            f'text:style-name="{odt.encode_style("Heading 1")}"', 'text:style-name="chapbreak"'))
        for ch in chapters.ALL:
            if first <= ch["num"] <= last:
                parts.append(chapter_body(ch["id"], images))
    odt.write_odt(path, "".join(parts), styles, TITLE, AUTHOR, images=images)
    return path, len(images)


def cover_page(jpeg, out):
    """A cover as one page the same size as the body, image centred on white."""
    from PIL import Image
    w, h = int(PAGE_W_IN * DPI), int(PAGE_H_IN * DPI)
    page = Image.new("RGB", (w, h), (255, 255, 255))
    art = Image.open(jpeg).convert("RGB")
    art.thumbnail((w, h), Image.LANCZOS)
    page.paste(art, ((w - art.width) // 2, (h - art.height) // 2))
    page.save(out, "PDF", resolution=DPI)
    return out


def pages(pdf):
    return len(re.findall(rb"/Type\s*/Page[^s]", open(pdf, "rb").read()))


def main():
    tmp = tempfile.mkdtemp()
    src = os.path.join(tmp, "body.odt")
    _, n_figs = combined_odt(src)
    print(f"combined document written, {n_figs} figures")

    r = subprocess.run(["soffice", "--headless", "--convert-to", "pdf",
                        "--outdir", tmp, src], capture_output=True, text=True)
    body = os.path.join(tmp, "body.pdf")
    if not os.path.exists(body):
        print("conversion failed:", r.stdout, r.stderr)
        return 1
    print("body:", pages(body), "pages")

    front = cover_page(os.path.join(BOOK, "cover_front.jpg"), os.path.join(tmp, "front.pdf"))
    back = cover_page(os.path.join(BOOK, "cover_back.jpg"), os.path.join(tmp, "back.pdf"))

    from pypdf import PdfWriter
    out = os.path.join(BOOK, "Using MonoGame to Create Platform Games.pdf")
    w = PdfWriter()
    for p in (front, body, back):
        w.append(p)
    w.add_metadata({"/Title": TITLE, "/Author": AUTHOR,
                    "/Subject": SUBTITLE, "/Creator": "tools/gen_pdf.py"})
    with open(out, "wb") as f:
        w.write(f)
    print(out, pages(out), "pages,",
          f"{os.path.getsize(out) / 1e6:.1f} MB")
    return 0


if __name__ == "__main__":
    sys.exit(main())
