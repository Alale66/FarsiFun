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
        PersianLetterDisplay.AlignToBaseline(
            textComponent,
            baselineLine
        );
    }
}