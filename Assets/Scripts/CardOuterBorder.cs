using UnityEngine;
using UnityEngine.UI;

public class CardOuterBorder : MaskableGraphic
{
    public float thickness = 5f;
    public float cornerRadius = 22f;

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();

        Rect rect = GetPixelAdjustedRect();

        float radius = Mathf.Clamp(
            cornerRadius,
            thickness,
            Mathf.Min(rect.width, rect.height) * 0.5f
        );

        float innerRadius = Mathf.Max(0f, radius - thickness);

        Vector2[] centers =
        {
            new Vector2(rect.xMax - radius, rect.yMax - radius),
            new Vector2(rect.xMin + radius, rect.yMax - radius),
            new Vector2(rect.xMin + radius, rect.yMin + radius),
            new Vector2(rect.xMax - radius, rect.yMin + radius)
        };

        const int steps = 8;
        int pointCount = 0;

        for (int corner = 0; corner < 4; corner++)
        {
            for (int step = 0; step <= steps; step++)
            {
                float angle =
                    (corner * 90f + step * 90f / steps)
                    * Mathf.Deg2Rad;

                Vector2 direction = new Vector2(
                    Mathf.Cos(angle),
                    Mathf.Sin(angle)
                );

                Vector2 outer = centers[corner] + direction * radius;
                Vector2 inner = centers[corner] + direction * innerRadius;

                vh.AddVert(
                    new Vector3(outer.x, outer.y),
                    color,
                    Vector2.zero
                );

                vh.AddVert(
                    new Vector3(inner.x, inner.y),
                    color,
                    Vector2.zero
                );

                pointCount++;
            }
        }

        for (int i = 0; i < pointCount; i++)
        {
            int next = (i + 1) % pointCount;

            int a = i * 2;
            int b = next * 2;

            vh.AddTriangle(a, b, a + 1);
            vh.AddTriangle(b, b + 1, a + 1);
        }
    }
}