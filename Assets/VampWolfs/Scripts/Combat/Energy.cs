using UnityEngine;

namespace VampWolfs
{
    /// <summary>
    /// Battlerite-style energy: starts at 0, fills by dealing damage, split in 4 bars of 25.
    /// Spent on EX abilities / energy skills, or all at once on the Ultimate.
    /// </summary>
    public class Energy : MonoBehaviour
    {
        public const float Max = 100f;
        public const float BarSize = 25f;

        public float Current { get; private set; }

        public int FullBars
        {
            get { return Mathf.FloorToInt(Current / BarSize); }
        }

        public void Add(float amount)
        {
            Current = Mathf.Clamp(Current + amount, 0f, Max);
        }

        public bool TrySpend(float amount)
        {
            if (Current + 0.001f < amount)
            {
                return false;
            }

            Current = Mathf.Max(0f, Current - amount);
            return true;
        }
    }
}
