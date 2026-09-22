using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LetterHuntGame : MonoBehaviour
{
    [SerializeField] private LetterCoinView[] coins;
    [SerializeField] private RectTransform treasureChest;
    [SerializeField] private TMP_Text scoreText;
    [Header("Coin Feedback")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip correctSound;
    [SerializeField] private AudioClip wrongSound;

    private readonly HashSet<LetterCoinView> wrongCoins =
        new HashSet<LetterCoinView>();

    public event Action Completed;

    private readonly List<Vector3> startPositions = new List<Vector3>();

    private int collected;
    private int targetCount;
    private string targetLetter;
    private bool gameFinished;

    public void SetupGame(LetterData data)
    {
        if (data == null ||
            coins == null ||
            coins.Length == 0 ||
            treasureChest == null ||
            scoreText == null)
        {
            Debug.LogWarning("Letter Hunt setup is incomplete.");
            return;
        }

        foreach (LetterCoinView coin in coins)
        {
            if (coin == null)
            {
                Debug.LogWarning("A Letter Hunt coin is missing.");
                return;
            }
        }

        StopAllCoroutines();
        wrongCoins.Clear();

        foreach (LetterCoinView coin in coins)
        {
            Image coinImage = coin.GetComponent<Image>();

            if (coinImage != null)
                coinImage.color = Color.white;
        }

        if (startPositions.Count != coins.Length)
        {
            startPositions.Clear();

            foreach (LetterCoinView coin in coins)
                startPositions.Add(coin.transform.position);
        }

        targetLetter = data.correctLetter;
        collected = 0;
        gameFinished = false;

        targetCount = Mathf.Min(5, coins.Length);

        List<string> letters = new List<string>();

        // For Alef: 3 coins with ا and 2 coins with آ.
        // Both forms count as correct.
        if (targetLetter == "ا")
        {
            int plainAlefCount = Mathf.Min(3, targetCount);

            for (int i = 0; i < targetCount; i++)
            {
                letters.Add(i < plainAlefCount ? "ا" : "آ");
            }
        }
        else
        {
            for (int i = 0; i < targetCount; i++)
                letters.Add(targetLetter);
        }

        List<string> distractors = new List<string>();

        if (data.choices != null)
        {
            foreach (string choice in data.choices)
            {
                if (string.IsNullOrEmpty(choice))
                    continue;

                if (choice == targetLetter)
                    continue;

                // آ is also correct when the target is ا.
                if (targetLetter == "ا" && choice == "آ")
                    continue;

                if (!distractors.Contains(choice))
                    distractors.Add(choice);
            }
        }

        if (letters.Count < coins.Length && distractors.Count == 0)
        {
            Debug.LogWarning("Letter Hunt needs distractor letters.");
            return;
        }

        // Shuffle distractors so, when possible, the two wrong
        // coins show different letters.
        Shuffle(distractors);

        int distractorIndex = 0;

        while (letters.Count < coins.Length)
        {
            letters.Add(
                distractors[distractorIndex % distractors.Count]
            );

            distractorIndex++;
        }

        Shuffle(letters);

        for (int i = 0; i < coins.Length; i++)
        {
            coins[i].gameObject.SetActive(true);
            coins[i].transform.position = startPositions[i];
            coins[i].Setup(letters[i], OnCoinSelected);
        }

        UpdateScore();
    }

    private void OnCoinSelected(LetterCoinView coin)
    {
        if (gameFinished || wrongCoins.Contains(coin))
            return;

        bool isCorrect =
            coin.Letter == targetLetter ||
            (targetLetter == "ا" && coin.Letter == "آ");

        if (!isCorrect)
        {
            if (audioSource != null && wrongSound != null)
                audioSource.PlayOneShot(wrongSound);

            StartCoroutine(FlashWrongCoin(coin));
            return;
        }

        if (audioSource != null && correctSound != null)
            audioSource.PlayOneShot(correctSound);

        coin.SetInteractable(false);
        StartCoroutine(MoveCoinToChest(coin));
    }

    private IEnumerator FlashWrongCoin(LetterCoinView coin)
    {
        Image coinImage = coin.GetComponent<Image>();

        if (coinImage == null)
            yield break;

        wrongCoins.Add(coin);

        Color originalColor = coinImage.color;
        coinImage.color = new Color(1f, 0.35f, 0.35f, originalColor.a);

        yield return new WaitForSeconds(0.35f);

        coinImage.color = originalColor;
        wrongCoins.Remove(coin);
    }

    private IEnumerator MoveCoinToChest(LetterCoinView coin)
    {
        Vector3 start = coin.transform.position;
        Vector3 destination = treasureChest.position;

        // float duration = 0.5f;
        float duration = correctSound != null ? Mathf.Max(0.5f, correctSound.length + 0.05f) : 0.5f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            float t = Mathf.Clamp01(elapsed / duration);

            coin.transform.position =
                Vector3.Lerp(start, destination, t);

            yield return null;
        }

        coin.gameObject.SetActive(false);

        collected++;
        UpdateScore();

        if (collected == targetCount)
        {
            gameFinished = true;

            foreach (LetterCoinView remainingCoin in coins)
            {
                if (remainingCoin != null && remainingCoin.gameObject.activeSelf)
                    remainingCoin.SetInteractable(false);
            }

            Completed?.Invoke();
        }
    }

    private void UpdateScore()
    {
        string score = collected + "/" + targetCount;

        scoreText.text = score
            .Replace('0', '۰')
            .Replace('1', '۱')
            .Replace('2', '۲')
            .Replace('3', '۳')
            .Replace('4', '۴')
            .Replace('5', '۵')
            .Replace('6', '۶')
            .Replace('7', '۷')
            .Replace('8', '۸')
            .Replace('9', '۹');
    }

    private void Shuffle<T>(List<T> items)
    {
        for (int i = 0; i < items.Count; i++)
        {
            int randomIndex =
                UnityEngine.Random.Range(i, items.Count);

            T temp = items[i];
            items[i] = items[randomIndex];
            items[randomIndex] = temp;
        }
    }
}