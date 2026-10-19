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

    public void SetText(string word, params string[] targetForms)
    {
        currentWord = word ?? string.Empty;
        targetCharacters.Clear();

        if (textComponent == null)
            textComponent = GetComponent<TMP_Text>();

        bool hasWaw = false;
        bool hasVowelMark = false;

        if (targetForms != null)
        {
            // Detect the long "oo" form before checking short vowels.
            foreach (string form in targetForms)
            {
                if (string.IsNullOrWhiteSpace(form))
                    continue;

                string normalized =
                    form.Normalize(NormalizationForm.FormKC);

                if (normalized.Contains("و"))
                    hasWaw = true;
            }

            if (hasWaw)
            {
                // In word examples, highlight Waw, not Alef or damma.
                targetCharacters.Add('و');
            }
            else
            {
                foreach (string form in targetForms)
                {
                    if (string.IsNullOrEmpty(form))
                        continue;

                    foreach (char character in form)
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

        if (textComponent == null)
            return;

        textComponent.OnPreRenderText -= ApplyHighlight;
        textComponent.OnPreRenderText += ApplyHighlight;

        textComponent.richText = true;

        string displayWord = PersianLetterDisplay.GetWordText(currentWord);

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
        // Waw may be rendered as an Arabic presentation form.
        if (displayedCharacter == '\u0648' ||
            displayedCharacter == '\uFEED' ||
            displayedCharacter == '\uFEEE')
        {
            return targetCharacters.Contains('\u0648');
        }

        string normalized = displayedCharacter.ToString()
            .Normalize(NormalizationForm.FormKC);

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

        ApplyHighlight(textComponent.textInfo);

        textComponent.UpdateVertexData(
            TMP_VertexDataUpdateFlags.Colors32
        );

        colorCoroutine = null;
    }

    private void ApplyHighlight(TMP_TextInfo textInfo)
    {
        for (int i = 0; i < textInfo.characterCount; i++)
        {
            TMP_CharacterInfo character =
                textInfo.characterInfo[i];

            if (!character.isVisible)
                continue;

            Color32 color = IsTarget(character.character)
                ? targetColor
                : character.color;

            Color32[] colors = textInfo.meshInfo[
                character.materialReferenceIndex
            ].colors32;

            for (int vertex = 0; vertex < 4; vertex++)
            {
                colors[character.vertexIndex + vertex] = color;
            }
        }
    }

    private void OnDestroy()
    {
        if (textComponent != null)
            textComponent.OnPreRenderText -= ApplyHighlight;
    }
}