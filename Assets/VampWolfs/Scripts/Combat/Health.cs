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

        /// <summary>Temporary shield from skills (Escudo de Éter, Halo…): absorbs before the Égide and health, then expires.</summary>
        public float Shield { get { return Time.time < shieldUntil ? shield : 0f; } }
        float shield, shieldUntil;

        /// <summary>Multiplies every heal received (class passives such as Sangue Antigo). Set by CombatUnit.</summary>
        public float HealReceivedMult = 1f;

        /// <summary>Gives a temporary shield (keeps the bigger one if one is already up).</summary>
        public void AddShield(float amount, float duration)
        {
            if (IsDead || amount <= 0f || duration <= 0f) return;
            shield = Mathf.Max(Shield, amount);
            shieldUntil = Mathf.Max(shieldUntil, Time.time + duration);
        }

        /// <summary>amount actually healed (used by the VFX).</summary>
        public event Action<float> Healed;

        StatusEffects status;
        float lastDamageTime = -100f;

        /// <summary>Max HP ÷ base max HP (gear). Fixed heals use it so they matter at every tier.</summary>
        public float HealScale { get; set; }

        public void Init(float maxHealth, Team team)
        {
            MaxHealth = maxHealth;
            Current = maxHealth;
            Team = team;
            IsDead = false;
            HealScale = 1f;
            status = GetComponent<StatusEffects>();
        }

        /// <summary>Heal a fixed amount designed for base health (scaled by gear HP).</summary>
        public void HealScaled(float amount)
        {
            Heal(amount * Mathf.Max(1f, HealScale));
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
        public bool TryTakeDamage(float amount, CombatUnit source, out float dealt, bool isTick = false)
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
                    if (!isTick) FloatingText.Spawn(transform.position + Vector3.up * 2.4f, "Imune", new Color(0.8f, 0.8f, 1f));
                    return false;
                }

                // bleeding/zone ticks don't pop the counter (it answers real hits only)
                if (status.IsCountering && !isTick)
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

            // order: skill shield → Égide (ward) → health
            float shielded = Mathf.Min(Shield, amount);
            shield -= shielded;
            amount -= shielded;
            float absorbed = Mathf.Min(Ward, amount);
            Ward -= absorbed;
            float toHealth = Mathf.Max(0f, Mathf.Min(amount - absorbed, Current));
            Current -= toHealth;
            dealt = shielded + absorbed + toHealth;

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

            amount *= Mathf.Max(0f, HealReceivedMult);
            float healed = Mathf.Min(amount, MaxHealth - Current);
            Current += healed;

            if (healed > 0f && Healed != null)
            {
                Healed(healed);
            }

            if (healed >= 1f)
            {
                FloatingText.Spawn(transform.position + Vector3.up * 2.4f, "+" + Mathf.RoundToInt(healed), new Color(0.45f, 1f, 0.45f));
            }
        }
    }
}
