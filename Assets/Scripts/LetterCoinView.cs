using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LetterCoinView : MonoBehaviour
{
    [SerializeField] private TMP_Text letterText;
    [SerializeField] private Button button;

    private string letter;
    private Action<LetterCoinView> onSelected;

    public string Letter => letter;

    public void Setup(
        string newLetter,
        Action<LetterCoinView> callback)
    {
        // Keep the original value for answer checking.
        letter = newLetter ?? string.Empty;
        onSelected = callback;

        letterText.text =
            PersianLetterDisplay.GetText(letter);

        button.interactable = true;

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(HandleSelected);
    }

    public void SetInteractable(bool value)
    {
        button.interactable = value;
    }

    private void HandleSelected()
    {
        onSelected?.Invoke(this);
    }
}