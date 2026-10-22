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
        public Button soundButton;
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

        finished = true;
        checkingAnswer = false;
        foundCorrectAnswers.Clear();

        if (cards != null)
        {
            foreach (PictureCard card in cards)
            {
                if (card != null && card.button != null)
                    card.button.interactable = false;
            }
        }

        if (cards == null ||
            cards.Length != 3 ||
            currentLetter == null)
        {
            Debug.LogError(
                "Picture Hunt: Cards or current letter are missing."
            );
            return;
        }

        foreach (PictureCard card in cards)
        {
            if (card == null ||
                card.button == null ||
                card.image == null ||
                card.feedback == null)
            {
                Debug.LogError(
                    "Picture Hunt: A card reference is missing."
                );
                return;
            }
        }

        bool containsMode =
            currentLetter.pictureHuntMode ==
            PictureHuntMode.ContainsTargetSound;

        List<ExampleData> correctCandidates =
            new List<ExampleData>();

        List<ExampleData> wrongCandidates =
            new List<ExampleData>();

        foreach (ExampleData example in GetValidExamples(currentLetter))
        {
            bool isCorrect = containsMode
                ? example.containsTargetSound
                : example.startsWithTarget;

            if (isCorrect)
                correctCandidates.Add(example);
            else
                wrongCandidates.Add(example);
        }

        // These examples are used only as incorrect pictures.
        if (currentLetter.pictureHuntDistractors != null)
        {
            foreach (ExampleData example in
                     currentLetter.pictureHuntDistractors)
            {
                if (example != null && example.image != null)
                    wrongCandidates.Add(example);
            }
        }

        wrongCandidates.RemoveAll(wrong =>
            correctCandidates.Exists(correct =>
                correct.image == wrong.image ||
                string.Equals(
                    correct.word?.Trim(),
                    wrong.word?.Trim(),
                    StringComparison.Ordinal
                )
            )
        );

        if (correctCandidates.Count < 2)
        {
            Debug.LogError(
                "Picture Hunt: At least two correct examples are needed."
            );
            return;
        }

        Shuffle(correctCandidates);

        ExampleData firstCorrect = correctCandidates[0];

        // Do not display the same word or image twice.
        correctCandidates.RemoveAll(example =>
            example.image == firstCorrect.image ||
            string.Equals(
                example.word?.Trim(),
                firstCorrect.word?.Trim(),
                StringComparison.Ordinal
            )
        );

        if (correctCandidates.Count == 0)
        {
            Debug.LogError(
                "Picture Hunt: Two different correct pictures are needed."
            );
            return;
        }

        if (wrongCandidates.Count == 0)
        {
            Debug.LogError(
                "Picture Hunt: Add an incorrect picture to Picture Hunt Distractors."
            );
            return;
        }

        ExampleData secondCorrect = PickRandom(correctCandidates);
        ExampleData wrongExample = PickRandom(wrongCandidates);

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
            cards[i].image.preserveAspect = true;
            cards[i].image.color = Color.white;

            cards[i].feedback.ResetVisual();

            cards[i].button.transition =
                Selectable.Transition.None;

            cards[i].button.onClick.RemoveAllListeners();

            cards[i].button.onClick.AddListener(
                () => SelectPicture(cardIndex, item.audio)
            );

            cards[i].button.interactable = true;

            Button soundButton = cards[i].soundButton;
            AudioClip wordClip = item.audio;

            if (soundButton != null)
            {
                soundButton.onClick.RemoveAllListeners();

                soundButton.interactable =
                    wordClip != null && gameAudioManager != null;

                soundButton.onClick.AddListener(
                    () => PlayPictureWord(cardIndex, wordClip)
                );
            }
        }

        finished = false;
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
            foundCorrectAnswers.Contains(index) ||
            (gameAudioManager != null &&
             gameAudioManager.IsPlayingLocked))
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
        }

        AudioClip feedbackSound =
            isCorrect ? correctSound : wrongSound;

        // First, play the picture's word.
        if (usingGameAudioManager)
        {
            if (wordSound != null)
            {
                gameAudioManager.PlayLocked(wordSound);

                yield return new WaitWhile(
                    () => gameAudioManager.IsPlayingLocked
                );
            }
        }
        else if (audioSource != null &&
                 wordSound != null)
        {
            audioSource.PlayOneShot(wordSound);

            yield return new WaitForSeconds(
                wordSound.length
            );
        }


        if (isCorrect)
            feedback.ShowCorrect();
        else
            feedback.ShowWrong();

        if (isCorrect)
        {
            PlayerRewardSystem.ReportCorrectAnswer(
                cards[index].button.transform as RectTransform
            );
        }
        else
        {
            PlayerRewardSystem.ReportWrongAnswer(
                cards[index].button.transform as RectTransform
            );
        }

        if (usingGameAudioManager)
        {
            if (feedbackSound != null)
            {
                gameAudioManager.PlayLocked(feedbackSound);

                yield return new WaitWhile(
                    () => gameAudioManager.IsPlayingLocked
                );
            }
        }
        else if (audioSource != null &&
                 feedbackSound != null)
        {
            audioSource.PlayOneShot(feedbackSound);

            yield return new WaitForSeconds(
                feedbackSound.length
            );
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

    private void PlayPictureWord(
    int index,
    AudioClip wordSound)
    {
        if (!isActiveAndEnabled ||
            finished ||
            checkingAnswer ||
            wordSound == null ||
            gameAudioManager == null ||
            gameAudioManager.IsPlayingLocked ||
            cards == null ||
            index < 0 ||
            index >= cards.Length)
        {
            return;
        }

        if (!cards[index].button.interactable)
            return;

        // Only play the word; do not check the answer.
        gameAudioManager.PlayLocked(wordSound);
    }
}