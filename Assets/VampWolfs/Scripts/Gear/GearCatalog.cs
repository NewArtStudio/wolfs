using System;
using System.Collections.Generic;
using UnityEngine;

namespace VampWolfs
{
    /// <summary>Numbers of the upgrade system (Resources/VampWolfs/gear.json → Rules).</summary>
    [Serializable]
    public class GearRulesData
    {
        public int RefineMax = 10;

        /// <summary>Bonus added by each refine level (index 0 = +1). Decreasing returns.</summary>
        public float[] RefineBonus = new float[0];

        /// <summary>Chance to BREAK the item when trying to go from +N to +N+1 (index 0 = +0 → +1).</summary>
        public float[] RefineBreakChance = new float[0];

        /// <summary>Reference power of a typical build at each tier (I..VIII). Keeps PvP time-to-kill similar across tiers.</summary>
        public float[] TierPower = new float[0];
    }

    [Serializable]
    public class SlotDef
    {
        public string Id;
        public string Name;
    }

    [Serializable]
    public class EquipTypeDef
    {
        public string Id;
        public string Name;
        public string[] Slots = new string[0];
        public bool TwoHand;

        /// <summary>weapon · armor · accessory (decides refine material and socket keys).</summary>
        public string Category;
    }

    /// <summary>One equipment model (the catalog entry). Each dropped piece is an ItemInstance pointing to it.</summary>
    [Serializable]
    public class EquipDef
    {
        public string Id;
        public string Name;
        public string Type;
        public int Tier = 1;

        /// <summary>vanguard / scout / arcane / sacred, or the attribute of accessories (str/dex/int/vit).</summary>
        public string Focus;
        public string ShieldKind;

        /// <summary>Weapon damage (0 for non-weapons).</summary>
        public float Dmg;

        /// <summary>Fixed base attributes of the model.</summary>
        public Attributes Attrs;

        /// <summary>Max of each attribute on this piece (base + player points) and the total points it accepts.</summary>
        public Attributes AttrCap;
        public int AttrTotal;

        public int CardSlots;
        public int GemSlot;
        public bool Refinable = true;

        /// <summary>Armour = % physical damage reduction. Evasion = speed + extra physical reduction. Ward = absorb shield (% of max HP).</summary>
        public float Armour;
        public float Evasion;
        public float Ward;

        /// <summary>Extra movement speed (0.05 = +5%). Boots only.</summary>
        public float Move;
    }

    /// <summary>Upgrade material (refine ore, attribute tonic, socket key, gem).</summary>
    [Serializable]
    public class MaterialDef
    {
        public const string Refine = "refine";
        public const string Enchant = "enchant";
        public const string UnlockCard = "unlockCard";
        public const string UnlockGem = "unlockGem";
        public const string Gem = "gem";

        public string Id;
        public string Name;
        public string Category;
        public string Kind;

        /// <summary>weapon · armor · accessory · any.</summary>
        public string Target;

        /// <summary>Attribute key for tonics and gems ("Str", "Dex", "Int", "Vit").</summary>
        public string Attr;

        /// <summary>Card socket opened by a key (1 = first).</summary>
        public int Slot;

        /// <summary>Attribute points given by a gem.</summary>
        public int Amount;

        public string Desc;
        public string Color;
    }

    /// <summary>
    /// The whole gear catalog (copied from the Space project and renamed for Victorian Europe).
    /// Loaded once from Resources/VampWolfs/gear.json.
    /// </summary>
    [Serializable]
    public class GearCatalog
    {
        public GearRulesData Rules = new GearRulesData();
        public List<SlotDef> Slots = new List<SlotDef>();
        public List<EquipTypeDef> Types = new List<EquipTypeDef>();
        public List<EquipDef> Equipment = new List<EquipDef>();
        public List<MaterialDef> Materials = new List<MaterialDef>();

        [NonSerialized] Dictionary<string, EquipTypeDef> typeById;
        [NonSerialized] Dictionary<string, EquipDef> equipById;
        [NonSerialized] Dictionary<string, MaterialDef> materialById;

        static GearCatalog instance;

        /// <summary>The loaded catalog (loads on first use).</summary>
        public static GearCatalog Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = Load();
                }

                return instance;
            }
        }

        static GearCatalog Load()
        {
            TextAsset asset = Resources.Load<TextAsset>("VampWolfs/gear");
            if (asset == null)
            {
                Debug.LogError("[VampWolfs] Resources/VampWolfs/gear.json not found. Gear system disabled.");
                GearCatalog empty = new GearCatalog();
                empty.BuildIndex();
                return empty;
            }

            return FromJson(asset.text);
        }

        public static GearCatalog FromJson(string json)
        {
            GearCatalog catalog = JsonUtility.FromJson<GearCatalog>(json);
            catalog.BuildIndex();
            return catalog;
        }

        public void BuildIndex()
        {
            typeById = new Dictionary<string, EquipTypeDef>();
            equipById = new Dictionary<string, EquipDef>();
            materialById = new Dictionary<string, MaterialDef>();

            foreach (EquipTypeDef t in Types) typeById[t.Id] = t;
            foreach (EquipDef e in Equipment) equipById[e.Id] = e;
            foreach (MaterialDef m in Materials) materialById[m.Id] = m;
        }

        public EquipDef GetEquip(string id)
        {
            EquipDef e;
            return id != null && equipById.TryGetValue(id, out e) ? e : null;
        }

        public MaterialDef GetMaterial(string id)
        {
            MaterialDef m;
            return id != null && materialById.TryGetValue(id, out m) ? m : null;
        }

        public EquipTypeDef TypeOf(EquipDef e)
        {
            EquipTypeDef t;
            return e != null && typeById.TryGetValue(e.Type, out t) ? t : null;
        }

        /// <summary>weapon · armor · accessory.</summary>
        public string CategoryOf(EquipDef e)
        {
            EquipTypeDef t = TypeOf(e);
            return t != null && !string.IsNullOrEmpty(t.Category) ? t.Category : "armor";
        }

        public bool IsTwoHand(EquipDef e)
        {
            EquipTypeDef t = TypeOf(e);
            return t != null && t.TwoHand;
        }

        /// <summary>Can this model be worn in this slot?</summary>
        public bool FitsSlot(EquipDef e, string slotId)
        {
            EquipTypeDef t = TypeOf(e);
            return t != null && Array.IndexOf(t.Slots, slotId) >= 0;
        }

        public string SlotName(string slotId)
        {
            foreach (SlotDef s in Slots)
            {
                if (s.Id == slotId) return s.Name;
            }

            return slotId;
        }

        /// <summary>Display name of an item (equipment with refine level, or material).</summary>
        public string NameOf(ItemInstance item)
        {
            if (item == null) return "-";
            EquipDef e = GetEquip(item.DefId);
            if (e != null) return (item.Refine > 0 ? "+" + item.Refine + " " : "") + e.Name;
            MaterialDef m = GetMaterial(item.DefId);
            if (m != null) return m.Name + (item.Qty > 1 ? " ×" + item.Qty : "");
            return item.DefId;
        }

        /// <summary>Equipment models of one tier (used by drops and the invader's loadout).</summary>
        public List<EquipDef> EquipmentOfTier(int tier)
        {
            List<EquipDef> list = new List<EquipDef>();
            foreach (EquipDef e in Equipment)
            {
                if (e.Tier == tier) list.Add(e);
            }

            return list;
        }
    }
}
