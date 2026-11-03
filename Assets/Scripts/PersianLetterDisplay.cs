using System.Text;
using TMPro;
using UnityEngine;

public static class PersianLetterDisplay
{
    private static string GetLetterForm(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        string trimmed = value.Trim();
        // Connected Heh forms as single glyphs.
        if (trimmed == "\u0640\u0647\u0640")
            return "\uFEEC"; // ـهـ

        if (trimmed == "\u0640\u0647")
            return "\uFEEA"; // ـه

        // Connected Ain forms.
        if (trimmed == "\u0640\u0639\u0640")
            return "\uFECC"; // ـعـ

        if (trimmed == "\u0640\u0639")
            return "\uFECA"; // ـع

        // Connected Ghain forms.
        if (trimmed == "\u0640\u063A\u0640")
            return "\uFED0"; // ـغـ

        if (trimmed == "\u0640\u063A")
            return "\uFECE"; // ـغ

        // Connected Persian Yeh forms.
        if (trimmed == "\u0627\u06CC\u0640")
            return "\u0627\uFBFE"; // ایـ

        // Convert standalone initial forms such as نـ.
        // Leave complete words and other forms unchanged.
        if (!trimmed.EndsWith("\u0640"))
            return value;

        string baseLetter = trimmed.TrimEnd('\u0640');

        if (baseLetter.Length != 1)
            return value;

        switch (baseLetter[0])
        {
            case 'ب': return "\uFE91";
            case 'پ': return "\uFB58";
            case 'ت': return "\uFE97";
            case 'ث': return "\uFE9B";
            case 'ج': return "\uFE9F";
            case 'چ': return "\uFB7C";
            case 'ح': return "\uFEA3";
            case 'خ': return "\uFEA7";
            case 'س': return "\uFEB3";
            case 'ش': return "\uFEB7";
            case 'ص': return "\uFEBB";
            case 'ض': return "\uFEBF";
            case 'ط': return "\uFEC3";
            case 'ظ': return "\uFEC7";
            case 'ع': return "\uFECB";
            case 'غ': return "\uFECF";
            case 'ف': return "\uFED3";
            case 'ق': return "\uFED7";
            case 'ک': return "\uFB90";
            case 'ك': return "\uFEDB";
            case 'گ': return "\uFB94";
            case 'ل': return "\uFEDF";
            case 'م': return "\uFEE3";
            case 'ن': return "\uFEE7";
            case 'ه': return "\uFEEB";
            case 'ی': return "\uFBFE";
            case 'ي': return "\uFEF3";

            default:
                return value;
        }
    }

    public static string GetText(string value)
    {
        return GetWordText(GetLetterForm(value));
    }

    public static string GetWordText(string value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        return value
            .Replace(
                "ا\u064E",
                "ا<voffset=0.30em>\u064E</voffset>"
            )
            .Replace(
                "ا\u064F",
                "ا<voffset=0.30em>\u064F</voffset>"
            )
            .Replace(
                "ا\u0650",
                "ا<voffset=-0.12em>\u0650</voffset>"
            )
            .Replace(
                "ر\u0650",
                "ر<voffset=-0.18em>\u0650</voffset>"
            );
    }

    // Shared spacing for Shadda inside words.
    private const float ShaddaOffsetEm = 0.20f;

    public static void ApplyWordMarkSpacing(TMP_TextInfo info)
    {
        for (int i = 0; i < info.characterCount; i++)
        {
            TMP_CharacterInfo character = info.characterInfo[i];

            if (!character.isVisible)
                continue;

            string normalized = character.character
                .ToString()
                .Normalize(NormalizationForm.FormKC);

            float offsetEm;

            if (normalized.Contains("\u0651"))
            {
                offsetEm = ShaddaOffsetEm;
            }
            else if (normalized.Contains("\u064E"))
            {
                offsetEm = 0.08f;
            }
            else if (normalized.Contains("\u064F"))
            {
                offsetEm = 0.25f;
            }
            else
            {
                continue;
            }

            Vector3[] vertices = info.meshInfo[
                character.materialReferenceIndex
            ].vertices;

            float offset = character.pointSize * offsetEm;

            for (int vertex = 0; vertex < 4; vertex++)
            {
                vertices[character.vertexIndex + vertex].y += offset;
            }
        }
    }

    public static void AlignToBaseline(
        TMP_Text label,
        RectTransform line)
    {
        if (label == null || line == null)
            return;

        label.ForceMeshUpdate();

        TMP_TextInfo info = label.textInfo;

        if (info.characterCount == 0 || info.lineCount == 0)
            return;

        RectTransform textRect = label.rectTransform;

        Vector3 lineWorldPosition =
            line.TransformPoint(line.rect.center);

        float targetY = textRect.InverseTransformPoint(
            lineWorldPosition
        ).y;

        float currentBaseline = info.lineInfo[0].baseline;

        textRect.position += textRect.TransformVector(
            new Vector3(0f, targetY - currentBaseline, 0f)
        );
    }

    public static void PositionSeparateMark(
        TMP_Text baseLabel,
        TMP_Text markLabel,
        RectTransform line,
        bool hasBase,
        bool isLowerMark,
        float gapEm,
        float standaloneDistanceEm)
    {
        if (baseLabel == null ||
            markLabel == null ||
            line == null ||
            !markLabel.gameObject.activeInHierarchy)
        {
            return;
        }

        baseLabel.ForceMeshUpdate();
        markLabel.ForceMeshUpdate();

        if (markLabel.textInfo.characterCount == 0)
            return;

        RectTransform baseRect = baseLabel.rectTransform;

        Bounds baseBounds = baseLabel.textBounds;
        Bounds markBounds = markLabel.textBounds;

        float fontSize = baseLabel.fontSize;
        float gap = fontSize * gapEm;

        float centerX = hasBase
            ? baseBounds.center.x
            : baseRect.rect.center.x;

        float targetEdgeY;

        if (hasBase)
        {
            targetEdgeY = isLowerMark
                ? baseBounds.min.y - gap
                : baseBounds.max.y + gap;
        }
        else
        {
            Vector3 lineWorldPosition =
                line.TransformPoint(line.rect.center);

            float lineLocalY = baseRect.InverseTransformPoint(
                lineWorldPosition
            ).y;

            float distance = fontSize * standaloneDistanceEm;

            targetEdgeY = isLowerMark
                ? lineLocalY - distance
                : lineLocalY + distance;
        }

        Vector3 desiredEdgeWorld = baseRect.TransformPoint(
            new Vector3(centerX, targetEdgeY, 0f)
        );

        Vector3 currentEdgeWorld =
            markLabel.rectTransform.TransformPoint(
                new Vector3(
                    markBounds.center.x,
                    isLowerMark
                        ? markBounds.max.y
                        : markBounds.min.y,
                    0f
                )
            );

        markLabel.rectTransform.position +=
            desiredEdgeWorld - currentEdgeWorld;
    }
}