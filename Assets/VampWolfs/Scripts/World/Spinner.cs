using UnityEngine;

namespace VampWolfs
{
    /// <summary>Turns its object around its local Z axis forever (windmill blades). Decoration only.</summary>
    public class Spinner : MonoBehaviour
    {
        public float DegreesPerSecond = 30f;

        void Update()
        {
            transform.Rotate(0f, 0f, DegreesPerSecond * Time.deltaTime, Space.Self);
        }
    }
}
