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
        public Color Color = new Color(0.6f, 0.7f, 0.9f);

        public override void Execute(CombatUnit caster, Vector3 aimPoint)
        {
            if (Heal > 0f) caster.Health.HealScaled(Heal);
            if (DamageReductionDuration > 0f) caster.Status.ApplyDamageReduction(DamageReduction, DamageReductionDuration);
            if (HasteDuration > 0f) caster.Status.ApplyHaste(Haste, HasteDuration);

            FloatingText.Spawn(caster.Position + Vector3.up * 2.8f, Name, Color);
            if (!Vfx.SkillFx.Buff(caster, this))
            {
                Effects.Burst(caster.Position, 1.6f, Color, 0.3f);
            }
        }
    }
}
