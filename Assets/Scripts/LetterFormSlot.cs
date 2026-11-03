using TMPro;
using UnityEngine;

public class LetterFormSlot : MonoBehaviour
{
    [SerializeField] private TMP_Text baseText;
    [SerializeField] private TMP_Text markText;

    [Header("Notebook Line")]
    [SerializeField] private RectTransform baselineLine;

    [Header("Mark Spacing")]
    [SerializeField] private float markGapEm = 0.02f;

    [SerializeField]
    private float standaloneMarkDistanceEm = 0.08f;

    private string currentBase = string.Empty;
    private string currentMark = string.Empty;

    public void SetForm(
        string baseValue,
        string markValue = "")
    {
        currentBase = baseValue ?? string.Empty;
        currentMark = markValue ?? string.Empty;

        if (baseText != null)
        {
            baseText.richText = true;
            baseText.text =
                PersianLetterDisplay.GetText(currentBase);
        }

        if (markText != null)
        {
            markText.richText = false;
            markText.text = currentMark;

            markText.gameObject.SetActive(
                !string.IsNullOrEmpty(currentMark)
            );
        }

        gameObject.SetActive(true);
    }

    private void LateUpdate()
    {
        if (baseText == null || baselineLine == null)
            return;

        bool hasBase =
            !string.IsNullOrWhiteSpace(currentBase);

        if (hasBase)
        {
            PersianLetterDisplay.AlignToBaseline(
                baseText,
                baselineLine
            );
        }

        PersianLetterDisplay.PositionSeparateMark(
            baseText,
            markText,
            baselineLine,
            hasBase,
            currentMark.Contains("\u0650"),
            markGapEm,
            standaloneMarkDistanceEm
        );
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }
}