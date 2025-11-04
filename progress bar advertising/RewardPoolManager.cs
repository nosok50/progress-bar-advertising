using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Responsible for generating a sequence of rewards for the progress bar.
/// This ScriptableObject demonstrates weighted picks, rarity weights,
/// simple cooldowns for recently used items, and forcing a super reward last.
/// </summary>
[CreateAssetMenu(menuName = "Shop/RewardPool")]
public class RewardPoolManager : ScriptableObject
{
    [Header("Data")]
    public List<RewardData> allRewards = new List<RewardData>();
    public List<Recipe> lvlRecipe = new List<Recipe>(); // optional project-specific recipes
    public List<Currencies> allCurrencies = new List<Currencies>();

    [Header("Rarity weights")]
    public int weightCommon = 60;
    public int weightRare = 25;
    public int weightEpic = 10;
    public int weightLegendary = 5;

    [Header("Diversity control")]
    public int maxConsecutiveSameType = 2;
    public int recentItemPenaltySteps = 3;
    [Range(0.0f, 1.0f)] public float recentItemPenaltyMultiplier = 0.2f;
    [Range(0.0f, 1.0f)] public float typeRepeatPenaltyMultiplier = 0.5f;

    /// <summary>
    /// Generate an array of rewards for a progress bar. If forceSuperLast is true,
    /// last slot will preferentially be a super reward (if found).
    /// </summary>
    public GeneratedReward[] GenerateProgressBarRewards(int totalCount, bool forceSuperLast = true)
    {
        if (totalCount <= 0) return Array.Empty<GeneratedReward>();
        var result = new GeneratedReward[totalCount];
        var slotTypes = GenerateSlotTypeSequence(totalCount, forceSuperLast);

        var recentItemUses = new Dictionary<string, int>(); // id -> penalty steps remaining
        var recentTypeUses = new Dictionary<RewardType, int>(); // type -> consecutive uses

        for (int i = 0; i < totalCount; i++)
        {
            if (i == totalCount - 1 && forceSuperLast)
            {
                var supers = allRewards.Where(r => r.isSuperReward).ToList();
                if (supers.Count > 0)
                {
                    var chosen = WeightedRandomPickFromList(supers, recentItemUses, recentTypeUses);
                    result[i] = GenerateSpecificReward(chosen);
                    DecrementPenaltiesAfterPick(chosen, recentItemUses, recentTypeUses);
                    continue;
                }
            }

            var desiredType = slotTypes[i];
            var candidates = allRewards.Where(r => r.type == desiredType && !r.isSuperReward).ToList();
            if (candidates.Count == 0) candidates = new List<RewardData>(allRewards);

            var pick = WeightedRandomPickFromList(candidates, recentItemUses, recentTypeUses);
            result[i] = GenerateSpecificReward(pick);
            DecrementPenaltiesAfterPick(pick, recentItemUses, recentTypeUses);
        }

        return result;
    }

    private List<RewardType> GenerateSlotTypeSequence(int totalCount, bool forceSuperLast)
    {
        var result = new List<RewardType>(totalCount);
        var typeBaseWeights = new Dictionary<RewardType, float>()
        {
            { RewardType.Resource, 25f },
            { RewardType.Crystal, 25f },
            { RewardType.Coin, 25f },
            { RewardType.Building, 25f },
            { RewardType.Other, 1f }
        };

        for (int i = 0; i < totalCount; i++)
        {
            if (i == totalCount - 1 && forceSuperLast)
            {
                var super = allRewards.FirstOrDefault(r => r.isSuperReward);
                if (super != null)
                {
                    result.Add(super.type);
                    continue;
                }
            }

            var weights = new Dictionary<RewardType, float>(typeBaseWeights);

            if (result.Count >= maxConsecutiveSameType)
            {
                var lastType = result.Last();
                bool lastAreSame = true;
                for (int k = 1; k <= maxConsecutiveSameType; k++)
                {
                    if (result[result.Count - k] != lastType) { lastAreSame = false; break; }
                }
                if (lastAreSame)
                    weights[lastType] *= 0.01f;
            }

            float total = weights.Values.Sum();
            float roll = UnityEngine.Random.value * total;
            float acc = 0f;
            RewardType chosen = RewardType.Resource;
            foreach (var kv in weights)
            {
                acc += kv.Value;
                if (roll <= acc) { chosen = kv.Key; break; }
            }
            result.Add(chosen);
        }

        return result;
    }

    private RewardData WeightedRandomPickFromList(List<RewardData> list, Dictionary<string, int> recentItemUses, Dictionary<RewardType, int> recentTypeUses)
    {
        float total = 0f;
        var weights = new Dictionary<RewardData, float>(list.Count);
        foreach (var r in list)
        {
            float w = GetWeight(r.rarity);
            w *= r.spawnWeight;

            if (recentItemUses.TryGetValue(r.id, out int penalty) && penalty > 0)
                w *= recentItemPenaltyMultiplier;

            if (recentTypeUses.TryGetValue(r.type, out int tcount) && tcount > 0)
                w *= Mathf.Pow(typeRepeatPenaltyMultiplier, tcount);

            w = Mathf.Max(0.0001f, w);
            weights[r] = w;
            total += w;
        }

        if (total <= 0f) return list[0];

        float roll = UnityEngine.Random.value * total;
        float acc = 0f;
        foreach (var kv in weights)
        {
            acc += kv.Value;
            if (roll <= acc) return kv.Key;
        }
        return list[0];
    }

    private void DecrementPenaltiesAfterPick(RewardData picked, Dictionary<string, int> recentItemUses, Dictionary<RewardType, int> recentTypeUses)
    {
        var keys = recentItemUses.Keys.ToList();
        foreach (var k in keys) recentItemUses[k] = Mathf.Max(0, recentItemUses[k] - 1);
        var tkeys = recentTypeUses.Keys.ToList();
        foreach (var k in tkeys) recentTypeUses[k] = Mathf.Max(0, recentTypeUses[k] - 1);

        if (picked != null)
            recentItemUses[picked.id] = recentItemPenaltySteps;

        if (!recentTypeUses.ContainsKey(picked.type)) recentTypeUses[picked.type] = 0;
        recentTypeUses[picked.type] += 1;
    }

    private GeneratedReward GenerateSpecificReward(RewardData baseReward)
    {
        var generated = new GeneratedReward
        {
            baseReward = baseReward,
            isSuperReward = baseReward?.isSuperReward ?? false
        };

        if (baseReward == null) return generated;

        switch (baseReward.type)
        {
            case RewardType.Resource:
                var res = DetermineResourceReward(baseReward.resourceCoefficient);
                generated.specificResource = res.resource;
                generated.amount = res.amount;
                generated.icon = res.resource?.valueIcon ?? baseReward.icon;
                break;

            case RewardType.Building:
                generated.amount = 0;
                generated.icon = baseReward.icon;
                break;

            case RewardType.Coin:
                int amount = (int)(LevelManager.instance?.GetCoinLimit() ?? 1000 * baseReward.resourceCoefficient);
                generated.amount = Mathf.RoundToInt(Mathf.Ceil(amount / 500f) * 500f);
                generated.icon = baseReward.icon;
                break;

            default:
                generated.amount = baseReward.fixedAmount;
                generated.icon = baseReward.icon;
                break;
        }

        return generated;
    }

    private (Currencies resource, int amount) DetermineResourceReward(float coefficient)
    {
        float chance = UnityEngine.Random.Range(0f, 1f);
        if (chance < 0.55f) return GetResourceFromProductionRecipes(coefficient);
        return GetResourceFromLevelRecipes(coefficient);
    }

    private (Currencies resource, int amount) GetResourceFromProductionRecipes(float coefficient)
    {
        var unlocked = LevelManager.instance?.unlockedAbilities;
        if (unlocked == null || unlocked.Count == 0) return GetDefaultResource(coefficient);

        var recipeAbilities = unlocked
            .Where(a => a is LevelAbilityForRecipe)
            .Cast<LevelAbilityForRecipe>()
            .Where(a => a.productionRecipe != null && a.productionRecipe.producedResource != null)
            .ToList();

        if (recipeAbilities.Count == 0) return GetDefaultResource(coefficient);

        var randomAbility = recipeAbilities[UnityEngine.Random.Range(0, recipeAbilities.Count)];
        var resource = randomAbility.productionRecipe.producedResource;
        int amount = Mathf.RoundToInt(resource.maxStackSize * coefficient);
        return (resource, amount);
    }

    private (Currencies resource, int amount) GetResourceFromLevelRecipes(float coefficient)
    {
        if (lvlRecipe == null || lvlRecipe.Count == 0) return GetDefaultResource(coefficient);
        int recipeIndex = Mathf.Clamp(PlayerStats.currentLevel + 1, 0, lvlRecipe.Count - 1);
        var recipe = lvlRecipe[recipeIndex];
        if (recipe?.requiredComponents == null || recipe.requiredComponents.Count == 0) return GetDefaultResource(coefficient);

        var randomComponent = recipe.requiredComponents[UnityEngine.Random.Range(0, recipe.requiredComponents.Count)];
        var resource = randomComponent?.component;
        if (resource == null) return GetDefaultResource(coefficient);

        int amount = Mathf.RoundToInt(resource.maxStackSize * coefficient);
        return (resource, amount);
    }

    private (Currencies resource, int amount) GetDefaultResource(float coefficient)
    {
        if (allCurrencies != null && allCurrencies.Count > 0)
        {
            var def = allCurrencies[0];
            int amount = Mathf.RoundToInt(def.maxStackSize * coefficient);
            return (def, amount);
        }

        Debug.LogWarning("[RewardPoolManager] No currencies defined; returning null resource.");
        return (null, 0);
    }

    private int GetWeight(RewardRarity r)
    {
        switch (r)
        {
            case RewardRarity.Common: return weightCommon;
            case RewardRarity.Rare: return weightRare;
            case RewardRarity.Epic: return weightEpic;
            case RewardRarity.Legendary: return weightLegendary;
            default: return 1;
        }
    }

    public Currencies FindCurrencyById(string id)
    {
        if (string.IsNullOrEmpty(id) || allCurrencies == null) return null;
        return allCurrencies.Find(c => c.valueNameKey == id);
    }
}
