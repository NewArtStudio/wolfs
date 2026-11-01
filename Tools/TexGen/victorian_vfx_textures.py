"""
Victorian replacements for VFX textures copied from the Space project (Resources/VFX/Textures):
  Rune.png  occult sigil circle (was a sci-fi HUD ring)    — ground zones / magic circles
  Hex.png   gothic lattice (was a hexagon grid)             — Blood Mirror shield pattern
  Bat.png   2x2 sheet of a bat flapping (new)               — EX Bat Swarm, familiar
White on transparent; the color comes from the material. Usage: python3 victorian_vfx_textures.py <Textures folder>
"""
import math
import sys
from PIL import Image, ImageDraw, ImageFilter

out = sys.argv[1].rstrip("/") + "/"


def glow(img, radius):
    """Soft halo under crisp lines (additive shaders love it)."""
    blur = img.filter(ImageFilter.GaussianBlur(radius))
    base = Image.new("RGBA", img.size, (255, 255, 255, 0))
    base.alpha_composite(blur)
    base.alpha_composite(img)
    return base


def sigil(size=512):
    S = size * 2  # supersample
    im = Image.new("RGBA", (S, S), (255, 255, 255, 0))
    d = ImageDraw.Draw(im)
    c = S / 2
    W = (255, 255, 255, 255)

    def ring(r, w):
        d.ellipse([c - r, c - r, c + r, c + r], outline=W, width=w)

    ring(S * 0.47, 10)
    ring(S * 0.43, 4)
    ring(S * 0.30, 6)
    ring(S * 0.12, 5)
    # pentagram (vampire occultism) touching the middle ring
    pts = [(c + S * 0.30 * math.sin(math.radians(i * 144)), c - S * 0.30 * math.cos(math.radians(i * 144))) for i in range(6)]
    d.line(pts, fill=W, width=7, joint="curve")
    # small glyphs between the outer rings: alternating crescents and crosses
    for i in range(16):
        a = math.radians(i * 22.5)
        x, y = c + S * 0.45 * math.sin(a), c - S * 0.45 * math.cos(a)
        r = S * 0.012
        if i % 2 == 0:
            d.ellipse([x - r, y - r, x + r, y + r], fill=W)
        else:
            d.line([(x - r * 1.6, y), (x + r * 1.6, y)], fill=W, width=4)
            d.line([(x, y - r * 1.6), (x, y + r * 1.6)], fill=W, width=4)
    # tick marks on the inner band
    for i in range(48):
        a = math.radians(i * 7.5)
        r0, r1 = S * 0.31, S * (0.335 if i % 4 == 0 else 0.322)
        d.line([(c + r0 * math.sin(a), c - r0 * math.cos(a)), (c + r1 * math.sin(a), c - r1 * math.cos(a))], fill=W, width=4)
    im = im.resize((size, size), Image.LANCZOS)
    return glow(im, 6)


def lattice(size=256):
    S = size * 2
    im = Image.new("RGBA", (S, S), (255, 255, 255, 0))
    d = ImageDraw.Draw(im)
    W = (255, 255, 255, 255)
    n = 4
    step = S / n
    for i in range(-1, n + 2):
        for j in range(-1, n + 2):
            x, y = i * step, j * step
            # diamond leading (stained-glass window) + quatrefoil in each cell
            d.polygon([(x + step / 2, y), (x + step, y + step / 2), (x + step / 2, y + step), (x, y + step / 2)], outline=W, width=5)
            cx, cy, r = x + step / 2, y + step / 2, step * 0.11
            for dx, dy in ((0, -1), (1, 0), (0, 1), (-1, 0)):
                d.ellipse([cx + dx * r - r, cy + dy * r - r, cx + dx * r + r, cy + dy * r + r], outline=W, width=3)
    im = im.resize((size, size), Image.LANCZOS)  # tiles seamlessly: the grid period divides the size
    return glow(im, 3)


def bat_frame(size, flap):
    """flap: 0 = wings up, 1 = wings down."""
    S = size * 4
    im = Image.new("RGBA", (S, S), (255, 255, 255, 0))
    d = ImageDraw.Draw(im)
    W = (255, 255, 255, 255)
    c = S / 2
    lift = (0.5 - flap) * S * 0.32   # wing tip height
    body = [(c - S * 0.05, c - S * 0.06), (c + S * 0.05, c - S * 0.06), (c + S * 0.04, c + S * 0.14), (c - S * 0.04, c + S * 0.14)]
    d.polygon(body, fill=W)
    d.ellipse([c - S * 0.06, c - S * 0.14, c + S * 0.06, c - S * 0.02], fill=W)
    d.polygon([(c - S * 0.05, c - S * 0.11), (c - S * 0.035, c - S * 0.19), (c - S * 0.015, c - S * 0.1)], fill=W)   # ears
    d.polygon([(c + S * 0.05, c - S * 0.11), (c + S * 0.035, c - S * 0.19), (c + S * 0.015, c - S * 0.1)], fill=W)
    for side in (-1, 1):
        tip = (c + side * S * 0.46, c - lift)
        root_top = (c + side * S * 0.04, c - S * 0.04)
        root_bot = (c + side * S * 0.04, c + S * 0.08)
        # scalloped trailing edge: three "fingers"
        f1 = (c + side * S * 0.34, c - lift * 0.4 + S * 0.10)
        f2 = (c + side * S * 0.22, c - lift * 0.2 + S * 0.05)
        f3 = (c + side * S * 0.12, c + S * 0.12)
        k1 = (c + side * S * 0.28, c - lift * 0.3 + S * 0.03)
        k2 = (c + side * S * 0.17, c - lift * 0.1 + S * 0.02)
        d.polygon([root_top, (c + side * S * 0.2, c - lift * 0.7 - S * 0.04), tip, f1, k1, f2, k2, f3, root_bot], fill=W)
    return im.resize((size, size), Image.LANCZOS)


def bats(size=256):
    half = size // 2
    sheet = Image.new("RGBA", (size, size), (255, 255, 255, 0))
    flaps = [0.0, 0.45, 1.0, 0.45]   # up, mid, down, mid (Unity sheets read top-left first)
    for i, f in enumerate(flaps):
        sheet.paste(bat_frame(half, f), ((i % 2) * half, (i // 2) * half))
    return sheet


sigil().save(out + "Rune.png")
lattice().save(out + "Hex.png")
bats().save(out + "Bat.png")
print("ok")
