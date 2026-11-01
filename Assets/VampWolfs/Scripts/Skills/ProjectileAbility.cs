using UnityEngine;

namespace VampWolfs
{
    /// <summary>Skillshot: fires a projectile toward the aim point.</summary>
    public class ProjectileAbility : Ability
    {
        public HitEffect Hit = new HitEffect();
        public float Speed = 24f;
        public float Radius = 0.35f;
        public bool Pierce;
        public Color Color = Color.red;
        public float VisualScale = 0.4f;

        [Tooltip("Projectiles per cast (fan). 1 = single shot.")]
        public int Count = 1;
        [Tooltip("Total fan angle in degrees when Count > 1.")]
        public float Spread;

        public override AttackMotion Motion
        {
            get { return AttackMotion.Throw; }
        }

        public override TelegraphInfo GetTelegraph(CombatUnit caster, Vector3 aimPoint)
        {
            Vector3 direction = FlatDirection(caster.Position, aimPoint, caster.transform.forward);
            return TelegraphInfo.Line(caster.Position, direction, Range, Radius * 2f);
        }

        public override void Execute(CombatUnit caster, Vector3 aimPoint)
        {
            Vector3 direction = FlatDirection(caster.Position, aimPoint, caster.transform.forward);
            Vector3 origin = caster.Position + direction * 0.6f;
            int n = Mathf.Max(1, Count);
            for (int i = 0; i < n; i++)
            {
                float angle = n == 1 ? 0f : -Spread * 0.5f + Spread * i / (n - 1);
                Vector3 dir = Quaternion.Euler(0f, angle, 0f) * direction;
                Projectile.Spawn(caster, origin, dir, this);
            }
        }
    }
}
