using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

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
    [SerializeField] private GameObject targetDisplay;
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
    [SerializeField] private AudioClip completionSound;
    [SerializeField] private GameAudioManager gameAudioManager;

    [Header("Panel Voices")]
    [SerializeField] private AudioSource voiceSource;
    [SerializeField] private AudioClip startPanelVoice;
    [SerializeField] private AudioClip completePanelVoice;
    [SerializeField] private Button startButton;
    [SerializeField] private Button continueButton;

    [Header("Start Screen")]
    [SerializeField] private GameObject startPanel;

    [Header("Completion Screen")]
    [SerializeField] private GameObject completePanel;

    [Header("Celebration")]
    [SerializeField]
    private PaperCelebrationEffect celebrationEffect;

    [Header("Round Result")]
    [SerializeField] private GameObject resultPanel;
    [SerializeField] private RectTransform resultCard;
    [SerializeField] private TMP_Text resultCountText;

    [SerializeField, Min(0f)]
    private float resultHoldDuration = 2f;

    [Header("Music")]
    [SerializeField] private AudioSource musicSource;

    [SerializeField]
    private AmbientAudioController ambientAudioController;

    [Header("Instruction Screen")]
    [SerializeField] private GameObject instructionPanel;
    [SerializeField] private AudioClip instructionVoice;

    [SerializeField, Min(0f)]
    private float instructionEndPause = 0.25f;

    [Header("Score Fly Effect")]
    [SerializeField] private ScoreFlyToken scoreFlyTokenPrefab;
    [SerializeField] private RectTransform scoreFlyLayer;
    [SerializeField] private RectTransform scoreTarget;

    [Header("Wrong Catch Feedback")]
    [SerializeField] private WrongCatchFeedback wrongCatchFeedback;

    private int pendingScoreAnimations;

    public event Action Completed;

    private readonly HashSet<FallingLetter> activeLetters =
        new HashSet<FallingLetter>();

    private Coroutine spawnRoutine;
    private Coroutine panelVoiceRoutine;

    private string targetLetter;
    private int caughtTargetCount;
    private bool isRunning;
    private bool isCompleting;
    private bool isPreparingGame;


    public void SetupGame(LetterData letterData)
    {
        StopGame();
        pendingScoreAnimations = 0;

        if (resultPanel != null)
            resultPanel.SetActive(false);

        if (resultCard != null)
            resultCard.localScale = Vector3.one;

        if (completePanel != null)
            completePanel.SetActive(false);

        if (instructionPanel != null)
            instructionPanel.SetActive(false);

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

        // منطق داخلی الف بر اساس «ا» انجام می‌شود.
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

        if (ambientAudioController != null)
        {
            ambientAudioController.SetMutedForBonusGame(
                true
            );
        }

        if (targetLetterText != null)
        {
            targetLetterText.text =
                targetLetter == "ا"
                    ? "آ  ا"
                    : targetLetter;
        }

        UpdateProgress();
        SetGameplayUIVisible(false);

        if (player != null)
        {
            player.ResetPosition();
            player.enabled = false;
            player.gameObject.SetActive(false);
        }

        if (startPanel != null)
        {
            startPanel.SetActive(true);
            PlayPanelVoice(startPanelVoice, startButton);
        }
        else
        {
            BeginGame();
        }
    }

    public void BeginGame()
    {
        if (isRunning ||
            isCompleting ||
            isPreparingGame ||
            string.IsNullOrWhiteSpace(targetLetter))
        {
            return;
        }

        isPreparingGame = true;

        StopPanelVoice();

        if (startPanel != null)
            startPanel.SetActive(false);

        SetGameplayUIVisible(false);

        // حرف هدف هنگام توضیح دیده شود،
        // ولی شمارنده هنوز مخفی باشد.
        if (targetDisplay != null)
            targetDisplay.SetActive(true);

        if (progressDisplay != null)
            progressDisplay.SetActive(false);

        if (instructionPanel != null)
            instructionPanel.SetActive(true);

        panelVoiceRoutine = StartCoroutine(
            PlayInstructionThenStart()
        );
    }

    private void SetGameplayUIVisible(bool visible)
    {
        if (targetDisplay != null)
        {
            targetDisplay.SetActive(visible);
        }
        else if (targetLetterText != null)
        {
            targetLetterText.gameObject.SetActive(
                visible
            );
        }

        if (progressDisplay != null)
        {
            progressDisplay.SetActive(visible);
        }
        else if (progressText != null)
        {
            progressText.gameObject.SetActive(
                visible
            );
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
               (
                   targetLetter == "ا" &&
                   letter == "آ"
               );
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
            // اجازه نده بیشتر از تعداد لازم امتیاز رزرو شود.
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

        // کشتی فعلاً دیده می‌شود ولی حرکت نمی‌کند.
        if (player != null)
            player.enabled = false;

        // کودک عدد نهایی شمارنده را می‌بیند.
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

        // دکمه قبل از نمایش پنل غیرفعال شود.
        if (continueButton != null)
            continueButton.interactable = false;


        if (completePanel != null)
            completePanel.SetActive(true);

        if (celebrationEffect != null)
            celebrationEffect.PlayCelebration();

        // ابتدا صدای کوتاه موفقیت پخش می‌شود.
        yield return StartCoroutine(
            PlayLockedAndWait(completionSound)
        );

        PlayPanelVoice(
            completePanelVoice,
            continueButton
        );
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

    public void ContinueAfterCompletion()
    {
        StopPanelVoice();

        if (celebrationEffect != null)
            celebrationEffect.StopCelebration();

        if (completePanel != null)
            completePanel.SetActive(false);

        if (ambientAudioController != null)
        {
            ambientAudioController.SetMutedForBonusGame(
                false
            );
        }

        Completed?.Invoke();
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

    private void PlayVoice(AudioClip clip)
    {
        if (clip == null)
            return;

        if (gameAudioManager != null)
        {
            gameAudioManager.PlayLocked(clip);
        }
        else if (audioSource != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }

    private IEnumerator PlayLockedAndWait(
        AudioClip clip
    )
    {
        if (clip == null)
            yield break;

        if (gameAudioManager != null)
        {
            gameAudioManager.PlayLocked(clip);

            yield return new WaitWhile(
                () => gameAudioManager.IsPlayingLocked
            );
        }
        else if (audioSource != null)
        {
            audioSource.PlayOneShot(clip);

            yield return new WaitForSecondsRealtime(
                clip.length
            );
        }
    }

    public void StopGame()
    {
        isRunning = false;
        isCompleting = false;
        isPreparingGame = false;

        if (spawnRoutine != null)
        {
            StopCoroutine(spawnRoutine);
            spawnRoutine = null;
        }

        StopAllCoroutines();
        ClearActiveLetters();
        ClearScoreFlyTokens();

        if (startPanel != null)
            startPanel.SetActive(false);

        SetGameplayUIVisible(false);

        if (player != null)
        {
            player.enabled = false;
            player.gameObject.SetActive(false);
        }

        if (completePanel != null)
            completePanel.SetActive(false);

        if (celebrationEffect != null)
            celebrationEffect.StopCelebration();

        if (resultPanel != null)
            resultPanel.SetActive(false);

        if (instructionPanel != null)
            instructionPanel.SetActive(false);

        if (resultCard != null)
            resultCard.localScale = Vector3.one;

        if (musicSource != null)
            musicSource.Stop();

        if (ambientAudioController != null)
        {
            ambientAudioController.SetMutedForBonusGame(
                false
            );
        }
        StopPanelVoice();
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

    private void PlayPanelVoice(
    AudioClip clip,
    Button buttonToUnlock)
    {
        StopPanelVoice();

        if (buttonToUnlock != null)
            buttonToUnlock.interactable = false;

        if (clip == null || voiceSource == null)
        {
            if (buttonToUnlock != null)
                buttonToUnlock.interactable = true;

            return;
        }

        voiceSource.clip = clip;
        voiceSource.loop = false;
        voiceSource.Play();

        panelVoiceRoutine = StartCoroutine(
            WaitForPanelVoice(buttonToUnlock)
        );
    }

    private IEnumerator WaitForPanelVoice(
        Button buttonToUnlock)
    {
        while (voiceSource != null &&
               voiceSource.isPlaying)
        {
            yield return null;
        }

        panelVoiceRoutine = null;

        if (buttonToUnlock != null)
            buttonToUnlock.interactable = true;
    }

    private void StopPanelVoice()
    {
        if (panelVoiceRoutine != null)
        {
            StopCoroutine(panelVoiceRoutine);
            panelVoiceRoutine = null;
        }

        if (voiceSource != null)
        {
            voiceSource.Stop();
            voiceSource.clip = null;
        }

        if (startButton != null)
            startButton.interactable = true;

        if (continueButton != null)
            continueButton.interactable = true;
    }

    private IEnumerator PlayInstructionThenStart()
    {
        if (instructionVoice != null &&
            voiceSource != null)
        {
            voiceSource.clip = instructionVoice;
            voiceSource.loop = false;
            voiceSource.Play();

            while (voiceSource.isPlaying)
                yield return null;
        }

        panelVoiceRoutine = null;

        if (instructionPanel != null)
            instructionPanel.SetActive(false);

        if (instructionEndPause > 0f)
        {
            yield return new WaitForSecondsRealtime(
                instructionEndPause
            );
        }

        StartGameplay();
    }

    private void StartGameplay()
    {
        if (!isPreparingGame)
            return;

        isPreparingGame = false;

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