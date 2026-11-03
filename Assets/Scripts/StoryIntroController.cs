using UnityEngine;
using UnityEngine.Video;

[RequireComponent(typeof(VideoPlayer))]
public class StoryIntroController : MonoBehaviour
{
    [SerializeField] private string nextScene = "ProfileSelection";

    private VideoPlayer videoPlayer;
    private bool leaving;

    private void Awake()
    {
        videoPlayer = GetComponent<VideoPlayer>();
        videoPlayer.isLooping = false;
    }

    private void OnEnable()
    {
        videoPlayer.loopPointReached += OnVideoFinished;
    }

    private void OnDisable()
    {
        videoPlayer.loopPointReached -= OnVideoFinished;
    }

    private void OnVideoFinished(VideoPlayer source)
    {
        Skip();
    }

    public void Skip()
    {
        if (leaving)
            return;

        leaving = true;
        videoPlayer.Pause();
        SceneFadeTransition.GoTo(nextScene);
    }
}