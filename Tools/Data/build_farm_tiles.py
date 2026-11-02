#!/usr/bin/env python3
"""
Gera os TILES da fazenda procedural -> Resources/VampWolfs/farm_tiles.json

Uso:
  python3 Tools/Data/build_farm_tiles.py Assets/VampWolfs/Resources/VampWolfs/farm_tiles.json [docs/img/mapa_tiles.png]

Regras que todo tile cumpre (o script confere e falha se quebrar):
  1. Tile quadrado de TILE m (coordenadas locais -9..9, x = leste, z = norte).
  2. FAIXA DE BORDA livre: nada que bloqueia a menos de MARGIN m da borda -> qualquer tile encaixa
     em qualquer outro, em qualquer rotação (90°) e espelhado.
  3. ESTRADA sai do meio de cada lado (as estradas sempre se ligam entre tiles).
  4. Sem "bolsões": todo lugar livre do tile é alcançável a pé a partir da borda.

Peças ("T"): ver PIECES abaixo. Campos: X, Z (centro, m), R (giro em graus, como o Unity),
L (comprimento: cercas, estradas, fileiras), W (largura: estradas, lago), S (escala).
Números [PROTÓTIPO]. Para trocar o visual de uma peça no Unity: prefab em
Resources/VampWolfs/FarmPieces/<T>.prefab (ver docs/MAPA.md).
"""
import json, math, random, sys

TILE = 18.0
HALF = TILE / 2
MARGIN = 2.0          # faixa livre em cada borda
CHAR_R = 0.8          # raio do MAIOR personagem (lobisomem 0,65 × tamanho 1,2) — igual ao WalkGrid.cs
ROAD_W = 2.2

# tipo: (forma, largura x, profundidade z ou raio, bloqueia?)
PIECES = {
    "barn":      ("box", 8.0, 7.0, True),
    "house":     ("box", 7.0, 6.0, True),
    "crypt":     ("box", 4.0, 3.4, True),
    "tent":      ("box", 3.0, 3.0, True),
    "windmill":  ("circle", 2.0, 0, True),
    "well":      ("circle", 0.9, 0, True),
    "haystack":  ("circle", 0.8, 0, True),
    "bale":      ("box", 1.4, 0.9, True),
    "tree":      ("circle", 0.35, 0, True),
    "deadtree":  ("circle", 0.3, 0, True),
    "rock":      ("circle", 0.6, 0, True),
    "crate":     ("box", 1.0, 1.0, True),
    "barrel":    ("circle", 0.45, 0, True),
    "cart":      ("box", 1.6, 3.0, True),
    "woodpile":  ("box", 2.2, 1.0, True),
    "trough":    ("box", 0.8, 2.4, True),
    "fence":     ("box", 0.3, None, True),     # L = comprimento
    "ironfence": ("box", 0.2, None, True),
    "hedge":     ("box", 0.9, None, True),
    "croprow":   ("box", 0.7, None, True),
    "grave":     ("box", 0.8, 0.3, True),
    "lantern":   ("circle", 0.15, 0, True),
    "scarecrow": ("circle", 0.25, 0, True),
    "millstone": ("circle", 0.8, 0, True),
    "sacks":     ("circle", 0.5, 0, True),
    "log":       ("box", 1.6, 0.5, True),
    # decoração (não bloqueia)
    "road":      ("box", None, None, False),   # W x L
    "pond":      ("ellipse", None, None, False),
    "dock":      ("box", None, None, False),
    "campfire":  ("circle", 0.6, 0, False),
    "reeds":     ("circle", 0.5, 0, False),
    "flowers":   ("circle", 0.4, 0, False),
    "pumpkin":   ("circle", 0.35, 0, False),
}


def P(t, x, z, r=0.0, **kw):
    d = {"T": t, "X": x, "Z": z, "R": r}
    d.update(kw)
    return d


def seg(t, x1, z1, x2, z2, **kw):
    """Peça comprida entre dois pontos (cerca, estrada, fileira de milho)."""
    dx, dz = x2 - x1, z2 - z1
    return P(t, (x1 + x2) / 2, (z1 + z2) / 2, math.degrees(math.atan2(dx, dz)), L=math.hypot(dx, dz), **kw)


def road(x1, z1, x2, z2, w=ROAD_W, e=""):
    """Estrada (decoração). e = lado da borda que ela liga ('n','e','s','w'); '' = parte central,
    desenhada quando o tile tem pelo menos uma estrada. O gerador só desenha os lados que a rede
    de estradas do mapa usa — assim o mapa não vira um tabuleiro."""
    return seg("road", x1, z1, x2, z2, W=w, E=e)


def spokes(hub_r=0.0):
    """Quatro pontas de estrada, da borda até o centro (hub_r = raio da praça)."""
    return [road(0, HALF, 0, hub_r, e="n"), road(0, -HALF, 0, -hub_r, e="s"),
            road(HALF, 0, hub_r, 0, e="e"), road(-HALF, 0, -hub_r, 0, e="w")]


def cross_roads():
    return spokes(0.0)


def plaza(r=2.6):
    out = spokes(r * 0.8)
    n = 10
    for i in range(n):
        a1, a2 = 2 * math.pi * i / n, 2 * math.pi * (i + 1) / n
        out.append(road(r * math.sin(a1), r * math.cos(a1), r * math.sin(a2), r * math.cos(a2), w=1.8))
    return out


def fence_box(x0, z0, x1, z1, gaps, t="fence", gap=1.2):
    """Retângulo de cerca; gaps = lados com abertura no meio ('n','s','e','w')."""
    out = []
    sides = {"n": ((x0, z1), (x1, z1)), "s": ((x0, z0), (x1, z0)), "w": ((x0, z0), (x0, z1)), "e": ((x1, z0), (x1, z1))}
    for k, (a, b) in sides.items():
        if k in gaps:
            mx, mz = (a[0] + b[0]) / 2, (a[1] + b[1]) / 2
            ux, uz = b[0] - a[0], b[1] - a[1]
            ln = math.hypot(ux, uz); ux, uz = ux / ln, uz / ln
            out.append(seg(t, a[0], a[1], mx - ux * gap, mz - uz * gap))
            out.append(seg(t, mx + ux * gap, mz + uz * gap, b[0], b[1]))
        else:
            out.append(seg(t, a[0], a[1], b[0], b[1]))
    return out


# ---------------------------------------------------------------- os 10 tiles

def t_pasture():
    p = cross_roads()
    p += [P("haystack", -4.2, 4.0), P("haystack", -5.6, 5.4, S=0.8), P("haystack", 4.6, -4.4, S=1.2),
          P("rock", -4.8, -4.6, S=1.4), P("rock", 3.4, 4.8, S=0.9), P("scarecrow", 5.2, 4.0),
          seg("fence", 2.6, 2.2, 2.6, 6.4), seg("fence", -6.4, -2.4, -2.4, -2.4),
          P("tree", -6.2, 6.2, S=1.1), P("tree", 6.2, -6.4),
          P("flowers", -3, 6.5), P("flowers", 6, 2), P("flowers", -6.5, -6.5)]
    return dict(Id="pasture", Name="Pasto", Weight=3, Max=0, Pieces=p)


def t_barnyard():
    p = cross_roads()                                        # o celeiro abre a porta para a estrada
    p += [P("barn", 4.0, 4.2, 0, S=0.75),
          P("haystack", -4.0, -4.6), P("haystack", -5.4, -5.6, S=0.8), P("bale", 4.2, -4.0, 20), P("bale", 5.6, -5.4, -10),
          P("cart", -4.6, 4.2, 15), P("lantern", 1.8, -3.4), P("barrel", -2.6, 5.8), P("barrel", -2.0, 6.5, S=0.8),
          P("pumpkin", 6, -2.8), P("flowers", -6.5, 1.2)]
    return dict(Id="barnyard", Name="Celeiro", Weight=2, Max=3, Pieces=p)


def t_pen():
    p = cross_roads()
    p += fence_box(-6.8, 1.8, -2.0, 6.8, gaps="se")
    p += [P("trough", -4.4, 3.2, 90), P("haystack", -5.4, -3.6, S=0.7),
          P("bale", 3.6, 4.0, 10), P("bale", 5.0, 5.2, 80), P("barrel", 3.0, -3.0),
          P("rock", 5.2, -5.0, S=1.2), P("tree", -5.8, -5.6), P("lantern", 1.6, 1.6), P("flowers", -3, -6)]
    p += fence_box(2.4, -6.8, 6.8, -2.6, gaps="nw")
    return dict(Id="pen", Name="Curral", Weight=2, Max=5, Pieces=p)


def t_orchard():
    p = spokes()
    for x in (-5.6, -2.8, 2.8, 5.6):
        for z in (-5.6, -2.8, 2.8, 5.6):
            if abs(x) == 2.8 and abs(z) == 2.8:
                continue                                      # clareira no centro
            p.append(P("tree", x, z, S=0.8 + 0.1 * ((x + z) % 3)))
    p += [P("barrel", 1.8, 1.8), P("crate", -1.9, -2.0, 15), P("pumpkin", -2.2, 2.0), P("flowers", 2.4, -2.2)]
    return dict(Id="orchard", Name="Pomar", Weight=2, Max=5, Pieces=p)


def t_pond():
    # estrada contorna o lago; o lago é raso (dá para andar) — decoração
    ring = [(0, 7.0), (-4.6, 5.8), (-5.6, 0), (-3.0, -3.2), (3.6, -3.2), (6.6, 0), (5.6, 5.6), (0, 7.0)]
    p = [road(a[0], a[1], b[0], b[1], w=1.8) for a, b in zip(ring, ring[1:])]
    p += [road(0, HALF, 0, 7.0, e="n"), road(-HALF, 0, -5.6, 0, e="w"), road(0, -HALF, 0, -3.2, e="s"), road(HALF, 0, 6.6, 0, e="e")]
    p += [P("pond", 1.6, 2.4, 10, W=7.5, L=6.0), P("dock", 1.2, -0.6, 0, W=1.4, L=2.8),
          P("reeds", 4.8, 3.6), P("reeds", -1.6, 4.8), P("reeds", 4.0, 5.6), P("reeds", -1.8, 0.6),
          P("rock", 5.6, 1.0, S=1.1), P("rock", -2.6, 5.8, S=0.8), P("tree", -4.6, 1.6, S=1.2), P("tree", 5.8, 6.2),
          P("log", -3.2, -5.6, 30), P("flowers", -6.0, -6.0)]
    return dict(Id="pond", Name="Lago", Weight=2, Max=4, Pieces=p)


def t_corn():
    p = cross_roads()
    # fileiras a 2,4 m: sobram 1,7 m entre elas — dá para correr no meio do milho
    for x in (-6.5, -4.1, -1.7):
        p.append(seg("croprow", x, 1.8, x, 6.8))
    for x in (1.7, 4.1, 6.5):
        p.append(seg("croprow", x, -6.8, x, -1.8))
    p += [P("scarecrow", 4.4, 4.4), P("pumpkin", 3.0, 3.0), P("pumpkin", 5.6, 2.6), P("pumpkin", 3.4, 6.0),
          P("cart", -4.4, -4.6, 75), P("sacks", -2.2, -3.0), P("sacks", -6.2, -2.8, S=0.8)]
    return dict(Id="corn", Name="Milharal", Weight=2, Max=5, Pieces=p)


def t_graveyard():
    p = spokes(2.2)
    p += fence_box(-6.6, -6.6, 6.6, 6.6, gaps="nsew", t="ironfence", gap=1.6)
    p.append(P("crypt", 0, 0))
    # túmulos espaçados (corredores de 1,7 m+): dá para lutar entre eles
    for sx in (-1, 1):
        for sz in (-1, 1):
            p.append(P("grave", sx * 3.0, sz * 4.4, 0))
            p.append(P("grave", sx * 5.2, sz * 4.4, 0))
            p.append(P("grave", sx * 4.6, sz * 2.0, 0))
    p += [P("deadtree", -4.2, 1.6), P("deadtree", 4.2, -1.6), P("lantern", 2.2, 2.4), P("lantern", -2.2, -2.4)]
    return dict(Id="graveyard", Name="Cemitério", Weight=1, Max=3, Pieces=p)


def t_windmill():
    p = spokes()
    p += [P("windmill", -4.0, 4.0), P("sacks", -1.6, 2.2), P("sacks", -2.4, 1.6, S=0.8), P("millstone", 4.2, 4.4),
          P("cart", 4.6, -4.0, -20), P("sacks", 2.6, -5.4), P("woodpile", -4.4, -4.4, 30), P("lantern", 1.6, -1.6),
          P("flowers", -6, -1.6)]
    return dict(Id="windmill", Name="Moinho", Weight=1, Max=2, Pieces=p)


def t_farmhouse():
    p = [road(0, -HALF, 0, 0, e="s"), road(-HALF, 0, 0, 0, e="w"), road(HALF, 0, 0, 0, e="e"),
         road(0, HALF, 2.0, 1.6, e="n"), road(2.0, 1.6, 0, 0, e="n"), road(-3.8, 0, -3.8, 1.2, w=1.6)]
    p += [P("house", -3.9, 3.9, 0, S=0.8), P("well", 3.6, 4.2), P("woodpile", 5.2, -4.0, 90), P("crate", 3.0, -5.6, 10),
          seg("hedge", -6.4, -2.2, -2.4, -2.2), seg("hedge", -6.4, -2.2, -6.4, -6.4), P("flowers", -4.4, -4.6),
          P("flowers", -3.2, -5.6), P("lantern", -1.4, 1.6), P("barrel", 5.8, 6.2)]
    return dict(Id="farmhouse", Name="Casa da fazenda", Weight=1, Max=3, Pieces=p)


def t_camp():
    p = plaza(2.6)
    p += [P("campfire", 0, 0), P("log", 0, 2.1, 90), P("log", 1.9, -1.1, 30), P("log", -1.9, -1.1, -30),
          P("tent", -4.6, 4.6, 45), P("tent", 4.6, -4.6, 45), P("cart", 4.8, 4.6, 30), P("crate", -4.4, -4.4, 10),
          P("crate", -5.6, -3.8, 35), P("barrel", -3.4, -5.6), P("lantern", 2.6, 2.8)]
    return dict(Id="camp", Name="Acampamento", Weight=1, Max=3, Pieces=p)


TILES = [t_pasture(), t_barnyard(), t_pen(), t_orchard(), t_pond(), t_corn(), t_graveyard(), t_windmill(), t_farmhouse(), t_camp()]


# ---------------------------------------------------------------- validação

def rot(x, z, deg):
    a = math.radians(deg)
    return x * math.cos(a) + z * math.sin(a), -x * math.sin(a) + z * math.cos(a)


def footprint(p):
    """(shape, data) em coordenadas do tile."""
    shape, w, d, block = PIECES[p["T"]]
    s = p.get("S", 1.0)
    if shape == "circle":
        return "circle", (p["X"], p["Z"], w * s)
    if shape == "ellipse":
        return "ellipse", (p["X"], p["Z"], p["W"] / 2, p["L"] / 2, p["R"])
    w = p.get("W", w) if w is None else w
    d = p.get("L", d) if d is None else d
    w, d = w * s, d * s
    corners = [rot(cx, cz, p["R"]) for cx, cz in ((-w / 2, -d / 2), (w / 2, -d / 2), (w / 2, d / 2), (-w / 2, d / 2))]
    return "box", [(p["X"] + cx, p["Z"] + cz) for cx, cz in corners]


def inside(fp, x, z, grow):
    shape, data = fp
    if shape == "circle":
        cx, cz, r = data
        return (x - cx) ** 2 + (z - cz) ** 2 <= (r + grow) ** 2
    if shape == "ellipse":
        cx, cz, a, b, r = data
        lx, lz = rot(x - cx, z - cz, -r)
        return (lx / (a + grow)) ** 2 + (lz / (b + grow)) ** 2 <= 1
    # caixa girada: distância até o polígono convexo
    pts = data
    n = len(pts)
    inside_poly = True
    for i in range(n):
        ax, az = pts[i]; bx, bz = pts[(i + 1) % n]
        if (bx - ax) * (z - az) - (bz - az) * (x - ax) < 0:
            inside_poly = False
    if inside_poly:
        return True
    if grow <= 0:
        return False
    for i in range(n):
        ax, az = pts[i]; bx, bz = pts[(i + 1) % n]
        dx, dz = bx - ax, bz - az
        t = max(0, min(1, ((x - ax) * dx + (z - az) * dz) / (dx * dx + dz * dz)))
        if (ax + t * dx - x) ** 2 + (az + t * dz - z) ** 2 <= grow * grow:
            return True
    return False


def extent(fp):
    shape, data = fp
    if shape == "circle":
        cx, cz, r = data
        return max(abs(cx) + r, abs(cz) + r)
    if shape == "ellipse":
        cx, cz, a, b, _ = data
        return max(abs(cx), abs(cz)) + max(a, b)
    return max(max(abs(x), abs(z)) for x, z in data)


def validate(tile):
    errors = []
    blockers = []
    for p in tile["Pieces"]:
        if p["T"] not in PIECES:
            errors.append("peça desconhecida " + p["T"])
            continue
        fp = footprint(p)
        if PIECES[p["T"]][3]:
            blockers.append(fp)
            if extent(fp) > HALF - MARGIN + 1e-6:
                errors.append("%s em (%.1f, %.1f) invade a faixa de borda (%.2f m > %.2f)" % (p["T"], p["X"], p["Z"], extent(fp), HALF - MARGIN))
    # estradas nos 4 meios de borda
    for edge, (mx, mz) in (("e", (HALF, 0)), ("w", (-HALF, 0)), ("n", (0, HALF)), ("s", (0, -HALF))):
        ends = []
        for p in tile["Pieces"]:
            if p["T"] == "road" and p.get("E", "") == edge:
                dx, dz = rot(0, p["L"] / 2, p["R"])
                ends += [(p["X"] + dx, p["Z"] + dz), (p["X"] - dx, p["Z"] - dz)]
        if not any(abs(ex - mx) < 0.05 and abs(ez - mz) < 0.05 for ex, ez in ends):
            errors.append("sem ponta de estrada '%s' chegando no meio da borda (%g, %g)" % (edge, mx, mz))
    # bolsões
    step = 0.25
    n = int(TILE / step)
    free = [[True] * n for _ in range(n)]
    blocked = 0
    for i in range(n):
        for j in range(n):
            x = -HALF + (i + 0.5) * step; z = -HALF + (j + 0.5) * step
            if any(inside(fp, x, z, CHAR_R) for fp in blockers):
                free[i][j] = False; blocked += 1
    seen = [[False] * n for _ in range(n)]
    stack = [(i, j) for i in range(n) for j in (0, n - 1)] + [(i, j) for j in range(n) for i in (0, n - 1)]
    stack = [(i, j) for i, j in stack if free[i][j]]
    for i, j in stack: seen[i][j] = True
    while stack:
        i, j = stack.pop()
        for di, dj in ((1, 0), (-1, 0), (0, 1), (0, -1)):
            a, b = i + di, j + dj
            if 0 <= a < n and 0 <= b < n and free[a][b] and not seen[a][b]:
                seen[a][b] = True; stack.append((a, b))
    # cada bolsão (grupo de lugares livres que não se ligam à borda) com mais de 0,25 m² é erro
    for i0 in range(n):
        for j0 in range(n):
            if not free[i0][j0] or seen[i0][j0]:
                continue
            group = [(i0, j0)]; seen[i0][j0] = True; k = 0
            while k < len(group):
                i, j = group[k]; k += 1
                for di, dj in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                    a2, b2 = i + di, j + dj
                    if 0 <= a2 < n and 0 <= b2 < n and free[a2][b2] and not seen[a2][b2]:
                        seen[a2][b2] = True; group.append((a2, b2))
            area = len(group) * step * step
            if area > 0.25:
                cx = sum(-HALF + (i + 0.5) * step for i, j in group) / len(group)
                cz = sum(-HALF + (j + 0.5) * step for i, j in group) / len(group)
                errors.append("bolsão fechado de %.2f m² em (%.1f, %.1f): lugar livre que não dá para alcançar" % (area, cx, cz))
    open_pct = 100 - blocked * 100 / (n * n)
    return errors, open_pct


def rounded(v):
    if isinstance(v, float):
        return round(v, 2)
    if isinstance(v, dict):
        return {k: rounded(x) for k, x in v.items()}
    if isinstance(v, list):
        return [rounded(x) for x in v]
    return v


# ---------------------------------------------------------------- prévia (PNG)

COLORS = {
    "barn": (130, 30, 25), "house": (150, 120, 80), "crypt": (110, 110, 120), "tent": (170, 150, 110),
    "windmill": (150, 140, 120), "well": (100, 100, 105), "haystack": (200, 165, 80), "bale": (200, 165, 80),
    "tree": (30, 80, 35), "deadtree": (80, 65, 50), "rock": (120, 120, 120), "crate": (140, 100, 60),
    "barrel": (110, 75, 45), "cart": (120, 85, 50), "woodpile": (110, 80, 50), "trough": (100, 75, 50),
    "fence": (115, 80, 50), "ironfence": (40, 40, 50), "hedge": (35, 90, 40), "croprow": (170, 170, 60),
    "grave": (150, 150, 160), "lantern": (255, 170, 70), "scarecrow": (200, 150, 70), "millstone": (130, 130, 130),
    "sacks": (210, 200, 170), "log": (100, 70, 45), "road": (95, 72, 48), "pond": (35, 60, 100), "dock": (120, 90, 60),
    "campfire": (255, 120, 40), "reeds": (60, 110, 60), "flowers": (190, 90, 160), "pumpkin": (230, 120, 30),
}
DRAW_ORDER = ["road", "pond", "dock", "reeds", "flowers"]


def transform(p, k, mirror):
    """Mesma conta do FarmGenerator.cs: espelha X, depois gira k×90°."""
    q = dict(p)
    x, z, r = p["X"], p["Z"], p["R"]
    if mirror:
        x, r = -x, -r
    x, z = rot(x, z, k * 90)
    q["X"], q["Z"], q["R"] = x, z, r + k * 90
    return q


DIRS = {"n": (0, 1), "e": (1, 0), "s": (0, -1), "w": (-1, 0)}


def world_edge(edge, k, mirror):
    """Lado local do tile -> lado no mundo depois de espelhar e girar (igual ao C#)."""
    x, z = DIRS[edge]
    if mirror:
        x = -x
    x, z = rot(x, z, k * 90)
    x, z = round(x), round(z)
    return [d for d, v in DIRS.items() if v == (x, z)][0]


def road_network(rng, n, landmarks=6, exits=2):
    """Árvore aleatória (DFS) sobre a grade de tiles; as estradas são os caminhos dessa árvore
    entre alguns pontos sorteados + saídas para a floresta. Devolve {(gx,gz): set(lados no mundo)}."""
    parent = {}
    seen = {(0, 0)}
    stack = [(rng.randrange(n), rng.randrange(n))]
    seen = {stack[0]}
    parent[stack[0]] = None
    while stack:
        c = stack[-1]
        nb = [(c[0] + dx, c[1] + dz) for dx, dz in DIRS.values()]
        nb = [q for q in nb if 0 <= q[0] < n and 0 <= q[1] < n and q not in seen]
        if not nb:
            stack.pop(); continue
        q = nb[rng.randrange(len(nb))]
        seen.add(q); parent[q] = c; stack.append(q)

    def path_to_root(c):
        out = [c]
        while parent[out[-1]] is not None:
            out.append(parent[out[-1]])
        return out

    open_sides = {}

    def link(a, b):
        for c, d in ((a, b), (b, a)):
            dx, dz = d[0] - c[0], d[1] - c[1]
            side = [s for s, v in DIRS.items() if v == (dx, dz)][0]
            open_sides.setdefault(c, set()).add(side)

    cells = [(x, z) for x in range(n) for z in range(n)]
    rng.shuffle(cells)
    marks = cells[:landmarks]
    border = [c for c in cells if c[0] in (0, n - 1) or c[1] in (0, n - 1)]
    marks += border[:exits]
    base = set(path_to_root(marks[0]))
    for m in marks[1:]:
        pa, pb = path_to_root(marks[0]), path_to_root(m)
        common = set(pa) & set(pb)
        lca = next(c for c in pa if c in common)
        for path in (pa, pb):
            for i in range(len(path) - 1):
                if path[i] == lca:
                    break
                link(path[i], path[i + 1])
    for c in border[:exits]:
        outs = [s for s, v in DIRS.items() if not (0 <= c[0] + v[0] < n and 0 <= c[1] + v[1] < n)]
        open_sides.setdefault(c, set()).add(outs[rng.randrange(len(outs))])
    return open_sides


def draw_tile(dr, tile, ox, oz, ppm, k=0, mirror=False, sides=None):
    """ox, oz = canto superior esquerdo em pixels; z cresce para cima. sides = lados (no mundo) com estrada."""
    def px(x, z):
        return ox + (x + HALF) * ppm, oz + (HALF - z) * ppm
    src = tile["Pieces"]
    if sides is not None:
        src = [p for p in src if p["T"] != "road" or (p.get("E", "") == "" and sides) or (p.get("E", "") and world_edge(p["E"], k, mirror) in sides)]
    pieces = [transform(p, k, mirror) for p in src]
    pieces.sort(key=lambda p: (0 if p["T"] in DRAW_ORDER else 1))
    for p in pieces:
        fp = footprint(p)
        col = COLORS.get(p["T"], (255, 0, 255))
        shape, data = fp
        if shape == "circle":
            cx, cz, r = data
            if p["T"] == "tree":
                r2 = 1.5 * p.get("S", 1.0)
                a, b = px(cx - r2, cz + r2), px(cx + r2, cz - r2)
                dr.ellipse([a, b], fill=(25, 60, 30), outline=(15, 40, 20))
            a, b = px(cx - r, cz + r), px(cx + r, cz - r)
            dr.ellipse([a, b], fill=col)
        elif shape == "ellipse":
            cx, cz, a_, b_, rr = data
            pts = []
            for i in range(24):
                t = 2 * math.pi * i / 24
                lx, lz = a_ * math.cos(t), b_ * math.sin(t)
                gx, gz = rot(lx, lz, rr)
                pts.append(px(cx + gx, cz + gz))
            dr.polygon(pts, fill=col)
        else:
            dr.polygon([px(x, z) for x, z in data], fill=col)


def preview(path, seed=7):
    from PIL import Image, ImageDraw
    ppm_t, ppm_m = 14, 7
    cols = 5
    tw = int(TILE * ppm_t)
    pad = 26
    map_n = 6
    mw = int(TILE * map_n * ppm_m)
    forest = 40
    W = max(cols * (tw + 10) + 10, mw + forest * 2 + 20)
    H = 2 * (tw + pad + 10) + 30 + mw + forest * 2 + 20
    img = Image.new("RGB", (W, H), (20, 16, 28))
    dr = ImageDraw.Draw(img)
    try:
        from PIL import ImageFont
        font = ImageFont.truetype("DejaVuSans.ttf", 13)
    except Exception:
        font = None
    grass = (46, 70, 38)
    for idx, t in enumerate(TILES):
        ox = 10 + (idx % cols) * (tw + 10)
        oz = 10 + (idx // cols) * (tw + pad + 10) + pad
        dr.rectangle([ox, oz, ox + tw, oz + tw], fill=grass)
        m = MARGIN * ppm_t
        dr.rectangle([ox + m, oz + m, ox + tw - m, oz + tw - m], outline=(70, 95, 60))
        draw_tile(dr, t, ox, oz, ppm_t)
        dr.text((ox, oz - 18), t["Name"] + ("  (máx. %d por mapa)" % t["Max"] if t["Max"] else ""), fill=(230, 220, 240), font=font)
    # mapa 6×6 sorteado (mesma ideia do jogo: peso, máximo por mapa, rotação e espelho)
    rng = random.Random(seed)
    oy = 2 * (tw + pad + 10) + 30
    ox0 = (W - mw) // 2
    dr.rectangle([ox0 - forest, oy, ox0 + mw + forest, oy + mw + forest * 2], fill=(14, 30, 18))
    oz0 = oy + forest
    dr.rectangle([ox0, oz0, ox0 + mw, oz0 + mw], fill=grass)
    used = {}
    net = road_network(rng, map_n)
    for gz in range(map_n):
        for gx in range(map_n):
            pool = [t for t in TILES if not t["Max"] or used.get(t["Id"], 0) < t["Max"]]
            total = sum(t["Weight"] for t in pool)
            r = rng.random() * total
            for t in pool:
                r -= t["Weight"]
                if r <= 0:
                    break
            used[t["Id"]] = used.get(t["Id"], 0) + 1
            k = rng.randrange(4); mirror = rng.random() < 0.5
            # gz cresce para o norte; na imagem o norte fica em cima
            draw_tile(dr, t, ox0 + gx * TILE * ppm_m, oz0 + (map_n - 1 - gz) * TILE * ppm_m, ppm_m, k, mirror, net.get((gx, gz), set()))
    for i in range(1, map_n):
        x = ox0 + i * TILE * ppm_m
        for y in range(oz0, oz0 + mw, 8):
            dr.line([(x, y), (x, y + 3)], fill=(70, 95, 60))
        y = oz0 + i * TILE * ppm_m
        for x in range(ox0, ox0 + mw, 8):
            dr.line([(x, y), (x + 3, y)], fill=(70, 95, 60))
    dr.text((ox0 - forest, oy - 18), "Exemplo de mapa 6x6 sorteado (seed %d), floresta em volta; linhas tracejadas = emendas dos tiles" % seed, fill=(230, 220, 240), font=font)
    img.save(path)


def main():
    out = sys.argv[1] if len(sys.argv) > 1 else "farm_tiles.json"
    ok = True
    for t in TILES:
        errs, open_pct = validate(t)
        status = "OK " if not errs else "ERRO"
        print("%s %-16s %3d peças · %4.1f%% livre" % (status, t["Name"], len(t["Pieces"]), open_pct))
        for e in errs:
            print("     - " + e)
        ok = ok and not errs
    if not ok:
        sys.exit("tiles inválidos: nada foi gravado")
    # formas de colisão de cada peça (o C# lê daqui: uma fonte só). W/D = 0 -> vem da peça (W, L).
    shapes = [{"T": t, "Shape": v[0], "W": v[1] or 0.0, "D": v[2] or 0.0, "Block": v[3]} for t, v in PIECES.items()]
    data = {"TileSize": TILE, "Margin": MARGIN, "Shapes": rounded(shapes), "Tiles": rounded(TILES)}
    with open(out, "w", encoding="utf-8") as f:
        json.dump(data, f, ensure_ascii=False, indent=1)
    print("gravado:", out)
    if len(sys.argv) > 2:
        preview(sys.argv[2], int(sys.argv[3]) if len(sys.argv) > 3 else 7)
        print("prévia:", sys.argv[2])


if __name__ == "__main__":
    main()
