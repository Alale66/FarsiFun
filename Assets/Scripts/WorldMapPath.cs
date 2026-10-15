using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]
[RequireComponent(typeof(CanvasRenderer))]
public class WorldMapPath : MaskableGraphic
{
    [SerializeField] private RectTransform startPoint;
    [SerializeField] private RectTransform curvePoint;
    [SerializeField] private RectTransform endPoint;

    [SerializeField, Range(2, 60)] private int dotCount = 14;
    [SerializeField, Min(1f)] private float dotRadius = 5f;

    protected override void OnEnable()
    {
        base.OnEnable();
        raycastTarget = false;
    }

    private void LateUpdate()
    {
        SetVerticesDirty();
    }

    public Vector3 GetWorldPoint(float progress)
    {
        if (startPoint == null ||
            curvePoint == null ||
            endPoint == null)
        {
            return transform.position;
        }

        float t = Mathf.Clamp01(progress);
        float remaining = 1f - t;

        return remaining * remaining * startPoint.position
            + 2f * remaining * t * curvePoint.position
            + t * t * endPoint.position;
    }

    protected override void OnPopulateMesh(VertexHelper mesh)
    {
        mesh.Clear();

        if (startPoint == null ||
            curvePoint == null ||
            endPoint == null)
        {
            return;
        }

        const int segments = 12;
        int count = Mathf.Max(2, dotCount);

        for (int i = 0; i < count; i++)
        {
            float t = i / (float)(count - 1);

            Vector3 center = rectTransform.InverseTransformPoint(
                GetWorldPoint(t)
            );

            int firstVertex = mesh.currentVertCount;

            mesh.AddVert(center, color, Vector2.zero);

            for (int j = 0; j < segments; j++)
            {
                float angle = j * Mathf.PI * 2f / segments;

                Vector3 offset = new Vector3(
                    Mathf.Cos(angle),
                    Mathf.Sin(angle),
                    0f
                ) * dotRadius;

                mesh.AddVert(
                    center + offset,
                    color,
                    Vector2.zero
                );
            }

            for (int j = 0; j < segments; j++)
            {
                mesh.AddTriangle(
                    firstVertex,
                    firstVertex + 1 + j,
                    firstVertex + 1 + (j + 1) % segments
                );
            }
        }
    }
}