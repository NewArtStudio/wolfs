using UnityEngine;

namespace VampWolfs
{
    /// <summary>Self buff: heal, damage reduction and/or haste.</summary>
    public class BuffAbility : Ability
    {
        public float Heal;
        public float DamageReduction;
        public float DamageReductionDuration;
        public float Haste;
        public float HasteDuration;

        // Skills system (nov/2026)
        [Tooltip("Temporary shield as a fraction of max health (0.25 = 25%).")]
        public float ShieldPct;
        public float ShieldDuration;
        public float DamageBuff;
        public float DamageBuffDuration;
        public float InvulnerableTime;
        [Tooltip("Removes stun, slow and root.")]
        public bool Cleanse;

        public override float AiRange { get { return 0f; } }
        public override bool IsDefensive { get { return ShieldPct > 0f || DamageReductionDuration > 0f || InvulnerableTime > 0f || Heal > 0f; } }
        public Color Color = new Color(0.6f, 0.7f, 0.9f);

        public override void Execute(CombatUnit caster, Vector3 aimPoint)
        {
            if (Heal > 0f) caster.Health.HealScaled(Heal);
            if (DamageReductionDuration > 0f) caster.Status.ApplyDamageReduction(DamageReduction, DamageReductionDuration);
            if (HasteDuration > 0f) caster.Status.ApplyHaste(Haste, HasteDuration);
            if (Cleanse) caster.Status.Cleanse();
            if (ShieldPct > 0f) caster.Health.AddShield(caster.Health.MaxHealth * ShieldPct, ShieldDuration);
            if (DamageBuffDuration > 0f) caster.Status.ApplyDamageBuff(DamageBuff, DamageBuffDuration);
            if (InvulnerableTime > 0f) caster.Status.ApplyInvulnerable(InvulnerableTime);

            FloatingText.Spawn(caster.Position + Vector3.up * 2.8f, Name, Color);
            if (!Vfx.SkillFx.Buff(caster, this))
            {
                Effects.Burst(caster.Position, 1.6f, Color, 0.3f);
            }
        }
    }
}
