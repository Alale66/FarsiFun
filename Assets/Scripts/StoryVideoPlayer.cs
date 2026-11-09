using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

public class StoryVideoPlayer : MonoBehaviour
{
    [SerializeField] private VideoPlayer videoPlayer;
    [SerializeField] private RawImage videoImage;

    [SerializeField, Min(0f)]
    private float endHoldDuration = 1.2f;

    private Coroutine endRoutine;

    private Action<bool> onFinished;
    private Coroutine prepareRoutine;
    private bool playing;

    private void OnEnable()
    {
        if (videoPlayer == null)
            return;

        videoPlayer.prepareCompleted += HandlePrepared;
        videoPlayer.loopPointReached += HandleEnded;
        videoPlayer.errorReceived += HandleError;
    }

    private void OnDisable()
    {
        if (videoPlayer != null)
        {
            videoPlayer.prepareCompleted -= HandlePrepared;
            videoPlayer.loopPointReached -= HandleEnded;
            videoPlayer.errorReceived -= HandleError;
        }

        CancelPlayback();
    }

    public void Play(VideoClip clip, Action<bool> finished)
    {
        CancelPlayback();

        if (videoPlayer == null ||
            videoImage == null ||
            videoPlayer.targetTexture == null ||
            clip == null)
        {
            Debug.LogError("Story video setup is incomplete.");
            finished?.Invoke(false);
            return;
        }

        gameObject.SetActive(true);

        onFinished = finished;
        playing = true;

        // Hide the previous frame while preparing.
        videoImage.enabled = false;
        videoImage.texture = videoPlayer.targetTexture;

        videoPlayer.playOnAwake = false;
        videoPlayer.isLooping = false;
        videoPlayer.source = VideoSource.VideoClip;
        videoPlayer.clip = clip;

        prepareRoutine = StartCoroutine(PrepareWithTimeout());
    }

    private IEnumerator PrepareWithTimeout()
    {
        videoPlayer.Prepare();

        float elapsed = 0f;

        while (playing && !videoPlayer.isPrepared)
        {
            elapsed += Time.unscaledDeltaTime;

            if (elapsed >= 20f)
            {
                // Clear before Finish to avoid stopping this routine.
                prepareRoutine = null;
                Debug.LogError("Story video preparation timed out.");
                Finish(false);
                yield break;
            }

            yield return null;
        }

        prepareRoutine = null;
    }

    private void HandlePrepared(VideoPlayer source)
    {
        if (!playing)
            return;

        videoImage.enabled = true;
        source.Play();
    }

    private void HandleEnded(VideoPlayer source)
    {
        if (!playing || endRoutine != null)
            return;

        source.Pause();

        endRoutine = StartCoroutine(HoldLastFrame());
    }

    private IEnumerator HoldLastFrame()
    {
        yield return new WaitForSecondsRealtime(
            endHoldDuration
        );

        endRoutine = null;
        Finish(true);
    }

    private void HandleError(VideoPlayer source, string message)
    {
        Debug.LogError("Story video: " + message);
        Finish(false);
    }

    private void Finish(bool succeeded)
    {
        if (!playing)
            return;

        Action<bool> callback = onFinished;

        CancelPlayback();
        gameObject.SetActive(false);

        callback?.Invoke(succeeded);
    }

    private void CancelPlayback()
    {
        playing = false;
        onFinished = null;

        if (endRoutine != null)
        {
            StopCoroutine(endRoutine);
            endRoutine = null;
        }

        if (prepareRoutine != null)
        {
            StopCoroutine(prepareRoutine);
            prepareRoutine = null;
        }

        if (videoPlayer != null)
            videoPlayer.Stop();

        if (videoImage != null)
            videoImage.enabled = false;
    }

    [ContextMenu("Test Video Playback")]
    private void TestVideoPlayback()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("Enter Play mode first.");
            return;
        }

        if (videoPlayer == null || videoPlayer.clip == null)
        {
            Debug.LogWarning("Assign a test Video Clip first.");
            return;
        }

        Play(videoPlayer.clip, succeeded =>
        {
            Debug.Log(
                succeeded
                    ? "Story video finished successfully."
                    : "Story video playback failed."
            );
        });
    }
}