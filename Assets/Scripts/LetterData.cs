using UnityEngine;

[System.Serializable]
public class ExampleData
{
    public string word;
    public Sprite image;
    public AudioClip audio;
}

[System.Serializable]
public class LetterFormData
{
    public string baseText;
    public string markText;
}

[CreateAssetMenu(
    fileName = "NewLetterData",
    menuName = "FarsiFun/Letter Data"
)]
public class LetterData : ScriptableObject
{
    public LetterFormData[] forms;
    public string targetLetter;

    public string[] choices = new string[3];

    public string correctLetter;

    [Header("Intro")]
    public string letterName;

    [Header("Examples")]
    public ExampleData[] examples;

    [Header("Audio")]
    public AudioClip letterNameAudio;
    public AudioClip letterSoundAudio;
}