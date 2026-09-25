using UnityEngine;

[CreateAssetMenu(
    fileName = "PersianAlphabetData",
    menuName = "FarsiFun/Alphabet Data"
)]
public class AlphabetData : ScriptableObject
{
    [Tooltip("All letter symbols that may appear in recognition games.")]
    public string[] letters =
    {
        "ا", "آ",
        "ب", "پ", "ت", "ث",
        "ج", "چ", "ح", "خ",
        "د", "ذ", "ر", "ز", "ژ",
        "س", "ش", "ص", "ض",
        "ط", "ظ", "ع", "غ",
        "ف", "ق", "ک", "گ",
        "ل", "م", "ن", "و",
        "ه", "ی"
    };
}