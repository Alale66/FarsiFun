using UnityEngine;
using UnityEngine.EventSystems;

public class LetterCatchPlayer : MonoBehaviour,
    IPointerDownHandler,
    IDragHandler
{
    [SerializeField] private RectTransform playArea;
    [SerializeField] private RectTransform catcher;

    public void OnPointerDown(PointerEventData eventData)
    {
        MoveCatcher(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        MoveCatcher(eventData);
    }

    private void MoveCatcher(PointerEventData eventData)
    {
        if (playArea == null || catcher == null)
            return;

        bool hasLocalPoint =
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                playArea,
                eventData.position,
                eventData.pressEventCamera,
                out Vector2 localPoint
            );

        if (!hasLocalPoint)
            return;

        float halfCatcherWidth =
            catcher.rect.width * catcher.localScale.x * 0.5f;

        float minimumX =
            playArea.rect.xMin + halfCatcherWidth;

        float maximumX =
            playArea.rect.xMax - halfCatcherWidth;

        float clampedX = Mathf.Clamp(
            localPoint.x,
            minimumX,
            maximumX
        );

        catcher.anchoredPosition = new Vector2(
            clampedX,
            catcher.anchoredPosition.y
        );
    }

    public void ResetPosition()
    {
        if (catcher == null)
            return;

        catcher.anchoredPosition = new Vector2(
            0f,
            catcher.anchoredPosition.y
        );
    }
}