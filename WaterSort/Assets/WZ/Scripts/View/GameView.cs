using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace WZSDK
{
    public class GameView : BaseView
    {
        [Header("Text")]
        public Text levelTxt;
        public Text level1Txt;
        public Text coinTxt;
        public Text gemTxt;
        public Text remainUndoCount;
        public Text AddBottleTxtCount;
        public Text ClearCountTxtCount;
        public Text tipsTxt;

        [Header("Icons")]
        public Transform coinIconInBoard;
        public Transform CapRoot;
        public GameObject undoRWIcon;
        public GameObject addRwIcon;
        public GameObject clearRWIcon;
        public GameObject promptIcon;
        public GameObject guidIcon;

        [Header("UI Elements")]
        public GameObject booster;
        public GameObject boosterTut;
        public GameObject boosterMask;
        public GameObject tips;
        public GameObject MoneyRoot;
        public GameObject WithdrawTop;
        public GameObject MoneyIcon;
        public GameObject IAAMoneyIcon;
        public GameObject LV;
        public GameObject LV1;

        [Header("Buttons")]
        public Button replayBtn;
        public Button Clear;
        public Button UndoBtn;
        public Button AddBottle;
        public Button Setting;
        public Button CoinBoard;
        public Button GemBoard;

        [Header("Settings")]
        public bool unlockHintView;


        public float barrageStartDelay = 6f;
        public float barrageInterval = 20f;

        private bool _isEventsBound;
        private GameManagerWZ GM => GameManagerWZ.instance;
        private float barrageTimer = 0f;
        private bool isBarrageActive = false;

        public GameObject Ng1;
        public GameObject Ng2;
        public GameObject Ng3;
        public GameObject Ng4;
        public GameObject Ng5;
        public GameObject Arrow1;
        public GameObject Arrow2;
        public GameObject Arrow3;
        public GameObject Arrow4;
        public GameObject StageIcon3;
        public GameObject WithdrawPrompt;
        public Slider Process;
        public Text content;
        public Text num;
        [Header("Barrage")]
        public TextMarquee barrage;
        private List<LevelMilestone> milestones;

        [System.Serializable]
        public class LevelMilestone
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



        public override void Update()
        {
            /* if (!isBarrageActive || barrage == null) return;

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
             }*/
        }

        #region 弹幕功能
        private void StartBarrage()
        {

            if (barrage == null || GameManagerWZ.instance.currentLv <= 2 || GameDefines.ifIAA) return;

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

            /*  if (GM.currentState == GameManager.GAME_STATE.FINISH)
                  return true;*/
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
            CoinBoard?.gameObject.SetActive(false);
        }

        public void RefreshStageUI()
        {
            RefreshAllData();
        }

        private void ResolveReferencesIfNeeded()
        {
            levelTxt ??= FindText("Area/TopButtons/LV/levelText");
            level1Txt ??= FindText("Area/TopButtons/LV1/levelText");
            coinTxt ??= FindText("Area/TopButtons/CurMoney/Bg1/CoinTxt");
            gemTxt ??= FindText("Area/TopButtons/GemMoney/Bg1/GemTxt");
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
            LV1 ??= FindObject("Area/TopButtons/LV1");

            replayBtn ??= FindButton("Area/TopButtons/Replay");
            Clear ??= FindButton("Area/BoosterButtons/ClearBtn");
            UndoBtn ??= FindButton("Area/BoosterButtons/UndoBtn");
            AddBottle ??= FindButton("Area/BoosterButtons/AddBottleBtn");
            Setting ??= FindButton("Area/TopButtons/Setting");
            CoinBoard ??= FindButton("Area/TopButtons/CurMoney/CoinBtn");
            GemBoard ??= FindButton("Area/TopButtons/GemMoney/GemBtn");

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
            Process ??= FindSlider("Area/TopButtons/WithdrawPrompt/bg/Process");
            content ??= FindText("Area/TopButtons/WithdrawPrompt/bg/content");
            num ??= FindText("Area/TopButtons/WithdrawPrompt/bg/Process/Fill Area/num");
            barrage ??= FindObject("Area/MarquBG/Marque")?.GetComponent<TextMarquee>();
        }

        private GameObject FindObject(string path)
        {
            Transform child = transform.Find(path);
            return child != null ? child.gameObject : null;
        }

        private Text FindText(string path)
        {
            return FindObject(path)?.GetComponent<Text>();
        }

        private Button FindButton(string path)
        {
            return FindObject(path)?.GetComponent<Button>();
        }

        private Slider FindSlider(string path)
        {
            return FindObject(path)?.GetComponent<Slider>();
        }

        public void GetLocation()
        {
            promptIcon ??= GameObject.Find("Canvas-UIManger/GamePlayUI/Footer/btnHint");
            guidIcon ??= GameObject.Find("Canvas-UIManger/GamePlayUI/Footer/btnLines");
        }

        private void RefreshTipsByStageAndLevel()
        {
            if (GM == null || tipsTxt == null) return;
            tips?.gameObject.SetActive(false);
            int currentStage = GM.currentStage;
            int currentLevel = GM.currentLv;
            string tipsMessage = null;
            if (currentStage == 0)
                tipsMessage = GetLevelObjectivesByStage(1);
            else if (currentStage == 1)
                tipsMessage = GetLevelObjectivesByStage(2);
            else if (currentStage >= 2 && currentLevel > 2 && currentLevel < GameDefines.SmallLevelGroupEnd)
                tipsMessage = GetLevelObjectivesByStage(3);

            if (!string.IsNullOrEmpty(tipsMessage))
            {
                tipsTxt.text = LocalizationManager.Instance.GetText(tipsMessage);
                return;
            }

            tipsTxt.text = string.Format(LocalizationManager.Instance.GetText("1029"), currentLevel);
        }

        private string GetLevelObjectivesByStage(int stageId)
        {
            var stageData = GM.GetStageData(stageId);
            if (stageData != null && stageData.ContainsKey("LevelObjectives"))
            {
                string levelObjectives = (string)stageData["LevelObjectives"];
                if (!string.IsNullOrEmpty(levelObjectives))
                {
                    return levelObjectives;
                }
            }
            return null;
        }

        private void RefreshLevelInfo()
        {
            if (GM == null)
                return;

            string levelText = string.Format(LocalizationManager.Instance.GetText("1004"), GM.currentLv);
            if (levelTxt != null)
                levelTxt.text = levelText;
            if (level1Txt != null)
                level1Txt.text = levelText;
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
            if (itemObj == null)
                return;

            LevelItem levelItem = itemObj.GetComponent<LevelItem>();
            if (levelItem == null)
                return;
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
            if (coinTxt != null && GM != null)
                coinTxt.text = FacadePayTypeExtend.RegionalChangeHandle(GM.currentCoin);
        }

        private void RefreshGemInfo()
        {
            if (gemTxt != null && GM != null)
                gemTxt.text = GM.currentGems.ToString();//FacadePayTypeExtend.RegionalChangeHandle();
        }

        private Dictionary<ERewardType, Vector3> GetFlyObjGold()
        {
            Vector3 fallback = transform.position;
            return new Dictionary<ERewardType, Vector3>
            {
                { ERewardType.Money, coinTxt != null ? coinTxt.transform.position : fallback },
                { ERewardType.Gem, gemTxt != null ? gemTxt.transform.position : fallback },
                { ERewardType.AddBottle, AddBottleTxtCount != null ? AddBottleTxtCount.transform.position : fallback },
                { ERewardType.Clear, ClearCountTxtCount != null ? ClearCountTxtCount.transform.position : fallback },
                { ERewardType.Undo, remainUndoCount != null ? remainUndoCount.transform.position : fallback },
               /* { ERewardType.Prompt, promptIcon != null ? promptIcon.transform.position : fallback },
                { ERewardType.Guid, guidIcon != null ? guidIcon.transform.position : fallback },*/
            };
        }

        public void RefreshUndoInfo()
        {
            if (GM == null || remainUndoCount == null || undoRWIcon == null) return;
            int undo = GM.currentUndo;
            bool hasCount = undo > 0;
            undoRWIcon.SetActive(hasCount);
            if (hasCount) remainUndoCount.text = undo.ToString();
        }

        public void RefreshClearInfo()
        {
            if (GM == null || ClearCountTxtCount == null || clearRWIcon == null) return;
            int clear = GM.currentClear;
            bool hasCount = clear > 0;
            clearRWIcon.gameObject.SetActive(hasCount);
            if (hasCount) ClearCountTxtCount.text = clear.ToString();
        }

        public void RefreshBottleCount()
        {
            if (GM == null || AddBottleTxtCount == null || addRwIcon == null) return;
            int bottleCount = GM.currentAddBottleCount;
            bool hasCount = bottleCount > 0;
            addRwIcon.SetActive(hasCount);
            if (hasCount) AddBottleTxtCount.text = bottleCount.ToString();
        }

        private void UpdateMoneyDisplay()
        {
            bool isIAA = GameDefines.ifIAA;
            IAAMoneyIcon?.SetActive(isIAA);

            if (GM.currentLv > GameDefines.SmallLevelGroupEnd)
            {
                //LV?.SetActive(true);
                HideWithdrawTop();
            }
            else
            {
                //LV?.SetActive(true);
                WithdrawTop?.SetActive(!isIAA);
                //MoneyRoot?.SetActive(true);
            }
        }

        private void HideWithdrawTop()
        {
            if (WithdrawTop != null)
                WithdrawTop.SetActive(false);

            if (LV1 != null)
                LV1.SetActive(true);
            if (LV != null)
                LV.SetActive(false);

            if (tips != null)
                tips.SetActive(false);
        }

        public void ShowWithdrawTop()
        {
            bool isIAA = GameDefines.ifIAA;

            if (GM != null && GM.currentLv <= GameDefines.SmallLevelGroupEnd)
            {
                WithdrawTop?.SetActive(!isIAA);
                tips?.SetActive(!isIAA);
            }
            else
            {
                HideWithdrawTop();
            }
        }

        public void UpdateWithdrawPrompt()
        {
            if (GM == null || WithdrawPrompt == null || StageIcon3 == null || Process == null || content == null || num == null)
                return;

            int currentLevel = GM.currentLv;
            float currentCoin = GM.currentCoin;
            int currentStage = GM.currentStage;

            if (currentLevel > GameDefines.SmallLevelGroupEnd)
            {
                WithdrawPrompt.SetActive(true);

                if (currentStage == 4)
                {
                    StageIcon3.SetActive(false);

                    var stage4Data = GM.GetStageData(4);
                    float targetWithdraw = 0f;

                    if (stage4Data != null && stage4Data.ContainsKey("Value"))
                    {
                        float.TryParse((string)stage4Data["Value"], out targetWithdraw);
                    }

                    float remaining = targetWithdraw - currentCoin;

                    if (remaining <= 0)
                    {
                        content.text = string.Format(LocalizationManager.Instance.GetText("2097"), 0, targetWithdraw);
                    }
                    else
                    {
                        content.text = string.Format(LocalizationManager.Instance.GetText("2097"), remaining.ToString("F2"), targetWithdraw);
                    }

                    float progress = Mathf.Clamp01(currentCoin / targetWithdraw);
                    Process.value = progress;
                    num.text = GM.GetCoin() + "/" + targetWithdraw;
                    //LV?.SetActive(true);
                }
                else if (currentStage == 5)
                {
                    StageIcon3.SetActive(false);
                    var stage5Data = GM.GetStageData(5);
                    int targetDays = 0;

                    if (stage5Data != null && stage5Data.ContainsKey("Value"))
                    {
                        int.TryParse((string)stage5Data["Value"], out targetDays);
                    }

                    int loginDay = GM.getLoginDay();
                    int remainingDays = targetDays - loginDay;

                    if (remainingDays <= 0)
                    {
                        content.text = LocalizationManager.Instance.GetText("2098");
                        Process.value = 1f;
                        num.text = loginDay + "/" + targetDays;
                    }
                    else
                    {
                        string format = LocalizationManager.Instance.GetText("2098");
                        content.text = string.Format(format, remainingDays);
                        float progress = (float)loginDay / targetDays;
                        Process.value = Mathf.Clamp01(progress);
                        num.text = loginDay + "/" + targetDays;
                    }
                    //LV?.SetActive(true);
                }
                else
                {
                    WithdrawPrompt.SetActive(false);
                    StageIcon3.SetActive(false);
                    //LV?.SetActive(false);
                }
            }
            else
            {
                WithdrawPrompt.SetActive(false);
                StageIcon3.SetActive(false);
            }
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
            _isEventsBound = false;
        }

        private void PlayClick()
        {

        }

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

            if (GM.currentStage == 3)
            {
                dataModel.Num = GM.currentStage;
                dataModel.Special = 1;
            }
            else if (GM.currentStage >= 4)
            {
                dataModel.Num = GM.currentStage;
            }
            else
            {
                dataModel.Num = GM.currentStage + 1;
            }

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
            if (!GM) return;

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
            //MoneyRoot.gameObject.SetActive(false);
        }

        public void ShowWithdraw() => UpdateMoneyDisplay();

        private void OnDestroy()
        {
            if (_isEventsBound) RemoveEvents();
            StopBarrage();
            CancelInvoke();
        }


    }
}
