using System;
using UnityEngine;

public abstract class BonusGameBase : MonoBehaviour
{
    private const float SharedMusicVolume = 0.22f;

    [Header("Local View States")]
    [Tooltip("The instruction panel shown before gameplay begins.")]
    [SerializeField] private GameObject instructionContent;

    [Tooltip("The main gameplay objects, such as the board or player.")]
    [SerializeField] private GameObject gameplayContent;

    [Header("Shared Music")]
    [Tooltip("The looping background music used during gameplay.")]
    [SerializeField] private AudioSource backgroundMusicSource;

    public event Action GameplayCompleted;

    public abstract void SetupGame(
        LessonData lesson,
        int currentLetterIndex
    );

    public abstract void BeginGame();

    public abstract void StopGame();

    /// <summary>
    /// Hides all local game elements while the shared start panel
    /// is visible and ensures gameplay music is stopped.
    /// </summary>
    protected void ShowWaitingState()
    {
        StopBackgroundMusic();

        SetActive(instructionContent, false);
        SetActive(gameplayContent, false);
    }

    /// <summary>
    /// Shows the local instruction panel while gameplay and
    /// background music remain disabled.
    /// </summary>
    protected void ShowInstructionState()
    {
        StopBackgroundMusic();

        SetActive(instructionContent, true);
        SetActive(gameplayContent, false);
    }

    /// <summary>
    /// Hides the instruction, reveals gameplay, and starts
    /// the shared background-music lifecycle.
    /// </summary>
    protected void ShowGameplayState()
    {
        SetActive(instructionContent, false);
        SetActive(gameplayContent, true);

        PlayBackgroundMusic();
    }

    /// <summary>
    /// Hides the local views and stops gameplay music before
    /// the shared completion panel is displayed.
    /// </summary>
    public void ShowCompletionState()
    {
        StopBackgroundMusic();

        SetActive(instructionContent, false);
        SetActive(gameplayContent, false);
    }

    /// <summary>
    /// Hides every local visual and stops background music
    /// when the bonus game exits.
    /// </summary>
    protected void HideGameState()
    {
        StopBackgroundMusic();

        SetActive(instructionContent, false);
        SetActive(gameplayContent, false);
    }

    /// <summary>
    /// Stops gameplay music before notifying the shared
    /// bonus-game manager.
    /// </summary>
    protected void NotifyGameplayCompleted()
    {
        StopBackgroundMusic();
        GameplayCompleted?.Invoke();
    }

    private void PlayBackgroundMusic()
    {
        if (backgroundMusicSource == null ||
            backgroundMusicSource.clip == null)
        {
            return;
        }

        backgroundMusicSource.volume =
            SharedMusicVolume;

        backgroundMusicSource.loop = true;
        backgroundMusicSource.time = 0f;
        backgroundMusicSource.Play();
    }

    private void StopBackgroundMusic()
    {
        if (backgroundMusicSource != null)
            backgroundMusicSource.Stop();
    }

    private static void SetActive(
        GameObject target,
        bool active)
    {
        if (target != null)
            target.SetActive(active);
    }
}