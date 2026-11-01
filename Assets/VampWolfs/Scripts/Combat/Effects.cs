using UnityEngine;

namespace VampWolfs
{
    /// <summary>Tiny placeholder visual effects (expanding discs and puffs). Swap for VFX Graph later.</summary>
    public static class Effects
    {
        /// <summary>A flat disc on the ground that expands and disappears.</summary>
        public static void Burst(Vector3 position, float radius, Color color, float duration)
        {
            Vector3 groundPosition = new Vector3(position.x, 0.05f, position.z);
            GameObject go = Shapes.Create(PrimitiveType.Cylinder, null, "Burst", groundPosition, Vector3.one, color, true);
            go.AddComponent<TimedEffect>().Begin(
                new Vector3(radius * 0.6f, 0.01f, radius * 0.6f),
                new Vector3(radius * 2f, 0.01f, radius * 2f),
                Vector3.zero,
                duration);
        }

        /// <summary>A few small cubes flying up (sheep poof, pickups).</summary>
        public static void Puff(Vector3 position, Color color, int count)
        {
            for (int i = 0; i < count; i++)
            {
                Vector3 start = position + new Vector3(Random.Range(-0.4f, 0.4f), 0.6f, Random.Range(-0.4f, 0.4f));
                GameObject go = Shapes.Create(PrimitiveType.Cube, null, "Puff", start, Vector3.one * 0.25f, color, true);
                Vector3 velocity = new Vector3(Random.Range(-1.5f, 1.5f), Random.Range(2f, 4f), Random.Range(-1.5f, 1.5f));
                go.AddComponent<TimedEffect>().Begin(Vector3.one * 0.3f, Vector3.one * 0.05f, velocity, 0.6f);
            }
        }
    }

    /// <summary>Scales and moves an object over time, then destroys it.</summary>
    public class TimedEffect : MonoBehaviour
    {
        Vector3 startScale;
        Vector3 endScale;
        Vector3 velocity;
        float startTime;
        float duration;

        public void Begin(Vector3 fromScale, Vector3 toScale, Vector3 moveVelocity, float seconds)
        {
            startScale = fromScale;
            endScale = toScale;
            velocity = moveVelocity;
            duration = Mathf.Max(0.01f, seconds);
            startTime = Time.time;
            transform.localScale = startScale;
        }

        void Update()
        {
            float t = (Time.time - startTime) / duration;
            transform.localScale = Vector3.Lerp(startScale, endScale, t);
            transform.position += velocity * Time.deltaTime;

            if (t >= 1f)
            {
                Destroy(gameObject);
            }
        }
    }
}
