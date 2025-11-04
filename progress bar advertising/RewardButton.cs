using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UI button representing a slot in the ad progress bar.
/// Kept generic so it can be reused in different UIs.
/// </summary>
public class RewardButton : MonoBehaviour
{
    [Header("UI")]
    public Image iconImage;
    public Image blackPlaceImage;
    public GameObject lockIcon;
    public TextMeshProUGUI watchText;
    public TextMeshProUGUI amountText;
    public GameObject noneBg;
    public GameObject glowBg;
    public GameObject superGlowBg;
    public GameObject loadIcon;

    [Header("Animation")]
    public Transform flyEndpoint;
    public float flyDuration = 0.8f;
    public float jumpDuration = 0.4f;
    public float jumpPower = 20f;

    [HideInInspector] public GeneratedReward generatedReward;
    public int indexInBar;
    private ButtonState _currentState = ButtonState.Locked;
    private Button _uiButton;

    /// <summary>Callback triggered when user clicks "watch". Provides this button as argument.</summary>
    public Action<RewardButton> onWatchClicked;

    private void Awake()
    {
        _uiButton = GetComponent<Button>();
    }

    /// <summary>Initialize visuals for given GeneratedReward.</summary>
    public void Initialize(GeneratedReward reward, int index)
    {
        generatedReward = reward;
        indexInBar = index;
        loadIcon.SetActive(false);

        iconImage.sprite = reward?.icon;
        UpdateAmountText();
        UpdateVisuals();
    }

    private void UpdateAmountText()
    {
        if (amountText == null || generatedReward == null)
            return;

        if (generatedReward.baseReward?.type == RewardType.Building)
        {
            amountText.text = ""; // building doesn't show amount by default
            return;
        }

        amountText.text = NumberFormater.FormatNumber(generatedReward.amount);
    }

    public void SetState(ButtonState state)
    {
        _currentState = state;
        UpdateVisuals();
        loadIcon.SetActive(false);
    }

    private void UpdateVisuals()
    {
        if (generatedReward == null) return;

        bool collected = _currentState == ButtonState.Collected;

        iconImage.gameObject.SetActive(!collected);
        blackPlaceImage.gameObject.SetActive(!collected);
        lockIcon.SetActive(_currentState == ButtonState.Locked);
        watchText.gameObject.SetActive(_currentState == ButtonState.Available);

        if (amountText != null) amountText.gameObject.SetActive(!collected);

        glowBg.SetActive(!collected);
        superGlowBg.SetActive(!collected && generatedReward.isSuperReward);
    }

    public void OnClick()
    {
        if (_currentState == ButtonState.Available)
            onWatchClicked?.Invoke(this);
    }

    /// <summary>Play the collect animation and call onComplete when finished.</summary>
    public void PlayCollectAnimation(Action onComplete = null)
    {
        transform.DOPunchPosition(Vector3.up * 15f, jumpDuration, 5, 1f).OnComplete(() =>
        {
            iconImage.gameObject.SetActive(false);

            // create a simple flying icon for polish demo
            var fly = new GameObject("flyIcon", typeof(CanvasRenderer), typeof(Image));
            var img = fly.GetComponent<Image>();
            img.sprite = iconImage.sprite;

            var canvas = GetComponentInParent<Canvas>();
            if (canvas == null)
            {
                Destroy(fly);
                SetState(ButtonState.Collected);
                onComplete?.Invoke();
                return;
            }

            fly.transform.SetParent(canvas.transform, false);
            fly.transform.position = iconImage.transform.position;

            fly.transform.DOMove(flyEndpoint.position, flyDuration).SetEase(Ease.InCubic).OnComplete(() =>
            {
                Destroy(fly);
                SetState(ButtonState.Collected);
                onComplete?.Invoke();
            });
        });
    }

    public ButtonSaveData GetSaveData() => new ButtonSaveData { index = indexInBar, state = _currentState };

    public void LoadFromData(ButtonSaveData d) => SetState(d.state);
}

[Serializable]
public struct ButtonSaveData
{
    public int index;
    public ButtonState state;
}
