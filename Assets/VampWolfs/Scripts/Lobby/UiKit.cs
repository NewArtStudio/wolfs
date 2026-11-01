using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace VampWolfs.Lobby
{
    /// <summary>Atalhos para montar a interface por código (UI Toolkit). Estilos ficam em Space.uss.</summary>
    public static class UiKit
    {
        public static VisualElement El(string classes = null, VisualElement parent = null)
        {
            var e = new VisualElement(); AddClasses(e, classes); if (parent != null) parent.Add(e); return e;
        }
        public static Label Lbl(string text, string classes = null, VisualElement parent = null)
        {
            var l = new Label(text) { enableRichText = true }; AddClasses(l, classes); if (parent != null) parent.Add(l); return l;
        }
        public static Button Btn(string text, Action onClick, string classes = null, VisualElement parent = null)
        {
            var b = new Button(onClick) { text = text, focusable = false }; AddClasses(b, classes); if (parent != null) parent.Add(b);   // sem foco: Enter não "clica" o último botão
            return b;
        }
        public static SvgIcon Icon(string name, string classes = null, VisualElement parent = null)
        {
            var i = new SvgIcon(name); AddClasses(i, classes); if (parent != null) parent.Add(i); return i;
        }
        public static void AddClasses(VisualElement e, string classes)
        {
            if (string.IsNullOrEmpty(classes)) return;
            foreach (var c in classes.Split(' ')) if (c.Length > 0) e.AddToClassList(c);
        }
        public static void Show(VisualElement e, bool on) { e.style.display = on ? DisplayStyle.Flex : DisplayStyle.None; }
        public static bool Visible(VisualElement e) { return e.style.display.keyword == StyleKeyword.Undefined ? e.style.display.value != DisplayStyle.None : e.resolvedStyle.display != DisplayStyle.None; }
        /// <summary>Muda o texto só se mudou (evita refazer layout todo frame).</summary>
        public static void SetText(Label l, string t) { if (l.text != t) l.text = t; }

        public static Color Hex(int hex) { return new Color(((hex >> 16) & 255) / 255f, ((hex >> 8) & 255) / 255f, (hex & 255) / 255f); }
        public static Color Hex(string css)
        {
            Color c; if (!string.IsNullOrEmpty(css) && ColorUtility.TryParseHtmlString(css, out c)) return c;
            return Color.gray;
        }

        /// <summary>Cor de cada classe. VampWolfs: vampiro vinho, lobisomem âmbar, caçador verde (as do Space ficam para referência).</summary>
        public static Color ClassColor(string classId)
        {
            switch (classId) { case "vampire": return Hex(0xc8203c); case "werewolf": return Hex(0xe08a2e); case "hunter": return Hex(0x5f9a4a); case "tank": return Hex(0x4d8dff); case "dps": return Hex(0xff5a4d); case "healer": return Hex(0x3fe08c); case "bruiser": return Hex(0xffb13b); case "toxologist": return Hex(0xb4ff3a); case "technomancer": return Hex(0x5fd4ff); case "gravitant": return Hex(0xa98bff); case "swarmer": return Hex(0xffd84a); case "strategist": return Hex(0xd8e2f0); }
            return Color.gray;
        }
    }

    /// <summary>Barra horizontal (HP, SP, ES, XP, progresso). Fill de 0 a 1.</summary>
    public sealed class Bar : VisualElement
    {
        public readonly VisualElement Fill;
        public readonly Label Text;
        float value = -1;
        public Bar(string classes = null, bool withText = false)
        {
            AddToClassList("bar"); UiKit.AddClasses(this, classes);
            Fill = new VisualElement(); Fill.AddToClassList("bar-fill"); Add(Fill);
            if (withText) { Text = new Label(); Text.AddToClassList("bar-text"); Text.pickingMode = PickingMode.Ignore; Add(Text); }
            pickingMode = PickingMode.Ignore;
        }
        public void Set(float k)
        {
            k = Mathf.Clamp01(k);
            if (Mathf.Abs(k - value) < 0.001f) return;
            value = k; Fill.style.width = Length.Percent(k * 100f);
        }
        public void SetColor(Color c) { Fill.style.backgroundColor = c; }
    }
}
