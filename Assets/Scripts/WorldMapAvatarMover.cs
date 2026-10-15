using System;
using System.Collections;
using UnityEngine;

public class WorldMapAvatarMover : MonoBehaviour
{
    [SerializeField, Min(0.1f)]
    private float approachDuration = 0.8f;

    [SerializeField, Min(0.1f)]
    private float travelDuration = 2.5f;

    public bool IsMoving { get; private set; }

    public event Action TravelCompleted;

    public void TravelPath(
        WorldMapPath path,
        bool reverse = false)
    {
        if (IsMoving || path == null)
            return;

        StartCoroutine(MoveAlongPath(path, reverse));
    }

    private IEnumerator MoveAlongPath(
        WorldMapPath path,
        bool reverse)
    {
        IsMoving = true;

        Vector3 originalPosition = transform.position;

        Vector3 startPosition = path.GetWorldPoint(
            reverse ? 1f : 0f
        );

        float elapsed = 0f;

        // Move from the current position to the route entrance.
        while (elapsed < approachDuration)
        {
            elapsed += Time.unscaledDeltaTime;

            float progress = Mathf.SmoothStep(
                0f,
                1f,
                Mathf.Clamp01(elapsed / approachDuration)
            );

            transform.position = Vector3.Lerp(
                originalPosition,
                startPosition,
                progress
            );

            yield return null;
        }

        transform.position = startPosition;
        elapsed = 0f;

        // Follow the route forward or backward.
        while (elapsed < travelDuration)
        {
            elapsed += Time.unscaledDeltaTime;

            float progress = Mathf.Clamp01(
                elapsed / travelDuration
            );

            transform.position = path.GetWorldPoint(
                reverse ? 1f - progress : progress
            );

            yield return null;
        }

        transform.position = path.GetWorldPoint(
            reverse ? 0f : 1f
        );

        IsMoving = false;
        TravelCompleted?.Invoke();
    }

    private void OnDisable()
    {
        StopAllCoroutines();
        IsMoving = false;
    }
}