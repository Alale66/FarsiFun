using System;
using TMPro;
using UnityEngine;

public class FallingLetter : MonoBehaviour
{
    [SerializeField] private TMP_Text letterText;

    private RectTransform rectTransform;
    private RectTransform playArea;
    private RectTransform catcher;

    private Action<FallingLetter> caughtCallback;
    private Action<FallingLetter> missedCallback;

    private float fallSpeed;
    private bool isResolved;

    public string LetterValue { get; private set; }

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        
        if (letterText != null)
            letterText.raycastTarget = false;
    }

    public void Initialize(
        string letter,
        float speed,
        RectTransform area,
        RectTransform catcherTransform,
        Action<FallingLetter> onCaught,
        Action<FallingLetter> onMissed)
    {
        LetterValue = letter;
        fallSpeed = speed;
        playArea = area;
        catcher = catcherTransform;
        caughtCallback = onCaught;
        missedCallback = onMissed;

        isResolved = false;

        if (letterText != null)
            letterText.text = letter;
    }

    private void Update()
    {
        if (isResolved ||
            rectTransform == null ||
            playArea == null ||
            catcher == null)
        {
            return;
        }

        rectTransform.anchoredPosition +=
            Vector2.down * fallSpeed * Time.deltaTime;

        Bounds letterBounds =
            RectTransformUtility.CalculateRelativeRectTransformBounds(
                playArea,
                rectTransform
            );

        Bounds catcherBounds =
            RectTransformUtility.CalculateRelativeRectTransformBounds(
                playArea,
                catcher
            );

        if (letterBounds.Intersects(catcherBounds))
        {
            Resolve(caughtCallback);
            return;
        }

        if (letterBounds.max.y < playArea.rect.yMin)
        {
            Resolve(missedCallback);
        }
    }

    private void Resolve(Action<FallingLetter> callback)
    {
        if (isResolved)
            return;

        isResolved = true;

        callback?.Invoke(this);

        Destroy(gameObject);
    }
}