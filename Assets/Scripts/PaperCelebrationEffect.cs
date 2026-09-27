using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PaperCelebrationEffect : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RectTransform effectArea;
    [SerializeField] private Sprite starSprite;

    [Header("Celebration Settings")]
    [SerializeField, Min(1)] private int pieceCount = 60;
    [SerializeField, Min(0.1f)] private float duration = 2.2f;

    [SerializeField]
    private Vector2 pieceSizeRange = new Vector2(16f, 34f);

    [SerializeField]
    private Vector2 speedRange = new Vector2(280f, 520f);

    [SerializeField, Min(0f)]
    private float gravity = 480f;

    [SerializeField]
    private Color[] colors =
    {
        new Color32(244, 190, 63, 255),
        new Color32(232, 91, 72, 255),
        new Color32(72, 157, 102, 255),
        new Color32(71, 154, 208, 255),
        new Color32(244, 143, 55, 255),
        new Color32(255, 238, 194, 255)
    };

    private readonly List<PaperPiece> activePieces =
        new List<PaperPiece>();

    private Coroutine celebrationRoutine;

    private void Awake()
    {
        if (effectArea == null)
            effectArea = transform as RectTransform;
    }

    public void PlayCelebration()
    {
        StopCelebration();

        if (effectArea == null)
            return;

        celebrationRoutine =
            StartCoroutine(PlayCelebrationRoutine());
    }

    public void StopCelebration()
    {
        if (celebrationRoutine != null)
        {
            StopCoroutine(celebrationRoutine);
            celebrationRoutine = null;
        }

        ClearPieces();
    }

    private IEnumerator PlayCelebrationRoutine()
    {
        CreateBurst(-1f);
        CreateBurst(1f);

        float elapsed = 0f;

        while (elapsed < duration)
        {
            float deltaTime = Time.unscaledDeltaTime;
            elapsed += deltaTime;

            float normalizedTime =
                Mathf.Clamp01(elapsed / duration);

            for (int i = 0; i < activePieces.Count; i++)
            {
                PaperPiece piece = activePieces[i];

                if (piece.rectTransform == null)
                    continue;

                piece.velocity +=
                    Vector2.down * gravity * deltaTime;

                piece.rectTransform.anchoredPosition +=
                    piece.velocity * deltaTime;

                piece.rectTransform.Rotate(
                    0f,
                    0f,
                    piece.rotationSpeed * deltaTime
                );

                Color color = piece.image.color;

                if (normalizedTime > 0.6f)
                {
                    color.a = 1f - Mathf.InverseLerp(
                        0.6f,
                        1f,
                        normalizedTime
                    );
                }

                piece.image.color = color;
            }

            yield return null;
        }

        ClearPieces();
        celebrationRoutine = null;
    }

    private void CreateBurst(float side)
    {
        int burstCount = pieceCount / 2;

        Vector2 burstOrigin = new Vector2(
            effectArea.rect.width * 0.3f * side,
            -effectArea.rect.height * 0.05f
        );

        for (int i = 0; i < burstCount; i++)
        {
            CreatePiece(burstOrigin);
        }
    }

    private void CreatePiece(Vector2 burstOrigin)
    {
        GameObject pieceObject = new GameObject(
            "Paper Confetti",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image)
        );

        pieceObject.transform.SetParent(effectArea, false);

        RectTransform pieceRect =
            pieceObject.GetComponent<RectTransform>();

        Image pieceImage =
            pieceObject.GetComponent<Image>();

        pieceImage.raycastTarget = false;

        bool useStar =
            starSprite != null &&
            Random.value < 0.2f;

        float size = Random.Range(
            pieceSizeRange.x,
            pieceSizeRange.y
        );

        if (useStar)
        {
            pieceImage.sprite = starSprite;
            pieceImage.preserveAspect = true;
            pieceRect.sizeDelta =
                new Vector2(size * 1.6f, size * 1.6f);
        }
        else
        {
            pieceRect.sizeDelta = new Vector2(
                size * Random.Range(0.4f, 0.7f),
                size * Random.Range(0.9f, 1.5f)
            );
        }

        pieceImage.color =
            colors != null && colors.Length > 0
                ? colors[Random.Range(0, colors.Length)]
                : Color.white;

        pieceRect.anchorMin = new Vector2(0.5f, 0.5f);
        pieceRect.anchorMax = new Vector2(0.5f, 0.5f);
        pieceRect.pivot = new Vector2(0.5f, 0.5f);

        pieceRect.anchoredPosition =
            burstOrigin +
            Random.insideUnitCircle * 35f;

        pieceRect.localRotation = Quaternion.Euler(
            0f,
            0f,
            Random.Range(0f, 360f)
        );

        float angle = Random.Range(25f, 155f);
        float speed = Random.Range(
            speedRange.x,
            speedRange.y
        );

        Vector2 direction = new Vector2(
            Mathf.Cos(angle * Mathf.Deg2Rad),
            Mathf.Sin(angle * Mathf.Deg2Rad)
        );

        activePieces.Add(new PaperPiece
        {
            rectTransform = pieceRect,
            image = pieceImage,
            velocity = direction * speed,
            rotationSpeed = Random.Range(-300f, 300f)
        });
    }

    private void ClearPieces()
    {
        for (int i = 0; i < activePieces.Count; i++)
        {
            if (activePieces[i].rectTransform != null)
            {
                Destroy(
                    activePieces[i]
                        .rectTransform
                        .gameObject
                );
            }
        }

        activePieces.Clear();
    }

    private void OnDisable()
    {
        StopCelebration();
    }

    private class PaperPiece
    {
        public RectTransform rectTransform;
        public Image image;
        public Vector2 velocity;
        public float rotationSpeed;
    }
}