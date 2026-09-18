
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

[RequireComponent(typeof(Button))]
public class PaperButtonFeedback : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler,
    IPointerDownHandler, IPointerUpHandler
{
    [SerializeField] private AudioClip clickSound;
    [SerializeField] private bool playClickSound = true;


    [SerializeField, Range(0f, 1f)]
    private float hoverBrightness = 0.9f;

    [SerializeField, Range(0f, 1f)]
    private float pressedBrightness = 0.8f;

    private static AudioSource sharedAudioSource;

    private Button button;
    private Graphic targetGraphic;
    private Color originalColor;
    private bool pointerInside;

    private void Awake()
    {
        button = GetComponent<Button>();
        targetGraphic = GetComponent<Graphic>();

        if (targetGraphic != null)
            originalColor = targetGraphic.color;

        if (sharedAudioSource == null)
        {
            GameObject audioObject = new GameObject("UI Click Audio");
            sharedAudioSource = audioObject.AddComponent<AudioSource>();
            sharedAudioSource.playOnAwake = false;
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        pointerInside = true;
        SetBrightness(hoverBrightness);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        pointerInside = false;
        SetBrightness(1f);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (button.interactable)
            SetBrightness(pressedBrightness);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        SetBrightness(pointerInside ? hoverBrightness : 1f);
    }

    private void SetBrightness(float brightness)
    {
        if (targetGraphic == null || !button.interactable)
            return;

        targetGraphic.color = new Color(
            originalColor.r * brightness,
            originalColor.g * brightness,
            originalColor.b * brightness,
            originalColor.a
        );
    }

    public void PlayClick()
    {
        if (!playClickSound || !button.interactable ||
            clickSound == null || sharedAudioSource == null)
            return;

        sharedAudioSource.PlayOneShot(clickSound);
    }

    private void OnDisable()
    {
        pointerInside = false;

        if (targetGraphic != null)
            targetGraphic.color = originalColor;
    }
    public void ResetFeedback()
    {
        pointerInside = false;

        if (targetGraphic != null)
            targetGraphic.color = originalColor;
    }
}
