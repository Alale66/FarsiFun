using System.Collections;
using UnityEngine;

public class ProfileSelectionTransition : MonoBehaviour
{
    [Header("Selection Screen")]
    [SerializeField] private CanvasGroup titleBoard;
    [SerializeField] private CanvasGroup profileCardsContainer;
    [SerializeField] private CanvasGroup settingsButton;
    [SerializeField] private CanvasGroup continueButton;

    [Header("Create Modal")]
    [SerializeField] private CanvasGroup createProfileModal;
    [SerializeField] private RectTransform modalTitleBoard;

    [Header("Delete Modal")]
    [SerializeField] private CanvasGroup deleteProfileModal;
    [SerializeField] private RectTransform deleteModalTitleBoard;

    [Header("Movement")]
    [SerializeField, Min(0.1f)] private float duration = 0.45f;
    [SerializeField] private float titleMoveUp = 220f;
    [SerializeField] private float cardsMoveLeft = 1800f;
    [SerializeField] private float settingsMoveUp = 280f;
    [SerializeField] private float continueMoveDown = 280f;
    [SerializeField] private float modalTitleMoveDown = 220f;

    private Vector2 titleHome;
    private Vector2 cardsHome;
    private Vector2 settingsHome;
    private Vector2 continueHome;
    private Vector2 createTitleHome;
    private Vector2 deleteTitleHome;

    private CanvasGroup activeModal;
    private RectTransform activeModalTitle;
    private Vector2 activeModalTitleHome;
    private bool transitioning;

    private void Awake()
    {
        titleHome = Rect(titleBoard).anchoredPosition;
        cardsHome = Rect(profileCardsContainer).anchoredPosition;
        settingsHome = Rect(settingsButton).anchoredPosition;
        continueHome = Rect(continueButton).anchoredPosition;

        createTitleHome = modalTitleBoard.anchoredPosition;
        deleteTitleHome = deleteModalTitleBoard.anchoredPosition;
    }

    private void Start()
    {
        StartCoroutine(AnimatePageEntrance());
    }

    private IEnumerator AnimatePageEntrance()
    {
        transitioning = true;

        SetInteraction(titleBoard, false);
        SetInteraction(profileCardsContainer, false);
        SetInteraction(settingsButton, false);
        SetInteraction(continueButton, false);

        float elapsed = 0f;
        ApplyPageEntrance(0f);

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;

            float progress = Mathf.SmoothStep(
                0f, 1f, Mathf.Clamp01(elapsed / duration)
            );

            ApplyPageEntrance(progress);
            yield return null;
        }

        ApplyPageEntrance(1f);

        SetInteraction(titleBoard, true);
        SetInteraction(profileCardsContainer, true);
        SetInteraction(settingsButton, true);
        SetInteraction(continueButton, true);

        transitioning = false;
    }

    private void ApplyPageEntrance(float progress)
    {
        titleBoard.alpha = progress;
        profileCardsContainer.alpha = progress;
        settingsButton.alpha = progress;
        continueButton.alpha = progress;

        float remaining = 1f - progress;

        Rect(titleBoard).anchoredPosition =
            titleHome + Vector2.up * titleMoveUp * remaining;

        Rect(profileCardsContainer).anchoredPosition =
            cardsHome + Vector2.right * cardsMoveLeft * remaining;

        Rect(settingsButton).anchoredPosition =
            settingsHome + Vector2.up * settingsMoveUp * remaining;

        Rect(continueButton).anchoredPosition =
            continueHome + Vector2.down * continueMoveDown * remaining;
    }

    public void OpenCreateModal()
    {
        Open(createProfileModal, modalTitleBoard, createTitleHome);
    }

    public void CloseCreateModal()
    {
        Close(createProfileModal);
    }

    public void OpenDeleteModal()
    {
        Open(deleteProfileModal, deleteModalTitleBoard, deleteTitleHome);
    }

    public void CloseDeleteModal()
    {
        Close(deleteProfileModal);
    }

    private void Open(
        CanvasGroup modal,
        RectTransform modalTitle,
        Vector2 titleHomePosition)
    {
        if (transitioning || activeModal != null ||
            modal.gameObject.activeSelf)
            return;

        activeModal = modal;
        activeModalTitle = modalTitle;
        activeModalTitleHome = titleHomePosition;
        StartCoroutine(Animate(true));
    }

    private void Close(CanvasGroup modal)
    {
        if (transitioning || activeModal != modal ||
            !modal.gameObject.activeSelf)
            return;

        StartCoroutine(Animate(false));
    }

    private IEnumerator Animate(bool opening)
    {
        transitioning = true;
        activeModal.gameObject.SetActive(true);

        SetInteraction(titleBoard, false);
        SetInteraction(profileCardsContainer, false);
        SetInteraction(settingsButton, false);
        SetInteraction(continueButton, false);
        SetInteraction(activeModal, false);

        Apply(opening ? 0f : 1f);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;

            float progress = Mathf.SmoothStep(
                0f, 1f, Mathf.Clamp01(elapsed / duration)
            );

            Apply(opening ? progress : 1f - progress);
            yield return null;
        }

        Apply(opening ? 1f : 0f);

        if (opening)
        {
            SetInteraction(activeModal, true);
        }
        else
        {
            activeModal.gameObject.SetActive(false);

            SetInteraction(titleBoard, true);
            SetInteraction(profileCardsContainer, true);
            SetInteraction(settingsButton, true);
            SetInteraction(continueButton, true);

            activeModal = null;
            activeModalTitle = null;
        }

        transitioning = false;
    }

    private void Apply(float amount)
    {
        titleBoard.alpha = 1f - amount;
        profileCardsContainer.alpha = 1f - amount;
        settingsButton.alpha = 1f - amount;
        continueButton.alpha = 1f - amount;
        activeModal.alpha = amount;

        Rect(titleBoard).anchoredPosition =
            titleHome + Vector2.up * titleMoveUp * amount;

        Rect(profileCardsContainer).anchoredPosition =
            cardsHome + Vector2.right * cardsMoveLeft * amount;

        Rect(settingsButton).anchoredPosition =
            settingsHome + Vector2.up * settingsMoveUp * amount;

        Rect(continueButton).anchoredPosition =
            continueHome + Vector2.down * continueMoveDown * amount;

        activeModalTitle.anchoredPosition =
            activeModalTitleHome +
            Vector2.up * modalTitleMoveDown * (1f - amount);
    }

    private static RectTransform Rect(CanvasGroup group)
    {
        return (RectTransform)group.transform;
    }

    private static void SetInteraction(CanvasGroup group, bool enabled)
    {
        group.interactable = enabled;
        group.blocksRaycasts = enabled;
    }
}