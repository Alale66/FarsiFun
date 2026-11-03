#if UNITY_EDITOR

using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class MainGameLoadCacheTool
{
    [MenuItem("Tools/FarsiFun/Prepare MainGame Lesson Loading")]
    private static void Prepare()
    {
        Scene scene = SceneManager.GetActiveScene();

        if (EditorApplication.isPlayingOrWillChangePlaymode ||
            scene.name != "MainGame")
        {
            Debug.LogError(
                "Exit Play Mode and open MainGame first."
            );

            return;
        }

        var games = new List<LetterChoiceGame>();

        foreach (GameObject root in scene.GetRootGameObjects())
        {
            games.AddRange(
                root.GetComponentsInChildren<LetterChoiceGame>(true)
            );
        }

        if (games.Count != 1)
        {
            Debug.LogError(
                "Expected exactly one LetterChoiceGame. Nothing changed."
            );

            return;
        }

        LetterChoiceGame game = games[0];
        var gameData = new SerializedObject(game);

        SerializedProperty lessons =
            gameData.FindProperty("islandLessons");

        if (gameData.FindProperty("lessonLoader") == null)
        {
            Debug.LogError(
                "Apply the LetterChoiceGame code changes first."
            );

            return;
        }

        if (lessons.arraySize == 0)
        {
            Debug.Log(
                "MainGame was already converted. Nothing changed."
            );

            return;
        }

        var sources = new LessonData[lessons.arraySize];

        for (int i = 0; i < sources.Length; i++)
        {
            sources[i] = lessons.GetArrayElementAtIndex(i)
                .objectReferenceValue as LessonData;

            if (sources[i] == null || sources[i].letters == null)
            {
                Debug.LogError(
                    "Missing lesson at index " + i +
                    ". Nothing changed."
                );

                return;
            }
        }

        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
        {
            AssetDatabase.CreateFolder(
                "Assets",
                "Resources"
            );
        }

        const string folder =
            "Assets/Resources/FarsiFunLessonCache";

        if (!AssetDatabase.IsValidFolder(folder))
        {
            AssetDatabase.CreateFolder(
                "Assets/Resources",
                "FarsiFunLessonCache"
            );
        }

        var paths = new string[sources.Length];

        var sounds =
            new List<MainGameLessonLoader.ChoiceSound>();

        var seenForms = new HashSet<string>();

        Undo.IncrementCurrentGroup();

        int undoGroup = Undo.GetCurrentGroup();

        Undo.SetCurrentGroupName(
            "Prepare MainGame Lesson Loading"
        );

        for (int i = 0; i < sources.Length; i++)
        {
            paths[i] =
                "FarsiFunLessonCache/Lesson_" +
                (i + 1).ToString("00");

            string assetPath =
                "Assets/Resources/" + paths[i] + ".asset";

            LessonData copy =
                AssetDatabase.LoadAssetAtPath<LessonData>(
                    assetPath
                );

            if (copy == null)
            {
                copy = Object.Instantiate(sources[i]);

                copy.name =
                    "Lesson_" + (i + 1).ToString("00");

                AssetDatabase.CreateAsset(copy, assetPath);

                Undo.RegisterCreatedObjectUndo(
                    copy,
                    "Create lesson cache"
                );
            }
            else
            {
                Undo.RecordObject(
                    copy,
                    "Refresh lesson cache"
                );

                EditorUtility.CopySerialized(
                    sources[i],
                    copy
                );

                EditorUtility.SetDirty(copy);
            }

            foreach (LetterData letter in sources[i].letters)
            {
                if (letter == null ||
                    letter.letterSoundAudio == null)
                {
                    continue;
                }

                foreach (string form in letter.GetGameForms())
                {
                    if (!string.IsNullOrWhiteSpace(form) &&
                        seenForms.Add(form))
                    {
                        sounds.Add(
                            new MainGameLessonLoader.ChoiceSound
                            {
                                form = form,
                                clip = letter.letterSoundAudio
                            }
                        );
                    }
                }
            }
        }

        MainGameLessonLoader loader =
            game.GetComponent<MainGameLessonLoader>();

        if (loader == null)
        {
            loader =
                Undo.AddComponent<MainGameLessonLoader>(
                    game.gameObject
                );
        }

        var loaderData = new SerializedObject(loader);

        SerializedProperty resourcePaths =
            loaderData.FindProperty("lessonResourcePaths");

        resourcePaths.arraySize = paths.Length;

        for (int i = 0; i < paths.Length; i++)
        {
            resourcePaths.GetArrayElementAtIndex(i)
                .stringValue = paths[i];
        }

        SerializedProperty entries =
            loaderData.FindProperty("choiceSounds");

        entries.arraySize = sounds.Count;

        for (int i = 0; i < sounds.Count; i++)
        {
            SerializedProperty entry =
                entries.GetArrayElementAtIndex(i);

            entry.FindPropertyRelative("form")
                .stringValue = sounds[i].form;

            entry.FindPropertyRelative("clip")
                .objectReferenceValue = sounds[i].clip;
        }

        loaderData.ApplyModifiedProperties();

        gameData.Update();

        gameData.FindProperty("lessonLoader")
            .objectReferenceValue = loader;

        gameData.FindProperty("islandLessons")
            .arraySize = 0;

        gameData.FindProperty("lesson")
            .objectReferenceValue = null;

        SerializedProperty testLesson =
            gameData.FindProperty("introTestLesson");

        if (testLesson != null)
            testLesson.objectReferenceValue = null;

        gameData.ApplyModifiedProperties();

        PrefabUtility.RecordPrefabInstancePropertyModifications(
            game
        );

        PrefabUtility.RecordPrefabInstancePropertyModifications(
            loader
        );

        EditorSceneManager.MarkSceneDirty(scene);
        AssetDatabase.SaveAssets();

        Undo.CollapseUndoOperations(undoGroup);

        Debug.Log(
            "MainGame converted: " +
            paths.Length + " resource paths and " +
            sounds.Count + " choice sounds. " +
            "Save MainGame and test. " +
            "Ctrl+Z undoes the scene conversion."
        );
    }
}

#endif