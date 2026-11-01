using UnityEngine;

namespace VampWolfs
{
    public enum TelegraphKind
    {
        None,
        Line,    // projectile path
        Circle   // area, melee reach or landing spot
    }

    /// <summary>Shape drawn on the ground while an ability is being cast.</summary>
    public struct TelegraphInfo
    {
        public TelegraphKind Kind;
        public Vector3 Origin;      // line start, or circle center
        public Vector3 Direction;   // line direction
        public float Length;        // line length
        public float Width;         // line width, or circle radius

        public static TelegraphInfo None
        {
            get { return new TelegraphInfo { Kind = TelegraphKind.None }; }
        }

        public static TelegraphInfo Line(Vector3 origin, Vector3 direction, float length, float width)
        {
            return new TelegraphInfo { Kind = TelegraphKind.Line, Origin = origin, Direction = direction, Length = length, Width = width };
        }

        public static TelegraphInfo Circle(Vector3 center, float radius)
        {
            return new TelegraphInfo { Kind = TelegraphKind.Circle, Origin = center, Width = radius };
        }
    }

    /// <summary>
    /// Draws the cast telegraph on the ground. In Battlerite, reading the enemy's telegraph
    /// and moving out of it is the core skill, so every enemy cast shows one.
    /// With the VFX files present it uses the ground shader copied from the Space project
    /// (glowing rim, striped fill and a front that grows with the cast); otherwise flat primitives.
    /// </summary>
    public class TelegraphIndicator : MonoBehaviour
    {
        const float GroundHeight = 0.03f;

        GameObject line;
        GameObject circle;
        Material material;

        // VFX version: two quads with the "Space/VFX/Ground" shader (mode 1 = circle, 3 = line)
        Vfx.VfxSystem.FxObj vfxLine, vfxCircle;
        Vfx.VfxSystem vfxOwner;

        public void Show(TelegraphInfo info, Color color)
        {
            Show(info, color, 0f);
        }

        /// <summary>progress = cast progress 0..1 (the fill grows toward the edge).</summary>
        public void Show(TelegraphInfo info, Color color, float progress)
        {
            if (Vfx.VfxHub.Ready && ShowVfx(info, color, progress))
            {
                if (line != null) { line.SetActive(false); circle.SetActive(false); }
                return;
            }

            EnsureObjects();
            material.color = color;

            line.SetActive(info.Kind == TelegraphKind.Line);
            circle.SetActive(info.Kind == TelegraphKind.Circle);

            if (info.Kind == TelegraphKind.Line)
            {
                Vector3 direction = info.Direction.sqrMagnitude > 0.0001f ? info.Direction.normalized : Vector3.forward;
                Vector3 center = info.Origin + direction * (info.Length * 0.5f);
                line.transform.position = new Vector3(center.x, GroundHeight, center.z);
                line.transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
                line.transform.localScale = new Vector3(info.Width, 0.02f, info.Length);
            }
            else if (info.Kind == TelegraphKind.Circle)
            {
                circle.transform.position = new Vector3(info.Origin.x, GroundHeight, info.Origin.z);
                circle.transform.localScale = new Vector3(info.Width * 2f, 0.01f, info.Width * 2f);
            }
        }

        bool ShowVfx(TelegraphInfo info, Color color, float progress)
        {
            var v = Vfx.VfxHub.Fx;
            if (vfxOwner != v) { ReleaseVfx(); vfxOwner = v; }
            Color c = new Color(color.r * 1.6f, color.g * 1.6f, color.b * 1.6f, 1f);   // HDR: the rim glows with the bloom

            if (info.Kind == TelegraphKind.Circle)
            {
                if (vfxLine != null) { v.Return(vfxLine); vfxLine = null; }
                if (vfxCircle == null) vfxCircle = v.Acquire("ground");
                vfxCircle.T.position = new Vector3(info.Origin.x, 0.05f, info.Origin.z);
                vfxCircle.T.rotation = Quaternion.identity;
                vfxCircle.T.localScale = new Vector3(info.Width * 2f, 1f, info.Width * 2f);
                Vfx.VfxSystem.GroundSet(vfxCircle, 1, c, c, new Vector4(progress, 0f, 0f, 0f), 0.9f, 1.2f);
                return true;
            }

            if (info.Kind == TelegraphKind.Line)
            {
                if (vfxCircle != null) { v.Return(vfxCircle); vfxCircle = null; }
                if (vfxLine == null) vfxLine = v.Acquire("ground");
                Vector3 direction = info.Direction.sqrMagnitude > 0.0001f ? info.Direction.normalized : Vector3.forward;
                direction.y = 0f;
                Vector3 center = info.Origin + direction * (info.Length * 0.5f);
                float width = Mathf.Max(0.6f, info.Width);
                vfxLine.T.position = new Vector3(center.x, 0.05f, center.z);
                vfxLine.T.rotation = Quaternion.LookRotation(direction, Vector3.up);
                vfxLine.T.localScale = new Vector3(width, 1f, info.Length);
                Vfx.VfxSystem.GroundSet(vfxLine, 3, c, c, new Vector4(progress, info.Length / Mathf.Max(0.2f, width), 0f, 0f), 0.9f, 1.2f);
                return true;
            }

            ReleaseVfx();
            return true;
        }

        void ReleaseVfx()
        {
            if (vfxOwner != null)
            {
                if (vfxLine != null) vfxOwner.Return(vfxLine);
                if (vfxCircle != null) vfxOwner.Return(vfxCircle);
            }
            vfxLine = null; vfxCircle = null;
        }

        public void Hide()
        {
            if (line != null) line.SetActive(false);
            if (circle != null) circle.SetActive(false);
            ReleaseVfx();
        }
        void EnsureObjects()
        {
            if (line != null)
            {
                return;
            }

            material = MaterialLibrary.NewUnlit(Color.red);

            // Parented to the world root (not the character) so rotation of the character doesn't affect it.
            Transform parent = transform.parent;

            line = Shapes.Create(PrimitiveType.Cube, parent, name + " Telegraph Line", Vector3.zero, Vector3.one, Color.red, true);
            line.GetComponent<Renderer>().sharedMaterial = material;

            circle = Shapes.Create(PrimitiveType.Cylinder, parent, name + " Telegraph Circle", Vector3.zero, Vector3.one, Color.red, true);
            circle.GetComponent<Renderer>().sharedMaterial = material;

            Hide();
        }

        void OnDestroy()
        {
            ReleaseVfx();
            if (line != null) Destroy(line);
            if (circle != null) Destroy(circle);
            if (material != null) Destroy(material);
        }
    }
}
