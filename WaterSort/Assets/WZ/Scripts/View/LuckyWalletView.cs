using UnityEngine;
using UnityEngine.UI;

namespace WZSDK
{
    public class LuckyWalletView : BaseView
    {
        private const float RefreshInterval = 0.2f;

        private Text currentCoinText;
        private Button withdrawButton;
        private Button close;

        private bool isInitialized;
        private bool isEventsBound;
        private float nextRefreshTime;

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
            RefreshWalletBalance();
            RefreshView();
        }

        public override void Start()
        {
          
        }

        public override void Update()
        {
            if (!isInitialized || Time.unscaledTime < nextRefreshTime)
                return;

            RefreshView();
            nextRefreshTime = Time.unscaledTime + RefreshInterval;
        }

        private void ResolveReferencesIfNeeded()
        {
            if (isInitialized)
                return;

            currentCoinText = FindByPath<Text>("Panel/CurCoin", "CurCoin");
            withdrawButton = FindByPath<Button>("Panel/Bottom/With", "With");
            close = FindByPath<Button>("Panel/Bottom/close", "close");

            isInitialized = currentCoinText != null || withdrawButton != null;
        }

        private T FindByPath<T>(string path, string fallbackName) where T : Component
        {
            GameObject target = FindChildUtility.FindChildByPath(gameObject, path);
            if (target == null)
                target = FindChildUtility.FindChild(gameObject, fallbackName);

            return target != null ? target.GetComponent<T>() : null;
        }

        private void BindEvents()
        {
            if (isEventsBound)
                return;

            withdrawButton?.onClick.AddListener(OnWithdrawClick);
            close?.onClick.AddListener(CloseClick);
            isEventsBound = true;
        }

        private void RemoveEvents()
        {
            if (!isEventsBound)
                return;

            withdrawButton?.onClick.RemoveListener(OnWithdrawClick);
            close?.onClick.RemoveListener(CloseClick);
            isEventsBound = false;
        }
        private void CloseClick()
        {
            SoundManager.Instance?.PlayUIClickSFX();
            CloseAndContinueStage3FlowIfNeeded();
        }

        private void OnWithdrawClick()
        {
            SoundManager.Instance?.PlayUIClickSFX();
            CloseAndContinueStage3FlowIfNeeded();
        }

        private void CloseAndContinueStage3FlowIfNeeded()
        {
            bool pendingMission = GameManagerWZ.instance != null
                                 && GameManagerWZ.instance.PendingStage3WithdrawMissionAfterLuckyWallet;

            CloseCurrentView();
            UIManager.instance?.CloseView(EUIType.LuckyWalletView);

            if (pendingMission)
                GameManagerWZ.instance.OpenWithdrawMissionAfterLuckyWalletIfPending();
        }

        private void CloseCurrentView()
        {
            if (canvasGroup != null)
                HideView();
            else
                gameObject.SetActive(false);
        }

        private void RefreshView()
        {
            if (currentCoinText == null || GameManagerWZ.instance == null)
                return;

            currentCoinText.text = FormatCoin((float)WithdrawApiService.GetCachedWalletBalance());
        }

        private void RefreshWalletBalance()
        {
            if (GameManagerWZ.instance == null)
                return;

            StartCoroutine(WithdrawApiService.QueryWalletBalance(wallet =>
            {
                RefreshView();
                if (wallet != null && wallet.PayPalAccountExists == 0 && !string.IsNullOrEmpty(wallet.PayPalAccountTip))
                {
                    WithdrawApiService.LogPaymentNotice("LuckyWalletView", wallet.PayPalAccountTip, LogType.Warning);
                }
            }, error =>
            {
                Debug.LogError($"钱包余额查询失败: {error}");
                RefreshView();
            }));
        }

        private string FormatCoin(float coin)
        {
            if (FacadePayTypeExtend.RegionalChangeHandle != null)
                return FacadePayTypeExtend.RegionalChangeHandle(coin);

            return $"{GetCurrencyMark()}{FormatPlainCoin(coin)}";
        }

        private string FormatPlainCoin(float coin)
        {
            int decimals = GameManagerWZ.instance != null ? Mathf.Max(GameManagerWZ.instance.decimals, 0) : 2;
            string formatted = coin.ToString($"F{decimals}");
            return formatted.TrimEnd('0').TrimEnd('.');
        }

        private string GetCurrencyMark()
        {
            return GameManagerWZ.instance != null ? GameManagerWZ.instance.mark : "$";
        }
    }
}
