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
    }

    /// <summary>
    /// Builds the night farm out of primitives: fenced field, barn, sheep pen, hay, trees, pond,
    /// lanterns, moonlight and purple mist (fog). Obstacles block projectiles = Battlerite-style cover.
    /// </summary>
    public static class FarmBuilder
    {
        static readonly Color Grass = new Color(0.16f, 0.27f, 0.13f);
        static readonly Color Dirt = new Color(0.32f, 0.24f, 0.16f);
        static readonly Color Wood = new Color(0.36f, 0.25f, 0.16f);
        static readonly Color BarnRed = new Color(0.45f, 0.08f, 0.07f);
        static readonly Color Roof = new Color(0.22f, 0.12f, 0.1f);
        static readonly Color Hay = new Color(0.78f, 0.64f, 0.3f);
        static readonly Color Leaves = new Color(0.1f, 0.2f, 0.1f);
        static readonly Color Water = new Color(0.12f, 0.2f, 0.35f);
        static readonly Color LanternLight = new Color(1f, 0.6f, 0.25f);

        public static FarmLayout Build(Transform root, MatchSettings settings)
        {
            float h = settings.ArenaHalfSize;

            Transform farm = new GameObject("Farm").transform;
            farm.SetParent(root, false);

            SetupLighting(root);

            // Ground and paths (decor only).
            Shapes.Create(PrimitiveType.Cube, farm, "Ground", new Vector3(0f, -0.5f, 0f), new Vector3(h * 2f + 30f, 1f, h * 2f + 30f), Grass);
            // Real floor collider (no Obstacle marker): keeps CharacterControllers grounded.
            GameObject floor = new GameObject("FloorCollider");
            floor.transform.SetParent(farm, false);
            floor.transform.localPosition = new Vector3(0f, -0.5f, 0f);
            floor.AddComponent<BoxCollider>().size = new Vector3(h * 2f + 30f, 1f, h * 2f + 30f);

            Shapes.Create(PrimitiveType.Cube, farm, "PathNS", new Vector3(0f, 0.01f, 0f), new Vector3(4f, 0.02f, h * 2f), Dirt);
            Shapes.Create(PrimitiveType.Cube, farm, "PathEW", new Vector3(0f, 0.012f, -2f), new Vector3(h * 2f, 0.02f, 3.5f), Dirt);

            BuildPerimeter(farm, h);
            BuildBarn(farm, new Vector3(-15f, 0f, 13f));
            BuildPen(farm, new Vector3(9f, 0f, -8f), 10f, 7f);

            BuildHaystack(farm, new Vector3(-6f, 0f, 4f));
            BuildHaystack(farm, new Vector3(-4.5f, 0f, 5.2f));
            BuildHaystack(farm, new Vector3(6f, 0f, 9f));
            BuildHaystack(farm, new Vector3(-12f, 0f, -10f));
            BuildHaystack(farm, new Vector3(16f, 0f, 4f));
            BuildHaystack(farm, new Vector3(2f, 0f, 18f));

            BuildTree(farm, new Vector3(-22f, 0f, -20f));
            BuildTree(farm, new Vector3(-24f, 0f, 2f));
            BuildTree(farm, new Vector3(22f, 0f, -18f));
            BuildTree(farm, new Vector3(18f, 0f, 14f));
            BuildTree(farm, new Vector3(-6f, 0f, 22f));
            BuildTree(farm, new Vector3(12f, 0f, 22f));

            Shapes.Create(PrimitiveType.Cylinder, farm, "Pond", new Vector3(-14f, 0.02f, -18f), new Vector3(7f, 0.01f, 4.5f), Water);

            BuildLantern(farm, new Vector3(-10f, 0f, 9f));
            BuildLantern(farm, new Vector3(3f, 0f, -3f));
            BuildLantern(farm, new Vector3(h - 7f, 0f, h - 7f));

            Physics.SyncTransforms();

            FarmLayout layout = new FarmLayout();
            layout.Root = root;
            layout.HalfSize = h;
            layout.PlayerSpawn = new Vector3(0f, 0f, -h + 6f);
            layout.ExtractionPosition = new Vector3(h - 5f, 0f, h - 5f);
            layout.InvaderSpawns = new[]
            {
                new Vector3(-h + 4f, 0f, h - 4f),
                new Vector3(h - 4f, 0f, -h + 4f),
                new Vector3(-h + 4f, 0f, -h + 4f),
                new Vector3(0f, 0f, h - 4f)
            };
            return layout;
        }

        /// <summary>Random walkable point (not inside an obstacle).</summary>
        public static Vector3 RandomFreePoint(FarmLayout layout, float margin)
        {
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

        // ---------------------------------------------------------------- pieces

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
            RenderSettings.fogStartDistance = 24f;
            RenderSettings.fogEndDistance = 60f;
        }

        static void BuildPerimeter(Transform parent, float h)
        {
            Transform fence = new GameObject("Perimeter").transform;
            fence.SetParent(parent, false);

            // Invisible walls keep everyone inside.
            Shapes.InvisibleWall(fence, new Vector3(0f, 1.5f, h + 0.5f), new Vector3(h * 2f + 2f, 3f, 1f));
            Shapes.InvisibleWall(fence, new Vector3(0f, 1.5f, -h - 0.5f), new Vector3(h * 2f + 2f, 3f, 1f));
            Shapes.InvisibleWall(fence, new Vector3(h + 0.5f, 1.5f, 0f), new Vector3(1f, 3f, h * 2f + 2f));
            Shapes.InvisibleWall(fence, new Vector3(-h - 0.5f, 1.5f, 0f), new Vector3(1f, 3f, h * 2f + 2f));

            // Visible wooden fence.
            FenceVisual(fence, new Vector3(-h, 0f, h), new Vector3(h, 0f, h));
            FenceVisual(fence, new Vector3(-h, 0f, -h), new Vector3(h, 0f, -h));
            FenceVisual(fence, new Vector3(h, 0f, -h), new Vector3(h, 0f, h));
            FenceVisual(fence, new Vector3(-h, 0f, -h), new Vector3(-h, 0f, h));
        }

        /// <summary>Posts and rails between two points, decor only.</summary>
        static void FenceVisual(Transform parent, Vector3 from, Vector3 to)
        {
            Vector3 delta = to - from;
            float length = delta.magnitude;
            Vector3 middle = (from + to) * 0.5f;
            float yaw = Mathf.Atan2(delta.x, delta.z) * Mathf.Rad2Deg;

            GameObject railLow = Shapes.Create(PrimitiveType.Cube, parent, "Rail", middle + Vector3.up * 0.45f, new Vector3(0.12f, 0.12f, length), Wood);
            railLow.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            GameObject railHigh = Shapes.Create(PrimitiveType.Cube, parent, "Rail", middle + Vector3.up * 0.95f, new Vector3(0.12f, 0.12f, length), Wood);
            railHigh.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);

            int posts = Mathf.Max(2, Mathf.RoundToInt(length / 3f) + 1);
            for (int i = 0; i < posts; i++)
            {
                Vector3 p = Vector3.Lerp(from, to, i / (float)(posts - 1));
                Shapes.Create(PrimitiveType.Cube, parent, "Post", p + Vector3.up * 0.6f, new Vector3(0.2f, 1.2f, 0.2f), Wood);
            }
        }

        /// <summary>Fence segment that blocks movement and projectiles.</summary>
        static void SolidFence(Transform parent, Vector3 from, Vector3 to)
        {
            Vector3 delta = to - from;
            Vector3 middle = (from + to) * 0.5f;
            float yaw = Mathf.Atan2(delta.x, delta.z) * Mathf.Rad2Deg;

            GameObject blocker = Shapes.InvisibleWall(parent, middle + Vector3.up * 0.6f, new Vector3(0.3f, 1.2f, delta.magnitude));
            blocker.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);

            FenceVisual(parent, from, to);
        }

        static void BuildBarn(Transform parent, Vector3 position)
        {
            Transform barn = new GameObject("Barn").transform;
            barn.SetParent(parent, false);
            barn.localPosition = position;

            Shapes.Obstacle(PrimitiveType.Cube, barn, "BarnBody", new Vector3(0f, 2.5f, 0f), new Vector3(8f, 5f, 7f), BarnRed);

            GameObject roofLeft = Shapes.Create(PrimitiveType.Cube, barn, "RoofL", new Vector3(-2.1f, 6.1f, 0f), new Vector3(5.2f, 0.3f, 7.6f), Roof);
            roofLeft.transform.localRotation = Quaternion.Euler(0f, 0f, 35f);
            GameObject roofRight = Shapes.Create(PrimitiveType.Cube, barn, "RoofR", new Vector3(2.1f, 6.1f, 0f), new Vector3(5.2f, 0.3f, 7.6f), Roof);
            roofRight.transform.localRotation = Quaternion.Euler(0f, 0f, -35f);

            Shapes.Create(PrimitiveType.Cube, barn, "Door", new Vector3(0f, 1.6f, -3.52f), new Vector3(3f, 3.2f, 0.1f), new Color(0.2f, 0.06f, 0.05f));
            Shapes.Create(PrimitiveType.Cube, barn, "DoorX1", new Vector3(0f, 1.6f, -3.58f), new Vector3(0.2f, 4.2f, 0.05f), Wood).transform.localRotation = Quaternion.Euler(0f, 0f, 43f);
            Shapes.Create(PrimitiveType.Cube, barn, "DoorX2", new Vector3(0f, 1.6f, -3.58f), new Vector3(0.2f, 4.2f, 0.05f), Wood).transform.localRotation = Quaternion.Euler(0f, 0f, -43f);
        }

        /// <summary>Sheep pen with two openings: walls to fight around.</summary>
        static void BuildPen(Transform parent, Vector3 center, float width, float depth)
        {
            Transform pen = new GameObject("SheepPen").transform;
            pen.SetParent(parent, false);

            float x0 = center.x - width * 0.5f;
            float x1 = center.x + width * 0.5f;
            float z0 = center.z - depth * 0.5f;
            float z1 = center.z + depth * 0.5f;
            float gap = 1.6f;

            SolidFence(pen, new Vector3(x0, 0f, z1), new Vector3(x1, 0f, z1));                  // north (closed)
            SolidFence(pen, new Vector3(x0, 0f, z0), new Vector3(center.x - gap, 0f, z0));      // south left
            SolidFence(pen, new Vector3(center.x + gap, 0f, z0), new Vector3(x1, 0f, z0));      // south right
            SolidFence(pen, new Vector3(x0, 0f, z0), new Vector3(x0, 0f, z1));                  // west (closed)
            SolidFence(pen, new Vector3(x1, 0f, z0), new Vector3(x1, 0f, center.z - gap));      // east bottom
            SolidFence(pen, new Vector3(x1, 0f, center.z + gap), new Vector3(x1, 0f, z1));      // east top
        }

        static void BuildHaystack(Transform parent, Vector3 position)
        {
            Shapes.Obstacle(PrimitiveType.Cylinder, parent, "Haystack", position + Vector3.up * 0.8f, new Vector3(1.6f, 0.8f, 1.6f), Hay);
        }

        static void BuildTree(Transform parent, Vector3 position)
        {
            Shapes.Obstacle(PrimitiveType.Cylinder, parent, "TreeTrunk", position + Vector3.up * 1.2f, new Vector3(0.6f, 1.2f, 0.6f), Wood);
            Shapes.Create(PrimitiveType.Sphere, parent, "TreeCrown", position + Vector3.up * 3.4f, new Vector3(3f, 2.6f, 3f), Leaves);
        }

        static void BuildLantern(Transform parent, Vector3 position)
        {
            Shapes.Obstacle(PrimitiveType.Cube, parent, "LanternPost", position + Vector3.up * 1.1f, new Vector3(0.2f, 2.2f, 0.2f), Wood);
            Shapes.Create(PrimitiveType.Cube, parent, "Lantern", position + Vector3.up * 2.3f, Vector3.one * 0.35f, LanternLight, true);

            GameObject lightGo = new GameObject("LanternLight");
            lightGo.transform.SetParent(parent, false);
            lightGo.transform.localPosition = position + Vector3.up * 2.3f;
            Light light = lightGo.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = LanternLight;
            light.range = 9f;
            light.intensity = 2.5f;
        }
    }
}
