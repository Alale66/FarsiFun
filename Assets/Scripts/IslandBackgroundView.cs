using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class IslandBackgroundView : MonoBehaviour
{
    [SerializeField] private Sprite[] islandBackgrounds =
        new Sprite[8];

    private void Start()
    {
        int index = SceneNavigator.SelectedIslandIndex;

        if (islandBackgrounds == null ||
            index < 0 ||
            index >= islandBackgrounds.Length ||
            islandBackgrounds[index] == null)
        {
            Debug.LogError(
                $"Island background is missing for island {index + 1}.",
                this
            );
            return;
        }

        GetComponent<Image>().sprite = islandBackgrounds[index];
    }
}