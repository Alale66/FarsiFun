using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MatchCardView : MonoBehaviour
{
    [SerializeField] private TMP_Text exampleText;
    [SerializeField] private Image exampleImage;
    [SerializeField] private Button button;
    [SerializeField] private Image cardImage;
    [SerializeField] private Image checkMark;
    [SerializeField] private Button soundButton;
    [SerializeField] private GameObject baseline;

    private ExampleData example;
    private bool isImageCard;
    private bool isMatched;

    private MemoryMatchGame game;
    private Outline highlightOutline;

    public ExampleData Example => example;
    public bool IsImageCard => isImageCard;
    public bool IsMatched => isMatched;

    private void Awake()
    {
        // حاشیه را روی تصویر اصلی کارت ایجاد می‌کنیم،
        // بدون اینکه رنگ خود تصویر را تغییر بدهیم.
        if (cardImage == null)
            cardImage = GetComponent<Image>();

        if (button == null)
            button = GetComponent<Button>();

        if (cardImage != null)
        {
            highlightOutline =
                cardImage.GetComponent<Outline>();

            if (highlightOutline == null)
            {
                highlightOutline =
                    cardImage.gameObject.AddComponent<Outline>();
            }

            highlightOutline.effectDistance =
                new Vector2(10f, -10f);

            highlightOutline.useGraphicAlpha = false;
            highlightOutline.enabled = false;
        }
    }

    public void SetGame(MemoryMatchGame memoryGame)
    {
        game = memoryGame;

        button.onClick.RemoveAllListeners();

        button.onClick.AddListener(() =>
        {
            if (game != null && !isMatched)
                game.SelectCard(this);
        });

        if (soundButton != null)
        {
            soundButton.onClick.RemoveListener(
                HandlePlayWord
            );

            soundButton.onClick.AddListener(
                HandlePlayWord
            );
        }
    }

    public void SetupAsImage(ExampleData data)
    {
        example = data;
        isImageCard = true;

        exampleImage.gameObject.SetActive(true);
        exampleText.gameObject.SetActive(false);

        exampleImage.sprite = data.image;

        exampleImage.preserveAspect = true;

        ResetForNewRound();

        if (baseline != null)
            baseline.SetActive(false);
    }

    public void SetupAsWord(ExampleData data)
    {
        example = data;
        isImageCard = false;

        exampleImage.gameObject.SetActive(false);
        exampleText.gameObject.SetActive(true);

        PersianLetterHighlighter highlighter =
            exampleText.GetComponent<PersianLetterHighlighter>();

        if (highlighter != null)
            highlighter.SetText(data.word);
        else
        {
            exampleText.richText = true;
            exampleText.text =
                PersianLetterDisplay.GetWordText(data.word);
        }

        ResetForNewRound();

        if (baseline != null)
            baseline.SetActive(true);
    }

    private void ResetForNewRound()
    {
        isMatched = false;
        button.interactable = true;
        ResetVisual();

        if (checkMark != null)
            checkMark.gameObject.SetActive(false);

        RefreshSoundButton();
    }

    public void SetInteractable(bool value)
    {
        if (isMatched && value)
            return;

        button.interactable = value;
        RefreshSoundButton();
    }

    public void ShowCorrect()
    {
        isMatched = true;
        button.interactable = false;

        SetOutline(new Color32(79, 157, 94, 255));

        if (checkMark != null)
            checkMark.gameObject.SetActive(true);

        RefreshSoundButton();
    }

    private void SetOutline(Color color)
    {
        if (highlightOutline == null)
            return;

        highlightOutline.effectColor = color;
        highlightOutline.enabled = true;
    }

    public void ShowSelected()
    {
        if (isMatched)
            return;

        // نارنجی ملایم
        SetOutline(new Color32(232, 156, 56, 255));
    }

    public void ShowWrong()
    {
        if (isMatched)
            return;

        if (checkMark != null)
            checkMark.gameObject.SetActive(false);

        SetOutline(new Color32(201, 104, 91, 255));
    }

    public void ResetVisual()
    {
        if (isMatched)
            return;

        if (highlightOutline != null)
            highlightOutline.enabled = false;
    }

    private void HandlePlayWord()
    {
        if (game != null &&
            isImageCard &&
            !isMatched &&
            button != null &&
            button.interactable)
        {
            game.PlayCardWord(this);
        }
    }

    private void RefreshSoundButton()
    {
        if (soundButton == null)
            return;

        soundButton.gameObject.SetActive(isImageCard);

        soundButton.interactable =
            isImageCard &&
            !isMatched &&
            button != null &&
            button.interactable &&
            example != null &&
            example.audio != null &&
            game != null &&
            game.HasWordAudioPlayer;
    }
}