using TMPro;
using UnityEngine;

public class LetterFormSlot : MonoBehaviour
{
    [SerializeField] private TMP_Text baseText;
    [SerializeField] private TMP_Text markText;

    public void SetForm(
        string baseValue,
        string markValue = "")
    {
        string combinedText =
            (baseValue ?? string.Empty) +
            (markValue ?? string.Empty);

        if (baseText != null)
        {
            baseText.richText = true;
            baseText.text =
                PersianLetterDisplay.GetText(combinedText);
        }

        if (markText != null)
        {
            markText.text = string.Empty;
            markText.gameObject.SetActive(false);
        }

        gameObject.SetActive(true);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }
}