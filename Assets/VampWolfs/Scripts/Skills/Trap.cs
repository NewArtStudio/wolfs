using System.Collections.Generic;
using UnityEngine;

namespace VampWolfs
{
    public class Trap : MonoBehaviour
    {
        CombatUnit owner;
        TrapAbility ability;
        float armedAt, until;
        GameObject jaws;

        public static Trap Spawn(CombatUnit owner, Vector3 position, TrapAbility ability)
        {
            GameObject go = new GameObject(ability.Name);
            go.transform.SetParent(owner != null ? owner.transform.parent : null, false);
            go.transform.position = new Vector3(position.x, 0f, position.z);
            Trap t = go.AddComponent<Trap>();
            t.owner = owner; t.ability = ability;
            t.armedAt = Time.time + ability.ArmTime;
            t.until = Time.time + ability.Lifetime;
            // small iron jaws on the ground (simple shapes: visible but easy to miss in the grass)
            Color iron = new Color(0.28f, 0.27f, 0.26f);
            t.jaws = Shapes.Create(PrimitiveType.Cylinder, go.transform, "Jaws", new Vector3(0f, 0.04f, 0f), new Vector3(0.9f, 0.04f, 0.9f), iron);
            Shapes.Create(PrimitiveType.Cube, go.transform, "Plate", new Vector3(0f, 0.07f, 0f), new Vector3(0.35f, 0.04f, 0.35f), ability.Color, true);
            return t;
        }

        void Update()
        {
            if (Time.time >= until || owner == null) { Destroy(gameObject); return; }
            if (Time.time < armedAt || owner.IsDead) return;

            List<Health> inside = CombatQuery.TargetsInRadius(owner, transform.position, ability.Radius);
            bool enemyInside = false;
            for (int i = 0; i < inside.Count; i++) if (inside[i].Team != Team.Neutral) { enemyInside = true; break; }
            if (!enemyInside) return;

            List<Health> targets = new List<Health>(inside);
            for (int i = 0; i < targets.Count; i++) ability.Hit.Apply(owner, targets[i]);
            if (!Vfx.SkillFx.TrapSnap(transform.position, ability)) Effects.Burst(transform.position, ability.Radius, ability.Color, 0.25f);
            Destroy(gameObject);
        }
    }
}
