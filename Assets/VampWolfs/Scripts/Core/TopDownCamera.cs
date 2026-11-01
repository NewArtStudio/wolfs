using UnityEngine;

namespace VampWolfs
{
    /// <summary>
    /// Battlerite-style camera: fixed angle from above, follows the player and leans
    /// slightly toward where the mouse is aiming.
    /// </summary>
    public class TopDownCamera : MonoBehaviour
    {
        public Transform Target;
        public Vector3 Offset = new Vector3(0f, 13f, -8.5f);

        [Tooltip("Multiplies Offset (1 = Offset as is). Mouse wheel changes it between ZoomMin and ZoomMax.")]
        public float Zoom = 1f;
        public float ZoomMin = 0.85f;
        public float ZoomMax = 1.9f;
        float zoomTarget = 1f;
        Vector3 basePosition;
        float fogStart, fogEnd;   // fog distances set by the farm, pushed back when the camera zooms out
        public float FollowSharpness = 10f;

        [Tooltip("How much the camera leans toward the aim point (0 = none).")]
        public float AimLookAhead = 0.25f;
        public float MaxLookAhead = 3.5f;

        CombatUnit targetUnit;

        public static Camera Create(Transform parent, Transform target, MatchSettings settings)
        {
            GameObject go = new GameObject("Main Camera");
            go.tag = "MainCamera";
            go.transform.SetParent(parent, false);

            Camera cam = go.AddComponent<Camera>();
            cam.fieldOfView = settings.CameraFieldOfView;
            cam.nearClipPlane = 0.3f;
            cam.farClipPlane = 200f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.08f, 0.06f, 0.12f);

            go.AddComponent<AudioListener>();

            TopDownCamera follow = go.AddComponent<TopDownCamera>();
            follow.Target = target;
            follow.Offset = new Vector3(0f, settings.CameraHeight, -settings.CameraBack);
            follow.ZoomMin = Mathf.Min(settings.CameraZoomMin, settings.CameraZoomMax);
            follow.ZoomMax = Mathf.Max(settings.CameraZoomMin, settings.CameraZoomMax);
            follow.Zoom = Mathf.Clamp(settings.CameraZoom, follow.ZoomMin, follow.ZoomMax);
            follow.zoomTarget = follow.Zoom;
            follow.fogStart = RenderSettings.fogStartDistance;
            follow.fogEnd = RenderSettings.fogEndDistance;
            follow.targetUnit = target.GetComponent<CombatUnit>();
            follow.SnapToTarget();

            return cam;
        }

        public void SnapToTarget()
        {
            if (Target == null)
            {
                return;
            }

            basePosition = Target.position + Offset * Zoom;
            transform.position = basePosition;
            transform.rotation = Quaternion.LookRotation(-Offset.normalized, Vector3.up);
        }

        void LateUpdate()
        {
            if (Target == null)
            {
                return;
            }

            Vector3 focus = Target.position;

            if (targetUnit != null && !targetUnit.IsDead)
            {
                Vector3 look = targetUnit.AimPoint - focus;
                look.y = 0f;
                focus += Vector3.ClampMagnitude(look * AimLookAhead, MaxLookAhead);
            }

            // Mouse wheel: forward = closer, back = farther (smoothed).
            float scroll = InputReader.ScrollDelta();
            if (Mathf.Abs(scroll) > 0.01f)
            {
                zoomTarget = Mathf.Clamp(zoomTarget - scroll * 0.08f, ZoomMin, ZoomMax);
            }

            Zoom = Mathf.Lerp(Zoom, zoomTarget, 1f - Mathf.Exp(-12f * Time.deltaTime));
            if (fogEnd > 0f)
            {
                RenderSettings.fogStartDistance = fogStart * Zoom;   // the mist stays at the edges of the view
                RenderSettings.fogEndDistance = fogEnd * Zoom;
            }

            Vector3 desired = focus + Offset * Zoom;
            float t = 1f - Mathf.Exp(-FollowSharpness * Time.deltaTime);
            basePosition = Vector3.Lerp(basePosition, desired, t);
            transform.position = basePosition + Vfx.VfxHub.ShakeOffset();   // screen shake from big hits
            transform.rotation = Quaternion.LookRotation(-Offset.normalized, Vector3.up);
        }
    }
}
