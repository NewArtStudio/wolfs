using System.Collections.Generic;
using UnityEngine;

namespace VampWolfs
{
    /// <summary>What kind of motion an ability release plays.</summary>
    public enum AttackMotion
    {
        None,     // dashes: body lean only
        Swing,    // melee: right arm slash
        Throw,    // projectile: right arm thrust forward
        BothArms  // areas, counters, buffs: both arms out
    }

    /// <summary>
    /// Animates a rigged humanoid without animation clips, by rotating its bones every frame:
    /// walk/run cycle in the direction of movement, idle breathing, cast wind-up, attack swing
    /// and stun slump. Bones are found by Unity Humanoid names (Hips, LeftUpperArm, ...), which the
    /// auto-rigged FBX files use. Rotations are applied around WORLD axes, so it works no matter
    /// how each bone's local axes were exported.
    ///
    /// Replace with an Animator + real clips (e.g. Mixamo) later; gameplay code doesn't change.
    /// </summary>
    public class ProceduralRig
    {
        readonly Transform root;
        readonly Dictionary<string, Transform> bones = new Dictionary<string, Transform>();
        readonly List<Transform> ordered = new List<Transform>();
        readonly List<Quaternion> bindRotations = new List<Quaternion>();

        float phase;

        static readonly string[] BoneNames =
        {
            "Hips", "Spine", "Chest", "Neck", "Head",
            "LeftShoulder", "LeftUpperArm", "LeftLowerArm", "LeftHand",
            "RightShoulder", "RightUpperArm", "RightLowerArm", "RightHand",
            "LeftUpperLeg", "LeftLowerLeg", "LeftFoot",
            "RightUpperLeg", "RightLowerLeg", "RightFoot"
        };

        ProceduralRig(Transform characterRoot)
        {
            root = characterRoot;
        }

        /// <summary>Returns null when the model has no humanoid bones (static mesh).</summary>
        public static ProceduralRig TryCreate(Transform characterRoot, Transform modelRoot)
        {
            ProceduralRig rig = new ProceduralRig(characterRoot);
            Dictionary<string, Transform> found = new Dictionary<string, Transform>();

            foreach (Transform t in modelRoot.GetComponentsInChildren<Transform>())
            {
                if (!found.ContainsKey(t.name))
                {
                    found[t.name] = t;
                }
            }

            // BoneNames is in hierarchy order (parents first), which Apply relies on.
            for (int i = 0; i < BoneNames.Length; i++)
            {
                Transform bone;
                if (found.TryGetValue(BoneNames[i], out bone))
                {
                    rig.bones[BoneNames[i]] = bone;
                    rig.ordered.Add(bone);
                    rig.bindRotations.Add(bone.localRotation);
                }
            }

            bool usable = rig.bones.ContainsKey("Hips") && rig.bones.ContainsKey("LeftUpperLeg") && rig.bones.ContainsKey("RightUpperArm");
            return usable ? rig : null;
        }

        /// <summary>Everything the rig needs to know about the character this frame.</summary>
        public struct State
        {
            public Vector3 WorldVelocity;
            public float MaxSpeed;
            public float CastProgress;      // 0..1 while winding up an ability, else 0
            public AttackMotion CastMotion; // motion of the ability being cast
            public float Attack01;          // 0..1 during the release swing, 1 when finished
            public AttackMotion AttackMotion;
            public bool Stunned;
        }

        public void Apply(State s, float dt)
        {
            // 1. back to the bind pose
            for (int i = 0; i < ordered.Count; i++)
            {
                ordered[i].localRotation = bindRotations[i];
            }

            Vector3 up = Vector3.up;
            Vector3 forward = root.forward;
            Vector3 right = root.right;

            Vector3 flatVelocity = new Vector3(s.WorldVelocity.x, 0f, s.WorldVelocity.z);
            float speed = flatVelocity.magnitude;
            float speed01 = Mathf.Clamp01(speed / Mathf.Max(0.1f, s.MaxSpeed));

            // Swing axis: perpendicular to the movement direction (so strafing moves legs sideways).
            // In Unity, a positive angle around Cross(up, dir) moves a hanging limb BACKWARD along dir.
            Vector3 moveDir = speed > 0.05f ? flatVelocity / speed : forward;
            Vector3 swingAxis = Vector3.Cross(up, moveDir);
            Vector3 bendAxis = right; // knees/elbows bend relative to the body facing

            // 2. locomotion cycle
            phase += dt * Mathf.Lerp(0f, 10f, speed01);
            float swing = Mathf.Sin(phase);
            float legAmp = 38f * speed01;
            float armAmp = 28f * speed01;

            Rotate("LeftUpperLeg", swingAxis, -swing * legAmp);
            Rotate("RightUpperLeg", swingAxis, swing * legAmp);
            Rotate("LeftLowerLeg", bendAxis, Mathf.Max(0f, Mathf.Sin(phase - 1.2f)) * 55f * speed01);
            Rotate("RightLowerLeg", bendAxis, Mathf.Max(0f, -Mathf.Sin(phase - 1.2f)) * 55f * speed01);

            Rotate("LeftUpperArm", swingAxis, swing * armAmp);
            Rotate("RightUpperArm", swingAxis, -swing * armAmp);
            Rotate("LeftLowerArm", bendAxis, -12f * speed01);
            Rotate("RightLowerArm", bendAxis, -12f * speed01);
            Rotate("Spine", up, swing * 6f * speed01);

            // 3. idle breathing
            float breathe = Mathf.Sin(Time.time * 2.2f) * (1f - speed01);
            Rotate("Chest", right, breathe * 2f);
            Rotate("Head", right, -breathe * 1.5f);

            // 4. cast wind-up: raise the arm(s) as the cast progresses
            if (s.CastProgress > 0f)
            {
                float w = Mathf.SmoothStep(0f, 1f, s.CastProgress);
                ApplyArmPose(s.CastMotion, w, true, right, up);
            }

            // 5. release: fast swing
            if (s.Attack01 < 1f)
            {
                float a = Mathf.Sin(s.Attack01 * Mathf.PI);
                ApplyArmPose(s.AttackMotion, a, false, right, up);
            }

            // 6. stunned: slump forward, head wobbles
            if (s.Stunned)
            {
                Rotate("Chest", right, 18f);
                Rotate("Head", right, 15f);
                Rotate("Head", up, Mathf.Sin(Time.time * 6f) * 12f);
                Rotate("LeftUpperArm", right, 10f);
                Rotate("RightUpperArm", right, 10f);
            }
        }

        void ApplyArmPose(AttackMotion motion, float weight, bool windUp, Vector3 right, Vector3 up)
        {
            switch (motion)
            {
                case AttackMotion.Swing:
                    if (windUp)
                    {
                        // arm cocked back and up, torso twisted away
                        Rotate("RightUpperArm", right, 70f * weight);
                        Rotate("RightUpperArm", up, -40f * weight);
                        Rotate("Spine", up, -20f * weight);
                    }
                    else
                    {
                        // slash across the front
                        Rotate("RightUpperArm", right, -95f * weight);
                        Rotate("RightUpperArm", up, 35f * weight);
                        Rotate("RightLowerArm", right, -30f * weight);
                        Rotate("Spine", up, 25f * weight);
                    }
                    break;

                case AttackMotion.Throw:
                    Rotate("RightUpperArm", right, (windUp ? -60f : -95f) * weight);
                    Rotate("RightLowerArm", right, (windUp ? -50f : -10f) * weight);
                    Rotate("Spine", up, (windUp ? -15f : 10f) * weight);
                    break;

                case AttackMotion.BothArms:
                    Rotate("LeftUpperArm", right, -70f * weight);
                    Rotate("RightUpperArm", right, -70f * weight);
                    Rotate("LeftUpperArm", up, 25f * weight);
                    Rotate("RightUpperArm", up, -25f * weight);
                    Rotate("Chest", right, (windUp ? -8f : 6f) * weight);
                    break;
            }
        }

        /// <summary>Rotates a bone around a world-space axis (no-op if the bone doesn't exist).</summary>
        void Rotate(string boneName, Vector3 worldAxis, float degrees)
        {
            Transform bone;
            if (Mathf.Abs(degrees) < 0.01f || !bones.TryGetValue(boneName, out bone))
            {
                return;
            }

            bone.rotation = Quaternion.AngleAxis(degrees, worldAxis) * bone.rotation;
        }
    }
}
