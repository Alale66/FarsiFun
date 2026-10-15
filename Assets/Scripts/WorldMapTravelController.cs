using System.Collections;
using UnityEngine;

public class WorldMapTravelController : MonoBehaviour
{
    [SerializeField] private WorldMapAvatarMover marker;
    [SerializeField] private SceneNavigator navigator;

    [SerializeField] private WorldMapIslandButton[] islands;
    [SerializeField] private WorldMapPath[] paths;

    [SerializeField] private CanvasGroup mapInteraction;

    private int currentIslandIndex;
    private bool travelling;
    private bool previousInteraction;

    private void Start()
    {
        currentIslandIndex = Mathf.Clamp(
            SceneNavigator.SelectedIslandIndex, 0, 7
        );

        foreach (WorldMapIslandButton island in islands)
        {
            if (island != null)
                island.IslandSelected += SelectIsland;
        }

        // Restore the marker beside the last visited island.
        if (currentIslandIndex > 0 &&
            marker != null &&
            paths != null &&
            currentIslandIndex - 1 < paths.Length &&
            paths[currentIslandIndex - 1] != null)
        {
            marker.transform.position =
                paths[currentIslandIndex - 1].GetWorldPoint(1f);
        }
    }

    private void SelectIsland(int targetIndex)
    {
        if (travelling || marker == null || navigator == null)
            return;

        if (targetIndex < 0 || targetIndex >= islands.Length)
            return;

        if (currentIslandIndex == targetIndex)
        {
            navigator.GoToIsland(targetIndex);
            return;
        }

        int firstPath = Mathf.Min(
            currentIslandIndex, targetIndex
        );

        int lastPath = Mathf.Max(
            currentIslandIndex, targetIndex
        );

        // Check every required route before moving.
        for (int i = firstPath; i < lastPath; i++)
        {
            if (paths == null ||
                i >= paths.Length ||
                paths[i] == null)
            {
                Debug.LogError(
                    "WorldMap: Missing route at index " + i
                );
                return;
            }
        }

        StartCoroutine(TravelToIsland(targetIndex));
    }

    private IEnumerator TravelToIsland(int targetIndex)
    {
        travelling = true;

        if (mapInteraction != null)
        {
            previousInteraction = mapInteraction.interactable;
            mapInteraction.interactable = false;
        }

        while (currentIslandIndex != targetIndex)
        {
            bool reverse = targetIndex < currentIslandIndex;

            int pathIndex = reverse
                ? currentIslandIndex - 1
                : currentIslandIndex;

            marker.TravelPath(paths[pathIndex], reverse);

            while (marker.IsMoving)
                yield return null;

            currentIslandIndex += reverse ? -1 : 1;
        }

        RestoreInteraction();
        navigator.GoToIsland(targetIndex);
    }

    private void RestoreInteraction()
    {
        if (travelling && mapInteraction != null)
            mapInteraction.interactable = previousInteraction;

        travelling = false;
    }

    private void OnDisable()
    {
        StopAllCoroutines();
        RestoreInteraction();
    }

    private void OnDestroy()
    {
        if (islands == null)
            return;

        foreach (WorldMapIslandButton island in islands)
        {
            if (island != null)
                island.IslandSelected -= SelectIsland;
        }
    }
}