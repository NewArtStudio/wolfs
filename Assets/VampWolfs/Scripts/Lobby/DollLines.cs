using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace VampWolfs.Lobby
{
    /// <summary>
    /// Linhas finas que ligam cada slot do Arsenal à parte do corpo do personagem (referência do criador, out/2026).
    /// Fica por cima do cenário e atrás dos cartões; os pontos do corpo são frações da área da prévia 3D
    /// (o modelo gira, então a ligação é aproximada, como na referência).
    /// </summary>
    public sealed class DollLines : VisualElement
    {
        /// <summary>Ponto do corpo de cada slot: (x, y) em fração da imagem do personagem (0,0 = canto de cima à esquerda).</summary>
        static readonly Dictionary<string, Vector2> Anchors = new Dictionary<string, Vector2>
        {
            { "head", new Vector2(0.5f, 0.13f) }, { "chest", new Vector2(0.46f, 0.33f) }, { "gloves", new Vector2(0.3f, 0.5f) }, { "boots", new Vector2(0.43f, 0.9f) },
            { "accMajor", new Vector2(0.56f, 0.28f) }, { "handR", new Vector2(0.7f, 0.47f) }, { "handL", new Vector2(0.66f, 0.55f) },
            { "acc1", new Vector2(0.58f, 0.6f) }, { "acc2", new Vector2(0.56f, 0.84f) }, { "acc3", new Vector2(0.6f, 0.88f) },
        };

        readonly Dictionary<string, VisualElement> slots = new Dictionary<string, VisualElement>();
        readonly HashSet<VisualElement> watched = new HashSet<VisualElement>();   // cada elemento ganha o aviso de layout uma vez só
        VisualElement figure;
        public Color Color = new Color(0.95f, 0.66f, 0.23f, 0.5f);

        public DollLines()
        {
            pickingMode = PickingMode.Ignore;
            AddToClassList("doll-lines");
            generateVisualContent += Draw;
        }

        /// <summary>Define a área do personagem e os cartões dos slots (chamar a cada render do boneco).</summary>
        public void Bind(VisualElement figureArea, Dictionary<string, VisualElement> slotCards)
        {
            figure = figureArea; slots.Clear();
            foreach (var kv in slotCards) { slots[kv.Key] = kv.Value; Watch(kv.Value); }
            if (figure != null) Watch(figure);
            MarkDirtyRepaint();
        }

        void Watch(VisualElement e)
        {
            if (!watched.Add(e)) return;
            e.RegisterCallback<GeometryChangedEvent>(ev => MarkDirtyRepaint());
        }

        void Draw(MeshGenerationContext ctx)
        {
            if (figure == null || slots.Count == 0) return;
            var fr = this.WorldToLocal(figure.worldBound);
            if (fr.width < 10 || fr.height < 10) return;
            var p = ctx.painter2D; p.lineJoin = LineJoin.Round; p.lineCap = LineCap.Round;
            foreach (var kv in slots)
            {
                Vector2 a; if (!Anchors.TryGetValue(kv.Key, out a)) continue;
                var r = this.WorldToLocal(kv.Value.worldBound);
                if (r.width < 4) continue;
                var target = new Vector2(fr.xMin + fr.width * a.x, fr.yMin + fr.height * a.y);
                // sai da borda do cartão voltada para o personagem; para os de baixo, da borda de cima
                bool below = r.yMin > target.y + 20f && r.xMax > target.x - 40f && r.xMin < fr.xMax;
                Vector2 start, elbow;
                if (below) { start = new Vector2(r.center.x, r.yMin); elbow = new Vector2(start.x, start.y - 14f); }
                else if (r.center.x < target.x) { start = new Vector2(r.xMax, r.center.y); elbow = new Vector2(start.x + 22f, start.y); }
                else { start = new Vector2(r.xMin, r.center.y); elbow = new Vector2(start.x - 22f, start.y); }
                p.strokeColor = Color; p.lineWidth = 1.2f;
                p.BeginPath(); p.MoveTo(start); p.LineTo(elbow); p.LineTo(target); p.Stroke();
                p.fillColor = new Color(Color.r, Color.g, Color.b, Mathf.Min(1f, Color.a * 1.8f));
                p.BeginPath(); p.Arc(target, 2.6f, 0f, 360f); p.Fill();
                p.BeginPath(); p.Arc(start, 2f, 0f, 360f); p.Fill();
            }
        }
    }
}
