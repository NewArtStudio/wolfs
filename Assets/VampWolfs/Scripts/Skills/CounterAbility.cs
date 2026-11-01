using UnityEngine;

namespace VampWolfs
{
    /// <summary>
    /// Battlerite counter: for a short window the next incoming hit is negated,
    /// the attacker is stunned and the caster heals. Punishes predictable attacks.
    /// </summary>
    public class CounterAbility : Ability
    {
        public float Duration = 1.2f;
        public float StunAttacker = 1f;
        public float HealOnTrigger = 12f;

        public override float AiRange { get { return 0f; } }
        public override bool IsDefensive { get { return true; } }

        public override TelegraphInfo GetTelegraph(CombatUnit caster, Vector3 aimPoint)
        {
            return TelegraphInfo.Circle(caster.Position, 1.2f);
        }

        public override void Execute(CombatUnit caster, Vector3 aimPoint)
        {
            caster.Status.StartCounter(Duration, StunAttacker, HealOnTrigger);
            Vfx.SkillFx.CounterStart(caster);
            FloatingText.Spawn(caster.Position + Vector3.up * 2.8f, Name, new Color(1f, 0.85f, 0.4f));
        }
    }
}
