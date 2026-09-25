using System;
using System.Collections;
using UnityEngine;

public class GameAudioManager : MonoBehaviour
{
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private UIInputLock inputLock;

    private Coroutine audioRoutine;

    public bool IsPlayingLocked { get; private set; }

    public void PlayLocked(AudioClip clip, Action onComplete = null)
    {
        if (clip == null || IsPlayingLocked)
            return;

        audioRoutine = StartCoroutine(
            PlayLockedRoutine(clip, onComplete)
        );
    }

    private IEnumerator PlayLockedRoutine(
        AudioClip clip,
        Action onComplete)
    {
        IsPlayingLocked = true;

        if (inputLock != null)
            inputLock.Lock();

        audioSource.clip = clip;
        audioSource.Play();

        yield return new WaitWhile(
            () => audioSource.isPlaying
        );

        if (inputLock != null)
            inputLock.Unlock();

        IsPlayingLocked = false;
        audioRoutine = null;

        onComplete?.Invoke();
    }

    public void PlayLockedSequence(
        AudioClip[] clips,
        Action onComplete = null)
    {
        PlayLockedSequence(clips, 0f, onComplete);
    }

    public void PlayLockedSequence(
        AudioClip[] clips,
        float pauseBetweenClips,
        Action onComplete = null)
    {
        if (clips == null ||
            clips.Length == 0 ||
            IsPlayingLocked)
        {
            return;
        }

        audioRoutine = StartCoroutine(
            PlayLockedSequenceRoutine(
                clips,
                pauseBetweenClips,
                onComplete
            )
        );
    }

    private IEnumerator PlayLockedSequenceRoutine(
        AudioClip[] clips,
        float pauseBetweenClips,
        Action onComplete)
    {
        IsPlayingLocked = true;

        if (inputLock != null)
            inputLock.Lock();

        for (int i = 0; i < clips.Length; i++)
        {
            AudioClip clip = clips[i];

            if (clip == null)
                continue;

            audioSource.clip = clip;
            audioSource.Play();

            yield return new WaitWhile(
                () => audioSource.isPlaying
            );

            if (pauseBetweenClips > 0f &&
                i < clips.Length - 1)
            {
                yield return new WaitForSeconds(
                    pauseBetweenClips
                );
            }
        }

        if (inputLock != null)
            inputLock.Unlock();

        IsPlayingLocked = false;
        audioRoutine = null;

        onComplete?.Invoke();
    }

    public void StopAudio()
    {
        if (audioRoutine != null)
        {
            StopCoroutine(audioRoutine);
            audioRoutine = null;
        }

        if (audioSource != null)
            audioSource.Stop();

        if (inputLock != null)
            inputLock.ForceUnlock();

        IsPlayingLocked = false;
    }
}