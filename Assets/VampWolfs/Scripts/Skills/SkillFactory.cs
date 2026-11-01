using System.Collections.Generic;
using UnityEngine;

namespace VampWolfs
{
    /// <summary>
    /// Turns a SkillDef (data) into a ready-to-cast Ability, and assembles a character's whole bar:
    /// weapon (LMB, RMB, Q, E, F), helmet (R), chest (Space), boots (Shift) and 2 class actives (1, 2),
    /// plus the passives. The 6 generic ability types (and Zone/Trap) do the work; the data only sets numbers.
    /// </summary>
    public static class SkillFactory
    {
        public static DamageType ParseType(string t)
        {
            switch (t)
            {
                case "melee": return DamageType.PhysicalMelee;
                case "ranged": return DamageType.PhysicalRanged;
                default: return DamageType.Magic;
            }
        }

        public static Color ParseColor(string hex)
        {
            Color c;
            return !string.IsNullOrEmpty(hex) && ColorUtility.TryParseHtmlString(hex, out c) ? c : new Color(1f, 0.8f, 0.5f);
        }

        /// <summary>Builds the ability. level = class skill level (1..10; gear skills ignore it).</summary>
        public static Ability Build(SkillDef s, int level)
        {
            if (s == null || s.IsPassive) return null;
            float g = s.IsClassSkill ? SkillCatalog.Grow(s, level) : 1f;
            Color color = ParseColor(s.Color);
            HitEffect hit = new HitEffect
            {
                Damage = s.Damage * g,
                Type = ParseType(s.DmgType),
                Lifesteal = s.Lifesteal,
                SlowAmount = s.SlowAmount,
                SlowDuration = s.SlowDuration,
                StunDuration = s.StunDuration,
                RootDuration = s.RootDuration,
                DotDps = s.DotDps * g,
                DotDuration = s.DotDuration,
                Knockback = s.Knockback,
                VulnAmount = s.VulnAmount,
                VulnDuration = s.VulnDuration,
                IsBasic = s.Basic,
            };

            Ability a;
            switch (s.Kind)
            {
                case "projectile":
                    a = new ProjectileAbility
                    {
                        Hit = hit, Speed = s.Speed > 0f ? s.Speed : 24f, Radius = s.Radius > 0f ? s.Radius : 0.35f, Pierce = s.Pierce,
                        Color = color, VisualScale = Mathf.Clamp(s.Radius > 0f ? s.Radius * 1.2f : 0.4f, 0.3f, 1f),
                        Count = Mathf.Max(1, s.Count), Spread = s.Spread,
                    };
                    break;
                case "melee":
                    a = new MeleeAbility { Hit = hit, Reach = s.Reach > 0f ? s.Reach : 2.4f, Angle = s.Angle > 0f ? s.Angle : 110f, Color = color };
                    break;
                case "area":
                    a = new AreaAbility
                    {
                        Hit = hit, Radius = s.Radius > 0f ? s.Radius : 3f, CenteredOnCaster = !s.AtAim, Color = color,
                        SelfHeal = s.SelfHeal * g, SelfHaste = s.SelfHaste, SelfHasteDuration = s.SelfHasteDuration,
                    };
                    break;
                case "dash":
                    a = new DashAbility
                    {
                        MaxDistance = s.MaxDistance > 0f ? s.MaxDistance : 6f, MinDistance = s.MinDistance, Duration = s.DashDuration > 0f ? s.DashDuration : 0.15f,
                        InvulnerableTime = s.Invuln, LandingRadius = s.LandingRadius, LandingHit = hit, Color = color, SelfHeal = s.SelfHeal * g,
                    };
                    break;
                case "counter":
                    a = new CounterAbility { Duration = s.CounterDuration > 0f ? s.CounterDuration : 1f, StunAttacker = s.CounterStun, HealOnTrigger = s.CounterHeal * g };
                    break;
                case "buff":
                    a = new BuffAbility
                    {
                        Heal = s.SelfHeal * g, DamageReduction = s.DR, DamageReductionDuration = s.DRDuration, Haste = s.SelfHaste, HasteDuration = s.SelfHasteDuration,
                        ShieldPct = s.ShieldPct * g, ShieldDuration = s.ShieldDuration, DamageBuff = s.DmgBuff, DamageBuffDuration = s.DmgBuffDuration,
                        InvulnerableTime = s.Invuln, Cleanse = s.Cleanse, Color = color,
                    };
                    break;
                case "zone":
                    a = new ZoneAbility { Hit = hit, Radius = s.Radius > 0f ? s.Radius : 3f, Duration = s.ZoneDuration > 0f ? s.ZoneDuration : 4f, AtAim = s.AtAim, HealPerSecond = s.ZoneHealPs * g, Color = color };
                    break;
                case "trap":
                    a = new TrapAbility { Hit = hit, Radius = s.Radius > 0f ? s.Radius : 1.4f, Lifetime = s.TrapLife > 0f ? s.TrapLife : 12f, ArmTime = s.TrapArm, Color = color };
                    break;
                default:
                    Debug.LogWarning("[VampWolfs] Unknown skill kind '" + s.Kind + "' (" + s.Id + ")");
                    return null;
            }

            a.Name = s.Name;
            a.Fx = s.Fx ?? "";
            a.Cooldown = SkillCatalog.CooldownAt(s, level);
            a.CastTime = s.CastTime;
            a.MoveMultiplierWhileCasting = s.MoveMult;
            a.EnergyCost = s.EnergyCost;
            a.Range = s.Range > 0f ? s.Range : a.Range;
            return a;
        }

        /// <summary>Which bar slot each gear key uses.</summary>
        static AbilitySlot SlotOfKey(string key)
        {
            switch (key)
            {
                case "primary": return AbilitySlot.Primary;
                case "q1": return AbilitySlot.Secondary;
                case "q2": return AbilitySlot.Q;
                case "q3": return AbilitySlot.E;
                case "ult": return AbilitySlot.Ultimate;
                case "head": return AbilitySlot.R;
                case "chest": return AbilitySlot.Mobility;
                default: return AbilitySlot.Boots;
            }
        }

        /// <summary>The skill chosen for a key (the character's choice if valid, else the first option).</summary>
        public static SkillDef Chosen(SkillCatalog sc, CharacterSave ch, string family, string key)
        {
            List<SkillDef> options = sc.Options(family, key);
            if (options.Count == 0) return null;
            string id = ch != null ? ch.ChoiceFor(family, key) : null;
            foreach (SkillDef o in options) if (o.Id == id) return o;
            return options[0];
        }

        /// <summary>Weapon family worn by the character ("unarmed" without a weapon).</summary>
        public static string WeaponFamily(GearCatalog gear, IDictionary<string, ItemInstance> equipped)
        {
            ItemInstance w;
            EquipDef e = equipped != null && equipped.TryGetValue("handR", out w) && w != null ? gear.GetEquip(w.DefId) : null;
            string f = SkillCatalog.FamilyOf(gear, e);
            return f ?? "unarmed";
        }

        /// <summary>
        /// The 10-slot bar of a character with its worn equipment, and its passives.
        /// Index = (int)AbilitySlot. Empty keys stay null.
        /// </summary>
        public static Ability[] BuildBar(SkillCatalog sc, GearCatalog gear, CharacterSave ch, IDictionary<string, ItemInstance> equipped, out PassiveStats passives)
        {
            Ability[] bar = new Ability[10];
            passives = new PassiveStats();

            string weapon = WeaponFamily(gear, equipped);
            foreach (string key in SkillCatalog.WeaponKeys)
            {
                SkillDef s = Chosen(sc, ch, weapon, key);
                if (s != null) bar[(int)SlotOfKey(key)] = Build(s, 1);
            }

            foreach (string slot in SkillCatalog.ArmorSlots)
            {
                ItemInstance item;
                EquipDef e = equipped != null && equipped.TryGetValue(slot, out item) && item != null ? gear.GetEquip(item.DefId) : null;
                string family = SkillCatalog.FamilyOf(gear, e);
                if (family == null) continue;
                SkillDef s = Chosen(sc, ch, family, slot);
                if (s != null) bar[(int)SlotOfKey(slot)] = Build(s, 1);
            }

            if (ch != null)
            {
                for (int i = 0; i < SkillCatalog.ActiveSlots; i++)
                {
                    string id = ch.ActiveAt(i);
                    SkillDef s = sc.Get(id);
                    int level = ch.SkillLevel(id);
                    if (s != null && level > 0 && !s.IsPassive && s.Owner == ch.ClassId)
                        bar[(int)AbilitySlot.Class1 + i] = Build(s, level);
                }

                foreach (SkillDef s in sc.ClassSkills(ch.ClassId))
                {
                    if (s.IsPassive) passives.Add(s, ch.SkillLevel(s.Id));
                }
            }

            return bar;
        }
    }
}
