"""Writes ODF text documents that use the publisher's own paragraph styles.

The styles come straight out of the Word template in ~/Downloads: styles.xml is
copied verbatim from it, so every chapter document carries the real
`Normal [PACKT]`, `Code [PACKT]`, `Tip [PACKT]` and heading definitions rather
than approximations of them.
"""
import html
import os
import re
import zipfile

# ODF encodes spaces and punctuation in internal style names.
def encode_style(display_name):
    out = []
    for ch in display_name:
        if ch.isalnum() or ch == "_":
            out.append(ch)
        else:
            out.append("_%s_" % format(ord(ch), "02x"))
    return "".join(out)


PARA = {
    "normal": "Normal [PACKT]",
    "h1": "Heading 1",
    "h2": "Heading 2",
    "h3": "Heading 3",
    "h4": "Heading 4",
    "code": "Code [PACKT]",
    "code_end": "Code End [PACKT]",
    "bullet": "Bullet [PACKT]",
    "bullet_end": "Bullet End [PACKT]",
    "bullet2": "Bullet Within Bullet [PACKT]",
    "number": "Numbered Bullet [PACKT]",
    "number_end": "Numbered Bullet End [PACKT]",
    "tip": "Tip [PACKT]",
    "info": "Information Box [PACKT]",
    "figure": "Figure [PACKT]",
    "quote": "Quote [PACKT]",
    "command": "Command Line [PACKT]",
    "command_end": "Command Line End [PACKT]",
    "title": "Introduction Title [PACKT]",
}

TEXT = {
    "code": "Code In Text [PACKT]",
    "bold": "Bold [PACKT]",
    "italic": "Italics [PACKT]",
    "keyword": "Key Word [PACKT]",
    "url": "URL [PACKT]",
}

# The template's "end of list" styles carry no list-style reference, so inside a
# real list the last item indents differently from the ones above it. These take
# the ordinary bullet style instead and add only the extra space after the list,
# which is all the end styles were for.
LIST_END_STYLES = """<style:style style:name="bullet_end_l" style:family="paragraph"
 style:parent-style-name="Bullet_20__5b_PACKT_5d_" style:list-style-name="WW8Num13">
<style:paragraph-properties fo:margin-left="0cm" fo:text-indent="0cm"
 fo:margin-bottom="0.212cm"/></style:style>
<style:style style:name="bullet2_end_l" style:family="paragraph"
 style:parent-style-name="Bullet_20_Within_20_Bullet_20__5b_PACKT_5d_"
 style:list-style-name="WW8Num13">
<style:paragraph-properties fo:margin-left="0cm" fo:text-indent="0cm"
 fo:margin-bottom="0.212cm"/></style:style>
<style:style style:name="number_end_l" style:family="paragraph"
 style:parent-style-name="Numbered_20_Bullet_20__5b_PACKT_5d_" style:list-style-name="WW8Num1">
<style:paragraph-properties fo:margin-left="0cm" fo:text-indent="0cm"
 fo:margin-bottom="0.212cm"/></style:style>"""


# A chapter opening in the combined book starts on a fresh page.
BREAK_STYLE = """<style:style style:name="chapbreak" style:family="paragraph"
 style:parent-style-name="Heading_20_1">
<style:paragraph-properties fo:break-before="page"/>
</style:style>"""


FRAME_STYLE = """<style:style style:name="fig" style:family="graphic">
<style:graphic-properties text:anchor-type="as-char" style:vertical-pos="middle"
 style:vertical-rel="text" fo:margin-top="0.15cm" fo:margin-bottom="0.15cm"
 style:wrap="none" draw:stroke="none" draw:fill="none"/>
</style:style>"""


CONTENT_HEAD = """<?xml version="1.0" encoding="UTF-8"?>
<office:document-content
 xmlns:office="urn:oasis:names:tc:opendocument:xmlns:office:1.0"
 xmlns:style="urn:oasis:names:tc:opendocument:xmlns:style:1.0"
 xmlns:text="urn:oasis:names:tc:opendocument:xmlns:text:1.0"
 xmlns:table="urn:oasis:names:tc:opendocument:xmlns:table:1.0"
 xmlns:draw="urn:oasis:names:tc:opendocument:xmlns:drawing:1.0"
 xmlns:fo="urn:oasis:names:tc:opendocument:xmlns:xsl-fo-compatible:1.0"
 xmlns:xlink="http://www.w3.org/1999/xlink"
 xmlns:svg="urn:oasis:names:tc:opendocument:xmlns:svg-compatible:1.0"
 xmlns:loext="urn:org:documentfoundation:names:experimental:office:xmlns:loext:1.0"
 office:version="1.3">
<office:automatic-styles>__AUTOSTYLES__</office:automatic-styles>
<office:body><office:text>
"""

CONTENT_TAIL = "</office:text></office:body></office:document-content>\n"

MANIFEST_HEAD = """<?xml version="1.0" encoding="UTF-8"?>
<manifest:manifest xmlns:manifest="urn:oasis:names:tc:opendocument:xmlns:manifest:1.0" manifest:version="1.3">
 <manifest:file-entry manifest:full-path="/" manifest:media-type="application/vnd.oasis.opendocument.text"/>
 <manifest:file-entry manifest:full-path="content.xml" manifest:media-type="text/xml"/>
 <manifest:file-entry manifest:full-path="styles.xml" manifest:media-type="text/xml"/>
 <manifest:file-entry manifest:full-path="meta.xml" manifest:media-type="text/xml"/>
"""

MANIFEST_TAIL = "</manifest:manifest>\n"


def meta_xml(title, subject=""):
    return f"""<?xml version="1.0" encoding="UTF-8"?>
<office:document-meta
 xmlns:office="urn:oasis:names:tc:opendocument:xmlns:office:1.0"
 xmlns:dc="http://purl.org/dc/elements/1.1/"
 xmlns:meta="urn:oasis:names:tc:opendocument:xmlns:meta:1.0"
 office:version="1.3">
<office:meta>
<dc:title>{html.escape(title)}</dc:title>
<dc:subject>{html.escape(subject)}</dc:subject>
<meta:generator>platform_book/tools/gen_book.py</meta:generator>
</office:meta></office:document-meta>
"""


_INLINE = re.compile(r"(`[^`]+`|\*\*[^*]+\*\*|\*[^*]+\*)")


def inline(text):
    """Turn `code`, **bold** and *italic* into Packt character styles."""
    out = []
    for part in _INLINE.split(text):
        if not part:
            continue
        if part.startswith("`") and part.endswith("`") and len(part) > 1:
            out.append(span(TEXT["code"], part[1:-1]))
        elif part.startswith("**") and part.endswith("**") and len(part) > 3:
            out.append(span(TEXT["bold"], part[2:-2]))
        elif part.startswith("*") and part.endswith("*") and len(part) > 1:
            out.append(span(TEXT["italic"], part[1:-1]))
        else:
            out.append(escape_text(part))
    return "".join(out)


def escape_text(s):
    s = html.escape(s, quote=False)
    # Preserve runs of spaces the way ODF requires.
    s = re.sub(r"  +", lambda m: " <text:s text:c=\"%d\"/>" % (len(m.group(0)) - 1), s)
    return s


def span(style_display, text):
    return (f'<text:span text:style-name="{encode_style(style_display)}">'
            f"{escape_text(text)}</text:span>")


# A Packt body column is about 12.5cm wide; a figure is capped by both
# dimensions so a tall portrait screenshot does not run off the page.
MAX_W_CM = 11.5
MAX_H_CM = 13.0


def image(name, pixel_size, style="figure"):
    """An image paragraph. `name` is the file inside the document's Pictures/."""
    pw, ph = pixel_size
    w, h = MAX_W_CM, MAX_W_CM * ph / pw
    if h > MAX_H_CM:
        h = MAX_H_CM
        w = MAX_H_CM * pw / ph
    return (f'<text:p text:style-name="{encode_style(PARA[style])}">'
            f'<draw:frame draw:style-name="fig" draw:name="{name}" '
            f'text:anchor-type="as-char" svg:width="{w:.2f}cm" svg:height="{h:.2f}cm" '
            f'draw:z-index="0">'
            f'<draw:image xlink:href="Pictures/{name}" xlink:type="simple" '
            f'xlink:show="embed" xlink:actuate="onLoad"/>'
            f'</draw:frame></text:p>\n')


def para(kind, text, raw=False, style_name=None):
    style = style_name or encode_style(PARA[kind])
    body = text if raw else inline(text)
    tag = "text:h" if kind in ("h1", "h2", "h3", "h4") else "text:p"
    level = ""
    if tag == "text:h":
        level = f' text:outline-level="{kind[1]}"'
    return f'<{tag} text:style-name="{style}"{level}>{body}</{tag}>\n'


def code_block(lines):
    """Code paragraphs, with the last one using the Code End style so the
    template's spacing after a listing is correct."""
    out = []
    for i, line in enumerate(lines):
        kind = "code_end" if i == len(lines) - 1 else "code"
        out.append(para(kind, escape_text(line.rstrip()) or "<text:s/>", raw=True))
    return "".join(out)


# The template's bullet styles carry a list-style reference, but ODF only draws
# a marker when the paragraphs sit inside a text:list. Paragraph style alone
# gives correctly indented text with no bullet in front of it.
LIST_STYLE = {"bullet": "WW8Num13", "bullet2": "WW8Num13", "number": "WW8Num1"}


def bullets(items, style="bullet"):
    """A list, wrapped so the marker is actually drawn."""
    end = style + "_end"
    body = []
    for i, item in enumerate(items):
        last = i == len(items) - 1
        kind = end if last else style
        name = f"{style}_end_l" if last else None
        body.append(f"<text:list-item>{para(kind, item, style_name=name)}</text:list-item>")
    return (f'<text:list text:style-name="{LIST_STYLE.get(style, "WW8Num13")}">'
            + "".join(body) + "</text:list>\n")


def write_odt(path, body_xml, styles_xml, title, subject="", images=None):
    """`images` maps a file name inside Pictures/ to a path on disk."""
    parent = os.path.dirname(path)
    if parent:
        os.makedirs(parent, exist_ok=True)
    images = images or {}

    manifest = [MANIFEST_HEAD]
    for name in images:
        manifest.append(f' <manifest:file-entry manifest:full-path="Pictures/{name}" '
                        f'manifest:media-type="image/png"/>\n')
    manifest.append(MANIFEST_TAIL)

    head = CONTENT_HEAD.replace("__AUTOSTYLES__",
                                (FRAME_STYLE if images else "")
                                + BREAK_STYLE + LIST_END_STYLES)

    with zipfile.ZipFile(path, "w", zipfile.ZIP_DEFLATED) as z:
        # The mimetype entry must be first and stored uncompressed.
        zi = zipfile.ZipInfo("mimetype")
        zi.compress_type = zipfile.ZIP_STORED
        z.writestr(zi, "application/vnd.oasis.opendocument.text")
        z.writestr("META-INF/manifest.xml", "".join(manifest))
        z.writestr("styles.xml", styles_xml)
        z.writestr("meta.xml", meta_xml(title, subject))
        z.writestr("content.xml", head + body_xml + CONTENT_TAIL)
        for name, src in images.items():
            z.write(src, f"Pictures/{name}")
    return path
