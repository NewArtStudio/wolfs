using System.Collections.Generic;
using UnityEngine;

namespace VampWolfs
{
    /// <summary>
    /// Trap placed at the aim point (Armadilha de Urso, Mina, Laço de Arame…): arms after a short delay and
    /// snaps on the first enemy that steps inside (hits everyone in the radius), or disappears after Lifetime.
    /// </summary>
    public class TrapAbility : Ability
    {
        public HitEffect Hit = new HitEffect();
        public float Radius = 1.4f;
        public float Lifetime = 12f;
        public float ArmTime = 0.6f;
        public Color Color = new Color(0.8f, 0.7f, 0.5f);

        public override float AiRange { get { return Range; } }

        public override TelegraphInfo GetTelegraph(CombatUnit caster, Vector3 aimPoint)
        {
            return TelegraphInfo.Circle(ClampToRange(caster.Position, aimPoint, Range), Radius);
        }

        public override void Execute(CombatUnit caster, Vector3 aimPoint)
        {
            Trap.Spawn(caster, ClampToRange(caster.Position, aimPoint, Range), this);
        }
    }
}
