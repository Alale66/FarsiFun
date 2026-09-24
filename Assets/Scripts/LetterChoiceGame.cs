
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
    [SerializeField] private SharedGamePanelUI sharedGamePanelUI;
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
    [SerializeField] private GameObject retryBadge;

    [Header("Letter Hunt UI")]
    [SerializeField] private LetterHuntGame letterHuntGame;

    [Header("Picture Hunt UI")]
    [SerializeField] private PictureHuntGame pictureHuntGame;

    [Header("Mini Game UI")]
    [SerializeField] private MemoryMatchGame memoryMatchGame;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip clickSound;
    [SerializeField] private AudioClip quizInstructionAudio;
    [SerializeField] private AudioClip miniGameInstructionAudio;
    [SerializeField] private AudioClip letterHuntInstructionAudio;
    [SerializeField] private AudioClip pictureHuntInstructionAudio;
    [SerializeField] private AudioClip quizCorrectSound;
    [SerializeField] private AudioClip quizWrongSound;
    [SerializeField] private AudioClip completionSound;
    [SerializeField] private UIInputLock inputLock;
    [SerializeField] private GameAudioManager gameAudioManager;

    private LessonStage currentStage;
    private int currentLetterIndex;
    private bool quizCompleted;
    private bool letterHuntCompleted;
    private bool pictureHuntCompleted;
    private bool miniGameCompleted;
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

    private void ShowStage(LessonStage stage, GameObject targetPanel)
    {
        currentStage = stage;

        if (sharedGamePanelUI != null)
            sharedGamePanelUI.ShowStage(stage);

        ShowScreen(targetPanel);
    }
    private void SetCompletionUI(bool completed)
    {
        if (sharedGamePanelUI != null)
            sharedGamePanelUI.SetCompletion(completed);
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

        quizCompleted = false;
        letterHuntCompleted = false;
        pictureHuntCompleted = false;
        miniGameCompleted = false;

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

        quizCompleted = false;

        if (wrongAnswerRoutine != null)
        {
            StopCoroutine(wrongAnswerRoutine);
            wrongAnswerRoutine = null;
        }

        quizChecking = false;

        LetterData data = lesson.letters[currentLetterIndex];

        feedbackText.text = "";

        retryBadge.SetActive(false);
        SetCompletionUI(false);

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
            SetCompletionUI(false);

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
            retryBadge.SetActive(true);
            SetCompletionUI(false);

            wrongAnswerRoutine =
                StartCoroutine(ShowWrongAnswer(selectedFeedback));
        }
    }

    private IEnumerator PlayQuizSuccessSequence()
    {
        // Correct-answer sound stays locked.
        if (gameAudioManager != null && quizCorrectSound != null)
        {
            gameAudioManager.PlayLocked(quizCorrectSound);

            yield return new WaitWhile(
                () => gameAudioManager.IsPlayingLocked
            );
        }
        else if (audioSource != null && quizCorrectSound != null)
        {
            audioSource.PlayOneShot(quizCorrectSound);
            yield return new WaitForSeconds(quizCorrectSound.length);
        }

        quizCompleted = true;
        SetCompletionUI(true);

        // Completion sound does not lock the UI.
        if (audioSource != null && completionSound != null)
            audioSource.PlayOneShot(completionSound);

        successSoundRoutine = null;
    }

    private IEnumerator ShowWrongAnswer(
     QuizChoiceFeedback selectedFeedback)
    {
        quizChecking = true;

        if (selectedFeedback != null)
            selectedFeedback.ShowWrong();

        if (gameAudioManager != null && quizWrongSound != null)
        {
            gameAudioManager.PlayLocked(quizWrongSound);

            yield return new WaitWhile(
                () => gameAudioManager.IsPlayingLocked
            );
        }
        else
        {
            yield return new WaitForSeconds(0.6f);
        }

        if (selectedFeedback != null)
            selectedFeedback.ResetVisual();

        quizChecking = false;
        wrongAnswerRoutine = null;
    }
    private void StopSuccessSounds()
    {
        if (successSoundRoutine != null)
        {
            StopCoroutine(successSoundRoutine);
            successSoundRoutine = null;
        }

        if (gameAudioManager != null &&
            gameAudioManager.IsPlayingLocked)
        {
            gameAudioManager.StopAudio();
        }

        // Fallback for audio still using the old AudioSource.
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
        ShowStage(LessonStage.Intro, introPanel);

        if (sharedGamePanelUI != null)
        {
            sharedGamePanelUI.SetNavigationVisibility(
                false,
                false,
                false
            );

            sharedGamePanelUI.SetCompletion(false);
        }
    }

    public void BackToIntro()
    {
        ShowIntro();
    }

    public void PlayLetterName()
    {
        LetterData data = lesson.letters[currentLetterIndex];

        if (gameAudioManager != null)
            gameAudioManager.PlayLocked(data.letterNameAudio);
    }

    public void PlayLetterSound()
    {
        LetterData data = lesson.letters[currentLetterIndex];

        if (gameAudioManager != null)
            gameAudioManager.PlayLocked(data.letterSoundAudio);
    }

    private void PlayExampleAudio(ExampleData example)
    {
        if (gameAudioManager != null)
            gameAudioManager.PlayLocked(example.audio);
    }

    public void PlayClickSound()
    {
        if (clickSound != null && audioSource != null)
            audioSource.PlayOneShot(clickSound);
    }

    // Quiz
    public void StartQuiz()
    {
        ShowStage(LessonStage.Quiz, quizPanel);

        if (sharedGamePanelUI != null)
        {
            sharedGamePanelUI.SetNavigationVisibility(
                true,
                true,
                true
            );

            sharedGamePanelUI.SetCompletion(quizCompleted);
        }

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
        if (gameAudioManager != null)
            gameAudioManager.PlayLocked(quizInstructionAudio);
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

        ShowStage(LessonStage.LetterHunt, letterHuntPanel);

        if (sharedGamePanelUI != null)
        {
            sharedGamePanelUI.SetNavigationVisibility(
                true,
                true,
                true
            );

            sharedGamePanelUI.SetCompletion(letterHuntCompleted);
        }

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

        letterHuntCompleted = false;

        SetCompletionUI(false);

        LetterData data = lesson.letters[currentLetterIndex];
        letterHuntGame.SetupGame(data);
    }

    private void OnLetterHuntCompleted()
    {
        letterHuntCompleted = true;

        SetCompletionUI(true);

        PlayCompletionOnly();
    }

    public void RetryLetterHunt()
    {
        ResetLetterHunt();
    }

    public void PlayLetterHuntInstruction()
    {
        if (gameAudioManager != null)
            gameAudioManager.PlayLocked(letterHuntInstructionAudio);
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

        ShowStage(LessonStage.PictureHunt, pictureHuntPanel);

        if (sharedGamePanelUI != null)
        {
            sharedGamePanelUI.SetNavigationVisibility(
                true,
                true,
                true
            );

            sharedGamePanelUI.SetCompletion(pictureHuntCompleted);
        }

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

        pictureHuntCompleted = false;

        SetCompletionUI(false);

        LetterData data = lesson.letters[currentLetterIndex];
        pictureHuntGame.SetupGame(data, lesson.letters);
    }

    private void OnPictureHuntCompleted()
    {
        pictureHuntCompleted = true;

        SetCompletionUI(true);

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

        ShowStage(LessonStage.MemoryMatch, miniGamePanel);

        if (sharedGamePanelUI != null)
        {
            sharedGamePanelUI.SetNavigationVisibility(
                true,
                true,
                true
            );

            sharedGamePanelUI.SetCompletion(miniGameCompleted);
        }


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

        miniGameCompleted = false;

        SetCompletionUI(false);

        LetterData data = lesson.letters[currentLetterIndex];
        memoryMatchGame.SetupGame(data);
    }

    private void OnMiniGameCompleted()
    {
        miniGameCompleted = true;

        SetCompletionUI(true);

        PlayCompletionOnly();
    }

    public void RetryMiniGame()
    {
        ResetMiniGame();
    }

    public void PlayMiniGameInstruction()
    {
        if (gameAudioManager != null)
            gameAudioManager.PlayLocked(miniGameInstructionAudio);
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
        StartCoroutine(PlayPictureHuntInstructionRoutine());
    }

    private IEnumerator PlayPictureHuntInstructionRoutine()
    {
        if (pictureHuntInstructionAudio == null || audioSource == null)
            yield break;

        if (inputLock != null)
            inputLock.Lock();

        audioSource.PlayOneShot(pictureHuntInstructionAudio);

        yield return new WaitForSeconds(
            pictureHuntInstructionAudio.length
        );

        if (inputLock != null)
            inputLock.Unlock();
    }
    public void SharedBack()
    {
        switch (currentStage)
        {
            case LessonStage.Quiz:
                BackToIntro();
                break;

            case LessonStage.LetterHunt:
                BackToQuiz();
                break;

            case LessonStage.PictureHunt:
                BackToLetterHunt();
                break;

            case LessonStage.MemoryMatch:
                BackToPictureHunt();
                break;
        }
    }

    public void SharedRetry()
    {
        switch (currentStage)
        {
            case LessonStage.Quiz:
                RetryQuiz();
                break;

            case LessonStage.LetterHunt:
                RetryLetterHunt();
                break;

            case LessonStage.PictureHunt:
                RetryPictureHunt();
                break;

            case LessonStage.MemoryMatch:
                RetryMiniGame();
                break;
        }
    }

    public void SharedNext()
    {
        switch (currentStage)
        {
            case LessonStage.Quiz:
                NextQuestion();
                break;

            case LessonStage.LetterHunt:
                ContinueAfterLetterHunt();
                break;

            case LessonStage.PictureHunt:
                ContinueAfterPictureHunt();
                break;

            case LessonStage.MemoryMatch:
                ContinueAfterMiniGame();
                break;
        }
    }
}
