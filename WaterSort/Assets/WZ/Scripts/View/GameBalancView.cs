using System;
using UnityEngine;
using UnityEngine.UI;

namespace WZSDK
{
    public class GameBalancView : BaseView
    {
        private const double MinimumCashOutAmount = 0.1d;
        private const float RefreshInterval = 0.2f;

        private static readonly string[] LevelProgressDescriptionKeys =
        {
            "3073",
            "3074",
            "3075",
            "3076",
        };

        private Text currentCoinText;
        private RectTransform levelProgressRoot;
        private Button withdrawButton;
        private Button closeButton;

        private bool isInitialized;
        private bool isEventsBound;
        private float nextRefreshTime;


        public override void Start()
        {

        }

        public override void InitView()
        {
            ResolveReferencesIfNeeded();
            BindEvents();
            RefreshWalletBalance();
            RefreshView();
        }

        private void OnDestroy()
        {
            RemoveEvents();
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

            currentCoinText = FindByPath<Text>("Plane/CurrrCoin", "CurrrCoin");
            levelProgressRoot = FindByPath<RectTransform>("Plane/LevelProgressTxt", "LevelProgressTxt");
            withdrawButton = FindByPath<Button>("Plane/With", "With");
            closeButton = FindByPath<Button>("Plane/ExitBtn", "ExitBtn");

            isInitialized = currentCoinText != null || levelProgressRoot != null || withdrawButton != null || closeButton != null;
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
            closeButton?.onClick.AddListener(OnCloseClick);
            isEventsBound = true;
        }

        private void RemoveEvents()
        {
            if (!isEventsBound)
                return;

            withdrawButton?.onClick.RemoveListener(OnWithdrawClick);
            closeButton?.onClick.RemoveListener(OnCloseClick);
            isEventsBound = false;
        }

        private void OnWithdrawClick()
        {
            double walletBalance = WithdrawApiService.GetCachedWalletBalance();
            if (walletBalance <= MinimumCashOutAmount)
            {
                string noticeText = LocalizationManager.Instance != null
                    ? LocalizationManager.Instance.GetText("3093")
                    : "Withdrawal amount must be greater than $0.10";
                UIManager.instance?.ShowView(EUIType.NoticeView, noticeText);
                return;
            }
            DataModel dataModel = WithdrawApiService.CreateWalletWithdrawDataModel();
            WithdrawOrderData queuedOrder = WithdrawApiService.CreateQueuedWalletOrder(dataModel.Money, dataModel.Email);
            if (queuedOrder != null)
            {
                dataModel.OrderNo = queuedOrder.OrderNo;
                dataModel.QueuedOrderNo = queuedOrder.OrderNo;
                dataModel.WithdrawType = queuedOrder.WithdrawType;
            }

            if (!TryStartWalletWithdrawRequest(dataModel))
                return;

            if (GameApp.viewManager != null)
                GameApp.viewManager.Open(ViewType.WithDrawProcess1, dataModel);

            UIManager.instance?.CloseView(EUIType.GameBalancView);
        }

        private bool TryStartWalletWithdrawRequest(DataModel dataModel)
        {
            if (dataModel == null || GameManagerWZ.instance == null)
            {
                Debug.LogWarning("Wallet withdraw request skipped: invalid data or missing GameManager.");
                return false;
            }

            ClientWithdrawRequest request = BuildWalletWithdrawRequest(dataModel);
            if (request == null)
            {
                Debug.LogWarning("Wallet withdraw request skipped: invalid request.");
                return false;
            }

            string queuedOrderNo = dataModel.QueuedOrderNo;
            GameManagerWZ.instance.StartCoroutine(WithdrawApiService.SubmitWithdraw(request, order =>
            {
                if (order == null)
                {
                    Debug.LogWarning("Wallet withdraw request returned empty order.");
                    return;
                }

                if (!string.IsNullOrEmpty(queuedOrderNo))
                    WithdrawApiService.ReplaceQueuedWalletOrder(queuedOrderNo, order);

                dataModel.QueuedOrderNo = string.Empty;
                dataModel.OrderNo = order.OrderNo;
                dataModel.OrderStatus = order.OrderStatus;
                dataModel.WithdrawType = order.WithdrawType;
                dataModel.Receiver = order.Receiver;
                dataModel.WalletFrozen = order.WalletFrozen;

                GameManagerWZ.instance.StartCoroutine(WithdrawApiService.QueryWalletBalance(_ => { }, null));
            }, error =>
            {
                Debug.LogWarning($"Wallet withdraw request returned error: {error}");
            }));

            return true;
        }

        private ClientWithdrawRequest BuildWalletWithdrawRequest(DataModel dataModel)
        {
            if (dataModel == null)
                return null;

            string receiver = !string.IsNullOrEmpty(dataModel.Email)
                ? dataModel.Email
                : (GameManagerWZ.instance != null ? GameManagerWZ.instance.getPlayerEmail() : string.Empty);

            return new ClientWithdrawRequest
            {
                WithdrawType = WithdrawConstants.WithdrawTypeWallet,
                Receiver = string.IsNullOrEmpty(receiver) ? null : receiver,
                Amount = Math.Round(dataModel.Money, 2),
                Currency = WithdrawConstants.CurrencyUsd,
                Note = "Wallet withdraw",
                EmailSubject = string.Empty,
                RecipientType = WithdrawConstants.RecipientTypeEmail
            };
        }

        private void HandleWalletWithdrawError(DataModel dataModel, string error)
        {
            if (dataModel != null && !string.IsNullOrEmpty(dataModel.QueuedOrderNo))
            {
                WithdrawApiService.RemoveQueuedWalletOrder(dataModel.QueuedOrderNo);
                dataModel.QueuedOrderNo = string.Empty;
            }

            if (!string.IsNullOrEmpty(error))
                WithdrawApiService.LogPaymentNotice("GameBalancView", error, LogType.Warning);

            if (ShouldShowPayPalErrorView(error))
            {
                UIManager.instance?.ShowView(EUIType.PayPalErrorView, dataModel);
            }
        }

        private bool ShouldShowPayPalErrorView(string error)
        {
            return !string.IsNullOrEmpty(error)
                   && (error.Contains("PayPal\u6536\u6b3e\u8d26\u53f7\u4e0d\u5b58\u5728")
                       || error.Contains("PayPal\u6536\u6b3e\u8d26\u53f7\u4e0d\u80fd\u4e3a\u7a7a"));
        }

        private void OnCloseClick()
        {
            UIManager.instance?.CloseView(EUIType.GameBalancView);
        }

        private DataModel CreateWithdrawData(GameManagerWZ gm)
        {
            return new DataModel
            {
                Level = gm.currentLv - 1,
                Money = GetWithdrawMoney(),
                Num = GetCurrentStage(gm),
                CloseType = 1,
                Special = DataModel.SpecialLuckyWalletWithdraw
            };
        }

        private void RefreshView()
        {
            RefreshCurrentCoin();
            RefreshLevelProgressTexts();
        }

        private void RefreshCurrentCoin()
        {
            if (currentCoinText == null || GameManagerWZ.instance == null)
                return;

            currentCoinText.text = FormatCoin(GetWithdrawMoney());
        }

        private double GetWithdrawMoney()
        {
            return WithdrawApiService.GetCachedWalletBalance();
        }

        private int GetCurrentStage(GameManagerWZ gm)
        {
            return gm != null ? Mathf.Max(gm.currentStage, 1) : 1;
        }

        private void RefreshLevelProgressTexts()
        {
            if (levelProgressRoot == null)
                return;

            for (int i = 0; i < levelProgressRoot.childCount; i++)
            {
                Transform child = levelProgressRoot.GetChild(i);
                Text childText = child.GetComponent<Text>();
                if (childText == null)
                    continue;

                bool shouldShow = i < LevelProgressDescriptionKeys.Length;
                child.gameObject.SetActive(shouldShow);

                if (shouldShow)
                    childText.text = GetLocalizedLevelProgressDescription(LevelProgressDescriptionKeys[i]);
            }
        }

        private string GetLocalizedLevelProgressDescription(string key)
        {
            if (string.IsNullOrEmpty(key))
                return string.Empty;

            if (LocalizationManager.Instance == null)
                return key;

            string value = LocalizationManager.Instance.GetText(key);
            return string.IsNullOrEmpty(value) ? key : value;
        }

        private string FormatCoin(double coin)
        {
            if (FacadePayTypeExtend.RegionalChangeHandle != null)
                return FacadePayTypeExtend.RegionalChangeHandle((float)coin);

            return $"{GetCurrencyMark()}{FormatPlainCoin(coin)}";
        }

        private string FormatPlainCoin(double coin)
        {
            int decimals = GameManagerWZ.instance != null ? Mathf.Max(GameManagerWZ.instance.decimals, 0) : 2;
            string formatted = coin.ToString($"F{decimals}");
            return formatted.TrimEnd('0').TrimEnd('.');
        }

        private string GetCurrencyMark()
        {
            return GameManagerWZ.instance != null ? GameManagerWZ.instance.mark : "$";
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
                    WithdrawApiService.LogPaymentNotice("GameBalancView", wallet.PayPalAccountTip, LogType.Warning);
                }
            }, error =>
            {
                Debug.LogError($"钱包余额查询失败: {error}");
                RefreshView();
            }));
        }

        
    }
}
