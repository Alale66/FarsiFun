using TMPro;
using UnityEngine;

[RequireComponent(typeof(TMP_Text))]
public class TMPGlyphCenterer : MonoBehaviour
{
    private TMP_Text label;

    private void Awake()
    {
        label = GetComponent<TMP_Text>();
    }

    private void OnEnable()
    {
        if (label == null)
            label = GetComponent<TMP_Text>();

        label.OnPreRenderText += CenterGlyph;
        label.SetVerticesDirty();
    }

    private void OnDisable()
    {
        if (label != null)
            label.OnPreRenderText -= CenterGlyph;
    }

    private void CenterGlyph(TMP_TextInfo textInfo)
    {
        Vector2 minimum = new Vector2(
            float.PositiveInfinity,
            float.PositiveInfinity
        );

        Vector2 maximum = new Vector2(
            float.NegativeInfinity,
            float.NegativeInfinity
        );

        bool hasVisibleCharacter = false;

        for (int i = 0; i < textInfo.characterCount; i++)
        {
            TMP_CharacterInfo character =
                textInfo.characterInfo[i];

            if (!character.isVisible)
                continue;

            hasVisibleCharacter = true;

            Vector3[] vertices = textInfo.meshInfo[
                character.materialReferenceIndex
            ].vertices;

            for (int corner = 0; corner < 4; corner++)
            {
                Vector2 position =
                    vertices[character.vertexIndex + corner];

                minimum = Vector2.Min(minimum, position);
                maximum = Vector2.Max(maximum, position);
            }
        }

        if (!hasVisibleCharacter)
            return;

        Vector2 glyphCenter = (minimum + maximum) * 0.5f;

        Vector2 offset =
            label.rectTransform.rect.center - glyphCenter;

        Vector3 movement = new Vector3(
            offset.x,
            offset.y,
            0f
        );

        for (int i = 0; i < textInfo.characterCount; i++)
        {
            TMP_CharacterInfo character =
                textInfo.characterInfo[i];

            if (!character.isVisible)
                continue;

            Vector3[] vertices = textInfo.meshInfo[
                character.materialReferenceIndex
            ].vertices;

            for (int corner = 0; corner < 4; corner++)
            {
                vertices[character.vertexIndex + corner] +=
                    movement;
            }
        }
    }
}