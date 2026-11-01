using System.Collections.Generic;
using UnityEngine;

namespace VampWolfs
{
    /// <summary>
    /// Bag dropped on death. Any living character that walks over it takes everything inside.
    /// </summary>
    public class LootBag : MonoBehaviour
    {
        public static readonly List<LootBag> Active = new List<LootBag>();

        const float PickupRadius = 1.6f;
        const float PickupDelay = 0.8f;

        Dictionary<ItemType, int> contents;
        List<ItemInstance> gear = new List<ItemInstance>();
        Transform model;
        float spawnTime;
        bool picked;

        public string DroppedBy { get; private set; }

        public int TotalItems
        {
            get
            {
                int total = 0;
                foreach (KeyValuePair<ItemType, int> pair in contents) total += pair.Value;
                foreach (ItemInstance item in gear) total += item.Qty;
                return total;
            }
        }

        public static LootBag Spawn(Transform parent, Vector3 position, Dictionary<ItemType, int> items, List<ItemInstance> gearItems, string droppedBy)
        {
            GameObject go = new GameObject("LootBag (" + droppedBy + ")");
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(position.x, 0f, position.z);

            GameObject modelGo = new GameObject("Model");
            modelGo.transform.SetParent(go.transform, false);
            Color sack = new Color(0.45f, 0.32f, 0.18f);
            Color gold = new Color(1f, 0.8f, 0.25f);
            Shapes.Create(PrimitiveType.Sphere, modelGo.transform, "Sack", new Vector3(0f, 0.45f, 0f), new Vector3(0.8f, 0.75f, 0.8f), sack);
            Shapes.Create(PrimitiveType.Cube, modelGo.transform, "Tie", new Vector3(0f, 0.88f, 0f), new Vector3(0.25f, 0.2f, 0.25f), gold, true);
            Shapes.Create(PrimitiveType.Cylinder, go.transform, "Glow", new Vector3(0f, 0.03f, 0f), new Vector3(1.6f, 0.01f, 1.6f), gold, true);

            GameObject lightGo = new GameObject("BagLight");
            lightGo.transform.SetParent(go.transform, false);
            lightGo.transform.localPosition = new Vector3(0f, 1.2f, 0f);
            Light light = lightGo.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = gold;
            light.range = 5f;
            light.intensity = 2f;

            LootBag bag = go.AddComponent<LootBag>();
            bag.contents = new Dictionary<ItemType, int>(items);
            if (gearItems != null) bag.gear = new List<ItemInstance>(gearItems);
            bag.model = modelGo.transform;
            bag.spawnTime = Time.time;
            bag.DroppedBy = droppedBy;

            FloatingText.Spawn(go.transform.position + Vector3.up * 1.6f, "Bag caiu (" + bag.TotalItems + " itens)", gold, 1.6f, 1f);
            return bag;
        }

        void OnEnable() { Active.Add(this); }
        void OnDisable() { Active.Remove(this); }

        void Update()
        {
            model.localPosition = new Vector3(0f, 0.1f + Mathf.Sin(Time.time * 3f) * 0.08f, 0f);
            model.Rotate(0f, 60f * Time.deltaTime, 0f);

            bool matchOver = MatchManager.Instance != null && MatchManager.Instance.IsOver;
            if (picked || matchOver || Time.time - spawnTime < PickupDelay)
            {
                return;
            }

            for (int i = 0; i < CombatUnit.All.Count; i++)
            {
                CombatUnit unit = CombatUnit.All[i];
                if (unit == null || unit.IsDead)
                {
                    continue;
                }

                Vector3 offset = unit.Position - transform.position;
                offset.y = 0f;
                if (offset.magnitude <= PickupRadius)
                {
                    PickUp(unit);
                    return;
                }
            }
        }

        void PickUp(CombatUnit unit)
        {
            picked = true;
            unit.Inventory.AddAll(contents);
            unit.Inventory.AddItems(GearCatalog.Instance, gear);
            Effects.Puff(transform.position, new Color(1f, 0.8f, 0.25f), 10);

            if (MatchManager.Instance != null)
            {
                MatchManager.Instance.OnLootPicked(this, unit);
            }

            Destroy(gameObject);
        }
    }
}
