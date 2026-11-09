using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Video;

public class IslandStoryController : MonoBehaviour
{
    [SerializeField] private IslandStoryData[] stories;
    [SerializeField] private IslandStoryData finalStory;

    private bool isFinalIsland;
    [SerializeField] private VideoClip notEnoughCoinsClip;
    [SerializeField] private StoryVideoPlayer storyVideoPlayer;
    [SerializeField] private IslandPurchasePanelUI purchasePanel;
    [SerializeField] private SceneNavigator navigator;

    private IslandStoryData currentStory;
    private string profileId;
    private bool running;

    public bool TryBegin(int islandIndex)
    {
        if (running)
            return true;

        PlayerProfileData profile = GetProfile();

        if (profile == null || islandIndex < 0 || islandIndex >= 8)
            return false;

        int nextIslandFirstLetter = (islandIndex + 1) * 5;

        if (islandIndex < 7 &&
            profile.highestUnlockedLetterIndex >= nextIslandFirstLetter)
            return false;

        // Check all five signs, including on replay.
        for (int i = 0; i < 5; i++)
        {
            if (Array.IndexOf(
                    profile.completedLetterIndices ?? new int[0],
                    islandIndex * 5 + i) < 0)
            {
                return false;
            }
        }

        isFinalIsland = islandIndex == 7;

        currentStory = isFinalIsland
            ? finalStory
            : FindStory(islandIndex);

        if (currentStory == null ||
            currentStory.price <= 0 ||
            storyVideoPlayer == null ||
            purchasePanel == null ||
            navigator == null)
        {
            Debug.LogError("Island story configuration is incomplete.");
            return false;
        }

        profileId = profile.id;
        running = true;

        purchasePanel.Hide();

        if (isFinalIsland && profile.hasPurchasedFinalShovel)
        {
            PlayAfterPurchase(0);
            return true;
        }

        bool finalShovelOwned =
     islandIndex == 7 &&
     profile.hasPurchasedFinalShovel;

        VideoClip clip =
            finalShovelOwned || profile.coins >= currentStory.price
                ? currentStory.introductionClip
                : notEnoughCoinsClip;

        PlayClip(clip, () =>
        {
            PlayerProfileData activeProfile = GetSameProfile();

            if (activeProfile == null)
            {
                ReturnToIsland();
                return;
            }

            if (isFinalIsland &&
                activeProfile.hasPurchasedFinalShovel)
            {
                PlayAfterPurchase(0);
            }
            else
            {
                ShowPurchase();
            }
        });

        return true;
    }

    private void ShowPurchase()
    {
        PlayerProfileData profile = GetSameProfile();

        if (profile == null)
        {
            ReturnToIsland();
            return;
        }

        purchasePanel.Show(
            currentStory,
            profile.coins,
            Purchase,
            ReturnToIsland
        );
    }

    private void Purchase()
    {
        PlayerProfileData profile = GetSameProfile();

        if (profile == null)
        {
            ReturnToIsland();
            return;
        }

        int startingCoins = profile.coins;

        // Hold the old number before the purchase changes it.
        purchasePanel.BeginPaymentDisplay(startingCoins);

        bool purchased =
     isFinalIsland
         ? PlayerProfileManager.Instance.TryPurchaseFinalShovel(
             currentStory.price
         )
         : PlayerProfileManager.Instance.TryPurchaseNextIsland(
             currentStory.islandIndex,
             currentStory.price
         );

        if (!purchased)
        {
            purchasePanel.EndPaymentDisplay();
            purchasePanel.Refresh(
                currentStory.price,
                profile.coins
            );

            return;
        }

        purchasePanel.PlayPaymentAnimation(
            startingCoins,
            profile.coins,
            () =>
            {
                purchasePanel.Hide();
                PlayAfterPurchase(0);
            }
        );
    }

    private void PlayAfterPurchase(int index)
    {
        if (GetSameProfile() == null)
        {
            ReturnToIsland();
            return;
        }

        VideoClip[] clips = currentStory.afterPurchaseClips;

        // Skip empty slots.
        while (clips != null &&
               index < clips.Length &&
               clips[index] == null)
        {
            index++;
        }

        if (clips == null || index >= clips.Length)
        {
            running = false;

            if (isFinalIsland)
            {
                SceneManager.LoadScene("WorldMap");
            }
            else
            {
                navigator.GoToIsland(currentStory.islandIndex + 1);
            }

            return;
        }

        int nextIndex = index + 1;

        PlayClip(
            clips[index],
            () => PlayAfterPurchase(nextIndex)
        );
    }

    private void PlayClip(VideoClip clip, Action next)
    {
        if (clip == null)
        {
            next?.Invoke();
            return;
        }

        storyVideoPlayer.Play(clip, succeeded =>
        {
            if (!succeeded)
                Debug.LogWarning("Story clip failed; continuing the flow.");

            next?.Invoke();
        });
    }

    private void ReturnToIsland()
    {
        purchasePanel.Hide();
        running = false;
        SceneManager.LoadScene("IslandView");
    }

    private PlayerProfileData GetSameProfile()
    {
        PlayerProfileData profile = GetProfile();

        return profile != null && profile.id == profileId
            ? profile
            : null;
    }

    private static PlayerProfileData GetProfile()
    {
        return PlayerProfileManager.Instance != null
            ? PlayerProfileManager.Instance.SelectedProfile
            : null;
    }

    private IslandStoryData FindStory(int islandIndex)
    {
        if (stories == null)
            return null;

        foreach (IslandStoryData story in stories)
        {
            if (story != null && story.islandIndex == islandIndex)
                return story;
        }

        return null;
    }
}