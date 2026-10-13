using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class ProfileSelectionController : MonoBehaviour
{
    [Header("Create Profile Modal")]
    [SerializeField] private GameObject createProfileModal;
    [SerializeField] private ProfileSelectionTransition profileTransition;
    [SerializeField] private TMP_InputField nameInputField;

    [Header("Buttons")]
    [SerializeField] private Button addProfileButton;
    [SerializeField] private Button closeButton;
    [SerializeField] private Button confirmButton;
    [SerializeField] private Button manageProfilesButton;
    [SerializeField] private Button continueButton;

    [Header("Avatar Selection")]
    [SerializeField] private AvatarOptionView[] avatarOptions;
    [SerializeField] private Image selectedAvatarImage;
    [SerializeField] private Sprite[] avatarSprites;

    [Header("Profile Cards")]
    [SerializeField] private Transform profileCardsContainer;
    [SerializeField] private ProfileCardView profileCardPrefab;

    [SerializeField]
    private GameObject addProfileCardRoot;

    [Header("Delete Profile Modal")]
    [SerializeField] private GameObject deleteProfileModal;
    [SerializeField] private Image deleteAvatarPreview;
    [SerializeField] private TMP_Text deleteProfileNameText;
    [SerializeField] private TMP_Text deleteProfileCoinText;
    [SerializeField] private Button cancelDeleteButton;
    [SerializeField] private Button confirmDeleteButton;

    private int selectedAvatarIndex;
    private bool isManagingProfiles;
    private PlayerProfileData pendingDeleteProfile;

    private void Awake()
    {
        if (addProfileButton != null)
            addProfileButton.onClick.AddListener(OpenCreateModal);

        if (closeButton != null)
            closeButton.onClick.AddListener(CloseCreateModal);

        if (confirmButton != null)
            confirmButton.onClick.AddListener(CreateProfile);

        foreach (AvatarOptionView option in avatarOptions)
        {
            if (option != null)
                option.Selected += HandleAvatarSelected;
        }

        if (manageProfilesButton != null)
        {
            manageProfilesButton.onClick.AddListener(
                ToggleManagementMode
            );
        }

        if (cancelDeleteButton != null)
        {
            cancelDeleteButton.onClick.AddListener(
                CloseDeleteModal
            );
        }

        if (confirmDeleteButton != null)
        {
            confirmDeleteButton.onClick.AddListener(
                ConfirmDeleteProfile
            );
        }

        if (continueButton != null)
        {
            continueButton.onClick.AddListener(
                ContinueToGame
            );
        }
    }

    private void Start()
    {
        createProfileModal.SetActive(false);

        if (deleteProfileModal != null)
            deleteProfileModal.SetActive(false);

        if (avatarOptions.Length > 0 &&
            avatarOptions[0] != null)
        {
            SelectAvatar(avatarOptions[0]);
        }

        RefreshProfileCards();
    }

    private void OpenCreateModal()
    {
        profileTransition.OpenCreateModal();

        nameInputField.text = string.Empty;
        nameInputField.ActivateInputField();

        if (avatarOptions.Length > 0 &&
            avatarOptions[0] != null)
        {
            SelectAvatar(avatarOptions[0]);
        }
    }

    private void CloseCreateModal()
    {
        profileTransition.CloseCreateModal();
    }

    private void HandleAvatarSelected(
        AvatarOptionView selectedOption)
    {
        SelectAvatar(selectedOption);
    }

    private void SelectAvatar(
        AvatarOptionView selectedOption)
    {
        selectedAvatarIndex =
            selectedOption.AvatarIndex;

        foreach (AvatarOptionView option in avatarOptions)
        {
            if (option != null)
            {
                option.SetSelected(
                    option == selectedOption
                );
            }
        }

        if (selectedAvatarImage != null)
        {
            selectedAvatarImage.sprite =
                selectedOption.AvatarSprite;
        }
    }

    private void CreateProfile()
    {
        string playerName =
            nameInputField.text.Trim();

        if (string.IsNullOrEmpty(playerName))
        {
            nameInputField.ActivateInputField();
            return;
        }

        if (PlayerProfileManager.Instance == null)
        {
            Debug.LogError(
                "PlayerProfileManager is missing."
            );
            return;
        }

        PlayerProfileManager.Instance.CreateProfile(
            playerName,
            selectedAvatarIndex
        );

        CloseCreateModal();
        RefreshProfileCards();
    }

    private void RefreshProfileCards()
    {
        if (PlayerProfileManager.Instance == null ||
            profileCardsContainer == null ||
            profileCardPrefab == null)
        {
            return;
        }

        ProfileCardView[] existingCards =
            profileCardsContainer.GetComponentsInChildren<
                ProfileCardView
            >();

        foreach (ProfileCardView card in existingCards)
            Destroy(card.gameObject);

        foreach (
            PlayerProfileData profile in
            PlayerProfileManager.Instance.Profiles)
        {
            if (profile.avatarIndex < 0 ||
                profile.avatarIndex >= avatarSprites.Length)
            {
                continue;
            }

            ProfileCardView card = Instantiate(
                profileCardPrefab,
                profileCardsContainer
            );

            card.Setup(
                profile,
                avatarSprites[profile.avatarIndex],
                HandleProfileSelected,
                HandleDeleteRequested
            );

            PlayerProfileData selectedProfile =
                PlayerProfileManager.Instance.SelectedProfile;

            card.SetSelected(
                selectedProfile != null &&
                selectedProfile.id == profile.id
            );

            card.SetManagementMode(isManagingProfiles);

            if (addProfileCardRoot != null)
            {
                card.transform.SetSiblingIndex(
                    addProfileCardRoot.transform.GetSiblingIndex()
                );
            }
        }

        bool canCreateProfile =
            PlayerProfileManager.Instance.Profiles.Count <
            PlayerProfileManager.MaximumProfiles;

        if (addProfileCardRoot != null)
        {
            addProfileCardRoot.SetActive(canCreateProfile);
        }
    }

    private void HandleProfileSelected(
    PlayerProfileData profile)
    {
        PlayerProfileManager.Instance.SelectProfile(
            profile.id
        );

        RefreshProfileCards();

        Debug.Log(
            "Selected profile: " +
            profile.displayName
        );
    }

    private void ToggleManagementMode()
    {
        isManagingProfiles = !isManagingProfiles;
        RefreshProfileCards();
    }

    private void HandleDeleteRequested(
    PlayerProfileData profile)
    {
        if (profile == null ||
            deleteProfileModal == null)
        {
            return;
        }

        pendingDeleteProfile = profile;

        if (deleteProfileNameText != null)
        {
            deleteProfileNameText.text =
                profile.displayName;
        }

        if (deleteProfileCoinText != null)
        {
            deleteProfileCoinText.text =
                ToPersianNumber(profile.coins) +
                " سکه";
        }

        if (deleteAvatarPreview != null &&
            profile.avatarIndex >= 0 &&
            profile.avatarIndex < avatarSprites.Length)
        {
            deleteAvatarPreview.sprite =
                avatarSprites[profile.avatarIndex];
        }

        profileTransition.OpenDeleteModal();
    }

    private void CloseDeleteModal()
    {
        if (deleteProfileModal != null)
            profileTransition.CloseDeleteModal();

        pendingDeleteProfile = null;
    }

    private void ConfirmDeleteProfile()
    {
        if (pendingDeleteProfile == null ||
            PlayerProfileManager.Instance == null)
        {
            return;
        }

        bool wasDeleted =
            PlayerProfileManager.Instance.DeleteProfile(
                pendingDeleteProfile.id
            );

        CloseDeleteModal();

        if (wasDeleted)
            RefreshProfileCards();
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

    private void ContinueToGame()
    {
        if (PlayerProfileManager.Instance == null ||
            PlayerProfileManager.Instance.SelectedProfile == null)
        {
            Debug.LogWarning("No profile selected.");
            return;
        }

        PlayerProfileData selectedProfile =
            PlayerProfileManager.Instance.SelectedProfile;

        Debug.Log(
            "Loading WorldMap with profile: " +
            selectedProfile.displayName +
            " | Coins: " +
            selectedProfile.coins
        );

        SceneManager.LoadScene("WorldMap");
    }

    private void OnDestroy()
    {
        if (addProfileButton != null)
            addProfileButton.onClick.RemoveListener(OpenCreateModal);

        if (closeButton != null)
            closeButton.onClick.RemoveListener(CloseCreateModal);

        if (confirmButton != null)
            confirmButton.onClick.RemoveListener(CreateProfile);

        foreach (AvatarOptionView option in avatarOptions)
        {
            if (option != null)
                option.Selected -= HandleAvatarSelected;
        }

        if (manageProfilesButton != null)
        {
            manageProfilesButton.onClick.RemoveListener(
                ToggleManagementMode
            );
        }

        if (cancelDeleteButton != null)
        {
            cancelDeleteButton.onClick.RemoveListener(
                CloseDeleteModal
            );
        }

        if (confirmDeleteButton != null)
        {
            confirmDeleteButton.onClick.RemoveListener(
                ConfirmDeleteProfile
            );
        }

        if (continueButton != null)
        {
            continueButton.onClick.RemoveListener(
                ContinueToGame
            );
        }
    }
}