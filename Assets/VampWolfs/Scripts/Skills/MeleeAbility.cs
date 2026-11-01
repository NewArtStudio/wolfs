using System.Collections.Generic;
using UnityEngine;

namespace VampWolfs
{
    /// <summary>Melee swing: hits every target in an arc in front of the caster.</summary>
    public class MeleeAbility : Ability
    {
        public HitEffect Hit = new HitEffect();
        public float Reach = 2.4f;

        [Tooltip("Total width of the arc, in degrees.")]
        public float Angle = 110f;

        public Color Color = new Color(1f, 0.85f, 0.6f);

        public override AttackMotion Motion
        {
            get { return AttackMotion.Swing; }
        }

        public override TelegraphInfo GetTelegraph(CombatUnit caster, Vector3 aimPoint)
        {
            Vector3 direction = FlatDirection(caster.Position, aimPoint, caster.transform.forward);
            return TelegraphInfo.Circle(caster.Position + direction * (Reach * 0.45f), Reach * 0.6f);
        }

        public override void Execute(CombatUnit caster, Vector3 aimPoint)
        {
            Vector3 direction = FlatDirection(caster.Position, aimPoint, caster.transform.forward);
            Vector3 center = caster.Position;

            List<Health> targets = new List<Health>(CombatQuery.TargetsInRadius(caster, center, Reach));

            for (int i = 0; i < targets.Count; i++)
            {
                Vector3 toTarget = targets[i].transform.position - center;
                toTarget.y = 0f;

                bool insideArc = toTarget.magnitude < 0.5f || Vector3.Angle(direction, toTarget) <= Angle * 0.5f;
                if (insideArc)
                {
                    float before = targets[i].Current + targets[i].Ward;
                    Hit.Apply(caster, targets[i]);
                    if (targets[i].Current + targets[i].Ward < before)   // blocked (counter, invulnerable) = no blood
                        Vfx.SkillFx.MeleeHit(targets[i].transform.position + Vector3.up * 1.2f * CharacterFactory.SizeMultiplier, toTarget.normalized, Fx == "rend");
                }
            }

            if (!Vfx.SkillFx.Melee(caster, this, direction))
            {
                Effects.Burst(center + direction * (Reach * 0.5f), Reach * 0.55f, Color, 0.15f);
            }
        }
    }
}
