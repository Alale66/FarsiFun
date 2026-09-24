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

    public void PlayLockedSequence(
    AudioClip[] clips,
    Action onComplete = null)
{
    if (clips == null || clips.Length == 0 || IsPlayingLocked)
        return;

    audioRoutine = StartCoroutine(
        PlayLockedSequenceRoutine(clips, onComplete)
    );
}

private IEnumerator PlayLockedSequenceRoutine(
    AudioClip[] clips,
    Action onComplete)
{
    IsPlayingLocked = true;

    if (inputLock != null)
        inputLock.Lock();

    foreach (AudioClip clip in clips)
    {
        if (clip == null)
            continue;

        audioSource.clip = clip;
        audioSource.Play();

        yield return new WaitWhile(
            () => audioSource.isPlaying
        );
    }

    if (inputLock != null)
        inputLock.Unlock();

    IsPlayingLocked = false;
    audioRoutine = null;

    onComplete?.Invoke();
}
}