using System.Collections;
using UnityEngine;

public class WorldMapTravelController : MonoBehaviour
{
    private const int LettersPerIsland = 5;

    [SerializeField] private WorldMapAvatarMover marker;
    [SerializeField] private SceneNavigator navigator;

    [SerializeField] private WorldMapIslandButton[] islands;
    [SerializeField] private WorldMapPath[] paths;

    [SerializeField] private CanvasGroup mapInteraction;

    private int currentIslandIndex;
    private bool travelling;
    private bool previousInteraction;
    private string parkingSaveKey;

    private void Start()
    {
        if (islands == null || islands.Length == 0)
            return;

        PlayerProfileData profile =
            PlayerProfileManager.Instance != null
                ? PlayerProfileManager.Instance.SelectedProfile
                : null;

        int highestUnlockedIsland = 0;

        if (profile != null)
        {
            highestUnlockedIsland = Mathf.Clamp(
                profile.highestUnlockedLetterIndex / LettersPerIsland,
                0,
                islands.Length - 1
            );

            parkingSaveKey =
                "WorldMapShipIsland_" + profile.id;
        }

        // Existing profiles start at their furthest unlocked island.
        // Later visits restore the ship's saved parking position.
        int savedIsland = string.IsNullOrEmpty(parkingSaveKey)
            ? highestUnlockedIsland
            : PlayerPrefs.GetInt(
                parkingSaveKey,
                highestUnlockedIsland
            );

        currentIslandIndex = Mathf.Clamp(
            savedIsland,
            0,
            highestUnlockedIsland
        );

        foreach (WorldMapIslandButton island in islands)
        {
            if (island != null)
                island.IslandSelected += SelectIsland;
        }

        RestoreMarkerPosition();
        SaveParkingPosition();
    }

    private void RestoreMarkerPosition()
    {
        if (marker == null || currentIslandIndex == 0)
            return;

        int pathIndex = currentIslandIndex - 1;

        if (paths != null &&
            pathIndex < paths.Length &&
            paths[pathIndex] != null)
        {
            marker.transform.position =
                paths[pathIndex].GetWorldPoint(1f);
        }
    }

    private void SelectIsland(int targetIndex)
    {
        if (travelling || navigator == null)
            return;

        if (islands == null ||
            targetIndex < 0 ||
            targetIndex >= islands.Length ||
            islands[targetIndex] == null)
        {
            return;
        }

        // Visiting an earlier island never moves the ship backward.
        if (targetIndex <= currentIslandIndex)
        {
            navigator.GoToIsland(targetIndex);
            return;
        }

        if (marker == null)
            return;

        // Validate all forward routes before starting.
        for (int i = currentIslandIndex; i < targetIndex; i++)
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

        while (currentIslandIndex < targetIndex)
        {
            marker.TravelPath(
                paths[currentIslandIndex],
                false
            );

            while (marker.IsMoving)
                yield return null;

            currentIslandIndex++;
            SaveParkingPosition();
        }

        RestoreInteraction();
        navigator.GoToIsland(targetIndex);
    }

    private void SaveParkingPosition()
    {
        if (string.IsNullOrEmpty(parkingSaveKey))
            return;

        PlayerPrefs.SetInt(
            parkingSaveKey,
            currentIslandIndex
        );

        PlayerPrefs.Save();
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