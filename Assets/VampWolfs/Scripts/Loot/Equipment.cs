using UnityEngine;

namespace VampWolfs
{
    /// <summary>
    /// Equipped gear. Design rule of this game: equipment is NEVER dropped on death —
    /// only the bag (Inventory) is at risk. Stats from gear are not applied yet in the prototype.
    /// </summary>
    public class Equipment : MonoBehaviour
    {
        public string Weapon = "-";
        public string Armor = "-";
        public int ItemPower;

        public void Set(string weapon, string armor, int itemPower)
        {
            Weapon = weapon;
            Armor = armor;
            ItemPower = itemPower;
        }
    }
}
