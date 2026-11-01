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

        public static CombatUnit CreateVampire(Transform parent, Vector3 position)
        {
            CombatUnit unit = CreateUnit(parent, "Vampiro", position, Team.Vampires, 160f, 6.2f, 0.5f, 2.2f, VampireTeamColor);
            UnitVisual v = unit.Visual;

            if (ArtLibrary.Vampire != null && ArtLibrary.Vampire.IsValid)
            {
                v.UseModel(ArtLibrary.Vampire);
            }
            else
            {
                BuildVampirePlaceholder(v);
            }

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
                v.UseModel(ArtLibrary.Werewolf);
            }
            else
            {
                BuildWerewolfPlaceholder(v);
            }

            unit.SetAbilities(WerewolfKit.Create());
            unit.Equipment.Set("Garras de Prata Negra T4", "Pele Ancestral T4", 840);
            return unit;
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
