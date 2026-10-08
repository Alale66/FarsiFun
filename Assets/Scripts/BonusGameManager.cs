using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Serialization;

public class BonusGameManager : MonoBehaviour
{
    [Serializable]
    private class BonusGameEntry
    {
        [Tooltip("The component that controls this bonus game.")]
        [SerializeField]
        private BonusGameBase game;

        [Tooltip("The visual root containing this game's UI.")]
        [SerializeField]
        private GameObject contentRoot;

        public BonusGameBase Game => game;
        public GameObject ContentRoot => contentRoot;
    }

    [Header("Bonus Games")]
    [SerializeField]
    private BonusGameEntry[] bonusGames;

    [Header("Shared Panels")]
    [SerializeField]
    [FormerlySerializedAs("recessStartPanel")]
    private GameObject bonusGameStartPanel;

    [SerializeField]
    [FormerlySerializedAs("recessCompletePanel")]
    private GameObject bonusGameCompletePanel;

    [Header("Shared Controls")]
    [SerializeField]
    private Button startButton;

    [SerializeField]
    private Button continueButton;

    [Header("Shared Target")]
    [Tooltip("Displays the current target letter for every bonus game.")]
    [SerializeField]
    private BonusGameTargetDisplay targetDisplay;

    [Header("Shared Audio")]
    [SerializeField]
    private GameAudioManager gameAudioManager;

    [SerializeField]
    private AudioClip startPanelVoice;

    [SerializeField]
    private AudioClip celebrationSound;

    [SerializeField]
    private AudioClip completePanelVoice;

    [Header("Shared Celebration")]
    [SerializeField]
    private PaperCelebrationEffect celebrationEffect;

    [Header("Shared Ambience")]
    [SerializeField]
    private AmbientAudioController ambientAudioController;

    public event Action Completed;

    private BonusGameBase activeGame;
    private GameObject activeContentRoot;
    private LetterData currentLetterData;
    private Coroutine panelVoiceRoutine;
    private bool ownsLockedAudio;

    /// <summary>
    /// Selects and prepares one random bonus game.
    /// </summary>
    public void SetupGame(
    LessonData lesson,
    int currentLetterIndex)
    {
        StopPanelVoice();
        StopCurrentGame();
        SetAllGameContentActive(false);

        currentLetterData = null;

        if (targetDisplay != null)
            targetDisplay.Hide();

        if (lesson == null ||
            lesson.letters == null ||
            currentLetterIndex < 0 ||
            currentLetterIndex >= lesson.letters.Length)
        {
            Debug.LogWarning(
                "BonusGameManager received invalid lesson data."
            );

            return;
        }

        currentLetterData = lesson.letters[currentLetterIndex];

        int selectedIndex = ChooseRandomGameIndex();

        if (selectedIndex < 0)
        {
            Debug.LogWarning(
                "BonusGameManager has no valid bonus games."
            );

            return;
        }

        BonusGameEntry selectedEntry =
            bonusGames[selectedIndex];

        activeGame = selectedEntry.Game;
        activeContentRoot = selectedEntry.ContentRoot;

        activeContentRoot.SetActive(true);

        activeGame.GameplayCompleted +=
            HandleGameplayCompleted;

        activeGame.SetupGame(
            lesson,
            currentLetterIndex
        );

        SetPanelActive(bonusGameCompletePanel, false);

        if (celebrationEffect != null)
            celebrationEffect.StopCelebration();

        if (ambientAudioController != null)
        {
            ambientAudioController.SetMutedForBonusGame(
                true
            );
        }

        SetPanelActive(bonusGameStartPanel, true);
        PlayPanelVoice(startPanelVoice, startButton);
    }

    /// <summary>
    /// Leaves the shared start panel and begins the selected
    /// game's own instruction and gameplay flow.
    /// </summary>
    public void BeginSelectedGame()
    {
        if (activeGame == null)
            return;

        StopPanelVoice();
        SetPanelActive(bonusGameStartPanel, false);

        if (targetDisplay != null)
            targetDisplay.Show(currentLetterData);

        activeGame.BeginGame();
    }

    /// <summary>
    /// Leaves the shared completion panel and returns control
    /// to the main lesson flow.
    /// </summary>
    public void ContinueAfterCompletion()
    {
        StopPanelVoice();

        if (celebrationEffect != null)
            celebrationEffect.StopCelebration();

        SetPanelActive(bonusGameCompletePanel, false);
        StopCurrentGame();

        if (ambientAudioController != null)
        {
            ambientAudioController.SetMutedForBonusGame(
                false
            );
        }

        Completed?.Invoke();
    }

    /// <summary>
    /// Stops the current bonus game and clears shared UI state.
    /// </summary>
    public void StopBonusGame()
    {
        StopPanelVoice();
        StopCurrentGame();
        SetAllGameContentActive(false);
        SetPanelActive(bonusGameStartPanel, false);
        SetPanelActive(bonusGameCompletePanel, false);

        if (celebrationEffect != null)
            celebrationEffect.StopCelebration();

        if (ambientAudioController != null)
        {
            ambientAudioController.SetMutedForBonusGame(
                false
            );
        }
    }

    private int ChooseRandomGameIndex()
    {
        if (bonusGames == null ||
            bonusGames.Length == 0)
        {
            return -1;
        }

        int validGameCount = 0;

        for (int i = 0; i < bonusGames.Length; i++)
        {
            if (IsValidEntry(bonusGames[i]))
                validGameCount++;
        }

        if (validGameCount == 0)
            return -1;

        int selectedValidIndex =
            UnityEngine.Random.Range(0, validGameCount);

        for (int i = 0; i < bonusGames.Length; i++)
        {
            if (!IsValidEntry(bonusGames[i]))
                continue;

            if (selectedValidIndex == 0)
                return i;

            selectedValidIndex--;
        }

        return -1;
    }

    private bool IsValidEntry(BonusGameEntry entry)
    {
        return entry != null &&
               entry.Game != null &&
               entry.ContentRoot != null;
    }

    private void HandleGameplayCompleted()
    {
        if (targetDisplay != null)
            targetDisplay.Hide();

        if (continueButton != null)
            continueButton.interactable = false;

        if (activeGame != null)
            activeGame.ShowCompletionState();

        SetPanelActive(bonusGameCompletePanel, true);

        if (celebrationEffect != null)
            celebrationEffect.PlayCelebration();

        PlayCompletionAudioSequence();
    }

    private void StopCurrentGame()
    {
        if (activeGame != null)
        {
            activeGame.GameplayCompleted -=
                HandleGameplayCompleted;

            activeGame.StopGame();
        }

        SetPanelActive(activeContentRoot, false);

        activeGame = null;
        activeContentRoot = null;
        currentLetterData = null;

        if (targetDisplay != null)
            targetDisplay.Hide();
    }

    private void SetAllGameContentActive(bool active)
    {
        if (bonusGames == null)
            return;

        for (int i = 0; i < bonusGames.Length; i++)
        {
            BonusGameEntry entry = bonusGames[i];

            if (entry != null)
            {
                SetPanelActive(
                    entry.ContentRoot,
                    active
                );
            }
        }
    }

    private void SetPanelActive(
        GameObject panel,
        bool active)
    {
        if (panel != null)
            panel.SetActive(active);
    }

    /// <summary>
    /// Plays a shared panel voice while keeping its action button
    /// disabled until the voice finishes.
    /// </summary>
    private void PlayPanelVoice(
        AudioClip clip,
        Button buttonToUnlock)
    {
        StopPanelVoice();

        if (buttonToUnlock != null)
            buttonToUnlock.interactable = false;

        if (clip == null || gameAudioManager == null)
        {
            if (buttonToUnlock != null)
                buttonToUnlock.interactable = true;

            return;
        }

        panelVoiceRoutine = StartCoroutine(
            PlayPanelVoiceRoutine(
                clip,
                buttonToUnlock
            )
        );
    }

    private IEnumerator PlayPanelVoiceRoutine(
        AudioClip clip,
        Button buttonToUnlock)
    {
        while (gameAudioManager != null &&
               gameAudioManager.IsPlayingLocked)
        {
            yield return null;
        }

        if (gameAudioManager == null)
        {
            if (buttonToUnlock != null)
                buttonToUnlock.interactable = true;

            panelVoiceRoutine = null;
            yield break;
        }

        bool voiceCompleted = false;
        ownsLockedAudio = true;

        gameAudioManager.PlayLocked(
            clip,
            () =>
            {
                voiceCompleted = true;
                ownsLockedAudio = false;
            }
        );

        while (!voiceCompleted)
            yield return null;

        panelVoiceRoutine = null;

        if (buttonToUnlock != null)
            buttonToUnlock.interactable = true;
    }

    /// <summary>
    /// Plays the shared celebration sound followed by the
    /// completion-panel voice.
    /// </summary>
    private void PlayCompletionAudioSequence()
    {
        StopPanelVoice();

        if (continueButton != null)
            continueButton.interactable = false;

        if (gameAudioManager == null)
        {
            if (continueButton != null)
                continueButton.interactable = true;

            return;
        }

        panelVoiceRoutine = StartCoroutine(
            PlayCompletionAudioSequenceRoutine()
        );
    }

    private IEnumerator PlayCompletionAudioSequenceRoutine()
    {
        while (gameAudioManager != null &&
               gameAudioManager.IsPlayingLocked)
        {
            yield return null;
        }

        if (gameAudioManager == null)
        {
            if (continueButton != null)
                continueButton.interactable = true;

            panelVoiceRoutine = null;
            yield break;
        }

        AudioClip[] clips;

        if (celebrationSound != null &&
            completePanelVoice != null)
        {
            clips = new AudioClip[]
            {
            celebrationSound,
            completePanelVoice
            };
        }
        else if (celebrationSound != null)
        {
            clips = new AudioClip[]
            {
            celebrationSound
            };
        }
        else if (completePanelVoice != null)
        {
            clips = new AudioClip[]
            {
            completePanelVoice
            };
        }
        else
        {
            if (continueButton != null)
                continueButton.interactable = true;

            panelVoiceRoutine = null;
            yield break;
        }

        bool sequenceCompleted = false;
        ownsLockedAudio = true;

        gameAudioManager.PlayLockedSequence(
            clips,
            0.15f,
            () =>
            {
                sequenceCompleted = true;
                ownsLockedAudio = false;
            }
        );

        while (!sequenceCompleted)
            yield return null;

        panelVoiceRoutine = null;

        if (continueButton != null)
            continueButton.interactable = true;
    }

    /// <summary>
    /// Stops only the shared panel voice owned by this manager.
    /// </summary>
    private void StopPanelVoice()
    {
        if (panelVoiceRoutine != null)
        {
            StopCoroutine(panelVoiceRoutine);
            panelVoiceRoutine = null;
        }

        if (ownsLockedAudio &&
            gameAudioManager != null &&
            gameAudioManager.IsPlayingLocked)
        {
            gameAudioManager.StopAudio();
        }

        ownsLockedAudio = false;

        if (startButton != null)
            startButton.interactable = true;

        if (continueButton != null)
            continueButton.interactable = true;
    }

    private void OnDisable()
    {
        StopBonusGame();
    }
}