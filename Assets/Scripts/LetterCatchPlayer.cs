using UnityEngine;
using UnityEngine.EventSystems;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class LetterCatchPlayer : MonoBehaviour,
    IPointerDownHandler,
    IDragHandler
{
    [SerializeField] private RectTransform playArea;
    [SerializeField] private RectTransform catcher;

    [SerializeField, Min(1f)]
    private float keyboardMoveSpeed = 700f;

    private void Update()
    {
        float direction = GetKeyboardDirection();

        if (Mathf.Abs(direction) < 0.01f ||
            playArea == null ||
            catcher == null)
        {
            return;
        }

        float nextX =
            catcher.anchoredPosition.x +
            direction *
            keyboardMoveSpeed *
            Time.deltaTime;

        SetCatcherX(nextX);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        MoveCatcherToPointer(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        MoveCatcherToPointer(eventData);
    }

    private void MoveCatcherToPointer(
        PointerEventData eventData)
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

        SetCatcherX(localPoint.x);
    }

    private void SetCatcherX(float xPosition)
    {
        float halfCatcherWidth =
            catcher.rect.width *
            catcher.localScale.x *
            0.5f;

        float minimumX =
            playArea.rect.xMin +
            halfCatcherWidth;

        float maximumX =
            playArea.rect.xMax -
            halfCatcherWidth;

        float clampedX = Mathf.Clamp(
            xPosition,
            minimumX,
            maximumX
        );

        catcher.anchoredPosition = new Vector2(
            clampedX,
            catcher.anchoredPosition.y
        );
    }

    private float GetKeyboardDirection()
    {
#if ENABLE_INPUT_SYSTEM
        Keyboard keyboard = Keyboard.current;

        if (keyboard == null)
            return 0f;

        bool moveLeft =
            keyboard.leftArrowKey.isPressed ||
            keyboard.aKey.isPressed;

        bool moveRight =
            keyboard.rightArrowKey.isPressed ||
            keyboard.dKey.isPressed;

#elif ENABLE_LEGACY_INPUT_MANAGER
        bool moveLeft =
            Input.GetKey(KeyCode.LeftArrow) ||
            Input.GetKey(KeyCode.A);

        bool moveRight =
            Input.GetKey(KeyCode.RightArrow) ||
            Input.GetKey(KeyCode.D);

#else
        bool moveLeft = false;
        bool moveRight = false;
#endif

        if (moveLeft == moveRight)
            return 0f;

        return moveLeft ? -1f : 1f;
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