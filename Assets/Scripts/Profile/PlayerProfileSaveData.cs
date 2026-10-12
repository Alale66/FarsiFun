using System;
using System.Collections.Generic;

[Serializable]
public class PlayerProfileSaveData
{
    public List<PlayerProfileData> profiles =
        new List<PlayerProfileData>();

    public string selectedProfileId;
}