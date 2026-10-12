using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ProfileCardView : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private Image avatarImage;
    [SerializeField] private TMP_Text profileNameText;
    [SerializeField] private TMP_Text coinText;

    [Header("Profile Management")]
    [SerializeField] private Button deleteButton;

    private PlayerProfileData profile;
    private Action<PlayerProfileData> clickHandler;
    private Action<PlayerProfileData> deleteHandler;

    private void Awake()
    {
        if (button == null)
            button = GetComponent<Button>();

        if (button != null)
            button.onClick.AddListener(HandleClicked);

        if (deleteButton != null)
        {
            deleteButton.onClick.AddListener(
                HandleDeleteClicked
            );

            deleteButton.gameObject.SetActive(false);
        }
    }

    public void Setup(
        PlayerProfileData profileData,
        Sprite avatarSprite,
        Action<PlayerProfileData> onClicked,
        Action<PlayerProfileData> onDelete = null)
    {
        profile = profileData;
        clickHandler = onClicked;
        deleteHandler = onDelete;

        if (avatarImage != null)
            avatarImage.sprite = avatarSprite;

        if (profileNameText != null)
            profileNameText.text = profile.displayName;

        if (coinText != null)
        {
            coinText.text =
                ToPersianNumber(profile.coins) +
                " سکه";
        }
    }

    public void SetManagementMode(bool enabled)
    {
        if (deleteButton != null)
            deleteButton.gameObject.SetActive(enabled);
    }

    private void HandleClicked()
    {
        if (profile != null)
            clickHandler?.Invoke(profile);
    }

    private void HandleDeleteClicked()
    {
        if (profile != null)
            deleteHandler?.Invoke(profile);
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
        if (button != null)
            button.onClick.RemoveListener(HandleClicked);

        if (deleteButton != null)
        {
            deleteButton.onClick.RemoveListener(
                HandleDeleteClicked
            );
        }
    }
}