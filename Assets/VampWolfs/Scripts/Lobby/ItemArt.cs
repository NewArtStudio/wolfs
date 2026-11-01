using System.Collections.Generic;
using UnityEngine;

namespace VampWolfs.Lobby
{
    /// <summary>
    /// Ícones pintados dos equipamentos (out/2026): `Art/Resources/ItemIcons/<id do equipamento>.png`, recortados das folhas feitas
    /// com IA por `Tools/Icons/cut_sheet.py`. Quando existe a imagem, o SvgIcon mostra ela no lugar do desenho vetorial "gear_<id>";
    /// sem a imagem, continua o vetorial de antes (nada quebra).
    /// </summary>
    public static class ItemArt
    {
        static readonly Dictionary<string, Texture2D> cache = new Dictionary<string, Texture2D>();

        /// <summary>Imagem para um nome de ícone ("gear_<id>"), ou null se não for equipamento ou não houver imagem.</summary>
        public static Texture2D ForIcon(string icon)
        {
            if (string.IsNullOrEmpty(icon) || !icon.StartsWith("gear_")) return null;
            return For(icon.Substring(5));
        }

        /// <summary>Imagem do equipamento pelo id, ou null.</summary>
        public static Texture2D For(string equipId)
        {
            if (string.IsNullOrEmpty(equipId)) return null;
            Texture2D t;
            if (!cache.TryGetValue(equipId, out t)) { t = Resources.Load<Texture2D>("ItemIcons/" + equipId); cache[equipId] = t; }
            return t;
        }
    }
}
