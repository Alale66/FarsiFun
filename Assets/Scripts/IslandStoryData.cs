using UnityEngine;
using UnityEngine.Video;

[CreateAssetMenu(
    fileName = "IslandStory",
    menuName = "FarsiFun/Island Story")]
public class IslandStoryData : ScriptableObject
{
    [Header("Island")]
    [Range(0, 7)]
    public int islandIndex;

    [Header("Purchase")]
    public string itemName;

    [Min(1)]
    public int price = 300;

    public Sprite itemSprite;

    [Header("Story Before Purchase")]
    public VideoClip introductionClip;

    [Header("Videos After Purchase — In Order")]
    public VideoClip[] afterPurchaseClips;
}