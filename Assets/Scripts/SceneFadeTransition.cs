using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SceneFadeTransition : MonoBehaviour
{
    private static bool transitioning;

    [RuntimeInitializeOnLoadMethod(
        RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetState()
    {
        transitioning = false;
    }

    public static void GoTo(string sceneName)
    {
        if (transitioning)
            return;

        if (!Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Debug.LogError(
                "Scene is missing from the Build Profile: " +
                sceneName
            );

            return;
        }

        transitioning = true;

        GameObject root = new GameObject(
            "SceneFadeTransition"
        );

        DontDestroyOnLoad(root);

        SceneFadeTransition transition =
            root.AddComponent<SceneFadeTransition>();

        transition.StartCoroutine(
            transition.TransitionRoutine(sceneName)
        );
    }

    private IEnumerator TransitionRoutine(string sceneName)
    {
        Canvas canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 32767;

        gameObject.AddComponent<GraphicRaycaster>();

        GameObject overlayObject = new GameObject(
            "FadeOverlay",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image)
        );

        overlayObject.transform.SetParent(
            transform,
            false
        );

        Image overlay = overlayObject.GetComponent<Image>();

        RectTransform rect = overlay.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        overlay.raycastTarget = true;

        Color cream = new Color(
            1f, 0.96f, 0.87f, 0f
        );

        overlay.color = cream;

        // Gently cover the splash screen.
        yield return Fade(overlay, cream, 0f, 1f, 0.65f);

        AsyncOperation loading =
            SceneManager.LoadSceneAsync(sceneName);

        while (!loading.isDone)
            yield return null;

        // Let the new scene finish its initial UI setup.
        yield return null;
        Canvas.ForceUpdateCanvases();

        // Gently reveal the profile screen.
        yield return Fade(overlay, cream, 1f, 0f, 0.75f);

        transitioning = false;
        Destroy(gameObject);
    }

    private IEnumerator Fade(
        Image overlay,
        Color color,
        float from,
        float to,
        float duration)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;

            float t = Mathf.SmoothStep(
                0f,
                1f,
                Mathf.Clamp01(elapsed / duration)
            );

            color.a = Mathf.Lerp(from, to, t);
            overlay.color = color;

            yield return null;
        }

        color.a = to;
        overlay.color = color;
    }
}