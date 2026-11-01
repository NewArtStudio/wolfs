using System.Collections.Generic;
using UnityEngine;

namespace VampWolfs
{
    /// <summary>
    /// Circle of effect, either around the caster (nova, howl) or at the aim point (ground target).
    /// Can also heal / haste the caster (ultimates).
    /// </summary>
    public class AreaAbility : Ability
    {
        public HitEffect Hit = new HitEffect();
        public float Radius = 3f;
        public bool CenteredOnCaster = true;

        public float SelfHeal;
        public float SelfHaste;
        public float SelfHasteDuration;

        public Color Color = new Color(0.8f, 0.1f, 0.2f);

        public override float AiRange { get { return CenteredOnCaster ? Radius * 0.85f : Range + Radius * 0.5f; } }

        Vector3 Center(CombatUnit caster, Vector3 aimPoint)
        {
            return CenteredOnCaster ? caster.Position : ClampToRange(caster.Position, aimPoint, Range);
        }

        public override TelegraphInfo GetTelegraph(CombatUnit caster, Vector3 aimPoint)
        {
            return TelegraphInfo.Circle(Center(caster, aimPoint), Radius);
        }

        public override void Execute(CombatUnit caster, Vector3 aimPoint)
        {
            Vector3 center = Center(caster, aimPoint);

            List<Health> targets = new List<Health>(CombatQuery.TargetsInRadius(caster, center, Radius));
            for (int i = 0; i < targets.Count; i++)
            {
                Hit.Apply(caster, targets[i]);
            }

            if (SelfHeal > 0f) caster.Health.HealScaled(SelfHeal);
            if (SelfHasteDuration > 0f) caster.Status.ApplyHaste(SelfHaste, SelfHasteDuration);

            if (!Vfx.SkillFx.Area(caster, this, center))
            {
                Effects.Burst(center, Radius, Color, 0.3f);
            }
        }
    }
}
