using System.Collections.Generic;
using System.Globalization;

namespace VampWolfs
{
    /// <summary>
    /// Skill descriptions built FROM THE NUMBERS (change the number in skills.json, the text follows).
    /// Example: "Dano 14 · Lento 40% por 1,5 s · Recarga 6 s".
    /// </summary>
    public static class SkillText
    {
        static readonly CultureInfo Pt = MakePt();

        /// <summary>pt-BR may be missing on some builds (invariant globalization): fall back to a comma-decimal copy.</summary>
        static CultureInfo MakePt()
        {
            try { return new CultureInfo("pt-BR"); }
            catch (System.Exception)
            {
                CultureInfo c = (CultureInfo)CultureInfo.InvariantCulture.Clone();
                c.NumberFormat.NumberDecimalSeparator = ",";
                c.NumberFormat.NumberGroupSeparator = ".";
                return c;
            }
        }

        static string N(float v) { return v.ToString(v % 1f == 0f ? "0" : "0.#", Pt); }
        static string Pct(float v) { return N(v * 100f) + "%"; }

        public static string KindName(SkillDef s)
        {
            switch (s.Kind)
            {
                case "projectile": return s.Count > 1 ? "Projéteis" : "Projétil";
                case "melee": return "Corpo a corpo";
                case "area": return s.AtAim ? "Área no alvo" : "Área em volta";
                case "dash": return "Deslocamento";
                case "counter": return "Contra-ataque";
                case "buff": return "Reforço";
                case "zone": return "Zona";
                case "trap": return "Armadilha";
                case "passive": return "Passiva";
            }
            return s.Kind;
        }

        public static string TypeName(string dmgType)
        {
            return dmgType == "melee" ? "físico perto (FOR)" : dmgType == "ranged" ? "físico longe (DES)" : "mágico (INT)";
        }

        /// <summary>Numbers of an active skill at a level (class skills grow with level; gear skills ignore it).</summary>
        public static string Numbers(SkillDef s, int level)
        {
            if (s.IsPassive) return Passive(s, level);
            float g = s.IsClassSkill ? SkillCatalog.Grow(s, level) : 1f;
            List<string> p = new List<string>();
            if (s.Damage > 0f)
            {
                string dmg = "Dano " + N(s.Damage * g) + (s.Count > 1 ? " × " + s.Count : "");
                if (s.Kind == "zone") dmg = "Dano " + N(s.Damage * g) + " por segundo dentro";
                p.Add(dmg);
            }
            if (s.DotDps > 0f) p.Add("Sangra " + N(s.DotDps * g) + "/s por " + N(s.DotDuration) + " s");
            if (s.Lifesteal > 0f) p.Add("Rouba " + Pct(s.Lifesteal) + " em vida");
            if (s.StunDuration > 0f) p.Add("Atordoa " + N(s.StunDuration) + " s");
            if (s.RootDuration > 0f) p.Add("Prende " + N(s.RootDuration) + " s");
            if (s.SlowDuration > 0f) p.Add("Lento " + Pct(s.SlowAmount) + (s.Kind == "zone" ? " enquanto dentro" : " por " + N(s.SlowDuration) + " s"));
            if (s.VulnDuration > 0f) p.Add("Marca: +" + Pct(s.VulnAmount) + " de dano recebido por " + N(s.VulnDuration) + " s");
            if (s.Knockback > 0f) p.Add("Empurra " + N(s.Knockback) + " m");
            if (s.Knockback < 0f) p.Add("Puxa " + N(-s.Knockback) + " m");
            if (s.Pierce) p.Add("Atravessa");
            if (s.SelfHeal > 0f) p.Add("Cura " + N(s.SelfHeal * g));
            if (s.ZoneHealPs > 0f) p.Add("Cura " + N(s.ZoneHealPs * g) + "/s dentro");
            if (s.CounterHeal > 0f) p.Add("Cura " + N(s.CounterHeal * g) + " ao contra-atacar");
            if (s.ShieldPct > 0f) p.Add("Escudo " + Pct(s.ShieldPct * g) + " da vida por " + N(s.ShieldDuration) + " s");
            if (s.DRDuration > 0f) p.Add("−" + Pct(s.DR) + " de dano recebido por " + N(s.DRDuration) + " s");
            if (s.DmgBuffDuration > 0f) p.Add("+" + Pct(s.DmgBuff) + " de dano por " + N(s.DmgBuffDuration) + " s");
            if (s.SelfHasteDuration > 0f) p.Add("+" + Pct(s.SelfHaste) + " de velocidade por " + N(s.SelfHasteDuration) + " s");
            if (s.Invuln > 0f) p.Add("Intocável " + N(s.Invuln) + " s");
            if (s.Cleanse) p.Add("Remove lentidão, prisão e atordoamento");
            if (s.Kind == "counter") p.Add("Postura " + N(s.CounterDuration) + " s, atordoa " + N(s.CounterStun) + " s");
            if (s.Kind == "dash") p.Add(N(s.MaxDistance) + " m");
            if (s.Kind == "zone") p.Add("Dura " + N(s.ZoneDuration) + " s");
            if (s.Kind == "trap") p.Add("Arma em " + N(s.TrapArm) + " s, dura " + N(s.TrapLife) + " s");
            if (s.Radius > 0f && (s.Kind == "area" || s.Kind == "zone" || s.Kind == "trap")) p.Add("Raio " + N(s.Radius) + " m");
            if (s.Range > 0f && (s.Kind == "projectile" || s.AtAim)) p.Add("Alcance " + N(s.Range) + " m");
            if (s.EnergyCost > 0f) p.Add("Custa " + N(s.EnergyCost) + " de energia");
            float cd = SkillCatalog.CooldownAt(s, level);
            if (cd > 0f && s.EnergyCost <= 0f) p.Add("Recarga " + N(cd) + " s");
            if (s.CastTime > 0f) p.Add("Conjura " + N(s.CastTime) + " s");
            return string.Join(" · ", p.ToArray());
        }

        static string Passive(SkillDef s, int level)
        {
            int k = level < 1 ? 1 : level;
            List<string> p = new List<string>();
            if (s.P_Damage > 0f) p.Add("+" + Pct(s.P_Damage * k) + " de dano");
            if (s.P_Hp > 0f) p.Add("+" + Pct(s.P_Hp * k) + " de vida máxima");
            if (s.P_Move > 0f) p.Add("+" + Pct(s.P_Move * k) + " de velocidade");
            if (s.P_Lifesteal > 0f) p.Add(Pct(s.P_Lifesteal * k) + " de roubo de vida");
            if (s.P_Cdr > 0f) p.Add("−" + Pct(s.P_Cdr * k) + " de recarga");
            if (s.P_Energy > 0f) p.Add("+" + Pct(s.P_Energy * k) + " de energia");
            if (s.P_DR > 0f) p.Add("−" + Pct(s.P_DR * k) + " de dano recebido");
            if (s.P_Heal > 0f) p.Add("+" + Pct(s.P_Heal * k) + " de cura recebida");
            if (s.P_LowHpDamage > 0f) p.Add("+" + Pct(s.P_LowHpDamage * k) + " de dano abaixo de 40% de vida");
            if (s.P_KillHeal > 0f) p.Add("Cura " + Pct(s.P_KillHeal * k) + " da vida ao matar");
            if (s.P_BasicDotDps > 0f) p.Add("Ataque básico sangra " + N(s.P_BasicDotDps * k) + "/s por 2 s");
            if (s.P_SlowedBonus > 0f) p.Add("+" + Pct(s.P_SlowedBonus * k) + " de dano em alvos lentos/presos");
            if (s.P_Regen > 0f) p.Add("Regenera " + N(s.P_Regen * k * 100f) + "% da vida por segundo");
            return string.Join(" · ", p.ToArray());
        }
    }
}
