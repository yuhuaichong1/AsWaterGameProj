using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


namespace WZSDK
{
    public class GamePlayerView : BaseView
    {
        private const int VisibleStageCount = 3;
        private const int LuckyVisibleCount = 7;
        private const int Stage3Id = 3;
        private const int Stage4Id = 4;
        private const int Stage5Id = 5;
        private const int AdMissionType = 4;
        private const int DefaultAdMissionTarget = 3;
        private const float DeferredStage3ProgressAnimationSpeed = 0.5f;

        [Header("Text")]
        private Text levelTxt;
        private Text _coinTxt;
        private Text _gemTxt;
        private Text remainUndoCount;
        private Text AddBottleTxtCount;
        private Text ClearCountTxtCount;
        private Text tipsTxt;
        private Text OverallCount;
        private Text Stage3Dec;
        private Text ProgressTxT;
        private Text LuckyCurCoin;

        [Header("Icons")]
        private Transform coinIconInBoard;
  
        private GameObject undoRWIcon;
        private GameObject addRwIcon;
        private GameObject clearRWIcon;

        [Header("UI Elements")]
        private GameObject booster;
        private GameObject tips;
        private GameObject MoneyRoot;
        private GameObject WithdrawTop;
        private GameObject MoneyIcon;
        private RectTransform Stage3Root;
        private RectTransform ProgressStateRoot;
        private RectTransform LuckyRoot;
        private RectTransform LuckyRootProgressStateRoot;
        private Image Progress;
        private Image Fill;
        private Image LuckyProgress;

        private GameObject LV;

        [Header("Buttons")]
        private Button replayBtn;
        private Button Clear;
        private Button UndoBtn;
        private Button AddBottle;
        private Button Setting;
        public Button CoinBoard;
        public Button GemBoard;
        private Button SlotBtn;

     


       private float barrageStartDelay = 6f;
        private float barrageInterval = 20f;

        private bool _isEventsBound;
        private GameManagerWZ GM => GameManagerWZ.instance;
        private float barrageTimer = 0f;
        private bool isBarrageActive = false;

        private GameObject Ng1;
        private GameObject Ng2;
        private GameObject Ng3;
        private GameObject Ng4;
        private GameObject Ng5;
        private GameObject Arrow1;
        private GameObject Arrow2;
        private GameObject Arrow3;
        private GameObject Arrow4;
        private GameObject StageIcon3;
        private GameObject WithdrawPrompt;
        private Image Process;
        private Text content;
        private Text num;
        [Header("Barrage")]
        private TextMarquee barrage;
        private List<LevelMilestone> milestones;
        private readonly WithdrawMissionStateItem[] progressStates = new WithdrawMissionStateItem[VisibleStageCount];
        private readonly LuckyItemProgress[] luckyProgressStates = new LuckyItemProgress[LuckyVisibleCount];
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
        private GameObject promptIcon;
        private GameObject guidIcon;
        public Text coinTxt => _coinTxt;
        public Text gemTxt => _gemTxt;

        [System.Serializable]
        private class LevelMilestone
        {
            public int startLevel;
            public int endLevel;
            public int[] displayLevels;
        }
        public override void Start()
        {
            ResolveReferencesIfNeeded();
            barrage?.gameObject.SetActive(false);
        }

       
        public override void InitView()
        {
            ResolveReferencesIfNeeded();

            FacadeGamePlayExtend.GetFlyObjGoldHandle += GetFlyObjGold;
            GenerateMilestones();
            RefreshAllData();
            BindEvents();
            StartBarrage();
        }
        public void GetLocation()
        {
            promptIcon = GameObject.Find("Canvas-UIManger/GamePlayUI/Footer/btnHint");
            guidIcon = GameObject.Find("Canvas-UIManger/GamePlayUI/Footer/btnLines");
        }


        public override void Update()
        {
            UpdateDeferredStage3ProgressAnimation();

            if (!isBarrageActive || barrage == null) return;

            if (IsGamePaused())
            {
                if (!barrage.IsPaused())
                    barrage.Pause();
                return;
            }
            else
            {
                if (barrage.IsPaused())
                {
                    barrage.Resume();
                    barrageTimer = 0f;
                }
            }
            if (!barrage.gameObject.activeInHierarchy)
            {
                barrageTimer += Time.deltaTime;


                if (barrageTimer >= barrageInterval)
                {
                    barrageTimer = 0f;
                    ShowBarrage();
                }
            }
        }

        #region 弹幕功能
        private void StartBarrage()
        {

            if (barrage == null || GameManagerWZ.instance.currentLv <= 2) return;

            isBarrageActive = true;
            barrageTimer = 0f;
            Invoke(nameof(ShowFirstBarrage), barrageStartDelay);
        }

        private void ShowFirstBarrage()
        {
            if (!isBarrageActive || barrage == null) return;
            ShowBarrage();
            barrageTimer = 0f;
        }

        private void StopBarrage()
        {
            isBarrageActive = false;
            CancelInvoke(nameof(ShowFirstBarrage));
            if (barrage != null)
            {
                barrage.Pause();
                barrage.gameObject.SetActive(false);
            }
        }

        private void ShowBarrage()
        {
            if (!isBarrageActive || barrage == null) return;


            string barrage1 = GetBarrageText();
            string barrage2 = GetBarrageText();
            barrage.onScrollComplete = null;

            barrage.onScrollComplete = () =>
            {
                barrageTimer = 0f;
            };

            barrage.SetMarqueeText(barrage1, barrage2);
            barrage.gameObject.SetActive(true);
        }
        #endregion
        private bool IsGamePaused()
        {
            if (GM == null) return false;

            if (GM.currentState == GameManagerWZ.GAME_STATE.FINISH)
                return true;
            return false;
        }
        private string GetBarrageText()
        {
            var allBarrageData = UserDataManager.Instance.getAllBarrageData();
            if (allBarrageData == null || allBarrageData.Count == 0)
                return "";

            int range = UnityEngine.Random.Range(0, allBarrageData.Count);
            string name = (string)allBarrageData[range]["play"];
            int num = int.Parse((string)allBarrageData[range]["Stage"]);
            return string.Format(LocalizationManager.Instance.GetText("2102"), name, num);
        }
        private int GetTotalLevels() => GameDefines.SmallLevelGroupStart - 1;

        private void GenerateMilestones()
        {
            milestones = new List<LevelMilestone>();
            int withdrawLevel = 8;

            milestones.Add(new LevelMilestone
            {
                startLevel = 1,
                endLevel = 4,
                displayLevels = new int[] { 1, 2, 3, 4, withdrawLevel }
            });

            milestones.Add(new LevelMilestone
            {
                startLevel = 5,
                endLevel = 8,
                displayLevels = new int[] { 4, 5, 6, 7, withdrawLevel }
            });
        }

        private void RefreshAllData()
        {
            if (!GM) return;
            GetLocation();
            RefreshLevelInfo();
            RefreshCoinInfo();
            RefreshGemInfo();
            RefreshUndoInfo();
            RefreshBottleCount();
            RefreshClearInfo();
            RefreshProgressInfo();
            RefreshTipsByStageAndLevel();
            UpdateMoneyDisplay();
            UpdateWithdrawPrompt();
            RefreshLuckyProgress();
        }

        public void RefreshStageUI()
        {
            RefreshAllData();
        }

        private void RefreshTipsByStageAndLevel()
        {
            if (GM == null || tipsTxt == null) return;
            int currentStage = GM.currentStage;
            int currentLevel = GM.currentLv;

            string tipsMessage = GetLevelObjectivesByStage(currentStage);
            if (!string.IsNullOrEmpty(tipsMessage))
            {
                tips.gameObject.SetActive(false);
                tipsTxt.text = tipsMessage;
                return;
            }

            tips.gameObject.SetActive(false);
            tipsTxt.text = string.Format(LocalizationManager.Instance.GetText("1029"), currentLevel);
        }

        private string GetLevelObjectivesByStage(int stageId)
        {
            var stageData = GM.GetStageData(stageId);
            if (stageData == null)
                return null;

            int stageValue = 0;
            if (stageData.ContainsKey("Value"))
            {
                int.TryParse((string)stageData["Value"], out stageValue);
            }

            if (stageData.ContainsKey("LevelObjectives"))
            {
                string levelObjectives = (string)stageData["LevelObjectives"];
                if (!string.IsNullOrEmpty(levelObjectives))
                    return string.Format(LocalizationManager.Instance.GetText(levelObjectives), stageValue);
            }

            if (stageData.ContainsKey("Type")
                && int.TryParse((string)stageData["Type"], out int stageType)
                && stageType == 4)
                return string.Format(LocalizationManager.Instance.GetText("2148"), stageValue);

            return null;
        }

        private void ResolveReferencesIfNeeded()
        {
            levelTxt ??= FindText("Area/TopButtons/LV/levelText");
            _coinTxt ??= FindText("Area/TopButtons/CurMoney/Bg1/CoinTxt");
            _gemTxt ??= FindText("Area/TopButtons/GemMoney/Bg1/GemTxt");
            remainUndoCount ??= FindText("Area/BoosterButtons/UndoBtn/UndoRWIcon/Undocount");
            AddBottleTxtCount ??= FindText("Area/BoosterButtons/AddBottleBtn/AddBottleRWIcon/Addcount");
            ClearCountTxtCount ??= FindText("Area/BoosterButtons/ClearBtn/ClearRWIcon/Clearcount");
            tipsTxt ??= FindText("Area/TopButtons/Tips/tipsTxt");

            coinIconInBoard ??= FindObject("Area/TopButtons/CurMoney/Bg1/MoneyIcon")?.transform;

            undoRWIcon ??= FindObject("Area/BoosterButtons/UndoBtn/UndoRWIcon");
            addRwIcon ??= FindObject("Area/BoosterButtons/AddBottleBtn/AddBottleRWIcon");
            clearRWIcon ??= FindObject("Area/BoosterButtons/ClearBtn/ClearRWIcon");

            booster ??= FindObject("Area/BoosterButtons");
            tips ??= FindObject("Area/TopButtons/Tips");
            MoneyRoot ??= FindObject("Area/TopButtons/GemMoney");
            WithdrawTop ??= FindObject("Area/TopButtons/WithdrawTop");
            MoneyIcon ??= FindObject("Area/TopButtons/CurMoney/Bg1/MoneyIcon/MoneyIcon");
            LV ??= FindObject("Area/TopButtons/LV");

            replayBtn ??= FindButton("Area/TopButtons/Replay");
            Clear ??= FindButton("Area/BoosterButtons/ClearBtn");
            UndoBtn ??= FindButton("Area/BoosterButtons/UndoBtn");
            AddBottle ??= FindButton("Area/BoosterButtons/AddBottleBtn");
            Setting ??= FindButton("Area/TopButtons/Setting");
            CoinBoard ??= FindButton("Area/TopButtons/CurMoney/CoinBtn");
            GemBoard ??= FindButton("Area/TopButtons/GemMoney/GemBtn");
            SlotBtn ??= FindButton("Area/TopButtons/SlotBtn");

            Ng1 ??= FindObject("Area/TopButtons/WithdrawTop/bg/NG1");
            Ng2 ??= FindObject("Area/TopButtons/WithdrawTop/bg/NG2");
            Ng3 ??= FindObject("Area/TopButtons/WithdrawTop/bg/NG3");
            Ng4 ??= FindObject("Area/TopButtons/WithdrawTop/bg/NG4");
            Ng5 ??= FindObject("Area/TopButtons/WithdrawTop/bg/NG5");
            Arrow1 ??= FindObject("Area/TopButtons/WithdrawTop/bg/Arrow1");
            Arrow2 ??= FindObject("Area/TopButtons/WithdrawTop/bg/Arrow2");
            Arrow3 ??= FindObject("Area/TopButtons/WithdrawTop/bg/Arrow3");
            Arrow4 ??= FindObject("Area/TopButtons/WithdrawTop/bg/Arrow4");

            StageIcon3 ??= FindObject("Area/TopButtons/WithdrawPrompt/bg/icon");
            WithdrawPrompt ??= FindObject("Area/TopButtons/WithdrawPrompt");
            Process ??= FindImage("Area/TopButtons/WithdrawPrompt/bg/Process/Fill Area/Fill");
            content ??= FindText("Area/TopButtons/WithdrawPrompt/bg/content");
            num ??= FindText("Area/TopButtons/WithdrawPrompt/bg/Process/Fill Area/num");
            barrage ??= FindObject("Area/MarquBG/Marque")?.GetComponent<TextMarquee>();
            Stage3Root ??= FindObject("Area/TopButtons/Stage3Root")?.GetComponent<RectTransform>();
            Progress ??= FindObject("Area/TopButtons/Stage3Root/BG/Progress")?.GetComponent<Image>();
            Fill ??= FindObject("Area/TopButtons/Stage3Root/OverallProgress/Fill")?.GetComponent<Image>();
            LuckyProgress ??= FindObject("Area/TopButtons/LuckyRoot/BG/LuckyProgress")?.GetComponent<Image>();
            OverallCount ??= FindText("Area/TopButtons/Stage3Root/OverallProgress/OverallCount");
            ProgressStateRoot ??= FindObject("Area/TopButtons/Stage3Root/ProgressStateRoot")?.GetComponent<RectTransform>();
            LuckyRoot ??= FindObject("Area/TopButtons/LuckyRoot")?.GetComponent<RectTransform>();
            LuckyRootProgressStateRoot ??= FindObject("Area/TopButtons/LuckyRoot/LuckyRootProgressStateRoot")?.GetComponent<RectTransform>();
            Stage3Dec ??= FindText("Area/TopButtons/Stage3Root/bg/Stage3Dec");
            ProgressTxT ??= FindText("Area/TopButtons/Stage3Root/BG/ProgressTxT");
            LuckyCurCoin ??= FindText("Area/TopButtons/LuckyRoot/bg/CurCoin");

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

        private void RefreshLevelInfo()
        {
            if (GM != null && levelTxt != null)
            {
                levelTxt.text = string.Format(LocalizationManager.Instance.GetText("1004"), GM.currentLv);
            }
        }

        private GameObject FindObject(string path)
        {
            return FindChildUtility.FindChildByPath(gameObject, path);
        }

        private Text FindText(string path)
        {
            return FindObject(path)?.GetComponent<Text>();
        }

        private Button FindButton(string path)
        {
            return FindObject(path)?.GetComponent<Button>();
        }

      
        private Image FindImage(string path)
        {
            return FindObject(path)?.GetComponent<Image>();
        }

        private void RefreshProgressInfo()
        {
            if (GM == null) return;

            int curLevel = GM.currentLv;
            int totalLevels = GetTotalLevels();

            if (curLevel > totalLevels)
            {
                ShowPostGameProgress(curLevel);
                return;
            }

            int[] levels = GetLevelsToShow(curLevel);

            SetLevelItem(Ng1, levels[0], curLevel);
            SetLevelItem(Ng2, levels[1], curLevel);
            SetLevelItem(Ng3, levels[2], curLevel);
            SetLevelItem(Ng4, levels[3], curLevel);
            SetLevelItem(Ng5, levels[4], curLevel);

            SetArrowPosition(curLevel);
            ShowWithdrawTop();
        }

        private void RefreshLuckyProgress()
        {
            if (LuckyRoot == null)
                return;

            bool shouldShowLuckyRoot = GM != null && GM.currentLv >= GameDefines.LuckyWalletUnlockLevel;
            LuckyRoot.gameObject.SetActive(shouldShowLuckyRoot);

            if (!shouldShowLuckyRoot || GM == null)
            {
                if (LuckyProgress != null)
                    LuckyProgress.fillAmount = 0f;

                return;
            }

            int groupStartLevel = GetLuckyGroupStartLevel(GM.currentLv);
            int completedCount = Mathf.Clamp(GM.currentLv - groupStartLevel, 0, LuckyVisibleCount);

            if (LuckyProgress != null)
                LuckyProgress.fillAmount = (float)completedCount / LuckyVisibleCount;

            for (int i = 0; i < luckyProgressStates.Length; i++)
            {
                int displayLevel = groupStartLevel + i;
                bool isFinished = GM.currentLv > displayLevel;
                bool isLuckyLevel = UserDataManager.Instance.ShouldShowLuckyRewardIcon(displayLevel);

                LuckyItemProgress stateItem = luckyProgressStates[i];
                stateItem?.SetState(displayLevel, isFinished, isLuckyLevel);
                stateItem?.SetLuckyRewardIconVisible(isLuckyLevel);

            }
        }

        private int GetLuckyGroupStartLevel(int currentLevel)
        {
            if (currentLevel <= GameDefines.LuckyWalletUnlockLevel)
                return GameDefines.LuckyWalletUnlockLevel;

            int offsetLevel = Mathf.Max(currentLevel - GameDefines.LuckyWalletUnlockLevel, 0);
            int groupIndex = offsetLevel / LuckyVisibleCount;
            return GameDefines.LuckyWalletUnlockLevel + groupIndex * LuckyVisibleCount;
        }

        private void ShowPostGameProgress(int curLevel)
        {
            int totalLevels = GetTotalLevels();
            SetLevelItem(Ng1, totalLevels, curLevel);
            SetLevelItem(Ng2, totalLevels, curLevel);
            SetLevelItem(Ng3, totalLevels, curLevel);
            SetLevelItem(Ng4, totalLevels, curLevel);
            SetLevelItem(Ng5, curLevel, curLevel);

            HideAllArrows();
            Arrow4?.SetActive(true);

            if (curLevel > GameDefines.SmallLevelGroupEnd)
            {
                HideWithdrawTop();
            }
            else
            {
                ShowWithdrawTop();
            }
        }

        private void SetLevelItem(GameObject itemObj, int level, int currentLevel)
        {
            LevelItem levelItem = itemObj.GetComponent<LevelItem>();
            levelItem.SetLevel(level, currentLevel);
        }

        private void SetArrowPosition(int curLevel)
        {
            HideAllArrows();

            if (curLevel == 1) return;

            int arrowIndex;

            if (curLevel <= 4)
            {
                arrowIndex = curLevel - 2;
            }
            else
            {
                arrowIndex = (curLevel - 1) % 4;
            }

            switch (arrowIndex)
            {
                case 0: Arrow1?.SetActive(true); break;
                case 1: Arrow2?.SetActive(true); break;
                case 2: Arrow3?.SetActive(true); break;
                case 3: Arrow4?.SetActive(true); break;
            }
        }

        private void HideAllArrows()
        {
            Arrow1?.SetActive(false);
            Arrow2?.SetActive(false);
            Arrow3?.SetActive(false);
            Arrow4?.SetActive(false);
        }

        private int[] GetLevelsToShow(int curLevel)
        {
            foreach (var milestone in milestones)
            {
                if (curLevel >= milestone.startLevel && curLevel <= milestone.endLevel)
                {
                    return milestone.displayLevels;
                }
            }
            return GenerateDefaultLevels(curLevel);
        }

        private int[] GenerateDefaultLevels(int curLevel)
        {
            int totalLevels = GetTotalLevels();
            int start = Mathf.Max(1, curLevel - 2);
            int end = Mathf.Min(totalLevels, start + 4);

            if (end - start < 4)
            {
                start = Mathf.Max(1, end - 4);
            }

            return new int[5] { start, start + 1, start + 2, start + 3, end };
        }

        private void RefreshCoinInfo()
        {
            if (_coinTxt != null && GM != null)
                _coinTxt.text = FacadePayTypeExtend.RegionalChangeHandle(GM.currentCoin);
        }

        private void RefreshGemInfo()
        {
            if (_gemTxt != null && GM != null)
                _gemTxt.text = GM.currentGems.ToString();//FacadePayTypeExtend.RegionalChangeHandle();
        }

        private Dictionary<ERewardType, Vector3> GetFlyObjGold() => new()
    {
        { ERewardType.Money, _coinTxt.transform.position },
        { ERewardType.Gem, _gemTxt.transform.position },
        { ERewardType.AddBottle, AddBottleTxtCount.transform.position },
        { ERewardType.Clear, ClearCountTxtCount.transform.position },
        { ERewardType.Undo, remainUndoCount.transform.position },
            /*    { ERewardType.Prompt, promptIcon.transform.position },
        { ERewardType.Guid, guidIcon.transform.position },*/
    };

        public void RefreshUndoInfo()
        {
            if (GM == null || remainUndoCount == null) return;
            int undo = GM.currentUndo;
            bool hasCount = undo > 0;
            undoRWIcon.SetActive(hasCount);
            if (hasCount) remainUndoCount.text = undo.ToString();
        }

        public void RefreshClearInfo()
        {
            if (GM == null || ClearCountTxtCount == null) return;
            int clear = GM.currentClear;
            bool hasCount = clear > 0;
            clearRWIcon.gameObject.SetActive(hasCount);
            if (hasCount) ClearCountTxtCount.text = clear.ToString();
        }

        public void RefreshBottleCount()
        {
            if (GM == null || AddBottleTxtCount == null) return;
            int bottleCount = GM.currentAddBottleCount;
            bool hasCount = bottleCount > 0;
            //AddBottleTxtCount.gameObject.SetActive(hasCount);
            addRwIcon.gameObject.SetActive(hasCount);
            if (hasCount) AddBottleTxtCount.text = bottleCount.ToString();
        }

        private void UpdateMoneyDisplay()
        {
            LV?.SetActive(true);
            HideWithdrawTop();
        }

        private void HideWithdrawTop()
        {
            if (WithdrawTop != null)
                WithdrawTop.SetActive(false);

            if (tips != null)
                tips.SetActive(false);
        }

        private void ShowWithdrawTop()
        {
            HideWithdrawTop();
        }

        public void UpdateWithdrawPrompt()
        {
            if (GM == null) return;

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

            float currentCoin = GM.currentCoin;
            int currentStage = GM.currentStage;

            if (GM.TryGetStageRequirement(currentStage, out int stageType, out int stageTarget) && stageTarget > 0  && (stageType == 2 || stageType == 3))
            {
                WithdrawPrompt.SetActive(true);
                StageIcon3.SetActive(false);
                LV?.SetActive(true);

                if (stageType == 2)
                {
                    float remaining = stageTarget - currentCoin;

                    if (remaining <= 0)
                    {
                        content.text = string.Format(LocalizationManager.Instance.GetText("2097"), 0, stageTarget);
                    }
                    else
                    {
                        content.text = string.Format(LocalizationManager.Instance.GetText("2097"), remaining.ToString("F2"), stageTarget);
                    }

                    float progress = Mathf.Clamp01(currentCoin / stageTarget);
                    Process.fillAmount = progress;
                    num.text = GM.GetCoin() + "/" + stageTarget;
                }
                else
                {
                    int loginDay = GM.getLoginDay();
                    int remainingDays = stageTarget - loginDay;

                    if (remainingDays <= 0)
                    {
                        content.text = string.Format(LocalizationManager.Instance.GetText("2098"), 0);
                        Process.fillAmount = 1f;
                        num.text = loginDay + "/" + stageTarget;
                    }
                    else
                    {
                        string format = LocalizationManager.Instance.GetText("2098");
                        content.text = string.Format(format, remainingDays);
                        float progress = (float)loginDay / stageTarget;
                        Process.fillAmount = Mathf.Clamp01(progress);
                        num.text = loginDay + "/" + stageTarget;
                    }
                }
            }
            else
            {
                WithdrawPrompt.SetActive(false);
                StageIcon3.SetActive(false);
                LV?.SetActive(true);
            }
        }

        private void RefreshStage3Prompt()
        {
            SetStage3RootState(true);
            SetStage3ProgressStateRootVisible(true);
            StageIcon3?.SetActive(false);
            LV?.SetActive(true);

            int missionTarget = GetStageTarget(Stage3Id, DefaultAdMissionTarget);
            int watchedAds = Mathf.Max(GM.GetStageAdWatchCount(), 0);
            int clampedAds = Mathf.Clamp(watchedAds, 0, missionTarget);
            float progressValue = Mathf.Clamp01((float)clampedAds / Mathf.Max(missionTarget, 1));

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
            StageIcon3?.SetActive(false);
            LV?.SetActive(true);

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
                && UIManager.instance != null
                && UIManager.instance.HasActiveView(EUIType.WithdrawFeedbackView);
        }

        private void SetStage3RootState(bool isVisible)
        {
            if (Stage3Root != null)
                Stage3Root.gameObject.SetActive(isVisible);

            if (isVisible && WithdrawPrompt != null)
                WithdrawPrompt.SetActive(false);

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

            deferredStage3StageId = safeStageId;
            deferredStage3TargetProgressValue = safeProgressValue;
            deferredStage3ShowProgressStates = showProgressStates;
            deferredStage3ProgressStateTargetCount = safeProgressStateTargetCount;

            if (!hasDisplayedStage3ProgressValue)
            {
                ApplyStage3ProgressVisualState(safeStageId, safeProgressValue, showProgressStates, safeProgressStateTargetCount);
                RememberDisplayedStage3ProgressState(safeStageId, safeProgressValue, showProgressStates, safeProgressStateTargetCount);
                hasDeferredStage3ProgressUpdate = false;
                return;
            }

            bool hasStageChanged = displayedStage3StageId != safeStageId;
            bool hasDisplayModeChanged = displayedStage3ShowProgressStates != showProgressStates
                || displayedStage3ProgressStateTargetCount != safeProgressStateTargetCount;

            if (hasStageChanged || hasDisplayModeChanged)
            {
                hasDeferredStage3ProgressUpdate = false;
                ApplyStage3ProgressVisualState(safeStageId, safeProgressValue, showProgressStates, safeProgressStateTargetCount);
                RememberDisplayedStage3ProgressState(safeStageId, safeProgressValue, showProgressStates, safeProgressStateTargetCount);
                return;
            }

            if (ShouldDeferStage3ProgressUpdate())
            {
                hasDeferredStage3ProgressUpdate = !Mathf.Approximately(displayedStage3ProgressValue, deferredStage3TargetProgressValue);
                return;
            }

            if (hasDeferredStage3ProgressUpdate)
                return;

            ApplyStage3ProgressVisualState(safeStageId, safeProgressValue, showProgressStates, safeProgressStateTargetCount);
            RememberDisplayedStage3ProgressState(safeStageId, safeProgressValue, showProgressStates, safeProgressStateTargetCount);
        }

        private void UpdateDeferredStage3ProgressAnimation()
        {
            if (!hasDisplayedStage3ProgressValue || !hasDeferredStage3ProgressUpdate)
                return;

            if (Stage3Root == null || !Stage3Root.gameObject.activeInHierarchy)
            {
                ResetDeferredStage3ProgressState();
                return;
            }

            if (ShouldDeferStage3ProgressUpdate())
                return;

            displayedStage3ProgressValue = Mathf.MoveTowards(
                displayedStage3ProgressValue,
                deferredStage3TargetProgressValue,
                DeferredStage3ProgressAnimationSpeed * Time.unscaledDeltaTime);

            ApplyStage3ProgressVisualState(
                deferredStage3StageId,
                displayedStage3ProgressValue,
                deferredStage3ShowProgressStates,
                deferredStage3ProgressStateTargetCount);

            if (!Mathf.Approximately(displayedStage3ProgressValue, deferredStage3TargetProgressValue))
                return;

            displayedStage3ProgressValue = deferredStage3TargetProgressValue;
            ApplyStage3ProgressVisualState(
                deferredStage3StageId,
                displayedStage3ProgressValue,
                deferredStage3ShowProgressStates,
                deferredStage3ProgressStateTargetCount);
            RememberDisplayedStage3ProgressState(
                deferredStage3StageId,
                displayedStage3ProgressValue,
                deferredStage3ShowProgressStates,
                deferredStage3ProgressStateTargetCount);
            hasDeferredStage3ProgressUpdate = false;
        }

        private void ApplyStage3ProgressVisualState(int stageId, float progressValue, bool showProgressStates, int progressStateTargetCount)
        {
            float safeProgressValue = Mathf.Clamp01(progressValue);

            if (Stage3Dec != null)
                Stage3Dec.text = GetStage3MissionDescription(safeProgressValue);

            if (Progress != null)
                Progress.fillAmount = safeProgressValue;

            if (Fill != null)
                Fill.fillAmount = safeProgressValue;

            if (OverallCount != null)
                OverallCount.text = $"{Mathf.RoundToInt(safeProgressValue * 100f)}%";

            if (showProgressStates)
                SetStage3ProgressState(GetDisplayedStage3CompletedCount(safeProgressValue, progressStateTargetCount), progressStateTargetCount);
        }

        private int GetDisplayedStage3CompletedCount(float progressValue, int targetCount)
        {
            if (targetCount <= 0)
                return 0;

            float safeProgressValue = Mathf.Clamp01(progressValue);
            float completedCount = safeProgressValue * targetCount;
            return Mathf.Clamp(Mathf.FloorToInt(completedCount + 0.0001f), 0, targetCount);
        }

        private bool ShouldDeferStage3ProgressUpdate()
        {
            return UIManager.instance != null && !UIManager.instance.IsGameplayOnlyUIActive();
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
            return string.Format(GetText("3041", "Watch {0} ads to withdraw all cash"), targetValueText);
        }

        private int GetRemainingProgressPercent(float progressValue)
        {
            float safeProgress = Mathf.Clamp01(progressValue);
            return Mathf.Clamp(Mathf.CeilToInt((1f - safeProgress) * 100f), 0, 100);
        }

        private string GetText(string key, string fallback)
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

            WithdrawMissionStateItem component = target.GetComponent<WithdrawMissionStateItem>();
            if (component == null)
                component = target.AddComponent<WithdrawMissionStateItem>();

            return component;
        }

        private LuckyItemProgress GetOrAddLuckyItemProgress(GameObject target)
        {
            if (target == null)
                return null;

            LuckyItemProgress component = target.GetComponent<LuckyItemProgress>();
            if (component == null)
                component = target.AddComponent<LuckyItemProgress>();

            component.Initialize();
            return component;
        }

        private void BindEvents()
        {
            if (_isEventsBound) RemoveEvents();

            replayBtn?.onClick.AddListener(OnReplayClick);
            Clear?.onClick.AddListener(OnClearClick);
            UndoBtn?.onClick.AddListener(OnUndoClick);
            AddBottle?.onClick.AddListener(OnAddBottleClick);
            Setting?.onClick.AddListener(OnSettingClick);
            CoinBoard?.onClick.AddListener(() => OnBoardClick());
            GemBoard?.onClick.AddListener(() => UIManager.instance.ShowView(EUIType.WithdrawView));
            SlotBtn?.onClick.AddListener(() => UIManager.instance.ShowView(EUIType.SlotView));

            _isEventsBound = true;
        }

        private void RemoveEvents()
        {
            FacadeGamePlayExtend.GetFlyObjGoldHandle -= GetFlyObjGold;
            replayBtn?.onClick.RemoveAllListeners();
            Clear?.onClick.RemoveAllListeners();
            UndoBtn?.onClick.RemoveAllListeners();
            AddBottle?.onClick.RemoveAllListeners();
            Setting?.onClick.RemoveAllListeners();
            CoinBoard?.onClick.RemoveAllListeners();
            GemBoard?.onClick.RemoveAllListeners();
            SlotBtn?.onClick.RemoveAllListeners();
            _isEventsBound = false;
        }

        private void PlayClick() => SoundManager.Instance?.PlayUIClickSFX();

        private void OnReplayClick()
        {
            PlayClick();
            UIManager.instance.ShowView(EUIType.ReStartView);
        }

        private void OnSettingClick()
        {
            PlayClick();
            UIManager.instance.ShowView(EUIType.SettingsView);
        }

        private void OnBoardClick()
        {
            DataModel dataModel = new DataModel();
            dataModel.Level = GM.currentLv - 1;
            dataModel.Money = GM.GetCoin();
            dataModel.CloseType = 1;
            dataModel.Num = Mathf.Max(GM.currentStage, 1);

            GameApp.viewManager.Open(ViewType.WithDraw, dataModel);
        }


        private void OnClearClick()
        {
            if (!GM) return;

            if (GM.currentClear > 0)
                GM.UseClear();
            else
                UIManager.instance.ShowView(EUIType.PropView, 2);
        }

        private void OnUndoClick()
        {
            if (!GM) return;

            if (GM.currentUndo > 0)
                GM.UseUndo();
            else
                UIManager.instance.ShowView(EUIType.PropView, 3);
        }

        private void OnAddBottleClick()
        {
            if (GM.currentAddBottleCount > 0)
                GM.UseAddBottle();
            else
                UIManager.instance.ShowView(EUIType.PropView, 1);
        }

        public void ShowBooster() => booster?.SetActive(true);
        public void HideBooster() => booster?.SetActive(false);

        public void HideWithdraw()
        {
            UpdateMoneyDisplay();
            WithdrawTop?.gameObject.SetActive(false);
            MoneyRoot?.gameObject.SetActive(false);
            Stage3Root?.gameObject.SetActive(false);
        }

        public void ShowWithdraw()
        {
            UpdateMoneyDisplay();
            UpdateWithdrawPrompt();
        }

        private void OnDestroy()
        {
            if (_isEventsBound) RemoveEvents();
            StopBarrage();
            CancelInvoke();
        }


    }
}
