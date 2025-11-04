using System;
using UnityEngine;

/// <summary>
/// High-level manager that coordinates ad showing and returns results via callbacks.
/// If no real SDK is compiled in, a MockAdsService can be used.
/// </summary>
public class AdManager : MonoBehaviour
{
    public static AdManager instance;
    private IAdsService _adsService;
    private bool _isBusy = false;
    private Action<bool> _pendingCallback = null;
    private Action _onLoadedToShow = null;

    private void Awake()
    {
        if (instance == null) { instance = this; DontDestroyOnLoad(gameObject); }
        else Destroy(gameObject);
    }

    private void Start()
    {
#if USE_ADMOB
        _adsService = new AdMobAdsService();
#elif USE_YANDEX
        _adsService = new YandexAdsService();
#else
        _adsService = new MockAdsService(); // demo fallback - instantly ready
#endif
        _adsService?.Initialize();
    }

    public bool adReady => _adsService != null && _adsService.IsRewardedReady;

    public void RequestRewardedAd() => _adsService?.RequestRewardedAd();

    public bool ShowRewardedAd()
    {
        if (_adsService == null) return false;
        try { return _adsService.ShowRewardedAd(); }
        catch (Exception e) { Debug.LogError("AdManager.ShowRewardedAd exception: " + e); return false; }
    }

    public bool ShowRewardedAd(Action<bool> callback)
    {
        if (_adsService == null) return false;
        if (_isBusy) return false;

        _isBusy = true;
        _pendingCallback = callback ?? (_ => { });

        AdsEvents.Instance.OnRewarded += OnRewarded;
        AdsEvents.Instance.OnAdFailedToLoad += OnFailedToLoad;
        AdsEvents.Instance.OnAdDismissed += OnDismissed;

        if (adReady)
        {
            bool showResult = false;
            try { showResult = _adsService.ShowRewardedAd(); }
            catch (Exception ex) { Debug.LogError("AdManager: Show exception: " + ex); showResult = false; }

            if (!showResult)
            {
                _pendingCallback?.Invoke(false);
                CleanupAfterShow();
                return false;
            }
            return true;
        }
        else
        {
            _onLoadedToShow = () =>
            {
                AdsEvents.Instance.OnAdLoaded -= _onLoadedToShow;
                bool showResult = false;
                try { showResult = _adsService.ShowRewardedAd(); }
                catch (Exception ex) { Debug.LogError("AdManager: Show after load exception: " + ex); showResult = false; }

                if (!showResult)
                {
                    _pendingCallback?.Invoke(false);
                    CleanupAfterShow();
                }
            };

            AdsEvents.Instance.OnAdLoaded += _onLoadedToShow;
            RequestRewardedAd();
            return true;
        }
    }

    private void OnRewarded(bool rewarded)
    {
        _pendingCallback?.Invoke(rewarded);
        CleanupAfterShow();
    }

    private void OnFailedToLoad(string msg)
    {
        _pendingCallback?.Invoke(false);
        CleanupAfterShow();
    }

    private void OnDismissed()
    {
        _pendingCallback?.Invoke(false);
        CleanupAfterShow();
    }

    private void CleanupAfterShow()
    {
        _isBusy = false;
        _pendingCallback = null;

        if (AdsEvents.Instance != null)
        {
            AdsEvents.Instance.OnRewarded -= OnRewarded;
            AdsEvents.Instance.OnAdFailedToLoad -= OnFailedToLoad;
            AdsEvents.Instance.OnAdDismissed -= OnDismissed;
            if (_onLoadedToShow != null) AdsEvents.Instance.OnAdLoaded -= _onLoadedToShow;
        }

        _onLoadedToShow = null;
    }
}