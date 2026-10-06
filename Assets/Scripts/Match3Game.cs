using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class Match3Game : BonusGameBase
{
    private const int RowCount = 6;
    private const int ColumnCount = 6;
    private const int FamilyCount = 5;

    [Header("Board")]
    [SerializeField] private RectTransform boardGrid;
    [SerializeField] private Match3TileView tilePrefab;
    [SerializeField] private AlphabetData alphabetData;

    [Header("Target Display")]
    [SerializeField] private TMP_Text targetDisplayText;
    [Header("Target Progress")]
    [SerializeField] private GameObject[] progressMarkers;
    [SerializeField] private GameObject closedChest;
    [SerializeField] private GameObject openChest;

    [SerializeField, Min(1)]
    private int requiredTargetCount = 8;

    [Header("Instructions")]
    [SerializeField]
    private AudioClip instructionVoice;

    [SerializeField]
    private GameAudioManager gameAudioManager;

    [SerializeField, Min(0f)]
    private float instructionFallbackDuration = 2.5f;

    [Header("Music")]
    [SerializeField] private AudioSource musicSource;

    [Header("Match Feedback")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip correctSound;
    [SerializeField] private AudioClip wrongSound;

    [SerializeField, Min(0f)]
    private float matchFeedbackDuration = 0.55f;
    [SerializeField, Min(0.05f)]
    private float matchFadeDuration = 0.25f;
    [SerializeField, Min(0.05f)]
    private float tileFallDuration = 0.35f;

    [Header("Board Shuffle")]
    [SerializeField, Min(1)]
    private int maximumShuffleAttempts = 100;

    [SerializeField, Min(0.1f)]
    private float shuffleMoveDuration = 0.65f;

    [SerializeField]
    private AudioClip shuffleSound;

    [Header("Completion")]
    [SerializeField] private AudioClip completionSound;

    [SerializeField, Min(0.1f)]
    private float chestAnimationDuration = 0.5f;

    [Header("Testing")]
    [SerializeField] private LessonData testLesson;

    [SerializeField, Min(0)]
    private int testLetterIndex;

    [SerializeField]
    private bool buildOnStartForTesting;

    private readonly List<LetterData> roundFamilies =
        new List<LetterData>();

    private readonly List<LetterData> temporaryFamilies =
        new List<LetterData>();

    private readonly Dictionary<LetterData, Color> familyColors =
        new Dictionary<LetterData, Color>();

    private Match3TileView[,] tiles;
    private Match3TileView selectedTile;
    private LetterData targetFamily;
    private bool isResolving;
    private int collectedTargetCount;
    private bool gameCompleted;
    private bool isPreparingGame;
    private bool isGameplayActive;

    private void Start()
    {
        if (buildOnStartForTesting)
            SetupGame(testLesson, testLetterIndex);
    }

    public override void SetupGame(
        LessonData lesson,
        int currentLetterIndex)
    {
        if (lesson == null ||
            lesson.letters == null ||
            lesson.letters.Length == 0)
        {
            Debug.LogWarning(
                "Match3Game needs a LessonData with at least one letter."
            );

            return;
        }

        if (alphabetData == null ||
            alphabetData.letters == null ||
            alphabetData.letters.Length == 0)
        {
            Debug.LogWarning(
                "Match3Game needs AlphabetData for filler letters."
            );

            return;
        }

        currentLetterIndex = Mathf.Clamp(
            currentLetterIndex,
            0,
            lesson.letters.Length - 1
        );

        targetFamily = lesson.letters[currentLetterIndex];

        if (targetFamily == null)
        {
            Debug.LogWarning(
                "Match3Game target LetterData is missing."
            );

            return;
        }

        ClearBoard();
        ClearTemporaryFamilies();

        roundFamilies.Clear();
        familyColors.Clear();
        selectedTile = null;
        gameCompleted = false;
        isPreparingGame = false;
        isGameplayActive = false;

        if (openChest != null)
            openChest.transform.localScale = Vector3.one;
        collectedTargetCount = 0;
        UpdateProgressDisplay();

        BuildRoundFamilies(
            lesson,
            currentLetterIndex
        );

        if (roundFamilies.Count < FamilyCount)
        {
            Debug.LogWarning(
                "Match3Game could not create five letter families."
            );

            return;
        }

        AssignFamilyColors();
        UpdateTargetDisplay();
        BuildBoard();

        if (!HasAvailableMove())
            ShuffleBoardUntilPlayable();

        SetBoardInteractable(false);

        // Keep local Match-3 visuals hidden while the shared
        // bonus-game start panel is visible.
        ShowWaitingState();
    }

    private void BuildRoundFamilies(
        LessonData lesson,
        int currentLetterIndex)
    {
        AddFamilyIfUnique(targetFamily);

        List<LetterData> learnedFamilies =
            new List<LetterData>();

        for (int i = 0; i < currentLetterIndex; i++)
        {
            if (lesson.letters[i] != null)
                learnedFamilies.Add(lesson.letters[i]);
        }

        Shuffle(learnedFamilies);
        AddFamiliesUntilFull(learnedFamilies);

        List<LetterData> futureFamilies =
            new List<LetterData>();

        for (
            int i = currentLetterIndex + 1;
            i < lesson.letters.Length;
            i++)
        {
            if (lesson.letters[i] != null)
                futureFamilies.Add(lesson.letters[i]);
        }

        Shuffle(futureFamilies);
        AddFamiliesUntilFull(futureFamilies);

        if (roundFamilies.Count < FamilyCount)
            AddAlphabetFillers();
    }

    private void AddAlphabetFillers()
    {
        List<string> availableLetters =
            new List<string>();

        for (int i = 0; i < alphabetData.letters.Length; i++)
        {
            string letter = alphabetData.letters[i];

            if (!string.IsNullOrWhiteSpace(letter))
                availableLetters.Add(letter.Trim());
        }

        Shuffle(availableLetters);

        for (
            int i = 0;
            i < availableLetters.Count &&
            roundFamilies.Count < FamilyCount;
            i++)
        {
            string displayedLetter =
                availableLetters[i];

            string familyId =
                NormalizeFamilyId(displayedLetter);

            if (ContainsFamilyId(familyId))
                continue;

            LetterData temporaryFamily =
                ScriptableObject.CreateInstance<LetterData>();

            temporaryFamily.name =
                "Match3 Filler " + displayedLetter;

            temporaryFamily.correctLetter = familyId;
            temporaryFamily.targetLetter = displayedLetter;

            if (familyId == "ا")
            {
                temporaryFamily.matchForms =
                    new string[] { "آ", "ا" };
            }
            else
            {
                temporaryFamily.matchForms =
                    new string[] { displayedLetter };
            }

            temporaryFamilies.Add(temporaryFamily);
            roundFamilies.Add(temporaryFamily);
        }
    }

    private void AddFamiliesUntilFull(
        List<LetterData> candidates)
    {
        for (
            int i = 0;
            i < candidates.Count &&
            roundFamilies.Count < FamilyCount;
            i++)
        {
            AddFamilyIfUnique(candidates[i]);
        }
    }

    private void AddFamilyIfUnique(LetterData family)
    {
        if (family == null)
            return;

        string familyId = GetFamilyId(family);

        if (ContainsFamilyId(familyId))
            return;

        roundFamilies.Add(family);
    }

    private bool ContainsFamilyId(string familyId)
    {
        for (int i = 0; i < roundFamilies.Count; i++)
        {
            if (GetFamilyId(roundFamilies[i]) == familyId)
                return true;
        }

        return false;
    }

    private void AssignFamilyColors()
    {
        List<Color> colors = new List<Color>
        {
        new Color32(12, 145, 127, 255),  // Green turquoise
        new Color32(48, 133, 194, 255),  // Blue
        new Color32(237, 79, 65, 255),   // Red coral
        new Color32(137, 67, 143, 255),  // Purple
        new Color32(244, 166, 24, 255)   // Golden yellow
        };
        Shuffle(colors);

        for (int i = 0; i < FamilyCount; i++)
        {
            familyColors.Add(
                roundFamilies[i],
                colors[i]
            );
        }
    }

    private void UpdateTargetDisplay()
    {
        if (targetDisplayText != null)
        {
            targetDisplayText.text =
                GetTargetDisplayValue(targetFamily);
        }
    }

    private void BuildBoard()
    {
        tiles = new Match3TileView[
            RowCount,
            ColumnCount
        ];

        for (int row = 0; row < RowCount; row++)
        {
            for (
                int column = 0;
                column < ColumnCount;
                column++)
            {
                LetterData family =
                    ChooseFamilyForCell(row, column);

                Match3TileView tile = Instantiate(
                    tilePrefab,
                    boardGrid
                );

                tile.Initialize(
                    row,
                    column,
                    OnTileClicked
                );

                tile.SetContent(
                    GetFamilyId(family),
                    GetRandomDisplayedForm(family),
                    familyColors[family],
                    family == targetFamily
                );

                tiles[row, column] = tile;
            }
        }
    }

    private LetterData ChooseFamilyForCell(
        int row,
        int column)
    {
        List<LetterData> candidates =
            new List<LetterData>(roundFamilies);

        for (int i = candidates.Count - 1; i >= 0; i--)
        {
            string familyId =
                GetFamilyId(candidates[i]);

            bool createsHorizontalMatch =
                column >= 2 &&
                tiles[row, column - 1].FamilyId == familyId &&
                tiles[row, column - 2].FamilyId == familyId;

            bool createsVerticalMatch =
                row >= 2 &&
                tiles[row - 1, column].FamilyId == familyId &&
                tiles[row - 2, column].FamilyId == familyId;

            if (createsHorizontalMatch ||
                createsVerticalMatch)
            {
                candidates.RemoveAt(i);
            }
        }

        int randomIndex = Random.Range(
            0,
            candidates.Count
        );

        return candidates[randomIndex];
    }

    /// <summary>
    /// Returns a random valid displayed form for a letter family.
    /// </summary>
    private string GetRandomDisplayedForm(
        LetterData family)
    {
        if (family == null)
            return "؟";

        string displayedForm =
            family.GetRandomGameForm();

        return string.IsNullOrWhiteSpace(displayedForm)
            ? "؟"
            : displayedForm;
    }

    /// <summary>
    /// Returns the shared game label for the target
    /// letter family.
    /// </summary>
    private string GetTargetDisplayValue(
        LetterData family)
    {
        if (family == null)
            return "؟";

        string displayValue =
            family.GetGameDisplayText();

        return string.IsNullOrWhiteSpace(displayValue)
            ? "؟"
            : displayValue;
    }

    private string GetFamilyId(LetterData family)
    {
        string familyId = null;

        if (!string.IsNullOrWhiteSpace(
            family.correctLetter))
        {
            familyId = family.correctLetter;
        }
        else if (!string.IsNullOrWhiteSpace(
            family.targetLetter))
        {
            familyId = family.targetLetter;
        }
        else
        {
            familyId = family.name;
        }

        return NormalizeFamilyId(familyId);
    }

    private string NormalizeFamilyId(string familyId)
    {
        if (string.IsNullOrWhiteSpace(familyId))
            return string.Empty;

        string normalized = familyId
            .Trim()
            .Replace("ي", "ی")
            .Replace("ك", "ک");

        if (normalized == "آ")
            normalized = "ا";

        return normalized;
    }

    private void OnTileClicked(Match3TileView tile)
    {
        if (tile == null ||
            !isGameplayActive ||
            isResolving ||
            gameCompleted)
        {
            return;
        }

        // Clicking the selected tile again cancels the selection.
        if (selectedTile == tile)
        {
            selectedTile.SetSelected(false);
            selectedTile = null;
            return;
        }

        // Select the first tile.
        if (selectedTile == null)
        {
            selectedTile = tile;
            selectedTile.SetSelected(true);
            return;
        }

        Match3TileView firstTile = selectedTile;

        firstTile.SetSelected(false);
        selectedTile = null;

        // If the tiles are not adjacent,
        // keep the second tile as the new selection.
        if (!AreAdjacent(firstTile, tile))
        {
            selectedTile = tile;
            selectedTile.SetSelected(true);
            return;
        }

        SwapTiles(firstTile, tile);

        HashSet<Match3TileView> matches =
            FindAllMatches();

        if (matches.Count == 0)
        {
            SwapTiles(firstTile, tile);
            PlaySound(wrongSound);

            Debug.Log("No match. Swap reverted.");
            return;
        }

        StartCoroutine(ResolveMatches(matches));
    }

    private bool AreAdjacent(
        Match3TileView first,
        Match3TileView second)
    {
        int rowDifference =
            Mathf.Abs(first.Row - second.Row);

        int columnDifference =
            Mathf.Abs(first.Column - second.Column);

        return rowDifference + columnDifference == 1;
    }

    private void SwapTiles(
        Match3TileView first,
        Match3TileView second)
    {
        int firstRow = first.Row;
        int firstColumn = first.Column;

        int secondRow = second.Row;
        int secondColumn = second.Column;

        tiles[firstRow, firstColumn] = second;
        tiles[secondRow, secondColumn] = first;

        first.SetCoordinates(
            secondRow,
            secondColumn
        );

        second.SetCoordinates(
            firstRow,
            firstColumn
        );

        RefreshTileOrder();
    }

    private void RefreshTileOrder()
    {
        for (int row = 0; row < RowCount; row++)
        {
            for (int column = 0;
                 column < ColumnCount;
                 column++)
            {
                int siblingIndex =
                    row * ColumnCount + column;

                tiles[row, column]
                    .transform
                    .SetSiblingIndex(siblingIndex);
            }
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(
            boardGrid
        );
    }

    private HashSet<Match3TileView> FindAllMatches()
    {
        HashSet<Match3TileView> matches =
            new HashSet<Match3TileView>();

        FindHorizontalMatches(matches);
        FindVerticalMatches(matches);

        return matches;
    }

    /// <summary>
    /// Checks whether at least one adjacent swap can create a match.
    /// </summary>
    private bool HasAvailableMove()
    {
        for (int row = 0; row < RowCount; row++)
        {
            for (int column = 0;
                 column < ColumnCount;
                 column++)
            {
                if (column + 1 < ColumnCount &&
                    SwapCreatesMatch(
                        row,
                        column,
                        row,
                        column + 1
                    ))
                {
                    return true;
                }

                if (row + 1 < RowCount &&
                    SwapCreatesMatch(
                        row,
                        column,
                        row + 1,
                        column
                    ))
                {
                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>
    /// Temporarily swaps two board references and checks
    /// whether the swap creates a match.
    /// </summary>
    private bool SwapCreatesMatch(
        int firstRow,
        int firstColumn,
        int secondRow,
        int secondColumn)
    {
        SwapTileReferences(
            firstRow,
            firstColumn,
            secondRow,
            secondColumn
        );

        bool createsMatch =
            FindAllMatches().Count > 0;

        SwapTileReferences(
            firstRow,
            firstColumn,
            secondRow,
            secondColumn
        );

        return createsMatch;
    }

    private void SwapTileReferences(
        int firstRow,
        int firstColumn,
        int secondRow,
        int secondColumn)
    {
        Match3TileView temporaryTile =
            tiles[firstRow, firstColumn];

        tiles[firstRow, firstColumn] =
            tiles[secondRow, secondColumn];

        tiles[secondRow, secondColumn] =
            temporaryTile;
    }

    /// <summary>
    /// Shuffles existing tiles until the board has no automatic
    /// matches and contains at least one valid move.
    /// </summary>
    private bool ShuffleBoardUntilPlayable()
    {
        List<Match3TileView> originalOrder =
            GetTilesInBoardOrder();

        List<Match3TileView> shuffledTiles =
            new List<Match3TileView>(originalOrder);

        for (int attempt = 0;
             attempt < maximumShuffleAttempts;
             attempt++)
        {
            Shuffle(shuffledTiles);
            ApplyTileOrder(shuffledTiles);

            bool hasAutomaticMatch =
                FindAllMatches().Count > 0;

            if (!hasAutomaticMatch &&
                HasAvailableMove())
            {
                RefreshTileOrder();
                return true;
            }
        }

        ApplyTileOrder(originalOrder);
        RefreshTileOrder();

        Debug.LogWarning(
            "Match3Game could not create a playable shuffled board."
        );

        return false;
    }

    private List<Match3TileView> GetTilesInBoardOrder()
    {
        List<Match3TileView> orderedTiles =
            new List<Match3TileView>(
                RowCount * ColumnCount
            );

        for (int row = 0; row < RowCount; row++)
        {
            for (int column = 0;
                 column < ColumnCount;
                 column++)
            {
                orderedTiles.Add(tiles[row, column]);
            }
        }

        return orderedTiles;
    }

    private void ApplyTileOrder(
        List<Match3TileView> orderedTiles)
    {
        int index = 0;

        for (int row = 0; row < RowCount; row++)
        {
            for (int column = 0;
                 column < ColumnCount;
                 column++)
            {
                Match3TileView tile =
                    orderedTiles[index++];

                tiles[row, column] = tile;
                tile.SetCoordinates(row, column);
            }
        }
    }

    /// <summary>
    /// Moves every tile from its current position to a new
    /// playable board position without resetting progress.
    /// </summary>
    private IEnumerator ShuffleStalledBoard()
    {
        SetBoardInteractable(false);

        GridLayoutGroup boardLayout =
            boardGrid.GetComponent<GridLayoutGroup>();

        if (boardLayout == null)
        {
            ShuffleBoardUntilPlayable();
            yield break;
        }

        Dictionary<Match3TileView, Vector3>
            previousPositions =
                new Dictionary<
                    Match3TileView,
                    Vector3
                >();

        Vector3[,] cellPositions =
            new Vector3[RowCount, ColumnCount];

        // Store every tile's current world position and
        // the fixed position of every board cell.
        for (int row = 0; row < RowCount; row++)
        {
            for (int column = 0;
                 column < ColumnCount;
                 column++)
            {
                Match3TileView tile =
                    tiles[row, column];

                Vector3 currentPosition =
                    tile.transform.position;

                previousPositions[tile] =
                    currentPosition;

                cellPositions[row, column] =
                    currentPosition;
            }
        }

        bool shuffleSucceeded =
            ShuffleBoardUntilPlayable();

        if (!shuffleSucceeded)
            yield break;

        Dictionary<Match3TileView, Vector3>
            targetPositions =
                new Dictionary<
                    Match3TileView,
                    Vector3
                >();

        // Map each shuffled tile to its new fixed cell position.
        for (int row = 0; row < RowCount; row++)
        {
            for (int column = 0;
                 column < ColumnCount;
                 column++)
            {
                targetPositions[
                    tiles[row, column]
                ] = cellPositions[row, column];
            }
        }

        boardLayout.enabled = false;

        // Return every tile to its visible starting position.
        foreach (
            KeyValuePair<Match3TileView, Vector3>
                item in previousPositions)
        {
            item.Key.transform.position =
                item.Value;
        }

        PlaySound(shuffleSound);

        float elapsed = 0f;

        while (elapsed < shuffleMoveDuration)
        {
            elapsed += Time.unscaledDeltaTime;

            float progress = Mathf.Clamp01(
                elapsed / shuffleMoveDuration
            );

            float smoothProgress =
                Mathf.SmoothStep(
                    0f,
                    1f,
                    progress
                );

            foreach (
                KeyValuePair<Match3TileView, Vector3>
                    item in targetPositions)
            {
                if (!previousPositions.TryGetValue(
                        item.Key,
                        out Vector3 startPosition))
                {
                    continue;
                }

                item.Key.transform.position =
                    Vector3.Lerp(
                        startPosition,
                        item.Value,
                        smoothProgress
                    );
            }

            yield return null;
        }

        foreach (
            KeyValuePair<Match3TileView, Vector3>
                item in targetPositions)
        {
            item.Key.transform.position =
                item.Value;
        }

        boardLayout.enabled = true;

        LayoutRebuilder.ForceRebuildLayoutImmediate(
            boardGrid
        );
    }


    private void FindHorizontalMatches(
            HashSet<Match3TileView> matches)
    {
        for (int row = 0; row < RowCount; row++)
        {
            int matchStartColumn = 0;

            for (int column = 1;
                 column <= ColumnCount;
                 column++)
            {
                bool continuesMatch =
                    column < ColumnCount &&
                    tiles[row, column].FamilyId ==
                    tiles[row, matchStartColumn].FamilyId;

                if (continuesMatch)
                    continue;

                int matchLength =
                    column - matchStartColumn;

                if (matchLength >= 3)
                {
                    for (int matchedColumn =
                             matchStartColumn;
                         matchedColumn < column;
                         matchedColumn++)
                    {
                        matches.Add(
                            tiles[row, matchedColumn]
                        );
                    }
                }

                matchStartColumn = column;
            }
        }
    }

    private void FindVerticalMatches(
        HashSet<Match3TileView> matches)
    {
        for (int column = 0;
             column < ColumnCount;
             column++)
        {
            int matchStartRow = 0;

            for (int row = 1;
                 row <= RowCount;
                 row++)
            {
                bool continuesMatch =
                    row < RowCount &&
                    tiles[row, column].FamilyId ==
                    tiles[matchStartRow, column].FamilyId;

                if (continuesMatch)
                    continue;

                int matchLength =
                    row - matchStartRow;

                if (matchLength >= 3)
                {
                    for (int matchedRow = matchStartRow;
                         matchedRow < row;
                         matchedRow++)
                    {
                        matches.Add(
                            tiles[matchedRow, column]
                        );
                    }
                }

                matchStartRow = row;
            }
        }
    }

    private IEnumerator ResolveMatches(
    HashSet<Match3TileView> matches)
    {
        isResolving = true;
        SetBoardInteractable(false);

        int safetyCounter = 0;

        while (matches.Count > 0 &&
               safetyCounter < 20)
        {
            AddTargetProgress(matches);
            bool reachedGoal = collectedTargetCount >= requiredTargetCount;

            foreach (Match3TileView matchedTile
                     in matches)
            {
                matchedTile.ShowMatchFeedback();
            }

            PlaySound(correctSound);

            yield return StartCoroutine(FadeMatchedTiles(matches));

            foreach (Match3TileView matchedTile
                     in matches)
            {
                matchedTile.HideMatchFeedback();
            }

            yield return StartCoroutine(AnimateTileFall(matches));
            if (reachedGoal)
            {
                gameCompleted = true;

                yield return StartCoroutine(
                    AnimateChestOpening()
                );

                isResolving = false;
                yield break;
            }

            matches = FindAllMatches();
            safetyCounter++;
        }

        if (safetyCounter >= 20)
        {
            Debug.LogWarning(
                "Match3 cascade stopped by safety limit."
            );
        }

        if (!gameCompleted &&
    !HasAvailableMove())
        {
            yield return StartCoroutine(
                ShuffleStalledBoard()
            );
        }

        SetBoardInteractable(true);

        isResolving = false;
    }

    private IEnumerator FadeMatchedTiles(
    HashSet<Match3TileView> matches)
    {
        float holdDuration = Mathf.Max(
            0f,
            matchFeedbackDuration -
            matchFadeDuration
        );

        if (holdDuration > 0f)
        {
            yield return new WaitForSeconds(
                holdDuration
            );
        }

        float elapsed = 0f;

        while (elapsed < matchFadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;

            float progress = Mathf.Clamp01(
                elapsed / matchFadeDuration
            );

            float alpha = Mathf.Lerp(
                1f,
                0f,
                progress
            );

            foreach (Match3TileView matchedTile
                     in matches)
            {
                matchedTile.SetVisualAlpha(alpha);
            }

            yield return null;
        }

        foreach (Match3TileView matchedTile
                 in matches)
        {
            matchedTile.SetVisualAlpha(0f);
        }
    }

    private IEnumerator AnimateTileFall(
    HashSet<Match3TileView> matches)
    {
        Dictionary<Match3TileView, Vector2>
            previousPositions =
                new Dictionary<
                    Match3TileView,
                    Vector2
                >();

        // Store the current tile positions before changing the board.
        for (int row = 0; row < RowCount; row++)
        {
            for (int column = 0;
                 column < ColumnCount;
                 column++)
            {
                Match3TileView tile =
                    tiles[row, column];

                RectTransform tileRect =
                    tile.transform as RectTransform;

                if (tileRect != null)
                {
                    previousPositions[tile] =
                        tileRect.anchoredPosition;
                }
            }
        }

        // Update the board data and generate replacement content.
        HashSet<Match3TileView> recycledTiles =
            RefillMatchedTiles(matches);

        GridLayoutGroup boardLayout =
            boardGrid.GetComponent<GridLayoutGroup>();

        if (boardLayout == null)
        {
            yield return null;
            yield break;
        }

        Canvas.ForceUpdateCanvases();

        LayoutRebuilder.ForceRebuildLayoutImmediate(
            boardGrid
        );

        Dictionary<Match3TileView, Vector2>
            targetPositions =
                new Dictionary<
                    Match3TileView,
                    Vector2
                >();

        Dictionary<Match3TileView, Vector2>
            animationStartPositions =
                new Dictionary<
                    Match3TileView,
                    Vector2
                >();

        // Capture the final positions calculated by the layout.
        for (int row = 0; row < RowCount; row++)
        {
            for (int column = 0;
                 column < ColumnCount;
                 column++)
            {
                Match3TileView tile =
                    tiles[row, column];

                RectTransform tileRect =
                    tile.transform as RectTransform;

                if (tileRect == null)
                    continue;

                Vector2 targetPosition =
                    tileRect.anchoredPosition;

                targetPositions[tile] =
                    targetPosition;

                Vector2 startPosition;

                if (recycledTiles.Contains(tile))
                {
                    // Spawn the new tile above the board.
                    startPosition =
                        targetPosition +
                        Vector2.up *
                        boardGrid.rect.height;
                }
                else if (previousPositions.TryGetValue(
                             tile,
                             out Vector2 previousPosition))
                {
                    startPosition = previousPosition;
                }
                else
                {
                    startPosition = targetPosition;
                }

                animationStartPositions[tile] =
                    startPosition;
            }
        }

        // Temporarily release tile positions from layout control.
        boardLayout.enabled = false;

        foreach (
            KeyValuePair<Match3TileView, Vector2>
            item in animationStartPositions)
        {
            RectTransform tileRect =
                item.Key.transform as RectTransform;

            if (tileRect != null)
                tileRect.anchoredPosition = item.Value;
        }

        float elapsed = 0f;

        while (elapsed < tileFallDuration)
        {
            elapsed += Time.unscaledDeltaTime;

            float progress = Mathf.Clamp01(
                elapsed / tileFallDuration
            );

            float smoothProgress = Mathf.SmoothStep(
                0f,
                1f,
                progress
            );

            foreach (
                KeyValuePair<Match3TileView, Vector2>
                item in targetPositions)
            {
                RectTransform tileRect =
                    item.Key.transform as RectTransform;

                if (tileRect == null)
                    continue;

                Vector2 startPosition =
                    animationStartPositions[item.Key];

                tileRect.anchoredPosition =
                    Vector2.Lerp(
                        startPosition,
                        item.Value,
                        smoothProgress
                    );
            }

            yield return null;
        }

        foreach (
            KeyValuePair<Match3TileView, Vector2>
            item in targetPositions)
        {
            RectTransform tileRect =
                item.Key.transform as RectTransform;

            if (tileRect != null)
                tileRect.anchoredPosition = item.Value;
        }

        boardLayout.enabled = true;

        LayoutRebuilder.ForceRebuildLayoutImmediate(
            boardGrid
        );
    }

    private HashSet<Match3TileView> RefillMatchedTiles(
    HashSet<Match3TileView> matches)
    {
        HashSet<Match3TileView> allRecycledTiles = new HashSet<Match3TileView>();

        for (int column = 0;
             column < ColumnCount;
             column++)
        {
            List<Match3TileView> recycledTiles =
                new List<Match3TileView>();

            // Keep matched tiles from this column so they can
            // be reused at the top of the same column.
            for (int row = 0; row < RowCount; row++)
            {
                Match3TileView tile =
                    tiles[row, column];

                if (matches.Contains(tile))
                {
                    recycledTiles.Add(tile);
                    allRecycledTiles.Add(tile);
                }
            }

            if (recycledTiles.Count == 0)
                continue;

            int destinationRow = RowCount - 1;

            // Move the remaining tiles down the column.
            for (int row = RowCount - 1;
                 row >= 0;
                 row--)
            {
                Match3TileView tile =
                    tiles[row, column];

                if (matches.Contains(tile))
                    continue;

                tiles[destinationRow, column] = tile;

                tile.SetCoordinates(
                    destinationRow,
                    column
                );

                destinationRow--;
            }

            int recycledIndex = 0;

            // Fill the empty cells at the top with recycled tiles.
            while (destinationRow >= 0)
            {
                Match3TileView tile =
                    recycledTiles[recycledIndex];

                recycledIndex++;

                LetterData family =
                    roundFamilies[
                        Random.Range(
                            0,
                            roundFamilies.Count
                        )
                    ];

                tile.SetCoordinates(
                    destinationRow,
                    column
                );

                tile.SetSelected(false);

                tile.SetContent(
                    GetFamilyId(family),
                    GetRandomDisplayedForm(family),
                    familyColors[family],
                    family == targetFamily
                );

                tiles[destinationRow, column] = tile;

                destinationRow--;
            }
        }

        RefreshTileOrder();
        return allRecycledTiles;
    }

    private void AddTargetProgress(
        HashSet<Match3TileView> matches)
    {
        foreach (Match3TileView matchedTile
                 in matches)
        {
            if (matchedTile.IsTargetFamily)
                collectedTargetCount++;
        }

        collectedTargetCount = Mathf.Min(
            collectedTargetCount,
            requiredTargetCount
        );

        UpdateProgressDisplay();
    }

    private void UpdateProgressDisplay()
    {
        if (progressMarkers != null)
        {
            for (int i = 0;
                 i < progressMarkers.Length;
                 i++)
            {
                GameObject marker =
                    progressMarkers[i];

                if (marker == null)
                    continue;

                // Keep the marker inside the layout at all times.
                marker.SetActive(true);

                CanvasGroup canvasGroup =
                    marker.GetComponent<CanvasGroup>();

                if (canvasGroup != null)
                {
                    canvasGroup.alpha =
                        i < collectedTargetCount
                            ? 1f
                            : 0f;

                    canvasGroup.interactable = false;
                    canvasGroup.blocksRaycasts = false;
                }
            }
        }

        if (closedChest != null)
            closedChest.SetActive(!gameCompleted);

        if (openChest != null)
            openChest.SetActive(gameCompleted);
    }

    private IEnumerator AnimateChestOpening()
    {
        if (openChest == null)
        {
            PlaySound(completionSound);

            if (musicSource != null)
                musicSource.Stop();

            FinishGameplay();
            yield break;
        }

        openChest.SetActive(true);
        openChest.transform.localScale =
            Vector3.one * 0.65f;

        PlaySound(completionSound);

        float elapsed = 0f;

        while (elapsed < chestAnimationDuration)
        {
            elapsed += Time.unscaledDeltaTime;

            float t = Mathf.Clamp01(
                elapsed / chestAnimationDuration
            );

            float scale;

            if (t < 0.75f)
            {
                float growTime = Mathf.SmoothStep(
                    0f,
                    1f,
                    t / 0.75f
                );

                scale = Mathf.Lerp(
                    0.65f,
                    1.12f,
                    growTime
                );
            }
            else
            {
                float settleTime = Mathf.SmoothStep(
                    0f,
                    1f,
                    (t - 0.75f) / 0.25f
                );

                scale = Mathf.Lerp(
                    1.12f,
                    1f,
                    settleTime
                );
            }

            openChest.transform.localScale =
                Vector3.one * scale;

            yield return null;
        }

        openChest.transform.localScale = Vector3.one;

        if (musicSource != null)
            musicSource.Stop();

        // Notify the shared bonus-game flow after the chest animation ends.
        FinishGameplay();
    }

    private void PlaySound(AudioClip clip)
    {
        if (audioSource != null &&
            clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }

    private void ClearBoard()
    {
        if (boardGrid == null)
            return;

        for (
            int i = boardGrid.childCount - 1;
            i >= 0;
            i--)
        {
            Destroy(
                boardGrid.GetChild(i).gameObject
            );
        }
    }

    private void ClearTemporaryFamilies()
    {
        for (int i = 0; i < temporaryFamilies.Count; i++)
        {
            if (temporaryFamilies[i] != null)
                Destroy(temporaryFamilies[i]);
        }

        temporaryFamilies.Clear();
    }

    private void Shuffle<T>(List<T> items)
    {
        for (int i = 0; i < items.Count; i++)
        {
            int randomIndex = Random.Range(
                i,
                items.Count
            );

            T temporary = items[i];
            items[i] = items[randomIndex];
            items[randomIndex] = temporary;
        }
    }

    private void OnDestroy()
    {
        ClearTemporaryFamilies();
    }

    /// <summary>
    /// Displays the Match-3 instructions and starts gameplay after
    /// the instruction voice or fallback delay finishes.
    /// </summary>
    public override void BeginGame()
    {
        if (isPreparingGame ||
            isGameplayActive ||
            gameCompleted)
        {
            return;
        }

        gameObject.SetActive(true);

        isPreparingGame = true;
        isGameplayActive = false;

        SetBoardInteractable(false);

        // Show only the target and instruction panel.
        // The Match-3 board remains hidden until gameplay starts.
        ShowInstructionState();

        StartCoroutine(PlayInstructionThenStart());
    }

    /// <summary>
    /// Stops Match-3 gameplay, animations, and instruction audio.
    /// </summary>
    public override void StopGame()
    {
        bool wasPreparingGame = isPreparingGame;

        isPreparingGame = false;
        isGameplayActive = false;

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

        // Hide all local Match-3 views when this bonus game stops.
        HideGameState();

        SetBoardInteractable(false);

        if (musicSource != null)
            musicSource.Stop();

        isResolving = false;
        selectedTile = null;
    }

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
        isPreparingGame = false;
        isGameplayActive = true;

        // Hide the instruction and reveal the complete
        // Match-3 gameplay view.
        ShowGameplayState();

        if (musicSource != null)
        {
            musicSource.loop = true;
            musicSource.time = 0f;
            musicSource.Play();
        }

        SetBoardInteractable(true);
    }
    private void SetBoardInteractable(bool interactable)
    {
        if (tiles == null)
            return;

        for (int row = 0; row < RowCount; row++)
        {
            for (int column = 0;
                 column < ColumnCount;
                 column++)
            {
                Match3TileView tile = tiles[row, column];

                if (tile != null)
                    tile.SetInteractable(interactable);
            }
        }
    }
}