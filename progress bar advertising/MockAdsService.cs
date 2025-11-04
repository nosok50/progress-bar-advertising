using System;
using UnityEngine;

/// <summary>
/// Very small mock ads service that immediately signals readiness and simulates
/// a reward flow. Useful for demos and editor previews.
/// </summary>
public class MockAdsService : IAdsService
{
    public bool IsRewardedReady { get; private set; } = true;

    public void Initialize()
    {
        Debug.Log("[MockAdsService] Initialized and ready.");
        IsRewardedReady = true;
    }

    public void RequestRewardedAd()
    {
        Debug.Log("[MockAdsService] RequestRewardedAd called. Simulating immediate load.");
        IsRewardedReady = true;
        AdsEvents.Instance?.InvokeAdLoaded();
    }

    public bool ShowRewardedAd()
    {
        if (!IsRewardedReady) return false;
        Debug.Log("[MockAdsService] ShowRewardedAd -> simulating successful watch.");
        // Simulate award and dismissal:
        AdsEvents.Instance?.InvokeRewarded(true);
        AdsEvents.Instance?.InvokeAdDismissed();
        return true;
    }
}