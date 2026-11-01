using UnityEngine;

namespace VampWolfs
{
    /// <summary>
    /// Helpers to build placeholder art from Unity primitives.
    /// Replace with real models later without touching gameplay code.
    /// </summary>
    public static class Shapes
    {
        /// <summary>Decorative primitive (collider removed). Position is local to the parent.</summary>
        public static GameObject Create(PrimitiveType type, Transform parent, string name, Vector3 localPosition, Vector3 localScale, Color color, bool unlit = false)
        {
            GameObject go = GameObject.CreatePrimitive(type);
            go.name = name;

            if (parent != null)
            {
                go.transform.SetParent(parent, false);
            }

            go.transform.localPosition = localPosition;
            go.transform.localScale = localScale;

            Collider col = go.GetComponent<Collider>();
            if (col != null)
            {
                // Disable first: Destroy is deferred to the end of the frame, and a live collider
                // at the spawn pose would push characters for one frame.
                col.enabled = false;
                Object.Destroy(col);
            }

            Renderer renderer = go.GetComponent<Renderer>();
            renderer.sharedMaterial = unlit ? MaterialLibrary.Unlit(color) : MaterialLibrary.Lit(color);
            if (unlit)
            {
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }

            return go;
        }

        /// <summary>Solid primitive that blocks movement AND projectiles (line of sight).</summary>
        public static GameObject Obstacle(PrimitiveType type, Transform parent, string name, Vector3 localPosition, Vector3 localScale, Color color, float yRotation = 0f)
        {
            GameObject go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localRotation = Quaternion.Euler(0f, yRotation, 0f);
            go.transform.localScale = localScale;
            go.GetComponent<Renderer>().sharedMaterial = MaterialLibrary.Lit(color);
            go.AddComponent<Obstacle>();
            return go;
        }

        /// <summary>Invisible blocking box (map borders).</summary>
        public static GameObject InvisibleWall(Transform parent, Vector3 center, Vector3 size)
        {
            GameObject go = new GameObject("InvisibleWall");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = center;
            BoxCollider box = go.AddComponent<BoxCollider>();
            box.size = size;
            go.AddComponent<Obstacle>();
            return go;
        }
    }
}
