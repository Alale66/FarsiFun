using System;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class IslandContinueButton : MonoBehaviour
{
    private const int LettersPerIsland = 5;
    private const int IslandCount = 8;

    [SerializeField] private LessonData lesson;
    [SerializeField] private SceneNavigator navigator;

    [SerializeField, HideInInspector]
    private bool hasCachedLessonData;

    [SerializeField, HideInInspector]
    private int cachedLetterCount;

    [SerializeField, HideInInspector]
    private bool cachedAllLettersValid;

    private bool canContinue;
    private int nextIslandIndex;

    private void Start()
    {
        Refresh();
    }

    public void Refresh()
    {
        int currentIslandIndex =
            SceneNavigator.SelectedIslandIndex;

        nextIslandIndex = currentIslandIndex + 1;

        PlayerProfileData profile =
            PlayerProfileManager.Instance != null
                ? PlayerProfileManager.Instance.SelectedProfile
                : null;

        int letterCount = cachedLetterCount;
        bool allLettersValid = cachedAllLettersValid;

        // Preserve existing behavior until conversion is run.
        if (!hasCachedLessonData)
        {
            letterCount =
                lesson != null && lesson.letters != null
                    ? lesson.letters.Length
                    : 0;

            allLettersValid = letterCount > 0;

            if (allLettersValid)
            {
                foreach (LetterData letter in lesson.letters)
                {
                    if (letter == null)
                    {
                        allLettersValid = false;
                        break;
                    }
                }
            }
        }

        canContinue =
            currentIslandIndex >= 0 &&
            nextIslandIndex < IslandCount &&
            profile != null &&
            profile.highestUnlockedLetterIndex >= nextIslandIndex * LettersPerIsland &&
            profile.completedLetterIndices != null &&
            allLettersValid &&
            letterCount > 0 &&
            letterCount <= LettersPerIsland;

        if (canContinue)
        {
            int firstLetterIndex =
                currentIslandIndex * LettersPerIsland;

            for (int i = 0; i < letterCount; i++)
            {
                int globalLetterIndex =
                    firstLetterIndex + i;

                if (Array.IndexOf(
                    profile.completedLetterIndices,
                    globalLetterIndex
                ) < 0)
                {
                    canContinue = false;
                    break;
                }
            }
        }

        GetComponent<Button>().interactable = canContinue;

        CanvasGroup group = GetComponent<CanvasGroup>();
        if (group == null)
            group = gameObject.AddComponent<CanvasGroup>();

        if (group != null)
        {
            group.alpha = canContinue ? 1f : 0f;
            group.interactable = canContinue;
            group.blocksRaycasts = canContinue;
        }
    }

    public void OpenNextIsland()
    {
        Refresh();

        if (!canContinue)
            return;

        if (navigator == null)
        {
            navigator =
                FindAnyObjectByType<SceneNavigator>();
        }

        if (navigator != null)
            navigator.GoToIsland(nextIslandIndex);
    }
}