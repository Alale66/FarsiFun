using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class LetterCatchGame : BonusGameBase
{
    [Header("Data")]
    [SerializeField] private AlphabetData alphabetData;

    [Header("Game Objects")]
    [SerializeField] private FallingLetter fallingLetterPrefab;
    [SerializeField] private RectTransform playArea;
    [SerializeField] private RectTransform catcher;
    [SerializeField] private LetterCatchPlayer player;

    [Header("UI")]
    [SerializeField] private TMP_Text progressText;
    [SerializeField] private GameObject progressDisplay;

    [Header("Game Settings")]
    [SerializeField, Min(1)]
    private int requiredTargetCount = 8;

    [SerializeField, Min(0.1f)]
    private float spawnInterval = 0.9f;

    [SerializeField, Min(1f)]
    private float minimumFallSpeed = 180f;

    [SerializeField, Min(1f)]
    private float maximumFallSpeed = 260f;

    [SerializeField, Range(0.1f, 0.9f)]
    private float targetSpawnChance = 0.4f;

    [Header("Sound Effects")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip correctSound;
    [SerializeField] private AudioClip wrongSound;

    [Header("Round Result")]
    [SerializeField] private GameObject resultPanel;
    [SerializeField] private RectTransform resultCard;
    [SerializeField] private TMP_Text resultCountText;

    [SerializeField, Min(0f)]
    private float resultHoldDuration = 2f;

    [Header("Music")]
    [SerializeField] private AudioSource musicSource;


    [Header("Instructions")]
    [SerializeField] private AudioClip instructionVoice;
    [SerializeField] private GameAudioManager gameAudioManager;

    [SerializeField, Min(0f)]
    private float instructionFallbackDuration = 2.5f;

    [Header("Score Fly Effect")]
    [SerializeField] private ScoreFlyToken scoreFlyTokenPrefab;
    [SerializeField] private RectTransform scoreFlyLayer;
    [SerializeField] private RectTransform scoreTarget;

    [Header("Wrong Catch Feedback")]
    [SerializeField] private WrongCatchFeedback wrongCatchFeedback;

    private int pendingScoreAnimations;

    private readonly HashSet<FallingLetter> activeLetters =
        new HashSet<FallingLetter>();

    private Coroutine spawnRoutine;
    private string targetLetter;
    // Stores the complete target family, including all valid forms.
    private LetterData targetFamily;
    private int caughtTargetCount;
    private bool isRunning;
    private bool isCompleting;
    private bool isPreparingGame;

    /// <summary>
    /// Prepares the Letter Catch game using the shared bonus-game
    /// setup contract.
    /// </summary>
    public override void SetupGame(
        LessonData lesson,
        int currentLetterIndex)
    {
        if (lesson == null ||
            lesson.letters == null ||
            currentLetterIndex < 0 ||
            currentLetterIndex >= lesson.letters.Length)
        {
            Debug.LogWarning(
                "LetterCatchGame received invalid lesson data."
            );

            return;
        }

        SetupLetterData(lesson.letters[currentLetterIndex]);
    }

    /// <summary>
    /// Prepares Letter Catch with the selected letter data.
    /// </summary>
    private void SetupLetterData(LetterData letterData)
    {
        StopGame();
        pendingScoreAnimations = 0;

        if (resultPanel != null)
            resultPanel.SetActive(false);

        if (resultCard != null)
            resultCard.localScale = Vector3.one;

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

        targetFamily = letterData;

        targetLetter =
            !string.IsNullOrWhiteSpace(letterData.correctLetter)
                ? letterData.correctLetter
                : letterData.targetLetter;


        // Alef uses "ا" as its canonical internal family identifier.
        if (targetLetter == "آ")
            targetLetter = "ا";

        if (string.IsNullOrWhiteSpace(targetLetter))
        {
            Debug.LogWarning(
                "LetterCatchGame does not have a target letter."
            );

            return;
        }

        caughtTargetCount = 0;
        isRunning = false;
        isCompleting = false;
        isPreparingGame = false;

        UpdateProgress();

        // Keep local Letter Catch visuals hidden while the shared
        // bonus-game start panel is visible.
        ShowWaitingState();

        if (player != null)
        {
            player.ResetPosition();
            player.enabled = false;
            player.gameObject.SetActive(false);
        }

    }

    public override void BeginGame()
    {
        if (isRunning ||
            isCompleting ||
            isPreparingGame ||
            string.IsNullOrWhiteSpace(targetLetter))
        {
            return;
        }

        isPreparingGame = true;

        // Show only the target and instruction panel.
        // Gameplay objects remain hidden until the voice finishes.
        ShowInstructionState();

        StartCoroutine(PlayInstructionThenStart());
    }

    /// <summary>
    /// Changes the visibility of the Letter Catch score display.
    /// </summary>
    private void SetGameplayUIVisible(bool visible)
    {
        if (progressDisplay != null)
        {
            progressDisplay.SetActive(visible);
        }
        else if (progressText != null)
        {
            progressText.gameObject.SetActive(visible);
        }
    }

    private IEnumerator SpawnLetters()
    {
        yield return new WaitForSeconds(0.5f);

        while (isRunning)
        {
            SpawnLetter();

            yield return new WaitForSeconds(
                spawnInterval
            );
        }

        spawnRoutine = null;
    }

    private void SpawnLetter()
    {
        string letter = ChooseLetter();

        FallingLetter fallingLetter =
            Instantiate(
                fallingLetterPrefab,
                playArea
            );

        RectTransform letterRect =
            fallingLetter.GetComponent<RectTransform>();

        float halfLetterWidth =
            letterRect.rect.width * 0.5f;

        float minimumX =
            playArea.rect.xMin + halfLetterWidth;

        float maximumX =
            playArea.rect.xMax - halfLetterWidth;

        float spawnX = UnityEngine.Random.Range(
            minimumX,
            maximumX
        );

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
            UnityEngine.Random.value <
            targetSpawnChance;

        if (shouldSpawnTarget)
            return ChooseTargetForm();

        string selectedLetter = targetLetter;

        for (int i = 0; i < 20; i++)
        {
            int randomIndex =
                UnityEngine.Random.Range(
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

    /// <summary>
    /// Chooses a random valid form from the current target family.
    /// </summary>
    private string ChooseTargetForm()
    {
        if (targetFamily != null)
            return targetFamily.GetRandomGameForm();

        return targetLetter;
    }

    /// <summary>
    /// Checks whether the caught letter belongs to the target family.
    /// </summary>
    private bool IsTargetLetter(string letter)
    {
        if (targetFamily != null)
            return targetFamily.IsGameForm(letter);

        return letter == targetLetter;
    }

    private void OnLetterCaught(FallingLetter fallingLetter)
    {
        if (fallingLetter == null ||
            !isRunning ||
            isCompleting)
        {
            return;
        }

        activeLetters.Remove(fallingLetter);

        string caughtLetter = fallingLetter.LetterValue;
        Vector3 caughtPosition =
            fallingLetter.transform.position;

        if (IsTargetLetter(caughtLetter))
        {
            // Do not reserve more score animations than required.
            if (caughtTargetCount + pendingScoreAnimations >=
                requiredTargetCount)
            {
                return;
            }

            PlaySound(correctSound);

            StartScoreFly(
                caughtLetter,
                caughtPosition
            );
        }
        else
        {
            PlaySound(wrongSound);

            if (wrongCatchFeedback != null)
                wrongCatchFeedback.Play();
        }
    }

    private void StartScoreFly(
    string letter,
    Vector3 startPosition)
    {
        if (scoreFlyTokenPrefab == null ||
            scoreFlyLayer == null ||
            scoreTarget == null)
        {
            AddScore();
            return;
        }

        pendingScoreAnimations++;

        ScoreFlyToken token = Instantiate(
            scoreFlyTokenPrefab,
            scoreFlyLayer
        );

        token.Play(
            letter,
            startPosition,
            scoreTarget,
            OnScoreTokenArrived
        );
    }

    private void OnScoreTokenArrived()
    {
        pendingScoreAnimations = Mathf.Max(
            0,
            pendingScoreAnimations - 1
        );

        if (!isRunning || isCompleting)
            return;

        AddScore();
    }

    private void AddScore()
    {
        caughtTargetCount = Mathf.Min(
            caughtTargetCount + 1,
            requiredTargetCount
        );

        UpdateProgress();

        if (caughtTargetCount >= requiredTargetCount)
            StartCoroutine(CompleteGame());
    }

    private void OnLetterMissed(
        FallingLetter fallingLetter
    )
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

        // Keep the ship visible but stop its movement.
        if (player != null)
            player.enabled = false;

        // Give the child time to see the final score.
        yield return new WaitForSeconds(0.45f);

        ClearActiveLetters();
        SetGameplayUIVisible(false);

        if (resultCountText != null)
        {
            resultCountText.text =
                ToPersianDigits(caughtTargetCount) +
                " از " +
                ToPersianDigits(
                    requiredTargetCount
                );
        }

        if (resultPanel != null)
        {
            resultPanel.SetActive(true);

            if (resultCard != null)
            {
                yield return StartCoroutine(
                    AnimateResultCard()
                );
            }

            yield return new WaitForSeconds(
                resultHoldDuration
            );

            resultPanel.SetActive(false);
        }

        yield return new WaitForSeconds(0.2f);

        if (player != null)
            player.gameObject.SetActive(false);

        if (musicSource != null)
            musicSource.Stop();

        // Notify the shared bonus-game flow after the result sequence ends.
        NotifyGameplayCompleted();
    }

    private IEnumerator AnimateResultCard()
    {
        if (resultCard == null)
            yield break;

        resultCard.localScale =
            Vector3.one * 0.65f;

        const float animationDuration = 0.4f;

        float elapsed = 0f;

        while (elapsed < animationDuration)
        {
            elapsed += Time.unscaledDeltaTime;

            float t = Mathf.Clamp01(
                elapsed / animationDuration
            );

            float scale;

            if (t < 0.75f)
            {
                float growTime =
                    Mathf.SmoothStep(
                        0f,
                        1f,
                        t / 0.75f
                    );

                scale = Mathf.Lerp(
                    0.65f,
                    1.08f,
                    growTime
                );
            }
            else
            {
                float settleTime =
                    Mathf.SmoothStep(
                        0f,
                        1f,
                        (t - 0.75f) / 0.25f
                    );

                scale = Mathf.Lerp(
                    1.08f,
                    1f,
                    settleTime
                );
            }

            resultCard.localScale =
                Vector3.one * scale;

            yield return null;
        }

        resultCard.localScale = Vector3.one;
    }

    private void UpdateProgress()
    {
        if (progressText == null)
            return;

        int displayedCount = Mathf.Min(
            caughtTargetCount,
            requiredTargetCount
        );

        progressText.text =
            ToPersianDigits(displayedCount) +
            " / " +
            ToPersianDigits(requiredTargetCount);
    }

    private string ToPersianDigits(int number)
    {
        return number
            .ToString()
            .Replace("0", "۰")
            .Replace("1", "۱")
            .Replace("2", "۲")
            .Replace("3", "۳")
            .Replace("4", "۴")
            .Replace("5", "۵")
            .Replace("6", "۶")
            .Replace("7", "۷")
            .Replace("8", "۸")
            .Replace("9", "۹");
    }

    private void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }

    public override void StopGame()
    {
        isRunning = false;
        isCompleting = false;
        bool wasPreparingGame = isPreparingGame;
        isPreparingGame = false;

        if (spawnRoutine != null)
        {
            StopCoroutine(spawnRoutine);
            spawnRoutine = null;
        }

        StopAllCoroutines();

        if (wasPreparingGame &&
    gameAudioManager != null &&
    gameAudioManager.IsPlayingLocked)
        {
            gameAudioManager.StopAudio();
        }
        else if (wasPreparingGame &&
                 audioSource != null)
        {
            audioSource.Stop();
        }

        ClearActiveLetters();
        ClearScoreFlyTokens();

        // Hide every local Letter Catch view when the game stops.
        HideGameState();

        if (player != null)
        {
            player.enabled = false;
            player.gameObject.SetActive(false);
        }

        if (resultPanel != null)
            resultPanel.SetActive(false);

        if (resultCard != null)
            resultCard.localScale = Vector3.one;

        if (musicSource != null)
            musicSource.Stop();
    }

    private void ClearActiveLetters()
    {
        foreach (
            FallingLetter fallingLetter
            in activeLetters
        )
        {
            if (fallingLetter != null)
            {
                Destroy(
                    fallingLetter.gameObject
                );
            }
        }

        activeLetters.Clear();
    }

    private void OnDisable()
    {
        StopGame();
    }

    /// <summary>
    /// Plays the instruction voice and starts gameplay
    /// after the voice or fallback delay finishes.
    /// </summary>
    private IEnumerator PlayInstructionThenStart()
    {
        if (instructionVoice != null &&
            gameAudioManager != null)
        {
            while (gameAudioManager.IsPlayingLocked &&
                   isPreparingGame)
            {
                yield return null;
            }

            if (!isPreparingGame)
                yield break;

            bool voiceCompleted = false;

            gameAudioManager.PlayLocked(
                instructionVoice,
                () => voiceCompleted = true
            );

            while (!voiceCompleted &&
                   isPreparingGame)
            {
                yield return null;
            }
        }
        else if (instructionVoice != null &&
                 audioSource != null)
        {
            audioSource.PlayOneShot(instructionVoice);

            yield return new WaitForSecondsRealtime(
                instructionVoice.length
            );
        }
        else if (instructionFallbackDuration > 0f)
        {
            yield return new WaitForSecondsRealtime(
                instructionFallbackDuration
            );
        }

        if (!isPreparingGame)
            yield break;

        StartGameplay();
    }

    private void StartGameplay()
    {
        if (!isPreparingGame)
            return;

        isPreparingGame = false;

        // Reveal the shared gameplay phase first, then restore
        // the Letter Catch HUD elements inside it.
        ShowGameplayState();
        SetGameplayUIVisible(true);

        if (player != null)
        {
            player.gameObject.SetActive(true);
            player.enabled = true;
            player.ResetPosition();
        }

        if (musicSource != null)
        {
            musicSource.loop = true;
            musicSource.time = 0f;
            musicSource.Play();
        }

        isRunning = true;
        spawnRoutine = StartCoroutine(SpawnLetters());
    }

    private void ClearScoreFlyTokens()
    {
        pendingScoreAnimations = 0;

        if (scoreFlyLayer == null)
            return;

        for (int i = scoreFlyLayer.childCount - 1;
             i >= 0;
             i--)
        {
            Destroy(
                scoreFlyLayer.GetChild(i).gameObject
            );
        }
    }
}