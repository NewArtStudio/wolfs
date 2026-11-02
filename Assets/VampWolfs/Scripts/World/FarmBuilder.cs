using UnityEngine;

namespace VampWolfs
{
    /// <summary>Key positions of the generated map.</summary>
    public struct FarmLayout
    {
        public Transform Root;
        public float HalfSize;
        public Vector3 PlayerSpawn;
        public Vector3 ExtractionPosition;
        public Vector3[] InvaderSpawns;

        /// <summary>Where the peasants' chests go (and their guards), and where roaming peasants start.</summary>
        public Vector3[] ChestSpawns;
        public Vector3[] RoamerSpawns;

        /// <summary>The procedural map (tiles, roads, pieces) and where one can walk.</summary>
        public FarmPlan Plan;
        public WalkGrid Grid;
        /// <summary>Same seed = same map (shown on the HUD: report it with bugs).</summary>
        public int Seed;
    }

    /// <summary>
    /// Builds the night farm. Since nov/2026 the map is PROCEDURAL: a grid of tiles (FarmGenerator, farm_tiles.json)
    /// that is different every match. This class keeps the moonlight and the purple mist (fog), picks the seed
    /// and answers "give me a free spot" (sheep).
    /// Obstacles block projectiles = Battlerite-style cover.
    /// </summary>
    public static class FarmBuilder
    {
        static System.Random spotRng = new System.Random();

        public static FarmLayout Build(Transform root, MatchSettings settings)
        {
            Transform farm = new GameObject("Farm").transform;
            farm.SetParent(root, false);

            SetupLighting(root);

            int seed = settings.FarmSeed != 0 ? settings.FarmSeed : Random.Range(1, int.MaxValue);
            FarmTileCatalog catalog = FarmTileCatalog.Instance;
            // the arena size now comes from the grid (6 tiles × 18 m = 108 m): AI and sheep read ArenaHalfSize
            settings.ArenaHalfSize = Mathf.Max(1, settings.FarmGridSize) * catalog.TileSize * 0.5f;
            spotRng = new System.Random(seed + 1);

            if (0.65f * settings.CharacterSize > WalkGrid.CharacterRadius + 0.01f)
            {
                Debug.LogWarning("[VampWolfs] Character Size " + settings.CharacterSize + " deixa o lobisomem maior que o raio da navegação ("
                    + WalkGrid.CharacterRadius + " m): a IA pode travar em corredores. Ajuste CHAR_R no build_farm_tiles.py e WalkGrid.CharacterRadius.");
            }

            FarmLayout layout = FarmGenerator.Build(root, farm, settings, seed);
            Debug.Log("[VampWolfs] Fazenda gerada: seed " + seed + ", " + settings.FarmGridSize + "×" + settings.FarmGridSize + " tiles");
            return layout;
        }

        /// <summary>Random walkable point the player can reach (not inside an obstacle, not in a closed pocket).</summary>
        public static Vector3 RandomFreePoint(FarmLayout layout, float margin)
        {
            float x, z;
            if (layout.Grid != null && layout.Grid.RandomPoint(spotRng, margin, 0.5f, out x, out z))
            {
                return new Vector3(x, 0f, z);
            }

            float h = layout.HalfSize - margin;
            for (int attempt = 0; attempt < 30; attempt++)
            {
                Vector3 point = new Vector3(Random.Range(-h, h), 0f, Random.Range(-h, h));
                if (!OverlapsObstacle(point, 1.2f))
                {
                    return point;
                }
            }

            return Vector3.zero;
        }

        static bool OverlapsObstacle(Vector3 point, float radius)
        {
            Collider[] hits = Physics.OverlapSphere(point + Vector3.up, radius);
            for (int i = 0; i < hits.Length; i++)
            {
                if (hits[i].GetComponent<Obstacle>() != null)
                {
                    return true;
                }
            }

            return false;
        }

        // ---------------------------------------------------------------- lighting

        static void SetupLighting(Transform root)
        {
            GameObject moon = new GameObject("Moonlight");
            moon.transform.SetParent(root, false);
            moon.transform.rotation = Quaternion.Euler(50f, -35f, 0f);
            Light light = moon.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(0.6f, 0.65f, 0.95f);
            light.intensity = 1.1f;
            light.shadows = LightShadows.Soft;

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.22f, 0.2f, 0.32f);

            // The "Mists": purple fog at the edges of the view.
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = new Color(0.17f, 0.12f, 0.24f);
            RenderSettings.fogStartDistance = 26f;
            RenderSettings.fogEndDistance = 64f;
        }
    }
}
