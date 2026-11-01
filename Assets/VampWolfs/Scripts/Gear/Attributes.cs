using System;

namespace VampWolfs
{
    /// <summary>
    /// The four Ragnarok-style attributes (same system as the Space project):
    /// STR = physical melee damage, DEX = ranged damage + faster casts/attacks,
    /// INT = magic damage + magic defense, VIT = health + defense + status resistance.
    /// </summary>
    [Serializable]
    public struct Attributes
    {
        public float Str;
        public float Dex;
        public float Int;
        public float Vit;

        /// <summary>Keys in display order (same names as the JSON fields).</summary>
        public static readonly string[] Keys = { "Str", "Dex", "Int", "Vit" };

        public float Sum
        {
            get { return Str + Dex + Int + Vit; }
        }

        public void Add(Attributes other)
        {
            Str += other.Str;
            Dex += other.Dex;
            Int += other.Int;
            Vit += other.Vit;
        }

        public float Get(string key)
        {
            switch (key)
            {
                case "Str": return Str;
                case "Dex": return Dex;
                case "Int": return Int;
                case "Vit": return Vit;
                default: return 0f;
            }
        }

        public void Set(string key, float value)
        {
            switch (key)
            {
                case "Str": Str = value; break;
                case "Dex": Dex = value; break;
                case "Int": Int = value; break;
                case "Vit": Vit = value; break;
            }
        }

        public static string DisplayName(string key)
        {
            switch (key)
            {
                case "Str": return "Força";
                case "Dex": return "Destreza";
                case "Int": return "Inteligência";
                case "Vit": return "Vitalidade";
                default: return key;
            }
        }

        public static string ShortName(string key)
        {
            switch (key)
            {
                case "Str": return "FOR";
                case "Dex": return "DES";
                case "Int": return "INT";
                case "Vit": return "VIT";
                default: return key;
            }
        }
    }
}
