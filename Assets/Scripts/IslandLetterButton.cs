using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button), typeof(Image))]
public class IslandLetterButton : MonoBehaviour
{
    [SerializeField, Range(0, 4)]
    private int letterIndex;

    [SerializeField] private LessonData lesson;
    [SerializeField] private Sprite unlockedSprite;
    [SerializeField] private Sprite lockedSprite;

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

        bool hasLetter =
            lesson != null &&
            lesson.letters != null &&
            letterIndex >= 0 &&
            letterIndex < lesson.letters.Length &&
            lesson.letters[letterIndex] != null;

        if (!hasLetter)
        {
            button.interactable = false;
            gameObject.SetActive(false);
            return;
        }

        LetterData letter = lesson.letters[letterIndex];

        if (label != null)
        {
            string displayText =
                !string.IsNullOrWhiteSpace(letter.targetLetter)
                    ? letter.targetLetter
                    : (letter.correctLetter ?? string.Empty);

            label.richText = true;

            label.text =
                PersianLetterDisplay.GetText(displayText);
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
    }

    public void OpenLetter()
    {
        if (!unlocked)
            return;

        SceneNavigator navigator =
            FindAnyObjectByType<SceneNavigator>();

        if (navigator != null)
        {
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