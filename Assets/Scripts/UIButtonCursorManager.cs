using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class UIButtonCursorManager : MonoBehaviour
{
    [Header("Cursor")]
    [SerializeField] private Texture2D handCursor;
    [SerializeField] private Vector2 hotspot = new Vector2(6f, 1f);

    private readonly List<RaycastResult> raycastResults =
        new List<RaycastResult>();

    private EventSystem cachedEventSystem;
    private PointerEventData pointerEventData;
    private bool handCursorIsActive;

    private void Update()
    {
#if UNITY_EDITOR || UNITY_STANDALONE || UNITY_WEBGL
        SetHandCursor(IsOverClickableButton());
#endif
    }

    private bool IsOverClickableButton()
    {
        if (handCursor == null || EventSystem.current == null)
            return false;

        if (cachedEventSystem != EventSystem.current ||
            pointerEventData == null)
        {
            cachedEventSystem = EventSystem.current;
            pointerEventData =
                new PointerEventData(cachedEventSystem);
        }

        pointerEventData.Reset();
        if (Mouse.current == null)
            return false;

        pointerEventData.position =
            Mouse.current.position.ReadValue();

        raycastResults.Clear();
        cachedEventSystem.RaycastAll(
            pointerEventData,
            raycastResults
        );

        if (raycastResults.Count == 0)
            return false;

        GameObject topObject =
            raycastResults[0].gameObject;

        Button button =
            topObject.GetComponentInParent<Button>();

        return button != null &&
               button.enabled &&
               button.gameObject.activeInHierarchy &&
               button.IsInteractable();
    }

    private void SetHandCursor(bool useHandCursor)
    {
        if (handCursorIsActive == useHandCursor)
            return;

        handCursorIsActive = useHandCursor;

        Cursor.SetCursor(
            useHandCursor ? handCursor : null,
            useHandCursor ? hotspot : Vector2.zero,
            CursorMode.Auto
        );
    }

    private void OnDisable()
    {
        ResetCursor();
    }

    private void OnDestroy()
    {
        ResetCursor();
    }

    private void ResetCursor()
    {
        handCursorIsActive = false;
        Cursor.SetCursor(
            null,
            Vector2.zero,
            CursorMode.Auto
        );
    }
}