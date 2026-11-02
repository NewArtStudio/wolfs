using System.Collections.Generic;
using UnityEngine;

namespace VampWolfs
{
    /// <summary>
    /// Peasants' chest: the main loot of the map. Stand still next to it to open (channel); taking damage
    /// interrupts (like the extraction portal). Common chests open in ChestChannelTime, rare (golden) ones in
    /// RareChestChannelTime and give more. On this farm everything is tier I (MatchSettings.MapLootTier).
    /// Loot goes to the BAG: at risk until you extract.
    /// </summary>
    public class LootChest : MonoBehaviour
    {
        public static readonly List<LootChest> All = new List<LootChest>();

        static readonly Color Wood = new Color(0.4f, 0.27f, 0.15f);
        static readonly Color Iron = new Color(0.18f, 0.18f, 0.2f);
        static readonly Color Gold = new Color(0.95f, 0.75f, 0.25f);
        static readonly Color CommonGlow = new Color(1f, 0.8f, 0.5f);
        static readonly Color RareGlow = new Color(1f, 0.78f, 0.2f);

        public const float OpenRadius = 1.9f;

        public bool IsRare { get; private set; }
        public bool IsOpen { get; private set; }
        public bool IsChanneling { get; private set; }
        public float Progress01 { get { return Mathf.Clamp01(elapsed / ChannelTime); } }
        public float RemainingTime { get { return Mathf.Max(0f, ChannelTime - elapsed); } }
        float ChannelTime { get { return IsRare ? settings.RareChestChannelTime : settings.ChestChannelTime; } }

        MatchSettings settings;
        float elapsed;
        Transform lid;
        Light glow;
        CombatUnit subscribed;

        public static LootChest Create(Transform parent, Vector3 position, float yaw, bool rare, MatchSettings settings)
        {
            GameObject go = new GameObject(rare ? "ChestRare" : "Chest");
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(position.x, 0f, position.z);
            go.transform.rotation = Quaternion.Euler(0f, yaw, 0f);

            Color trim = rare ? Gold : Iron;
            Shapes.Create(PrimitiveType.Cube, go.transform, "Box", new Vector3(0f, 0.35f, 0f), new Vector3(1.3f, 0.7f, 0.85f), rare ? new Color(0.35f, 0.12f, 0.1f) : Wood);
            Shapes.Create(PrimitiveType.Cube, go.transform, "BandL", new Vector3(-0.45f, 0.36f, 0f), new Vector3(0.1f, 0.72f, 0.87f), trim);
            Shapes.Create(PrimitiveType.Cube, go.transform, "BandR", new Vector3(0.45f, 0.36f, 0f), new Vector3(0.1f, 0.72f, 0.87f), trim);
            Shapes.Create(PrimitiveType.Cube, go.transform, "Lock", new Vector3(0f, 0.55f, -0.44f), new Vector3(0.18f, 0.22f, 0.05f), Gold);

            // the lid hinges at the back edge
            Transform hinge = new GameObject("Lid").transform;
            hinge.SetParent(go.transform, false);
            hinge.localPosition = new Vector3(0f, 0.7f, 0.42f);
            Shapes.Create(PrimitiveType.Cube, hinge, "LidTop", new Vector3(0f, 0.12f, -0.42f), new Vector3(1.32f, 0.24f, 0.87f), rare ? new Color(0.35f, 0.12f, 0.1f) : Wood);
            Shapes.Create(PrimitiveType.Cube, hinge, "LidBand", new Vector3(0f, 0.13f, -0.42f), new Vector3(0.12f, 0.26f, 0.89f), trim);

            // glow on the ground so it reads from far away (rare = gold)
            Color glowColor = rare ? RareGlow : CommonGlow;
            Shapes.Create(PrimitiveType.Cylinder, go.transform, "GlowRing", new Vector3(0f, 0.07f, 0f), new Vector3(OpenRadius * 2f, 0.005f, OpenRadius * 2f), glowColor * 0.45f, true);

            GameObject lightGo = new GameObject("ChestLight");
            lightGo.transform.SetParent(go.transform, false);
            lightGo.transform.localPosition = new Vector3(0f, 1.4f, 0f);
            Light light = lightGo.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = glowColor;
            light.range = rare ? 7f : 5f;
            light.intensity = rare ? 3f : 1.8f;
            light.shadows = LightShadows.None;

            LootChest chest = go.AddComponent<LootChest>();
            chest.IsRare = rare;
            chest.settings = settings;
            chest.lid = hinge;
            chest.glow = light;
            return chest;
        }

        void OnEnable() { All.Add(this); }
        void OnDisable() { All.Remove(this); }

        void Update()
        {
            Animate();
            if (IsOpen) return;

            MatchManager match = MatchManager.Instance;
            CombatUnit player = match != null ? match.Player : null;
            if (match == null || match.IsOver || player == null || player.IsDead)
            {
                IsChanneling = false;
                return;
            }
            Subscribe(player);

            Vector3 offset = player.Position - transform.position;
            offset.y = 0f;
            // standing still next to it (moving or casting = not opening)
            bool opening = offset.magnitude <= OpenRadius && player.MoveInput.sqrMagnitude < 0.01f && !player.IsCasting
                && Time.time - player.LastCastTime > 0.5f && !OtherChestOpening();
            if (!opening)
            {
                IsChanneling = false;
                elapsed = 0f;
                return;
            }

            IsChanneling = true;
            elapsed += Time.deltaTime;
            if (elapsed >= ChannelTime) Open(player, match);
        }

        /// <summary>Only one chest opens at a time (two chests side by side don't open together).</summary>
        bool OtherChestOpening()
        {
            for (int i = 0; i < All.Count; i++) if (All[i] != this && All[i] != null && All[i].IsChanneling) return true;
            return false;
        }

        void Open(CombatUnit opener, MatchManager match)
        {
            IsOpen = true;
            IsChanneling = false;
            Unsubscribe();

            GearCatalog catalog = GearCatalog.Instance;
            int tier = settings.MapLootTier;
            int equipment = IsRare ? Random.Range(4, 6) : Random.Range(2, 4);
            int materials = IsRare ? Random.Range(2, 4) : Random.Range(1, 3);
            List<string> names = new List<string>();

            for (int i = 0; i < equipment; i++)
            {
                ItemInstance item = GearLoadouts.RandomEquipment(catalog, tier);
                if (item == null) continue;
                EquipDef def = catalog.GetEquip(item.DefId);
                // rare chests: better refine chance (still the same tier)
                if (IsRare && def != null && def.Refinable && Random.value < 0.6f) item.Refine = Mathf.Max(item.Refine, Random.Range(1, 4));
                opener.Inventory.AddItem(catalog, item);
                names.Add(catalog.NameOf(item));
            }
            for (int i = 0; i < materials; i++)
            {
                ItemInstance m = GearLoadouts.RandomMaterial(catalog);
                opener.Inventory.AddItem(catalog, m);
                names.Add(catalog.NameOf(m));
            }
            if (IsRare)
            {
                opener.Inventory.Add(ItemType.MoonShard, 2);
                names.Add("2 " + Inventory.DisplayName(ItemType.MoonShard));
            }

            FloatingText.Spawn(transform.position + Vector3.up * 2.4f, (IsRare ? "BAÚ RARO!\n" : "Baú aberto!\n") + string.Join("\n", names.ToArray()),
                IsRare ? RareGlow : new Color(1f, 0.85f, 0.35f), 2.6f, IsRare ? 1.1f : 0.95f);
            Effects.Burst(transform.position + Vector3.up * 0.8f, IsRare ? 2.4f : 1.6f, IsRare ? RareGlow : CommonGlow, 0.4f);
            match.AwardXp(IsRare ? MatchManager.XpRareChest : MatchManager.XpChest, IsRare ? "baú raro" : "baú");
        }

        void Animate()
        {
            if (IsOpen)
            {
                lid.localRotation = Quaternion.RotateTowards(lid.localRotation, Quaternion.Euler(105f, 0f, 0f), 240f * Time.deltaTime);
                glow.intensity = Mathf.MoveTowards(glow.intensity, 0f, Time.deltaTime * 2f);
                return;
            }
            float pulse = 0.5f + 0.5f * Mathf.Sin(Time.time * 2.5f + transform.position.x);
            float baseIntensity = IsRare ? 3f : 1.8f;
            glow.intensity = baseIntensity * (0.75f + 0.25f * pulse) * (IsChanneling ? 1f + Progress01 : 1f);
            // the lid rattles a little while being opened
            lid.localRotation = IsChanneling ? Quaternion.Euler(Progress01 * 20f + Mathf.Abs(Mathf.Sin(Time.time * 18f)) * 4f, 0f, 0f) : Quaternion.identity;
        }

        void Subscribe(CombatUnit player)
        {
            if (subscribed == player) return;
            Unsubscribe();
            subscribed = player;
            subscribed.Health.Damaged += OnPlayerDamaged;
        }

        void Unsubscribe()
        {
            if (subscribed != null && subscribed.Health != null) subscribed.Health.Damaged -= OnPlayerDamaged;
            subscribed = null;
        }

        void OnPlayerDamaged(float amount, CombatUnit source)
        {
            if (!IsChanneling || amount <= 0f) return;
            elapsed = 0f;
            FloatingText.Spawn(subscribed.Position + Vector3.up * 3f, "Abertura interrompida!", new Color(1f, 0.8f, 0.5f), 1.2f, 1.1f);
        }

        void OnDestroy()
        {
            Unsubscribe();
        }
    }
}
