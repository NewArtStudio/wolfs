using UnityEngine;

namespace VampWolfs
{
    /// <summary>
    /// Werewolf: melee bruiser. Closes the gap with leaps, wins up close, tanky.
    /// All numbers here are first-pass balance values — tune freely.
    /// </summary>
    public static class WerewolfKit
    {
        static readonly Color Claw = new Color(1f, 0.8f, 0.55f);
        static readonly Color Feral = new Color(1f, 0.55f, 0.15f);
        static readonly Color Howl = new Color(0.9f, 0.9f, 0.6f);
        static readonly Color Moon = new Color(0.85f, 0.15f, 0.1f);

        public static Ability[] Create()
        {
            MeleeAbility claws = new MeleeAbility
            {
                Name = "Garras",
                Fx = "claws",
                CastTime = 0.35f,
                MoveMultiplierWhileCasting = 0.5f,
                Reach = 2.4f,
                Angle = 110f,
                Color = Claw,
                Hit = new HitEffect { Type = DamageType.PhysicalMelee, Damage = 11f }
            };

            DashAbility pounce = new DashAbility
            {
                Name = "Bote",
                Fx = "pounce",
                Cooldown = 6f,
                CastTime = 0.2f,
                MoveMultiplierWhileCasting = 0f,
                MaxDistance = 7f,
                MinDistance = 3f,
                Duration = 0.35f,
                LandingRadius = 2.2f,
                Color = Feral,
                LandingHit = new HitEffect { Type = DamageType.PhysicalMelee, Damage = 14f, SlowAmount = 0.4f, SlowDuration = 1.5f },
                Ex = new DashAbility
                {
                    Name = "EX Bote Selvagem",
                    Fx = "pounce",
                    Cooldown = 6f,
                    EnergyCost = 25f,
                    CastTime = 0.2f,
                    MoveMultiplierWhileCasting = 0f,
                    MaxDistance = 8f,
                    MinDistance = 3f,
                    Duration = 0.35f,
                    LandingRadius = 2.4f,
                    Color = Feral,
                    LandingHit = new HitEffect { Type = DamageType.PhysicalMelee, Damage = 16f, StunDuration = 0.8f }
                }
            };

            DashAbility rush = new DashAbility
            {
                Name = "Investida",
                Fx = "rush",
                Cooldown = 7f,
                CastTime = 0f,
                MaxDistance = 5f,
                MinDistance = 3f,
                Duration = 0.15f,
                Color = Feral,
                Ex = new DashAbility
                {
                    Name = "EX Investida Fantasma",
                    Fx = "rush",
                    Cooldown = 7f,
                    EnergyCost = 25f,
                    CastTime = 0f,
                    MaxDistance = 6f,
                    MinDistance = 3f,
                    Duration = 0.18f,
                    InvulnerableTime = 0.5f,
                    Color = Feral
                }
            };

            AreaAbility howl = new AreaAbility
            {
                Name = "Uivo",
                Fx = "howl",
                Cooldown = 10f,
                CastTime = 0.3f,
                MoveMultiplierWhileCasting = 0.2f,
                Radius = 4f,
                CenteredOnCaster = true,
                Color = Howl,
                Hit = new HitEffect { Type = DamageType.PhysicalMelee, Damage = 5f, SlowAmount = 0.5f, SlowDuration = 2f }
            };

            BuffAbility thickHide = new BuffAbility
            {
                Name = "Couro Grosso",
                Fx = "hide",
                Cooldown = 12f,
                CastTime = 0f,
                DamageReduction = 0.6f,
                DamageReductionDuration = 2.5f,
                Color = new Color(0.6f, 0.7f, 0.9f)
            };

            MeleeAbility rend = new MeleeAbility
            {
                Name = "Dilacerar",
                Fx = "rend",
                Cooldown = 5f,
                EnergyCost = 25f,
                CastTime = 0.45f,
                MoveMultiplierWhileCasting = 0.2f,
                Reach = 2.8f,
                Angle = 140f,
                Color = Moon,
                Hit = new HitEffect { Type = DamageType.PhysicalMelee, Damage = 22f }
            };

            AreaAbility bloodMoon = new AreaAbility
            {
                Name = "Lua de Sangue",
                Fx = "blood_moon",
                EnergyCost = Energy.Max,
                Cooldown = 1f,
                CastTime = 0.5f,
                MoveMultiplierWhileCasting = 0f,
                Radius = 4.5f,
                CenteredOnCaster = true,
                SelfHeal = 30f,
                SelfHaste = 0.4f,
                SelfHasteDuration = 4f,
                Color = Moon,
                Hit = new HitEffect { Type = DamageType.PhysicalMelee, Damage = 20f }
            };

            return new Ability[] { claws, pounce, rush, howl, thickHide, rend, bloodMoon };
        }
    }
}
