using System;
using UnityEngine;

public static class PlayerRewardSystem
{
    private const int CorrectAnswerCoins = 1;
    private const int WrongAnswerCoins = 1;
    private const int LearningCardCoins = 1;

    public static event Action<int, RectTransform>
        CoinFeedbackRequested;

    public static void ReportCorrectAnswer(
        RectTransform target = null)
    {
        ChangeCoins(CorrectAnswerCoins, target);
    }

    public static void ReportWrongAnswer(
        RectTransform target = null)
    {
        ChangeCoins(-WrongAnswerCoins, target);
    }

    public static void ReportLearningCardTap(
        RectTransform target = null)
    {
        ChangeCoins(LearningCardCoins, target);
    }

    private static void ChangeCoins(
        int amount,
        RectTransform target)
    {
        PlayerProfileManager manager =
            PlayerProfileManager.Instance;

        if (manager == null ||
            manager.SelectedProfile == null)
        {
            return;
        }

        manager.ChangeCoins(amount);

        if (target != null)
            CoinFeedbackRequested?.Invoke(amount, target);
    }
}