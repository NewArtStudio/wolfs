using System.Collections.Generic;
using UnityEngine;

namespace VampWolfs
{
    /// <summary>
    /// The Base (between matches), drawn with IMGUI like the rest of the prototype HUD:
    /// worn equipment (10 slots), stash, item details, upgrades (refine, tonics, keys, gems),
    /// the attributes those turn into, and the button to enter the farm.
    /// </summary>
    public class BaseScreen : MonoBehaviour
    {
        GameBootstrap bootstrap;
        GearCatalog catalog;

        string selectedGear;      // uid of the selected equipment piece (worn or in the stash)
        string selectedMaterial;  // uid of the selected material in the stash
        string message = "";
        Color messageColor = Color.white;
        bool confirmReset;

        Vector2 stashScroll;
        Vector2 detailScroll;

        float scale = 1f;
        int stylesBuiltForHeight = -1;
        GUIStyle label;
        GUIStyle small;
        GUIStyle title;
        GUIStyle header;
        GUIStyle row;
        GUIStyle rowSelected;
        GUIStyle bigButton;
        GUIStyle button;

        static readonly Color Panel = new Color(0.1f, 0.08f, 0.14f, 0.96f);
        static readonly Color Accent = new Color(0.85f, 0.2f, 0.3f);
        static readonly Color Good = new Color(0.55f, 1f, 0.6f);
        static readonly Color Bad = new Color(1f, 0.4f, 0.35f);
        static readonly string[] TierRoman = { "0", "I", "II", "III", "IV", "V", "VI", "VII", "VIII" };

        public void Init(GameBootstrap owner)
        {
            bootstrap = owner;
            catalog = GearCatalog.Instance;
        }

        PlayerProfile Profile
        {
            get { return bootstrap.Profile; }
        }

        int enabledFrame;

        void OnEnable()
        {
            enabledFrame = Time.frameCount;
        }

        void Update()
        {
            // Ignore the same Enter press that closed the end-of-match screen.
            if (Time.frameCount > enabledFrame + 1 && InputReader.RestartPressed())
            {
                bootstrap.StartMatch();
            }
        }

        void OnGUI()
        {
            if (bootstrap == null || Profile == null)
            {
                return;
            }

            EnsureStyles();
            DrawRect(new Rect(0, 0, Screen.width, Screen.height), new Color(0.05f, 0.04f, 0.08f, 1f));

            float m = Px(16);
            float top = Px(70);
            float bottom = Screen.height - Px(80);
            float h = bottom - top;
            float leftW = Screen.width * 0.30f;
            float midW = Screen.width * 0.30f;
            float rightW = Screen.width - leftW - midW - m * 4;

            DrawHeader(new Rect(m, Px(12), Screen.width - m * 2, Px(48)));
            DrawEquipped(new Rect(m, top, leftW, h));
            DrawStash(new Rect(m * 2 + leftW, top, midW, h));
            DrawDetails(new Rect(m * 3 + leftW + midW, top, rightW, h * 0.55f - Px(6)));
            DrawStats(new Rect(m * 3 + leftW + midW, top + h * 0.55f + Px(6), rightW, h * 0.45f - Px(6)));
            DrawFooter(new Rect(m, bottom + Px(12), Screen.width - m * 2, Px(56)));
        }

        // ---------------------------------------------------------------- panels

        void DrawHeader(Rect r)
        {
            DrawRect(r, Panel);
            GUI.Label(new Rect(r.x + Px(14), r.y, r.width, r.height), "<b>BASE</b>  ·  Vampiro", title);
            string info = "Partidas: " + Profile.Runs + "   Extrações: " + Profile.Extractions +
                          "   ·   Sangue " + Profile.Blood + "   Lã " + Profile.Wool + "   Fragmento Lunar " + Profile.MoonShard;
            GUI.Label(new Rect(r.x, r.y, r.width - Px(14), r.height), info, new GUIStyle(label) { alignment = TextAnchor.MiddleRight });
        }

        void DrawEquipped(Rect r)
        {
            DrawRect(r, Panel);
            GUI.Label(new Rect(r.x + Px(12), r.y + Px(8), r.width, Px(26)), "<b>EQUIPADO</b> <color=#80ff80>(nunca cai)</color>", header);

            float y = r.y + Px(42);
            float rowH = Px(44);

            foreach (SlotDef slot in catalog.Slots)
            {
                ItemInstance item = Profile.GetEquipped(slot.Id);
                Rect rowRect = new Rect(r.x + Px(10), y, r.width - Px(20), rowH - Px(4));
                bool selected = item != null && item.Uid == selectedGear;

                string text = "<size=" + Px(11) + "><color=#a99fbf>" + slot.Name + "</color></size>\n" +
                              (item != null ? ItemLine(item) : "<color=#6b6478>— vazio —</color>");

                if (GUI.Button(rowRect, text, selected ? rowSelected : row) && item != null)
                {
                    selectedGear = item.Uid;
                    message = "";
                }

                y += rowH;
            }
        }

        void DrawStash(Rect r)
        {
            DrawRect(r, Panel);
            GUI.Label(new Rect(r.x + Px(12), r.y + Px(8), r.width, Px(26)), "<b>BAÚ</b> <color=#a99fbf>(seguro, fica na base)</color>", header);

            List<ItemInstance> gear = new List<ItemInstance>();
            List<ItemInstance> materials = new List<ItemInstance>();
            foreach (ItemInstance item in Profile.Stash)
            {
                if (catalog.GetEquip(item.DefId) != null) gear.Add(item);
                else materials.Add(item);
            }

            gear.Sort((a, b) => catalog.GetEquip(b.DefId).Tier.CompareTo(catalog.GetEquip(a.DefId).Tier));

            float rowH = Px(40);
            float contentH = (gear.Count + materials.Count + 2) * rowH + Px(20);
            Rect view = new Rect(r.x + Px(6), r.y + Px(40), r.width - Px(12), r.height - Px(48));
            Rect content = new Rect(0, 0, view.width - Px(18), contentH);

            stashScroll = GUI.BeginScrollView(view, stashScroll, content);
            float y = 0;

            GUI.Label(new Rect(Px(6), y, content.width, rowH), "<b>Equipamentos</b> (" + gear.Count + ")", label);
            y += rowH * 0.8f;
            foreach (ItemInstance item in gear)
            {
                bool selected = item.Uid == selectedGear;
                if (GUI.Button(new Rect(Px(4), y, content.width - Px(8), rowH - Px(4)), ItemLine(item), selected ? rowSelected : row))
                {
                    selectedGear = item.Uid;
                    message = "";
                }

                y += rowH;
            }

            y += Px(10);
            GUI.Label(new Rect(Px(6), y, content.width, rowH), "<b>Materiais de melhoria</b>", label);
            y += rowH * 0.8f;
            foreach (ItemInstance item in materials)
            {
                MaterialDef mat = catalog.GetMaterial(item.DefId);
                bool selected = item.Uid == selectedMaterial;
                string text = "<color=" + (mat != null ? mat.Color : "#ffffff") + ">●</color> " + catalog.NameOf(item);
                if (GUI.Button(new Rect(Px(4), y, content.width - Px(8), rowH - Px(4)), text, selected ? rowSelected : row))
                {
                    selectedMaterial = selected ? null : item.Uid;
                    message = "";
                }

                y += rowH;
            }

            GUI.EndScrollView();
        }

        void DrawDetails(Rect r)
        {
            DrawRect(r, Panel);
            ItemInstance item = FindGear(selectedGear);
            EquipDef e = item != null ? catalog.GetEquip(item.DefId) : null;
            ItemInstance matItem = Profile.Stash.Find(i => i.Uid == selectedMaterial);
            MaterialDef mat = matItem != null ? catalog.GetMaterial(matItem.DefId) : null;

            float x = r.x + Px(12);
            float w = r.width - Px(24);
            float y = r.y + Px(8);

            if (e == null)
            {
                GUI.Label(new Rect(x, y, w, Px(60)), "Clique num equipamento (vestido ou do baú) para ver os detalhes.\n" +
                    "Para melhorar: selecione um <b>material</b> no baú e um <b>equipamento</b>.", small);
                y += Px(60);
            }
            else
            {
                bool worn = WornSlotOf(item) != null;
                GUI.Label(new Rect(x, y, w, Px(26)), "<b>" + catalog.NameOf(item) + "</b>", header);
                y += Px(26);

                EquipTypeDef type = catalog.TypeOf(e);
                GUI.Label(new Rect(x, y, w, Px(20)), "Faixa " + TierRoman[Mathf.Clamp(e.Tier, 0, 8)] + "  ·  " + (type != null ? type.Name : e.Type) + "  ·  estilo " + FocusName(e.Focus), small);
                y += Px(22);

                string body = "";
                if (e.Dmg > 0) body += "Dano da arma: <b>" + Mathf.RoundToInt(e.Dmg * (e.Refinable ? CharacterStats.RefineMult(catalog.Rules, item.Refine) : 1f)) + "</b>\n";
                if (e.Armour > 0) body += "Armadura: " + e.Armour + "%   ";
                if (e.Evasion > 0) body += "Evasão: " + e.Evasion + "   ";
                if (e.Ward > 0) body += "Égide: " + e.Ward + "   ";
                if (e.Move > 0) body += "Velocidade: +" + Mathf.RoundToInt(e.Move * 100f) + "%";
                if (e.Armour > 0 || e.Evasion > 0 || e.Ward > 0 || e.Move > 0) body += "\n";

                Attributes current = GearUpgrades.CurrentAttributes(e, item);
                body += "Atributos (" + (int)current.Sum + "/" + e.AttrTotal + " pontos):  ";
                foreach (string k in Attributes.Keys)
                {
                    body += Attributes.ShortName(k) + " " + current.Get(k) + "/" + e.AttrCap.Get(k) + "   ";
                }

                body += "\nRefino: " + (e.Refinable ? "+" + item.Refine + " / +" + catalog.Rules.RefineMax : "não refina") +
                        "   ·   Cartas: " + item.CardsOpen + "/" + e.CardSlots + " abertos" +
                        "   ·   Gema: " + (e.GemSlot <= 0 ? "sem encaixe" : !item.GemOpen ? "trancado" : string.IsNullOrEmpty(item.Gem) ? "vazio" : GemName(item.Gem));

                Rect bodyRect = new Rect(x, y, w, Mathf.Max(Px(40), r.yMax - y - Px(136)));
                float bodyHeight = Mathf.Max(bodyRect.height, small.CalcHeight(new GUIContent(body), w - Px(18)));
                detailScroll = GUI.BeginScrollView(bodyRect, detailScroll, new Rect(0, 0, w - Px(18), bodyHeight));
                GUI.Label(new Rect(0, 0, w - Px(18), bodyHeight), body, small);
                GUI.EndScrollView();

                float by = r.yMax - Px(96);
                if (worn)
                {
                    if (GUI.Button(new Rect(x, by, Px(160), Px(34)), "Desequipar", button))
                    {
                        Profile.Unequip(WornSlotOf(item));
                        Profile.Save();
                        Say("Guardado no baú.", Color.white);
                    }
                }
                else if (GUI.Button(new Rect(x, by, Px(160), Px(34)), "Equipar", button))
                {
                    string error = Profile.Equip(catalog, item.Uid, null);
                    if (error != null) Say(error, Bad);
                    else
                    {
                        Profile.Save();
                        Say("Equipado.", Good);
                    }
                }
            }

            // Upgrade row: selected material → selected equipment.
            float uy = r.yMax - Px(56);
            if (mat != null)
            {
                string desc = GearUpgrades.Describe(catalog, mat);
                if (e == null)
                {
                    GUI.Label(new Rect(x, uy, w, Px(50)), "<b>" + mat.Name + "</b>: " + desc + "\nAgora clique no equipamento que vai receber.", small);
                }
                else
                {
                    UpgradeResult preview = GearUpgrades.Check(catalog, mat, e, item);
                    GUI.Label(new Rect(x, uy, w - Px(150), Px(50)), "<b>" + mat.Name + "</b> → " + (preview.Ok ? preview.Message : "<color=#ff7060>" + preview.Message + "</color>"), small);
                    GUI.enabled = preview.Ok;
                    if (GUI.Button(new Rect(r.xMax - Px(150), uy + Px(4), Px(138), Px(36)), "Aplicar", button))
                    {
                        ApplyMaterial(matItem, item);
                    }

                    GUI.enabled = true;
                }
            }

            if (!string.IsNullOrEmpty(message))
            {
                ShadowLabel(new Rect(x, r.yMax - Px(128), w, Px(28)), message, label, messageColor);
            }
        }

        void ApplyMaterial(ItemInstance matItem, ItemInstance target)
        {
            string materialName = catalog.GetMaterial(matItem.DefId).Name;
            UpgradeResult result = Profile.ApplyMaterial(catalog, matItem.Uid, target.Uid, Random.value);

            if (result.Broke)
            {
                Say(result.Message, Bad);
                selectedGear = null;
            }
            else if (result.Ok)
            {
                Say(materialName + ": " + result.Message, Good);
            }
            else
            {
                Say(result.Message, Bad);
            }

            if (Profile.Stash.Find(i => i.Uid == matItem.Uid) == null) selectedMaterial = null;
            Profile.Save();
        }

        void DrawStats(Rect r)
        {
            DrawRect(r, Panel);
            CharacterStats s = CharacterStats.Compute(catalog, Profile.EquippedMap());
            float hp = CharacterFactory.VampireHealth * s.HpMult;

            string text =
                "<b>ATRIBUTOS DO PERSONAGEM</b>   Faixa de poder " + s.PowerTier.ToString("0.0") + "\n" +
                "FOR " + s.Eff.Str + "   DES " + s.Eff.Dex + "   INT " + s.Eff.Int + "   VIT " + s.Eff.Vit +
                (s.Bonus.Sum > 0 ? "   <color=#59d8ff>(gemas +" + s.Bonus.Sum + ")</color>" : "") + "\n\n" +
                "Vida: <b>" + Mathf.RoundToInt(hp) + "</b>   Égide: " + Mathf.RoundToInt(hp * s.WardPct) + "\n" +
                "Dano mágico (INT): <b>" + Pct(s.DamageMult(DamageType.Magic, catalog.Rules)) + "</b>   " +
                "Físico perto (FOR): " + Pct(s.DamageMult(DamageType.PhysicalMelee, catalog.Rules)) + "   " +
                "Físico longe (DES): " + Pct(s.DamageMult(DamageType.PhysicalRanged, catalog.Rules)) + "\n" +
                "Redução física: " + Mathf.RoundToInt(s.PhysReductionPct) + "%   Defesa: " + s.Def + "   Defesa mágica: " + s.Mdef + "\n" +
                "Velocidade: " + Pct(s.MoveMult) + "   Conjuração: " + Pct(s.CastMult) + " do tempo   Ataque básico: " + Pct(s.AspdMult) + " do tempo\n" +
                "Resistência a lentidão/atordoamento: " + Mathf.RoundToInt(s.StatusResist * 100f) + "%\n" +
                "Encaixes de carta abertos: " + s.OpenCardSockets + "/" + s.CardSockets + " <color=#a99fbf>(cartas ainda sem efeito)</color>";

            GUI.Label(new Rect(r.x + Px(12), r.y + Px(8), r.width - Px(24), r.height - Px(16)), text, small);
        }

        void DrawFooter(Rect r)
        {
            if (GUI.Button(new Rect(r.xMax - Px(320), r.y, Px(320), r.height), "ENTRAR NA FAZENDA  (Enter)", bigButton))
            {
                bootstrap.StartMatch();
            }

            GUI.Label(new Rect(r.x, r.y, r.width - Px(540), r.height),
                "O que você <b>veste</b> nunca cai. O que achar na partida vai para a <b>bag</b> e só fica seu se extrair.\n" +
                "Refino a partir do +5 pode <color=#ff7060><b>quebrar</b></color> a peça.", small);

            Rect resetRect = new Rect(r.xMax - Px(520), r.y + Px(10), Px(180), Px(36));
            if (!confirmReset)
            {
                if (GUI.Button(resetRect, "Recomeçar perfil", button)) confirmReset = true;
            }
            else if (GUI.Button(resetRect, "Confirmar: apagar tudo?", button))
            {
                bootstrap.ResetProfile();
                confirmReset = false;
                selectedGear = null;
                selectedMaterial = null;
                Say("Perfil recomeçado com o kit inicial.", Color.white);
            }
        }

        // ---------------------------------------------------------------- helpers

        string ItemLine(ItemInstance item)
        {
            EquipDef e = catalog.GetEquip(item.DefId);
            if (e == null) return catalog.NameOf(item);
            return catalog.NameOf(item) + "  <color=#a99fbf><size=" + Px(11) + ">F" + TierRoman[Mathf.Clamp(e.Tier, 0, 8)] + "</size></color>";
        }

        ItemInstance FindGear(string uid)
        {
            if (string.IsNullOrEmpty(uid)) return null;
            ItemInstance stashed = Profile.Stash.Find(i => i.Uid == uid);
            if (stashed != null) return stashed;
            foreach (EquippedSlot s in Profile.Worn)
            {
                if (s.Item != null && s.Item.Uid == uid) return s.Item;
            }

            return null;
        }

        string WornSlotOf(ItemInstance item)
        {
            foreach (EquippedSlot s in Profile.Worn)
            {
                if (s.Item == item) return s.Slot;
            }

            return null;
        }

        string GemName(string id)
        {
            MaterialDef gem = catalog.GetMaterial(id);
            return gem != null ? gem.Name : id;
        }

        static string FocusName(string focus)
        {
            switch (focus)
            {
                case "vanguard": return "vanguarda";
                case "scout": return "batedor";
                case "arcane": return "arcano";
                case "sacred": return "sagrado";
                case "str": return "força";
                case "dex": return "destreza";
                case "int": return "inteligência";
                case "vit": return "vitalidade";
                default: return focus;
            }
        }

        static string Pct(float multiplier)
        {
            return Mathf.RoundToInt(multiplier * 100f) + "%";
        }

        void Say(string text, Color color)
        {
            message = text;
            messageColor = color;
        }

        int Px(float value)
        {
            return Mathf.RoundToInt(value * scale);
        }

        static void DrawRect(Rect rect, Color color)
        {
            Color old = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = old;
        }

        void ShadowLabel(Rect rect, string text, GUIStyle style, Color color)
        {
            Color original = style.normal.textColor;
            style.normal.textColor = new Color(0f, 0f, 0f, 0.8f);
            GUI.Label(new Rect(rect.x + 1, rect.y + 1, rect.width, rect.height), text, style);
            style.normal.textColor = color;
            GUI.Label(rect, text, style);
            style.normal.textColor = original;
        }

        void EnsureStyles()
        {
            if (stylesBuiltForHeight == Screen.height && label != null)
            {
                return;
            }

            stylesBuiltForHeight = Screen.height;
            scale = Mathf.Max(0.6f, Screen.height / 1080f);

            label = new GUIStyle(GUI.skin.label) { fontSize = Px(16), richText = true, wordWrap = true };
            label.normal.textColor = Color.white;
            small = new GUIStyle(label) { fontSize = Px(14) };
            header = new GUIStyle(label) { fontSize = Px(18) };
            title = new GUIStyle(label) { fontSize = Px(24), alignment = TextAnchor.MiddleLeft };

            row = new GUIStyle(GUI.skin.button) { fontSize = Px(14), richText = true, alignment = TextAnchor.MiddleLeft, wordWrap = false };
            row.padding = new RectOffset(Px(10), Px(6), Px(2), Px(2));
            rowSelected = new GUIStyle(row);
            rowSelected.normal.background = SolidTexture(new Color(0.45f, 0.12f, 0.2f));
            rowSelected.hover.background = rowSelected.normal.background;
            rowSelected.normal.scaledBackgrounds = new Texture2D[0]; // high-DPI skins would draw these instead
            rowSelected.hover.scaledBackgrounds = new Texture2D[0];
            rowSelected.normal.textColor = Color.white;

            button = new GUIStyle(GUI.skin.button) { fontSize = Px(15), fontStyle = FontStyle.Bold };
            bigButton = new GUIStyle(GUI.skin.button) { fontSize = Px(20), fontStyle = FontStyle.Bold };
            bigButton.normal.background = SolidTexture(Accent);
            bigButton.hover.background = SolidTexture(new Color(0.95f, 0.3f, 0.4f));
            bigButton.normal.scaledBackgrounds = new Texture2D[0];
            bigButton.hover.scaledBackgrounds = new Texture2D[0];
            bigButton.normal.textColor = Color.white;
            bigButton.hover.textColor = Color.white;
        }

        static readonly Dictionary<Color, Texture2D> textures = new Dictionary<Color, Texture2D>();

        static Texture2D SolidTexture(Color color)
        {
            Texture2D cached;
            if (textures.TryGetValue(color, out cached) && cached != null) return cached;

            Texture2D texture = new Texture2D(1, 1);
            textures[color] = texture;
            texture.SetPixel(0, 0, color);
            texture.Apply();
            return texture;
        }
    }
}
