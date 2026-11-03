using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;

public class PersianLetterHighlighter : MonoBehaviour
{
    [SerializeField] private TMP_Text textComponent;

    private readonly Color32 targetColor =
        new Color32(201, 68, 58, 255);

    private readonly HashSet<char> targetCharacters =
        new HashSet<char>();

    private Coroutine colorCoroutine;
    private string currentWord;
    private bool silentWawOnly;

    public void SetText(string word, params string[] targetForms)
    {
        silentWawOnly = false;
        currentWord = word ?? string.Empty;
        targetCharacters.Clear();

        if (textComponent == null)
            textComponent = GetComponent<TMP_Text>();

        bool hasShadda = false;
        bool hasWaw = false;
        bool hasVowelMark = false;

        if (targetForms != null)
        {
            foreach (string form in targetForms)
            {
                if (string.IsNullOrWhiteSpace(form))
                    continue;

                string normalized =
                    form.Normalize(NormalizationForm.FormKC);

                if (normalized.Contains("\u0651"))
                    hasShadda = true;

                if (normalized.Contains("و"))
                    hasWaw = true;
            }

            if (hasShadda)
            {
                targetCharacters.Add('\u0651');
            }
            else if (hasWaw)
            {
                targetCharacters.Add('و');
            }
            else
            {
                foreach (string form in targetForms)
                {
                    if (string.IsNullOrEmpty(form))
                        continue;

                    string normalized =
                        form.Normalize(NormalizationForm.FormKC);

                    foreach (char character in normalized)
                    {
                        if (IsShortVowel(character))
                        {
                            targetCharacters.Add(character);
                            hasVowelMark = true;
                        }
                    }
                }

                if (!hasVowelMark)
                {
                    foreach (string form in targetForms)
                    {
                        if (string.IsNullOrEmpty(form))
                            continue;

                        string normalized =
                            form.Normalize(NormalizationForm.FormKC);

                        foreach (char character in normalized)
                        {
                            if (char.IsLetter(character) &&
                                character != '\u0640')
                            {
                                targetCharacters.Add(
                                    NormalizeLetter(character)
                                );
                            }
                        }
                    }
                }
            }
        }
        // Include Heh when explicitly listed in the Kasra lesson.
        if (targetCharacters.Contains('\u0650') &&
            targetForms != null)
        {
            foreach (string form in targetForms)
            {
                if (string.IsNullOrWhiteSpace(form))
                    continue;

                string normalized = form
                    .Normalize(NormalizationForm.FormKC)
                    .Replace("\u0640", "")
                    .Trim();

                if (normalized == "ه")
                {
                    targetCharacters.Add('ه');
                    break;
                }
            }
        }

        if (textComponent == null)
            return;

        textComponent.OnPreRenderText -= ApplyHighlight;
        textComponent.OnPreRenderText += ApplyHighlight;

        textComponent.richText = true;

        string wordForDisplay = currentWord;

        if (targetCharacters.Contains('\u0644'))
        {
            wordForDisplay = wordForDisplay.Replace(
                "\u0644\u0627",
                "\uFEDD\u0627"
            );
        }

        string displayWord =
            PersianLetterDisplay.GetWordText(wordForDisplay);

        if (currentWord.Length >= 2 &&
            currentWord[0] == 'ا' &&
            IsShortVowel(currentWord[1]) &&
            targetCharacters.Contains(currentWord[1]))
        {
            displayWord =
                "<color=#C9443A>ا</color>" +
                displayWord.Substring(1);
        }

        textComponent.text = displayWord;
        RefreshHighlight();
    }

    public void SetSilentWawText(string word)
    {
        SetText(word, "و");

        silentWawOnly = true;
        RefreshHighlight();
    }

    private static bool IsShortVowel(char character)
    {
        return character == '\u064E' || // Fatha
               character == '\u0650' || // Kasra
               character == '\u064F';   // Damma
    }

    private static char NormalizeLetter(char character)
    {
        if (character == 'ي' || character == 'ى')
            return 'ی';

        if (character == 'ك')
            return 'ک';

        return character;
    }

    private bool IsTarget(char displayedCharacter)
    {
        // Recognize all displayed forms of Lam.
        if (targetCharacters.Contains('\u0644') &&
            (displayedCharacter == '\u0644' ||
             (displayedCharacter >= '\uFEDD' &&
              displayedCharacter <= '\uFEE0')))
        {
            return true;
        }

        // Waw: normal, isolated, and final forms.
        if (targetCharacters.Contains('\u0648') &&
            (displayedCharacter == '\u0648' ||
             displayedCharacter == '\uFEED' ||
             displayedCharacter == '\uFEEE'))
        {
            return true;
        }

        string normalized = displayedCharacter.ToString()
            .Normalize(NormalizationForm.FormKC);

        if (targetCharacters.Contains('\u0651') &&
            normalized.Contains("\u0651"))
        {
            return true;
        }

        return normalized.Length == 1 &&
               targetCharacters.Contains(
                   NormalizeLetter(normalized[0])
               );
    }

    private void OnEnable()
    {
        if (currentWord != null)
            RefreshHighlight();
    }

    private void OnDisable()
    {
        if (colorCoroutine != null)
        {
            StopCoroutine(colorCoroutine);
            colorCoroutine = null;
        }
    }

    private void RefreshHighlight()
    {
        if (colorCoroutine != null)
        {
            StopCoroutine(colorCoroutine);
            colorCoroutine = null;
        }

        if (textComponent == null || !isActiveAndEnabled)
            return;

        colorCoroutine = StartCoroutine(ColorAfterRTLUpdate());
    }

    private IEnumerator ColorAfterRTLUpdate()
    {
        yield return new WaitForEndOfFrame();

        textComponent.ForceMeshUpdate();
        colorCoroutine = null;
    }

    private void ApplyHighlight(TMP_TextInfo textInfo)
    {
        PersianLetterDisplay.ApplyWordMarkSpacing(textInfo);

        for (int i = 0; i < textInfo.characterCount; i++)
        {
            TMP_CharacterInfo character =
                textInfo.characterInfo[i];

            if (!character.isVisible)
                continue;

            bool shouldHighlight = IsTarget(character.character);

            if (silentWawOnly)
            {
                shouldHighlight =
                    shouldHighlight && HasKhahOnRight(textInfo, i);
            }

            Color32 color = shouldHighlight
                ? targetColor
                : character.color;

            Color32[] colors = textInfo.meshInfo[
                character.materialReferenceIndex
            ].colors32;

            for (int vertex = 0; vertex < 4; vertex++)
                colors[character.vertexIndex + vertex] = color;
        }
    }

    private void OnDestroy()
    {
        if (textComponent != null)
            textComponent.OnPreRenderText -= ApplyHighlight;
    }

    private bool HasKhahOnRight(TMP_TextInfo info, int index)
    {
        TMP_CharacterInfo waw = info.characterInfo[index];

        float wawCenter =
            (waw.bottomLeft.x + waw.topRight.x) * 0.5f;

        float nearestDistance = float.PositiveInfinity;
        char nearestLetter = '\0';

        for (int i = 0; i < info.characterCount; i++)
        {
            if (i == index)
                continue;

            TMP_CharacterInfo candidate = info.characterInfo[i];

            if (!candidate.isVisible ||
                candidate.lineNumber != waw.lineNumber)
            {
                continue;
            }

            string normalized = candidate.character.ToString()
                .Normalize(NormalizationForm.FormKC);

            if (normalized.Length != 1 ||
                !char.IsLetter(normalized[0]) ||
                normalized[0] == '\u0640')
            {
                continue;
            }

            float center =
                (candidate.bottomLeft.x + candidate.topRight.x) * 0.5f;

            float distance = center - wawCenter;

            if (distance > 0f && distance < nearestDistance)
            {
                nearestDistance = distance;
                nearestLetter = normalized[0];
            }
        }

        return nearestLetter == 'خ';
    }
}