using UnityEngine;

public class UIHintBob : MonoBehaviour
{
    [SerializeField, Min(0f)]
    private float moveDistance = 12f;

    [SerializeField, Min(0f)]
    private float moveSpeed = 3f;

    private RectTransform rectTransform;
    private Vector2 startPosition;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
    }

    private void OnEnable()
    {
        if (rectTransform == null)
            rectTransform = GetComponent<RectTransform>();

        startPosition = rectTransform.anchoredPosition;
    }

    private void Update()
    {
        if (rectTransform == null)
            return;

        float offset =
            Mathf.Sin(Time.unscaledTime * moveSpeed) *
            moveDistance;

        rectTransform.anchoredPosition =
            startPosition + Vector2.up * offset;
    }

    private void OnDisable()
    {
        if (rectTransform != null)
            rectTransform.anchoredPosition = startPosition;
    }
}