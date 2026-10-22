using TMPro;
using UnityEngine;

public class TMPBaselineAligner : MonoBehaviour
{
    [SerializeField] private TMP_Text textComponent;
    [SerializeField] private RectTransform baselineLine;

    private void Awake()
    {
        if (textComponent == null)
            textComponent = GetComponent<TMP_Text>();
    }

    private void LateUpdate()
    {
        if (textComponent == null ||
            baselineLine == null)
        {
            return;
        }

        textComponent.ForceMeshUpdate();

        TMP_TextInfo info = textComponent.textInfo;

        if (info.characterCount == 0 ||
            info.lineCount == 0)
        {
            return;
        }

        RectTransform textRect =
            textComponent.rectTransform;

        Vector3 lineWorldPosition =
            baselineLine.TransformPoint(
                baselineLine.rect.center
            );

        float targetY = textRect.InverseTransformPoint(
            lineWorldPosition
        ).y;

        float currentBaseline =
            info.lineInfo[0].baseline;

        textRect.position += textRect.TransformVector(
            new Vector3(
                0f,
                targetY - currentBaseline,
                0f
            )
        );
    }
}