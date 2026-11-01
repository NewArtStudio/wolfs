"""
Builds VampWolfs' skill catalog: Assets/VampWolfs/Resources/VampWolfs/skills.json  (all numbers are [PROTÓTIPO]).

Two sources of abilities (decided with the creator, nov/2026, inspired by Albion Online):
  * EQUIPMENT: the weapon gives the basic attack (LMB), 3 keys (RMB, Q, E) — each key with 2-3 options the
    player chooses — and a fixed ultimate (F). Helmet (R), chest (Space) and boots (Shift) give one key each,
    also with 3 options. Skills belong to the item FAMILY (e.g. every "Sabre de Cavalaria" tier shares a kit);
    a different weapon family is a different kit. Gear skills have no level: they grow with the gear stats.
  * CLASS: 20 per class (10 actives + 10 passives), level 1..10, unlocked by character level (1..50).
    The player slots 2 actives (keys 1 and 2); every learned passive is always on. 1 skill point per level.

Descriptions are built in C# from the numbers (SkillText); here only a short flavour line ("Desc").
Usage:  python3 build_skill_data.py <output skills.json>
"""
import json
import sys

out_path = sys.argv[1]

# Every field of C# SkillDef with its default (always written, so JsonUtility never guesses).
DEFAULTS = dict(
    Id="", Name="", Desc="", Kind="", Source="", Owner="", Key="", Fx="", Color="#ffffff", ReqLevel=1,
    Cooldown=0.0, CastTime=0.0, MoveMult=0.35, EnergyCost=0.0, Range=0.0, Speed=0.0, Radius=0.0,
    Count=1, Spread=0.0, Pierce=False, Reach=0.0, Angle=0.0, AtAim=False, Basic=False,
    Damage=0.0, DmgType="", Lifesteal=0.0, SlowAmount=0.0, SlowDuration=0.0, StunDuration=0.0, RootDuration=0.0,
    DotDps=0.0, DotDuration=0.0, Knockback=0.0, VulnAmount=0.0, VulnDuration=0.0,
    SelfHeal=0.0, SelfHaste=0.0, SelfHasteDuration=0.0, DR=0.0, DRDuration=0.0, ShieldPct=0.0, ShieldDuration=0.0,
    DmgBuff=0.0, DmgBuffDuration=0.0, Invuln=0.0, Cleanse=False,
    MaxDistance=0.0, MinDistance=0.0, DashDuration=0.0, LandingRadius=0.0,
    CounterDuration=0.0, CounterStun=0.0, CounterHeal=0.0,
    ZoneDuration=0.0, ZoneHealPs=0.0, TrapLife=0.0, TrapArm=0.0,
    P_Damage=0.0, P_Hp=0.0, P_Move=0.0, P_Lifesteal=0.0, P_Cdr=0.0, P_Energy=0.0, P_DR=0.0, P_Heal=0.0,
    P_LowHpDamage=0.0, P_KillHeal=0.0, P_BasicDotDps=0.0, P_SlowedBonus=0.0, P_Regen=0.0,
    GrowPct=0.0,
)

skills = []
families = []


def add(kind, sid, name, desc, **kw):
    s = dict(DEFAULTS)
    s.update(kw)
    s["Id"], s["Name"], s["Desc"], s["Kind"] = sid, name, desc, kind
    unknown = set(kw) - set(DEFAULTS)
    assert not unknown, (sid, unknown)
    skills.append(s)
    return s


# shorthands (sensible defaults per kind)
def proj(sid, name, desc, **kw):
    kw.setdefault("CastTime", 0.35); kw.setdefault("Range", 11.0); kw.setdefault("Speed", 26.0); kw.setdefault("Radius", 0.35)
    return add("projectile", sid, name, desc, **kw)


def melee(sid, name, desc, **kw):
    kw.setdefault("CastTime", 0.35); kw.setdefault("Reach", 2.5); kw.setdefault("Angle", 110.0)
    return add("melee", sid, name, desc, **kw)


def area(sid, name, desc, **kw):
    kw.setdefault("CastTime", 0.3); kw.setdefault("Radius", 3.0)
    if kw.get("AtAim"): kw.setdefault("Range", 9.0)
    return add("area", sid, name, desc, **kw)


def dash(sid, name, desc, **kw):
    kw.setdefault("MaxDistance", 6.0); kw.setdefault("MinDistance", 2.5); kw.setdefault("DashDuration", 0.15)
    return add("dash", sid, name, desc, **kw)


def counter(sid, name, desc, **kw):
    kw.setdefault("CounterDuration", 1.0); kw.setdefault("CounterStun", 0.8)
    return add("counter", sid, name, desc, **kw)


def buff(sid, name, desc, **kw):
    return add("buff", sid, name, desc, **kw)


def zone(sid, name, desc, **kw):
    kw.setdefault("CastTime", 0.35); kw.setdefault("Radius", 3.0); kw.setdefault("ZoneDuration", 4.0)
    if kw.get("AtAim"): kw.setdefault("Range", 10.0)
    return add("zone", sid, name, desc, **kw)


def trap(sid, name, desc, **kw):
    kw.setdefault("CastTime", 0.25); kw.setdefault("Radius", 1.4); kw.setdefault("Range", 8.0)
    kw.setdefault("TrapLife", 12.0); kw.setdefault("TrapArm", 0.6)
    return add("trap", sid, name, desc, **kw)


def passive(sid, name, desc, **kw):
    return add("passive", sid, name, desc, **kw)


def family(fid, name, slot, dmg, color, kit):
    """kit: dict key -> list of skills (weapon: primary, q1, q2, q3, ult; armor: its slot key)."""
    families.append(dict(Id=fid, Name=name, Slot=slot))
    for key, lst in kit.items():
        for s in lst:
            s["Source"] = slot
            s["Owner"] = fid
            s["Key"] = key
            if not s["DmgType"]:
                s["DmgType"] = dmg
            if s["Color"] == "#ffffff":
                s["Color"] = color
            if key == "primary":
                s["Basic"] = True
            if key == "ult":
                s["EnergyCost"] = 100.0
                s["Cooldown"] = max(s["Cooldown"], 3.0)


ULT = dict(MoveMult=0.0)

# ====================================================================== WEAPONS (10 families)

family("handR_arcane", "Varinha", "weapon", "magic", "#b46cff", {
    "primary": [proj("w_wand_basic", "Faísca Arcana", "Centelha rápida de éter.", Damage=8, CastTime=0.3, MoveMult=0.55, Speed=26)],
    "q1": [
        proj("w_wand_ray", "Raio Arcano", "Disparo concentrado que atravessa a névoa.", Damage=17, CastTime=0.6, Cooldown=4, Range=13, Speed=32, MoveMult=0.25),
        proj("w_wand_fan", "Leque de Faíscas", "Três faíscas em leque.", Damage=7, Count=3, Spread=26, Cooldown=5, CastTime=0.4),
        proj("w_wand_slow", "Orbe Entorpecente", "Orbe lento que pesa nas pernas.", Damage=11, SlowAmount=0.4, SlowDuration=2, Cooldown=6, Speed=18),
    ],
    "q2": [
        proj("w_wand_root", "Prisão Etérea", "Prende o alvo no lugar.", Damage=5, RootDuration=1.3, Cooldown=10, Speed=22, Range=10),
        area("w_wand_blast", "Explosão Arcana", "Explode no ponto mirado.", Damage=16, AtAim=True, Radius=2.4, CastTime=0.6, Cooldown=8),
        area("w_wand_push", "Repulsão", "Empurra tudo para longe de você.", Damage=6, Radius=3.0, Knockback=4, Cooldown=9, CastTime=0.15),
    ],
    "q3": [
        dash("w_wand_blink", "Piscar", "Some e reaparece adiante.", MaxDistance=6.5, MinDistance=6.5, DashDuration=0.06, Cooldown=8, Fx="mist_step"),
        buff("w_wand_shield", "Escudo de Éter", "Bolha que absorve dano.", ShieldPct=0.22, ShieldDuration=3, Cooldown=12),
        zone("w_wand_fog", "Névoa Arcana", "Área que deixa os inimigos lentos.", AtAim=True, Radius=3, ZoneDuration=4, SlowAmount=0.35, SlowDuration=0.6, Cooldown=12),
    ],
    "ult": [zone("w_wand_ult", "Tempestade de Éter", "Tempestade arcana sobre o ponto mirado.", AtAim=True, Radius=3.6, ZoneDuration=4, Damage=6, SlowAmount=0.3, SlowDuration=0.6, CastTime=0.6, **ULT)],
})

family("handR_sacred", "Crucifixo", "weapon", "magic", "#ffd27a", {
    "primary": [proj("w_cross_basic", "Luz Benta", "Luz que fere e devolve um pouco de vida.", Damage=7, Lifesteal=0.25, CastTime=0.32, MoveMult=0.55, Speed=24)],
    "q1": [
        proj("w_cross_ray", "Raio Sagrado", "Feixe de luz concentrada.", Damage=15, CastTime=0.55, Cooldown=4, Speed=30, Range=12, MoveMult=0.25),
        zone("w_cross_water", "Água Benta", "Frasco que queima o chão sob os ímpios.", AtAim=True, Radius=2.2, ZoneDuration=3, Damage=5, Cooldown=7, Range=9),
        proj("w_cross_sign", "Sinal da Cruz", "Cruz de luz que atravessa os inimigos.", Damage=12, Pierce=True, Cooldown=5, Speed=24),
    ],
    "q2": [
        proj("w_cross_exorcism", "Exorcismo", "Atordoa a criatura atingida.", Damage=5, StunDuration=0.9, Cooldown=11, Speed=22, Range=9),
        area("w_cross_flash", "Clarão", "Luz que cega e desacelera em volta.", Damage=6, Radius=3.5, SlowAmount=0.5, SlowDuration=1.5, Cooldown=9, CastTime=0.2),
        proj("w_cross_mark", "Marca do Pecador", "O alvo marcado sofre mais dano.", Damage=4, VulnAmount=0.2, VulnDuration=4, Cooldown=10),
    ],
    "q3": [
        buff("w_cross_prayer", "Oração", "Cura e protege por um instante.", SelfHeal=22, DR=0.3, DRDuration=2, Cooldown=12),
        zone("w_cross_sanctuary", "Santuário", "Chão sagrado que cura quem o conjurou.", Radius=3, ZoneDuration=4, ZoneHealPs=5, Cooldown=15),
        dash("w_cross_step", "Passo Abençoado", "Avanço protegido por luz.", MaxDistance=5, Invuln=0.3, Cooldown=9),
    ],
    "ult": [area("w_cross_ult", "Juízo Final", "Coluna de luz que atordoa todos na área.", AtAim=True, Radius=4, Damage=28, StunDuration=1.0, CastTime=0.8, Range=10, **ULT)],
})

family("handR_scout", "Revólver", "weapon", "ranged", "#e8c38a", {
    "primary": [proj("w_rev_basic", "Disparo", "Tiro rápido de revólver.", Damage=7, CastTime=0.22, MoveMult=0.7, Speed=38, Range=12, Radius=0.25)],
    "q1": [
        proj("w_rev_aimed", "Tiro Certeiro", "Mira com calma e acerta forte.", Damage=19, CastTime=0.7, Cooldown=5, Range=15, Speed=46, MoveMult=0.2),
        proj("w_rev_fan", "Leque de Balas", "Esvazia o tambor num leque.", Damage=5, Count=5, Spread=40, Cooldown=6, CastTime=0.35, Speed=34, Range=9),
        proj("w_rev_leg", "Tiro na Perna", "Deixa o alvo mancando.", Damage=8, SlowAmount=0.5, SlowDuration=2, Cooldown=6, Speed=40),
    ],
    "q2": [
        dash("w_rev_roll", "Rolamento", "Rola para fora de perigo.", MaxDistance=4.5, MinDistance=4.5, Cooldown=7),
        zone("w_rev_smoke", "Bomba de Fumaça", "Fumaça que atrasa quem a atravessa.", AtAim=True, Radius=3, ZoneDuration=4, SlowAmount=0.4, SlowDuration=0.6, Cooldown=12, Range=7),
        trap("w_rev_beartrap", "Armadilha de Urso", "Mandíbulas de ferro escondidas no feno.", Damage=10, RootDuration=1.5, Cooldown=12),
    ],
    "q3": [
        buff("w_rev_reload", "Recarga Rápida", "Mais rápido e mais letal por um tempo.", SelfHaste=0.3, SelfHasteDuration=2.5, DmgBuff=0.2, DmgBuffDuration=3, Cooldown=14),
        proj("w_rev_warning", "Tiro de Advertência", "Tiro que empurra o alvo para trás.", Damage=6, Knockback=3, Cooldown=9, Speed=40),
        area("w_rev_grenade", "Granada", "Explode no ponto mirado.", Damage=16, AtAim=True, Radius=2.5, CastTime=0.5, Cooldown=9, Range=9),
    ],
    "ult": [proj("w_rev_ult", "Fogo à Vontade", "Rajada de nove balas.", Damage=9, Count=9, Spread=60, CastTime=0.5, Speed=38, Range=11, **ULT)],
})

family("handR_vanguard", "Pistola de Pederneira", "weapon", "ranged", "#ff9a4a", {
    "primary": [proj("w_flint_basic", "Tiro de Pederneira", "Disparo pesado e lento.", Damage=10, CastTime=0.4, MoveMult=0.5, Speed=30, Range=11)],
    "q1": [
        proj("w_flint_charge", "Carga de Pólvora", "Tiro que arremessa o alvo.", Damage=19, Knockback=2.5, CastTime=0.6, Cooldown=6, Speed=32),
        proj("w_flint_buck", "Chumbo Grosso", "Espalha chumbo de perto.", Damage=9, Count=3, Spread=24, Range=6.5, Cooldown=5, Speed=34),
        proj("w_flint_fire", "Bala Incendiária", "Bala que deixa o alvo queimando.", Damage=8, DotDps=4, DotDuration=4, Cooldown=6, Speed=30),
    ],
    "q2": [
        melee("w_flint_butt", "Coronhada", "Golpe com a coronha que atordoa.", Damage=8, StunDuration=0.8, Reach=2.2, Angle=90, Cooldown=9, CastTime=0.25, DmgType="melee"),
        trap("w_flint_powder", "Pólvora no Chão", "Rastro de pólvora que explode.", Damage=18, Radius=1.8, Cooldown=11),
        dash("w_flint_hop", "Recuo Tático", "Salto rápido para reposicionar.", MaxDistance=4.5, Cooldown=8),
    ],
    "q3": [
        buff("w_flint_iron", "Couraça de Ferro", "Aguenta o tranco por dois segundos.", DR=0.5, DRDuration=2, Cooldown=13),
        zone("w_flint_smoke", "Cortina de Fumaça", "Fumaça de pólvora que atrasa os inimigos.", AtAim=True, Radius=3.2, ZoneDuration=4, SlowAmount=0.35, SlowDuration=0.6, Cooldown=12, Range=8),
        proj("w_flint_hook", "Gancho de Abordagem", "Puxa o alvo para perto.", Damage=6, Knockback=-4.5, Cooldown=12, Speed=28, Range=10),
    ],
    "ult": [proj("w_flint_ult", "Canhão de Mão", "Bala de canhão que atravessa e arremessa.", Damage=38, Knockback=4, Pierce=True, Radius=0.7, CastTime=0.9, Speed=30, Range=14, Fx="feast", **ULT)],
})

family("melee1H_scout", "Bengala-Espada", "weapon", "melee", "#d8e2f0", {
    "primary": [melee("w_cane_basic", "Estocada", "Estocada rápida e precisa.", Damage=8, Reach=2.4, Angle=70, CastTime=0.25, MoveMult=0.6)],
    "q1": [
        dash("w_cane_lunge", "Investida Elegante", "Avança e fere ao chegar.", MaxDistance=5, LandingRadius=1.6, Damage=10, Cooldown=6, Fx="rush"),
        melee("w_cane_hidden", "Lâmina Oculta", "Corte que deixa o alvo sangrando.", Damage=12, DotDps=4, DotDuration=3, Cooldown=6, Reach=2.4),
        counter("w_cane_feint", "Finta", "Provoca o golpe e responde atordoando.", CounterDuration=1.0, CounterStun=0.8, Cooldown=10),
    ],
    "q2": [
        melee("w_cane_tendon", "Golpe no Tendão", "Deixa o alvo lento.", Damage=9, SlowAmount=0.5, SlowDuration=2, Cooldown=7),
        area("w_cane_whirl", "Redemoinho", "Giro com a lâmina em volta de si.", Damage=12, Radius=2.8, Cooldown=8, CastTime=0.25),
        melee("w_cane_disarm", "Desarme", "Abre a guarda: o alvo sofre mais dano.", Damage=6, VulnAmount=0.25, VulnDuration=3, Cooldown=10),
    ],
    "q3": [
        dash("w_cane_dodge", "Esquiva", "Desvio rápido, intocável.", MaxDistance=4, MinDistance=4, Invuln=0.25, Cooldown=8),
        buff("w_cane_duelist", "Passo do Duelista", "Fica bem mais rápido.", SelfHaste=0.4, SelfHasteDuration=2, Cooldown=11),
        counter("w_cane_guard", "Guarda Alta", "Apara o golpe e se cura.", CounterDuration=1.2, CounterStun=0.6, CounterHeal=10, Cooldown=13),
    ],
    "ult": [melee("w_cane_ult", "Mil Cortes", "Tempestade de cortes em volta de você.", Damage=30, Angle=360, Reach=3.2, DotDps=5, DotDuration=3, CastTime=0.5, Fx="rend", **ULT)],
})

family("melee1H_vanguard", "Sabre de Cavalaria", "weapon", "melee", "#c0c6ce", {
    "primary": [melee("w_sabre_basic", "Corte de Sabre", "Corte largo de cavalaria.", Damage=10, Reach=2.5, Angle=110, CastTime=0.35, MoveMult=0.5, Fx="claws")],
    "q1": [
        dash("w_sabre_charge", "Carga de Cavalaria", "Avança e derruba ao chegar.", MaxDistance=6, LandingRadius=2, Damage=12, SlowAmount=0.4, SlowDuration=1.5, Cooldown=7, Fx="pounce"),
        melee("w_sabre_heavy", "Golpe Pesado", "Golpe lento e brutal.", Damage=20, CastTime=0.6, Cooldown=5, MoveMult=0.2),
        melee("w_sabre_wide", "Talho Largo", "Varre tudo à sua frente.", Damage=13, Angle=180, Reach=3, Cooldown=6, CastTime=0.4),
    ],
    "q2": [
        melee("w_sabre_pommel", "Pomo no Queixo", "Atordoa com o cabo do sabre.", Damage=6, StunDuration=1.0, Reach=2.2, Angle=80, Cooldown=10, CastTime=0.25),
        buff("w_sabre_cry", "Grito de Guerra", "Mais dano por alguns segundos.", DmgBuff=0.25, DmgBuffDuration=4, Cooldown=14, Fx="howl"),
        proj("w_sabre_pull", "Laço", "Laça o alvo e o traz para perto.", Damage=4, Knockback=-4, Cooldown=12, Speed=24, Range=8, DmgType="melee"),
    ],
    "q3": [
        counter("w_sabre_block", "Bloqueio", "Bloqueia e atordoa quem atacar.", CounterDuration=1.1, CounterStun=1.0, Cooldown=12),
        buff("w_sabre_cuirass", "Peitoral", "Reduz o dano recebido.", DR=0.4, DRDuration=3, Cooldown=13, Fx="hide"),
        dash("w_sabre_advance", "Avanço", "Ganha terreno rapidamente.", MaxDistance=5, Cooldown=8, Fx="rush"),
    ],
    "ult": [dash("w_sabre_ult", "Carga do Regimento", "Carga devastadora que atordoa na chegada.", MaxDistance=8, MinDistance=4, DashDuration=0.3, LandingRadius=3, Damage=28, StunDuration=1.0, CastTime=0.3, Fx="pounce", **ULT)],
})

family("melee2H_scout", "Foice de Ceifador", "weapon", "melee", "#9de07a", {
    "primary": [melee("w_scythe_basic", "Ceifada", "Arco largo de foice.", Damage=11, Reach=3.0, Angle=140, CastTime=0.45, MoveMult=0.45, Fx="claws")],
    "q1": [
        area("w_scythe_harvest", "Colheita", "Giro que fere e cura você.", Damage=13, Radius=3.2, SelfHeal=6, Cooldown=7, CastTime=0.35),
        proj("w_scythe_hook", "Gancho da Foice", "Puxa a vítima para a lâmina.", Damage=8, Knockback=-4, Cooldown=10, Speed=26, Range=7.5),
        melee("w_scythe_deep", "Corte Profundo", "Sangramento forte.", Damage=10, DotDps=6, DotDuration=4, Cooldown=7, Reach=3),
    ],
    "q2": [
        melee("w_scythe_low", "Ceifa Baixa", "Corta as pernas: muito lento.", Damage=9, SlowAmount=0.6, SlowDuration=2, Reach=3, Cooldown=8),
        area("w_scythe_spin", "Rodopio", "Empurra todos em volta.", Damage=8, Radius=3, Knockback=3, Cooldown=10, CastTime=0.2),
        melee("w_scythe_mark", "Marca da Morte", "O alvo sofre muito mais dano.", Damage=5, VulnAmount=0.3, VulnDuration=4, Cooldown=12, Reach=3),
    ],
    "q3": [
        dash("w_scythe_leap", "Salto", "Salto longo.", MaxDistance=6, DashDuration=0.25, Cooldown=8, Fx="pounce"),
        dash("w_scythe_shadow", "Sombra", "Atravessa como sombra, intocável.", MaxDistance=5, Invuln=0.35, Cooldown=10, Fx="mist_step"),
        buff("w_scythe_frenzy", "Frenesi", "Mais rápido e mais forte.", SelfHaste=0.25, SelfHasteDuration=3, DmgBuff=0.2, DmgBuffDuration=3, Cooldown=15),
    ],
    "ult": [area("w_scythe_ult", "Dança da Morte", "Ceifa tudo em volta, sangra e cura.", Damage=24, Radius=4, DotDps=6, DotDuration=4, SelfHeal=20, CastTime=0.5, Fx="blood_moon", **ULT)],
})

family("melee2H_vanguard", "Machado de Carrasco", "weapon", "melee", "#ff7a5a", {
    "primary": [melee("w_axe_basic", "Machadada", "Golpe pesado e lento.", Damage=14, Reach=2.6, Angle=100, CastTime=0.55, MoveMult=0.35, Fx="claws")],
    "q1": [
        area("w_axe_down", "Golpe Descendente", "Racha o chão à sua frente.", Damage=22, AtAim=True, Range=3.2, Radius=1.8, CastTime=0.7, Cooldown=6, Fx="pounce"),
        proj("w_axe_throw", "Arremesso de Machado", "Machado girando pelo ar.", Damage=16, Range=9, Speed=22, Radius=0.5, Cooldown=6, CastTime=0.45),
        melee("w_axe_split", "Rachar", "Golpe que atordoa.", Damage=12, StunDuration=0.8, Cooldown=9, CastTime=0.5),
    ],
    "q2": [
        buff("w_axe_fury", "Fúria", "Dano muito maior por um tempo.", DmgBuff=0.3, DmgBuffDuration=4, Cooldown=15, Fx="howl"),
        area("w_axe_quake", "Terremoto", "Treme o chão: todos ficam lentos.", Damage=8, Radius=3.5, SlowAmount=0.5, SlowDuration=2, Cooldown=10, Fx="howl"),
        dash("w_axe_shoulder", "Golpe de Ombro", "Avança e derruba quem estiver na chegada.", MaxDistance=4, LandingRadius=1.8, Damage=8, Knockback=3, Cooldown=9, Fx="rush"),
    ],
    "q3": [
        buff("w_axe_ironskin", "Pele de Ferro", "Metade do dano recebido.", DR=0.5, DRDuration=2.5, Cooldown=14, Fx="hide"),
        counter("w_axe_defy", "Desafio", "Espera o golpe e devolve atordoando.", CounterDuration=1.2, CounterStun=1.0, Cooldown=12),
        dash("w_axe_charge", "Investida", "Corrida curta.", MaxDistance=5, Cooldown=8, Fx="rush"),
    ],
    "ult": [melee("w_axe_ult", "Execução", "O golpe do carrasco.", Damage=46, Reach=3, Angle=120, CastTime=1.0, VulnAmount=0.2, VulnDuration=3, Fx="rend", **ULT)],
})

family("twoHand_arcane", "Cajado", "weapon", "magic", "#7ad8ff", {
    "primary": [proj("w_staff_basic", "Fogo-Fátuo", "Chama fantasma.", Damage=11, CastTime=0.45, MoveMult=0.45, Speed=22, Range=12, Radius=0.4)],
    "q1": [
        area("w_staff_pillar", "Coluna de Fogo", "Chamas sobem do chão no ponto mirado.", Damage=20, AtAim=True, Radius=2, CastTime=0.8, Cooldown=5, Range=11),
        proj("w_staff_wisps", "Enxame Fátuo", "Três chamas que queimam.", Damage=5, Count=3, Spread=20, DotDps=3, DotDuration=3, Cooldown=6),
        proj("w_staff_necro", "Raio Necrótico", "Atravessa todos no caminho.", Damage=15, Pierce=True, CastTime=0.55, Cooldown=6, Speed=28, Range=13),
    ],
    "q2": [
        zone("w_staff_salt", "Círculo de Sal", "Ninguém atravessa o sal com pressa.", AtAim=True, Radius=3, ZoneDuration=4, SlowAmount=0.5, SlowDuration=0.6, Cooldown=13),
        area("w_staff_burst", "Explosão Espectral", "Empurra tudo em volta.", Damage=10, Radius=3.2, Knockback=4, Cooldown=10, CastTime=0.2),
        proj("w_staff_chains", "Correntes Espectrais", "Prende o alvo.", Damage=6, RootDuration=1.5, Cooldown=12, Speed=20, Range=10),
    ],
    "q3": [
        dash("w_staff_teleport", "Teleporte", "Some num estalo.", MaxDistance=7, MinDistance=7, DashDuration=0.05, Cooldown=10, Fx="mist_step"),
        buff("w_staff_barrier", "Barreira Espectral", "Escudo grande e curto.", ShieldPct=0.3, ShieldDuration=2.5, Cooldown=14),
        buff("w_staff_cleanse", "Purificação", "Remove lentidão e atordoamento e acelera.", Cleanse=True, SelfHaste=0.3, SelfHasteDuration=1.5, Cooldown=14),
    ],
    "ult": [zone("w_staff_ult", "Sabá", "Círculo de bruxaria que consome os inimigos.", AtAim=True, Radius=4.5, ZoneDuration=5, Damage=7, SlowAmount=0.3, SlowDuration=0.6, CastTime=0.7, **ULT)],
})

family("twoHand_vanguard", "Mosquete", "weapon", "ranged", "#e0b060", {
    "primary": [proj("w_musket_basic", "Tiro de Mosquete", "Bala longa e pesada.", Damage=13, CastTime=0.55, MoveMult=0.35, Speed=40, Range=14, Radius=0.3)],
    "q1": [
        proj("w_musket_snipe", "Tiro de Precisão", "Atravessa e vai longe.", Damage=26, Pierce=True, CastTime=1.0, Cooldown=7, Speed=55, Range=18, MoveMult=0.0),
        melee("w_musket_bayonet", "Baioneta", "Estocada que empurra.", Damage=14, Knockback=2.5, Reach=2.8, Angle=60, Cooldown=6, DmgType="melee"),
        proj("w_musket_grape", "Metralha", "Quatro balas em leque.", Damage=8, Count=4, Spread=30, Cooldown=6, Speed=38, Range=10),
    ],
    "q2": [
        trap("w_musket_snare", "Laço de Arame", "Prende quem pisar.", Damage=6, RootDuration=1.8, Cooldown=12),
        trap("w_musket_mine", "Mina", "Explode quem pisar.", Damage=24, Radius=1.8, Cooldown=13),
        dash("w_musket_back", "Recuo", "Salto para reposicionar.", MaxDistance=5, Cooldown=9),
    ],
    "q3": [
        buff("w_musket_camo", "Camuflagem", "Corre para a próxima posição.", SelfHaste=0.45, SelfHasteDuration=2, Cooldown=12),
        buff("w_musket_trench", "Trincheira", "Abaixa e aguenta.", DR=0.45, DRDuration=3, Cooldown=13, Fx="hide"),
        zone("w_musket_smoke", "Granada de Fumaça", "Fumaça densa.", AtAim=True, Radius=3.2, ZoneDuration=4, SlowAmount=0.4, SlowDuration=0.6, Cooldown=12),
    ],
    "ult": [area("w_musket_ult", "Salva de Artilharia", "Bombardeio no ponto mirado.", AtAim=True, Radius=4, Damage=34, CastTime=1.2, Range=14, Fx="blood_moon", **ULT)],
})

family("unarmed", "Mãos nuas", "weapon", "melee", "#d0c0b0", {
    "primary": [melee("w_fist_basic", "Soco", "Sem arma, só os punhos.", Damage=5, Reach=1.9, Angle=80, CastTime=0.3, MoveMult=0.6)],
})

# ====================================================================== ARMOR (12 families, 3 options each)

family("head_vanguard", "Elmos", "head", "melee", "#c0c6ce", {"head": [
    area("h_van_taunt", "Brado", "Grito que desacelera quem está perto.", Radius=4, SlowAmount=0.3, SlowDuration=2, Cooldown=14, CastTime=0.1, Fx="howl"),
    buff("h_van_fort", "Fortitude", "Cura e reduz o dano.", SelfHeal=18, DR=0.2, DRDuration=3, Cooldown=16, Fx="hide"),
    counter("h_van_block", "Elmo Erguido", "Bloqueia o próximo golpe.", CounterDuration=0.9, CounterStun=0.6, Cooldown=15),
]})
family("head_scout", "Chapéus de Couro", "head", "ranged", "#e8c38a", {"head": [
    buff("h_sco_hunt", "Olhar de Caçador", "Mais dano por alguns segundos.", DmgBuff=0.2, DmgBuffDuration=4, Cooldown=16),
    zone("h_sco_smoke", "Fumaça", "Nuvem que atrasa os inimigos.", Radius=3, ZoneDuration=3.5, SlowAmount=0.4, SlowDuration=0.6, Cooldown=15),
    buff("h_sco_breath", "Fôlego", "Arrancada curta.", SelfHaste=0.4, SelfHasteDuration=2, Cooldown=12),
]})
family("head_arcane", "Cartolas", "head", "magic", "#b46cff", {"head": [
    buff("h_arc_purify", "Purificar", "Livra-se de lentidão e atordoamento.", Cleanse=True, Cooldown=15),
    area("h_arc_burst", "Explosão de Mana", "Empurra quem está perto.", Radius=3, Knockback=3.5, Damage=5, Cooldown=14, CastTime=0.1),
    buff("h_arc_veil", "Véu", "Intocável por um instante.", Invuln=1.0, Cooldown=18, Fx="mist_step"),
]})
family("head_sacred", "Chapéus de Inquisidor", "head", "magic", "#ffd27a", {"head": [
    buff("h_sac_bless", "Bênção", "Cura forte.", SelfHeal=28, Cooldown=16),
    buff("h_sac_halo", "Halo", "Escudo de luz.", ShieldPct=0.2, ShieldDuration=4, Cooldown=16),
    area("h_sac_silence", "Silêncio Sagrado", "Atordoa quem está muito perto.", Radius=2.5, StunDuration=0.6, Cooldown=18, CastTime=0.15),
]})

family("chest_vanguard", "Couraças", "chest", "melee", "#c0c6ce", {"chest": [
    dash("c_van_ram", "Investida Blindada", "Avança e empurra na chegada.", MaxDistance=5, LandingRadius=1.8, Knockback=3, Damage=6, Cooldown=12, Fx="pounce"),
    buff("c_van_wall", "Muralha", "Reduz muito o dano por pouco tempo.", DR=0.6, DRDuration=2, Cooldown=16, Fx="hide"),
    counter("c_van_retal", "Retaliação", "Contra-ataca o próximo golpe.", CounterDuration=1.0, CounterStun=0.9, Cooldown=14),
]})
family("chest_scout", "Sobretudos", "chest", "ranged", "#e8c38a", {"chest": [
    dash("c_sco_roll", "Rolamento", "Rola, intocável.", MaxDistance=5, MinDistance=5, Invuln=0.2, Cooldown=9),
    buff("c_sco_sprint", "Disparada", "Muito mais rápido por pouco tempo.", SelfHaste=0.6, SelfHasteDuration=1.5, Cooldown=12),
    dash("c_sco_vault", "Salto Mortal", "Salto longo.", MaxDistance=6.5, DashDuration=0.25, Cooldown=10, Fx="pounce"),
]})
family("chest_arcane", "Sobrecasacas Ocultistas", "chest", "magic", "#b46cff", {"chest": [
    dash("c_arc_blink", "Translocação", "Reaparece adiante.", MaxDistance=7, MinDistance=7, DashDuration=0.05, Cooldown=11, Fx="mist_step"),
    buff("c_arc_cloak", "Manto Espectral", "Intocável e mais rápido.", Invuln=0.9, SelfHaste=0.3, SelfHasteDuration=1.5, Cooldown=18, Fx="mist_step"),
    buff("c_arc_mana", "Escudo de Mana", "Escudo grande.", ShieldPct=0.3, ShieldDuration=3, Cooldown=16),
]})
family("chest_sacred", "Batinas", "chest", "magic", "#ffd27a", {"chest": [
    dash("c_sac_step", "Passo Divino", "Avança e se cura.", MaxDistance=5, SelfHeal=10, Cooldown=11),
    buff("c_sac_aegis", "Égide Sagrada", "Escudo e redução de dano.", ShieldPct=0.15, ShieldDuration=3, DR=0.25, DRDuration=3, Cooldown=17),
    counter("c_sac_martyr", "Martírio", "Contra-ataca e se cura muito.", CounterDuration=1.0, CounterStun=0.6, CounterHeal=25, Cooldown=16),
]})

family("boots_vanguard", "Botas de Montaria", "boots", "melee", "#c0c6ce", {"boots": [
    dash("b_van_stomp", "Pisotear", "Salta e deixa todos lentos na chegada.", MaxDistance=4, LandingRadius=2, SlowAmount=0.4, SlowDuration=2, Cooldown=10, Fx="pounce"),
    buff("b_van_steady", "Firmeza", "Reduz o dano e remove lentidão.", DR=0.3, DRDuration=2.5, Cleanse=True, Cooldown=15, Fx="hide"),
    buff("b_van_march", "Marcha", "Passo acelerado.", SelfHaste=0.3, SelfHasteDuration=3, Cooldown=12),
]})
family("boots_scout", "Botinas", "boots", "ranged", "#e8c38a", {"boots": [
    buff("b_sco_run", "Corrida", "Arrancada.", SelfHaste=0.5, SelfHasteDuration=2, Cooldown=11),
    dash("b_sco_jump", "Pulo", "Pulo longo.", MaxDistance=6, DashDuration=0.22, Cooldown=9, Fx="rush"),
    dash("b_sco_escape", "Escapada", "Escapa intocável.", MaxDistance=4, Invuln=0.3, Cooldown=11),
]})
family("boots_arcane", "Sapatos de Verniz", "boots", "magic", "#b46cff", {"boots": [
    buff("b_arc_float", "Levitar", "Flutua: mais rápido e protegido.", SelfHaste=0.4, SelfHasteDuration=2, DR=0.2, DRDuration=2, Cooldown=13),
    dash("b_arc_blink", "Piscar Curto", "Some e reaparece.", MaxDistance=4.5, MinDistance=4.5, DashDuration=0.05, Cooldown=9, Fx="mist_step"),
    dash("b_arc_ghost", "Passo Fantasma", "Atravessa intocável.", MaxDistance=5, Invuln=0.4, Cooldown=12, Fx="mist_step"),
]})
family("boots_sacred", "Botas de Peregrino", "boots", "magic", "#ffd27a", {"boots": [
    buff("b_sac_pilgrim", "Peregrino", "Mais rápido e se cura.", SelfHaste=0.3, SelfHasteDuration=2.5, SelfHeal=8, Cooldown=12),
    buff("b_sac_firm", "Passo Firme", "Reduz o dano.", DR=0.25, DRDuration=3, Cooldown=14, Fx="hide"),
    dash("b_sac_flee", "Fuga Santa", "Corrida abençoada.", MaxDistance=5, Cooldown=10),
]})

# ====================================================================== CLASSES (20 skills each)

classes = [
    dict(Id="vampire", Name="Vampiro", Desc="Nobre da noite: magia de sangue, roubo de vida e névoa.", Health=160.0, Speed=6.2, DmgType="magic", Color="#c8203c"),
    dict(Id="werewolf", Name="Lobisomem", Desc="Fera da lua: garras, saltos e regeneração.", Health=200.0, Speed=6.0, DmgType="melee", Color="#e08a2e"),
    dict(Id="hunter", Name="Caçador de Recompensas", Desc="Profissional da prata: armadilhas, marcas e tiros certeiros.", Health=170.0, Speed=6.1, DmgType="ranged", Color="#5f9a4a"),
]

ACTIVE_LEVELS = [1, 1, 4, 7, 10, 14, 18, 23, 29, 35]
PASSIVE_LEVELS = [2, 3, 5, 8, 12, 16, 20, 26, 32, 40]
GROW = 0.11   # actives: +11% damage/heal/shield/DoT per level (level 10 = +99%)


def klass(cid, dmg, color, actives, passives):
    assert len(actives) == 10 and len(passives) == 10, cid
    for s, lvl in zip(actives, ACTIVE_LEVELS):
        s.update(Source="class", Owner=cid, Key="active", ReqLevel=lvl, GrowPct=GROW)
        if not s["DmgType"]: s["DmgType"] = dmg
        if s["Color"] == "#ffffff": s["Color"] = color
    for s, lvl in zip(passives, PASSIVE_LEVELS):
        s.update(Source="class", Owner=cid, Key="passive", ReqLevel=lvl)
        if s["Color"] == "#ffffff": s["Color"] = color


# Passive numbers are PER LEVEL (level 10 = 10x).
klass("vampire", "magic", "#c8203c", [
    proj("c_vam_leech", "Sanguessuga", "Projétil que rouba muita vida.", Damage=10, Lifesteal=0.6, Cooldown=6, CastTime=0.35, Fx="blood_bolt"),
    dash("c_vam_mist", "Passo da Névoa", "Vira névoa por um instante.", MaxDistance=6, Invuln=0.15, Cooldown=9, Fx="mist_step"),
    proj("c_vam_gaze", "Olhar Hipnótico", "Atordoa quem cruzar o olhar.", Damage=6, StunDuration=1.0, Cooldown=12, Speed=20, Range=9, Fx="gaze"),
    area("c_vam_nova", "Nova Sanguínea", "Explosão de sangue em volta: fere, desacelera e cura.", Damage=14, Radius=3.5, SlowAmount=0.4, SlowDuration=1.5, SelfHeal=10, Cooldown=10, CastTime=0.25, Fx="nova"),
    counter("c_vam_mirror", "Espelho de Sangue", "Anula o próximo golpe, atordoa e cura.", CounterDuration=1.2, CounterStun=1.0, CounterHeal=12, Cooldown=14, Fx="mirror"),
    proj("c_vam_bats", "Revoada", "Cinco morcegos em leque.", Damage=6, Count=5, Spread=40, Cooldown=9, Speed=20, Range=9, Fx="blood_bolt"),
    proj("c_vam_lance", "Lança Carmesim", "Lança de sangue que atravessa.", Damage=22, Pierce=True, CastTime=0.7, Cooldown=8, Speed=32, Range=15, Fx="lance"),
    zone("c_vam_pool", "Poça de Sangue", "Sangue fervente no chão.", AtAim=True, Radius=3, ZoneDuration=4, Damage=5, SlowAmount=0.3, SlowDuration=0.6, Cooldown=14),
    buff("c_vam_mistform", "Forma de Névoa", "Intocável e veloz.", Invuln=1.2, SelfHaste=0.35, SelfHasteDuration=1.5, Cooldown=18, Fx="mist_step"),
    proj("c_vam_feast", "Banquete Carmesim", "Torrente de sangue que atravessa e cura muito.", Damage=30, Lifesteal=0.6, Pierce=True, Radius=0.7, CastTime=0.6, Cooldown=25, Speed=26, Range=16, Fx="feast"),
], [
    passive("p_vam_thirst", "Sede de Sangue", "Todo dano rouba vida.", P_Lifesteal=0.015),
    passive("p_vam_pale", "Pele Pálida", "Mais vida máxima.", P_Hp=0.02),
    passive("p_vam_night", "Noite Eterna", "Mais dano.", P_Damage=0.015),
    passive("p_vam_reflex", "Reflexos Sobrenaturais", "Mais velocidade.", P_Move=0.01),
    passive("p_vam_fangs", "Presas Afiadas", "O ataque básico faz sangrar.", P_BasicDotDps=0.6),
    passive("p_vam_immortal", "Imortal", "Cura ao matar.", P_KillHeal=0.03),
    passive("p_vam_charm", "Carisma Sombrio", "Recarga menor das habilidades.", P_Cdr=0.02),
    passive("p_vam_frenzy", "Frenesi Sanguíneo", "Mais dano com pouca vida (abaixo de 40%).", P_LowHpDamage=0.03),
    passive("p_vam_ancient", "Sangue Antigo", "Recebe mais cura.", P_Heal=0.02),
    passive("p_vam_lord", "Senhor da Noite", "Recebe menos dano.", P_DR=0.015),
])

klass("werewolf", "melee", "#e08a2e", [
    melee("c_wol_rend", "Garras Rasgantes", "Garras que deixam sangrando.", Damage=12, DotDps=4, DotDuration=3, Reach=2.5, Cooldown=5, Fx="claws"),
    dash("c_wol_pounce", "Bote", "Salta e fere ao cair.", MaxDistance=7, LandingRadius=2.2, Damage=14, SlowAmount=0.4, SlowDuration=1.5, DashDuration=0.35, CastTime=0.2, Cooldown=8, Fx="pounce"),
    area("c_wol_howl", "Uivo", "Uivo que deixa todos lentos.", Damage=5, Radius=4, SlowAmount=0.5, SlowDuration=2, Cooldown=12, Fx="howl"),
    buff("c_wol_hide", "Couro Grosso", "Reduz muito o dano.", DR=0.55, DRDuration=2.5, Cooldown=14, Fx="hide"),
    melee("c_wol_maul", "Dilacerar", "Golpe largo e brutal.", Damage=22, Angle=140, Reach=2.8, CastTime=0.45, Cooldown=7, Fx="rend"),
    dash("c_wol_rush", "Investida", "Corrida curta.", MaxDistance=5, Cooldown=7, Fx="rush"),
    melee("c_wol_bite", "Mordida", "Mordida que atordoa.", Damage=12, StunDuration=0.8, Reach=2.2, Angle=70, Cooldown=10, Fx="claws"),
    buff("c_wol_scent", "Faro de Sangue", "Mais veloz e mais forte.", SelfHaste=0.4, SelfHasteDuration=3, DmgBuff=0.2, DmgBuffDuration=4, Cooldown=16, Fx="howl"),
    melee("c_wol_toss", "Arremesso", "Agarra e arremessa longe.", Damage=10, Knockback=4.5, StunDuration=0.5, Reach=2.2, Angle=70, Cooldown=14, Fx="rend"),
    area("c_wol_moon", "Lua de Sangue", "Fúria lunar: fere tudo em volta, cura e acelera.", Damage=20, Radius=4.5, SelfHeal=30, SelfHaste=0.4, SelfHasteDuration=4, CastTime=0.5, Cooldown=30, Fx="blood_moon"),
], [
    passive("p_wol_predator", "Instinto Predador", "Mais dano.", P_Damage=0.015),
    passive("p_wol_fur", "Pelagem Espessa", "Mais vida máxima.", P_Hp=0.025),
    passive("p_wol_regen", "Regeneração Lupina", "Regenera vida com o tempo (% da vida por segundo).", P_Regen=0.001),
    passive("p_wol_paws", "Patas Velozes", "Mais velocidade.", P_Move=0.01),
    passive("p_wol_serrated", "Garras Serrilhadas", "O ataque básico faz sangrar.", P_BasicDotDps=0.7),
    passive("p_wol_beast", "Fúria Bestial", "Mais dano com pouca vida (abaixo de 40%).", P_LowHpDamage=0.035),
    passive("p_wol_pack", "Lei da Matilha", "Recarga menor das habilidades.", P_Cdr=0.02),
    passive("p_wol_hardened", "Couro Endurecido", "Recebe menos dano.", P_DR=0.015),
    passive("p_wol_hunt", "Caçada", "Mais dano em alvos lentos ou presos.", P_SlowedBonus=0.025),
    passive("p_wol_appetite", "Apetite Voraz", "Cura ao matar.", P_KillHeal=0.04),
])

klass("hunter", "ranged", "#5f9a4a", [
    proj("c_hun_silver", "Bala de Prata", "Bala benzida, pesada.", Damage=16, CastTime=0.5, Cooldown=5, Speed=42, Range=14),
    dash("c_hun_roll", "Rolamento", "Rola para longe.", MaxDistance=4.5, MinDistance=4.5, Cooldown=7),
    trap("c_hun_beartrap", "Armadilha de Urso", "Prende e fere quem pisar.", Damage=10, RootDuration=1.5, Cooldown=12),
    proj("c_hun_bola", "Boleadeira", "Enrola as pernas do alvo.", Damage=5, RootDuration=1.2, Cooldown=11, Speed=24, Range=10),
    zone("c_hun_holywater", "Água Benta", "Queima e atrasa as criaturas da noite.", AtAim=True, Radius=2.5, ZoneDuration=3.5, Damage=5, SlowAmount=0.3, SlowDuration=0.6, Cooldown=10, Range=9),
    proj("c_hun_net", "Rede", "Rede de pesca: muito lento.", Damage=3, SlowAmount=0.7, SlowDuration=2, Cooldown=10, Speed=22, Range=9, Radius=0.6),
    proj("c_hun_stake", "Estaca", "Estaca de freixo: abre a guarda.", Damage=22, VulnAmount=0.25, VulnDuration=4, CastTime=0.6, Cooldown=12, Speed=30, Range=12),
    zone("c_hun_lantern", "Lampião Quebrado", "Óleo em chamas no chão.", AtAim=True, Radius=2.5, ZoneDuration=4, Damage=7, Cooldown=14, Range=9),
    proj("c_hun_hook", "Arpão", "Puxa e atordoa a presa.", Damage=8, Knockback=-5, StunDuration=0.5, Cooldown=14, Speed=28, Range=11),
    proj("c_hun_bounty", "Cartaz de Procurado", "Marca a presa: sofre muito mais dano.", Damage=10, VulnAmount=0.4, VulnDuration=6, Cooldown=25, Speed=30, Range=14),
], [
    passive("p_hun_eye", "Olho de Caçador", "Mais dano.", P_Damage=0.015),
    passive("p_hun_leather", "Couro Curtido", "Mais vida máxima.", P_Hp=0.02),
    passive("p_hun_silver", "Prata Consagrada", "Mais dano em alvos lentos ou presos.", P_SlowedBonus=0.03),
    passive("p_hun_light", "Passos Leves", "Mais velocidade.", P_Move=0.012),
    passive("p_hun_barbed", "Munição Farpada", "O ataque básico faz sangrar.", P_BasicDotDps=0.6),
    passive("p_hun_veteran", "Veterano", "Recebe menos dano.", P_DR=0.015),
    passive("p_hun_prep", "Preparação", "Recarga menor das habilidades.", P_Cdr=0.025),
    passive("p_hun_contract", "Contrato Cumprido", "Cura ao matar.", P_KillHeal=0.03),
    passive("p_hun_cold", "Sangue Frio", "Mais energia ao causar dano.", P_Energy=0.03),
    passive("p_hun_lastbullet", "Última Bala", "Mais dano com pouca vida (abaixo de 40%).", P_LowHpDamage=0.03),
])

# ---------------------------------------------------------------- checks
ids = [s["Id"] for s in skills]
assert len(ids) == len(set(ids)), "duplicate skill id"
for c in classes:
    own = [s for s in skills if s["Owner"] == c["Id"]]
    assert len(own) == 20, (c["Id"], len(own))
for s in skills:
    assert s["Source"] and s["Owner"] and s["Key"], s["Id"]
    if s["Kind"] != "passive":
        assert s["DmgType"] in ("magic", "melee", "ranged"), s["Id"]

catalog = dict(Classes=classes, Families=families, Skills=skills)
with open(out_path, "w", encoding="utf-8") as f:
    json.dump(catalog, f, ensure_ascii=False, indent=1)
weapon = sum(1 for s in skills if s["Source"] == "weapon")
armor = sum(1 for s in skills if s["Source"] in ("head", "chest", "boots"))
print("classes:", len(classes), "class skills:", sum(1 for s in skills if s["Source"] == "class"),
      "weapon skills:", weapon, "armor skills:", armor, "families:", len(families))
