namespace VampWolfs
{
    /// <summary>
    /// Sum of the character's learned passive skills (class skills; numbers per skill level in skills.json).
    /// Read by DamageSystem (damage, lifesteal, energy, defense, kills), Health (healing received) and
    /// CombatUnit (max health, speed, cooldowns, regeneration). All values are fractions (0.15 = +15%).
    /// </summary>
    public class PassiveStats
    {
        public float Damage, Hp, Move, Lifesteal, Cdr, Energy, DR, Heal, LowHpDamage, KillHeal, BasicDotDps, SlowedBonus, Regen;

        /// <summary>Adds one passive at a level (values in the data are per level).</summary>
        public void Add(SkillDef s, int level)
        {
            if (s == null || level <= 0) return;
            float k = level;
            Damage += s.P_Damage * k;
            Hp += s.P_Hp * k;
            Move += s.P_Move * k;
            Lifesteal += s.P_Lifesteal * k;
            Cdr += s.P_Cdr * k;
            Energy += s.P_Energy * k;
            DR += s.P_DR * k;
            Heal += s.P_Heal * k;
            LowHpDamage += s.P_LowHpDamage * k;
            KillHeal += s.P_KillHeal * k;
            BasicDotDps += s.P_BasicDotDps * k;
            SlowedBonus += s.P_SlowedBonus * k;
            Regen += s.P_Regen * k;
        }

        /// <summary>Caps so stacked passives never break the game [PROTÓTIPO].</summary>
        public float CdrCapped { get { return System.Math.Min(Cdr, 0.4f); } }
        public float DRCapped { get { return System.Math.Min(DR, 0.4f); } }
    }
}
