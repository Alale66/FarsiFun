using UnityEngine;

public static class NavigationClickAudio
{
    private static AudioSource source;

    public static void Play(AudioClip clip)
    {
        if (clip == null)
            return;

        if (source == null)
        {
            GameObject audioObject =
                new GameObject("Navigation Click Audio");

            Object.DontDestroyOnLoad(audioObject);

            source = audioObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = false;
            source.spatialBlend = 0f;
        }

        source.PlayOneShot(clip);
    }
}