using UnityEngine;

/// <summary>
/// Scriptable object describing a reward slot template.
/// Extend as needed in your project.
/// </summary>
[CreateAssetMenu(menuName = "Shop/RewardData")]
public class RewardData : ScriptableObject
{
    [Header("Identity")]
    public string id;
    public string displayNameKey;

    [Header("Classification")]
    public RewardType type = RewardType.Other;
    public RewardRarity rarity = RewardRarity.Common;

    [Header("Spawn / balancing")]
    [Tooltip("Local spawn weight in addition to rarity weight.")]
    public int spawnWeight = 100;

    [Header("Visuals")]
    public Sprite icon;

    [Header("Amounts")]
    [Tooltip("Fixed amount used for coin/crystal/other types.")]
    public int fixedAmount;

    [Header("Resource")]
    public float resourceCoefficient = 1f;

    [Header("Building")]
    public GameObject buildingPrefab;

    [Header("Meta")]
    public bool isSuperReward = false;
}
