using TMPro;
using UnityEngine;

public class PlayerWalletDisplay : MonoBehaviour
{
    [SerializeField] private TMP_Text coinCountText;

    private void Start()
    {
        RefreshDisplay();

        if (PlayerProfileManager.Instance != null)
        {
            PlayerProfileManager.Instance.CoinsChanged +=
                HandleCoinsChanged;
        }
    }

    private void HandleCoinsChanged(int newCoinCount)
    {
        SetCoinText(newCoinCount);
    }

    public void RefreshDisplay()
    {
        PlayerProfileManager manager =
            PlayerProfileManager.Instance;

        if (manager == null ||
            manager.SelectedProfile == null)
        {
            SetCoinText(0);
            return;
        }

        SetCoinText(
            manager.SelectedProfile.coins
        );
    }

    private void SetCoinText(int coinCount)
    {
        if (coinCountText != null)
        {
            coinCountText.text =
                ToPersianNumber(coinCount);
        }
    }

    private static string ToPersianNumber(int number)
    {
        return number.ToString()
            .Replace('0', '۰')
            .Replace('1', '۱')
            .Replace('2', '۲')
            .Replace('3', '۳')
            .Replace('4', '۴')
            .Replace('5', '۵')
            .Replace('6', '۶')
            .Replace('7', '۷')
            .Replace('8', '۸')
            .Replace('9', '۹');
    }

    private void OnDestroy()
    {
        if (PlayerProfileManager.Instance != null)
        {
            PlayerProfileManager.Instance.CoinsChanged -=
                HandleCoinsChanged;
        }
    }
}