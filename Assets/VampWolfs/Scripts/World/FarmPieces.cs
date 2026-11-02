using System.Collections.Generic;
using UnityEngine;

namespace VampWolfs
{
    /// <summary>
    /// The LOOK of each farm piece (barn, fence, tree…), made of primitives for now.
    ///
    /// To swap a piece for a real model, no code needed: put a prefab at
    ///   Resources/VampWolfs/FarmPieces/&lt;type&gt;.prefab   (e.g. barn.prefab, tree.prefab, fence.prefab)
    /// - pivot on the ground at the center, facing +Z (same as the placeholder);
    /// - long pieces (fence, ironfence, hedge, croprow, road) = a 2 m segment along +Z, repeated along the length;
    /// - colliders inside the prefab are switched OFF: collision always comes from farm_tiles.json (the size the
    ///   map generator checked), so a new model never closes a path by accident.
    /// </summary>
    public static class FarmPieces
    {
        public static readonly Color Grass = new Color(0.16f, 0.27f, 0.13f);
        public static readonly Color Dirt = new Color(0.32f, 0.24f, 0.16f);
        static readonly Color Wood = new Color(0.36f, 0.25f, 0.16f);
        static readonly Color DarkWood = new Color(0.22f, 0.15f, 0.1f);
        static readonly Color BarnRed = new Color(0.45f, 0.08f, 0.07f);
        static readonly Color Roof = new Color(0.22f, 0.12f, 0.1f);
        static readonly Color Hay = new Color(0.78f, 0.64f, 0.3f);
        static readonly Color Leaves = new Color(0.1f, 0.2f, 0.1f);
        static readonly Color Water = new Color(0.12f, 0.2f, 0.35f);
        static readonly Color Stone = new Color(0.42f, 0.42f, 0.45f);
        static readonly Color DarkStone = new Color(0.28f, 0.28f, 0.31f);
        static readonly Color Iron = new Color(0.1f, 0.1f, 0.12f);
        static readonly Color Plaster = new Color(0.55f, 0.5f, 0.42f);
        static readonly Color Corn = new Color(0.5f, 0.52f, 0.18f);
        static readonly Color Canvas = new Color(0.6f, 0.53f, 0.4f);
        static readonly Color WindowGlow = new Color(1f, 0.75f, 0.35f);
        public static readonly Color LanternLight = new Color(1f, 0.6f, 0.25f);
        static readonly Color FireLight = new Color(1f, 0.45f, 0.15f);

        static readonly Dictionary<string, GameObject> prefabCache = new Dictionary<string, GameObject>();

        static bool IsLong(string t)
        {
            return t == "fence" || t == "ironfence" || t == "hedge" || t == "croprow" || t == "road";
        }

        /// <summary>
        /// Builds the look of one placed piece. Static parts go under "staticRoot" (may be merged for speed);
        /// moving parts (windmill blades) and lights go under "dynamicRoot".
        /// </summary>
        public static void Build(PlacedPiece piece, Transform staticRoot, Transform dynamicRoot, System.Random rng)
        {
            FarmPieceDef d = piece.Def;
            Transform root = new GameObject(d.T).transform;
            root.SetParent(staticRoot, false);
            root.localPosition = new Vector3(piece.X, 0f, piece.Z);
            root.localRotation = Quaternion.Euler(0f, piece.Yaw, 0f);
            if (!IsLong(d.T) && d.T != "pond" && d.T != "dock") root.localScale = Vector3.one * d.Scale;

            if (TryPrefab(d, root)) return;

            switch (d.T)
            {
                case "barn": Barn(root); break;
                case "house": House(root); break;
                case "crypt": Crypt(root); break;
                case "tent": Tent(root); break;
                case "windmill": Windmill(root, dynamicRoot, piece); break;
                case "well": Well(root); break;
                case "haystack": Haystack(root); break;
                case "bale": P(PrimitiveType.Cube, root, new Vector3(0f, 0.4f, 0f), new Vector3(1.4f, 0.8f, 0.9f), Hay); break;
                case "tree": Tree(root, rng); break;
                case "deadtree": DeadTree(root, rng); break;
                case "rock": Rock(root, rng); break;
                case "crate": Crate(root); break;
                case "barrel": Barrel(root); break;
                case "cart": Cart(root); break;
                case "woodpile": Woodpile(root); break;
                case "trough": Trough(root); break;
                case "fence": Fence(root, d.L); break;
                case "ironfence": IronFence(root, d.L); break;
                case "hedge": P(PrimitiveType.Cube, root, new Vector3(0f, 0.65f, 0f), new Vector3(0.9f, 1.3f, d.L), new Color(0.11f, 0.24f, 0.11f)); break;
                case "croprow": CropRow(root, d.L, rng); break;
                case "grave": Grave(root, rng); break;
                case "lantern": Lantern(root, dynamicRoot, piece); break;
                case "scarecrow": Scarecrow(root); break;
                case "millstone": P(PrimitiveType.Cylinder, root, new Vector3(0f, 0.2f, 0f), new Vector3(1.6f, 0.2f, 1.6f), Stone); break;
                case "sacks": Sacks(root); break;
                case "log": Log(root); break;
                case "road": P(PrimitiveType.Cube, root, new Vector3(0f, 0.035f, 0f), new Vector3(d.W, 0.02f, d.L), Dirt); break;
                case "pond": Pond(root, d); break;
                case "dock": P(PrimitiveType.Cube, root, new Vector3(0f, 0.15f, 0f), new Vector3(d.W, 0.1f, d.L), Wood); break;
                case "campfire": Campfire(root, dynamicRoot, piece); break;
                case "reeds": Reeds(root, rng); break;
                case "flowers": Flowers(root, rng); break;
                case "pumpkin": P(PrimitiveType.Sphere, root, new Vector3(0f, 0.25f, 0f), new Vector3(0.7f, 0.5f, 0.7f), new Color(0.85f, 0.42f, 0.1f)); break;
                default: Debug.LogWarning("[VampWolfs] peça sem visual: " + d.T); break;
            }
        }

        /// <summary>Invisible collider from the checked footprint (blocks movement and projectiles).</summary>
        public static void AddCollider(PlacedPiece piece, Transform parent)
        {
            FarmFootprint f = piece.Footprint;
            if (!piece.Blocks || f == null) return;
            GameObject go = new GameObject(piece.Def.T + "_col");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(f.X, 1.5f, f.Z);
            go.transform.localRotation = Quaternion.Euler(0f, f.Yaw, 0f);
            if (f.IsBox)
            {
                go.AddComponent<BoxCollider>().size = new Vector3(f.HalfW * 2f, 3f, f.HalfD * 2f);
            }
            else
            {
                CapsuleCollider c = go.AddComponent<CapsuleCollider>();
                c.radius = Mathf.Max(f.HalfW, f.HalfD);
                c.height = 3f + c.radius * 2f;
            }
            // low pieces (crates, graves…) block walking but shots fly over them: no Obstacle marker
            if (!piece.Low) go.AddComponent<Obstacle>();
        }

        // ---------------------------------------------------------------- prefab override

        static bool TryPrefab(FarmPieceDef d, Transform root)
        {
            GameObject prefab;
            if (!prefabCache.TryGetValue(d.T, out prefab))
            {
                prefab = Resources.Load<GameObject>("VampWolfs/FarmPieces/" + d.T);
                prefabCache[d.T] = prefab;
            }
            if (prefab == null) return false;

            if (IsLong(d.T))
            {
                int n = Mathf.Max(1, Mathf.RoundToInt(d.L / 2f));
                float seg = d.L / n;
                for (int i = 0; i < n; i++)
                {
                    GameObject g = Object.Instantiate(prefab, root);
                    g.transform.localPosition = new Vector3(0f, 0f, -d.L * 0.5f + seg * (i + 0.5f));
                    g.transform.localRotation = Quaternion.identity;
                    g.transform.localScale = new Vector3(d.T == "road" ? d.W / 2.2f : 1f, 1f, seg / 2f);
                    DisableColliders(g);
                }
            }
            else
            {
                GameObject g = Object.Instantiate(prefab, root);
                g.transform.localPosition = Vector3.zero;
                g.transform.localRotation = Quaternion.identity;
                DisableColliders(g);
            }
            return true;
        }

        static void DisableColliders(GameObject g)
        {
            foreach (Collider c in g.GetComponentsInChildren<Collider>(true)) c.enabled = false;
        }

        // ---------------------------------------------------------------- primitive looks

        static GameObject P(PrimitiveType type, Transform parent, Vector3 pos, Vector3 scale, Color color, float yaw = 0f, float roll = 0f, float pitch = 0f)
        {
            GameObject go = Shapes.Create(type, parent, type.ToString(), pos, scale, color);
            if (yaw != 0f || roll != 0f || pitch != 0f) go.transform.localRotation = Quaternion.Euler(pitch, yaw, roll);
            return go;
        }

        static GameObject Glow(Transform parent, Vector3 pos, Vector3 scale, Color color)
        {
            return Shapes.Create(PrimitiveType.Cube, parent, "Glow", pos, scale, color, true);
        }

        /// <summary>Slight color variation in 7 fixed steps (each distinct color is a material: keep them few).</summary>
        static Color Vary(Color c, System.Random rng, float amount)
        {
            float k = 1f + (rng.Next(7) - 3) / 3f * amount;
            return new Color(c.r * k, c.g * k, c.b * k, c.a);
        }

        static void Barn(Transform r)
        {
            P(PrimitiveType.Cube, r, new Vector3(0f, 2.5f, 0f), new Vector3(8f, 5f, 7f), BarnRed);
            P(PrimitiveType.Cube, r, new Vector3(-2.1f, 6.1f, 0f), new Vector3(5.2f, 0.3f, 7.6f), Roof, 0f, 35f);
            P(PrimitiveType.Cube, r, new Vector3(2.1f, 6.1f, 0f), new Vector3(5.2f, 0.3f, 7.6f), Roof, 0f, -35f);
            P(PrimitiveType.Cube, r, new Vector3(0f, 1.6f, -3.52f), new Vector3(3f, 3.2f, 0.1f), new Color(0.2f, 0.06f, 0.05f));
            P(PrimitiveType.Cube, r, new Vector3(0f, 1.6f, -3.58f), new Vector3(0.2f, 4.2f, 0.05f), Wood, 0f, 43f);
            P(PrimitiveType.Cube, r, new Vector3(0f, 1.6f, -3.58f), new Vector3(0.2f, 4.2f, 0.05f), Wood, 0f, -43f);
            Glow(r, new Vector3(0f, 4.3f, -3.56f), new Vector3(0.9f, 0.6f, 0.05f), WindowGlow);
        }

        static void House(Transform r)
        {
            P(PrimitiveType.Cube, r, new Vector3(0f, 1.8f, 0f), new Vector3(7f, 3.6f, 6f), Plaster);
            P(PrimitiveType.Cube, r, new Vector3(0f, 4.4f, -1.55f), new Vector3(7.4f, 0.3f, 3.7f), Roof, 0f, 0f, -30f);
            P(PrimitiveType.Cube, r, new Vector3(0f, 4.4f, 1.55f), new Vector3(7.4f, 0.3f, 3.7f), Roof, 0f, 0f, 30f);
            P(PrimitiveType.Cube, r, new Vector3(0f, 4.3f, 0f), new Vector3(6.9f, 1.4f, 2.4f), Plaster);   // gable fill
            P(PrimitiveType.Cube, r, new Vector3(2.4f, 5.1f, 0.8f), new Vector3(0.7f, 1.8f, 0.7f), DarkStone);
            P(PrimitiveType.Cube, r, new Vector3(0f, 1.1f, -3.02f), new Vector3(1.2f, 2.2f, 0.06f), DarkWood);
            Glow(r, new Vector3(-2f, 2f, -3.03f), new Vector3(1f, 0.8f, 0.04f), WindowGlow);
            Glow(r, new Vector3(2f, 2f, -3.03f), new Vector3(1f, 0.8f, 0.04f), WindowGlow);
            Glow(r, new Vector3(3.53f, 2f, 0f), new Vector3(0.04f, 0.8f, 1f), WindowGlow);
        }

        static void Crypt(Transform r)
        {
            P(PrimitiveType.Cube, r, new Vector3(0f, 1.3f, 0f), new Vector3(4f, 2.6f, 3.4f), DarkStone);
            P(PrimitiveType.Cube, r, new Vector3(0f, 2.75f, 0f), new Vector3(4.4f, 0.3f, 3.8f), Stone);
            P(PrimitiveType.Cube, r, new Vector3(0f, 1f, -1.72f), new Vector3(1.2f, 2f, 0.06f), Iron);
            P(PrimitiveType.Cube, r, new Vector3(0f, 3.5f, 0f), new Vector3(0.18f, 1.2f, 0.18f), Stone);
            P(PrimitiveType.Cube, r, new Vector3(0f, 3.7f, 0f), new Vector3(0.7f, 0.18f, 0.18f), Stone);
            Glow(r, new Vector3(0f, 1.2f, -1.76f), new Vector3(0.5f, 0.12f, 0.03f), new Color(0.55f, 1f, 0.6f));
        }

        static void Tent(Transform r)
        {
            P(PrimitiveType.Cube, r, new Vector3(-0.75f, 0.95f, 0f), new Vector3(1.9f, 0.08f, 3f), Canvas, 0f, 52f);
            P(PrimitiveType.Cube, r, new Vector3(0.75f, 0.95f, 0f), new Vector3(1.9f, 0.08f, 3f), Canvas, 0f, -52f);
            P(PrimitiveType.Cube, r, new Vector3(0f, 0.9f, 1.45f), new Vector3(0.1f, 1.8f, 0.1f), Wood);
            P(PrimitiveType.Cube, r, new Vector3(0f, 0.9f, -1.45f), new Vector3(0.1f, 1.8f, 0.1f), Wood);
        }

        static void Windmill(Transform r, Transform dynamicRoot, PlacedPiece piece)
        {
            P(PrimitiveType.Cylinder, r, new Vector3(0f, 2.5f, 0f), new Vector3(4f, 2.5f, 4f), Stone);
            P(PrimitiveType.Cylinder, r, new Vector3(0f, 6.2f, 0f), new Vector3(3f, 1.2f, 3f), Plaster);
            P(PrimitiveType.Cube, r, new Vector3(0f, 7.9f, 0f), new Vector3(3.2f, 0.9f, 3.2f), Roof, 45f);
            P(PrimitiveType.Cube, r, new Vector3(0f, 1.1f, -2.02f), new Vector3(1.1f, 2.2f, 0.06f), DarkWood);
            Glow(r, new Vector3(0f, 5.9f, -1.52f), new Vector3(0.6f, 0.6f, 0.04f), WindowGlow);

            // blades turn (Spinner), so they live outside the merged static meshes
            float s = piece.Def.Scale;
            Transform hub = new GameObject("Blades").transform;
            hub.SetParent(dynamicRoot, false);
            hub.localPosition = new Vector3(piece.X, 0f, piece.Z) + Quaternion.Euler(0f, piece.Yaw, 0f) * new Vector3(0f, 6.6f, -1.9f) * s;
            hub.localRotation = Quaternion.Euler(0f, piece.Yaw, 0f);
            hub.localScale = Vector3.one * s;
            for (int i = 0; i < 4; i++)
            {
                Transform arm = new GameObject("Arm").transform;
                arm.SetParent(hub, false);
                arm.localRotation = Quaternion.Euler(0f, 0f, i * 90f);
                P(PrimitiveType.Cube, arm, new Vector3(0f, 2.6f, 0f), new Vector3(0.15f, 5.2f, 0.1f), DarkWood);
                P(PrimitiveType.Cube, arm, new Vector3(0.45f, 3f, 0f), new Vector3(0.75f, 3.8f, 0.04f), Canvas);
            }
            hub.gameObject.AddComponent<Spinner>().DegreesPerSecond = 25f;
        }

        static void Well(Transform r)
        {
            P(PrimitiveType.Cylinder, r, new Vector3(0f, 0.45f, 0f), new Vector3(1.8f, 0.45f, 1.8f), Stone);
            Glow(r, new Vector3(0f, 0.91f, 0f), new Vector3(1.2f, 0.02f, 1.2f), new Color(0.04f, 0.06f, 0.1f));
            P(PrimitiveType.Cube, r, new Vector3(-0.8f, 1.3f, 0f), new Vector3(0.15f, 1.7f, 0.15f), Wood);
            P(PrimitiveType.Cube, r, new Vector3(0.8f, 1.3f, 0f), new Vector3(0.15f, 1.7f, 0.15f), Wood);
            P(PrimitiveType.Cube, r, new Vector3(0f, 2.25f, 0f), new Vector3(2f, 0.15f, 1.3f), Roof, 0f, 0f, 0f);
            P(PrimitiveType.Cylinder, r, new Vector3(0f, 1.8f, 0f), new Vector3(0.15f, 0.8f, 0.15f), DarkWood, 0f, 90f);
        }

        static void Haystack(Transform r)
        {
            P(PrimitiveType.Cylinder, r, new Vector3(0f, 0.7f, 0f), new Vector3(1.6f, 0.7f, 1.6f), Hay);
            P(PrimitiveType.Sphere, r, new Vector3(0f, 1.4f, 0f), new Vector3(1.6f, 0.9f, 1.6f), Hay * 0.95f);
        }

        static void Tree(Transform r, System.Random rng)
        {
            P(PrimitiveType.Cylinder, r, new Vector3(0f, 1.2f, 0f), new Vector3(0.6f, 1.2f, 0.6f), Wood);
            Color c = Vary(Leaves, rng, 0.25f);
            P(PrimitiveType.Sphere, r, new Vector3(0f, 3.3f, 0f), new Vector3(3f, 2.4f, 3f), c);
            P(PrimitiveType.Sphere, r, new Vector3(0.5f, 4.2f, 0.3f), new Vector3(1.9f, 1.6f, 1.9f), c * 1.1f);
        }

        static void DeadTree(Transform r, System.Random rng)
        {
            Color c = new Color(0.25f, 0.21f, 0.18f);
            P(PrimitiveType.Cylinder, r, new Vector3(0f, 1.4f, 0f), new Vector3(0.45f, 1.4f, 0.45f), c);
            float y = (float)rng.NextDouble() * 360f;
            P(PrimitiveType.Cube, r, new Vector3(0.5f, 2.6f, 0f), new Vector3(0.15f, 1.4f, 0.15f), c, y, -40f);
            P(PrimitiveType.Cube, r, new Vector3(-0.4f, 2.2f, 0.2f), new Vector3(0.12f, 1.1f, 0.12f), c, y + 40f, 45f);
        }

        static void Rock(Transform r, System.Random rng)
        {
            P(PrimitiveType.Sphere, r, new Vector3(0f, 0.3f, 0f), new Vector3(1.3f, 0.8f, 1.1f), Vary(Stone, rng, 0.15f), (float)rng.NextDouble() * 360f, 8f);
        }

        static void Crate(Transform r)
        {
            P(PrimitiveType.Cube, r, new Vector3(0f, 0.5f, 0f), Vector3.one, Wood);
            P(PrimitiveType.Cube, r, new Vector3(0f, 0.5f, 0f), new Vector3(1.04f, 0.15f, 1.04f), DarkWood);
        }

        static void Barrel(Transform r)
        {
            P(PrimitiveType.Cylinder, r, new Vector3(0f, 0.55f, 0f), new Vector3(0.9f, 0.55f, 0.9f), Wood);
            P(PrimitiveType.Cylinder, r, new Vector3(0f, 0.75f, 0f), new Vector3(0.94f, 0.05f, 0.94f), Iron);
        }

        static void Cart(Transform r)
        {
            P(PrimitiveType.Cube, r, new Vector3(0f, 0.8f, 0f), new Vector3(1.6f, 0.15f, 3f), Wood);
            P(PrimitiveType.Cube, r, new Vector3(-0.75f, 1.1f, 0f), new Vector3(0.1f, 0.5f, 3f), DarkWood);
            P(PrimitiveType.Cube, r, new Vector3(0.75f, 1.1f, 0f), new Vector3(0.1f, 0.5f, 3f), DarkWood);
            P(PrimitiveType.Cylinder, r, new Vector3(-0.9f, 0.6f, -0.6f), new Vector3(1.2f, 0.06f, 1.2f), DarkWood, 0f, 90f);
            P(PrimitiveType.Cylinder, r, new Vector3(0.9f, 0.6f, -0.6f), new Vector3(1.2f, 0.06f, 1.2f), DarkWood, 0f, 90f);
            P(PrimitiveType.Cube, r, new Vector3(0f, 0.6f, 2.2f), new Vector3(0.1f, 0.1f, 1.6f), Wood);
            P(PrimitiveType.Cube, r, new Vector3(0f, 1.1f, 0.4f), new Vector3(1.2f, 0.4f, 1.6f), Hay);
        }

        static void Woodpile(Transform r)
        {
            for (int i = 0; i < 5; i++)
            {
                float x = -0.8f + (i % 3) * 0.8f + (i >= 3 ? 0.4f : 0f);
                float y = i >= 3 ? 0.65f : 0.25f;
                P(PrimitiveType.Cylinder, r, new Vector3(x, y, 0f), new Vector3(0.5f, 0.5f, 0.5f), Wood, 0f, 0f, 90f);   // log along Z
            }
        }

        static void Trough(Transform r)
        {
            P(PrimitiveType.Cube, r, new Vector3(0f, 0.3f, 0f), new Vector3(0.8f, 0.6f, 2.4f), Wood);
            Glow(r, new Vector3(0f, 0.61f, 0f), new Vector3(0.6f, 0.02f, 2.2f), new Color(0.08f, 0.13f, 0.2f));
        }

        static void Fence(Transform r, float length)
        {
            P(PrimitiveType.Cube, r, new Vector3(0f, 0.45f, 0f), new Vector3(0.12f, 0.12f, length), Wood);
            P(PrimitiveType.Cube, r, new Vector3(0f, 0.95f, 0f), new Vector3(0.12f, 0.12f, length), Wood);
            int posts = Mathf.Max(2, Mathf.RoundToInt(length / 2f) + 1);
            for (int i = 0; i < posts; i++)
            {
                float z = -length * 0.5f + length * i / (posts - 1);
                P(PrimitiveType.Cube, r, new Vector3(0f, 0.6f, z), new Vector3(0.2f, 1.2f, 0.2f), Wood);
            }
        }

        static void IronFence(Transform r, float length)
        {
            P(PrimitiveType.Cube, r, new Vector3(0f, 1.35f, 0f), new Vector3(0.06f, 0.06f, length), Iron);
            P(PrimitiveType.Cube, r, new Vector3(0f, 0.25f, 0f), new Vector3(0.06f, 0.06f, length), Iron);
            int bars = Mathf.Max(2, Mathf.RoundToInt(length / 0.5f) + 1);
            for (int i = 0; i < bars; i++)
            {
                float z = -length * 0.5f + length * i / (bars - 1);
                P(PrimitiveType.Cube, r, new Vector3(0f, 0.75f, z), new Vector3(0.05f, 1.5f, 0.05f), Iron);
            }
            P(PrimitiveType.Cube, r, new Vector3(0f, 0.9f, -length * 0.5f), new Vector3(0.3f, 1.8f, 0.3f), DarkStone);
            P(PrimitiveType.Cube, r, new Vector3(0f, 0.9f, length * 0.5f), new Vector3(0.3f, 1.8f, 0.3f), DarkStone);
        }

        static void CropRow(Transform r, float length, System.Random rng)
        {
            P(PrimitiveType.Cube, r, new Vector3(0f, 0.8f, 0f), new Vector3(0.6f, 1.6f, length), Vary(Corn, rng, 0.1f));
            int tufts = Mathf.Max(2, Mathf.RoundToInt(length / 0.8f));
            for (int i = 0; i < tufts; i++)
            {
                float z = -length * 0.5f + (i + 0.5f) * length / tufts;
                P(PrimitiveType.Cube, r, new Vector3(0f, 1.75f, z), new Vector3(0.5f, 0.4f, 0.5f), Vary(Corn * 0.8f, rng, 0.15f), (float)rng.NextDouble() * 90f);
            }
        }

        static void Grave(Transform r, System.Random rng)
        {
            float tilt = ((float)rng.NextDouble() * 2f - 1f) * 7f;
            P(PrimitiveType.Cube, r, new Vector3(0f, 0.55f, 0f), new Vector3(0.8f, 1.1f, 0.22f), Vary(Stone, rng, 0.12f), 0f, tilt);
            P(PrimitiveType.Cube, r, new Vector3(0f, 0.06f, -0.8f), new Vector3(0.8f, 0.08f, 1.4f), new Color(0.2f, 0.17f, 0.13f));
        }

        static void Lantern(Transform r, Transform dynamicRoot, PlacedPiece piece)
        {
            P(PrimitiveType.Cube, r, new Vector3(0f, 1.1f, 0f), new Vector3(0.2f, 2.2f, 0.2f), Wood);
            Glow(r, new Vector3(0f, 2.3f, 0f), Vector3.one * 0.35f, LanternLight);
            AddLight(dynamicRoot, new Vector3(piece.X, 2.3f, piece.Z), LanternLight, 9f, 2.5f);
        }

        static void Scarecrow(Transform r)
        {
            P(PrimitiveType.Cube, r, new Vector3(0f, 1f, 0f), new Vector3(0.12f, 2f, 0.12f), Wood);
            P(PrimitiveType.Cube, r, new Vector3(0f, 1.5f, 0f), new Vector3(1.6f, 0.1f, 0.1f), Wood);
            P(PrimitiveType.Cube, r, new Vector3(0f, 1.35f, 0f), new Vector3(0.6f, 0.7f, 0.3f), new Color(0.35f, 0.2f, 0.25f));
            P(PrimitiveType.Sphere, r, new Vector3(0f, 2.05f, 0f), Vector3.one * 0.45f, Hay);
            P(PrimitiveType.Cylinder, r, new Vector3(0f, 2.3f, 0f), new Vector3(0.7f, 0.04f, 0.7f), DarkWood);
            P(PrimitiveType.Cylinder, r, new Vector3(0f, 2.42f, 0f), new Vector3(0.35f, 0.12f, 0.35f), DarkWood);
        }

        static void Sacks(Transform r)
        {
            Color c = new Color(0.72f, 0.66f, 0.52f);
            P(PrimitiveType.Sphere, r, new Vector3(-0.2f, 0.35f, 0f), new Vector3(0.55f, 0.7f, 0.5f), c);
            P(PrimitiveType.Sphere, r, new Vector3(0.25f, 0.33f, 0.15f), new Vector3(0.5f, 0.66f, 0.5f), c * 0.95f);
            P(PrimitiveType.Sphere, r, new Vector3(0f, 0.75f, 0.05f), new Vector3(0.45f, 0.55f, 0.45f), c * 1.05f);
        }

        static void Log(Transform r)
        {
            P(PrimitiveType.Cylinder, r, new Vector3(0f, 0.25f, 0f), new Vector3(0.5f, 0.8f, 0.5f), Wood, 0f, 90f);   // lying along X
        }

        static void Pond(Transform r, FarmPieceDef d)
        {
            P(PrimitiveType.Cylinder, r, new Vector3(0f, 0.04f, 0f), new Vector3(d.W + 0.8f, 0.01f, d.L + 0.8f), new Color(0.2f, 0.17f, 0.11f));
            P(PrimitiveType.Cylinder, r, new Vector3(0f, 0.06f, 0f), new Vector3(d.W, 0.01f, d.L), Water);
        }

        static void Campfire(Transform r, Transform dynamicRoot, PlacedPiece piece)
        {
            for (int i = 0; i < 7; i++)
            {
                float a = i * Mathf.PI * 2f / 7f;
                P(PrimitiveType.Sphere, r, new Vector3(Mathf.Cos(a) * 0.55f, 0.1f, Mathf.Sin(a) * 0.55f), new Vector3(0.3f, 0.2f, 0.3f), DarkStone);
            }
            P(PrimitiveType.Cylinder, r, new Vector3(0f, 0.15f, 0f), new Vector3(0.18f, 0.45f, 0.18f), DarkWood, 0f, 80f);
            P(PrimitiveType.Cylinder, r, new Vector3(0f, 0.15f, 0f), new Vector3(0.18f, 0.45f, 0.18f), DarkWood, 90f, 80f);
            Glow(r, new Vector3(0f, 0.4f, 0f), new Vector3(0.35f, 0.5f, 0.35f), FireLight);
            Glow(r, new Vector3(0f, 0.3f, 0f), new Vector3(0.5f, 0.25f, 0.5f), new Color(1f, 0.75f, 0.3f));
            AddLight(dynamicRoot, new Vector3(piece.X, 1.2f, piece.Z), FireLight, 10f, 3f);
        }

        static void Reeds(Transform r, System.Random rng)
        {
            for (int i = 0; i < 5; i++)
            {
                float x = ((float)rng.NextDouble() - 0.5f) * 0.8f, z = ((float)rng.NextDouble() - 0.5f) * 0.8f;
                float h = 0.6f + (float)rng.NextDouble() * 0.5f;
                P(PrimitiveType.Cube, r, new Vector3(x, h * 0.5f, z), new Vector3(0.05f, h, 0.05f), new Color(0.2f, 0.32f, 0.16f));
            }
        }

        static void Flowers(Transform r, System.Random rng)
        {
            Color[] colors = { new Color(0.75f, 0.35f, 0.65f), new Color(0.8f, 0.78f, 0.85f), new Color(0.55f, 0.35f, 0.8f) };
            for (int i = 0; i < 6; i++)
            {
                float x = ((float)rng.NextDouble() - 0.5f) * 0.9f, z = ((float)rng.NextDouble() - 0.5f) * 0.9f;
                P(PrimitiveType.Sphere, r, new Vector3(x, 0.12f, z), Vector3.one * 0.16f, colors[rng.Next(colors.Length)]);
            }
        }

        /// <summary>Small things scattered on the grass (no collision): tufts, pebbles, flowers.</summary>
        public static void Scatter(Transform parent, float x, float z, System.Random rng)
        {
            Transform r = new GameObject("Scatter").transform;
            r.SetParent(parent, false);
            r.localPosition = new Vector3(x, 0f, z);
            r.localRotation = Quaternion.Euler(0f, (float)rng.NextDouble() * 360f, 0f);
            int kind = rng.Next(5);
            if (kind <= 1)
            {
                Color g = Vary(new Color(0.13f, 0.24f, 0.11f), rng, 0.2f);
                for (int i = 0; i < 3; i++)
                    P(PrimitiveType.Cube, r, new Vector3((i - 1) * 0.12f, 0.15f, (i % 2) * 0.1f), new Vector3(0.06f, 0.3f + i * 0.05f, 0.06f), g, i * 25f, (i - 1) * 15f);
            }
            else if (kind == 2)
            {
                P(PrimitiveType.Sphere, r, new Vector3(0f, 0.06f, 0f), new Vector3(0.35f, 0.15f, 0.28f), Vary(Stone, rng, 0.2f));
            }
            else if (kind == 3)
            {
                Flowers(r, rng);
            }
            else
            {
                P(PrimitiveType.Cylinder, r, new Vector3(0f, 0.03f, 0f), new Vector3(1.6f, 0.005f, 1.2f), Vary(new Color(0.19f, 0.3f, 0.15f), rng, 0.1f));
            }
        }

        /// <summary>A forest tree outside the playable area (no collision: the invisible wall stops everyone).</summary>
        public static void ForestTree(Transform parent, float x, float z, System.Random rng)
        {
            Transform r = new GameObject("ForestTree").transform;
            r.SetParent(parent, false);
            r.localPosition = new Vector3(x, 0f, z);
            float s = 0.9f + (float)rng.NextDouble() * 0.7f;
            r.localScale = Vector3.one * s;
            Color c = Vary(new Color(0.06f, 0.14f, 0.08f), rng, 0.25f);
            P(PrimitiveType.Cylinder, r, new Vector3(0f, 1.5f, 0f), new Vector3(0.6f, 1.5f, 0.6f), DarkWood);
            if (rng.Next(3) == 0)
            {
                P(PrimitiveType.Sphere, r, new Vector3(0f, 3.8f, 0f), new Vector3(3.6f, 3.2f, 3.6f), c);
            }
            else
            {
                // pine: three shrinking layers
                for (int i = 0; i < 3; i++)
                    P(PrimitiveType.Cube, r, new Vector3(0f, 2.4f + i * 1.3f, 0f), new Vector3(3.2f - i * 0.9f, 1.2f, 3.2f - i * 0.9f), c * (1f + i * 0.08f), 45f + i * 20f);
            }
        }

        public static void Bush(Transform parent, float x, float z, System.Random rng)
        {
            float s = 0.8f + (float)rng.NextDouble() * 0.8f;
            GameObject b = Shapes.Create(PrimitiveType.Sphere, parent, "Bush", new Vector3(x, 0.4f * s, z), new Vector3(1.8f, 1.1f, 1.6f) * s, Vary(new Color(0.08f, 0.17f, 0.09f), rng, 0.25f));
            b.transform.localRotation = Quaternion.Euler(0f, (float)rng.NextDouble() * 360f, 0f);
        }

        static void AddLight(Transform parent, Vector3 pos, Color color, float range, float intensity)
        {
            GameObject go = new GameObject("PointLight");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            Light light = go.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = color;
            light.range = range;
            light.intensity = intensity;
            light.shadows = LightShadows.None;
        }
    }
}
