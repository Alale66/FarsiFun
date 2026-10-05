using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Match3TileView : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_Text letterText;
    [SerializeField] private Image tileImage;
    [SerializeField] private Image targetRing;
    [SerializeField] private Button button;
    [SerializeField] private Outline matchOutline;
    [SerializeField] private CanvasGroup tileCanvasGroup;

    public int Row { get; private set; }
    public int Column { get; private set; }

    public string FamilyId { get; private set; }
    public bool IsTargetFamily { get; private set; }

    private Action<Match3TileView> clickHandler;

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
    }

    public void Initialize(
        int row,
        int column,
        Action<Match3TileView> onClicked)
    {
        Row = row;
        Column = column;
        clickHandler = onClicked;

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
    private void HandleClicked()
    {
        clickHandler?.Invoke(this);
    }

    private void OnDestroy()
    {
        if (button != null)
            button.onClick.RemoveListener(HandleClicked);
    }
}