using System;
using System.Collections;
using UnityEngine;

public class UIDragHint : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The hand image that moves between the two UI positions.")]
    [SerializeField] private RectTransform handRect;

    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Animation")]
    [SerializeField, Min(0.1f)]
    private float moveDuration = 0.7f;

    [SerializeField, Min(0f)]
    private float pauseBetweenRepeats = 0.2f;

    [SerializeField, Min(1)]
    private int repeatCount = 2;

    private Coroutine animationRoutine;

    private void Awake()
    {
        if (handRect == null)
            handRect = transform as RectTransform;

        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }
    }

    /// <summary>
    /// Demonstrates a drag gesture between two world-space UI positions.
    /// </summary>
    public void Play(
        Vector3 startPosition,
        Vector3 endPosition,
        Action onComplete = null)
    {
        StopHint();

        gameObject.SetActive(true);

        animationRoutine = StartCoroutine(
            PlayRoutine(
                startPosition,
                endPosition,
                onComplete
            )
        );
    }

    /// <summary>
    /// Immediately stops and hides the current drag hint.
    /// </summary>
    public void StopHint()
    {
        if (animationRoutine != null)
        {
            StopCoroutine(animationRoutine);
            animationRoutine = null;
        }

        if (canvasGroup != null)
            canvasGroup.alpha = 0f;
    }

    private IEnumerator PlayRoutine(
        Vector3 startPosition,
        Vector3 endPosition,
        Action onComplete)
    {
        for (int repeat = 0;
             repeat < repeatCount;
             repeat++)
        {
            if (handRect != null)
            {
                handRect.position = startPosition;
                handRect.localScale = Vector3.one * 0.9f;
            }

            if (canvasGroup != null)
                canvasGroup.alpha = 1f;

            float elapsed = 0f;

            while (elapsed < moveDuration)
            {
                elapsed += Time.unscaledDeltaTime;

                float t = Mathf.Clamp01(
                    elapsed / moveDuration
                );

                float easedT = Mathf.SmoothStep(
                    0f,
                    1f,
                    t
                );

                if (handRect != null)
                {
                    handRect.position = Vector3.Lerp(
                        startPosition,
                        endPosition,
                        easedT
                    );

                    float pulse =
                        1f + Mathf.Sin(t * Mathf.PI) * 0.12f;

                    handRect.localScale =
                        Vector3.one * pulse;
                }

                yield return null;
            }

            if (canvasGroup != null)
                canvasGroup.alpha = 0f;

            if (repeat < repeatCount - 1 &&
                pauseBetweenRepeats > 0f)
            {
                yield return new WaitForSecondsRealtime(
                    pauseBetweenRepeats
                );
            }
        }

        animationRoutine = null;
        onComplete?.Invoke();
    }

    private void OnDisable()
    {
        StopHint();
    }
}