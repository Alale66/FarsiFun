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

    public void Setup(string newLetter, Action<LetterCoinView> callback)
    {
        letter = newLetter;
        onSelected = callback;

        letterText.text = letter;
        button.interactable = true;

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => onSelected?.Invoke(this));
    }

    public void SetInteractable(bool value)
    {
        button.interactable = value;
    }
}