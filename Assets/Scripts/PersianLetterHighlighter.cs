using System.Collections;
using TMPro;
using UnityEngine;

public class PersianLetterHighlighter : MonoBehaviour
{
    [SerializeField] private TMP_Text textComponent;

    private readonly Color32 targetColor =
        new Color32(201, 68, 58, 255); // #C9443A

    private Coroutine colorCoroutine;

    public void SetText(string word)
    {
        textComponent.text = word;

        if (colorCoroutine != null)
            StopCoroutine(colorCoroutine);

        colorCoroutine = StartCoroutine(ColorAfterRTLUpdate());
    }

    private IEnumerator ColorAfterRTLUpdate()
    {
        // صبر می‌کنیم RTLTMPro کار شکل‌دهی متن را تمام کند
        yield return new WaitForEndOfFrame();

        textComponent.ForceMeshUpdate();

        TMP_TextInfo textInfo = textComponent.textInfo;

        int targetIndex = -1;
        float rightMostX = float.NegativeInfinity;

        // اولین حرف فارسی از نظر بصری راست‌ترین glyph است
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
            yield break;

        TMP_CharacterInfo target =
            textInfo.characterInfo[targetIndex];

        int materialIndex =
            target.materialReferenceIndex;

        int vertexIndex =
            target.vertexIndex;

        Color32[] colors =
            textInfo.meshInfo[materialIndex].colors32;

        colors[vertexIndex + 0] = targetColor;
        colors[vertexIndex + 1] = targetColor;
        colors[vertexIndex + 2] = targetColor;
        colors[vertexIndex + 3] = targetColor;

        textInfo.meshInfo[materialIndex].mesh.colors32 = colors;

        textComponent.UpdateGeometry(
            textInfo.meshInfo[materialIndex].mesh,
            materialIndex
        );
    }
}