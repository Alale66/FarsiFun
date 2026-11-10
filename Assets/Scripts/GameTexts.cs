using System;
using System.Collections.Generic;
using UnityEngine;

public static class GameTexts
{
    [Serializable]
    private class Entry
    {
        public string key;
        public string value;
    }

    [Serializable]
    private class TextFile
    {
        public Entry[] entries;
    }

    private static Dictionary<string, string> texts;

    [RuntimeInitializeOnLoadMethod(
        RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetCache()
    {
        texts = null;
    }

    public static string Get(string key, string fallback)
    {
        if (texts == null)
            Load();

        return texts.TryGetValue(key, out string value)
            ? value
            : fallback;
    }

    private static void Load()
    {
        texts = new Dictionary<string, string>();

        TextAsset file = Resources.Load<TextAsset>(
            "Texts/game_texts_fa"
        );

        if (file == null)
        {
            Debug.LogError("Game texts JSON was not found.");
            return;
        }

        try
        {
            TextFile data =
                JsonUtility.FromJson<TextFile>(file.text);

            if (data?.entries == null)
                throw new Exception("Missing entries array.");

            foreach (Entry entry in data.entries)
            {
                if (entry == null ||
                    string.IsNullOrWhiteSpace(entry.key) ||
                    entry.value == null)
                {
                    continue;
                }

                texts[entry.key] = entry.value;
            }
        }
        catch (Exception exception)
        {
            texts.Clear();
            Debug.LogError(
                "Cannot load game texts: " + exception.Message
            );
        }
    }
}