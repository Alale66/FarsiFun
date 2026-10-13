using System.Collections;
using TMPro;
using UnityEngine;

public class CoinFeedbackDisplay : MonoBehaviour
{
    [SerializeField] private TMP_Text popupTemplate;
    [SerializeField, Min(0.1f)] private float duration = 0.75f;
    [SerializeField] private float riseDistance = 100f;

    private RectTransform layer;
    private Canvas rootCanvas;

    private void Awake()
    {
        layer = transform as RectTransform;

        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas != null)
            rootCanvas = canvas.rootCanvas;
    }

    private void OnEnable()
    {
        PlayerRewardSystem.CoinFeedbackRequested += ShowPopup;
    }

    private void OnDisable()
    {
        PlayerRewardSystem.CoinFeedbackRequested -= ShowPopup;
    }

    private void ShowPopup(int amount, RectTransform target)
    {
        if (popupTemplate == null ||
            layer == null ||
            rootCanvas == null ||
            target == null)
        {
            return;
        }

        Camera eventCamera =
            rootCanvas.renderMode ==
            RenderMode.ScreenSpaceOverlay
                ? null
                : rootCanvas.worldCamera;

        Vector3 targetCenter =
            target.TransformPoint(target.rect.center);

        Vector2 screenPosition =
            RectTransformUtility.WorldToScreenPoint(
                eventCamera,
                targetCenter
            );

        if (!RectTransformUtility
            .ScreenPointToLocalPointInRectangle(
                layer,
                screenPosition,
                eventCamera,
                out Vector2 localPosition))
        {
            return;
        }

        TMP_Text popup =
            Instantiate(popupTemplate, layer);

        popup.outlineColor = new Color32(100, 42, 12, 255); // #642A0C
        popup.outlineWidth = 0.2f;

        popup.gameObject.SetActive(true);
        popup.raycastTarget = false;


        popup.gameObject.SetActive(true);
        popup.raycastTarget = false;
        popup.text =
            (amount > 0 ? "+" : "−") +
            ToPersianNumber(Mathf.Abs(amount));

        popup.color = amount > 0
            ? new Color(0.18f, 0.68f, 0.30f, 1f)
            : new Color(0.94f, 0.25f, 0.27f, 1f);

        RectTransform popupRect =
            popup.rectTransform;

        popupRect.anchorMin =
            new Vector2(0.5f, 0.5f);
        popupRect.anchorMax =
            new Vector2(0.5f, 0.5f);

        StartCoroutine(
            AnimatePopup(popup, localPosition)
        );
    }

    private IEnumerator AnimatePopup(
        TMP_Text popup,
        Vector2 startPosition)
    {
        RectTransform popupRect =
            popup.rectTransform;

        CanvasGroup popupGroup = popup.GetComponent<CanvasGroup>();

        if (popupGroup == null)
            popupGroup = popup.gameObject.AddComponent<CanvasGroup>();

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;

            float t = Mathf.Clamp01(
                elapsed / duration
            );

            popupRect.anchoredPosition =
                startPosition +
                Vector2.up *
                riseDistance *
                Mathf.SmoothStep(0f, 1f, t);

            popupRect.localScale =
                Vector3.one *
                Mathf.Lerp(0.8f, 1.15f, t);

            popupGroup.alpha = t < 0.5f
                ? 1f
                : 1f - (t - 0.5f) / 0.5f;

            yield return null;
        }

        Destroy(popup.gameObject);
    }

    private static string ToPersianNumber(int number)
    {
        return number.ToString()
            .Replace('0', '۰')
            .Replace('1', '۱')
            .Replace('2', '۲')
            .Replace('3', '۳')
            .Replace('4', '۴')
            .Replace('5', '۵')
            .Replace('6', '۶')
            .Replace('7', '۷')
            .Replace('8', '۸')
            .Replace('9', '۹');
    }
}