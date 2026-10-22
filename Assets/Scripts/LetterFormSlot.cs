using TMPro;
using UnityEngine;

public class LetterFormSlot : MonoBehaviour
{
    [SerializeField] private TMP_Text baseText;
    [SerializeField] private TMP_Text markText;

    [Header("Notebook Line")]
    [SerializeField] private RectTransform baselineLine;

    [Header("Mark Spacing")]
    [SerializeField]
    private float markGapEm = 0.02f;

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

        RectTransform textRect = baseText.rectTransform;

        baseText.ForceMeshUpdate();

        Vector3 lineWorldPosition =
            baselineLine.TransformPoint(
                baselineLine.rect.center
            );

        bool hasBase =
            !string.IsNullOrWhiteSpace(currentBase);

        float baseline = 0f;

        if (hasBase &&
            baseText.textInfo.characterCount > 0)
        {
            baseline =
                baseText.textInfo.characterInfo[0].baseLine;

            // Move along the text's local Y axis so its
            // typographic baseline meets the notebook line.
            float lineLocalY =
                textRect.InverseTransformPoint(
                    lineWorldPosition
                ).y;

            textRect.position += textRect.TransformVector(
                new Vector3(
                    0f,
                    lineLocalY - baseline,
                    0f
                )
            );
        }

        if (markText == null ||
            !markText.gameObject.activeInHierarchy)
        {
            return;
        }

        markText.ForceMeshUpdate();

        if (markText.textInfo.characterCount == 0)
            return;

        bool isLowerMark =
            currentMark.Contains("\u0650");

        Bounds baseBounds = baseText.textBounds;
        Bounds markBounds = markText.textBounds;

        float fontSize = baseText.fontSize;
        float gap = fontSize * markGapEm;

        float centerX = hasBase
            ? baseBounds.center.x
            : textRect.rect.center.x;

        float targetEdgeY;

        if (hasBase)
        {
            targetEdgeY = isLowerMark
                ? baseBounds.min.y - gap
                : baseBounds.max.y + gap;
        }
        else
        {
            float lineLocalY =
                textRect.InverseTransformPoint(
                    lineWorldPosition
                ).y;

            float distance =
                fontSize * standaloneMarkDistanceEm;

            targetEdgeY = isLowerMark
                ? lineLocalY - distance
                : lineLocalY + distance;
        }

        Vector3 desiredEdgeWorld =
            textRect.TransformPoint(
                new Vector3(
                    centerX,
                    targetEdgeY,
                    0f
                )
            );

        Vector3 currentEdgeWorld =
            markText.rectTransform.TransformPoint(
                new Vector3(
                    markBounds.center.x,
                    isLowerMark
                        ? markBounds.max.y
                        : markBounds.min.y,
                    0f
                )
            );

        markText.rectTransform.position +=
            desiredEdgeWorld - currentEdgeWorld;
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }
}