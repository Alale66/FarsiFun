using UnityEngine;

public class ContinueButtonWiggle : MonoBehaviour
{
    [SerializeField, Range(0f, 10f)] private float angle = 3.5f;
    [SerializeField, Range(0f, 0.15f)] private float scaleAmount = 0.035f;
    [SerializeField, Min(0.1f)] private float cyclesPerSecond = 1.2f;

    private RectTransform rect;
    private Quaternion originalRotation;
    private Vector3 originalScale;
    private float elapsed;

    private void OnEnable()
    {
        rect = transform as RectTransform;
        originalRotation = transform.localRotation;
        originalScale = transform.localScale;
        elapsed = 0f;
    }

    private void Update()
    {
        elapsed += Time.unscaledDeltaTime;
        float wave = Mathf.Sin(elapsed * Mathf.PI * 2f * cyclesPerSecond);

        rect.localRotation =
            originalRotation * Quaternion.Euler(0f, 0f, angle * wave);

        rect.localScale =
            originalScale * (1f + scaleAmount * wave * wave);
    }

    private void OnDisable()
    {
        if (rect == null) return;

        rect.localRotation = originalRotation;
        rect.localScale = originalScale;
    }
}