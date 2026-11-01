using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace VampWolfs.Lobby
{
    /// <summary>
    /// Tela de HABILIDADES do personagem escolhido (nov/2026), em duas abas:
    ///  - CLASSE: as 20 habilidades da classe (10 ativas + 10 passivas). "+" gasta 1 ponto (nível 1 a 10), "−" devolve;
    ///    as ativas vão nas teclas 1 e 2 (botões "1"/"2"); as passivas aprendidas valem sempre.
    ///    Cada habilidade libera num nível do personagem (1 a 50); 1 ponto por nível.
    ///  - EQUIPAMENTO (estilo Albion): o que a arma, o capacete, o peito e as botas vestidos dão. Em cada tecla com
    ///    opções o jogador escolhe uma; o ataque básico e o ultimate são fixos da arma.
    /// À direita, a ficha da habilidade selecionada com os números (montados a partir dos dados: SkillText).
    /// </summary>
    public sealed class SkillsScreen
    {
        readonly LobbyUI ui;
        readonly VisualElement modal, body, detail, tabs;
        readonly Label title, subtitle;
        string tab = "class";
        string selected;

        public bool IsOpen { get { return UiKit.Visible(modal); } }
        SkillCatalog Sc { get { return SkillCatalog.Instance; } }
        CharacterSave Ch { get { return ui.Profile.Current; } }

        static readonly string[] KeyLabels = { "Clique esq.", "Clique dir.", "Q", "E", "F (ultimate)" };

        public SkillsScreen(LobbyUI ui, VisualElement root)
        {
            this.ui = ui;
            modal = UiKit.El("modal skills", root);
            var card = new FrameBox("sk-card"); modal.Add(card);
            var top = UiKit.El("sk-top", card);
            var head = UiKit.El("sk-head", top);
            title = UiKit.Lbl("", "sk-title", head);
            subtitle = UiKit.Lbl("", "sk-sub", head);
            tabs = UiKit.El("sk-tabs", top);
            var tools = UiKit.El("sk-tools", top);
            UiKit.Btn("+1 nível (teste)", () => { Ch.AddXp(SkillCatalog.XpToNext(Ch.Level) - Ch.Xp); Changed(); }, "btn sm ghost", tools);
            UiKit.Btn("Devolver pontos", () => { Ch.ResetSkills(); Changed(); }, "btn sm ghost", tools);
            UiKit.Btn("Fechar (Esc)", Close, "btn sm", tools);

            var main = UiKit.El("sk-main", card);
            var scroll = new ScrollView(ScrollViewMode.Vertical); scroll.AddToClassList("sk-scroll"); main.Add(scroll);
            body = scroll.contentContainer;
            detail = new FrameBox("sk-detail"); main.Add(detail);
            UiKit.Show(modal, false);
        }

        public void Open()
        {
            selected = null;
            UiKit.Show(modal, true);
            modal.BringToFront();
            Render();
        }

        public void Close()
        {
            if (!IsOpen) return;
            UiKit.Show(modal, false);
            ui.Save();
            ui.RenderBase();
        }

        void Changed()
        {
            ui.Save();
            Render();
        }

        void Render()
        {
            var ch = Ch; var cls = Sc.Class(ch.ClassId);
            title.text = "HABILIDADES · " + ch.Name;
            subtitle.text = cls.Name + " · nível " + ch.Level + " · <b>" + ch.PointsFree + "</b> ponto" + (ch.PointsFree == 1 ? "" : "s") + " livre" + (ch.PointsFree == 1 ? "" : "s") + " (" + ch.PointsSpent + "/" + ch.PointsTotal + " usados)";
            tabs.Clear();
            Tab("class", "CLASSE");
            Tab("gear", "EQUIPAMENTO");
            body.Clear();
            if (tab == "class") RenderClass(); else RenderGear();
            RenderDetail();
        }

        void Tab(string id, string label)
        {
            var b = UiKit.Btn(label, () => { tab = id; selected = null; Render(); }, "sk-tab" + (tab == id ? " on" : ""), tabs);
            b.focusable = false;
        }

        /* ======================= aba CLASSE ======================= */

        void RenderClass()
        {
            var all = Sc.ClassSkills(Ch.ClassId);
            var cols = UiKit.El("sk-cols", body);
            var left = UiKit.El("sk-col", cols);
            var right = UiKit.El("sk-col", cols);
            Section(left, "ATIVAS", "teclas 1 e 2 · escolha duas");
            Section(right, "PASSIVAS", "valem sempre depois de aprendidas");
            foreach (var s in all) ClassRow(s.IsPassive ? right : left, s);
        }

        static void Section(VisualElement parent, string name, string sub)
        {
            var h = UiKit.El("sk-sec", parent);
            UiKit.El("sk-sec-bar", h);
            UiKit.Lbl(name, "sk-sec-t", h);
            UiKit.Lbl(sub, "sk-sec-s", h);
        }

        void ClassRow(VisualElement parent, SkillDef s)
        {
            var ch = Ch;
            int lvl = ch.SkillLevel(s.Id);
            bool locked = ch.Level < s.ReqLevel;
            var row = new FrameBox("sk-row" + (selected == s.Id ? " on" : "") + (locked ? " locked" : "") + (lvl > 0 ? " learned" : "")); parent.Add(row);
            var acc = UiKit.El("sk-accent", row); acc.style.backgroundColor = SkillFactory.ParseColor(s.Color);
            var ic = UiKit.Icon(KindIcon(s), "sk-ic", row); ic.Color = locked ? UiKit.Hex(0x6e5a4a) : SkillFactory.ParseColor(s.Color);
            var tx = UiKit.El("sk-tx", row);
            UiKit.Lbl(s.Name, "sk-name", tx);
            UiKit.Lbl(locked ? "Libera no nível " + s.ReqLevel : SkillText.KindName(s) + " · nível " + lvl + "/" + SkillCatalog.MaxSkillLevel, "sk-meta", tx);
            var pips = UiKit.El("sk-pips", tx);
            for (int i = 0; i < SkillCatalog.MaxSkillLevel; i++) UiKit.El("sk-pip" + (i < lvl ? " on" : ""), pips);

            var btns = UiKit.El("sk-btns", row);
            if (!s.IsPassive && lvl > 0)
            {
                for (int k = 0; k < SkillCatalog.ActiveSlots; k++)
                {
                    int key = k;
                    bool on = ch.ActiveAt(k) == s.Id;
                    var kb = UiKit.Btn((k + 1).ToString(), () => { ch.SetActive(key, on ? "" : s.Id); Changed(); }, "sk-key" + (on ? " on" : ""), btns);
                    kb.tooltip = on ? "Tirar da tecla " + (k + 1) : "Pôr na tecla " + (k + 1);
                }
            }
            var minus = UiKit.Btn("−", () => { if (ch.Unlearn(s)) Changed(); }, "sk-pm", btns);
            minus.SetEnabled(lvl > 0);
            string why = ch.CannotLearn(s);
            var plus = UiKit.Btn("+", () => { if (ch.Learn(s)) { selected = s.Id; Changed(); } }, "sk-pm plus", btns);
            plus.SetEnabled(why == null);
            if (why != null) plus.tooltip = why;

            foreach (var c in row.Children()) if (c != btns) NoPick(c);
            row.RegisterCallback<PointerDownEvent>(e => { if (e.target == row) { selected = s.Id; Render(); } });
        }

        /* ======================= aba EQUIPAMENTO ======================= */

        void RenderGear()
        {
            var gear = ui.Catalog; var eq = ui.Profile.EquippedMap();
            string weapon = SkillFactory.WeaponFamily(gear, eq);
            var wf = Sc.Family(weapon);
            Section(body, "ARMA · " + (wf != null ? wf.Name.ToUpperInvariant() : "—"), WeaponName(gear, eq));
            for (int i = 0; i < SkillCatalog.WeaponKeys.Length; i++) KeyRow(weapon, SkillCatalog.WeaponKeys[i], KeyLabels[i]);

            string[] names = { "CAPACETE", "PEITO", "BOTAS" };
            string[] keys = { "R", "Espaço", "Shift" };
            for (int i = 0; i < SkillCatalog.ArmorSlots.Length; i++)
            {
                string slot = SkillCatalog.ArmorSlots[i];
                ItemInstance item;
                EquipDef e = eq.TryGetValue(slot, out item) && item != null ? gear.GetEquip(item.DefId) : null;
                string fam = SkillCatalog.FamilyOf(gear, e);
                var fd = Sc.Family(fam);
                Section(body, names[i] + " · " + (fd != null ? fd.Name.ToUpperInvariant() : "VAZIO"), e != null ? gear.NameOf(item) : "Vista uma peça no Arsenal para ganhar a habilidade");
                if (fam != null) KeyRow(fam, slot, keys[i]);
            }
            UiKit.Lbl("As habilidades vêm da FAMÍLIA da peça (todo Sabre de Cavalaria tem o mesmo kit, de qualquer faixa). Trocar de arma no Arsenal troca o kit. A escolha de cada tecla fica guardada por família.", "sk-note", body);
        }

        static string WeaponName(GearCatalog gear, Dictionary<string, ItemInstance> eq)
        {
            ItemInstance w;
            return eq.TryGetValue("handR", out w) && w != null ? gear.NameOf(w) : "Sem arma: só os punhos";
        }

        /// <summary>Uma tecla do equipamento: rótulo da tecla e as opções (clicar escolhe; fixa = só uma).</summary>
        void KeyRow(string family, string key, string label)
        {
            var options = Sc.Options(family, key);
            if (options.Count == 0) return;
            var chosen = SkillFactory.Chosen(Sc, Ch, family, key);
            var row = UiKit.El("sk-keyrow", body);
            UiKit.Lbl(label, "sk-keylabel", row);
            var opts = UiKit.El("sk-opts", row);
            foreach (var o in options)
            {
                var opt = o;
                bool on = chosen != null && chosen.Id == o.Id;
                var chip = new FrameBox("sk-opt" + (on ? " on" : "") + (selected == o.Id ? " sel" : "")); opts.Add(chip);
                var ic = UiKit.Icon(KindIcon(o), "sk-opt-ic", chip); ic.Color = SkillFactory.ParseColor(o.Color);
                var tx = UiKit.El("sk-opt-tx", chip);
                UiKit.Lbl(o.Name, "sk-opt-name", tx);
                UiKit.Lbl(on ? (options.Count > 1 ? "EM USO" : "FIXA") : SkillText.KindName(o), "sk-opt-meta" + (on ? " on" : ""), tx);
                foreach (var c in chip.Children()) NoPick(c);
                chip.AddManipulator(new Clickable(() =>
                {
                    selected = opt.Id;
                    if (options.Count > 1) { Ch.SetChoice(family, key, opt.Id); ui.Save(); }
                    Render();
                }));
            }
        }

        /* ======================= ficha ======================= */

        void RenderDetail()
        {
            detail.Clear();
            var s = Sc.Get(selected);
            if (s == null)
            {
                UiKit.Lbl("FICHA", "sk-d-kicker", detail);
                UiKit.Lbl(tab == "class"
                    ? "Clique numa habilidade para ver os números. \"+\" gasta um ponto (até o nível 10); \"−\" devolve. Coloque duas ativas nas teclas 1 e 2. Você ganha 1 ponto por nível (até o nível 50)."
                    : "Clique numa habilidade para ver os números e escolhê-la para a tecla. Cada arma tem 3 teclas com opções e um ultimate próprio; capacete, peito e botas dão uma tecla cada.", "sk-d-text", detail);
                return;
            }

            int lvl = s.IsClassSkill ? Mathf.Max(1, Ch.SkillLevel(s.Id)) : 1;
            var color = SkillFactory.ParseColor(s.Color);
            var hd = UiKit.El("sk-d-head", detail);
            var ic = UiKit.Icon(KindIcon(s), "sk-d-ic", hd); ic.Color = color;
            var tx = UiKit.El("sk-d-tx", hd);
            var nm = UiKit.Lbl(s.Name, "sk-d-name", tx); nm.style.color = color;
            UiKit.Lbl(SkillText.KindName(s) + (s.IsPassive ? "" : " · dano " + SkillText.TypeName(s.DmgType)) + (s.IsClassSkill ? " · libera no nível " + s.ReqLevel : ""), "sk-d-meta", tx);
            UiKit.Lbl(s.Desc, "sk-d-desc", detail);
            UiKit.Lbl(s.IsClassSkill ? "NÍVEL " + lvl + (Ch.SkillLevel(s.Id) == 0 ? " (não aprendida)" : "") : "NÚMEROS", "sk-d-kicker", detail);
            UiKit.Lbl(SkillText.Numbers(s, lvl), "sk-d-text", detail);
            if (s.IsClassSkill && lvl < SkillCatalog.MaxSkillLevel)
            {
                UiKit.Lbl("NO NÍVEL " + (Ch.SkillLevel(s.Id) == 0 ? 1 : lvl + 1), "sk-d-kicker", detail);
                UiKit.Lbl(SkillText.Numbers(s, Ch.SkillLevel(s.Id) == 0 ? 1 : lvl + 1), "sk-d-text dim", detail);
            }
            if (!s.IsClassSkill) UiKit.Lbl("Habilidade de equipamento: cresce com o equipamento (atributos, refino e faixa), não tem nível.", "sk-d-text dim", detail);
        }

        static void NoPick(VisualElement e) { e.pickingMode = PickingMode.Ignore; foreach (var c in e.Children()) NoPick(c); }

        public static string KindIcon(SkillDef s)
        {
            switch (s.Kind)
            {
                case "projectile": return "shot";
                case "melee": return "strike";
                case "area": return "burst";
                case "dash": return "charge";
                case "counter": return "shield";
                case "buff": return "up";
                case "zone": return "cloud";
                case "trap": return "target";
            }
            return "star";
        }
    }
}
