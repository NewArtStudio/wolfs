using System.Collections.Generic;
using UnityEngine;

namespace VampWolfs
{
    /// <summary>
    /// Worn gear of a character (10 slots, same system as the Space project). Design rule of this game:
    /// worn equipment is NEVER dropped on death — only the bag (Inventory) is at risk.
    /// Stats holds what the gear and attributes turn into in combat.
    /// </summary>
    public class Equipment : MonoBehaviour
    {
        public string Weapon = "-";
        public string Armor = "-";

        /// <summary>Average tier × 100 (shown as "Faixa" in the HUD).</summary>
        public int ItemPower;

        /// <summary>Slot id → worn piece.</summary>
        public readonly Dictionary<string, ItemInstance> Worn = new Dictionary<string, ItemInstance>();

        CharacterStats stats = new CharacterStats();

        public CharacterStats Stats
        {
            get { return stats; }
        }

        /// <summary>Text-only setup (kept for compatibility; Wear replaces it when gear is available).</summary>
        public void Set(string weapon, string armor, int itemPower)
        {
            Weapon = weapon;
            Armor = armor;
            ItemPower = itemPower;
        }

        /// <summary>Wears these pieces and recomputes the stats.</summary>
        public void Wear(GearCatalog catalog, IDictionary<string, ItemInstance> items)
        {
            Worn.Clear();
            foreach (KeyValuePair<string, ItemInstance> pair in items)
            {
                if (pair.Value != null) Worn[pair.Key] = pair.Value;
            }

            stats = CharacterStats.Compute(catalog, Worn);

            ItemInstance weapon;
            ItemInstance chest;
            Worn.TryGetValue("handR", out weapon);
            Worn.TryGetValue("chest", out chest);
            Weapon = weapon != null ? catalog.NameOf(weapon) : "Desarmado";
            Armor = chest != null ? catalog.NameOf(chest) : "Sem casaco";
            ItemPower = Mathf.RoundToInt(stats.PowerTier * 100f);
        }
    }
}
