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

        [Tooltip("Fraction of the damage healed back to the caster (vampire identity).")]
        public float Lifesteal;

        public float SlowAmount;
        public float SlowDuration;
        public float StunDuration;

        public void Apply(CombatUnit caster, Health target)
        {
            bool landed = DamageSystem.Deal(caster, target, Damage, Lifesteal);
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

        /// <summary>Returns true if the hit landed (not blocked by invulnerability or counter).</summary>
        public static bool Deal(CombatUnit caster, Health target, float amount, float lifesteal)
        {
            if (target == null || target.IsDead)
            {
                return false;
            }

            float dealt;
            if (!target.TryTakeDamage(amount, caster, out dealt))
            {
                return false;
            }

            if (dealt <= 0f)
            {
                return true;
            }

            bool fromPlayer = caster != null && caster.IsPlayer;
            FloatingText.Spawn(target.transform.position + Vector3.up * 2.2f, Mathf.RoundToInt(dealt).ToString(), fromPlayer ? PlayerDamageColor : EnemyDamageColor);

            UnitVisual visual = target.GetComponent<UnitVisual>();
            if (visual != null)
            {
                visual.Flash();
            }

            if (caster != null && !caster.IsDead)
            {
                float rate = target.Team == Team.Neutral ? EnergyPerNeutralDamage : EnergyPerDamage;
                caster.Energy.Add(dealt * rate);

                if (lifesteal > 0f)
                {
                    caster.Health.Heal(dealt * lifesteal);
                }
            }

            return true;
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
