using System.Collections;
using TMPro;
using UnityEngine;

public class PersianLetterHighlighter : MonoBehaviour
{
    [SerializeField] private TMP_Text textComponent;

    private readonly Color32 targetColor =
        new Color32(201, 68, 58, 255);

    private Coroutine colorCoroutine;
    private string currentWord;

    public void SetText(string word)
    {
        currentWord = word;

        if (textComponent == null)
            return;

        textComponent.text = word;
        RefreshHighlight();
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
        // Wait for RTLTMPro to update the displayed text.
        yield return new WaitForEndOfFrame();

        textComponent.ForceMeshUpdate();

        TMP_TextInfo textInfo = textComponent.textInfo;

        int targetIndex = -1;
        float rightMostX = float.NegativeInfinity;

        for (int i = 0; i < textInfo.characterCount; i++)
        {
            TMP_CharacterInfo characterInfo =
                textInfo.characterInfo[i];

            if (!characterInfo.isVisible)
                continue;

            if (characterInfo.topRight.x > rightMostX)
            {
                rightMostX = characterInfo.topRight.x;
                targetIndex = i;
            }
        }

        if (targetIndex == -1)
        {
            colorCoroutine = null;
            yield break;
        }

        TMP_CharacterInfo target =
            textInfo.characterInfo[targetIndex];

        int materialIndex = target.materialReferenceIndex;
        int vertexIndex = target.vertexIndex;

        Color32[] colors =
            textInfo.meshInfo[materialIndex].colors32;

        for (int i = 0; i < 4; i++)
            colors[vertexIndex + i] = targetColor;

        textInfo.meshInfo[materialIndex].mesh.colors32 = colors;

        textComponent.UpdateGeometry(
            textInfo.meshInfo[materialIndex].mesh,
            materialIndex
        );

        colorCoroutine = null;
    }
}