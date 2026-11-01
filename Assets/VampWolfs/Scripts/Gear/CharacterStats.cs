using System;
using System.Collections.Generic;

namespace VampWolfs
{
    /// <summary>Which attribute scales an ability's damage.</summary>
    public enum DamageType
    {
        Magic,          // INT (vampire blood magic, witch, alchemist)
        PhysicalMelee,  // STR (claws, blades)
        PhysicalRanged  // DEX (pistols, muskets, crossbows)
    }

    /// <summary>
    /// Balance numbers of the attribute system. Formulas come from the Space project (Ragnarok-style),
    /// adapted to skill-shot combat: no random hit/miss or dodge — DEX gives faster casts/attacks and
    /// Evasion gives speed + damage reduction instead. All [PROTÓTIPO].
    /// </summary>
    public static class GearBalance
    {
        /// <summary>Max of each attribute coming from equipment (gem bonuses go above it).</summary>
        public const float EquipCap = 99f;

        public const float ArmourCap = 75f;                    // % physical reduction
        public const float EvasionReductionPerPoint = 0.5f;    // each Evasion point = 0.5% physical reduction
        public const float EvasionMovePerPoint = 0.003f;       // each Evasion point = +0.3% move speed
        public const float MoveBonusCap = 0.4f;                // +40% max from gear
        public const float WardPctPerPoint = 0.001f;           // each Ward point = 0.1% of max HP as absorb shield
        public const float WardRegenPctPerSecond = 0.05f;      // ward refills 5%/s after WardRegenDelay
        public const float WardRegenDelay = 3f;

        public const float CastDexFull = 150f;                 // cast time × (1 − DEX/150)...
        public const float MinCastMult = 0.5f;                 // ...but never below 50% (telegraphs must stay readable)
        public const float AspdDexK = 300f;                    // basic attack time ÷ (1 + DEX/300)

        public const float StatusResistPerVit = 0.01f;         // −1% stun/slow duration per VIT
        public const float StatusResistCap = 0.5f;

        public const float DefScale = 0.08f;                   // flat defense: (VIT/2 physical, INT + VIT/2 magic) × DefScale
        public const float MinDamagePct = 0.1f;                // a hit never does less than 10%

        public const float UnarmedDmg = 8f;
    }

    /// <summary>
    /// Everything a character's gear and attributes turn into in combat. Computed by <see cref="Compute"/>
    /// from the 10 equipped slots; the Base screen shows the same numbers.
    /// </summary>
    public sealed class CharacterStats
    {
        /// <summary>Effective attributes (gear capped at 99 + gem bonuses).</summary>
        public Attributes Eff;
        public Attributes Gear;
        public Attributes Bonus;

        public float WeaponDmg = GearBalance.UnarmedDmg;
        public float AtkMelee;
        public float AtkRanged;
        public float Matk;

        public float Def;
        public float Mdef;

        /// <summary>% physical reduction from Armour + Evasion (capped).</summary>
        public float PhysReductionPct;
        public float Armour;
        public float Evasion;

        /// <summary>Absorb shield as a fraction of max HP.</summary>
        public float WardPct;

        public float MoveMult = 1f;
        public float CastMult = 1f;
        public float AspdMult = 1f;
        public float StatusResist;

        /// <summary>Average tier of the equipped pieces (0 = naked). Used for HP scaling and invasion brackets.</summary>
        public float PowerTier;

        /// <summary>Max HP multiplier: tier scaling × (1 + VIT%).</summary>
        public float HpMult = 1f;

        public int CardSockets;
        public int OpenCardSockets;

        /// <summary>Outgoing damage multiplier for one damage type (≈1 with a full tier I kit).</summary>
        public float DamageMult(DamageType type, GearRulesData rules)
        {
            float atk = type == DamageType.PhysicalMelee ? AtkMelee : type == DamageType.PhysicalRanged ? AtkRanged : Matk;
            float reference = rules.TierPower != null && rules.TierPower.Length > 0 ? rules.TierPower[0] : 56f;
            return 0.5f + 0.5f * (WeaponDmg + atk) / reference;
        }

        /// <summary>Damage a hit does after this character's defenses.</summary>
        public float Mitigate(float damage, DamageType type)
        {
            float reduced = damage;
            if (type == DamageType.Magic)
            {
                reduced -= Mdef * GearBalance.DefScale;
            }
            else
            {
                reduced *= 1f - PhysReductionPct / 100f;
                reduced -= Def * GearBalance.DefScale;
            }

            return Math.Max(damage * GearBalance.MinDamagePct, reduced);
        }

        // ---------------------------------------------------------------- formulas (from Space / Ragnarok)

        /// <summary>ATK from the main attribute: A + ⌊A/10⌋².</summary>
        public static float StatusAtk(float a)
        {
            a = (float)Math.Floor(Math.Max(0f, a));
            float tens = (float)Math.Floor(a / 10f);
            return a + tens * tens;
        }

        /// <summary>Average magic attack between Space's MATK min (I + ⌊I/7⌋²) and max (I + ⌊I/5⌋²).</summary>
        public static float MagicAtk(float i)
        {
            i = (float)Math.Floor(Math.Max(0f, i));
            float min = i + (float)Math.Pow(Math.Floor(i / 7f), 2);
            float max = i + (float)Math.Pow(Math.Floor(i / 5f), 2);
            return (min + max) / 2f;
        }

        /// <summary>Multiplier from refine level with decreasing returns (+1 = first entry of RefineBonus).</summary>
        public static float RefineMult(GearRulesData rules, int refine)
        {
            float mult = 1f;
            for (int i = 0; i < refine && rules.RefineBonus != null && i < rules.RefineBonus.Length; i++)
            {
                mult += rules.RefineBonus[i];
            }

            return mult;
        }

        /// <summary>Reference power at a (fractional) tier, interpolated; tier 0 = 0.</summary>
        public static float TierPowerAt(GearRulesData rules, float tier)
        {
            float[] p = rules.TierPower;
            if (p == null || p.Length == 0) return 1f;
            if (tier <= 1f) return p[0] * Math.Max(0f, tier);
            int lower = Math.Min(p.Length - 1, (int)Math.Floor(tier) - 1);
            int upper = Math.Min(p.Length - 1, lower + 1);
            float t = tier - (lower + 1);
            return p[lower] + (p[upper] - p[lower]) * t;
        }

        /// <summary>Stats of a character wearing these items (slot id → item; null/missing = empty).</summary>
        public static CharacterStats Compute(GearCatalog catalog, IDictionary<string, ItemInstance> equipped)
        {
            CharacterStats s = new CharacterStats();
            GearRulesData rules = catalog.Rules;
            bool hasWeapon = false;
            float weaponDmg = 0f;
            float tierSum = 0f;
            int pieces = 0;

            foreach (KeyValuePair<string, ItemInstance> pair in equipped)
            {
                ItemInstance item = pair.Value;
                EquipDef e = item != null ? catalog.GetEquip(item.DefId) : null;
                if (e == null) continue;

                float refineMult = e.Refinable ? RefineMult(rules, item.Refine) : 1f;

                if (e.Dmg > 0f)
                {
                    hasWeapon = true;
                    weaponDmg += e.Dmg * refineMult;
                }

                s.Armour += e.Armour * refineMult;
                s.Evasion += e.Evasion * refineMult;
                s.WardPct += e.Ward * refineMult * GearBalance.WardPctPerPoint;
                s.MoveMult += e.Move;

                s.Gear.Add(GearUpgrades.CurrentAttributes(e, item));
                s.Bonus.Add(GearUpgrades.GemBonus(catalog, item));

                s.CardSockets += e.CardSlots;
                s.OpenCardSockets += Math.Min(item.CardsOpen, e.CardSlots);

                tierSum += e.Tier;
                pieces++;
            }

            s.WeaponDmg = hasWeapon ? weaponDmg : GearBalance.UnarmedDmg;

            s.Eff = new Attributes
            {
                Str = (float)Math.Floor(Math.Min(GearBalance.EquipCap, s.Gear.Str) + s.Bonus.Str),
                Dex = (float)Math.Floor(Math.Min(GearBalance.EquipCap, s.Gear.Dex) + s.Bonus.Dex),
                Int = (float)Math.Floor(Math.Min(GearBalance.EquipCap, s.Gear.Int) + s.Bonus.Int),
                Vit = (float)Math.Floor(Math.Min(GearBalance.EquipCap, s.Gear.Vit) + s.Bonus.Vit),
            };

            float str = s.Eff.Str, dex = s.Eff.Dex, intel = s.Eff.Int, vit = s.Eff.Vit;

            s.AtkMelee = StatusAtk(str) + (float)Math.Floor(dex / 5f);
            s.AtkRanged = StatusAtk(dex) + (float)Math.Floor(str / 5f);
            s.Matk = MagicAtk(intel);
            s.Def = (float)Math.Floor(vit / 2f);
            s.Mdef = intel + (float)Math.Floor(vit / 2f);

            s.Armour = Math.Min(GearBalance.ArmourCap, s.Armour);
            s.PhysReductionPct = Math.Min(GearBalance.ArmourCap, s.Armour + s.Evasion * GearBalance.EvasionReductionPerPoint);
            s.MoveMult = 1f + Math.Min(GearBalance.MoveBonusCap, (s.MoveMult - 1f) + s.Evasion * GearBalance.EvasionMovePerPoint);

            s.CastMult = Math.Max(GearBalance.MinCastMult, 1f - dex / GearBalance.CastDexFull);
            s.AspdMult = 1f / (1f + dex / GearBalance.AspdDexK);
            s.StatusResist = Math.Min(GearBalance.StatusResistCap, vit * GearBalance.StatusResistPerVit);

            s.PowerTier = pieces > 0 ? tierSum / pieces : 0f;
            float reference = rules.TierPower != null && rules.TierPower.Length > 0 ? rules.TierPower[0] : 1f;
            float tierScale = 0.5f + 0.5f * TierPowerAt(rules, s.PowerTier) / reference;
            s.HpMult = tierScale * (1f + vit / 100f);

            return s;
        }
    }
}
