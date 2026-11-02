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
        [Tooltip("Half the side of the square farm, in meters. Calculated from the farm grid when the match starts.")]
        public float ArenaHalfSize = 28f;

        [Header("Farm map (procedural, see docs/MAPA.md)")]
        [Tooltip("Tiles per side (6 = 6×6 tiles of 18 m = 108 m).")]
        public int FarmGridSize = 6;

        [Tooltip("0 = a new map every match. Any other number always builds the same map (tests, bug reports: the HUD shows the seed).")]
        public int FarmSeed = 0;

        [Tooltip("Merge the static pieces into big meshes (fewer draw calls). Turn off if something looks wrong.")]
        public bool FarmMergeMeshes = true;

        [Header("Invasion")]
        [Tooltip("Seconds of farming before the werewolf invades.")]
        public float InvasionTime = 45f;

        [Tooltip("Seconds before the invasion when the warning banner appears.")]
        public float InvasionWarning = 8f;

        [Header("Sheep")]
        [Tooltip("Minimum number of sheep.")]
        public int SheepCount = 14;

        [Tooltip("Sheep per farm tile (6×6 = 36 tiles × 1 = 36 sheep). The real count is the larger of this and SheepCount.")]
        public float SheepPerTile = 1f;
        public float SheepRespawnTime = 10f;

        [Header("Loot")]
        [Range(0f, 1f)]
        [Tooltip("Share of the dead character's BAG that drops on the ground. Equipment never drops.")]
        public float BagDropShare = 0.6f;

        [Header("Extraction")]
        public float ExtractionChannelTime = 4f;
        public float ExtractionRadius = 2.2f;

        [Header("Camera & proportions (Battlerite-like)")]
        [Tooltip("Camera height above the player, in meters.")]
        public float CameraHeight = 13f;

        [Tooltip("How far behind the player the camera sits, in meters (height/back sets the angle, ~57° by default).")]
        public float CameraBack = 8.5f;

        [Tooltip("Vertical field of view in degrees. Lower = closer / bigger characters.")]
        public float CameraFieldOfView = 40f;

        [Tooltip("Multiplies character model size and collision radius.")]
        public float CharacterSize = 1.2f;

        [Tooltip("Pulls the camera back (multiplies height and distance, same angle). 1 = old framing. The mouse wheel changes it in game.")]
        public float CameraZoom = 1.35f;

        [Tooltip("Mouse wheel limits for CameraZoom.")]
        public float CameraZoomMin = 0.85f;
        public float CameraZoomMax = 1.9f;

        [Header("Werewolf AI")]
        [Range(0f, 1f)]
        [Tooltip("Chance the AI tries to dash away from a projectile about to hit it.")]
        public float AiDodgeChance = 0.45f;

        [Tooltip("Average seconds between AI decisions. Lower = harder.")]
        public float AiReactionTime = 0.25f;

        [Header("Peasants & chests (see docs/CAMPONESES.md)")]
        [Tooltip("Tier of everything that drops on this map (chests and peasants). The farm is tier I.")]
        public int MapLootTier = 1;

        [Tooltip("Chests per map (each guarded by a group of peasants).")]
        public int ChestCount = 6;

        [Range(0f, 1f)]
        [Tooltip("Chance a chest is RARE (golden: more items, better refine, takes longer to open).")]
        public float RareChestChance = 0.25f;

        [Tooltip("Seconds standing still to open a common / rare chest (damage restarts it).")]
        public float ChestChannelTime = 3f;
        public float RareChestChannelTime = 5f;

        [Tooltip("Guards around each chest (min and max).")]
        public int GuardsMin = 2;
        public int GuardsMax = 4;

        [Tooltip("Extra peasants walking alone around the farm.")]
        public int RoamingPeasants = 4;

        [Tooltip("Peasants attack enemies closer than this (meters).")]
        public float PeasantAggroRange = 9f;

        [Tooltip("Peasants give up the chase this far from home and walk back healing to full.")]
        public float PeasantLeashRange = 18f;

        [Range(0f, 1f)]
        [Tooltip("Chance a dead peasant drops one equipment piece (only of MapLootTier).")]
        public float PeasantDropChance = 0.25f;
    }
}
