using System;
using System.Collections.Generic;
using UnityEngine;

namespace VampWolfs
{
    /// <summary>
    /// What happens to a target when an ability connects. Shared by projectiles, melee swings,
    /// area blasts and dash landings so every ability type speaks the same language.
    /// </summary>
    [Serializable]
    public class HitEffect
    {
        public float Damage;

        [Tooltip("Which attribute scales this damage and which defense reduces it.")]
        public DamageType Type = DamageType.Magic;

        [Tooltip("Fraction of the damage healed back to the caster (vampire identity).")]
        public float Lifesteal;

        public float SlowAmount;
        public float SlowDuration;
        public float StunDuration;

        // Skills system (nov/2026)
        public float RootDuration;
        [Tooltip("Bleeding/burning damage per second.")]
        public float DotDps;
        public float DotDuration;
        [Tooltip("Meters pushed away from the caster (negative = pulled toward the caster).")]
        public float Knockback;
        [Tooltip("Marked: the target takes this much more damage (0.25 = +25%).")]
        public float VulnAmount;
        public float VulnDuration;
        [Tooltip("Basic attack (LMB): applies the class passive bleeding.")]
        public bool IsBasic;
        [Tooltip("Repeating tick (zones): never triggers counters, no 'Imune' spam.")]
        public bool IsTick;

        public void Apply(CombatUnit caster, Health target)
        {
            // Status-only hits (slow/root zones, marks with 0 damage) don't go through the damage path,
            // so they never trigger a counter; invulnerable targets ignore them.
            bool landed;
            if (Damage > 0f) landed = DamageSystem.Deal(caster, target, Damage, Lifesteal, Type, IsTick);
            else
            {
                StatusEffects s0 = target.GetComponent<StatusEffects>();
                landed = s0 == null || !s0.IsInvulnerable;
            }

            if (!landed || target.IsDead)
            {
                return;
            }

            StatusEffects status = target.GetComponent<StatusEffects>();
            if (status == null)
            {
                return;
            }

            if (SlowDuration > 0f) status.ApplySlow(SlowAmount, SlowDuration);
            if (StunDuration > 0f) status.ApplyStun(StunDuration);
            if (RootDuration > 0f) status.ApplyRoot(RootDuration);
            if (VulnDuration > 0f) status.ApplyVulnerable(VulnAmount, VulnDuration);
            if (DotDps > 0f && DotDuration > 0f) status.ApplyDot(caster, DotDps, DotDuration, Type);
            if (IsBasic && caster != null && caster.Passives.BasicDotDps > 0f) status.ApplyDot(caster, caster.Passives.BasicDotDps, 2f, Type);
            if (Knockback != 0f) Push(caster, target);
        }

        /// <summary>Knockback (positive) or pull (negative): a short forced move, never past the caster.</summary>
        void Push(CombatUnit caster, Health target)
        {
            CombatUnit unit = target.GetComponent<CombatUnit>();
            if (unit == null || caster == null || unit.IsDead) return;
            Vector3 away = unit.Position - caster.Position;
            away.y = 0f;
            float distance = away.magnitude;
            Vector3 dir = distance > 0.01f ? away / distance : caster.transform.forward;
            float meters = Knockback;
            if (meters < 0f) meters = -Mathf.Min(-meters, Mathf.Max(0f, distance - 1.4f));   // pull stops in front of the caster
            if (Mathf.Abs(meters) < 0.1f) return;
            if (unit.IsInForcedMove || unit.Status.IsInvulnerable) return;   // don't cancel a dash (its landing hit) mid-air
            const float time = 0.2f;
            unit.CancelCast(false);
            unit.StartForcedMove(dir * (meters / time), time, null);
        }
    }

    /// <summary>Single place where damage, energy gain and lifesteal are resolved.</summary>
    public static class DamageSystem
    {
        /// <summary>Energy gained per point of damage dealt to an enemy character.</summary>
        public const float EnergyPerDamage = 0.6f;

        /// <summary>Energy gained per point of damage dealt to neutral creatures (farming).</summary>
        public const float EnergyPerNeutralDamage = 0.2f;

        static readonly Color PlayerDamageColor = new Color(1f, 0.85f, 0.3f);
        static readonly Color EnemyDamageColor = new Color(1f, 0.3f, 0.25f);
        static readonly Color DotColor = new Color(0.85f, 0.25f, 0.3f);

        /// <summary>Returns true if the hit landed (not blocked by invulnerability or counter).</summary>
        public static bool Deal(CombatUnit caster, Health target, float amount, float lifesteal, DamageType type = DamageType.Magic, bool isDot = false)
        {
            if (target == null || target.IsDead)
            {
                return false;
            }

            float attackerMult;
            amount = ApplyGear(caster, target, amount, type, out attackerMult);

            float dealt;
            if (!target.TryTakeDamage(amount, caster, out dealt, isDot))
            {
                return false;
            }

            if (dealt <= 0f)
            {
                return true;
            }

            bool fromPlayer = caster != null && caster.IsPlayer;
            FloatingText.Spawn(target.transform.position + Vector3.up * (isDot ? 1.8f : 2.2f), Mathf.RoundToInt(dealt).ToString(), isDot ? DotColor : (fromPlayer ? PlayerDamageColor : EnemyDamageColor));

            UnitVisual visual = target.GetComponent<UnitVisual>();
            if (visual != null)
            {
                visual.Flash();
            }

            if (caster != null && !caster.IsDead)
            {
                float rate = target.Team == Team.Neutral ? EnergyPerNeutralDamage : EnergyPerDamage;
                // Energy from the damage BEFORE the gear multiplier, so EX/ultimate pacing is the same at every tier.
                caster.Energy.Add(dealt / Mathf.Max(0.01f, attackerMult) * rate * (1f + caster.Passives.Energy));

                lifesteal += caster.Passives.Lifesteal;
                if (lifesteal > 0f)
                {
                    caster.Health.Heal(dealt * lifesteal);
                }

                // Passive "heal on kill" (Imortal, Apetite Voraz…): only for characters, not sheep.
                if (target.IsDead && target.Team != Team.Neutral && caster.Passives.KillHeal > 0f)
                {
                    caster.Health.Heal(caster.Health.MaxHealth * caster.Passives.KillHeal);
                }
            }

            return true;
        }

        /// <summary>
        /// Attributes and gear: the caster's ATK/MATK (from STR/DEX/INT + weapon) multiplies the damage,
        /// the target's armour, evasion and flat defense reduce it. Creatures without gear are unaffected.
        /// </summary>
        static float ApplyGear(CombatUnit caster, Health target, float amount, DamageType type, out float attackerMult)
        {
            GearRulesData rules = GearCatalog.Instance.Rules;
            attackerMult = 1f;

            if (caster != null && caster.Equipment != null)
            {
                attackerMult = caster.Equipment.Stats.DamageMult(type, rules);
                amount *= attackerMult;
            }

            // Skills: passives and buffs of the attacker, marks and passives of the target.
            if (caster != null)
            {
                PassiveStats p = caster.Passives;
                float bonus = p.Damage + (caster.Status != null ? caster.Status.DamageBuffBonus : 0f);
                if (caster.Health != null && caster.Health.Normalized < 0.4f) bonus += p.LowHpDamage;
                StatusEffects ts = target.GetComponent<StatusEffects>();
                if (ts != null && (ts.IsSlowed || ts.IsRooted || ts.IsStunned)) bonus += p.SlowedBonus;
                amount *= 1f + bonus;
            }

            StatusEffects targetStatus = target.GetComponent<StatusEffects>();
            if (targetStatus != null) amount *= 1f + targetStatus.VulnerableBonus;
            CombatUnit targetUnit = target.GetComponent<CombatUnit>();
            if (targetUnit != null) amount *= 1f - targetUnit.Passives.DRCapped;

            Equipment targetGear = target.GetComponent<Equipment>();
            if (targetGear != null)
            {
                amount = targetGear.Stats.Mitigate(amount, type);
            }

            return amount;
        }
    }

    /// <summary>Physics queries used by abilities.</summary>
    public static class CombatQuery
    {
        static readonly List<Health> results = new List<Health>();

        /// <summary>
        /// Living, hittable targets in a radius (enemy characters and neutral creatures).
        /// The returned list is reused: copy it if you need to keep it.
        /// </summary>
        public static List<Health> TargetsInRadius(CombatUnit caster, Vector3 center, float radius)
        {
            results.Clear();
            Collider[] hits = Physics.OverlapSphere(center + Vector3.up, radius);

            for (int i = 0; i < hits.Length; i++)
            {
                Health health = hits[i].GetComponentInParent<Health>();
                if (health == null || health.IsDead || results.Contains(health))
                {
                    continue;
                }

                if (caster != null && (health == caster.Health || health.Team == caster.Team))
                {
                    continue;
                }

                results.Add(health);
            }

            return results;
        }
    }
}
