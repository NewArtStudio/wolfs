using System.Collections.Generic;
using UnityEngine;

namespace VampWolfs
{
    /// <summary>
    /// Builds the procedural farm in the scene from a FarmPlan:
    /// ground, the tiles' pieces (look + invisible colliders), little things on the grass,
    /// the forest around the map and the invisible walls. Also finds where the player and the portal start.
    /// See docs/MAPA.md.
    /// </summary>
    public static class FarmGenerator
    {
        public const float ForestDepth = 10f;      // forest band outside the playable area
        const int ScatterPerTile = 14;

        public static FarmLayout Build(Transform root, Transform farm, MatchSettings settings, int seed)
        {
            FarmTileCatalog catalog = FarmTileCatalog.Instance;
            FarmPlan plan = FarmPlan.Create(catalog, settings.FarmGridSize, seed);
            float h = plan.HalfSize;

            BuildGround(farm, h);

            Transform statics = new GameObject("Static").transform;     // never moves: merged for speed
            statics.SetParent(farm, false);
            Transform dynamics = new GameObject("Dynamic").transform;   // lights, windmill blades
            dynamics.SetParent(farm, false);
            Transform colliders = new GameObject("Colliders").transform;
            colliders.SetParent(farm, false);

            System.Random look = new System.Random(seed ^ 0x5bd1e995);
            foreach (PlacedPiece piece in plan.Pieces)
            {
                FarmPieces.Build(piece, statics, dynamics, look);
                FarmPieces.AddCollider(piece, colliders);
            }

            ScatterDecor(plan, statics, look);
            BuildForest(farm, h, look);
            BuildWalls(farm, h);

            if (settings.FarmMergeMeshes)
            {
                // fewer draw calls: a 6×6 map has thousands of primitive pieces
                StaticBatchingUtility.Combine(statics.gameObject);
            }

            Physics.SyncTransforms();

            WalkGrid grid = WalkGrid.FromPlan(plan);
            return MakeLayout(root, plan, grid, seed);
        }

        // ---------------------------------------------------------------- ground, scatter, forest, walls

        static void BuildGround(Transform farm, float h)
        {
            float size = h * 2f + ForestDepth * 2f + 30f;
            // Real floor collider (no Obstacle marker): keeps CharacterControllers grounded.
            GameObject floor = new GameObject("FloorCollider");
            floor.transform.SetParent(farm, false);
            floor.transform.localPosition = new Vector3(0f, -0.5f, 0f);
            floor.AddComponent<BoxCollider>().size = new Vector3(size, 1f, size);
            // darker earth under the forest (top at y 0); the field 2 cm above it; roads/pond/patches a bit higher
            // (a few cm between flat layers so they never flicker)
            Shapes.Create(PrimitiveType.Cube, farm, "ForestFloor", new Vector3(0f, -0.5f, 0f), new Vector3(size, 1f, size), new Color(0.07f, 0.12f, 0.07f));
            Shapes.Create(PrimitiveType.Cube, farm, "FieldFloor", new Vector3(0f, -0.48f, 0f), new Vector3(h * 2f + 1f, 1f, h * 2f + 1f), FarmPieces.Grass);
        }

        /// <summary>Tufts, pebbles and flowers where nothing else stands (not on roads, not in the pond).</summary>
        static void ScatterDecor(FarmPlan plan, Transform parent, System.Random rng)
        {
            float half = plan.TileSize * 0.5f - 0.5f;
            foreach (FarmPlacement cell in plan.Cells)
            {
                int placed = 0;
                for (int attempt = 0; attempt < ScatterPerTile * 4 && placed < ScatterPerTile; attempt++)
                {
                    float x = cell.CenterX + ((float)rng.NextDouble() * 2f - 1f) * half;
                    float z = cell.CenterZ + ((float)rng.NextDouble() * 2f - 1f) * half;
                    if (Covered(plan, x, z)) continue;
                    FarmPieces.Scatter(parent, x, z, rng);
                    placed++;
                }
            }
        }

        static bool Covered(FarmPlan plan, float x, float z)
        {
            foreach (PlacedPiece p in plan.Pieces)
            {
                if (p.Footprint != null && p.Footprint.Contains(x, z, 0.4f)) return true;
            }
            return false;
        }

        static void BuildForest(Transform farm, float h, System.Random rng)
        {
            Transform forest = new GameObject("Forest").transform;
            forest.SetParent(farm, false);

            // bushes along the edge hide the hard line between field and forest
            for (float t = -h; t <= h; t += 2.2f)
            {
                FarmPieces.Bush(forest, t + Jitter(rng, 0.6f), h + 0.9f + Jitter(rng, 0.4f), rng);
                FarmPieces.Bush(forest, t + Jitter(rng, 0.6f), -h - 0.9f + Jitter(rng, 0.4f), rng);
                FarmPieces.Bush(forest, h + 0.9f + Jitter(rng, 0.4f), t + Jitter(rng, 0.6f), rng);
                FarmPieces.Bush(forest, -h - 0.9f + Jitter(rng, 0.4f), t + Jitter(rng, 0.6f), rng);
            }

            const float spacing = 3.3f;
            float outer = h + ForestDepth;
            for (float z = -outer; z <= outer; z += spacing)
            {
                for (float x = -outer; x <= outer; x += spacing)
                {
                    float px = x + Jitter(rng, spacing * 0.4f), pz = z + Jitter(rng, spacing * 0.4f);
                    if (Mathf.Abs(px) < h + 2f && Mathf.Abs(pz) < h + 2f) continue;   // only outside the field
                    FarmPieces.ForestTree(forest, px, pz, rng);
                }
            }
        }

        static float Jitter(System.Random rng, float amount)
        {
            return ((float)rng.NextDouble() * 2f - 1f) * amount;
        }

        static void BuildWalls(Transform farm, float h)
        {
            Transform walls = new GameObject("Walls").transform;
            walls.SetParent(farm, false);
            Shapes.InvisibleWall(walls, new Vector3(0f, 1.5f, h + 0.5f), new Vector3(h * 2f + 2f, 3f, 1f));
            Shapes.InvisibleWall(walls, new Vector3(0f, 1.5f, -h - 0.5f), new Vector3(h * 2f + 2f, 3f, 1f));
            Shapes.InvisibleWall(walls, new Vector3(h + 0.5f, 1.5f, 0f), new Vector3(1f, 3f, h * 2f + 2f));
            Shapes.InvisibleWall(walls, new Vector3(-h - 0.5f, 1.5f, 0f), new Vector3(1f, 3f, h * 2f + 2f));
        }

        // ---------------------------------------------------------------- start positions

        static FarmLayout MakeLayout(Transform root, FarmPlan plan, WalkGrid grid, int seed)
        {
            System.Random rng = new System.Random(seed * 31 + 7);
            float h = plan.HalfSize;
            float side = h * 2f;

            FarmLayout layout = new FarmLayout();
            layout.Root = root;
            layout.HalfSize = h;
            layout.Plan = plan;
            layout.Grid = grid;
            layout.Seed = seed;

            float px, pz;
            if (!grid.RandomPoint(rng, 6f, 1.5f, out px, out pz)) { px = 0f; pz = -h + 6f; }
            layout.PlayerSpawn = new Vector3(px, 0f, pz);

            // portal: far enough to cross part of the map (40%–80% of the side), never inside anything
            float ex, ez;
            if (!grid.RandomPointAround(rng, px, pz, side * 0.4f, side * 0.8f, 5f, 2.6f, out ex, out ez)
                && !grid.RandomPoint(rng, 5f, 2.6f, out ex, out ez))
            {
                ex = -px; ez = -pz;
            }
            layout.ExtractionPosition = new Vector3(ex, 0f, ez);

            // fallback invader spawns spread over the map (MatchManager prefers a point at fighting distance)
            List<Vector3> spawns = new List<Vector3>();
            for (int i = 0; i < 8; i++)
            {
                float sx, sz;
                if (grid.RandomPoint(rng, 4f, 1.2f, out sx, out sz)) spawns.Add(new Vector3(sx, 0f, sz));
            }
            if (spawns.Count == 0) spawns.Add(new Vector3(-px, 0f, -pz));
            layout.InvaderSpawns = spawns.ToArray();
            return layout;
        }
    }
}
