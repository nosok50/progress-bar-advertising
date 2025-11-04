using UnityEngine;

/// <summary>
/// Simple adapter interface for rewarded ad providers (AdMob, Yandex, Mock).
/// </summary>
public interface IAdsService
{
    void Initialize();
    void RequestRewardedAd();
    bool IsRewardedReady { get; }
    bool ShowRewardedAd();
}