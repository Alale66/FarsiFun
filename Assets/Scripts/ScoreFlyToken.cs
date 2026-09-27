using System;
using System.Collections;
using TMPro;
using UnityEngine;

public class ScoreFlyToken : MonoBehaviour
{
    [SerializeField] private TMP_Text letterText;

    [SerializeField, Min(0.1f)]
    private float flightDuration = 0.65f;

    [SerializeField, Min(0f)]
    private float arcHeight = 120f;

    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
    }

    public void Play(
        string letter,
        Vector3 startPosition,
        RectTransform target,
        Action onArrived)
    {
        if (letterText != null)
            letterText.text = letter;

        rectTransform.position = startPosition;
        rectTransform.localScale = Vector3.one * 0.7f;

        if (canvasGroup != null)
            canvasGroup.alpha = 1f;

        StartCoroutine(
            FlyToTarget(target, onArrived)
        );
    }

    private IEnumerator FlyToTarget(
        RectTransform target,
        Action onArrived)
    {
        if (target == null)
        {
            onArrived?.Invoke();
            Destroy(gameObject);
            yield break;
        }

        Vector3 startPosition = rectTransform.position;
        float elapsed = 0f;

        while (elapsed < flightDuration)
        {
            elapsed += Time.unscaledDeltaTime;

            float t = Mathf.Clamp01(
                elapsed / flightDuration
            );

            float easedT = Mathf.SmoothStep(
                0f,
                1f,
                t
            );

            Vector3 endPosition = target.position;

            Vector3 controlPosition =
                (startPosition + endPosition) * 0.5f +
                Vector3.up * arcHeight;

            float inverseT = 1f - easedT;

            rectTransform.position =
                inverseT * inverseT * startPosition +
                2f * inverseT * easedT *
                controlPosition +
                easedT * easedT * endPosition;

            float scale;

            if (t < 0.2f)
            {
                scale = Mathf.Lerp(
                    0.7f,
                    1.1f,
                    t / 0.2f
                );
            }
            else
            {
                scale = Mathf.Lerp(
                    1.1f,
                    0.45f,
                    (t - 0.2f) / 0.8f
                );
            }

            rectTransform.localScale =
                Vector3.one * scale;

            if (canvasGroup != null && t > 0.8f)
            {
                canvasGroup.alpha = Mathf.Lerp(
                    1f,
                    0f,
                    (t - 0.8f) / 0.2f
                );
            }

            yield return null;
        }

        onArrived?.Invoke();
        Destroy(gameObject);
    }
}