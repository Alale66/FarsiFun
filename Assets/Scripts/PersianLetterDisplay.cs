public static class PersianLetterDisplay
{
    private static string GetLetterForm(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        string trimmed = value.Trim();

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
            );
    }
}