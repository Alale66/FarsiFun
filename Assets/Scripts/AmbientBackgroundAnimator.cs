using UnityEngine;

public class AmbientBackgroundAnimator : MonoBehaviour
{
    [Header("Layers")]
    [SerializeField] private RectTransform cloudLayer;
    [SerializeField] private RectTransform sunLayer;
    [SerializeField] private RectTransform shipLayer;
    [SerializeField] private RectTransform waveMiddleLayer;
    [SerializeField] private RectTransform waveFrontLayer;

    [Header("Cloud")]
    [SerializeField] private float cloudDistance = 18f;
    [SerializeField] private float cloudDuration = 18f;

    [Header("Sun")]
    [SerializeField] private float sunDegreesPerSecond = 3f;

    [Header("Ship")]
    [SerializeField] private float shipVerticalDistance = 3f;
    [SerializeField] private float shipRotationAngle = 1.2f;
    [SerializeField] private float shipDuration = 4.8f;

    [Header("Middle Wave")]
    [SerializeField] private float middleWaveHorizontalDistance = 8f;
    [SerializeField] private float middleWaveVerticalDistance = 1.5f;
    [SerializeField] private float middleWaveDuration = 7f;

    [Header("Front Wave")]
    [SerializeField] private float frontWaveHorizontalDistance = 11f;
    [SerializeField] private float frontWaveVerticalDistance = 2f;
    [SerializeField] private float frontWaveDuration = 5.5f;

    private Vector2 cloudStartPosition;
    private Vector2 shipStartPosition;
    private Vector2 middleWaveStartPosition;
    private Vector2 frontWaveStartPosition;

    private Quaternion sunStartRotation;
    private Quaternion shipStartRotation;

    private void OnEnable()
    {
        if (cloudLayer != null)
            cloudStartPosition = cloudLayer.anchoredPosition;

        if (sunLayer != null)
            sunStartRotation = sunLayer.localRotation;

        if (shipLayer != null)
        {
            shipStartPosition = shipLayer.anchoredPosition;
            shipStartRotation = shipLayer.localRotation;
        }

        if (waveMiddleLayer != null)
        {
            middleWaveStartPosition =
                waveMiddleLayer.anchoredPosition;
        }

        if (waveFrontLayer != null)
        {
            frontWaveStartPosition =
                waveFrontLayer.anchoredPosition;
        }
    }

    private void Update()
    {
        float time = Time.unscaledTime;

        AnimateCloud(time);
        AnimateSun(time);
        AnimateShip(time);
        AnimateMiddleWave(time);
        AnimateFrontWave(time);
    }

    private void AnimateCloud(float time)
    {
        if (cloudLayer == null || cloudDuration <= 0f)
            return;

        float movement = Mathf.Sin(
            time * Mathf.PI * 2f / cloudDuration
        );

        cloudLayer.anchoredPosition =
            cloudStartPosition +
            Vector2.right * movement * cloudDistance;
    }

    private void AnimateSun(float time)
    {
        if (sunLayer == null)
            return;

        sunLayer.localRotation =
            sunStartRotation *
            Quaternion.Euler(
                0f,
                0f,
                -time * sunDegreesPerSecond
            );
    }

    private void AnimateShip(float time)
    {
        if (shipLayer == null || shipDuration <= 0f)
            return;

        float movement = Mathf.Sin(
            time * Mathf.PI * 2f / shipDuration
        );

        shipLayer.anchoredPosition =
            shipStartPosition +
            Vector2.up *
            movement *
            shipVerticalDistance;

        shipLayer.localRotation =
            shipStartRotation *
            Quaternion.Euler(
                0f,
                0f,
                movement * shipRotationAngle
            );
    }

    private void AnimateMiddleWave(float time)
    {
        if (waveMiddleLayer == null ||
            middleWaveDuration <= 0f)
        {
            return;
        }

        float phase =
            time * Mathf.PI * 2f / middleWaveDuration;

        waveMiddleLayer.anchoredPosition =
            middleWaveStartPosition +
            new Vector2(
                Mathf.Sin(phase) *
                middleWaveHorizontalDistance,

                Mathf.Cos(phase) *
                middleWaveVerticalDistance
            );
    }

    private void AnimateFrontWave(float time)
    {
        if (waveFrontLayer == null ||
            frontWaveDuration <= 0f)
        {
            return;
        }

        float phase =
            time * Mathf.PI * 2f / frontWaveDuration;

        waveFrontLayer.anchoredPosition =
            frontWaveStartPosition +
            new Vector2(
                -Mathf.Sin(phase) *
                frontWaveHorizontalDistance,

                Mathf.Cos(phase) *
                frontWaveVerticalDistance
            );
    }

    private void OnDisable()
    {
        if (cloudLayer != null)
            cloudLayer.anchoredPosition =
                cloudStartPosition;

        if (sunLayer != null)
            sunLayer.localRotation =
                sunStartRotation;

        if (shipLayer != null)
        {
            shipLayer.anchoredPosition =
                shipStartPosition;

            shipLayer.localRotation =
                shipStartRotation;
        }

        if (waveMiddleLayer != null)
        {
            waveMiddleLayer.anchoredPosition =
                middleWaveStartPosition;
        }

        if (waveFrontLayer != null)
        {
            waveFrontLayer.anchoredPosition =
                frontWaveStartPosition;
        }
    }
}