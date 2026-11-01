using System.Collections.Generic;
using UnityEngine;

namespace VampWolfs
{
    /// <summary>Damage numbers and short messages that float up in the world. Drawn by GameHUD.</summary>
    public static class FloatingText
    {
        public struct Entry
        {
            public Vector3 Position;
            public string Text;
            public Color Color;
            public float StartTime;
            public float Duration;
            public float Size;
        }

        const int MaxEntries = 60;

        public static readonly List<Entry> Entries = new List<Entry>();

        public static void Spawn(Vector3 worldPosition, string text, Color color, float duration = 0.9f, float size = 1f)
        {
            Entry entry = new Entry
            {
                Position = worldPosition + new Vector3(Random.Range(-0.3f, 0.3f), 0f, Random.Range(-0.2f, 0.2f)),
                Text = text,
                Color = color,
                StartTime = Time.time,
                Duration = duration,
                Size = size
            };

            Entries.Add(entry);
            if (Entries.Count > MaxEntries)
            {
                Entries.RemoveAt(0);
            }
        }

        public static void RemoveExpired()
        {
            for (int i = Entries.Count - 1; i >= 0; i--)
            {
                if (Time.time - Entries[i].StartTime > Entries[i].Duration)
                {
                    Entries.RemoveAt(i);
                }
            }
        }

        public static void Clear()
        {
            Entries.Clear();
        }
    }
}
