using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class IslandPurchasePanelUI : MonoBehaviour
{
    [SerializeField] private Image itemImage;
    [SerializeField] private TMP_Text itemNameText;
    [SerializeField] private TMP_Text priceText;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private Button buyButton;
    [SerializeField] private Button returnButton;
    [SerializeField]
    private PlayerWalletDisplay playerWallet;
    [SerializeField, Min(0.1f)]
    private float fadeInDuration = 0.5f;
    [Header("Payment Animation")]
    [SerializeField] private Sprite paymentCoinSprite;
    [SerializeField] private AudioClip paymentCoinSound;
    [SerializeField, Min(0.05f)]
    private float coinFlightDuration = 0.16f;
    [SerializeField] private AudioClip purchaseSuccessSound;

    private GameObject flyingCoin;
    private bool paymentAnimating;

    private CanvasGroup canvasGroup;
    private Coroutine fadeRoutine;
    private Action buyAction;
    private Action returnAction;
    private bool busy;

    private void OnEnable()
    {
        buyButton.onClick.AddListener(HandleBuy);
        returnButton.onClick.AddListener(HandleReturn);
    }

    private void OnDisable()
    {
        if (paymentAnimating)
            EndPaymentDisplay();

        if (flyingCoin != null)
        {
            Destroy(flyingCoin);
            flyingCoin = null;
        }

        if (fadeRoutine != null)
        {
            StopCoroutine(fadeRoutine);
            fadeRoutine = null;
        }
        buyButton.onClick.RemoveListener(HandleBuy);
        returnButton.onClick.RemoveListener(HandleReturn);

        buyAction = null;
        returnAction = null;
    }

    public void Show(
        IslandStoryData story,
        int coins,
        Action onBuy,
        Action onReturn)
    {
        if (story == null)
            return;

        buyAction = onBuy;
        returnAction = onReturn;

        itemImage.sprite = story.itemSprite;
        itemImage.enabled = story.itemSprite != null;
        itemImage.preserveAspect = true;

        SetText(itemNameText, story.itemName);
        SetText(priceText, ToPersianDigits(story.price) + " سکه");

        Refresh(story.price, coins);

        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();

            if (canvasGroup == null)
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        if (fadeRoutine != null)
        {
            StopCoroutine(fadeRoutine);
            fadeRoutine = null;
        }

        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = true;

        gameObject.SetActive(true);
        transform.SetAsLastSibling();

        if (playerWallet != null)
        {
            playerWallet.gameObject.SetActive(true);
            playerWallet.RefreshDisplay();
            playerWallet.transform.SetAsLastSibling();
        }

        fadeRoutine = StartCoroutine(FadeIn());
    }

    public void Refresh(int price, int coins)
    {
        statusText.color = Color.black;
        busy = false;

        int missing = Mathf.Max(0, price - coins);

        buyButton.interactable = missing == 0 && price > 0;
        returnButton.interactable = true;

        SetText(
            statusText,
            missing > 0
                ? ToPersianDigits(missing) + " سکه دیگه لازم داری!"
                : "برای ادامهٔ سفر آماده‌ای؟"
        );
    }

    public void ShowError(string message)
    {
        statusText.color = Color.black;
        busy = false;
        buyButton.interactable = false;
        returnButton.interactable = true;
        SetText(statusText, message);
    }

    public void Hide()
    {
        buyAction = null;
        returnAction = null;
        gameObject.SetActive(false);
    }

    private IEnumerator FadeIn()
    {
        float elapsed = 0f;
        float duration = Mathf.Max(0.1f, fadeInDuration);

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;

            float progress = Mathf.Clamp01(
                elapsed / duration
            );

            canvasGroup.alpha = Mathf.SmoothStep(
                0f,
                1f,
                progress
            );

            yield return null;
        }

        canvasGroup.alpha = 1f;
        canvasGroup.interactable = true;
        fadeRoutine = null;
    }

    private void HandleBuy()
    {
        if (busy || buyAction == null)
            return;

        busy = true;
        buyButton.interactable = false;
        returnButton.interactable = false;

        buyAction.Invoke();
    }

    private void HandleReturn()
    {
        if (busy)
            return;

        returnAction?.Invoke();
    }

    private static void SetText(TMP_Text label, string text)
    {
        label.text = PersianLetterDisplay.GetWordText(
            text ?? string.Empty
        );
    }

    private static string ToPersianDigits(int value)
    {
        string result = value.ToString();

        for (int i = 0; i <= 9; i++)
        {
            result = result.Replace(
                (char)('0' + i),
                (char)('۰' + i)
            );
        }

        return result;
    }

    public void BeginPaymentDisplay(int startingCoins)
    {
        paymentAnimating = true;
        busy = true;

        buyButton.interactable = false;
        returnButton.interactable = false;

        if (playerWallet != null)
            playerWallet.BeginPaymentDisplay(startingCoins);
    }

    public void EndPaymentDisplay()
    {
        paymentAnimating = false;

        if (playerWallet != null)
            playerWallet.EndPaymentDisplay();
    }

    public void PlayPaymentAnimation(
        int startingCoins,
        int remainingCoins,
        Action finished)
    {
        StartCoroutine(
            PaymentAnimationRoutine(
                startingCoins,
                remainingCoins,
                finished
            )
        );
    }

    private IEnumerator PaymentAnimationRoutine(
        int startingCoins,
        int remainingCoins,
        Action finished)
    {
        const int visualCoinCount = 20;

        RectTransform panel =
            (RectTransform)transform;

        RectTransform walletRect = playerWallet != null
            ? playerWallet.GetComponent<RectTransform>()
            : null;

        RectTransform itemRect = itemImage.rectTransform;

        Vector3 startPosition = walletRect != null
            ? panel.InverseTransformPoint(
                walletRect.TransformPoint(walletRect.rect.center)
            )
            : Vector3.zero;

        Vector3 targetPosition = panel.InverseTransformPoint(
            itemRect.TransformPoint(itemRect.rect.center)
        );

        Image coinImage = null;
        RectTransform coinRect = null;

        if (paymentCoinSprite != null)
        {
            flyingCoin = new GameObject(
                "payment_coin",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image)
            );

            flyingCoin.transform.SetParent(transform, false);
            flyingCoin.transform.SetAsLastSibling();

            coinImage = flyingCoin.GetComponent<Image>();
            coinImage.sprite = paymentCoinSprite;
            coinImage.preserveAspect = true;
            coinImage.raycastTarget = false;

            coinRect = flyingCoin.GetComponent<RectTransform>();
            coinRect.anchorMin = panel.pivot;
            coinRect.anchorMax = panel.pivot;
            coinRect.sizeDelta = new Vector2(65f, 65f);
        }

        float duration = paymentCoinSound != null &&
                        paymentCoinSound.length > 0f
            ? paymentCoinSound.length / visualCoinCount
            : Mathf.Max(0.05f, coinFlightDuration);

        if (paymentCoinSound != null)
            NavigationClickAudio.Play(paymentCoinSound);

        for (int i = 0; i < visualCoinCount; i++)
        {
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;

                float progress = Mathf.Clamp01(
                    elapsed / duration
                );

                if (coinRect != null)
                {
                    Vector3 position = Vector3.Lerp(
                        startPosition,
                        targetPosition,
                        progress
                    );

                    position.y +=
                        Mathf.Sin(progress * Mathf.PI) * 90f;

                    coinRect.localPosition = position;
                    coinRect.localScale = Vector3.one *
                        Mathf.Lerp(1f, 0.65f, progress);
                }

                float paymentProgress =
                    (i + progress) / visualCoinCount;

                int displayedCoins = Mathf.RoundToInt(
                    Mathf.Lerp(
                        startingCoins,
                        remainingCoins,
                        paymentProgress
                    )
                );

                if (playerWallet != null)
                    playerWallet.ShowPaymentBalance(displayedCoins);

                yield return null;
            }

            // if (paymentCoinSound != null)
            //     NavigationClickAudio.Play(paymentCoinSound);
        }

        if (flyingCoin != null)
        {
            Destroy(flyingCoin);
            flyingCoin = null;
        }

        if (purchaseSuccessSound != null)
            NavigationClickAudio.Play(purchaseSuccessSound);

        if (playerWallet != null)
            playerWallet.ShowPaymentBalance(remainingCoins);

        SetText(statusText, "خرید انجام شد!\n");
        statusText.color = new Color32(38, 115, 67, 255);

        yield return new WaitForSecondsRealtime(3f);

        EndPaymentDisplay();
        finished?.Invoke();
    }

#if UNITY_EDITOR
    [Header("Payment Preview")]
    [SerializeField] private IslandStoryData paymentTestStory;

    [ContextMenu("Testing/Preview Payment")]
    private void PreviewPayment()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("Enter Play mode first.");
            return;
        }

        if (paymentAnimating || busy)
            return;

        if (paymentTestStory == null ||
            paymentTestStory.price <= 0)
        {
            Debug.LogWarning("Assign Payment Test Story first.");
            return;
        }

        int testStartingCoins = paymentTestStory.price + 50;

        Show(
            paymentTestStory,
            testStartingCoins,
            () =>
            {
                BeginPaymentDisplay(testStartingCoins);

                PlayPaymentAnimation(
                    testStartingCoins,
                    50,
                    () =>
                    {
                        Hide();

                        if (playerWallet != null)
                            playerWallet.RefreshDisplay();
                    }
                );
            },
            () =>
            {
                Hide();

                if (playerWallet != null)
                    playerWallet.RefreshDisplay();
            }
        );

        if (playerWallet != null)
            playerWallet.ShowPaymentBalance(testStartingCoins);
    }
#endif
}