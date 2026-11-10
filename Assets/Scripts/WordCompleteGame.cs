using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class WordCompleteGame : BonusGameBase
{
    [Header("Example")]
    [SerializeField] private Image exampleImage;
    [SerializeField] private TMP_Text wordText;
    [SerializeField] private RectTransform dropTarget;

    [Header("Draggable Piece")]
    [SerializeField] private WordCompletePiece draggablePiece;
    [SerializeField] private TMP_Text pieceText;

    [Header("Navigation")]
    [SerializeField] private Button previousButton;
    [SerializeField] private Button nextButton;
    [SerializeField] private Image[] progressDots;
    [SerializeField] private Sprite unfinishedDot;
    [SerializeField] private Sprite completedDot;

    [Header("Audio")]
    [SerializeField] private GameAudioManager gameAudioManager;
    [SerializeField] private AudioSource effectsSource;
    [SerializeField] private AudioClip correctSound;
    [SerializeField] private AudioClip wrongSound;

    [Header("Timing")]
    [SerializeField, Min(0f)]
    private float correctPause = 2f;

    [SerializeField, Min(0f)]
    private float instructionDuration = 5f;

    [Header("Target Position")]
    [SerializeField] private float shaddaGap = 12f;

    [Header("Drag Hint")]
    [SerializeField] private UIDragHint dragHint;

    [SerializeField, Min(0f)]
    private float firstHintDelay = 3f;

    [Header("Instruction")]
    [SerializeField] private TMP_Text instructionText;

    [Header("Piece Font Size")]
    [SerializeField] private float shaddaPieceFontSize = 140f;
    [SerializeField] private float wawPieceFontSize = 75f;

    private const int RequiredExampleCount = 3;

    private sealed class Round
    {
        public ExampleData Example;
        public string IncompleteWord;
        public char TargetCharacter;
        public int TargetOccurrence;
        public bool Solved;
    }

    private readonly List<Round> rounds = new List<Round>();
    private readonly List<int> matchingGlyphs = new List<int>();

    private TMP_Text subscribedWordText;

    private int currentIndex;
    private bool isShaddaLesson;
    private bool running;
    private bool busy;
    private bool layoutReady;
    private bool targetFound;
    private bool firstHintScheduled;
    private bool meshCallbackReceived;
    private bool ownsWordAudio;

    private Coroutine layoutRoutine;
    private Coroutine hintRoutine;

    private void BindReferences()
    {
        if (previousButton != null)
        {
            previousButton.onClick.RemoveListener(ShowPrevious);
            previousButton.onClick.AddListener(ShowPrevious);
        }

        if (nextButton != null)
        {
            nextButton.onClick.RemoveListener(ShowNext);
            nextButton.onClick.AddListener(ShowNext);
        }

        if (subscribedWordText != null)
        {
            subscribedWordText.OnPreRenderText -=
                HandleWordMesh;
        }

        subscribedWordText = wordText;

        if (subscribedWordText != null)
        {
            subscribedWordText.OnPreRenderText +=
                HandleWordMesh;
        }

        if (draggablePiece != null)
        {
            draggablePiece.DragStarted -= StopDragHint;
            draggablePiece.DragStarted += StopDragHint;
        }
    }

    public override void SetupGame(
        LessonData lesson,
        int currentLetterIndex)
    {
        StopGame();

        rounds.Clear();
        firstHintScheduled = false;
        currentIndex = 0;

        if (lesson == null ||
            lesson.letters == null ||
            currentLetterIndex < 0 ||
            currentLetterIndex >= lesson.letters.Length)
        {
            Debug.LogError("Word Complete: Invalid lesson.");
            return;
        }

        LetterData data = lesson.letters[currentLetterIndex];

        if (data == null || !data.UsesWordCompleteGame)
        {
            Debug.LogError(
                "Word Complete needs a supported exception lesson."
            );
            return;
        }

        if (exampleImage == null ||
            wordText == null ||
            dropTarget == null ||
            draggablePiece == null ||
            pieceText == null)
        {
            Debug.LogError(
                "Word Complete: Assign the UI references."
            );
            return;
        }

        BindReferences();

        isShaddaLesson = data.IsTashdid;

        if (instructionText != null)
        {
            instructionText.text = isShaddaLesson
                ? GameTexts.Get(
                    "word_complete_shadda_instruction",
                    "جای تشدید خالیه!\nتشدید رو بکش و کلمه رو کامل کن!"
                )
                : GameTexts.Get(
                    "word_complete_silent_waw_instruction",
                    "جای «و» خالیه!\nواو ناخوانا رو بکش و کلمه رو کامل کن!"
                );
        }

        HashSet<string> usedWords = new HashSet<string>();

        if (data.examples != null)
        {
            foreach (ExampleData example in data.examples)
            {
                if (example == null ||
                    example.image == null ||
                    string.IsNullOrWhiteSpace(example.word))
                {
                    continue;
                }

                string wordKey = example.word.Trim();

                if (usedWords.Contains(wordKey))
                    continue;

                if (!TryCreateRound(example, out Round round))
                    continue;

                rounds.Add(round);
                usedWords.Add(wordKey);

                if (rounds.Count == RequiredExampleCount)
                    break;
            }
        }

        if (rounds.Count != RequiredExampleCount)
        {
            Debug.LogError(
                "Word Complete needs three different valid examples. " +
                "Shadda words must contain U+0651. " +
                "Silent Waw words must contain Kha followed by Waw."
            );

            rounds.Clear();
            return;
        }

        pieceText.richText = true;
        pieceText.enableAutoSizing = false;

        pieceText.fontSize = isShaddaLesson
            ? shaddaPieceFontSize
            : wawPieceFontSize;

        pieceText.text = isShaddaLesson
            ? "\u0651"
            : PersianLetterDisplay.GetText("\u0648");

        ShowWaitingState();
    }

    public override void BeginGame()
    {
        if (rounds.Count != RequiredExampleCount || running)
            return;

        running = true;
        busy = true;

        StartCoroutine(BeginRoutine());
    }

    private IEnumerator BeginRoutine()
    {
        ShowInstructionState();

        if (instructionDuration > 0f)
        {
            yield return new WaitForSecondsRealtime(
                instructionDuration
            );
        }

        if (!running)
            yield break;

        ShowGameplayState();

        draggablePiece.gameObject.SetActive(true);

        draggablePiece.Initialize(
            CanDrag,
            HandlePieceDropped
        );

        busy = false;
        ShowRound(0);
    }

    public override void StopGame()
    {
        running = false;
        busy = false;
        layoutReady = false;
        targetFound = false;

        StopDragHint();
        StopAllCoroutines();
        layoutRoutine = null;

        if (ownsWordAudio &&
            gameAudioManager != null &&
            gameAudioManager.IsPlayingLocked)
        {
            gameAudioManager.StopAudio();
        }

        ownsWordAudio = false;

        if (draggablePiece != null)
        {
            draggablePiece.ReturnHome();
            draggablePiece.SetInteractable(false);
        }

        HideGameState();
    }

    private bool CanDrag()
    {
        return running &&
               !busy &&
               layoutReady &&
               targetFound &&
               currentIndex >= 0 &&
               currentIndex < rounds.Count &&
               !rounds[currentIndex].Solved &&
               !IsAudioLocked();
    }

    private bool IsAudioLocked()
    {
        return gameAudioManager != null &&
               gameAudioManager.IsPlayingLocked;
    }

    private void ShowRound(int index)
    {
        if (index < 0 || index >= rounds.Count)
            return;

        StopDragHint();

        if (layoutRoutine != null)
        {
            StopCoroutine(layoutRoutine);
            layoutRoutine = null;
        }

        layoutReady = false;
        targetFound = false;
        currentIndex = index;

        Round round = rounds[index];

        draggablePiece.ReturnHome();
        draggablePiece.SetInteractable(false);

        exampleImage.sprite = round.Example.image;
        exampleImage.preserveAspect = true;
        exampleImage.color = Color.white;

        wordText.richText = true;

        wordText.text = PersianLetterDisplay.GetWordText(
            round.Solved
                ? round.Example.word
                : round.IncompleteWord
        );

        dropTarget.gameObject.SetActive(false);
        draggablePiece.gameObject.SetActive(!round.Solved);

        RefreshNavigation();

        layoutRoutine = StartCoroutine(
            RefreshWordLayout()
        );
    }

    private IEnumerator RefreshWordLayout()
    {
        yield return null;
        yield return new WaitForEndOfFrame();

        if (!running ||
            currentIndex < 0 ||
            currentIndex >= rounds.Count)
        {
            layoutRoutine = null;
            yield break;
        }

        layoutReady = true;

        // Explicitly locate and hide the target after RTL layout.
        RebuildWordDisplay();

        Round round = rounds[currentIndex];

        draggablePiece.SetInteractable(
            !round.Solved && targetFound
        );

        if (!round.Solved && !targetFound)
        {
            Debug.LogError(
                "Word Complete: Target glyph was not found. Word: " +
                round.Example.word +
                " | Target: U+" +
                ((int)round.TargetCharacter).ToString("X4") +
                " | Rendered characters: " +
                GetRenderedCharacterCodes()
            );
        }

        if (dragHint != null &&
            currentIndex == 0 &&
            targetFound &&
            !round.Solved &&
            !firstHintScheduled)
        {
            firstHintScheduled = true;

            hintRoutine = StartCoroutine(
                PlayFirstHintAfterDelay()
            );
        }

        layoutRoutine = null;
    }

    private void RebuildWordDisplay()
    {
        if (wordText == null)
            return;

        meshCallbackReceived = false;

        wordText.ForceMeshUpdate();

        TMP_TextInfo info = wordText.textInfo;

        // Keep shared spacing even if the render event did not run.
        if (!meshCallbackReceived)
            PersianLetterDisplay.ApplyWordMarkSpacing(info);

        // Gameplay does not depend solely on OnPreRenderText.
        ApplyTargetToMesh(info);

        wordText.UpdateVertexData(
            TMP_VertexDataUpdateFlags.All
        );
    }

    private void HandleWordMesh(TMP_TextInfo textInfo)
    {
        meshCallbackReceived = true;

        PersianLetterDisplay.ApplyWordMarkSpacing(textInfo);

        ApplyTargetToMesh(textInfo);
    }

    private void ApplyTargetToMesh(TMP_TextInfo textInfo)
    {
        if (!running ||
            !layoutReady ||
            currentIndex < 0 ||
            currentIndex >= rounds.Count)
        {
            return;
        }

        Round round = rounds[currentIndex];

        if (round.Solved)
        {
            targetFound = false;
            dropTarget.gameObject.SetActive(false);
            return;
        }

        matchingGlyphs.Clear();

        for (int i = 0; i < textInfo.characterCount; i++)
        {
            TMP_CharacterInfo character =
                textInfo.characterInfo[i];

            if (!character.isVisible)
                continue;

            if (MatchesTarget(character, round.TargetCharacter))
                matchingGlyphs.Add(i);
        }

        matchingGlyphs.Sort((first, second) =>
            textInfo.characterInfo[second].bottomLeft.x.CompareTo(
                textInfo.characterInfo[first].bottomLeft.x
            )
        );

        if (round.TargetOccurrence < 0 ||
            round.TargetOccurrence >= matchingGlyphs.Count)
        {
            targetFound = false;
            dropTarget.gameObject.SetActive(false);
            return;
        }

        TMP_CharacterInfo target = textInfo.characterInfo[
            matchingGlyphs[round.TargetOccurrence]
        ];

        float x =
            (target.bottomLeft.x + target.topRight.x) * 0.5f;

        float y = isShaddaLesson
            ? target.topRight.y + shaddaGap
            : (target.bottomLeft.y + target.topRight.y) * 0.5f;

        dropTarget.position =
            wordText.rectTransform.TransformPoint(
                new Vector3(x, y, 0f)
            );

        targetFound = true;
        dropTarget.gameObject.SetActive(true);

        if (!isShaddaLesson)
        {
            int materialIndex = target.materialReferenceIndex;
            int vertexIndex = target.vertexIndex;

            Vector3[] vertices =
                textInfo.meshInfo[materialIndex].vertices;

            Color32[] colors =
                textInfo.meshInfo[materialIndex].colors32;

            Vector3 hiddenPosition = vertices[vertexIndex];

            for (int vertex = 0; vertex < 4; vertex++)
            {
                vertices[vertexIndex + vertex] = hiddenPosition;
                colors[vertexIndex + vertex].a = 0;
            }
        }
    }

    private static bool MatchesTarget(
        TMP_CharacterInfo character,
        char target)
    {
        if (NormalizeCharacter(character.character) == target)
            return true;

        if (character.textElement != null)
        {
            uint unicode = character.textElement.unicode;

            if (unicode <= char.MaxValue)
            {
                return NormalizeCharacter((char)unicode) == target;
            }
        }

        return false;
    }

    private IEnumerator PlayFirstHintAfterDelay()
    {
        yield return new WaitForSecondsRealtime(firstHintDelay);

        hintRoutine = null;

        if (!CanDrag() ||
            currentIndex != 0 ||
            draggablePiece.IsDragging ||
            dragHint == null)
        {
            yield break;
        }

        dragHint.Play(
            draggablePiece.transform.position,
            dropTarget.position
        );
    }

    private void StopDragHint()
    {
        if (hintRoutine != null)
        {
            StopCoroutine(hintRoutine);
            hintRoutine = null;
        }

        if (dragHint != null)
            dragHint.StopHint();
    }

    private void HandlePieceDropped(
        Vector2 screenPosition,
        Camera eventCamera)
    {
        if (!CanDrag())
            return;

        bool insideTarget =
            RectTransformUtility.RectangleContainsScreenPoint(
                dropTarget,
                screenPosition,
                eventCamera
            );

        if (!insideTarget)
        {
            draggablePiece.ShowWrongFeedback();

            if (effectsSource != null && wrongSound != null)
                effectsSource.PlayOneShot(wrongSound);

            return;
        }

        StartCoroutine(CompleteCurrentRound());
    }

    private IEnumerator CompleteCurrentRound()
    {
        busy = true;
        StopDragHint();

        Round round = rounds[currentIndex];
        round.Solved = true;

        draggablePiece.SetInteractable(false);
        draggablePiece.gameObject.SetActive(false);
        dropTarget.gameObject.SetActive(false);

        wordText.text = PersianLetterDisplay.GetWordText(
            round.Example.word
        );

        // Rebuild even when the underlying string is unchanged.
        // This restores the hidden Waw.
        RebuildWordDisplay();

        RefreshNavigation();

        if (effectsSource != null && correctSound != null)
        {
            effectsSource.PlayOneShot(correctSound);

            yield return new WaitForSecondsRealtime(
                correctSound.length
            );
        }

        AudioClip wordAudio = round.Example.audio;

        if (wordAudio != null)
        {
            if (gameAudioManager != null)
            {
                ownsWordAudio = true;

                gameAudioManager.PlayLocked(wordAudio);

                yield return new WaitWhile(
                    () => gameAudioManager != null &&
                          gameAudioManager.IsPlayingLocked
                );

                ownsWordAudio = false;
            }
            else if (effectsSource != null)
            {
                effectsSource.PlayOneShot(wordAudio);

                yield return new WaitForSecondsRealtime(
                    wordAudio.length
                );
            }
        }

        if (correctPause > 0f)
        {
            yield return new WaitForSecondsRealtime(
                correctPause
            );
        }

        bool allSolved = true;

        foreach (Round item in rounds)
        {
            if (!item.Solved)
            {
                allSolved = false;
                break;
            }
        }

        if (allSolved)
        {
            running = false;
            busy = false;

            RefreshNavigation();
            NotifyGameplayCompleted();
            yield break;
        }

        int nextIndex = currentIndex + 1;

        if (nextIndex >= rounds.Count ||
            rounds[nextIndex].Solved)
        {
            nextIndex = rounds.FindIndex(
                item => !item.Solved
            );
        }

        busy = false;
        ShowRound(nextIndex);
    }

    private void ShowPrevious()
    {
        if (!CanNavigate() || currentIndex <= 0)
            return;

        ShowRound(currentIndex - 1);
    }

    private void ShowNext()
    {
        if (!CanNavigate() ||
            currentIndex < 0 ||
            currentIndex + 1 >= rounds.Count ||
            !rounds[currentIndex].Solved)
        {
            return;
        }

        ShowRound(currentIndex + 1);
    }

    private bool CanNavigate()
    {
        return running &&
               !busy &&
               draggablePiece != null &&
               !draggablePiece.IsDragging &&
               !IsAudioLocked();
    }

    private void LateUpdate()
    {
        UpdateNavigationButtons();
    }

    private void UpdateNavigationButtons()
    {
        bool canNavigate = CanNavigate();

        if (previousButton != null)
        {
            previousButton.interactable =
                canNavigate && currentIndex > 0;
        }

        if (nextButton != null)
        {
            nextButton.interactable =
                canNavigate &&
                currentIndex >= 0 &&
                currentIndex + 1 < rounds.Count &&
                rounds[currentIndex].Solved;
        }
    }

    private void RefreshNavigation()
    {
        UpdateNavigationButtons();

        if (progressDots == null)
            return;

        for (int i = 0; i < progressDots.Length; i++)
        {
            Image dot = progressDots[i];

            if (dot == null)
                continue;

            bool hasRound = i < rounds.Count;
            dot.gameObject.SetActive(hasRound);

            if (!hasRound)
                continue;

            Sprite sprite = rounds[i].Solved
                ? completedDot
                : unfinishedDot;

            if (sprite != null)
                dot.sprite = sprite;

            dot.rectTransform.localScale =
                Vector3.one * (i == currentIndex ? 1.2f : 1f);
        }
    }

    private bool TryCreateRound(
        ExampleData example,
        out Round round)
    {
        round = null;

        string word = example.word.Trim();
        string incompleteWord = word;
        int targetIndex = -1;

        if (isShaddaLesson)
        {
            int markIndex = word.IndexOf('\u0651');

            if (markIndex < 0)
                return false;

            targetIndex = markIndex - 1;

            while (targetIndex >= 0 &&
                   IsCombiningMark(word[targetIndex]))
            {
                targetIndex--;
            }

            incompleteWord = word.Remove(markIndex, 1);
        }
        else
        {
            for (int i = 0; i < word.Length; i++)
            {
                // Waw: U+0648
                if (NormalizeCharacter(word[i]) != '\u0648')
                    continue;

                int previousIndex = i - 1;

                while (previousIndex >= 0 &&
                       IsCombiningMark(word[previousIndex]))
                {
                    previousIndex--;
                }

                // Kha: U+062E
                if (previousIndex >= 0 &&
                    NormalizeCharacter(word[previousIndex]) == '\u062E')
                {
                    targetIndex = i;
                    break;
                }
            }
        }

        if (targetIndex < 0 ||
            !char.IsLetter(word[targetIndex]))
        {
            return false;
        }

        char targetCharacter =
            NormalizeCharacter(word[targetIndex]);

        int occurrence = 0;

        for (int i = 0; i < targetIndex; i++)
        {
            if (NormalizeCharacter(word[i]) == targetCharacter)
                occurrence++;
        }

        round = new Round
        {
            Example = example,
            IncompleteWord = incompleteWord,
            TargetCharacter = targetCharacter,
            TargetOccurrence = occurrence
        };

        return true;
    }

    private static bool IsCombiningMark(char character)
    {
        UnicodeCategory category =
            char.GetUnicodeCategory(character);

        return category == UnicodeCategory.NonSpacingMark ||
               category == UnicodeCategory.SpacingCombiningMark ||
               category == UnicodeCategory.EnclosingMark;
    }

    private static char NormalizeCharacter(char character)
    {
        // Explicit handling for isolated/final Waw.
        if (character == '\uFEED' || character == '\uFEEE')
            return '\u0648';

        string normalized = character.ToString().Normalize(
            NormalizationForm.FormKC
        );

        if (normalized.Length != 1)
            return '\0';

        switch (normalized[0])
        {
            case '\u064A':
            case '\u0649':
                return '\u06CC';

            case '\u0643':
                return '\u06A9';

            default:
                return normalized[0];
        }
    }

    private string GetRenderedCharacterCodes()
    {
        if (wordText == null)
            return string.Empty;

        StringBuilder result = new StringBuilder();

        TMP_TextInfo info = wordText.textInfo;

        for (int i = 0; i < info.characterCount; i++)
        {
            if (i > 0)
                result.Append(" ");

            result.Append("U+");
            result.Append(
                ((int)info.characterInfo[i].character)
                .ToString("X4")
            );
        }

        return result.ToString();
    }

    private void OnDisable()
    {
        StopGame();
    }

    private void OnDestroy()
    {
        if (subscribedWordText != null)
        {
            subscribedWordText.OnPreRenderText -=
                HandleWordMesh;
        }

        if (previousButton != null)
            previousButton.onClick.RemoveListener(ShowPrevious);

        if (nextButton != null)
            nextButton.onClick.RemoveListener(ShowNext);

        if (draggablePiece != null)
            draggablePiece.DragStarted -= StopDragHint;
    }
}