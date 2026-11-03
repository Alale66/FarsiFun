using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneNavigator : MonoBehaviour
{
    public static int SelectedLetterIndex { get; private set; }
    public static int SelectedIslandIndex { get; private set; }

    [RuntimeInitializeOnLoadMethod(
        RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetSelection()
    {
        SelectedLetterIndex = 0;
        SelectedIslandIndex = 0;
    }

    public void GoToScene(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }

    public void GoToIsland(int islandIndex)
    {
        SelectedIslandIndex = Mathf.Clamp(islandIndex, 0, 7);

        PlayerProfileData profile =
            PlayerProfileManager.Instance != null
                ? PlayerProfileManager.Instance.SelectedProfile
                : null;

        if (profile != null)
        {
            string parkingKey = "WorldMapShipIsland_" + profile.id;

            int highestUnlockedIsland = Mathf.Clamp(
                profile.highestUnlockedLetterIndex / 5,
                0,
                7
            );

            int savedIsland = PlayerPrefs.GetInt(
                parkingKey,
                highestUnlockedIsland
            );

            int parkingIsland = Mathf.Clamp(
                Mathf.Max(savedIsland, SelectedIslandIndex),
                0,
                highestUnlockedIsland
            );

            PlayerPrefs.SetInt(parkingKey, parkingIsland);
            PlayerPrefs.Save();
        }

        SceneManager.LoadScene("IslandView");
    }

    public void GoToLetter(int letterIndex)
    {
        SelectedLetterIndex = Mathf.Max(0, letterIndex);
        SceneManager.LoadScene("MainGame");
    }
}