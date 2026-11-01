using System.Collections.Generic;

namespace VampWolfs.Lobby
{
    /// <summary>
    /// Ícones de inventário de cada equipamento, gerados a partir de gearIconSvg() do protótipo (32_gear_visuals.js).
    /// viewBox 0 0 40 40, cores próprias. Use Icons.TryGet("gear_" + id).
    /// </summary>
    public static class GearIcons
    {
        public static readonly Dictionary<string, string> Markup = new Dictionary<string, string>
        {
            { "rusted_cuirass", "<defs><linearGradient id=\"g_rusted_cuirass\" x1=\"0\" y1=\"0\" x2=\"0\" y2=\"1\"><stop offset=\"0\" stop-color=\"#8a5634\"/><stop offset=\"1\" stop-color=\"#4e2f1e\"/></linearGradient></defs><path fill=\"url(#g_rusted_cuirass)\" stroke=\"#0a0d12\" stroke-width=\"1.2\" stroke-linejoin=\"round\" d=\"M13 5l4-2 3 3 3-3 4 2 6 4-2 7-3-1v16H12V15l-3 1-2-7z\"/><circle cx=\"15\" cy=\"12\" r=\"1.3\" fill=\"#c0c6ce\"/><circle cx=\"25\" cy=\"12\" r=\"1.3\" fill=\"#c0c6ce\"/><path d=\"M14 20h12M14 24h12\" stroke=\"#4e2f1e\" stroke-width=\"1.6\"/><circle cx=\"17\" cy=\"27\" r=\"2\" fill=\"#5e2e14\" opacity=\".8\"/>" },
            { "leather_vest", "<defs><linearGradient id=\"g_leather_vest\" x1=\"0\" y1=\"0\" x2=\"0\" y2=\"1\"><stop offset=\"0\" stop-color=\"#7a5030\"/><stop offset=\"1\" stop-color=\"#3a2516\"/></linearGradient></defs><path fill=\"url(#g_leather_vest)\" stroke=\"#0a0d12\" stroke-width=\"1.2\" stroke-linejoin=\"round\" d=\"M13 5l4-2 3 3 3-3 4 2 6 4-2 7-3-1v16H12V15l-3 1-2-7z\"/><path d=\"M14 9l12 14\" stroke=\"#3a2516\" stroke-width=\"2.2\"/><rect x=\"14\" y=\"24\" width=\"5\" height=\"5\" fill=\"#3a2516\"/><rect x=\"21\" y=\"24\" width=\"5\" height=\"5\" fill=\"#3a2516\"/><path d=\"M20 6v25\" stroke=\"#c9a66b\" stroke-width=\".8\" stroke-dasharray=\"1.5 1.5\"/>" },
            { "tattered_robe", "<defs><linearGradient id=\"g_tattered_robe\" x1=\"0\" y1=\"0\" x2=\"0\" y2=\"1\"><stop offset=\"0\" stop-color=\"#5d4f74\"/><stop offset=\"1\" stop-color=\"#2e2640\"/></linearGradient></defs><path fill=\"url(#g_tattered_robe)\" stroke=\"#0a0d12\" stroke-width=\"1.2\" stroke-linejoin=\"round\" d=\"M12 6l5-3h6l5 3 3 6-4 2-1-3v25l-4-2-2 3-2-3-2 3-2-3-4 2V11l-1 3-4-2z\"/><path fill=\"#2e2640\" d=\"M13 35l3-4 2 4 2-3 2 3 2-4 3 4z\"/><rect x=\"18\" y=\"12\" width=\"4\" height=\"4\" transform=\"rotate(45 20 14)\" fill=\"#7fd8ff\"/>" },
            { "chain_mail", "<defs><linearGradient id=\"g_chain_mail\" x1=\"0\" y1=\"0\" x2=\"0\" y2=\"1\"><stop offset=\"0\" stop-color=\"#b9c0c8\"/><stop offset=\"1\" stop-color=\"#5d636b\"/></linearGradient></defs><path fill=\"url(#g_chain_mail)\" stroke=\"#0a0d12\" stroke-width=\"1.2\" stroke-linejoin=\"round\" d=\"M13 5l4-2 3 3 3-3 4 2 6 4-2 7-3-1v16H12V15l-3 1-2-7z\"/><g stroke=\"#ffffff\" stroke-width=\".8\" fill=\"none\" opacity=\".75\"><path d=\"M13 12h14\"/><path d=\"M13 16h14\"/><path d=\"M13 20h14\"/><path d=\"M13 24h14\"/><path d=\"M13 28h14\"/></g>" },
            { "pilgrim_vestments", "<defs><linearGradient id=\"g_pilgrim_vestments\" x1=\"0\" y1=\"0\" x2=\"0\" y2=\"1\"><stop offset=\"0\" stop-color=\"#d9cba6\"/><stop offset=\"1\" stop-color=\"#7a2a24\"/></linearGradient></defs><path fill=\"url(#g_pilgrim_vestments)\" stroke=\"#0a0d12\" stroke-width=\"1.2\" stroke-linejoin=\"round\" d=\"M12 6l5-3h6l5 3 3 6-4 2-1-3v25l-4-2-2 3-2-3-2 3-2-3-4 2V11l-1 3-4-2z\"/><rect x=\"18\" y=\"10\" width=\"4\" height=\"25\" fill=\"#7a2a24\"/><rect x=\"18\" y=\"13\" width=\"4\" height=\"4\" transform=\"rotate(45 20 15)\" fill=\"#9de8ff\"/>" },
            { "rough_greaves", "<defs><linearGradient id=\"g_rough_greaves\" x1=\"0\" y1=\"0\" x2=\"0\" y2=\"1\"><stop offset=\"0\" stop-color=\"#7a6e60\"/><stop offset=\"1\" stop-color=\"#3e362e\"/></linearGradient></defs><path fill=\"url(#g_rough_greaves)\" stroke=\"#0a0d12\" stroke-width=\"1.2\" stroke-linejoin=\"round\" d=\"M10 4h11v20l9 4v7H10z\"/><path d=\"M10 31h20\" stroke=\"#3e362e\" stroke-width=\"2\"/><path d=\"M13 8l2 2M17 15l1 3M24 29l2-1\" stroke=\"#a99a86\" stroke-width=\"1.2\"/>" },
            { "iron_greaves", "<defs><linearGradient id=\"g_iron_greaves\" x1=\"0\" y1=\"0\" x2=\"0\" y2=\"1\"><stop offset=\"0\" stop-color=\"#6d737b\"/><stop offset=\"1\" stop-color=\"#33373d\"/></linearGradient></defs><path fill=\"url(#g_iron_greaves)\" stroke=\"#0a0d12\" stroke-width=\"1.2\" stroke-linejoin=\"round\" d=\"M10 4h11v20l9 4v7H10z\"/><path d=\"M10 31h20\" stroke=\"#33373d\" stroke-width=\"2\"/><circle cx=\"15.5\" cy=\"13\" r=\"2\" fill=\"#c0c6ce\"/>" },
            { "bronze_greaves", "<defs><linearGradient id=\"g_bronze_greaves\" x1=\"0\" y1=\"0\" x2=\"0\" y2=\"1\"><stop offset=\"0\" stop-color=\"#b07a3e\"/><stop offset=\"1\" stop-color=\"#5a3a1a\"/></linearGradient></defs><path fill=\"url(#g_bronze_greaves)\" stroke=\"#0a0d12\" stroke-width=\"1.2\" stroke-linejoin=\"round\" d=\"M10 4h11v20l9 4v7H10z\"/><path d=\"M10 31h20\" stroke=\"#5a3a1a\" stroke-width=\"2\"/><path d=\"M12 7l7 0M12 18h7\" stroke=\"#ffd08a\" stroke-width=\"1.4\"/>" },
            { "trimmed_greaves", "<defs><linearGradient id=\"g_trimmed_greaves\" x1=\"0\" y1=\"0\" x2=\"0\" y2=\"1\"><stop offset=\"0\" stop-color=\"#a9b0ba\"/><stop offset=\"1\" stop-color=\"#4b5058\"/></linearGradient></defs><path fill=\"url(#g_trimmed_greaves)\" stroke=\"#0a0d12\" stroke-width=\"1.2\" stroke-linejoin=\"round\" d=\"M10 4h11v20l9 4v7H10z\"/><path d=\"M10 31h20\" stroke=\"#4b5058\" stroke-width=\"2\"/><path d=\"M10 9h11\" stroke=\"#e8b84a\" stroke-width=\"2.2\"/>" },
            { "stone_greaves", "<defs><linearGradient id=\"g_stone_greaves\" x1=\"0\" y1=\"0\" x2=\"0\" y2=\"1\"><stop offset=\"0\" stop-color=\"#8d8c86\"/><stop offset=\"1\" stop-color=\"#4c4b47\"/></linearGradient></defs><path fill=\"url(#g_stone_greaves)\" stroke=\"#0a0d12\" stroke-width=\"1.2\" stroke-linejoin=\"round\" d=\"M10 4h11v20l9 4v7H10z\"/><path d=\"M10 31h20\" stroke=\"#4c4b47\" stroke-width=\"2\"/><path d=\"M12 10l4 3-2 4 3 3\" stroke=\"#4c4b47\" stroke-width=\"1.2\" fill=\"none\"/><path d=\"M22 27l3 3\" stroke=\"#4c4b47\" stroke-width=\"1.2\"/>" },
            { "rusted_greathelm", "<defs><linearGradient id=\"g_rusted_greathelm\" x1=\"0\" y1=\"0\" x2=\"0\" y2=\"1\"><stop offset=\"0\" stop-color=\"#8a5634\"/><stop offset=\"1\" stop-color=\"#4e2f1e\"/></linearGradient></defs><path fill=\"url(#g_rusted_greathelm)\" stroke=\"#0a0d12\" stroke-width=\"1.2\" stroke-linejoin=\"round\" d=\"M8 24c0-10 5-17 12-17s12 7 12 17v6H8z\"/><path d=\"M11 19h18v5H11z\" fill=\"#07080a\"/><path d=\"M12 21.5h16\" stroke=\"#7fd8ff\" stroke-width=\"1.2\"/><circle cx=\"12\" cy=\"15\" r=\"1.2\" fill=\"#7fd8ff\"/><circle cx=\"28\" cy=\"15\" r=\"1.2\" fill=\"#7fd8ff\"/><circle cx=\"16\" cy=\"11\" r=\"2\" fill=\"#5e2e14\" opacity=\".8\"/>" },
            { "soldier_greathelm", "<defs><linearGradient id=\"g_soldier_greathelm\" x1=\"0\" y1=\"0\" x2=\"0\" y2=\"1\"><stop offset=\"0\" stop-color=\"#4a5058\"/><stop offset=\"1\" stop-color=\"#23262c\"/></linearGradient></defs><path fill=\"url(#g_soldier_greathelm)\" stroke=\"#0a0d12\" stroke-width=\"1.2\" stroke-linejoin=\"round\" d=\"M8 24c0-10 5-17 12-17s12 7 12 17v6H8z\"/><path d=\"M11 19h18v5H11z\" fill=\"#07080a\"/><path d=\"M12 21.5h16\" stroke=\"#7fd8ff\" stroke-width=\"1.2\"/><path d=\"M20 7v11\" stroke=\"#23262c\" stroke-width=\"3\"/><path d=\"M15 30h10v3H15z\" fill=\"#23262c\"/>" },
            { "wrapped_greathelm", "<defs><linearGradient id=\"g_wrapped_greathelm\" x1=\"0\" y1=\"0\" x2=\"0\" y2=\"1\"><stop offset=\"0\" stop-color=\"#33373d\"/><stop offset=\"1\" stop-color=\"#8c7f68\"/></linearGradient></defs><path fill=\"url(#g_wrapped_greathelm)\" stroke=\"#0a0d12\" stroke-width=\"1.2\" stroke-linejoin=\"round\" d=\"M8 24c0-10 5-17 12-17s12 7 12 17v6H8z\"/><path d=\"M11 19h18v5H11z\" fill=\"#07080a\"/><path d=\"M12 21.5h16\" stroke=\"#7fd8ff\" stroke-width=\"1.2\"/><path d=\"M9 14h22M8.5 17h23M10 11h20\" stroke=\"#8c7f68\" stroke-width=\"2\"/>" },
            { "spired_greathelm", "<defs><linearGradient id=\"g_spired_greathelm\" x1=\"0\" y1=\"0\" x2=\"0\" y2=\"1\"><stop offset=\"0\" stop-color=\"#aab1bb\"/><stop offset=\"1\" stop-color=\"#5d636b\"/></linearGradient></defs><path fill=\"url(#g_spired_greathelm)\" stroke=\"#0a0d12\" stroke-width=\"1.2\" stroke-linejoin=\"round\" d=\"M8 24c0-10 5-17 12-17s12 7 12 17v6H8z\"/><path d=\"M11 19h18v5H11z\" fill=\"#07080a\"/><path d=\"M12 21.5h16\" stroke=\"#e8b84a\" stroke-width=\"1.2\"/><path fill=\"url(#g_spired_greathelm)\" stroke=\"#0a0d12\" stroke-width=\"1.2\" stroke-linejoin=\"round\" d=\"M18.5 8L20 0l1.5 8z\"/><path d=\"M17 8h6\" stroke=\"#e8b84a\" stroke-width=\"2\"/>" },
            { "elite_greathelm", "<defs><linearGradient id=\"g_elite_greathelm\" x1=\"0\" y1=\"0\" x2=\"0\" y2=\"1\"><stop offset=\"0\" stop-color=\"#1c1f24\"/><stop offset=\"1\" stop-color=\"#e8b84a\"/></linearGradient></defs><path fill=\"url(#g_elite_greathelm)\" stroke=\"#0a0d12\" stroke-width=\"1.2\" stroke-linejoin=\"round\" d=\"M8 24c0-10 5-17 12-17s12 7 12 17v6H8z\"/><path d=\"M11 19h18v5H11z\" fill=\"#07080a\"/><path d=\"M12 21.5h16\" stroke=\"#ffd27a\" stroke-width=\"1.2\"/><path d=\"M9 16h22\" stroke=\"#e8b84a\" stroke-width=\"1.6\"/><path d=\"M30 12l6-6M10 12L4 6\" stroke=\"#e8b84a\" stroke-width=\"2.2\"/>" },
            { "stocky_mitts", "<defs><linearGradient id=\"g_stocky_mitts\" x1=\"0\" y1=\"0\" x2=\"0\" y2=\"1\"><stop offset=\"0\" stop-color=\"#3a3e45\"/><stop offset=\"1\" stop-color=\"#6d737b\"/></linearGradient></defs><path fill=\"url(#g_stocky_mitts)\" stroke=\"#0a0d12\" stroke-width=\"1.2\" stroke-linejoin=\"round\" d=\"M12 36V22l-3-6 3-2 3 4V8h3v10V6h3v12V8h3v11l2-4 3 2-3 9v10z\"/><rect x=\"12\" y=\"16\" width=\"14\" height=\"5\" fill=\"#6d737b\"/>" },
            { "riveted_mitts", "<defs><linearGradient id=\"g_riveted_mitts\" x1=\"0\" y1=\"0\" x2=\"0\" y2=\"1\"><stop offset=\"0\" stop-color=\"#6d737b\"/><stop offset=\"1\" stop-color=\"#33373d\"/></linearGradient></defs><path fill=\"url(#g_riveted_mitts)\" stroke=\"#0a0d12\" stroke-width=\"1.2\" stroke-linejoin=\"round\" d=\"M12 36V22l-3-6 3-2 3 4V8h3v10V6h3v12V8h3v11l2-4 3 2-3 9v10z\"/><circle cx=\"15\" cy=\"28\" r=\"1.3\" fill=\"#d7dce2\"/><circle cx=\"19\" cy=\"28\" r=\"1.3\" fill=\"#d7dce2\"/><circle cx=\"23\" cy=\"28\" r=\"1.3\" fill=\"#d7dce2\"/>" },
            { "tempered_mitts", "<defs><linearGradient id=\"g_tempered_mitts\" x1=\"0\" y1=\"0\" x2=\"0\" y2=\"1\"><stop offset=\"0\" stop-color=\"#6f8aa6\"/><stop offset=\"1\" stop-color=\"#33475c\"/></linearGradient></defs><path fill=\"url(#g_tempered_mitts)\" stroke=\"#0a0d12\" stroke-width=\"1.2\" stroke-linejoin=\"round\" d=\"M12 36V22l-3-6 3-2 3 4V8h3v10V6h3v12V8h3v11l2-4 3 2-3 9v10z\"/><path d=\"M12 31h14\" stroke=\"#c0c6ce\" stroke-width=\"2.4\"/>" },
            { "bolstered_mitts", "<defs><linearGradient id=\"g_bolstered_mitts\" x1=\"0\" y1=\"0\" x2=\"0\" y2=\"1\"><stop offset=\"0\" stop-color=\"#5a5f48\"/><stop offset=\"1\" stop-color=\"#3a2516\"/></linearGradient></defs><path fill=\"url(#g_bolstered_mitts)\" stroke=\"#0a0d12\" stroke-width=\"1.2\" stroke-linejoin=\"round\" d=\"M12 36V22l-3-6 3-2 3 4V8h3v10V6h3v12V8h3v11l2-4 3 2-3 9v10z\"/><path d=\"M12 26h14M12 31h14\" stroke=\"#3a2516\" stroke-width=\"2.6\"/>" },
            { "moulded_mitts", "<defs><linearGradient id=\"g_moulded_mitts\" x1=\"0\" y1=\"0\" x2=\"0\" y2=\"1\"><stop offset=\"0\" stop-color=\"#16181c\"/><stop offset=\"1\" stop-color=\"#33373d\"/></linearGradient></defs><path fill=\"url(#g_moulded_mitts)\" stroke=\"#0a0d12\" stroke-width=\"1.2\" stroke-linejoin=\"round\" d=\"M12 36V22l-3-6 3-2 3 4V8h3v10V6h3v12V8h3v11l2-4 3 2-3 9v10z\"/><path d=\"M19 22v12\" stroke=\"#7fd8ff\" stroke-width=\"1.6\"/>" },
            { "blaster_comum", "<defs><linearGradient id=\"g_blaster_comum\" x1=\"0\" y1=\"0\" x2=\"0\" y2=\"1\"><stop offset=\"0\" stop-color=\"#4a5058\"/><stop offset=\"1\" stop-color=\"#23262c\"/></linearGradient></defs><path fill=\"url(#g_blaster_comum)\" stroke=\"#0a0d12\" stroke-width=\"1.2\" stroke-linejoin=\"round\" d=\"M6 15h22l3 3H18l-2 2v9h-6v-8l-4-2z\"/><path d=\"M10 17h14\" stroke=\"#7fd8ff\" stroke-width=\"1.6\"/>" },
            { "blaster_incomum", "<defs><linearGradient id=\"g_blaster_incomum\" x1=\"0\" y1=\"0\" x2=\"0\" y2=\"1\"><stop offset=\"0\" stop-color=\"#4a5058\"/><stop offset=\"1\" stop-color=\"#23262c\"/></linearGradient></defs><path fill=\"url(#g_blaster_incomum)\" stroke=\"#0a0d12\" stroke-width=\"1.2\" stroke-linejoin=\"round\" d=\"M2 16h30l4 3H20l-2 2v8h-5v-7H6l-4-2z\"/><rect x=\"12\" y=\"11\" width=\"9\" height=\"4\" fill=\"#23262c\"/><path d=\"M24 15v5M28 15v5\" stroke=\"#7dff9a\" stroke-width=\"1.8\"/>" },
            { "super_blaster", "<defs><linearGradient id=\"g_super_blaster\" x1=\"0\" y1=\"0\" x2=\"0\" y2=\"1\"><stop offset=\"0\" stop-color=\"#4a5058\"/><stop offset=\"1\" stop-color=\"#23262c\"/></linearGradient></defs><path fill=\"url(#g_super_blaster)\" stroke=\"#0a0d12\" stroke-width=\"1.2\" stroke-linejoin=\"round\" d=\"M1 15h34l4 3v3H22l-2 3v7h-5v-7H9l-6 3-2-3z\"/><circle cx=\"17\" cy=\"22\" r=\"3.2\" fill=\"#23262c\"/><path d=\"M23 14v7M27 14v7M31 14v7\" stroke=\"#ffa040\" stroke-width=\"1.8\"/>" },
            { "escudao", "<defs><linearGradient id=\"g_escudao\" x1=\"0\" y1=\"0\" x2=\"0\" y2=\"1\"><stop offset=\"0\" stop-color=\"#5d636b\"/><stop offset=\"1\" stop-color=\"#1d3a5e\"/></linearGradient></defs><path fill=\"url(#g_escudao)\" stroke=\"#0a0d12\" stroke-width=\"1.2\" stroke-linejoin=\"round\" d=\"M20 3l14 8v16l-14 9-14-9V11z\"/><path d=\"M20 8l9 5v11l-9 6-9-6V13z\" fill=\"#123050\" stroke=\"#8fd0ff\" stroke-width=\"1.2\"/><circle cx=\"20\" cy=\"19\" r=\"3\" fill=\"#5d636b\"/>" },
            { "forca_extra", "<defs><linearGradient id=\"g_forca_extra\" x1=\"0\" y1=\"0\" x2=\"0\" y2=\"1\"><stop offset=\"0\" stop-color=\"#d9a441\"/><stop offset=\"1\" stop-color=\"#6b4a14\"/></linearGradient></defs><circle cx=\"20\" cy=\"20\" r=\"12\" fill=\"url(#g_forca_extra)\" stroke=\"#0a0d12\" stroke-width=\"1.2\" stroke-linejoin=\"round\"/><path d=\"M20 10l6 10-6 10-6-10z\" fill=\"#ff4a3a\" stroke=\"#0a0d12\" stroke-width=\"1\"/><path d=\"M20 2v6M20 32v6M2 20h6M32 20h6\" stroke=\"#d9a441\" stroke-width=\"2\"/>" },
            { "inti_extra", "<defs><linearGradient id=\"g_inti_extra\" x1=\"0\" y1=\"0\" x2=\"0\" y2=\"1\"><stop offset=\"0\" stop-color=\"#d9a441\"/><stop offset=\"1\" stop-color=\"#6b4a14\"/></linearGradient></defs><circle cx=\"20\" cy=\"20\" r=\"12\" fill=\"url(#g_inti_extra)\" stroke=\"#0a0d12\" stroke-width=\"1.2\" stroke-linejoin=\"round\"/><path d=\"M20 10l6 10-6 10-6-10z\" fill=\"#4aa8ff\" stroke=\"#0a0d12\" stroke-width=\"1\"/><path d=\"M20 2v6M20 32v6M2 20h6M32 20h6\" stroke=\"#d9a441\" stroke-width=\"2\"/>" },
            { "dex_extra", "<defs><linearGradient id=\"g_dex_extra\" x1=\"0\" y1=\"0\" x2=\"0\" y2=\"1\"><stop offset=\"0\" stop-color=\"#d9a441\"/><stop offset=\"1\" stop-color=\"#6b4a14\"/></linearGradient></defs><path fill=\"url(#g_dex_extra)\" stroke=\"#0a0d12\" stroke-width=\"1.2\" stroke-linejoin=\"round\" d=\"M12 14h16l-8 18z\"/><path d=\"M20 6l6 8h-12z\" fill=\"#5dff8a\"/><circle cx=\"20\" cy=\"16\" r=\"3\" fill=\"#5dff8a\" opacity=\".9\"/>" },
            { "vit_extra", "<defs><linearGradient id=\"g_vit_extra\" x1=\"0\" y1=\"0\" x2=\"0\" y2=\"1\"><stop offset=\"0\" stop-color=\"#d9a441\"/><stop offset=\"1\" stop-color=\"#6b4a14\"/></linearGradient></defs><path fill=\"url(#g_vit_extra)\" stroke=\"#0a0d12\" stroke-width=\"1.2\" stroke-linejoin=\"round\" d=\"M12 14h16l-8 18z\"/><path d=\"M20 6l6 8h-12z\" fill=\"#ffb03a\"/><circle cx=\"20\" cy=\"16\" r=\"3\" fill=\"#ffb03a\" opacity=\".9\"/>" },
        };

        /* ---- ícones das peças que a faixa I não tinha (módulos da mão esquerda e acessórios de cada atributo) ---- */
        static readonly Dictionary<string, string> StatColor = new Dictionary<string, string> { { "str", "#ff4a3a" }, { "dex", "#5dff8a" }, { "int", "#4aa8ff" }, { "vit", "#ffb03a" } };

        static string Grad(string id, string a, string b) { return "<defs><linearGradient id=\"g_" + id + "\" x1=\"0\" y1=\"0\" x2=\"0\" y2=\"1\"><stop offset=\"0\" stop-color=\"" + a + "\"/><stop offset=\"1\" stop-color=\"" + b + "\"/></linearGradient></defs>"; }
        const string Edge = "stroke=\"#0a0d12\" stroke-width=\"1.2\" stroke-linejoin=\"round\"";

        static GearIcons()
        {
            Markup["focus_module"] = Grad("focus_module", "#4a5a78", "#1d2538") +
                "<circle cx=\"20\" cy=\"20\" r=\"13\" fill=\"url(#g_focus_module)\" " + Edge + "/><circle cx=\"20\" cy=\"20\" r=\"6\" fill=\"#7fd8ff\"/>" +
                "<path d=\"M20 4v6M20 30v6M4 20h6M30 20h6\" stroke=\"#7fd8ff\" stroke-width=\"1.6\"/>";
            Markup["med_module"] = Grad("med_module", "#5d636b", "#2a3a2e") +
                "<path fill=\"url(#g_med_module)\" " + Edge + " d=\"M9 8h22v24H9z\"/>" +
                "<path d=\"M17 12h6v5h5v6h-5v5h-6v-5h-5v-6h5z\" fill=\"#7dff9a\" stroke=\"#0a0d12\" stroke-width=\"1\"/>";
            // ---- armas melee e escudos novos (out/2026) ----
            Markup["shock_baton"] = Grad("shock_baton", "#8a929c", "#3a4048") +
                "<path fill=\"#2a2d33\" " + Edge + " d=\"M6 32l3 3 6-6-3-3z\"/>" +
                "<path fill=\"#c0c6ce\" " + Edge + " d=\"M10 25l5 5 2-2-5-5z\"/>" +
                "<path fill=\"url(#g_shock_baton)\" " + Edge + " d=\"M14 24l3 3 13-13-3-3z\"/>" +
                "<circle cx=\"30\" cy=\"10\" r=\"4\" fill=\"#7fd8ff\" stroke=\"#0a0d12\" stroke-width=\"1\"/>" +
                "<path d=\"M28 3l2 3-3 1 2 2M37 10l-3 1 1 2-3 1\" stroke=\"#bff0ff\" stroke-width=\"1.2\" fill=\"none\"/>";
            Markup["vibro_blade"] = Grad("vibro_blade", "#d6dde4", "#6d7680") +
                "<path fill=\"#3a2a1e\" " + Edge + " d=\"M5 32l3 3 5-5-3-3z\"/>" +
                "<path fill=\"#d9a441\" " + Edge + " d=\"M8 25l7 7 2-2-7-7z\"/>" +
                "<path fill=\"url(#g_vibro_blade)\" " + Edge + " d=\"M13 24l3 3 18-19 1-5-5 1z\"/>" +
                "<path d=\"M16.5 26L34 7.5\" stroke=\"#7dffb0\" stroke-width=\"1\"/>";
            Markup["breach_hammer"] = Grad("breach_hammer", "#7a8088", "#33373d") +
                "<path fill=\"#4a4f57\" " + Edge + " d=\"M3 34l3 3 19-19-3-3z\"/>" +
                "<path fill=\"#2a2d33\" " + Edge + " d=\"M4 31l5 5 3-3-5-5z\"/>" +
                "<path fill=\"url(#g_breach_hammer)\" " + Edge + " d=\"M24 4.5L35.5 16 30 21.5 18.5 10z\"/>" +
                "<path d=\"M21.5 7.5l11 11\" stroke=\"#ffa040\" stroke-width=\"2\"/>";
            Markup["plasma_glaive"] = Grad("plasma_glaive", "#ffd08a", "#ff6a1a") +
                "<path fill=\"#3a4048\" " + Edge + " d=\"M3 35l2 2 22-22-2-2z\"/>" +
                "<path fill=\"#d9a441\" " + Edge + " d=\"M23 17l3 3 3-3-3-3z\"/>" +
                "<path fill=\"url(#g_plasma_glaive)\" " + Edge + " d=\"M27 14c2-6 6-10 11-12-1 6-4 11-9 14z\"/>" +
                "<path d=\"M29 13c2-4 4-7 7-9\" stroke=\"#fff2c0\" stroke-width=\"1\" fill=\"none\"/>";
            Markup["buckler"] = Grad("buckler", "#9aa4ae", "#3e4650") +
                "<circle cx=\"20\" cy=\"20\" r=\"14\" fill=\"url(#g_buckler)\" " + Edge + "/>" +
                "<circle cx=\"20\" cy=\"20\" r=\"10\" fill=\"none\" stroke=\"#5dff8a\" stroke-width=\"1.2\" opacity=\".8\"/>" +
                "<circle cx=\"20\" cy=\"20\" r=\"4\" fill=\"#c0c6ce\" stroke=\"#0a0d12\" stroke-width=\"1\"/>" +
                "<circle cx=\"20\" cy=\"8\" r=\"1.2\" fill=\"#c0c6ce\"/><circle cx=\"20\" cy=\"32\" r=\"1.2\" fill=\"#c0c6ce\"/><circle cx=\"8\" cy=\"20\" r=\"1.2\" fill=\"#c0c6ce\"/><circle cx=\"32\" cy=\"20\" r=\"1.2\" fill=\"#c0c6ce\"/>";
            Markup["energy_shield"] = Grad("energy_shield", "#9de8ff", "#2a6aa8") +
                "<path fill=\"none\" stroke=\"#5d636b\" stroke-width=\"3\" stroke-linejoin=\"round\" d=\"M20 3l14 8v16l-14 9-14-9V11z\"/>" +
                "<path fill=\"url(#g_energy_shield)\" opacity=\".6\" d=\"M20 6l11 6.5v13L20 33 9 25.5v-13z\"/>" +
                "<path d=\"M14 15l6 4 6-4M14 21l6 4 6-4\" stroke=\"#e6fbff\" stroke-width=\"1\" fill=\"none\" opacity=\".8\"/>" +
                "<circle cx=\"20\" cy=\"34\" r=\"2.2\" fill=\"#7fd8ff\" stroke=\"#0a0d12\" stroke-width=\"1\"/>";
            foreach (var kv in StatColor)
            {
                string core = "core_" + kv.Key, chip = "chip_" + kv.Key;
                Markup[core] = Grad(core, "#d9a441", "#6b4a14") +
                    "<circle cx=\"20\" cy=\"20\" r=\"12\" fill=\"url(#g_" + core + ")\" " + Edge + "/>" +
                    "<path d=\"M20 10l6 10-6 10-6-10z\" fill=\"" + kv.Value + "\" stroke=\"#0a0d12\" stroke-width=\"1\"/>" +
                    "<path d=\"M20 2v6M20 32v6M2 20h6M32 20h6\" stroke=\"#d9a441\" stroke-width=\"2\"/>";
                Markup[chip] = Grad(chip, "#d9a441", "#6b4a14") +
                    "<path fill=\"url(#g_" + chip + ")\" " + Edge + " d=\"M12 14h16l-8 18z\"/>" +
                    "<path d=\"M20 6l6 8h-12z\" fill=\"" + kv.Value + "\"/><circle cx=\"20\" cy=\"16\" r=\"3\" fill=\"" + kv.Value + "\" opacity=\".9\"/>";
            }
        }

        /// <summary>
        /// Qual desenho usar para um equipamento: o próprio (faixa I), o da peça da faixa I que ele reaproveita ("model"),
        /// ou o do tipo de peça (módulos da mão esquerda e acessórios, pela cor do atributo). Repetir desenho entre faixas é intencional.
        /// </summary>
        public static string Resolve(EquipDef e)
        {
            if (e == null) return null;
            if (Markup.ContainsKey(e.Id)) return e.Id;
            if (!string.IsNullOrEmpty(e.Model) && Markup.ContainsKey(e.Model)) return e.Model;
            if (e.ShieldKind == "buckler") return "buckler";
            if (e.ShieldKind == "energy") return "energy_shield";
            if (e.Type == "melee1H") return e.Focus == "scout" ? "vibro_blade" : "shock_baton";
            if (e.Type == "melee2H") return e.Focus == "scout" ? "plasma_glaive" : "breach_hammer";
            if (e.Type == "handL") return e.Focus == "arcane" ? "focus_module" : e.Focus == "sacred" ? "med_module" : "escudao";
            if (e.Type == "accMajor" && Markup.ContainsKey("core_" + e.Focus)) return "core_" + e.Focus;
            if (e.Type == "accMinor" && Markup.ContainsKey("chip_" + e.Focus)) return "chip_" + e.Focus;
            return null;
        }
    }
}
