using System;
using System.Collections;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Main controller for the ad-progress UI. Generates rewards, handles timers,
/// button states, wave animations and integrates with AdManager and RewardGrantManager.
/// </summary>
public class ProgressBarController : MonoBehaviour
{
    public static ProgressBarController Instance { get; private set; }

    [Header("References")]
    public RewardPoolManager rewardPool;
    public Transform endpoint;
    public RewardButton[] rewardButtons;
    public Image[] nodeImages;
    public Image[] chainImages;

    [Header("UI Windows")]
    public GameObject rewardsWindow;
    public GameObject cooldownWindow;
    public TextMeshProUGUI rewardsTimerText;
    public TextMeshProUGUI cooldownTimerText;

    [Header("Colors")]
    public Color nodeLockedColor = Color.gray;
    public Color nodeUnlockedColor = Color.white;
    public Color chainLockedColor = Color.gray;
    public Color chainFilledColor = Color.white;

    [Header("Settings")]
    public float rewardDisplayDurationSeconds = 60f;
    public float nextProgressBarCooldownSeconds = 30f;
    public bool autoStart = false;

    [SerializeField] private float scaleUp = 1.1f;
    [SerializeField] private float singleAnimDuration = 0.4f;
    [SerializeField] private float delayBetween = 0.15f;
    [SerializeField] private float pauseBetweenWaves = 10f;

    private GeneratedReward[] currentRewards;
    private bool[] collected;
    private long rewardDisplayEndUnix = 0;
    private long nextProgressBarAvailableUnix = 0;
    private Coroutine timerRoutine;
    private Sequence waveSequence;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        if (autoStart) OpenMenu();
    }

    public void OpenMenu()
    {
        StopTimerUpdate();

        long now = TimeSerialization.NowUnixSeconds();

        if (now < nextProgressBarAvailableUnix)
        {
            ShowCooldownWindow(nextProgressBarAvailableUnix - now);
            return;
        }

        if (currentRewards == null || now >= rewardDisplayEndUnix)
            InitializeNewProgressBar();

        ShowRewardsWindow(rewardDisplayEndUnix - now);
    }

    public void CloseMenu()
    {
        StopTimerUpdate();
        StopWaveAnimation();
        rewardsWindow?.SetActive(false);
        cooldownWindow?.SetActive(false);
    }

    private void InitializeNewProgressBar()
    {
        int count = rewardButtons?.Length ?? 0;
        if (count == 0)
        {
            Debug.LogWarning("[ProgressBarController] No reward buttons assigned.");
            return;
        }

        currentRewards = rewardPool.GenerateProgressBarRewards(count);
        collected = new bool[count];

        rewardDisplayEndUnix = TimeSerialization.ToUnixSeconds(DateTime.UtcNow.AddSeconds(rewardDisplayDurationSeconds));

        RefreshButtons();
        UpdateProgressVisual();
    }

    private void ShowRewardsWindow(long secondsRemaining)
    {
        rewardsWindow?.SetActive(true);
        cooldownWindow?.SetActive(false);

        rewardsWindow.transform.DOScale(1.05f, singleAnimDuration * 0.5f).SetEase(Ease.OutSine)
            .OnComplete(() => rewardsWindow.transform.DOScale(1f, singleAnimDuration * 0.5f).SetEase(Ease.InSine));

        RefreshButtons();
        UpdateProgressVisual();

        StartWaveAnimation();
        StartTimerUpdate(secondsRemaining, rewardsTimerText, OnRewardTimerEnd);
    }

    private void OnRewardTimerEnd()
    {
        InitializeNewProgressBar();
        ShowRewardsWindow((long)rewardDisplayDurationSeconds);
    }

    private void ShowCooldownWindow(long secondsRemaining)
    {
        rewardsWindow?.SetActive(false);
        cooldownWindow?.SetActive(true);

        cooldownWindow.transform.DOScale(1.05f, singleAnimDuration * 0.5f).SetEase(Ease.OutSine)
            .OnComplete(() => cooldownWindow.transform.DOScale(1f, singleAnimDuration * 0.5f).SetEase(Ease.InSine));

        StopWaveAnimation();
        StartTimerUpdate(secondsRemaining, cooldownTimerText, OnCooldownEnd);
    }

    private void OnCooldownEnd()
    {
        InitializeNewProgressBar();
        ShowRewardsWindow((long)rewardDisplayDurationSeconds);
    }

    public void StartWaveAnimation()
    {
        if (rewardButtons == null || rewardButtons.Length == 0) return;
        StopWaveAnimation();

        waveSequence = DOTween.Sequence();
        for (int i = 0; i < rewardButtons.Length; i++)
        {
            var btn = rewardButtons[i];
            if (btn == null) continue;

            Vector3 original = Vector3.one;
            Vector3 target = original * scaleUp;

            var pulse = DOTween.Sequence()
                .Append(btn.transform.DOScale(target, singleAnimDuration * 0.5f).SetEase(Ease.OutSine))
                .Append(btn.transform.DOScale(original, singleAnimDuration * 0.5f).SetEase(Ease.InSine))
                .SetDelay(i * delayBetween);

            waveSequence.Join(pulse);
        }

        waveSequence.AppendInterval(pauseBetweenWaves);
        waveSequence.SetLoops(-1, LoopType.Restart);
        waveSequence.Play();
    }

    public void StopWaveAnimation()
    {
        if (waveSequence != null && waveSequence.IsActive())
        {
            waveSequence.Kill();
            waveSequence = null;
        }

        if (rewardButtons == null) return;
        foreach (var btn in rewardButtons)
        {
            if (btn != null)
            {
                btn.transform.DOKill();
                btn.transform.localScale = Vector3.one;
            }
        }
    }

    private void RefreshButtons()
    {
        if (rewardButtons == null || currentRewards == null) return;

        for (int i = 0; i < rewardButtons.Length; i++)
        {
            var btn = rewardButtons[i];
            if (btn == null) continue;

            btn.Initialize(currentRewards[i], i);
            btn.flyEndpoint = endpoint;
            btn.onWatchClicked = OnWatchClicked;

            if (collected != null && i < collected.Length && collected[i])
                btn.SetState(ButtonState.Collected);
            else if (i == FirstNotCollectedIndex())
                btn.SetState(ButtonState.Available);
            else
                btn.SetState(ButtonState.Locked);
        }
    }

    private int FirstNotCollectedIndex()
    {
        if (collected == null) return -1;
        for (int i = 0; i < collected.Length; i++)
            if (!collected[i]) return i;
        return -1;
    }

    private bool AllCollected()
    {
        if (collected == null) return false;
        foreach (var c in collected) if (!c) return false;
        return true;
    }

    private void OnWatchClicked(RewardButton b)
    {
        if (b == null) return;
        b.loadIcon.SetActive(true);

        bool started = AdManager.instance.ShowRewardedAd((rewarded) =>
        {
            b.loadIcon.SetActive(false);
            if (rewarded)
            {
                GrantRewardAndAnimate(b);
            }
            else
            {
                ExternalHooks.OnShowMessage?.Invoke("Video not available", MessageType.Danger);
            }
        });

        if (!started)
        {
            b.loadIcon.SetActive(false);
            ExternalHooks.OnShowMessage?.Invoke("Video not available", MessageType.Danger);
        }
    }

    private void GrantRewardAndAnimate(RewardButton b)
    {
        int idx = b.indexInBar;
        if (collected == null || idx < 0 || idx >= collected.Length) return;
        if (collected[idx]) return;

        b.PlayCollectAnimation(() =>
        {
            collected[idx] = true;
            UpdateProgressVisual();
            RefreshButtons();

            RewardGrantManager.Instance.GrantReward(currentRewards[idx]);

            if (AllCollected())
            {
                StopTimerUpdate();
                nextProgressBarAvailableUnix = TimeSerialization.ToUnixSeconds(DateTime.UtcNow.AddSeconds(nextProgressBarCooldownSeconds));
                ShowCooldownWindow((long)nextProgressBarCooldownSeconds);
            }
        });
    }

    private void UpdateProgressVisual()
    {
        if (nodeImages != null)
        {
            for (int i = 0; i < nodeImages.Length; i++)
            {
                bool active = (collected != null && i < collected.Length && collected[i]);
                nodeImages[i].color = active ? nodeUnlockedColor : nodeLockedColor;
            }
        }

        if (chainImages != null)
        {
            for (int i = 0; i < chainImages.Length; i++)
            {
                bool filled = (collected != null && i < collected.Length - 1 && collected[i]);
                chainImages[i].color = filled ? chainFilledColor : chainLockedColor;
            }
        }
    }

    private IEnumerator TimerRoutine(long secondsRemaining, TMP_Text targetText, Action onEnd)
    {
        while (secondsRemaining > 0)
        {
            var t = TimeSpan.FromSeconds(secondsRemaining);
            if (targetText != null) targetText.text = $"{t.Minutes:D2}:{t.Seconds:D2}";
            yield return new WaitForSeconds(1f);
            secondsRemaining--;
        }

        if (targetText != null) targetText.text = "";
        onEnd?.Invoke();
    }

    public void StartTimerUpdate(long secondsRemaining, TMP_Text targetText, Action onEnd)
    {
        StopTimerUpdate();
        if (!gameObject.activeInHierarchy) return;
        timerRoutine = StartCoroutine(TimerRoutine(secondsRemaining, targetText, onEnd));
    }

    public void StopTimerUpdate()
    {
        if (timerRoutine != null) StopCoroutine(timerRoutine);
        timerRoutine = null;
    }

    // ===================== Save/Load helpers =====================
    public ProgressBarSaveData GetSaveData()
    {
        if (collected == null || currentRewards == null) return null;
        var sd = new ProgressBarSaveData
        {
            collected = collected,
            generatedRewards = new GeneratedRewardSerializable[currentRewards.Length],
            nextProgressBarAvailableUnix = nextProgressBarAvailableUnix,
            rewardDisplayEndUnix = rewardDisplayEndUnix
        };

        for (int i = 0; i < currentRewards.Length; i++)
            sd.generatedRewards[i] = currentRewards[i].ToSerializable();

        return sd;
    }

    public void LoadFromSaveData(ProgressBarSaveData sd)
    {
        if (sd == null) return;
        nextProgressBarAvailableUnix = sd.nextProgressBarAvailableUnix;
        rewardDisplayEndUnix = sd.rewardDisplayEndUnix;
        collected = sd.collected ?? new bool[rewardButtons.Length];
        currentRewards = new GeneratedReward[rewardButtons.Length];

        for (int i = 0; i < sd.generatedRewards.Length && i < currentRewards.Length; i++)
            currentRewards[i] = GeneratedReward.FromSerializable(sd.generatedRewards[i], rewardPool);
    }
}
/// <summary>
/// Utility helpers to convert DateTime to unix seconds and back.
/// Kept as small static helper for saves and comparisons.
/// </summary>
public static class TimeSerialization
{
    public static long ToUnixSeconds(DateTime dt) => new DateTimeOffset(dt.ToUniversalTime()).ToUnixTimeSeconds();
    public static DateTime FromUnixSeconds(long unixSeconds) => DateTimeOffset.FromUnixTimeSeconds(unixSeconds).UtcDateTime;
    public static long NowUnixSeconds() => ToUnixSeconds(DateTime.UtcNow);
}