using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class LetterChoiceGame : MonoBehaviour
{
    [Header("Lesson Data")]
    [SerializeField] private LessonData lesson;

    [Header("Screens")]
    [SerializeField] private GameObject introPanel;
    [SerializeField] private GameObject quizPanel;

    [Header("Intro UI")]
    [SerializeField] private TMP_Text[] exampleTexts;
    [SerializeField] private Image[] exampleImages;
    [SerializeField] private Button[] exampleButtons;
    [SerializeField] private TMP_Text letterTitleText;
    [SerializeField] private LetterFormSlot[] letterFormSlots;

    [Header("Quiz UI")]
    [SerializeField] private TMP_Text feedbackText;
    [SerializeField] private TMP_Text[] choiceTexts;
    [SerializeField] private Button[] choiceButtons;
    [SerializeField] private GameObject rewardBadge;
    [SerializeField] private GameObject retryBadge;
    [SerializeField] private Button nextButton;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;

    private int currentLetterIndex;

    private void Start()
    {
        LoadLetter(0);
        ShowIntro();
    }

    private void LoadLetter(int index)
    {
        if (lesson == null ||
            lesson.letters == null ||
            lesson.letters.Length == 0)
        {
            return;
        }

        currentLetterIndex = index;

        LetterData data = lesson.letters[currentLetterIndex];
        for (int i = 0; i < letterFormSlots.Length; i++)
        {
            if (data.forms != null && i < data.forms.Length)
            {
                LetterFormData form = data.forms[i];

                letterFormSlots[i].SetForm(
                    form.baseText,
                    form.markText
                );
            }
            else
            {
                letterFormSlots[i].Hide();
            }
        }
        List<ExampleData> randomExamples =
            new List<ExampleData>(data.examples);

        Shuffle(randomExamples);

        for (int i = 0; i < exampleTexts.Length && i < randomExamples.Count; i++)
        {
            ExampleData example = randomExamples[i];

            PersianLetterHighlighter highlighter = exampleTexts[i].GetComponent<PersianLetterHighlighter>();

            if (highlighter != null)
            {
                highlighter.SetText(example.word);
            }
            else
            {
                exampleTexts[i].text = example.word;
            }

            exampleImages[i].sprite = example.image;

            exampleButtons[i].onClick.RemoveAllListeners();

            exampleButtons[i].onClick.AddListener(() =>
            {
                PlayExampleAudio(example);
            });
        }
        // Intro
        letterTitleText.text = "یادگیری حرف " + "<color=#C9443A>" + data.letterName + "</color>";

        // Quiz
        feedbackText.text = "";
        rewardBadge.SetActive(false);
        retryBadge.SetActive(false);
        nextButton.interactable = false;
        foreach (Button button in choiceButtons)
        {
            button.interactable = true;
        }

        List<string> shuffledChoices =
            new List<string>(data.choices);

        Shuffle(shuffledChoices);

        for (int i = 0; i < choiceTexts.Length; i++)
        {
            string choice = shuffledChoices[i];

            choiceTexts[i].text = choice;

            choiceButtons[i].onClick.RemoveAllListeners();
            choiceButtons[i].onClick.AddListener(
                () => CheckAnswer(choice)
            );
        }
    }
    private void CheckAnswer(string selectedLetter)
    {
        LetterData data = lesson.letters[currentLetterIndex];

        if (selectedLetter == data.correctLetter)
        {
            feedbackText.text = "";
            retryBadge.SetActive(false);
            rewardBadge.SetActive(true);

            nextButton.interactable = true;
            foreach (Button button in choiceButtons)
            {
                button.interactable = false;
            }
        }
        else
        {
            feedbackText.text = "";
            rewardBadge.SetActive(false);
            retryBadge.SetActive(true);

            nextButton.interactable = false;
        }
    }

    private void Shuffle<T>(List<T> items)
    {
        for (int i = 0; i < items.Count; i++)
        {
            int randomIndex = Random.Range(i, items.Count);

            T temp = items[i];
            items[i] = items[randomIndex];
            items[randomIndex] = temp;
        }
    }

    public void ShowIntro()
    {
        introPanel.SetActive(true);
        quizPanel.SetActive(false);
    }

    public void StartQuiz()
    {
        introPanel.SetActive(false);
        quizPanel.SetActive(true);
    }

    public void PlayLetterName()
    {
        LetterData data = lesson.letters[currentLetterIndex];

        if (data.letterNameAudio != null)
        {
            audioSource.clip = data.letterNameAudio;
            audioSource.Play();
        }
    }

    public void PlayLetterSound()
    {
        LetterData data = lesson.letters[currentLetterIndex];

        if (data.letterSoundAudio != null)
        {
            audioSource.clip = data.letterSoundAudio;
            audioSource.Play();
        }
    }

    public void NextQuestion()
    {
        int nextIndex = currentLetterIndex + 1;

        if (nextIndex >= lesson.letters.Length)
            nextIndex = 0;

        // حرف جدید اول باید معرفی شود
        ShowIntro();
        LoadLetter(nextIndex);
    }
    private void PlayExampleAudio(ExampleData example)
    {
        if (example.audio == null)
            return;

        audioSource.clip = example.audio;
        audioSource.Play();
    }

}