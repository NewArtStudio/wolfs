using UnityEngine;

namespace VampWolfs
{
    /// <summary>
    /// Buffs and debuffs on a character. Every effect is just an "active until" timestamp,
    /// so they are easy to read and debug.
    /// </summary>
    public class StatusEffects : MonoBehaviour
    {
        float stunUntil;
        float slowUntil;
        float slowAmount;
        float hasteUntil;
        float hasteAmount;
        float invulnerableUntil;
        float reductionUntil;
        float reductionAmount;
        float counterUntil;
        float counterStun;
        float counterHeal;

        public bool IsStunned { get { return Time.time < stunUntil; } }
        public bool IsSlowed { get { return Time.time < slowUntil; } }
        public bool IsHasted { get { return Time.time < hasteUntil; } }
        public bool IsInvulnerable { get { return Time.time < invulnerableUntil; } }
        public bool IsCountering { get { return Time.time < counterUntil; } }
        public bool HasDamageReduction { get { return Time.time < reductionUntil; } }

        public float MoveSpeedMultiplier
        {
            get
            {
                float multiplier = 1f;
                if (IsSlowed) multiplier *= 1f - slowAmount;
                if (IsHasted) multiplier *= 1f + hasteAmount;
                return multiplier;
            }
        }

        public float DamageTakenMultiplier
        {
            get { return HasDamageReduction ? 1f - reductionAmount : 1f; }
        }

        /// <summary>VIT status resistance from gear: shorter slows and stuns (counters included).</summary>
        float ResistMult()
        {
            Equipment gear = GetComponent<Equipment>();
            return gear != null ? 1f - gear.Stats.StatusResist : 1f;
        }

        public void ApplyStun(float duration)
        {
            duration *= ResistMult();
            if (IsInvulnerable || duration <= 0f) return;
            stunUntil = Mathf.Max(stunUntil, Time.time + duration);
            FloatingText.Spawn(transform.position + Vector3.up * 2.8f, "Atordoado", new Color(1f, 0.9f, 0.3f));
        }

        public void ApplySlow(float amount, float duration)
        {
            duration *= ResistMult();
            if (IsInvulnerable || duration <= 0f) return;
            if (!IsSlowed || amount >= slowAmount) slowAmount = Mathf.Clamp01(amount);
            slowUntil = Mathf.Max(slowUntil, Time.time + duration);
        }

        public void ApplyHaste(float amount, float duration)
        {
            if (duration <= 0f) return;
            hasteAmount = Mathf.Max(0f, amount);
            hasteUntil = Mathf.Max(hasteUntil, Time.time + duration);
        }

        public void ApplyInvulnerable(float duration)
        {
            invulnerableUntil = Mathf.Max(invulnerableUntil, Time.time + duration);
        }

        public void ApplyDamageReduction(float amount, float duration)
        {
            reductionAmount = Mathf.Clamp01(amount);
            reductionUntil = Mathf.Max(reductionUntil, Time.time + duration);
        }

        /// <summary>Battlerite-style counter: the next hit is negated and the attacker is punished.</summary>
        public void StartCounter(float duration, float stunAttacker, float healSelf)
        {
            counterUntil = Time.time + duration;
            counterStun = stunAttacker;
            counterHeal = healSelf;
        }

        public void TriggerCounter(CombatUnit attacker)
        {
            counterUntil = 0f;
            FloatingText.Spawn(transform.position + Vector3.up * 2.8f, "Contra-ataque!", new Color(1f, 0.8f, 0.2f), 1.2f, 1.3f);

            if (attacker != null && !attacker.IsDead)
            {
                attacker.Status.ApplyStun(counterStun);
            }

            Health health = GetComponent<Health>();
            if (health != null)
            {
                health.HealScaled(counterHeal);
            }

            Effects.Burst(transform.position, 2.2f, new Color(1f, 0.8f, 0.2f), 0.3f);
        }

        public void ClearAll()
        {
            stunUntil = slowUntil = hasteUntil = invulnerableUntil = reductionUntil = counterUntil = 0f;
        }

        /// <summary>Short text for the HUD, e.g. "Lento · Contra-ataque".</summary>
        public string Describe()
        {
            string text = "";
            if (IsStunned) text = Append(text, "Atordoado");
            if (IsSlowed) text = Append(text, "Lento");
            if (IsHasted) text = Append(text, "Veloz");
            if (IsInvulnerable) text = Append(text, "Imune");
            if (IsCountering) text = Append(text, "Contra-ataque");
            if (HasDamageReduction) text = Append(text, "Couraça");
            return text;
        }

        static string Append(string text, string item)
        {
            return text.Length == 0 ? item : text + " · " + item;
        }
    }
}
