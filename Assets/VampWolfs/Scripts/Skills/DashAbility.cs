using System.Collections.Generic;
using UnityEngine;

namespace VampWolfs
{
    /// <summary>
    /// Fast movement toward the aim point (dash, blink, leap). Optional invulnerability window
    /// and optional damage on landing (werewolf pounce).
    /// </summary>
    public class DashAbility : Ability
    {
        public float MaxDistance = 6f;
        public float MinDistance = 2f;
        public float Duration = 0.15f;

        [Tooltip("Seconds of invulnerability starting when the dash begins (0 = none).")]
        public float InvulnerableTime;

        [Tooltip("0 = no landing hit.")]
        public float LandingRadius;
        public HitEffect LandingHit = new HitEffect();

        public Color Color = new Color(0.6f, 0.5f, 0.9f);

        float Distance(CombatUnit caster, Vector3 aimPoint)
        {
            Vector3 offset = aimPoint - caster.Position;
            offset.y = 0f;
            return Mathf.Clamp(offset.magnitude, MinDistance, MaxDistance);
        }

        public override AttackMotion Motion
        {
            get { return AttackMotion.None; }
        }

        public override TelegraphInfo GetTelegraph(CombatUnit caster, Vector3 aimPoint)
        {
            Vector3 direction = FlatDirection(caster.Position, aimPoint, caster.transform.forward);
            Vector3 landing = caster.Position + direction * Distance(caster, aimPoint);
            return TelegraphInfo.Circle(landing, LandingRadius > 0f ? LandingRadius : 0.7f);
        }

        public override void Execute(CombatUnit caster, Vector3 aimPoint)
        {
            Vector3 direction = FlatDirection(caster.Position, aimPoint, caster.transform.forward);
            float distance = Distance(caster, aimPoint);
            float duration = Mathf.Max(0.05f, Duration);

            if (InvulnerableTime > 0f)
            {
                caster.Status.ApplyInvulnerable(InvulnerableTime);
            }

            Effects.Burst(caster.Position, 1f, Color, 0.25f);
            caster.StartForcedMove(direction * (distance / duration), duration, delegate { OnLand(caster); });
        }

        void OnLand(CombatUnit caster)
        {
            if (caster == null || caster.IsDead)
            {
                return;
            }

            if (LandingRadius <= 0f)
            {
                return;
            }

            List<Health> targets = new List<Health>(CombatQuery.TargetsInRadius(caster, caster.Position, LandingRadius));
            for (int i = 0; i < targets.Count; i++)
            {
                LandingHit.Apply(caster, targets[i]);
            }

            Effects.Burst(caster.Position, LandingRadius, Color, 0.3f);
        }
    }
}
