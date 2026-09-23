
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LetterChoiceGame : MonoBehaviour
{
    [Header("Lesson Data")]
    [SerializeField] private LessonData lesson;

    [Header("Screens")]
    [SerializeField] private GameObject introPanel;
    [SerializeField] private GameObject quizPanel;
    [SerializeField] private GameObject letterHuntPanel;
    [SerializeField] private GameObject miniGamePanel;
    [SerializeField] private GameObject pictureHuntPanel;

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
    [SerializeField] private QuizChoiceFeedback[] choiceFeedbacks;
    [SerializeField] private GameObject rewardBadge;
    [SerializeField] private GameObject retryBadge;
    [SerializeField] private Button nextButton;
    [SerializeField] private Button quizRetryButton;

    [Header("Letter Hunt UI")]
    [SerializeField] private LetterHuntGame letterHuntGame;
    [SerializeField] private Button letterHuntNextButton;
    [SerializeField] private Button letterHuntRetryButton;
    [SerializeField] private GameObject letterHuntRewardBadge;

    [Header("Picture Hunt UI")]
    [SerializeField] private PictureHuntGame pictureHuntGame;
    [SerializeField] private Button pictureHuntNextButton;
    [SerializeField] private Button pictureHuntRetryButton;
    [SerializeField] private GameObject pictureHuntRewardBadge;

    [Header("Mini Game UI")]
    [SerializeField] private MemoryMatchGame memoryMatchGame;
    [SerializeField] private Button miniGameNextButton;
    [SerializeField] private Button miniGameRetryButton;
    [SerializeField] private GameObject miniGameRewardBadge;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip clickSound;
    [SerializeField] private AudioClip quizInstructionAudio;
    [SerializeField] private AudioClip miniGameInstructionAudio;
    [SerializeField] private AudioClip letterHuntInstructionAudio;
    [SerializeField] private AudioClip PictureHuntInstructionAudio;
    [SerializeField] private AudioClip quizCorrectSound;
    [SerializeField] private AudioClip quizWrongSound;
    [SerializeField] private AudioClip completionSound;

    private int currentLetterIndex;

    private bool quizVisited;
    private bool letterHuntVisited;
    private bool pictureHuntVisited;
    private bool miniGameVisited;
    private bool quizChecking;

    private Coroutine wrongAnswerRoutine;
    private Coroutine successSoundRoutine;

    private void Start()
    {
        if (memoryMatchGame != null)
            memoryMatchGame.Completed += OnMiniGameCompleted;

        if (letterHuntGame != null)
            letterHuntGame.Completed += OnLetterHuntCompleted;

        if (pictureHuntGame != null)
            pictureHuntGame.Completed += OnPictureHuntCompleted;

        LoadLetter(0);
        ShowIntro();
    }

    private void OnDestroy()
    {
        if (memoryMatchGame != null)
            memoryMatchGame.Completed -= OnMiniGameCompleted;

        if (letterHuntGame != null)
            letterHuntGame.Completed -= OnLetterHuntCompleted;

        if (pictureHuntGame != null)
            pictureHuntGame.Completed -= OnPictureHuntCompleted;
    }

    // Screen visibility is managed in one place.
    private void ShowScreen(GameObject targetPanel)
    {
        StopSuccessSounds();

        if (introPanel != null)
            introPanel.SetActive(introPanel == targetPanel);

        if (quizPanel != null)
            quizPanel.SetActive(quizPanel == targetPanel);

        if (letterHuntPanel != null)
            letterHuntPanel.SetActive(letterHuntPanel == targetPanel);

        if (pictureHuntPanel != null)
            pictureHuntPanel.SetActive(pictureHuntPanel == targetPanel);

        if (miniGamePanel != null)
            miniGamePanel.SetActive(miniGamePanel == targetPanel);
    }

    private void SetCompletionUI(
        Button next,
        Button retry,
        GameObject badge,
        bool completed)
    {
        if (next != null)
            next.interactable = completed;

        if (retry != null)
            retry.interactable = completed;

        if (badge != null)
            badge.SetActive(completed);
    }

    private void LoadLetter(int index)
    {
        if (lesson == null ||
            lesson.letters == null ||
            lesson.letters.Length == 0)
        {
            return;
        }

        StopSuccessSounds();

        currentLetterIndex = index;

        quizVisited = false;
        letterHuntVisited = false;
        pictureHuntVisited = false;
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

        ResetQuiz();
    }

    private void ResetQuiz()
    {
        StopSuccessSounds();

        if (wrongAnswerRoutine != null)
        {
            StopCoroutine(wrongAnswerRoutine);
            wrongAnswerRoutine = null;
        }

        quizChecking = false;

        LetterData data = lesson.letters[currentLetterIndex];

        feedbackText.text = "";
        rewardBadge.SetActive(false);
        retryBadge.SetActive(false);

        nextButton.interactable = false;

        if (quizRetryButton != null)
            quizRetryButton.interactable = false;

        if (choiceFeedbacks != null)
        {
            foreach (QuizChoiceFeedback feedback in choiceFeedbacks)
            {
                if (feedback != null)
                    feedback.ResetVisual();
            }
        }

        List<string> shuffledChoices =
            new List<string>(data.choices);

        // For Alef, show either ا or آ as the correct choice.
        if (data.correctLetter == "ا")
        {
            string selectedAlefForm =
                Random.Range(0, 2) == 0 ? "ا" : "آ";

            int correctChoiceIndex =
                shuffledChoices.IndexOf(data.correctLetter);

            if (correctChoiceIndex >= 0)
            {
                shuffledChoices[correctChoiceIndex] =
                    selectedAlefForm;
            }
            else
            {
                Debug.LogWarning(
                    "Quiz choices do not contain the correct letter."
                );
            }
        }

        Shuffle(shuffledChoices);

        for (int i = 0; i < choiceTexts.Length; i++)
        {
            string choice = shuffledChoices[i];
            int choiceIndex = i;

            choiceTexts[i].text = choice;
            choiceButtons[i].interactable = true;

            choiceButtons[i].onClick.RemoveAllListeners();
            choiceButtons[i].onClick.AddListener(
                () => CheckAnswer(choice, choiceIndex)
            );
        }
    }

    private void CheckAnswer(string selectedLetter, int choiceIndex)
    {
        if (quizChecking)
            return;

        LetterData data = lesson.letters[currentLetterIndex];

        QuizChoiceFeedback selectedFeedback = null;

        if (choiceFeedbacks != null &&
            choiceIndex >= 0 &&
            choiceIndex < choiceFeedbacks.Length)
        {
            selectedFeedback = choiceFeedbacks[choiceIndex];
        }

        bool isCorrect =
            selectedLetter == data.correctLetter ||
            (data.correctLetter == "ا" && selectedLetter == "آ");

        if (isCorrect)
        {
            quizChecking = true;

            feedbackText.text = "";
            retryBadge.SetActive(false);
            rewardBadge.SetActive(false);

            if (selectedFeedback != null)
                selectedFeedback.ShowCorrect();

            StopSuccessSounds();
            successSoundRoutine =
                StartCoroutine(PlayQuizSuccessSequence());

            foreach (Button button in choiceButtons)
                button.interactable = false;
        }
        else
        {
            feedbackText.text = "";
            rewardBadge.SetActive(false);
            retryBadge.SetActive(true);
            nextButton.interactable = false;

            wrongAnswerRoutine =
                StartCoroutine(ShowWrongAnswer(selectedFeedback));
        }
    }

    private IEnumerator PlayQuizSuccessSequence()
    {
        if (audioSource != null && quizCorrectSound != null)
        {
            audioSource.PlayOneShot(quizCorrectSound);
            yield return new WaitForSeconds(quizCorrectSound.length);
        }

        // Show the reward when the completion sound starts.
        rewardBadge.SetActive(true);
        nextButton.interactable = true;

        if (quizRetryButton != null)
            quizRetryButton.interactable = true;

        if (audioSource != null && completionSound != null)
        {
            audioSource.PlayOneShot(completionSound);
            yield return new WaitForSeconds(completionSound.length);
        }

        successSoundRoutine = null;
    }

    private IEnumerator ShowWrongAnswer(
        QuizChoiceFeedback selectedFeedback)
    {
        quizChecking = true;

        if (selectedFeedback != null)
            selectedFeedback.ShowWrong();

        if (audioSource != null && quizWrongSound != null)
            audioSource.PlayOneShot(quizWrongSound);

        yield return new WaitForSeconds(0.6f);

        if (selectedFeedback != null)
            selectedFeedback.ResetVisual();

        quizChecking = false;
        wrongAnswerRoutine = null;
    }

    private void StopSuccessSounds()
    {
        if (successSoundRoutine == null)
            return;

        StopCoroutine(successSoundRoutine);
        successSoundRoutine = null;

        if (audioSource != null)
            audioSource.Stop();
    }

    private void PlayCompletionOnly()
    {
        StopSuccessSounds();

        if (audioSource != null && completionSound != null)
            audioSource.PlayOneShot(completionSound);
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

    // Intro
    public void ShowIntro()
    {
        ShowScreen(introPanel);
    }

    public void BackToIntro()
    {
        ShowIntro();
    }

    public void PlayLetterName()
    {
        LetterData data = lesson.letters[currentLetterIndex];

        if (audioSource != null && data.letterNameAudio != null)
        {
            audioSource.clip = data.letterNameAudio;
            audioSource.Play();
        }
    }

    public void PlayLetterSound()
    {
        LetterData data = lesson.letters[currentLetterIndex];

        if (audioSource != null && data.letterSoundAudio != null)
        {
            audioSource.clip = data.letterSoundAudio;
            audioSource.Play();
        }
    }

    private void PlayExampleAudio(ExampleData example)
    {
        if (example.audio == null || audioSource == null)
            return;

        audioSource.clip = example.audio;
        audioSource.Play();
    }

    public void PlayClickSound()
    {
        if (clickSound != null && audioSource != null)
            audioSource.PlayOneShot(clickSound);
    }

    // Quiz
    public void StartQuiz()
    {
        ShowScreen(quizPanel);

        if (!quizVisited)
        {
            quizVisited = true;
            PlayQuizInstruction();
        }
    }

    public void BackToQuiz()
    {
        StartQuiz();
    }

    public void RetryQuiz()
    {
        ResetQuiz();
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
        StartLetterHunt();
    }

    // Letter Hunt
    public void StartLetterHunt()
    {
        if (letterHuntPanel == null || letterHuntGame == null)
            return;

        ShowScreen(letterHuntPanel);

        if (!letterHuntVisited)
        {
            letterHuntVisited = true;
            ResetLetterHunt();
            PlayLetterHuntInstruction();
        }
    }

    private void ResetLetterHunt()
    {
        if (letterHuntGame == null)
            return;

        StopSuccessSounds();

        SetCompletionUI(
            letterHuntNextButton,
            letterHuntRetryButton,
            letterHuntRewardBadge,
            false
        );

        LetterData data = lesson.letters[currentLetterIndex];
        letterHuntGame.SetupGame(data);
    }

    private void OnLetterHuntCompleted()
    {
        SetCompletionUI(
            letterHuntNextButton,
            letterHuntRetryButton,
            letterHuntRewardBadge,
            true
        );

        PlayCompletionOnly();
    }

    public void RetryLetterHunt()
    {
        ResetLetterHunt();
    }

    public void PlayLetterHuntInstruction()
    {
        if (letterHuntInstructionAudio == null || audioSource == null)
            return;

        audioSource.clip = letterHuntInstructionAudio;
        audioSource.Play();
    }

    public void ContinueAfterLetterHunt()
    {
        StartPictureHunt();
    }

    public void BackToLetterHunt()
    {
        StartLetterHunt();
    }

    // Picture Hunt
    public void StartPictureHunt()
    {
        if (pictureHuntPanel == null || pictureHuntGame == null)
            return;

        ShowScreen(pictureHuntPanel);

        if (!pictureHuntVisited)
        {
            pictureHuntVisited = true;
            ResetPictureHunt();
            PlayPictureHuntInstruction();
        }
    }

    private void ResetPictureHunt()
    {
        if (pictureHuntGame == null)
            return;

        StopSuccessSounds();

        SetCompletionUI(
            pictureHuntNextButton,
            pictureHuntRetryButton,
            pictureHuntRewardBadge,
            false
        );

        pictureHuntGame.SetupGame();
    }

    private void OnPictureHuntCompleted()
    {
        SetCompletionUI(
            pictureHuntNextButton,
            pictureHuntRetryButton,
            pictureHuntRewardBadge,
            true
        );

        PlayCompletionOnly();
    }

    public void RetryPictureHunt()
    {
        ResetPictureHunt();
    }

    public void ContinueAfterPictureHunt()
    {
        StartMiniGame();
    }

    public void BackToPictureHunt()
    {
        StartPictureHunt();
    }

    // Memory Match
    public void StartMiniGame()
    {
        if (memoryMatchGame == null || miniGamePanel == null)
            return;

        ShowScreen(miniGamePanel);

        if (!miniGameVisited)
        {
            miniGameVisited = true;
            ResetMiniGame();
            PlayMiniGameInstruction();
        }
    }

    private void ResetMiniGame()
    {
        if (memoryMatchGame == null)
            return;

        StopSuccessSounds();

        SetCompletionUI(
            miniGameNextButton,
            miniGameRetryButton,
            miniGameRewardBadge,
            false
        );

        LetterData data = lesson.letters[currentLetterIndex];
        memoryMatchGame.SetupGame(data);
    }

    private void OnMiniGameCompleted()
    {
        SetCompletionUI(
            miniGameNextButton,
            miniGameRetryButton,
            miniGameRewardBadge,
            true
        );

        PlayCompletionOnly();
    }

    public void RetryMiniGame()
    {
        ResetMiniGame();
    }

    public void PlayMiniGameInstruction()
    {
        if (miniGameInstructionAudio == null || audioSource == null)
            return;

        audioSource.clip = miniGameInstructionAudio;
        audioSource.Play();
    }

    public void ContinueAfterMiniGame()
    {
        int nextIndex = currentLetterIndex + 1;

        if (nextIndex >= lesson.letters.Length)
            nextIndex = 0;

        LoadLetter(nextIndex);
        ShowIntro();
    }

    public void PlayPictureHuntInstruction()
    {
        if (PictureHuntInstructionAudio == null || audioSource == null)
            return;

        audioSource.clip = PictureHuntInstructionAudio;
        audioSource.Play();
    }
}
