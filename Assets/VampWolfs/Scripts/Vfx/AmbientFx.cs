using UnityEngine;

namespace VampWolfs.Vfx
{
    /// <summary>
    /// Vida no cenário da Fazenda à noite: vaga-lumes perto do jogador, névoa roxa rente ao chão ("as Brumas")
    /// e o portal de extração soltando faíscas violeta (mais fortes enquanto o jogador extrai).
    /// Só enfeite; criado pelo VfxHub junto com os efeitos da partida.
    /// </summary>
    public sealed class AmbientFx : MonoBehaviour
    {
        static readonly Color Firefly = new Color(1.4f, 1.6f, 0.5f);
        static readonly Color GroundMist = new Color(0.32f, 0.24f, 0.45f, 0.35f);
        static readonly Color PortalSpark = new Color(1.3f, 0.6f, 2.2f);

        float fireflyTimer, mistTimer;
        ExtractionPoint portal;

        void Update()
        {
            var v = VfxHub.Fx; if (v == null) return;
            CombatUnit player = Player();
            Vector3 c = player != null ? player.Position : Vector3.zero;

            fireflyTimer -= Time.deltaTime;
            if (fireflyTimer <= 0f)
            {
                fireflyTimer = 0.18f;
                var p = c + new Vector3(Random.Range(-14f, 14f), Random.Range(0.6f, 2.6f), Random.Range(-10f, 12f));
                v.Glows.Emit(p, Random.insideUnitSphere * 0.25f, Random.Range(0.1f, 0.16f), Random.Range(2.5f, 4.5f), Firefly);
            }

            mistTimer -= Time.deltaTime;
            if (mistTimer <= 0f)
            {
                mistTimer = 0.3f;
                var p = c + new Vector3(Random.Range(-16f, 16f), 0.25f, Random.Range(-12f, 14f));
                v.Smoke.Emit(p, new Vector3(Random.Range(-0.3f, 0.3f), 0.02f, Random.Range(-0.3f, 0.3f)), Random.Range(2.5f, 4f), Random.Range(4f, 6f), GroundMist, -1f, 8f);
            }

            if (portal == null) portal = FindFirstObjectByType<ExtractionPoint>();
            if (portal != null && Random.value < (portal.IsChanneling ? 0.9f : 0.3f))
            {
                float a = Random.Range(0f, Mathf.PI * 2f), r = Random.Range(0.4f, 1.6f);
                var p = portal.transform.position + new Vector3(Mathf.Cos(a) * r, 0.1f, Mathf.Sin(a) * r);
                v.Embers.Emit(p, Vector3.up * Random.Range(1.5f, portal.IsChanneling ? 5f : 2.5f), Random.Range(0.06f, 0.1f), Random.Range(0.7f, 1.3f), PortalSpark);
            }
        }

        static CombatUnit Player()
        {
            for (int i = 0; i < CombatUnit.All.Count; i++)
            {
                var u = CombatUnit.All[i];
                if (u != null && u.IsPlayer) return u;
            }
            return null;
        }
    }
}
