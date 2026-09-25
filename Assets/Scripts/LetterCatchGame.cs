using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class LetterCatchGame : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] private AlphabetData alphabetData;

    [Header("Game Objects")]
    [SerializeField] private FallingLetter fallingLetterPrefab;
    [SerializeField] private RectTransform playArea;
    [SerializeField] private RectTransform catcher;
    [SerializeField] private LetterCatchPlayer player;

    [Header("UI")]
    [SerializeField] private TMP_Text targetLetterText;
    [SerializeField] private TMP_Text progressText;

    [Header("Game Settings")]
    [SerializeField, Min(1)] private int requiredTargetCount = 8;

    [SerializeField, Min(0.1f)]
    private float spawnInterval = 0.9f;

    [SerializeField, Min(1f)]
    private float minimumFallSpeed = 180f;

    [SerializeField, Min(1f)]
    private float maximumFallSpeed = 260f;

    [SerializeField, Range(0.1f, 0.9f)]
    private float targetSpawnChance = 0.4f;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip correctSound;
    [SerializeField] private AudioClip wrongSound;
    [SerializeField] private AudioClip completionSound;

    public event Action Completed;

    private readonly HashSet<FallingLetter> activeLetters =
        new HashSet<FallingLetter>();

    private Coroutine spawnRoutine;

    private string targetLetter;
    private int caughtTargetCount;

    private bool isRunning;
    private bool isCompleting;

    public void SetupGame(LetterData letterData)
    {
        StopGame();

        if (letterData == null ||
            alphabetData == null ||
            alphabetData.letters == null ||
            alphabetData.letters.Length == 0 ||
            fallingLetterPrefab == null ||
            playArea == null ||
            catcher == null)
        {
            Debug.LogWarning(
                "LetterCatchGame is missing required references."
            );

            return;
        }

        targetLetter =
            !string.IsNullOrWhiteSpace(letterData.correctLetter)
                ? letterData.correctLetter
                : letterData.targetLetter;

        if (string.IsNullOrWhiteSpace(targetLetter))
        {
            Debug.LogWarning(
                "LetterCatchGame does not have a target letter."
            );

            return;
        }

        caughtTargetCount = 0;
        isRunning = true;
        isCompleting = false;

        if (targetLetterText != null)
            targetLetterText.text = targetLetter;

        UpdateProgress();

        if (player != null)
            player.ResetPosition();

        spawnRoutine = StartCoroutine(SpawnLetters());
    }

    private IEnumerator SpawnLetters()
    {
        yield return new WaitForSeconds(0.5f);

        while (isRunning)
        {
            SpawnLetter();

            yield return new WaitForSeconds(spawnInterval);
        }

        spawnRoutine = null;
    }

    private void SpawnLetter()
    {
        string letter = ChooseLetter();

        FallingLetter fallingLetter =
            Instantiate(fallingLetterPrefab, playArea);

        RectTransform letterRect =
            fallingLetter.GetComponent<RectTransform>();

        float halfLetterWidth =
            letterRect.rect.width * 0.5f;

        float minimumX =
            playArea.rect.xMin + halfLetterWidth;

        float maximumX =
            playArea.rect.xMax - halfLetterWidth;

        float spawnX =
            UnityEngine.Random.Range(minimumX, maximumX);

        float spawnY =
            playArea.rect.yMax -
            letterRect.rect.height * 0.5f;

        letterRect.anchoredPosition =
            new Vector2(spawnX, spawnY);

        float speed = UnityEngine.Random.Range(
            minimumFallSpeed,
            maximumFallSpeed
        );

        activeLetters.Add(fallingLetter);

        fallingLetter.Initialize(
            letter,
            speed,
            playArea,
            catcher,
            OnLetterCaught,
            OnLetterMissed
        );
    }

    private string ChooseLetter()
    {
        bool shouldSpawnTarget =
            UnityEngine.Random.value < targetSpawnChance;

        if (shouldSpawnTarget)
            return ChooseTargetForm();

        string selectedLetter = targetLetter;

        for (int i = 0; i < 20; i++)
        {
            int randomIndex = UnityEngine.Random.Range(
                0,
                alphabetData.letters.Length
            );

            selectedLetter =
                alphabetData.letters[randomIndex];

            if (!IsTargetLetter(selectedLetter))
                return selectedLetter;
        }

        return selectedLetter;
    }

    private string ChooseTargetForm()
    {
        if (targetLetter == "ا")
        {
            return UnityEngine.Random.Range(0, 2) == 0
                ? "ا"
                : "آ";
        }

        return targetLetter;
    }

    private bool IsTargetLetter(string letter)
    {
        return letter == targetLetter ||
               (targetLetter == "ا" && letter == "آ");
    }

    private void OnLetterCaught(FallingLetter fallingLetter)
    {
        if (fallingLetter == null)
            return;

        activeLetters.Remove(fallingLetter);

        if (IsTargetLetter(fallingLetter.LetterValue))
        {
            caughtTargetCount++;
            UpdateProgress();

            PlaySound(correctSound);

            if (caughtTargetCount >= requiredTargetCount)
                StartCoroutine(CompleteGame());
        }
        else
        {
            PlaySound(wrongSound);
        }
    }

    private void OnLetterMissed(FallingLetter fallingLetter)
    {
        if (fallingLetter != null)
            activeLetters.Remove(fallingLetter);
    }

    private IEnumerator CompleteGame()
    {
        if (isCompleting)
            yield break;

        isCompleting = true;
        isRunning = false;

        yield return new WaitForSeconds(0.5f);

        ClearActiveLetters();
        PlaySound(completionSound);

        Completed?.Invoke();
    }

    private void UpdateProgress()
    {
        if (progressText != null)
        {
            progressText.text =
                caughtTargetCount +
                " / " +
                requiredTargetCount;
        }
    }

    private void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
            audioSource.PlayOneShot(clip);
    }

    public void StopGame()
    {
        isRunning = false;
        isCompleting = false;

        if (spawnRoutine != null)
        {
            StopCoroutine(spawnRoutine);
            spawnRoutine = null;
        }

        StopAllCoroutines();
        ClearActiveLetters();
    }

    private void ClearActiveLetters()
    {
        foreach (FallingLetter fallingLetter in activeLetters)
        {
            if (fallingLetter != null)
                Destroy(fallingLetter.gameObject);
        }

        activeLetters.Clear();
    }

    private void OnDisable()
    {
        StopGame();
    }
}