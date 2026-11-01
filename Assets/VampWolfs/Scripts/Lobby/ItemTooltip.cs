using UnityEngine;
using UnityEngine.UIElements;

namespace VampWolfs.Lobby
{
    /// <summary>
    /// Ficha flutuante do item ao passar o mouse no Arsenal (copiada do Space, ligada ao equipamento do VampWolfs):
    /// título com o refino (+N), estilo e faixa, imagem, tipo, encaixes de carta e de gema (abertos, trancados ou com gema),
    /// atributos "atual (teto)" e pontos livres, números da peça com o refino, e a diferença para a peça vestida no mesmo slot.
    /// Material de melhoria: o que faz e como usar. Não recebe clique: só aparece e some.
    /// </summary>
    public sealed class ItemTooltip
    {
        readonly VisualElement root;     // a janela do Arsenal (coordenadas da ficha)
        readonly FrameBox box;
        bool visible;

        public ItemInstance Item { get; private set; }
        public bool Visible { get { return visible; } }

        static readonly Color Amber = UiKit.Hex(0xd9a441), Muted = UiKit.Hex(0xa1907a);

        public ItemTooltip(VisualElement parent)
        {
            root = parent;
            box = new FrameBox("itt"); box.pickingMode = PickingMode.Ignore;
            parent.Add(box); UiKit.Show(box, false);
        }

        public void Hide() { if (!visible) return; visible = false; Item = null; UiKit.Show(box, false); }

        /// <summary>Mostra a ficha perto do ponteiro (posição no painel). compareWith = peça vestida no slot (ou null).</summary>
        public void Show(GearCatalog catalog, ItemInstance it, ItemInstance compareWith, Vector2 panelPos)
        {
            if (it == null) { Hide(); return; }
            Build(catalog, it, compareWith);
            visible = true; UiKit.Show(box, true);
            box.BringToFront();
            Move(panelPos);
        }

        /// <summary>Acompanha o ponteiro: à direita dele, ou à esquerda quando não cabe; nunca sai da janela.</summary>
        public void Move(Vector2 panelPos)
        {
            if (!visible) return;
            var p = root.WorldToLocal(panelPos);
            float w = 330f, h = box.layout.height > 10 ? box.layout.height : 520f;
            float W = root.layout.width, H = root.layout.height;
            float x = p.x + 22f; if (x + w > W - 8f) x = p.x - w - 22f;
            float y = p.y - 40f; if (y + h > H - 8f) y = H - 8f - h; if (y < 8f) y = 8f;
            box.style.left = x; box.style.top = y;
        }

        void Build(GearCatalog catalog, ItemInstance it, ItemInstance cmpIt)
        {
            box.Clear(); Item = it;
            var e = catalog.GetEquip(it.DefId);
            var mat = e == null ? catalog.GetMaterial(it.DefId) : null;

            var hd = UiKit.El("itt-hd", box);
            UiKit.Lbl(catalog.NameOf(it), "itt-title", hd);

            var tag = UiKit.El("itt-tag", box);
            if (e != null)
            {
                UiKit.Lbl("FAIXA " + ArsenalScreen.Roman(e.Tier), "itt-tier", tag);
                var stc = ItemStyle.ColorOf(e);
                if (stc.HasValue) { var sl = UiKit.Lbl(ItemStyle.NameOf(e), "itt-style", tag); sl.style.color = stc.Value; }
            }
            else if (mat != null) UiKit.Lbl((mat.Category ?? "Material").ToUpperInvariant(), "itt-rar", tag);

            Sep();
            var mid = UiKit.El("itt-mid", box);
            var pic = new FrameBox("itt-pic"); mid.Add(pic);
            var ic = UiKit.Icon(ArsenalScreen.IconFor(catalog, it), "itt-pic-ic", pic); ic.Color = ArsenalScreen.ColorFor(catalog, it);
            var info = UiKit.El("itt-info", mid);
            if (e != null)
            {
                var t = catalog.TypeOf(e);
                UiKit.Lbl("Tipo", "itt-k", info);
                UiKit.Lbl(t != null ? t.Name + (t.TwoHand ? " · duas mãos" : "") : e.Type, "itt-v", info);
                UiKit.Lbl("Slot", "itt-k gap", info);
                UiKit.Lbl(t != null && t.Slots.Length > 0 ? catalog.SlotName(t.Slots[0]) + (t.Slots.Length > 1 ? " (" + t.Slots.Length + " vagas)" : "") : "—", "itt-v", info);
            }
            else
            {
                UiKit.Lbl("Quantidade", "itt-k", info); UiKit.Lbl("×" + it.Qty, "itt-v", info);
            }

            if (e == null)
            {
                if (mat != null)
                {
                    Sep();
                    UiKit.Lbl(GearUpgrades.Describe(catalog, mat), "itt-desc", box);
                    UiKit.Lbl(mat.Kind == MaterialDef.Gem ? "Arraste até uma peça com o encaixe de gema aberto para engastar" : "Arraste até uma peça (vestida ou no baú) para usar", "itt-foot", box);
                }
                return;
            }

            // encaixes: cartas e gema; começam trancados, chaves abrem
            Sep();
            UiKit.Lbl("ENCAIXES", "itt-sec", box);
            var cards = UiKit.El("itt-cards", box);
            if (e.CardSlots <= 0 && e.GemSlot <= 0) UiKit.Lbl("Sem encaixes · peça com mais atributos", "itt-none", cards);
            for (int i = 0; i < e.CardSlots; i++) Socket(cards, i < it.CardsOpen, "Carta");
            var gem = !string.IsNullOrEmpty(it.Gem) ? catalog.GetMaterial(it.Gem) : null;
            if (e.GemSlot > 0) (gem != null ? GemSocket(cards, gem) : Socket(cards, it.GemOpen, "Gema")).AddToClassList("gem");
            if (gem != null) UiKit.Lbl(gem.Name + ": " + GearUpgrades.Describe(catalog, gem), "itt-note", box);
            if (e.CardSlots > 0) UiKit.Lbl("cartas ainda sem efeito (em breve)", "itt-note", box);

            // atributos: atual (teto) de cada um, total usado / total da peça, diferença para a peça vestida no mesmo slot
            var cmp = cmpIt != null && cmpIt.Uid != it.Uid ? catalog.GetEquip(cmpIt.DefId) : null;
            if (cmp == null) cmpIt = null;
            var now = GearUpgrades.CurrentAttributes(e, it);
            var other = cmp != null ? GearUpgrades.CurrentAttributes(cmp, cmpIt) : new Attributes();
            Sep();
            UiKit.Lbl("ATRIBUTOS (" + now.Sum + "/" + e.AttrTotal + ")", "itt-sec", box);
            bool anyAttr = false;
            foreach (var k in Attributes.Keys)
            {
                float v = now.Get(k), cap = e.AttrCap.Get(k);
                if (cap <= 0 && v <= 0) continue;
                anyAttr = true;
                Row("st_" + k.ToLowerInvariant(), Attributes.DisplayName(k), v + "  <color=#a1907a>(" + cap + ")</color>", cmp != null ? v - other.Get(k) : (float?)null, "");
            }
            if (!anyAttr) UiKit.Lbl("Sem atributos", "itt-none", box);
            int free = GearUpgrades.FreePoints(e, it);
            if (free > 0) UiKit.Lbl(free + " ponto" + (free > 1 ? "s" : "") + " livre" + (free > 1 ? "s" : "") + " · use Cristais de atributo (o número entre parênteses é o teto)", "itt-note", box);

            // números da peça (com o refino)
            float rm = e.Refinable ? CharacterStats.RefineMult(catalog.Rules, it.Refine) : 1f;
            float crm = cmp != null && cmp.Refinable ? CharacterStats.RefineMult(catalog.Rules, cmpIt.Refine) : 1f;
            bool any = false;
            System.Action<string, string, float, float, string> stat = (icon, name, v, o, suf) =>
            {
                if (v <= 0 && (cmp == null || o <= 0)) return;
                if (!any) { Sep(); any = true; }
                v = Mathf.Round(v * 10f) / 10f; o = Mathf.Round(o * 10f) / 10f;
                Row(icon, name, v + suf, cmp != null ? Mathf.Round((v - o) * 10f) / 10f : (float?)null, suf);
            };
            stat("st_bolt", "Dano", e.Dmg * rm, cmp != null ? cmp.Dmg * crm : 0f, "");
            stat("st_shield", "Armour", e.Armour * rm, cmp != null ? cmp.Armour * crm : 0f, "%");
            stat("st_dodge", "Evasion", e.Evasion * rm, cmp != null ? cmp.Evasion * crm : 0f, "");
            stat("st_es", "Égide", e.Ward * rm, cmp != null ? cmp.Ward * crm : 0f, "");
            stat("st_speed", "Velocidade", e.Move * 100f, cmp != null ? cmp.Move * 100f : 0f, "%");
            if (e.Refinable && it.Refine > 0)
                UiKit.Lbl("Refino +" + it.Refine + " · +" + Mathf.RoundToInt((rm - 1f) * 100f) + "% nos números da peça", "itt-note", box);
            if (cmp != null) UiKit.Lbl("Comparado com o que você veste (" + catalog.NameOf(cmpIt) + ")", "itt-foot", box);
        }

        /// <summary>Encaixe de gema ocupado: ícone e nome da gema na cor dela.</summary>
        static FrameBox GemSocket(VisualElement parent, MaterialDef gem)
        {
            var c = new FrameBox("itt-card"); parent.Add(c); c.AddToClassList("filled");
            var ic = UiKit.Icon("up_gem", "itt-card-ic", c); ic.Color = UiKit.Hex(gem.Color);
            UiKit.Lbl(gem.Name, "itt-card-t", c);
            return c;
        }

        /// <summary>Um encaixe: aberto ("Inserir Carta/Gema") ou trancado (cadeado; precisa de chave).</summary>
        static FrameBox Socket(VisualElement parent, bool open, string what)
        {
            var c = new FrameBox("itt-card"); parent.Add(c);
            c.EnableInClassList("locked", !open);
            var pl = UiKit.Icon(open ? "ui_plus" : "ui_lock", "itt-card-ic", c); pl.Color = open ? Muted : UiKit.Hex(0x6e5a4a);
            UiKit.Lbl(open ? "Inserir\n" + what : what + "\ntrancada", "itt-card-t", c);
            return c;
        }

        void Sep() { UiKit.El("itt-sep", box); }

        void Row(string icon, string name, string val, float? delta, string suf)
        {
            var r = UiKit.El("itt-stat", box);
            var ic = UiKit.Icon(icon, "itt-stat-ic", r); ic.Color = Amber;
            UiKit.Lbl(name, "itt-stat-k", r);
            UiKit.Lbl(val, "itt-stat-v", r);
            var dl = UiKit.Lbl("", "itt-stat-d", r);
            if (delta.HasValue)
            {
                float dv = delta.Value;
                dl.text = dv == 0 ? "(=)" : "(" + (dv > 0 ? "+" : "") + dv + suf + ")";
                dl.style.color = dv > 0 ? UiKit.Hex(0x7cc46a) : dv < 0 ? UiKit.Hex(0xe0564a) : Muted;
            }
        }
    }
}
