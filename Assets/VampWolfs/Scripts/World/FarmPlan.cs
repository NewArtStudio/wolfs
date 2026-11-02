using System;
using System.Collections.Generic;

namespace VampWolfs
{
    /// <summary>A collision footprint on the ground, in WORLD meters (box or circle/ellipse seen from above).</summary>
    public class FarmFootprint
    {
        public bool IsBox;
        public float X, Z;          // center
        public float HalfW, HalfD;  // box half sizes, or ellipse radii (circle: both = radius)
        public float Yaw;           // degrees

        /// <summary>True if (x, z) is inside the shape grown by "grow" meters.</summary>
        public bool Contains(float x, float z, float grow)
        {
            float lx, lz;
            FarmPlan.Rotate(x - X, z - Z, -Yaw, out lx, out lz);
            if (IsBox)
            {
                float dx = Math.Max(Math.Abs(lx) - HalfW, 0f);
                float dz = Math.Max(Math.Abs(lz) - HalfD, 0f);
                return dx * dx + dz * dz <= grow * grow;
            }
            float a = HalfW + grow, b = HalfD + grow;
            return (lx * lx) / (a * a) + (lz * lz) / (b * b) <= 1f;
        }

        /// <summary>Radius of a circle that surely contains the shape (for bounding boxes).</summary>
        public float Reach { get { return IsBox ? (float)Math.Sqrt(HalfW * HalfW + HalfD * HalfD) : Math.Max(HalfW, HalfD); } }
    }

    /// <summary>A piece already placed in the world (tile rotation/mirror applied).</summary>
    public class PlacedPiece
    {
        public FarmPieceDef Def;
        public float X, Z, Yaw;     // world
        public FarmFootprint Footprint;
        public bool Blocks;
        public int Cell;            // index of the tile it belongs to
    }

    /// <summary>One cell of the grid: which tile, how it is turned, which sides have roads.</summary>
    public class FarmPlacement
    {
        public int Gx, Gz;
        public FarmTileDef Tile;
        public int Turns;           // 0..3 × 90° (clockwise seen from above)
        public bool Mirror;         // mirrored east-west before turning
        public int RoadSides;       // bit mask of WORLD sides with a road: N=1, E=2, S=4, W=8
        public float CenterX, CenterZ;
    }

    /// <summary>
    /// The procedural map as pure data (no Unity objects): which tile goes where, the road network and
    /// every piece already in world coordinates. Same seed = same map (multiplayer, bug reports).
    /// FarmGenerator turns it into GameObjects; WalkGrid uses the footprints for navigation.
    /// </summary>
    public class FarmPlan
    {
        public const int N = 1, E = 2, S = 4, W = 8;

        public int Seed;
        public int Size;            // tiles per side
        public float TileSize;
        public float HalfSize;      // half the side of the whole map
        public FarmPlacement[] Cells;
        public readonly List<PlacedPiece> Pieces = new List<PlacedPiece>();

        public FarmPlacement Cell(int gx, int gz) { return Cells[gz * Size + gx]; }

        // ---------------------------------------------------------------- creation

        public static FarmPlan Create(FarmTileCatalog catalog, int size, int seed)
        {
            size = Math.Max(1, size);
            Random rng = new Random(seed);
            FarmPlan plan = new FarmPlan();
            plan.Seed = seed;
            plan.Size = size;
            plan.TileSize = catalog.TileSize;
            plan.HalfSize = size * catalog.TileSize * 0.5f;
            plan.Cells = new FarmPlacement[size * size];

            Dictionary<string, int> used = new Dictionary<string, int>();
            for (int gz = 0; gz < size; gz++)
            {
                for (int gx = 0; gx < size; gx++)
                {
                    FarmPlacement p = new FarmPlacement();
                    p.Gx = gx;
                    p.Gz = gz;
                    p.Tile = PickTile(catalog, used, rng);
                    p.Turns = rng.Next(4);
                    p.Mirror = rng.Next(2) == 1;
                    p.CenterX = -plan.HalfSize + (gx + 0.5f) * plan.TileSize;
                    p.CenterZ = -plan.HalfSize + (gz + 0.5f) * plan.TileSize;
                    plan.Cells[gz * size + gx] = p;
                }
            }

            plan.BuildRoads(rng);
            plan.PlacePieces(catalog);
            return plan;
        }

        static FarmTileDef PickTile(FarmTileCatalog catalog, Dictionary<string, int> used, Random rng)
        {
            if (catalog.Tiles == null || catalog.Tiles.Length == 0) return null;
            float total = 0f;
            foreach (FarmTileDef t in catalog.Tiles) if (Allowed(t, used)) total += Math.Max(0f, t.Weight);
            FarmTileDef pick = catalog.Tiles[0];
            if (total > 0f)
            {
                float r = (float)rng.NextDouble() * total;
                foreach (FarmTileDef t in catalog.Tiles)
                {
                    if (!Allowed(t, used)) continue;
                    pick = t;
                    r -= Math.Max(0f, t.Weight);
                    if (r <= 0f) break;
                }
            }
            int n;
            used.TryGetValue(pick.Id, out n);
            used[pick.Id] = n + 1;
            return pick;
        }

        static bool Allowed(FarmTileDef t, Dictionary<string, int> used)
        {
            int n;
            used.TryGetValue(t.Id, out n);
            return t.Max <= 0 || n < t.Max;
        }

        // ---------------------------------------------------------------- roads

        static readonly int[] Dx = { 0, 1, 0, -1 };   // N, E, S, W
        static readonly int[] Dz = { 1, 0, -1, 0 };
        static readonly int[] Bit = { N, E, S, W };

        /// <summary>
        /// Random spanning tree (depth-first walk) over the tiles; the roads are the tree paths between a few
        /// random places plus 2 exits into the forest. Gives winding country roads, not a city grid.
        /// </summary>
        void BuildRoads(Random rng)
        {
            int count = Size * Size;
            int[] parent = new int[count];
            bool[] seen = new bool[count];
            for (int i = 0; i < count; i++) parent[i] = -1;

            List<int> stack = new List<int>();
            int start = rng.Next(count);
            stack.Add(start);
            seen[start] = true;
            List<int> next = new List<int>(4);
            while (stack.Count > 0)
            {
                int c = stack[stack.Count - 1];
                next.Clear();
                for (int d = 0; d < 4; d++)
                {
                    int x = c % Size + Dx[d], z = c / Size + Dz[d];
                    if (x >= 0 && x < Size && z >= 0 && z < Size && !seen[z * Size + x]) next.Add(z * Size + x);
                }
                if (next.Count == 0) { stack.RemoveAt(stack.Count - 1); continue; }
                int q = next[rng.Next(next.Count)];
                seen[q] = true;
                parent[q] = c;
                stack.Add(q);
            }

            List<int> cells = new List<int>();
            for (int i = 0; i < count; i++) cells.Add(i);
            Shuffle(cells, rng);
            int landmarks = Math.Max(2, Math.Min(count, count / 6));
            List<int> marks = cells.GetRange(0, Math.Min(landmarks, cells.Count));
            List<int> border = cells.FindAll(i => IsBorder(i));
            int exits = Math.Min(2, border.Count);
            for (int i = 0; i < exits; i++) marks.Add(border[i]);

            for (int m = 1; m < marks.Count; m++) LinkTreePath(marks[0], marks[m], parent);

            for (int i = 0; i < exits; i++)
            {
                int c = border[i];
                List<int> outs = new List<int>();
                for (int d = 0; d < 4; d++)
                {
                    int x = c % Size + Dx[d], z = c / Size + Dz[d];
                    if (x < 0 || x >= Size || z < 0 || z >= Size) outs.Add(d);
                }
                Cells[c].RoadSides |= Bit[outs[rng.Next(outs.Count)]];
            }
        }

        bool IsBorder(int i)
        {
            int x = i % Size, z = i / Size;
            return x == 0 || z == 0 || x == Size - 1 || z == Size - 1;
        }

        void LinkTreePath(int a, int b, int[] parent)
        {
            List<int> pa = PathToRoot(a, parent), pb = PathToRoot(b, parent);
            HashSet<int> inB = new HashSet<int>(pb);
            int lca = pa.Find(c => inB.Contains(c));
            LinkUntil(pa, lca);
            LinkUntil(pb, lca);
        }

        void LinkUntil(List<int> path, int stop)
        {
            for (int i = 0; i < path.Count - 1 && path[i] != stop; i++) Link(path[i], path[i + 1]);
        }

        static List<int> PathToRoot(int c, int[] parent)
        {
            List<int> p = new List<int> { c };
            while (parent[p[p.Count - 1]] >= 0) p.Add(parent[p[p.Count - 1]]);
            return p;
        }

        void Link(int a, int b)
        {
            int ax = a % Size, az = a / Size, bx = b % Size, bz = b / Size;
            for (int d = 0; d < 4; d++)
            {
                if (ax + Dx[d] == bx && az + Dz[d] == bz) Cells[a].RoadSides |= Bit[d];
                if (bx + Dx[d] == ax && bz + Dz[d] == az) Cells[b].RoadSides |= Bit[d];
            }
        }

        static void Shuffle(List<int> list, Random rng)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                int t = list[i]; list[i] = list[j]; list[j] = t;
            }
        }

        // ---------------------------------------------------------------- pieces in the world

        void PlacePieces(FarmTileCatalog catalog)
        {
            for (int i = 0; i < Cells.Length; i++)
            {
                FarmPlacement cell = Cells[i];
                if (cell.Tile == null || cell.Tile.Pieces == null) continue;
                foreach (FarmPieceDef def in cell.Tile.Pieces)
                {
                    if (def.T == "road" && !RoadDrawn(def, cell)) continue;

                    PlacedPiece p = new PlacedPiece();
                    p.Def = def;
                    p.Cell = i;
                    float lx, lz, yaw;
                    ToWorld(def.X, def.Z, def.R, cell.Turns, cell.Mirror, out lx, out lz, out yaw);
                    p.X = cell.CenterX + lx;
                    p.Z = cell.CenterZ + lz;
                    p.Yaw = yaw;

                    FarmShapeDef shape = catalog.Shape(def.T);
                    p.Blocks = shape != null && shape.Block;
                    p.Footprint = MakeFootprint(shape, def, p.X, p.Z, yaw);
                    Pieces.Add(p);
                }
            }
        }

        /// <summary>Road pieces: the middle part shows if the tile has any road; a side spoke only if that side is used.</summary>
        static bool RoadDrawn(FarmPieceDef def, FarmPlacement cell)
        {
            if (cell.RoadSides == 0) return false;
            if (string.IsNullOrEmpty(def.E)) return true;
            return (cell.RoadSides & WorldSide(def.E, cell.Turns, cell.Mirror)) != 0;
        }

        static FarmFootprint MakeFootprint(FarmShapeDef shape, FarmPieceDef def, float x, float z, float yaw)
        {
            if (shape == null) return null;
            float s = def.Scale;
            FarmFootprint f = new FarmFootprint { X = x, Z = z, Yaw = yaw };
            if (shape.Shape == "circle")
            {
                f.HalfW = f.HalfD = shape.W * s;
            }
            else if (shape.Shape == "ellipse")
            {
                f.HalfW = def.W * 0.5f;
                f.HalfD = def.L * 0.5f;
            }
            else
            {
                f.IsBox = true;
                f.HalfW = (shape.W > 0f ? shape.W : def.W) * s * 0.5f;
                f.HalfD = (shape.D > 0f ? shape.D : def.L) * s * 0.5f;
            }
            return f;
        }

        // ---------------------------------------------------------------- math (same as build_farm_tiles.py)

        /// <summary>Unity's Quaternion.Euler(0, deg, 0) * (x, 0, z), without Unity.</summary>
        public static void Rotate(float x, float z, float deg, out float rx, out float rz)
        {
            double a = deg * Math.PI / 180.0;
            double c = Math.Cos(a), s = Math.Sin(a);
            rx = (float)(x * c + z * s);
            rz = (float)(-x * s + z * c);
        }

        /// <summary>Tile-local position/yaw -> offset/yaw in the world: mirror east-west, then turn.</summary>
        public static void ToWorld(float x, float z, float yaw, int turns, bool mirror, out float wx, out float wz, out float wyaw)
        {
            if (mirror) { x = -x; yaw = -yaw; }
            Rotate(x, z, turns * 90f, out wx, out wz);
            wyaw = yaw + turns * 90f;
        }

        /// <summary>Which WORLD side a tile-local side ("n","e","s","w") ends up on.</summary>
        public static int WorldSide(string localSide, int turns, bool mirror)
        {
            int d = localSide == "n" ? 0 : localSide == "e" ? 1 : localSide == "s" ? 2 : 3;
            float x = Dx[d], z = Dz[d], wx, wz, unused;
            ToWorld(x, z, 0f, turns, mirror, out wx, out wz, out unused);
            int rx = (int)Math.Round(wx), rz = (int)Math.Round(wz);
            for (int k = 0; k < 4; k++) if (Dx[k] == rx && Dz[k] == rz) return Bit[k];
            return N;
        }
    }
}
