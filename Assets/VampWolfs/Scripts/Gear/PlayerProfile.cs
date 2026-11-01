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
    /// What the player owns between matches, saved to disk: the characters (each with class, level, skills and
    /// worn equipment, which never drops), the shared stash (safe storage at the Base) and farmed resources.
    /// Extraction moves the bag into the stash. The equipment methods below act on the selected character.
    /// </summary>
    [Serializable]
    public class PlayerProfile
    {
        const string FileName = "vampwolfs_profile.json";

        /// <summary>Old saves (one vampire, before nov/2026): moved into Characters on load, then left empty.</summary>
        public List<EquippedSlot> Equipped = new List<EquippedSlot>();

        /// <summary>Every character of the account (vampires, werewolves, bounty hunters).</summary>
        public List<CharacterSave> Characters = new List<CharacterSave>();
        /// <summary>Index of the selected character (the one that goes to the Farm).</summary>
        public int Active;

        /// <summary>Most characters an account can have [PROTÓTIPO].</summary>
        public const int MaxCharacters = 8;
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

        /// <summary>Saving is off when the catalog failed to load, so a broken build never overwrites real progress.</summary>
        [NonSerialized] public bool SavingEnabled = true;

        // ---------------------------------------------------------------- save / load

        public static PlayerProfile LoadOrCreate(GearCatalog catalog)
        {
            bool fileExists = File.Exists(FilePath);

            if (fileExists)
            {
                try
                {
                    PlayerProfile loaded = JsonUtility.FromJson<PlayerProfile>(File.ReadAllText(FilePath));
                    if (loaded != null)
                    {
                        if (catalog.IsLoaded)
                        {
                            loaded.Sanitize(catalog);
                        }
                        else
                        {
                            loaded.SavingEnabled = false; // catalog broken: don't touch the real save
                        }

                        return loaded;
                    }
                }
                catch (Exception e)
                {
                    Debug.LogWarning("[VampWolfs] Could not read profile: " + e.Message);
                }

                // Corrupt file: keep a backup instead of silently losing it.
                try
                {
                    File.Copy(FilePath, FilePath + ".bak", true);
                    Debug.LogWarning("[VampWolfs] Broken profile saved as " + FileName + ".bak; starting a new one.");
                }
                catch (Exception e)
                {
                    Debug.LogWarning("[VampWolfs] Could not back up profile: " + e.Message);
                }
            }

            PlayerProfile profile = GearLoadouts.NewVampireProfile(catalog);
            profile.SavingEnabled = catalog.IsLoaded;
            profile.Save();
            return profile;
        }

        public void Save()
        {
            if (!SavingEnabled)
            {
                return;
            }

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

        /// <summary>
        /// Cleans a loaded save: unknown items are dropped; worn pieces in a wrong/duplicate slot or blocked by
        /// a two-handed weapon go back to the stash (nothing the player owns is lost).
        /// </summary>
        void Sanitize(GearCatalog catalog)
        {
            if (Stash == null) Stash = new List<ItemInstance>();
            if (Equipped == null) Equipped = new List<EquippedSlot>();
            if (Characters == null) Characters = new List<CharacterSave>();

            Stash.RemoveAll(i => i == null || (catalog.GetEquip(i.DefId) == null && catalog.GetMaterial(i.DefId) == null));

            SkillCatalog sc = SkillCatalog.Instance;

            // Old save (one vampire): its worn gear becomes the first character.
            if (Characters.Count == 0)
            {
                CharacterSave vampire = CharacterSave.Create("vampire", "Vampiro");
                vampire.Equipped = Equipped;
                vampire.AutoAllocate(sc);
                Characters.Add(vampire);
                Active = 0;
            }
            else if (Equipped.Count > 0)
            {
                Stash.AddRange(Equipped.ConvertAll(x => x.Item).FindAll(x => x != null));   // never lose items
            }
            Equipped = new List<EquippedSlot>();

            Characters.RemoveAll(c => c == null);
            foreach (CharacterSave c in Characters)
            {
                c.Sanitize(sc);
                c.Equipped = SanitizeWorn(catalog, c.Equipped);
            }
            Active = Mathf.Clamp(Active, 0, Mathf.Max(0, Characters.Count - 1));
        }

        /// <summary>Worn pieces in a wrong/duplicate slot or blocked by a two-handed weapon go back to the stash.</summary>
        List<EquippedSlot> SanitizeWorn(GearCatalog catalog, List<EquippedSlot> worn)
        {
            if (worn == null) return new List<EquippedSlot>();
            List<EquippedSlot> valid = new List<EquippedSlot>();
            foreach (EquippedSlot s in worn)
            {
                if (s == null || s.Item == null) continue;
                EquipDef e = catalog.GetEquip(s.Item.DefId);
                if (e == null) continue;

                bool fits = catalog.FitsSlot(e, s.Slot) && !valid.Exists(v => v.Slot == s.Slot);
                if (fits) valid.Add(s);
                else Stash.Add(s.Item);
            }

            EquippedSlot right = valid.Find(v => v.Slot == "handR");
            EquippedSlot left = valid.Find(v => v.Slot == "handL");
            if (right != null && left != null && catalog.IsTwoHand(catalog.GetEquip(right.Item.DefId)))
            {
                valid.Remove(left);
                Stash.Add(left.Item);
            }

            return valid;
        }

        // ---------------------------------------------------------------- characters

        /// <summary>The selected character (creates a vampire if the list is somehow empty).</summary>
        public CharacterSave Current
        {
            get
            {
                if (Characters == null) Characters = new List<CharacterSave>();
                if (Characters.Count == 0) Characters.Add(CharacterSave.Create("vampire", "Vampiro"));
                Active = Mathf.Clamp(Active, 0, Characters.Count - 1);
                return Characters[Active];
            }
        }

        /// <summary>Equipment worn by the selected character.</summary>
        public List<EquippedSlot> Worn
        {
            get
            {
                CharacterSave c = Current;
                if (c.Equipped == null) c.Equipped = new List<EquippedSlot>();
                return c.Equipped;
            }
        }

        public void SelectCharacter(int index)
        {
            if (Characters != null && index >= 0 && index < Characters.Count) Active = index;
        }

        /// <summary>Creates a character with its class starter kit and starting skills, and selects it. Null = limit reached.</summary>
        public CharacterSave AddCharacter(GearCatalog catalog, string classId, string name)
        {
            if (Characters == null) Characters = new List<CharacterSave>();
            if (Characters.Count >= MaxCharacters) return null;
            SkillCatalog sc = SkillCatalog.Instance;
            if (string.IsNullOrEmpty(name)) name = sc.Class(classId).Name;
            CharacterSave c = CharacterSave.Create(classId, name.Length > 20 ? name.Substring(0, 20) : name);
            foreach (KeyValuePair<string, ItemInstance> pair in GearLoadouts.StarterKit(catalog, classId))
            {
                c.Equipped.Add(new EquippedSlot { Slot = pair.Key, Item = pair.Value });
            }
            c.AutoAllocate(sc);
            Characters.Add(c);
            Active = Characters.Count - 1;
            return c;
        }

        // ---------------------------------------------------------------- equipment

        public Dictionary<string, ItemInstance> EquippedMap()
        {
            Dictionary<string, ItemInstance> map = new Dictionary<string, ItemInstance>();
            foreach (EquippedSlot s in Worn)
            {
                if (s.Item != null) map[s.Slot] = s.Item;
            }

            return map;
        }

        public ItemInstance GetEquipped(string slot)
        {
            foreach (EquippedSlot s in Worn)
            {
                if (s.Slot == slot) return s.Item;
            }

            return null;
        }

        void SetEquipped(string slot, ItemInstance item)
        {
            Worn.RemoveAll(s => s.Slot == slot);
            if (item != null) Worn.Add(new EquippedSlot { Slot = slot, Item = item });
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
                foreach (EquippedSlot s in Worn)
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
                AddToStash(catalog, item.CloneWithQty(item.Qty)); // copy: the bag and the stash never share objects
            }
        }
    }
}
