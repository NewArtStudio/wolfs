using System.Collections.Generic;
using UnityEngine;

namespace VampWolfs
{
    /// <summary>
    /// "GPS" for AI characters on the procedural map: straight line when the way is clear, otherwise an A*
    /// path (WalkGrid) around barns and fences. One per AI character (it keeps that character's path).
    /// Used by WerewolfAI (invader) and PeasantAI.
    /// </summary>
    public class NavAgent
    {
        readonly List<float> path = new List<float>();   // x,z pairs
        int pathIndex;
        float nextPathTime;
        Vector3 pathGoal;

        /// <summary>
        /// Direction from "pos" toward "goal". "straight" is returned when there is no map grid or no path.
        /// Recomputes at most every 0.6 s (0.3 s if the goal moved a lot); after a failure waits 1 s.
        /// </summary>
        public Vector3 Direction(Vector3 pos, Vector3 goal, Vector3 straight)
        {
            MatchManager match = MatchManager.Instance;
            WalkGrid grid = match != null ? match.Grid : null;
            if (grid == null || grid.LineClear(pos.x, pos.z, goal.x, goal.z, 1f))
            {
                path.Clear();
                return straight;
            }

            bool goalMoved = (goal - pathGoal).sqrMagnitude > 9f;
            if (Time.time >= nextPathTime || (goalMoved && Time.time >= nextPathTime - 0.3f))
            {
                nextPathTime = Time.time + 0.6f;
                pathGoal = goal;
                pathIndex = 0;
                if (!grid.FindPath(pos.x, pos.z, goal.x, goal.z, path))
                {
                    path.Clear();
                    nextPathTime = Time.time + 1f;
                }
            }
            if (path.Count == 0)
            {
                return straight;
            }

            // skip corners already reached, and jump ahead while the next corner is in plain sight
            int corners = path.Count / 2;
            while (pathIndex < corners - 1)
            {
                float cx = path[pathIndex * 2], cz = path[pathIndex * 2 + 1];
                float nx = path[pathIndex * 2 + 2], nz = path[pathIndex * 2 + 3];
                bool reached = (cx - pos.x) * (cx - pos.x) + (cz - pos.z) * (cz - pos.z) < 0.6f;
                if (reached || grid.LineClear(pos.x, pos.z, nx, nz, 0.8f)) pathIndex++;
                else break;
            }

            if (pathIndex >= corners) return straight;
            Vector3 to = new Vector3(path[pathIndex * 2] - pos.x, 0f, path[pathIndex * 2 + 1] - pos.z);
            return to.sqrMagnitude > 0.0001f ? to.normalized : straight;
        }

        public void Clear()
        {
            path.Clear();
        }
    }
}
