using System;
using UnityEngine;

public abstract class BonusGameBase : MonoBehaviour
{
    [Header("Local View States")]
    [Tooltip("The instruction panel shown before gameplay begins.")]
    [SerializeField] private GameObject instructionContent;

    [Tooltip("The main gameplay objects, such as the board or player.")]
    [SerializeField] private GameObject gameplayContent;

    public event Action GameplayCompleted;

    public abstract void SetupGame(
        LessonData lesson,
        int currentLetterIndex
    );

    public abstract void BeginGame();

    public abstract void StopGame();

    /// <summary>
    /// Hides all local game elements while the shared start panel
    /// is visible.
    /// </summary>
    protected void ShowWaitingState()
    {
        SetActive(instructionContent, false);
        SetActive(gameplayContent, false);
    }

    /// <summary>
    /// Shows the local instruction panel while gameplay remains hidden.
    /// </summary>
    protected void ShowInstructionState()
    {
        SetActive(instructionContent, true);
        SetActive(gameplayContent, false);
    }

    /// <summary>
    /// Hides the instruction and reveals the local gameplay content.
    /// </summary>
    protected void ShowGameplayState()
    {
        SetActive(instructionContent, false);
        SetActive(gameplayContent, true);
    }

    /// <summary>
    /// Hides the local instruction and gameplay views before
    /// the shared completion panel is displayed.
    /// </summary>
    public void ShowCompletionState()
    {
        SetActive(instructionContent, false);
        SetActive(gameplayContent, false);
    }

    /// <summary>
    /// Hides every local visual when the bonus game stops.
    /// </summary>
    protected void HideGameState()
    {
        SetActive(instructionContent, false);
        SetActive(gameplayContent, false);
    }

    protected void NotifyGameplayCompleted()
    {
        GameplayCompleted?.Invoke();
    }

    private static void SetActive(
        GameObject target,
        bool active)
    {
        if (target != null)
            target.SetActive(active);
    }
}