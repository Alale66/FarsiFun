using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class PlayerProfileManager : MonoBehaviour
{
    public static PlayerProfileManager Instance { get; private set; }
    public const int MaximumProfiles = 4;
    public event Action<int> CoinsChanged;

    private const int StartingCoins = 20;
    private const string SaveFileName = "player_profiles.json";

    private PlayerProfileSaveData saveData =
        new PlayerProfileSaveData();

    public IReadOnlyList<PlayerProfileData> Profiles =>
        saveData.profiles;

    public PlayerProfileData SelectedProfile =>
        FindProfile(saveData.selectedProfileId);

    private string SavePath =>
        Path.Combine(
            Application.persistentDataPath,
            SaveFileName
        );

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        Load();
    }

    public PlayerProfileData CreateProfile(
        string playerName,
        int avatarIndex)
    {
        if (saveData.profiles.Count >= MaximumProfiles)
        {
            Debug.LogWarning(
                "Maximum profile count reached."
            );

            return null;
        }

        PlayerProfileData profile =
            new PlayerProfileData(
                Guid.NewGuid().ToString(),
                playerName.Trim(),
                avatarIndex,
                StartingCoins
            );

        saveData.profiles.Add(profile);
        saveData.selectedProfileId = profile.id;

        Save();
        return profile;
    }

    public void SelectProfile(string profileId)
    {
        if (FindProfile(profileId) == null)
            return;

        saveData.selectedProfileId = profileId;
        Save();
    }

    public bool DeleteProfile(string profileId)
    {
        PlayerProfileData profile =
            FindProfile(profileId);

        if (profile == null)
            return false;

        bool deletedSelectedProfile =
            saveData.selectedProfileId == profileId;

        saveData.profiles.Remove(profile);

        if (deletedSelectedProfile)
        {
            saveData.selectedProfileId =
                saveData.profiles.Count > 0
                    ? saveData.profiles[
                        saveData.profiles.Count - 1
                    ].id
                    : string.Empty;
        }

        Save();
        return true;
    }

    public void MarkLetterCompleted(int letterIndex, int totalLetters)
    {
        PlayerProfileData profile = SelectedProfile;

        if (profile == null ||
            totalLetters <= 0 ||
            letterIndex < 0 ||
            letterIndex >= totalLetters)
        {
            return;
        }

        List<int> completed = new List<int>(
            profile.completedLetterIndices ?? new int[0]
        );

        if (!completed.Contains(letterIndex))
        {
            completed.Add(letterIndex);
            completed.Sort();
        }

        profile.completedLetterIndices = completed.ToArray();

        const int lettersPerIsland = 5;

        int currentIslandLastLetterIndex = Mathf.Min(
            (letterIndex / lettersPerIsland + 1) * lettersPerIsland - 1,
            totalLetters - 1
        );

        int nextLetterIndex = Mathf.Min(
            letterIndex + 1,
            currentIslandLastLetterIndex
        );

        int previousUnlockedIndex = profile.highestUnlockedLetterIndex;

        profile.highestUnlockedLetterIndex = Mathf.Max(
            profile.highestUnlockedLetterIndex,
            nextLetterIndex
        );

        Save();

        if (profile.highestUnlockedLetterIndex > previousUnlockedIndex)
        {
            PlayerPrefs.SetInt(
                "PendingLetterUnlock_" + profile.id,
                profile.highestUnlockedLetterIndex
            );

            PlayerPrefs.Save();
        }
    }

    public bool TryPurchaseNextIsland(int islandIndex, int price)
    {
        const int lettersPerIsland = 5;
        const int totalIslands = 8;

        PlayerProfileData profile = SelectedProfile;

        if (profile == null ||
            islandIndex < 0 ||
            islandIndex >= totalIslands - 1 ||
            price <= 0)
        {
            return false;
        }

        int firstLetterIndex = islandIndex * lettersPerIsland;
        int nextIslandFirstLetterIndex =
            (islandIndex + 1) * lettersPerIsland;

        // Already unlocked: never charge again.
        if (profile.highestUnlockedLetterIndex >=
            nextIslandFirstLetterIndex)
        {
            return false;
        }

        // All five signs must be completed before purchasing.
        for (int i = 0; i < lettersPerIsland; i++)
        {
            if (Array.IndexOf(
                    profile.completedLetterIndices ?? new int[0],
                    firstLetterIndex + i) < 0)
            {
                return false;
            }
        }

        if (profile.coins < price)
            return false;

        int previousCoins = profile.coins;
        int previousUnlockedIndex =
            profile.highestUnlockedLetterIndex;

        profile.coins -= price;
        profile.highestUnlockedLetterIndex =
            nextIslandFirstLetterIndex;

        try
        {
            Save();
        }
        catch (Exception exception)
        {
            profile.coins = previousCoins;
            profile.highestUnlockedLetterIndex =
                previousUnlockedIndex;

            Debug.LogException(exception);
            return false;
        }

        PlayerPrefs.SetInt(
            "PendingLetterUnlock_" + profile.id,
            nextIslandFirstLetterIndex
        );
        PlayerPrefs.Save();

        CoinsChanged?.Invoke(profile.coins);
        return true;
    }

    public void ChangeCoins(int amount)
    {
        PlayerProfileData profile = SelectedProfile;

        if (profile == null)
            return;

        profile.coins = Mathf.Max(
            0,
            profile.coins + amount
        );

        Save();
        CoinsChanged?.Invoke(profile.coins);
    }

    private PlayerProfileData FindProfile(string profileId)
    {
        if (string.IsNullOrEmpty(profileId))
            return null;

        return saveData.profiles.Find(
            profile => profile.id == profileId
        );
    }

    private void Save()
    {
        string json = JsonUtility.ToJson(
            saveData,
            true
        );

        File.WriteAllText(SavePath, json);
    }

    private void Load()
    {
        if (!File.Exists(SavePath))
            return;

        string json = File.ReadAllText(SavePath);

        PlayerProfileSaveData loadedData =
            JsonUtility.FromJson<PlayerProfileSaveData>(
                json
            );

        if (loadedData != null)
            saveData = loadedData;

        if (saveData.profiles == null)
        {
            saveData.profiles =
                new List<PlayerProfileData>();
        }
    }

    [ContextMenu("Clear All Profiles For Testing")]
    private void ClearAllProfilesForTesting()
    {
        saveData = new PlayerProfileSaveData();
        Save();

        Debug.Log(
            "All test profiles were deleted from: " +
            Application.persistentDataPath
        );
    }

    public bool TryPurchaseFinalShovel(int price)
    {
        PlayerProfileData profile = SelectedProfile;

        if (profile == null ||
            profile.hasPurchasedFinalShovel ||
            price <= 0 ||
            profile.coins < price ||
            profile.highestUnlockedLetterIndex < 35)
        {
            return false;
        }

        // All five signs of island eight must be completed.
        for (int i = 35; i < 40; i++)
        {
            if (Array.IndexOf(
                    profile.completedLetterIndices ?? new int[0],
                    i) < 0)
            {
                return false;
            }
        }

        int previousCoins = profile.coins;

        profile.coins -= price;
        profile.hasPurchasedFinalShovel = true;

        try
        {
            Save();
        }
        catch (Exception exception)
        {
            profile.coins = previousCoins;
            profile.hasPurchasedFinalShovel = false;

            Debug.LogException(exception);
            return false;
        }

        CoinsChanged?.Invoke(profile.coins);
        return true;
    }

#if UNITY_EDITOR
    [ContextMenu("Testing/Add Coins Up To 350")]
    private void AddTestCoins()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("Enter Play mode first.");
            return;
        }

        PlayerProfileData profile = SelectedProfile;

        if (profile == null)
            return;

        ChangeCoins(Mathf.Max(0, 350 - profile.coins));
    }
#endif
}