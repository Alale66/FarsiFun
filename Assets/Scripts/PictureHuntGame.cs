using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PictureHuntGame : MonoBehaviour
{
    [Serializable]
    public class PictureCard
    {
        public Button button;
        public Image image;
        public QuizChoiceFeedback feedback;
    }

    [Header("Three cards in PictureHuntPanel")]
    [SerializeField] private PictureCard[] cards;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip correctSound;
    [SerializeField] private AudioClip wrongSound;
    [SerializeField] private UIInputLock inputLock;
    [SerializeField] private GameAudioManager gameAudioManager;

    public event Action Completed;

    private readonly HashSet<int> foundCorrectAnswers =
        new HashSet<int>();

    private bool[] correctAnswers;
    private bool finished;
    private bool checkingAnswer;

    public void SetupGame(
        LetterData currentLetter,
        LetterData[] allLetters)
    {
        StopAllCoroutines();

        if (cards == null || cards.Length != 3 ||
            currentLetter == null ||
            allLetters == null ||
            allLetters.Length == 0)
        {
            Debug.LogError(
                "Picture Hunt: Cards or lesson data are missing."
            );
            return;
        }

        List<ExampleData> correctCandidates =
            GetValidExamples(currentLetter);

        List<ExampleData> wrongCandidates =
            new List<ExampleData>();

        foreach (LetterData letter in allLetters)
        {
            if (letter == null || letter == currentLetter)
                continue;

            List<ExampleData> examples =
                GetValidExamples(letter);

            wrongCandidates.AddRange(examples);
        }

        if (correctCandidates.Count < 2)
        {
            Debug.LogError(
                "Picture Hunt: The current letter needs at least two examples."
            );
            return;
        }

        if (wrongCandidates.Count == 0)
        {
            Debug.LogError(
                "Picture Hunt: No wrong-picture candidates were found."
            );
            return;
        }

        finished = false;
        checkingAnswer = false;
        foundCorrectAnswers.Clear();

        int firstCorrectIndex =
            UnityEngine.Random.Range(0, correctCandidates.Count);

        ExampleData firstCorrect =
            correctCandidates[firstCorrectIndex];

        correctCandidates.RemoveAt(firstCorrectIndex);

        ExampleData secondCorrect =
            PickRandom(correctCandidates);

        ExampleData wrongExample =
            PickRandom(wrongCandidates);

        var selected = new List<(ExampleData item, bool correct)>
        {
            (firstCorrect, true),
            (secondCorrect, true),
            (wrongExample, false)
        };

        Shuffle(selected);

        correctAnswers = new bool[cards.Length];

        for (int i = 0; i < cards.Length; i++)
        {
            int cardIndex = i;
            ExampleData item = selected[i].item;

            correctAnswers[i] = selected[i].correct;

            cards[i].image.sprite = item.image;
            cards[i].image.color = Color.white;

            cards[i].feedback.ResetVisual();

            cards[i].button.transition =
                Selectable.Transition.None;

            cards[i].button.onClick.RemoveAllListeners();
            cards[i].button.interactable = true;

            cards[i].button.onClick.AddListener(
                () => SelectPicture(cardIndex, item.audio)
            );
        }
    }

    private List<ExampleData> GetValidExamples(
        LetterData letter)
    {
        List<ExampleData> validExamples =
            new List<ExampleData>();

        if (letter == null || letter.examples == null)
            return validExamples;

        foreach (ExampleData example in letter.examples)
        {
            if (example != null && example.image != null)
                validExamples.Add(example);
        }

        return validExamples;
    }

    private ExampleData PickRandom(
        List<ExampleData> items)
    {
        return items[
            UnityEngine.Random.Range(0, items.Count)
        ];
    }

    private void Shuffle<T>(List<T> items)
    {
        for (int i = items.Count - 1; i > 0; i--)
        {
            int randomIndex =
                UnityEngine.Random.Range(0, i + 1);

            T temp = items[i];
            items[i] = items[randomIndex];
            items[randomIndex] = temp;
        }
    }

    private void SelectPicture(
        int index,
        AudioClip wordSound)
    {
        if (finished ||
            checkingAnswer ||
            foundCorrectAnswers.Contains(index))
        {
            return;
        }

        StartCoroutine(
            CheckPicture(index, wordSound)
        );
    }

    private IEnumerator CheckPicture(
        int index,
        AudioClip wordSound)
    {
        checkingAnswer = true;

        bool usingGameAudioManager =
            gameAudioManager != null;

        if (!usingGameAudioManager && inputLock != null)
            inputLock.Lock();

        bool isCorrect = correctAnswers[index];
        QuizChoiceFeedback feedback =
            cards[index].feedback;

        if (isCorrect)
        {
            foundCorrectAnswers.Add(index);
            cards[index].button.interactable = false;
            feedback.ShowCorrect();
        }
        else
        {
            feedback.ShowWrong();
        }

        AudioClip feedbackSound =
            isCorrect ? correctSound : wrongSound;

        if (usingGameAudioManager)
        {
            gameAudioManager.PlayLockedSequence(
                new AudioClip[]
                {
                    feedbackSound,
                    wordSound
                }
            );

            yield return new WaitWhile(
                () => gameAudioManager.IsPlayingLocked
            );
        }
        else
        {
            if (audioSource != null &&
                feedbackSound != null)
            {
                audioSource.PlayOneShot(feedbackSound);

                yield return new WaitForSeconds(
                    feedbackSound.length
                );
            }

            if (audioSource != null &&
                wordSound != null)
            {
                audioSource.PlayOneShot(wordSound);

                yield return new WaitForSeconds(
                    wordSound.length
                );
            }
        }

        if (!isCorrect)
            feedback.ResetVisual();

        if (isCorrect &&
            foundCorrectAnswers.Count == 2)
        {
            finished = true;

            foreach (PictureCard card in cards)
                card.button.interactable = false;

            checkingAnswer = false;

            if (!usingGameAudioManager &&
                inputLock != null)
            {
                inputLock.Unlock();
            }

            Completed?.Invoke();
            yield break;
        }

        checkingAnswer = false;

        if (!usingGameAudioManager &&
            inputLock != null)
        {
            inputLock.Unlock();
        }
    }
}