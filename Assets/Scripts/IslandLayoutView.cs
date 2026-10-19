using UnityEngine;

public class IslandLayoutView : MonoBehaviour
{
    [SerializeField] private GameObject[] islandLayouts =
        new GameObject[8];

    private void Awake()
    {
        int selectedIndex = SceneNavigator.SelectedIslandIndex;

        for (int i = 0; i < islandLayouts.Length; i++)
        {
            if (islandLayouts[i] != null)
                islandLayouts[i].SetActive(i == selectedIndex);
        }
    }
}