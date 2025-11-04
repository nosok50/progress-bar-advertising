using UnityEngine;

/// <summary>
/// Centralized reward grant logic. For demo this class uses ExternalHooks
/// instead of direct references to in-project systems (UI managers, placement systems).
/// </summary>
public class RewardGrantManager : MonoBehaviour
{
    public static RewardGrantManager Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
    }

    /// <summary>
    /// Grant the provided generated reward. This method routes different types
    /// to external hooks so the demo remains decoupled and easy to connect.
    /// </summary>
    public void GrantReward(GeneratedReward r)
    {
        if (r == null || r.baseReward == null) return;

        switch (r.baseReward.type)
        {
            case RewardType.Resource:
                GrantResource(r);
                break;
            case RewardType.Coin:
                GrantCoins(r);
                break;
            case RewardType.Crystal:
                GrantCrystals(r);
                break;
            case RewardType.Building:
                GrantBuilding(r);
                break;
            case RewardType.Other:
            default:
                ExternalHooks.OnShowMessage?.Invoke("Special reward granted (demo).", MessageType.Info);
                break;
        }
    }

    private void GrantResource(GeneratedReward r)
    {
        if (r.specificResource != null)
        {
            Debug.Log($"GrantResource: {r.specificResource.valueNameKey} x{r.amount}");
            ExternalHooks.OnOpenResourceReward?.Invoke(r.specificResource, r.amount);
        }
    }

    private void GrantCoins(GeneratedReward r)
    {
        Debug.Log($"GrantCoins: {r.amount}");
        PlayerStats.coins += r.amount; // safe to keep as demo; replace if needed
        ExternalHooks.OnAddCoins?.Invoke(r.amount);
    }

    private void GrantCrystals(GeneratedReward r)
    {
        Debug.Log($"GrantCrystals: {r.amount}");
        CloudManager.instance?.AddCurrency(r.amount); // if present, keep behavior
        ExternalHooks.OnAddCrystals?.Invoke(r.amount);
    }

    private void GrantBuilding(GeneratedReward r)
    {
        Debug.Log($"GrantBuilding: prefab={r.baseReward.buildingPrefab?.name ?? "null"}");
        ExternalHooks.OnStartPlacingBuilding?.Invoke(r.baseReward.buildingPrefab);
    }
}
