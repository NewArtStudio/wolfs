using System.Collections.Generic;
using UnityEngine;

namespace VampWolfs
{
    /// <summary>
    /// Lingering area on the ground (Névoa Arcana, Água Benta, Poça de Sangue, Sabá…): every tick it hits
    /// enemies inside (damage per tick, slow) and can heal the caster while the caster stands inside.
    /// </summary>
    public class ZoneAbility : Ability
    {
        /// <summary>Applied to enemies inside every tick (Damage = per tick).</summary>
        public HitEffect Hit = new HitEffect();
        public float Radius = 3f;
        public float Duration = 4f;
        public bool AtAim = true;
        [Tooltip("Heals the caster per second while inside.")]
        public float HealPerSecond;
        public Color Color = new Color(0.6f, 0.4f, 0.9f);

        public const float Tick = 0.5f;

        Vector3 Center(CombatUnit caster, Vector3 aimPoint)
        {
            return AtAim ? ClampToRange(caster.Position, aimPoint, Range) : new Vector3(caster.Position.x, 0f, caster.Position.z);
        }

        public override float AiRange { get { return AtAim ? Range + Radius * 0.5f : Radius * 0.8f; } }

        public override TelegraphInfo GetTelegraph(CombatUnit caster, Vector3 aimPoint)
        {
            return TelegraphInfo.Circle(Center(caster, aimPoint), Radius);
        }

        public override void Execute(CombatUnit caster, Vector3 aimPoint)
        {
            Zone.Spawn(caster, Center(caster, aimPoint), this);
        }
    }

    /// <summary>The zone placed in the world (lives Duration seconds, then disappears).</summary>
    public class Zone : MonoBehaviour
    {
        CombatUnit owner;
        ZoneAbility ability;
        float until, nextTick;
        Vfx.VfxSystem.FxObj look;
        Vfx.VfxSystem vfx;
        GameObject fallback;

        public static Zone Spawn(CombatUnit owner, Vector3 center, ZoneAbility ability)
        {
            GameObject go = new GameObject(ability.Name);
            go.transform.SetParent(owner != null ? owner.transform.parent : null, false);
            go.transform.position = center;
            Zone z = go.AddComponent<Zone>();
            z.owner = owner; z.ability = ability;
            z.until = Time.time + ability.Duration;
            z.nextTick = Time.time;
            z.vfx = Vfx.VfxHub.Fx;
            if (z.vfx != null) z.look = z.vfx.Acquire("ground");
            else z.fallback = Shapes.Create(PrimitiveType.Cylinder, go.transform, "Zone", new Vector3(0f, 0.03f, 0f), new Vector3(ability.Radius * 2f, 0.01f, ability.Radius * 2f), new Color(ability.Color.r, ability.Color.g, ability.Color.b), true);
            Vfx.SkillFx.ZoneStart(center, ability);
            return z;
        }

        void Update()
        {
            if (Time.time >= until || owner == null) { Destroy(gameObject); return; }
            Vector3 c = transform.position;

            if (look != null && look.Go != null)
            {
                float remain = Mathf.Clamp01((until - Time.time) / 0.4f);
                float grow = Mathf.Clamp01((Time.time - (until - ability.Duration)) / 0.25f);
                float r = ability.Radius * Mathf.Lerp(0.6f, 1f, grow);
                look.T.position = new Vector3(c.x, 0.05f, c.z);
                look.T.rotation = Quaternion.identity;
                look.T.localScale = new Vector3(r * 2f, 1f, r * 2f);
                Color col = new Color(ability.Color.r * 1.6f, ability.Color.g * 1.6f, ability.Color.b * 1.6f, 1f);
                float style = ability.HealPerSecond > 0f ? 2f : (ability.Hit.Damage > 0f ? 0f : 1f);   // 0 fire, 1 ice/slow, 2 heal
                Vfx.VfxSystem.GroundSet(look, 4, col, new Color(ability.Color.r * 0.4f, ability.Color.g * 0.4f, ability.Color.b * 0.4f, 1f), new Vector4(0.6f, 0f, 0f, style), 0.85f * remain, 1.2f);
                Vfx.SkillFx.ZoneTick(c, ability);
            }

            if (Time.time < nextTick) return;
            nextTick = Time.time + ZoneAbility.Tick;

            if (!owner.IsDead)
            {
                List<Health> targets = new List<Health>(CombatQuery.TargetsInRadius(owner, c, ability.Radius));
                for (int i = 0; i < targets.Count; i++) ability.Hit.Apply(owner, targets[i]);

                if (ability.HealPerSecond > 0f)
                {
                    Vector3 d = owner.Position - c; d.y = 0f;
                    if (d.magnitude <= ability.Radius) owner.Health.HealScaled(ability.HealPerSecond * ZoneAbility.Tick);
                }
            }
        }

        void OnDestroy()
        {
            if (look != null && vfx != null) vfx.Return(look);
            look = null;
        }
    }
}
