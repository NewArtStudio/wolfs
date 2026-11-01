"""
Builds VampWolfs' gear catalog from the Space project's equipment data.

Same system as Space (10 slots, 8 tiers, 4 styles, STR/DEX/INT/VIT with base + cap + total per piece,
refine, card/gem sockets), with every item renamed to fit Victorian-era Europe.

Usage:
    python3 build_gear_data.py <path-to-space-repo> <output gear.json>

Output is read at runtime from Assets/VampWolfs/Resources/VampWolfs/gear.json (Unity JsonUtility format:
arrays of objects, PascalCase keys).
"""
import json
import sys
from collections import defaultdict

space_root, out_path = sys.argv[1], sys.argv[2]
data_dir = space_root.rstrip("/") + "/Assets/_Project/Data/Prototype/"

equipment = json.load(open(data_dir + "equipment.json", encoding="utf-8"))
equip_types = json.load(open(data_dir + "equipTypes.json", encoding="utf-8"))

# ---------------------------------------------------------------- slots and types (same structure as Space)

SLOT_NAMES = {
    "head": "Chapéu / Elmo",
    "chest": "Casaco / Couraça",
    "gloves": "Luvas",
    "boots": "Botas",
    "handR": "Mão direita",
    "handL": "Mão esquerda",
    "accMajor": "Relíquia",
    "acc1": "Joia 1",
    "acc2": "Joia 2",
    "acc3": "Joia 3",
}
SLOT_ORDER = ["head", "chest", "gloves", "boots", "handR", "handL", "accMajor", "acc1", "acc2", "acc3"]

TYPE_NAMES = {
    "head": "Chapéu / Elmo", "chest": "Casaco / Couraça", "gloves": "Luvas", "boots": "Botas",
    "handR": "Arma de uma mão", "twoHand": "Arma de duas mãos", "melee1H": "Lâmina (1 mão)",
    "melee2H": "Arma pesada (2 mãos)", "handL": "Mão esquerda", "accMajor": "Relíquia", "accMinor": "Joia",
}

# ---------------------------------------------------------------- Victorian naming

# Tier words (I..VIII) per material family. Written as "de ..." complements so Portuguese gender never breaks.
TIERS = {
    "metal":   ["de Ferro Velho", "de Ferro Batido", "de Aço", "de Aço de Sheffield", "de Bronze Lavrado", "de Prata", "da Guarda Real", "da Coroa Imperial"],
    "leather": ["de Couro Cru", "de Couro Curtido", "de Couro de Cervo", "de Couro Encerado", "de Couro Inglês", "de Couro de Lobo", "do Caçador Real", "da Coroa Imperial"],
    "arcane":  ["de Linho Gasto", "de Lã Negra", "de Algodão Egípcio", "de Veludo", "de Seda", "de Seda Bordada", "da Ordem Hermética", "da Coroa Imperial"],
    "sacred":  ["do Noviço", "do Pároco", "do Capelão", "do Cônego", "do Bispo", "do Arcebispo", "do Cardeal", "do Sumo Pontífice"],
    "weapon":  ["de Sucata", "de Ferro", "de Aço", "de Sheffield", "de Birmingham", "de Prata", "da Guarda Real", "da Coroa Imperial"],
    "wood":    ["de Freixo", "de Teixo", "de Ébano", "de Marfim", "de Osso Entalhado", "de Prata Rúnica", "da Ordem Hermética", "da Coroa Imperial"],
    "jewel":   ["de Latão", "de Cobre", "de Estanho Polido", "de Prata", "de Prata Lavrada", "de Ouro", "de Ouro Branco", "da Coroa Imperial"],
}

# (type, focus or shield kind) -> (base nouns [1st, alternate for duplicates], tier family)
NOUNS = {
    ("chest", "vanguard"): (["Couraça", "Peitoral de Cavalaria"], "metal"),
    ("chest", "scout"): (["Sobretudo", "Gibão de Caçador"], "leather"),
    ("chest", "arcane"): (["Sobrecasaca Ocultista", "Manto de Médium"], "arcane"),
    ("chest", "sacred"): (["Batina", "Hábito de Inquisidor"], "sacred"),
    ("head", "vanguard"): (["Elmo de Cavalaria", "Capacete de Couraceiro"], "metal"),
    ("head", "scout"): (["Chapéu de Abas Largas", "Boné de Caça"], "leather"),
    ("head", "arcane"): (["Cartola", "Capuz de Médium"], "arcane"),
    ("head", "sacred"): (["Chapéu de Inquisidor", "Solidéu"], "sacred"),
    ("gloves", "vanguard"): (["Manoplas", "Luvas de Malha"], "metal"),
    ("gloves", "scout"): (["Luvas de Pelica", "Luvas de Arqueiro"], "leather"),
    ("gloves", "arcane"): (["Luvas de Seda Bordadas", "Mitenes de Ocultista"], "arcane"),
    ("gloves", "sacred"): (["Luvas de Capelão", "Luvas Bentas"], "sacred"),
    ("boots", "vanguard"): (["Botas de Montaria", "Grevas"], "metal"),
    ("boots", "scout"): (["Botinas de Cano Curto", "Botas de Rastreador"], "leather"),
    ("boots", "arcane"): (["Sapatos de Verniz", "Botinas de Salão"], "arcane"),
    ("boots", "sacred"): (["Botas de Peregrino", "Sandálias de Penitente"], "sacred"),
    ("handR", "vanguard"): (["Pistola de Pederneira", "Pistola de Duelo"], "weapon"),
    ("handR", "scout"): (["Revólver", "Derringer"], "weapon"),
    ("handR", "arcane"): (["Varinha", "Bastão de Ocultista"], "wood"),
    ("handR", "sacred"): (["Crucifixo", "Aspersório"], "sacred"),
    ("twoHand", "vanguard"): (["Mosquete", "Bacamarte"], "weapon"),
    ("twoHand", "arcane"): (["Cajado", "Cetro Hermético"], "wood"),
    ("melee1H", "vanguard"): (["Sabre de Cavalaria", "Espada de Oficial"], "weapon"),
    ("melee1H", "scout"): (["Bengala-Espada", "Florete"], "weapon"),
    ("melee2H", "vanguard"): (["Machado de Carrasco", "Montante"], "weapon"),
    ("melee2H", "scout"): (["Foice de Ceifador", "Alabarda"], "weapon"),
    ("handL", "heavy"): (["Escudo de Carvalho", "Escudo Ferrado"], "metal"),
    ("handL", "buckler"): (["Broquel", "Escudo de Duelo"], "leather"),
    ("handL", "energy"): (["Escudo Bento", "Égide Sagrada"], "sacred"),
    ("handL", "arcane"): (["Grimório", "Livro de Sombras"], "arcane"),
    ("handL", "sacred"): (["Relicário de Mão", "Lanterna Sagrada"], "sacred"),
    ("handL", "vanguard"): (["Adaga de Aparo", "Mão-Esquerda"], "weapon"),
    ("handL", "scout"): (["Faca de Arremesso", "Adaga de Aparo"], "weapon"),
    ("accMajor", "str"): (["Medalha do Regimento", "Insígnia de Boxe"], "jewel"),
    ("accMajor", "dex"): (["Relógio de Bolso", "Bússola de Bolso"], "jewel"),
    ("accMajor", "int"): (["Monóculo de Ocultista", "Pêndulo de Médium"], "jewel"),
    ("accMajor", "vit"): (["Frasco de Tônico Vital", "Amuleto de Alho"], "jewel"),
    ("accMinor", "str"): (["Anel de Sinete", "Bracelete de Ferro"], "jewel"),
    ("accMinor", "dex"): (["Abotoaduras", "Alfinete de Gravata"], "jewel"),
    ("accMinor", "int"): (["Broche de Camafeu", "Anel de Ônix"], "jewel"),
    ("accMinor", "vit"): (["Rosário", "Medalhão de Santo"], "jewel"),
}


def noun_key(e):
    if e["type"] == "handL" and e.get("shield"):
        return ("handL", e["shield"])
    return (e["type"], e["focus"])


items = sorted(equipment.values(), key=lambda e: (e["tier"], SLOT_ORDER.index(equip_types[e["type"]]["slots"][0]) if equip_types[e["type"]]["slots"][0] in SLOT_ORDER else 99, e["id"]))
used_names = set()
group_count = defaultdict(int)
out_equipment = []

for e in items:
    key = noun_key(e)
    nouns, family = NOUNS[key]
    index = group_count[(key, e["tier"])]
    group_count[(key, e["tier"])] += 1
    noun = nouns[min(index, len(nouns) - 1)]
    name = noun + " " + TIERS[family][e["tier"] - 1]
    if name in used_names:
        name = name + " (variante)"
    used_names.add(name)

    stats = e.get("stats", {})
    out_equipment.append({
        "Id": e["id"],
        "Name": name,
        "Type": e["type"],
        "Tier": e["tier"],
        "Focus": e["focus"],
        "ShieldKind": e.get("shield") or "",
        "Model": e.get("model") or "",  # which tier-I drawing the lobby icon reuses
        "Dmg": e["dmg"],
        "Attrs": {"Str": e["attrs"]["str"], "Dex": e["attrs"]["dex"], "Int": e["attrs"]["int"], "Vit": e["attrs"]["vit"]},
        "AttrCap": {"Str": e["attrCap"]["str"], "Dex": e["attrCap"]["dex"], "Int": e["attrCap"]["int"], "Vit": e["attrCap"]["vit"]},
        "AttrTotal": e["attrTotal"],
        "CardSlots": e["cardSlots"],
        "GemSlot": e["gemSlot"],
        "Refinable": e["refinable"],
        "Armour": stats.get("armour", 0),
        "Evasion": stats.get("evasion", 0),
        "Ward": stats.get("es", 0),
        "Move": stats.get("move", 0),
    })

# ---------------------------------------------------------------- upgrade materials (Space items.json → Victorian names)

materials = [
    {"Id": "ore_refine_weapon", "Name": "Lingote de Aço de Sheffield", "Category": "Material de refino", "Kind": "refine", "Target": "weapon",
     "Desc": "Refina uma arma (+1, até +{max}). Do +5 em diante pode falhar — e a arma QUEBRA.", "Color": "#ff8a3d"},
    {"Id": "ore_refine_armor", "Name": "Rebites de Latão Temperado", "Category": "Material de refino", "Kind": "refine", "Target": "armor",
     "Desc": "Refina uma peça de armadura ou mão esquerda (+1, até +{max}). Do +5 em diante pode falhar — e a peça QUEBRA.", "Color": "#7fb2ff"},
    {"Id": "crystal_str", "Name": "Tônico de Força", "Category": "Tônico de atributo", "Kind": "enchant", "Target": "any", "Attr": "Str",
     "Desc": "+1 de Força numa peça (até o teto do atributo e o total de pontos da peça).", "Color": "#e0782f"},
    {"id_": 0, "Id": "crystal_dex", "Name": "Tônico de Destreza", "Category": "Tônico de atributo", "Kind": "enchant", "Target": "any", "Attr": "Dex",
     "Desc": "+1 de Destreza numa peça (até o teto do atributo e o total de pontos da peça).", "Color": "#2ec4a6"},
    {"Id": "crystal_int", "Name": "Tônico de Intelecto", "Category": "Tônico de atributo", "Kind": "enchant", "Target": "any", "Attr": "Int",
     "Desc": "+1 de Inteligência numa peça (até o teto do atributo e o total de pontos da peça).", "Color": "#9b6bff"},
    {"Id": "crystal_vit", "Name": "Tônico de Vitalidade", "Category": "Tônico de atributo", "Kind": "enchant", "Target": "any", "Attr": "Vit",
     "Desc": "+1 de Vitalidade numa peça (até o teto do atributo e o total de pontos da peça).", "Color": "#f2d27a"},
    {"Id": "key_card_weapon", "Name": "Chave de Relojoeiro · Arma", "Category": "Chave de encaixe", "Kind": "unlockCard", "Target": "weapon", "Slot": 1,
     "Desc": "Abre o 1º encaixe de carta de uma arma.", "Color": "#ffd36b"},
    {"Id": "key_card_armor", "Name": "Chave de Relojoeiro · Armadura", "Category": "Chave de encaixe", "Kind": "unlockCard", "Target": "armor", "Slot": 1,
     "Desc": "Abre o 1º encaixe de carta de uma peça de armadura.", "Color": "#ffd36b"},
    {"Id": "key_card_acc", "Name": "Chave de Relojoeiro · Joia", "Category": "Chave de encaixe", "Kind": "unlockCard", "Target": "accessory", "Slot": 1,
     "Desc": "Abre o 1º encaixe de carta de uma joia ou relíquia.", "Color": "#ffd36b"},
    {"Id": "key_gem", "Name": "Pinça de Joalheiro", "Category": "Chave de encaixe", "Kind": "unlockGem", "Target": "any",
     "Desc": "Abre o encaixe de gema de um equipamento (se ele tiver).", "Color": "#59d8ff"},
    {"Id": "gem_ruby", "Name": "Rubi", "Category": "Gema", "Kind": "gem", "Target": "any", "Attr": "Str", "Amount": 3,
     "Desc": "Gema: +3 de Força (passa do teto da peça). Vai num encaixe de gema aberto.", "Color": "#e8203a"},
    {"Id": "gem_emerald", "Name": "Esmeralda", "Category": "Gema", "Kind": "gem", "Target": "any", "Attr": "Dex", "Amount": 3,
     "Desc": "Gema: +3 de Destreza (passa do teto da peça). Vai num encaixe de gema aberto.", "Color": "#2ecc71"},
    {"Id": "gem_sapphire", "Name": "Safira", "Category": "Gema", "Kind": "gem", "Target": "any", "Attr": "Int", "Amount": 3,
     "Desc": "Gema: +3 de Inteligência (passa do teto da peça). Vai num encaixe de gema aberto.", "Color": "#3d6bff"},
    {"Id": "gem_amber", "Name": "Âmbar", "Category": "Gema", "Kind": "gem", "Target": "any", "Attr": "Vit", "Amount": 3,
     "Desc": "Gema: +3 de Vitalidade (passa do teto da peça). Vai num encaixe de gema aberto.", "Color": "#ffb02e"},
]
for m in materials:
    m.pop("id_", None)
    m.setdefault("Attr", "")
    m.setdefault("Slot", 0)
    m.setdefault("Amount", 0)

# ---------------------------------------------------------------- tier power (keeps PvP time-to-kill similar across tiers)


def status_atk(a):
    a = int(a)
    return a + (a // 10) ** 2


tier_power = []
for t in range(1, 9):
    tier_items = [e for e in equipment.values() if e["tier"] == t]
    weapons = [e["dmg"] for e in tier_items if e["dmg"] > 0]
    avg_weapon = sum(weapons) / len(weapons)
    # a focused build puts roughly half of all attribute points of 10 pieces into its main attribute
    avg_total = sum(e["attrTotal"] for e in tier_items) / len(tier_items)
    main_attr = min(99, avg_total * 10 * 0.5)
    tier_power.append(round(avg_weapon + status_atk(main_attr), 1))

catalog = {
    "Rules": {
        "RefineMax": 10,
        "RefineBonus": [0.05, 0.05, 0.05, 0.04, 0.04, 0.03, 0.03, 0.02, 0.02, 0.02],
        "RefineBreakChance": [0, 0, 0, 0, 0.20, 0.30, 0.40, 0.55, 0.65, 0.75],
        "TierPower": tier_power,
    },
    "Slots": [{"Id": s, "Name": SLOT_NAMES[s]} for s in SLOT_ORDER],
    "Types": [{"Id": k, "Name": TYPE_NAMES[k], "Slots": v["slots"], "TwoHand": bool(v.get("twoHand")), "Category": v["category"]}
              for k, v in equip_types.items()],
    "Equipment": out_equipment,
    "Materials": materials,
}

with open(out_path, "w", encoding="utf-8") as f:
    json.dump(catalog, f, ensure_ascii=False, indent=1)

print("equipment:", len(out_equipment), "materials:", len(materials), "tier power:", tier_power)
print("sample names:", [e["Name"] for e in out_equipment[:12]])
