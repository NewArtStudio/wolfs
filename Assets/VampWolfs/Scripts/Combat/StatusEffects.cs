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

        // Skills system (nov/2026): root, bleeding/burning, vulnerability (marked), damage buff.
        float rootUntil;
        float vulnerableUntil;
        float vulnerableAmount;
        float damageBuffUntil;
        float damageBuffAmount;

        /// <summary>Damage over time (bleeding, burning). Ticks every DotTick seconds through DamageSystem.</summary>
        class Dot { public CombatUnit Source; public float Dps; public float Until; public DamageType Type; }
        readonly System.Collections.Generic.List<Dot> dots = new System.Collections.Generic.List<Dot>();
        const float DotTick = 0.5f;
        float nextDotTick;

        public bool IsStunned { get { return Time.time < stunUntil; } }
        public bool IsSlowed { get { return Time.time < slowUntil; } }
        public bool IsHasted { get { return Time.time < hasteUntil; } }
        public bool IsInvulnerable { get { return Time.time < invulnerableUntil; } }
        public bool IsCountering { get { return Time.time < counterUntil; } }
        public bool HasDamageReduction { get { return Time.time < reductionUntil; } }
        public bool IsRooted { get { return Time.time < rootUntil; } }
        public bool IsVulnerable { get { return Time.time < vulnerableUntil; } }
        public bool HasDamageBuff { get { return Time.time < damageBuffUntil; } }
        public bool IsBleeding { get { return dots.Count > 0; } }

        /// <summary>Extra damage this unit takes (Marca/Estaca/Cartaz de Procurado): 0.25 = +25%.</summary>
        public float VulnerableBonus { get { return IsVulnerable ? vulnerableAmount : 0f; } }

        /// <summary>Extra damage this unit deals (Grito de Guerra, Fúria…): 0.25 = +25%.</summary>
        public float DamageBuffBonus { get { return HasDamageBuff ? damageBuffAmount : 0f; } }

        public float MoveSpeedMultiplier
        {
            get
            {
                float multiplier = 1f;
                if (IsRooted) return 0f;
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

        /// <summary>Can't move (can still cast). Shortened by VIT like slows.</summary>
        public void ApplyRoot(float duration)
        {
            duration *= ResistMult();
            if (IsInvulnerable || duration <= 0f) return;
            rootUntil = Mathf.Max(rootUntil, Time.time + duration);
            FloatingText.Spawn(transform.position + Vector3.up * 2.8f, "Preso", new Color(0.7f, 0.9f, 0.6f));
        }

        public void ApplyVulnerable(float amount, float duration)
        {
            if (IsInvulnerable || duration <= 0f) return;
            vulnerableAmount = Mathf.Max(IsVulnerable ? vulnerableAmount : 0f, amount);
            vulnerableUntil = Mathf.Max(vulnerableUntil, Time.time + duration);
            FloatingText.Spawn(transform.position + Vector3.up * 3.1f, "Marcado", new Color(1f, 0.55f, 0.3f));
        }

        public void ApplyDamageBuff(float amount, float duration)
        {
            if (duration <= 0f) return;
            damageBuffAmount = Mathf.Max(HasDamageBuff ? damageBuffAmount : 0f, amount);
            damageBuffUntil = Mathf.Max(damageBuffUntil, Time.time + duration);
        }

        /// <summary>Bleeding / burning: dps for duration, credited to source (lifesteal and energy work).</summary>
        public void ApplyDot(CombatUnit source, float dps, float duration, DamageType type)
        {
            if (IsInvulnerable || dps <= 0f || duration <= 0f) return;
            // the same source refreshes its own DoT instead of stacking forever
            foreach (Dot d in dots)
            {
                if (d.Source == source)
                {
                    d.Dps = Mathf.Max(d.Dps, dps);
                    d.Until = Mathf.Max(d.Until, Time.time + duration);
                    return;
                }
            }
            dots.Add(new Dot { Source = source, Dps = dps, Until = Time.time + duration, Type = type });
            if (nextDotTick < Time.time) nextDotTick = Time.time + DotTick;
        }

        /// <summary>Removes slows, roots and stuns (Purificar, Firmeza…).</summary>
        public void Cleanse()
        {
            stunUntil = slowUntil = rootUntil = 0f;
        }

        void Update()
        {
            if (dots.Count == 0 || Time.time < nextDotTick) return;
            nextDotTick = Time.time + DotTick;
            Health health = GetComponent<Health>();
            for (int i = dots.Count - 1; i >= 0; i--)
            {
                Dot d = dots[i];
                if (Time.time >= d.Until || health == null || health.IsDead) { dots.RemoveAt(i); continue; }
                DamageSystem.Deal(d.Source != null && !d.Source.IsDead ? d.Source : null, health, d.Dps * DotTick, 0f, d.Type, true);
            }
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

            if (!Vfx.SkillFx.CounterTrigger(transform.position))
            {
                Effects.Burst(transform.position, 2.2f, new Color(1f, 0.8f, 0.2f), 0.3f);
            }
        }

        public void ClearAll()
        {
            stunUntil = slowUntil = hasteUntil = invulnerableUntil = reductionUntil = counterUntil = 0f;
            rootUntil = vulnerableUntil = damageBuffUntil = 0f;
            dots.Clear();
        }

        /// <summary>Short text for the HUD, e.g. "Lento · Contra-ataque".</summary>
        public string Describe()
        {
            string text = "";
            if (IsStunned) text = Append(text, "Atordoado");
            if (IsSlowed) text = Append(text, "Lento");
            if (IsRooted) text = Append(text, "Preso");
            if (IsBleeding) text = Append(text, "Sangrando");
            if (IsVulnerable) text = Append(text, "Marcado");
            if (HasDamageBuff) text = Append(text, "Fúria");
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
