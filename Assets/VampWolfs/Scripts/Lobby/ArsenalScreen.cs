using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace VampWolfs.Lobby
{
    /// <summary>
    /// Arsenal do lobby (copiado do Space, ligado ao equipamento do VampWolfs): o vampiro no meio do cenário com os
    /// 10 slots em volta ligados por linhas, o status do personagem e o BAÚ (filtro por categoria, busca, ordenar).
    /// Arraste do baú para o slot (ou duplo clique) para vestir; arraste do slot para a grade para tirar;
    /// arraste um material (lingote, cristal, chave, gema) até uma peça para melhorá-la. Só na Base.
    /// As regras ficam no PlayerProfile / GearUpgrades; esta tela só mostra e chama.
    /// </summary>
    public sealed class ArsenalScreen
    {
        sealed class Sel { public string Kind, Id; }        // Kind: slot | inv
        sealed class DropTarget { public string Type, Id; } // Type: slot | grid | cell (Id = uid do item na célula)

        readonly LobbyUI ui;
        readonly VisualElement chars;   // aba com o personagem escolhido (nome, classe, nível)
        readonly VisualElement modal, colL, colR, rowB, stats, ctabs, grid, detailBox, stageView;
        readonly Label invCap, stageName, stageSub, stageNote;
        readonly DollLines dollLines;
        readonly ItemTooltip tooltip;
        readonly TextField search;
        FrameBox upgPop;
        bool spinning; float spinX;
        Sel sel; string tab = "all", query = ""; bool delArmed;
        readonly Dictionary<string, VisualElement> slotEls = new Dictionary<string, VisualElement>();

        // arrastar e soltar
        Sel dragFrom; Vector2 dragStart; bool dragging; SvgIcon ghost;
        // duplo clique feito à mão: o clique simples refaz a grade, então o clickCount do UI Toolkit não chega a 2
        string lastClickUid; float lastClickTime;
        const float DoubleClickTime = 0.35f;

        PlayerProfile P { get { return ui.Profile; } }
        GearCatalog C { get { return ui.Catalog; } }
        public bool IsOpen { get { return UiKit.Visible(modal); } }

        /// <summary>Abas de categoria do baú (id, nome, ícone).</summary>
        static readonly string[][] Tabs =
        {
            new[] { "all", "Tudo", "" }, new[] { "weapon", "Armas", "inv_equipment" }, new[] { "armor", "Vestes", "inv_chest" },
            new[] { "accessory", "Joias", "inv_acc" }, new[] { "material", "Materiais", "inv_upgrades" },
        };
        const int MinCells = 48;

        public ArsenalScreen(LobbyUI ui, VisualElement root)
        {
            this.ui = ui;
            modal = UiKit.El("modal arsenal", root);
            var card = UiKit.El("ars-card", modal);
            var top = UiKit.El("ars-top", card);
            var brand = UiKit.El("ars-brand", top);
            UiKit.Lbl("Mansão · Arsenal", "kicker", brand);
            UiKit.Lbl("Equipamento & Baú", "h2", brand);
            chars = UiKit.El("ars-chars", top);
            UiKit.Btn("Fechar (Esc)", Close, "btn sm ghost", top);

            var body = UiKit.El("ars-body", card);
            // personagem livre no cenário (a mesma pintura do palco), slots em volta ligados por linhas
            var doll = UiKit.El("ars-doll", body);
            var bg = Resources.Load<Texture2D>("Base/BaseBackdrop");
            if (bg != null) { var bgi = UiKit.El("doll-bg", doll); bgi.style.backgroundImage = new StyleBackground(bg); }
            UiKit.El("doll-shade", doll);
            var dh = UiKit.El("doll-head", doll); UiKit.El("doll-head-bar", dh); UiKit.Lbl("EQUIPAMENTOS", "doll-title", dh);
            dollLines = new DollLines(); doll.Add(dollLines);
            var stage = UiKit.El("doll-stage", doll);
            colL = UiKit.El("doll-col left", doll);
            stageView = UiKit.El("stage-view", stage);
            stageView.RegisterCallback<PointerDownEvent>(ev => { if (ev.button == 0) { spinning = true; spinX = ev.position.x; ev.StopPropagation(); } });
            stageView.RegisterCallback<PointerMoveEvent>(ev => { if (!spinning) return; PortraitStudio.Spin((spinX - ev.position.x) * 0.6f); spinX = ev.position.x; });
            stageView.RegisterCallback<PointerUpEvent>(ev => spinning = false);
            stageView.RegisterCallback<PointerLeaveEvent>(ev => spinning = false);
            var who = UiKit.El("doll-id", doll);
            stageName = UiKit.Lbl("", "stage-name", who);
            stageSub = UiKit.Lbl("", "stage-sub", who);
            NoPick(who);
            stageNote = UiKit.Lbl("Arraste o personagem para girar.", "stage-note", stage);
            colR = UiKit.El("doll-col right", doll);
            rowB = UiKit.El("doll-row", doll);

            var statsBox = new FrameBox("ars-stats"); body.Add(statsBox);
            var statsScroll = new ScrollView(ScrollViewMode.Vertical); statsScroll.AddToClassList("ars-stats-scroll"); statsBox.Add(statsScroll);
            stats = statsScroll.contentContainer;

            var inv = new FrameBox("ars-inv"); body.Add(inv);
            var ih = UiKit.El("inv-head", inv);
            UiKit.Lbl("BAÚ DA MANSÃO", "ars-sec-title", ih);
            invCap = UiKit.Lbl("", "inv-cap", ih);
            ctabs = UiKit.El("inv-ctabs", inv);
            var tools = UiKit.El("inv-tools", inv);
            search = new TextField(); search.AddToClassList("inv-search"); tools.Add(search);
            search.RegisterValueChangedCallback(ev => { query = (ev.newValue ?? "").Trim().ToLowerInvariant(); RenderInv(); });
            UiKit.Btn("Ordenar", () => { SortStash(); ui.Save(); RenderInv(); }, "btn sm ghost", tools);
            var gridScroll = new ScrollView(ScrollViewMode.Vertical); gridScroll.AddToClassList("inv-scroll"); inv.Add(gridScroll);
            grid = UiKit.El("inv-grid ars", gridScroll);
            grid.userData = new DropTarget { Type = "grid" };
            gridScroll.userData = new DropTarget { Type = "grid" };
            detailBox = UiKit.El("inv-detail ars-detail", inv);

            tooltip = new ItemTooltip(modal);
            modal.RegisterCallback<PointerMoveEvent>(OnPointerMove);
            modal.RegisterCallback<PointerUpEvent>(OnPointerUp);
            UiKit.Show(modal, false);
        }

        /* ---------------- abrir / fechar ---------------- */
        public void Open()
        {
            if (!ui.InBase) return;
            sel = null; delArmed = false;
            UiKit.Show(modal, true);
            modal.BringToFront();
            PortraitStudio.SetPreviewActive(true);
            Render();
        }

        public void Close()
        {
            if (!IsOpen) return;
            CancelDrag();
            search.Blur();   // o campo escondido não pode segurar o foco (bloquearia as teclas na Base)
            UiKit.Show(modal, false); tooltip.Hide(); CloseUpgrade();
            PortraitStudio.SetPreviewActive(false);
            ui.Save(); ui.RenderBase();
        }

        /// <summary>Esc dentro do Arsenal: fecha a janela de melhoria; false = nada a fechar (fecha a tela).</summary>
        public bool Escape()
        {
            if (upgPop != null) { CloseUpgrade(); return true; }
            return false;
        }

        void Render() { RenderChar(); RenderDoll(); RenderStats(); RenderInv(); RenderDetail(); }

        /// <summary>Quem está vestindo (o personagem escolhido no lobby; o baú é de todos).</summary>
        void RenderChar()
        {
            chars.Clear();
            var ch = P.Current; var cls = SkillCatalog.Instance.Class(ch.ClassId);
            var t = UiKit.El("ars-ch on", chars); t.style.borderBottomColor = UiKit.ClassColor(ch.ClassId);
            UiKit.Lbl(ch.Level.ToString(), "key", t);
            var n = UiKit.El("ars-ch-n", t);
            UiKit.Lbl("<b>" + ch.Name + "</b>", "nm", n);
            UiKit.Lbl(cls.Name + " · nível " + ch.Level, "rk", n);
        }
        void GearChanged() { ui.Save(); Render(); }

        /* ---------------- ícones e cores dos itens ---------------- */
        static readonly string[] RomanNums = { "I", "II", "III", "IV", "V", "VI", "VII", "VIII" };
        public static string Roman(int tier) { return tier >= 1 && tier <= RomanNums.Length ? RomanNums[tier - 1] : tier.ToString(); }

        static string SlotIcon(string slotId)
        {
            switch (slotId)
            {
                case "head": return "inv_head";
                case "chest": return "inv_chest";
                case "gloves": return "inv_gloves";
                case "boots": return "inv_boots";
                case "handR": case "handL": return "inv_equipment";
                default: return "inv_acc";
            }
        }

        /// <summary>Ícone de um item (equipamento: desenho próprio; material: pelo que faz).</summary>
        public static string IconFor(GearCatalog catalog, ItemInstance it)
        {
            var e = catalog.GetEquip(it.DefId);
            if (e != null)
            {
                var g = GearIcons.Resolve(e);   // faixas II+ reaproveitam o desenho da peça equivalente
                if (g != null) return "gear_" + g;
                var t = catalog.TypeOf(e);
                return t != null && t.Slots.Length > 0 ? SlotIcon(t.Slots[0]) : "inv_equipment";
            }

            var m = catalog.GetMaterial(it.DefId);
            if (m == null) return "inv_materials";
            switch (m.Kind)
            {
                case MaterialDef.Refine: return "up_ore";
                case MaterialDef.Enchant: return "up_crystal";
                case MaterialDef.UnlockGem: return "up_gemkey";
                case MaterialDef.UnlockCard: return "up_key";
                case MaterialDef.Gem: return "up_gem";
            }
            return "inv_materials";
        }

        /// <summary>Cor do ícone: estilo da peça (equipamento) ou a cor do material.</summary>
        public static Color ColorFor(GearCatalog catalog, ItemInstance it)
        {
            var e = catalog.GetEquip(it.DefId);
            if (e != null) { var c = ItemStyle.ColorOf(e); return c ?? UiKit.Hex(0xe6d6b4); }
            var m = catalog.GetMaterial(it.DefId);
            return m != null ? UiKit.Hex(m.Color) : UiKit.Hex(0xe6d6b4);
        }

        string CategoryOf(ItemInstance it)
        {
            var e = C.GetEquip(it.DefId);
            return e != null ? C.CategoryOf(e) : "material";
        }

        ItemInstance FindStash(string uid) { return P.Stash.Find(i => i.Uid == uid); }

        /// <summary>Peça (vestida ou no baú) pelo uid.</summary>
        ItemInstance FindAny(string uid)
        {
            var it = FindStash(uid); if (it != null) return it;
            foreach (var kv in P.EquippedMap()) if (kv.Value.Uid == uid) return kv.Value;
            return null;
        }

        /* ---------------- boneco (10 slots) ---------------- */
        /// <summary>Onde cada slot fica em volta do personagem (como no Space): 4 à esquerda, 4 à direita, 2 embaixo.</summary>
        static readonly string[] LeftOrder = { "head", "chest", "gloves", "boots" }, RightOrder = { "accMajor", "handR", "handL", "acc1" }, BottomOrder = { "acc2", "acc3" };

        void RenderDoll()
        {
            colL.Clear(); colR.Clear(); rowB.Clear(); slotEls.Clear();
            var eq = P.EquippedMap();
            var placed = new HashSet<string>();
            foreach (var id in LeftOrder) { placed.Add(id); SlotCard(id, eq, colL, false); }
            foreach (var id in RightOrder) { placed.Add(id); SlotCard(id, eq, colR, false); }
            foreach (var id in BottomOrder) { placed.Add(id); SlotCard(id, eq, rowB, true); }
            foreach (var s in C.Slots) if (!placed.Contains(s.Id)) SlotCard(s.Id, eq, rowB, true);   // slots novos nos dados

            var st = CharacterStats.Compute(C, eq);
            var hero = P.Current;
            stageName.text = hero.Name; stageName.style.color = UiKit.ClassColor(hero.ClassId);
            stageSub.text = SkillCatalog.Instance.Class(hero.ClassId).Name + " · faixa de poder " + st.PowerTier.ToString("0.0");
            var tex = PortraitStudio.Preview;
            if (tex != null)
            {
                stageView.style.backgroundImage = new StyleBackground(Background.FromRenderTexture((RenderTexture)tex));
            }
            dollLines.Bind(stageView, slotEls);
        }

        bool HandLBlocked(Dictionary<string, ItemInstance> eq)
        {
            ItemInstance r; return eq.TryGetValue("handR", out r) && C.IsTwoHand(C.GetEquip(r.DefId));
        }

        /// <summary>Cartão de um slot: ícone do item numa moldura, nome do slot, nome do item, +refino e encaixes.</summary>
        void SlotCard(string slotId, Dictionary<string, ItemInstance> eq, VisualElement parent, bool small)
        {
            ItemInstance it; eq.TryGetValue(slotId, out it);
            var ed = it != null ? C.GetEquip(it.DefId) : null;
            bool on = sel != null && sel.Kind == "slot" && sel.Id == slotId;
            bool blocked = slotId == "handL" && it == null && HandLBlocked(eq);
            var b = new FrameBox("eqs" + (small ? " small" : "") + (it != null ? " full" : "") + (on ? " on" : "") + (blocked ? " blocked" : "")); parent.Add(b);
            var icb = new FrameBox("eqs-ic"); b.Add(icb);
            var ic = UiKit.Icon(it != null ? IconFor(C, it) : SlotIcon(slotId), "eqs-svg", icb);
            ic.Color = it != null ? ColorFor(C, it) : UiKit.Hex(0x5a4a3e);
            if (it != null) { var rs = UiKit.El("eqs-rar", icb); rs.style.backgroundColor = ColorFor(C, it); }
            var t = UiKit.El("eqs-t", b);
            UiKit.Lbl(C.SlotName(slotId), "eqs-n", t);
            UiKit.Lbl(ed != null ? ed.Name : blocked ? "Bloqueada · duas mãos" : "Vazio", "eqs-s", t);
            var foot = UiKit.El("eqs-foot", t);
            UiKit.Lbl(ed != null && ed.Refinable ? "+" + it.Refine : "", "eqs-plus", foot);
            int cards = ed != null ? ed.CardSlots : 0;
            for (int i = 0; i < cards; i++) UiKit.El("eqs-sock" + (i < it.CardsOpen ? " open" : ""), foot);
            if (ed != null && ed.GemSlot > 0) UiKit.El("eqs-sock gem" + (it.GemOpen ? " open" : "") + (!string.IsNullOrEmpty(it.Gem) ? " filled" : ""), foot);
            foreach (var c in b.Children()) NoPick(c);
            b.userData = new DropTarget { Type = "slot", Id = slotId };
            string sid = slotId;
            b.RegisterCallback<PointerDownEvent>(ev =>
            {
                if (ev.button != 0) return;
                tooltip.Hide();
                BeginPress(new Sel { Kind = "slot", Id = sid }, ev.position);
                ev.StopPropagation();
            });
            if (it != null)
            {
                b.RegisterCallback<PointerEnterEvent>(ev => { if (!dragging) { var x = P.GetEquipped(sid); if (x != null) tooltip.Show(C, x, null, ev.position); } });
                b.RegisterCallback<PointerMoveEvent>(ev => tooltip.Move(ev.position));
                b.RegisterCallback<PointerLeaveEvent>(ev => tooltip.Hide());
            }
            slotEls[slotId] = b;
        }
        static void NoPick(VisualElement e) { e.pickingMode = PickingMode.Ignore; foreach (var c in e.Children()) NoPick(c); }

        string BestSlot(ItemInstance it)
        {
            var e = C.GetEquip(it.DefId);
            return e != null ? P.SlotFor(C, e) : null;
        }

        void TryEquip(string uid, string slotId)
        {
            if (slotId == null) return;
            string err = P.Equip(C, uid, slotId);
            if (err != null) { ui.Banner(err, "warn", 2.2f); Flash(slotId); return; }
            sel = new Sel { Kind = "slot", Id = slotId };
            GearChanged();
        }

        void TryUnequip(string slotId)
        {
            if (P.GetEquipped(slotId) == null) return;
            P.Unequip(slotId); sel = null; GearChanged();
        }

        void Flash(string slotId)
        {
            VisualElement b; if (!slotEls.TryGetValue(slotId, out b)) return;
            b.AddToClassList("deny"); b.schedule.Execute(() => b.RemoveFromClassList("deny")).StartingIn(600);
        }

        /* ---------------- status (ícone · nome · valor · barra) ---------------- */
        void StatRow(string icon, string label, string val, float frac, string sub)
        {
            var r = UiKit.El("st", stats);
            var line = UiKit.El("st-line", r);
            var ic = UiKit.Icon(icon, "st-ic", line); ic.Color = UiKit.Hex(0xd9a441);
            var lb = UiKit.El("st-lb", line);
            UiKit.Lbl(label, "st-l", lb);
            if (sub != null) UiKit.Lbl(sub, "st-sub", lb);
            UiKit.Lbl(val, "st-v", line);
            var bar = new Bar("st-bar"); bar.SetColor(UiKit.Hex(0xd9a441)); bar.Set(frac); line.Add(bar);
        }

        void SectionTitle(string text) { var h = UiKit.El("st-sec", stats); UiKit.Lbl(text, "st-sec-t", h); }
        static string Pct(float mult) { return Mathf.RoundToInt(mult * 100f) + "%"; }

        void RenderStats()
        {
            stats.Clear();
            var eq = P.EquippedMap();
            var s = CharacterStats.Compute(C, eq);
            var rules = C.Rules;
            var cls = SkillCatalog.Instance.Class(P.Current.ClassId);
            int hp = Mathf.RoundToInt(cls.Health * s.HpMult);
            UiKit.Lbl("STATUS · " + cls.Name.ToUpperInvariant(), "ars-sec-title", stats);
            StatRow("st_heart", "Vida", hp.ToString(), hp / 600f, "base " + cls.Health + " · faixa " + s.PowerTier.ToString("0.0") + " · +" + Mathf.RoundToInt(s.Eff.Vit) + "% pela VIT");
            StatRow("st_bolt", "Dano mágico", Pct(s.DamageMult(DamageType.Magic, rules)), s.DamageMult(DamageType.Magic, rules) / 3f, "arma " + Mathf.RoundToInt(s.WeaponDmg) + " + MATK " + Mathf.RoundToInt(s.Matk) + " (INT)");
            StatRow("st_shield", "Armour", Mathf.RoundToInt(s.Armour) + "%", s.Armour / GearBalance.ArmourCap, "redução física total " + Mathf.RoundToInt(s.PhysReductionPct) + "% · teto " + GearBalance.ArmourCap + "%");
            StatRow("st_dodge", "Evasion", Mathf.RoundToInt(s.Evasion).ToString(), s.Evasion / 60f, "cada ponto: −0,5% de dano físico e +0,3% de velocidade");
            StatRow("st_es", "Égide", Mathf.RoundToInt(s.WardPct * hp) + " (" + Mathf.RoundToInt(s.WardPct * 100f) + "%)", s.WardPct / 0.3f, "escudo que absorve antes da vida e se recarrega");
            StatRow("st_speed", "Velocidade", (cls.Speed * s.MoveMult).ToString("0.0#") + " m/s", (s.MoveMult - 1f) / GearBalance.MoveBonusCap, "base " + cls.Speed + " m/s · +" + Mathf.RoundToInt((s.MoveMult - 1f) * 100f) + "%");
            StatRow("st_star", "Conjuração", Pct(s.CastMult) + " do tempo", 1f - s.CastMult, "DEX encurta a conjuração (mínimo " + Pct(GearBalance.MinCastMult) + ")");
            StatRow("st_elem", "Resist. a efeitos", Mathf.RoundToInt(s.StatusResist * 100f) + "%", s.StatusResist / GearBalance.StatusResistCap, "atordoar e lentidão duram menos (VIT)");

            SectionTitle("ATRIBUTOS");
            AttrRow("st_str", "Força", "STR", s.Gear.Str, s.Eff.Str);
            AttrRow("st_dex", "Destreza", "DEX", s.Gear.Dex, s.Eff.Dex);
            AttrRow("st_int", "Inteligência", "INT", s.Gear.Int, s.Eff.Int);
            AttrRow("st_vit", "Vitalidade", "VIT", s.Gear.Vit, s.Eff.Vit);

            SectionTitle("DERIVADOS");
            var kd = UiKit.El("kv", stats);
            KvCell(kd, "ATK corpo a corpo", "+" + Mathf.RoundToInt(s.AtkMelee) + " (STR)");
            KvCell(kd, "ATK à distância", "+" + Mathf.RoundToInt(s.AtkRanged) + " (DEX)");
            KvCell(kd, "MATK", "+" + Mathf.RoundToInt(s.Matk) + " (INT)");
            KvCell(kd, "DEF / MDEF", Mathf.RoundToInt(s.Def) + " / " + Mathf.RoundToInt(s.Mdef));
            KvCell(kd, "Dano físico perto", Pct(s.DamageMult(DamageType.PhysicalMelee, rules)));
            KvCell(kd, "Dano físico longe", Pct(s.DamageMult(DamageType.PhysicalRanged, rules)));
            KvCell(kd, "Ataque básico", Pct(s.AspdMult) + " do tempo");

            SectionTitle("PROGRESSO");
            var kv = UiKit.El("kv", stats);
            KvCell(kv, "Equipados", eq.Count + "/" + (C.Slots.Count > 0 ? C.Slots.Count : 10));
            KvCell(kv, "Encaixes de carta", s.OpenCardSockets + "/" + s.CardSockets + " abertos");
            KvCell(kv, "Nível e XP", P.Current.Level + " · " + P.Current.Xp + "/" + SkillCatalog.XpToNext(P.Current.Level));
            KvCell(kv, "Pontos de habilidade", P.Current.PointsFree + " livres");
            UiKit.Lbl("Equipamentos dão no máximo " + GearBalance.EquipCap + " por atributo; gemas passam do teto. A faixa de poder (média das faixas dos 10 slots) escolhe o lobisomem que invade a sua Fazenda. Cartas ainda sem efeito.", "note", stats);
        }

        void AttrRow(string icon, string name, string key, float gear, float eff)
        {
            string sub = key + (gear > GearBalance.EquipCap ? " · teto " + GearBalance.EquipCap + " (equip. " + gear + ")" : " · equipamento " + gear + (eff > gear ? " + gema " + (eff - gear) : ""));
            StatRow(icon, name, eff.ToString(), eff / 99f, sub);
        }
        static void KvCell(VisualElement p, string k, string v) { var c = UiKit.El("kv-i half", p); UiKit.Lbl(k, "kv-k", c); UiKit.Lbl(v, "kv-v", c); }

        /* ---------------- Baú ---------------- */
        bool Matches(ItemInstance it)
        {
            if (tab != "all" && CategoryOf(it) != tab) return false;
            if (query.Length > 0 && !C.NameOf(it).ToLowerInvariant().Contains(query)) return false;
            return true;
        }

        static string FmtQty(int q) { return q >= 1000 ? (q / 1000f).ToString(q >= 10000 ? "0" : "0.0") + "k" : q.ToString(); }

        /// <summary>
        /// Seta de "melhor que o vestido": compara com a peça mais fraca entre os slots onde o item entra
        /// (3 joias = a pior das três). Slot vazio não ganha seta (senão toda peça daquele tipo teria).
        /// </summary>
        bool IsUpgrade(ItemInstance it, EquipDef d)
        {
            var t = C.TypeOf(d); if (t == null || t.Slots.Length == 0) return false;
            double weakest = double.MaxValue;
            foreach (var sid in t.Slots)
            {
                var cur = P.GetEquipped(sid); if (cur == null) return false;
                weakest = System.Math.Min(weakest, ItemStyle.Power(C, C.GetEquip(cur.DefId), cur));
            }
            return ItemStyle.Power(C, d, it) > weakest;
        }

        void Cell(ItemInstance it)
        {
            bool on = sel != null && sel.Kind == "inv" && sel.Id == it.Uid;
            var d = C.GetEquip(it.DefId);
            var c = UiKit.El("ic" + (on ? " on" : "") + (Matches(it) ? "" : " dim") + (d != null ? " gear" : ""), grid);
            // cor do ESTILO (o jogo não tem raridade): brilho subindo da base, borda fina e faixa embaixo
            var sc = ItemStyle.ColorOf(d);
            if (sc.HasValue)
            {
                var gl = UiKit.El("ic-glow", c);
                gl.style.backgroundImage = new StyleBackground(ItemStyle.Glow);
                gl.style.unityBackgroundImageTintColor = sc.Value;
                var edge = UiKit.El("ic-edge", c);
                var ec = sc.Value; ec.a = 0.45f;
                edge.style.borderTopColor = ec; edge.style.borderLeftColor = ec; edge.style.borderRightColor = ec; edge.style.borderBottomColor = sc.Value;
            }
            else c.style.borderBottomColor = ColorFor(C, it);
            var ic = UiKit.Icon(IconFor(C, it), "ic-svg", c); ic.Color = ColorFor(C, it);
            if (d != null && IsUpgrade(it, d)) UiKit.El("ic-up", c);   // seta verde: melhor que o vestido
            if (it.Qty > 1) UiKit.Lbl(FmtQty(it.Qty), "qq", c);
            if (d != null) UiKit.Lbl((it.Refine > 0 ? "+" + it.Refine + " " : "") + Roman(d.Tier), "ic-tier", c);
            foreach (var x in c.Children()) x.pickingMode = PickingMode.Ignore;
            string uid = it.Uid;
            c.userData = new DropTarget { Type = "cell", Id = uid };   // soltar material em cima = usar nesta peça
            c.RegisterCallback<PointerEnterEvent>(ev =>
            {
                if (dragging) return;
                var item = FindStash(uid); if (item == null) return;
                string best = BestSlot(item);
                tooltip.Show(C, item, best != null ? P.GetEquipped(best) : null, ev.position);
            });
            c.RegisterCallback<PointerMoveEvent>(ev => tooltip.Move(ev.position));
            c.RegisterCallback<PointerLeaveEvent>(ev => tooltip.Hide());
            c.RegisterCallback<PointerDownEvent>(ev =>
            {
                if (ev.button != 0) return;
                tooltip.Hide();
                bool dbl = lastClickUid == uid && Time.unscaledTime - lastClickTime < DoubleClickTime;
                lastClickUid = uid; lastClickTime = Time.unscaledTime;
                if (dbl) { lastClickUid = null; var item = FindStash(uid); if (item != null && C.GetEquip(item.DefId) != null) { TryEquip(uid, BestSlot(item)); ev.StopPropagation(); return; } }
                BeginPress(new Sel { Kind = "inv", Id = uid }, ev.position);
                ev.StopPropagation();
            });
        }

        void RenderInv()
        {
            invCap.text = "<b>" + P.Stash.Count + "</b> itens";
            ctabs.Clear();
            foreach (var t in Tabs)
            {
                string id = t[0]; int count = 0;
                foreach (var it in P.Stash) if (id == "all" || CategoryOf(it) == id) count++;
                var b = UiKit.El("ctab" + (tab == id ? " on" : ""), ctabs);
                if (t[2].Length > 0) UiKit.Icon(t[2], "ctab-ic", b);
                UiKit.Lbl(t[1] + " <size=10>" + count + "</size>", "ctab-t", b);
                foreach (var x in b.Children()) x.pickingMode = PickingMode.Ignore;
                b.RegisterCallback<PointerDownEvent>(ev => { tab = id; RenderInv(); ev.StopPropagation(); });
            }
            grid.Clear();
            foreach (var it in P.Stash) Cell(it);
            for (int i = P.Stash.Count; i < MinCells; i++) UiKit.El("ic empty", grid).pickingMode = PickingMode.Ignore;
        }

        void SortStash()
        {
            P.Stash.Sort((a, b) =>
            {
                int ca = CatOrder(CategoryOf(a)), cb = CatOrder(CategoryOf(b));
                if (ca != cb) return ca.CompareTo(cb);
                var ea = C.GetEquip(a.DefId); var eb = C.GetEquip(b.DefId);
                int ta = ea != null ? ea.Tier : 0, tb = eb != null ? eb.Tier : 0;
                if (ta != tb) return tb.CompareTo(ta);
                return string.CompareOrdinal(C.NameOf(a), C.NameOf(b));
            });
        }
        static int CatOrder(string c) { return c == "weapon" ? 0 : c == "armor" ? 1 : c == "accessory" ? 2 : 3; }

        /* ---------------- detalhes / ações ---------------- */
        void DetailHeader(string icon, Color color, string name, string sub)
        {
            var dt = UiKit.El("dt", detailBox);
            var big = UiKit.Icon(icon, "dt-big", dt); big.Color = color;
            var tx = UiKit.El("dtt", dt);
            UiKit.Lbl("<b>" + name + "</b>", "dt-n", tx);
            UiKit.Lbl(sub, "dt-s", tx);
        }

        void RenderDetail()
        {
            detailBox.Clear();
            if (sel == null)
            {
                UiKit.Lbl("Selecione um item ou um slot. Arraste um equipamento do baú até o slot do personagem (ou dê duplo clique) para vestir; arraste de volta para a grade para tirar. Arraste um lingote, cristal, chave ou gema até uma peça para melhorá-la.", "dt-empty", detailBox);
                return;
            }
            if (sel.Kind == "slot")
            {
                var it = P.GetEquipped(sel.Id);
                if (it == null)
                {
                    bool blocked = sel.Id == "handL" && HandLBlocked(P.EquippedMap());
                    DetailHeader(SlotIcon(sel.Id), UiKit.Hex(0xa1907a), C.SlotName(sel.Id), blocked ? "Bloqueada pela arma de duas mãos" : "Slot vazio");
                    UiKit.Lbl(blocked ? "Tire a arma de duas mãos para usar a mão esquerda." : "Arraste uma peça compatível do baú.", "dt-p", detailBox);
                    return;
                }
                DetailHeader(IconFor(C, it), ColorFor(C, it), C.NameOf(it), C.SlotName(sel.Id) + " · vestido");
                var acts = UiKit.El("dta", detailBox);
                string sid = sel.Id;
                UiKit.Btn("Tirar", () => TryUnequip(sid), "btn sm", acts);
                return;
            }
            var item = FindStash(sel.Id); if (item == null) { sel = null; RenderDetail(); return; }
            var d = C.GetEquip(item.DefId); var mat = C.GetMaterial(item.DefId);
            string bestSlot = d != null ? BestSlot(item) : null;
            DetailHeader(IconFor(C, item), ColorFor(C, item), C.NameOf(item), d != null ? (C.TypeOf(d) != null ? C.TypeOf(d).Name : d.Type) + " · faixa " + Roman(d.Tier) : (mat != null ? mat.Category : "Item") + " · ×" + item.Qty);
            if (mat != null) UiKit.Lbl(GearUpgrades.Describe(C, mat) + "\n<color=#d9a441>Arraste até uma peça (slot do personagem ou célula do baú) para usar.</color>", "dt-p", detailBox);
            var a = UiKit.El("dta", detailBox);
            string uid = item.Uid;
            if (d != null) UiKit.Btn("Vestir" + (bestSlot != null ? " (" + C.SlotName(bestSlot) + ")" : ""), () => TryEquip(uid, bestSlot), "btn sm primary", a);
            var del = UiKit.Btn(delArmed ? "Confirmar descarte" + (item.Qty > 1 ? " ×" + item.Qty : "") : "Descartar" + (item.Qty > 1 ? " pilha" : ""), null, "btn sm danger", a);
            del.clicked += () =>
            {
                if (!delArmed) { delArmed = true; RenderDetail(); detailBox.schedule.Execute(() => { if (delArmed) { delArmed = false; if (IsOpen) RenderDetail(); } }).StartingIn(2500); return; }
                delArmed = false; P.Stash.Remove(item); sel = null; ui.Save(); Render();
            };
        }

        /* ---------------- arrastar e soltar ---------------- */
        void BeginPress(Sel what, Vector2 panelPos)
        {
            dragFrom = what; dragStart = panelPos; dragging = false;
        }

        void OnPointerMove(PointerMoveEvent ev)
        {
            if (dragFrom == null) return;
            Vector2 p = ev.position;
            if (!dragging)
            {
                if ((p - dragStart).sqrMagnitude < 36f) return;
                ItemInstance it = dragFrom.Kind == "inv" ? FindStash(dragFrom.Id) : P.GetEquipped(dragFrom.Id);
                if (it == null) { dragFrom = null; return; }
                dragging = true; tooltip.Hide();
                ghost = new SvgIcon(IconFor(C, it)); ghost.Color = ColorFor(C, it); ghost.AddToClassList("drag-ghost"); modal.Add(ghost);
                if (dragFrom.Kind == "inv") HintSlots(it);
            }
            var local = modal.WorldToLocal(p);
            ghost.style.left = local.x - 24; ghost.style.top = local.y - 24;
        }

        void OnPointerUp(PointerUpEvent ev)
        {
            if (dragFrom == null) return;
            var from = dragFrom; bool wasDrag = dragging;
            CancelDrag();
            if (!wasDrag)
            {
                sel = from; delArmed = false;   // clique simples: seleciona
                RenderDoll(); RenderInv(); RenderDetail();
                return;
            }
            var target = FindDropTarget(ev.position);
            if (target == null) return;
            if (from.Kind == "inv" && IsMaterial(FindStash(from.Id)))
            {
                // material solto numa peça: abre a confirmação com a prévia
                ItemInstance piece = target.Type == "slot" ? P.GetEquipped(target.Id) : target.Type == "cell" ? FindStash(target.Id) : null;
                if (piece != null && piece.Uid != from.Id && C.GetEquip(piece.DefId) != null) OpenUpgrade(from.Id, piece.Uid);
                else if (target.Type == "slot" || target.Type == "cell") ui.Banner("Solte o material em cima de um equipamento", "warn", 2f);
                return;
            }
            if (from.Kind == "inv" && target.Type == "slot") TryEquip(from.Id, target.Id);
            else if (from.Kind == "slot" && (target.Type == "grid" || target.Type == "cell")) TryUnequip(from.Id);
        }

        DropTarget FindDropTarget(Vector2 panelPos)
        {
            var picked = modal.panel != null ? modal.panel.Pick(panelPos) : null;
            for (var e = picked; e != null; e = e.parent) { var t = e.userData as DropTarget; if (t != null) return t; }
            return null;
        }

        void HintSlots(ItemInstance it)
        {
            var mat = it != null ? C.GetMaterial(it.DefId) : null;
            var e = it != null ? C.GetEquip(it.DefId) : null;
            bool handLBlocked = HandLBlocked(P.EquippedMap());
            foreach (var kv in slotEls)
            {
                bool can = false;
                if (mat != null) { var piece = P.GetEquipped(kv.Key); can = piece != null && GearUpgrades.Check(C, mat, C.GetEquip(piece.DefId), piece).Ok; }
                else if (e != null) can = C.FitsSlot(e, kv.Key) && !(kv.Key == "handL" && handLBlocked);
                kv.Value.EnableInClassList("can", can);
            }
        }

        bool IsMaterial(ItemInstance it) { return it != null && C.GetMaterial(it.DefId) != null; }

        /* ---------------- melhorias: confirmar o uso de um material numa peça ---------------- */
        /// <summary>Janela de confirmação: material → peça, com a prévia do que muda. "Aplicar" mantém a janela aberta para repetir.</summary>
        void OpenUpgrade(string matUid, string pieceUid)
        {
            CloseUpgrade(); tooltip.Hide();
            var mat = FindStash(matUid); var piece = FindAny(pieceUid);
            if (mat == null || piece == null) return;
            var mdef = C.GetMaterial(mat.DefId); var e = C.GetEquip(piece.DefId);
            upgPop = new FrameBox("upg-pop"); modal.Add(upgPop);
            UiKit.Lbl("MELHORIA DE EQUIPAMENTO", "upg-kicker", upgPop);
            var row = UiKit.El("upg-row", upgPop);
            var mi = new FrameBox("upg-ic"); row.Add(mi); var mic = UiKit.Icon(IconFor(C, mat), "upg-svg", mi); mic.Color = ColorFor(C, mat);
            UiKit.Lbl("→", "upg-arrow", row);
            var pi = new FrameBox("upg-ic"); row.Add(pi); var pic = UiKit.Icon(IconFor(C, piece), "upg-svg", pi); pic.Color = ColorFor(C, piece);
            var names = UiKit.El("upg-names", row);
            UiKit.Lbl(mdef.Name + "  <color=#a1907a>×" + mat.Qty + "</color>", "upg-mat", names);
            UiKit.Lbl(C.NameOf(piece), "upg-piece", names);
            var chk = GearUpgrades.Check(C, mdef, e, piece);
            UiKit.Lbl(chk.Message, chk.Ok ? "upg-preview" : "upg-preview warn", upgPop);
            UiKit.Lbl(GearUpgrades.Describe(C, mdef), "upg-desc", upgPop);
            var acts = UiKit.El("upg-acts", upgPop);
            var ok = UiKit.Btn("Aplicar", () =>
            {
                var r = P.ApplyMaterial(C, matUid, pieceUid, Random.value);
                if (r.Broke) ui.Banner(r.Message, "danger", 3f);
                else ui.Banner(r.Ok ? mdef.Name + ": " + r.Message : r.Message, r.Ok ? "ok" : "warn", 1.8f);
                ui.Save(); Render();
                // continua aberta enquanto houver material e a peça existir (quebrou = fecha)
                if (!r.Broke && FindStash(matUid) != null && FindAny(pieceUid) != null) OpenUpgrade(matUid, pieceUid); else CloseUpgrade();
            }, "btn sm primary", acts);
            ok.SetEnabled(chk.Ok);
            UiKit.Btn("Fechar", CloseUpgrade, "btn sm ghost", acts);
            upgPop.RegisterCallback<PointerDownEvent>(ev => ev.StopPropagation());
        }

        void CloseUpgrade() { if (upgPop != null) { upgPop.RemoveFromHierarchy(); upgPop = null; } }

        void CancelDrag()
        {
            dragFrom = null; dragging = false;
            if (ghost != null) { ghost.RemoveFromHierarchy(); ghost = null; }
            HintSlots(null);
        }
    }
}
