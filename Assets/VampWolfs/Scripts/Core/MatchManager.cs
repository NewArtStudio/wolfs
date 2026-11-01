using System.Collections.Generic;
using UnityEngine;

namespace VampWolfs
{
    public enum MatchState
    {
        Farming,     // alone in the map, farming sheep
        Invaded,     // the werewolf is in the map
        Extracted,   // player reached the portal: keeps the bag (win)
        PlayerDead   // player died: bag dropped (loss)
    }

    /// <summary>
    /// Match rules in the spirit of Albion's Mists:
    /// farm alone -> an invader may arrive -> PvP -> the loser drops (part of) the BAG only
    /// -> leave through the extraction portal to keep what you carry.
    /// </summary>
    public class MatchManager : MonoBehaviour
    {
        public static MatchManager Instance { get; private set; }

        public MatchSettings Settings { get; private set; }
        public MatchState State { get; private set; }
        public CombatUnit Player { get; private set; }
        public CombatUnit Invader { get; private set; }
        public ExtractionPoint Extraction { get; private set; }
        public float Elapsed { get; private set; }
        public bool InvaderDefeated { get; private set; }

        /// <summary>Class name of the invader ("Lobisomem", "Caçador de Recompensas"…), for banners and the HUD.</summary>
        public string InvaderClassName { get; private set; }

        /// <summary>XP rewards [PROTÓTIPO].</summary>
        public const int XpSheep = 8, XpInvader = 120, XpExtraction = 60;

        /// <summary>XP earned by the hero this match (shown on the end screen).</summary>
        public int XpEarned { get; private set; }

        /// <summary>Bag contents the player kept (only valid when Extracted).</summary>
        public Dictionary<ItemType, int> FinalLoot { get; private set; }

        /// <summary>Equipment and materials the player extracted (now in the stash).</summary>
        public List<ItemInstance> FinalItems { get; private set; }

        /// <summary>Saved progress (worn gear, stash). Extraction deposits the bag here.</summary>
        public PlayerProfile Profile { get; private set; }

        /// <summary>How many items were on the ground and how many were destroyed by the last death.</summary>
        public int LastDropOnGround { get; private set; }
        public int LastDropDestroyed { get; private set; }

        public string BannerText { get; private set; }
        public Color BannerColor { get; private set; }
        public float BannerUntil { get; private set; }

        public bool IsOver
        {
            get { return State == MatchState.Extracted || State == MatchState.PlayerDead; }
        }

        public float TimeUntilInvasion
        {
            get { return Mathf.Max(0f, Settings.InvasionTime - Elapsed); }
        }

        GameBootstrap bootstrap;
        FarmLayout layout;
        bool invasionWarned;
        readonly List<float> sheepRespawnTimes = new List<float>();

        public void Init(GameBootstrap owner, MatchSettings settings, FarmLayout farmLayout, CombatUnit player, ExtractionPoint extraction, PlayerProfile profile)
        {
            Instance = this;
            bootstrap = owner;
            Settings = settings;
            layout = farmLayout;
            Player = player;
            Extraction = extraction;
            State = MatchState.Farming;
            FinalLoot = new Dictionary<ItemType, int>();
            FinalItems = new List<ItemInstance>();
            Profile = profile;
            InvaderClassName = "Invasor";
            if (Profile != null)
            {
                Profile.Runs++;
                Profile.Save();
            }

            Player.Died += OnUnitDied;

            for (int i = 0; i < Settings.SheepCount; i++)
            {
                SpawnSheep();
            }

            ShowBanner("Farme ovelhas e extraia no portal roxo.\nCuidado: as brumas atraem invasores.", new Color(0.85f, 0.8f, 1f), 6f);
        }

        void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        void Update()
        {
            UpdateSheepRespawns();

            if (IsOver)
            {
                return;
            }

            Elapsed += Time.deltaTime;

            if (State == MatchState.Farming)
            {
                if (!invasionWarned && Elapsed >= Settings.InvasionTime - Settings.InvasionWarning)
                {
                    invasionWarned = true;
                    ShowBanner("Uivos ecoam na névoa... algo se aproxima.", new Color(1f, 0.7f, 0.3f), 4f);
                }

                if (Elapsed >= Settings.InvasionTime)
                {
                    SpawnInvader();
                }
            }
        }

        public void ShowBanner(string text, Color color, float duration)
        {
            BannerText = text;
            BannerColor = color;
            BannerUntil = Time.time + duration;
        }

        /// <summary>Leaves the match and goes back to the Base screen.</summary>
        public void RequestRestart()
        {
            bootstrap.ReturnToBase();
        }

        // ---------------------------------------------------------------- invasion

        void SpawnInvader()
        {
            Vector3 spawn = FarthestSpawnFromPlayer();

            // The invader is always of ANOTHER class (decided with the creator), with gear of the player's power tier.
            GearCatalog catalog = GearCatalog.Instance;
            SkillCatalog skills = SkillCatalog.Instance;
            int tier = Mathf.Clamp(Mathf.RoundToInt(Player.Equipment.Stats.PowerTier), 1, 8);
            string heroClass = Profile != null ? Profile.Current.ClassId : "vampire";
            List<string> others = new List<string>();
            foreach (ClassDef c in skills.Classes) if (c.Id != heroClass) others.Add(c.Id);
            string invaderClass = others.Count > 0 ? others[Random.Range(0, others.Count)] : "werewolf";
            ClassDef invaderDef = skills.Class(invaderClass);
            InvaderClassName = invaderDef.Name;

            CharacterSave brain = CharacterSave.Create(invaderClass, invaderDef.Name);
            brain.Level = Mathf.Clamp(Profile != null ? Profile.Current.Level : 1, 1, SkillCatalog.MaxCharacterLevel);
            brain.AutoAllocate(skills);
            Dictionary<string, ItemInstance> invaderGear = GearLoadouts.ClassLoadout(catalog, invaderClass, tier, Random.Range(0, 5));

            Invader = CharacterFactory.CreateCharacter(layout.Root, spawn, invaderDef, invaderDef.Name + " invasor", true);
            CharacterFactory.ApplyLoadout(Invader, brain, invaderGear);
            Invader.gameObject.AddComponent<WerewolfAI>().Init(Settings);
            Invader.Died += OnUnitDied;

            // The invader arrives with loot it farmed elsewhere: the reason to fight it.
            Invader.Inventory.Add(ItemType.Blood, 5);
            Invader.Inventory.Add(ItemType.Wool, 4);
            Invader.Inventory.Add(ItemType.MoonShard, 2);

            // Invasion by power bracket: the invader carries some loot of the player's tier.
            Invader.Inventory.AddItem(catalog, GearLoadouts.RandomEquipment(catalog, tier));
            Invader.Inventory.AddItem(catalog, GearLoadouts.RandomEquipment(catalog, tier));
            Invader.Inventory.AddItem(catalog, GearLoadouts.RandomMaterial(catalog));
            Invader.Inventory.AddItem(catalog, GearLoadouts.RandomMaterial(catalog));

            State = MatchState.Invaded;
            if (!Vfx.SkillFx.InvaderArrives(spawn))
            {
                Effects.Burst(spawn, 3.5f, new Color(0.55f, 0.25f, 0.9f), 0.8f);
            }
            ShowBanner("UM " + InvaderClassName.ToUpperInvariant() + " INVADIU A FAZENDA!\nLute pela bag dele ou fuja pelo portal.", new Color(1f, 0.45f, 0.25f), 4.5f);
        }

        Vector3 FarthestSpawnFromPlayer()
        {
            Vector3 best = layout.InvaderSpawns[0];
            float bestDistance = -1f;

            for (int i = 0; i < layout.InvaderSpawns.Length; i++)
            {
                float d = Vector3.Distance(layout.InvaderSpawns[i], Player.Position);
                if (d > bestDistance)
                {
                    bestDistance = d;
                    best = layout.InvaderSpawns[i];
                }
            }

            return best;
        }

        // ---------------------------------------------------------------- experience

        /// <summary>Gives XP to the selected character (saved at once on level up).</summary>
        public void AwardXp(int amount, string reason)
        {
            if (Profile == null || amount <= 0 || State == MatchState.PlayerDead) return;
            CharacterSave hero = Profile.Current;
            XpEarned += amount;
            int levels = hero.AddXp(amount);
            if (Player != null) FloatingText.Spawn(Player.Position + Vector3.up * 3.3f, "+" + amount + " XP", new Color(0.75f, 0.6f, 1f), 1.2f, 0.9f);
            if (levels > 0)
            {
                ShowBanner("NÍVEL " + hero.Level + "!\n+" + levels + " ponto" + (levels > 1 ? "s" : "") + " de habilidade (use no lobby)", new Color(0.8f, 0.65f, 1f), 4f);
                Vfx.SkillFx.LevelUp(Player != null ? Player.Position : Vector3.zero);
                Profile.Save();
            }
        }

        // ---------------------------------------------------------------- death & loot

        void OnUnitDied(CombatUnit unit, CombatUnit killer)
        {
            // Match already decided (e.g. a projectile still in flight): don't change the result.
            if (State == MatchState.Extracted)
            {
                return;
            }

            bool alreadyOver = IsOver;
            DropBag(unit, !alreadyOver);

            if (alreadyOver)
            {
                return;
            }

            if (unit == Player)
            {
                State = MatchState.PlayerDead;
                if (Profile != null) Profile.Save(); // the bag is lost; worn gear stays in the profile
            }
            else if (unit == Invader)
            {
                InvaderDefeated = true;
                ShowBanner(InvaderClassName + " derrotado!\nPegue a bag dele e extraia no portal.", new Color(0.6f, 1f, 0.6f), 4.5f);
                if (killer == Player || killer == null) AwardXp(XpInvader, "invasor derrotado");
            }
        }

        /// <summary>Only the BAG drops, and only a share of it. Equipment is never touched.</summary>
        void DropBag(CombatUnit unit, bool recordStats)
        {
            Dictionary<ItemType, int> all = unit.Inventory.TakeAll();
            Dictionary<ItemType, int> dropped = new Dictionary<ItemType, int>();
            int total = 0;
            int onGround = 0;

            foreach (KeyValuePair<ItemType, int> pair in all)
            {
                total += pair.Value;
                int amount = Mathf.FloorToInt(pair.Value * Settings.BagDropShare + 0.5f);
                if (amount > 0)
                {
                    dropped[pair.Key] = amount;
                    onGround += amount;
                }
            }

            // Found equipment and materials: each piece drops with the same chance, stacks keep the same share.
            List<ItemInstance> droppedItems = new List<ItemInstance>();
            foreach (ItemInstance item in unit.Inventory.TakeAllItems())
            {
                total += item.Qty;
                int amount = item.Qty == 1 ? (Random.value < Settings.BagDropShare ? 1 : 0) : Mathf.FloorToInt(item.Qty * Settings.BagDropShare + 0.5f);
                if (amount <= 0) continue;
                droppedItems.Add(amount == item.Qty ? item : item.CloneWithQty(amount));
                onGround += amount;
            }

            if (recordStats)
            {
                LastDropOnGround = onGround;
                LastDropDestroyed = total - onGround;
            }

            if (onGround > 0)
            {
                LootBag.Spawn(layout.Root, unit.Position, dropped, droppedItems, unit.DisplayName);
            }
        }

        public void OnLootPicked(LootBag bag, CombatUnit picker)
        {
            if (picker == Player)
            {
                ShowBanner("Você pegou a bag (+" + bag.TotalItems + " itens).\nAgora extraia para garantir!", new Color(1f, 0.85f, 0.35f), 3.5f);
            }
            else
            {
                ShowBanner(picker.DisplayName + " pegou a bag.", new Color(1f, 0.5f, 0.3f), 3f);
            }
        }

        public void OnPlayerExtracted()
        {
            if (IsOver)
            {
                return;
            }

            // Extracted players have left the map: nothing can hit them anymore.
            Player.CancelCast(false);
            Player.Status.ApplyInvulnerable(9999f);

            FinalLoot = Player.Inventory.Snapshot();
            FinalItems = new List<ItemInstance>(Player.Inventory.Items);
            State = MatchState.Extracted;

            if (Profile != null)
            {
                Profile.DepositBag(GearCatalog.Instance, Player.Inventory);
                Player.Inventory.TakeAll();
                Player.Inventory.TakeAllItems();
                Profile.Extractions++;
                AwardXp(XpExtraction, "extração");
                Profile.Save();
            }
        }

        // ---------------------------------------------------------------- sheep

        void SpawnSheep()
        {
            Vector3 position = FarmBuilder.RandomFreePoint(layout, 3f);
            Sheep.Create(layout.Root, position);
        }

        public void OnSheepKilled()
        {
            sheepRespawnTimes.Add(Time.time + Settings.SheepRespawnTime);
        }

        void UpdateSheepRespawns()
        {
            for (int i = sheepRespawnTimes.Count - 1; i >= 0; i--)
            {
                if (Time.time >= sheepRespawnTimes[i])
                {
                    sheepRespawnTimes.RemoveAt(i);
                    SpawnSheep();
                }
            }
        }
    }
}
