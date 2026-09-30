using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class Match3Game : MonoBehaviour
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
    [SerializeField] private Image targetDisplayImage;
    [Header("Target Progress")]
    [SerializeField] private GameObject[] progressMarkers;
    [SerializeField] private GameObject closedChest;
    [SerializeField] private GameObject openChest;

    [SerializeField, Min(1)]
    private int requiredTargetCount = 8;

    [Header("Match Feedback")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip correctSound;
    [SerializeField] private AudioClip wrongSound;

    [SerializeField, Min(0f)]
    private float matchFeedbackDuration = 0.55f;

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

    private void Start()
    {
        if (buildOnStartForTesting)
            SetupGame(testLesson, testLetterIndex);
    }

    public void SetupGame(
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
        //     new Color32(15, 145, 143, 255),  // Turquoise
        //     new Color32(48, 133, 194, 255),  // Bright blue
        //     new Color32(10, 139, 108, 255),  // Emerald green
        //     new Color32(237, 79, 65, 255),   // Coral red
        //     new Color32(137, 67, 143, 255),  // Playful purple
        //     new Color32(244, 166, 24, 255)   // Golden yellow
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

        if (targetDisplayImage != null)
        {
            targetDisplayImage.color =
                familyColors[targetFamily];
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

    private string GetRandomDisplayedForm(
        LetterData family)
    {
        List<string> forms = GetValidForms(family);

        int randomIndex = Random.Range(
            0,
            forms.Count
        );

        return forms[randomIndex];
    }

    private string GetTargetDisplayValue(
        LetterData family)
    {
        List<string> forms = GetValidForms(family);

        return string.Join("  ", forms);
    }

    private List<string> GetValidForms(
        LetterData family)
    {
        List<string> forms = new List<string>();

        if (family.matchForms != null)
        {
            for (int i = 0; i < family.matchForms.Length; i++)
            {
                string form = family.matchForms[i];

                if (!string.IsNullOrWhiteSpace(form) &&
                    !forms.Contains(form))
                {
                    forms.Add(form);
                }
            }
        }

        if (forms.Count == 0 &&
            !string.IsNullOrWhiteSpace(family.correctLetter))
        {
            forms.Add(family.correctLetter);
        }

        if (forms.Count == 0 &&
            !string.IsNullOrWhiteSpace(family.targetLetter))
        {
            forms.Add(family.targetLetter);
        }

        if (forms.Count == 0)
            forms.Add("؟");

        return forms;
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
        if (tile == null || isResolving || gameCompleted)
        {
            return;
        }

        // کلیک دوباره روی همان خانه، انتخاب را لغو می‌کند.
        if (selectedTile == tile)
        {
            selectedTile.SetSelected(false);
            selectedTile = null;
            return;
        }

        // اولین خانه انتخاب می‌شود.
        if (selectedTile == null)
        {
            selectedTile = tile;
            selectedTile.SetSelected(true);
            return;
        }

        Match3TileView firstTile = selectedTile;

        firstTile.SetSelected(false);
        selectedTile = null;

        // اگر دو خانه همسایه نباشند،
        // خانه دوم به‌عنوان انتخاب جدید باقی می‌ماند.
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

            yield return new WaitForSeconds(
                matchFeedbackDuration
            );

            foreach (Match3TileView matchedTile
                     in matches)
            {
                matchedTile.HideMatchFeedback();
            }

            RefillMatchedTiles(matches);

            // یک فریم صبر می‌کنیم تا Grid به‌روزرسانی شود.
            yield return null;
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

        isResolving = false;
    }

    private void RefillMatchedTiles(
        HashSet<Match3TileView> matches)
    {
        for (int column = 0;
             column < ColumnCount;
             column++)
        {
            List<Match3TileView> recycledTiles =
                new List<Match3TileView>();

            // Tileهای Match‌شده‌ی این ستون نگه داشته شوند
            // تا دوباره در بالای همان ستون استفاده شوند.
            for (int row = 0; row < RowCount; row++)
            {
                Match3TileView tile =
                    tiles[row, column];

                if (matches.Contains(tile))
                    recycledTiles.Add(tile);
            }

            if (recycledTiles.Count == 0)
                continue;

            int destinationRow = RowCount - 1;

            // Tileهای باقی‌مانده به پایین ستون منتقل شوند.
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

            // جای خالی بالای ستون با Tileهای بازیافتی پر شود.
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

                // Marker باید همیشه در Layout باقی بماند.
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
        if (closedChest != null)
            closedChest.SetActive(false);

        if (openChest == null)
        {
            PlaySound(completionSound);
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
}