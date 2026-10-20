using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class FeedMonsterGame : BonusGameBase
{
    [Header("Instruction")]
    [SerializeField] private AudioClip instructionAudio;
    [SerializeField, Min(1f)]
    private float instructionDuration = 4f;

    [Header("Cards")]
    [SerializeField] private MonsterFoodCardView[] cards;

    [Header("Monster")]
    [SerializeField] private RectTransform monsterRoot;
    [SerializeField] private RectTransform mouthDropZone;
    [SerializeField] private Image mouth;
    [SerializeField] private Image eyes;

    [Header("Mouth Sprites")]
    [SerializeField] private Sprite openMouth;
    [SerializeField] private Sprite closedMouth;
    [SerializeField] private Sprite happyMouth;

    [Header("Eye Sprites")]
    [SerializeField] private Sprite normalEyes;
    [SerializeField] private Sprite happyEyes;
    [SerializeField] private Sprite chewingEyes;
    [SerializeField] private Sprite sadEyes;

    [Header("Idle Animation")]
    [SerializeField] private RectTransform rightArmPivot;
    [SerializeField] private Sprite blinkEyes;
    [SerializeField] private float armAngle = 5f;
    [SerializeField] private float armSpeed = 2f;
    [SerializeField] private RectTransform targetSoundBubble;

    [Header("Target Sound Button")]
    [SerializeField] private Button targetSoundButton;

    [Header("Target")]
    [SerializeField] private TMP_Text targetSoundText;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip correctSound;
    [SerializeField] private AudioClip wrongSound;
    [SerializeField] private GameAudioManager gameAudioManager;
    [SerializeField] private AudioClip chewingSound;

    [Header("Shared Hand Hint")]
    [SerializeField] private UIDragHint dragHint;
    [SerializeField, Min(1f)]
    private float hintIdleDelay = 7f;

    [System.Serializable]
    public class TargetMouthEntry
    {
        public LetterData letter;
        public Sprite mouthSprite;
        public AudioClip monsterSoundAudio;
    }


    [Header("Target Sound Mouths")]
    [SerializeField] private TargetMouthEntry[] targetMouths;
    [SerializeField, Min(1f)]
    private float soundReminderDelay = 9f;

    // Runtime state.
    private Sprite currentTargetMouth;
    private float nextTargetSoundTime;
    private bool speakingTargetSound;
    private AudioClip targetSoundAudio;
    private Vector3 monsterRestScale;
    private Vector3 bubbleRestScale;
    private bool extraMotionSaved;
    private float bubbleTime;
    private Quaternion armRestRotation;
    private bool armRotationSaved;
    private float armTime;
    private float nextBlinkTime;
    private float blinkEndTime;
    private bool blinking;
    private float nextHintTime;
    private bool handHintPlaying;
    private bool ready;
    private bool playing;
    private bool busy;
    private int eatenCount;
    private Vector3 monsterStartPosition;
    private readonly List<MonsterFoodCardView> usedCards = new List<MonsterFoodCardView>();
    // Setup and instruction.
    public override void SetupGame(LessonData lesson, int currentLetterIndex)
    {
        StopGame();
        if (lesson == null || lesson.letters == null || currentLetterIndex < 0 || currentLetterIndex >= lesson.letters.Length)
        {
            Debug.LogError("Feed Monster: Invalid lesson.");
            return;
        }

        LetterData data = lesson.letters[currentLetterIndex];
        targetSoundAudio = data != null ? data.letterSoundAudio : null;
        currentTargetMouth = null;
        if (targetMouths != null)
        {
            foreach (TargetMouthEntry entry in targetMouths)
            {
                if (entry != null && entry.letter == data)
                {
                    currentTargetMouth = entry.mouthSprite;

                    if (entry.monsterSoundAudio != null)
                        targetSoundAudio = entry.monsterSoundAudio;

                    break;
                }
            }
        }

        if (targetSoundButton != null)
        {
            targetSoundButton.onClick.RemoveListener(PlayTargetSound);
            targetSoundButton.onClick.AddListener(PlayTargetSound);
            targetSoundButton.interactable = false;
        }

        if (data == null || cards == null || cards.Length != 6 || mouthDropZone == null || monsterRoot == null)
        {
            Debug.LogError("Feed Monster: Check scene references.");
            return;
        }

        foreach (MonsterFoodCardView card in cards)
        {
            if (card == null)
            {
                Debug.LogError("Feed Monster: A card is missing.");
                return;
            }
        }

        List<ExampleData> correct = new List<ExampleData>();
        List<ExampleData> wrong = new List<ExampleData>();
        if (data.examples != null)
        {
            foreach (ExampleData example in data.examples)
            {
                if (example == null || example.image == null)
                    continue;
                // This game asks whether the word contains the sound.
                if (example.containsTargetSound)
                    AddUnique(correct, example);
                else
                    AddUnique(wrong, example);
            }
        }

        if (data.pictureHuntDistractors != null)
        {
            foreach (ExampleData example in data.pictureHuntDistractors)
            {
                if (example != null && example.image != null && !example.containsTargetSound)
                {
                    AddUnique(wrong, example);
                }
            }
        }

        wrong.RemoveAll(item => correct.Exists(answer => SameExample(item, answer)));
        if (correct.Count < 3 || wrong.Count < 3)
        {
            Debug.LogError($"Feed Monster | Lesson: {lesson.name} | " + $"Index: {currentLetterIndex} | Letter: {data.name} | " + $"Correct: {correct.Count} | Wrong: {wrong.Count}");
            return;
        }

        Shuffle(correct);
        Shuffle(wrong);
        List<(ExampleData example, bool correct)> selected = new List<(ExampleData, bool)>();
        for (int i = 0; i < 3; i++)
        {
            selected.Add((correct[i], true));
            selected.Add((wrong[i], false));
        }

        Shuffle(selected);
        monsterStartPosition = monsterRoot.localPosition;
        eatenCount = 0;
        if (targetSoundText != null)
        {
            targetSoundText.richText = true;
            targetSoundText.text = data.GetGameDisplayText();
        }

        for (int i = 0; i < cards.Length; i++)
        {
            cards[i].Setup(selected[i].example, selected[i].correct, CanDrag, HandleDropped);
            usedCards.Add(cards[i]);
            cards[i].SetInteractable(false);
        }

        SetFace(normalEyes, happyMouth);
        ready = true;
        ShowWaitingState();
    }

    public override void BeginGame()
    {
        if (!ready || playing || busy)
            return;
        busy = true;
        StartCoroutine(ShowInstructionThenPlay());
    }

    private IEnumerator ShowInstructionThenPlay()
    {
        playing = false;
        foreach (MonsterFoodCardView card in cards)
            card.SetInteractable(false);
        ShowInstructionState();
        float startedAt = Time.unscaledTime;
        // Play the instruction if a voice clip is assigned.
        yield return PlayClip(instructionAudio);
        // Keep the guide visible for at least four seconds.
        float remaining = instructionDuration - (Time.unscaledTime - startedAt);
        if (remaining > 0f)
            yield return new WaitForSecondsRealtime(remaining);
        if (!ready)
            yield break;
        ShowGameplayState();
        playing = true;
        busy = false;
        ResetIdleAnimation();
        foreach (MonsterFoodCardView card in cards)
            card.SetInteractable(true);
        nextTargetSoundTime = Time.unscaledTime + 0.7f;
        nextHintTime = Time.unscaledTime + 2f;
    }

    // Card interaction and feedback.
    private bool CanDrag()
    {
        return playing && !busy && (gameAudioManager == null || !gameAudioManager.IsPlayingLocked);
    }

    private void HandleDropped(MonsterFoodCardView card, PointerEventData eventData)
    {
        if (!CanDrag())
        {
            card.ReturnHome();
            return;
        }

        bool overMouth = RectTransformUtility.RectangleContainsScreenPoint(mouthDropZone, eventData.position, eventData.pressEventCamera);
        if (!overMouth)
        {
            card.ReturnHome();
            return;
        }

        // Lock the round before starting feedback.
        busy = true;
        StopHandHint();
        SetFace(normalEyes, happyMouth);
        card.SetInteractable(false);
        StartCoroutine(ResolveFood(card));
    }

    private IEnumerator ResolveFood(MonsterFoodCardView card)
    {
        yield return PlayClip(card.Example.audio);
        if (card.IsCorrect)
        {
            SetFace(normalEyes, openMouth);
            RectTransform rect = card.CardRect;
            Vector3 startPosition = rect.position;
            Vector3 startScale = rect.localScale;
            float elapsed = 0f;
            const float duration = 0.4f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                rect.position = Vector3.Lerp(startPosition, mouthDropZone.position, t);
                rect.localScale = Vector3.Lerp(startScale, startScale * 0.08f, t);
                yield return null;
            }

            card.gameObject.SetActive(false);
            if (audioSource != null && audioSource.isActiveAndEnabled && chewingSound != null)
            {
                audioSource.clip = chewingSound;
                audioSource.loop = false;
                audioSource.Play();
            }

            SetFace(chewingEyes, closedMouth);
            for (int i = 0; i < 3; i++)
            {
                const float chewDuration = 0.28f;
                float chewTime = 0f;
                SetFace(chewingEyes, closedMouth);
                while (chewTime < chewDuration)
                {
                    chewTime += Time.deltaTime;
                    float t = Mathf.Clamp01(chewTime / chewDuration);
                    float movement = Mathf.Sin(t * Mathf.PI);
                    // A tiny squash and stretch while chewing.
                    monsterRoot.localScale = Vector3.Scale(monsterRestScale, new Vector3(1f + movement * 0.015f, 1f - movement * 0.02f, 1f));
                    if (t >= 0.57f)
                        SetFace(chewingEyes, openMouth);
                    yield return null;
                }
            }

            monsterRoot.localScale = monsterRestScale;
            if (audioSource != null)
            {
                while (audioSource.isPlaying)
                    yield return null;
            }

            SetFace(happyEyes, happyMouth);
            yield return PlayClip(correctSound);
            eatenCount++;
            yield return new WaitForSeconds(0.35f);
            if (eatenCount >= 3)
            {
                playing = false;
                StopHandHint();
                if (targetSoundButton != null)
                    targetSoundButton.interactable = false;
                foreach (MonsterFoodCardView remaining in cards)
                    remaining.SetInteractable(false);
                busy = false;
                NotifyGameplayCompleted();
                yield break;
            }
        }
        else
        {
            card.ShowWrongOutline(true);
            SetFace(sadEyes, closedMouth);
            yield return PlayClip(wrongSound);
            float elapsed = 0f;
            while (elapsed < 0.4f)
            {
                elapsed += Time.deltaTime;
                monsterRoot.localPosition = monsterStartPosition + Vector3.right * Mathf.Sin(elapsed * 45f) * 7f;
                yield return null;
            }

            monsterRoot.localPosition = monsterStartPosition;
            card.ReturnHome();
        }

        SetFace(normalEyes, happyMouth);
        busy = false;
        nextTargetSoundTime = Time.unscaledTime + soundReminderDelay;
        nextHintTime = Time.unscaledTime + hintIdleDelay;
    }

    // Local sound effects; background music is managed by BonusGameBase.
    private IEnumerator PlayClip(AudioClip clip)
    {
        if (audioSource == null || !audioSource.isActiveAndEnabled || clip == null)
            yield break;
        audioSource.clip = clip;
        audioSource.loop = false;
        audioSource.Play();
        while (audioSource.isPlaying)
            yield return null;
    }

    private void SetFace(Sprite eyeSprite, Sprite mouthSprite)
    {
        if (eyes != null && eyeSprite != null)
            eyes.sprite = eyeSprite;
        if (mouth != null && mouthSprite != null)
        {
            mouth.sprite = mouthSprite;
            mouth.preserveAspect = true;
        }
    }

    public override void StopGame()
    {
        playing = false;
        busy = false;
        ready = false;
        speakingTargetSound = false;
        SetFace(normalEyes, happyMouth);
        if (targetSoundButton != null)
            targetSoundButton.interactable = false;
        StopHandHint();
        StopAllCoroutines();
        ResetIdleAnimation();
        if (audioSource != null)
            audioSource.Stop();
        foreach (MonsterFoodCardView card in usedCards)
        {
            if (card == null)
                continue;
            card.ReturnHome();
            card.SetInteractable(false);
        }

        if (usedCards.Count > 0 && monsterRoot != null)
            monsterRoot.localPosition = monsterStartPosition;
        usedCards.Clear();
        HideGameState();
    }

    private void OnDisable()
    {
        StopGame();
    }

    private static bool SameExample(ExampleData a, ExampleData b)
    {
        return a.image == b.image || (!string.IsNullOrWhiteSpace(a.word) && string.Equals(a.word.Trim(), b.word?.Trim(), System.StringComparison.Ordinal));
    }

    private static void AddUnique(List<ExampleData> list, ExampleData example)
    {
        if (!list.Exists(item => SameExample(item, example)))
            list.Add(example);
    }

    private static void Shuffle<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            T temp = list[i];
            list[i] = list[j];
            list[j] = temp;
        }
    }

    // Idle motion and blinking.
    private void ResetIdleAnimation()
    {
        if (!extraMotionSaved && monsterRoot != null)
        {
            monsterRestScale = monsterRoot.localScale;
            if (targetSoundBubble != null)
                bubbleRestScale = targetSoundBubble.localScale;
            extraMotionSaved = true;
        }

        if (extraMotionSaved && monsterRoot != null)
        {
            monsterRoot.localScale = monsterRestScale;
            if (targetSoundBubble != null)
                targetSoundBubble.localScale = bubbleRestScale;
        }

        bubbleTime = 0f;
        if (rightArmPivot != null)
        {
            if (!armRotationSaved)
            {
                armRestRotation = rightArmPivot.localRotation;
                armRotationSaved = true;
            }

            rightArmPivot.localRotation = armRestRotation;
        }

        armTime = 0f;
        blinking = false;
        nextBlinkTime = Time.time + Random.Range(2f, 4f);
    }

    private void Update()
    {
        UpdateTargetSoundReminder();
        UpdateHandHint();
        if (targetSoundButton != null)
        {
            targetSoundButton.interactable = CanDrag() && targetSoundAudio != null && audioSource != null && audioSource.isActiveAndEnabled && !IsAnyCardDragging();
        }

        if (!playing)
            return;
        if (targetSoundBubble != null && extraMotionSaved)
        {
            bubbleTime += Time.deltaTime;
            float pulse = speakingTargetSound ? 1f + Mathf.Sin(bubbleTime * 8f) * 0.05f : 1f + Mathf.Sin(bubbleTime * 2.5f) * 0.025f;
            targetSoundBubble.localScale = bubbleRestScale * pulse;
        }

        // Feedback expressions take priority over idle animation.
        if (busy)
        {
            if (rightArmPivot != null && armRotationSaved)
                rightArmPivot.localRotation = armRestRotation;
            blinking = false;
            nextBlinkTime = Time.time + Random.Range(2f, 4f);
            return;
        }

        if (rightArmPivot != null && armRotationSaved)
        {
            armTime += Time.deltaTime;
            float angle = Mathf.Sin(armTime * armSpeed) * armAngle;
            rightArmPivot.localRotation = armRestRotation * Quaternion.Euler(0f, 0f, angle);
        }

        if (eyes == null || blinkEyes == null || normalEyes == null)
            return;
        if (blinking)
        {
            if (Time.time >= blinkEndTime)
            {
                eyes.sprite = normalEyes;
                blinking = false;
                nextBlinkTime = Time.time + Random.Range(2f, 4f);
            }
        }
        else if (Time.time >= nextBlinkTime)
        {
            eyes.sprite = blinkEyes;
            blinking = true;
            blinkEndTime = Time.time + 0.12f;
        }
    }

    // Target sound, reminders, and the shared drag hint.
    private bool IsAnyCardDragging()
    {
        if (cards == null)
            return false;
        foreach (MonsterFoodCardView card in cards)
        {
            if (card != null && card.IsDragging)
                return true;
        }

        return false;
    }

    private void UpdateTargetSoundReminder()
    {
        if (!playing)
            return;
        if (!CanDrag() || IsAnyCardDragging())
        {
            nextTargetSoundTime = Time.unscaledTime + soundReminderDelay;
            return;
        }

        // Let an active hand demonstration finish before speaking.
        if (handHintPlaying || Time.unscaledTime < nextTargetSoundTime)
            return;
        nextTargetSoundTime = Time.unscaledTime + soundReminderDelay;
        PlayTargetSound();
    }

    public void PlayTargetSound()
    {
        if (!CanDrag() || IsAnyCardDragging() || targetSoundAudio == null || audioSource == null || !audioSource.isActiveAndEnabled)
        {
            return;
        }

        busy = true;
        speakingTargetSound = true;
        StopHandHint();
        StartCoroutine(PlayTargetSoundRoutine());
    }

    private IEnumerator PlayTargetSoundRoutine()
    {
        SetFace(normalEyes, currentTargetMouth != null ? currentTargetMouth : openMouth);
        yield return PlayClip(targetSoundAudio);
        SetFace(normalEyes, happyMouth);
        speakingTargetSound = false;
        busy = false;
        nextTargetSoundTime = Time.unscaledTime + soundReminderDelay;
        nextHintTime = Time.unscaledTime + 1f;
    }

    private void UpdateHandHint()
    {
        if (dragHint == null)
            return;
        if (!playing || !CanDrag() || IsAnyCardDragging())
        {
            StopHandHint();
            nextHintTime = Time.unscaledTime + hintIdleDelay;
            return;
        }

        if (handHintPlaying || Time.unscaledTime < nextHintTime)
            return;
        foreach (MonsterFoodCardView card in cards)
        {
            if (card == null || !card.gameObject.activeInHierarchy || !card.IsCorrect)
                continue;
            Vector3 start = card.CardRect.TransformPoint(card.CardRect.rect.center);
            Vector3 destination = mouthDropZone.TransformPoint(mouthDropZone.rect.center);
            handHintPlaying = true;
            nextHintTime = float.PositiveInfinity;
            dragHint.Play(start, destination, () =>
            {
                handHintPlaying = false;
                nextHintTime = Time.unscaledTime + hintIdleDelay;
            });
            return;
        }

        nextHintTime = Time.unscaledTime + hintIdleDelay;
    }

    private void StopHandHint()
    {
        handHintPlaying = false;
        if (dragHint != null)
            dragHint.StopHint();
    }
}
