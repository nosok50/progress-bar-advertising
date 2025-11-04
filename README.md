# Unity Ad Progress Bar System  

<img width="1443" height="471" alt="Frame 26085815" src="https://github.com/user-attachments/assets/264467ec-39d5-4b9e-b73e-0cd5ee846c44" />

## Overview  
This is an implementation of a progress bar for watching rewarded videos.  
The player receives a sequence of rewards. To claim the most valuable reward at the end, all previous videos must be watched in order.  
The system is already used in the game **Eco City**.  
Store link: [Google Play](https://play.google.com/store/apps/details?id=com.nosok.EcoCityInc&pcampaignid=web_share)

## Description  
The system generates a set of rewards for the progress bar, displays them as buttons, manages their states *(available, claimed, locked)*, and handles ad playback through the `IAdsService` abstraction.  
After a successful ad view, the reward is granted and a collection animation is played.  
When all rewards in the set are obtained, a cooldown period begins before the next set becomes available.

---

## Key Features  
- Progressive reward system where the final slot is unique and becomes available only after watching all previous videos  
- Reward generation based on rarity and weighted values (`spawnWeight`)  
- Simple integration with any SDK via the `IAdsService` interface and the `AdsEvents` event hub  
- Mock mode for local testing without a real Ads SDK  
- Inspector configuration for timers and cooldown durations  
- Progress bar state persistence in `ProgressBarSaveData`  

---

## Core Scripts  
| Script | Description |
|--------|--------------|
| **RewardPoolManager** (`ScriptableObject`) | Generates the `GeneratedReward` array |
| **ProgressBarController** | Manages UI logic, timers, and button animations |
| **RewardButton** | Component for each progress bar cell, includes collection animation |
| **RewardGrantManager** | Converts `GeneratedReward` into real in-game actions via `ExternalHooks` |
| **AdManager** | High-level ad controller that uses `IAdsService` and `AdsEvents` |
| **MockAdsService** | Convenient for local functionality testing |

<img width="1443" height="471" alt="Frame 26085816" src="https://github.com/user-attachments/assets/c14691bb-4a1c-4588-8120-d42bdeb08a90" />

---

## Quick Integration  

1. Copy the script folder into your Unity project.  
2. Create a `RewardPoolManager` asset through *Create → Asset* and fill in the `allRewards` and `allCurrencies` lists.  
3. Prepare a UI prefab with buttons. Attach a `RewardButton` component to each one, set the `flyEndpoint`, and assign the required image references.  
4. Place a `ProgressBarController` in the scene and link:  
   - `rewardButtons`  
   - `nodeImages`  
   - `chainImages`  
   - `endpoint`  
   - `rewardsWindow` and `cooldownWindow`  
5. Add `AdManager` and `AdsEvents` to the scene. By default, the editor connects `MockAdsService`, allowing testing without an SDK.  
6. Open the **DemoScene** and press **Play** to check the behavior.  

---

## Connecting a Real Ads SDK  

- Implement the `IAdsService` adapter for your SDK.  
- Add the define symbol `USE_ADMOB` or `USE_YANDEX` in your build settings so that `AdManager` automatically selects the appropriate implementation.  
- The adapter should call:  
  - `AdsEvents.Instance.InvokeAdLoaded`  
  - `InvokeRewarded`  
  - `InvokeAdDismissed`  
  - `InvokeAdFailedToLoad`  
  according to the ad lifecycle.  

---

## External Integration Setup  

- The example includes a class `ExternalHooks` with actions:  
  - `OnOpenResourceReward`  
  - `OnAddCoins`  
  - `OnAddCrystals`  
  - `OnStartPlacingBuilding`  
  - `OnShowMessage`  
- Connect these hooks to your UI and economy managers for smooth integration with your project.  

<img width="1443" height="471" alt="Frame 26085817" src="https://github.com/user-attachments/assets/1d604f5e-933f-4aec-8800-e7c653b28439" />

---

## Testing  

- For local testing, use `MockAdsService`. It instantly marks ads as ready and simulates a successful view.  
- Check the following scenarios:  
  1. Open the progress bar when a reward set is available  
  2. Click an available button, watch the ad, and receive a reward  
  3. Try clicking a locked button to ensure it does not start ad playback  
  4. Collect all rewards and verify that the cooldown starts and the window switches  

---

## Saving State  

- `ProgressBarController` provides methods `GetSaveData` and `LoadFromSaveData` to store the `collected` array and generated rewards.  
- `GeneratedReward` is serialized through the compact structure `GeneratedRewardSerializable` using identifiers.  

---

## Customization  

- Adjust rarity weights and `spawnWeight` in `RewardData` to control probability.  
- Tune `rewardDisplayDurationSeconds` and `nextProgressBarCooldownSeconds` to configure activity and cooldown cycles.  
- Button animations are implemented using **DOTween Sequences**. Replace them with **Animator** or another plugin if needed.  

<img width="1443" height="471" alt="Frame 26085818" src="https://github.com/user-attachments/assets/d35ecb29-693a-48f4-b724-45c0edc188fc" />

---

## Contact  

If you need build instructions or a short demo scene, let me know and I will prepare text for the **README** or inspector examples.
```
