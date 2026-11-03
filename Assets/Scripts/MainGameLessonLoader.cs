using System;
using UnityEngine;

public class MainGameLessonLoader : MonoBehaviour
{
    [Serializable]
    public struct ChoiceSound
    {
        public string form;
        public AudioClip clip;
    }

    [SerializeField]
    private string[] lessonResourcePaths;

    [SerializeField]
    private ChoiceSound[] choiceSounds;

    public bool IsConfigured =>
        lessonResourcePaths != null &&
        lessonResourcePaths.Length > 0;

    public LessonData LoadLesson(int islandIndex)
    {
        if (!IsConfigured ||
            islandIndex < 0 ||
            islandIndex >= lessonResourcePaths.Length ||
            string.IsNullOrWhiteSpace(
                lessonResourcePaths[islandIndex]))
        {
            Debug.LogError(
                "MainGame: selected lesson resource path is missing."
            );

            return null;
        }

        LessonData result = Resources.Load<LessonData>(
            lessonResourcePaths[islandIndex]
        );

        if (result == null)
        {
            Debug.LogError(
                "MainGame: could not load lesson " +
                lessonResourcePaths[islandIndex]
            );
        }

        return result;
    }

    public AudioClip FindChoiceAudio(string choice)
    {
        if (choiceSounds == null ||
            string.IsNullOrWhiteSpace(choice))
        {
            return null;
        }

        foreach (ChoiceSound entry in choiceSounds)
        {
            if (entry.form == choice && entry.clip != null)
                return entry.clip;
        }

        return null;
    }
}