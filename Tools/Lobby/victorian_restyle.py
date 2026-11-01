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


css += """
/* ===================== VampWolfs: personagens (nov/2026) ===================== */
.space-root .bs .bs-rec-h { margin-top: 18px; }
.space-root .bs .bs-ctile-pts { position: absolute; right: 6px; top: 6px; padding: 0 5px; font-size: 11px; color: #1a1006; background-color: rgba(217, 164, 65, 0.95); -unity-font-definition: url("Fonts/CrimsonText-Bold.ttf"); }
.space-root .bs-rc-row.on { height: auto; min-height: 62px; align-items: flex-start; }
.space-root .bs-rc-info { flex-grow: 1; flex-shrink: 1; }
.space-root .bs .bs-rc-desc { font-size: 12px; color: #e6d6b4; white-space: normal; margin-top: 3px; -unity-font-definition: url("Fonts/CrimsonText-Regular.ttf"); }
.space-root .bs-rc-namerow { flex-direction: row; align-items: center; margin: 8px 0 8px 0; }
.space-root .bs-rc-namerow .bs-rc-role { margin-right: 10px; }
.space-root .bs-rc-name-field { flex-grow: 1; margin: 0; }
.space-root .bs-rc-name-field .unity-text-field__input { background-color: rgba(15, 8, 8, 0.9); border-color: rgba(190, 146, 70, 0.6); color: #f3e6c9; font-size: 15px; -unity-font-definition: url("Fonts/CrimsonText-SemiBold.ttf"); }

/* ===================== VampWolfs: tela de HABILIDADES (sk-) ===================== */
.space-root .skills { background-color: rgba(8, 3, 4, 0.72); }
.space-root .sk-card { width: 1320px; max-width: 96%; height: 860px; max-height: 94%; padding: 18px 20px; --frame-fill: rgba(22, 11, 12, 0.97); --frame-border: rgba(190, 146, 70, 0.7); --frame-glow: rgba(217, 164, 65, 0.12); }
.space-root .sk-top { flex-direction: row; align-items: center; margin-bottom: 12px; }
.space-root .sk-head { flex-grow: 1; }
.space-root .sk-title { font-size: 28px; color: #f3e6c9; letter-spacing: 1px; -unity-font-definition: url("Fonts/IMFellEnglishSC-Regular.ttf"); }
.space-root .sk-sub { font-size: 15px; color: #c9b896; -unity-font-definition: url("Fonts/CrimsonText-Regular.ttf"); }
.space-root .sk-tabs { flex-direction: row; margin-right: 18px; }
.space-root .sk-tab { margin: 0 6px 0 0; padding: 6px 16px; font-size: 16px; color: #c9b896; background-color: rgba(40, 20, 18, 0.8); border-width: 1px; border-color: rgba(160, 130, 110, 0.4); border-radius: 0; -unity-font-definition: url("Fonts/IMFellEnglishSC-Regular.ttf"); }
.space-root .sk-tab.on { color: #1a1006; background-color: #d9a441; border-color: #ecc879; }
.space-root .sk-tools { flex-direction: row; }
.space-root .sk-main { flex-direction: row; flex-grow: 1; }
.space-root .sk-scroll { flex-grow: 1; margin-right: 14px; }
.space-root .sk-detail { width: 360px; padding: 14px 16px; --frame-fill: rgba(30, 16, 17, 0.95); --frame-border: rgba(160, 130, 110, 0.45); }
.space-root .sk-cols { flex-direction: row; }
.space-root .sk-col { flex-grow: 1; flex-basis: 0; margin-right: 10px; }
.space-root .sk-sec { flex-direction: row; align-items: center; margin: 6px 0 8px 0; }
.space-root .sk-sec-bar { width: 4px; height: 18px; margin-right: 8px; background-color: #d9a441; }
.space-root .sk-sec-t { font-size: 18px; color: #f3e6c9; margin-right: 10px; -unity-font-definition: url("Fonts/IMFellEnglishSC-Regular.ttf"); }
.space-root .sk-sec-s { font-size: 13px; color: #a1907a; -unity-font-definition: url("Fonts/CrimsonText-Regular.ttf"); }
.space-root .sk-row { flex-direction: row; align-items: center; min-height: 58px; margin-bottom: 6px; padding: 5px 8px 5px 12px; --frame-fill: rgba(30, 16, 17, 0.94); --frame-border: rgba(160, 130, 110, 0.32); --frame-cut: 6; }
.space-root .sk-row:hover { --frame-border: rgba(190, 146, 70, 0.8); }
.space-root .sk-row.on { --frame-border: rgba(236, 200, 121, 1); --frame-glow: rgba(217, 164, 65, 0.2); }
.space-root .sk-row.learned { --frame-fill: rgba(44, 22, 20, 0.95); }
.space-root .sk-row.locked { opacity: 0.55; }
.space-root .sk-accent { position: absolute; left: 3px; top: 8px; bottom: 8px; width: 3px; }
.space-root .sk-ic { width: 30px; height: 30px; margin-right: 10px; flex-shrink: 0; }
.space-root .sk-tx { flex-grow: 1; flex-shrink: 1; }
.space-root .sk-name { font-size: 16px; color: #f3e6c9; -unity-font-definition: url("Fonts/CrimsonText-Bold.ttf"); }
.space-root .sk-meta { font-size: 12px; color: #a1907a; -unity-font-definition: url("Fonts/CrimsonText-Regular.ttf"); }
.space-root .sk-pips { flex-direction: row; margin-top: 3px; }
.space-root .sk-pip { width: 10px; height: 4px; margin-right: 2px; background-color: rgba(255, 255, 255, 0.1); }
.space-root .sk-pip.on { background-color: #d9a441; }
.space-root .sk-btns { flex-direction: row; align-items: center; flex-shrink: 0; }
.space-root .sk-key, .space-root .sk-pm { width: 28px; height: 28px; margin: 0 0 0 4px; padding: 0; font-size: 15px; color: #f3e6c9; background-color: rgba(40, 20, 18, 0.9); border-width: 1px; border-color: rgba(160, 130, 110, 0.45); border-radius: 0; -unity-text-align: middle-center; -unity-font-definition: url("Fonts/CrimsonText-Bold.ttf"); }
.space-root .sk-key.on { color: #1a1006; background-color: #d9a441; border-color: #ecc879; }
.space-root .sk-pm.plus { border-color: rgba(190, 146, 70, 0.8); }
.space-root .sk-key:hover, .space-root .sk-pm:hover { border-color: #ecc879; }
.space-root .sk-keyrow { flex-direction: row; align-items: center; margin: 0 0 8px 12px; }
.space-root .sk-keylabel { width: 110px; font-size: 14px; color: #d9a441; -unity-font-definition: url("Fonts/CrimsonText-Bold.ttf"); }
.space-root .sk-opts { flex-direction: row; flex-grow: 1; flex-wrap: wrap; }
.space-root .sk-opt { flex-direction: row; align-items: center; width: 230px; height: 52px; margin: 0 8px 6px 0; padding: 4px 8px; --frame-fill: rgba(30, 16, 17, 0.94); --frame-border: rgba(160, 130, 110, 0.32); --frame-cut: 6; }
.space-root .sk-opt:hover { --frame-border: rgba(190, 146, 70, 0.85); }
.space-root .sk-opt.on { --frame-fill: rgba(52, 30, 14, 0.95); --frame-border: rgba(236, 200, 121, 1); --frame-glow: rgba(217, 164, 65, 0.2); }
.space-root .sk-opt.sel { --frame-border: rgba(240, 214, 150, 1); }
.space-root .sk-opt-ic { width: 26px; height: 26px; margin-right: 8px; flex-shrink: 0; }
.space-root .sk-opt-tx { flex-grow: 1; flex-shrink: 1; }
.space-root .sk-opt-name { font-size: 14px; color: #f3e6c9; white-space: nowrap; overflow: hidden; text-overflow: ellipsis; -unity-font-definition: url("Fonts/CrimsonText-Bold.ttf"); }
.space-root .sk-opt-meta { font-size: 11px; color: #a1907a; -unity-font-definition: url("Fonts/CrimsonText-Regular.ttf"); }
.space-root .sk-opt-meta.on { color: #d9a441; }
.space-root .sk-note { font-size: 13px; color: #a1907a; margin: 10px 0 0 12px; white-space: normal; -unity-font-definition: url("Fonts/CrimsonText-Regular.ttf"); }
.space-root .sk-d-head { flex-direction: row; align-items: center; margin-bottom: 8px; }
.space-root .sk-d-ic { width: 40px; height: 40px; margin-right: 12px; }
.space-root .sk-d-tx { flex-shrink: 1; }
.space-root .sk-d-name { font-size: 22px; -unity-font-definition: url("Fonts/IMFellEnglishSC-Regular.ttf"); }
.space-root .sk-d-meta { font-size: 12px; color: #a1907a; white-space: normal; -unity-font-definition: url("Fonts/CrimsonText-Regular.ttf"); }
.space-root .sk-d-desc { font-size: 15px; color: #e6d6b4; margin-bottom: 10px; white-space: normal; -unity-font-style: italic; -unity-font-definition: url("Fonts/CrimsonText-Regular.ttf"); }
.space-root .sk-d-kicker { font-size: 13px; color: #d9a441; letter-spacing: 1px; margin-top: 8px; -unity-font-definition: url("Fonts/CrimsonText-Bold.ttf"); }
.space-root .sk-d-text { font-size: 14px; color: #f3e6c9; white-space: normal; -unity-font-definition: url("Fonts/CrimsonText-Regular.ttf"); }
.space-root .sk-d-text.dim { color: #a1907a; }
"""
open(dst, "w", encoding="utf-8").write(css)
print("ok:", dst)
