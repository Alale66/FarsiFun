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

    private bool mutedForBonusGame;

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

        float targetVolume;

        if (mutedForBonusGame)
        {
            targetVolume = 0f;
        }
        else
        {
            targetVolume = shouldDuck
                ? duckedVolume
                : normalVolume;
        }

        ambientSource.volume = Mathf.MoveTowards(
            ambientSource.volume,
            targetVolume,
            volumeChangeSpeed * Time.unscaledDeltaTime
        );
    }

    public void SetMutedForBonusGame(bool muted)
    {
        mutedForBonusGame = muted;

        if (ambientSource == null)
            return;

        if (muted)
        {
            ambientSource.volume = 0f;
        }
        else
        {
            if (!ambientSource.isPlaying)
                ambientSource.Play();

            ambientSource.volume = normalVolume;
        }
    }
}