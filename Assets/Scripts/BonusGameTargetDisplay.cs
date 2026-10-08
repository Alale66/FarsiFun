using TMPro;
using UnityEngine;

public class BonusGameTargetDisplay : MonoBehaviour
{
    [Header("UI")]
    [Tooltip("Displays all valid forms of the current target letter.")]
    [SerializeField] private TMP_Text targetLetterText;

    /// <summary>
    /// Displays the shared target label for the selected letter.
    /// </summary>
    public void Show(LetterData letterData)
    {
        if (letterData == null)
        {
            Hide();
            return;
        }

        if (targetLetterText != null)
        {
            targetLetterText.text =
                letterData.GetGameDisplayText();
        }

        gameObject.SetActive(true);
    }

    /// <summary>
    /// Hides the shared target display.
    /// </summary>
    public void Hide()
    {
        gameObject.SetActive(false);
    }
}