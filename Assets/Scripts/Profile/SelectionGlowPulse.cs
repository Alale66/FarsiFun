using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class SelectionGlowPulse : MonoBehaviour
{
    [SerializeField] private float cyclesPerSecond = 1.2f;
    [SerializeField] private float growAmount = 0.06f;

    private Image glowImage;
    private Vector3 originalScale;
    private Color originalColor;
    private float elapsed;

    private void OnEnable()
    {
        glowImage = GetComponent<Image>();
        originalScale = transform.localScale;
        originalColor = glowImage.color;
        elapsed = 0f;
    }

    private void Update()
    {
        elapsed += Time.unscaledDeltaTime;

        float pulse = 0.5f - 0.5f * Mathf.Cos(
            elapsed * Mathf.PI * 2f * cyclesPerSecond
        );

        transform.localScale =
            originalScale * (1f + growAmount * pulse);

        Color color = originalColor;
        color.a = originalColor.a * Mathf.Lerp(0.75f, 1f, pulse);
        glowImage.color = color;
    }

    private void OnDisable()
    {
        if (glowImage == null) return;

        transform.localScale = originalScale;
        glowImage.color = originalColor;
    }
}