using System;
using UnityEngine;
using UnityEngine.UI;

// Add this component to the IslandView Canvas.
public class IslandCurrentLetterPointer : MonoBehaviour
{
    [SerializeField] private Vector2 arrowSize = new Vector2(60f, 78f);
    [SerializeField] private float gapAboveLetter = 18f;
    [SerializeField] private float bounceHeight = 12f;
    [SerializeField] private float bounceSpeed = 1.2f;
    [SerializeField] private Sprite arrowSprite;

    private Canvas canvas;
    private RectTransform arrow;
    private IslandLetterButton[] letters;
    private readonly Vector3[] corners = new Vector3[4];

    private void Start()
    {
        if (arrowSprite == null)
        {
            Debug.LogWarning("Assign the island arrow sprite in Arrow Sprite.", this);
            enabled = false;
            return;
        }

        canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("Island pointer needs a Canvas parent.", this);
            enabled = false;
            return;
        }

        canvas = canvas.rootCanvas;
        letters = FindObjectsByType<IslandLetterButton>(
     FindObjectsInactive.Include);

        GameObject visual = new GameObject("CurrentLetterArrow",
            typeof(RectTransform), typeof(CanvasRenderer),
            typeof(Image));
        arrow = visual.GetComponent<RectTransform>();
        arrow.SetParent(canvas.transform, false);
        arrow.anchorMin = arrow.anchorMax = new Vector2(0.5f, 0.5f);
        arrow.pivot = new Vector2(0.5f, 0f);
        arrow.sizeDelta = arrowSize;
        Image graphic = visual.GetComponent<Image>();
        graphic.sprite = arrowSprite;
        graphic.color = Color.white;
        graphic.preserveAspect = true;
        graphic.raycastTarget = false;
        visual.SetActive(false);
    }

    private void LateUpdate()
    {
        if (arrow == null) return;
        PlayerProfileData profile = PlayerProfileManager.Instance != null
            ? PlayerProfileManager.Instance.SelectedProfile : null;
        IslandLetterButton target = null;

        if (profile != null)
        {
            int current = profile.highestUnlockedLetterIndex;
            bool completed = Array.IndexOf(
                profile.completedLetterIndices ?? Array.Empty<int>(), current) >= 0;
            if (!completed && current / 5 == SceneNavigator.SelectedIslandIndex)
            {
                foreach (IslandLetterButton letter in letters)
                {
                    if (letter != null && letter.isActiveAndEnabled &&
                        letter.LocalLetterIndex == current % 5 &&
                        letter.GetComponent<Button>().IsInteractable())
                    {
                        target = letter;
                        break;
                    }
                }
            }
        }

        arrow.gameObject.SetActive(target != null);
        if (target == null) return;

        RectTransform targetRect = target.transform as RectTransform;
        if (targetRect == null) return;
        targetRect.GetWorldCorners(corners);
        Vector3 topCenter = (corners[1] + corners[2]) * 0.5f;
        Canvas targetCanvas = target.GetComponentInParent<Canvas>();
        Camera targetCamera = targetCanvas.renderMode == RenderMode.ScreenSpaceOverlay
            ? null : targetCanvas.worldCamera;
        Camera arrowCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay
            ? null : canvas.worldCamera;
        Vector2 screen = RectTransformUtility.WorldToScreenPoint(targetCamera, topCenter);
        RectTransform parent = (RectTransform)canvas.transform;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            parent, screen, arrowCamera, out Vector2 point))
        {
            float bounce = (1f + Mathf.Sin(Time.unscaledTime * bounceSpeed *
                Mathf.PI * 2f)) * 0.5f * bounceHeight;
            arrow.localPosition = new Vector3(point.x,
                point.y + gapAboveLetter + bounce, 0f);
        }
        arrow.SetAsLastSibling();
    }

    private void OnDisable()
    {
        if (arrow != null) arrow.gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        if (arrow != null) Destroy(arrow.gameObject);
    }
}
