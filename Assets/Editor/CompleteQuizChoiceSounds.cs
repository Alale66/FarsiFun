#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class CompleteQuizChoiceSounds
{
    [MenuItem("Tools/FarsiFun/Complete All Quiz Choice Sounds")]
    private static void Complete()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (EditorApplication.isPlayingOrWillChangePlaymode || scene.name != "MainGame")
        {
            Debug.LogError("Exit Play Mode and open the MainGame scene first.");
            return;
        }

        var loaders = new List<MainGameLessonLoader>();
        foreach (GameObject root in scene.GetRootGameObjects())
            loaders.AddRange(root.GetComponentsInChildren<MainGameLessonLoader>(true));
        if (loaders.Count != 1)
        {
            Debug.LogError("Expected exactly one MainGameLessonLoader. Nothing changed.");
            return;
        }

        var serialized = new SerializedObject(loaders[0]);
        SerializedProperty entries = serialized.FindProperty("choiceSounds");
        var bank = new Dictionary<string, AudioClip>(StringComparer.Ordinal);
        for (int i = 0; i < entries.arraySize; i++)
        {
            SerializedProperty entry = entries.GetArrayElementAtIndex(i);
            string form = entry.FindPropertyRelative("form").stringValue;
            var clip = entry.FindPropertyRelative("clip").objectReferenceValue as AudioClip;
            Add(bank, form, clip);
        }

        string[] guids = AssetDatabase.FindAssets("t:LetterData", new[] { "Assets/Data" });
        var paths = new List<string>();
        foreach (string guid in guids)
            paths.Add(AssetDatabase.GUIDToAssetPath(guid));
        paths.Sort(StringComparer.Ordinal);
        var letters = new List<LetterData>();
        foreach (string path in paths)
        {
            LetterData letter = AssetDatabase.LoadAssetAtPath<LetterData>(path);
            if (letter == null) continue;
            letters.Add(letter);
            if (letter.letterSoundAudio == null)
            {
                if (!letter.UsesWordCompleteGame)
                    Debug.LogWarning("Missing Letter Sound Audio: " + path, letter);
                continue;
            }
            foreach (string form in letter.GetGameForms())
            {
                Add(bank, form, letter.letterSoundAudio);
                if (!string.IsNullOrWhiteSpace(form))
                    Add(bank, form.Trim().Normalize(NormalizationForm.FormKC), letter.letterSoundAudio);
            }
        }

        if (letters.Count == 0 || bank.Count == 0)
        {
            Debug.LogError("No letter audio data found. Nothing changed.");
            return;
        }

        Undo.RecordObject(loaders[0], "Complete quiz choice sounds");
        entries.arraySize = bank.Count;
        int index = 0;
        foreach (var pair in bank)
        {
            SerializedProperty entry = entries.GetArrayElementAtIndex(index++);
            entry.FindPropertyRelative("form").stringValue = pair.Key;
            entry.FindPropertyRelative("clip").objectReferenceValue = pair.Value;
        }
        serialized.ApplyModifiedProperties();
        PrefabUtility.RecordPrefabInstancePropertyModifications(loaders[0]);
        EditorUtility.SetDirty(loaders[0]);
        EditorSceneManager.MarkSceneDirty(scene);

        var reported = new HashSet<string>(StringComparer.Ordinal);
        foreach (LetterData letter in letters)
        {
            if (letter.UsesWordCompleteGame || letter.choices == null) continue;
            foreach (string choice in letter.choices)
            {
                if (string.IsNullOrWhiteSpace(choice) || bank.ContainsKey(choice)) continue;
                if (letter.IsGameForm(choice) && letter.letterSoundAudio != null) continue;
                if (reported.Add(choice))
                    Debug.LogWarning("No quiz audio found for: [" + choice + "]. Assign its Letter Sound Audio, then run this tool again.", letter);
            }
        }
        Debug.Log("Quiz choice sounds completed: " + bank.Count + " entries from " + letters.Count + " letter assets. Save MainGame with Ctrl+S, then test the quiz.", loaders[0]);
    }

    private static void Add(Dictionary<string, AudioClip> bank, string form, AudioClip clip)
    {
        if (!string.IsNullOrWhiteSpace(form) && clip != null && !bank.ContainsKey(form))
            bank.Add(form, clip);
    }
}
#endif
