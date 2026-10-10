using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class LetterCannonTargetView : MonoBehaviour
{
    [Header("UI")]
    [Tooltip("Displays the letter assigned to this target.")]
    [SerializeField] private TMP_Text letterText;

    [Tooltip("Receives the child's target selection.")]
    [SerializeField] private Button button;

    [Tooltip("Shows a red halo after an incorrect selection.")]
    [SerializeField] private Outline wrongOutline;

    public int TargetIndex { get; private set; }
    public string DisplayedLetter { get; private set; }

    public RectTransform TargetRect { get; private set; }

    private Action<LetterCannonTargetView> selectedHandler;

    private void Awake()
    {
        TargetRect = transform as RectTransform;

        if (button == null)
            button = GetComponent<Button>();

        if (wrongOutline == null)
            wrongOutline = GetComponent<Outline>();

        ShowWrongOutline(false);
    }

    /// <summary>
    /// Connects this target to the Letter Cannon game.
    /// </summary>
    public void Initialize(
        int targetIndex,
        Action<LetterCannonTargetView> onSelected)
    {
        TargetIndex = targetIndex;
        selectedHandler = onSelected;

        if (button != null)
        {
            button.onClick.RemoveListener(
                HandleSelected
            );

            button.onClick.AddListener(
                HandleSelected
            );
        }

        ShowWrongOutline(false);
        SetInteractable(true);
    }

    /// <summary>
    /// Updates the letter displayed inside the target.
    /// </summary>
    public void SetLetter(string letter)
    {
        DisplayedLetter = letter ?? string.Empty;

        if (letterText != null)
            letterText.text = DisplayedLetter;

        ShowWrongOutline(false);
    }

    /// <summary>
    /// Enables or disables target selection.
    /// </summary>
    public void SetInteractable(bool interactable)
    {
        if (button != null)
            button.interactable = interactable;
    }

    /// <summary>
    /// Shows or hides the incorrect-answer halo.
    /// </summary>
    public void ShowWrongOutline(bool visible)
    {
        if (wrongOutline != null)
            wrongOutline.enabled = visible;
    }

    private void HandleSelected()
    {
        if (button != null &&
            !button.interactable)
        {
            return;
        }

        selectedHandler?.Invoke(this);
    }

    private void OnDestroy()
    {
        if (button != null)
        {
            button.onClick.RemoveListener(
                HandleSelected
            );
        }
    }
}