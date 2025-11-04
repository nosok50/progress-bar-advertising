using System;
using UnityEngine;

/// <summary>
/// Runtime representation of a concretized reward slot (chosen from RewardData).
/// Includes small helpers to serialize minimal identifiers for saving/loading.
/// </summary>
[Serializable]
public class GeneratedReward
{
    public RewardData baseReward;
    public Currencies specificResource;
    public int amount;
    public Sprite icon;
    public bool isSuperReward;

    public GeneratedRewardSerializable ToSerializable()
    {
        return new GeneratedRewardSerializable
        {
            baseRewardId = baseReward?.id,
            specificResourceId = specificResource?.valueNameKey,
            amount = amount,
            isSuperReward = isSuperReward
        };
    }

    public static GeneratedReward FromSerializable(GeneratedRewardSerializable ser, RewardPoolManager pool)
    {
        var gen = new GeneratedReward
        {
            amount = ser.amount,
            isSuperReward = ser.isSuperReward
        };

        if (!string.IsNullOrEmpty(ser.baseRewardId) && pool != null)
            gen.baseReward = pool.allRewards.Find(r => r.id == ser.baseRewardId);

        if (!string.IsNullOrEmpty(ser.specificResourceId) && pool != null)
            gen.specificResource = pool.FindCurrencyById(ser.specificResourceId);

        // choose icon precedence: specificResource -> baseReward -> null
        gen.icon = gen.specificResource?.valueIcon ?? gen.baseReward?.icon;

        return gen;
    }
}

[Serializable]
public class GeneratedRewardSerializable
{
    public string baseRewardId;
    public string specificResourceId;
    public int amount;
    public bool isSuperReward;
}
