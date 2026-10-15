using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
public class LetterIdleFloat : MonoBehaviour
{
    [SerializeField] private float amplitude = 6f;
    [SerializeField] private float speed = 1.5f;

    private RectTransform rect;
    private Button parentButton;
    private Vector2 homePosition;
    private float phaseOffset;

    private void Start()
    {
        rect = GetComponent<RectTransform>();
        parentButton = GetComponentInParent<Button>();
        homePosition = rect.anchoredPosition;
        phaseOffset = Random.Range(0f, Mathf.PI * 2f);
    }

    private void Update()
    {
        bool available =
            parentButton != null && parentButton.interactable;

        float offset = available
            ? Mathf.Sin(Time.unscaledTime * speed + phaseOffset) * amplitude
            : 0f;

        rect.anchoredPosition =
            homePosition + Vector2.up * offset;
    }

    private void OnDisable()
    {
        if (rect != null)
            rect.anchoredPosition = homePosition;
    }
}