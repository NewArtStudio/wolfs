using System.Collections.Generic;
using UnityEngine;

namespace VampWolfs
{
    /// <summary>
    /// A skillshot in flight. Travels in a straight line at chest height, hits the first target
    /// (or every target, if it pierces) and is stopped by obstacles (walls, fences, hay, trees).
    /// </summary>
    public class Projectile : MonoBehaviour
    {
        /// <summary>Projectiles currently flying (the AI reads this to dodge).</summary>
        public static readonly List<Projectile> Active = new List<Projectile>();

        public const float FlightHeight = 1.1f;

        public CombatUnit Owner { get; private set; }
        public Vector3 Direction { get; private set; }
        public float Speed { get { return ability.Speed; } }

        ProjectileAbility ability;
        float traveled;
        readonly List<Health> alreadyHit = new List<Health>();

        public static Projectile Spawn(CombatUnit owner, Vector3 origin, Vector3 direction, ProjectileAbility ability)
        {
            Transform parent = owner != null ? owner.transform.parent : null;
            Vector3 position = new Vector3(origin.x, FlightHeight, origin.z);

            GameObject go = Shapes.Create(PrimitiveType.Sphere, parent, ability.Name, position, Vector3.one * ability.VisualScale, ability.Color, true);

            Light glow = go.AddComponent<Light>();
            glow.type = LightType.Point;
            glow.color = ability.Color;
            glow.range = 3.5f;
            glow.intensity = 2f;

            Projectile projectile = go.AddComponent<Projectile>();
            projectile.Owner = owner;
            projectile.Direction = direction.normalized;
            projectile.ability = ability;
            return projectile;
        }

        void OnEnable() { Active.Add(this); }
        void OnDisable() { Active.Remove(this); }

        void Update()
        {
            float step = Mathf.Min(ability.Speed * Time.deltaTime, ability.Range - traveled);

            // Sweep the whole movement of this frame (so low FPS can't skip through thin fences),
            // nearest hits first. SphereCastAll also reports what overlaps the start (distance 0).
            RaycastHit[] hits = Physics.SphereCastAll(transform.position, ability.Radius, Direction, step);
            System.Array.Sort(hits, CompareDistance);

            for (int i = 0; i < hits.Length; i++)
            {
                Vector3 hitPoint = transform.position + Direction * hits[i].distance;

                if (hits[i].collider.GetComponent<Obstacle>() != null)
                {
                    Effects.Burst(hitPoint, 0.5f, ability.Color, 0.15f);
                    Destroy(gameObject);
                    return;
                }

                Health target = hits[i].collider.GetComponentInParent<Health>();
                if (target == null || target.IsDead || alreadyHit.Contains(target))
                {
                    continue;
                }

                if (Owner != null && (target == Owner.Health || target.Team == Owner.Team))
                {
                    continue;
                }

                alreadyHit.Add(target);
                ability.Hit.Apply(Owner, target);
                Effects.Burst(hitPoint, 0.7f, ability.Color, 0.2f);

                if (!ability.Pierce)
                {
                    Destroy(gameObject);
                    return;
                }
            }

            transform.position += Direction * step;
            traveled += step;

            if (traveled >= ability.Range - 0.001f)
            {
                Destroy(gameObject);
            }
        }

        static int CompareDistance(RaycastHit a, RaycastHit b)
        {
            return a.distance.CompareTo(b.distance);
        }
    }
}
