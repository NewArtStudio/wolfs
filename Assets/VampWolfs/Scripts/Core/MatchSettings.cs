using System;
using UnityEngine;

namespace VampWolfs
{
    /// <summary>
    /// All tunable numbers for one prototype match.
    /// Edit them in the Inspector on the GameBootstrap object (no code changes needed).
    /// </summary>
    [Serializable]
    public class MatchSettings
    {
        [Header("Arena")]
        [Tooltip("Half the side of the square farm, in meters.")]
        public float ArenaHalfSize = 28f;

        [Header("Invasion")]
        [Tooltip("Seconds of farming before the werewolf invades.")]
        public float InvasionTime = 45f;

        [Tooltip("Seconds before the invasion when the warning banner appears.")]
        public float InvasionWarning = 8f;

        [Header("Sheep")]
        public int SheepCount = 14;
        public float SheepRespawnTime = 10f;

        [Header("Loot")]
        [Range(0f, 1f)]
        [Tooltip("Share of the dead character's BAG that drops on the ground. Equipment never drops.")]
        public float BagDropShare = 0.6f;

        [Header("Extraction")]
        public float ExtractionChannelTime = 4f;
        public float ExtractionRadius = 2.2f;

        [Header("Werewolf AI")]
        [Range(0f, 1f)]
        [Tooltip("Chance the AI tries to dash away from a projectile about to hit it.")]
        public float AiDodgeChance = 0.45f;

        [Tooltip("Average seconds between AI decisions. Lower = harder.")]
        public float AiReactionTime = 0.25f;
    }
}
