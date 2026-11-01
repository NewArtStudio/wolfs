using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace VampWolfs.Lobby
{
    /// <summary>
    /// Tela inicial do lobby, copiada da Base Espacial (mesmo desenho e classes bs- do Space.uss), em versão VampWolfs:
    ///  - painel da esquerda: nome do jogo, texto, atalhos INVENTÁRIO e FAMILIAR ("em breve") e o REGISTRO
    ///    (recursos guardados, incursões e extrações);
    ///  - painel da direita: PERSONAGENS — card do personagem escolhido (retrato, classe, nível, XP, Habilidades,
    ///    Equipamento) e a grade com todos os personagens + vagas para criar novos (Vampiro, Lobisomem ou Caçador);
    ///    embaixo o botão EXPLORAR, que entra direto na Fazenda com o personagem escolhido.
    /// Molduras em FrameBox, ícones em Icons ("ui_*"), retratos do PortraitStudio. Só chama comandos do LobbyUI.
    /// </summary>
    public sealed class BaseScreen
    {
        readonly LobbyUI ui;
        public readonly VisualElement Root;
        VisualElement partyList, record, benchGrid, rightCol;
        Label partyCount;
        Button resetBtn;
        bool resetArmed;

        // janela de criar personagem (abre à esquerda da coluna, como o "Recrutar" do Space)
        FrameBox recruitPop;
        string recruitPick;
        TextField recruitName;

        /// <summary>Frase curta da função de cada classe.</summary>
        static readonly Dictionary<string, string> Role = new Dictionary<string, string>
        {
            { "vampire", "Magia de sangue · roubo de vida" }, { "werewolf", "Garras e saltos · regeneração" }, { "hunter", "Armadilhas e tiros · marcas" },
        };

        public BaseScreen(LobbyUI ui, VisualElement parent)
        {
            this.ui = ui;
            Root = UiKit.El("base bs", parent);
            Root.pickingMode = PickingMode.Ignore;
            BuildBrand();
            BuildSide();
        }

        SkillCatalog Skills { get { return SkillCatalog.Instance; } }

        /* ======================= painel da esquerda ======================= */

        void BuildBrand()
        {
            var brand = new FrameBox("bs-brand"); Root.Add(brand);
            var kick = UiKit.El("bs-kicker-row", brand);
            var kic = UiKit.Icon("ui_moon", "bs-kicker-ic", kick); kic.Color = UiKit.Hex(0xd9a441);
            UiKit.Lbl("LONDRES · INVERNO DE 1888", "bs-kicker", kick);
            var head = UiKit.El("bs-head", brand);
            UiKit.El("bs-head-bar", head);
            var col = UiKit.El("bs-head-col", head);
            UiKit.Lbl("VAMPWOLFS", "bs-title", col);
            UiKit.Lbl("Escolha quem desce até a Fazenda — vampiro, lobisomem ou caçador de recompensas. Cace ovelhas, encha a bolsa e fuja pelo ponto de extração antes que o invasor tome tudo o que você carrega.", "bs-body", col);
            var tiles = UiKit.El("bs-tiles", brand);
            Tile(tiles, "ui_crate", "INVENTÁRIO", "Gerencie itens\ne equipamentos.", ui.OpenArsenal, false);
            Tile(tiles, "ui_bat", "FAMILIAR", "Morcego ou corvo\nque recolhe o saque.", () => ui.Banner("Familiar: em breve", "warn"), true).style.marginLeft = 14;

            var rh = UiKit.El("bs-sec-h bs-rec-h", brand);
            UiKit.El("bs-sec-bar", rh);
            UiKit.Lbl("REGISTRO", "bs-sec-t", rh);
            record = UiKit.El("bs-record", brand);
        }

        FrameBox Tile(VisualElement parent, string icon, string title, string sub, System.Action click, bool soon)
        {
            var t = new FrameBox("bs-tile" + (soon ? " soon" : "")); parent.Add(t);
            t.AddManipulator(new Clickable(click));
            var ic = UiKit.Icon(icon, "bs-tile-ic", t); ic.Color = UiKit.Hex(0xd9a441);
            UiKit.Lbl(title, "bs-tile-t", t);
            UiKit.Lbl(sub, "bs-tile-s", t);
            if (soon) UiKit.Lbl("EM BREVE", "bs-soon", t);
            return t;
        }

        /* ======================= painel da direita ======================= */

        void BuildSide()
        {
            var right = UiKit.El("bs-right", Root); rightCol = right;
            right.pickingMode = PickingMode.Ignore;   // o vazio da coluna deixa arrastar (girar o personagem)
            var side = new FrameBox("bs-side"); right.Add(side);
            var h = UiKit.El("bs-sec-h", side);
            UiKit.El("bs-sec-bar", h);
            UiKit.Lbl("PERSONAGENS", "bs-sec-t", h);
            partyCount = UiKit.Lbl("1/" + PlayerProfile.MaxCharacters, "bs-chip", h);
            partyList = UiKit.El("bs-party", side);

            // todos os personagens + vagas livres (clicar escolhe; vaga livre abre o "criar personagem")
            var bh = new FrameBox("bs-bench-h"); side.Add(bh);
            var bi = UiKit.Icon("ui_people", "bs-bench-ic", bh); bi.Color = UiKit.Hex(0xd9a441);
            UiKit.Lbl("MANSÃO", "bs-bench-t", bh);
            UiKit.Lbl("escolha ou crie", "bs-bench-n", bh);
            var wrap = UiKit.El("bs-bench-wrap", side);
            var view = new ScrollView(ScrollViewMode.Vertical); view.AddToClassList("bs-bench"); wrap.Add(view);
            view.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
            benchGrid = view.contentContainer; benchGrid.AddToClassList("bs-bench-grid");

            UiKit.El("bs-spacer", right);
            var ex = new Button(ui.Explore) { focusable = false }; ex.AddToClassList("bs-explore"); right.Add(ex);
            ex.text = "";
            var exi = UiKit.Icon("ui_chevrons", "bs-explore-ic", ex); exi.Color = UiKit.Hex(0x2a1606);
            UiKit.Lbl("EXPLORAR", "bs-explore-t", ex);
        }

        /// <summary>Refaz o card, a grade e o registro (chamado pelo LobbyUI.RenderBase).</summary>
        public void Render()
        {
            var p = ui.Profile;
            partyCount.text = p.Characters.Count + "/" + PlayerProfile.MaxCharacters;
            partyList.Clear();
            Card(partyList, p.Current);
            benchGrid.Clear();
            for (int i = 0; i < p.Characters.Count; i++) RosterTile(i, p.Characters[i], i == p.Active);
            for (int i = p.Characters.Count; i < PlayerProfile.MaxCharacters; i++) EmptyTile();
            RenderRecord();
            RenderRecruit();
        }

        /// <summary>Card do personagem escolhido: retrato, classe, nível, XP, Habilidades (pontos livres) e Equipamento.</summary>
        void Card(VisualElement parent, CharacterSave ch)
        {
            var cls = Skills.Class(ch.ClassId);
            var color = UiKit.ClassColor(ch.ClassId);
            var card = new FrameBox("bs-card"); parent.Add(card);
            var accent = UiKit.El("bs-card-accent", card); accent.style.backgroundColor = color;
            var top = UiKit.El("bs-card-top", card);

            var pf = UiKit.El("bs-portrait", top);
            var tex = PortraitStudio.Portrait(ch.ClassId);
            if (tex != null) { var img = new Image { image = tex, scaleMode = ScaleMode.ScaleAndCrop }; img.AddToClassList("bs-portrait-img"); pf.Add(img); }
            else { var sil = UiKit.Icon("ui_person", "bs-portrait-sil", pf); sil.Color = new Color(1f, 1f, 1f, 0.25f); }
            UiKit.Lbl(ch.Level.ToString(), "bs-slot", pf);

            var info = UiKit.El("bs-info", top);
            var nameRow = UiKit.El("bs-name-row", info);
            UiKit.Lbl(ch.Name, "bs-name", nameRow);
            var cic = UiKit.Icon(ClassIcon(ch.ClassId), "bs-cls-ic", nameRow); cic.Color = color;
            var cl = UiKit.Lbl(cls.Name.ToUpperInvariant(), "bs-cls", info); cl.style.color = color;
            UiKit.Lbl("Nível " + ch.Level + " de " + SkillCatalog.MaxCharacterLevel, "bs-rank", info);
            var xpRow = UiKit.El("bs-xp-row", info);
            int need = SkillCatalog.XpToNext(ch.Level);
            var bar = new Bar("bs-xp"); bar.Set(need > 0 ? ch.Xp / (float)need : 1f); xpRow.Add(bar);
            UiKit.Lbl(need > 0 ? "XP " + ch.Xp + "/" + need : "XP máximo", "bs-xp-t", xpRow);

            // poder do equipamento (o que a invasão usa para escolher o invasor)
            var catalog = ui.Catalog;
            var st = CharacterStats.Compute(catalog, ui.Profile.EquippedMap());
            int hp = Mathf.RoundToInt(cls.Health * st.HpMult * (1f + SkillFactory.PassivesOf(SkillCatalog.Instance, ch).Hp));
            UiKit.Lbl("Faixa de poder " + st.PowerTier.ToString("0.0") + " · Vida " + hp, "bs-power", card);

            var btns = UiKit.El("bs-btns", card);
            int free = ch.PointsFree;
            IconButton(btns, "ui_grid", "Habilidades" + (free > 0 ? " (" + free + ")" : ""), ui.OpenSkills, "bs-btn skills" + (free > 0 ? " hot" : ""));
            IconButton(btns, "ui_wrench", "Equipamento (" + ui.Profile.EquippedMap().Count + "/" + SlotCount() + ")", ui.OpenArsenal, "bs-btn gear");
        }

        public static string ClassIcon(string classId)
        {
            return classId == "werewolf" ? "ui_cls_werewolf" : classId == "hunter" ? "ui_cls_hunter" : "ui_cls_vampire";
        }

        int SlotCount()
        {
            return ui.Catalog.Slots.Count > 0 ? ui.Catalog.Slots.Count : 10;
        }

        /// <summary>Quadrado de um personagem na grade: retrato, nome, classe e nível. Clicar escolhe.</summary>
        void RosterTile(int index, CharacterSave ch, bool selected)
        {
            var color = UiKit.ClassColor(ch.ClassId);
            var t = new FrameBox("bs-ctile" + (selected ? " sel" : "")); UiKit.El("bs-ccell", benchGrid).Add(t);
            var bar = UiKit.El("bs-ctile-accent", t); bar.style.backgroundColor = color;
            var pf = UiKit.El("bs-ctile-portrait", t);
            var tex = PortraitStudio.Portrait(ch.ClassId);
            if (tex != null) { var img = new Image { image = tex, scaleMode = ScaleMode.ScaleAndCrop }; img.AddToClassList("bs-portrait-img"); pf.Add(img); }
            UiKit.Lbl(ch.Name, "bs-ctile-name", t);
            var cl = UiKit.Lbl(Skills.Class(ch.ClassId).Name.ToUpperInvariant() + " · N" + ch.Level, "bs-ctile-cls", t); cl.style.color = color;
            if (ch.PointsFree > 0) UiKit.Lbl(ch.PointsFree + " pts", "bs-ctile-pts", t);
            int i = index;
            t.AddManipulator(new Clickable(() => { if (!selected) ui.SelectCharacter(i); }));
        }

        /// <summary>Vaga livre: silhueta com "+"; clicar abre a janela de criar personagem.</summary>
        void EmptyTile()
        {
            bool open = recruitPop != null;
            var t = new FrameBox("bs-ctile empty" + (open ? " sel" : "")); UiKit.El("bs-ccell", benchGrid).Add(t);
            var pf = UiKit.El("bs-ctile-portrait empty", t);
            var sil = UiKit.Icon("ui_person", "bs-ctile-sil", pf); sil.Color = new Color(1f, 1f, 1f, 0.14f);
            var add = UiKit.El("bs-hex", pf); var ai = UiKit.Icon("ui_hex_add", "bs-hex-ic", add); ai.Color = UiKit.Hex(0xd9a441);
            UiKit.Lbl("Vaga livre", "bs-ctile-name dim", t);
            UiKit.Lbl("Criar personagem", "bs-ctile-cls dim", t);
            t.AddManipulator(new Clickable(() => { if (recruitPop == null) { recruitPick = recruitPick ?? "vampire"; OpenRecruit(); } else CloseRecruit(); }));
        }

        /* ---------------- criar personagem ---------------- */

        void OpenRecruit()
        {
            if (recruitPop == null) { recruitPop = new FrameBox("bs-recruit"); rightCol.Add(recruitPop); recruitPop.style.top = 0f; }
            Render();
        }

        void CloseRecruit()
        {
            if (recruitPop != null) { recruitPop.RemoveFromHierarchy(); recruitPop = null; }
            recruitName = null;
            Render();
        }

        /// <summary>Janela à esquerda da coluna: as 3 classes (retrato, nome, função, texto) + nome + CRIAR.</summary>
        void RenderRecruit()
        {
            if (recruitPop == null) return;
            string typed = recruitName != null ? recruitName.value : "";
            recruitPop.Clear();
            var hd = UiKit.El("bs-rc-head", recruitPop);
            UiKit.El("bs-rc-bar", hd);
            var tx = UiKit.El("bs-rc-htx", hd);
            UiKit.Lbl("NOVO PERSONAGEM", "bs-rc-title", tx);
            UiKit.Lbl("Escolha a classe e dê um nome", "bs-rc-sub", tx);
            var close = UiKit.El("bs-rc-close", hd); var ci = UiKit.Icon("ui_close", "bs-rc-close-ic", close); ci.Color = UiKit.Hex(0xa1907a);
            close.AddManipulator(new Clickable(CloseRecruit));

            var list = new ScrollView(ScrollViewMode.Vertical); list.AddToClassList("bs-rc-list"); recruitPop.Add(list);
            list.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
            foreach (var cls in Skills.Classes)
            {
                string c = cls.Id; var color = UiKit.ClassColor(c); bool on = recruitPick == c;
                var row = new FrameBox("bs-rc-row" + (on ? " on" : "")); list.Add(row);
                var acc = UiKit.El("bs-rc-accent", row); acc.style.backgroundColor = color;
                var pf = UiKit.El("bs-rc-portrait", row); pf.style.borderTopColor = pf.style.borderBottomColor = pf.style.borderLeftColor = pf.style.borderRightColor = color;
                var img = new Image { image = PortraitStudio.Portrait(c), scaleMode = ScaleMode.ScaleAndCrop }; img.AddToClassList("bs-portrait-img"); pf.Add(img);
                var info = UiKit.El("bs-rc-info", row);
                var nm = UiKit.Lbl(cls.Name.ToUpperInvariant(), "bs-rc-name", info); nm.style.color = color;
                string role; UiKit.Lbl(Role.TryGetValue(c, out role) ? role : cls.Desc, "bs-rc-role", info);
                if (on) UiKit.Lbl(cls.Desc + " Vida " + Mathf.RoundToInt(cls.Health) + " · velocidade " + cls.Speed.ToString("0.0"), "bs-rc-desc", info);
                row.AddManipulator(new Clickable(() => { recruitPick = c; RenderRecruit(); }));
            }

            var nameRow = UiKit.El("bs-rc-namerow", recruitPop);
            UiKit.Lbl("Nome", "bs-rc-role", nameRow);
            recruitName = new TextField { value = typed, maxLength = 20 }; recruitName.AddToClassList("bs-rc-name-field"); nameRow.Add(recruitName);
            var go = UiKit.Btn("CRIAR " + Skills.Class(recruitPick).Name.ToUpperInvariant(), () =>
            {
                string name = recruitName != null ? recruitName.value.Trim() : "";
                string cls = recruitPick;
                CloseRecruit();
                ui.CreateCharacter(cls, name);
            }, "bs-rc-go", recruitPop);
            go.SetEnabled(ui.Profile.Characters.Count < PlayerProfile.MaxCharacters);
        }

        /* ======================= registro ======================= */

        void RenderRecord()
        {
            record.Clear();
            var p = ui.Profile;
            var res = UiKit.El("bs-rec-row", record);
            Stat(res, "st_drop", "Sangue", p.Blood.ToString());
            Stat(res, "inv_materials", "Lã", p.Wool.ToString());
            Stat(res, "ui_moon", "Fragmentos", p.MoonShard.ToString());
            var runs = UiKit.El("bs-rec-row", record);
            Stat(runs, "ui_chevrons", "Incursões", p.Runs.ToString());
            Stat(runs, "st_star", "Extrações", p.Extractions.ToString());
            Stat(runs, "ui_crate", "No baú", p.Stash.Count.ToString());

            var foot = UiKit.El("bs-rec-foot", record);
            UiKit.Lbl("Morrer na Fazenda perde só a bolsa: o que você veste e o baú (de todos os personagens) ficam a salvo.", "bs-rec-note", foot);
            resetBtn = UiKit.Btn(resetArmed ? "Confirmar reset" : "Resetar perfil", ResetClicked, "btn sm ghost danger-text", foot);
        }

        static void Stat(VisualElement parent, string icon, string name, string value)
        {
            var c = UiKit.El("bs-rec", parent);
            var ic = UiKit.Icon(icon, "bs-rec-ic", c); ic.Color = UiKit.Hex(0xd9a441);
            var tx = UiKit.El("bs-rec-tx", c);
            UiKit.Lbl(value, "bs-rec-v", tx);
            UiKit.Lbl(name, "bs-rec-k", tx);
        }

        static Button IconButton(VisualElement parent, string icon, string text, System.Action click, string classes)
        {
            var b = new Button(click) { focusable = false }; UiKit.AddClasses(b, classes); b.text = ""; parent.Add(b);
            var ic = UiKit.Icon(icon, "bs-btn-ic", b);
            ic.Color = classes.Contains("skills") ? UiKit.Hex(0xd9a441) : new Color(0.93f, 0.88f, 0.78f);
            UiKit.Lbl(text, "bs-btn-t", b);
            return b;
        }

        void ResetClicked()
        {
            if (!resetArmed)
            {
                resetArmed = true; resetBtn.text = "Confirmar reset";
                resetBtn.schedule.Execute(() => { resetArmed = false; if (resetBtn != null) resetBtn.text = "Resetar perfil"; }).StartingIn(2500);
                return;
            }

            resetArmed = false;
            ui.ResetProfile();
            ui.Banner("Perfil resetado", "ok");
        }

        /// <summary>A janela de criar personagem conta como janela aberta (Enter não explora com ela aberta).</summary>
        public bool ModalOpen { get { return recruitPop != null; } }

        /// <summary>Esc: fecha a janela de criar personagem.</summary>
        public bool CloseModal()
        {
            if (recruitPop == null) return false;
            CloseRecruit();
            return true;
        }
    }
}
