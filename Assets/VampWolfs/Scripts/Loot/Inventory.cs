using System.Collections.Generic;
using UnityEngine;

namespace VampWolfs
{
    /// <summary>Resources farmed during a run.</summary>
    public enum ItemType
    {
        Blood,
        Wool,
        MoonShard
    }

    /// <summary>
    /// The BAG: what was farmed in this run. This is the only thing at risk when you die.
    /// Equipment lives in the separate Equipment component and never drops.
    /// </summary>
    public class Inventory : MonoBehaviour
    {
        /// <summary>Stable display order for the HUD.</summary>
        public static readonly ItemType[] AllTypes = { ItemType.Blood, ItemType.Wool, ItemType.MoonShard };

        readonly Dictionary<ItemType, int> items = new Dictionary<ItemType, int>();

        public int TotalItems
        {
            get
            {
                int total = 0;
                foreach (KeyValuePair<ItemType, int> pair in items) total += pair.Value;
                return total;
            }
        }

        public int TotalValue
        {
            get { return ValueOf(items); }
        }

        public void Add(ItemType type, int amount)
        {
            if (amount <= 0) return;
            items[type] = Count(type) + amount;
        }

        public void AddAll(Dictionary<ItemType, int> other)
        {
            foreach (KeyValuePair<ItemType, int> pair in other)
            {
                Add(pair.Key, pair.Value);
            }
        }

        public int Count(ItemType type)
        {
            int value;
            return items.TryGetValue(type, out value) ? value : 0;
        }

        /// <summary>Empties the bag and returns what was inside.</summary>
        public Dictionary<ItemType, int> TakeAll()
        {
            Dictionary<ItemType, int> copy = Snapshot();
            items.Clear();
            return copy;
        }

        public Dictionary<ItemType, int> Snapshot()
        {
            return new Dictionary<ItemType, int>(items);
        }

        public static string DisplayName(ItemType type)
        {
            switch (type)
            {
                case ItemType.Blood: return "Sangue";
                case ItemType.Wool: return "Lã";
                case ItemType.MoonShard: return "Fragmento Lunar";
                default: return type.ToString();
            }
        }

        /// <summary>Placeholder "silver" value per item, used for the end-of-run summary.</summary>
        public static int UnitValue(ItemType type)
        {
            switch (type)
            {
                case ItemType.Blood: return 10;
                case ItemType.Wool: return 6;
                case ItemType.MoonShard: return 120;
                default: return 1;
            }
        }

        public static int ValueOf(Dictionary<ItemType, int> bag)
        {
            int total = 0;
            foreach (KeyValuePair<ItemType, int> pair in bag) total += pair.Value * UnitValue(pair.Key);
            return total;
        }
    }
}
