using System;

namespace VampWolfs
{
    /// <summary>Result of checking or applying an upgrade material.</summary>
    public struct UpgradeResult
    {
        public bool Ok;

        /// <summary>Preview ("Força 3 → 4", "+4 → +5 · 20% de chance de QUEBRAR") or the reason it can't be used.</summary>
        public string Message;

        /// <summary>The refine failed and the item was destroyed.</summary>
        public bool Broke;

        public static UpgradeResult Fail(string message)
        {
            return new UpgradeResult { Ok = false, Message = message };
        }

        public static UpgradeResult Success(string message)
        {
            return new UpgradeResult { Ok = true, Message = message };
        }
    }

    /// <summary>
    /// Equipment upgrades, same rules as the Space project (Progression/ItemUpgrades.cs):
    /// refine +N (weapon and armor use different materials; accessories don't refine), tonics +1 to an
    /// attribute up to the piece's cap and total, keys open card/gem sockets, gems go in an open gem socket.
    /// Changes for VampWolfs (creator's decision): refine has decreasing returns and, from +5 on, a failed
    /// refine BREAKS the item; gems give attribute points instead of Space's threat aura.
    /// </summary>
    public static class GearUpgrades
    {
        /// <summary>Attributes of the piece now: model base + points the player added.</summary>
        public static Attributes CurrentAttributes(EquipDef e, ItemInstance item)
        {
            Attributes a = e.Attrs;
            if (item != null) a.Add(item.Alloc);
            return a;
        }

        /// <summary>Attribute bonus of the gem in the socket (goes above the equipment cap).</summary>
        public static Attributes GemBonus(GearCatalog catalog, ItemInstance item)
        {
            Attributes bonus = new Attributes();
            if (item == null || !item.GemOpen || string.IsNullOrEmpty(item.Gem)) return bonus;
            MaterialDef gem = catalog.GetMaterial(item.Gem);
            if (gem != null && !string.IsNullOrEmpty(gem.Attr)) bonus.Set(gem.Attr, gem.Amount);
            return bonus;
        }

        /// <summary>Points still free on the piece (total of the model − base − already added).</summary>
        public static int FreePoints(EquipDef e, ItemInstance item)
        {
            return Math.Max(0, e.AttrTotal - (int)CurrentAttributes(e, item).Sum);
        }

        /// <summary>How much more fits in this attribute (its cap and the piece's free points).</summary>
        public static int RoomIn(EquipDef e, ItemInstance item, string attr)
        {
            int byCap = (int)(e.AttrCap.Get(attr) - CurrentAttributes(e, item).Get(attr));
            return Math.Max(0, Math.Min(byCap, FreePoints(e, item)));
        }

        /// <summary>Chance (0..1) that refining from +level to +level+1 breaks the item.</summary>
        public static float BreakChance(GearRulesData rules, int level)
        {
            return rules.RefineBreakChance != null && level >= 0 && level < rules.RefineBreakChance.Length ? rules.RefineBreakChance[level] : 0f;
        }

        static bool Fits(string target, string category)
        {
            return string.IsNullOrEmpty(target) || target == "any" || target == category;
        }

        static string CategoryName(string category)
        {
            return category == "weapon" ? "arma" : category == "accessory" ? "joia" : "armadura";
        }

        /// <summary>Can the material be used on this piece? If yes, Message is a preview of the change.</summary>
        public static UpgradeResult Check(GearCatalog catalog, MaterialDef mat, EquipDef e, ItemInstance item)
        {
            if (mat == null) return UpgradeResult.Fail("Isso não é um material de melhoria");
            if (e == null || item == null) return UpgradeResult.Fail(mat.Name + " só funciona em equipamentos");

            string category = catalog.CategoryOf(e);
            if (!Fits(mat.Target, category)) return UpgradeResult.Fail(mat.Name + " é para " + CategoryName(mat.Target) + ", não para " + CategoryName(category));

            GearRulesData rules = catalog.Rules;

            switch (mat.Kind)
            {
                case MaterialDef.Refine:
                    {
                        if (!e.Refinable) return UpgradeResult.Fail(e.Name + " não pode ser refinado");
                        if (item.Refine >= rules.RefineMax) return UpgradeResult.Fail(e.Name + " já está no refino máximo (+" + rules.RefineMax + ")");
                        float chance = BreakChance(rules, item.Refine);
                        string risk = chance > 0f ? " · " + Math.Round(chance * 100f) + "% de chance de QUEBRAR" : " · seguro";
                        return UpgradeResult.Success("+" + item.Refine + " → +" + (item.Refine + 1) + risk);
                    }

                case MaterialDef.Enchant:
                    {
                        string k = mat.Attr;
                        float now = CurrentAttributes(e, item).Get(k);
                        float cap = e.AttrCap.Get(k);
                        if (FreePoints(e, item) <= 0) return UpgradeResult.Fail(e.Name + " já usou todos os " + e.AttrTotal + " pontos");
                        if (now >= cap) return UpgradeResult.Fail(Attributes.DisplayName(k) + " já está no teto desta peça (" + cap + ")");
                        return UpgradeResult.Success(Attributes.DisplayName(k) + " " + now + " → " + (now + 1) + " (teto " + cap + ")");
                    }

                case MaterialDef.UnlockCard:
                    if (e.CardSlots < mat.Slot) return UpgradeResult.Fail(e.Name + (e.CardSlots == 0 ? " não tem encaixe de carta" : " não tem o " + mat.Slot + "º encaixe"));
                    if (item.CardsOpen >= mat.Slot) return UpgradeResult.Fail("O " + mat.Slot + "º encaixe já está aberto");
                    if (item.CardsOpen < mat.Slot - 1) return UpgradeResult.Fail("Abra antes o " + (mat.Slot - 1) + "º encaixe");
                    return UpgradeResult.Success("abre o " + mat.Slot + "º encaixe de carta");

                case MaterialDef.UnlockGem:
                    if (e.GemSlot <= 0) return UpgradeResult.Fail(e.Name + " não tem encaixe de gema");
                    if (item.GemOpen) return UpgradeResult.Fail("O encaixe de gema já está aberto");
                    return UpgradeResult.Success("abre o encaixe de gema");

                case MaterialDef.Gem:
                    {
                        if (e.GemSlot <= 0) return UpgradeResult.Fail(e.Name + " não tem encaixe de gema");
                        if (!item.GemOpen) return UpgradeResult.Fail("O encaixe de gema está trancado · abra com uma Pinça de Joalheiro");
                        if (item.Gem == mat.Id) return UpgradeResult.Fail(e.Name + " já tem " + mat.Name);
                        MaterialDef old = catalog.GetMaterial(item.Gem);
                        return UpgradeResult.Success("+" + mat.Amount + " " + Attributes.DisplayName(mat.Attr) + (old != null ? " · troca " + old.Name + " (volta para o baú)" : ""));
                    }
            }

            return UpgradeResult.Fail("Material desconhecido");
        }

        /// <summary>
        /// Changes the item (does NOT consume the material or handle the old gem — PlayerProfile does).
        /// roll01: random 0..1 for refine; roll &lt; break chance = the item breaks (Broke = true).
        /// </summary>
        public static UpgradeResult ApplyToItem(GearCatalog catalog, MaterialDef mat, EquipDef e, ItemInstance item, float roll01)
        {
            UpgradeResult check = Check(catalog, mat, e, item);
            if (!check.Ok) return check;

            switch (mat.Kind)
            {
                case MaterialDef.Refine:
                    if (roll01 < BreakChance(catalog.Rules, item.Refine))
                    {
                        return new UpgradeResult { Ok = false, Broke = true, Message = "O refino falhou · " + e.Name + " QUEBROU" };
                    }

                    item.Refine++;
                    break;

                case MaterialDef.Enchant:
                    item.Alloc.Set(mat.Attr, item.Alloc.Get(mat.Attr) + 1);
                    break;

                case MaterialDef.UnlockCard:
                    item.CardsOpen = mat.Slot;
                    break;

                case MaterialDef.UnlockGem:
                    item.GemOpen = true;
                    break;

                case MaterialDef.Gem:
                    item.Gem = mat.Id;
                    break;
            }

            return check;
        }

        /// <summary>
        /// Fills the free points of a piece following an attribute priority (starter kits, invader gear):
        /// weights 3/2/1/0 for the 1st/2nd/3rd/4th attribute, always within caps. Same as Space.
        /// </summary>
        public static void AutoAllocate(EquipDef e, ItemInstance item, string[] priority)
        {
            if (e == null || item == null || priority == null) return;
            float[] weights = { 3f, 2f, 1f, 0f };

            while (FreePoints(e, item) > 0)
            {
                string best = null;
                float bestScore = float.MaxValue;

                for (int i = 0; i < priority.Length && i < weights.Length; i++)
                {
                    string k = priority[i];
                    if (weights[i] <= 0f || RoomIn(e, item, k) <= 0) continue;
                    float score = item.Alloc.Get(k) / weights[i];
                    if (score < bestScore)
                    {
                        bestScore = score;
                        best = k;
                    }
                }

                if (best == null) break;
                item.Alloc.Set(best, item.Alloc.Get(best) + 1);
            }
        }

        /// <summary>Material description with the rule numbers filled in.</summary>
        public static string Describe(GearCatalog catalog, MaterialDef mat)
        {
            if (mat == null || mat.Desc == null) return "";
            return mat.Desc.Replace("{max}", catalog.Rules.RefineMax.ToString());
        }
    }
}
