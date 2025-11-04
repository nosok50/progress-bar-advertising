using System;
using UnityEngine;

/// <summary>
/// Lightweight hooks to connect this demo ad-progress system
/// with the project's real systems (UI, economy, placement, messages).
/// By default these hooks only log actions; in a real project you
/// should assign them to your managers (e.g. ResourceMenu.Open(...)).
/// </summary>
public static class ExternalHooks
{
    /// <summary>Called when a resource reward should be shown to the player.</summary>
    public static Action<Currencies, int> OnOpenResourceReward = (res, amount) =>
    {
        Debug.Log($"[ExternalHooks] Open resource reward: {res?.valueNameKey ?? "null"} x{amount}");
    };

    /// <summary>Called when coins should be added to player state (UI/animation handled externally).</summary>
    public static Action<int> OnAddCoins = amount =>
    {
        Debug.Log($"[ExternalHooks] Add coins: {amount}");
    };

    /// <summary>Called when crystals (premium currency) should be added to player state.</summary>
    public static Action<int> OnAddCrystals = amount =>
    {
        Debug.Log($"[ExternalHooks] Add crystals: {amount}");
    };

    /// <summary>Called when we want to start placing a building prefab.</summary>
    public static Action<GameObject> OnStartPlacingBuilding = prefab =>
    {
        Debug.Log($"[ExternalHooks] Start placing building: {prefab?.name ?? "null"}");
    };

    /// <summary>Generic project message (e.g. UI toast). MessageType is simple enum defined below.</summary>
    public static Action<string, MessageType> OnShowMessage = (text, type) =>
    {
        Debug.Log($"[ExternalHooks] Message ({type}): {text}");
    };
}

/// <summary>Simple message type used in hooks - swap for your real enum if needed.</summary>
public enum MessageType
{
    Info,
    Success,
    Warning,
    Danger
}
