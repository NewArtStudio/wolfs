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

        /// <summary>Égide (Ward): absorb shield that takes damage before health and refills after a pause.</summary>
        public float Ward { get; private set; }
        public float MaxWard { get; private set; }

        /// <summary>amount taken (health + ward), attacker (may be null).</summary>
        public event Action<float, CombatUnit> Damaged;

        /// <summary>killer (may be null).</summary>
        public event Action<CombatUnit> Died;

        StatusEffects status;
        float lastDamageTime = -100f;

        public void Init(float maxHealth, Team team)
        {
            MaxHealth = maxHealth;
            Current = maxHealth;
            Team = team;
            IsDead = false;
            status = GetComponent<StatusEffects>();
        }

        /// <summary>Changes max health (gear) keeping the same health fraction, and sets the ward size.</summary>
        public void SetMaxHealth(float maxHealth, float maxWard)
        {
            float fraction = Normalized;
            MaxHealth = Mathf.Max(1f, maxHealth);
            Current = MaxHealth * fraction;
            MaxWard = Mathf.Max(0f, maxWard);
            Ward = MaxWard;
        }

        void Update()
        {
            if (IsDead || Ward >= MaxWard || Time.time - lastDamageTime < GearBalance.WardRegenDelay)
            {
                return;
            }

            Ward = Mathf.Min(MaxWard, Ward + MaxWard * GearBalance.WardRegenPctPerSecond * Time.deltaTime);
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

            lastDamageTime = Time.time;

            float absorbed = Mathf.Min(Ward, amount);
            Ward -= absorbed;
            float toHealth = Mathf.Min(amount - absorbed, Current);
            Current -= toHealth;
            dealt = absorbed + toHealth;

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
