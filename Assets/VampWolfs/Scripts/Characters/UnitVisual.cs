using System.Collections.Generic;
using UnityEngine;

namespace VampWolfs
{
    /// <summary>
    /// Placeholder look of a character or creature: a set of primitives under a "Model" child.
    /// Handles hit flash, status tint and death pose. Replace with an Animator later
    /// (gameplay code only calls Flash / PlayDeath).
    /// </summary>
    public class UnitVisual : MonoBehaviour
    {
        public Transform Model { get; private set; }

        readonly List<Renderer> renderers = new List<Renderer>();
        MaterialPropertyBlock block;
        StatusEffects status;
        GameObject teamRing;

        float flashUntil;
        bool tinted;

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
            block = new MaterialPropertyBlock();
        }

        void Start()
        {
            status = GetComponent<StatusEffects>();
        }

        /// <summary>Adds a primitive body part under the model. Position is local to the character.</summary>
        public GameObject Part(PrimitiveType type, string partName, Vector3 localPosition, Vector3 localScale, Color color, bool unlit = false)
        {
            GameObject part = Shapes.Create(type, Model, partName, localPosition, localScale, color, unlit);
            if (!unlit)
            {
                renderers.Add(part.GetComponent<Renderer>());
            }

            return part;
        }

        /// <summary>Colored disc under the character so teams read instantly (Battlerite does the same).</summary>
        public void AddTeamRing(Color color, float radius)
        {
            teamRing = Shapes.Create(PrimitiveType.Cylinder, transform, "TeamRing", new Vector3(0f, 0.02f, 0f), new Vector3(radius * 2f, 0.01f, radius * 2f), color, true);
        }

        public void Flash()
        {
            flashUntil = Time.time + 0.08f;
        }

        public void PlayDeath()
        {
            Model.localRotation = Quaternion.Euler(0f, 0f, 90f);
            Model.localPosition = new Vector3(0.6f, 0.2f, 0f);
            if (teamRing != null) teamRing.SetActive(false);
            SetColor(false, Color.white);
        }

        void Update()
        {
            Color color;
            bool wantTint = DesiredTint(out color);

            if (wantTint || tinted)
            {
                SetColor(wantTint, color);
            }
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
                block.SetColor("_BaseColor", color); // URP
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
