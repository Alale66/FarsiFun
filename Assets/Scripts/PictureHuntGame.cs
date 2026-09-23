
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PictureHuntGame : MonoBehaviour
{
    [Serializable]
    public class PictureItem
    {
        public Sprite picture;
        public AudioClip wordSound;
    }

    [Serializable]
    public class PictureCard
    {
        public Button button;
        public Image image;
        public QuizChoiceFeedback feedback;
    }

    [Header("Three cards in PictureHuntPanel")]
    [SerializeField] private PictureCard[] cards;

    [Header("Pictures for the current lesson")]
    [SerializeField] private PictureItem[] alefPictures;
    [SerializeField] private PictureItem[] aaPictures;
    [SerializeField] private PictureItem[] wrongPictures;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip correctSound;
    [SerializeField] private AudioClip wrongSound;

    public event Action Completed;

    private readonly HashSet<int> foundCorrectAnswers = new HashSet<int>();

    private bool[] correctAnswers;
    private bool finished;
    private bool checkingAnswer;

    public void SetupGame()
    {
        StopAllCoroutines();

        if (cards == null || cards.Length != 3 ||
            alefPictures == null || alefPictures.Length == 0 ||
            aaPictures == null || aaPictures.Length == 0 ||
            wrongPictures == null || wrongPictures.Length == 0)
        {
            Debug.LogError(
                "Picture Hunt: Assign three cards and all picture groups."
            );
            return;
        }

        finished = false;
        checkingAnswer = false;
        foundCorrectAnswers.Clear();

        // One ا picture, one آ picture, and one wrong picture.
        var selected = new List<(PictureItem item, bool correct)>
        {
            (PickRandom(alefPictures), true),
            (PickRandom(aaPictures), true),
            (PickRandom(wrongPictures), false)
        };

        // Randomize card positions.
        for (int i = selected.Count - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);

            (selected[i], selected[j]) =
                (selected[j], selected[i]);
        }

        correctAnswers = new bool[cards.Length];

        for (int i = 0; i < cards.Length; i++)
        {
            int cardIndex = i;
            PictureItem item = selected[i].item;

            correctAnswers[i] = selected[i].correct;

            cards[i].image.sprite = item.picture;
            cards[i].image.color = Color.white;

            // Clear the check mark and red border on every card.
            cards[i].feedback.ResetVisual();

            // Keep disabled cards fully visible.
            cards[i].button.transition = Selectable.Transition.None;

            cards[i].button.onClick.RemoveAllListeners();
            cards[i].button.interactable = true;

            cards[i].button.onClick.AddListener(
                () => SelectPicture(cardIndex, item.wordSound)
            );
        }
    }

    private PictureItem PickRandom(PictureItem[] items)
    {
        return items[UnityEngine.Random.Range(0, items.Length)];
    }

    private void SelectPicture(int index, AudioClip wordSound)
    {
        if (finished ||
            checkingAnswer ||
            foundCorrectAnswers.Contains(index))
        {
            return;
        }

        StartCoroutine(CheckPicture(index, wordSound));
    }

    private IEnumerator CheckPicture(int index, AudioClip wordSound)
    {
        checkingAnswer = true;

        bool isCorrect = correctAnswers[index];
        QuizChoiceFeedback feedback = cards[index].feedback;

        // Show visual feedback immediately.
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

        // 1. Play correct/wrong sound immediately.
        AudioClip feedbackSound = isCorrect
            ? correctSound
            : wrongSound;

        if (audioSource != null && feedbackSound != null)
        {
            audioSource.PlayOneShot(feedbackSound);
            yield return new WaitForSeconds(feedbackSound.length);
        }

        // 2. Say the picture's word.
        if (audioSource != null && wordSound != null)
        {
            audioSource.PlayOneShot(wordSound);
            yield return new WaitForSeconds(wordSound.length);
        }

        // Wrong answer: hide the red border.
        if (!isCorrect)
            feedback.ResetVisual();

        // 3. Complete after the second correct answer's word finishes.
        if (isCorrect && foundCorrectAnswers.Count == 2)
        {
            finished = true;

            foreach (PictureCard card in cards)
                card.button.interactable = false;

            checkingAnswer = false;
            Completed?.Invoke();
            yield break;
        }

        checkingAnswer = false;
    }
}
