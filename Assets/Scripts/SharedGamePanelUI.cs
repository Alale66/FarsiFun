using UnityEngine;
using UnityEngine.UI;

public enum LessonStage
{
    Intro,
    Quiz,
    LetterHunt,
    PictureHunt,
    MemoryMatch
}

public class SharedGamePanelUI : MonoBehaviour
{
    [Header("Stage Content")]
    [SerializeField] private GameObject introContent;
    [SerializeField] private GameObject quizContent;
    [SerializeField] private GameObject letterHuntContent;
    [SerializeField] private GameObject pictureHuntContent;
    [SerializeField] private GameObject memoryMatchContent;

    [Header("Shared Navigation")]
    [SerializeField] private Button backButton;
    [SerializeField] private Button retryButton;
    [SerializeField] private Button nextButton;
    [SerializeField] private GameObject rewardBadge;

    public void ShowStage(LessonStage stage)
    {
        SetContentActive(introContent, stage == LessonStage.Intro);
        SetContentActive(quizContent, stage == LessonStage.Quiz);
        SetContentActive(letterHuntContent, stage == LessonStage.LetterHunt);
        SetContentActive(pictureHuntContent, stage == LessonStage.PictureHunt);
        SetContentActive(memoryMatchContent, stage == LessonStage.MemoryMatch);
    }

    public void SetNavigationVisibility(
    bool showBack,
    bool showRetry,
    bool showNext)
    {
        SetButtonVisible(backButton, showBack);
        SetButtonVisible(retryButton, showRetry);
        SetButtonVisible(nextButton, showNext);
    }

    public void SetCompletion(bool completed)
    {
        if (nextButton != null)
            nextButton.interactable = completed;

        if (retryButton != null)
            retryButton.interactable = completed;

        if (rewardBadge != null)
            rewardBadge.SetActive(completed);
    }

    public void SetRetryInteractable(bool interactable)
    {
        if (retryButton != null)
            retryButton.interactable = interactable;
    }

    private void SetContentActive(GameObject content, bool active)
    {
        if (content != null)
            content.SetActive(active);
    }

    private void SetButtonVisible(Button button, bool visible)
    {
        if (button != null)
            button.gameObject.SetActive(visible);
    }
}