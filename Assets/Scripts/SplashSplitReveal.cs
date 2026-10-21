using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SplashSplitReveal : MonoBehaviour
{
    [Header("Paper Pieces")]
    [SerializeField] private RectTransform paperLeft;
    [SerializeField] private RectTransform paperRight;

    [Header("Timing")]
    [SerializeField] private float startDelay = 0.6f;
    [SerializeField] private float pullDuration = 0.7f;
    [SerializeField] private float pauseBetweenPulls = 0.15f;
    [SerializeField] private float endDelay = 2f;

    [Header("Opening")]
    [SerializeField, Range(0f, 1f)]
    private float horizontalOpening = 0.35f;

    [SerializeField, Range(0f, 1f)]
    private float verticalOpening = 0.35f;

    [Header("Sound")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip tearSound;

    [Header("Navigation")]
    [SerializeField] private bool goToProfileWhenFinished;
    [SerializeField]
    private string nextScene =
        "ProfileSelection";

    private Vector2 leftStart;
    private Vector2 rightStart;
    private Vector2 openingOffset;

    private IEnumerator Start()
    {
        if (paperLeft == null || paperRight == null ||
            paperLeft.parent != paperRight.parent)
        {
            Debug.LogError(
                "Splash: Assign both paper pieces " +
                "under the same Canvas.",
                this
            );

            yield break;
        }

        RectTransform area =
            paperLeft.parent as RectTransform;

        if (area == null)
            yield break;

        Canvas.ForceUpdateCanvases();

        leftStart = paperLeft.anchoredPosition;
        rightStart = paperRight.anchoredPosition;

        openingOffset = new Vector2(
            area.rect.width * horizontalOpening,
            area.rect.height * verticalOpening
        );

        yield return new WaitForSecondsRealtime(
            Mathf.Max(0f, startDelay)
        );

        float[] stops = { 0f, 0.3f, 0.65f, 1f };

        for (int pull = 0; pull < stops.Length - 1; pull++)
        {
            if (audioSource != null && tearSound != null)
                audioSource.PlayOneShot(tearSound);

            float elapsed = 0f;
            float duration = Mathf.Max(0.1f, pullDuration);

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;

                float t = Mathf.Clamp01(
                    elapsed / duration
                );

                float eased = Mathf.SmoothStep(
                    0f, 1f, t
                );

                SetOpening(
                    Mathf.Lerp(
                        stops[pull],
                        stops[pull + 1],
                        eased
                    )
                );

                yield return null;
            }

            SetOpening(stops[pull + 1]);

            if (pull < stops.Length - 2)
            {
                yield return new WaitForSecondsRealtime(
                    Mathf.Max(0f, pauseBetweenPulls)
                );
            }
        }

        yield return new WaitForSecondsRealtime(
            Mathf.Max(0f, endDelay)
        );

        if (goToProfileWhenFinished)
            SceneFadeTransition.GoTo(nextScene);
    }

    private void SetOpening(float progress)
    {
        Vector2 offset = openingOffset * progress;

        paperLeft.anchoredPosition =
            leftStart + offset;

        paperRight.anchoredPosition =
            rightStart - offset;
    }
}