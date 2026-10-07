using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.EventSystems;

public class Match3TileView : MonoBehaviour,
    IPointerDownHandler,
    IDragHandler,
    IPointerUpHandler
{
    [Header("UI")]
    [SerializeField] private TMP_Text letterText;
    [SerializeField] private Image tileImage;
    [SerializeField] private Image targetRing;
    [SerializeField] private Button button;
    [SerializeField] private Outline matchOutline;
    [SerializeField] private CanvasGroup tileCanvasGroup;

    [Header("Drag")]
    [SerializeField, Min(5f)]
    private float minimumDragDistance = 35f;

    [SerializeField, Min(0.05f)]
    private float invalidDragReturnDuration = 0.2f;

    [Header("Tutorial Preview")]
    [SerializeField, Min(0.1f)]
    [Tooltip("Duration of the automatic tutorial drag movement.")]
    private float tutorialDragDuration = 0.7f;

    [SerializeField, Min(0.05f)]
    [Tooltip("Duration of the automatic return to the original positions.")]
    private float tutorialReturnDuration = 0.25f;

    [SerializeField, Min(0f)]
    [Tooltip("Pause between repeated tutorial drag previews.")]
    private float tutorialRepeatPause = 0.2f;

    [SerializeField, Min(1)]
    [Tooltip("Number of times the tutorial drag preview is repeated.")]
    private int tutorialRepeatCount = 2;

    private RectTransform tileRect;
    private GridLayoutGroup parentGridLayout;
    private RectTransform gridRect;
    private Vector2 originalAnchoredPosition;
    private Func<Match3TileView, Vector2Int, bool> dragHandler;
    private Vector2 dragStartPosition;
    private Vector2 dragStartLocalPosition;
    private bool suppressNextClick;
    private Action<Match3TileView> clickHandler;
    private Action interactionStartedHandler;

    private Func<
    Match3TileView,
    Vector2Int,
    Match3TileView
> neighborProvider;

    private Func<
        Match3TileView,
        Vector2Int,
        Match3TileView
    > matchTileProvider;

    private Match3TileView previewNeighbor;
    private RectTransform previewNeighborRect;
    private Vector2 previewNeighborOriginalPosition;
    private bool isReturningFromInvalidDrag;
    private Coroutine tutorialPreviewRoutine;

    public int Row { get; private set; }
    public int Column { get; private set; }

    public string FamilyId { get; private set; }
    public bool IsTargetFamily { get; private set; }
    public string DisplayedLetter { get; private set; }

    private void Awake()
    {
        if (tileImage == null)
            tileImage = GetComponent<Image>();

        if (button == null)
            button = GetComponent<Button>();

        if (matchOutline == null)
            matchOutline = GetComponent<Outline>();

        if (matchOutline != null)
            matchOutline.enabled = false;

        if (tileCanvasGroup == null)
            tileCanvasGroup = GetComponent<CanvasGroup>();

        if (tileCanvasGroup != null)
            tileCanvasGroup.alpha = 1f;

        tileRect = transform as RectTransform;
        parentGridLayout =
    GetComponentInParent<GridLayoutGroup>();

        if (parentGridLayout != null)
        {
            gridRect =
                parentGridLayout.transform as RectTransform;
        }
    }

    public void Initialize(
        int row,
        int column,
        Action<Match3TileView> onClicked,
        Func<Match3TileView, Vector2Int, bool> onDragged,
        Func<
            Match3TileView,
            Vector2Int,
            Match3TileView
        > getNeighbor,
        Func<
            Match3TileView,
            Vector2Int,
            Match3TileView
        > getMatchingTile,
        Action onInteractionStarted)
    {
        matchTileProvider = getMatchingTile;
        interactionStartedHandler = onInteractionStarted;

        Row = row;
        Column = column;

        clickHandler = onClicked;
        dragHandler = onDragged;
        neighborProvider = getNeighbor;

        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(HandleClicked);
        }

        SetSelected(false);
        SetInteractable(true);
    }

    public void SetCoordinates(int row, int column)
    {
        Row = row;
        Column = column;
    }

    public void SetContent(
        string familyId,
        string displayedLetter,
        Color familyColor,
        bool isTargetFamily)
    {
        SetVisualAlpha(1f);

        FamilyId = familyId;
        IsTargetFamily = isTargetFamily;
        DisplayedLetter = displayedLetter;

        if (letterText != null)
        {
            letterText.text = displayedLetter;

            letterText.fontStyle = isTargetFamily
                ? FontStyles.Bold
                : FontStyles.Normal;

            letterText.fontSize = isTargetFamily
                ? 62f
                : 58f;
        }

        if (tileImage != null)
            tileImage.color = familyColor;

        if (targetRing != null)
            targetRing.gameObject.SetActive(isTargetFamily);
    }

    public void SetSelected(bool selected)
    {
        transform.localScale = selected
            ? Vector3.one * 1.08f
            : Vector3.one;
    }

    public void SetInteractable(bool interactable)
    {
        if (button != null)
            button.interactable = interactable;
    }

    public void ShowMatchFeedback()
    {
        if (matchOutline != null)
            matchOutline.enabled = true;

        transform.localScale =
            Vector3.one * 1.12f;
    }

    public void HideMatchFeedback()
    {
        if (matchOutline != null)
            matchOutline.enabled = false;

        transform.localScale = Vector3.one;
    }

    public void SetVisualAlpha(float alpha)
    {
        if (tileCanvasGroup != null)
        {
            tileCanvasGroup.alpha =
                Mathf.Clamp01(alpha);
        }
    }

    /// <summary>
    /// Automatically previews a drag between this tile and a
    /// neighboring tile without changing the board data.
    /// </summary>
    public void PlayTutorialDragPreview(
        Match3TileView destinationTile,
        Action onComplete = null)
    {
        if (destinationTile == null ||
            destinationTile == this ||
            tileRect == null)
        {
            onComplete?.Invoke();
            return;
        }

        if (tutorialPreviewRoutine != null)
        {
            StopCoroutine(tutorialPreviewRoutine);
            tutorialPreviewRoutine = null;
        }

        tutorialPreviewRoutine = StartCoroutine(
            TutorialDragPreviewRoutine(
                destinationTile,
                onComplete
            )
        );
    }

    /// <summary>
    /// Moves both tutorial tiles toward each other's positions,
    /// highlights the matching tile, and restores the layout.
    /// </summary>
    private IEnumerator TutorialDragPreviewRoutine(
        Match3TileView destinationTile,
        Action onComplete)
    {
        RectTransform destinationRect =
            destinationTile.transform as RectTransform;

        if (destinationRect == null)
        {
            tutorialPreviewRoutine = null;
            onComplete?.Invoke();
            yield break;
        }

        if (parentGridLayout != null)
            parentGridLayout.enabled = false;

        Vector2 sourcePosition =
            tileRect.anchoredPosition;

        Vector2 destinationPosition =
            destinationRect.anchoredPosition;

        for (int repeat = 0;
             repeat < tutorialRepeatCount;
             repeat++)
        {
            ShowValidSwapHint();

            float elapsed = 0f;

            while (elapsed < tutorialDragDuration)
            {
                elapsed += Time.unscaledDeltaTime;

                float progress = Mathf.Clamp01(
                    elapsed / tutorialDragDuration
                );

                float smoothProgress = Mathf.SmoothStep(
                    0f,
                    1f,
                    progress
                );

                tileRect.anchoredPosition = Vector2.Lerp(
                    sourcePosition,
                    destinationPosition,
                    smoothProgress
                );

                destinationRect.anchoredPosition = Vector2.Lerp(
                    destinationPosition,
                    sourcePosition,
                    smoothProgress
                );

                yield return null;
            }

            elapsed = 0f;

            while (elapsed < tutorialReturnDuration)
            {
                elapsed += Time.unscaledDeltaTime;

                float progress = Mathf.Clamp01(
                    elapsed / tutorialReturnDuration
                );

                float smoothProgress = Mathf.SmoothStep(
                    0f,
                    1f,
                    progress
                );

                tileRect.anchoredPosition = Vector2.Lerp(
                    destinationPosition,
                    sourcePosition,
                    smoothProgress
                );

                destinationRect.anchoredPosition = Vector2.Lerp(
                    sourcePosition,
                    destinationPosition,
                    smoothProgress
                );

                yield return null;
            }

            tileRect.anchoredPosition = sourcePosition;
            destinationRect.anchoredPosition =
                destinationPosition;

            HideValidSwapHint();

            if (repeat < tutorialRepeatCount - 1 &&
                tutorialRepeatPause > 0f)
            {
                yield return new WaitForSecondsRealtime(
                    tutorialRepeatPause
                );
            }
        }

        if (parentGridLayout != null)
        {
            parentGridLayout.enabled = true;

            if (gridRect != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(
                    gridRect
                );
            }
        }

        tutorialPreviewRoutine = null;
        onComplete?.Invoke();
    }

    /// <summary>
    /// Stores the pointer position when the tile is pressed.
    /// </summary>
    public void OnPointerDown(PointerEventData eventData)
    {
        if ((button != null && !button.interactable) || isReturningFromInvalidDrag)
        {
            return;
        }

        interactionStartedHandler?.Invoke();

        if (parentGridLayout != null)
            parentGridLayout.enabled = false;

        dragStartPosition = eventData.position;
        if (gridRect != null)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                gridRect,
                eventData.position,
                eventData.pressEventCamera,
                out dragStartLocalPosition
            );
        }
        suppressNextClick = false;

        if (tileRect != null)
            originalAnchoredPosition = tileRect.anchoredPosition;

        transform.localScale = Vector3.one * 1.08f;
    }

    /// <summary>
    /// Previews a possible swap by moving both tiles toward
    /// each other's positions without changing the board data.
    /// </summary>
    public void OnDrag(PointerEventData eventData)
    {
        if ((button != null && !button.interactable) ||
     isReturningFromInvalidDrag)
        {
            return;
        }

        if (tileRect == null ||
            neighborProvider == null)
        {
            return;
        }

        Vector2 dragDelta =
            eventData.position - dragStartPosition;

        if (dragDelta.sqrMagnitude <= 0f)
            return;

        Vector2Int direction;

        if (Mathf.Abs(dragDelta.x) >
            Mathf.Abs(dragDelta.y))
        {
            direction = dragDelta.x > 0f
                ? Vector2Int.right
                : Vector2Int.left;
        }
        else
        {
            direction = dragDelta.y > 0f
                ? Vector2Int.up
                : Vector2Int.down;
        }

        Match3TileView neighbor =
            neighborProvider(this, direction);

        if (neighbor == null)
        {
            ResetDragPreview();
            transform.localScale = Vector3.one * 1.08f;
            return;
        }

        if (previewNeighbor != null)
            previewNeighbor.HideValidSwapHint();

        if (previewNeighbor != neighbor)
        {
            if (previewNeighborRect != null)
            {
                previewNeighborRect.anchoredPosition =
                    previewNeighborOriginalPosition;
            }

            previewNeighbor = neighbor;
            previewNeighborRect =
                neighbor.transform as RectTransform;

            if (previewNeighborRect != null)
            {
                previewNeighborOriginalPosition =
                    previewNeighborRect.anchoredPosition;
            }
        }

        if (previewNeighborRect == null)
            return;

        Match3TileView matchingTile =
    matchTileProvider != null
        ? matchTileProvider.Invoke(this, direction)
        : null;

        // Clear the previous preview highlights first.
        HideValidSwapHint();
        previewNeighbor.HideValidSwapHint();

        // Keep the dragged tile visibly raised.
        transform.localScale = Vector3.one * 1.08f;

        // Highlight only the tile that will join the match.
        if (matchingTile != null)
            matchingTile.ShowValidSwapHint();

        float tileDistance = Vector2.Distance(
            originalAnchoredPosition,
            previewNeighborOriginalPosition
        );

        Vector2 currentLocalPosition =
     dragStartLocalPosition;

        if (gridRect != null)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                gridRect,
                eventData.position,
                eventData.pressEventCamera,
                out currentLocalPosition
            );
        }

        Vector2 localDragDelta =
            currentLocalPosition - dragStartLocalPosition;

        float dragDistance =
            Mathf.Abs(direction.x) > 0
                ? Mathf.Abs(localDragDelta.x)
                : Mathf.Abs(localDragDelta.y);

        float progress = Mathf.Clamp01(
            dragDistance / Mathf.Max(tileDistance, 1f)
        );

        tileRect.anchoredPosition = Vector2.Lerp(
            originalAnchoredPosition,
            previewNeighborOriginalPosition,
            progress
        );

        previewNeighborRect.anchoredPosition = Vector2.Lerp(
            previewNeighborOriginalPosition,
            originalAnchoredPosition,
            progress
        );
    }

    /// <summary>
    /// Completes a valid swap or smoothly restores an invalid
    /// drag preview to its original positions.
    /// </summary>
    public void OnPointerUp(PointerEventData eventData)
    {
        if ((button != null && !button.interactable) ||
            isReturningFromInvalidDrag)
        {
            return;
        }

        Vector2 dragDelta =
            eventData.position - dragStartPosition;

        if (dragDelta.magnitude < minimumDragDistance)
        {
            ResetDragPreview();
            return;
        }

        Vector2Int direction;

        if (Mathf.Abs(dragDelta.x) >
            Mathf.Abs(dragDelta.y))
        {
            direction = dragDelta.x > 0f
                ? Vector2Int.right
                : Vector2Int.left;
        }
        else
        {
            direction = dragDelta.y > 0f
                ? Vector2Int.up
                : Vector2Int.down;
        }

        suppressNextClick = true;

        Match3TileView matchingTile =
    matchTileProvider != null
        ? matchTileProvider.Invoke(this, direction)
        : null;

        bool previewedAsValid =
            matchingTile != null;

        // Clear the drag hint before Match feedback is applied.
        if (previewedAsValid)
            ResetDragPreview();

        bool swapAccepted =
            dragHandler != null &&
            dragHandler.Invoke(this, direction);

        if (!swapAccepted)
        {
            if (!previewedAsValid)
            {
                StartCoroutine(
                    AnimateInvalidDragReturn()
                );
            }
        }
        else if (!previewedAsValid)
        {
            ResetDragPreview();
        }

        StartCoroutine(ResetClickSuppression());
    }

    /// <summary>
    /// Smoothly returns both preview tiles after an invalid swap.
    /// </summary>
    private IEnumerator AnimateInvalidDragReturn()
    {
        isReturningFromInvalidDrag = true;

        Vector2 tileStartPosition =
            tileRect != null
                ? tileRect.anchoredPosition
                : originalAnchoredPosition;

        RectTransform neighborRect =
            previewNeighborRect;

        Vector2 neighborStartPosition =
            neighborRect != null
                ? neighborRect.anchoredPosition
                : previewNeighborOriginalPosition;

        float elapsed = 0f;

        while (elapsed < invalidDragReturnDuration)
        {
            elapsed += Time.unscaledDeltaTime;

            float progress = Mathf.Clamp01(
                elapsed / invalidDragReturnDuration
            );

            float smoothProgress = Mathf.SmoothStep(
                0f,
                1f,
                progress
            );

            if (tileRect != null)
            {
                tileRect.anchoredPosition = Vector2.Lerp(
                    tileStartPosition,
                    originalAnchoredPosition,
                    smoothProgress
                );
            }

            if (neighborRect != null)
            {
                neighborRect.anchoredPosition = Vector2.Lerp(
                    neighborStartPosition,
                    previewNeighborOriginalPosition,
                    smoothProgress
                );
            }

            yield return null;
        }

        ResetDragPreview();
        isReturningFromInvalidDrag = false;
    }

    /// <summary>
    /// Restores both tiles after a drag preview ends.
    /// </summary>
    private void ResetDragPreview()
    {
        if (tileRect != null)
        {
            tileRect.anchoredPosition =
                originalAnchoredPosition;
        }

        if (previewNeighborRect != null)
        {
            previewNeighborRect.anchoredPosition = previewNeighborOriginalPosition;
        }

        HideValidSwapHint();

        if (previewNeighbor != null)
            previewNeighbor.HideValidSwapHint();

        previewNeighbor = null;
        previewNeighborRect = null;
        transform.localScale = Vector3.one;
        if (parentGridLayout != null)
        {
            parentGridLayout.enabled = true;

            if (gridRect != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(
                    gridRect
                );
            }
        }
    }

    /// <summary>
    /// Prevents the Button click event from firing after a drag.
    /// </summary>
    private IEnumerator ResetClickSuppression()
    {
        yield return null;
        suppressNextClick = false;
    }

    private void HandleClicked()
    {
        if (suppressNextClick)
            return;

        clickHandler?.Invoke(this);
    }

    private void OnDestroy()
    {
        if (button != null)
            button.onClick.RemoveListener(HandleClicked);
    }

    /// <summary>
    /// Highlights a neighboring tile that can create a valid match.
    /// </summary>
    public void ShowValidSwapHint()
    {
        transform.localScale = Vector3.one * 1.15f;

        if (matchOutline != null)
            matchOutline.enabled = true;
    }

    /// <summary>
    /// Removes the valid-swap preview from this tile.
    /// </summary>
    public void HideValidSwapHint()
    {
        transform.localScale = Vector3.one;

        if (matchOutline != null)
            matchOutline.enabled = false;
    }
}