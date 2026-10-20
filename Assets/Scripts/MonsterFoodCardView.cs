using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasGroup))]
public class MonsterFoodCardView : MonoBehaviour,
    IBeginDragHandler,
    IDragHandler,
    IEndDragHandler
{
    [SerializeField] private Image picture;

    private RectTransform cardRect;
    private RectTransform dragArea;
    private CanvasGroup canvasGroup;

    private Vector3 homePosition;
    private Vector3 homeScale;
    private Vector2 pointerOffset;
    private int homeSiblingIndex;
    private int activePointerId;

    private bool interactable;
    private bool dragging;

    private Func<bool> canDrag;
    private Action<MonsterFoodCardView, PointerEventData> dropped;
    private Outline wrongOutline;
    private CardOuterBorder outerBorder;
    private Image wrongOverlay;
    private Quaternion homeRotation;
    private float idleTime;
    private float idlePhase;

    public ExampleData Example { get; private set; }
    public bool IsCorrect { get; private set; }
    public bool IsDragging => dragging;
    public RectTransform CardRect => cardRect;

    private void Awake()
    {
        cardRect = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();

        wrongOutline = GetComponent<Outline>();

        if (wrongOutline == null)
            wrongOutline = gameObject.AddComponent<Outline>();

        wrongOutline.effectColor = new Color32(201, 68, 58, 255);
        wrongOutline.effectDistance = new Vector2(5f, -5f);
        wrongOutline.useGraphicAlpha = true;
        wrongOutline.enabled = false;
    }

    public void Setup(
        ExampleData example,
        bool isCorrect,
        Func<bool> canDragNow,
        Action<MonsterFoodCardView, PointerEventData> onDropped)
    {
        ShowWrongOutline(false);
        if (cardRect == null)
            Awake();

        Example = example;
        IsCorrect = isCorrect;
        canDrag = canDragNow;
        dropped = onDropped;

        dragArea = cardRect.parent as RectTransform;
        homePosition = cardRect.localPosition;
        homeScale = cardRect.localScale;

        homeRotation = cardRect.localRotation;
        idleTime = 0f;

        // Different timing for each card.
        idlePhase = UnityEngine.Random.Range(0f, Mathf.PI * 2f);

        if (picture != null)
        {
            picture.sprite = example != null
                ? example.image
                : null;

            picture.preserveAspect = true;
            picture.raycastTarget = false;
        }

        gameObject.SetActive(true);
        SetInteractable(true);
    }

    public void SetInteractable(bool value)
    {
        interactable = value;

        if (canvasGroup != null)
        {
            canvasGroup.interactable = value;
            canvasGroup.blocksRaycasts = value && !dragging;
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

        dragging = true;
        activePointerId = eventData.pointerId;
        homeSiblingIndex = transform.GetSiblingIndex();

        pointerOffset =
            (Vector2)cardRect.localPosition - pointerPosition;

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

            cardRect.localPosition = new Vector3(
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

        dragging = false;
        canvasGroup.blocksRaycasts = interactable;

        if (canDrag != null && !canDrag())
        {
            ReturnHome();
            return;
        }

        if (dropped != null)
            dropped.Invoke(this, eventData);
        else
            ReturnHome();
    }

    public void ReturnHome()
    {
        ShowWrongOutline(false);
        dragging = false;

        cardRect.localPosition = homePosition;
        cardRect.localScale = homeScale;
        cardRect.localRotation = homeRotation;

        transform.SetSiblingIndex(homeSiblingIndex);
        SetInteractable(true);
    }

    private void OnDisable()
    {
        if (!dragging)
            return;

        dragging = false;

        if (cardRect != null)
        {
            cardRect.localRotation = homeRotation;
            cardRect.localPosition = homePosition;
            cardRect.localScale = homeScale;
            transform.SetSiblingIndex(homeSiblingIndex);
        }

        if (canvasGroup != null)
            canvasGroup.blocksRaycasts = interactable;
    }

    public void ShowWrongOutline(bool visible)
    {
        ShowWrongOverlay(visible);

        if (wrongOutline != null)
            wrongOutline.enabled = false;

        if (outerBorder == null && visible)
        {
            GameObject borderObject = new GameObject(
                "WrongOuterBorder",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(CardOuterBorder)
            );

            borderObject.transform.SetParent(transform, false);

            RectTransform borderRect =
                borderObject.GetComponent<RectTransform>();

            borderRect.anchorMin = Vector2.zero;
            borderRect.anchorMax = Vector2.one;

            borderRect.offsetMin = new Vector2(18f, 18f);
            borderRect.offsetMax = new Vector2(-18f, -18f);

            outerBorder = borderObject.GetComponent<CardOuterBorder>();
            outerBorder.color = new Color32(230, 65, 55, 255);
            outerBorder.raycastTarget = false;
        }

        if (outerBorder != null)
            outerBorder.gameObject.SetActive(visible);
    }

    private void ShowWrongOverlay(bool visible)
    {
        if (wrongOverlay == null && visible)
        {
            Image cardImage = GetComponent<Image>();

            GameObject overlayObject = new GameObject(
                "WrongOverlay",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image)
            );

            overlayObject.transform.SetParent(transform, false);
            overlayObject.transform.SetAsLastSibling();

            RectTransform rect =
                overlayObject.GetComponent<RectTransform>();

            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            wrongOverlay = overlayObject.GetComponent<Image>();

            wrongOverlay.sprite = cardImage.sprite;
            wrongOverlay.type = cardImage.type;
            wrongOverlay.preserveAspect = cardImage.preserveAspect;
            wrongOverlay.color = new Color32(255, 65, 55, 65);
            wrongOverlay.raycastTarget = false;
        }

        if (wrongOverlay != null)
            wrongOverlay.gameObject.SetActive(visible);
    }

    private void Update()
    {
        if (cardRect == null ||
            dragging ||
            !interactable ||
            canDrag == null ||
            !canDrag())
        {
            return;
        }

        idleTime += Time.deltaTime;

        float wave = idleTime * 1.6f + idlePhase;

        cardRect.localPosition =
            homePosition +
            Vector3.up * Mathf.Sin(wave) * 3f;

        cardRect.localRotation =
            homeRotation *
            Quaternion.Euler(
                0f,
                0f,
                Mathf.Sin(wave * 0.8f) * 1.2f
            );
    }
}