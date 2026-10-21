using UnityEngine;

[RequireComponent(typeof(RectTransform))]
public class SplashLogoMotion : MonoBehaviour
{
    [SerializeField] private float floatAmount = 5f;
    [SerializeField] private float rotationAmount = 1f;
    [SerializeField, Min(0.1f)]
    private float cycleDuration = 3f;

    private RectTransform logoRect;
    private Vector2 startPosition;
    private Quaternion startRotation;
    private float elapsed;

    private void Awake()
    {
        logoRect = GetComponent<RectTransform>();
    }

    private void OnEnable()
    {
        startPosition = logoRect.anchoredPosition;
        startRotation = logoRect.localRotation;
        elapsed = 0f;
    }

    private void Update()
    {
        elapsed += Time.unscaledDeltaTime;

        float phase = elapsed * Mathf.PI * 2f /
            Mathf.Max(0.1f, cycleDuration);

        float wave = Mathf.Sin(phase);

        logoRect.anchoredPosition =
            startPosition + Vector2.up * wave * floatAmount;

        logoRect.localRotation =
            startRotation *
            Quaternion.Euler(
                0f,
                0f,
                Mathf.Sin(phase * 0.5f) * rotationAmount
            );
    }

    private void OnDisable()
    {
        if (logoRect == null)
            return;

        logoRect.anchoredPosition = startPosition;
        logoRect.localRotation = startRotation;
    }
}