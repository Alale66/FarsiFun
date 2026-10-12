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
}