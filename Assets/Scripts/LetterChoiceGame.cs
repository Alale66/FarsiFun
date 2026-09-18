
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
    [SerializeField] private GameObject miniGamePanel;

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

    [Header("Mini Game UI")]
    [SerializeField] private MemoryMatchGame memoryMatchGame;
    [SerializeField] private Button miniGameNextButton;
    [SerializeField] private Button miniGameRetryButton;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip clickSound;
    [SerializeField] private AudioClip quizInstructionAudio;
    [SerializeField] private AudioClip miniGameInstructionAudio;

    private int currentLetterIndex;
    private bool quizVisited;
    private bool miniGameVisited;

    private void Start()
    {
        if (memoryMatchGame != null)
            memoryMatchGame.Completed += OnMiniGameCompleted;

        LoadLetter(0);
        ShowIntro();
    }

    private void OnDestroy()
    {
        if (memoryMatchGame != null)
            memoryMatchGame.Completed -= OnMiniGameCompleted;
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

        // A new letter starts with fresh Quiz and Match screens.
        quizVisited = false;
        miniGameVisited = false;

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

        for (int i = 0;
             i < exampleTexts.Length && i < randomExamples.Count;
             i++)
        {
            ExampleData example = randomExamples[i];

            PersianLetterHighlighter highlighter =
                exampleTexts[i].GetComponent<PersianLetterHighlighter>();

            if (highlighter != null)
                highlighter.SetText(example.word);
            else
                exampleTexts[i].text = example.word;

            exampleImages[i].sprite = example.image;

            exampleButtons[i].onClick.RemoveAllListeners();
            exampleButtons[i].onClick.AddListener(
                () => PlayExampleAudio(example)
            );
        }

        letterTitleText.text =
            "یادگیری حرف " +
            "<color=#C9443A>" +
            data.letterName +
            "</color>";

        // Reset Quiz for the new letter.
        ResetQuiz();
    }

    private void ResetQuiz()
    {
        LetterData data = lesson.letters[currentLetterIndex];

        feedbackText.text = "";
        rewardBadge.SetActive(false);
        retryBadge.SetActive(false);
        nextButton.interactable = false;

        List<string> shuffledChoices =
            new List<string>(data.choices);

        Shuffle(shuffledChoices);

        for (int i = 0; i < choiceTexts.Length; i++)
        {
            string choice = shuffledChoices[i];

            choiceTexts[i].text = choice;
            choiceButtons[i].interactable = true;

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
                button.interactable = false;
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
            int randomIndex =
                UnityEngine.Random.Range(i, items.Count);

            T temp = items[i];
            items[i] = items[randomIndex];
            items[randomIndex] = temp;
        }
    }

    public void ShowIntro()
    {
        introPanel.SetActive(true);
        quizPanel.SetActive(false);
        miniGamePanel.SetActive(false);
    }

    public void StartQuiz()
    {
        introPanel.SetActive(false);
        miniGamePanel.SetActive(false);
        quizPanel.SetActive(true);

        // Play the instruction only on the first visit.
        if (!quizVisited)
        {
            quizVisited = true;
            PlayQuizInstruction();
        }
    }

    public void RetryQuiz()
    {
        // Only reset and shuffle the Quiz.
        // Do not replay its instruction.
        ResetQuiz();
    }

    public void BackToIntro()
    {
        ShowIntro();
    }

    public void BackToQuiz()
    {
        miniGamePanel.SetActive(false);
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

    private void PlayExampleAudio(ExampleData example)
    {
        if (example.audio == null)
            return;

        audioSource.clip = example.audio;
        audioSource.Play();
    }

    public void PlayClickSound()
    {
        if (clickSound != null && audioSource != null)
            audioSource.PlayOneShot(clickSound);
    }

    public void PlayQuizInstruction()
    {
        if (quizInstructionAudio == null || audioSource == null)
            return;

        audioSource.clip = quizInstructionAudio;
        audioSource.Play();
    }

    public void NextQuestion()
    {
        StartMiniGame();
    }

    public void StartMiniGame()
    {
        if (memoryMatchGame == null || miniGamePanel == null)
            return;

        introPanel.SetActive(false);
        quizPanel.SetActive(false);
        miniGamePanel.SetActive(true);

        // Returning from Quiz preserves the existing cards.
        if (!miniGameVisited)
        {
            miniGameVisited = true;

            if (miniGameNextButton != null)
                miniGameNextButton.interactable = false;

            if (miniGameRetryButton != null)
                miniGameRetryButton.interactable = false;

            LetterData data = lesson.letters[currentLetterIndex];
            memoryMatchGame.SetupGame(data);

            PlayMiniGameInstruction();
        }
    }

    public void PlayMiniGameInstruction()
    {
        if (miniGameInstructionAudio == null || audioSource == null)
            return;

        audioSource.clip = miniGameInstructionAudio;
        audioSource.Play();
    }

    public void RetryMiniGame()
    {
        if (memoryMatchGame == null)
            return;

        if (miniGameNextButton != null)
            miniGameNextButton.interactable = false;

        if (miniGameRetryButton != null)
            miniGameRetryButton.interactable = false;

        LetterData data = lesson.letters[currentLetterIndex];

        // Only reshuffle and reset the cards.
        memoryMatchGame.SetupGame(data);
    }

    private void OnMiniGameCompleted()
    {
        if (miniGameNextButton != null)
            miniGameNextButton.interactable = true;

        if (miniGameRetryButton != null)
            miniGameRetryButton.interactable = true;
    }

    public void ContinueAfterMiniGame()
    {
        int nextIndex = currentLetterIndex + 1;

        if (nextIndex >= lesson.letters.Length)
            nextIndex = 0;

        miniGamePanel.SetActive(false);

        LoadLetter(nextIndex);
        ShowIntro();
    }
}
