using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MemoryMatchGame : MonoBehaviour
{
    [SerializeField] private MatchCardView[] cards;

    [Header("Audio")]
    [SerializeField] private AudioClip correctSound;
    [SerializeField] private AudioClip wrongSound;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private GameAudioManager gameAudioManager;
    [SerializeField, Min(0f)] private float matchedWordPause = 0.25f;

    public event Action Completed;

    private MatchCardView firstSelected;
    private MatchCardView secondSelected;

    private readonly HashSet<MatchCardView> matchedCards =
        new HashSet<MatchCardView>();

    private bool isChecking;

    public void SetupGame(LetterData letterData)
    {
        if (letterData == null ||
            letterData.examples == null ||
            letterData.examples.Length < 3 ||
            cards == null ||
            cards.Length != 6)
        {
            Debug.LogWarning(
                "MemoryMatchGame needs six cards and at least three examples."
            );
            return;
        }

        StopAllCoroutines();

        matchedCards.Clear();
        firstSelected = null;
        secondSelected = null;
        isChecking = false;

        List<ExampleData> examples =
            new List<ExampleData>(letterData.examples);

        Shuffle(examples);

        List<CardSetupData> cardSetups =
            new List<CardSetupData>();

        for (int i = 0; i < 3; i++)
        {
            ExampleData example = examples[i];

            cardSetups.Add(new CardSetupData(example, true));
            cardSetups.Add(new CardSetupData(example, false));
        }

        Shuffle(cardSetups);

        for (int i = 0; i < cards.Length; i++)
        {
            CardSetupData setup = cardSetups[i];

            cards[i].SetGame(this);

            if (setup.isImage)
                cards[i].SetupAsImage(setup.example);
            else
                cards[i].SetupAsWord(setup.example);

            cards[i].SetInteractable(true);
        }
    }

    public void SelectCard(MatchCardView card)
    {
        if (isChecking ||
            card == null ||
            card.IsMatched ||
            matchedCards.Contains(card))
        {
            return;
        }

        if (firstSelected == null)
        {
            firstSelected = card;
            firstSelected.ShowSelected();
            return;
        }

        if (card == firstSelected)
            return;

        secondSelected = card;
        secondSelected.ShowSelected();

        isChecking = true;
        StartCoroutine(CheckMatch());
    }

    private IEnumerator CheckMatch()
    {
        MatchCardView first = firstSelected;
        MatchCardView second = secondSelected;

        // هر دو کارت ۰.۲۵ ثانیه نارنجی می‌مانند.
        yield return new WaitForSeconds(0.25f);

        bool isMatch =
            first.Example == second.Example &&
            first.IsImageCard != second.IsImageCard;

        if (isMatch)
        {
            matchedCards.Add(first);
            matchedCards.Add(second);

            first.ShowCorrect();
            second.ShowCorrect();

            AudioClip matchedWordAudio =
                first.Example != null
                    ? first.Example.audio
                    : null;

            if (gameAudioManager != null)
            {
                AudioClip[] sequence =
                {
            correctSound,
            matchedWordAudio
        };

                gameAudioManager.PlayLockedSequence(
                    sequence,
                    matchedWordPause
                );

                yield return new WaitWhile(
                    () => gameAudioManager.IsPlayingLocked
                );
            }
            else if (audioSource != null)
            {
                if (correctSound != null)
                {
                    audioSource.PlayOneShot(correctSound);

                    yield return new WaitForSeconds(
                        correctSound.length
                    );
                }

                if (correctSound != null &&
                    matchedWordAudio != null &&
                    matchedWordPause > 0f)
                {
                    yield return new WaitForSeconds(
                        matchedWordPause
                    );
                }

                if (matchedWordAudio != null)
                {
                    audioSource.PlayOneShot(matchedWordAudio);

                    yield return new WaitForSeconds(
                        matchedWordAudio.length
                    );
                }
            }
        }
        else
        {
            first.ShowWrong();
            second.ShowWrong();

            if (gameAudioManager != null && wrongSound != null)
            {
                gameAudioManager.PlayLocked(wrongSound);
            }
            else if (audioSource != null && wrongSound != null)
            {
                audioSource.PlayOneShot(wrongSound);
            }

            yield return new WaitForSeconds(0.6f);

            first.ResetVisual();
            second.ResetVisual();
        }

        firstSelected = null;
        secondSelected = null;
        isChecking = false;

        if (matchedCards.Count == cards.Length)
        {
            Completed?.Invoke();
        }
    }

    private void Shuffle<T>(List<T> items)
    {
        for (int i = 0; i < items.Count; i++)
        {
            int randomIndex = UnityEngine.Random.Range(i, items.Count);
            T temp = items[i];
            items[i] = items[randomIndex];
            items[randomIndex] = temp;
        }
    }

    private class CardSetupData
    {
        public ExampleData example;
        public bool isImage;

        public CardSetupData(ExampleData example, bool isImage)
        {
            this.example = example;
            this.isImage = isImage;
        }
    }
}