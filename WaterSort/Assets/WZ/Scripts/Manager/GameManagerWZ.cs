
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static WZSDK.Tutorial;
using Random = UnityEngine.Random;

namespace WZSDK
{
    public class GameManagerWZ : MonoBehaviour
    {
        #region 组件引用
        public LevelGenerator levelGen;
        private Tutorial tutorial;
        public UIManager uiManager;
        public GameObject NetworkErrorTip;
        public GameObject bg;

        public GameSceneWZ GameScene;
        #endregion

        #region 玩家数据
        public string userName;
        public string userID;
        public int userLevel;
        public int userExp;
        #endregion

        #region 资源数据
        [HideInInspector] public float currentCoin;
        [HideInInspector] public float currentGems;
        #endregion

        #region 道具数据
        [HideInInspector] public int currentUndo;
        [HideInInspector] public int currentClear;
        [HideInInspector] public int currentAddBottleCount;
        #endregion

        #region 关卡/阶段数据
        [HideInInspector] public int currentLv;
        [HideInInspector] public int currentStage;
        [HideInInspector] public int maxUnlockedStage;
        [HideInInspector] public int currentStageAdWatchCount;
        [HideInInspector] public int currentStageAdWatchOwnerStage;
        [HideInInspector] public int currentBottleFull;
        [HideInInspector] public int completedAnimationBottles;
        private int lastCompletedLevel;
        #endregion

        #region 登录阶段进度
        public int stage5CurrentCycle = 0;
        #endregion

        #region 配置标志
        public bool useCustomIAAValue;
        public bool ifIAA;
        #endregion

        #region 货币格式
        private float exchangeRate = 1f;
        public int decimals = 2;
        public string mark = "$";
        #endregion

        #region 消除计数
        private int currentEliminationCount;
        private int totalEliminationCount;
        #endregion

        #region 定时器相关
        private STimer pouringTimer;
        private bool isPouringTimerActive;
        private STimer levelTimer;
        private bool isLevelTimerActive;
        private bool canTriggerReward;
        private bool isLevelTimerPausedByUI;
        #endregion

        #region UI队列
        private Queue<EUIType> pendingUIQueue = new Queue<EUIType>();
        private bool isProcessingUIQueue = false;
        private bool isProcessingStage4SlotFlow = false;
        #endregion

        #region 启动流程
        private bool startupFlowStarted;
        private bool serverConfigRequestStarted;
        private bool serverConfigResolved;
        private bool loadingCompletionTriggered;
        private bool loadingCompleted;
        private LoadingView activeLoadingView;
        /// <summary>
        /// 第2关阶段完成后：先走 CMBtn 引导，再弹 Mission，最后才 CreateLevel。
        /// </summary>
        private bool stage2CashGuidePending;
        private bool deferHostLevelLoad;

        public bool Stage2CashGuidePending => stage2CashGuidePending;
        public bool DeferHostLevelLoad => deferHostLevelLoad;

        /// <summary>
        /// Stage3 广告次数达标后：先弹 LuckyWallet，确认后再弹 WithdrawMissionView。
        /// </summary>
        private bool pendingStage3WithdrawMissionAfterLuckyWallet;

        /// <summary>
        /// Stage3 达标链路进行中（LuckyWallet→Mission→Feedback→Slot），禁止自动进 Stage4 / 抢弹。
        /// </summary>
        private bool stage3CompletionFlowActive;

        /// <summary>
        /// Stage3 达标广告是否来自通关激励视频；Slot 流程结束后若为 true 则 NextLevel。
        /// </summary>
        private bool stage3CompletionFromLevelClearAd;

        public bool PendingStage3WithdrawMissionAfterLuckyWallet => pendingStage3WithdrawMissionAfterLuckyWallet;
        public bool Stage3CompletionFromLevelClearAd => stage3CompletionFromLevelClearAd;
        public bool IsStage3CompletionFlowActive =>
            stage3CompletionFlowActive
            || pendingStage3WithdrawMissionAfterLuckyWallet
            || stage3CompletionFromLevelClearAd;

        /// <summary>
        /// 阶段兑现（如 Goal1 WithDraw）已接管过关表现时，跳过紧随其后的通关成功页。
        /// </summary>
        private bool replaceSuccessWithWithdraw;

        /// <summary>
        /// 递增后作废仍在等待的 ShowSuccessViewCoroutine，避免 WithDraw 关闭后又弹出通关页。
        /// </summary>
        private int successViewEpoch;
        #endregion

        #region 事件回调
        private System.Action<int, int> OnStageUpdated;
        private System.Action OnCoinChanged;
        private System.Action OnLevelChanged;
        private System.Action OnLoginDayChanged;
        private System.Action OnAdWatchCountChanged;
        #endregion

        #region 枚举与单例
        public enum GAME_STATE { WAIT, PLAYING, FINISH }
        [HideInInspector] public GAME_STATE currentState;
        public static GameManagerWZ instance;
        private const int Stage3Id = 3;
        private const int Stage4Id = 4;
        private const int Stage5Id = 5;
        private const int NoCoinRewardEndLevel = 2;
        private const float Stage3AdRewardAmount = 0.1f;
        private const float Stage4SlotRewardAmount = 50f;
        private const float NormalCoinHardCap = 2999.5f;
        public const float Stage5CompensationTargetCoin = 2700f;
        private const int ActiveStageSystemVersion = 2;
        private const string AutoNextStageField = "AutoNextStage";
        #endregion

        #region 生命周期
        void Awake()
        {
            instance = this;
            Application.targetFrameRate = 60;
            GameObject.DontDestroyOnLoad(this);
        }

        void Start()
        {
            SetNetworkErrorTipVisible(false);
            if (WaterSortWZBridge.HostDriven)
            {
                // 宿主驱动：跳过 WZ 独立 Loading/玩法壳，但仍拉配置并初始化数据/SDK。
                if (startupFlowStarted)
                    return;
                startupFlowStarted = true;
                if (bg != null)
                    bg.SetActive(false);
                // 保留 Main Camera 作为 Canvas 渲染相机，并补齐宿主所需的 UICamera 节点名。
                WaterSortWZBridge.EnsureHostUiCamera();
                RequestServerConfig();
                return;
            }
            StartGame();
        }
        public void StartGame()
        {
            if (startupFlowStarted)
                return;

            startupFlowStarted = true;
            ShowLoadingScreen();
        }
        void OnServerResponse(bool iaaValue, bool isVersionMatch, bool forcedUpload)
        {
            if (serverConfigResolved)
                return;

            serverConfigResolved = true;
            FacadeAppsFlyerExtend.NotifyUploadPolicyResolved(forcedUpload);//
            Debug.Log($"[服务器模式] forcedUpload={forcedUpload}");
            if (WaterSortWZBridge.HostDriven)
                GameDefines.ifIAA = global::GameDefines.ifIAA;
            else
                GameDefines.ifIAA = useCustomIAAValue ? ifIAA : (isVersionMatch && iaaValue);
            InitGame();
            CompleteLoadingScreen();
        }

        void ShowLoadingScreen()
        {
            UIManager.instance.ShowView(EUIType.LoadingView, (view) =>
            {
                activeLoadingView = view as LoadingView;
                bg.gameObject.SetActive(false);
       
                TDAnalyticsMgr.Instance.LoadingStart();
                activeLoadingView?.StartSimulatedProgress();
                RequestServerConfig();
            });
        }

        void RequestServerConfig()
        {
            if (serverConfigRequestStarted)
                return;

            serverConfigRequestStarted = true;
            NetworkFrame.Instance.FetchServerData(GameDefines.URL, OnServerResponse);
        }

        void CompleteLoadingScreen()
        {
            if (loadingCompletionTriggered)
                return;

            loadingCompletionTriggered = true;
            if (activeLoadingView != null)
            {
                activeLoadingView.CompleteProgress(OnLoadingComplete);
                return;
            }

            OnLoadingComplete();
        }

        void OnLoadingComplete()
        {
            if (loadingCompleted)
                return;

            loadingCompleted = true;
            activeLoadingView = null;
            InitUI();
            UIManager.instance.CloseView(EUIType.LoadingView);
         
            //LevelManager.Instance.LoadCurrentLevel();
            TDAnalyticsMgr.Instance.LoadFinish();
        }

        void Update()
        {
            STimerManager.Instance.UpdateInstance();
            ManageTimerBasedOnUI();
        }

        void OnDestroy()
        {
            UnregisterStageEvents();
            ClearFacadeUserHandles();
        }

        void SetNetworkErrorTipVisible(bool visible)
        {
            if (NetworkErrorTip != null)
                NetworkErrorTip.SetActive(visible);
        }
        #endregion


        /// <summary>
        /// 设置最大关卡数
        /// </summary>
        /// <returns></returns>
        int GetTotalLevels()
        {
            return 30;
            /* TextAsset[] allLevels = Resources.LoadAll<TextAsset>("Level");
             return allLevels.Length;*/
        }

        /// <summary>
        /// 关卡完成调用
        /// </summary>
        public void CheckIfAllAnimationsCompleted()
        {
            currentState = GAME_STATE.FINISH;
            StopLevelTimer();
            ResetPouringTimer();
            ShowFinishLevel();
            TDAnalyticsMgr.Instance.LevelComplete(currentLv);
        }


        /// <summary>
        /// 每次消除后调用判断是否30秒打开RewardView面板
        /// </summary>
        public void OnPourCompleteForLuckyReward()
        {
            if (!GameDefines.EnableLuckyRewardOnPourComplete) return;
            if (currentState == GAME_STATE.FINISH || UIManager.instance.HasActiveView(EUIType.RewardView) ||
                HasActiveSuccessView() || currentLv <= GameDefines.CloseInt) return;
            CheckPourCompleteForReward();
        }


        /// <summary>
        /// 消除多少次弹转盘奖励
        /// </summary>
        /// <param name="count"></param>
        public void AddEliminationCount(int count = 1)
        {
            if (count <= 0) return;

            currentEliminationCount += count;
            totalEliminationCount += count;
            SaveEliminationCount();
            CheckLuckySpinCondition();
        }
        /// <summary>
        /// 消除添加经验
        /// </summary>
        /// <param name="value"></param>
        public void AddExp(int value)
        {
            if (value <= 0) return;
            UserDataManager.Instance.AddExp(value);
            userExp = UserDataManager.Instance.GetExp();
            userLevel = UserDataManager.Instance.GetUserLevel();
        }

   




        #region 初始化
        void InitGame()
        {
            currentState = GAME_STATE.WAIT;
            currentBottleFull = 0;
            completedAnimationBottles = 0;

            SetFirstData();
            GetCurrentLevel();
            LoadEliminationCount();
            LoadStageData();
            LoadStageAdWatchData();
            LoadStage5CycleData();
            CheckDailyLogin();
            RegisterStageEvents();
            RegisterFacadeUserHandles();
            WaterSortWZBridge.InitializeOrSync();
            FacadePayTypeExtend.RegionalChangeHandle += RegionalChange;
            exchangeRate = 1;
            OnLoginDayChanged?.Invoke();
            CheckCoinStageProgress();
        }

        void InitUI()
        {
            if (WaterSortWZBridge.HostDriven)
            {
                TDAnalyticsMgr.Instance.EnterMainUI();
                TDAnalyticsMgr.Instance.LoginSuccess();
                // 宿主负责玩法 HUD；仅保留特效层供 WZ 奖励飞行动画使用。
                UIManager.instance.ShowView(EUIType.UIEffectView);
                return;
            }

            // UseHostGameplayHud：主界面走宿主 UIGamePlay，不再打开 GamePlayerView/GameView。
            if (WaterSortWZBridge.UsesWzGameplayHud)
                UIManager.instance.ShowView(GameDefines.ifIAA ? EUIType.GameView : EUIType.GamePlayerView);
            else
            {
                WaterSortWZBridge.EnsureHostUiCamera();
                // 把 WZ 侧 ifIAA 同步到宿主，再刷新 UIGamePlay。
                WaterSortWZBridge.InitializeOrSync();
            }

            TDAnalyticsMgr.Instance.EnterMainUI();
            TDAnalyticsMgr.Instance.LoginSuccess();
            UIManager.instance.ShowView(EUIType.Tutorial);
            UIManager.instance.ShowView(EUIType.UIEffectView);
            tutorial = UIManager.instance.GetView<Tutorial>(EUIType.Tutorial);
            UIManager.instance.CloseView(EUIType.Tutorial);
            levelGen.InitLvGen();
            UpdateBoosterState();
            ResetLevelTimerState();
            StartLevelTimer();
            ResetPouringTimerState();
            TryShowStage5CompensationLuckySpin(currentStage);
            RefreshHostGameplayHud();
        }

        void SetFirstData()
        {
            if (!PlayerPrefs.HasKey(FacadePlayerPrefExtend.start))
            {
                PlayerPrefs.SetInt(FacadePlayerPrefExtend.start, 1);
                PlayerPrefs.SetInt(FacadePlayerPrefExtend.coin, 0);
                PlayerPrefs.SetInt(FacadePlayerPrefExtend.gems, 0);
                PlayerPrefs.SetInt(FacadePlayerPrefExtend.AddBottleCount, 1);
                PlayerPrefs.SetInt(FacadePlayerPrefExtend.ClearColorCount, 1);
                PlayerPrefs.SetInt(FacadePlayerPrefExtend.undo, 5);
                PlayerPrefs.SetInt(FacadePlayerPrefExtend.currentEliminationCount, 0);
                PlayerPrefs.SetInt(FacadePlayerPrefExtend.totalEliminationCount, 0);
                PlayerPrefs.SetInt(FacadePlayerPrefExtend.AddDay, 0);
                PlayerPrefs.SetInt(FacadePlayerPrefExtend.StageAdWatchCount, 0);
                PlayerPrefs.SetInt(FacadePlayerPrefExtend.StageAdWatchOwnerStage, 0);
            }

            userName = UserDataManager.Instance.GetUserName();
            userID = UserDataManager.Instance.GetUserID();
            userLevel = UserDataManager.Instance.GetUserLevel();
            userExp = UserDataManager.Instance.GetExp();

            currentCoin = NormalizeRegularCoin(PlayerPrefs.GetFloat(FacadePlayerPrefExtend.coin, 0));
            currentGems = PlayerPrefs.GetFloat(FacadePlayerPrefExtend.gems, 0);
            currentUndo = PlayerPrefs.GetInt(FacadePlayerPrefExtend.undo, 5);
            currentClear = PlayerPrefs.GetInt(FacadePlayerPrefExtend.ClearColorCount, 1);
            currentAddBottleCount = PlayerPrefs.GetInt(FacadePlayerPrefExtend.AddBottleCount, 1);
        }

        void GetCurrentLevel()
        {
            currentLv = PlayerPrefs.GetInt(FacadePlayerPrefExtend.currentLevel, 1);
        }

        void LoadStageData()
        {
            MigrateStageProgressIfNeeded();

            int defaultStage = GetStageData(1) == null ? 0 : 1;
            currentStage = PlayerPrefs.GetInt(FacadePlayerPrefExtend.Stage, defaultStage);
            if (currentStage <= 0 || GetStageData(currentStage) == null)
                currentStage = defaultStage;

            maxUnlockedStage = PlayerPrefs.GetInt(FacadePlayerPrefExtend.MaxUnlockedStage, currentStage);
            if (maxUnlockedStage < currentStage)
                maxUnlockedStage = currentStage;

            SaveCurrentStage();
            SaveMaxUnlockedStage();
            PlayerPrefs.Save();
        }

        void LoadStage5CycleData()
        {
            if (IsLoginStage(currentStage))
            {
                stage5CurrentCycle = PlayerPrefs.GetInt(FacadePlayerPrefExtend.Stage5_CurrentCycle, 0);
                PlayerPrefs.SetInt(FacadePlayerPrefExtend.AddDay, stage5CurrentCycle);
            }
            else
            {
                stage5CurrentCycle = 0;
            }
        }

        void LoadEliminationCount()
        {
            currentEliminationCount = PlayerPrefs.GetInt(FacadePlayerPrefExtend.currentEliminationCount, 0);
            totalEliminationCount = PlayerPrefs.GetInt(FacadePlayerPrefExtend.totalEliminationCount, 0);
        }

        void RegisterStageEvents()
        {
            OnStageUpdated += OnStageChanged;
            OnCoinChanged += OnCoinResourceChanged;
            OnLoginDayChanged += OnLoginDayProgressChanged;
            OnAdWatchCountChanged += OnAdWatchProgressChanged;
        }

        void UnregisterStageEvents()
        {
            OnStageUpdated -= OnStageChanged;
            OnCoinChanged -= OnCoinResourceChanged;
            OnLoginDayChanged -= OnLoginDayProgressChanged;
            OnAdWatchCountChanged -= OnAdWatchProgressChanged;
            FacadePayTypeExtend.RegionalChangeHandle -= RegionalChange;
        }
        #endregion

        #region 阶段系统
        void MigrateStageProgressIfNeeded()
        {
            if (PlayerPrefs.GetInt(FacadePlayerPrefExtend.StageSystemVersion, 0) >= ActiveStageSystemVersion)
                return;

            int maxConfiguredStageId = GetMaxConfiguredStageId();
            if (maxConfiguredStageId <= 0)
                return;

            int legacyStage = PlayerPrefs.GetInt(FacadePlayerPrefExtend.Stage, 0);
            int legacyMaxStage = PlayerPrefs.GetInt(FacadePlayerPrefExtend.MaxUnlockedStage, legacyStage);

            int migratedStage = Mathf.Clamp(NormalizeLegacyStage(legacyStage), 1, maxConfiguredStageId);
            int migratedMaxStage = Mathf.Clamp(NormalizeLegacyStage(legacyMaxStage), migratedStage, maxConfiguredStageId);

            PlayerPrefs.SetInt(FacadePlayerPrefExtend.Stage, migratedStage);
            PlayerPrefs.SetInt(FacadePlayerPrefExtend.MaxUnlockedStage, migratedMaxStage);
            PlayerPrefs.SetInt(FacadePlayerPrefExtend.StageSystemVersion, ActiveStageSystemVersion);
            PlayerPrefs.Save();
        }

        public void SaveCurrentStage() => PlayerPrefs.SetInt(FacadePlayerPrefExtend.Stage, currentStage);
        public void SaveMaxUnlockedStage() => PlayerPrefs.SetInt(FacadePlayerPrefExtend.MaxUnlockedStage, maxUnlockedStage);
        public Dictionary<string, object> GetStageData(int stageId) => UserDataManager.Instance.getStageData(stageId);

        int NormalizeLegacyStage(int legacyStage)
        {
            if (legacyStage <= 0) return 1;
            if (legacyStage <= 2) return legacyStage + 1;
            return legacyStage;
        }

        int GetMaxConfiguredStageId()
        {
            int maxStageId = 0;
            while (GetStageData(maxStageId + 1) != null)
                maxStageId++;

            return maxStageId;
        }

        public bool TryGetStageRequirement(int stageId, out int type, out int value)
        {
            type = 0;
            value = 0;

            var stageData = GetStageData(stageId);
            if (stageData == null || !stageData.ContainsKey("Type") || !stageData.ContainsKey("Value"))
                return false;

            return int.TryParse((string)stageData["Type"], out type)
                && int.TryParse((string)stageData["Value"], out value);
        }

        public bool ShouldAutoEnterNextStage(int stageId)
        {
            var stageData = GetStageData(stageId);
            if (stageData == null || !stageData.ContainsKey(AutoNextStageField))
                return true;

            string rawValue = stageData[AutoNextStageField] as string;
            if (string.IsNullOrEmpty(rawValue))
                return true;

            if (bool.TryParse(rawValue, out bool boolValue))
                return boolValue;

            if (int.TryParse(rawValue, out int intValue))
                return intValue != 0;

            return true;
        }

        bool IsLoginStage(int stageId)
        {
            return TryGetStageRequirement(stageId, out int type, out _) && type == 3;
        }

        bool IsCoinStage(int stageId)
        {
            return TryGetStageRequirement(stageId, out int type, out _) && type == 2;
        }

        bool IsAdWatchStage(int stageId)
        {
            return TryGetStageRequirement(stageId, out int type, out _) && type == 4;
        }

        void TryAutoEnterCompletedStage(int stageId)
        {
            if (GameDefines.ifIAA
                || stageId <= 0
                || currentStage != stageId
                || !ShouldAutoEnterNextStage(stageId)
                || !IsStageCompleted(stageId))
            {
                return;
            }

            // Stage3（看广告）达标后必须走 LuckyWallet→Mission→Feedback→Slot，禁止自动进 Stage4。
            if (stageId == Stage3Id || IsAdWatchStage(stageId))
                return;

            TryEnterNextStage();
        }

        void HandleCompletedStage(int completedStage)
        {
            double curCoin = Math.Round((double)currentCoin, decimals);

            if (completedStage == 1)
            {
                DataModel dataModel = new DataModel
                {
                    Level = currentLv - 1,
                    Money = curCoin,
                    Num = completedStage,
                    CloseType = 1,
                    HistoryId = string.Empty
                };

                // Goal1 兑现页替代通关成功页，避免两者叠弹。
                BeginWithdrawInsteadOfSuccess();
                GameApp.viewManager.Open(ViewType.WithDraw, dataModel);

                if (GameManagerWZ.instance.currentStage == 1 || GameManagerWZ.instance.currentStage == 2)
                {
                    StartCoroutine(ShowWithdrawTutorialType3Step0());
                }
                //GameScene.withDraw(currentLv - 1, curCoin, 1, ViewType.SuccessMessage, string.Empty, completedStage);
            }
            else if (completedStage == 2)
            {
                AddCoin(GameDefines.AddCoin);
                double stage2WithdrawAmount = GetCoin();
                GameScene?.setHistory(stage2WithdrawAmount, completedStage);
                WithdrawApiService.EnsureQueuedFirstAdOrder(stage2WithdrawAmount);

                // 顺序：CMBtn 引导 → WithdrawMissionView → 再 CreateLevel。
                stage2CashGuidePending = true;
                deferHostLevelLoad = true;
                StartCoroutine(ShowStage2CashOutTutorialWhenReady());
            }
        }

        /// <summary>
        /// Goal2 引导走完后：关引导、弹 Mission，仍不加载关卡。
        /// </summary>
        public void FinishStage2CashGuideAndShowMission()
        {
            stage2CashGuidePending = false;
            deferHostLevelLoad = true;
            CloseActiveTutorial();

            if (GameApp.viewManager != null)
            {
                if (GameApp.viewManager.IsOpen((int)ViewType.WithDraw))
                    GameApp.viewManager.Close((int)ViewType.WithDraw);
                if (GameApp.viewManager.IsOpen((int)ViewType.WithDrawMethod))
                    GameApp.viewManager.Close((int)ViewType.WithDrawMethod);
                if (GameApp.viewManager.IsOpen((int)ViewType.WithDrawConfirm))
                    GameApp.viewManager.Close((int)ViewType.WithDrawConfirm);
                if (GameApp.viewManager.IsOpen((int)ViewType.WithDrawProcess1))
                    GameApp.viewManager.Close((int)ViewType.WithDrawProcess1);
            }

            if (UIManager.instance != null
                && !UIManager.instance.HasActiveView(EUIType.WithdrawMissionView))
            {
                UIManager.instance.ShowView(EUIType.WithdrawMissionView);
            }
        }

        /// <summary>
        /// Mission Continue 后：真正加载宿主关卡。
        /// </summary>
        public void ResumeDeferredHostLevelLoad()
        {
            if (!deferHostLevelLoad && !stage2CashGuidePending)
                return;

            stage2CashGuidePending = false;
            deferHostLevelLoad = false;
            CloseActiveTutorial();

            int hostLevel = Mathf.Max(currentLv, 1);
            XrCode.FacadePlayer.SetLevel?.Invoke(hostLevel);
            XrCode.FacadeGamePlay.StartLevel?.Invoke();

            if (WaterSortWZBridge.UsesWzGameplayHud && uiManager != null)
            {
                if (GameDefines.ifIAA)
                    uiManager.GetView<GameView>(EUIType.GameView)?.InitView();
                else
                    uiManager.GetView<GamePlayerView>(EUIType.GamePlayerView)?.InitView();
            }

            UpdateBoosterState();
            StartLevelTimer();
            RefreshHostGameplayHud();
        }

        IEnumerator ShowStage2CashOutTutorialWhenReady()
        {
            // 先不要露出错误引导；通关页关掉后再绑 CMBtn。
            CloseActiveTutorial();

            // 成功页协程有约 1s 延迟：先等它出现（或确认不会弹），再等关闭。
            float appearWait = 0f;
            while (appearWait < 2.5f && !HasActiveSuccessView())
            {
                if (currentState != GAME_STATE.FINISH && appearWait > 0.35f)
                    break;
                appearWait += 0.1f;
                yield return new WaitForSeconds(0.1f);
            }

            float closeWait = 0f;
            while (closeWait < 30f
                   && (HasActiveSuccessView()
                       || IsCompletionFlowBlockedBySpecialView()
                       || IsWithdrawFlowUiOpen()))
            {
                closeWait += 0.1f;
                yield return new WaitForSeconds(0.1f);
            }

            // 再空两帧，吞掉关闭通关页残留的 PointerClick。
            yield return null;
            yield return null;

            // 若此时已在兑现/排队流程中，绝不再弹 CMBtn 引导。
            if (UIManager.instance == null || IsWithdrawFlowUiOpen())
                yield break;

            // 引导尚未完成才弹 CMBtn；Mission 已开则跳过。
            if (!stage2CashGuidePending
                || UIManager.instance.HasActiveView(EUIType.WithdrawMissionView))
                yield break;

            UIManager.instance.ShowView(EUIType.Tutorial);
            Tutorial tutorialView = UIManager.instance.GetView<Tutorial>(EUIType.Tutorial);
            if (tutorialView == null)
                yield break;

            if (!tutorialView.gameObject.activeSelf)
                tutorialView.gameObject.SetActive(true);

            tutorialView.currentType = TYPE.TYPE3;
            tutorialView.step = 3;
            tutorialView.GoToStep(3);
        }

        IEnumerator ShowWithdrawTutorialType3Step0()
        {
            // Wait one frame so WithDraw Open/layout finishes before guide binds to its button.
            yield return null;

            if (UIManager.instance == null || GameApp.viewManager == null)
                yield break;

            if (GameApp.viewManager.GetView<WithDraw>((int)ViewType.WithDraw) == null)
                yield break;

            // 第1关只要 WithDraw，不要被通关清引导逻辑误伤；确保 Tutorial 真正激活后再绑步骤。
            UIManager.instance.ShowView(EUIType.Tutorial);
            Tutorial tutorialView = UIManager.instance.GetView<Tutorial>(EUIType.Tutorial);
            if (tutorialView == null)
                yield break;

            if (!tutorialView.gameObject.activeSelf)
                tutorialView.gameObject.SetActive(true);

            tutorialView.currentType = TYPE.TYPE3;
            tutorialView.GoToStep(0);

            // 再等一帧：若 Unity Start 曾抢跑，重新绑定一次 Goal1 手指。
            yield return null;
            if (tutorialView == null || !tutorialView.isActiveAndEnabled)
                yield break;
            if (GameApp.viewManager.GetView<WithDraw>((int)ViewType.WithDraw) == null)
                yield break;

            tutorialView.currentType = TYPE.TYPE3;
            tutorialView.GoToStep(0);
        }

        void RefreshStageUI()
        {
            // 宿主 UIGamePlay：刷新移植的 LuckyRoot/Stage3Root。
            XrCode.FacadeGamePlay.RefreshWzStageHud?.Invoke();

            if (!WaterSortWZBridge.UsesWzGameplayHud || uiManager == null)
                return;

            if (GameDefines.ifIAA)
            {
                var gameView = uiManager.GetView<GameView>(EUIType.GameView);
                gameView?.RefreshStageUI();
                return;
            }

            var gamePlayerView = uiManager.GetView<GamePlayerView>(EUIType.GamePlayerView);
            gamePlayerView?.RefreshStageUI();
        }

        public bool IsStageCompleted(int stageId)
        {
            if (!TryGetStageRequirement(stageId, out int type, out int value))
                return false;

            switch (type)
            {
                case 1: return currentLv > value;
                case 2: return currentCoin >= value;
                case 3: return getLoginDay() >= value;
                case 4: return GetStageAdWatchCount() >= value;
                default: return false;
            }
        }

        public bool TryEnterNextStage()
        {
            if (GameDefines.ifIAA || !IsStageCompleted(currentStage))
                return false;

            int completedStage = currentStage;
            int nextStageId = currentStage + 1;
            if (GetStageData(nextStageId) == null)
                return false;

            currentStage = nextStageId;
            if (currentStage > maxUnlockedStage)
            {
                maxUnlockedStage = currentStage;
                SaveMaxUnlockedStage();
            }

            SaveCurrentStage();
            PlayerPrefs.Save();

            OnStageUpdated?.Invoke(currentStage, completedStage);
            HandleCompletedStage(completedStage);
            return true;
        }

        void CheckCoinStageProgress()
        {
            // Slot/Progress 流程中不要因金币已达标立刻跳过 Stage4。
            if (isProcessingStage4SlotFlow)
                return;

            while (IsCoinStage(currentStage)
                && ShouldAutoEnterNextStage(currentStage)
                && IsStageCompleted(currentStage))
            {
                if (!TryEnterNextStage())
                    break;
            }
        }

        void CheckDeferredStageProgressOnLevelComplete(int stageId)
        {
            if (GameDefines.ifIAA || stageId <= 0 || currentStage != stageId)
                return;

            if (!TryGetStageRequirement(stageId, out int type, out _))
                return;

            if (type != 1 || !ShouldAutoEnterNextStage(stageId) || !IsStageCompleted(stageId))
                return;

            if (TryEnterNextStage())
                CheckCoinStageProgress();
        }

        void OnStageChanged(int newStage, int oldStage)
        {
            Debug.Log($"阶段已更新，从 {oldStage} 到 {newStage}");

            if (IsAdWatchStage(newStage))
            {
                ResetStageAdWatchProgress(newStage);
            }
            else if (IsAdWatchStage(oldStage))
            {
                ResetStageAdWatchProgress();
            }

            if (IsLoginStage(newStage))
            {
                stage5CurrentCycle = 0;
                SaveStage5CycleData();
            }
            else if (IsLoginStage(oldStage))
            {
                stage5CurrentCycle = 0;
                PlayerPrefs.SetInt(FacadePlayerPrefExtend.AddDay, 0);
                PlayerPrefs.DeleteKey(FacadePlayerPrefExtend.Stage5_StartDay);
                PlayerPrefs.DeleteKey(FacadePlayerPrefExtend.Stage5_CurrentCycle);
                PlayerPrefs.Save();
            }

            RefreshStageUI();
            TryShowStage4TargetWithdrawView(newStage);
            TryShowStage5CompensationLuckySpin(newStage);
            CheckCoinStageProgress();
        }

        bool HasCompletedStage5CompensationLuckySpin()
        {
            return PlayerPrefs.GetInt(FacadePlayerPrefExtend.Stage5LuckySpinCompleted, 0) != 0;
        }

        void MarkStage5CompensationLuckySpinCompleted()
        {
            PlayerPrefs.SetInt(FacadePlayerPrefExtend.Stage5LuckySpinCompleted, 1);
            PlayerPrefs.Save();
        }

        void TryShowStage5CompensationLuckySpin(int stageId)
        {
            if (GameDefines.ifIAA
                || stageId != Stage5Id
                || currentStage != Stage5Id
                || UIManager.instance == null
                || isProcessingStage4SlotFlow
                || HasCompletedStage5CompensationLuckySpin()
                || UIManager.instance.HasActiveView(EUIType.BloatView)
                || UIManager.instance.HasActiveView(EUIType.LuckySpinView)
                || UIManager.instance.HasActiveView(EUIType.BonusWheelView))
            {
                return;
            }

            float compensationAmount = Mathf.Max(Stage5CompensationTargetCoin - currentCoin, 0f);
            if (compensationAmount <= 0f)
            {
                MarkStage5CompensationLuckySpinCompleted();
                return;
            }

            double coinBeforeReward = Math.Round((double)currentCoin, decimals);
            UIManager.instance.ShowView(EUIType.BloatView, new LuckySpinOpenData
            {
                ForceMoneyReward = true,
                ForcedMoneyAmount = compensationAmount,
                MoneyRewardTarget = LuckySpinMoneyRewardTarget.CurrentCoin,
                IsMandatory = true,
                AutoStartSpin = false,
                FixedBonusWheelAngleIndex = 1,
                OnRewardComplete = () =>
                {
                    MarkStage5CompensationLuckySpinCompleted();
                    OpenStage5CompensationSuccessView(coinBeforeReward);
                }
            });
        }

        void OpenStage5CompensationSuccessView(double coinBeforeReward)
        {
            if (GameApp.viewManager == null)
                return;

            DataModel dataModel = new DataModel
            {
                Level = Mathf.Max(currentLv - 1, 0),
                Money = coinBeforeReward,
                CloseType = 1,
                Num = Stage5Id,
                Special = DataModel.SpecialRewardAlreadyGranted,
            };

            GameApp.viewManager.Open(ViewType.WithDrawSuc, dataModel);
        }

        void TryShowStage4TargetWithdrawView(int stageId)
        {
            if (GameDefines.ifIAA
                || stageId != Stage4Id
                || UIManager.instance == null
                || PlayerPrefs.GetInt(FacadePlayerPrefExtend.HasShownStage4, 0) != 0
                || UIManager.instance.HasActiveView(EUIType.TargetWithdrawView)
                || UIManager.instance.HasActiveView(EUIType.SlotView)
                || stage3CompletionFlowActive
                || pendingStage3WithdrawMissionAfterLuckyWallet
                || UIManager.instance.HasActiveView(EUIType.LuckyWalletView)
                || UIManager.instance.HasActiveView(EUIType.WithdrawMissionView))
            {
                return;
            }

            ShowStage4SlotView();
        }

        void ShowStage4SlotView()
        {
            if (UIManager.instance == null || UIManager.instance.HasActiveView(EUIType.SlotView))
                return;

            PlayerPrefs.SetInt(FacadePlayerPrefExtend.HasShownStage4, 1);
            PlayerPrefs.Save();
            BeginStage4SlotFlow();
            UIManager.instance.ShowView(
                EUIType.SlotView,
                Stage4SlotRewardAmount,
                (Action)(() => UIManager.instance?.ShowView(EUIType.ProgressView, Stage4SlotRewardAmount)));
        }

        public void BeginStage4SlotFlow()
        {
            isProcessingStage4SlotFlow = true;
        }

        /// <summary>
        /// WithdrawFeedbackView 关闭后：进 Stage4 并强制弹出 SlotView。
        /// </summary>
        public void CompleteStage3FeedbackAndShowSlot()
        {
            stage3CompletionFlowActive = false;
            pendingStage3WithdrawMissionAfterLuckyWallet = false;

            // 先占住 Slot 流程，避免进 Stage4 后因金币已达标被 CheckCoin 立刻推到 Stage5。
            BeginStage4SlotFlow();

            if (currentStage == Stage3Id && IsStageCompleted(Stage3Id))
                TryEnterNextStage();

            if (currentStage != Stage4Id)
            {
                isProcessingStage4SlotFlow = false;
                return;
            }

            // Feedback 路径必须出 Slot；忽略可能因旧逻辑误写的 HasShownStage4。
            ShowStage4SlotView();
        }

        /// <summary>
        /// WithdrawFeedbackView 关闭后：解除 Slot 拦截（兼容旧调用）。
        /// </summary>
        public void AllowStage4SlotAfterStage3Feedback()
        {
            stage3CompletionFlowActive = false;
            pendingStage3WithdrawMissionAfterLuckyWallet = false;
        }

        public void CompleteStage4SlotFlow()
        {
            isProcessingStage4SlotFlow = false;
            stage3CompletionFlowActive = false;

            if (currentStage == Stage5Id)
                TryShowStage5CompensationLuckySpin(Stage5Id);

            // Stage3 达标链路结束：通关广告则进下一关，否则留在当前关继续玩。
            if (stage3CompletionFromLevelClearAd)
            {
                stage3CompletionFromLevelClearAd = false;
                NextLevel();
            }
            else
            {
                // Slot 结束后若 Stage4 金币已达标，再允许自动推进。
                CheckCoinStageProgress();
            }
        }

        /// <summary>
        /// 通关成功页：Stage3 达标链路未走完时，暂不 NextLevel（等 Slot 结束后再进）。
        /// </summary>
        public bool ShouldDeferNextLevelForStage3CompletionFlow()
        {
            return !GameDefines.ifIAA
                   && (IsStage3CompletionFlowActive
                       || (currentStage == Stage3Id && IsStageCompleted(Stage3Id)));
        }

        /// <summary>
        /// Stage3 广告次数刚达标：弹出 LuckyWalletView，玩家确认后再进 Mission。
        /// </summary>
        public void NotifyStage3AdTargetReached(bool fromLevelClearAd)
        {
            if (GameDefines.ifIAA || currentStage != Stage3Id || !IsStageCompleted(Stage3Id))
                return;

            if (fromLevelClearAd)
                stage3CompletionFromLevelClearAd = true;

            stage3CompletionFlowActive = true;

            if (UIManager.instance == null)
                return;

            // 已进入 Feedback / Slot 链路后半段，不再重弹 LuckyWallet。
            if (UIManager.instance.HasActiveView(EUIType.WithdrawFeedbackView)
                || UIManager.instance.HasActiveView(EUIType.SlotView)
                || UIManager.instance.HasActiveView(EUIType.ProgressView))
            {
                return;
            }

            if (UIManager.instance.HasActiveView(EUIType.LuckyWalletView)
                || pendingStage3WithdrawMissionAfterLuckyWallet)
            {
                return;
            }

            // 达标前可能已开着 Mission：先关掉，确保 LuckyWallet 优先。
            if (UIManager.instance.HasActiveView(EUIType.WithdrawMissionView))
                UIManager.instance.CloseView(EUIType.WithdrawMissionView);

            pendingStage3WithdrawMissionAfterLuckyWallet = true;
            UIManager.instance.ShowView(EUIType.LuckyWalletView);
        }

        /// <summary>
        /// LuckyWallet 确认/关闭后：打开 WithdrawMissionView。
        /// </summary>
        public void OpenWithdrawMissionAfterLuckyWalletIfPending()
        {
            if (!pendingStage3WithdrawMissionAfterLuckyWallet)
                return;

            pendingStage3WithdrawMissionAfterLuckyWallet = false;
            stage3CompletionFlowActive = true;

            if (UIManager.instance == null)
                return;

            if (!UIManager.instance.HasActiveView(EUIType.WithdrawMissionView))
                UIManager.instance.ShowView(EUIType.WithdrawMissionView);
        }

        void OnCoinResourceChanged()
        {
            CheckCoinStageProgress();
            RefreshStageUI();
        }

        void OnLoginDayProgressChanged()
        {
            if (IsLoginStage(currentStage))
                TryAutoEnterCompletedStage(currentStage);

            RefreshStageUI();
        }

        void OnAdWatchProgressChanged()
        {
            if (IsAdWatchStage(currentStage))
                TryAutoEnterCompletedStage(currentStage);

            RefreshStageUI();
        }

        public bool TrySyncStageProgress(int stageId)
        {
            if (GameDefines.ifIAA || stageId != currentStage || !IsStageCompleted(currentStage))
                return false;

            return TryEnterNextStage();
        }

        void SaveStage5CycleData()
        {
            if (IsLoginStage(currentStage))
            {
                PlayerPrefs.SetInt(FacadePlayerPrefExtend.Stage5_CurrentCycle, stage5CurrentCycle);
                PlayerPrefs.SetInt(FacadePlayerPrefExtend.AddDay, stage5CurrentCycle);
                PlayerPrefs.Save();
            }
        }

        void CheckStage5Progress()
        {
            if (!IsLoginStage(currentStage))
                return;

            stage5CurrentCycle++;
            SaveStage5CycleData();
            OnLoginDayChanged?.Invoke();
        }
        #endregion

        #region 登录天数系统
        void CheckDailyLogin()
        {
            string today = DateTime.Now.ToString("yyyy-MM-dd");
            string lastLoginDate = PlayerPrefs.GetString(FacadePlayerPrefExtend.LastLoginDateKey, "");

            if (lastLoginDate != today)
            {
                OnDailyLogin();
                PlayerPrefs.SetString(FacadePlayerPrefExtend.LastLoginDateKey, today);
                PlayerPrefs.Save();
            }
        }

        public void OnDailyLogin()
        {
            if (IsLoginStage(currentStage))
                CheckStage5Progress();
        }

        public void SaveLoginDay(int day)
        {
            stage5CurrentCycle = Mathf.Max(day, 0);
            PlayerPrefs.SetInt(FacadePlayerPrefExtend.AddDay, stage5CurrentCycle);
            PlayerPrefs.SetInt(FacadePlayerPrefExtend.Stage5_CurrentCycle, stage5CurrentCycle);
            PlayerPrefs.Save();
        }

        public int getLoginDay() => stage5CurrentCycle;

        void LoadStageAdWatchData()
        {
            currentStageAdWatchCount = PlayerPrefs.GetInt(FacadePlayerPrefExtend.StageAdWatchCount, 0);
            currentStageAdWatchOwnerStage = PlayerPrefs.GetInt(FacadePlayerPrefExtend.StageAdWatchOwnerStage, 0);
            EnsureStageAdWatchProgressState();
        }

        void EnsureStageAdWatchProgressState()
        {
            if (IsAdWatchStage(currentStage))
            {
                if (currentStageAdWatchOwnerStage != currentStage)
                    ResetStageAdWatchProgress(currentStage);

                return;
            }

            if (currentStageAdWatchCount != 0 || currentStageAdWatchOwnerStage != 0)
                ResetStageAdWatchProgress();
        }

        public void RecordStageAdWatch(int count = 1)
        {
            if (count <= 0 || !IsAdWatchStage(currentStage))
                return;

            if (currentStageAdWatchOwnerStage != currentStage)
                ResetStageAdWatchProgress(currentStage);

            int previousCount = currentStageAdWatchCount;
            currentStageAdWatchCount += count;
            SaveStageAdWatchData();
            ApplyStageAdWatchReward(previousCount, currentStageAdWatchCount);
            OnAdWatchCountChanged?.Invoke();
        }

        public int GetStageAdWatchCount() => currentStageAdWatchCount;

        void ResetStageAdWatchProgress(int ownerStage = 0)
        {
            currentStageAdWatchCount = 0;
            currentStageAdWatchOwnerStage = ownerStage;
            SaveStageAdWatchData();
        }

        void SaveStageAdWatchData()
        {
            PlayerPrefs.SetInt(FacadePlayerPrefExtend.StageAdWatchCount, currentStageAdWatchCount);
            PlayerPrefs.SetInt(FacadePlayerPrefExtend.StageAdWatchOwnerStage, currentStageAdWatchOwnerStage);
            PlayerPrefs.Save();
        }

        void ApplyStageAdWatchReward(int previousCount, int newCount)
        {
            if (currentStage != Stage3Id)
                return;

            int missionTarget = GetStageAdRewardTarget();
            int rewardableTarget = Mathf.Max(missionTarget - 1, 0);
            int previousRewardCount = Mathf.Clamp(previousCount, 0, rewardableTarget);
            int newRewardCount = Mathf.Clamp(newCount, 0, rewardableTarget);
            int rewardCountDelta = newRewardCount - previousRewardCount;
            if (rewardCountDelta <= 0)
                return;

            float rewardAmount = Stage3AdRewardAmount * rewardCountDelta;
            AddCoin(rewardAmount);
            AddRewardToPendingStage2History(rewardAmount);
        }

        int GetStageAdRewardTarget()
        {
            if (TryGetStageRequirement(Stage3Id, out int stageType, out int stageTarget)
                && stageType == 4
                && stageTarget > 0)
            {
                return stageTarget;
            }

            return 3;
        }

        void AddRewardToPendingStage2History(double rewardAmount)
        {
            if (rewardAmount <= 0d)
                return;

            List<HistoryModel> historyModels = SPlayerPrefs.GetListTem<HistoryModel>(FacadePlayerPrefExtend.History);
            if (historyModels == null)
                return;

            for (int i = historyModels.Count - 1; i >= 0; i--)
            {
                HistoryModel historyModel = historyModels[i];
                if (historyModel == null || historyModel.Stage != 2 || historyModel.Status == 1)
                    continue;

                historyModel.Money = Math.Round(historyModel.Money + rewardAmount, decimals);
                SPlayerPrefs.SetListTem(FacadePlayerPrefExtend.History, historyModels);
                WithdrawApiService.EnsureQueuedFirstAdOrder(historyModel.Money);
                return;
            }
        }
        #endregion

        #region 定时器系统（30秒奖励）
        void ResetLevelTimerState()
        {
            StopLevelTimer();
            isLevelTimerActive = false;
            canTriggerReward = false;
            isLevelTimerPausedByUI = false;
        }

        public void StartLevelTimer()
        {
            if (currentLv <= 3 || currentState == GAME_STATE.FINISH) return;

            StopLevelTimer();
            canTriggerReward = false;
            isLevelTimerActive = true;
            isLevelTimerPausedByUI = false;

            levelTimer = STimerManager.Instance.CreateSTimer(
                targetTime: GetLevelTimerInterval(currentLv),
                loopCount: 0,
                ifscale: true,
                ifAutoPushPool: false,
                endAction: OnLevelTimerComplete,
                updateAction: GetTime
            );
        }

        float GetLevelTimerInterval(int level)
        {
            if (GameDefines.LevelTimerConfig == null || GameDefines.LevelTimerConfig.Count == 0)
                return GameDefines.TimerReward_Interval;

            List<int> configLevels = new List<int>(GameDefines.LevelTimerConfig.Keys);
            configLevels.Sort();

            foreach (int configLevel in configLevels)
            {
                if (level <= configLevel)
                {
                    GameDefines.TimerReward_Interval = GameDefines.LevelTimerConfig[configLevel];
                    return GameDefines.TimerReward_Interval;
                }
            }

            if (configLevels.Count > 0)
                GameDefines.TimerReward_Interval = GameDefines.LevelTimerConfig[configLevels[configLevels.Count - 1]];

            return GameDefines.TimerReward_Interval;
        }

        public void GetTime(float time) {/* Debug.LogError(time);*/ }

        public void StopLevelTimer()
        {
            levelTimer?.Close();
            levelTimer = null;
            isLevelTimerActive = false;
            isLevelTimerPausedByUI = false;
        }

        public void PauseLevelTimerByUI()
        {
            if (levelTimer != null && isLevelTimerActive && !isLevelTimerPausedByUI)
            {
                levelTimer.Pause();
                isLevelTimerPausedByUI = true;
            }
        }

        public void ResumeLevelTimerByUI()
        {
            if (levelTimer != null && isLevelTimerActive && isLevelTimerPausedByUI)
            {
                levelTimer.Start();
                isLevelTimerPausedByUI = false;
            }
        }

        void OnLevelTimerComplete()
        {
            canTriggerReward = true;
            isLevelTimerActive = false;
            isLevelTimerPausedByUI = false;
        }

        public void CheckPourCompleteForReward()
        {
            if (currentLv <= GameDefines.CloseInt || currentState == GAME_STATE.FINISH) return;
            if (UIManager.instance.HasActiveView(EUIType.RewardView) || HasActiveSuccessView()) return;

            if (canTriggerReward)
                StartCoroutine(ShowRewardViewDelayed(0.5f));
        }

        IEnumerator ShowRewardViewDelayed(float delay)
        {
            yield return new WaitForSeconds(delay);

            if (currentState != GAME_STATE.FINISH && currentLv > GameDefines.CloseInt &&
                !UIManager.instance.HasActiveView(EUIType.RewardView) &&
                !HasActiveSuccessView())
            {
                UIManager.instance.ShowView(EUIType.RewardView);
            }
        }

        public void RestartLevelTimer()
        {
            StopLevelTimer();
            canTriggerReward = false;
            isLevelTimerActive = false;
            isLevelTimerPausedByUI = false;
            StartLevelTimer();
        }

        void ManageTimerBasedOnUI()
        {
            if (currentLv <= GameDefines.CloseInt || currentState == GAME_STATE.FINISH || UIManager.instance == null)
                return;

            bool isGameplayOnly = UIManager.instance.IsGameplayOnlyUIActive();

            if (isGameplayOnly)
            {
                if (isLevelTimerPausedByUI)
                    ResumeLevelTimerByUI();
                else if (!isLevelTimerActive && levelTimer == null && !canTriggerReward)
                    StartLevelTimer();
            }
            else
            {
                if (isLevelTimerActive && !isLevelTimerPausedByUI)
                    PauseLevelTimerByUI();
            }
        }
        #endregion

        #region 倒水计时器系统
        public void ResetPouringTimerState()
        {
            StopPouringTimer();
            isPouringTimerActive = false;
        }

        public void StopPouringTimer() => pouringTimer?.Close();

        public void PausePouringTimer()
        {
            if (pouringTimer != null && isPouringTimerActive)
                pouringTimer.Pause();
        }

        public void ResumePouringTimer()
        {
            if (pouringTimer != null && isPouringTimerActive)
                pouringTimer.Start();
        }

        public void ResetPouringTimer()
        {
            StopPouringTimer();
            isPouringTimerActive = false;
        }


        #endregion

        #region 关卡完成


        public void ShowFinishLevel()
        {
            if (currentState == GAME_STATE.FINISH)
                StartCoroutine(ShowFinishLevelIE());
        }

        IEnumerator ShowFinishLevelIE()
        {
            yield return new WaitForSeconds(0.5f);

            PrepareLevelCompletion();

            // 第1关阶段完成后只弹 WithDraw，不再弹 ChallangeSuccessView。
            if (ShouldSkipSuccessForWithdrawFlow())
                yield break;

            if (UIManager.instance.HasActiveView(EUIType.RewardView))
            {
                pendingUIQueue.Enqueue(GetSuccessViewTypeForCompletedLevel(GetLastCompletedLevel()));
                if (!isProcessingUIQueue)
                    StartCoroutine(ProcessUIQueue());
                yield break;
            }

            ShowSuccessView();
        }

        IEnumerator ProcessUIQueue()
        {
            if (isProcessingUIQueue) yield break;
            isProcessingUIQueue = true;

            while (pendingUIQueue.Count > 0)
            {
                EUIType nextUI = pendingUIQueue.Peek();

                while (UIManager.instance.HasActiveView(EUIType.RewardView) || HasActiveSuccessView())
                    yield return new WaitForSeconds(0.1f);

                yield return null;
                pendingUIQueue.Dequeue();

                if (nextUI == EUIType.ChallangeSuccessView || nextUI == EUIType.LuckySuccessView)
                {
                    if (ShouldSkipSuccessForWithdrawFlow())
                        continue;

                    ShowSuccessView(nextUI);
                    while (!UIManager.instance.HasActiveView(nextUI))
                    {
                        if (ShouldSkipSuccessForWithdrawFlow())
                            break;
                        yield return null;
                    }
                    while (UIManager.instance.HasActiveView(nextUI))
                        yield return new WaitForSeconds(0.1f);
                }
            }

            isProcessingUIQueue = false;
        }



        void PrepareLevelCompletion()
        {
            int totalLevels = GetTotalLevels();
            int stageAtLevelComplete = currentStage;
            lastCompletedLevel = currentLv;

            if (currentLv < totalLevels)
            {
                currentLv++;
                PlayerPrefs.SetInt(FacadePlayerPrefExtend.currentLevel, currentLv);
              
            }
            else
            {
                currentLv = PlayerPrefs.GetInt(FacadePlayerPrefExtend.currentLevel);
            }
            OnLevelChanged?.Invoke();
            OnCoinChanged?.Invoke();
            CheckDeferredStageProgressOnLevelComplete(stageAtLevelComplete);
        }

        bool HasActiveSuccessView()
        {
            return UIManager.instance != null &&
                   (UIManager.instance.HasActiveView(EUIType.ChallangeSuccessView) ||
                    UIManager.instance.HasActiveView(EUIType.LuckySuccessView));
        }

        bool IsCompletionFlowBlockedBySpecialView()
        {
            bool hasLuckySpinView = UIManager.instance != null && UIManager.instance.HasActiveView(EUIType.LuckySpinView);
            bool hasBonusWheelView = UIManager.instance != null && UIManager.instance.HasActiveView(EUIType.BonusWheelView);
            bool hasBloatView = UIManager.instance != null && UIManager.instance.HasActiveView(EUIType.BloatView);
            bool hasWithDrawSucView = GameApp.viewManager != null && GameApp.viewManager.IsOpen((int)ViewType.WithDrawSuc);
            // 注意：不要把 WithDraw 算进“阻塞等待”。否则协程会空等到兑现关闭后再弹通关页。
            return hasLuckySpinView || hasBonusWheelView || hasBloatView || hasWithDrawSucView;
        }

        int GetLastCompletedLevel()
        {
            if (lastCompletedLevel > 0)
                return lastCompletedLevel;

            return Mathf.Max(currentLv - 1, 1);
        }

        bool IsLuckySuccessLevel(int completedLevel)
        {
            return completedLevel > 0 &&
                   UserDataManager.Instance != null &&
                   UserDataManager.Instance.IsLuckyRewardLevel(completedLevel);
        }

        EUIType GetSuccessViewTypeForCompletedLevel(int completedLevel)
        {
            // 宿主驱动统一走 ChallangeSuccessView，确保外部 continue 回调可用。
            if (WaterSortWZBridge.HostDriven || GameDefines.ifIAA)
                return EUIType.ChallangeSuccessView;

            return IsLuckySuccessLevel(completedLevel) ? EUIType.LuckySuccessView : EUIType.ChallangeSuccessView;
        }

        void ShowSuccessView()
        {
            ShowSuccessView(GetSuccessViewTypeForCompletedLevel(GetLastCompletedLevel()));
        }

        void ShowSuccessView(EUIType successViewType) => StartCoroutine(ShowSuccessViewCoroutine(successViewType));

        IEnumerator ShowSuccessViewCoroutine(EUIType successViewType)
        {
            int epoch = successViewEpoch;

            if (HasActiveSuccessView())
                yield break;

            if (ShouldSkipSuccessForWithdrawFlow())
                yield break;

            yield return new WaitForSeconds(1.0f);

            if (epoch != successViewEpoch || ShouldSkipSuccessForWithdrawFlow())
                yield break;

            while (IsCompletionFlowBlockedBySpecialView())
            {
                if (epoch != successViewEpoch || ShouldSkipSuccessForWithdrawFlow())
                    yield break;
                yield return new WaitForSeconds(0.1f);
            }

            if (HasActiveSuccessView())
                yield break;

            if (epoch != successViewEpoch || ShouldSkipSuccessForWithdrawFlow())
                yield break;

            int completedLevel = GetLastCompletedLevel();

            // 宿主驱动时必须弹出成功页并回调继续，否则 Win 后棋盘已清空却不会 StartLevel。
            // 独立模式下 CloseInt 前跳过网赚成功页（旧逻辑）。
            if (WaterSortWZBridge.HostDriven)
            {
                ShowSuccessViewWithHostContinue(successViewType, completedLevel);
            }
            else if (GameDefines.ifIAA || currentLv > GameDefines.CloseInt)
            {
                ShowSuccessViewWithHostContinue(successViewType, completedLevel);
            }
            else
            {
                // 独立包前几关无成功页时仍需进入下一关，避免卡死。
                NextLevel();
            }
        }

        void BeginWithdrawInsteadOfSuccess()
        {
            replaceSuccessWithWithdraw = true;
            successViewEpoch++;
            CloseActiveSuccessViews();
        }

        public void EndWithdrawInsteadOfSuccessAndContinue()
        {
            replaceSuccessWithWithdraw = false;
            CloseActiveSuccessViews();
            // Goal1 结束后清掉宿主 ifTutorial，避免紧接着误弹 Goal2 宿主/Redirect 引导。
            if (FacadeGuide.GetIfTutorial?.Invoke() == true)
                FacadeGuide.SetIfTutorial?.Invoke(false);
            NextLevel();
        }

        public bool IsReplacingSuccessWithWithdraw()
        {
            return replaceSuccessWithWithdraw;
        }

        bool ShouldSkipSuccessForWithdrawFlow()
        {
            return replaceSuccessWithWithdraw || IsWithdrawFlowUiOpen();
        }

        bool IsWithDrawUiOpen()
        {
            return GameApp.viewManager != null && GameApp.viewManager.IsOpen((int)ViewType.WithDraw);
        }

        bool IsWithdrawFlowUiOpen()
        {
            if (GameApp.viewManager == null)
                return false;

            return GameApp.viewManager.IsOpen((int)ViewType.WithDraw)
                   || GameApp.viewManager.IsOpen((int)ViewType.WithDrawMethod)
                   || GameApp.viewManager.IsOpen((int)ViewType.WithDrawProcess)
                   || GameApp.viewManager.IsOpen((int)ViewType.WithDrawProcess1)
                   || GameApp.viewManager.IsOpen((int)ViewType.WithDrawConfirm)
                   || GameApp.viewManager.IsOpen((int)ViewType.WithDrawQues)
                   || GameApp.viewManager.IsOpen((int)ViewType.WithDrawContinue)
                   || GameApp.viewManager.IsOpen((int)ViewType.WithDrawKeepEarn);
        }

        void CloseActiveSuccessViews()
        {
            if (UIManager.instance == null)
                return;

            if (UIManager.instance.HasActiveView(EUIType.ChallangeSuccessView))
                UIManager.instance.CloseView(EUIType.ChallangeSuccessView);
            if (UIManager.instance.HasActiveView(EUIType.LuckySuccessView))
                UIManager.instance.CloseView(EUIType.LuckySuccessView);
        }

        public void CloseActiveTutorial()
        {
            if (UIManager.instance != null)
            {
                // 不要用 HasActiveView：Close 后可能 alpha=0 但仍留在 activeViews，导致手指残留。
                int guard = 8;
                while (guard-- > 0 && UIManager.instance.GetView<Tutorial>(EUIType.Tutorial) != null)
                    UIManager.instance.CloseView(EUIType.Tutorial);
            }

            // 对象池/场景中残留实例一并强制隐藏。
            Tutorial[] tutorials = Resources.FindObjectsOfTypeAll<Tutorial>();
            for (int i = 0; i < tutorials.Length; i++)
            {
                Tutorial tutorialView = tutorials[i];
                if (tutorialView == null)
                    continue;
                if (!tutorialView.gameObject.scene.IsValid())
                    continue;

                tutorialView.ForceHideGuideVisuals();
                if (tutorialView.gameObject.activeSelf)
                    tutorialView.gameObject.SetActive(false);
            }
        }

        void ShowSuccessViewWithHostContinue(EUIType successViewType, int completedLevel)
        {
            // 通关页出现前清掉残留引导（尤其是第2关阶段引导）。
            CloseActiveTutorial();

            if (uiManager == null)
            {
                if (WaterSortWZBridge.HostDriven && WaterSortWZBridge.TryConsumeContinueAction(out var orphanContinue))
                    orphanContinue.Invoke();
                return;
            }

            Action continueAction = null;
            bool hasContinue = WaterSortWZBridge.HostDriven
                && WaterSortWZBridge.TryConsumeContinueAction(out continueAction);

            BaseView shown = hasContinue
                ? uiManager.ShowView(successViewType, completedLevel, continueAction)
                : uiManager.ShowView(successViewType, completedLevel);

            // Prefab 加载失败时不能吞掉回调，否则棋盘已清空却无法开下一关。
            if (hasContinue && shown == null && continueAction != null)
                continueAction.Invoke();
        }
        #endregion

        #region 道具系统
        void UpdateBoosterState()
        {
            if (!WaterSortWZBridge.UsesWzGameplayHud)
            {
                // 宿主 UIGamePlay 道具显隐由宿主玩法模块自行管理。
                XrCode.FacadeGamePlay.SetProp1CountShow?.Invoke();
                XrCode.FacadeGamePlay.SetProp2CountShow?.Invoke();
                XrCode.FacadeGamePlay.SetProp3CountShow?.Invoke();
                return;
            }

            object gameView = GameDefines.ifIAA ?
                uiManager.GetView<GameView>(EUIType.GameView) :
                uiManager.GetView<GamePlayerView>(EUIType.GamePlayerView);

            if (gameView == null) return;

            if (currentLv == 1)
            {
                if (gameView is GameView gv)
                {
                    gv.HideBooster();
                    gv.HideWithdraw();
                }
                else if (gameView is GamePlayerView gpv)
                {
                    gpv.HideBooster();
                    gpv.HideWithdraw();
                }
            }
            else
            {
                if (gameView is GameView gv)
                {
                    gv.ShowBooster();
                    gv.ShowWithdraw();
                }
                else if (gameView is GamePlayerView gpv)
                {
                    gpv.ShowBooster();
                    gpv.ShowWithdraw();
                }
            }
        }

        public void OnHintBottleAdded()
        {
            if (currentAddBottleCount <= 0) return;
            currentAddBottleCount--;
            SaveAddBottleData();
            RefreshBottleCountUI();
        }

        public bool UseAddBottle()
        {
            if (currentAddBottleCount <= 0) return false;

            currentAddBottleCount--;
            SaveAddBottleData();
            RefreshBottleCountUI();
            TryExecutePropAction(ERewardType.AddBottle);
            return true;
        }

        public void RewardHintBottle(int amount = 1)
        {
            currentAddBottleCount += amount;
            SaveAddBottleData();
            RefreshBottleCountUI();
        }

        public void ProcessClear()
        {
            UseClear();
        }

        public void RewardClear(int amount = 1)
        {
            currentClear += amount;
            SaveClearData();
            RefreshClearInfoUI();
        }

        public void RewardUndo(int amount = 1)
        {
            currentUndo += amount;
            SaveUndoData();
            RefreshUndoInfoUI();
        }

        public bool UseUndo()
        {
            if (currentUndo <= 0)
            {
                UIManager.instance.ShowView(EUIType.NoticeView, LocalizationManager.Instance.GetText("1034"));
                return false;
            }

            currentUndo--;
            SaveUndoData();
            RefreshUndoInfoUI();
            TryExecutePropAction(ERewardType.Undo);
            return true;
        }

        public bool UseClear()
        {
            if (currentClear <= 0) return false;

            currentClear--;
            SaveClearData();
            RefreshClearInfoUI();
            TryExecutePropAction(ERewardType.Clear);
            return true;
        }

        bool TryExecutePropAction(ERewardType rewardType)
        {
            try
            {
                switch (rewardType)
                {
                    case ERewardType.AddBottle:
                        if (FacadeGamePlayExtend.Func_INCREASEHandle != null)
                        {
                            FacadeGamePlayExtend.Func_INCREASEHandle.Invoke();
                            return true;
                        }

                        if (FacadeGamePlayExtend.OnUnlockTmpHandle != null)
                        {
                            FacadeGamePlayExtend.OnUnlockTmpHandle.Invoke();
                            return true;
                        }
                        break;

                    case ERewardType.Clear:
                        if (FacadeGamePlayExtend.Func_CLEARHandle != null)
                        {
                            FacadeGamePlayExtend.Func_CLEARHandle.Invoke(true);
                            return true;
                        }

                        if (FacadeGamePlayExtend.ClearAllCellsHandle != null)
                        {
                            FacadeGamePlayExtend.ClearAllCellsHandle.Invoke();
                            return true;
                        }
                        break;

                    case ERewardType.Undo:
                        if (FacadeGamePlayExtend.Func_HAMMERHandle != null)
                        {
                            FacadeGamePlayExtend.Func_HAMMERHandle.Invoke();
                            return true;
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Prop action failed: {rewardType}, error={ex}");
                return false;
            }

            Debug.LogWarning($"Prop action is not wired: {rewardType}");
            return false;
        }

        void RefreshBottleCountUI()
        {
            XrCode.FacadeGamePlay.SetProp1CountShow?.Invoke();
            if (!WaterSortWZBridge.UsesWzGameplayHud || uiManager == null) return;

            if (GameDefines.ifIAA)
                uiManager.GetView<GameView>(EUIType.GameView)?.RefreshBottleCount();
            else
                uiManager.GetView<GamePlayerView>(EUIType.GamePlayerView)?.RefreshBottleCount();
        }

        void RefreshClearInfoUI()
        {
            XrCode.FacadeGamePlay.SetProp2CountShow?.Invoke();
            if (!WaterSortWZBridge.UsesWzGameplayHud || uiManager == null) return;

            if (GameDefines.ifIAA)
                uiManager.GetView<GameView>(EUIType.GameView)?.RefreshClearInfo();
            else
                uiManager.GetView<GamePlayerView>(EUIType.GamePlayerView)?.RefreshClearInfo();
        }

        void RefreshUndoInfoUI()
        {
            XrCode.FacadeGamePlay.SetProp3CountShow?.Invoke();
            if (!WaterSortWZBridge.UsesWzGameplayHud || uiManager == null) return;

            if (GameDefines.ifIAA)
                uiManager.GetView<GameView>(EUIType.GameView)?.RefreshUndoInfo();
            else
                uiManager.GetView<GamePlayerView>(EUIType.GamePlayerView)?.RefreshUndoInfo();
        }

        public void AddUndo() { }
        public void ProcessUndo()
        {
            TryExecutePropAction(ERewardType.Undo);
        }
        #endregion

        #region 消除次数系统
        void SaveEliminationCount()
        {
            PlayerPrefs.SetInt(FacadePlayerPrefExtend.currentEliminationCount, currentEliminationCount);
            PlayerPrefs.SetInt(FacadePlayerPrefExtend.totalEliminationCount, totalEliminationCount);
            PlayerPrefs.Save();
        }



        void CheckLuckySpinCondition()
        {
            if (currentEliminationCount >= GameDefines.LuckySpin_Elimination_Interval)
                TriggerLuckySpin();
        }

        void TriggerLuckySpin()
        {
            if (UIManager.instance.HasActiveView(EUIType.RewardView)) return;

            currentEliminationCount = 0;
            SaveEliminationCount();
            FacadeEffectExtend.PlayCongratulationEffectHandle?.Invoke();
            UIManager.instance.ShowView(EUIType.LuckySpinView);
        }

        public void ResetEliminationCount()
        {
            currentEliminationCount = 0;
            SaveEliminationCount();
        }
        #endregion

        #region 货币
        public void AddCoin(float moreCoin)
        {
            AddCoin(moreCoin, currentLv);
        }

        public void AddCoin(float moreCoin, int rewardLevel)
        {
            if (moreCoin == 0) return;
            if (moreCoin > 0f && !CanGrantCoinRewardForLevel(rewardLevel)) return;
            currentCoin = NormalizeRegularCoin(currentCoin + moreCoin);
            SaveCoinData();
            UpdateCoinUI();
            OnCoinChanged?.Invoke();
        }

        public void AddCoinTem(float moreCoin)
        {
            currentCoin = NormalizeRegularCoin(currentCoin + moreCoin);
            SaveCoinData();
            UpdateCoinUI();
            OnCoinChanged?.Invoke();
        }

        public void AddGems(float addGems)
        {
            if (addGems <= 0) return;
            currentGems += addGems;
            SaveGemsData();
            UpdateGemUI();
        }

        public void SubCoin(int subCoin)
        {
            if (subCoin <= 0) return;
            currentCoin = NormalizeRegularCoin(currentCoin - subCoin);
            SaveCoinData();
            UpdateCoinUI();
            OnCoinChanged?.Invoke();
        }

        public void SubGem(int subGem)
        {
            if (subGem <= 0) return;
            currentGems -= subGem;
            SaveGemsData();
            UpdateGemUI();
        }

        public void UpdateCoinUI()
        {
            // 宿主 UIGamePlay 余额 / Stage HUD。
            XrCode.FacadeGamePlay.SetCurMoneyShow?.Invoke();
            XrCode.FacadeGamePlay.RefreshWzStageHud?.Invoke();

            if (!WaterSortWZBridge.UsesWzGameplayHud || uiManager == null)
                return;

            if (GameDefines.ifIAA)
            {
                var gameView = uiManager.GetView<GameView>(EUIType.GameView);
                if (gameView?.coinTxt != null)
                    gameView.coinTxt.text = FacadePayTypeExtend.RegionalChangeHandle(currentCoin);
                gameView?.UpdateWithdrawPrompt();
            }
            else
            {
                var gameView = uiManager.GetView<GamePlayerView>(EUIType.GamePlayerView);
                if (gameView?.coinTxt != null)
                    gameView.coinTxt.text = FacadePayTypeExtend.RegionalChangeHandle(currentCoin);
                gameView?.UpdateWithdrawPrompt();
            }
        }

        public void UpdateGemUI()
        {
            if (!WaterSortWZBridge.UsesWzGameplayHud || uiManager == null)
                return;

            if (GameDefines.ifIAA)
            {
                var gameView = uiManager.GetView<GameView>(EUIType.GameView);
                if (gameView?.gemTxt != null)
                    gameView.gemTxt.text = currentGems.ToString();
            }
            else
            {
                var gameView = uiManager.GetView<GamePlayerView>(EUIType.GamePlayerView);
                if (gameView?.gemTxt != null)
                    gameView.gemTxt.text = currentGems.ToString();
            }
        }

        string RegionalChange(float value)
        {
            if (!GameDefines.ifIAA)
            {
                value *= exchangeRate;
                return $"{mark}{value.ToString($"F{decimals}")}";
            }
            return $"{Mathf.CeilToInt(value)}";
        }

        public double GetCoin() => Math.Round((double)currentCoin, decimals);

        public bool CanGrantCoinRewardForCurrentLevel()
        {
            return CanGrantCoinRewardForLevel(currentLv);
        }

        public bool CanGrantCoinRewardForLevel(int level)
        {
            if (GameDefines.ifIAA)
                return true;

            return level <= 0 || level > NoCoinRewardEndLevel;
        }

        public float GetCoinRewardAmount(int gearType, int rewardLevel = -1)
        {
            int targetLevel = rewardLevel > 0 ? rewardLevel : currentLv;
            if (!CanGrantCoinRewardForLevel(targetLevel))
                return 0f;

            return getResourceNum(1, gearType, 0);
        }

        float NormalizeRegularCoin(float coinValue)
        {
            if (GameDefines.ifIAA)
                return coinValue;

            return Mathf.Min(coinValue, NormalCoinHardCap);
        }
        #endregion

        #region 存档
        public void SaveCoinData()
        {
            currentCoin = NormalizeRegularCoin(currentCoin);
            PlayerPrefs.SetFloat(FacadePlayerPrefExtend.coin, currentCoin);
        }

        public float getCoinData() => NormalizeRegularCoin(PlayerPrefs.GetFloat(FacadePlayerPrefExtend.coin));
        public int getLevel() => PlayerPrefs.GetInt(FacadePlayerPrefExtend.currentLevel);
        public void SaveGemsData() => PlayerPrefs.SetFloat(FacadePlayerPrefExtend.gems, currentGems);
        public void SaveUndoData() => PlayerPrefs.SetInt(FacadePlayerPrefExtend.undo, currentUndo);
        public void SaveClearData() => PlayerPrefs.SetInt(FacadePlayerPrefExtend.ClearColorCount, currentClear);
        public void SaveAddBottleData() => PlayerPrefs.SetInt(FacadePlayerPrefExtend.AddBottleCount, currentAddBottleCount);
        public void SavePlayerName(string name) => PlayerPrefs.SetString(FacadePlayerPrefExtend.AddName, name);
        public string getPlayerName() => PlayerPrefs.GetString(FacadePlayerPrefExtend.AddName);
        public void SavePlayerPhone(string phone) => PlayerPrefs.SetString(FacadePlayerPrefExtend.AddPhone, phone);
        public string getPlayerPhone() => PlayerPrefs.GetString(FacadePlayerPrefExtend.AddPhone);
        public void SavePlayerEmail(string email) => PlayerPrefs.SetString(FacadePlayerPrefExtend.AddEmail, email);
        public string getPlayerEmail() => PlayerPrefs.GetString(FacadePlayerPrefExtend.AddEmail);
        public void SavePlayerIndex(int index) => PlayerPrefs.SetInt(FacadePlayerPrefExtend.AddIndex, index);
        public int getPlayerIndex() => PlayerPrefs.GetInt(FacadePlayerPrefExtend.AddIndex);
        public int getPlayerStage() => currentStage;
        #endregion

        void RegisterFacadeUserHandles()
        {
            UserWalletData cachedWallet = WithdrawApiService.GetCachedWallet();
            if (cachedWallet != null)
                GameDefines.LuckyChestPlaceholderCoin = (float)Math.Max(cachedWallet.Balance, 0d);

            FacadeUserExtend.GetUserCoinHandle = () => currentCoin;
            FacadeUserExtend.SetUserCoinHandle = value =>
            {
                currentCoin = NormalizeRegularCoin(value);
                SaveCoinData();
                UpdateCoinUI();
                OnCoinChanged?.Invoke();
            };
            FacadeUserExtend.AddUserCoinHandle = AddCoin;
            FacadeUserExtend.GetWithdrawalCoinHandle = () => GameDefines.LuckyChestPlaceholderCoin;
            FacadeUserExtend.AddWithdrawalCoinHandle = value => WithdrawApiService.AddWalletIncome(value);
        }

        void ClearFacadeUserHandles()
        {
            FacadeUserExtend.GetUserCoinHandle = null;
            FacadeUserExtend.SetUserCoinHandle = null;
            FacadeUserExtend.AddUserCoinHandle = null;
            FacadeUserExtend.GetWithdrawalCoinHandle = null;
            FacadeUserExtend.AddWithdrawalCoinHandle = null;
        }

        #region 关卡管理
        public void ReplayGame()
        {
            currentState = GAME_STATE.WAIT;
            currentBottleFull = 0;
            replaceSuccessWithWithdraw = false;
            successViewEpoch++;
            CloseActiveSuccessViews();

            SetFirstData();
            ResetLevelTimerState();
            ResetPouringTimerState();
            levelGen.InitLvGen();

            // WZ LevelManager 已移除：Game2 / 宿主 HUD 都要推进宿主关卡，否则棋盘空白。
            // 第2关后 defer：只同步关卡号，等 Mission Continue。
            if (TryAdvanceHostGameplayLevel() && (WaterSortWZBridge.HostDriven || WaterSortWZBridge.UseHostGameplayHud || deferHostLevelLoad || stage2CashGuidePending))
            {
                UpdateBoosterState();
                RefreshHostGameplayHud();
                return;
            }

            if (WaterSortWZBridge.HostDriven || WaterSortWZBridge.UseHostGameplayHud)
            {
                UpdateBoosterState();
                StartLevelTimer();
                RefreshHostGameplayHud();
                return;
            }

            if (GameDefines.ifIAA)
                uiManager.GetView<GameView>(EUIType.GameView).InitView();
            else
                uiManager.GetView<GamePlayerView>(EUIType.GamePlayerView).InitView();

            if (currentLv == 1 && tutorial != null)
            {
                tutorial.step = 0;
                tutorial.GoToStep(0);
            }

            UpdateBoosterState();
            StartLevelTimer();

          
        }

        public void NextLevel()
        {
            currentState = GAME_STATE.WAIT;
            currentBottleFull = 0;
            completedAnimationBottles = 0;
            replaceSuccessWithWithdraw = false;
            successViewEpoch++;
            CloseActiveSuccessViews();
            if ((WaterSortWZBridge.HostDriven || WaterSortWZBridge.UseHostGameplayHud) && !stage2CashGuidePending)
                CloseActiveTutorial();
           // LevelManager.Instance.LoadCurrentLevel();
            SetFirstData();
            ResetLevelTimerState();
            ResetPouringTimerState();
            levelGen.InitLvGen();

            // UseHostGameplayHud：只推进宿主关卡，不刷 WZ GamePlayerView。
            // 第2关后 defer：只同步关卡号，等 Mission Continue 再 CreateLevel。
            if (TryAdvanceHostGameplayLevel() && (WaterSortWZBridge.HostDriven || WaterSortWZBridge.UseHostGameplayHud || deferHostLevelLoad || stage2CashGuidePending))
            {
                UpdateBoosterState();
                RefreshHostGameplayHud();
                return;
            }

            if (WaterSortWZBridge.HostDriven || WaterSortWZBridge.UseHostGameplayHud)
            {
                UpdateBoosterState();
                StartLevelTimer();
                RefreshHostGameplayHud();
                return;
            }

            if (GameDefines.ifIAA)
                uiManager.GetView<GameView>(EUIType.GameView).InitView();
            else
                uiManager.GetView<GamePlayerView>(EUIType.GamePlayerView).InitView();

            UpdateBoosterState();
            StartLevelTimer();

            if (currentLv == 1)
            {
                tutorial.ShowView();
                tutorial.currentType = Tutorial.TYPE.TYPE1;
            }
            else
            {
                tutorial.gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// 推进宿主玩法关卡。WZ 原生 LevelManager 已下线，瓶子只由宿主 CreateLevel 生成。
        /// </summary>
        bool TryAdvanceHostGameplayLevel()
        {
            // 第2关后：只同步关卡号到 HUD，等 Mission Continue 再 CreateLevel。
            if (deferHostLevelLoad || stage2CashGuidePending)
            {
                if (WaterSortWZBridge.TryConsumeContinueAction(out _))
                {
                    // 丢弃成功页 continue，避免提前开瓶。
                }

                int hostLevel = Mathf.Max(currentLv, 1);
                XrCode.FacadePlayer.SetLevel?.Invoke(hostLevel);
                return true;
            }

            if (WaterSortWZBridge.TryConsumeContinueAction(out var continueAction))
            {
                continueAction.Invoke();
                return true;
            }

            // UseHostGameplayHud / Game2：由宿主 CreateLevel 建瓶。
            if (!WaterSortWZBridge.HostDriven
                && !WaterSortWZBridge.UseHostGameplayHud
                && !WaterSortWZBridge.IsWzEntryScene())
                return false;

            int level = Mathf.Max(currentLv, 1);
            XrCode.FacadePlayer.SetLevel?.Invoke(level);
            XrCode.FacadeGamePlay.StartLevel?.Invoke();
            return true;
        }

        void RefreshHostGameplayHud()
        {
            if (!WaterSortWZBridge.UseHostGameplayHud && !WaterSortWZBridge.HostDriven)
                return;

            XrCode.FacadeGamePlay.SetLevelShow?.Invoke();
            XrCode.FacadeGamePlay.SetCurMoneyShow?.Invoke();
            XrCode.FacadeGamePlay.SetProp1CountShow?.Invoke();
            XrCode.FacadeGamePlay.SetProp2CountShow?.Invoke();
            XrCode.FacadeGamePlay.SetProp3CountShow?.Invoke();
            XrCode.FacadeGamePlay.RefreshWzStageHud?.Invoke();
        }
        #endregion

        #region 工具方法
        public float getResourceNum(int type, int gearType, int gemType)
        {
            Dictionary<string, object> dic = null;

            if (type == 1)
            {
                foreach (var dictionary in UserDataManager.Instance.getAllGearData())
                {
                    if (currentCoin > int.Parse((string)dictionary["CurrencyMin"]) && currentCoin <= int.Parse((string)dictionary["CurrencyMax"]))
                    {
                        dic = dictionary;
                        break;
                    }
                }

                if (dic != null)
                {
                    if (gearType == 1)
                    {
                        float min = float.Parse((string)dic["LuckMin"]);
                        float max = float.Parse((string)dic["LuckMax"]);
                        return Random.Range(min, max);
                    }
                    else if (gearType == 2)
                    {
                        float min = float.Parse((string)dic["EliminateMin"]);
                        float max = float.Parse((string)dic["EliminateMax"]);
                        return Random.Range(min, max);
                    }
                    else
                    {
                        return float.Parse((string)dic["TurntableMax"]);
                    }
                }
            }
            else
            {
                foreach (var dictionary in UserDataManager.Instance.getAllGemData())
                {
                    if (currentGems > int.Parse((string)dictionary["CurrencyMin"]) && currentGems <= int.Parse((string)dictionary["CurrencyMax"]))
                    {
                        dic = dictionary;
                        break;
                    }
                }

                if (dic != null)
                {
                    if (gemType == 1)
                    {
                        float min = float.Parse((string)dic["LuckMin"]);
                        float max = float.Parse((string)dic["LuckMax"]);
                        return Random.Range(min, max);
                    }
                    else
                    {
                        return float.Parse((string)dic["TurntableMin"]);
                    }
                }
            }
            return 0;
        }
        #endregion
    }
}
