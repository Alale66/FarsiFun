using System;
using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasGroup))]
public class WordCompletePiece : MonoBehaviour,
    IBeginDragHandler,
    IDragHandler,
    IEndDragHandler
{
    [Header("Idle Movement")]
    [SerializeField] private float floatDistance = 7f;
    [SerializeField] private float floatSpeed = 1.8f;

    private bool homeReady;
    private RectTransform pieceRect;
    private RectTransform dragArea;
    private CanvasGroup canvasGroup;

    private Vector3 homePosition;
    private Vector2 pointerOffset;

    private int homeSiblingIndex;
    private int activePointerId;

    private bool interactable;
    private bool dragging;

    private Func<bool> canDrag;
    private Action<Vector2, Camera> dropped;
    private Outline wrongOutline;
    private Coroutine wrongFeedbackRoutine;

    public bool IsDragging => dragging;
    public event Action DragStarted;

    private void Awake()
    {
        pieceRect = transform as RectTransform;
        canvasGroup = GetComponent<CanvasGroup>();
        wrongOutline = GetComponent<Outline>();

        if (wrongOutline == null)
            wrongOutline = gameObject.AddComponent<Outline>();

        wrongOutline.effectColor = new Color32(201, 68, 58, 255);
        wrongOutline.effectDistance = new Vector2(8f, -8f);
        wrongOutline.useGraphicAlpha = false;
        wrongOutline.enabled = false;
    }

    public void Initialize(
        Func<bool> canDragNow,
        Action<Vector2, Camera> onDropped)
    {
        ResetWrongFeedback();
        ReturnHome();

        dragArea = pieceRect.parent as RectTransform;
        homePosition = pieceRect.localPosition;
        homeReady = true;
        homeSiblingIndex = transform.GetSiblingIndex();

        canDrag = canDragNow;
        dropped = onDropped;

        SetInteractable(true);
    }

    public void SetInteractable(bool value)
    {
        interactable = value;

        if (canvasGroup != null)
        {
            canvasGroup.interactable = value;
            canvasGroup.blocksRaycasts =
                value && !dragging;
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!interactable ||
            dragging ||
            dragArea == null ||
            eventData.button != PointerEventData.InputButton.Left ||
            (canDrag != null && !canDrag()))
        {
            return;
        }

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
            dragArea,
            eventData.position,
            eventData.pressEventCamera,
            out Vector2 pointerPosition))
        {
            return;
        }

        homeSiblingIndex = transform.GetSiblingIndex();
        activePointerId = eventData.pointerId;

        pointerOffset =
            (Vector2)pieceRect.localPosition - pointerPosition;

        dragging = true;
        DragStarted?.Invoke();
        transform.SetAsLastSibling();
        canvasGroup.blocksRaycasts = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!dragging ||
            eventData.pointerId != activePointerId)
        {
            return;
        }

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            dragArea,
            eventData.position,
            eventData.pressEventCamera,
            out Vector2 pointerPosition))
        {
            Vector2 position = pointerPosition + pointerOffset;

            pieceRect.localPosition = new Vector3(
                position.x,
                position.y,
                homePosition.z
            );
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!dragging ||
            eventData.pointerId != activePointerId)
        {
            return;
        }

        bool canAcceptDrop =
            interactable &&
            (canDrag == null || canDrag());

        ReturnHome();

        if (canAcceptDrop)
        {
            dropped?.Invoke(
                eventData.position,
                eventData.pressEventCamera
            );
        }
    }

    public void ReturnHome()
    {
        bool wasDragging = dragging;
        dragging = false;

        if (pieceRect != null && homeReady)
        {
            pieceRect.localPosition = homePosition;

            if (wasDragging)
                transform.SetSiblingIndex(homeSiblingIndex);
        }

        if (canvasGroup != null)
            canvasGroup.blocksRaycasts = interactable;
    }

    private void OnDisable()
    {
        ResetWrongFeedback();
        ReturnHome();
    }

    private void LateUpdate()
    {
        if (!homeReady || pieceRect == null || dragging)
            return;

        float offset = interactable
            ? Mathf.Sin(Time.unscaledTime * floatSpeed) * floatDistance
            : 0f;

        pieceRect.localPosition =
            homePosition + Vector3.up * offset;
    }

    public void ShowWrongFeedback()
    {
        ResetWrongFeedback();

        wrongFeedbackRoutine =
            StartCoroutine(FlashWrongOutline());
    }

    private IEnumerator FlashWrongOutline()
    {
        if (wrongOutline == null)
            yield break;

        wrongOutline.enabled = true;

        yield return new WaitForSecondsRealtime(0.5f);

        wrongOutline.enabled = false;
        wrongFeedbackRoutine = null;
    }

    private void ResetWrongFeedback()
    {
        if (wrongFeedbackRoutine != null)
        {
            StopCoroutine(wrongFeedbackRoutine);
            wrongFeedbackRoutine = null;
        }

        if (wrongOutline != null)
            wrongOutline.enabled = false;
    }
}