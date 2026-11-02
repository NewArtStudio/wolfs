using UnityEngine;

namespace VampWolfs
{
    /// <summary>The three peasant weapons.</summary>
    public enum PeasantWeapon
    {
        Dagger,     // adaga: fast, close, low damage
        Plow,       // arado: slow wide swing that pushes, telegraphed
        Shotgun     // espingarda: fan of pellets at short range, slow reload
    }

    /// <summary>
    /// Abilities and stats of the peasants. Peasants wear no gear, so their damage is multiplied by 0.5
    /// (DamageSystem: naked = half): the numbers below are BEFORE that. All values [PROTÓTIPO].
    /// </summary>
    public static class PeasantKit
    {
        public static float Health(PeasantWeapon w)
        {
            return w == PeasantWeapon.Plow ? 75f : w == PeasantWeapon.Shotgun ? 50f : 60f;
        }

        public static float Speed(PeasantWeapon w)
        {
            return w == PeasantWeapon.Plow ? 4.6f : 5.2f;
        }

        public static string Name(PeasantWeapon w)
        {
            return w == PeasantWeapon.Plow ? "Arado" : w == PeasantWeapon.Shotgun ? "Espingarda" : "Adaga";
        }

        /// <summary>A 10-slot bar with only the basic attack (Primary).</summary>
        public static Ability[] Create(PeasantWeapon w)
        {
            Ability[] bar = new Ability[10];
            bar[(int)AbilitySlot.Primary] = Basic(w);
            return bar;
        }

        static Ability Basic(PeasantWeapon w)
        {
            switch (w)
            {
                case PeasantWeapon.Plow:
                    return new MeleeAbility
                    {
                        Name = "Golpe de Arado",
                        Fx = "rend",
                        Cooldown = 2.4f,
                        CastTime = 0.75f,                 // long wind-up: you see it coming (telegraph)
                        MoveMultiplierWhileCasting = 0.2f,
                        Reach = 2.8f,
                        Angle = 140f,
                        Color = new Color(0.75f, 0.6f, 0.4f),
                        Hit = new HitEffect { Type = DamageType.PhysicalMelee, Damage = 26f, Knockback = 2.5f }
                    };
                case PeasantWeapon.Shotgun:
                    return new ProjectileAbility
                    {
                        Name = "Tiro de Espingarda",
                        Cooldown = 3.2f,
                        CastTime = 0.6f,
                        MoveMultiplierWhileCasting = 0f,
                        Range = 10f,
                        Speed = 28f,
                        Count = 5,
                        Spread = 32f,
                        Radius = 0.2f,
                        VisualScale = 0.18f,
                        Color = new Color(1f, 0.85f, 0.5f),
                        Hit = new HitEffect { Type = DamageType.PhysicalRanged, Damage = 8f }
                    };
                default:
                    return new MeleeAbility
                    {
                        Name = "Facada",
                        Fx = "claws",
                        Cooldown = 0.5f,
                        CastTime = 0.25f,
                        MoveMultiplierWhileCasting = 0.6f,
                        Reach = 1.9f,
                        Angle = 80f,
                        Color = new Color(0.85f, 0.85f, 0.9f),
                        Hit = new HitEffect { Type = DamageType.PhysicalMelee, Damage = 12f }
                    };
            }
        }
    }
}
