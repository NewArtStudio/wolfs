using System;
using System.Collections.Generic;
using UnityEngine;

namespace VampWolfs
{
    /// <summary>One skill as written in Resources/VampWolfs/skills.json (built by Tools/Data/build_skill_data.py).</summary>
    [Serializable]
    public class SkillDef
    {
        public string Id, Name, Desc, Kind, Source, Owner, Key, Fx, Color;
        public int ReqLevel = 1;

        public float Cooldown, CastTime, MoveMult, EnergyCost, Range, Speed, Radius;
        public int Count = 1;
        public float Spread;
        public bool Pierce;
        public float Reach, Angle;
        public bool AtAim, Basic;

        public float Damage;
        public string DmgType;
        public float Lifesteal, SlowAmount, SlowDuration, StunDuration, RootDuration, DotDps, DotDuration, Knockback, VulnAmount, VulnDuration;
        public float SelfHeal, SelfHaste, SelfHasteDuration, DR, DRDuration, ShieldPct, ShieldDuration, DmgBuff, DmgBuffDuration, Invuln;
        public bool Cleanse;
        public float MaxDistance, MinDistance, DashDuration, LandingRadius;
        public float CounterDuration, CounterStun, CounterHeal;
        public float ZoneDuration, ZoneHealPs, TrapLife, TrapArm;

        public float P_Damage, P_Hp, P_Move, P_Lifesteal, P_Cdr, P_Energy, P_DR, P_Heal, P_LowHpDamage, P_KillHeal, P_BasicDotDps, P_SlowedBonus, P_Regen;

        /// <summary>Class actives: +GrowPct per level on damage, DoT, heals and shields.</summary>
        public float GrowPct;

        public bool IsPassive { get { return Kind == "passive"; } }
        public bool IsClassSkill { get { return Source == "class"; } }
    }

    /// <summary>Playable class: base stats and the damage type its own skills use.</summary>
    [Serializable]
    public class ClassDef
    {
        public string Id, Name, Desc, DmgType, Color;
        public float Health = 160f, Speed = 6f;
    }

    /// <summary>Item family that owns a set of skills (e.g. "melee1H_vanguard" = every Sabre de Cavalaria).</summary>
    [Serializable]
    public class FamilyDef
    {
        public string Id, Name, Slot;
    }

    /// <summary>
    /// All skills of the game (Albion-style gear skills + class skills), loaded from Resources/VampWolfs/skills.json.
    /// Rules decided with the creator (nov/2026): the weapon gives LMB + RMB/Q/E (choose among options) + F (fixed
    /// ultimate); helmet R, chest Space, boots Shift (choose among options); the class gives 2 slotted actives
    /// (keys 1/2) and passives. Class skills go from level 1 to 10; characters from 1 to 50 (1 skill point per level).
    /// </summary>
    [Serializable]
    public class SkillCatalog
    {
        public const int MaxSkillLevel = 10;
        public const int MaxCharacterLevel = 50;
        public const int ActiveSlots = 2;

        /// <summary>Keys of the weapon kit in HUD order (LMB, RMB, Q, E, F).</summary>
        public static readonly string[] WeaponKeys = { "primary", "q1", "q2", "q3", "ult" };
        /// <summary>Armor slots that give a skill (the "perna" of the design = boots).</summary>
        public static readonly string[] ArmorSlots = { "head", "chest", "boots" };

        public List<ClassDef> Classes = new List<ClassDef>();
        public List<FamilyDef> Families = new List<FamilyDef>();
        public List<SkillDef> Skills = new List<SkillDef>();

        [NonSerialized] Dictionary<string, SkillDef> byId;
        [NonSerialized] bool loaded;

        static SkillCatalog instance;

        public static SkillCatalog Instance
        {
            get
            {
                if (instance == null)
                {
                    TextAsset json = Resources.Load<TextAsset>("VampWolfs/skills");
                    instance = json != null ? FromJson(json.text) : new SkillCatalog();
                    if (!instance.loaded) Debug.LogError("[VampWolfs] Resources/VampWolfs/skills.json not found or invalid: characters will have no skills.");
                }

                return instance;
            }
        }

        public bool IsLoaded { get { return loaded; } }

        public static SkillCatalog FromJson(string json)
        {
            SkillCatalog c = null;
            try { c = JsonUtility.FromJson<SkillCatalog>(json); }
            catch (Exception e) { Debug.LogError("[VampWolfs] skills.json: " + e.Message); }
            if (c == null) c = new SkillCatalog();
            c.BuildIndex();
            c.loaded = c.Skills.Count > 0 && c.Classes.Count > 0;
            return c;
        }

        void BuildIndex()
        {
            byId = new Dictionary<string, SkillDef>();
            foreach (SkillDef s in Skills) if (s != null && !string.IsNullOrEmpty(s.Id)) byId[s.Id] = s;
        }

        // ---------------------------------------------------------------- lookups

        public SkillDef Get(string id)
        {
            SkillDef s;
            return id != null && byId != null && byId.TryGetValue(id, out s) ? s : null;
        }

        public ClassDef Class(string id)
        {
            foreach (ClassDef c in Classes) if (c.Id == id) return c;
            return Classes.Count > 0 ? Classes[0] : new ClassDef { Id = "vampire", Name = "Vampiro", DmgType = "magic" };
        }

        public FamilyDef Family(string id)
        {
            foreach (FamilyDef f in Families) if (f.Id == id) return f;
            return null;
        }

        /// <summary>Skills of a family/class for one key ("q1", "head", "active"...), in data order (first = default).</summary>
        public List<SkillDef> Options(string owner, string key)
        {
            List<SkillDef> list = new List<SkillDef>();
            foreach (SkillDef s in Skills) if (s.Owner == owner && s.Key == key) list.Add(s);
            return list;
        }

        /// <summary>The 20 skills of a class (actives first, then passives), sorted by unlock level.</summary>
        public List<SkillDef> ClassSkills(string classId)
        {
            List<SkillDef> list = new List<SkillDef>();
            foreach (SkillDef s in Skills) if (s.Source == "class" && s.Owner == classId) list.Add(s);
            list.Sort((a, b) => a.IsPassive != b.IsPassive ? (a.IsPassive ? 1 : -1) : a.ReqLevel.CompareTo(b.ReqLevel));
            return list;
        }

        /// <summary>Skill family of an equipment piece: weapon = type_focus (e.g. "melee1H_vanguard"), armor = slot_focus.</summary>
        public static string FamilyOf(GearCatalog gear, EquipDef e)
        {
            if (e == null) return null;
            string type = e.Type;
            if (type == "head" || type == "chest" || type == "boots") return type + "_" + e.Focus;
            if (gear != null && gear.CategoryOf(e) == "weapon") return type + "_" + e.Focus;
            return null;
        }

        // ---------------------------------------------------------------- levels

        /// <summary>XP from level L to L+1 [PROTÓTIPO]: 50 + 15·(L−1) + (L−1)². Level 1→50 ≈ 58 mil XP.</summary>
        public static int XpToNext(int level)
        {
            if (level >= MaxCharacterLevel) return 0;
            int k = Mathf.Max(0, level - 1);
            return 50 + 15 * k + k * k;
        }

        /// <summary>Skill points earned at a character level: 2 at level 1, then +1 per level (51 at level 50).</summary>
        public static int PointsAtLevel(int level)
        {
            return Mathf.Clamp(level, 1, MaxCharacterLevel) + 1;
        }

        /// <summary>Multiplier of damage/heal/shield/DoT for a class active at a skill level (1 at level 1).</summary>
        public static float Grow(SkillDef s, int level)
        {
            return 1f + s.GrowPct * (Mathf.Clamp(level, 1, MaxSkillLevel) - 1);
        }

        /// <summary>Cooldown at a skill level: −3% per level after the first (class actives only).</summary>
        public static float CooldownAt(SkillDef s, int level)
        {
            if (!s.IsClassSkill) return s.Cooldown;
            return s.Cooldown * (1f - 0.03f * (Mathf.Clamp(level, 1, MaxSkillLevel) - 1));
        }
    }
}
