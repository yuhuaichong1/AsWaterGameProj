using System;
using UnityEngine;
using UnityEngine.UI;
namespace WZSDK
{
    public class WithDrawProcess1 : BaseViewWithDraw
    {
        private const int ProcessStateCount = 5;
        private const string VerificationCompletedTextKey = "2052";
        private const float StepDuration = 1f;
        private const float CompletionActionsShowDelay =1f;
        private static readonly string[] OtherStageWithdrawStateContentKeys = { "3017", "3019", "3021" };
        private static readonly string[] OtherStageWithdrawFinishedStateContentKeys = { "3018", "3020", "3072" };

        private static readonly string[] Stage3WithdrawStateContentKeys = { "3017", "3019", "3021" };
        private static readonly string[] Stage3WithdrawFinishedStateContentKeys = { "3018", "3020", "3022" };

        private Button continueButton;
        private Button closeButton;
        private GameObject continueRoot;
        private GameObject closeRoot;
        private RectTransform Dec;
        private float time;
        private int completedStateCount;
        private bool isAnimating;
        private double money;
        private Text moneyContent;
        private RectTransform stateRoot;
        private Text Num;
        private DataModel dataModel;
        private readonly WithDrawProcessStateItem[] processStates = new WithDrawProcessStateItem[ProcessStateCount];
        private int visibleStateCount = ProcessStateCount;
        private int currentStageId = 1;
        private bool isSubmitStarted;
        private bool isSubmitCompleted;
        private bool isSubmitSuccessful;
        private bool isProgressFlowCompleted;
        private WithdrawOrderData currentOrder;
        private Coroutine showCompletionActionsCoroutine;
        private bool completionActionsVisible;

        protected override void OnAwake()
        {
            continueButton = FindByPath<Button>("PanelA/Continue", "Continue");
            closeButton = FindByPath<Button>("PanelA/close", "close");
            moneyContent = FindByPath<Text>("PanelA/PanelB/content", "content");
            Num = FindByPath<Text>("Panel/titleContent", "titleContent");
            stateRoot = FindByPath<RectTransform>("PanelA/PanelC/StateRoot", "StateRoot");
            Dec = FindByPath<RectTransform>("PanelA/PanelC/Dec", "Dec");
            continueRoot = continueButton != null ? continueButton.gameObject : FindByPath("PanelA/Continue", "Continue");
            closeRoot = closeButton != null ? closeButton.gameObject : FindByPath("PanelA/close", "close");

            continueButton?.onClick.AddListener(OnButtonClick);
            closeButton?.onClick.AddListener(OnCloseBtnClick);

            CacheProcessStates();
            ResetProgressFlow();
        }

        protected override void HandleViewArgs(object[] args)
        {
            // 排队/进度页出现时不应再叠 CMBtn 引导。
            GameManagerWZ.instance?.CloseActiveTutorial();

            dataModel = args != null && args.Length > 0 ? args[0] as DataModel : null;
            if (dataModel == null)
                dataModel = new DataModel();

            currentStageId = GetCurrentStageId();
            money = GetCurrentWithdrawMoney();
            visibleStateCount = GetVisibleStateCount();
            moneyContent.text = GameManagerWZ.instance.mark + money.ToString("F2");
            ApplyProcessStateContents();
            Num.text = string.Format(LocalizationManager.Instance.GetText("2062"), currentStageId);
            isSubmitStarted = false;
            isSubmitCompleted = false;
            isSubmitSuccessful = false;
            isProgressFlowCompleted = false;
            currentOrder = null;
            ResetProgressFlow();
        }

        public override void InitView()
        {
            base.InitView();
        }

        private void CacheProcessStates()
        {
            if (stateRoot == null)
                return;

            int count = Mathf.Min(stateRoot.childCount, processStates.Length);
            for (int i = 0; i < count; i++)
                processStates[i] = GetOrAddProcessStateItem(stateRoot.GetChild(i).gameObject);
        }

        private void ApplyProcessStateContents()
        {
            string fallbackWaitContent = GetLocalizedText(VerificationCompletedTextKey, "Verification completed");
            string fallbackFinishedContent = GetLocalizedText(VerificationCompletedTextKey, "Verification completed");

            for (int i = 0; i < processStates.Length; i++)
            {
                string waitingContent = GetWaitingStateContent(i, fallbackWaitContent);
                string finishedContent = GetFinishedStateContent(i, fallbackFinishedContent);
                processStates[i]?.SetContents(waitingContent, finishedContent);
            }
        }

        private WithDrawProcessStateItem GetOrAddProcessStateItem(GameObject target)
        {
            if (target == null)
                return null;

            WithDrawProcessStateItem component = target.GetComponent<WithDrawProcessStateItem>();
            if (component == null)
                component = target.AddComponent<WithDrawProcessStateItem>();

            component.Initialize();
            return component;
        }

        private string GetLocalizedText(string key, string fallback)
        {
            if (LocalizationManager.Instance == null)
                return fallback;

            string value = LocalizationManager.Instance.GetText(key);
            return string.IsNullOrEmpty(value) ? fallback : value;
        }

        private void ResetProgressFlow()
        {
            if (showCompletionActionsCoroutine != null)
            {
                StopCoroutine(showCompletionActionsCoroutine);
                showCompletionActionsCoroutine = null;
            }

            time = 0f;
            completedStateCount = 0;
            isAnimating = visibleStateCount > 0;
            isProgressFlowCompleted = false;
            completionActionsVisible = false;

            if (Dec != null)
                Dec.gameObject.SetActive(false);

            for (int i = 0; i < processStates.Length; i++)
            {
                if (processStates[i] == null)
                    continue;

                processStates[i].ResetState();
                processStates[i].SetVisible(i == 0 && i < visibleStateCount);
            }

            RefreshProcessStateLines();

            if (continueRoot != null)
                continueRoot.SetActive(false);

            if (closeRoot != null)
                closeRoot.SetActive(false);
        }

        private void UpdateProgressFlow(int newCompletedCount)
        {
            int maxCompletableCount = GetMaxCompletableCount();
            int clampedCount = Mathf.Clamp(newCompletedCount, 0, maxCompletableCount);
            while (completedStateCount < clampedCount)
            {
                processStates[completedStateCount]?.SetCompleted(true);
                completedStateCount++;

                if (completedStateCount < visibleStateCount)
                {
                    processStates[completedStateCount]?.SetVisible(true);
                    processStates[completedStateCount]?.ResetState();
                }
            }

            RefreshProcessStateLines();
        }

        private void RefreshProcessStateLines()
        {
            int currentVisibleStateCount = ResolveCurrentlyVisibleStateCount();
            for (int i = 0; i < processStates.Length; i++)
            {
                if (processStates[i] == null)
                    continue;

                processStates[i].SetLineVisible(i < currentVisibleStateCount - 1);
            }
        }

        private int ResolveCurrentlyVisibleStateCount()
        {
            if (visibleStateCount <= 0)
                return 0;

            return Mathf.Clamp(completedStateCount + 1, 1, visibleStateCount);
        }

        private int GetCurrentStageId()
        {
            if (dataModel != null && dataModel.Num > 0)
                return dataModel.Num;

            if (GameManagerWZ.instance != null && GameManagerWZ.instance.currentStage > 0)
                return GameManagerWZ.instance.currentStage;

            return 1;
        }

        private double GetCurrentWithdrawMoney()
        {
            if (IsLuckyWalletWithdraw())
                return WithdrawApiService.GetCachedWalletBalance();

            if (dataModel != null && dataModel.Money > 0d)
                return dataModel.Money;

            if (GameManagerWZ.instance == null)
                return 0;

            return GameManagerWZ.instance.GetCoin();
        }

        private bool IsLuckyWalletWithdraw()
        {
            return dataModel != null && dataModel.Special == DataModel.SpecialLuckyWalletWithdraw;
        }

        private int GetVisibleStateCount()
        {
            if (!IsWithdrawButtonFlow())
                return 4;

            int configuredCount = Mathf.Max(
                GetWithdrawStateContentKeysForStage(currentStageId).Length,
                GetWithdrawFinishedStateContentKeysForStage(currentStageId).Length);

            return Mathf.Clamp(configuredCount > 0 ? configuredCount : 3, 0, ProcessStateCount);
        }

        private int GetMaxCompletableCount()
        {
            if (!IsWithdrawButtonFlow())
                return visibleStateCount;

            return Mathf.Max(visibleStateCount - 1, 0);
        }

        private bool IsWithdrawButtonFlow()
        {
            return dataModel != null && dataModel.Special == DataModel.SpecialNone;
        }

        private T FindByPath<T>(string path, string fallbackName = null) where T : Component
        {
            Transform target = FindTransformByPath(path);
            if (target != null)
                return target.GetComponent<T>();

            return !string.IsNullOrEmpty(fallbackName) ? FindChildUtility.FindChild<T>(gameObject, fallbackName) : null;
        }

        private GameObject FindByPath(string path, string fallbackName = null)
        {
            Transform target = FindTransformByPath(path);
            if (target != null)
                return target.gameObject;

            return !string.IsNullOrEmpty(fallbackName) ? FindChildUtility.FindChild(gameObject, fallbackName) : null;
        }

        private Transform FindTransformByPath(string path)
        {
            if (string.IsNullOrEmpty(path))
                return null;

            Transform current = transform;
            string[] segments = path.Split('/');
            for (int i = 0; i < segments.Length; i++)
            {
                current = FindChildUtility.FindChild<Transform>(current, segments[i]);
                if (current == null)
                    return null;
            }

            return current;
        }

        private void StartSubmitIfNeeded()
        {
            if (isSubmitStarted)
                return;

            isSubmitStarted = true;
            SetActionButtonsInteractable(false);
            StartCoroutine(SubmitWithdrawCoroutine());
        }

        private System.Collections.IEnumerator SubmitWithdrawCoroutine()
        {
            string queuedWalletOrderNo = GetQueuedWalletOrderNo();
            ClientWithdrawRequest request = BuildWithdrawRequest();
            if (request == null)
            {
                CleanupQueuedWalletOrder(queuedWalletOrderNo);
                HandleWithdrawSubmitError("提现参数不能为空");
                yield break;
            }

            yield return WithdrawApiService.SubmitWithdraw(request, order =>
            {
                currentOrder = order;
                if (order == null)
                {
                    CleanupQueuedWalletOrder(queuedWalletOrderNo);
                    HandleWithdrawSubmitError("提现结果为空");
                    return;
                }

                if (IsLuckyWalletWithdraw() && !string.IsNullOrEmpty(queuedWalletOrderNo))
                    WithdrawApiService.ReplaceQueuedWalletOrder(queuedWalletOrderNo, order);

                if (dataModel != null)
                {
                    dataModel.QueuedOrderNo = string.Empty;
                    dataModel.SubmitOnOpen = false;
                }

                dataModel.OrderNo = order != null ? order.OrderNo : dataModel.OrderNo;
                dataModel.OrderStatus = order != null ? order.OrderStatus : dataModel.OrderStatus;
                dataModel.WithdrawType = order != null ? order.WithdrawType : dataModel.WithdrawType;
                dataModel.Receiver = order != null ? order.Receiver : dataModel.Email;
                dataModel.WalletFrozen = order != null ? order.WalletFrozen : dataModel.WalletFrozen;

                if (ShouldHandleImmediateOrderState() && TryHandleImmediateOrderState(order))
                    return;

                isSubmitCompleted = true;
                isSubmitSuccessful = true;

                if (isProgressFlowCompleted)
                    ShowCompletionActions();

                if (IsLuckyWalletWithdraw())
                    StartCoroutine(WithdrawApiService.QueryWalletBalance(_ => { money = GetCurrentWithdrawMoney(); RefreshMoneyText(); }, null));

                SetActionButtonsInteractable(true);
            }, error =>
            {
                CleanupQueuedWalletOrder(queuedWalletOrderNo);
                HandleWithdrawSubmitError(error);
            });
        }

        private bool ShouldHandleImmediateOrderState()
        {
            return !IsLuckyWalletWithdraw();
        }

        private bool ShouldSubmitOnOpen()
        {
            return dataModel != null && dataModel.SubmitOnOpen && !IsLuckyWalletWithdraw();
        }

        private string GetQueuedWalletOrderNo()
        {
            if (dataModel == null)
                return string.Empty;

            if (!string.IsNullOrEmpty(dataModel.QueuedOrderNo))
                return dataModel.QueuedOrderNo;

            return dataModel.OrderNo != null && dataModel.OrderNo.StartsWith("local-wallet-", StringComparison.OrdinalIgnoreCase)
                ? dataModel.OrderNo
                : string.Empty;
        }

        private void CleanupQueuedWalletOrder(string queuedWalletOrderNo)
        {
            if (IsLuckyWalletWithdraw() && !string.IsNullOrEmpty(queuedWalletOrderNo))
                WithdrawApiService.RemoveQueuedWalletOrder(queuedWalletOrderNo);

            if (dataModel != null)
            {
                dataModel.QueuedOrderNo = string.Empty;
                dataModel.SubmitOnOpen = false;
            }
        }

        private bool TryHandleImmediateOrderState(WithdrawOrderData order)
        {
            if (order == null)
            {
                HandleWithdrawSubmitError("提现结果为空");
                return true;
            }

            if (order.RequiresNewReceiver)
            {
                HandleWithdrawSubmitError("当前PayPal收款账号不存在，请重新填写正确的PayPal邮箱");
                return true;
            }

            if (order.OrderStatus == 4)
            {
                HandleWithdrawSubmitError(string.IsNullOrEmpty(order.ErrorMessage) ? "提现失败，请稍后重试" : order.ErrorMessage);
                return true;
            }

            if (order.OrderStatus == 5)
            {
                HandleWithdrawSubmitError("PayPal余额不足，等待平台补发");
                return true;
            }

            return false;
        }

        private ClientWithdrawRequest BuildWithdrawRequest()
        {
            GameManagerWZ gm = GameManagerWZ.instance;
            string receiver = dataModel != null && !string.IsNullOrEmpty(dataModel.Email)
                ? dataModel.Email
                : (gm != null ? gm.getPlayerEmail() : string.Empty);
            bool isRetry = dataModel != null && dataModel.RetryWithdraw && !string.IsNullOrEmpty(dataModel.OrderNo);
            bool allowEmptyReceiver = IsLuckyWalletWithdraw() && !isRetry;

            if (string.IsNullOrEmpty(receiver) && !allowEmptyReceiver)
            {
                return null;
            }

            string withdrawType = ResolveWithdrawType();

            ClientWithdrawRequest request = new ClientWithdrawRequest
            {
                OrderNo = isRetry ? dataModel.OrderNo : null,
                WithdrawType = withdrawType,
                Receiver = string.IsNullOrEmpty(receiver) ? null : receiver,
                Amount = isRetry ? (double?)null : Math.Round(GetCurrentWithdrawMoney(), 2),
                Currency = WithdrawConstants.CurrencyUsd,
                Note = string.Equals(withdrawType, WithdrawConstants.WithdrawTypeWallet, StringComparison.OrdinalIgnoreCase)
                    ? "Wallet withdraw"
                    : "First ad withdraw",
                EmailSubject = string.Empty,
                RecipientType = WithdrawConstants.RecipientTypeEmail
            };

            return request;
        }

        private string ResolveWithdrawType()
        {
            if (dataModel != null && !string.IsNullOrEmpty(dataModel.WithdrawType))
                return dataModel.WithdrawType;

            return IsLuckyWalletWithdraw()
                ? WithdrawConstants.WithdrawTypeWallet
                : WithdrawConstants.WithdrawTypeFirstAd;
        }

        private void HandleWithdrawSubmitError(string error)
        {
            isSubmitCompleted = true;
            isSubmitSuccessful = false;
            isAnimating = false;

            if (!string.IsNullOrEmpty(error))
                WithdrawApiService.LogPaymentNotice("WithDrawProcess1", error, LogType.Warning);

            if (!string.IsNullOrEmpty(error)
                && (error.Contains("PayPal收款账号不存在") || error.Contains("PayPal收款账号不能为空")))
            {
                UIManager.instance?.ShowView(EUIType.PayPalErrorView, dataModel);
            }

            GameApp.viewManager.Close(ViewId);
        }

        private void RefreshMoneyText()
        {
            if (moneyContent != null && GameManagerWZ.instance != null)
                moneyContent.text = GameManagerWZ.instance.mark + money.ToString("F2");
        }

        private void SetActionButtonsInteractable(bool isInteractable)
        {
            if (continueButton != null)
                continueButton.interactable = isInteractable;

            if (closeButton != null)
                closeButton.interactable = isInteractable;
        }

        private void ShowCompletionActions()
        {
            if (completionActionsVisible || showCompletionActionsCoroutine != null)
                return;

            showCompletionActionsCoroutine = StartCoroutine(ShowCompletionActionsDelayed());
        }

        private System.Collections.IEnumerator ShowCompletionActionsDelayed()
        {
            if (CompletionActionsShowDelay > 0f)
                yield return new WaitForSeconds(CompletionActionsShowDelay);

            showCompletionActionsCoroutine = null;

            if (this == null || !gameObject.activeInHierarchy)
                yield break;

            ShowCompletionActionsImmediately();
        }

        private void ShowCompletionActionsImmediately()
        {
            if (completionActionsVisible)
                return;

            completionActionsVisible = true;
            UpdateLastVisibleStateTextIfNeeded();

            if (Dec != null && IsWithdrawButtonFlow())
                Dec.gameObject.SetActive(true);

            if (continueRoot != null)
                continueRoot.SetActive(true);

            if (closeRoot != null)
                closeRoot.SetActive(true);
        }


        public void OnButtonClick()
        {
            if (RequiresClickToSubmit())
            {
                if (!isSubmitCompleted)
                {
                    StartSubmitIfNeeded();
                    return;
                }

                if (!isSubmitSuccessful)
                    return;
            }

            StartCoroutine(DelayedNextLevel());
        }

        public void OnCloseBtnClick()
        {
            StartCoroutine(DelayedNextLevel());
        }

        private System.Collections.IEnumerator DelayedNextLevel()
        {
            yield return new WaitForSeconds(0.2f);
            if (ShouldCloseWithoutAdvance())
            {
                GameApp.viewManager.Close(ViewId);
                yield break;
            }

            var gm = GameManagerWZ.instance;
            if (gm != null)
            {
                // Goal2 流程：Process1 若仍出现，Continue 后进 Mission，不立刻开下一关。
                if (gm.Stage2CashGuidePending || gm.DeferHostLevelLoad
                    || (dataModel != null && dataModel.Num == 2))
                {
                    GameApp.viewManager.Close(ViewId);
                    gm.FinishStage2CashGuideAndShowMission();
                    yield break;
                }

                // Goal1 用兑现替代通关页，或 stage1/2/3 流程：结束后推进下一关。
                if (gm.IsReplacingSuccessWithWithdraw())
                    gm.EndWithdrawInsteadOfSuccessAndContinue();
                else if (gm.currentStage <= 3 || WaterSortWZBridge.HostDriven)
                    gm.NextLevel();
            }
            GameApp.viewManager.Close(ViewId);
        }

        private bool ShouldCloseWithoutAdvance()
        {
            return dataModel == null
                   || IsLuckyWalletWithdraw()
                   || dataModel.RetryWithdraw
                   || GameManagerWZ.instance == null;
        }

        private bool RequiresClickToSubmit()
        {
            return false;
        }

        private void UpdateLastVisibleStateTextIfNeeded()
        {
            if (visibleStateCount <= 0)
                return;

            WithDrawProcessStateItem lastVisibleState = processStates[visibleStateCount - 1];
            lastVisibleState?.SetFinishedContentVisible(true);
        }

        private string[] GetWithdrawStateContentKeysForStage(int stageId)
        {
            return IsWithdrawButtonFlow() ? Stage3WithdrawStateContentKeys : OtherStageWithdrawStateContentKeys;
        }

        private string[] GetWithdrawFinishedStateContentKeysForStage(int stageId)
        {
            return IsWithdrawButtonFlow() ? Stage3WithdrawFinishedStateContentKeys : OtherStageWithdrawFinishedStateContentKeys;
        }

        private string GetWaitingStateContent(int index, string fallbackContent)
        {
            string content = GetLocalizedStateContent(GetWithdrawStateContentKeysForStage(currentStageId), index, fallbackContent);
            return ComposeQueueStateContentIfNeeded(index, content);
        }

        private string GetFinishedStateContent(int index, string fallbackContent)
        {
            string content = GetLocalizedStateContent(GetWithdrawFinishedStateContentKeysForStage(currentStageId), index, fallbackContent);
            return ComposeQueueStateContentIfNeeded(index, content);
        }

        private string ComposeQueueStateContentIfNeeded(int index, string content)
        {
            if (!ShouldPrefixQueueStateContent(index) || string.IsNullOrEmpty(content))
                return content;

            string queueingStatePrefix = GetQueueingStatePrefix();
            if (string.IsNullOrEmpty(queueingStatePrefix))
                return content;

            if (content.StartsWith(queueingStatePrefix, StringComparison.Ordinal))
                return content;

            return $"{queueingStatePrefix}\n{content}";
        }

        private bool ShouldPrefixQueueStateContent(int index)
        {
            return IsWithdrawButtonFlow()
                   && currentStageId != 3
                   && visibleStateCount > 0
                   && index == visibleStateCount - 1;
        }

        private string GetQueueingStatePrefix()
        {
            return GetLocalizedText("3029", "In queue");
        }

        private string GetLocalizedStateContent(string[] keys, int index, string fallbackContent)
        {
            if (keys == null || index < 0 || index >= keys.Length)
                return fallbackContent;

            return GetLocalizedText(keys[index], fallbackContent);
        }


        private void Update()
        {
            if (!isAnimating)
                return;

            time += Time.deltaTime;
            int newCompletedCount = Mathf.FloorToInt(time / StepDuration);
            UpdateProgressFlow(newCompletedCount);

            if (completedStateCount >= GetMaxCompletableCount())
            {
                isProgressFlowCompleted = true;
                ShowCompletionActions();
                isAnimating = false;
            }

        }
    }
}
