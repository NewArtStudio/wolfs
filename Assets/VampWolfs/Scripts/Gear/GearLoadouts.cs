using System.Collections.Generic;
using UnityEngine;

namespace VampWolfs
{
    /// <summary>Starter kit, invader gear and random drops, all built from the catalog.</summary>
    public static class GearLoadouts
    {
        public static readonly string[] VampirePriority = { "Int", "Vit", "Dex", "Str" };
        public static readonly string[] WerewolfPriority = { "Str", "Vit", "Dex", "Int" };
        public static readonly string[] HunterPriority = { "Dex", "Vit", "Str", "Int" };

        /// <summary>
        /// Starting equipment of each class (tier I): the vampire is a blood mage (Varinha + arcane clothes),
        /// the werewolf a brute (Machado de Carrasco + plate), the hunter a gunslinger (Pistola + leather).
        /// </summary>
        public static Dictionary<string, ItemInstance> StarterKit(GearCatalog catalog, string classId)
        {
            return ClassLoadout(catalog, classId, 1, 0);
        }

        /// <summary>Gear of a class at a tier (starter kits and the AI invader, which arrives with refine 0..4).</summary>
        public static Dictionary<string, ItemInstance> ClassLoadout(GearCatalog catalog, string classId, int tier, int refine)
        {
            tier = Mathf.Clamp(tier, 1, 8);
            if (classId == "vampire" && tier == 1 && refine == 0) return VampireStarter(catalog);
            Dictionary<string, ItemInstance> gear = new Dictionary<string, ItemInstance>();
            switch (classId)
            {
                case "werewolf":
                    {
                        string[] p = WerewolfPriority;
                        Wear(catalog, gear, "head", "head", "vanguard", tier, p, refine);
                        Wear(catalog, gear, "chest", "chest", "vanguard", tier, p, refine);
                        Wear(catalog, gear, "gloves", "gloves", "vanguard", tier, p, refine);
                        Wear(catalog, gear, "boots", "boots", "vanguard", tier, p, refine);
                        Wear(catalog, gear, "handR", "melee2H", "vanguard", tier, p, refine);
                        Wear(catalog, gear, "accMajor", "accMajor", "str", tier, p, 0);
                        Wear(catalog, gear, "acc1", "accMinor", "vit", tier, p, 0);
                        break;
                    }
                case "hunter":
                    {
                        string[] p = HunterPriority;
                        Wear(catalog, gear, "head", "head", "scout", tier, p, refine);
                        Wear(catalog, gear, "chest", "chest", "scout", tier, p, refine);
                        Wear(catalog, gear, "gloves", "gloves", "scout", tier, p, refine);
                        Wear(catalog, gear, "boots", "boots", "scout", tier, p, refine);
                        Wear(catalog, gear, "handR", "handR", "vanguard", tier, p, refine);
                        Wear(catalog, gear, "handL", "handL", "scout", tier, p, refine);
                        Wear(catalog, gear, "accMajor", "accMajor", "dex", tier, p, 0);
                        Wear(catalog, gear, "acc1", "accMinor", "vit", tier, p, 0);
                        break;
                    }
                default:
                    {
                        string[] p = VampirePriority;
                        Wear(catalog, gear, "head", "head", "arcane", tier, p, refine);
                        Wear(catalog, gear, "chest", "chest", "arcane", tier, p, refine);
                        Wear(catalog, gear, "gloves", "gloves", "arcane", tier, p, refine);
                        Wear(catalog, gear, "boots", "boots", "arcane", tier, p, refine);
                        Wear(catalog, gear, "handR", "handR", "arcane", tier, p, refine);
                        Wear(catalog, gear, "handL", "handL", "arcane", tier, p, refine);
                        Wear(catalog, gear, "accMajor", "accMajor", "int", tier, p, 0);
                        Wear(catalog, gear, "acc1", "accMinor", "vit", tier, p, 0);
                        break;
                    }
            }
            return gear;
        }

        /// <summary>
        /// Catalog model of this type + focus at this tier; if that tier doesn't have one (e.g. grimoires
        /// start at tier II), the closest tier that does. Null if the combination doesn't exist at all.
        /// </summary>
        public static EquipDef Find(GearCatalog catalog, string type, string focus, int tier)
        {
            EquipDef best = null;
            int bestDistance = int.MaxValue;

            foreach (EquipDef e in catalog.Equipment)
            {
                if (e.Type != type || e.Focus != focus) continue;
                int distance = Mathf.Abs(e.Tier - tier);
                if (distance < bestDistance)
                {
                    best = e;
                    bestDistance = distance;
                }
            }

            return best;
        }

        static void Wear(GearCatalog catalog, Dictionary<string, ItemInstance> loadout, string slot, string type, string focus, int tier, string[] priority, int refine)
        {
            EquipDef e = Find(catalog, type, focus, tier);
            if (e == null) return;
            ItemInstance item = ItemInstance.Create(e.Id, 1);
            item.Refine = e.Refinable ? refine : 0;
            GearUpgrades.AutoAllocate(e, item, priority);
            loadout[slot] = item;
        }

        /// <summary>Vampire blood mage: arcane tier I set, INT focus.</summary>
        public static Dictionary<string, ItemInstance> VampireStarter(GearCatalog catalog)
        {
            Dictionary<string, ItemInstance> gear = new Dictionary<string, ItemInstance>();
            string[] p = VampirePriority;
            Wear(catalog, gear, "head", "head", "arcane", 1, p, 0);
            Wear(catalog, gear, "chest", "chest", "arcane", 1, p, 0);
            Wear(catalog, gear, "gloves", "gloves", "arcane", 1, p, 0);
            Wear(catalog, gear, "boots", "boots", "arcane", 1, p, 0);
            Wear(catalog, gear, "handR", "handR", "arcane", 1, p, 0);
            Wear(catalog, gear, "handL", "handL", "arcane", 1, p, 0);
            Wear(catalog, gear, "accMajor", "accMajor", "int", 1, p, 0);
            Wear(catalog, gear, "acc1", "accMinor", "vit", 1, p, 0);
            return gear;
        }

        /// <summary>Werewolf bruiser for the invader AI: vanguard armor, two-handed melee, STR focus.</summary>
        public static Dictionary<string, ItemInstance> WerewolfLoadout(GearCatalog catalog, int tier)
        {
            tier = Mathf.Clamp(tier, 1, 8);
            int refine = Random.Range(0, 5);
            Dictionary<string, ItemInstance> gear = new Dictionary<string, ItemInstance>();
            string[] p = WerewolfPriority;
            Wear(catalog, gear, "head", "head", "vanguard", tier, p, refine);
            Wear(catalog, gear, "chest", "chest", "vanguard", tier, p, refine);
            Wear(catalog, gear, "gloves", "gloves", "vanguard", tier, p, refine);
            Wear(catalog, gear, "boots", "boots", "vanguard", tier, p, refine);
            Wear(catalog, gear, "handR", "melee2H", "vanguard", tier, p, refine);
            Wear(catalog, gear, "accMajor", "accMajor", "str", tier, p, 0);
            Wear(catalog, gear, "acc1", "accMinor", "vit", tier, p, 0);
            Wear(catalog, gear, "acc2", "accMinor", "str", tier, p, 0);
            return gear;
        }

        /// <summary>New save: vampire starter kit worn, a few spare pieces and upgrade materials to try the system.</summary>
        public static PlayerProfile NewVampireProfile(GearCatalog catalog)
        {
            PlayerProfile profile = new PlayerProfile();
            profile.AddCharacter(catalog, "vampire", "Vampiro");

            AddSpare(catalog, profile, "handR", "scout", 1);
            AddSpare(catalog, profile, "chest", "sacred", 1);
            AddSpare(catalog, profile, "accMinor", "dex", 1);

            AddMaterial(catalog, profile, "ore_refine_weapon", 6);
            AddMaterial(catalog, profile, "ore_refine_armor", 6);
            AddMaterial(catalog, profile, "crystal_str", 3);
            AddMaterial(catalog, profile, "crystal_dex", 3);
            AddMaterial(catalog, profile, "crystal_int", 3);
            AddMaterial(catalog, profile, "crystal_vit", 3);
            AddMaterial(catalog, profile, "key_card_weapon", 1);
            AddMaterial(catalog, profile, "key_card_armor", 1);
            AddMaterial(catalog, profile, "key_gem", 2);
            AddMaterial(catalog, profile, "gem_sapphire", 1);
            AddMaterial(catalog, profile, "gem_amber", 1);
            return profile;
        }

        static void AddSpare(GearCatalog catalog, PlayerProfile profile, string type, string focus, int tier)
        {
            EquipDef e = Find(catalog, type, focus, tier);
            if (e != null) profile.AddToStash(catalog, ItemInstance.Create(e.Id, 1));
        }

        static void AddMaterial(GearCatalog catalog, PlayerProfile profile, string id, int qty)
        {
            if (catalog.GetMaterial(id) != null) profile.AddToStash(catalog, ItemInstance.Create(id, qty));
        }

        // ---------------------------------------------------------------- drops

        static readonly string[] CommonMaterials = { "crystal_str", "crystal_dex", "crystal_int", "crystal_vit", "ore_refine_weapon", "ore_refine_armor" };
        static readonly string[] RareMaterials = { "key_card_weapon", "key_card_armor", "key_card_acc", "key_gem", "gem_ruby", "gem_emerald", "gem_sapphire", "gem_amber" };

        /// <summary>Random upgrade material (rare ones ~20% of the time).</summary>
        public static ItemInstance RandomMaterial(GearCatalog catalog)
        {
            string[] pool = Random.value < 0.2f ? RareMaterials : CommonMaterials;
            return ItemInstance.Create(pool[Random.Range(0, pool.Length)], 1);
        }

        /// <summary>Random equipment piece of a tier, with some points pre-filled.</summary>
        public static ItemInstance RandomEquipment(GearCatalog catalog, int tier)
        {
            List<EquipDef> pool = catalog.EquipmentOfTier(Mathf.Clamp(tier, 1, 8));
            if (pool.Count == 0) return null;
            EquipDef e = pool[Random.Range(0, pool.Count)];
            ItemInstance item = ItemInstance.Create(e.Id, 1);
            if (e.Refinable && Random.value < 0.3f) item.Refine = Random.Range(1, 4);
            return item;
        }
    }
}
