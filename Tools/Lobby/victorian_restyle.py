"""
Restyles the lobby stylesheet copied from the Space project (Space.uss) into VampWolfs' cartoon-Victorian look.

What it does (run once on a fresh copy of Space.uss; the result is Assets/VampWolfs/Resources/Lobby/Lobby.uss):
  - fonts: Rajdhani (sci-fi titles) -> IM Fell English SC (Victorian print); Barlow (body) -> Crimson Text;
  - colors: cold blues/steel greys of the space station become warm wine/walnut/parchment tones
    (red and blue channels swapped on low-saturation or dark colors), the amber "hazard" accent becomes brass,
    the few saturated blues get Victorian replacements;
  - appends the VampWolfs-only classes ("em breve" states, REGISTRO block).

Usage: python3 victorian_restyle.py <Space.uss> <Lobby.uss>
"""
import re
import sys
import colorsys

src, dst = sys.argv[1], sys.argv[2]
css = open(src, encoding="utf-8").read()

FONTS = {
    "Rajdhani-Bold.ttf": "IMFellEnglishSC-Regular.ttf",
    "Rajdhani-SemiBold.ttf": "CrimsonText-Bold.ttf",
    "Barlow-Regular.ttf": "CrimsonText-Regular.ttf",
    "Barlow-Medium.ttf": "CrimsonText-SemiBold.ttf",
    "Barlow-SemiBold.ttf": "CrimsonText-Bold.ttf",
}
# Space-only texture (talent tree background) that does not exist here
css = css.replace('background-image: url("Textures/tree_bg.png"); ', '')

for a, b in FONTS.items():
    css = css.replace('Fonts/' + a, 'Fonts/' + b)

# explicit replacements for accent colors (r, g, b) -> (r, g, b)
EXPLICIT = {
    (243, 169, 58): (217, 164, 65),   # hazard amber -> brass
    (255, 192, 98): (236, 200, 121),  # light amber -> pale brass
    (255, 196, 102): (236, 200, 121),
    (255, 196, 96): (236, 200, 121),
    (214, 156, 62): (190, 146, 70),   # dim amber -> old brass
    (255, 210, 74): (240, 207, 106),  # cast yellow -> candle
    (255, 210, 122): (240, 214, 150),
    (61, 139, 255): (168, 36, 58),    # XP blue -> wine
    (111, 168, 255): (196, 155, 224), # rank blue -> violet
    (112, 168, 255): (196, 155, 224),
    (77, 141, 255): (168, 36, 58),
    (83, 224, 138): (124, 196, 106),  # ok green -> moss
    (255, 79, 74): (224, 86, 74),     # danger -> brick red
    (127, 216, 255): (217, 164, 65),  # cyan highlights -> brass
}


def warm(r, g, b):
    if (r, g, b) in EXPLICIT:
        return EXPLICIT[(r, g, b)]
    h, s, v = colorsys.rgb_to_hsv(r / 255, g / 255, b / 255)
    if b > r and (s < 0.55 or v < 0.3):
        r, b = b, r                    # cold dark/grey -> warm dark/taupe
        if v < 0.3:                    # dark panels lean to wine (less green)
            r = min(255, int(r * 1.05) + 2)
            g = int(g * 0.8)
    return r, g, b


def hex_sub(m):
    h = m.group(1)
    if len(h) == 3:
        h = "".join(c * 2 for c in h)
    r, g, b = int(h[0:2], 16), int(h[2:4], 16), int(h[4:6], 16)
    r, g, b = warm(r, g, b)
    return "#%02x%02x%02x" % (r, g, b)


def rgba_sub(m):
    r, g, b, a = int(m.group(1)), int(m.group(2)), int(m.group(3)), m.group(4)
    r, g, b = warm(r, g, b)
    return "rgba(%d, %d, %d, %s)" % (r, g, b, a)


css = re.sub(r"#([0-9a-fA-F]{6}|[0-9a-fA-F]{3})\b", hex_sub, css)
css = re.sub(r"rgba\(\s*(\d+),\s*(\d+),\s*(\d+),\s*([0-9.]+)\s*\)", rgba_sub, css)

header = """/* =====================================================================
   VampWolfs — estilos do lobby (UI Toolkit), copiados do Space.uss e recoloridos
   para a era vitoriana cartunizada por Tools/Lobby/victorian_restyle.py:
   painéis vinho/nogueira, texto pergaminho, acento latão; fontes IM Fell English SC
   (títulos) e Crimson Text (texto). Prefixos bs- (tela inicial) e ars- (Arsenal) como no Space.
   ===================================================================== */
"""
css = re.sub(r"\A/\*.*?\*/\s*", header, css, count=1, flags=re.S)

css += """

/* ===================== VampWolfs: classes novas ===================== */
/* "em breve": esmaecido, com selo de latão */
.space-root .bs .bs-soon { position: absolute; right: 6px; top: 6px; padding: 1px 6px; font-size: 11px; color: #1a1006; background-color: rgba(217, 164, 65, 0.92); letter-spacing: 1px; -unity-font-definition: url("Fonts/CrimsonText-Bold.ttf"); }
.space-root .bs-tile.soon { opacity: 0.78; }
.space-root .bs .bs-rank.soon { color: #a1907a; -unity-font-style: italic; }
.space-root .bs-xp.soon .bar-fill { background-color: rgba(161, 144, 122, 0.4); }
.space-root .bs .bs-btn.soon { opacity: 0.6; }
.space-root .bs .bs-power { font-size: 14px; color: #e6d6b4; margin-top: 6px; margin-bottom: 2px; -unity-font-definition: url("Fonts/CrimsonText-SemiBold.ttf"); }

/* REGISTRO (recursos guardados e incursões), no lugar da Contingência do Space */
.space-root .bs-record { flex-shrink: 0; padding: 4px 2px 2px 2px; }
.space-root .bs-rec-row { flex-direction: row; margin-bottom: 8px; }
.space-root .bs-rec { flex-grow: 1; flex-basis: 0; flex-direction: row; align-items: center; margin-right: 8px; padding: 6px 8px; background-color: rgba(40, 20, 18, 0.7); border-left-width: 2px; border-color: rgba(190, 146, 70, 0.7); }
.space-root .bs-rec-ic { width: 20px; height: 20px; margin-right: 8px; flex-shrink: 0; }
.space-root .bs-rec-tx { flex-direction: column; }
.space-root .bs .bs-rec-v { font-size: 20px; color: #f3e6c9; -unity-font-definition: url("Fonts/IMFellEnglishSC-Regular.ttf"); }
.space-root .bs .bs-rec-k { font-size: 12px; color: #c9b896; -unity-font-definition: url("Fonts/CrimsonText-Regular.ttf"); white-space: nowrap; }
.space-root .bs-rec-foot { flex-direction: row; align-items: center; margin-top: 2px; }
.space-root .bs .bs-rec-note { flex-grow: 1; flex-shrink: 1; font-size: 13px; color: #c9b896; margin-right: 8px; -unity-font-definition: url("Fonts/CrimsonText-Regular.ttf"); }
"""

open(dst, "w", encoding="utf-8").write(css)
print("ok:", dst)
