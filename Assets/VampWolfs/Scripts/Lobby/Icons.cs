using System.Collections.Generic;

namespace VampWolfs.Lobby
{
    /// <summary>Ícone vetorial no formato SVG do protótipo (viewBox 0 0 24 24, traço em "currentColor").</summary>
    public struct IconDef
    {
        public string Markup; public float StrokeWidth;
        /// <summary>Tamanho do viewBox (24 nos ícones de traço; 40 nos de equipamento).</summary>
        public float ViewBox;
        /// <summary>true = cores próprias do SVG (ícones de equipamento); false = traço na cor do elemento.</summary>
        public bool Colored;
        public IconDef(string markup, float strokeWidth) { Markup = markup; StrokeWidth = strokeWidth; ViewBox = 24; Colored = false; }
        public IconDef(string markup, float viewBox, bool colored) { Markup = markup; StrokeWidth = 1; ViewBox = viewBox; Colored = colored; }
    }

    /// <summary>
    /// Ícones copiados do protótipo (60_ui.js ICONS, 65_tree_ui.js TALENT_ICONS, 66_inventory_ui.js INV_ICONS).
    /// Os de inventário têm o prefixo "inv_". Desenhados por <see cref="SvgIcon"/>, sem imagens.
    /// </summary>
    public static class Icons
    {
        public static readonly Dictionary<string, IconDef> All = new Dictionary<string, IconDef>
        {
            { "cone", new IconDef("<path d=\"M12 21 3 6a12 12 0 0 1 18 0Z\"/><path d=\"M12 21v-6\"/>", 1.9f) },
            { "strike", new IconDef("<path d=\"m4 20 9-9\"/><path d=\"m13 11 2-6 5-1-1 5-6 2\"/><path d=\"m6 14 4 4\"/>", 1.9f) },
            { "arc", new IconDef("<path d=\"M4 15a8 8 0 0 1 16 0\"/><path d=\"m17 11 3 4-4 1\"/>", 1.9f) },
            { "rhythm", new IconDef("<path d=\"M5 14a7 7 0 0 1 14 0\"/><path d=\"M8 14a4 4 0 0 1 8 0\"/><circle cx=\"12\" cy=\"15\" r=\"1.5\" fill=\"currentColor\"/>", 1.9f) },
            { "drop", new IconDef("<path d=\"M12 3s6 7 6 11a6 6 0 0 1-12 0c0-4 6-11 6-11Z\"/><circle cx=\"12\" cy=\"15\" r=\"1.6\" fill=\"currentColor\"/>", 1.9f) },
            { "shieldbreak", new IconDef("<path d=\"M12 3 5 6v6c0 4.5 3 7.5 7 9 4-1.5 7-4.5 7-9V6Z\"/><path d=\"m11 7 2 4-2 2 2 4\"/>", 1.9f) },
            { "star", new IconDef("<path d=\"m12 3 2.6 5.6 6 .7-4.5 4.1 1.2 6L12 16.4 6.7 19.4l1.2-6L3.4 9.3l6-.7Z\"/>", 1.9f) },
            { "charge", new IconDef("<path d=\"m4 6 5 6-5 6\"/><path d=\"m10 6 5 6-5 6\"/><path d=\"m16 6 5 6-5 6\"/>", 1.9f) },
            { "flame", new IconDef("<path d=\"M12 3c1 4 6 6 6 11a6 6 0 0 1-12 0c0-3 2-5 3-6 0 2 1 3 2 3 0-3-1-5 1-8Z\"/><path d=\"M12 13c1 1.5 2 2.5 2 4a2 2 0 0 1-4 0c0-1.5 1-2.5 2-4Z\"/>", 1.9f) },
            { "shield", new IconDef("<path d=\"M12 3 5 6v6c0 4.5 3 7.5 7 9 4-1.5 7-4.5 7-9V6Z\"/><path d=\"M12 7v10\"/>", 1.9f) },
            { "scream", new IconDef("<path d=\"M9 5a4 4 0 0 0-4 4v3l-1 2h2v2a2 2 0 0 0 2 2h2v2\"/><path d=\"M15 9a3 3 0 0 1 0 6\"/><path d=\"M18 6a7 7 0 0 1 0 12\"/>", 1.9f) },
            { "elem", new IconDef("<path d=\"M8 14c0-3 3-4 3-8 3 2 5 5 5 8a4 4 0 0 1-8 0Z\"/><path d=\"m17 4-2 4h3l-2 4\"/><path d=\"M5 18h2M6 17v2\"/>", 1.9f) },
            { "plus", new IconDef("<circle cx=\"12\" cy=\"12\" r=\"9\"/><path d=\"M12 7v10M7 12h10\"/>", 1.9f) },
            { "heart", new IconDef("<path d=\"M12 20s-7-4.5-7-10a4 4 0 0 1 7-2.5A4 4 0 0 1 19 10c0 5.5-7 10-7 10Z\"/>", 1.9f) },
            { "hammer", new IconDef("<path d=\"m14 6 4 4\"/><path d=\"M11 3h7l3 3-3 3-7-6Z\"/><path d=\"m12 7-8 8 3 3 8-8\"/>", 1.9f) },
            { "clap", new IconDef("<path d=\"M8 13 5 10a1.5 1.5 0 0 1 2-2l4 4\"/><path d=\"m9 9-2-3a1.5 1.5 0 0 1 2.5-1.5L14 10\"/><path d=\"M14 10 12 6.5A1.5 1.5 0 0 1 14.5 5l3 6c1.5 3 .5 6.5-2.5 8s-6 .5-7.5-2L5 14\"/><path d=\"M19 3l1-1M21 6h1\"/>", 1.9f) },
            { "swirl", new IconDef("<path d=\"M12 12a2 2 0 1 1 2-2 4 4 0 1 1-4-4 6 6 0 1 1-6 6\"/>", 1.9f) },
            { "burst", new IconDef("<path d=\"m12 2 2 6 6-3-3 6 6 2-6 2 3 6-6-3-2 6-2-6-6 3 3-6-6-2 6-2-3-6 6 3Z\"/>", 1.9f) },
            { "hourglass", new IconDef("<path d=\"M7 3h10M7 21h10\"/><path d=\"M8 3c0 5 8 5 8 9s-8 4-8 9\"/><path d=\"M16 3c0 5-8 5-8 9s8 4 8 9\"/>", 1.9f) },
            { "people", new IconDef("<circle cx=\"12\" cy=\"7\" r=\"3\"/><circle cx=\"5.5\" cy=\"9\" r=\"2.2\"/><circle cx=\"18.5\" cy=\"9\" r=\"2.2\"/><path d=\"M7 20a5 5 0 0 1 10 0\"/><path d=\"M2 19a3.5 3.5 0 0 1 5-3M22 19a3.5 3.5 0 0 0-5-3\"/>", 1.9f) },
            { "helm", new IconDef("<path d=\"M5 13a7 7 0 0 1 14 0v4l-3 3H8l-3-3Z\"/><path d=\"M12 6v8\"/><path d=\"M8 13h3M13 13h3\"/>", 1.9f) },
            { "banner", new IconDef("<path d=\"M6 21V3\"/><path d=\"M6 4h11l-2 4 2 4H6\"/>", 1.9f) },
            { "skull", new IconDef("<path d=\"M5 11a7 7 0 0 1 14 0c0 2-1 3.5-2 4.5V19H7v-3.5C6 14.5 5 13 5 11Z\"/><circle cx=\"9.5\" cy=\"11\" r=\"1.5\"/><circle cx=\"14.5\" cy=\"11\" r=\"1.5\"/><path d=\"M10 19v-2M14 19v-2\"/>", 1.9f) },
            { "target", new IconDef("<circle cx=\"12\" cy=\"12\" r=\"8\"/><circle cx=\"12\" cy=\"12\" r=\"3\"/><path d=\"M12 2v4M12 18v4M2 12h4M18 12h4\"/>", 1.9f) },
            { "horn", new IconDef("<path d=\"M4 10v4h3l9 5V5L7 10Z\"/><path d=\"M19 9a4 4 0 0 1 0 6\"/>", 1.9f) },
            { "ration", new IconDef("<rect x=\"4\" y=\"9\" width=\"16\" height=\"10\" rx=\"2\"/><path d=\"M8 9V6h8v3\"/><path d=\"M12 12v4M10 14h4\"/>", 1.9f) },
            { "eye", new IconDef("<path d=\"M2 12s4-7 10-7 10 7 10 7-4 7-10 7S2 12 2 12Z\"/><circle cx=\"12\" cy=\"12\" r=\"3\"/>", 1.9f) },
            { "hands", new IconDef("<path d=\"m3 12 4-4 4 2 3-2 4 1 3 3\"/><path d=\"m7 8 5 6 2-1 2 2\"/><path d=\"m3 12 5 5 2-1\"/>", 1.9f) },
            { "shard", new IconDef("<path d=\"M12 2 16 9 12 22 8 9Z\"/><path d=\"M8 9h8\"/><path d=\"m5 8 2 3-2 6M19 8l-2 3 2 6\"/>", 1.9f) },
            { "snow", new IconDef("<path d=\"M12 2v20M3.3 7l17.4 10M20.7 7 3.3 17\"/><path d=\"m9 4 3 2 3-2M9 20l3-2 3 2M4 10.5l3.5-.5L6 6.8M20 13.5l-3.5.5 1.5 3.2M4 13.5l3.5.5L6 17.2M20 10.5l-3.5-.5L18 6.8\"/>", 1.9f) },
            { "crown", new IconDef("<path d=\"m3 8 4 4 5-7 5 7 4-4-2 11H5Z\"/><path d=\"M5 19h14\"/>", 1.9f) },
            { "taunt", new IconDef("<path d=\"M12 3 4 6v6c0 5 3.5 8 8 9 4.5-1 8-4 8-9V6Z\"/><path d=\"m8 11 4-3 4 3M8 15l4-3 4 3\"/>", 2f) },
            { "up", new IconDef("<path d=\"M12 20V5M6 11l6-6 6 6\"/><path d=\"M5 20h14\"/>", 2f) },
            { "shot", new IconDef("<circle cx=\"12\" cy=\"12\" r=\"7\"/><circle cx=\"12\" cy=\"12\" r=\"2\" fill=\"currentColor\"/><path d=\"M12 2v4M12 18v4M2 12h4M18 12h4\"/>", 2f) },
            { "boom", new IconDef("<path d=\"m12 2 2 6 6-3-3 6 6 2-6 2 3 6-6-3-2 6-2-6-6 3 3-6-6-2 6-2-3-6 6 3Z\"/>", 2f) },
            { "heal", new IconDef("<circle cx=\"12\" cy=\"12\" r=\"9\"/><path d=\"M12 7v10M7 12h10\"/>", 2.2f) },
            { "stop", new IconDef("<path d=\"M8 3h8l5 5v8l-5 5H8l-5-5V8Z\"/><path d=\"M7 17 17 7\"/>", 2f) },
            { "inv_head", new IconDef("<path d=\"M5 15a7 7 0 0 1 14 0v3H5z\"/><path d=\"M8.5 15h7\"/><path d=\"M12 8v3\"/>", 1.6f) },
            { "inv_chest", new IconDef("<path d=\"M8 4 4 7v6l3 2v5h10v-5l3-2V7l-4-3-2 2h-4z\"/>", 1.6f) },
            { "inv_gloves", new IconDef("<path d=\"M7 21v-6l-2-5 2-1 2 3V5h2v6V4h2v7V5h2v7l1-3 2 1-2 7v4z\"/>", 1.6f) },
            { "inv_boots", new IconDef("<path d=\"M8 3h5v10l6 3v5H8z\"/><path d=\"M8 17h11\"/>", 1.6f) },
            { "inv_handR", new IconDef("<path d=\"M2 11h13l2-2h5v3h-5l-2 2v5h-3v-4H2z\"/>", 1.6f) },
            { "inv_handL", new IconDef("<path d=\"M12 3l8 3v6c0 5-4 8-8 9-4-1-8-4-8-9V6z\"/><path d=\"M12 7v10\"/>", 1.6f) },
            { "inv_accMajor", new IconDef("<circle cx=\"12\" cy=\"12\" r=\"5\"/><path d=\"M12 2v4M12 18v4M2 12h4M18 12h4\"/>", 1.6f) },
            { "inv_acc", new IconDef("<path d=\"M12 3l6 9-6 9-6-9z\"/><path d=\"M6 12h12\"/>", 1.6f) },
            { "inv_equipment", new IconDef("<path d=\"M8 4 4 7v6l3 2v5h10v-5l3-2V7l-4-3-2 2h-4z\"/>", 1.6f) },
            { "inv_materials", new IconDef("<path d=\"M6 20l4-15 8 5-3 10z\"/><path d=\"M10 5l1 15\"/>", 1.6f) },
            { "inv_essences", new IconDef("<path d=\"M12 3c4 5 6 8 6 11a6 6 0 0 1-12 0c0-3 2-6 6-11z\"/><path d=\"M9.5 15a2.5 2.5 0 0 0 2.5 2.5\"/>", 1.6f) },
            { "inv_upgrades", new IconDef("<path d=\"M12 3l3 6 6 1-4.5 4 1 6-5.5-3-5.5 3 1-6L3 10l6-1z\"/>", 1.6f) },
            // materiais de melhoria (out/2026): minério de refino, cristal de atributo, chave de encaixe, chave de engaste
            { "up_ore", new IconDef("<path d=\"M5 15l3-7 6-3 5 5-1 7-7 3z\"/><path d=\"M8 8l4 4 7-2M12 12l-1 8\"/><path d=\"M17 3v3M15.5 4.5h3\"/>", 1.6f) },
            { "up_crystal", new IconDef("<path d=\"M12 2l5 6-5 14-5-14z\"/><path d=\"M7 8h10M12 2v20\"/>", 1.6f) },
            { "up_key", new IconDef("<circle cx=\"7.5\" cy=\"12\" r=\"3.5\"/><path d=\"M11 12h10M17 12v3M20 12v2\"/>", 1.6f) },
            // gemas (out/2026): Espinélio lapidado em almofada e o minério bruto com uma face lapidada
            { "up_gem", new IconDef("<path d=\"M8 3h8l5 5v8l-5 5H8l-5-5V8z\"/><path d=\"M9 8h6l2 4-2 4H9l-2-4z\"/><path d=\"M8 3l1 5M16 3l-1 5M21 12h-4M3 12h4M8 21l1-5M16 21l-1-5\"/>", 1.5f) },
            { "up_gem_ore", new IconDef("<path d=\"M4 15l2-7 6-4 6 3 2 7-5 6-7-1z\"/><path d=\"M10 10l4-1 2 4-3 3-3-2z\"/>", 1.6f) },
            { "up_gemkey", new IconDef("<path d=\"M7 4h10l3 4-8 12L4 8z\"/><path d=\"M4 8h16M9 4l3 4 3-4\"/>", 1.6f) },
            { "inv_resources", new IconDef("<path d=\"M4 17l3-8 6-3 6 5 1 6-8 3z\"/><path d=\"M7 9l5 4 7-2M12 13v7\"/>", 1.6f) },            // ícones da ficha de status do Arsenal
            { "st_heart", new IconDef("<path fill=\"currentColor\" d=\"M12 21s-8-5.2-8-11a4.5 4.5 0 0 1 8-2.8A4.5 4.5 0 0 1 20 10c0 5.8-8 11-8 11z\"/>", 1.8f) },
            { "st_shield", new IconDef("<path d=\"M12 3l8 3v6c0 5-4 8-8 9-4-1-8-4-8-9V6z\"/>", 1.8f) },
            { "st_bolt", new IconDef("<path fill=\"currentColor\" d=\"M13 2 4 14h7l-1 8 9-12h-7z\"/>", 1.8f) },
            { "st_drop", new IconDef("<path fill=\"currentColor\" d=\"M12 3c4 5 6 8 6 11a6 6 0 0 1-12 0c0-3 2-6 6-11z\"/>", 1.8f) },
            { "st_star", new IconDef("<path fill=\"currentColor\" d=\"M12 2l2.6 6.6L21 9l-5 4.4L17.5 21 12 17l-5.5 4 1.5-7.6L3 9l6.4-.4z\"/>", 1.8f) },
            { "st_speed", new IconDef("<path d=\"M4 8h9M2 12h11M4 16h9\"/><path d=\"M14 5l7 7-7 7\"/>", 1.8f) },   // velocidade de movimento (botas)
            { "st_dodge", new IconDef("<path d=\"M4 18c6 0 10-4 12-12M10 20c5-1 8-4 10-9\"/>", 1.8f) },
            { "st_es", new IconDef("<circle cx=\"12\" cy=\"12\" r=\"8\"/><path d=\"M12 6l3 6-3 6-3-6z\"/>", 1.8f) },
            { "st_elem", new IconDef("<circle cx=\"12\" cy=\"12\" r=\"8\"/><path d=\"M12 4v16M4 12h16\"/>", 1.8f) },
            // tela inicial (out/2026): painel, cards da party, recrutar e explorar
            { "ui_bot", new IconDef("<circle cx=\"12\" cy=\"13\" r=\"7\"/><circle cx=\"12\" cy=\"13\" r=\"2.4\" fill=\"currentColor\"/><path d=\"M12 6V3.5\"/><circle cx=\"12\" cy=\"2.6\" r=\"1.1\" fill=\"currentColor\"/><path d=\"M3 11.5v3M21 11.5v3\"/><path d=\"M9 21.5h6\"/>", 1.6f) },
            { "ui_crate", new IconDef("<path d=\"M3 8l9-4 9 4v9l-9 4-9-4z\"/><path d=\"M3 8l9 4 9-4M12 12v9\"/><path d=\"M7.5 6l9 4v4\"/>", 1.6f) },
            // VampWolfs (vitoriano): lua, morcego (familiar) e o símbolo da classe Vampiro
            { "ui_moon", new IconDef("<path fill=\"currentColor\" d=\"M14.5 3a8.5 8.5 0 1 0 6.5 12.6A7 7 0 0 1 14.5 3z\"/>", 1.6f) },
            { "ui_bat", new IconDef("<path fill=\"currentColor\" d=\"M12 9.2c-.8 0-1.3.5-1.6 1.1L9.9 8.6l-.4 1.8C7.6 11.6 5 12 2 11.5c1.7 1.3 2.6 3.1 2.6 5.2 1.3-.9 2.8-1.2 4.2-.8.5-1.1 1.3-1.7 2.1-1.8.3.9.6 1.4 1.1 1.4s.8-.5 1.1-1.4c.8.1 1.6.7 2.1 1.8 1.4-.4 2.9-.1 4.2.8 0-2.1.9-3.9 2.6-5.2-3 .5-5.6.1-7.5-1.1l-.4-1.8-.5 1.7c-.3-.6-.8-1.1-1.6-1.1z\"/>", 1.4f) },
            { "ui_cls_vampire", new IconDef("<path fill=\"currentColor\" d=\"M3 4c2.8 2.2 5.8 3.2 9 3.2S18.2 6.2 21 4c-.6 6-3.2 10.8-7 13.6V12l-1 3-1-4-1 4-1-3v5.6C6.2 14.8 3.6 10 3 4z\"/>", 1.6f) },
            { "ui_grid", new IconDef("<rect x=\"4\" y=\"4\" width=\"6.5\" height=\"6.5\"/><rect x=\"13.5\" y=\"4\" width=\"6.5\" height=\"6.5\"/><rect x=\"4\" y=\"13.5\" width=\"6.5\" height=\"6.5\"/><rect x=\"13.5\" y=\"13.5\" width=\"6.5\" height=\"6.5\"/>", 1.8f) },
            { "ui_wrench", new IconDef("<path d=\"M14.5 6.5a4 4 0 0 0 5 5L21 13l-2 2-1.5-1.5-8 8a2 2 0 0 1-3-3l8-8L13 9l2-2z\"/><path d=\"M4 4l5 5M6 4H4v2\"/>", 1.7f) },
            { "ui_people", new IconDef("<circle cx=\"9\" cy=\"8\" r=\"3.5\" fill=\"currentColor\"/><path fill=\"currentColor\" d=\"M2.5 20c0-4 3-6.5 6.5-6.5s6.5 2.5 6.5 6.5z\"/><circle cx=\"17\" cy=\"9\" r=\"2.6\" fill=\"currentColor\"/><path fill=\"currentColor\" d=\"M15.5 13.6c3.5 0 6 2 6 5.4h-4.3c0-2-.6-3.8-1.7-5.4z\"/>", 1.4f) },
            // Arsenal (out/2026): ícones dos atributos e da ficha do item
            { "st_str", new IconDef("<path d=\"M7 11V7a1.5 1.5 0 0 1 3 0v3\"/><path d=\"M10 10V6a1.5 1.5 0 0 1 3 0v4\"/><path d=\"M13 10V7a1.5 1.5 0 0 1 3 0v4\"/><path d=\"M16 11V9a1.5 1.5 0 0 1 3 0v4c0 4-3 7-7 7h-1c-3 0-5-2-5-5v-2a1.5 1.5 0 0 1 3 0\"/>", 1.8f) },
            { "st_dex", new IconDef("<path d=\"M20 4 9 15\"/><path d=\"M20 4c-6 0-10 2-12 6l-1 4 4-1c4-2 6-6 9-9Z\"/><path d=\"m4 20 5-5\"/>", 1.8f) },
            { "st_int", new IconDef("<path d=\"M9 4a3 3 0 0 0-3 3 3 3 0 0 0-2 5 3 3 0 0 0 2 5 3 3 0 0 0 3 3 2 2 0 0 0 3-1V5a2 2 0 0 0-3-1Z\"/><path d=\"M15 4a3 3 0 0 1 3 3 3 3 0 0 1 2 5 3 3 0 0 1-2 5 3 3 0 0 1-3 3 2 2 0 0 1-3-1\"/>", 1.8f) },
            { "st_vit", new IconDef("<path d=\"M12 3l8 3v6c0 5-4 8-8 9-4-1-8-4-8-9V6Z\"/><path d=\"M12 15s-3-1.8-3-4a1.6 1.6 0 0 1 3-.9 1.6 1.6 0 0 1 3 .9c0 2.2-3 4-3 4Z\"/>", 1.8f) },
            { "ui_lock", new IconDef("<rect x=\"5\" y=\"11\" width=\"14\" height=\"9\" rx=\"1.5\"/><path d=\"M8 11V8a4 4 0 0 1 8 0v3\"/>", 1.8f) },
            { "ui_plus", new IconDef("<path d=\"M12 6v12M6 12h12\"/>", 1.8f) },
            { "ui_person", new IconDef("<circle cx=\"12\" cy=\"8.5\" r=\"4\"/><path d=\"M4.5 21c.4-4.5 3.6-7.3 7.5-7.3s7.1 2.8 7.5 7.3\"/>", 1.2f) },
            { "ui_add", new IconDef("<path d=\"M12 6v12M6 12h12\"/>", 2.2f) },
            { "ui_chevrons", new IconDef("<path d=\"m4 5 7 7-7 7\"/><path d=\"m12 5 7 7-7 7\"/>", 2.6f) },
            { "ui_cls_tank", new IconDef("<path fill=\"currentColor\" d=\"M12 2.5 4.5 5.5v6c0 5 3.3 8.4 7.5 10 4.2-1.6 7.5-5 7.5-10v-6z\"/>", 1.6f) },
            { "ui_cls_dps", new IconDef("<path fill=\"currentColor\" d=\"M12 2.5c1.2 4.5 7 6.7 7 12.3a7 7 0 0 1-14 0c0-3.4 2.3-5.6 3.5-6.8 0 2.3 1.2 3.4 2.3 3.4 0-3.4-1.2-5.7 1.2-8.9z\"/>", 1.6f) },
            { "ui_cls_technomancer", new IconDef("<path fill=\"currentColor\" d=\"M13 2 4 14h6l-1 8 9-12h-6z\"/>", 1.6f) },
            { "ui_cls_toxologist", new IconDef("<path fill=\"currentColor\" d=\"M9 3h6v2h-1v4.2l5.2 8.6A2.1 2.1 0 0 1 17.4 21H6.6a2.1 2.1 0 0 1-1.8-3.2L10 9.2V5H9z\"/>", 1.6f) },
            // Toxologista (out/2026): veneno, nuvem, âncora, empurrão
            { "toxin", new IconDef("<path d=\"M12 3c3.5 4.5 5.5 7.5 5.5 10.5a5.5 5.5 0 0 1-11 0C6.5 10.5 8.5 7.5 12 3z\"/><circle cx=\"10\" cy=\"13\" r=\"1\"/><circle cx=\"14\" cy=\"15\" r=\"1\"/><path d=\"M10.5 17.5h3\"/>", 1.6f) },
            { "cloud", new IconDef("<path d=\"M7 18a4 4 0 0 1-.6-8A5.5 5.5 0 0 1 17 8.5a4.5 4.5 0 0 1 .5 9z\"/><path d=\"M9 13h.01M13 12h.01M15 15h.01\"/>", 1.6f) },
            { "anchor", new IconDef("<circle cx=\"12\" cy=\"5\" r=\"2\"/><path d=\"M12 7v14M8 11h8M5 14a7 7 0 0 0 14 0\"/>", 1.6f) },
            { "push", new IconDef("<path d=\"M4 12h11M11 7l5 5-5 5\"/><path d=\"M19 5v14\"/>", 1.6f) },
            { "ui_cls_healer", new IconDef("<path fill=\"currentColor\" d=\"M9.5 3h5v6.5H21v5h-6.5V21h-5v-6.5H3v-5h6.5z\"/>", 1.6f) },
            // Bruiser (out/2026): martelo de breacher; e ícones das skills novas (gancho, corrente, perigo, parede, gravidade)
            { "ui_cls_bruiser", new IconDef("<path fill=\"currentColor\" d=\"M4 3h10l3 3-3 3h-3v12H7V9H4z\"/>", 1.6f) },
            { "hook", new IconDef("<path d=\"M12 3v9\"/><path d=\"M7 13a5 5 0 0 0 10 0\"/><path d=\"m5 11 2 2 2-2\"/><path d=\"m15 11 2 2 2-2\"/><circle cx=\"12\" cy=\"3.5\" r=\"1\" fill=\"currentColor\"/>", 1.9f) },
            { "chain", new IconDef("<path d=\"M9 15H7a3 3 0 0 1 0-6h2\"/><path d=\"M15 9h2a3 3 0 0 1 0 6h-2\"/><path d=\"M8 12h8\"/>", 1.9f) },
            { "hazard", new IconDef("<path d=\"M12 3 2.5 20h19Z\"/><path d=\"M12 9v5\"/><circle cx=\"12\" cy=\"17\" r=\"1\" fill=\"currentColor\"/>", 1.9f) },
            { "wall", new IconDef("<path d=\"M4 6h16v12H4Z\"/><path d=\"M4 10h16M4 14h16\"/><path d=\"M9 6v4M15 10v4M9 14v4\"/>", 1.9f) },
            { "gravity", new IconDef("<circle cx=\"12\" cy=\"12\" r=\"2.5\"/><path d=\"M4.5 9a8 8 0 0 1 15 0\"/><path d=\"M19.5 15a8 8 0 0 1-15 0\"/><path d=\"m17 6 2.5 3L22 7\"/>", 1.9f) },
            // Gravitante, Enxamista e Estrategista (out/2026): classes e skills
            { "ui_cls_gravitant", new IconDef("<circle cx=\"12\" cy=\"12\" r=\"4\" fill=\"currentColor\"/><path d=\"M2.5 12a9.5 4.2 0 0 1 19 0\"/><path d=\"M21.5 12a9.5 4.2 0 0 1-19 0\"/>", 1.8f) },
            { "ui_cls_swarmer", new IconDef("<circle cx=\"12\" cy=\"6\" r=\"3.2\" fill=\"currentColor\"/><circle cx=\"5.5\" cy=\"17\" r=\"3.2\" fill=\"currentColor\"/><circle cx=\"18.5\" cy=\"17\" r=\"3.2\" fill=\"currentColor\"/>", 1.6f) },
            { "ui_cls_strategist", new IconDef("<path fill=\"currentColor\" d=\"M12 2l2.6 5.6 6.1.7-4.5 4.2 1.2 6L12 15.6 6.6 18.5l1.2-6L3.3 8.3l6.1-.7z\"/>", 1.6f) },
            { "drone", new IconDef("<circle cx=\"12\" cy=\"12\" r=\"2.5\"/><path d=\"M10 10 7 7M14 10l3-3M10 14l-3 3M14 14l3 3\"/><circle cx=\"5.5\" cy=\"5.5\" r=\"2.3\"/><circle cx=\"18.5\" cy=\"5.5\" r=\"2.3\"/><circle cx=\"5.5\" cy=\"18.5\" r=\"2.3\"/><circle cx=\"18.5\" cy=\"18.5\" r=\"2.3\"/>", 1.6f) },
            { "order", new IconDef("<path d=\"M5 9l7-5 7 5\"/><path d=\"M5 14l7-5 7 5\"/><path d=\"M5 19l7-5 7 5\"/>", 1.9f) },
            { "crate", new IconDef("<path d=\"M3 8l9-4 9 4v9l-9 4-9-4z\"/><path d=\"M3 8l9 4 9-4M12 12v9\"/><path d=\"M7.5 6l9 4v4\"/>", 1.6f) },
            { "dome", new IconDef("<path d=\"M3 18a9 9 0 0 1 18 0\"/><path d=\"M2 18h20\"/><path d=\"M8 18a4 7 0 0 1 8 0\"/><path d=\"M12 9v9\"/>", 1.8f) },
            { "orbital", new IconDef("<circle cx=\"12\" cy=\"15\" r=\"5\"/><path d=\"M12 10V9M12 21v-1M7 15H6M18 15h-1\"/><path d=\"M12 2v7M9 6l3 3 3-3\"/>", 1.8f) },
            { "medevac", new IconDef("<path d=\"M3 6h18M12 6v3\"/><path d=\"M6 12a6 3.5 0 0 1 12 0v2a2 2 0 0 1-2 2H8a2 2 0 0 1-2-2z\"/><path d=\"M12 11v4M10 13h4\"/><path d=\"M8 19.5h8\"/>", 1.7f) },
            { "turret", new IconDef("<path d=\"M8 20h8l-1-6H9z\"/><rect x=\"8\" y=\"9\" width=\"8\" height=\"5\" rx=\"1\"/><path d=\"M16 11h5\"/>", 1.8f) },
            { "emp", new IconDef("<circle cx=\"12\" cy=\"12\" r=\"9\"/><path d=\"M13 5.5l-4 7h4l-2 6 5-8.5h-4z\"/>", 1.7f) },
            { "beam", new IconDef("<path d=\"M12 2v14M9 2v11M15 2v11\"/><path d=\"M5.5 20a6.5 3 0 0 1 13 0\"/>", 1.8f) },
            { "ui_swap", new IconDef("<path d=\"M7 4v15M3.5 15.5 7 19l3.5-3.5\"/><path d=\"M17 20V5M13.5 8.5 17 5l3.5 3.5\"/>", 1.9f) },
            { "ui_close", new IconDef("<path d=\"M6 6l12 12M18 6 6 18\"/>", 2.2f) },
            { "ui_chevron_down", new IconDef("<path d=\"m5 8 7 7 7-7\"/>", 2.6f) },
            { "ui_hex_add", new IconDef("<path d=\"M12 2.5 20.2 7.25v9.5L12 21.5 3.8 16.75v-9.5z\"/><path d=\"M12 8.2v7.6M8.2 12h7.6\"/>", 1.7f) },
            { "ui_gear", new IconDef("<circle cx=\"12\" cy=\"12\" r=\"3\"/><path d=\"M12 2v3M12 19v3M2 12h3M19 12h3M4.9 4.9l2.1 2.1M17 17l2.1 2.1M4.9 19.1 7 17M17 7l2.1-2.1\"/>", 1.8f) },
        };

        /// <summary>Procura nos ícones de traço e nos de equipamento ("gear_&lt;id&gt;").</summary>
        public static bool TryGet(string name, out IconDef def)
        {
            if (All.TryGetValue(name ?? "", out def)) return true;
            string markup;
            if (name != null && name.StartsWith("gear_") && GearIcons.Markup.TryGetValue(name.Substring(5), out markup)) { def = new IconDef(markup, 40, true); return true; }
            return false;
        }
    }
}
