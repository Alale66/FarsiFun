using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ExampleData
{
    public string word;
    public Sprite image;
    public AudioClip audio;

    [Tooltip("Does this word start with this lesson's target sound or letter?")]
    public bool startsWithTarget;

    [Tooltip("Does this word contain this lesson's target sound?")]
    public bool containsTargetSound;
}

[System.Serializable]
public class LetterFormData
{
    public string baseText;
    public string markText;
}

public enum LessonType
{
    Letter = 0,
    Vowel = 1
}

public enum PictureHuntMode
{
    StartsWithTarget = 0,
    ContainsTargetSound = 1
}

[CreateAssetMenu(
    fileName = "NewLetterData",
    menuName = "FarsiFun/Letter Data"
)]
public class LetterData : ScriptableObject
{
    [Header("Lesson Type")]
    public LessonType lessonType = LessonType.Letter;

    public bool IsVowel => lessonType == LessonType.Vowel;
    public LetterFormData[] forms;

    public string targetLetter;

    public string[] choices = new string[3];

    public string correctLetter;

    [Header("Match 3")]
    [Tooltip("Different displayed forms that count as the same letter family.")]
    public string[] matchForms;

    [Header("Intro")]
    public string letterName;

    [Header("Examples")]
    public ExampleData[] examples;

    [Header("Picture Hunt")]
    public PictureHuntMode pictureHuntMode =
        PictureHuntMode.StartsWithTarget;

    [Tooltip("Extra incorrect examples for Picture Hunt only.")]
    public ExampleData[] pictureHuntDistractors;

    [TextArea(2, 3)]
    public string pictureHuntInstructionText;

    [Tooltip("Full instruction sentence for this lesson.")]
    public AudioClip pictureHuntInstructionAudio;

    [Header("Audio")]
    public AudioClip letterNameAudio;
    public AudioClip letterSoundAudio;

    /// <summary>
    /// Returns the valid letter forms used across
    /// letter-focused games.
    /// </summary>
    public string[] GetGameForms()
    {
        List<string> validForms = new List<string>();

        if (matchForms != null)
        {
            for (int i = 0; i < matchForms.Length; i++)
            {
                AddUniqueForm(validForms, matchForms[i]);
            }
        }

        if (validForms.Count == 0)
        {
            AddUniqueForm(validForms, correctLetter);
            AddUniqueForm(validForms, targetLetter);
        }

        return validForms.ToArray();
    }

    /// <summary>
    /// Returns a random valid form of this letter family.
    /// </summary>
    public string GetRandomGameForm()
    {
        string[] gameForms = GetGameForms();

        if (gameForms.Length == 0)
            return string.Empty;

        int randomIndex = Random.Range(
            0,
            gameForms.Length
        );

        return gameForms[randomIndex];
    }

    /// <summary>
    /// Checks whether a displayed value belongs
    /// to this letter family.
    /// </summary>
    public bool IsGameForm(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;

        string[] gameForms = GetGameForms();

        for (int i = 0; i < gameForms.Length; i++)
        {
            if (gameForms[i] == value)
                return true;
        }

        return false;
    }

    public string GetGameDisplayText()
    {
        string[] gameForms = GetGameForms();

        for (int i = 0; i < gameForms.Length; i++)
        {
            gameForms[i] =
                PersianLetterDisplay.GetText(gameForms[i]);
        }

        return string.Join("  ", gameForms);
    }

    private static void AddUniqueForm(
        List<string> formsList,
        string value)
    {
        if (string.IsNullOrWhiteSpace(value) ||
            formsList.Contains(value))
        {
            return;
        }

        formsList.Add(value);
    }

    // Converts standalone vowel marks into their complete forms.
    // Examples: ـَ → اَ, ـِ → اِ, ـُ → اُ
    public static string GetBonusGameForm(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        string normalized = value.Trim().Normalize(
            System.Text.NormalizationForm.FormKC
        );

        // Preserve complete letters and connected letter forms.
        foreach (char character in normalized)
        {
            if (char.IsLetter(character) &&
                character != '\u0640')
            {
                return value.Trim();
            }
        }

        bool hasFatha = normalized.Contains("\u064E");
        bool hasKasra = normalized.Contains("\u0650");
        bool hasDamma = normalized.Contains("\u064F");

        int markCount =
            (hasFatha ? 1 : 0) +
            (hasKasra ? 1 : 0) +
            (hasDamma ? 1 : 0);

        if (markCount != 1)
            return string.Empty;

        if (hasFatha)
            return "ا\u064E";

        if (hasKasra)
            return "ا\u0650";

        return "ا\u064F";
    }

    public string[] GetBonusGameForms()
    {
        List<string> result = new List<string>();

        foreach (string form in GetGameForms())
        {
            AddUniqueForm(
                result,
                GetBonusGameForm(form)
            );
        }

        return result.ToArray();
    }

    public string GetRandomBonusGameForm()
    {
        string[] availableForms = GetBonusGameForms();

        if (availableForms.Length == 0)
            return string.Empty;

        return availableForms[
            Random.Range(0, availableForms.Length)
        ];
    }
}