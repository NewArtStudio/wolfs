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
    /// </summary>
    public class TelegraphIndicator : MonoBehaviour
    {
        const float GroundHeight = 0.03f;

        GameObject line;
        GameObject circle;
        Material material;

        public void Show(TelegraphInfo info, Color color)
        {
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

        public void Hide()
        {
            if (line != null) line.SetActive(false);
            if (circle != null) circle.SetActive(false);
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
            if (line != null) Destroy(line);
            if (circle != null) Destroy(circle);
            if (material != null) Destroy(material);
        }
    }
}
