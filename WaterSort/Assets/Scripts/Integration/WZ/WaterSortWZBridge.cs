using System;
using System.Collections.Generic;
using UnityEngine;
using WZSDK;
using XrCode;
using HostERewardType = ERewardType;

/// <summary>
/// WaterSort 宿主与 WZSDK 的项目专属桥接。
/// 以 WZ 为准：广告/成功页/提现走 WZ；玩法关卡由宿主创建。
/// Game2 场景可作为入口直接开玩。
/// </summary>
public static class WaterSortWZBridge
{
    public const bool HostDriven = false;

    private static bool _wired;
    private static bool _pendingHostContinue;
    private static Action _pendingContinueAction;
    private static bool _interstitialCallbacksWired;
    private static bool _level1IntroDismissed;

    /// <summary>第1关开场页（kaiju_ui_1）是否已点过 Start Game。</summary>
    public static bool Level1IntroDismissed => _level1IntroDismissed;

    public static bool IsLevel1IntroUiOpen()
    {
        if (GameApp.viewManager != null)
        {
            if (GameApp.viewManager.IsOpen((int)ViewType.kaiju_ui_1))
                return true;
            if (GameApp.viewManager.IsOpen((int)ViewType.kaiju_ui_1_1))
                return true;
        }

        if (WZSDK.UIManager.instance != null
            && WZSDK.UIManager.instance.HasActiveView(WZSDK.EUIType.WelcomeView))
            return true;

        return false;
    }

    public static bool IsReady =>
        _wired
        && GameManagerWZ.instance != null
        && AdsControl.Instance != null;

    public static void NotifyLevel1IntroOpened()
    {
        _level1IntroDismissed = false;
        // 开场期间若倒水引导已误弹出，立刻关掉。
        FacadeGuide.CloseGuide?.Invoke();
    }

    public static void NotifyLevel1IntroDismissed()
    {
        if (_level1IntroDismissed)
            return;

        _level1IntroDismissed = true;

        // 开场关闭后再播第1关倒水引导。
        int level = FacadePlayer.GetLevel?.Invoke() ?? 1;
        if (level <= 1)
            FacadeGuide.PlayLevel1PourTutorial?.Invoke();
    }


    public static void InitializeOrSync()
    {
        SyncIaaFlag();
        WireFacades();
        SyncLevelFromHost();
    }

    public static void ReportSessionStart(int sessionId)
    {
        if (!IsReady) return;
        SyncLevelToWz(sessionId);
        GameManagerWZ.instance.currentState = GameManagerWZ.GAME_STATE.PLAYING;
    }

    public static void ReportSessionComplete(int sessionId, bool success)
    {
        if (!IsReady)
        {
            FallbackHostComplete(sessionId, success);
            return;
        }

        SyncLevelToWz(sessionId);
        if (!success)
        {
            TDAnalyticsMgr.Instance?.LevelFail(sessionId);
            return;
        }

        _pendingHostContinue = true;
        _pendingContinueAction = OnHostContinueAfterWzSuccess;
        GameManagerWZ.instance.CheckIfAllAnimationsCompleted();
    }

    public static void ReportBottlePacked(int amount = 1)
    {
        if (!IsReady || GameManagerWZ.instance == null) return;
        for (int i = 0; i < amount; i++)
            GameManagerWZ.instance.OnPourCompleteForLuckyReward();
    }

    public static void OpenWithdraw()
    {
        TryOpenWithdraw(-1);
    }

    /// <summary>
    /// 打开 WZ WithDraw。goalStage&gt;0 时按指定 Goal 打开（第2关引导应开 Goal2，而不是已推进的 stage3）。
    /// </summary>
    public static bool TryOpenWithdraw(int goalStage = -1)
    {
        InitializeOrSync();
        if (GameManagerWZ.instance == null || GameApp.viewManager == null)
            return false;

        var gm = GameManagerWZ.instance;
        int num = goalStage > 0 ? goalStage : Mathf.Max(gm.currentStage, 1);
        var dataModel = new DataModel
        {
            Level = Mathf.Max(gm.currentLv - 1, 0),
            Money = gm.GetCoin(),
            CloseType = 1,
            Num = num,
            Name = gm.getPlayerName(),
            Phone = gm.getPlayerPhone(),
            Email = gm.getPlayerEmail()
        };
        GameApp.viewManager.Open(ViewType.WithDraw, dataModel);
        return true;
    }

    public static void OpenWithdrawHistory()
    {
        if (!IsReady || GameManagerWZ.instance == null || GameApp.viewManager == null) return;

        var gm = GameManagerWZ.instance;
        var dataModel = new DataModel
        {
            Level = Mathf.Max(gm.currentLv - 1, 0),
            Money = gm.GetCoin(),
            CloseType = 1,
            Name = gm.getPlayerName(),
            Phone = gm.getPlayerPhone(),
            Email = gm.getPlayerEmail()
        };
        GameApp.viewManager.Open(ViewType.WithDrawHistory, dataModel);
    }

    public static void NotifyHostAdvanced(int nextSessionId)
    {
        if (!IsReady || GameManagerWZ.instance == null) return;
        SyncLevelToWz(nextSessionId);
        GameManagerWZ.instance.TrySyncStageProgress(GameManagerWZ.instance.currentStage);
    }

    public static bool TryConsumeContinueAction(out Action continueAction)
    {
        continueAction = _pendingContinueAction;
        _pendingContinueAction = null;
        bool had = _pendingHostContinue && continueAction != null;
        _pendingHostContinue = false;
        return had;
    }

    public static bool ShouldDeferHostLevelCreate()
    {
        return GameManagerWZ.instance != null && GameManagerWZ.instance.DeferHostLevelLoad;
    }

    private static void OnHostContinueAfterWzSuccess()
    {
        if (GameManagerWZ.instance != null)
        {
            int nextLevel = Mathf.Max(GameManagerWZ.instance.currentLv, 1);
            FacadePlayer.SetLevel?.Invoke(nextLevel);
        }
        else
        {
            FacadePlayer.AddLevel?.Invoke(1);
        }

        // 第2关后等 Mission Continue 再开瓶。
        if (ShouldDeferHostLevelCreate())
            return;

        FacadeGamePlay.StartLevel?.Invoke();
        NotifyHostAdvanced(FacadePlayer.GetLevel?.Invoke() ?? 1);
    }

    private static void FallbackHostComplete(int sessionId, bool success)
    {
        if (!success) return;
        XrCode.UIManager.Instance.OpenAsync<UILevelCompleted>(
            global::EUIType.EUILevelCompleted, UIOpenType.None, null, sessionId);
        FacadePlayer.AddLevel?.Invoke(1);
    }

    private static void WireFacades()
    {
        if (_wired) return;
        _wired = true;

        FacadeAd.ShowRewardAd = ShowHostRewardedViaWz;
        FacadeAd.ShowInterAd = ShowHostInterstitialViaWz;
        FacadeAd.GetRewardAdReady = () =>
            AdsControl.Instance != null && AdsControl.Instance.IsRewardedVideoAvailable();
        FacadeAd.GetInterAdReady = MaxInterstitialReadySafe;

        FacadeGamePlayExtend.CreateLevelHandle = level =>
        {
            SyncLevelToWz(level);
            FacadeGamePlay.CreateLevel?.Invoke();
        };
        FacadeGamePlayExtend.RePlayHandle = () => FacadeGamePlay.RePlay?.Invoke();
        FacadeGamePlayExtend.Func_INCREASEHandle = () => FacadeGamePlay.Func_Porp1?.Invoke();
        FacadeGamePlayExtend.Func_CLEARHandle = _ => FacadeGamePlay.Func_Porp2?.Invoke();
        FacadeGamePlayExtend.Func_HAMMERHandle = () => FacadeGamePlay.Func_Porp3?.Invoke();
        FacadeGamePlayExtend.GetFlyObjGoldHandle = MapFlyGoals;
        FacadeGamePlayExtend.GetLevelProgressText = () => FacadeGamePlay.GetLevelProgress?.Invoke();

        FacadeUserExtend.GetLevelHandle = () => FacadePlayer.GetLevel?.Invoke() ?? 1;
        FacadeUserExtend.SetLevelHandle = value => FacadePlayer.SetLevel?.Invoke(value);
        FacadeUserExtend.AddLevelHandle = value => FacadePlayer.AddLevel?.Invoke(value);
        FacadeUserExtend.GetAddSpacePropNumHandle = () => FacadePlayer.GetProp1Num?.Invoke() ?? 0;
        FacadeUserExtend.SetAddSpacePropCountHandle = value => FacadePlayer.SetProp1Num?.Invoke(value);
        FacadeUserExtend.AddAddSpacePropNumHandle = value => FacadePlayer.AddProp1Num?.Invoke(value);
        FacadeUserExtend.GetClearPropNumHandle = () => FacadePlayer.GetProp2Num?.Invoke() ?? 0;
        FacadeUserExtend.SetClearPropNumHandle = value => FacadePlayer.SetProp2Num?.Invoke(value);
        FacadeUserExtend.AddClearPropNumHandle = value => FacadePlayer.AddProp2Num?.Invoke(value);
        FacadeUserExtend.GetHammerPropNumHandle = () => FacadePlayer.GetProp3Num?.Invoke() ?? 0;
        FacadeUserExtend.SetHammerPropNumHandle = value => FacadePlayer.SetProp3Num?.Invoke(value);
        FacadeUserExtend.AddHammerPropNumHandle = value => FacadePlayer.AddProp3Num?.Invoke(value);
        FacadeUserExtend.GetUserIDHandle = () => FacadePlayer.GetPlayerID?.Invoke();
        FacadeUserExtend.GetUserNameHandle = () => FacadePlayer.GetPlayerName?.Invoke();
        FacadeUserExtend.GetUserLevelHandle = () => FacadePlayer.GetPlayerLevel?.Invoke() ?? 0;
        FacadeUserExtend.GetExpHandle = () => FacadePlayer.GetPlayerExp?.Invoke() ?? 0;
        FacadeUserExtend.AddExpHandle = value => FacadePlayer.AddPlayerExp?.Invoke(value);

        WireInterstitialCallbacks();
    }

    private static void WireInterstitialCallbacks()
    {
        if (_interstitialCallbacksWired) return;
        _interstitialCallbacksWired = true;
        MaxSdkCallbacks.Interstitial.OnAdHiddenEvent += OnWzInterstitialHidden;
        MaxSdkCallbacks.Interstitial.OnAdDisplayFailedEvent += OnWzInterstitialDisplayFailed;
    }

    private static Dictionary<WZSDK.ERewardType, Vector3> MapFlyGoals()
    {
        var hostGoals = FacadeGamePlay.GetFlyObjGoalPos?.Invoke();
        var result = new Dictionary<WZSDK.ERewardType, Vector3>();
        if (hostGoals == null) return result;

        foreach (var kv in hostGoals)
        {
            switch (kv.Key)
            {
                case HostERewardType.Money:
                    result[WZSDK.ERewardType.Money] = kv.Value;
                    result[WZSDK.ERewardType.LuckyWalletMoney] = kv.Value;
                    break;
                case HostERewardType.Prop1:
                    result[WZSDK.ERewardType.AddBottle] = kv.Value;
                    break;
                case HostERewardType.Prop2:
                    result[WZSDK.ERewardType.Clear] = kv.Value;
                    break;
                case HostERewardType.Prop3:
                    result[WZSDK.ERewardType.Undo] = kv.Value;
                    break;
            }
        }

        return result;
    }

    private static void ShowHostRewardedViaWz()
    {
        if (AdsControl.Instance == null)
        {
            FacadeAd.RewardAdNotReady?.Invoke();
            return;
        }

        AdsControl.Instance.ShowRewardedAd(
            "Prop_reward",
            success =>
            {
                if (success)
                    FacadeAd.RewardAdReceivedReward?.Invoke("MAX", 0, 0, "unknown", 1);
                else
                    FacadeAd.RewardAdClosed?.Invoke("MAX", 0, 0, "unknown");
            },
            autoShowWithdrawMissionView: false);
    }

    private static void ShowHostInterstitialViaWz()
    {
        if (AdsControl.Instance == null)
        {
            FacadeAd.InterstitialAdNotReady?.Invoke();
            return;
        }

        AdsControl.Instance.ShowInterstitialAd();
    }

    private static void OnWzInterstitialHidden(string adUnitId, MaxSdkBase.AdInfo adInfo)
    {
        double revenue = adInfo != null ? adInfo.Revenue : 0d;
        string precision = adInfo != null ? adInfo.RevenuePrecision : "unknown";
        FacadeAd.InterstitialAdClosed?.Invoke("MAX", revenue, 0, precision);
    }

    private static void OnWzInterstitialDisplayFailed(string adUnitId, MaxSdkBase.ErrorInfo errorInfo, MaxSdkBase.AdInfo adInfo)
    {
        FacadeAd.InterstitialAdDisplayFailed?.Invoke("MAX", errorInfo != null ? errorInfo.Message : "display failed");
    }

    private static bool MaxInterstitialReadySafe()
    {
        try
        {
            var ads = AdsControl.Instance;
            if (ads == null) return false;
            string unitId = ads.maxInterstitialAdUnitId;
            if (string.IsNullOrEmpty(unitId) || unitId.StartsWith("YOUR_", StringComparison.Ordinal))
                return false;
            return MaxSdk.IsInterstitialReady(unitId);
        }
        catch
        {
            return false;
        }
    }

    private static void SyncIaaFlag()
    {
        // Game2 / WZ 入口以 WZ 侧为准；否则把宿主 ifIAA 同步给 WZ。
        if (IsWzEntryScene() && GameManagerWZ.instance != null)
            global::GameDefines.ifIAA = WZSDK.GameDefines.ifIAA;
        else
            WZSDK.GameDefines.ifIAA = global::GameDefines.ifIAA;
    }

    private static void SyncLevelFromHost()
    {
        int hostLevel = FacadePlayer.GetLevel?.Invoke() ?? 1;
        SyncLevelToWz(hostLevel);
    }

    private static void SyncLevelToWz(int level)
    {
        if (GameManagerWZ.instance == null) return;
        int safe = Mathf.Max(level, 1);
        GameManagerWZ.instance.currentLv = safe;
        PlayerPrefs.SetInt(FacadePlayerPrefExtend.currentLevel, safe);
    }

    public static bool IsWzEntryScene()
    {
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (!scene.IsValid()) return false;
        if (scene.name == "Game2") return true;
        string path = scene.path ?? string.Empty;
        return path.Replace('\\', '/').Contains("/WZ/Scenes/");
    }

    /// <summary>
    /// Game2 场景缺少宿主 Game 时，运行时补齐入口对象。
    /// </summary>
    public static void EnsureHostGameForWzEntry()
    {
        if (!IsWzEntryScene()) return;
        EnsureHostUiCamera();
        if (Game.Instance != null) return;

        var go = new GameObject("--- WaterSort Host Entry ---");
        UnityEngine.Object.DontDestroyOnLoad(go);
        go.AddComponent<Game>();
    }

    /// <summary>
    /// 宿主 UIManager 需要 Canvas/UICamera；WZ Canvas 只有 Main Camera，这里补齐同名节点。
    /// </summary>
    public static void EnsureHostUiCamera()
    {
        var canvasGo = GameObject.Find("Canvas");
        if (canvasGo == null) return;

        Transform canvas = canvasGo.transform;
        if (canvas.Find("UICamera") != null) return;

        var mainCamGo = GameObject.Find("Main Camera");
        Camera sourceCam = mainCamGo != null ? mainCamGo.GetComponent<Camera>() : null;
        if (sourceCam == null)
            sourceCam = UnityEngine.Object.FindObjectOfType<Camera>();
        if (sourceCam == null) return;

        var uiCamGo = new GameObject("UICamera");
        uiCamGo.transform.SetParent(canvas, false);
        uiCamGo.transform.position = sourceCam.transform.position;
        uiCamGo.transform.rotation = sourceCam.transform.rotation;
        var uiCam = uiCamGo.AddComponent<Camera>();
        uiCam.CopyFrom(sourceCam);
        uiCam.depth = sourceCam.depth + 1;

        var canvasComp = canvasGo.GetComponent<Canvas>();
        if (canvasComp != null && canvasComp.renderMode != RenderMode.ScreenSpaceOverlay)
            canvasComp.worldCamera = uiCam;
    }
}

/// <summary>
/// 场景常驻启动器：Game2 补宿主入口，宿主 Run 后接线 WZ。
/// </summary>
[DefaultExecutionOrder(-1000)]
public class WaterSortWZBootstrap : MonoBehaviour
{
    private static WaterSortWZBootstrap _instance;
    private bool _initialized;
    private bool _hostEnsured;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureExists()
    {
        if (_instance != null) return;
        var go = new GameObject(nameof(WaterSortWZBootstrap));
        DontDestroyOnLoad(go);
        _instance = go.AddComponent<WaterSortWZBootstrap>();
    }

    private void Awake()
    {
        TryEnsureHost();
    }

    private void Update()
    {
        TryEnsureHost();

        if (_initialized) return;
        if (Game.Instance == null || Game.Instance.GameState != EGameState.Run) return;
        if (GameManagerWZ.instance == null) return;

        WaterSortWZBridge.InitializeOrSync();
        _initialized = true;
    }

    private void TryEnsureHost()
    {
        if (_hostEnsured) return;
        if (!WaterSortWZBridge.IsWzEntryScene()) return;
        WaterSortWZBridge.EnsureHostGameForWzEntry();
        _hostEnsured = Game.Instance != null;
    }
}
