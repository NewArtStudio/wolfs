using UnityEngine;

namespace VampWolfs.Vfx
{
    /// <summary>
    /// Visual de um projétil em voo (no lugar da esfera lisa): núcleo brilhante virado para a câmera, estrela girando,
    /// risco de luz atrás (lanças) e o rastro de partículas da receita (SkillFx.ProjectileTrail).
    /// Os objetos são emprestados do VfxSystem e devolvidos quando o projétil some.
    /// </summary>
    public sealed class ProjectileFx : MonoBehaviour
    {
        ProjectileAbility ability;
        Vector3 direction, last;
        VfxSystem owner;
        VfxSystem.FxObj core, flare, streak;
        float scale, spin;

        public static ProjectileFx Attach(GameObject go, ProjectileAbility ability, Vector3 direction)
        {
            var v = VfxHub.Fx; if (v == null) return null;
            var fx = go.AddComponent<ProjectileFx>();
            fx.ability = ability; fx.direction = direction.normalized; fx.owner = v;
            fx.scale = Mathf.Max(0.3f, ability.VisualScale) * CharacterFactory.SizeMultiplier;
            fx.core = v.Acquire("glow");
            fx.flare = v.Acquire("flare");
            if (ability.Fx == "lance" || ability.Fx == "lance_ex" || ability.Fx == "feast") fx.streak = v.Acquire("streak");
            fx.last = go.transform.position;
            fx.spin = Random.Range(0f, 360f);
            fx.Place();
            return fx;
        }

        void LateUpdate()
        {
            if (owner == null) return;
            SkillFx.ProjectileTrail(ability, last, transform.position, direction, scale);
            last = transform.position;
            Place();
        }

        void Place()
        {
            if (owner == null) return;
            var c = SkillFx.CoreColor(ability);
            float pulse = 1f + 0.12f * Mathf.Sin(Time.time * 30f);
            spin += Time.deltaTime * 360f;
            owner.PlaceBillboard(core, transform.position, scale * 2.4f * pulse, c);
            owner.PlaceBillboard(flare, transform.position, scale * 3.2f, new Color(c.r * 0.7f, c.g * 0.7f, c.b * 0.7f, 1f), spin);
            if (streak != null)
            {
                float len = ability.Fx == "feast" ? 3.5f : 2.4f;
                owner.PlaceBeam(streak, transform.position - direction * len * scale, transform.position, scale * 0.9f);
                VfxSystem.SetColor(streak, c);
            }
        }

        void OnDestroy()
        {
            if (owner == null) return;
            owner.Return(core); owner.Return(flare);
            if (streak != null) owner.Return(streak);
        }
    }
}
