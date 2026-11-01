using System.Collections.Generic;
using UnityEngine;

namespace VampWolfs
{
    /// <summary>
    /// Look of a character or creature, under a "Model" child: either a 3D model (UseModel) or
    /// placeholder primitives (Part). Handles hit flash, status tint, death pose and a small
    /// procedural motion (bob/lean while running, lunge on attack) so static models don't look
    /// like statues. When rigged models arrive, an Animator replaces the procedural motion;
    /// gameplay code only calls SetMotion / PlayAttack / Flash / PlayDeath.
    /// </summary>
    public class UnitVisual : MonoBehaviour
    {
        public Transform Model { get; private set; }

        readonly List<Renderer> renderers = new List<Renderer>();
        MaterialPropertyBlock block;
        StatusEffects status;
        GameObject teamRing;

        // Procedural motion: applied to "pose", the only child of Model.
        Transform pose;
        Vector3 localVelocity;
        float referenceSpeed = 6f;
        float attackTime = -10f;
        bool dead;

        float flashUntil;
        bool tinted;

        const float AttackDuration = 0.18f;

        static readonly Color FlashColor = new Color(1f, 1f, 1f);
        static readonly Color StunTint = new Color(1f, 0.9f, 0.35f);
        static readonly Color CounterTint = new Color(1f, 0.75f, 0.2f);
        static readonly Color InvulnerableTint = new Color(0.75f, 0.75f, 1f);
        static readonly Color ReductionTint = new Color(0.55f, 0.65f, 0.85f);

        void Awake()
        {
            GameObject model = new GameObject("Model");
            model.transform.SetParent(transform, false);
            Model = model.transform;

            GameObject poseGo = new GameObject("Pose");
            poseGo.transform.SetParent(Model, false);
            pose = poseGo.transform;

            block = new MaterialPropertyBlock();
        }

        void Start()
        {
            status = GetComponent<StatusEffects>();
        }

        // ---------------------------------------------------------------- building

        /// <summary>Adds a primitive body part. Position is local to the character.</summary>
        public GameObject Part(PrimitiveType type, string partName, Vector3 localPosition, Vector3 localScale, Color color, bool unlit = false)
        {
            GameObject part = Shapes.Create(type, pose, partName, localPosition, localScale, color, unlit);
            if (!unlit)
            {
                renderers.Add(part.GetComponent<Renderer>());
            }

            return part;
        }

        /// <summary>
        /// Uses a 3D model (e.g. an imported FBX): scaled to art.Height, feet on the ground,
        /// centered on the character, with a material built from the art textures.
        /// </summary>
        public void UseModel(CharacterArt art)
        {
            GameObject instance = Instantiate(art.Model, pose, false);
            instance.name = art.Model.name;
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.Euler(0f, art.YawOffset, 0f);

            // Models only draw: no physics, no animation of their own.
            foreach (Collider col in instance.GetComponentsInChildren<Collider>())
            {
                col.enabled = false;
                Destroy(col);
            }

            foreach (Animator animator in instance.GetComponentsInChildren<Animator>())
            {
                animator.enabled = false;
            }

            Material material = (art.BaseColor != null || art.Normal != null) ? MaterialLibrary.Textured(art.BaseColor, art.Normal) : null;
            Renderer[] modelRenderers = instance.GetComponentsInChildren<Renderer>();

            for (int i = 0; i < modelRenderers.Length; i++)
            {
                if (material != null)
                {
                    Material[] slots = new Material[Mathf.Max(1, modelRenderers[i].sharedMaterials.Length)];
                    for (int s = 0; s < slots.Length; s++) slots[s] = material;
                    modelRenderers[i].sharedMaterials = slots;
                }

                renderers.Add(modelRenderers[i]);
            }

            FitToHeight(instance.transform, modelRenderers, art.Height);
        }

        void FitToHeight(Transform instance, Renderer[] modelRenderers, float height)
        {
            if (modelRenderers.Length == 0)
            {
                return;
            }

            Bounds bounds = CombinedBounds(modelRenderers);
            if (bounds.size.y > 0.0001f)
            {
                float factor = height / bounds.size.y;
                instance.localScale = instance.localScale * factor;
            }

            // Recompute after scaling, then move so the feet touch y=0 and the model is centered.
            bounds = CombinedBounds(modelRenderers);
            Vector3 offset = new Vector3(transform.position.x - bounds.center.x, transform.position.y - bounds.min.y, transform.position.z - bounds.center.z);
            instance.position += offset;
        }

        static Bounds CombinedBounds(Renderer[] list)
        {
            Bounds bounds = list[0].bounds;
            for (int i = 1; i < list.Length; i++)
            {
                bounds.Encapsulate(list[i].bounds);
            }

            return bounds;
        }

        /// <summary>Colored disc under the character so teams read instantly (Battlerite does the same).</summary>
        public void AddTeamRing(Color color, float radius)
        {
            teamRing = Shapes.Create(PrimitiveType.Cylinder, transform, "TeamRing", new Vector3(0f, 0.02f, 0f), new Vector3(radius * 2f, 0.01f, radius * 2f), color, true);
        }

        // ---------------------------------------------------------------- called by gameplay

        /// <summary>World velocity of the character this frame (drives bob and lean).</summary>
        public void SetMotion(Vector3 worldVelocity, float maxSpeed)
        {
            localVelocity = transform.InverseTransformDirection(worldVelocity);
            referenceSpeed = Mathf.Max(0.1f, maxSpeed);
        }

        /// <summary>Quick forward lunge when an ability is released.</summary>
        public void PlayAttack()
        {
            attackTime = Time.time;
        }

        public void Flash()
        {
            flashUntil = Time.time + 0.08f;
        }

        public void PlayDeath()
        {
            dead = true;
            pose.localPosition = Vector3.zero;
            pose.localRotation = Quaternion.identity;
            Model.localRotation = Quaternion.Euler(0f, 0f, 90f);
            Model.localPosition = new Vector3(0.6f, 0.2f, 0f);
            if (teamRing != null) teamRing.SetActive(false);
            SetColor(false, Color.white);
        }

        // ---------------------------------------------------------------- per frame

        void Update()
        {
            Color color;
            bool wantTint = DesiredTint(out color);

            if (wantTint || tinted)
            {
                SetColor(wantTint, color);
            }
        }

        void LateUpdate()
        {
            if (dead)
            {
                return;
            }

            float speed01 = Mathf.Clamp01(new Vector3(localVelocity.x, 0f, localVelocity.z).magnitude / referenceSpeed);

            // Bob up and down while running.
            float bob = Mathf.Abs(Mathf.Sin(Time.time * 11f)) * 0.08f * speed01;

            // Lean into the movement direction (forward/back and sideways).
            float leanForward = Mathf.Clamp(localVelocity.z / referenceSpeed, -1f, 1f) * 8f;
            float leanSide = Mathf.Clamp(-localVelocity.x / referenceSpeed, -1f, 1f) * 6f;

            // Attack lunge: quick push forward and back.
            float attack01 = Mathf.Clamp01((Time.time - attackTime) / AttackDuration);
            float lunge = attack01 < 1f ? Mathf.Sin(attack01 * Mathf.PI) : 0f;

            pose.localPosition = new Vector3(0f, bob, lunge * 0.25f);
            pose.localRotation = Quaternion.Euler(leanForward + lunge * 10f, 0f, leanSide);
        }

        bool DesiredTint(out Color color)
        {
            color = Color.white;

            if (Time.time < flashUntil)
            {
                color = FlashColor;
                return true;
            }

            if (status == null)
            {
                return false;
            }

            if (status.IsStunned) { color = StunTint; return true; }
            if (status.IsCountering) { color = CounterTint; return true; }
            if (status.IsInvulnerable) { color = InvulnerableTint; return true; }
            if (status.HasDamageReduction) { color = ReductionTint; return true; }
            return false;
        }

        void SetColor(bool on, Color color)
        {
            block.Clear();
            if (on)
            {
                block.SetColor("_BaseColor", color); // URP (multiplies the texture)
                block.SetColor("_Color", color);     // Built-in
            }

            for (int i = 0; i < renderers.Count; i++)
            {
                if (renderers[i] != null)
                {
                    renderers[i].SetPropertyBlock(block);
                }
            }

            tinted = on;
        }
    }
}
