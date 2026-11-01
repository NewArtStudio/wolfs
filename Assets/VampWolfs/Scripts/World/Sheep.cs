using System.Collections.Generic;
using UnityEngine;

namespace VampWolfs
{
    /// <summary>
    /// Farmable creature (PvE). Wanders, flees from characters, drops loot straight into the
    /// killer's BAG — the bag is what is at risk in PvP.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class Sheep : MonoBehaviour
    {
        public static readonly List<Sheep> All = new List<Sheep>();

        const float WalkSpeed = 1.4f;
        const float FleeSpeed = 4.2f;
        const float FleeDistance = 5f;
        const float MaxHealth = 18f;

        public Health Health { get; private set; }

        CharacterController controller;
        Vector3 wanderTarget;
        float nextWanderTime;
        float halfSize;

        public static Sheep Create(Transform parent, Vector3 position)
        {
            GameObject go = new GameObject("Sheep");
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(position.x, 0f, position.z);
            go.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);

            CharacterController cc = go.AddComponent<CharacterController>();
            cc.radius = 0.55f;
            cc.height = 1.3f;
            cc.center = new Vector3(0f, 0.65f, 0f);
            cc.stepOffset = 0.2f;

            Health health = go.AddComponent<Health>();
            health.Init(MaxHealth, Team.Neutral);

            UnitVisual visual = go.AddComponent<UnitVisual>();
            Color wool = new Color(0.9f, 0.9f, 0.86f);
            Color black = new Color(0.1f, 0.1f, 0.1f);
            visual.Part(PrimitiveType.Cube, "Body", new Vector3(0f, 0.75f, 0f), new Vector3(0.9f, 0.7f, 1.3f), wool);
            visual.Part(PrimitiveType.Cube, "Head", new Vector3(0f, 0.95f, 0.75f), new Vector3(0.4f, 0.42f, 0.45f), black);
            visual.Part(PrimitiveType.Cube, "LegFL", new Vector3(-0.28f, 0.2f, 0.4f), new Vector3(0.15f, 0.4f, 0.15f), black);
            visual.Part(PrimitiveType.Cube, "LegFR", new Vector3(0.28f, 0.2f, 0.4f), new Vector3(0.15f, 0.4f, 0.15f), black);
            visual.Part(PrimitiveType.Cube, "LegBL", new Vector3(-0.28f, 0.2f, -0.4f), new Vector3(0.15f, 0.4f, 0.15f), black);
            visual.Part(PrimitiveType.Cube, "LegBR", new Vector3(0.28f, 0.2f, -0.4f), new Vector3(0.15f, 0.4f, 0.15f), black);

            Sheep sheep = go.AddComponent<Sheep>();
            sheep.controller = cc;
            sheep.Health = health;
            sheep.halfSize = MatchManager.Instance != null ? MatchManager.Instance.Settings.ArenaHalfSize : 28f;
            sheep.wanderTarget = go.transform.position;
            health.Died += sheep.OnDied;
            return sheep;
        }

        void OnEnable() { All.Add(this); }
        void OnDisable() { All.Remove(this); }

        void Update()
        {
            if (Health == null || Health.IsDead)
            {
                return;
            }

            Vector3 velocity;
            CombatUnit threat = NearestThreat();

            if (threat != null)
            {
                Vector3 away = transform.position - threat.Position;
                away.y = 0f;
                velocity = away.normalized * FleeSpeed;
            }
            else
            {
                if (Time.time > nextWanderTime)
                {
                    nextWanderTime = Time.time + Random.Range(2f, 5f);
                    Vector3 offset = new Vector3(Random.Range(-5f, 5f), 0f, Random.Range(-5f, 5f));
                    float h = halfSize - 2f;
                    Vector3 target = transform.position + offset;
                    wanderTarget = new Vector3(Mathf.Clamp(target.x, -h, h), 0f, Mathf.Clamp(target.z, -h, h));
                }

                Vector3 toTarget = wanderTarget - transform.position;
                toTarget.y = 0f;
                velocity = toTarget.magnitude > 0.5f ? toTarget.normalized * WalkSpeed : Vector3.zero;
            }

            if (velocity.sqrMagnitude > 0.01f)
            {
                transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(velocity), 360f * Time.deltaTime);
            }

            if (controller.enabled)
            {
                controller.Move((velocity + Vector3.down * 4f) * Time.deltaTime);
            }
        }

        CombatUnit NearestThreat()
        {
            CombatUnit nearest = null;
            float best = FleeDistance;

            for (int i = 0; i < CombatUnit.All.Count; i++)
            {
                CombatUnit unit = CombatUnit.All[i];
                if (unit == null || unit.IsDead)
                {
                    continue;
                }

                float d = Vector3.Distance(unit.Position, transform.position);
                if (d < best)
                {
                    best = d;
                    nearest = unit;
                }
            }

            return nearest;
        }

        void OnDied(CombatUnit killer)
        {
            controller.enabled = false;

            if (killer != null && !killer.IsDead)
            {
                GiveLoot(killer);
            }

            Effects.Puff(transform.position, new Color(0.9f, 0.9f, 0.86f), 8);

            if (MatchManager.Instance != null)
            {
                MatchManager.Instance.OnSheepKilled();
            }

            Destroy(gameObject);
        }

        static void GiveLoot(CombatUnit killer)
        {
            Vector3 textPosition = killer.Position + Vector3.up * 3.1f;

            killer.Inventory.Add(ItemType.Blood, 1);
            string text = "+1 " + Inventory.DisplayName(ItemType.Blood);

            if (Random.value < 0.5f)
            {
                killer.Inventory.Add(ItemType.Wool, 1);
                text += "  +1 " + Inventory.DisplayName(ItemType.Wool);
            }

            if (Random.value < 0.06f)
            {
                killer.Inventory.Add(ItemType.MoonShard, 1);
                text += "  +1 " + Inventory.DisplayName(ItemType.MoonShard) + "!";
            }

            // Gear drops (go to the bag, at risk until extraction).
            GearCatalog catalog = GearCatalog.Instance;
            ItemInstance drop = null;
            if (Random.value < 0.04f) drop = GearLoadouts.RandomEquipment(catalog, 1);
            else if (Random.value < 0.12f) drop = GearLoadouts.RandomMaterial(catalog);
            if (drop != null)
            {
                killer.Inventory.AddItem(catalog, drop);
                text += "\n+ " + catalog.NameOf(drop) + "!";
            }

            FloatingText.Spawn(textPosition, text, new Color(1f, 0.85f, 0.35f), 1.4f, 0.9f);

            // Feeding: drinking from the kill restores a little health and energy.
            killer.Health.HealScaled(6f);
            killer.Energy.Add(5f);
        }
    }
}
