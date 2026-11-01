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
            Projectile.Spawn(caster, origin, direction, this);
        }
    }
}
