using UnityEngine;

namespace VampWolfs.Lobby
{
    /// <summary>
    /// Cor e nome do ESTILO de um equipamento (coluna "focus" da planilha), usados no inventário do Arsenal
    /// (copiado do Space — o jogo não tem raridade): a célula ganha um brilho na cor do estilo.
    /// Acessórios usam o atributo (str/dex/int/vit), que cai na mesma família de cor do estilo equivalente.
    /// Cores escolhidas para não confundir com as cores das classes (azul, vermelho, verde).
    /// </summary>
    public static class ItemStyle
    {
        public static readonly Color Vanguard = UiKit.Hex(0xe0782f);   // laranja-ferrugem · placas pesadas · STR
        public static readonly Color Scout = UiKit.Hex(0x2ec4a6);      // verde-água · malha leve · DEX
        public static readonly Color Arcane = UiKit.Hex(0x9b6bff);     // violeta · condutores · INT
        public static readonly Color Sacred = UiKit.Hex(0xf2d27a);     // dourado-claro · médico/suporte · VIT

        /// <summary>Cor do estilo; null = item sem estilo (materiais, essências…).</summary>
        public static Color? ColorOf(EquipDef e)
        {
            if (e == null || string.IsNullOrEmpty(e.Focus)) return null;
            switch (e.Focus)
            {
                case "vanguard": case "str": return Vanguard;
                case "scout": case "dex": return Scout;
                case "arcane": case "int": return Arcane;
                case "sacred": case "vit": return Sacred;
            }
            return null;
        }

        /// <summary>Nome curto do estilo para a ficha do item.</summary>
        public static string NameOf(EquipDef e)
        {
            if (e == null || string.IsNullOrEmpty(e.Focus)) return null;
            switch (e.Focus)
            {
                case "vanguard": return "VANGUARDA"; case "scout": return "BATEDOR"; case "arcane": return "ARCANO"; case "sacred": return "SAGRADO";
                case "str": return "STR"; case "dex": return "DEX"; case "int": return "INT"; case "vit": return "VIT";
            }
            return e.Focus.ToUpperInvariant();
        }

        /// <summary>
        /// "Força" da peça para a seta de melhor [PROTÓTIPO]: a faixa manda (faixa maior = melhor); na mesma faixa,
        /// a soma de dano, Armour, Evasion, Ward e atributos atuais (com o refino no dano/defesas) desempata.
        /// </summary>
        public static double Power(GearCatalog catalog, EquipDef e, ItemInstance it)
        {
            if (e == null) return 0;
            float refine = e.Refinable && it != null ? CharacterStats.RefineMult(catalog.Rules, it.Refine) : 1f;
            return e.Tier * 1000.0 + (e.Dmg + e.Armour + e.Evasion + e.Ward) * refine + GearUpgrades.CurrentAttributes(e, it).Sum;
        }

        static Texture2D glow;
        /// <summary>Degradê vertical branco (opaco embaixo, transparente em cima), tingido com a cor do estilo na célula.</summary>
        public static Texture2D Glow
        {
            get
            {
                if (glow != null) return glow;
                glow = new Texture2D(1, 32, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear, name = "Brilho do estilo" };
                for (int y = 0; y < 32; y++) { float t = y / 31f; glow.SetPixel(0, y, new Color(1f, 1f, 1f, 0.45f * Mathf.Pow(1f - t, 1.6f))); }
                glow.Apply();
                return glow;
            }
        }
    }
}
