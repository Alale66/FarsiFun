using System;

[Serializable]
public class PlayerProfileData
{
    public string id;
    public string displayName;
    public int avatarIndex;
    public int coins;
    public int highestUnlockedLetterIndex = 0;
    public int[] completedLetterIndices = new int[0];
    public string createdAt;

    public PlayerProfileData(
        string profileId,
        string playerName,
        int selectedAvatarIndex,
        int startingCoins)
    {
        id = profileId;
        displayName = playerName;
        avatarIndex = selectedAvatarIndex;
        coins = startingCoins;
        createdAt = DateTime.UtcNow.ToString("O");
    }
}