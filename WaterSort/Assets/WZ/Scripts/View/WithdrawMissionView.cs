using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.UI;

namespace WZSDK
{
    public class WithdrawMissionView : BaseView
    {
        const int VisibleStageCount = 3;
        const int Stage3Id = 3;
        const int Stage4Id = 4;
        const int Stage5Id = 5;
        const int AdMissionType = 4;
        const int DefaultAdMissionTarget = 3;
        const float RefreshInterval = 0.2f;
        const string EmptyPayPalValue = "--";
        const float Stage3AdDisplayIncrease = 0.1f;
        const string Stage3AdIncreaseColor = "#E60000";
        const float TestButtonYOffset = 120f;
        const string TestSubmitButtonName = "TestFirstWithdrawBtn";
        const string TestSubmitButtonText = "测试首提请求";
        const string TestSubmitMissingPayPalText = "PayPal账号为空，无法发送首提请求";
        const string TestSubmitTimeoutText = "首提测试请求超时或未完成";

        private Text timeText;
        private Text stateText;
        private Text curCoinText;
        private Text queueSizeText;
        private Image progress;
        private RectTransform progressStateRoot;
        private Text decText;
        private Button generateMoreBtn;
        private Button continueBtn;
        private Button testFirstWithdrawBtn;

        private readonly WithdrawMissionStateItem[] progressStates = new WithdrawMissionStateItem[VisibleStageCount];

        private bool isInitialized;
        private bool isSubmittingWithdrawRequest;
        private float nextRefreshTime;
        private string defaultStateValue;
        private string defaultQueueValue;
        private string defaultDecValue;

        public override void InitView()
        {
            InitializeIfNeeded();
            RefreshView();
        }

        public override void Start()
        {
            InitializeIfNeeded();
            RefreshView();
        }

        public override void Update()
        {
            if (!isInitialized || UnityEngine.Time.unscaledTime < nextRefreshTime)
                return;

            RefreshView();
            nextRefreshTime = UnityEngine.Time.unscaledTime + RefreshInterval;
        }

        void InitializeIfNeeded()
        {
            if (isInitialized)
                return;

            timeText = FindChildUtility.FindChild(gameObject, "Time")?.GetComponent<Text>();
            stateText = FindChildUtility.FindChild(gameObject, "State")?.GetComponent<Text>();
            curCoinText = FindChildUtility.FindChild(gameObject, "CurCoin")?.GetComponent<Text>();
            if (curCoinText != null)
                curCoinText.supportRichText = true;
            queueSizeText = FindChildUtility.FindChild(gameObject, "QueueSize")?.GetComponent<Text>();
            progress = FindChildUtility.FindChild(gameObject, "Progress")?.GetComponent<Image>();
            progressStateRoot = FindChildUtility.FindChild(gameObject, "ProgressStateRoot")?.GetComponent<RectTransform>();
            decText = FindChildUtility.FindChild(gameObject, "Dec")?.GetComponent<Text>();
            generateMoreBtn = FindChildUtility.FindChild(gameObject, "GenerateMoreBtn")?.GetComponent<Button>();
            continueBtn = FindChildUtility.FindChild(gameObject, "continuebtn")?.GetComponent<Button>();
            if (continueBtn == null)
                continueBtn = FindChildUtility.FindChild(gameObject, "continue")?.GetComponent<Button>();

            EnsureTestFirstWithdrawButton();

            continueBtn?.onClick.AddListener(ContinueClick);
            generateMoreBtn?.onClick.AddListener(GenerateMoreBtnClick);
            testFirstWithdrawBtn?.onClick.AddListener(TestSubmitFirstWithdrawClick);

            if (stateText != null)
                defaultStateValue = stateText.text;

            if (queueSizeText != null)
                defaultQueueValue = queueSizeText.text;

            if (decText != null)
                defaultDecValue = decText.text;

            if (progressStateRoot != null)
            {
                for (int i = 0; i < VisibleStageCount && i < progressStateRoot.childCount; i++)
                {
                    progressStates[i] = GetOrAddStateItem(progressStateRoot.GetChild(i).gameObject);
                }
            }

            isInitialized = true;
        }

        void ContinueClick()
        {
            HandleActionButtonClick();
        }

        void GenerateMoreBtnClick()
        {
            HandleActionButtonClick();
        }

        void TestSubmitFirstWithdrawClick()
        {
            if (isSubmittingWithdrawRequest)
                return;

            SoundManager.Instance?.PlayUIClickSFX();
            StartCoroutine(SubmitStage3WithdrawTestCoroutine());
        }

        void HandleActionButtonClick()
        {
            if (isSubmittingWithdrawRequest)
                return;

            var gm = GameManagerWZ.instance;
            // 第2关引导后的 Mission：Continue 后再加载关卡。
            if (gm != null && gm.DeferHostLevelLoad)
            {
                UIManager.instance?.CloseView(EUIType.WithdrawMissionView);
                gm.ResumeDeferredHostLevelLoad();
                return;
            }

            if (gm == null || gm.currentStage != Stage3Id || !gm.IsStageCompleted(Stage3Id))
            {
                UIManager.instance?.CloseView(EUIType.WithdrawMissionView);
                return;
            }

            CompleteStage3WithdrawIfReady();
        }

        void RefreshView()
        {
            var gm = GameManagerWZ.instance;
            if (gm == null)
                return;

            RefreshTime();
            RefreshMoney(gm);
            RefreshMissionState(gm);
            RefreshTestFirstWithdrawButton(gm);
        }

        void RefreshTime()
        {
            if (timeText == null)
                return;

            if (WithdrawApiService.TryGetFirstAdOrderCreateTime(out DateTimeOffset firstOrderTime))
            {
                timeText.text = firstOrderTime.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
                return;
            }

            timeText.text = EmptyPayPalValue;
        }

        void RefreshMoney(GameManagerWZ gm)
        {
            if (curCoinText == null || gm == null)
                return;

            curCoinText.text = GetDisplayedWithdrawText(gm);
        }

        void RefreshMissionState(GameManagerWZ gm)
        {
            int currentStage = Mathf.Max(gm.currentStage, 1);

            if (currentStage == Stage3Id)
            {
                RefreshStage3Mission(gm);
                return;
            }

            if (currentStage == Stage4Id || currentStage == Stage5Id)
            {
                RefreshReservedMission(gm, currentStage);
                return;
            }

            RefreshDefaultMission();
        }

        void RefreshStage3Mission(GameManagerWZ gm)
        {
            int missionTarget = GetStageTarget(gm, Stage3Id, DefaultAdMissionTarget);
            int watchedAds = Mathf.Max(gm.GetStageAdWatchCount(), 0);
            int clampedAds = Mathf.Clamp(watchedAds, 0, missionTarget);
            int remainingAds = Mathf.Max(missionTarget - watchedAds, 0);
            bool isCompleted = remainingAds <= 0;

            RestoreStaticTexts();
            SetStage3StateText(isCompleted);
            SetMissionDescription(GetStage3MissionDescription(gm, clampedAds, missionTarget));
            SetQueueText(GetStage3SecondaryText(gm, clampedAds, missionTarget), ShouldShowStage3SecondaryText(clampedAds, missionTarget));
            SetGenerateMoreState(isCompleted);
            SetProgressState(clampedAds, missionTarget);
        }

        void RefreshReservedMission(GameManagerWZ gm, int stageId)
        {
            RestoreStaticTexts();
            SetMissionDescription(GetStageObjectiveText(gm, stageId));
            SetGenerateMoreState(false);
            SetProgressState(0, VisibleStageCount);
        }

        void RefreshDefaultMission()
        {
            RestoreStaticTexts();
            SetMissionDescription(defaultDecValue);
            SetGenerateMoreState(false);
            SetProgressState(0, VisibleStageCount);
        }

        void RestoreStaticTexts()
        {
            if (stateText != null && !string.IsNullOrEmpty(defaultStateValue))
                stateText.text = defaultStateValue;

            if (queueSizeText != null)
            {
                queueSizeText.gameObject.SetActive(!string.IsNullOrEmpty(defaultQueueValue));
                if (!string.IsNullOrEmpty(defaultQueueValue))
                    queueSizeText.text = defaultQueueValue;
            }
        }

        void SetMissionDescription(string description)
        {
            if (decText == null)
                return;

            decText.text = string.IsNullOrEmpty(description) ? defaultDecValue : description;
        }

        void SetQueueText(string description, bool isVisible)
        {
            if (queueSizeText == null)
                return;

            queueSizeText.gameObject.SetActive(isVisible);
            if (isVisible)
                queueSizeText.text = description;
        }

        void SetStage3StateText(bool isCompleted)
        {
            if (stateText == null)
                return;

            stateText.text = isCompleted ? GetText("3032", "success") : GetText("3029", "In queue");
        }

        string GetStage3MissionDescription(GameManagerWZ gm, int watchedAds, int missionTarget)
        {
            int safeTarget = Mathf.Max(missionTarget, 1);
            int clampedAds = Mathf.Clamp(watchedAds, 0, safeTarget);
            int remainingAds = Mathf.Max(safeTarget - clampedAds, 0);
            if (remainingAds <= 0)
            {
                string successText = string.Format(GetText("3085", "{0} successful authentications: Funds will be transferred immediately."), safeTarget);
                return $"{successText}\n{GetStage3PayPalValue(gm)}";
            }

            if (clampedAds <= 0)
                return string.Format(GetText("3084", "Watch ads to complete active verification. Watch {0} times for direct payout."), safeTarget);

            return string.Format(GetText("3043", "Successful authentication {0} times: Withdrawal amount +0.1"), clampedAds);
        }

        bool ShouldShowStage3SecondaryText(int watchedAds, int missionTarget)
        {
            int safeTarget = Mathf.Max(missionTarget, 1);
            int clampedAds = Mathf.Clamp(watchedAds, 0, safeTarget);
            int remainingAds = Mathf.Max(safeTarget - clampedAds, 0);
            return remainingAds > 0;
        }

        string GetStage3SecondaryText(GameManagerWZ gm, int watchedAds, int missionTarget)
        {
            int safeTarget = Mathf.Max(missionTarget, 1);
            int clampedAds = Mathf.Clamp(watchedAds, 0, safeTarget);
            int remainingAds = Mathf.Max(safeTarget - clampedAds, 0);
            if (remainingAds <= 0)
                return string.Empty;

            if (clampedAds <= 0)
                return GetStage3PayPalText(gm);

            return string.Format(GetText("3044", "Watch {0} more times for immediate arrival."), remainingAds);
        }

        string GetStage3PayPalText(GameManagerWZ gm)
        {
            return string.Format(GetText("3086", "PayPal account: {0}"), GetStage3PayPalValue(gm));
        }

        string GetStage3PayPalValue(GameManagerWZ gm)
        {
            string email = gm != null ? gm.getPlayerEmail() : string.Empty;
            return string.IsNullOrEmpty(email) ? EmptyPayPalValue : email;
        }

        void SetGenerateMoreState(bool isReady)
        {
            if (generateMoreBtn == null)
                return;

            generateMoreBtn.gameObject.SetActive(isReady);
            generateMoreBtn.interactable = isReady;
        }

        void RefreshTestFirstWithdrawButton(GameManagerWZ gm)
        {
            if (testFirstWithdrawBtn == null)
                return;

            bool isVisible = GameDefines.EnableWithdrawMissionFirstAdTestButton && gm != null && gm.currentStage == Stage3Id;
            if (testFirstWithdrawBtn.gameObject.activeSelf != isVisible)
                testFirstWithdrawBtn.gameObject.SetActive(isVisible);

            testFirstWithdrawBtn.interactable = isVisible && !isSubmittingWithdrawRequest;
        }

        void SetProgressState(int completedCount, int targetCount)
        {
            int safeTarget = Mathf.Max(targetCount, 1);
            int clampedCompletedCount = Mathf.Clamp(completedCount, 0, safeTarget);
            int visibleCompletedCount = Mathf.Clamp(completedCount, 0, progressStates.Length);

            for (int i = 0; i < progressStates.Length; i++)
                progressStates[i]?.SetState(i + 1, i < visibleCompletedCount);

            if (progress != null)
                progress.fillAmount = ResolveProgressFillAmount(clampedCompletedCount, safeTarget);
        }

        float ResolveProgressFillAmount(int completedCount, int targetCount)
        {
            int safeTarget = Mathf.Max(targetCount, 1);
            int clampedCompletedCount = Mathf.Clamp(completedCount, 0, safeTarget);
            if (clampedCompletedCount <= 1 || safeTarget <= 1)
                return 0f;

            return Mathf.Clamp01((float)(clampedCompletedCount - 1) / (safeTarget - 1));
        }

        int GetStageTarget(GameManagerWZ gm, int stageId, int fallbackValue)
        {
            if (gm != null
                && gm.TryGetStageRequirement(stageId, out int stageType, out int stageTarget)
                && stageType == AdMissionType
                && stageTarget > 0)
            {
                return stageTarget;
            }

            return fallbackValue;
        }

        string GetStageObjectiveText(GameManagerWZ gm, int stageId)
        {
            if (gm == null)
                return string.Empty;

            var stageData = gm.GetStageData(stageId);
            if (stageData == null || !stageData.ContainsKey("LevelObjectives"))
                return string.Empty;

            string textKey = stageData["LevelObjectives"] as string;
            if (string.IsNullOrEmpty(textKey))
                return string.Empty;

            string localizedText = GetText(textKey, string.Empty);
            if (string.IsNullOrEmpty(localizedText))
                return string.Empty;

            int targetValue = 0;
            if (stageData.ContainsKey("Value"))
                int.TryParse(stageData["Value"] as string, out targetValue);

            try
            {
                return targetValue > 0 ? string.Format(localizedText, targetValue) : localizedText;
            }
            catch
            {
                return localizedText;
            }
        }

        string FormatCoin(float coin)
        {
            if (FacadePayTypeExtend.RegionalChangeHandle != null)
                return FacadePayTypeExtend.RegionalChangeHandle(coin);

            var gm = GameManagerWZ.instance;
            if (gm == null)
                return coin.ToString("F2");

            return $"{gm.mark}{coin.ToString($"F{gm.decimals}")}";
        }

        float GetDisplayedWithdrawAmount(GameManagerWZ gm)
        {
            if (gm == null)
                return 0f;

            if (gm.currentStage == Stage3Id)
            {
                return GetStage3WithdrawAmount(gm);
            }

            return gm.currentCoin;
        }

        float GetStage3WithdrawAmount(GameManagerWZ gm)
        {
            int missionTarget = GetStageTarget(gm, Stage3Id, DefaultAdMissionTarget);
            int rewardableWatchCount = Mathf.Max(missionTarget - 1, 0);
            int watchedAds = gm != null ? Mathf.Max(gm.GetStageAdWatchCount(), 0) : 0;
            int rewardedAds = Mathf.Clamp(watchedAds, 0, rewardableWatchCount);
            return GameDefines.AddCoin + rewardedAds * Stage3AdDisplayIncrease;
        }

        string GetDisplayedWithdrawText(GameManagerWZ gm)
        {
            if (gm == null)
                return "0";

            if (gm.currentStage != Stage3Id)
                return FormatCoin(GetDisplayedWithdrawAmount(gm));

            float currentAmount = GetDisplayedWithdrawAmount(gm);
            int watchedAds = Mathf.Max(gm.GetStageAdWatchCount(), 0);
            if (watchedAds <= 0)
                return FormatPlainCoin(GameDefines.AddCoin);

            int missionTarget = GetStageTarget(gm, Stage3Id, DefaultAdMissionTarget);
            int rewardableWatchCount = Mathf.Max(missionTarget - 1, 0);
            if (watchedAds <= rewardableWatchCount)
            {
                float previousAmount = Mathf.Max(currentAmount - Stage3AdDisplayIncrease, 0f);
                return $"{FormatPlainCoin(previousAmount)}{FormatStage3IncreaseText()}";
            }

            return FormatPlainCoin(currentAmount);
        }

        string FormatStage3IncreaseText()
        {
            return $"<color={Stage3AdIncreaseColor}>+{FormatPlainCoin(Stage3AdDisplayIncrease)}</color>";
        }

        string FormatPlainCoin(float coin)
        {
            string formatted = coin.ToString("F2");
            return formatted.TrimEnd('0').TrimEnd('.');
        }

        string GetText(string key, string fallback)
        {
            if (LocalizationManager.Instance == null)
                return fallback;

            string value = LocalizationManager.Instance.GetText(key);
            return string.IsNullOrEmpty(value) ? fallback : value;
        }

        WithdrawMissionStateItem GetOrAddStateItem(GameObject target)
        {
            if (target == null)
                return null;

            WithdrawMissionStateItem component = target.GetComponent<WithdrawMissionStateItem>();
            if (component == null)
                component = target.AddComponent<WithdrawMissionStateItem>();

            return component;
        }

        void CompleteStage3WithdrawIfReady()
        {
            var gm = GameManagerWZ.instance;
            if (gm == null || gm.currentStage != Stage3Id || !gm.IsStageCompleted(Stage3Id))
                return;

            if (TryOpenStage3EmailInputIfNeeded(gm))
                return;

            StartCoroutine(SubmitStage3WithdrawCoroutine(gm));
        }

        bool TryOpenStage3EmailInputIfNeeded(GameManagerWZ gm)
        {
            if (gm == null || !string.IsNullOrEmpty(gm.getPlayerEmail()))
                return false;

            GameApp.viewManager?.Open(ViewType.WithDrawMethod, CreateStage3EmailInputDataModel(gm));
            return true;
        }

        DataModel CreateStage3EmailInputDataModel(GameManagerWZ gm)
        {
            return new DataModel
            {
                Level = gm != null ? gm.currentLv : 0,
                Num = Stage3Id,
                Money = GetDisplayedWithdrawAmount(gm),
                Name = gm != null ? gm.getPlayerName() : string.Empty,
                Phone = string.Empty,
                Email = gm != null ? gm.getPlayerEmail() : string.Empty,
                Index = 3,
                CloseType = 1,
                Special = DataModel.SpecialWithdrawMissionBindEmail,
                UseProcessViewOnMethodExit = false,
                SubmitRetryWithdrawOnMethodExit = false,
                RetryWithdraw = false,
                CloseWithoutAdvance = true
            };
        }

        IEnumerator SubmitStage3WithdrawCoroutine(GameManagerWZ gm)
        {
            if (gm == null)
                yield break;

            isSubmittingWithdrawRequest = true;
            SetActionButtonsInteractable(false);

            HistoryModel pendingHistory = GetPendingStage2History();
            double withdrawAmount = GetDisplayedWithdrawAmount(gm);

            WithdrawApiService.EnsureQueuedFirstAdOrder(withdrawAmount);
            WithdrawApiService.MarkQueuedFirstAdOrderUnderReview();

            if (!GameDefines.EnableWithdrawMissionServerRequest)
            {
                CompleteStage3WithdrawLocally(gm, pendingHistory, withdrawAmount, null);
                yield break;
            }

            ClientWithdrawRequest request = BuildStage3WithdrawRequest(gm, withdrawAmount);
            if (request == null)
            {
                Debug.LogWarning("Stage3 FIRST_AD withdraw request skipped: PayPal receiver is empty.");
                CompleteStage3WithdrawLocally(gm, pendingHistory, withdrawAmount, null);
                yield break;
            }

            Debug.Log($"Stage3 FIRST_AD withdraw request moved to background: amount={withdrawAmount:F2}, receiver={request.Receiver}");
            gm.StartCoroutine(SubmitStage3WithdrawBackgroundCoroutine(request));
            CompleteStage3WithdrawLocally(gm, pendingHistory, withdrawAmount, null);
        }

        static IEnumerator SubmitStage3WithdrawBackgroundCoroutine(ClientWithdrawRequest request)
        {
            if (request == null)
                yield break;

            bool requestCompleted = false;
            yield return WithdrawApiService.SubmitWithdraw(request, order =>
            {
                requestCompleted = true;
                Debug.Log($"Stage3 FIRST_AD background request succeeded: orderNo={order?.OrderNo}, status={order?.OrderStatus}");
                PushApiService.ReportWithdrawStage3Apply();

                if (order != null)
                    WithdrawApiService.ReplaceQueuedFirstAdOrder(order);
            }, error =>
            {
                requestCompleted = true;
                Debug.LogWarning($"Stage3 FIRST_AD background request failed: {error}");
            });

            if (!requestCompleted)
                Debug.LogWarning("Stage3 FIRST_AD background request failed: request did not complete.");
        }

        IEnumerator SubmitStage3WithdrawTestCoroutine()
        {
            GameManagerWZ gm = GameManagerWZ.instance;
            if (gm == null)
                yield break;

            isSubmittingWithdrawRequest = true;
            SetActionButtonsInteractable(false);

            HistoryModel pendingHistory = GetPendingStage2History();
            double withdrawAmount = GetDisplayedWithdrawAmount(gm);

            ClientWithdrawRequest request = BuildStage3WithdrawRequest(gm, withdrawAmount);
            if (request == null)
            {
                isSubmittingWithdrawRequest = false;
                SetActionButtonsInteractable(true);
                Debug.LogWarning("Stage3 FIRST_AD test request skipped: PayPal receiver is empty.");
                WithdrawApiService.LogPaymentNotice("WithdrawMissionView", TestSubmitMissingPayPalText, LogType.Warning);
                yield break;
            }

            bool requestCompleted = false;
            Debug.Log($"Stage3 FIRST_AD test request start: amount={withdrawAmount:F2}, receiver={request.Receiver}");

            yield return WithdrawApiService.SubmitWithdraw(request, order =>
            {
                requestCompleted = true;
                isSubmittingWithdrawRequest = false;
                SetActionButtonsInteractable(true);
                Debug.Log($"Stage3 FIRST_AD test request succeeded: orderNo={order?.OrderNo}, status={order?.OrderStatus}");
                WithdrawApiService.LogPaymentNotice("WithdrawMissionView", $"首提测试成功: {order?.OrderNo ?? "--"}", LogType.Log);
            }, error =>
            {
                requestCompleted = true;
                isSubmittingWithdrawRequest = false;
                SetActionButtonsInteractable(true);
                Debug.LogWarning($"Stage3 FIRST_AD test request failed: {error}");
                WithdrawApiService.LogPaymentNotice("WithdrawMissionView", error, LogType.Warning);
            });

            if (!requestCompleted)
            {
                isSubmittingWithdrawRequest = false;
                SetActionButtonsInteractable(true);
                Debug.LogWarning("Stage3 FIRST_AD test request failed: request did not complete.");
                WithdrawApiService.LogPaymentNotice("WithdrawMissionView", TestSubmitTimeoutText, LogType.Warning);
            }
        }

        void CompleteStage3WithdrawLocally(GameManagerWZ gm, HistoryModel pendingHistory, double withdrawAmount, WithdrawOrderData order)
        {
            isSubmittingWithdrawRequest = false;
            SetActionButtonsInteractable(true);

            if (order != null)
                WithdrawApiService.ReplaceQueuedFirstAdOrder(order);

            MarkStage2HistorySubmitted(pendingHistory);
            DeductCurrentWithdrawMoney(gm, (float)withdrawAmount);
            bool shouldShowWithdrawFeedbackView = ShouldShowWithdrawFeedbackViewAfterStage3(gm);
            UIManager.instance?.CloseView(EUIType.WithdrawMissionView);
            if (shouldShowWithdrawFeedbackView)
            {
                UIManager.instance?.ShowView(EUIType.WithdrawFeedbackView);
            }
        }

        bool ShouldShowWithdrawFeedbackViewAfterStage3(GameManagerWZ gm)
        {
            return !GameDefines.ifIAA
                && gm != null
                && gm.currentStage == Stage3Id
                && gm.IsStageCompleted(Stage3Id);
        }

        ClientWithdrawRequest BuildStage3WithdrawRequest(GameManagerWZ gm, double withdrawAmount)
        {
            string receiver = gm != null ? gm.getPlayerEmail() : string.Empty;
            if (string.IsNullOrEmpty(receiver))
                return null;

            return new ClientWithdrawRequest
            {
                WithdrawType = WithdrawConstants.WithdrawTypeFirstAd,
                Receiver = receiver,
                Amount = Math.Round(withdrawAmount, 2),
                Currency = WithdrawConstants.CurrencyUsd,
                Note = "Stage3 first ad withdraw",
                EmailSubject = string.Empty,
                RecipientType = WithdrawConstants.RecipientTypeEmail
            };
        }

        HistoryModel GetPendingStage2History()
        {
            List<HistoryModel> historyModels = SPlayerPrefs.GetListTem<HistoryModel>(FacadePlayerPrefExtend.History);
            if (historyModels == null)
                return null;

            for (int i = historyModels.Count - 1; i >= 0; i--)
            {
                HistoryModel historyModel = historyModels[i];
                if (historyModel != null && historyModel.Stage == 2 && historyModel.Status != 1)
                    return historyModel;
            }

            return null;
        }

        void MarkStage2HistorySubmitted(HistoryModel pendingHistory)
        {
            List<HistoryModel> historyModels = SPlayerPrefs.GetListTem<HistoryModel>(FacadePlayerPrefExtend.History);
            if (historyModels == null)
                return;

            for (int i = historyModels.Count - 1; i >= 0; i--)
            {
                HistoryModel historyModel = historyModels[i];
                if (historyModel == null || historyModel.Stage != 2 || historyModel.Status == 1)
                    continue;

                if (pendingHistory == null || string.IsNullOrEmpty(pendingHistory.ID) || historyModel.ID == pendingHistory.ID)
                {
                    historyModel.Status = 1;
                    break;
                }
            }

            SPlayerPrefs.SetListTem(FacadePlayerPrefExtend.History, historyModels);
        }

        void HandleStage3WithdrawError(string error)
        {
            isSubmittingWithdrawRequest = false;
            SetActionButtonsInteractable(true);

            if (!string.IsNullOrEmpty(error))
                Debug.LogWarning($"Stage3 FIRST_AD withdraw error: {error}");

            UIManager.instance?.CloseView(EUIType.WithdrawMissionView);
        }

        void SetActionButtonsInteractable(bool isInteractable)
        {
            if (continueBtn != null)
                continueBtn.interactable = isInteractable;

            if (generateMoreBtn != null)
                generateMoreBtn.interactable = isInteractable;

            if (testFirstWithdrawBtn != null)
                testFirstWithdrawBtn.interactable = isInteractable;
        }

        void DeductCurrentWithdrawMoney(GameManagerWZ gm, float withdrawMoney)
        {
            if (withdrawMoney <= 0f)
                return;

            gm.AddCoinTem(-withdrawMoney);
            gm.UpdateCoinUI();
        }

        void EnsureTestFirstWithdrawButton()
        {
            if (!GameDefines.EnableWithdrawMissionFirstAdTestButton)
                return;

            if (testFirstWithdrawBtn != null)
                return;

            Transform existingButton = FindChildUtility.FindChild(gameObject, TestSubmitButtonName)?.transform;
            if (existingButton != null)
            {
                testFirstWithdrawBtn = existingButton.GetComponent<Button>();
                ConfigureTestFirstWithdrawButton(existingButton.gameObject);
                return;
            }

            if (continueBtn == null)
                return;

            GameObject buttonObject = Instantiate(continueBtn.gameObject, continueBtn.transform.parent);
            buttonObject.name = TestSubmitButtonName;
            buttonObject.SetActive(true);
            testFirstWithdrawBtn = buttonObject.GetComponent<Button>();

            RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
            RectTransform continueRect = continueBtn.GetComponent<RectTransform>();
            if (buttonRect != null && continueRect != null)
            {
                buttonRect.anchorMin = continueRect.anchorMin;
                buttonRect.anchorMax = continueRect.anchorMax;
                buttonRect.pivot = continueRect.pivot;
                buttonRect.sizeDelta = continueRect.sizeDelta;
                buttonRect.anchoredPosition = continueRect.anchoredPosition + new Vector2(0f, TestButtonYOffset);
            }

            ConfigureTestFirstWithdrawButton(buttonObject);
        }

        void ConfigureTestFirstWithdrawButton(GameObject buttonObject)
        {
            if (buttonObject == null)
                return;

            Text buttonText = buttonObject.GetComponentInChildren<Text>(true);
            if (buttonText == null)
                return;

            LocalizedText localizedText = buttonText.GetComponent<LocalizedText>();
            if (localizedText != null)
            {
                localizedText.textId = string.Empty;
                localizedText.enabled = false;
            }

            buttonText.text = TestSubmitButtonText;
            buttonText.resizeTextForBestFit = true;
            buttonText.fontSize = 52;
        }
    }
}
