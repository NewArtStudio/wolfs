using System.Collections.Generic;
using UnityEngine;

namespace VampWolfs
{
    /// <summary>
    /// Lingering area on the ground (Névoa Arcana, Água Benta, Poça de Sangue, Sabá…): every tick it hits
    /// enemies inside (damage per tick, slow) and can heal the caster while the caster stands inside.
    /// </summary>
    public class ZoneAbility : Ability
    {
        /// <summary>Applied to enemies inside every tick. Damage here is PER SECOND (each 0.5 s tick does half).</summary>
        public HitEffect Hit = new HitEffect();
        public float Radius = 3f;
        public float Duration = 4f;
        public bool AtAim = true;
        [Tooltip("Heals the caster per second while inside.")]
        public float HealPerSecond;
        public Color Color = new Color(0.6f, 0.4f, 0.9f);

        public const float Tick = 0.5f;

        /// <summary>The hit of one tick: damage per second × Tick, marked as a tick (no counters).</summary>
        public HitEffect TickHit()
        {
            return new HitEffect
            {
                Damage = Hit.Damage * Tick, Type = Hit.Type, Lifesteal = Hit.Lifesteal, SlowAmount = Hit.SlowAmount, SlowDuration = Hit.SlowDuration,
                StunDuration = Hit.StunDuration, RootDuration = Hit.RootDuration, DotDps = Hit.DotDps, DotDuration = Hit.DotDuration,
                VulnAmount = Hit.VulnAmount, VulnDuration = Hit.VulnDuration, IsTick = true,
            };
        }

        Vector3 Center(CombatUnit caster, Vector3 aimPoint)
        {
            return AtAim ? ClampToRange(caster.Position, aimPoint, Range) : new Vector3(caster.Position.x, 0f, caster.Position.z);
        }

        public override float AiRange { get { return AtAim ? Range + Radius * 0.5f : Radius * 0.8f; } }

        public override TelegraphInfo GetTelegraph(CombatUnit caster, Vector3 aimPoint)
        {
            return TelegraphInfo.Circle(Center(caster, aimPoint), Radius);
        }

        public override void Execute(CombatUnit caster, Vector3 aimPoint)
        {
            Zone.Spawn(caster, Center(caster, aimPoint), this);
        }
    }
}
