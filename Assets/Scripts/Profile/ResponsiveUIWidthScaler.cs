using UnityEngine;
using UnityEngine.UI;

public class ResponsiveUIWidthScaler : MonoBehaviour
{
    [SerializeField, Min(0f)]
    private float horizontalMargin = 80f;

    [SerializeField, Range(0.5f, 1f)]
    private float minimumScale = 0.75f;

    private RectTransform rectTransform;
    private RectTransform parentRect;
    private Canvas rootCanvas;

    private void Awake()
    {
        rectTransform = transform as RectTransform;
        parentRect = transform.parent as RectTransform;
        rootCanvas = GetComponentInParent<Canvas>();

        if (rootCanvas != null)
            rootCanvas = rootCanvas.rootCanvas;
    }

    private void Start()
    {
        UpdateScale();
    }

    private void OnRectTransformDimensionsChange()
    {
        UpdateScale();
    }

    private void UpdateScale()
    {
        if (rectTransform == null ||
            parentRect == null)
        {
            return;
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(
            rectTransform
        );

        float preferredWidth =
            LayoutUtility.GetPreferredWidth(rectTransform);

        float canvasWidth = rootCanvas != null
     ? rootCanvas.pixelRect.width /
       rootCanvas.scaleFactor
     : parentRect.rect.width;

        float availableWidth =
            canvasWidth -
            horizontalMargin * 2f;

        if (preferredWidth <= 0f)
            return;

        float scale = Mathf.Clamp(
            availableWidth / preferredWidth,
            minimumScale,
            1f
        );

        rectTransform.localScale =
            Vector3.one * scale;
    }
}