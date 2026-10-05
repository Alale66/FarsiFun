using System;
using UnityEngine;

public abstract class BonusGameBase : MonoBehaviour
{
    [Header("Shared View States")]
    [Tooltip("The target display shown during instruction and gameplay.")]
    [SerializeField] private GameObject targetContent;

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
        SetActive(targetContent, false);
        SetActive(instructionContent, false);
        SetActive(gameplayContent, false);
    }

    /// <summary>
    /// Shows the target and instruction while keeping gameplay hidden.
    /// </summary>
    protected void ShowInstructionState()
    {
        SetActive(targetContent, true);
        SetActive(instructionContent, true);
        SetActive(gameplayContent, false);
    }

    /// <summary>
    /// Shows the target and gameplay after the instruction finishes.
    /// </summary>
    protected void ShowGameplayState()
    {
        SetActive(targetContent, true);
        SetActive(instructionContent, false);
        SetActive(gameplayContent, true);
    }

    /// <summary>
    /// Hides every local visual when the bonus game stops.
    /// </summary>
    protected void HideGameState()
    {
        SetActive(targetContent, false);
        SetActive(instructionContent, false);
        SetActive(gameplayContent, false);
    }

    /// <summary>
    /// Hides the local game view and notifies the shared
    /// bonus-game flow that gameplay has finished.
    /// </summary>
    protected void FinishGameplay()
    {
        HideGameState();
        GameplayCompleted?.Invoke();
    }

    /// <summary>
    /// Changes the visibility of the shared target display.
    /// </summary>
    protected void SetTargetContentVisible(bool visible)
    {
        SetActive(targetContent, visible);
    }

    private void SetActive(
        GameObject target,
        bool active)
    {
        if (target != null)
            target.SetActive(active);
    }
}