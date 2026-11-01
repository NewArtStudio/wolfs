using UnityEngine;

namespace VampWolfs
{
    /// <summary>The 7 Battlerite-style ability slots (order = HUD order).</summary>
    public enum AbilitySlot
    {
        Primary = 0,    // LMB  - basic attack, no cooldown, main energy source
        Secondary = 1,  // RMB  - stronger attack (has EX)
        Mobility = 2,   // Space - dash/leap (has EX)
        Q = 3,          // control (stun/slow)
        E = 4,          // defensive (counter / damage reduction)
        R = 5,          // energy skill (costs 1 energy bar)
        Ultimate = 6    // F    - costs ALL energy (100)
    }

    /// <summary>
    /// Base class for every ability. Concrete types (projectile, melee, area, dash, counter, buff)
    /// are generic and configured with numbers in the character kits, so new characters
    /// are mostly data, not new code.
    /// </summary>
    public abstract class Ability
    {
        public string Name = "Ability";
        public float Cooldown;
        public float CastTime = 0.3f;
        public float EnergyCost;

        [Tooltip("Movement speed multiplier while casting (0 = rooted, 1 = full speed).")]
        public float MoveMultiplierWhileCasting = 0.35f;

        public float Range = 10f;

        /// <summary>Optional EX version (Shift + key). Shares the cooldown with this ability.</summary>
        public Ability Ex;

        float readyAt;
        float lastCooldown = 1f;

        public bool IsReady
        {
            get { return Time.time >= readyAt; }
        }

        public float CooldownRemaining
        {
            get { return Mathf.Max(0f, readyAt - Time.time); }
        }

        /// <summary>1 right after use, 0 when ready (for HUD fill).</summary>
        public float CooldownFraction
        {
            get { return lastCooldown <= 0f ? 0f : CooldownRemaining / lastCooldown; }
        }

        public void StartCooldown(float duration)
        {
            lastCooldown = duration;
            readyAt = Time.time + duration;
        }

        public void ResetCooldown()
        {
            readyAt = 0f;
        }

        /// <summary>Body motion played by rigged models when this ability is cast and released.</summary>
        public virtual AttackMotion Motion
        {
            get { return AttackMotion.BothArms; }
        }

        /// <summary>Shape shown on the ground while casting.</summary>
        public virtual TelegraphInfo GetTelegraph(CombatUnit caster, Vector3 aimPoint)
        {
            return TelegraphInfo.None;
        }

        /// <summary>Called when the cast time ends.</summary>
        public abstract void Execute(CombatUnit caster, Vector3 aimPoint);

        // ---------------------------------------------------------------- helpers

        /// <summary>Direction on the ground plane from 'from' to 'to'.</summary>
        public static Vector3 FlatDirection(Vector3 from, Vector3 to, Vector3 fallback)
        {
            Vector3 direction = to - from;
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.0001f)
            {
                direction = fallback;
                direction.y = 0f;
            }

            return direction.normalized;
        }

        /// <summary>Aim point pulled back so it is at most maxDistance from the caster.</summary>
        public static Vector3 ClampToRange(Vector3 origin, Vector3 aimPoint, float maxDistance)
        {
            Vector3 offset = aimPoint - origin;
            offset.y = 0f;
            offset = Vector3.ClampMagnitude(offset, maxDistance);
            return new Vector3(origin.x + offset.x, 0f, origin.z + offset.z);
        }
    }
}
