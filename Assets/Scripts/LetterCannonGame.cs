using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class LetterCannonGame : BonusGameBase
{
    [Header("Gameplay")]
    [SerializeField, Min(1)]
    private int requiredCorrectHits = 5;

    [SerializeField]
    private LetterCannonTargetView[] targets;

    [Header("Cannon")]
    [SerializeField] private Image cannonImage;
    [SerializeField] private Sprite cannonLeftSprite;
    [SerializeField] private Sprite cannonStraightSprite;
    [SerializeField] private Sprite cannonRightSprite;
    [SerializeField] private RectTransform muzzlePoint;

    [Header("Muzzle Positions")]
    [Tooltip("Local cannon muzzle position while aiming left.")]
    [SerializeField]
    private Vector2 leftMuzzlePosition =
        new Vector2(-105f, 165f);

    [Tooltip("Local cannon muzzle position while aiming forward.")]
    [SerializeField]
    private Vector2 centerMuzzlePosition =
        new Vector2(0f, 205f);

    [Tooltip("Local cannon muzzle position while aiming right.")]
    [SerializeField]
    private Vector2 rightMuzzlePosition =
        new Vector2(105f, 165f);

    [Header("Fuse Spark Positions")]
    [Tooltip("Local fuse spark position while aiming left.")]
    [SerializeField]
    private Vector2 leftFuseSparkPosition =
new Vector2(-65f, -130f);

    [Tooltip("Local fuse spark position while aiming forward.")]
    [SerializeField]
    private Vector2 centerFuseSparkPosition =
        new Vector2(-45f, -134f);

    [Tooltip("Local fuse spark position while aiming right.")]
    [SerializeField]
    private Vector2 rightFuseSparkPosition =
        new Vector2(45f, -130f);

    [Header("Effects")]
    [SerializeField] private Image cannonballImage;
    [SerializeField] private Image correctBurstImage;

    [Header("Fire Effects")]
    [SerializeField] private Image muzzleFlashImage;
    [SerializeField] private Image fuseSparkImage;

    [SerializeField, Min(0.05f)]
    private float muzzleFlashDuration = 0.15f;

    [SerializeField, Min(0.1f)]
    private float fusePulseDuration = 0.45f;

    [Header("Progress")]
    [SerializeField] private TMP_Text progressText;

    [Header("Timing")]
    [SerializeField, Min(0f)]
    private float aimDelay = 0.2f;

    [SerializeField, Min(0.05f)]
    private float cannonRecoilDuration = 0.22f;

    [SerializeField, Min(1f)]
    private float cannonRecoilDistance = 18f;

    [SerializeField, Min(0.1f)]
    private float projectileDuration = 0.55f;

    [SerializeField, Min(0.1f)]
    private float correctBurstDuration = 0.45f;

    [SerializeField, Min(0.1f)]
    private float wrongShakeDuration = 0.35f;

    [SerializeField, Min(0f)]
    private float instructionFallbackDelay = 1.5f;

    [Header("Audio")]
    [SerializeField] private GameAudioManager gameAudioManager;
    [SerializeField] private AudioSource effectsAudioSource;
    [SerializeField] private AudioClip instructionVoice;
    [SerializeField] private AudioClip cannonShotSound;
    [SerializeField] private AudioClip correctSound;
    [SerializeField] private AudioClip wrongSound;

    private LessonData currentLesson;
    private LetterData currentLetter;
    private Coroutine gameRoutine;
    private Coroutine fuseSparkRoutine;
    private Coroutine muzzleFlashRoutine;
    private int correctHitCount;
    private int aimedTargetIndex = 1;
    private bool isRunning;
    private bool isResolving;
    private bool gameCompleted;
    private bool ownsInstructionAudio;

    /// <summary>
    /// Prepares Letter Cannon with the selected lesson letter.
    /// </summary>
    public override void SetupGame(
        LessonData lesson,
        int currentLetterIndex)
    {
        StopGame();

        if (lesson == null ||
            lesson.letters == null ||
            currentLetterIndex < 0 ||
            currentLetterIndex >= lesson.letters.Length)
        {
            Debug.LogWarning(
                "LetterCannonGame received invalid lesson data."
            );

            return;
        }

        if (targets == null ||
            targets.Length < 3 ||
            cannonImage == null ||
            muzzlePoint == null ||
            cannonballImage == null ||
            correctBurstImage == null ||
            progressText == null)
        {
            Debug.LogWarning(
                "LetterCannonGame is missing required references."
            );

            return;
        }

        currentLesson = lesson;
        currentLetter = lesson.letters[currentLetterIndex];

        if (currentLetter == null)
        {
            Debug.LogWarning(
                "LetterCannonGame received an empty letter."
            );

            return;
        }

        correctHitCount = 0;
        aimedTargetIndex = 1;
        isRunning = false;
        isResolving = false;
        gameCompleted = false;

        for (int i = 0; i < targets.Length; i++)
        {
            if (targets[i] == null)
                continue;

            targets[i].Initialize(
                i,
                HandleTargetSelected
            );

            targets[i].SetInteractable(false);
        }

        SetCannonPose(1);
        SetEffectVisible(cannonballImage, false);
        SetEffectVisible(correctBurstImage, false);

        UpdateProgress();
        ShowWaitingState();
    }

    /// <summary>
    /// Shows the local instruction and begins gameplay after
    /// the instruction voice or fallback delay finishes.
    /// </summary>
    public override void BeginGame()
    {
        if (currentLetter == null ||
            isRunning ||
            isResolving ||
            gameCompleted)
        {
            return;
        }

        StopOwnedRoutine();
        ShowInstructionState();

        gameRoutine = StartCoroutine(
            PlayInstructionThenStart()
        );
    }

    /// <summary>
    /// Stops gameplay, animations, audio, and local game views.
    /// </summary>
    public override void StopGame()
    {
        StopOwnedRoutine();
        StopFuseSpark();
        SetEffectVisible(muzzleFlashImage, false);

        isRunning = false;
        isResolving = false;
        gameCompleted = false;

        SetTargetsInteractable(false);
        SetEffectVisible(cannonballImage, false);
        SetEffectVisible(correctBurstImage, false);

        SetCannonPose(1);
        HideGameState();

        currentLesson = null;
        currentLetter = null;
        correctHitCount = 0;
    }

    private IEnumerator PlayInstructionThenStart()
    {
        if (instructionVoice != null &&
            gameAudioManager != null)
        {
            while (gameAudioManager.IsPlayingLocked)
                yield return null;

            bool voiceCompleted = false;
            ownsInstructionAudio = true;

            gameAudioManager.PlayLocked(
                instructionVoice,
                () =>
                {
                    voiceCompleted = true;
                    ownsInstructionAudio = false;
                }
            );

            while (!voiceCompleted)
                yield return null;
        }
        else if (instructionFallbackDelay > 0f)
        {
            yield return new WaitForSeconds(
                instructionFallbackDelay
            );
        }

        gameRoutine = null;

        if (currentLetter == null)
            yield break;

        ShowGameplayState();

        correctHitCount = 0;
        isRunning = true;
        isResolving = false;
        gameCompleted = false;

        UpdateProgress();
        PrepareRound();
    }

    /// <summary>
    /// Creates one correct option and two different distractors,
    /// then distributes them randomly between the targets.
    /// </summary>
    private void PrepareRound()
    {
        if (currentLetter == null ||
            targets == null ||
            targets.Length == 0)
        {
            return;
        }

        List<string> options = new List<string>();
        List<string> distractors = BuildDistractorList();

        string correctForm =
            currentLetter.GetRandomGameForm();

        if (string.IsNullOrWhiteSpace(correctForm))
            correctForm = currentLetter.correctLetter;

        if (string.IsNullOrWhiteSpace(correctForm))
            correctForm = currentLetter.targetLetter;

        AddUniqueValue(options, correctForm);
        Shuffle(distractors);

        for (int i = 0;
             i < distractors.Count &&
             options.Count < targets.Length;
             i++)
        {
            AddUniqueValue(
                options,
                distractors[i]
            );
        }

        while (options.Count < targets.Length)
            options.Add("؟");

        Shuffle(options);

        for (int i = 0; i < targets.Length; i++)
        {
            if (targets[i] == null)
                continue;

            targets[i].SetLetter(options[i]);
            targets[i].SetInteractable(true);
        }

        StartFuseSpark();
    }

    /// <summary>
    /// Builds distractor forms from the current letter choices
    /// and the other letters available in the lesson.
    /// </summary>
    private List<string> BuildDistractorList()
    {
        List<string> distractors =
            new List<string>();

        if (currentLetter.choices != null)
        {
            for (int i = 0;
                 i < currentLetter.choices.Length;
                 i++)
            {
                AddDistractor(
                    distractors,
                    currentLetter.choices[i]
                );
            }
        }

        if (currentLesson != null &&
            currentLesson.letters != null)
        {
            for (int i = 0;
                 i < currentLesson.letters.Length;
                 i++)
            {
                LetterData letter =
                    currentLesson.letters[i];

                if (letter == null ||
                    letter == currentLetter)
                {
                    continue;
                }

                string[] forms =
                    letter.GetGameForms();

                for (int formIndex = 0;
                     formIndex < forms.Length;
                     formIndex++)
                {
                    AddDistractor(
                        distractors,
                        forms[formIndex]
                    );
                }
            }
        }

        return distractors;
    }

    private void AddDistractor(
        List<string> distractors,
        string value)
    {
        if (string.IsNullOrWhiteSpace(value) ||
            currentLetter.IsGameForm(value) ||
            distractors.Contains(value))
        {
            return;
        }

        distractors.Add(value);
    }

    private void Update()
    {
        if (!isRunning ||
            isResolving ||
            gameCompleted ||
            targets == null ||
            targets.Length == 0)
        {
            return;
        }

        if (WasAimLeftPressed())
        {
            AimAtTarget(
                aimedTargetIndex - 1
            );
        }
        else if (WasAimRightPressed())
        {
            AimAtTarget(
                aimedTargetIndex + 1
            );
        }

        if (WasFirePressed() &&
            aimedTargetIndex >= 0 &&
            aimedTargetIndex < targets.Length &&
            targets[aimedTargetIndex] != null)
        {
            HandleTargetSelected(
                targets[aimedTargetIndex]
            );
        }
    }

    /// <summary>
    /// Changes the selected cannon direction without firing.
    /// </summary>
    private void AimAtTarget(int targetIndex)
    {
        aimedTargetIndex = Mathf.Clamp(
            targetIndex,
            0,
            targets.Length - 1
        );

        SetCannonPose(aimedTargetIndex);
    }

    private static bool WasAimLeftPressed()
    {
#if ENABLE_INPUT_SYSTEM
        Keyboard keyboard = Keyboard.current;

        return keyboard != null &&
               (keyboard.leftArrowKey.wasPressedThisFrame ||
                keyboard.aKey.wasPressedThisFrame);

#elif ENABLE_LEGACY_INPUT_MANAGER
    return Input.GetKeyDown(KeyCode.LeftArrow) ||
           Input.GetKeyDown(KeyCode.A);

#else
    return false;
#endif
    }

    private static bool WasAimRightPressed()
    {
#if ENABLE_INPUT_SYSTEM
        Keyboard keyboard = Keyboard.current;

        return keyboard != null &&
               (keyboard.rightArrowKey.wasPressedThisFrame ||
                keyboard.dKey.wasPressedThisFrame);

#elif ENABLE_LEGACY_INPUT_MANAGER
    return Input.GetKeyDown(KeyCode.RightArrow) ||
           Input.GetKeyDown(KeyCode.D);

#else
    return false;
#endif
    }

    private static bool WasFirePressed()
    {
#if ENABLE_INPUT_SYSTEM
        Keyboard keyboard = Keyboard.current;

        return keyboard != null &&
               (keyboard.spaceKey.wasPressedThisFrame ||
                keyboard.enterKey.wasPressedThisFrame ||
                keyboard.numpadEnterKey.wasPressedThisFrame);

#elif ENABLE_LEGACY_INPUT_MANAGER
    return Input.GetKeyDown(KeyCode.Space) ||
           Input.GetKeyDown(KeyCode.Return) ||
           Input.GetKeyDown(KeyCode.KeypadEnter);

#else
    return false;
#endif
    }

    private void HandleTargetSelected(
        LetterCannonTargetView selectedTarget)
    {
        if (!isRunning ||
            isResolving ||
            gameCompleted ||
            selectedTarget == null)
        {
            return;
        }

        aimedTargetIndex = Mathf.Clamp(
            selectedTarget.TargetIndex,
            0,
            targets.Length - 1
        );

        gameRoutine = StartCoroutine(
            ResolveShot(selectedTarget)
        );
    }

    /// <summary>
    /// Aims the cannon, flies the projectile to the selected
    /// target, and resolves the answer.
    /// </summary>
    private IEnumerator ResolveShot(
        LetterCannonTargetView selectedTarget)
    {
        isResolving = true;
        SetTargetsInteractable(false);

        SetCannonPose(
            selectedTarget.TargetIndex
        );

        if (aimDelay > 0f)
        {
            yield return new WaitForSeconds(
                aimDelay
            );
        }

        StopFuseSpark();
        PlayMuzzleFlash();

        PlayEffect(cannonShotSound);

        StartCoroutine(
            PlayCannonRecoil(
                selectedTarget.TargetIndex
            )
        );

        yield return StartCoroutine(
            FlyCannonball(selectedTarget)
        );

        bool isCorrect =
            currentLetter != null &&
            currentLetter.IsGameForm(
                selectedTarget.DisplayedLetter
            );

        if (isCorrect)
        {
            PlayEffect(correctSound);

            yield return StartCoroutine(
                PlayCorrectBurst(selectedTarget)
            );

            correctHitCount++;
            UpdateProgress();

            if (correctHitCount >=
                requiredCorrectHits)
            {
                gameCompleted = true;
                isRunning = false;
                isResolving = false;
                gameRoutine = null;

                NotifyGameplayCompleted();
                yield break;
            }

            PrepareRound();
        }
        else
        {
            PlayEffect(wrongSound);

            yield return StartCoroutine(
                ShakeWrongTarget(selectedTarget)
            );

            StartFuseSpark();
            SetTargetsInteractable(true);
        }

        isResolving = false;
        gameRoutine = null;
    }

    /// <summary>
    /// Moves the cannon briefly in the opposite direction of
    /// the shot, then smoothly returns it to its resting position.
    /// </summary>
    private IEnumerator PlayCannonRecoil(int targetIndex)
    {
        if (cannonImage == null)
            yield break;

        RectTransform cannonRect =
            cannonImage.rectTransform;

        Vector2 originalPosition =
            cannonRect.anchoredPosition;

        Vector3 originalScale =
            cannonRect.localScale;

        Vector3 recoilScale = new Vector3(
            originalScale.x * 1.04f,
            originalScale.y * 0.93f,
            originalScale.z
        );

        Vector2 recoilDirection;

        if (targetIndex <= 0)
        {
            recoilDirection =
                new Vector2(0.7f, -1f).normalized;
        }
        else if (targetIndex >= 2)
        {
            recoilDirection =
                new Vector2(-0.7f, -1f).normalized;
        }
        else
        {
            recoilDirection = Vector2.down;
        }

        Vector2 recoilPosition =
            originalPosition +
            recoilDirection *
            cannonRecoilDistance;

        float outwardDuration =
            cannonRecoilDuration * 0.35f;

        float returnDuration =
            cannonRecoilDuration - outwardDuration;

        float elapsed = 0f;

        while (elapsed < outwardDuration)
        {
            elapsed += Time.deltaTime;

            float t = Mathf.Clamp01(
                elapsed / outwardDuration
            );

            cannonRect.anchoredPosition =
                Vector2.Lerp(
                    originalPosition,
                    recoilPosition,
                    Mathf.SmoothStep(0f, 1f, t)
                );

            cannonRect.localScale =
                Vector3.Lerp(
                    originalScale,
                    recoilScale,
                    Mathf.SmoothStep(0f, 1f, t)
                );

            yield return null;
        }

        elapsed = 0f;

        while (elapsed < returnDuration)
        {
            elapsed += Time.deltaTime;

            float t = Mathf.Clamp01(
                elapsed / returnDuration
            );

            cannonRect.anchoredPosition =
                Vector2.Lerp(
                    recoilPosition,
                    originalPosition,
                    Mathf.SmoothStep(0f, 1f, t)
                );

            cannonRect.localScale =
                Vector3.Lerp(
                    recoilScale,
                    originalScale,
                    Mathf.SmoothStep(0f, 1f, t)
                );

            yield return null;
        }

        cannonRect.anchoredPosition =
            originalPosition;

        cannonRect.localScale =
            originalScale;
    }

    private IEnumerator FlyCannonball(
        LetterCannonTargetView selectedTarget)
    {
        if (cannonballImage == null ||
            muzzlePoint == null ||
            selectedTarget.TargetRect == null)
        {
            yield break;
        }

        RectTransform cannonballRect =
            cannonballImage.rectTransform;

        cannonballRect.position =
            muzzlePoint.position;

        cannonballRect.localScale =
            Vector3.one * 1.1f;

        cannonballImage.gameObject.SetActive(true);

        Vector3 startPosition =
            muzzlePoint.position;

        float elapsed = 0f;

        while (elapsed < projectileDuration)
        {
            elapsed += Time.deltaTime;

            float t = Mathf.Clamp01(
                elapsed / projectileDuration
            );

            float easedT = Mathf.SmoothStep(
                0f,
                1f,
                t
            );

            Vector3 targetPosition =
                selectedTarget.TargetRect.position;

            cannonballRect.position =
                Vector3.Lerp(
                    startPosition,
                    targetPosition,
                    easedT
                );

            cannonballRect.localScale =
                Vector3.one *
                Mathf.Lerp(
                    1.1f,
                    0.45f,
                    easedT
                );

            yield return null;
        }

        cannonballImage.gameObject.SetActive(false);
    }

    private IEnumerator PlayCorrectBurst(
        LetterCannonTargetView selectedTarget)
    {
        if (correctBurstImage == null ||
            selectedTarget.TargetRect == null)
        {
            yield break;
        }

        RectTransform burstRect =
            correctBurstImage.rectTransform;

        burstRect.position =
            selectedTarget.TargetRect.position;

        burstRect.localScale =
            Vector3.one * 0.35f;

        Color originalColor =
            correctBurstImage.color;

        correctBurstImage.color =
            new Color(
                originalColor.r,
                originalColor.g,
                originalColor.b,
                1f
            );

        correctBurstImage.gameObject.SetActive(true);

        float elapsed = 0f;

        while (elapsed < correctBurstDuration)
        {
            elapsed += Time.deltaTime;

            float t = Mathf.Clamp01(
                elapsed / correctBurstDuration
            );

            burstRect.localScale =
                Vector3.one *
                Mathf.Lerp(
                    0.35f,
                    1.25f,
                    Mathf.SmoothStep(0f, 1f, t)
                );

            float alpha = t < 0.65f
                ? 1f
                : Mathf.Lerp(
                    1f,
                    0f,
                    (t - 0.65f) / 0.35f
                );

            correctBurstImage.color =
                new Color(
                    originalColor.r,
                    originalColor.g,
                    originalColor.b,
                    alpha
                );

            yield return null;
        }

        correctBurstImage.color = originalColor;
        correctBurstImage.gameObject.SetActive(false);
    }

    private IEnumerator ShakeWrongTarget(
    LetterCannonTargetView selectedTarget)
    {
        RectTransform targetRect =
            selectedTarget.TargetRect;

        if (targetRect == null)
            yield break;

        Vector2 originalPosition =
            targetRect.anchoredPosition;

        selectedTarget.ShowWrongOutline(true);

        float elapsed = 0f;

        while (elapsed < wrongShakeDuration)
        {
            elapsed += Time.deltaTime;

            float normalizedTime =
                Mathf.Clamp01(
                    elapsed / wrongShakeDuration
                );

            float strength =
                Mathf.Lerp(
                    14f,
                    0f,
                    normalizedTime
                );

            float offset =
                Mathf.Sin(
                    normalizedTime *
                    Mathf.PI *
                    8f
                ) * strength;

            targetRect.anchoredPosition =
                originalPosition +
                Vector2.right * offset;

            yield return null;
        }

        targetRect.anchoredPosition =
            originalPosition;

        yield return new WaitForSeconds(0.2f);

        selectedTarget.ShowWrongOutline(false);
    }

    /// <summary>
    /// Selects the perspective-correct cannon sprite and
    /// matching projectile origin.
    /// </summary>
    private void SetCannonPose(int targetIndex)
    {
        if (targetIndex <= 0)
        {
            if (cannonImage != null)
                cannonImage.sprite = cannonLeftSprite;

            if (muzzlePoint != null)
            {
                muzzlePoint.anchoredPosition =
                    leftMuzzlePosition;
            }

            if (fuseSparkImage != null)
            {
                fuseSparkImage.rectTransform.anchoredPosition =
                    leftFuseSparkPosition;
            }

            return;
        }

        if (targetIndex >= 2)
        {
            if (cannonImage != null)
                cannonImage.sprite = cannonRightSprite;

            if (muzzlePoint != null)
            {
                muzzlePoint.anchoredPosition =
                    rightMuzzlePosition;
            }

            if (fuseSparkImage != null)
            {
                fuseSparkImage.rectTransform.anchoredPosition =
                    rightFuseSparkPosition;
            }

            return;
        }

        if (cannonImage != null)
            cannonImage.sprite = cannonStraightSprite;

        if (muzzlePoint != null)
        {
            muzzlePoint.anchoredPosition =
                centerMuzzlePosition;
        }

        if (fuseSparkImage != null)
        {
            fuseSparkImage.rectTransform.anchoredPosition =
                centerFuseSparkPosition;
        }
    }

    private void SetTargetsInteractable(
        bool interactable)
    {
        if (targets == null)
            return;

        for (int i = 0; i < targets.Length; i++)
        {
            if (targets[i] != null)
            {
                targets[i].SetInteractable(
                    interactable
                );
            }
        }
    }

    private void UpdateProgress()
    {
        if (progressText == null)
            return;

        string value =
            correctHitCount +
            " / " +
            requiredCorrectHits;

        progressText.text =
            ConvertToPersianDigits(value);
    }

    private static string ConvertToPersianDigits(
        string value)
    {
        return value
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

    private void PlayEffect(AudioClip clip)
    {
        if (effectsAudioSource != null &&
            clip != null)
        {
            effectsAudioSource.PlayOneShot(clip);
        }
    }

    private void StopOwnedRoutine()
    {
        if (gameRoutine != null)
        {
            StopCoroutine(gameRoutine);
            gameRoutine = null;
        }

        StopAllCoroutines();

        if (ownsInstructionAudio &&
            gameAudioManager != null &&
            gameAudioManager.IsPlayingLocked)
        {
            gameAudioManager.StopAudio();
        }

        ownsInstructionAudio = false;
    }

    private static void SetEffectVisible(
        Graphic effect,
        bool visible)
    {
        if (effect != null)
            effect.gameObject.SetActive(visible);
    }

    private static void AddUniqueValue(
        List<string> values,
        string value)
    {
        if (string.IsNullOrWhiteSpace(value) ||
            values.Contains(value))
        {
            return;
        }

        values.Add(value);
    }

    private static void Shuffle<T>(
        List<T> values)
    {
        for (int i = 0; i < values.Count; i++)
        {
            int randomIndex =
                Random.Range(i, values.Count);

            T temporaryValue = values[i];
            values[i] = values[randomIndex];
            values[randomIndex] = temporaryValue;
        }
    }

    /// <summary>
    /// Shows and animates the burning cannon fuse.
    /// </summary>
    private void StartFuseSpark()
    {
        StopFuseSpark();

        if (fuseSparkImage == null)
            return;

        fuseSparkImage.gameObject.SetActive(true);

        fuseSparkRoutine = StartCoroutine(
            AnimateFuseSpark()
        );
    }

    /// <summary>
    /// Creates a continuous flame flicker using scale,
    /// rotation, and transparency changes.
    /// </summary>
    private IEnumerator AnimateFuseSpark()
    {
        RectTransform sparkRect =
            fuseSparkImage.rectTransform;

        Vector3 originalScale = sparkRect.localScale;
        Quaternion originalRotation =
            sparkRect.localRotation;

        Color originalColor = fuseSparkImage.color;
        float elapsed = 0f;

        while (fuseSparkImage.gameObject.activeSelf)
        {
            elapsed += Time.deltaTime;

            float pulse =
                (Mathf.Sin(
                    elapsed *
                    Mathf.PI *
                    2f /
                    fusePulseDuration
                ) + 1f) * 0.5f;

            float secondPulse =
                (Mathf.Sin(
                    elapsed *
                    Mathf.PI *
                    3.4f /
                    fusePulseDuration
                ) + 1f) * 0.5f;

            sparkRect.localScale =
                originalScale *
                Mathf.Lerp(0.82f, 1.12f, pulse);

            sparkRect.localRotation =
                originalRotation *
                Quaternion.Euler(
                    0f,
                    0f,
                    Mathf.Lerp(-7f, 7f, secondPulse)
                );

            Color animatedColor = originalColor;
            animatedColor.a =
                Mathf.Lerp(0.72f, 1f, secondPulse);

            fuseSparkImage.color = animatedColor;

            yield return null;
        }

        fuseSparkRoutine = null;
    }

    /// <summary>
    /// Stops and hides the burning cannon fuse.
    /// </summary>
    private void StopFuseSpark()
    {
        if (fuseSparkRoutine != null)
        {
            StopCoroutine(fuseSparkRoutine);
            fuseSparkRoutine = null;
        }

        if (fuseSparkImage == null)
            return;

        fuseSparkImage.rectTransform.localScale =
            Vector3.one;

        fuseSparkImage.rectTransform.localRotation =
            Quaternion.identity;

        Color color = fuseSparkImage.color;
        color.a = 1f;
        fuseSparkImage.color = color;

        fuseSparkImage.gameObject.SetActive(false);
    }

    /// <summary>
    /// Shows a short muzzle flash when the cannon fires.
    /// </summary>
    private void PlayMuzzleFlash()
    {
        if (muzzleFlashImage == null)
            return;

        if (muzzleFlashRoutine != null)
        {
            StopCoroutine(muzzleFlashRoutine);
        }

        muzzleFlashRoutine = StartCoroutine(
            AnimateMuzzleFlash()
        );
    }

    /// <summary>
    /// Quickly enlarges and fades the muzzle flash.
    /// </summary>
    private IEnumerator AnimateMuzzleFlash()
    {
        RectTransform flashRect =
            muzzleFlashImage.rectTransform;

        Vector3 originalScale = Vector3.one;

        muzzleFlashImage.gameObject.SetActive(true);
        muzzleFlashImage.color = Color.white;
        flashRect.localScale = Vector3.one * 0.45f;

        float elapsed = 0f;

        while (elapsed < muzzleFlashDuration)
        {
            elapsed += Time.deltaTime;

            float t = Mathf.Clamp01(
                elapsed / muzzleFlashDuration
            );

            flashRect.localScale =
                Vector3.Lerp(
                    Vector3.one * 0.45f,
                    Vector3.one * 1.15f,
                    Mathf.SmoothStep(0f, 1f, t)
                );

            Color color = Color.white;
            color.a = 1f - t;
            muzzleFlashImage.color = color;

            yield return null;
        }

        flashRect.localScale = originalScale;
        muzzleFlashImage.color = Color.white;
        muzzleFlashImage.gameObject.SetActive(false);

        muzzleFlashRoutine = null;
    }
}