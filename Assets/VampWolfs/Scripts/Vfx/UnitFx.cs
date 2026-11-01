using UnityEngine;

namespace VampWolfs.Vfx
{
    /// <summary>
    /// Efeitos que acompanham um personagem (ou criatura) enquanto algo dura — só lê o estado, não muda nada:
    ///  - carga na mão durante a conjuração e rastro durante dashes;
    ///  - Espelho de Sangue (escudo de vitral vermelho), Couro Grosso (escudo de aço), névoa da invulnerabilidade,
    ///    estrelas do atordoamento e faíscas da pressa;
    ///  - poeira dos passos;
    ///  - sangue ao levar dano, cruzes ao curar e a morte (vampiro vira cinzas e morcegos; fera explode em sangue).
    /// Sem VFX (arquivos ausentes) não faz nada.
    /// </summary>
    public sealed class UnitFx : MonoBehaviour
    {
        public enum Kind { Vampire, Beast, Creature, Human }

        Kind kind;
        CombatUnit unit;
        Health health;
        StatusEffects status;
        VfxSystem.FxObj shield, ring;
        Renderer teamRing; Color ringColor; float ringRadius;
        VfxSystem owner;   // o VfxSystem que emprestou o escudo (para devolver ao certo)
        float stepTimer;
        DashAbility lastDash;

        static readonly Color MirrorShield = new Color(1.8f, 0.12f, 0.18f, 0.9f);
        static readonly Color HideShield = new Color(0.7f, 0.95f, 1.5f, 0.75f);

        /// <summary>Liga os efeitos num personagem/criatura (chamar depois de criar Health/StatusEffects).</summary>
        public static UnitFx Attach(GameObject go, Kind kind)
        {
            var fx = go.AddComponent<UnitFx>();
            fx.kind = kind;
            fx.unit = go.GetComponent<CombatUnit>();
            fx.health = go.GetComponent<Health>();
            fx.status = go.GetComponent<StatusEffects>();
            var tr = go.transform.Find("TeamRing");
            if (tr != null)
            {
                fx.teamRing = tr.GetComponent<Renderer>();
                fx.ringRadius = tr.localScale.x * 0.5f;
                var m = fx.teamRing != null ? fx.teamRing.sharedMaterial : null;
                fx.ringColor = m != null ? m.color : Color.white;
            }
            if (fx.health != null)
            {
                fx.health.Damaged += fx.OnDamaged;
                fx.health.Healed += fx.OnHealed;
                fx.health.Died += fx.OnDied;
            }
            return fx;
        }

        float Size { get { return kind == Kind.Creature ? 0.55f : CharacterFactory.SizeMultiplier; } }
        Vector3 Chest { get { return transform.position + Vector3.up * 1.3f * Size; } }

        /// <summary>Altura acima da cabeça (estrelas do atordoamento): a fera é bem mais alta que o vampiro.</summary>
        float HeadTop { get { return (kind == Kind.Beast ? 3.35f : 2.45f) * Size; } }

        Color BloodColor { get { return kind == Kind.Vampire ? new Color(1.1f, 0.02f, 0.08f) : new Color(1.6f, 0.08f, 0.06f); } }

        void OnDamaged(float amount, CombatUnit source)
        {
            if (!VfxHub.Ready || amount <= 0f) return;
            Vector3 dir = source != null ? transform.position - source.Position : -transform.forward;
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.01f) dir = -transform.forward;
            SkillFx.Damaged(Chest, dir.normalized, amount, BloodColor);
        }

        void OnHealed(float amount)
        {
            if (!VfxHub.Ready || kind == Kind.Creature) return;
            // o vampiro se cura bebendo sangue: cruzes vermelhas; os outros, verdes
            SkillFx.Healed(transform.position, amount, kind == Kind.Vampire ? new Color(2f, 0.2f, 0.3f) : new Color(0.5f, 2f, 0.6f));
        }

        void OnDied(CombatUnit killer)
        {
            ReleaseShield();
            if (!VfxHub.Ready || kind == Kind.Creature) return;   // ovelhas: o Sheep chama o efeito dele
            if (kind == Kind.Vampire) SkillFx.VampireDeath(transform.position);
            else SkillFx.BeastDeath(transform.position);   // fera e caçador: sangue
        }

        void LateUpdate()
        {
            var v = VfxHub.Fx;
            if (v == null || unit == null || unit.IsDead) { ReleaseShield(); return; }

            UpdateRing(v);

            // conjuração e dash
            if (unit.IsCasting && unit.CurrentCast != null) SkillFx.CastTick(unit, unit.CurrentCast, unit.CastProgress);
            if (unit.IsInForcedMove && lastDash != null) SkillFx.DashTick(unit, lastDash);

            // estados
            if (status != null)
            {
                if (status.IsStunned) SkillFx.StunTick(transform.position + Vector3.up * HeadTop);
                // névoa só na invulnerabilidade do dash (a do fim de partida dura até voltar à Base)
                if (status.IsInvulnerable && unit.IsInForcedMove && SkillFx.Every(36f))
                    v.Smoke.Emit(Chest + Random.insideUnitSphere * 0.5f * Size, Random.insideUnitSphere * 0.4f, Random.Range(0.6f, 1f) * Size, 0.6f, SkillFx.MistSmoke, -1f, 30f);
                if (status.IsHasted && unit.Velocity.sqrMagnitude > 1f && SkillFx.Every(30f))
                    v.Sparks.Emit(transform.position + Vector3.up * Random.Range(0.2f, 1.6f) * Size, -unit.Velocity * 0.3f, 0.07f, 0.3f, SkillFx.Moon);
                UpdateShield(v);
            }

            // poeira dos passos
            float speed = new Vector3(unit.Velocity.x, 0f, unit.Velocity.z).magnitude;
            if (speed > 1.5f && !unit.IsInForcedMove)
            {
                stepTimer -= Time.deltaTime * (speed / 6f);
                if (stepTimer <= 0f) { stepTimer = 0.32f; SkillFx.Footstep(transform.position, Mathf.Clamp01(speed / 8f)); }
            }
        }

        /// <summary>O dash atual (o DashAbility avisa quem está se deslocando com qual receita).</summary>
        public void SetDash(DashAbility a) { lastDash = a; }

        void UpdateShield(VfxSystem v)
        {
            bool mirror = status.IsCountering, hide = status.HasDamageReduction;
            if (!mirror && !hide) { DropShieldOnly(); return; }
            if (shield == null || shield.Go == null) { shield = v.Acquire("shield"); owner = v; }
            float pulse = 1f + 0.04f * Mathf.Sin(Time.time * 9f);
            float r = 1.15f * Size * pulse;
            shield.T.position = transform.position + Vector3.up * 1.15f * Size;
            shield.T.rotation = Quaternion.Euler(0f, Time.time * 40f, 0f);
            shield.T.localScale = new Vector3(r * 1.7f, r * 2.3f, r * 1.7f);
            VfxSystem.SetColor(shield, mirror ? MirrorShield : HideShield);
        }

        void DropShieldOnly()
        {
            if (shield != null && owner != null) owner.Return(shield);
            shield = null;
        }

        /// <summary>Anel do time brilhando no chão (no lugar do disco liso), como no Battlerite.</summary>
        void UpdateRing(VfxSystem v)
        {
            if (teamRing == null || ringRadius <= 0f) return;
            if (ring == null || ring.Go == null) { ring = v.Acquire("ground"); owner = v; teamRing.enabled = false; }
            float R = ringRadius * 1.05f;
            ring.T.position = new Vector3(transform.position.x, 0.04f, transform.position.z);
            ring.T.rotation = Quaternion.identity;
            ring.T.localScale = new Vector3(R * 2f, 1f, R * 2f);
            var c = new Color(ringColor.r * 1.6f, ringColor.g * 1.6f, ringColor.b * 1.6f, 1f);
            VfxSystem.GroundSet(ring, 0, c, ringColor, new Vector4(0.86f, 0.07f, 0.18f, 0f), 0.85f, 1.1f);
        }

        void ReleaseShield()
        {
            if (owner != null)
            {
                if (shield != null) owner.Return(shield);
                if (ring != null) owner.Return(ring);
            }
            if (ring != null && teamRing != null && unit != null && !unit.IsDead) teamRing.enabled = true;   // sem VFX volta o disco
            shield = null; ring = null; owner = null;
        }

        void OnDestroy()
        {
            ReleaseShield();
            if (health != null)
            {
                health.Damaged -= OnDamaged;
                health.Healed -= OnHealed;
                health.Died -= OnDied;
            }
        }
    }
}
