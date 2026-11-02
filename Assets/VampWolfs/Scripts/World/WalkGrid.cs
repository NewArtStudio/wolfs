using System;
using System.Collections.Generic;

namespace VampWolfs
{
    /// <summary>
    /// Where a character can stand on the farm, in 0.25 m cells (pure C#, no physics: built from the
    /// footprints of the generated map). Used to spawn things only in reachable places and as the
    /// invader's "GPS" (A* path around barns and fences).
    /// A cell is free when a character of radius CharacterRadius fits centered on it.
    /// </summary>
    public class WalkGrid
    {
        public const float CharacterRadius = 0.8f;   // biggest character (werewolf 0.65 × size 1.2), same as build_farm_tiles.py

        public readonly float Cell;
        public readonly float HalfSize;
        public readonly int Count;          // cells per side

        readonly bool[] free;
        readonly bool[] reachable;          // free AND connected to the biggest open area

        public WalkGrid(float halfSize, float cell)
        {
            HalfSize = halfSize;
            Cell = cell;
            Count = Math.Max(1, (int)Math.Ceiling(halfSize * 2f / cell));
            free = new bool[Count * Count];
            reachable = new bool[Count * Count];
        }

        public static WalkGrid FromPlan(FarmPlan plan, float cell = 0.25f)
        {
            WalkGrid g = new WalkGrid(plan.HalfSize, cell);
            for (int i = 0; i < g.free.Length; i++) g.free[i] = true;

            // map edge: keep the character inside the invisible walls
            int edge = (int)Math.Ceiling(CharacterRadius / cell);
            for (int z = 0; z < g.Count; z++)
                for (int x = 0; x < g.Count; x++)
                    if (x < edge || z < edge || x >= g.Count - edge || z >= g.Count - edge) g.free[z * g.Count + x] = false;

            foreach (PlacedPiece p in plan.Pieces)
            {
                if (p.Blocks && p.Footprint != null) g.Stamp(p.Footprint);
            }
            g.ComputeReachable();
            return g;
        }

        void Stamp(FarmFootprint f)
        {
            float reach = f.Reach + CharacterRadius;
            int x0, z0, x1, z1;
            ToCell(f.X - reach, f.Z - reach, out x0, out z0);
            ToCell(f.X + reach, f.Z + reach, out x1, out z1);
            for (int z = Math.Max(0, z0); z <= Math.Min(Count - 1, z1); z++)
            {
                for (int x = Math.Max(0, x0); x <= Math.Min(Count - 1, x1); x++)
                {
                    float cx, cz;
                    CellCenter(x, z, out cx, out cz);
                    if (f.Contains(cx, cz, CharacterRadius)) free[z * Count + x] = false;
                }
            }
        }

        /// <summary>Flood fill: the biggest connected open area is "reachable"; small closed pockets are not.</summary>
        void ComputeReachable()
        {
            int[] region = new int[free.Length];
            int best = 0, bestSize = 0, id = 0;
            Queue<int> q = new Queue<int>();
            for (int i = 0; i < free.Length; i++)
            {
                if (!free[i] || region[i] != 0) continue;
                id++;
                int size = 0;
                region[i] = id;
                q.Enqueue(i);
                while (q.Count > 0)
                {
                    int c = q.Dequeue();
                    size++;
                    int cx = c % Count, cz = c / Count;
                    for (int d = 0; d < 4; d++)
                    {
                        int nx = cx + (d == 0 ? 1 : d == 1 ? -1 : 0), nz = cz + (d == 2 ? 1 : d == 3 ? -1 : 0);
                        if (nx < 0 || nz < 0 || nx >= Count || nz >= Count) continue;
                        int n = nz * Count + nx;
                        if (free[n] && region[n] == 0) { region[n] = id; q.Enqueue(n); }
                    }
                }
                if (size > bestSize) { bestSize = size; best = id; }
            }
            for (int i = 0; i < free.Length; i++) reachable[i] = region[i] == best && best != 0;
        }

        // ---------------------------------------------------------------- queries

        public void ToCell(float x, float z, out int cx, out int cz)
        {
            cx = (int)Math.Floor((x + HalfSize) / Cell);
            cz = (int)Math.Floor((z + HalfSize) / Cell);
        }

        public void CellCenter(int cx, int cz, out float x, out float z)
        {
            x = -HalfSize + (cx + 0.5f) * Cell;
            z = -HalfSize + (cz + 0.5f) * Cell;
        }

        bool InGrid(int cx, int cz) { return cx >= 0 && cz >= 0 && cx < Count && cz < Count; }

        public bool IsFree(float x, float z)
        {
            int cx, cz; ToCell(x, z, out cx, out cz);
            return InGrid(cx, cz) && free[cz * Count + cx];
        }

        public bool IsReachable(float x, float z)
        {
            int cx, cz; ToCell(x, z, out cx, out cz);
            return InGrid(cx, cz) && reachable[cz * Count + cx];
        }

        /// <summary>Reachable and with "clearance" meters of reachable floor around (portal, spawns).</summary>
        public bool IsClear(float x, float z, float clearance)
        {
            if (!IsReachable(x, z)) return false;
            int steps = (int)Math.Ceiling(clearance / Cell);
            int cx, cz; ToCell(x, z, out cx, out cz);
            for (int dz = -steps; dz <= steps; dz++)
                for (int dx = -steps; dx <= steps; dx++)
                {
                    if ((dx * dx + dz * dz) * Cell * Cell > clearance * clearance) continue;
                    int nx = cx + dx, nz = cz + dz;
                    if (!InGrid(nx, nz) || !reachable[nz * Count + nx]) return false;
                }
            return true;
        }

        /// <summary>Random reachable point at least "margin" from the map edge with "clearance" around it.</summary>
        public bool RandomPoint(Random rng, float margin, float clearance, out float x, out float z)
        {
            float h = HalfSize - margin;
            for (int attempt = 0; attempt < 300; attempt++)
            {
                x = (float)(rng.NextDouble() * 2.0 - 1.0) * h;
                z = (float)(rng.NextDouble() * 2.0 - 1.0) * h;
                if (IsClear(x, z, clearance)) return true;
            }
            x = z = 0f;
            return false;
        }

        /// <summary>Like RandomPoint, but between minDistance and maxDistance from (fromX, fromZ).</summary>
        public bool RandomPointAround(Random rng, float fromX, float fromZ, float minDistance, float maxDistance, float margin, float clearance, out float x, out float z)
        {
            float h = HalfSize - margin;
            for (int attempt = 0; attempt < 400; attempt++)
            {
                double a = rng.NextDouble() * Math.PI * 2.0;
                double d = minDistance + rng.NextDouble() * (maxDistance - minDistance);
                x = fromX + (float)(Math.Cos(a) * d);
                z = fromZ + (float)(Math.Sin(a) * d);
                if (Math.Abs(x) > h || Math.Abs(z) > h) continue;
                if (IsClear(x, z, clearance)) return true;
            }
            x = z = 0f;
            return false;
        }

        /// <summary>
        /// True if a character can walk in a straight line between the two points (samples every half cell).
        /// ignoreEnds: meters at each end not checked (a character touching a wall stands on a "blocked" cell).
        /// </summary>
        public bool LineClear(float x0, float z0, float x1, float z1, float ignoreEnds = 0f)
        {
            float dx = x1 - x0, dz = z1 - z0;
            float len = (float)Math.Sqrt(dx * dx + dz * dz);
            int steps = Math.Max(1, (int)Math.Ceiling(len / (Cell * 0.5f)));
            for (int i = 0; i <= steps; i++)
            {
                float t = i / (float)steps;
                if (ignoreEnds > 0f && (t * len < ignoreEnds || (1f - t) * len < ignoreEnds)) continue;
                if (!IsFree(x0 + dx * t, z0 + dz * t)) return false;
            }
            return true;
        }

        /// <summary>Nearest free cell to a point (the character may be touching a wall), searching up to "radius" cells.</summary>
        bool NearestFree(int cx, int cz, int radius, out int fx, out int fz)
        {
            // "free" here means REACHABLE: a path to a closed pocket would search the whole map and fail
            fx = cx; fz = cz;
            if (InGrid(cx, cz) && reachable[cz * Count + cx]) return true;
            for (int r = 1; r <= radius; r++)
                for (int dz = -r; dz <= r; dz++)
                    for (int dx = -r; dx <= r; dx++)
                    {
                        if (Math.Abs(dx) != r && Math.Abs(dz) != r) continue;
                        int nx = cx + dx, nz = cz + dz;
                        if (InGrid(nx, nz) && reachable[nz * Count + nx]) { fx = nx; fz = nz; return true; }
                    }
            return false;
        }

        // ---------------------------------------------------------------- A*

        int[] cameFrom;
        float[] gScore;
        int[] stamp;
        int[] closed;
        int stampId;
        readonly MinHeap open = new MinHeap();
        readonly List<int> cells = new List<int>();

        /// <summary>
        /// A* path (8 directions, no cutting corners). Fills "path" with world points (x, z pairs) from the
        /// start to the goal, already simplified to the corners that matter. False if there is no way.
        /// </summary>
        public bool FindPath(float sx, float sz, float gx, float gz, List<float> path, int maxNodes = 120000)
        {
            path.Clear();
            int ax, az, bx, bz;
            ToCell(sx, sz, out ax, out az);
            ToCell(gx, gz, out bx, out bz);
            if (!NearestFree(ax, az, 8, out ax, out az) || !NearestFree(bx, bz, 12, out bx, out bz)) return false;

            int n = Count * Count;
            if (cameFrom == null) { cameFrom = new int[n]; gScore = new float[n]; stamp = new int[n]; closed = new int[n]; }
            stampId++;

            int start = az * Count + ax, goal = bz * Count + bx;
            open.Clear();
            stamp[start] = stampId; gScore[start] = 0f; cameFrom[start] = -1;
            open.Push(start, Heuristic(ax, az, bx, bz));
            int expanded = 0;
            bool found = false;

            while (open.Count > 0)
            {
                int c = open.Pop();
                if (closed[c] == stampId) continue;   // stale heap entry (a better one was already expanded)
                closed[c] = stampId;
                if (c == goal) { found = true; break; }
                if (++expanded > maxNodes) break;
                int cx = c % Count, cz = c / Count;
                for (int dz = -1; dz <= 1; dz++)
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        if (dx == 0 && dz == 0) continue;
                        int nx = cx + dx, nz = cz + dz;
                        if (!InGrid(nx, nz) || !free[nz * Count + nx]) continue;
                        if (dx != 0 && dz != 0 && (!free[cz * Count + nx] || !free[nz * Count + cx])) continue;   // no corner cutting
                        int nb = nz * Count + nx;
                        float g = gScore[c] + (dx != 0 && dz != 0 ? 1.4142f : 1f);
                        if (stamp[nb] == stampId && g >= gScore[nb]) continue;
                        stamp[nb] = stampId; gScore[nb] = g; cameFrom[nb] = c;
                        open.Push(nb, g + Heuristic(nx, nz, bx, bz));
                    }
            }
            if (!found) return false;

            cells.Clear();
            for (int c = goal; c >= 0; c = cameFrom[c]) cells.Add(c);
            cells.Reverse();

            // keep only the corners: skip cells while the straight line stays walkable
            int anchor = 0;
            AddCell(path, cells[0]);
            for (int i = 2; i < cells.Count; i++)
            {
                float x0, z0, x1, z1;
                CellCenter(cells[anchor] % Count, cells[anchor] / Count, out x0, out z0);
                CellCenter(cells[i] % Count, cells[i] / Count, out x1, out z1);
                if (!LineClear(x0, z0, x1, z1))
                {
                    anchor = i - 1;
                    AddCell(path, cells[anchor]);
                }
            }
            AddCell(path, cells[cells.Count - 1]);
            return true;
        }

        void AddCell(List<float> path, int c)
        {
            float x, z;
            CellCenter(c % Count, c / Count, out x, out z);
            path.Add(x);
            path.Add(z);
        }

        static float Heuristic(int ax, int az, int bx, int bz)
        {
            int dx = Math.Abs(ax - bx), dz = Math.Abs(az - bz);
            return Math.Max(dx, dz) + 0.4142f * Math.Min(dx, dz);
        }

        /// <summary>Small binary heap of (cell, priority) for A*.</summary>
        class MinHeap
        {
            readonly List<int> items = new List<int>();
            readonly List<float> keys = new List<float>();
            public int Count { get { return items.Count; } }

            public void Clear() { items.Clear(); keys.Clear(); }

            public void Push(int item, float key)
            {
                items.Add(item); keys.Add(key);
                int i = items.Count - 1;
                while (i > 0)
                {
                    int p = (i - 1) / 2;
                    if (keys[p] <= keys[i]) break;
                    Swap(i, p); i = p;
                }
            }

            public int Pop()
            {
                int top = items[0];
                int last = items.Count - 1;
                Swap(0, last);
                items.RemoveAt(last); keys.RemoveAt(last);
                int i = 0;
                while (true)
                {
                    int l = i * 2 + 1, r = l + 1, m = i;
                    if (l < items.Count && keys[l] < keys[m]) m = l;
                    if (r < items.Count && keys[r] < keys[m]) m = r;
                    if (m == i) break;
                    Swap(i, m); i = m;
                }
                return top;
            }

            void Swap(int a, int b)
            {
                int t = items[a]; items[a] = items[b]; items[b] = t;
                float k = keys[a]; keys[a] = keys[b]; keys[b] = k;
            }
        }
    }
}
