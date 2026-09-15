using TMPro;
using UnityEngine;

public class LetterFormSlot : MonoBehaviour
{
    [SerializeField] private TMP_Text baseText;
    [SerializeField] private TMP_Text markText;

    public void SetForm(string baseValue, string markValue = "")
    {
        baseText.text = baseValue;

        if (markText != null)
        {
            bool hasMark = !string.IsNullOrEmpty(markValue);

            markText.gameObject.SetActive(hasMark);

            if (hasMark)
                markText.text = markValue;
        }

        gameObject.SetActive(true);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }
}