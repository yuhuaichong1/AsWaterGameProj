using System.Collections;
using System;
using UnityEngine;
using UnityEngine.UI;

namespace WZSDK
{
    public class LuckySuccessView : BaseView
    {
        private const float RefreshInterval = 0.2f;
        private const float DefaultLuckyWalletAdReward = 0f;

        private Text CurrentLuckyCoin;
        private Text CurrentLuckyCoinTxT;
        private Button AdBtn;
        private Button AbandonBtn;

        private bool isReferencesResolved;
        private bool isEventsBound;
        private bool isProcessingAd;
        private bool isClosing;
        private int completedLuckyLevel;
        private float nextRefreshTime;
        private float currentRewardAmount = DefaultLuckyWalletAdReward;

        private void Awake()
        {
            ResolveReferencesIfNeeded();
        }

        private void OnDestroy()
        {
            RemoveEvents();
        }

        public override void InitView()
        {
            ResolveReferencesIfNeeded();
            BindEvents();
            isProcessingAd = false;
            isClosing = false;
            SetButtonsInteractable(true);
            currentRewardAmount = DefaultLuckyWalletAdReward;
            RefreshView();
            RefreshRewardAmountFromServer();
        }

        public override void Start()
        {
            ResolveReferencesIfNeeded();
            RefreshView();
        }

        public override void Update()
        {
            if (!isReferencesResolved || Time.unscaledTime < nextRefreshTime)
                return;

            RefreshView();
            nextRefreshTime = Time.unscaledTime + RefreshInterval;
        }

        protected override void HandleViewArgs(object[] args)
        {
            if (args == null || args.Length <= 0)
                return;

            if (args[0] is int level)
                completedLuckyLevel = Mathf.Max(level, 0);
        }

        private void ResolveReferencesIfNeeded()
        {
            if (isReferencesResolved)
                return;

            CurrentLuckyCoin = FindChildUtility.FindChild<Text>(gameObject, "CurrentLuckyCoin");
            CurrentLuckyCoinTxT = FindChildUtility.FindChild<Text>(gameObject, "CurrentLuckyCoinTxT");
            AdBtn = FindChildUtility.FindChild<Button>(gameObject, "AdBtn");
            AbandonBtn = FindChildUtility.FindChild<Button>(gameObject, "AbandonBtn");

            isReferencesResolved = CurrentLuckyCoin != null || CurrentLuckyCoinTxT != null || AdBtn != null || AbandonBtn != null;
        }

        private void BindEvents()
        {
            if (isEventsBound)
                return;

            AdBtn?.onClick.AddListener(OnAdClick);
            AbandonBtn?.onClick.AddListener(OnAbandonClick);
            isEventsBound = true;
        }

        private void RemoveEvents()
        {
            if (!isEventsBound)
                return;

            AdBtn?.onClick.RemoveListener(OnAdClick);
            AbandonBtn?.onClick.RemoveListener(OnAbandonClick);
            isEventsBound = false;
        }

        private void OnAdClick()
        {
            if (isProcessingAd || isClosing || AdsControl.Instance == null)
                return;

            SoundManager.Instance?.PlayUIClickSFX();
            isProcessingAd = true;
            SetButtonsInteractable(false);

            AdsControl.Instance.ShowRewardedAd("challenge_success_reward", success =>
            {
                isProcessingAd = false;

                if (!success)
                {
                    if (!isClosing)
                        SetButtonsInteractable(true);
                    return;
                }

                int luckyLevel = ResolveCompletedLuckyLevel();
                if (luckyLevel > 0)
                    WithdrawApiService.AddWalletIncome(currentRewardAmount, WithdrawConstants.WalletIncomeTypeLucky, luckyLevel);
                else
                    WithdrawApiService.AddWalletIncome(currentRewardAmount, WithdrawConstants.WalletIncomeTypeLucky);

                CloseAndNextLevel(ChallangeSuccessView.HasPendingLuckySpinReward());
            }, false);
        }

        private void OnAbandonClick()
        {
            if (isProcessingAd || isClosing)
                return;

            SoundManager.Instance?.PlayUIClickSFX();
            CloseAndNextLevel(ChallangeSuccessView.HasPendingLuckySpinReward());
        }

        private void CloseAndNextLevel(bool shouldShowLuckySpin)
        {
            if (isClosing)
                return;

            isClosing = true;
            SetButtonsInteractable(false);
            Time.timeScale = 1f;

            if (GameManagerWZ.instance != null)
                GameManagerWZ.instance.StartCoroutine(DelayedNextLevel(shouldShowLuckySpin));
            else
                UIManager.instance?.CloseView(EUIType.LuckySuccessView);
        }

        private IEnumerator DelayedNextLevel(bool shouldShowLuckySpin)
        {
            UIManager.instance?.CloseView(EUIType.LuckySuccessView);
            yield return null;

            GameManagerWZ.instance?.NextLevel();

            if (!shouldShowLuckySpin)
                yield break;

            ChallangeSuccessView.ConsumePendingLuckySpinReward();
            yield return null;
            UIManager.instance?.ShowView(EUIType.LuckySpinView, new LuckySpinOpenData());
        }

        private void SetButtonsInteractable(bool isInteractable)
        {
            if (AdBtn != null)
                AdBtn.interactable = isInteractable;

            if (AbandonBtn != null)
                AbandonBtn.interactable = isInteractable;
        }

        private void RefreshView()
        {
            string luckyChestCoinText = FormatCoin(currentRewardAmount);

            if (CurrentLuckyCoin != null)
                CurrentLuckyCoin.text = luckyChestCoinText;

            if (CurrentLuckyCoinTxT != null)
                CurrentLuckyCoinTxT.text = luckyChestCoinText;
        }

        private void RefreshRewardAmountFromServer()
        {
            int luckyLevel = ResolveCompletedLuckyLevel();
            Debug.Log($"LuckySuccess reward request start: api={GameDefines.LuckyRewardConfigApiPath}, requestLevel={luckyLevel}, currentRewardAmount={currentRewardAmount:F2}");
            StartCoroutine(WithdrawApiService.QueryLuckyRewardConfig(config =>
            {
                float serverRewardAmount = config != null && config.LuckyAmount.HasValue
                    ? (float)config.LuckyAmount.Value
                    : 0f;
                currentRewardAmount = RoundRewardAmount(serverRewardAmount);
                Debug.Log($"LuckySuccess reward config applied: level={luckyLevel}, rawServerAmount={serverRewardAmount:F6}, serverAmount={serverRewardAmount:F2}, displayAmount={currentRewardAmount:F2}");
                RefreshView();
            }, error =>
            {
                currentRewardAmount = 0f;
                Debug.LogWarning($"LuckySuccess reward query failed: level={luckyLevel}, error={error}");
                RefreshView();
            }, luckyLevelOverride: luckyLevel > 0 ? luckyLevel : (int?)null));
        }

        private int ResolveCompletedLuckyLevel()
        {
            if (completedLuckyLevel > 0)
                return completedLuckyLevel;

            GameManagerWZ gm = GameManagerWZ.instance;
            if (gm != null)
                return Mathf.Max(gm.currentLv - 1, 0);

            int currentLevel = PlayerPrefs.GetInt(FacadePlayerPrefExtend.currentLevel, 1);
            return Mathf.Max(currentLevel - 1, 0);
        }

        private string FormatCoin(float coin)
        {
            if (FacadePayTypeExtend.RegionalChangeHandle != null)
                return FacadePayTypeExtend.RegionalChangeHandle(coin);

            return $"{GetCurrencyMark()}{FormatPlainCoin(coin)}";
        }

        private string FormatPlainCoin(float coin)
        {
            return RoundRewardAmount(coin).ToString("F2");
        }

        private string GetCurrencyMark()
        {
            return GameManagerWZ.instance != null ? GameManagerWZ.instance.mark : "$";
        }

        private static float RoundRewardAmount(float amount)
        {
            return (float)Math.Round(amount, 2, MidpointRounding.AwayFromZero);
        }
    }
}
