using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(AudioSource))]
public class MenuMusicController : MonoBehaviour
{
    private static MenuMusicController instance;

    [SerializeField] private AudioSource musicSource;

    [SerializeField, Range(0f, 1f)]
    private float volume = 0.2f;

    [Header("Scenes with menu music")]
    [SerializeField] private string[] menuSceneNames =
    {
        "ProfileSelection",
        "WorldMap",
        "IslandView"
    };

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;

        // This object must be at the root of the Hierarchy.
        DontDestroyOnLoad(gameObject);

        if (musicSource == null)
            musicSource = GetComponent<AudioSource>();

        musicSource.playOnAwake = false;
        musicSource.loop = true;
        musicSource.spatialBlend = 0f;
        musicSource.volume = volume;

        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private void Start()
    {
        UpdateMusic(SceneManager.GetActiveScene().name);
    }

    private void HandleSceneLoaded(
        Scene scene,
        LoadSceneMode mode)
    {
        UpdateMusic(scene.name);
    }

    private void UpdateMusic(string sceneName)
    {
        if (musicSource == null)
            return;

        bool shouldPlay = false;

        if (menuSceneNames != null)
        {
            foreach (string menuScene in menuSceneNames)
            {
                if (sceneName == menuScene)
                {
                    shouldPlay = true;
                    break;
                }
            }
        }

        if (shouldPlay)
        {
            musicSource.volume = volume;

            // Keep the current playback position between menus.
            if (musicSource.clip != null &&
                !musicSource.isPlaying)
            {
                musicSource.Play();
            }
        }
        else
        {
            musicSource.Stop();
        }
    }

    private void OnDestroy()
    {
        if (instance != this)
            return;

        SceneManager.sceneLoaded -= HandleSceneLoaded;
        instance = null;
    }
}