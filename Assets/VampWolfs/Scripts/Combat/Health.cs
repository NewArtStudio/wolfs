using System;
using UnityEngine;

namespace VampWolfs
{
    /// <summary>Anything that can take damage: characters and sheep.</summary>
    public class Health : MonoBehaviour
    {
        public float MaxHealth = 100f;
        public Team Team = Team.Neutral;

        public float Current { get; private set; }
        public bool IsDead { get; private set; }

        public float Normalized
        {
            get { return MaxHealth > 0f ? Current / MaxHealth : 0f; }
        }

        /// <summary>amount actually lost, attacker (may be null).</summary>
        public event Action<float, CombatUnit> Damaged;

        /// <summary>killer (may be null).</summary>
        public event Action<CombatUnit> Died;

        StatusEffects status;

        public void Init(float maxHealth, Team team)
        {
            MaxHealth = maxHealth;
            Current = maxHealth;
            Team = team;
            IsDead = false;
            status = GetComponent<StatusEffects>();
        }

        /// <summary>
        /// Returns false when the hit was blocked (invulnerable or countered): in that case
        /// on-hit effects such as stuns must not be applied either.
        /// </summary>
        public bool TryTakeDamage(float amount, CombatUnit source, out float dealt)
        {
            dealt = 0f;

            if (IsDead)
            {
                return false;
            }

            if (status != null)
            {
                if (status.IsInvulnerable)
                {
                    FloatingText.Spawn(transform.position + Vector3.up * 2.4f, "Imune", new Color(0.8f, 0.8f, 1f));
                    return false;
                }

                if (status.IsCountering)
                {
                    status.TriggerCounter(source);
                    return false;
                }

                amount *= status.DamageTakenMultiplier;
            }

            if (amount <= 0f)
            {
                return true;
            }

            dealt = Mathf.Min(amount, Current);
            Current -= dealt;

            if (Damaged != null)
            {
                Damaged(dealt, source);
            }

            if (Current <= 0f)
            {
                Current = 0f;
                IsDead = true;
                if (Died != null)
                {
                    Died(source);
                }
            }

            return true;
        }

        public void Heal(float amount)
        {
            if (IsDead || amount <= 0f)
            {
                return;
            }

            float healed = Mathf.Min(amount, MaxHealth - Current);
            Current += healed;

            if (healed >= 1f)
            {
                FloatingText.Spawn(transform.position + Vector3.up * 2.4f, "+" + Mathf.RoundToInt(healed), new Color(0.45f, 1f, 0.45f));
            }
        }
    }
}
