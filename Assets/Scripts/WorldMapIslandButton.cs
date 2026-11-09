using System;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button), typeof(Image))]
public class WorldMapIslandButton : MonoBehaviour
{
    [SerializeField, Min(0)] private int islandIndex;
    [SerializeField] private Sprite unlockedSprite;
    [SerializeField] private Sprite lockedSprite;
    [SerializeField] private GameObject lockIcon;
    [SerializeField] private AudioClip clickSound;

    private const int LettersPerIsland = 5;

    public event Action<int> IslandSelected;

    public void SelectIsland()
    {
        if (!GetComponent<Button>().interactable)
            return;

        NavigationClickAudio.Play(clickSound);

        IslandSelected?.Invoke(islandIndex);
    }

    private void Start()
    {
        Refresh();
    }

    public void Refresh()
    {
        PlayerProfileData profile =
            PlayerProfileManager.Instance != null
                ? PlayerProfileManager.Instance.SelectedProfile
                : null;

        bool unlocked = islandIndex == 0;

        if (islandIndex > 0 && profile != null)
        {
            int firstLetterIndex = islandIndex * LettersPerIsland;

            unlocked =
                profile.highestUnlockedLetterIndex >= firstLetterIndex;
        }

        Button button = GetComponent<Button>();
        button.transition = Selectable.Transition.None;
        button.interactable = unlocked;

        Image image = GetComponent<Image>();
        image.color = Color.white;

        Sprite sprite =
            unlocked ? unlockedSprite : lockedSprite;

        if (sprite != null)
            image.sprite = sprite;

        if (lockIcon != null)
            lockIcon.SetActive(!unlocked);
    }
}