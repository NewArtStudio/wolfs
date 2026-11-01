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

            transform.position = Target.position + Offset;
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

            Vector3 desired = focus + Offset;
            float t = 1f - Mathf.Exp(-FollowSharpness * Time.deltaTime);
            transform.position = Vector3.Lerp(transform.position, desired, t);
            transform.rotation = Quaternion.LookRotation(-Offset.normalized, Vector3.up);
        }
    }
}
