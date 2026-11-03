using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class NavigationButtonSound : MonoBehaviour
{
    [SerializeField] private AudioClip clickSound;

    private Button button;

    private void Awake()
    {
        button = GetComponent<Button>();
        button.onClick.AddListener(PlayClick);
    }

    private void PlayClick()
    {
        NavigationClickAudio.Play(clickSound);
    }

    private void OnDestroy()
    {
        if (button != null)
            button.onClick.RemoveListener(PlayClick);
    }
}