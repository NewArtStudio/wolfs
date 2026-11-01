using UnityEngine;

namespace VampWolfs
{
    /// <summary>
    /// Exit portal (like leaving the Mists). Stand inside for a few seconds to extract and keep
    /// your bag. Taking damage interrupts the channel, so you can't simply run away mid-fight.
    /// </summary>
    public class ExtractionPoint : MonoBehaviour
    {
        static readonly Color PortalColor = new Color(0.6f, 0.3f, 1f);

        MatchSettings settings;
        CombatUnit subscribedPlayer;
        float elapsed;
        Transform pillar;
        Light glow;

        public bool IsChanneling { get; private set; }

        public float Progress01
        {
            get { return settings == null ? 0f : Mathf.Clamp01(elapsed / settings.ExtractionChannelTime); }
        }

        public float RemainingTime
        {
            get { return settings == null ? 0f : Mathf.Max(0f, settings.ExtractionChannelTime - elapsed); }
        }

        public static ExtractionPoint Create(Transform parent, Vector3 position, MatchSettings settings)
        {
            GameObject go = new GameObject("ExtractionPortal");
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(position.x, 0f, position.z);

            float r = settings.ExtractionRadius;
            Shapes.Create(PrimitiveType.Cylinder, go.transform, "Ring", new Vector3(0f, 0.03f, 0f), new Vector3(r * 2f, 0.01f, r * 2f), PortalColor, true);
            Shapes.Create(PrimitiveType.Cylinder, go.transform, "Inner", new Vector3(0f, 0.04f, 0f), new Vector3(r * 1.7f, 0.01f, r * 1.7f), new Color(0.12f, 0.05f, 0.2f), true);
            GameObject pillarGo = Shapes.Create(PrimitiveType.Cylinder, go.transform, "Pillar", new Vector3(0f, 3f, 0f), new Vector3(0.25f, 3f, 0.25f), PortalColor, true);

            GameObject lightGo = new GameObject("PortalLight");
            lightGo.transform.SetParent(go.transform, false);
            lightGo.transform.localPosition = new Vector3(0f, 2f, 0f);
            Light light = lightGo.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = PortalColor;
            light.range = 10f;
            light.intensity = 3f;

            ExtractionPoint point = go.AddComponent<ExtractionPoint>();
            point.settings = settings;
            point.pillar = pillarGo.transform;
            point.glow = light;
            return point;
        }

        void Update()
        {
            Animate();

            MatchManager match = MatchManager.Instance;
            if (match == null || match.IsOver)
            {
                IsChanneling = false;
                return;
            }

            CombatUnit player = match.Player;
            if (player == null || player.IsDead)
            {
                IsChanneling = false;
                return;
            }

            Subscribe(player);

            Vector3 offset = player.Position - transform.position;
            offset.y = 0f;
            bool inside = offset.magnitude <= settings.ExtractionRadius;

            if (!inside)
            {
                IsChanneling = false;
                elapsed = 0f;
                return;
            }

            IsChanneling = true;
            elapsed += Time.deltaTime;

            if (elapsed >= settings.ExtractionChannelTime)
            {
                IsChanneling = false;
                match.OnPlayerExtracted();
            }
        }

        void Animate()
        {
            float pulse = 0.5f + 0.5f * Mathf.Sin(Time.time * 3f);
            float boost = IsChanneling ? 1f + Progress01 * 2f : 1f;
            glow.intensity = (2.5f + pulse * 1.5f) * boost;
            pillar.localScale = new Vector3(0.25f + pulse * 0.08f, 3f, 0.25f + pulse * 0.08f);
        }

        void Subscribe(CombatUnit player)
        {
            if (subscribedPlayer == player)
            {
                return;
            }

            Unsubscribe();
            subscribedPlayer = player;
            subscribedPlayer.Health.Damaged += OnPlayerDamaged;
        }

        void Unsubscribe()
        {
            if (subscribedPlayer != null && subscribedPlayer.Health != null)
            {
                subscribedPlayer.Health.Damaged -= OnPlayerDamaged;
            }

            subscribedPlayer = null;
        }

        void OnPlayerDamaged(float amount, CombatUnit source)
        {
            if (!IsChanneling || amount <= 0f)
            {
                return;
            }

            elapsed = 0f;
            FloatingText.Spawn(subscribedPlayer.Position + Vector3.up * 3f, "Extração interrompida!", new Color(0.8f, 0.6f, 1f), 1.2f, 1.1f);
        }

        void OnDestroy()
        {
            Unsubscribe();
        }
    }
}
