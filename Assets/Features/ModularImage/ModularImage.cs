using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Features.ModularImage
{
    [RequireComponent(typeof(CanvasRenderer))]
    public class ModularImage : Image
    {
        [Header("Modular Settings")] [SerializeField]
        private CornerMode cornerMode = CornerMode.Uniform;

        [SerializeField] private float uniformRadius = 10f;
        [SerializeField] private CornerRadius cornerRadius = new CornerRadius(10f);
        [SerializeField] private int cornerSegments = 5;
        [SerializeField] private bool useModularShape = true;

        [Header("Outline")] [SerializeField] private OutlineSettings outline = OutlineSettings.None;

        public CornerMode Mode
        {
            get => cornerMode;
            set
            {
                cornerMode = value;
                SetVerticesDirty();
            }
        }

        public float UniformRadius
        {
            get => uniformRadius;
            set
            {
                uniformRadius = value;
                if (cornerMode == CornerMode.Uniform)
                    SetVerticesDirty();
            }
        }

        public CornerRadius Corners
        {
            get => cornerRadius;
            set
            {
                cornerRadius = value;
                if (cornerMode == CornerMode.Individual)
                    SetVerticesDirty();
            }
        }

        public bool UseModularShape
        {
            get => useModularShape;
            set
            {
                useModularShape = value;
                SetVerticesDirty();
            }
        }

        public OutlineSettings Outline
        {
            get => outline;
            set
            {
                outline = value;
                SetVerticesDirty();
            }
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            if (!useModularShape)
            {
                base.OnPopulateMesh(vh);
                return;
            }

            vh.Clear();

            Rect rect = GetPixelAdjustedRect();
            var effectiveRadius = GetEffectiveRadius(rect);

            if (outline.enabled && outline.width > 0)
            {
                GenerateWithSmartOutline(vh, rect, effectiveRadius, color, outline);
            }
            else
            {
                GenerateRoundedRect(vh, rect, effectiveRadius, color);
            }
        }

        private void GenerateWithSmartOutline(VertexHelper vh, Rect rect, CornerRadius radius, Color fillColor,
            OutlineSettings outlineSettings)
        {
            switch (outlineSettings.type)
            {
                case OutlineType.Outer:
                    // Fill first, outline on top
                    GenerateRoundedRect(vh, rect, radius, fillColor);
                    GenerateOutlineOnly(vh, rect, radius, outlineSettings);
                    break;

                case OutlineType.Center:
                    // Fill first, outline on top
                    GenerateRoundedRect(vh, rect, radius, fillColor);
                    GenerateOutlineOnly(vh, rect, radius, outlineSettings);
                    break;

                case OutlineType.Inner:
                    // Outline first and fill on top
                    GenerateInnerOutline(vh, rect, radius, outlineSettings);
                    GenerateInnerFill(vh, rect, radius, outlineSettings, fillColor);
                    break;
            }
        }

        private void GenerateInnerOutline(VertexHelper vh, Rect rect, CornerRadius radius,
            OutlineSettings outlineSettings)
        {
            // For Inner: the outline takes up the entire original space
            GenerateRoundedRect(vh, rect, radius, outlineSettings.color);
        }

        private void GenerateInnerFill(VertexHelper vh, Rect rect, CornerRadius radius, OutlineSettings outlineSettings,
            Color fillColor)
        {
            // Fill is smaller, inside
            Rect innerRect = new Rect(
                rect.x + outlineSettings.width,
                rect.y + outlineSettings.width,
                rect.width - outlineSettings.width * 2,
                rect.height - outlineSettings.width * 2
            );

            var innerRadius = new CornerRadius(
                Mathf.Max(0, radius.topLeft - outlineSettings.width),
                Mathf.Max(0, radius.topRight - outlineSettings.width),
                Mathf.Max(0, radius.bottomRight - outlineSettings.width),
                Mathf.Max(0, radius.bottomLeft - outlineSettings.width)
            );

            // Check that the inner rect is valid
            if (innerRect.width > 0 && innerRect.height > 0)
            {
                GenerateRoundedRect(vh, innerRect, innerRadius, fillColor);
            }
        }

        private CornerRadius GetEffectiveRadius(Rect rect)
        {
            switch (cornerMode)
            {
                case CornerMode.Uniform:
                    return new CornerRadius(uniformRadius);

                case CornerMode.FullRounded:
                    float maxRadius = Mathf.Min(rect.width, rect.height) * 0.5f;
                    return new CornerRadius(maxRadius);

                case CornerMode.Individual:
                default:
                    return cornerRadius;
            }
        }

        private void GenerateRoundedRect(VertexHelper vh, Rect rect, CornerRadius radius, Color color)
        {
            // Clamp radius
            float maxRadius = Mathf.Min(rect.width, rect.height) * 0.5f;
            var clampedRadius = new CornerRadius(
                Mathf.Min(radius.topLeft, maxRadius),
                Mathf.Min(radius.topRight, maxRadius),
                Mathf.Min(radius.bottomRight, maxRadius),
                Mathf.Min(radius.bottomLeft, maxRadius)
            );

            List<Vector2> vertices = GenerateVertices(rect, clampedRadius);

            int startIndex = vh.currentVertCount;

            foreach (var vertex in vertices)
            {
                AddVertex(vh, vertex, color);
            }

            // Correct triangulation for convex polygon
            TriangulateConvexPolygon(vh, startIndex, vertices.Count);
        }

        private void GenerateOutlineOnly(VertexHelper vh, Rect rect, CornerRadius radius,
            OutlineSettings outlineSettings)
        {
            if (outlineSettings.type == OutlineType.Inner)
                return;

            Rect outerRect, innerRect;
            CornerRadius outerRadius, innerRadius;

            if (outlineSettings.type == OutlineType.Outer)
            {
                outerRect = new Rect(
                    rect.x - outlineSettings.width,
                    rect.y - outlineSettings.width,
                    rect.width + outlineSettings.width * 2,
                    rect.height + outlineSettings.width * 2
                );
                innerRect = rect;
                outerRadius = new CornerRadius(
                    radius.topLeft + outlineSettings.width,
                    radius.topRight + outlineSettings.width,
                    radius.bottomRight + outlineSettings.width,
                    radius.bottomLeft + outlineSettings.width
                );
                innerRadius = radius;
            }
            else
            {
                float halfWidth = outlineSettings.width * 0.5f;
                outerRect = new Rect(
                    rect.x - halfWidth,
                    rect.y - halfWidth,
                    rect.width + outlineSettings.width,
                    rect.height + outlineSettings.width
                );
                innerRect = new Rect(
                    rect.x + halfWidth,
                    rect.y + halfWidth,
                    rect.width - outlineSettings.width,
                    rect.height - outlineSettings.width
                );
                outerRadius = new CornerRadius(
                    radius.topLeft + halfWidth,
                    radius.topRight + halfWidth,
                    radius.bottomRight + halfWidth,
                    radius.bottomLeft + halfWidth
                );
                innerRadius = new CornerRadius(
                    Mathf.Max(0, radius.topLeft - halfWidth),
                    Mathf.Max(0, radius.topRight - halfWidth),
                    Mathf.Max(0, radius.bottomRight - halfWidth),
                    Mathf.Max(0, radius.bottomLeft - halfWidth)
                );
            }

            if (innerRect.width <= 0 || innerRect.height <= 0)
                return;

            float maxOuterRadius = Mathf.Min(outerRect.width, outerRect.height) * 0.5f;
            float maxInnerRadius = Mathf.Min(innerRect.width, innerRect.height) * 0.5f;

            var clampedOuterRadius = new CornerRadius(
                Mathf.Min(outerRadius.topLeft, maxOuterRadius),
                Mathf.Min(outerRadius.topRight, maxOuterRadius),
                Mathf.Min(outerRadius.bottomRight, maxOuterRadius),
                Mathf.Min(outerRadius.bottomLeft, maxOuterRadius)
            );

            var clampedInnerRadius = new CornerRadius(
                Mathf.Min(innerRadius.topLeft, maxInnerRadius),
                Mathf.Min(innerRadius.topRight, maxInnerRadius),
                Mathf.Min(innerRadius.bottomRight, maxInnerRadius),
                Mathf.Min(innerRadius.bottomLeft, maxInnerRadius)
            );

            var outerVertices = GenerateVertices(outerRect, clampedOuterRadius);
            var innerVertices = GenerateVertices(innerRect, clampedInnerRadius);

            CreateOutlineBand(vh, outerVertices, innerVertices, outlineSettings.color);
        }


        private void CreateOutlineBand(VertexHelper vh, List<Vector2> outerVertices, List<Vector2> innerVertices,
            Color outlineColor)
        {
            int outerCount = outerVertices.Count;
            int innerCount = innerVertices.Count;

            // Use the smallest number to avoid errors
            int count = Mathf.Min(outerCount, innerCount);

            int startIndex = vh.currentVertCount;

            // Add all vertices
            for (int i = 0; i < count; i++)
            {
                AddVertex(vh, outerVertices[i], outlineColor);
            }

            for (int i = 0; i < count; i++)
            {
                AddVertex(vh, innerVertices[i], outlineColor);
            }

            // Create triangles for the band
            for (int i = 0; i < count; i++)
            {
                int next = (i + 1) % count;

                int outerCurrent = startIndex + i;
                int outerNext = startIndex + next;
                int innerCurrent = startIndex + count + i;
                int innerNext = startIndex + count + next;

                // First triangle of the quad
                vh.AddTriangle(outerCurrent, innerCurrent, outerNext);
                // Second triangle of the quad
                vh.AddTriangle(outerNext, innerCurrent, innerNext);
            }
        }

        private List<Vector2> GenerateVertices(Rect rect, CornerRadius radius)
        {
            List<Vector2> vertices = new List<Vector2>();

            // Bottom-left corner
            if (radius.bottomLeft > 0)
            {
                Vector2 center = new Vector2(rect.xMin + radius.bottomLeft, rect.yMin + radius.bottomLeft);
                AddArcVertices(vertices, center, radius.bottomLeft, 180f, 270f);
            }
            else
            {
                vertices.Add(new Vector2(rect.xMin, rect.yMin));
            }

            // Bottom-right corner
            if (radius.bottomRight > 0)
            {
                Vector2 center = new Vector2(rect.xMax - radius.bottomRight, rect.yMin + radius.bottomRight);
                AddArcVertices(vertices, center, radius.bottomRight, 270f, 360f);
            }
            else
            {
                vertices.Add(new Vector2(rect.xMax, rect.yMin));
            }

            // Top-right corner
            if (radius.topRight > 0)
            {
                Vector2 center = new Vector2(rect.xMax - radius.topRight, rect.yMax - radius.topRight);
                AddArcVertices(vertices, center, radius.topRight, 0f, 90f);
            }
            else
            {
                vertices.Add(new Vector2(rect.xMax, rect.yMax));
            }

            // Top-left corner
            if (radius.topLeft > 0)
            {
                Vector2 center = new Vector2(rect.xMin + radius.topLeft, rect.yMax - radius.topLeft);
                AddArcVertices(vertices, center, radius.topLeft, 90f, 180f);
            }
            else
            {
                vertices.Add(new Vector2(rect.xMin, rect.yMax));
            }

            return vertices;
        }

        private void AddArcVertices(List<Vector2> vertices, Vector2 center, float radius, float startAngle,
            float endAngle)
        {
            for (int i = 0; i <= cornerSegments; i++)
            {
                float t = (float)i / cornerSegments;
                float angle = Mathf.Lerp(startAngle, endAngle, t) * Mathf.Deg2Rad;

                Vector2 point = center + new Vector2(
                    Mathf.Cos(angle) * radius,
                    Mathf.Sin(angle) * radius
                );

                vertices.Add(point);
            }
        }

        private void TriangulateConvexPolygon(VertexHelper vh, int startIndex, int vertexCount)
        {
            for (int i = 1; i < vertexCount - 1; i++)
            {
                vh.AddTriangle(startIndex, startIndex + i, startIndex + i + 1);
            }
        }

        private int AddVertex(VertexHelper vh, Vector2 position, Color color)
        {
            UIVertex vertex = UIVertex.simpleVert;
            vertex.position = position;
            vertex.color = color;
    
            vh.AddVert(vertex);
            return vh.currentVertCount - 1;
        }

        // Convenance methods
        public void SetOutline(float width, Color color, OutlineType type)
        {
            Outline = new OutlineSettings(width > 0, width, color, type);
        }

        public void RemoveOutline()
        {
            Outline = OutlineSettings.None;
        }

        public void SetOutlineColor(Color color)
        {
            var newOutline = outline;
            newOutline.color = color;
            Outline = newOutline;
        }

        public void SetOutlineWidth(float width)
        {
            var newOutline = outline;
            newOutline.width = width;
            newOutline.enabled = width > 0;
            Outline = newOutline;
        }

#if UNITY_EDITOR
        void Reset()
        {
            Image existingImage = GetComponent<Image>();
            if (existingImage != null && existingImage != this)
            {
                ModularImageUtility.ReplaceWithModularImage(gameObject);
            }
        }

        protected override void OnValidate()
        {
            base.OnValidate();
            cornerSegments = Mathf.Max(1, cornerSegments);
            SetVerticesDirty();
        }
#endif
    }
}