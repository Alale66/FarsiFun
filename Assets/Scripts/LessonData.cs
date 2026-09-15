using UnityEngine;

[CreateAssetMenu(
    fileName = "NewLessonData",
    menuName = "FarsiFun/Lesson Data"
)]
public class LessonData : ScriptableObject
{
    public string lessonName;

    public LetterData[] letters;
}