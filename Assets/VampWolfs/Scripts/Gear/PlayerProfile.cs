using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace VampWolfs
{
    /// <summary>One worn piece (JsonUtility can't save dictionaries).</summary>
    [Serializable]
    public class EquippedSlot
    {
        public string Slot;
        public ItemInstance Item;
    }

    /// <summary>
    /// What the player owns between matches, saved to disk: worn equipment (never drops), the stash
    /// (safe storage at the Base) and farmed resources. Extraction moves the bag into the stash.
    /// </summary>
    [Serializable]
    public class PlayerProfile
    {
        const string FileName = "vampwolfs_profile.json";

        public List<EquippedSlot> Equipped = new List<EquippedSlot>();
        public List<ItemInstance> Stash = new List<ItemInstance>();
        public int Blood;
        public int Wool;
        public int MoonShard;
        public int Runs;
        public int Extractions;

        static string FilePath
        {
            get { return Path.Combine(Application.persistentDataPath, FileName); }
        }

        // ---------------------------------------------------------------- save / load

        public static PlayerProfile LoadOrCreate(GearCatalog catalog)
        {
            try
            {
                if (File.Exists(FilePath))
                {
                    PlayerProfile loaded = JsonUtility.FromJson<PlayerProfile>(File.ReadAllText(FilePath));
                    if (loaded != null)
                    {
                        loaded.RemoveUnknownItems(catalog);
                        return loaded;
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning("[VampWolfs] Could not read profile, starting a new one: " + e.Message);
            }

            PlayerProfile profile = GearLoadouts.NewVampireProfile(catalog);
            profile.Save();
            return profile;
        }

        public void Save()
        {
            try
            {
                File.WriteAllText(FilePath, JsonUtility.ToJson(this, true));
            }
            catch (Exception e)
            {
                Debug.LogWarning("[VampWolfs] Could not save profile: " + e.Message);
            }
        }

        /// <summary>Deletes the save and starts over with the starter kit.</summary>
        public static PlayerProfile Reset(GearCatalog catalog)
        {
            PlayerProfile profile = GearLoadouts.NewVampireProfile(catalog);
            profile.Save();
            return profile;
        }

        void RemoveUnknownItems(GearCatalog catalog)
        {
            Stash.RemoveAll(i => i == null || (catalog.GetEquip(i.DefId) == null && catalog.GetMaterial(i.DefId) == null));
            Equipped.RemoveAll(s => s == null || s.Item == null || catalog.GetEquip(s.Item.DefId) == null);
        }

        // ---------------------------------------------------------------- equipment

        public Dictionary<string, ItemInstance> EquippedMap()
        {
            Dictionary<string, ItemInstance> map = new Dictionary<string, ItemInstance>();
            foreach (EquippedSlot s in Equipped)
            {
                if (s.Item != null) map[s.Slot] = s.Item;
            }

            return map;
        }

        public ItemInstance GetEquipped(string slot)
        {
            foreach (EquippedSlot s in Equipped)
            {
                if (s.Slot == slot) return s.Item;
            }

            return null;
        }

        void SetEquipped(string slot, ItemInstance item)
        {
            Equipped.RemoveAll(s => s.Slot == slot);
            if (item != null) Equipped.Add(new EquippedSlot { Slot = slot, Item = item });
        }

        /// <summary>Best slot for a piece: a free compatible slot, else the first compatible one.</summary>
        public string SlotFor(GearCatalog catalog, EquipDef e)
        {
            EquipTypeDef type = catalog.TypeOf(e);
            if (type == null || type.Slots.Length == 0) return null;

            foreach (string slot in type.Slots)
            {
                if (GetEquipped(slot) == null) return slot;
            }

            return type.Slots[0];
        }

        /// <summary>Wears a piece from the stash. Returns an error message, or null on success.</summary>
        public string Equip(GearCatalog catalog, string uid, string slot)
        {
            ItemInstance item = Stash.Find(i => i.Uid == uid);
            EquipDef e = item != null ? catalog.GetEquip(item.DefId) : null;
            if (e == null) return "Escolha um equipamento do baú";
            if (slot == null) slot = SlotFor(catalog, e);
            if (!catalog.FitsSlot(e, slot)) return e.Name + " não vai em " + catalog.SlotName(slot);

            ItemInstance rightHand = GetEquipped("handR");
            if (slot == "handL" && rightHand != null && catalog.IsTwoHand(catalog.GetEquip(rightHand.DefId)))
            {
                return "A arma de duas mãos ocupa a mão esquerda";
            }

            Stash.Remove(item);

            ItemInstance previous = GetEquipped(slot);
            if (previous != null) Stash.Add(previous);

            if (slot == "handR" && catalog.IsTwoHand(e))
            {
                ItemInstance offHand = GetEquipped("handL");
                if (offHand != null)
                {
                    Stash.Add(offHand);
                    SetEquipped("handL", null);
                }
            }

            SetEquipped(slot, item);
            return null;
        }

        public void Unequip(string slot)
        {
            ItemInstance item = GetEquipped(slot);
            if (item == null) return;
            SetEquipped(slot, null);
            Stash.Add(item);
        }

        // ---------------------------------------------------------------- stash

        /// <summary>Adds to the stash; materials stack with the same kind.</summary>
        public void AddToStash(GearCatalog catalog, ItemInstance item)
        {
            if (item == null) return;

            if (catalog.GetMaterial(item.DefId) != null)
            {
                ItemInstance stack = Stash.Find(i => i.DefId == item.DefId);
                if (stack != null)
                {
                    stack.Qty += item.Qty;
                    return;
                }
            }

            Stash.Add(item);
        }

        /// <summary>Uses one unit of a stash material on a worn or stashed piece.</summary>
        public UpgradeResult ApplyMaterial(GearCatalog catalog, string materialUid, string targetUid, float roll01)
        {
            ItemInstance matItem = Stash.Find(i => i.Uid == materialUid);
            MaterialDef mat = matItem != null ? catalog.GetMaterial(matItem.DefId) : null;
            if (mat == null) return UpgradeResult.Fail("Escolha um material do baú");

            string wornSlot = null;
            ItemInstance target = Stash.Find(i => i.Uid == targetUid);
            if (target == null)
            {
                foreach (EquippedSlot s in Equipped)
                {
                    if (s.Item != null && s.Item.Uid == targetUid)
                    {
                        target = s.Item;
                        wornSlot = s.Slot;
                    }
                }
            }

            EquipDef e = target != null ? catalog.GetEquip(target.DefId) : null;
            if (e == null) return UpgradeResult.Fail("Escolha o equipamento que vai receber o material");

            string oldGem = target.Gem;
            UpgradeResult result = GearUpgrades.ApplyToItem(catalog, mat, e, target, roll01);
            if (!result.Ok && !result.Broke)
            {
                return result; // invalid use: nothing consumed
            }

            matItem.Qty--;
            if (matItem.Qty <= 0) Stash.Remove(matItem);

            if (result.Broke)
            {
                if (wornSlot != null) SetEquipped(wornSlot, null);
                else Stash.Remove(target);
            }
            else if (mat.Kind == MaterialDef.Gem && !string.IsNullOrEmpty(oldGem))
            {
                AddToStash(catalog, ItemInstance.Create(oldGem, 1)); // old gem goes back to the stash
            }

            return result;
        }

        /// <summary>Extraction: everything in the bag becomes safe in the stash.</summary>
        public void DepositBag(GearCatalog catalog, Inventory bag)
        {
            Blood += bag.Count(ItemType.Blood);
            Wool += bag.Count(ItemType.Wool);
            MoonShard += bag.Count(ItemType.MoonShard);
            foreach (ItemInstance item in bag.Items)
            {
                AddToStash(catalog, item);
            }
        }
    }
}
