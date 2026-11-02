using System.Collections.Generic;
using UnityEngine;

namespace VampWolfs
{
    /// <summary>
    /// Prototype HUD drawn with IMGUI (no prefabs or Canvas needed, works in URP).
    /// Health/energy/cast bars over heads, Battlerite-style ability bar, bag panel,
    /// extraction progress, invasion banner and end screens.
    /// Replace with a uGUI/UI Toolkit HUD when the design settles.
    /// </summary>
    public class GameHUD : MonoBehaviour
    {
        static readonly string[] SlotKeys = { "LMB", "RMB", "Espaço", "Q", "E", "R", "F", "Shift", "1", "2" };

        /// <summary>Bar order (nov/2026): weapon (LMB RMB Q E F) · gear (R Space Shift) · class (1 2).</summary>
        static readonly AbilitySlot[] BarOrder =
        {
            AbilitySlot.Primary, AbilitySlot.Secondary, AbilitySlot.Q, AbilitySlot.E, AbilitySlot.Ultimate,
            AbilitySlot.R, AbilitySlot.Mobility, AbilitySlot.Boots,
            AbilitySlot.Class1, AbilitySlot.Class2,
        };
        static readonly string[] BarGroups = { "ARMA", "CAPACETE · PEITO · BOTAS", "CLASSE" };
        static readonly int[] GroupStarts = { 0, 5, 8 };

        static readonly Color PanelColor = new Color(0f, 0f, 0f, 0.55f);
        static readonly Color BarBack = new Color(0.08f, 0.08f, 0.08f, 0.85f);
        static readonly Color PlayerHealth = new Color(0.25f, 0.85f, 0.3f);
        static readonly Color EnemyHealth = new Color(0.9f, 0.25f, 0.2f);
        static readonly Color EnergyColor = new Color(0.55f, 0.45f, 1f);
        static readonly Color CastColor = new Color(1f, 0.8f, 0.3f);
        static readonly Color ExColor = new Color(0.7f, 0.5f, 1f);
        static readonly Color PortalColor = new Color(0.65f, 0.35f, 1f);

        MatchManager match;
        Camera cam;
        bool showHelp = true;
        bool showGear;

        float scale = 1f;
        int stylesBuiltForHeight = -1;
        GUIStyle label;
        GUIStyle labelCenter;
        GUIStyle small;
        GUIStyle smallCenter;
        GUIStyle smallRight;
        GUIStyle banner;
        GUIStyle title;
        GUIStyle floating;
        GUIStyle button;

        public void Init(MatchManager matchManager, Camera camera)
        {
            match = matchManager;
            cam = camera;
        }

        void Update()
        {
            FloatingText.RemoveExpired();

            if (InputReader.HelpPressed())
            {
                showHelp = !showHelp;
            }

            if (InputReader.InventoryPressed())
            {
                showGear = !showGear;
            }

            if (match != null && match.IsOver && InputReader.RestartPressed())
            {
                match.RequestRestart();
            }
        }

        void OnGUI()
        {
            if (match == null || cam == null)
            {
                return;
            }

            EnsureStyles();

            DrawWorldBars();
            DrawFloatingTexts();
            DrawTopStatus();
            DrawTrackers();
            DrawPortalArrow();
            DrawBanner();
            DrawBag();
            DrawExtraction();
            DrawPlayerBars();
            DrawAbilityBar();

            if (showHelp)
            {
                DrawHelp();
            }

            if (showGear && !match.IsOver)
            {
                DrawGearWindow();
            }

            if (match.IsOver)
            {
                DrawEndScreen();
            }
        }

        // ---------------------------------------------------------------- world-space

        void DrawWorldBars()
        {
            for (int i = 0; i < CombatUnit.All.Count; i++)
            {
                CombatUnit unit = CombatUnit.All[i];
                if (unit == null || unit.Health == null || unit.IsDead)
                {
                    continue;
                }

                Vector2 p;
                if (!WorldToGui(unit.Position + Vector3.up * (unit.Controller.height + 0.5f), out p))
                {
                    continue;
                }

                float w = Px(110);
                float h = Px(11);
                float x = p.x - w * 0.5f;
                float y = p.y;

                ShadowLabel(new Rect(x - Px(40), y - Px(22), w + Px(80), Px(20)), unit.DisplayName, smallCenter, Color.white);
                Bar(new Rect(x, y, w, h), unit.Health.Normalized, unit.IsPlayer ? PlayerHealth : EnemyHealth);
                DrawWard(new Rect(x, y, w, h), unit.Health);
                EnergySegments(new Rect(x, y + h + Px(2), w, Px(5)), unit.Energy.Current);

                float nextY = y + h + Px(9);

                if (unit.IsCasting)
                {
                    Bar(new Rect(x, nextY, w, Px(5)), unit.CastProgress, CastColor);
                    ShadowLabel(new Rect(x - Px(40), nextY + Px(4), w + Px(80), Px(18)), unit.CurrentCast.Name, smallCenter, CastColor);
                    nextY += Px(20);
                }

                string status = unit.Status.Describe();
                if (status.Length > 0)
                {
                    ShadowLabel(new Rect(x - Px(40), nextY + Px(2), w + Px(80), Px(18)), status, smallCenter, new Color(1f, 0.9f, 0.5f));
                }
            }

            for (int i = 0; i < Sheep.All.Count; i++)
            {
                Sheep sheep = Sheep.All[i];
                if (sheep == null || sheep.Health == null || sheep.Health.IsDead || sheep.Health.Normalized >= 0.999f)
                {
                    continue;
                }

                Vector2 p;
                if (WorldToGui(sheep.transform.position + Vector3.up * 1.7f, out p))
                {
                    float w = Px(50);
                    Bar(new Rect(p.x - w * 0.5f, p.y, w, Px(6)), sheep.Health.Normalized, new Color(0.9f, 0.9f, 0.8f));
                }
            }
        }

        void DrawFloatingTexts()
        {
            for (int i = 0; i < FloatingText.Entries.Count; i++)
            {
                FloatingText.Entry entry = FloatingText.Entries[i];
                float t = (Time.time - entry.StartTime) / Mathf.Max(0.01f, entry.Duration);

                Vector2 p;
                if (!WorldToGui(entry.Position + Vector3.up * (t * 1.2f), out p))
                {
                    continue;
                }

                Color color = entry.Color;
                color.a = 1f - t * t;
                floating.fontSize = Px(20f * entry.Size);
                ShadowLabel(new Rect(p.x - Px(150), p.y - Px(15), Px(300), Px(30)), entry.Text, floating, color);
            }
        }

        // ---------------------------------------------------------------- screen-space panels

        void DrawTopStatus()
        {
            string text;
            Color color = Color.white;

            switch (match.State)
            {
                case MatchState.Farming:
                    text = "FARMANDO  ·  invasão em " + FormatTime(match.TimeUntilInvasion);
                    color = match.TimeUntilInvasion < match.Settings.InvasionWarning ? new Color(1f, 0.6f, 0.3f) : Color.white;
                    break;
                case MatchState.Invaded:
                    text = match.InvaderDefeated ? "INVASOR DERROTADO  ·  extraia para garantir o loot" : "INVADIDO  ·  derrote o " + match.InvaderClassName.ToLowerInvariant() + " ou extraia";
                    color = match.InvaderDefeated ? new Color(0.6f, 1f, 0.6f) : new Color(1f, 0.45f, 0.3f);
                    break;
                case MatchState.Extracted:
                    text = "EXTRAÍDO";
                    break;
                default:
                    text = "MORTO";
                    break;
            }

            float w = Px(620);
            Rect rect = new Rect((Screen.width - w) * 0.5f, Px(12), w, Px(34));
            DrawRect(rect, PanelColor);
            ShadowLabel(rect, text + "    " + FormatTime(match.Elapsed), labelCenter, color);
        }

        void DrawTrackers()
        {
            CombatUnit player = match.Player;
            if (player == null || player.IsDead)
            {
                return;
            }

            float w = Px(260);
            float x = Screen.width - w - Px(12);
            float y = Px(12);
            List<string> lines = new List<string>();

            if (match.Extraction != null)
            {
                lines.Add("<color=#b98cff>Portal de extração</color>: " + Mathf.RoundToInt(Vector3.Distance(player.Position, match.Extraction.transform.position)) + " m");
            }

            if (match.Invader != null && !match.Invader.IsDead)
            {
                lines.Add("<color=#ff9a40>" + match.InvaderClassName + "</color>: " + Mathf.RoundToInt(Vector3.Distance(player.Position, match.Invader.Position)) + " m");
            }

            for (int i = 0; i < LootBag.Active.Count; i++)
            {
                if (LootBag.Active[i] != null)
                {
                    lines.Add("<color=#ffd24d>Bag no chão</color>: " + Mathf.RoundToInt(Vector3.Distance(player.Position, LootBag.Active[i].transform.position)) + " m");
                }
            }

            lines.Add("<color=#8f86a8>Mapa nº " + match.MapSeed + "</color>");

            Rect rect = new Rect(x, y, w, Px(10) + lines.Count * Px(22));
            DrawRect(rect, PanelColor);
            for (int i = 0; i < lines.Count; i++)
            {
                GUI.Label(new Rect(x + Px(10), y + Px(5) + i * Px(22), w - Px(20), Px(22)), lines[i], label);
            }
        }

        void DrawBanner()
        {
            if (string.IsNullOrEmpty(match.BannerText) || Time.time > match.BannerUntil)
            {
                return;
            }

            float remaining = match.BannerUntil - Time.time;
            Color color = match.BannerColor;
            color.a = Mathf.Clamp01(remaining / 0.6f);

            Rect rect = new Rect(0f, Screen.height * 0.16f, Screen.width, Px(90));
            ShadowLabel(rect, match.BannerText, banner, color);
        }

        void DrawBag()
        {
            CombatUnit player = match.Player;
            if (player == null)
            {
                return;
            }

            float w = Px(290);
            float x = Px(12);
            float lineH = Px(22);
            List<ItemInstance> found = player.Inventory.Items;
            int itemLines = Mathf.Min(found.Count, 4) + (found.Count > 4 ? 1 : 0);
            float h = Px(40) + (Inventory.AllTypes.Length + itemLines) * lineH + Px(78);
            float y = Screen.height - h - Px(12);

            DrawRect(new Rect(x, y, w, h), PanelColor);
            GUI.Label(new Rect(x + Px(10), y + Px(6), w, lineH), "<b>BAG</b> <color=#ff8080>(em risco)</color>   valor: " + player.Inventory.TotalValue, label);

            float ly = y + Px(32);
            for (int i = 0; i < Inventory.AllTypes.Length; i++)
            {
                ItemType type = Inventory.AllTypes[i];
                GUI.Label(new Rect(x + Px(16), ly, w, lineH), Inventory.DisplayName(type) + ":  <b>" + player.Inventory.Count(type) + "</b>", label);
                ly += lineH;
            }

            for (int i = 0; i < found.Count && i < 4; i++)
            {
                GUI.Label(new Rect(x + Px(16), ly, w - Px(20), lineH), "<color=#ffd24d>" + GearCatalog.Instance.NameOf(found[i]) + "</color>", small);
                ly += lineH;
            }

            if (found.Count > 4)
            {
                GUI.Label(new Rect(x + Px(16), ly, w, lineH), "… e mais " + (found.Count - 4), small);
                ly += lineH;
            }

            ly += Px(6);
            GUI.Label(new Rect(x + Px(10), ly, w, lineH), "<b>EQUIPAMENTO</b> <color=#80ff80>(seguro)</color>  Faixa " + player.Equipment.Stats.PowerTier.ToString("0.0") + "  (I)", label);
            GUI.Label(new Rect(x + Px(16), ly + lineH, w, lineH), player.Equipment.Weapon, small);
            GUI.Label(new Rect(x + Px(16), ly + lineH * 1.8f, w, lineH), player.Equipment.Armor, small);
        }

        /// <summary>Purple arrow on the edge of the screen pointing to the portal when it is off screen (big map + mist).</summary>
        void DrawPortalArrow()
        {
            CombatUnit player = match.Player;
            if (match.Extraction == null || player == null || player.IsDead || match.IsOver)
            {
                return;
            }

            Vector2 portal, me;
            bool front = WorldToGui(match.Extraction.transform.position + Vector3.up, out portal);
            WorldToGui(player.Position, out me);
            float margin = Px(56);
            if (front && portal.x > margin && portal.x < Screen.width - margin && portal.y > margin && portal.y < Screen.height - margin)
            {
                return;   // visible: no arrow
            }

            Vector2 dir = front ? portal - me : me - portal;
            if (dir.sqrMagnitude < 1f)
            {
                return;
            }
            dir.Normalize();

            // where the ray from the screen center hits the margin rectangle
            Vector2 center = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
            float sx = (Screen.width * 0.5f - margin) / Mathf.Max(0.001f, Mathf.Abs(dir.x));
            float sy = (Screen.height * 0.5f - margin) / Mathf.Max(0.001f, Mathf.Abs(dir.y));
            Vector2 tip = center + dir * Mathf.Min(sx, sy);
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

            Matrix4x4 old = GUI.matrix;
            GUIUtility.RotateAroundPivot(angle, tip);
            DrawRect(new Rect(tip.x - Px(26), tip.y - Px(4), Px(20), Px(8)), PortalColor);
            GUI.matrix = old;
            GUIUtility.RotateAroundPivot(angle - 45f, tip);   // square with one corner on the tip = arrow head
            DrawRect(new Rect(tip.x - Px(16), tip.y - Px(16), Px(16), Px(16)), PortalColor);
            GUI.matrix = old;

            Vector2 labelPos = tip - dir * Px(52);
            float distance = Vector3.Distance(player.Position, match.Extraction.transform.position);
            ShadowLabel(new Rect(labelPos.x - Px(60), labelPos.y - Px(11), Px(120), Px(22)), "Portal " + Mathf.RoundToInt(distance) + " m", smallCenter, PortalColor);
        }

        void DrawExtraction()
        {
            ExtractionPoint extraction = match.Extraction;
            if (extraction == null || !extraction.IsChanneling)
            {
                return;
            }

            float w = Px(360);
            Rect rect = new Rect((Screen.width - w) * 0.5f, Screen.height * 0.62f, w, Px(18));
            Bar(rect, extraction.Progress01, PortalColor);
            ShadowLabel(new Rect(rect.x, rect.y - Px(26), w, Px(24)), "Extraindo... " + extraction.RemainingTime.ToString("0.0") + "s  (dano interrompe)", labelCenter, PortalColor);
        }

        void DrawPlayerBars()
        {
            CombatUnit player = match.Player;
            if (player == null)
            {
                return;
            }

            float w = AbilityBarWidth();
            float x = (Screen.width - w) * 0.5f;
            float y = Screen.height - Px(142);

            Rect healthRect = new Rect(x, y, w, Px(16));
            Bar(healthRect, player.Health.Normalized, PlayerHealth);
            DrawWard(healthRect, player.Health);
            ShadowLabel(healthRect, Mathf.CeilToInt(player.Health.Current) + " / " + Mathf.RoundToInt(player.Health.MaxHealth), smallCenter, Color.white);

            Rect energyRect = new Rect(x, y + Px(20), w, Px(10));
            EnergySegments(energyRect, player.Energy.Current);
        }

        void DrawAbilityBar()
        {
            CombatUnit player = match.Player;
            if (player == null)
            {
                return;
            }

            bool exHeld = false;   // EX versions are gone (Shift is now the boots skill)
            float size = Px(66);
            float gap = Px(6);
            float x = (Screen.width - AbilityBarWidth()) * 0.5f;
            float y = Screen.height - Px(84);

            for (int b = 0; b < BarOrder.Length; b++)
            {
                int i = (int)BarOrder[b];
                int group = b >= GroupStarts[2] ? 2 : b >= GroupStarts[1] ? 1 : 0;
                Rect rect = new Rect(x + b * (size + gap) + group * Px(18), y, size, size);
                if (b == GroupStarts[group])
                {
                    int count = (group < 2 ? GroupStarts[group + 1] : BarOrder.Length) - GroupStarts[group];
                    ShadowLabel(new Rect(rect.x, y - Px(18), count * (size + gap) - gap, Px(16)), BarGroups[group], smallCenter, new Color(0.75f, 0.68f, 0.85f));
                }
                Ability ability = player.GetAbility((AbilitySlot)i);

                if (ability == null)
                {
                    DrawRect(rect, PanelColor);
                    continue;
                }

                bool showEx = exHeld && ability.Ex != null;
                Ability shown = showEx ? ability.Ex : ability;
                bool affordable = shown.EnergyCost <= 0f || player.Energy.Current + 0.001f >= shown.EnergyCost;
                bool usable = ability.IsReady && affordable && !player.IsDead;

                Color border = showEx ? ExColor : (usable ? new Color(0.9f, 0.9f, 0.9f, 0.9f) : new Color(0.3f, 0.3f, 0.3f, 0.9f));
                DrawRect(rect, border);
                DrawRect(Inset(rect, Px(2)), new Color(0.1f, 0.08f, 0.12f, 0.95f));

                // Ultimate: fills up with energy.
                if ((AbilitySlot)i == AbilitySlot.Ultimate)
                {
                    float fill = Mathf.Clamp01(player.Energy.Current / Energy.Max);
                    Rect inner = Inset(rect, Px(2));
                    DrawRect(new Rect(inner.x, inner.yMax - inner.height * fill, inner.width, inner.height * fill), new Color(0.45f, 0.2f, 0.7f, 0.6f));
                }

                GUI.Label(new Rect(rect.x + Px(5), rect.y + Px(2), size, Px(18)), "<b>" + SlotKeys[i] + "</b>", small);
                GUI.Label(new Rect(rect.x + Px(3), rect.y + Px(28), size - Px(6), Px(44)), shown.Name, smallCenter);

                if (shown.EnergyCost > 0f)
                {
                    Color costColor = affordable ? ExColor : new Color(0.5f, 0.4f, 0.5f);
                    ShadowLabel(new Rect(rect.x, rect.y + Px(2), size - Px(5), Px(18)), Mathf.RoundToInt(shown.EnergyCost).ToString(), smallRight, costColor);
                }

                if (!ability.IsReady)
                {
                    Rect inner = Inset(rect, Px(2));
                    float fraction = ability.CooldownFraction;
                    DrawRect(new Rect(inner.x, inner.y, inner.width, inner.height * fraction), new Color(0f, 0f, 0f, 0.65f));
                    if (ability.CooldownRemaining >= 0.1f)
                    {
                        ShadowLabel(rect, ability.CooldownRemaining.ToString(ability.CooldownRemaining < 1f ? "0.0" : "0"), title, Color.white);
                    }
                }
            }

            if (exHeld)
            {
                ShadowLabel(new Rect(x, y - Px(56), AbilityBarWidth(), Px(20)), "SHIFT: versões EX (custam energia)", smallCenter, ExColor);
            }
        }

        /// <summary>Read-only view of the worn gear and stats during a match (changes only at the Base).</summary>
        void DrawGearWindow()
        {
            CombatUnit player = match.Player;
            if (player == null) return;

            GearCatalog catalog = GearCatalog.Instance;
            CharacterStats st = player.Equipment.Stats;

            float w = Px(430);
            float h = Px(520);
            Rect r = new Rect(Screen.width - w - Px(12), Px(80), w, h);
            DrawRect(r, new Color(0.05f, 0.04f, 0.08f, 0.9f));

            string text = "<b>EQUIPAMENTO</b>  (I fecha · trocar só na Base)\n\n";
            foreach (SlotDef slot in catalog.Slots)
            {
                ItemInstance item;
                player.Equipment.Worn.TryGetValue(slot.Id, out item);
                text += "<color=#a99fbf>" + slot.Name + ":</color> " + (item != null ? catalog.NameOf(item) : "—") + "\n";
            }

            text += "\nFOR " + st.Eff.Str + "  DES " + st.Eff.Dex + "  INT " + st.Eff.Int + "  VIT " + st.Eff.Vit + "\n" +
                    "Vida " + Mathf.RoundToInt(player.Health.MaxHealth) + "  ·  Égide " + Mathf.RoundToInt(player.Health.MaxWard) + "\n" +
                    "Dano mágico " + Mathf.RoundToInt(st.DamageMult(DamageType.Magic, catalog.Rules) * 100f) + "%  ·  " +
                    "Redução física " + Mathf.RoundToInt(st.PhysReductionPct) + "%\n" +
                    "Conjuração " + Mathf.RoundToInt(st.CastMult * 100f) + "% do tempo  ·  Velocidade " + Mathf.RoundToInt(st.MoveMult * 100f) + "%";

            GUI.Label(new Rect(r.x + Px(12), r.y + Px(10), w - Px(24), h - Px(20)), text, small);
        }

        void DrawHelp()
        {
            string text =
                "<b>CONTROLES</b>  (H esconde)\n" +
                "WASD mover · mouse mira\n" +
                "LMB segurar: ataque básico (gera energia)\n" +
                "Arma: RMB / Q / E · F: ultimate (100 de energia)\n" +
                "Capacete: R · Peito: Espaço · Botas: Shift\n" +
                "Classe: 1 e 2 (escolha no lobby)\n" +
                "C: cancelar conjuração · I: equipamento\n\n" +
                "<b>OBJETIVO</b>\n" +
                "Mate ovelhas → itens vão para a BAG.\n" +
                "Morrer = perde a bag, mantém o equipamento.\n" +
                "Fique no portal roxo para extrair.";

            float w = Px(330);
            float h = Px(250);
            Rect rect = new Rect(Px(12), Px(12), w, h);
            DrawRect(rect, PanelColor);
            GUI.Label(Inset(rect, Px(10)), text, small);
        }

        void DrawEndScreen()
        {
            DrawRect(new Rect(0f, 0f, Screen.width, Screen.height), new Color(0f, 0f, 0f, 0.6f));

            float w = Px(600);
            float h = Px(440);
            Rect panel = new Rect((Screen.width - w) * 0.5f, (Screen.height - h) * 0.5f, w, h);
            DrawRect(panel, new Color(0.07f, 0.05f, 0.1f, 0.95f));

            bool won = match.State == MatchState.Extracted;
            string heading = won ? "EXTRAÍDO COM SUCESSO" : "VOCÊ MORREU";
            Color headingColor = won ? new Color(0.65f, 1f, 0.65f) : new Color(1f, 0.35f, 0.3f);
            ShadowLabel(new Rect(panel.x, panel.y + Px(18), w, Px(44)), heading, banner, headingColor);

            string body;
            if (won)
            {
                body = "Loot garantido:\n";
                foreach (ItemType type in Inventory.AllTypes)
                {
                    int count;
                    match.FinalLoot.TryGetValue(type, out count);
                    body += Inventory.DisplayName(type) + ": " + count + "\n";
                }
                for (int i = 0; i < match.FinalItems.Count && i < 6; i++)
                {
                    body += "<color=#ffd24d>" + GearCatalog.Instance.NameOf(match.FinalItems[i]) + "</color>\n";
                }

                if (match.FinalItems.Count > 6) body += "… e mais " + (match.FinalItems.Count - 6) + "\n";
                if (match.FinalItems.Count > 0) body += "<color=#80ff80>Tudo guardado no baú da Base.</color>\n";

                body += "\nValor total: <b>" + Inventory.ValueOf(match.FinalLoot) + "</b>" +
                        (match.InvaderDefeated ? "\nVocê derrotou o invasor!" : "");
                body += "\n<color=#c8a8ff>+" + match.XpEarned + " XP nesta incursão</color>";
            }
            else
            {
                body = "Sua BAG caiu no chão: " + match.LastDropOnGround + " itens dropados, " +
                       match.LastDropDestroyed + " destruídos.\n" +
                       "<color=#80ff80>Seu equipamento foi mantido.</color>\n\n" +
                       "Dica: leia o telegraph vermelho do invasor e saia dele;\n" +
                       "guarde o deslocamento (Espaço/Shift) para quando ele pular em você.";
            }

            GUI.Label(new Rect(panel.x + Px(30), panel.y + Px(76), w - Px(60), h - Px(140)), body, labelCenter);

            Rect buttonRect = new Rect(panel.x + (w - Px(260)) * 0.5f, panel.yMax - Px(62), Px(260), Px(42));
            if (GUI.Button(buttonRect, "Voltar à Base (Enter)", button))
            {
                match.RequestRestart();
            }
        }

        // ---------------------------------------------------------------- drawing helpers

        float AbilityBarWidth()
        {
            return BarOrder.Length * Px(66) + (BarOrder.Length - 1) * Px(6) + 2 * Px(18);
        }

        void EnergySegments(Rect rect, float energy)
        {
            int segments = Mathf.RoundToInt(Energy.Max / Energy.BarSize);
            float gap = Px(3);
            float segmentWidth = (rect.width - gap * (segments - 1)) / segments;

            for (int s = 0; s < segments; s++)
            {
                Rect segment = new Rect(rect.x + s * (segmentWidth + gap), rect.y, segmentWidth, rect.height);
                float fill = Mathf.Clamp01((energy - s * Energy.BarSize) / Energy.BarSize);
                Bar(segment, fill, fill >= 1f ? EnergyColor : new Color(0.35f, 0.3f, 0.6f));
            }
        }

        /// <summary>Égide (ward) drawn as a light-blue strip on the top half of a health bar.</summary>
        void DrawWard(Rect healthRect, Health health)
        {
            if (health.MaxWard > 0f && health.Ward > 0f)
            {
                float fill = Mathf.Clamp01(health.Ward / health.MaxHealth);
                DrawRect(new Rect(healthRect.x, healthRect.y, healthRect.width * fill, healthRect.height * 0.45f), new Color(0.55f, 0.85f, 1f, 0.9f));
            }

            // temporary shield from skills (Escudo de Éter, Halo…): golden strip at the bottom of the bar
            if (health.Shield > 0f)
            {
                float fill = Mathf.Clamp01(health.Shield / health.MaxHealth);
                DrawRect(new Rect(healthRect.x, healthRect.y + healthRect.height * 0.6f, healthRect.width * fill, healthRect.height * 0.4f), new Color(1f, 0.85f, 0.4f, 0.95f));
            }
        }

        void Bar(Rect rect, float fill, Color color)
        {
            DrawRect(rect, BarBack);
            DrawRect(new Rect(rect.x, rect.y, rect.width * Mathf.Clamp01(fill), rect.height), color);
        }

        static void DrawRect(Rect rect, Color color)
        {
            Color old = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = old;
        }

        static Rect Inset(Rect rect, float amount)
        {
            return new Rect(rect.x + amount, rect.y + amount, rect.width - amount * 2f, rect.height - amount * 2f);
        }

        void ShadowLabel(Rect rect, string text, GUIStyle style, Color color)
        {
            Color original = style.normal.textColor;

            style.normal.textColor = new Color(0f, 0f, 0f, color.a * 0.85f);
            GUI.Label(new Rect(rect.x + Px(1.5f), rect.y + Px(1.5f), rect.width, rect.height), text, style);

            style.normal.textColor = color;
            GUI.Label(rect, text, style);

            style.normal.textColor = original;
        }

        bool WorldToGui(Vector3 world, out Vector2 gui)
        {
            Vector3 screen = cam.WorldToScreenPoint(world);
            gui = new Vector2(screen.x, Screen.height - screen.y);
            return screen.z > 0f;
        }

        int Px(float value)
        {
            return Mathf.RoundToInt(value * scale);
        }

        static string FormatTime(float seconds)
        {
            int total = Mathf.CeilToInt(seconds);
            return (total / 60) + ":" + (total % 60).ToString("00");
        }

        void EnsureStyles()
        {
            if (stylesBuiltForHeight == Screen.height && label != null)
            {
                return;
            }

            stylesBuiltForHeight = Screen.height;
            scale = Mathf.Max(0.6f, Screen.height / 1080f);

            label = new GUIStyle(GUI.skin.label) { fontSize = Px(16), richText = true };
            label.normal.textColor = Color.white;

            labelCenter = new GUIStyle(label) { alignment = TextAnchor.MiddleCenter, wordWrap = true };

            small = new GUIStyle(label) { fontSize = Px(13), wordWrap = true };
            smallCenter = new GUIStyle(small) { alignment = TextAnchor.MiddleCenter };
            smallRight = new GUIStyle(small) { alignment = TextAnchor.UpperRight };

            banner = new GUIStyle(label) { fontSize = Px(30), alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold, wordWrap = true };
            title = new GUIStyle(label) { fontSize = Px(26), alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
            floating = new GUIStyle(label) { fontSize = Px(20), alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };

            button = new GUIStyle(GUI.skin.button) { fontSize = Px(18), fontStyle = FontStyle.Bold };
        }
    }
}
