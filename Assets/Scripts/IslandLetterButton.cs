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