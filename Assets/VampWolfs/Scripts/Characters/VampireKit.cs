using UnityEngine;

namespace VampWolfs
{
    /// <summary>
    /// Vampire: ranged blood mage. Kites, pokes with skillshots, sustains through lifesteal.
    /// All numbers here are first-pass balance values — tune freely.
    /// </summary>
    public static class VampireKit
    {
        static readonly Color Blood = new Color(0.9f, 0.05f, 0.15f);
        static readonly Color DarkBlood = new Color(0.55f, 0.0f, 0.1f);
        static readonly Color Mist = new Color(0.6f, 0.5f, 0.95f);
        static readonly Color Gaze = new Color(0.95f, 0.3f, 0.9f);

        public static Ability[] Create()
        {
            ProjectileAbility bloodBolt = new ProjectileAbility
            {
                Name = "Seta de Sangue",
                Fx = "blood_bolt",
                CastTime = 0.3f,
                MoveMultiplierWhileCasting = 0.55f,
                Range = 11f,
                Speed = 24f,
                Radius = 0.35f,
                Color = Blood,
                VisualScale = 0.4f,
                Hit = new HitEffect { Damage = 9f, Lifesteal = 0.2f }
            };

            ProjectileAbility crimsonLance = new ProjectileAbility
            {
                Name = "Lança Carmesim",
                Fx = "lance",
                Cooldown = 4f,
                CastTime = 0.75f,
                MoveMultiplierWhileCasting = 0.2f,
                Range = 15f,
                Speed = 32f,
                Radius = 0.4f,
                Color = DarkBlood,
                VisualScale = 0.55f,
                Hit = new HitEffect { Damage = 18f, Lifesteal = 0.2f },
                Ex = new ProjectileAbility
                {
                    Name = "EX Lança Perfurante",
                    Fx = "lance_ex",
                    Cooldown = 4f,
                    EnergyCost = 25f,
                    CastTime = 0.75f,
                    MoveMultiplierWhileCasting = 0.2f,
                    Range = 16f,
                    Speed = 34f,
                    Radius = 0.45f,
                    Pierce = true,
                    Color = Gaze,
                    VisualScale = 0.6f,
                    Hit = new HitEffect { Damage = 22f, Lifesteal = 0.3f, SlowAmount = 0.5f, SlowDuration = 1.5f }
                }
            };

            DashAbility mistStep = new DashAbility
            {
                Name = "Passo da Névoa",
                Fx = "mist_step",
                Cooldown = 8f,
                CastTime = 0f,
                MaxDistance = 6f,
                MinDistance = 3f,
                Duration = 0.15f,
                InvulnerableTime = 0.15f,
                Color = Mist,
                Ex = new DashAbility
                {
                    Name = "EX Revoada de Morcegos",
                    Fx = "bats",
                    Cooldown = 8f,
                    EnergyCost = 25f,
                    CastTime = 0f,
                    MaxDistance = 8f,
                    MinDistance = 4f,
                    Duration = 0.22f,
                    InvulnerableTime = 0.8f,
                    Color = Mist
                }
            };

            ProjectileAbility hypnoticGaze = new ProjectileAbility
            {
                Name = "Olhar Hipnótico",
                Fx = "gaze",
                Cooldown = 10f,
                CastTime = 0.4f,
                MoveMultiplierWhileCasting = 0.3f,
                Range = 9f,
                Speed = 20f,
                Radius = 0.4f,
                Color = Gaze,
                VisualScale = 0.45f,
                Hit = new HitEffect { Damage = 6f, StunDuration = 1f }
            };

            CounterAbility bloodMirror = new CounterAbility
            {
                Name = "Espelho de Sangue",
                Fx = "mirror",
                Cooldown = 12f,
                CastTime = 0f,
                Duration = 1.2f,
                StunAttacker = 1f,
                HealOnTrigger = 12f
            };

            AreaAbility bloodNova = new AreaAbility
            {
                Name = "Nova Sanguínea",
                Fx = "nova",
                Cooldown = 6f,
                EnergyCost = 25f,
                CastTime = 0.25f,
                MoveMultiplierWhileCasting = 0.3f,
                Radius = 3.5f,
                CenteredOnCaster = true,
                SelfHeal = 10f,
                Color = Blood,
                Hit = new HitEffect { Damage = 14f, SlowAmount = 0.4f, SlowDuration = 1.5f }
            };

            ProjectileAbility crimsonFeast = new ProjectileAbility
            {
                Name = "Banquete Carmesim",
                Fx = "feast",
                EnergyCost = Energy.Max,
                Cooldown = 1f,
                CastTime = 0.6f,
                MoveMultiplierWhileCasting = 0f,
                Range = 16f,
                Speed = 26f,
                Radius = 0.75f,
                Pierce = true,
                Color = Blood,
                VisualScale = 1f,
                Hit = new HitEffect { Damage = 32f, Lifesteal = 0.6f }
            };

            return new Ability[] { bloodBolt, crimsonLance, mistStep, hypnoticGaze, bloodMirror, bloodNova, crimsonFeast };
        }
    }
}
