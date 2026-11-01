using System;
using System.Collections.Generic;
using UnityEngine;

namespace VampWolfs
{
    /// <summary>Level of one learned class skill (JsonUtility can't save dictionaries).</summary>
    [Serializable]
    public class SkillLevelEntry
    {
        public string Id;
        public int Level;
    }

    /// <summary>Which option the player picked for one gear key ("melee1H_vanguard:q2" → skill id).</summary>
    [Serializable]
    public class SkillChoice
    {
        public string Key;
        public string SkillId;
    }

    /// <summary>
    /// One playable character of the account (nov/2026): class, level 1..50, XP, learned class skills (1..10),
    /// the 2 actives on keys 1/2, the option chosen for each gear key and the equipment it wears.
    /// The stash and the resources are shared by every character (like Albion).
    /// </summary>
    [Serializable]
    public class CharacterSave
    {
        public string Id;
        public string Name;
        public string ClassId = "vampire";
        public int Level = 1;
        public int Xp;
        public List<SkillLevelEntry> Skills = new List<SkillLevelEntry>();
        public List<string> Actives = new List<string>();
        public List<SkillChoice> Choices = new List<SkillChoice>();
        public List<EquippedSlot> Equipped = new List<EquippedSlot>();

        public static CharacterSave Create(string classId, string name)
        {
            return new CharacterSave
            {
                Id = Guid.NewGuid().ToString("N").Substring(0, 12),
                Name = name,
                ClassId = classId,
            };
        }

        // ---------------------------------------------------------------- skills

        public int SkillLevel(string id)
        {
            if (string.IsNullOrEmpty(id)) return 0;
            foreach (SkillLevelEntry e in Skills) if (e.Id == id) return e.Level;
            return 0;
        }

        public int PointsSpent
        {
            get { int n = 0; foreach (SkillLevelEntry e in Skills) n += Mathf.Max(0, e.Level); return n; }
        }

        public int PointsTotal { get { return SkillCatalog.PointsAtLevel(Level); } }
        public int PointsFree { get { return Mathf.Max(0, PointsTotal - PointsSpent); } }

        /// <summary>Why the skill can't get one more level (null = it can).</summary>
        public string CannotLearn(SkillDef s)
        {
            if (s == null || s.Owner != ClassId) return "Habilidade de outra classe";
            if (Level < s.ReqLevel) return "Libera no nível " + s.ReqLevel;
            if (SkillLevel(s.Id) >= SkillCatalog.MaxSkillLevel) return "Nível máximo";
            if (PointsFree <= 0) return "Sem pontos livres";
            return null;
        }

        /// <summary>+1 level in a class skill. A newly learned active goes to a free key (1 or 2).</summary>
        public bool Learn(SkillDef s)
        {
            if (CannotLearn(s) != null) return false;
            SkillLevelEntry e = Skills.Find(x => x.Id == s.Id);
            if (e == null) { e = new SkillLevelEntry { Id = s.Id, Level = 0 }; Skills.Add(e); }
            e.Level++;
            if (!s.IsPassive && e.Level == 1)
            {
                for (int i = 0; i < SkillCatalog.ActiveSlots; i++)
                {
                    if (string.IsNullOrEmpty(ActiveAt(i))) { SetActive(i, s.Id); break; }
                }
            }
            return true;
        }

        /// <summary>−1 level (refund one point). Level 0 removes it from keys 1/2.</summary>
        public bool Unlearn(SkillDef s)
        {
            SkillLevelEntry e = s != null ? Skills.Find(x => x.Id == s.Id) : null;
            if (e == null || e.Level <= 0) return false;
            e.Level--;
            if (e.Level <= 0)
            {
                Skills.Remove(e);
                for (int i = 0; i < Actives.Count; i++) if (Actives[i] == s.Id) Actives[i] = "";
            }
            return true;
        }

        /// <summary>Gives every point back.</summary>
        public void ResetSkills()
        {
            Skills.Clear();
            Actives.Clear();
        }

        public string ActiveAt(int i)
        {
            return i >= 0 && i < Actives.Count ? Actives[i] : null;
        }

        /// <summary>Puts a learned active on key 1 (i = 0) or 2 (i = 1); if it was on the other key, they swap.</summary>
        public void SetActive(int i, string id)
        {
            while (Actives.Count < SkillCatalog.ActiveSlots) Actives.Add("");
            if (i < 0 || i >= SkillCatalog.ActiveSlots) return;
            if (string.IsNullOrEmpty(id)) { Actives[i] = ""; return; }
            int other = Actives.IndexOf(id);
            if (other >= 0 && other != i) Actives[other] = Actives[i];
            Actives[i] = id ?? "";
        }

        public string ChoiceFor(string family, string key)
        {
            string k = family + ":" + key;
            foreach (SkillChoice c in Choices) if (c.Key == k) return c.SkillId;
            return null;
        }

        public void SetChoice(string family, string key, string skillId)
        {
            string k = family + ":" + key;
            SkillChoice c = Choices.Find(x => x.Key == k);
            if (c == null) Choices.Add(new SkillChoice { Key = k, SkillId = skillId });
            else c.SkillId = skillId;
        }

        /// <summary>
        /// Spends the free points automatically (new characters and the AI invader): first the first two actives,
        /// then alternating actives and passives by unlock order, up to level 10 each.
        /// </summary>
        public void AutoAllocate(SkillCatalog sc)
        {
            List<SkillDef> all = sc.ClassSkills(ClassId);
            List<SkillDef> actives = all.FindAll(s => !s.IsPassive);
            List<SkillDef> passives = all.FindAll(s => s.IsPassive);
            if (actives.Count > 0) Learn(actives[0]);
            if (actives.Count > 1) Learn(actives[1]);
            int guard = 0;
            while (PointsFree > 0 && guard++ < 400)
            {
                bool any = false;
                // the 2 slotted actives first, then the passives, then the rest
                foreach (SkillDef s in actives) if (s.Id == ActiveAt(0) || s.Id == ActiveAt(1)) any |= Learn(s);
                foreach (SkillDef s in passives) any |= Learn(s);
                if (!any) foreach (SkillDef s in actives) any |= Learn(s);
                if (!any) break;
            }
        }

        // ---------------------------------------------------------------- experience

        /// <summary>Adds XP; returns how many levels were gained (max level 50).</summary>
        public int AddXp(int amount)
        {
            if (amount <= 0 || Level >= SkillCatalog.MaxCharacterLevel) return 0;
            Xp += amount;
            int gained = 0;
            while (Level < SkillCatalog.MaxCharacterLevel && Xp >= SkillCatalog.XpToNext(Level))
            {
                Xp -= SkillCatalog.XpToNext(Level);
                Level++;
                gained++;
            }

            if (Level >= SkillCatalog.MaxCharacterLevel) Xp = 0;
            return gained;
        }

        // ---------------------------------------------------------------- cleanup

        /// <summary>Fixes a loaded record (unknown skills, levels out of range, too many points after a data change).</summary>
        public void Sanitize(SkillCatalog sc)
        {
            if (string.IsNullOrEmpty(Id)) Id = Guid.NewGuid().ToString("N").Substring(0, 12);
            if (string.IsNullOrEmpty(Name)) Name = sc.Class(ClassId).Name;
            if (Skills == null) Skills = new List<SkillLevelEntry>();
            if (Actives == null) Actives = new List<string>();
            if (Choices == null) Choices = new List<SkillChoice>();
            if (Equipped == null) Equipped = new List<EquippedSlot>();
            Level = Mathf.Clamp(Level, 1, SkillCatalog.MaxCharacterLevel);
            Xp = Mathf.Max(0, Xp);
            if (!sc.IsLoaded) return;   // without the data nothing can be checked: keep as is

            Skills.RemoveAll(e => e == null || sc.Get(e.Id) == null || sc.Get(e.Id).Owner != ClassId || e.Level <= 0);
            foreach (SkillLevelEntry e in Skills) e.Level = Mathf.Min(e.Level, SkillCatalog.MaxSkillLevel);
            if (PointsSpent > PointsTotal) ResetSkills();   // data changed: give the points back instead of cheating
            for (int i = 0; i < Actives.Count; i++)
            {
                SkillDef s = sc.Get(Actives[i]);
                if (s == null || s.IsPassive || SkillLevel(s.Id) <= 0) Actives[i] = "";
            }
            Choices.RemoveAll(c => c == null || sc.Get(c.SkillId) == null);
        }
    }
}
