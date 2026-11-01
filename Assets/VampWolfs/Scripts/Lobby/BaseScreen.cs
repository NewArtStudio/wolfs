using UnityEngine;
using UnityEngine.UIElements;

namespace VampWolfs.Lobby
{
    /// <summary>
    /// Tela inicial do lobby, copiada da Base Espacial (mesmo desenho e classes bs- do Space.uss), em versão VampWolfs:
    ///  - painel da esquerda: nome do jogo, texto e os atalhos INVENTÁRIO e FAMILIAR (familiar = "em breve");
    ///  - painel da direita: PERSONAGEM (1 card, o vampiro: retrato, classe, patente/XP "em breve", Habilidades "em breve",
    ///    Equipamento) e o REGISTRO (recursos guardados, incursões e extrações); embaixo o botão EXPLORAR;
    ///  - EXPLORAR entra direto na Fazenda (por enquanto é o único destino, sem janela de escolha).
    /// Molduras em FrameBox, ícones em Icons ("ui_*"), retrato do PortraitStudio. Só chama comandos do LobbyUI.
    /// </summary>
    public sealed class BaseScreen
    {
        const string ClassId = "vampire";

        readonly LobbyUI ui;
        public readonly VisualElement Root;
        VisualElement partyList, record;
        Label partyCount;
        Button resetBtn;
        bool resetArmed;

        public BaseScreen(LobbyUI ui, VisualElement parent)
        {
            this.ui = ui;
            Root = UiKit.El("base bs", parent);
            Root.pickingMode = PickingMode.Ignore;
            BuildBrand();
            BuildSide();
        }

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
            UiKit.Lbl("Prepare seu vampiro, escolha os equipamentos e desça até a Fazenda. Cace ovelhas, encha a bolsa e fuja pelo ponto de extração antes que o lobisomem invasor tome tudo o que você carrega.", "bs-body", col);
            var tiles = UiKit.El("bs-tiles", brand);
            Tile(tiles, "ui_crate", "INVENTÁRIO", "Gerencie itens\ne equipamentos.", ui.OpenArsenal, false);
            Tile(tiles, "ui_bat", "FAMILIAR", "Morcego ou corvo\nque recolhe o saque.", () => ui.Banner("Familiar: em breve", "warn"), true).style.marginLeft = 14;
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
            var right = UiKit.El("bs-right", Root);
            var side = new FrameBox("bs-side"); right.Add(side);
            var h = UiKit.El("bs-sec-h", side);
            UiKit.El("bs-sec-bar", h);
            UiKit.Lbl("PERSONAGEM", "bs-sec-t", h);
            partyCount = UiKit.Lbl("1/1", "bs-chip", h);
            partyList = UiKit.El("bs-party", side);

            // REGISTRO: o que já está guardado (no lugar da Contingência do Space, que aqui ainda não existe)
            var rh = new FrameBox("bs-bench-h"); side.Add(rh);
            var ri = UiKit.Icon("ui_crate", "bs-bench-ic", rh); ri.Color = UiKit.Hex(0xd9a441);
            UiKit.Lbl("REGISTRO", "bs-bench-t", rh);
            record = UiKit.El("bs-record", side);

            UiKit.El("bs-spacer", right);
            var ex = new Button(ui.Explore) { focusable = false }; ex.AddToClassList("bs-explore"); right.Add(ex);
            ex.text = "";
            var exi = UiKit.Icon("ui_chevrons", "bs-explore-ic", ex); exi.Color = UiKit.Hex(0x2a1606);
            UiKit.Lbl("EXPLORAR", "bs-explore-t", ex);
        }

        /// <summary>Refaz o card e o registro (chamado pelo LobbyUI.RenderBase).</summary>
        public void Render()
        {
            partyCount.text = "1/1";
            partyList.Clear();
            Card(partyList);
            RenderRecord();
        }

        /// <summary>Card do vampiro: retrato, classe, patente e XP ("em breve"), Habilidades ("em breve") e Equipamento.</summary>
        void Card(VisualElement parent)
        {
            var color = UiKit.ClassColor(ClassId);
            var card = new FrameBox("bs-card"); parent.Add(card);
            var accent = UiKit.El("bs-card-accent", card); accent.style.backgroundColor = color;
            var top = UiKit.El("bs-card-top", card);

            var pf = UiKit.El("bs-portrait", top);
            var tex = PortraitStudio.Portrait();
            if (tex != null) { var img = new Image { image = tex, scaleMode = ScaleMode.ScaleAndCrop }; img.AddToClassList("bs-portrait-img"); pf.Add(img); }
            else { var sil = UiKit.Icon("ui_person", "bs-portrait-sil", pf); sil.Color = new Color(1f, 1f, 1f, 0.25f); }
            UiKit.Lbl("1", "bs-slot", pf);

            var info = UiKit.El("bs-info", top);
            var nameRow = UiKit.El("bs-name-row", info);
            UiKit.Lbl("Vampiro", "bs-name", nameRow);
            var cic = UiKit.Icon("ui_cls_vampire", "bs-cls-ic", nameRow); cic.Color = color;
            var cl = UiKit.Lbl("VAMPIRO · MAGIA DE SANGUE", "bs-cls", info); cl.style.color = color;
            UiKit.Lbl("Patente · em breve", "bs-rank soon", info);
            var xpRow = UiKit.El("bs-xp-row", info);
            var bar = new Bar("bs-xp soon"); bar.Set(0f); xpRow.Add(bar);
            UiKit.Lbl("XP · em breve", "bs-xp-t", xpRow);

            // poder do equipamento (o que a invasão usa para escolher o lobisomem)
            var catalog = ui.Catalog;
            var st = CharacterStats.Compute(catalog, ui.Profile.EquippedMap());
            int hp = Mathf.RoundToInt(CharacterFactory.VampireHealth * st.HpMult);
            UiKit.Lbl("Faixa de poder " + st.PowerTier.ToString("0.0") + " · Vida " + hp + " · Dano mágico " + Mathf.RoundToInt(st.DamageMult(DamageType.Magic, catalog.Rules) * 100f) + "%", "bs-power", card);

            var btns = UiKit.El("bs-btns", card);
            var bh = IconButton(btns, "ui_grid", "Habilidades · em breve", () => ui.Banner("Habilidades: em breve", "warn"), "bs-btn skills soon");
            bh.tooltip = "A árvore de habilidades chega numa próxima versão.";
            IconButton(btns, "ui_wrench", "Equipamento (" + ui.Profile.EquippedMap().Count + "/" + SlotCount() + ")", ui.OpenArsenal, "bs-btn gear");
        }

        int SlotCount()
        {
            return ui.Catalog.Slots.Count > 0 ? ui.Catalog.Slots.Count : 10;
        }

        void RenderRecord()
        {
            record.Clear();
            var p = ui.Profile;
            var res = UiKit.El("bs-rec-row", record);
            Stat(res, "st_drop", "Sangue", p.Blood.ToString());
            Stat(res, "inv_materials", "Lã", p.Wool.ToString());
            Stat(res, "ui_moon", "Fragmento Lunar", p.MoonShard.ToString());
            var runs = UiKit.El("bs-rec-row", record);
            Stat(runs, "ui_chevrons", "Incursões", p.Runs.ToString());
            Stat(runs, "st_star", "Extrações", p.Extractions.ToString());
            Stat(runs, "ui_crate", "Itens no baú", p.Stash.Count.ToString());

            var foot = UiKit.El("bs-rec-foot", record);
            UiKit.Lbl("Morrer na Fazenda perde só a bolsa: o que você veste e o baú ficam a salvo.", "bs-rec-note", foot);
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

        /// <summary>O lobby do VampWolfs não abre janelas próprias na tela inicial (só o Arsenal).</summary>
        public bool ModalOpen { get { return false; } }

        /// <summary>Esc na tela inicial: nada a fechar.</summary>
        public bool CloseModal() { return false; }
    }
}
