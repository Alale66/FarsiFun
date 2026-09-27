using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class WrongCatchFeedback : MonoBehaviour
{
    [SerializeField] private Graphic targetGraphic;

    [SerializeField]
    private Color wrongColor =
        new Color32(210, 70, 65, 255);

    [SerializeField, Min(0.05f)]
    private float duration = 0.3f;

    [SerializeField, Min(1f)]
    private float pulseScale = 1.08f;

    private Color originalColor;
    private Vector3 originalScale;
    private Coroutine feedbackRoutine;

    private void Awake()
    {
        if (targetGraphic == null)
            targetGraphic = GetComponent<Graphic>();

        if (targetGraphic != null)
            originalColor = targetGraphic.color;

        originalScale = transform.localScale;
    }

    public void Play()
    {
        if (!gameObject.activeInHierarchy)
            return;

        if (feedbackRoutine != null)
            StopCoroutine(feedbackRoutine);

        feedbackRoutine =
            StartCoroutine(PlayFeedback());
    }

    private IEnumerator PlayFeedback()
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;

            float t = Mathf.Clamp01(
                elapsed / duration
            );

            float strength =
                Mathf.Sin(t * Mathf.PI);

            if (targetGraphic != null)
            {
                targetGraphic.color = Color.Lerp(
                    originalColor,
                    wrongColor,
                    strength
                );
            }

            transform.localScale =
                originalScale *
                Mathf.Lerp(
                    1f,
                    pulseScale,
                    strength
                );

            yield return null;
        }

        ResetVisual();
        feedbackRoutine = null;
    }

    private void ResetVisual()
    {
        if (targetGraphic != null)
            targetGraphic.color = originalColor;

        transform.localScale = originalScale;
    }

    private void OnDisable()
    {
        if (feedbackRoutine != null)
        {
            StopCoroutine(feedbackRoutine);
            feedbackRoutine = null;
        }

        ResetVisual();
    }
}