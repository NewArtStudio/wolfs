using System.Collections.Generic;
using UnityEngine;

namespace VampWolfs
{
    /// <summary>
    /// Farm folk (camponês / camponesa) with a dagger, a plow or a shotgun. They guard the chests in groups
    /// (or roam alone), attack the player AND the invader, and drop only tier-I equipment (MatchSettings.MapLootTier).
    /// The brain is PeasantAI; this class creates them, keeps the group and handles death/loot.
    /// </summary>
    public class Peasant : MonoBehaviour
    {
        public static readonly List<Peasant> All = new List<Peasant>();

        static readonly Color RingColor = new Color(0.75f, 0.55f, 0.3f);
        static readonly Color[] Skins = { new Color(0.93f, 0.78f, 0.65f), new Color(0.82f, 0.64f, 0.5f), new Color(0.62f, 0.45f, 0.33f) };
        static readonly Color[] Shirts = { new Color(0.55f, 0.5f, 0.4f), new Color(0.35f, 0.4f, 0.5f), new Color(0.5f, 0.32f, 0.28f), new Color(0.4f, 0.45f, 0.3f) };
        static readonly Color Trousers = new Color(0.28f, 0.24f, 0.2f);
        static readonly Color Straw = new Color(0.8f, 0.68f, 0.38f);
        static readonly Color Apron = new Color(0.82f, 0.8f, 0.72f);
        static readonly Color Steel = new Color(0.7f, 0.72f, 0.78f);
        static readonly Color Wood = new Color(0.36f, 0.25f, 0.16f);

        public CombatUnit Unit { get; private set; }
        public PeasantWeapon Weapon { get; private set; }
        public Vector3 Home { get; set; }
        public bool IsGuard { get; private set; }

        /// <summary>Who this peasant is fighting (null = calm).</summary>
        public CombatUnit Target { get; set; }

        /// <summary>The other peasants of the same chest: when one is attacked, all come.</summary>
        public List<Peasant> Group { get; set; }

        public static Peasant Create(Transform parent, Vector3 position, PeasantWeapon weapon, bool female, bool guard, MatchSettings settings, System.Random rng)
        {
            string name = female ? "Camponesa" : "Camponês";
            CombatUnit unit = CharacterFactory.CreateUnit(parent, name, position, Team.Peasants,
                PeasantKit.Health(weapon), PeasantKit.Speed(weapon), 0.45f, 1.9f, RingColor);
            unit.IsMinor = true;
            unit.SetAbilities(PeasantKit.Create(weapon));
            BuildLook(unit.Visual, weapon, female, rng);
            Vfx.InkOutline.Attach(unit.Visual.Model.gameObject, Vfx.InkOutline.EnemyInk);
            Vfx.UnitFx.Attach(unit.gameObject, Vfx.UnitFx.Kind.Human);
            unit.transform.rotation = Quaternion.Euler(0f, (float)rng.NextDouble() * 360f, 0f);

            Peasant p = unit.gameObject.AddComponent<Peasant>();
            p.Unit = unit;
            p.Weapon = weapon;
            p.Home = position;
            p.IsGuard = guard;
            p.Group = new List<Peasant> { p };
            unit.Died += p.OnDied;
            unit.Health.Damaged += p.OnDamaged;

            unit.gameObject.AddComponent<PeasantAI>().Init(p, settings);
            return p;
        }

        void OnEnable() { All.Add(this); }
        void OnDisable() { All.Remove(this); }

        /// <summary>Hit by someone: fight back, and call the group.</summary>
        void OnDamaged(float amount, CombatUnit source)
        {
            if (source == null || source.IsDead || source.Team == Team.Peasants || source.Team == Team.Neutral) return;
            Alert(source);
        }

        /// <summary>This peasant and its whole group go after "enemy" (unless already fighting someone).</summary>
        public void Alert(CombatUnit enemy)
        {
            if (Group == null) return;
            for (int i = 0; i < Group.Count; i++)
            {
                Peasant p = Group[i];
                if (p == null || p.Unit == null || p.Unit.IsDead) continue;
                if (p.Target == null || p.Target.IsDead || p == this) p.Target = enemy;
            }
        }

        void OnDied(CombatUnit self, CombatUnit killer)
        {
            MatchManager match = MatchManager.Instance;
            if (killer != null && !killer.IsDead && match != null && !match.IsOver)
            {
                GiveLoot(killer, match.Settings);
                if (killer.IsPlayer) match.AwardXp(MatchManager.XpPeasant, Unit.DisplayName.ToLowerInvariant());
            }
            Effects.Puff(Unit.Position + Vector3.up, new Color(0.5f, 0.45f, 0.4f), 6);
            Destroy(gameObject, 3f);   // let the death pose show, then clean up
        }

        /// <summary>Peasants drop ONLY tier-I equipment (decided with the creator), and not always.</summary>
        static void GiveLoot(CombatUnit killer, MatchSettings settings)
        {
            if (Random.value >= settings.PeasantDropChance) return;
            GearCatalog catalog = GearCatalog.Instance;
            ItemInstance drop = GearLoadouts.RandomEquipment(catalog, settings.MapLootTier);
            if (drop == null) return;
            killer.Inventory.AddItem(catalog, drop);
            if (killer.IsPlayer)
            {
                FloatingText.Spawn(killer.Position + Vector3.up * 3.1f, "+ " + catalog.NameOf(drop) + "!", new Color(1f, 0.85f, 0.35f), 1.4f, 0.9f);
            }
        }

        // ---------------------------------------------------------------- look (primitives; swap for models later)

        static void BuildLook(UnitVisual v, PeasantWeapon weapon, bool female, System.Random rng)
        {
            Color skin = Skins[rng.Next(Skins.Length)];
            Color shirt = Shirts[rng.Next(Shirts.Length)];

            if (female)
            {
                v.Part(PrimitiveType.Cylinder, "Skirt", new Vector3(0f, 0.45f, 0f), new Vector3(0.75f, 0.45f, 0.65f), shirt * 0.8f);
                v.Part(PrimitiveType.Cube, "Apron", new Vector3(0f, 0.55f, 0.3f), new Vector3(0.45f, 0.7f, 0.05f), Apron);
            }
            else
            {
                v.Part(PrimitiveType.Cube, "LegL", new Vector3(-0.14f, 0.42f, 0f), new Vector3(0.2f, 0.84f, 0.22f), Trousers);
                v.Part(PrimitiveType.Cube, "LegR", new Vector3(0.14f, 0.42f, 0f), new Vector3(0.2f, 0.84f, 0.22f), Trousers);
            }

            v.Part(PrimitiveType.Cube, "Body", new Vector3(0f, 1.15f, 0f), new Vector3(0.55f, 0.65f, 0.32f), shirt);
            v.Part(PrimitiveType.Cube, "ArmL", new Vector3(-0.36f, 1.12f, 0f), new Vector3(0.15f, 0.6f, 0.15f), shirt * 0.9f);
            v.Part(PrimitiveType.Cube, "ArmR", new Vector3(0.36f, 1.12f, 0.08f), new Vector3(0.15f, 0.6f, 0.15f), shirt * 0.9f);
            v.Part(PrimitiveType.Sphere, "Head", new Vector3(0f, 1.66f, 0f), Vector3.one * 0.38f, skin);

            if (female)
            {
                // headscarf / bonnet
                v.Part(PrimitiveType.Sphere, "Bonnet", new Vector3(0f, 1.72f, -0.04f), new Vector3(0.44f, 0.36f, 0.44f), Apron * 0.95f);
            }
            else
            {
                // straw hat
                v.Part(PrimitiveType.Cylinder, "HatBrim", new Vector3(0f, 1.84f, 0f), new Vector3(0.75f, 0.02f, 0.75f), Straw);
                v.Part(PrimitiveType.Cylinder, "HatTop", new Vector3(0f, 1.93f, 0f), new Vector3(0.38f, 0.09f, 0.38f), Straw * 0.95f);
            }

            switch (weapon)
            {
                case PeasantWeapon.Dagger:
                    v.Part(PrimitiveType.Cube, "DaggerHilt", new Vector3(0.38f, 0.82f, 0.22f), new Vector3(0.06f, 0.06f, 0.14f), Wood);
                    v.Part(PrimitiveType.Cube, "DaggerBlade", new Vector3(0.38f, 0.82f, 0.42f), new Vector3(0.04f, 0.05f, 0.28f), Steel);
                    break;
                case PeasantWeapon.Plow:
                    // hand plow held like a heavy pole: long handle + iron share at the front
                    v.Part(PrimitiveType.Cube, "PlowHandle", new Vector3(0.3f, 1f, 0.45f), new Vector3(0.08f, 0.08f, 1.5f), Wood);
                    v.Part(PrimitiveType.Cube, "PlowShare", new Vector3(0.3f, 0.82f, 1.2f), new Vector3(0.36f, 0.36f, 0.08f), Steel * 0.7f);
                    v.Part(PrimitiveType.Cube, "PlowGrip", new Vector3(0.3f, 1.1f, -0.25f), new Vector3(0.4f, 0.06f, 0.06f), Wood);
                    break;
                case PeasantWeapon.Shotgun:
                    v.Part(PrimitiveType.Cube, "GunStock", new Vector3(0.3f, 1.12f, 0.1f), new Vector3(0.1f, 0.14f, 0.4f), Wood);
                    // a cylinder points up: lay the barrel forward
                    v.Part(PrimitiveType.Cylinder, "GunBarrel", new Vector3(0.3f, 1.16f, 0.62f), new Vector3(0.06f, 0.35f, 0.06f), new Color(0.15f, 0.15f, 0.17f))
                        .transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                    break;
            }

            // same proportions as the heroes (MatchSettings.CharacterSize)
            v.Model.localScale = Vector3.one * CharacterFactory.SizeMultiplier;
        }
    }
}
