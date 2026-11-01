using System.Collections.Generic;
using UnityEngine;

namespace VampWolfs
{
    /// <summary>
    /// Builds characters from code: components + stats + kit + placeholder model.
    /// When real models exist, only the "model" part of each method changes.
    /// </summary>
    public static class CharacterFactory
    {
        public static readonly Color VampireTeamColor = new Color(0.85f, 0.1f, 0.2f);
        public static readonly Color WerewolfTeamColor = new Color(1f, 0.55f, 0.1f);

        /// <summary>Scales models and collision (MatchSettings.CharacterSize). Set by GameBootstrap.</summary>
        public static float SizeMultiplier = 1f;

        /// <summary>Vampire stats before gear (the Base screen previews gear on top of these).</summary>
        public const float VampireHealth = 160f;
        public const float VampireSpeed = 6.2f;

        public static CombatUnit CreateVampire(Transform parent, Vector3 position)
        {
            CombatUnit unit = CreateUnit(parent, "Vampiro", position, Team.Vampires, VampireHealth, VampireSpeed, 0.5f, 2.2f, VampireTeamColor);
            UnitVisual v = unit.Visual;

            if (ArtLibrary.Vampire != null && ArtLibrary.Vampire.IsValid)
            {
                v.UseModel(ArtLibrary.Vampire, SizeMultiplier);
            }
            else
            {
                BuildVampirePlaceholder(v);
            }

            Vfx.InkOutline.Attach(v.Model.gameObject, Vfx.InkOutline.Ink);   // cartoon ink line
            Vfx.UnitFx.Attach(unit.gameObject, Vfx.UnitFx.Kind.Vampire);       // blood, cast glow, shields, footsteps, death

            unit.SetAbilities(VampireKit.Create());
            unit.Equipment.Set("Adaga Ritual T4", "Manto da Noite T4", 820);
            return unit;
        }

        public static CombatUnit CreateWerewolf(Transform parent, Vector3 position)
        {
            CombatUnit unit = CreateUnit(parent, "Lobisomem", position, Team.Werewolves, 200f, 6.0f, 0.65f, 2.6f, WerewolfTeamColor);
            UnitVisual v = unit.Visual;

            if (ArtLibrary.Werewolf != null && ArtLibrary.Werewolf.IsValid)
            {
                v.UseModel(ArtLibrary.Werewolf, SizeMultiplier);
            }
            else
            {
                BuildWerewolfPlaceholder(v);
            }

            Vfx.InkOutline.Attach(v.Model.gameObject, Vfx.InkOutline.EnemyInk);
            Vfx.UnitFx.Attach(unit.gameObject, Vfx.UnitFx.Kind.Beast);

            unit.SetAbilities(WerewolfKit.Create());
            unit.Equipment.Set("Garras de Prata Negra T4", "Pele Ancestral T4", 840);
            return unit;
        }

        public static readonly Color HunterTeamColor = new Color(0.35f, 0.65f, 0.3f);

        /// <summary>
        /// Any playable class (nov/2026): body, stats from the class data, art or placeholder, ink outline and VFX.
        /// The skills come from SkillFactory.BuildBar (gear + class); call ApplyLoadout after creating.
        /// enemy = drawn with the red enemy ink (the AI invader).
        /// </summary>
        public static CombatUnit CreateCharacter(Transform parent, Vector3 position, ClassDef cls, string displayName, bool enemy)
        {
            string id = cls != null ? cls.Id : "vampire";
            Team team = id == "werewolf" ? Team.Werewolves : id == "hunter" ? Team.Humans : Team.Vampires;
            Color ring = id == "werewolf" ? WerewolfTeamColor : id == "hunter" ? HunterTeamColor : VampireTeamColor;
            float radius = id == "werewolf" ? 0.65f : 0.5f;
            float height = id == "werewolf" ? 2.6f : id == "hunter" ? 2.15f : 2.2f;
            float health = cls != null ? cls.Health : VampireHealth;
            float speed = cls != null ? cls.Speed : VampireSpeed;

            CombatUnit unit = CreateUnit(parent, displayName, position, team, health, speed, radius, height, ring);
            BuildLook(unit.Visual, id, SizeMultiplier);
            Vfx.InkOutline.Attach(unit.Visual.Model.gameObject, enemy ? Vfx.InkOutline.EnemyInk : Vfx.InkOutline.Ink);
            Vfx.UnitFx.Attach(unit.gameObject, id == "werewolf" ? Vfx.UnitFx.Kind.Beast : id == "hunter" ? Vfx.UnitFx.Kind.Human : Vfx.UnitFx.Kind.Vampire);
            return unit;
        }

        /// <summary>Gives the unit its bar (gear + class skills) and passives, then wears the gear.</summary>
        public static void ApplyLoadout(CombatUnit unit, CharacterSave character, IDictionary<string, ItemInstance> equipped)
        {
            PassiveStats passives;
            Ability[] bar = SkillFactory.BuildBar(SkillCatalog.Instance, GearCatalog.Instance, character, equipped, out passives);
            unit.SetAbilities(bar);
            unit.SetPassives(passives);
            unit.ApplyGear(GearCatalog.Instance, equipped);
        }

        static CharacterArt ArtOf(string classId)
        {
            return classId == "werewolf" ? ArtLibrary.Werewolf : classId == "hunter" ? ArtLibrary.MonsterHunter : ArtLibrary.Vampire;
        }

        /// <summary>3D model of the class if assigned, else the primitive placeholder.</summary>
        static void BuildLook(UnitVisual v, string classId, float size)
        {
            CharacterArt art = ArtOf(classId);
            if (art != null && art.IsValid)
            {
                v.UseModel(art, size);
                return;
            }

            if (classId == "werewolf") BuildWerewolfPlaceholder(v);
            else if (classId == "hunter") BuildHunterPlaceholder(v);
            else BuildVampirePlaceholder(v);
            v.Model.localScale = Vector3.one * size;   // the model only (the unit root also carries the collider)
        }

        /// <summary>Look only (lobby stage and portraits) of any class.</summary>
        public static GameObject CreateDisplay(Transform parent, string classId, float sizeMultiplier = 1f)
        {
            if (classId == "vampire" || string.IsNullOrEmpty(classId)) return CreateDisplayVampire(parent, sizeMultiplier);
            GameObject go = new GameObject("Vitrine " + classId);   // built unparented: see CreateDisplayVampire
            UnitVisual v = go.AddComponent<UnitVisual>();
            BuildLook(v, classId, sizeMultiplier);
            Vfx.InkOutline.Attach(v.Model.gameObject, Vfx.InkOutline.Ink);
            go.transform.SetParent(parent, false);
            return go;
        }

        /// <summary>Bounty hunter placeholder: long coat, wide-brimmed hat, silver bandolier.</summary>
        static void BuildHunterPlaceholder(UnitVisual v)
        {
            Color coat = new Color(0.32f, 0.24f, 0.16f);
            Color hat = new Color(0.12f, 0.1f, 0.08f);
            Color skin = new Color(0.86f, 0.7f, 0.58f);
            Color silver = new Color(0.8f, 0.82f, 0.85f);

            v.Part(PrimitiveType.Capsule, "Body", new Vector3(0f, 1f, 0f), new Vector3(0.75f, 0.95f, 0.7f), coat);
            v.Part(PrimitiveType.Cube, "CoatTail", new Vector3(0f, 0.55f, -0.2f), new Vector3(0.8f, 0.8f, 0.25f), coat);
            v.Part(PrimitiveType.Sphere, "Head", new Vector3(0f, 1.95f, 0f), new Vector3(0.45f, 0.5f, 0.45f), skin);
            v.Part(PrimitiveType.Cylinder, "HatBrim", new Vector3(0f, 2.15f, 0f), new Vector3(0.95f, 0.03f, 0.95f), hat);
            v.Part(PrimitiveType.Cylinder, "HatTop", new Vector3(0f, 2.3f, 0f), new Vector3(0.45f, 0.15f, 0.45f), hat);
            v.Part(PrimitiveType.Cube, "Bandolier", new Vector3(0f, 1.25f, 0.3f), new Vector3(0.12f, 0.9f, 0.06f), silver);
        }

        /// <summary>
        /// Vampire look only (no combat, no collision): used by the lobby stage and portraits.
        /// The rigged model still breathes/idles through ProceduralRig.
        /// </summary>
        public static GameObject CreateDisplayVampire(Transform parent, float sizeMultiplier = 1f)
        {
            // Built unparented (always active, so UnitVisual.Awake runs even if the parent is inactive),
            // then attached: FitToHeight works from the origin, so the local offsets stay right.
            GameObject go = new GameObject("Vampiro (vitrine)");
            UnitVisual v = go.AddComponent<UnitVisual>();

            if (ArtLibrary.Vampire != null && ArtLibrary.Vampire.IsValid)
            {
                v.UseModel(ArtLibrary.Vampire, sizeMultiplier);
            }
            else
            {
                BuildVampirePlaceholder(v);
                go.transform.localScale = Vector3.one * sizeMultiplier;
            }

            Vfx.InkOutline.Attach(v.Model.gameObject, Vfx.InkOutline.Ink);
            go.transform.SetParent(parent, false);
            return go;
        }

        /// <summary>Primitive look used when no 3D model is assigned.</summary>
        static void BuildVampirePlaceholder(UnitVisual v)
        {
            Color coat = new Color(0.12f, 0.08f, 0.14f);
            Color cape = new Color(0.55f, 0.02f, 0.08f);
            Color skin = new Color(0.86f, 0.84f, 0.82f);
            Color hair = new Color(0.05f, 0.05f, 0.07f);
            Color eyes = new Color(1f, 0.1f, 0.1f);

            v.Part(PrimitiveType.Capsule, "Body", new Vector3(0f, 1f, 0f), new Vector3(0.8f, 0.95f, 0.8f), coat);
            v.Part(PrimitiveType.Cube, "Cape", new Vector3(0f, 1.05f, -0.38f), new Vector3(1.05f, 1.6f, 0.08f), cape);
            v.Part(PrimitiveType.Cube, "Collar", new Vector3(0f, 1.75f, -0.2f), new Vector3(0.95f, 0.35f, 0.12f), cape);
            v.Part(PrimitiveType.Sphere, "Head", new Vector3(0f, 2.0f, 0f), new Vector3(0.5f, 0.55f, 0.5f), skin);
            v.Part(PrimitiveType.Sphere, "Hair", new Vector3(0f, 2.14f, -0.05f), new Vector3(0.52f, 0.36f, 0.52f), hair);
            v.Part(PrimitiveType.Sphere, "EyeL", new Vector3(-0.1f, 2.03f, 0.22f), Vector3.one * 0.08f, eyes, true);
            v.Part(PrimitiveType.Sphere, "EyeR", new Vector3(0.1f, 2.03f, 0.22f), Vector3.one * 0.08f, eyes, true);
        }

        /// <summary>Primitive look used when no 3D model is assigned.</summary>
        static void BuildWerewolfPlaceholder(UnitVisual v)
        {
            Color fur = new Color(0.33f, 0.24f, 0.17f);
            Color darkFur = new Color(0.2f, 0.14f, 0.1f);
            Color claw = new Color(0.9f, 0.88f, 0.8f);
            Color eyes = new Color(1f, 0.85f, 0.1f);

            v.Part(PrimitiveType.Capsule, "Body", new Vector3(0f, 1.15f, 0f), new Vector3(1.2f, 1.1f, 1.05f), fur);
            v.Part(PrimitiveType.Cube, "Head", new Vector3(0f, 2.3f, 0.2f), new Vector3(0.7f, 0.6f, 0.7f), darkFur);
            v.Part(PrimitiveType.Cube, "Snout", new Vector3(0f, 2.2f, 0.65f), new Vector3(0.35f, 0.3f, 0.45f), darkFur);
            v.Part(PrimitiveType.Cube, "EarL", new Vector3(-0.22f, 2.72f, 0.1f), new Vector3(0.15f, 0.3f, 0.1f), darkFur);
            v.Part(PrimitiveType.Cube, "EarR", new Vector3(0.22f, 2.72f, 0.1f), new Vector3(0.15f, 0.3f, 0.1f), darkFur);
            v.Part(PrimitiveType.Cube, "ArmL", new Vector3(-0.75f, 1.3f, 0.25f), new Vector3(0.3f, 1.1f, 0.3f), fur);
            v.Part(PrimitiveType.Cube, "ArmR", new Vector3(0.75f, 1.3f, 0.25f), new Vector3(0.3f, 1.1f, 0.3f), fur);
            v.Part(PrimitiveType.Cube, "ClawL", new Vector3(-0.75f, 0.7f, 0.4f), new Vector3(0.32f, 0.15f, 0.35f), claw);
            v.Part(PrimitiveType.Cube, "ClawR", new Vector3(0.75f, 0.7f, 0.4f), new Vector3(0.32f, 0.15f, 0.35f), claw);
            v.Part(PrimitiveType.Sphere, "EyeL", new Vector3(-0.15f, 2.4f, 0.56f), Vector3.one * 0.1f, eyes, true);
            v.Part(PrimitiveType.Sphere, "EyeR", new Vector3(0.15f, 2.4f, 0.56f), Vector3.one * 0.1f, eyes, true);
        }

        static CombatUnit CreateUnit(Transform parent, string displayName, Vector3 position, Team team, float maxHealth,
            float moveSpeed, float radius, float height, Color teamColor)
        {
            radius *= SizeMultiplier;
            height *= SizeMultiplier;

            GameObject go = new GameObject(displayName);
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(position.x, 0f, position.z);

            CharacterController controller = go.AddComponent<CharacterController>();
            controller.radius = radius;
            controller.height = height;
            controller.center = new Vector3(0f, height * 0.5f, 0f);
            controller.stepOffset = 0.2f;
            controller.skinWidth = 0.05f;

            go.AddComponent<StatusEffects>();
            go.AddComponent<Health>().Init(maxHealth, team);
            go.AddComponent<Energy>();
            go.AddComponent<Inventory>();
            go.AddComponent<Equipment>();
            go.AddComponent<TelegraphIndicator>();
            UnitVisual visual = go.AddComponent<UnitVisual>();
            visual.AddTeamRing(teamColor, radius + 0.3f);

            CombatUnit unit = go.AddComponent<CombatUnit>();
            unit.Init(displayName, moveSpeed);
            return unit;
        }
    }
}
