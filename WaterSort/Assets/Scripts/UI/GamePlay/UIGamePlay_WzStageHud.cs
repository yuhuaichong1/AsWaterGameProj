using UnityEngine;
using UnityEngine.UI;
using WZSDK;
using HostGameDefines = global::GameDefines;
using WzGameDefines = WZSDK.GameDefines;
using WzUIManager = WZSDK.UIManager;

namespace XrCode
{
    /// <summary>
    /// 从 GamePlayerView 移植的 LuckyRoot / Stage3Root 阶段 HUD。
    /// </summary>
    public partial class UIGamePlay
    {
        private const int VisibleStageCount = 3;
        private const int LuckyVisibleCount = 7;
        private const int Stage3Id = 3;
        private const int Stage4Id = 4;
        private const int Stage5Id = 5;
        private const int AdMissionType = 4;
        private const int DefaultAdMissionTarget = 3;
        private const float DeferredStage3ProgressAnimationSpeed = 0.5f;
        private const string GamePlayerViewPrefabPath = "View/GamePlayerView";

        private RectTransform Stage3Root;
        private RectTransform ProgressStateRoot;
        private RectTransform LuckyRoot;
        private RectTransform LuckyRootProgressStateRoot;
        private Image Stage3Progress;
        private Image Stage3Fill;
        private Image LuckyProgress;
        private Text OverallCount;
        private Text Stage3Dec;
        private Text ProgressTxT;
        private Text LuckyCurCoin;

        private readonly WithdrawMissionStateItem[] progressStates = new WithdrawMissionStateItem[VisibleStageCount];
        private readonly LuckyItemProgress[] luckyProgressStates = new LuckyItemProgress[LuckyVisibleCount];

        private bool wzStageHudReady;
        private bool hasDisplayedStage3ProgressValue;
        private bool hasDeferredStage3ProgressUpdate;
        private float displayedStage3ProgressValue;
        private float deferredStage3TargetProgressValue;
        private int displayedStage3StageId;
        private int deferredStage3StageId;
        private bool displayedStage3ShowProgressStates;
        private bool deferredStage3ShowProgressStates;
        private int displayedStage3ProgressStateTargetCount;
        private int deferredStage3ProgressStateTargetCount;

        private GameManagerWZ GM => GameManagerWZ.instance;

        private void EnsureWzStageHud()
        {
            if (wzStageHudReady)
                return;

            // 与宿主 UIGamePlay 一致，使用全局 GameDefines.ifIAA（由桥接同步）。
            if (HostGameDefines.ifIAA)
                return;

            // 与 WLProgress 同级挂到 Plane/Top，便于替换宿主旧进度条区域。
            Transform top = null;
            if (mWLProgress != null)
                top = mWLProgress.transform.parent;
            if (top == null && mTransform != null)
                top = mTransform.Find("Plane/Top");
            if (top == null)
                return;

            LuckyRoot = top.Find("LuckyRoot") as RectTransform
                        ?? top.Find("WzStageHudHost/LuckyRoot") as RectTransform;
            Stage3Root = top.Find("Stage3Root") as RectTransform
                         ?? top.Find("WzStageHudHost/Stage3Root") as RectTransform;

            if (LuckyRoot == null || Stage3Root == null)
                TryCloneWzStageHudFromGamePlayerView(top);

            BindWzStageHudRefs();
            wzStageHudReady = LuckyRoot != null || Stage3Root != null;
        }

        private void TryCloneWzStageHudFromGamePlayerView(Transform topParent)
        {
            var prefab = Resources.Load<GameObject>(GamePlayerViewPrefabPath);
            if (prefab == null)
            {
                Debug.LogWarning("[UIGamePlay] 无法加载 GamePlayerView 预制体，LuckyRoot/Stage3Root 未移植。");
                return;
            }

            var temp = UnityEngine.Object.Instantiate(prefab);
            temp.name = "GamePlayerView_HudCloneSource";
            temp.SetActive(false);

            try
            {
                Transform srcTopButtons = FindChildByPath(temp.transform, "Area/TopButtons");
                if (srcTopButtons == null)
                    return;

                // Top 与 TopButtons 坐标系不同：建一个等价容器，保留预制体本地坐标。
                RectTransform hudHost = EnsureWzStageHudHost(topParent as RectTransform, srcTopButtons as RectTransform);
                if (hudHost == null)
                    return;

                if (LuckyRoot == null)
                {
                    Transform srcLucky = srcTopButtons.Find("LuckyRoot");
                    if (srcLucky != null)
                    {
                        var cloned = UnityEngine.Object.Instantiate(srcLucky.gameObject, hudHost, false);
                        cloned.name = "LuckyRoot";
                        LuckyRoot = cloned.transform as RectTransform;
                        LuckyRoot.SetAsLastSibling();
                    }
                }

                if (Stage3Root == null)
                {
                    Transform srcStage3 = srcTopButtons.Find("Stage3Root");
                    if (srcStage3 != null)
                    {
                        var cloned = UnityEngine.Object.Instantiate(srcStage3.gameObject, hudHost, false);
                        cloned.name = "Stage3Root";
                        Stage3Root = cloned.transform as RectTransform;
                        Stage3Root.SetAsLastSibling();
                    }
                }
            }
            finally
            {
                UnityEngine.Object.Destroy(temp);
            }
        }

        private static RectTransform EnsureWzStageHudHost(RectTransform topParent, RectTransform srcTopButtons)
        {
            if (topParent == null)
                return null;

            Transform existing = topParent.Find("WzStageHudHost");
            if (existing != null)
                return existing as RectTransform;

            var hostGo = new GameObject("WzStageHudHost", typeof(RectTransform));
            var host = hostGo.GetComponent<RectTransform>();
            host.SetParent(topParent, false);

            if (srcTopButtons != null)
            {
                host.anchorMin = srcTopButtons.anchorMin;
                host.anchorMax = srcTopButtons.anchorMax;
                host.pivot = srcTopButtons.pivot;
                host.anchoredPosition = srcTopButtons.anchoredPosition;
                host.sizeDelta = srcTopButtons.sizeDelta;
                host.localRotation = srcTopButtons.localRotation;
                host.localScale = srcTopButtons.localScale;
            }
            else
            {
                host.anchorMin = new Vector2(0f, 1f);
                host.anchorMax = new Vector2(1f, 1f);
                host.pivot = new Vector2(0.5f, 0.5f);
                host.anchoredPosition = new Vector2(0f, -172.7f);
                host.sizeDelta = new Vector2(0f, 345.4f);
            }

            return host;
        }

        private static void ResetHudAnchors(RectTransform rt)
        {
            if (rt == null)
                return;
            rt.SetAsLastSibling();
        }

        private void BindWzStageHudRefs()
        {
            if (Stage3Root != null)
            {
                Stage3Progress = FindHudImage(Stage3Root, "BG/Progress", "Progress");
                Stage3Fill = FindHudImage(Stage3Root, "OverallProgress/Fill", "Fill");
                OverallCount = FindHudText(Stage3Root, "OverallProgress/OverallCount", "OverallCount");
                ProgressStateRoot = FindChildByPath(Stage3Root, "ProgressStateRoot") as RectTransform
                                    ?? FindChildUtility.FindChild(Stage3Root, "ProgressStateRoot")?.transform as RectTransform;
                Stage3Dec = FindHudText(Stage3Root, "bg/Stage3Dec", "Stage3Dec");
                ProgressTxT = FindHudText(Stage3Root, "BG/ProgressTxT", "ProgressTxT");
                Stage3Root.gameObject.SetActive(false);
            }

            if (LuckyRoot != null)
            {
                LuckyProgress = FindHudImage(LuckyRoot, "BG/LuckyProgress", "LuckyProgress");
                LuckyRootProgressStateRoot = FindChildByPath(LuckyRoot, "LuckyRootProgressStateRoot") as RectTransform
                                            ?? FindChildUtility.FindChild(LuckyRoot, "LuckyRootProgressStateRoot")?.transform as RectTransform;
                LuckyCurCoin = FindHudText(LuckyRoot, "bg/CurCoin", "CurCoin");
                LuckyRoot.gameObject.SetActive(false);
            }

            if (ProgressStateRoot != null)
            {
                for (int i = 0; i < VisibleStageCount && i < ProgressStateRoot.childCount; i++)
                    progressStates[i] = GetOrAddStateItem(ProgressStateRoot.GetChild(i).gameObject);
            }

            if (LuckyRootProgressStateRoot != null)
            {
                for (int i = 0; i < LuckyVisibleCount && i < LuckyRootProgressStateRoot.childCount; i++)
                    luckyProgressStates[i] = GetOrAddLuckyItemProgress(LuckyRootProgressStateRoot.GetChild(i).gameObject);
            }
        }

        private static Image FindHudImage(Transform root, string path, string fallbackName)
        {
            Transform t = FindChildByPath(root, path);
            if (t == null && !string.IsNullOrEmpty(fallbackName))
            {
                // 仅在目标路径下按名查找，避免命中 ProgressState 等其它同名节点。
                Transform pathParent = root;
                int slash = path.LastIndexOf('/');
                if (slash >= 0)
                {
                    Transform parent = FindChildByPath(root, path.Substring(0, slash));
                    if (parent != null)
                        pathParent = parent;
                }

                t = pathParent.Find(fallbackName);
                if (t == null)
                    t = FindChildUtility.FindChild(pathParent, fallbackName)?.transform;
            }

            return t != null ? t.GetComponent<Image>() : null;
        }

        private static Text FindHudText(Transform root, string path, string fallbackName)
        {
            Transform t = FindChildByPath(root, path);
            if (t == null && !string.IsNullOrEmpty(fallbackName))
                t = FindChildUtility.FindChild(root, fallbackName)?.transform;
            return t != null ? t.GetComponent<Text>() : null;
        }

        private void RefreshWzStageHud()
        {
            EnsureWzStageHud();
            if (!wzStageHudReady || HostGameDefines.ifIAA)
                return;

            RefreshLuckyProgress();
            UpdateWithdrawPrompt();
        }

        protected override void OnUpdate()
        {
            UpdateDeferredStage3ProgressAnimation();
        }

        private void RefreshLuckyProgress()
        {
            if (LuckyRoot == null)
                return;

            int unlockLevel = WzGameDefines.LuckyWalletUnlockLevel;
            bool shouldShowLuckyRoot = GM != null && GM.currentLv >= unlockLevel;
            LuckyRoot.gameObject.SetActive(shouldShowLuckyRoot);

            if (!shouldShowLuckyRoot || GM == null)
            {
                if (LuckyProgress != null)
                    LuckyProgress.fillAmount = 0f;
                return;
            }

            if (LuckyCurCoin != null)
                LuckyCurCoin.text = GM.mark + GM.GetCoin().ToString($"F{Mathf.Max(GM.decimals, 0)}");

            int groupStartLevel = GetLuckyGroupStartLevel(GM.currentLv);
            int completedCount = Mathf.Clamp(GM.currentLv - groupStartLevel, 0, LuckyVisibleCount);

            if (LuckyProgress != null)
                LuckyProgress.fillAmount = (float)completedCount / LuckyVisibleCount;

            for (int i = 0; i < luckyProgressStates.Length; i++)
            {
                int displayLevel = groupStartLevel + i;
                bool isFinished = GM.currentLv > displayLevel;
                bool isLuckyLevel = UserDataManager.Instance != null
                    && UserDataManager.Instance.ShouldShowLuckyRewardIcon(displayLevel);

                LuckyItemProgress stateItem = luckyProgressStates[i];
                stateItem?.SetState(displayLevel, isFinished, isLuckyLevel);
                stateItem?.SetLuckyRewardIconVisible(isLuckyLevel);
            }
        }

        private int GetLuckyGroupStartLevel(int currentLevel)
        {
            int unlock = WzGameDefines.LuckyWalletUnlockLevel;
            if (currentLevel <= unlock)
                return unlock;

            int offsetLevel = Mathf.Max(currentLevel - unlock, 0);
            int groupIndex = offsetLevel / LuckyVisibleCount;
            return unlock + groupIndex * LuckyVisibleCount;
        }

        private void UpdateWithdrawPrompt()
        {
            if (GM == null)
                return;

            if (ShouldPreviewStage4PromptWhileWithdrawFeedbackVisible())
            {
                if (TryRefreshStage45Prompt(Stage4Id))
                    return;
            }

            if (GM.currentStage == Stage3Id)
            {
                RefreshStage3Prompt();
                return;
            }

            if ((GM.currentStage == Stage4Id || GM.currentStage == Stage5Id)
                && TryRefreshStage45Prompt(GM.currentStage))
            {
                return;
            }

            SetStage3RootState(false);
        }

        private void RefreshStage3Prompt()
        {
            SetStage3RootState(true);
            SetStage3ProgressStateRootVisible(true);

            int missionTarget = GetStageTarget(Stage3Id, DefaultAdMissionTarget);
            int watchedAds = Mathf.Max(GM.GetStageAdWatchCount(), 0);
            float progressValue = Mathf.Clamp01((float)Mathf.Clamp(watchedAds, 0, missionTarget) / Mathf.Max(missionTarget, 1));

            if (ProgressTxT != null)
                ProgressTxT.text = watchedAds + "/" + missionTarget;

            RefreshStage3ProgressVisuals(Stage3Id, progressValue, true, missionTarget);
        }

        private bool TryRefreshStage45Prompt(int stageId)
        {
            if (GM == null
                || !GM.TryGetStageRequirement(stageId, out int stageType, out int stageTarget)
                || stageTarget <= 0
                || (stageType != 2 && stageType != 3))
            {
                return false;
            }

            SetStage3RootState(true);
            SetStage3ProgressStateRootVisible(false);

            float progressValue;
            string progressText;

            if (stageType == 2)
            {
                float currentCoin = GM.currentCoin;
                progressValue = Mathf.Clamp01(currentCoin / stageTarget);
                progressText = $"{GM.GetCoin()}/{stageTarget}";
            }
            else
            {
                int loginDay = GM.getLoginDay();
                progressValue = Mathf.Clamp01((float)loginDay / stageTarget);
                progressText = $"{loginDay}/{stageTarget}";
            }

            if (ProgressTxT != null)
                ProgressTxT.text = progressText;

            RefreshStage3ProgressVisuals(stageId, progressValue, false, 0);
            return true;
        }

        private bool ShouldPreviewStage4PromptWhileWithdrawFeedbackVisible()
        {
            return GM != null
                && GM.currentStage == Stage3Id
                && GM.IsStageCompleted(Stage3Id)
                && WzUIManager.instance != null
                && WzUIManager.instance.HasActiveView(WZSDK.EUIType.WithdrawFeedbackView);
        }

        private void SetStage3RootState(bool isVisible)
        {
            if (Stage3Root != null)
                Stage3Root.gameObject.SetActive(isVisible);

            if (!isVisible)
                ResetDeferredStage3ProgressState();
        }

        private void SetStage3ProgressStateRootVisible(bool isVisible)
        {
            if (ProgressStateRoot != null)
                ProgressStateRoot.gameObject.SetActive(isVisible);
        }

        private void SetStage3ProgressState(int completedCount, int targetCount)
        {
            int visibleCompletedCount = Mathf.Clamp(completedCount, 0, progressStates.Length);
            for (int i = 0; i < progressStates.Length; i++)
                progressStates[i]?.SetState(i + 1, i < visibleCompletedCount);
        }

        private void RefreshStage3ProgressVisuals(int stageId, float progressValue, bool showProgressStates, int progressStateTargetCount)
        {
            int safeStageId = Mathf.Max(stageId, 0);
            float safeProgressValue = Mathf.Clamp01(progressValue);
            int safeProgressStateTargetCount = Mathf.Max(progressStateTargetCount, 0);

            EnsureStage3VisualRefsBound();

            deferredStage3StageId = safeStageId;
            deferredStage3TargetProgressValue = safeProgressValue;
            deferredStage3ShowProgressStates = showProgressStates;
            deferredStage3ProgressStateTargetCount = safeProgressStateTargetCount;

            // 宿主 UIGamePlay：ProgressTxT 已即时更新，进度条/描述必须同步立即生效。
            // 不再依赖 WZ 弹层延迟（Game2 常有 Legacy/Tutorial 层导致永远 defer）。
            hasDeferredStage3ProgressUpdate = false;
            ApplyStage3ProgressVisualState(safeStageId, safeProgressValue, showProgressStates, safeProgressStateTargetCount);
            RememberDisplayedStage3ProgressState(safeStageId, safeProgressValue, showProgressStates, safeProgressStateTargetCount);
        }

        private void EnsureStage3VisualRefsBound()
        {
            if (Stage3Root == null)
                return;

            if (Stage3Progress == null)
                Stage3Progress = FindHudImage(Stage3Root, "BG/Progress", "Progress");
            if (Stage3Fill == null)
                Stage3Fill = FindHudImage(Stage3Root, "OverallProgress/Fill", null);
            if (OverallCount == null)
                OverallCount = FindHudText(Stage3Root, "OverallProgress/OverallCount", "OverallCount");
            if (Stage3Dec == null)
                Stage3Dec = FindHudText(Stage3Root, "bg/Stage3Dec", "Stage3Dec");
            if (ProgressTxT == null)
                ProgressTxT = FindHudText(Stage3Root, "BG/ProgressTxT", "ProgressTxT");

            // 克隆预制体偶发 Type=Simple，fillAmount 不产生可见变化。
            if (Stage3Progress != null && Stage3Progress.type != Image.Type.Filled)
                Stage3Progress.type = Image.Type.Filled;
            if (Stage3Fill != null && Stage3Fill.type != Image.Type.Filled)
                Stage3Fill.type = Image.Type.Filled;
        }

        private void UpdateDeferredStage3ProgressAnimation()
        {
            // 宿主侧已改为即时刷新，保留空实现以免 OnUpdate 调用报错。
        }

        private void ApplyStage3ProgressVisualState(int stageId, float progressValue, bool showProgressStates, int progressStateTargetCount)
        {
            EnsureStage3VisualRefsBound();
            float safeProgressValue = Mathf.Clamp01(progressValue);

            if (Stage3Dec != null)
                Stage3Dec.text = GetStage3MissionDescription(safeProgressValue);

            if (Stage3Progress != null)
                Stage3Progress.fillAmount = safeProgressValue;

            if (Stage3Fill != null)
                Stage3Fill.fillAmount = safeProgressValue;

            if (OverallCount != null)
                OverallCount.text = $"{Mathf.RoundToInt(safeProgressValue * 100f)}%";

            if (showProgressStates)
                SetStage3ProgressState(GetDisplayedStage3CompletedCount(safeProgressValue, progressStateTargetCount), progressStateTargetCount);
        }

        private int GetDisplayedStage3CompletedCount(float progressValue, int targetCount)
        {
            if (targetCount <= 0)
                return 0;

            float completedCount = Mathf.Clamp01(progressValue) * targetCount;
            return Mathf.Clamp(Mathf.FloorToInt(completedCount + 0.0001f), 0, targetCount);
        }

        private bool ShouldDeferStage3ProgressUpdate()
        {
            return false;
        }

        private void RememberDisplayedStage3ProgressState(int stageId, float progressValue, bool showProgressStates, int progressStateTargetCount)
        {
            displayedStage3StageId = Mathf.Max(stageId, 0);
            displayedStage3ProgressValue = Mathf.Clamp01(progressValue);
            displayedStage3ShowProgressStates = showProgressStates;
            displayedStage3ProgressStateTargetCount = Mathf.Max(progressStateTargetCount, 0);
            hasDisplayedStage3ProgressValue = true;
        }

        private void ResetDeferredStage3ProgressState()
        {
            hasDisplayedStage3ProgressValue = false;
            hasDeferredStage3ProgressUpdate = false;
            displayedStage3ProgressValue = 0f;
            deferredStage3TargetProgressValue = 0f;
            displayedStage3StageId = 0;
            deferredStage3StageId = 0;
            displayedStage3ShowProgressStates = false;
            deferredStage3ShowProgressStates = false;
            displayedStage3ProgressStateTargetCount = 0;
            deferredStage3ProgressStateTargetCount = 0;
        }

        private int GetStageTarget(int stageId, int fallbackValue)
        {
            if (GM != null
                && GM.TryGetStageRequirement(stageId, out int stageType, out int stageTarget)
                && stageType == AdMissionType
                && stageTarget > 0)
            {
                return stageTarget;
            }

            return fallbackValue;
        }

        private string GetStage3MissionDescription(float progressValue)
        {
            return GetStage3RootTargetText(GetRemainingProgressPercent(progressValue).ToString());
        }

        private string GetStage3RootTargetText(string targetValueText)
        {
            return string.Format(GetWzHudText("3041", "Watch {0} ads to withdraw all cash"), targetValueText);
        }

        private int GetRemainingProgressPercent(float progressValue)
        {
            float safeProgress = Mathf.Clamp01(progressValue);
            return Mathf.Clamp(Mathf.CeilToInt((1f - safeProgress) * 100f), 0, 100);
        }

        private string GetWzHudText(string key, string fallback)
        {
            if (LocalizationManager.Instance == null)
                return fallback;

            string value = LocalizationManager.Instance.GetText(key);
            return string.IsNullOrEmpty(value) ? fallback : value;
        }

        private WithdrawMissionStateItem GetOrAddStateItem(GameObject target)
        {
            if (target == null)
                return null;

            var component = target.GetComponent<WithdrawMissionStateItem>();
            if (component == null)
                component = target.AddComponent<WithdrawMissionStateItem>();
            return component;
        }

        private LuckyItemProgress GetOrAddLuckyItemProgress(GameObject target)
        {
            if (target == null)
                return null;

            var component = target.GetComponent<LuckyItemProgress>();
            if (component == null)
                component = target.AddComponent<LuckyItemProgress>();
            component.Initialize();
            return component;
        }

        private static Transform FindChildByPath(Transform root, string path)
        {
            if (root == null || string.IsNullOrEmpty(path))
                return null;

            Transform current = root;
            string[] parts = path.Split('/');
            for (int i = 0; i < parts.Length; i++)
            {
                if (current == null)
                    return null;
                current = current.Find(parts[i]);
            }

            return current;
        }
    }
}
