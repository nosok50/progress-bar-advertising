using System;
using UnityEngine;

/// <summary>
/// Lightweight event hub for ad lifecycle events.
/// Services should invoke appropriate methods when state changes.
/// </summary>
public class AdsEvents : MonoBehaviour
{
    public static AdsEvents Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null) { Instance = this; DontDestroyOnLoad(gameObject); }
        else Destroy(gameObject);
    }

    public event Action OnAdLoaded = delegate { };
    public event Action<string> OnAdFailedToLoad = delegate { };
    public event Action OnAdDismissed = delegate { };
    public event Action<bool> OnRewarded = delegate { };

    public void InvokeAdLoaded() => OnAdLoaded?.Invoke();
    public void InvokeAdFailedToLoad(string msg) => OnAdFailedToLoad?.Invoke(msg);
    public void InvokeAdDismissed() => OnAdDismissed?.Invoke();
    public void InvokeRewarded(bool success) => OnRewarded?.Invoke(success);
}