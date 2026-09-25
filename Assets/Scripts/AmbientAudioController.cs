using UnityEngine;

public class AmbientAudioController : MonoBehaviour
{
    [SerializeField] private AudioSource ambientSource;
    [SerializeField] private GameAudioManager gameAudioManager;

    [Header("Volume")]
    [SerializeField, Range(0f, 1f)]
    private float normalVolume = 0.05f;

    [SerializeField, Range(0f, 1f)]
    private float duckedVolume = 0.01f;

    [SerializeField]
    private float volumeChangeSpeed = 0.15f;

    private void Awake()
    {
        if (ambientSource == null)
            ambientSource = GetComponent<AudioSource>();
    }

    private void Start()
    {
        if (ambientSource == null)
            return;

        ambientSource.loop = true;
        ambientSource.volume = normalVolume;

        if (!ambientSource.isPlaying)
            ambientSource.Play();
    }

    private void Update()
    {
        if (ambientSource == null)
            return;

        bool shouldDuck =
            gameAudioManager != null &&
            gameAudioManager.IsPlayingLocked;

        float targetVolume = shouldDuck
            ? duckedVolume
            : normalVolume;

        ambientSource.volume = Mathf.MoveTowards(
            ambientSource.volume,
            targetVolume,
            volumeChangeSpeed * Time.unscaledDeltaTime
        );
    }
}