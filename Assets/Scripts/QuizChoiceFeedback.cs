using UnityEngine;
using UnityEngine.UI;

public class QuizChoiceFeedback : MonoBehaviour
{
    [SerializeField] private Image checkMark;

    private Outline outline;

    private void Awake()
    {
        Image cardImage = GetComponent<Image>();

        outline = cardImage.GetComponent<Outline>();

        if (outline == null)
            outline = gameObject.AddComponent<Outline>();

        outline.effectDistance = new Vector2(9f, -9f);
        outline.useGraphicAlpha = false;

        ResetVisual();
    }

    public void ShowCorrect()
    {
        outline.enabled = false;

        if (checkMark != null)
            checkMark.gameObject.SetActive(true);
    }

    public void ShowWrong()
    {
        if (checkMark != null)
            checkMark.gameObject.SetActive(false);

        outline.effectColor = new Color32(201, 104, 91, 255);
        outline.enabled = true;
    }

    public void ResetVisual()
    {
        if (outline != null)
            outline.enabled = false;

        if (checkMark != null)
            checkMark.gameObject.SetActive(false);
    }
}