using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

[RequireComponent(typeof(Button), typeof(Image))]
public class IslandLetterButton : MonoBehaviour
{
    [SerializeField, Range(0, 4)]
    private int letterIndex;

    [SerializeField] private LessonData lesson;
    [SerializeField] private Sprite unlockedSprite;
    [SerializeField] private Sprite lockedSprite;
    [SerializeField] private string islandNote;
    [SerializeField] private AudioClip clickSound;
    [SerializeField] private AudioClip unlockSound;

    private bool revealingUnlock;

    [SerializeField, HideInInspector]
    private bool hasCachedLetterData;

    [SerializeField, HideInInspector]
    private bool cachedHasLetter;

    [SerializeField, HideInInspector]
    private string cachedDisplayText;

    private int actualLetterIndex;
    private bool unlocked;

    private void Start()
    {
        Button button = GetComponent<Button>();
        Image image = GetComponent<Image>();

        TMP_Text label =
            GetComponentInChildren<TMP_Text>(true);

        actualLetterIndex =
            SceneNavigator.SelectedIslandIndex * 5 +
            letterIndex;

        bool hasLetter = cachedHasLetter;

        string displayText =
            cachedDisplayText ?? string.Empty;

        // Preserve existing behavior until conversion is run.
        if (!hasCachedLetterData)
        {
            hasLetter =
                lesson != null &&
                lesson.letters != null &&
                letterIndex >= 0 &&
                letterIndex < lesson.letters.Length &&
                lesson.letters[letterIndex] != null;

            if (hasLetter)
            {
                LetterData letter =
                    lesson.letters[letterIndex];

                displayText =
                    !string.IsNullOrWhiteSpace(letter.targetLetter)
                        ? letter.targetLetter
                        : (letter.correctLetter ?? string.Empty);
            }
        }

        if (!hasLetter)
        {
            button.interactable = false;
            gameObject.SetActive(false);
            return;
        }

        if (label != null)
        {
            label.richText = true;

            string text =
                PersianLetterDisplay.GetText(displayText);

            if (!string.IsNullOrWhiteSpace(islandNote))
            {
                text +=
                    "\n<size=35%>" +
                    PersianLetterDisplay.GetWordText(islandNote) +
                    "</size>";
            }

            label.text = text;
        }

        PlayerProfileData profile =
            PlayerProfileManager.Instance != null
                ? PlayerProfileManager.Instance.SelectedProfile
                : null;

        int highestUnlockedIndex =
            profile != null
                ? profile.highestUnlockedLetterIndex
                : 0;

        unlocked =
            actualLetterIndex <= highestUnlockedIndex;

        button.transition = Selectable.Transition.None;
        button.interactable = unlocked;

        image.color = Color.white;

        Sprite sprite =
            unlocked ? unlockedSprite : lockedSprite;

        if (sprite != null)
            image.sprite = sprite;

        if (profile != null && unlocked)
        {
            string unlockKey =
                "PendingLetterUnlock_" + profile.id;

            if (PlayerPrefs.GetInt(unlockKey, -1) == actualLetterIndex)
            {
                StartCoroutine(
                    RevealUnlock(button, image, unlockKey)
                );
            }
        }
    }

    private IEnumerator RevealUnlock(
    Button button,
    Image image,
    string unlockKey)
    {
        revealingUnlock = true;
        button.interactable = false;

        Transform visual = image.transform;

        Vector3 originalScale = visual.localScale;
        Quaternion originalRotation = visual.localRotation;

        if (lockedSprite != null)
            image.sprite = lockedSprite;

        image.color = Color.white;

        yield return new WaitForSecondsRealtime(0.35f);

        // Shake while the letter still looks locked.
        const float shakeDuration = 0.9f;
        float elapsed = 0f;

        while (elapsed < shakeDuration)
        {
            elapsed += Time.unscaledDeltaTime;

            float progress =
                Mathf.Clamp01(elapsed / shakeDuration);

            float angle =
                Mathf.Sin(progress * Mathf.PI * 16f) *
                9f *
                (1f - progress);

            visual.localRotation =
                originalRotation *
                Quaternion.Euler(0f, 0f, angle);

            yield return null;
        }

        visual.localRotation = originalRotation;

        if (unlockedSprite != null)
            image.sprite = unlockedSprite;

        NavigationClickAudio.Play(unlockSound);
        StartCoroutine(ScatterRockPieces(image));

        // Pop and briefly glow gold.
        const float revealDuration = 0.65f;
        Color gold = new Color(1f, 0.78f, 0.3f, 1f);

        elapsed = 0f;

        while (elapsed < revealDuration)
        {
            elapsed += Time.unscaledDeltaTime;

            float progress =
                Mathf.Clamp01(elapsed / revealDuration);

            float pop =
                Mathf.Sin(progress * Mathf.PI);

            visual.localScale =
                originalScale * (1f + pop * 0.2f);

            image.color =
                Color.Lerp(gold, Color.white, progress);

            yield return null;
        }

        visual.localScale = originalScale;
        visual.localRotation = originalRotation;
        image.color = Color.white;

        revealingUnlock = false;
        button.interactable = unlocked;

        PlayerPrefs.DeleteKey(unlockKey);
        PlayerPrefs.Save();
    }

    private IEnumerator ScatterRockPieces(Image source)
    {
        if (lockedSprite == null)
            yield break;

        const int grid = 3;
        const int count = grid * grid;
        const float duration = 0.65f;

        RectTransform sourceRect = source.rectTransform;
        Rect spriteRect = lockedSprite.textureRect;
        Texture2D texture = lockedSprite.texture;

        RockFragmentGraphic[] pieces = new RockFragmentGraphic[count];
        Vector3[] origins = new Vector3[count];
        Vector3[] velocities = new Vector3[count];
        float[] spins = new float[count];

        // Shared irregular edges keep the fragments fitted together.
        int steps = grid * 2;
        Vector2[,] fracturePoints =
            new Vector2[steps + 1, steps + 1];

        for (int x = 0; x <= steps; x++)
        {
            for (int y = 0; y <= steps; y++)
            {
                float px = x / (float)steps;
                float py = y / (float)steps;

                if (x > 0 && x < steps)
                    px += Random.Range(-0.045f, 0.045f);

                if (y > 0 && y < steps)
                    py += Random.Range(-0.045f, 0.045f);

                fracturePoints[x, y] = new Vector2(px, py);
            }
        }

        int[] edgeX = { 0, 0, 0, 1, 2, 2, 2, 1 };
        int[] edgeY = { 0, 1, 2, 2, 2, 1, 0, 0 };

        for (int i = 0; i < count; i++)
        {
            int x = i % grid;
            int y = i / grid;

            GameObject piece = new GameObject(
                "rock_piece",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(RockFragmentGraphic)
            );

            piece.transform.SetParent(sourceRect, false);
            piece.transform.SetAsLastSibling();

            RockFragmentGraphic graphic =
                piece.GetComponent<RockFragmentGraphic>();

            Vector2 center = new Vector2(
                (x + 0.5f) * sourceRect.rect.width / grid,
                (y + 0.5f) * sourceRect.rect.height / grid
            );

            Vector2[] vertices = new Vector2[8];
            Vector2[] uvs = new Vector2[8];

            for (int j = 0; j < vertices.Length; j++)
            {
                Vector2 point = fracturePoints[
                    x * 2 + edgeX[j],
                    y * 2 + edgeY[j]
                ];

                vertices[j] = new Vector2(
                    point.x * sourceRect.rect.width,
                    point.y * sourceRect.rect.height
                ) - center;

                uvs[j] = new Vector2(
                    (spriteRect.x + point.x * spriteRect.width)
                        / texture.width,
                    (spriteRect.y + point.y * spriteRect.height)
                        / texture.height
                );
            }

            RectTransform rect = graphic.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.zero;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = sourceRect.rect.size / grid;
            rect.anchoredPosition = center;

            graphic.Configure(texture, vertices, uvs);

            pieces[i] = graphic;
            origins[i] = rect.anchoredPosition3D;

            velocities[i] = new Vector3(
                (x - 1) * Random.Range(160f, 260f)
                    + Random.Range(-70f, 70f),
                Random.Range(180f, 380f),
                0f
            );

            spins[i] = Random.Range(-400f, 400f);
        }

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Min(elapsed, duration);
            float progress = t / duration;

            for (int i = 0; i < count; i++)
            {
                if (pieces[i] == null)
                    continue;

                RectTransform rect = pieces[i].rectTransform;

                rect.anchoredPosition3D =
                    origins[i] + velocities[i] * t
                    + Vector3.down * (600f * t * t);

                rect.localRotation =
                    Quaternion.Euler(0f, 0f, spins[i] * t);

                pieces[i].color = new Color(
                    1f, 1f, 1f,
                    1f - Mathf.InverseLerp(0.35f, 1f, progress)
                );
            }

            yield return null;
        }

        foreach (RockFragmentGraphic piece in pieces)
        {
            if (piece != null)
                Destroy(piece.gameObject);
        }
    }

    public void OpenLetter()
    {
        if (!unlocked || revealingUnlock)
            return;

        SceneNavigator navigator =
            FindAnyObjectByType<SceneNavigator>();

        if (navigator != null)
        {
            NavigationClickAudio.Play(clickSound);
            navigator.GoToLetter(actualLetterIndex);
        }
        else
        {
            Debug.LogError(
                "IslandView: SceneNavigator is missing."
            );
        }
    }
}

public class RockFragmentGraphic : MaskableGraphic
{
    private Texture fragmentTexture;
    private Vector2[] points;
    private Vector2[] textureUVs;

    public override Texture mainTexture =>
        fragmentTexture != null
            ? fragmentTexture
            : Texture2D.whiteTexture;

    public void Configure(
        Texture texture,
        Vector2[] vertices,
        Vector2[] uvs)
    {
        fragmentTexture = texture;
        points = vertices;
        textureUVs = uvs;

        raycastTarget = false;
        SetAllDirty();
    }

    protected override void OnPopulateMesh(VertexHelper mesh)
    {
        mesh.Clear();

        if (points == null || points.Length < 3)
            return;

        Vector2 center = Vector2.zero;
        Vector2 centerUV = Vector2.zero;

        for (int i = 0; i < points.Length; i++)
        {
            center += points[i];
            centerUV += textureUVs[i];
        }

        center /= points.Length;
        centerUV /= points.Length;

        mesh.AddVert(center, color, centerUV);

        for (int i = 0; i < points.Length; i++)
            mesh.AddVert(points[i], color, textureUVs[i]);

        for (int i = 0; i < points.Length; i++)
            mesh.AddTriangle(
                0,
                i + 1,
                (i + 1) % points.Length + 1
            );
    }
}