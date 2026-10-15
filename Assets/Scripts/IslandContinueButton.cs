using System;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class IslandContinueButton : MonoBehaviour
{
    [SerializeField] private LessonData lesson;
    [SerializeField] private SceneNavigator navigator;

    private bool canContinue;
    private int nextIslandIndex;

    private void Start()
    {
        nextIslandIndex =
            SceneNavigator.SelectedIslandIndex + 1;

        PlayerProfileData profile =
            PlayerProfileManager.Instance != null
                ? PlayerProfileManager.Instance.SelectedProfile
                : null;

        int nextLetterIndex = nextIslandIndex * 4;

        canContinue =
            nextIslandIndex < 8 &&
            profile != null &&
            profile.completedLetterIndices != null &&
            lesson != null &&
            lesson.letters != null &&
            nextLetterIndex < lesson.letters.Length &&
            lesson.letters[nextLetterIndex] != null;

        if (canContinue)
        {
            for (int i = 0; i < nextLetterIndex; i++)
            {
                if (Array.IndexOf(
                    profile.completedLetterIndices, i) < 0)
                {
                    canContinue = false;
                    break;
                }
            }
        }

        GetComponent<Button>().interactable = canContinue;

        CanvasGroup group = GetComponent<CanvasGroup>();

        if (group != null)
        {
            group.alpha = canContinue ? 1f : 0f;
            group.interactable = canContinue;
            group.blocksRaycasts = canContinue;
        }
    }

    public void OpenNextIsland()
    {
        if (canContinue && navigator != null)
            navigator.GoToIsland(nextIslandIndex);
    }
}