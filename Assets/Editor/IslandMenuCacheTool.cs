#if UNITY_EDITOR

using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class IslandMenuCacheTool
{
    [MenuItem("Tools/FarsiFun/Cache Island Menu Data")]
    private static void CacheIslandMenuData()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogError(
                "Exit Play Mode before caching island menu data."
            );
            return;
        }

        Scene scene = SceneManager.GetActiveScene();

        if (!scene.IsValid() || scene.name != "IslandView")
        {
            Debug.LogError("Open the IslandView scene first.");
            return;
        }

        var letterButtons =
            new List<IslandLetterButton>();

        var continueButtons =
            new List<IslandContinueButton>();

        foreach (GameObject root in scene.GetRootGameObjects())
        {
            letterButtons.AddRange(
                root.GetComponentsInChildren<IslandLetterButton>(true)
            );

            continueButtons.AddRange(
                root.GetComponentsInChildren<IslandContinueButton>(true)
            );
        }

        if (letterButtons.Count == 0)
        {
            Debug.LogError(
                "No IslandLetterButton components were found. " +
                "Nothing was changed."
            );
            return;
        }

        Undo.IncrementCurrentGroup();

        int undoGroup = Undo.GetCurrentGroup();

        Undo.SetCurrentGroupName("Cache Island Menu Data");

        int changed = 0;

        foreach (IslandLetterButton button in letterButtons)
        {
            var serialized = new SerializedObject(button);

            if (serialized.FindProperty("hasCachedLetterData").boolValue &&
                serialized.FindProperty("lesson").objectReferenceValue == null)
            {
                continue;
            }

            LessonData lesson =
                serialized.FindProperty("lesson")
                    .objectReferenceValue as LessonData;

            int index =
                serialized.FindProperty("letterIndex").intValue;

            bool valid =
                lesson != null &&
                lesson.letters != null &&
                index >= 0 &&
                index < lesson.letters.Length &&
                lesson.letters[index] != null;

            string display = string.Empty;

            if (valid)
            {
                LetterData letter = lesson.letters[index];

                display =
                    !string.IsNullOrWhiteSpace(letter.targetLetter)
                        ? letter.targetLetter
                        : (letter.correctLetter ?? string.Empty);
            }

            Undo.RecordObject(button, "Cache Island Menu Data");

            serialized.FindProperty("cachedHasLetter")
                .boolValue = valid;

            serialized.FindProperty("cachedDisplayText")
                .stringValue = display;

            serialized.FindProperty("hasCachedLetterData")
                .boolValue = true;

            serialized.FindProperty("lesson")
                .objectReferenceValue = null;

            serialized.ApplyModifiedProperties();

            PrefabUtility.RecordPrefabInstancePropertyModifications(
                button
            );

            EditorUtility.SetDirty(button);
            changed++;
        }

        foreach (IslandContinueButton button in continueButtons)
        {
            var serialized = new SerializedObject(button);

            if (serialized.FindProperty("hasCachedLessonData").boolValue &&
                serialized.FindProperty("lesson").objectReferenceValue == null)
            {
                continue;
            }

            LessonData lesson =
                serialized.FindProperty("lesson")
                    .objectReferenceValue as LessonData;

            int count =
                lesson != null && lesson.letters != null
                    ? lesson.letters.Length
                    : 0;

            bool allValid = count > 0;

            if (allValid)
            {
                foreach (LetterData letter in lesson.letters)
                {
                    if (letter == null)
                    {
                        allValid = false;
                        break;
                    }
                }
            }

            Undo.RecordObject(button, "Cache Island Menu Data");

            serialized.FindProperty("cachedLetterCount")
                .intValue = count;

            serialized.FindProperty("cachedAllLettersValid")
                .boolValue = allValid;

            serialized.FindProperty("hasCachedLessonData")
                .boolValue = true;

            serialized.FindProperty("lesson")
                .objectReferenceValue = null;

            serialized.ApplyModifiedProperties();

            PrefabUtility.RecordPrefabInstancePropertyModifications(
                button
            );

            EditorUtility.SetDirty(button);
            changed++;
        }

        Undo.CollapseUndoOperations(undoGroup);

        if (changed > 0)
            EditorSceneManager.MarkSceneDirty(scene);

        Debug.Log(
            "Island menu cache: updated " + changed +
            " components. Save IslandView, then test. " +
            "Ctrl+Z undoes this conversion."
        );
    }
}

#endif