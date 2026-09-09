"""Builds the whole book as one EPUB 3.

The same chapter sources the PDF uses, converted to XHTML instead of ODF: one
document per chapter, a generated table of contents, and the front and back
covers as the first and last pages. Reflowable, so the code listings are set in
a monospaced face at a size that survives a phone screen.
"""
import html
import os
import re
import sys
import zipfile

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import chapters

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), ".."))
BOOK = os.path.join(ROOT, "book")
SRC = os.path.join(BOOK, "src")
FIGS = os.path.join(BOOK, "figures")

TITLE = "Using MonoGame to Create Platform Games"
SUBTITLE = "Build two complete arcade platform games in C# and ship them to Android and iOS"
AUTHOR = "Paul F. Johnson"
UID = "urn:uuid:5d0f8a4e-2c1b-4a77-9f3e-6b2a1c9d4e70"

CSS = """\
html { font-size: 100%; }
body { font-family: Georgia, "Times New Roman", serif; line-height: 1.5;
       margin: 0 5%; text-align: left; }
h1 { font-size: 1.7em; line-height: 1.2; margin: 2em 0 0.8em; }
h2 { font-size: 1.3em; margin: 1.8em 0 0.5em; }
h3 { font-size: 1.1em; margin: 1.5em 0 0.4em; }
h4 { font-size: 1em; font-style: italic; margin: 1.3em 0 0.3em; }
p { margin: 0 0 0.8em; }
code, pre { font-family: "DejaVu Sans Mono", "Courier New", monospace; }
code { font-size: 0.88em; }
pre { font-size: 0.78em; line-height: 1.35; white-space: pre-wrap;
      word-wrap: break-word; background: #f4f4f2; border-left: 3px solid #c8c8c4;
      padding: 0.6em 0.8em; margin: 0 0 1em; }
blockquote.tip, aside.info { background: #f2f5f7; border-left: 3px solid #7a9bb0;
      padding: 0.6em 0.9em; margin: 0 0 1em; font-size: 0.95em; }
aside.info { border-left-color: #d6a12a; background: #faf6ec; }
p.command { font-family: "DejaVu Sans Mono", "Courier New", monospace;
      font-size: 0.82em; background: #f4f4f2; padding: 0.4em 0.6em; margin: 0 0 0.5em; }
p.quote { font-style: italic; margin: 0 2em 1em; }
p.figure { text-align: center; font-size: 0.85em; color: #555; margin: 0.3em 0 1.2em; }
img { max-width: 100%; height: auto; }
div.cover { margin: 0; padding: 0; text-align: center; }
div.cover img { max-width: 100%; max-height: 100%; }
h1.booktitle { font-size: 2.1em; margin-top: 3em; }
p.subtitle { font-size: 1.1em; color: #444; }
p.author { font-size: 1.2em; margin-top: 2em; }
nav#toc ol { list-style: none; padding-left: 0; }
nav#toc ol ol { padding-left: 1.2em; }
nav#toc li { margin: 0.25em 0; }
"""

_INLINE = re.compile(r"(`[^`]+`|\*\*[^*]+\*\*|\*[^*]+\*)")


def esc(s):
    return html.escape(s, quote=False)


def inline(text):
    out = []
    for part in _INLINE.split(text):
        if not part:
            continue
        if part.startswith("`") and part.endswith("`") and len(part) > 1:
            out.append("<code>" + esc(part[1:-1]) + "</code>")
        elif part.startswith("**") and part.endswith("**") and len(part) > 3:
            out.append("<strong>" + esc(part[2:-2]) + "</strong>")
        elif part.startswith("*") and part.endswith("*") and len(part) > 1:
            out.append("<em>" + esc(part[1:-1]) + "</em>")
        else:
            out.append(esc(part))
    return "".join(out)


def convert(text, figure):
    """The book's markup to XHTML. Mirrors gen_book.convert construct for construct."""
    out, lines, i = [], text.split("\n"), 0
    bullets, kind = [], None

    def flush():
        nonlocal bullets, kind
        if bullets:
            tag = "ol" if kind == "number" else "ul"
            out.append(f"<{tag}>" + "".join(f"<li>{inline(b)}</li>" for b in bullets) + f"</{tag}>\n")
            bullets, kind = [], None

    while i < len(lines):
        s = lines[i].rstrip()
        t = s.strip()

        if t.startswith("```"):
            flush()
            i += 1
            block = []
            while i < len(lines) and not lines[i].strip().startswith("```"):
                block.append(lines[i])
                i += 1
            i += 1
            if block:
                out.append("<pre><code>" + esc("\n".join(block)) + "</code></pre>\n")
            continue

        if not t:
            flush()
            i += 1
            continue

        m = re.match(r"^(#{1,4})\s+(.*)$", t)
        if m:
            flush()
            n = len(m.group(1))
            out.append(f"<h{n}>{inline(m.group(2))}</h{n}>\n")
            i += 1
            continue

        if t.startswith("-- "):
            if kind != "bullet2":
                flush()
            kind, _ = "bullet2", bullets.append(t[3:])
            i += 1
            continue
        if t.startswith("- "):
            if kind != "bullet":
                flush()
            kind, _ = "bullet", bullets.append(t[2:])
            i += 1
            continue
        m = re.match(r"^\d+\.\s+(.*)$", t)
        if m:
            if kind != "number":
                flush()
            kind, _ = "number", bullets.append(m.group(1))
            i += 1
            continue

        flush()
        if t.startswith("> "):
            out.append(f'<blockquote class="tip"><p>{inline(t[2:])}</p></blockquote>\n')
        elif t.startswith("! "):
            out.append(f'<aside class="info"><p>{inline(t[2:])}</p></aside>\n')
        elif t.startswith("$ "):
            out.append(f'<p class="command">{esc(t[2:])}</p>\n')
        elif t.startswith('" '):
            out.append(f'<p class="quote">{inline(t[2:])}</p>\n')
        elif t.startswith("[fig] "):
            if figure:
                out.append(f'<p class="figure"><img src="images/{figure}" alt=""/></p>\n')
                figure = None
            out.append(f'<p class="figure">{inline(t[6:])}</p>\n')
        else:
            out.append(f"<p>{inline(t)}</p>\n")
        i += 1

    flush()
    return "".join(out)


def page(title, body, css="style.css"):
    return f"""<?xml version="1.0" encoding="utf-8"?>
<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml" xmlns:epub="http://www.idpf.org/2007/ops"
      lang="en-GB" xml:lang="en-GB">
<head><meta charset="utf-8"/><title>{esc(title)}</title>
<link rel="stylesheet" type="text/css" href="{css}"/></head>
<body>
{body}</body>
</html>
"""


def main():
    out = os.path.join(BOOK, "Using MonoGame to Create Platform Games.epub")
    docs, images, nav_items = [], {}, []

    docs.append(("cover.xhtml", page("Cover",
        '<div class="cover"><img src="images/cover_front.jpg" alt="Cover"/></div>\n')))
    images["cover_front.jpg"] = os.path.join(BOOK, "cover_front.jpg")

    docs.append(("title.xhtml", page("Title page",
        f'<h1 class="booktitle">{esc(TITLE)}</h1>\n'
        f'<p class="subtitle">{esc(SUBTITLE)}</p>\n'
        f'<p class="author">{esc(AUTHOR)}</p>\n'
        f'<p><a href="https://github.com/nodoid/monogame-platform-games">'
        f'github.com/nodoid/monogame-platform-games</a></p>\n')))

    for part, name, first, last in chapters.PARTS:
        kids = []
        for ch in chapters.ALL:
            if not first <= ch["num"] <= last:
                continue
            cid = ch["id"]
            text = open(os.path.join(SRC, cid + ".txt"), encoding="utf-8").read()
            fig = None
            shot = os.path.join(FIGS, cid + ".png")
            if os.path.exists(shot):
                fig = cid + ".png"
                images[fig] = shot
            docs.append((cid + ".xhtml", page(ch["title"], convert(text, fig))))
            kids.append((f"{cid}.xhtml", f"{ch['num']}. {ch['title']}"))
        nav_items.append((f"Part {part}: {name}", kids))

    docs.append(("backcover.xhtml", page("About this book",
        '<div class="cover"><img src="images/cover_back.jpg" alt="Back cover"/></div>\n')))
    images["cover_back.jpg"] = os.path.join(BOOK, "cover_back.jpg")

    nav = ['<nav epub:type="toc" id="toc"><h1>Contents</h1><ol>']
    for part_title, kids in nav_items:
        # A nav list item must open with an anchor or a span; bare text is
        # invalid EPUB and epubcheck rejects it.
        nav.append(f"<li><span>{esc(part_title)}</span><ol>")
        for href, label in kids:
            nav.append(f'<li><a href="{href}">{esc(label)}</a></li>')
        nav.append("</ol></li>")
    nav.append("</ol></nav>")
    docs.insert(2, ("nav.xhtml", page("Contents", "".join(nav))))

    media = {".jpg": "image/jpeg", ".jpeg": "image/jpeg", ".png": "image/png"}
    manifest = ['<item id="css" href="style.css" media-type="text/css"/>',
                '<item id="nav" href="nav.xhtml" media-type="application/xhtml+xml" '
                'properties="nav"/>']
    spine = []
    for name, _ in docs:
        ident = name.replace(".xhtml", "")
        if name == "nav.xhtml":
            spine.append('<itemref idref="nav"/>')
            continue
        props = ' properties="svg"' if False else ""
        manifest.append(f'<item id="{ident}" href="{name}" '
                        f'media-type="application/xhtml+xml"{props}/>')
        spine.append(f'<itemref idref="{ident}"/>')
    for name in images:
        ident = "img_" + re.sub(r"\W", "_", name)
        cover = ' properties="cover-image"' if name == "cover_front.jpg" else ""
        manifest.append(f'<item id="{ident}" href="images/{name}" '
                        f'media-type="{media[os.path.splitext(name)[1].lower()]}"{cover}/>')

    opf = f"""<?xml version="1.0" encoding="utf-8"?>
<package xmlns="http://www.idpf.org/2007/opf" version="3.0" unique-identifier="bookid"
         xml:lang="en-GB">
  <metadata xmlns:dc="http://purl.org/dc/elements/1.1/">
    <dc:identifier id="bookid">{UID}</dc:identifier>
    <dc:title>{esc(TITLE)}</dc:title>
    <dc:creator>{esc(AUTHOR)}</dc:creator>
    <dc:language>en-GB</dc:language>
    <dc:description>{esc(SUBTITLE)}</dc:description>
    <meta property="dcterms:modified">2026-09-09T00:00:00Z</meta>
  </metadata>
  <manifest>
    {"".join(manifest)}
  </manifest>
  <spine>
    {"".join(spine)}
  </spine>
</package>
"""

    container = """<?xml version="1.0" encoding="utf-8"?>
<container version="1.0" xmlns="urn:oasis:names:tc:opendocument:xmlns:container">
  <rootfiles><rootfile full-path="OEBPS/content.opf"
    media-type="application/oebps-package+xml"/></rootfiles>
</container>
"""

    with zipfile.ZipFile(out, "w", zipfile.ZIP_DEFLATED) as z:
        zi = zipfile.ZipInfo("mimetype")
        zi.compress_type = zipfile.ZIP_STORED
        z.writestr(zi, "application/epub+zip")
        z.writestr("META-INF/container.xml", container)
        z.writestr("OEBPS/content.opf", opf)
        z.writestr("OEBPS/style.css", CSS)
        for name, body in docs:
            z.writestr("OEBPS/" + name, body)
        for name, path in images.items():
            z.write(path, "OEBPS/images/" + name)

    print(out, f"{os.path.getsize(out) / 1e6:.1f} MB,",
          len(docs), "documents,", len(images), "images")
    return 0


if __name__ == "__main__":
    sys.exit(main())
