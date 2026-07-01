
using AsGame.Data;
using AsGame.UI;
using AsGame.Water;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace XrCode
{
    public partial class GamePlayModule : BaseModule
    {
        private int curLevelIndex;
        private List<CupData> curLevelData;
        private Dictionary<int, Bottle> cups;
        private Dictionary<int, Bottle> shadows;
        private GameStatus status;
        private bool _shuffleMode;//是否处于刷新状态
        private bool _shuffleAnimating;
        private Bottle _selected;//被选中的瓶子
        private PourActionRecord pourAction;//上一次的操作结果
        Dictionary<int, Queue<int>> _pendingFullCupsByColor;
        private bool _isCheckingPack;
        private int _pendingPackOps;
        private bool _packCompleteCheckRunning;
        private readonly Dictionary<Pocket, Queue<int>> _pocketAnimQueues = new();
        private readonly HashSet<Pocket> _pocketAnimProcessing = new();
        private readonly HashSet<Pocket> _reservedPockets = new();
        private int _collected;
        private int _needCollect;
        private List<int> _pocketColors;
        private List<GameObject> _shuffleFxObjects;
        private Dictionary<int, GameObject> _shuffleFxByCupId;

        private bool canReduceProp1Count;
        private float[] pocketXPos;

        private bool LRBool;
        private STimer LRTimer;
        private int LSCount;
        private STimer LRPauseTimer;

        private STimer LCTime;

        private bool showUITip1;
        private bool showUITip2;
        private bool showUITip3;

        protected override void OnLoad()
        {
            curLevelData = new List<CupData>();
            cups = new Dictionary<int, Bottle>();
            shadows = new Dictionary<int, Bottle>();
            _pendingFullCupsByColor = new Dictionary<int, Queue<int>>();
            _pocketColors = new List<int>();
            _shuffleFxObjects = new List<GameObject>();
            _shuffleFxByCupId = new Dictionary<int, GameObject>();
            showUITip1 = SPlayerPrefs.GetBool(PlayerPrefDefines.showUITip1, true);
            showUITip2 = SPlayerPrefs.GetBool(PlayerPrefDefines.showUITip2, true);
            showUITip3 = SPlayerPrefs.GetBool(PlayerPrefDefines.showUITip3, true);

            float PInterval = Screen.width / 4;
            pocketXPos = new float[4] { PInterval * -1.35f, PInterval * -0.45f, PInterval * 0.45f, PInterval * 1.35f };

            FacadeAdd();

            LoadData();
            InitGame();

            LRTimer = STimerManager.Instance.CreateSTimer(GameDefines.ClockTime1, 0, true, false, () => { LRBool = true; });
            LRTimer.Pause();
            LRPauseTimer = STimerManager.Instance.CreateSTimer(GameDefines.HoldTime, 0, true, false, () => { LRTimer.Pause(); });
            LRPauseTimer.Pause();
        }

        #region Facade

        /// <summary>
        /// 添加Facade接口
        /// </summary>
        private void FacadeAdd()
        {
            FacadeGamePlay.StartLevel += StartLevel;
            FacadeGamePlay.CreateLevel += CreateLevel;
            FacadeGamePlay.Func_Porp1 += Func_Porp1;
            FacadeGamePlay.Func_Porp2 += Func_Porp2;
            FacadeGamePlay.Func_Porp3 += Func_Porp3;
            FacadeGamePlay.RePlay += RePlay;
            FacadeGamePlay.GetStatus += GetStatus;
            FacadeGamePlay.EndPorp1 += EndShuffleMode;
            FacadeGamePlay.ReStartLRTimer += ReStartLRTimer;
            FacadeGamePlay.GetLevelProgress += GetLevelProgress;
        }

        /// <summary>
        /// 去除Facade接口
        /// </summary>
        private void FacadeRemove()
        {
            FacadeGamePlay.StartLevel -= StartLevel;
            FacadeGamePlay.CreateLevel += CreateLevel;
            FacadeGamePlay.Func_Porp1 -= Func_Porp1;
            FacadeGamePlay.Func_Porp2 -= Func_Porp2;
            FacadeGamePlay.Func_Porp3 -= Func_Porp3;
            FacadeGamePlay.RePlay -= RePlay;
            FacadeGamePlay.GetStatus -= GetStatus;
            FacadeGamePlay.EndPorp1 -= EndShuffleMode;
            FacadeGamePlay.ReStartLRTimer += ReStartLRTimer;
            FacadeGamePlay.GetLevelProgress -= GetLevelProgress;
        }

        #endregion

        #region Get/Set

        private GameStatus GetStatus()
        {
            return status;
        }

        #endregion

        /// <summary>
        /// 加载持久化数据
        /// </summary>
        private void LoadData()
        {

        }

        /// <summary>
        /// 初始化游戏进程
        /// </summary>
        private void InitGame()
        {
            //CreateLevel();
        }

        /// <summary>
        /// 创建关卡
        /// </summary>
        private void StartLevel()
        {
            if(FacadeGuide.GetIfTutorial())
            {
                FacadeGuide.PlayGuideByTargetType();
            }
            else
            {
                CreateLevel();
            }
        }
        private void CreateLevel()
        {

            FacadeGamePlay.AbleProp1Btn(true);
            FacadeGamePlay.AbleProp2Btn(false);
            FacadeGamePlay.AbleProp3Btn(false);
            FacadeGamePlay.SetShuffleTipShow(false);

            ClearBoard();
            pourAction = null;
            _selected = null;
            _shuffleMode = false;
            CheckNewPlayUnlock();

            curLevelIndex = FacadePlayer.GetLevel();

            FacadeGamePlay.SetLevelShow();
            GetLevelData(curLevelIndex);
            GenerateCups();
            BuildPocketColors();
            GeneratePockets();
            foreach (var kv in cups)
            {
                if (kv.Value == null || !kv.Value.IsCollect())
                    continue;
                CheckUnlockCup(kv.Value.GetTopColorId());
                RegisterFullCup(kv.Value);
            }
            BeginCheckPack();

            status = GameStatus.Gaming;

            if (curLevelIndex > 3)
            {
                LRBool = false;
                //LRTimer.targetTime = curLevelIndex <= GameDefines.ClockLv ? GameDefines.ClockTime1 : GameDefines.ClockTime2;
                int inarId = GameDefines.ClockLvArr.ToList().GetRangeIndex(curLevelIndex);
                LRTimer.targetTime = GameDefines.ClockTimeArr[inarId];
                LRTimer.ReStart();
                LRPauseTimer.Stop();
                LoopPlayCongratulationEffect(true);
            }

            FacadeGamePlay.ScrollingTipAnim();
            
            ShowUITip();
        }

        /// <summary>
        /// 清理场景中的物体
        /// </summary>
        void ClearBoard()
        {
            _pendingFullCupsByColor.Clear();
            _isCheckingPack = false;
            _pendingPackOps = 0;
            _packCompleteCheckRunning = false;
            _pocketAnimQueues.Clear();
            _pocketAnimProcessing.Clear();
            _reservedPockets.Clear();
            foreach (var kv in cups)
                if (kv.Value != null) GameObject.Destroy(kv.Value.gameObject);
            cups.Clear();
            shadows.Clear();
            foreach (Transform c in FacadeGamePlay.GetCupPart()) GameObject.Destroy(c.gameObject);
            foreach (Transform c in FacadeGamePlay.GetCupPartShadow()) GameObject.Destroy(c.gameObject);
            foreach (Transform c in FacadeGamePlay.GetPockets()) GameObject.Destroy(c.gameObject);
        }

        void CheckNewPlayUnlock()
        {
            // 旧版“新玩法解锁弹窗”逻辑已随框架改版废弃，保留空实现以兼容调用点。
        }

        /// <summary>
        /// 获取当前关卡数据
        /// </summary>
        /// <param name="levelIndex">关卡 id</param>
        private void GetLevelData(int levelIndex)
        {
            TextAsset jsonDataTA = ResourceMod.Instance.SyncLoad<TextAsset>($"Json/Levels/level_{levelIndex}.json");

            LevelConfig config = JsonConvert.DeserializeObject<LevelConfig>(jsonDataTA.text);

            List<CupData> cupList = new List<CupData>();
            for (int i = 0; i < config.cups.Count; i++)
            {
                var jsonCup = config.cups[i];
                CupData cup = new CupData
                {
                    id = i,
                    position = new Vector2(jsonCup.x, jsonCup.y),
                    colors = jsonCup.colors ?? new List<int>(),
                    whNums = jsonCup.whNums,
                    whMask = jsonCup.whMask,
                    isVideo = jsonCup.isVideo,
                    isLock = jsonCup.isLock,
                    lockColor = jsonCup.lockColor,
                    lockNums = jsonCup.lockNums,
                    isNull = jsonCup.isNull,
                    isEmptyCup = jsonCup.isEmptyCup
                };
                cupList.Add(cup);
            }
            curLevelData = cupList;
            //curLevelData = LevelConfigLoader.LoadLevel(levelIndex);


            if (curLevelData == null || curLevelData.Count == 0)
                D.Error($"level_{levelIndex} is not exist or empty");
        }

        /// <summary>
        /// 生成水瓶
        /// </summary>
        private void GenerateCups()
        {
            for (int i = 0; i < curLevelData.Count; i++)
            {
                CupData data = curLevelData[i];

                if (data.isNull != 0)
                {
                    cups[data.id] = null;
                    continue;
                }

                Vector3 pos = new Vector3(data.position.x, data.position.y + GameConstants.HalfBottleHeight, 0);
                Bottle bottle = Bottle.Create(FacadeGamePlay.GetCupPart());
                bottle.name = $"Bottle_{i}";
                bottle.transform.localPosition = pos;
                bottle.Init(data, OnCupClick);

                var shadow = Bottle.CreateShadow(FacadeGamePlay.GetCupPartShadow());
                shadow.transform.localPosition = pos + new Vector3(
                GameConstants.BottleShadowDiffX,
                -GameConstants.BottleHeight + GameConstants.BottleShadowDiffY, 0);
                bottle.BindShadow(shadow);

                cups[data.id] = bottle;
                shadows[data.id] = shadow;

                //RefreshAddBottleButton();
            }
        }

        private void BuildPocketColors()
        {
            _pocketColors.Clear();
            _needCollect = 0;
            _collected = 0;
            Dictionary<int, int> colorCount = new Dictionary<int, int>();
            foreach (CupData cup in curLevelData)
            {
                if (cup.isNull != 0) continue;
                foreach (int c in cup.colors)
                {
                    if (!colorCount.ContainsKey(c)) colorCount[c] = 0;
                    colorCount[c]++;
                    if (colorCount[c] % 4 == 0)
                    {
                        _pocketColors.Add(c);
                        colorCount[c] = 0;
                    }
                }

                _needCollect += cup.colors.Count;
            }

            _needCollect /= 4;
            if (curLevelIndex != 1)
                _pocketColors.Shuffle();
        }

        /// <summary>
        /// 生成饮料
        /// </summary>
        private void GeneratePockets()
        {
            for (var i = 0; i < 4; i++)
            {
                bool locked = i > 4;
                int color = locked ? 0 : (_pocketColors.Count > 0 ? _pocketColors[0] : 0);
                if (!locked && _pocketColors.Count > 0) _pocketColors.RemoveAt(0);
                Pocket pocket = Pocket.Create(FacadeGamePlay.GetPockets(), locked);
                RectTransform rt = (RectTransform)pocket.transform;
                rt.anchoredPosition = new Vector2(pocketXPos[i], 0);
                pocket.Init(locked, color, OnUnlockPocket);
            }
        }

        /// <summary>
        /// 当水瓶被点击
        /// </summary>
        /// <param name="cup">被点击的水瓶</param>
        private void OnCupClick(Bottle cup)
        {
            CheckOpenLuckyReward();

            if (cup.IsVideo())
            {
                FacadeAd.PlayInterAd(EAdSource.UnlockBottle, (count) => { cup.UnlockVideo(); }, (errMsg) => { cup.UnlockVideo(); });
                //FacadeAd.PlayROIAdByWeight(EAdSource.UnlockBottle, (count) => { cup.UnlockVideo(); }, (errMsg) => { cup.UnlockVideo(); }, () => { cup.UnlockVideo(); }, GameDefines.WeightAdRange, GameDefines.AdWeight);
                //FacadeAd.PlayRewardAd(EAdSource.UnlockBottle, (count) =>
                //{
                //    cup.UnlockVideo();
                //}, null, () =>
                //{
                //    cup.UnlockVideo();
                //});
                return;
            }

            if (status == GameStatus.UsingProp && _shuffleMode)
            {
                //if (_shuffleAnimating) return;
                if (cup.IsUnShuffle())
                {
                    UIManager.Instance.OpenNotice2(FacadeLanguage.GetText("10096"));
                    return;
                }

                Game.Instance.StartCoroutine(Func_Prop1_Func(cup));
                return;
            }

            if (status != GameStatus.Gaming) return;
            if (cup.IsLock() || cup.Pouring) return;

            if (_selected == null)
            {
                // 对应 Cocos：仅非空瓶可作为倒出源，空瓶可作为接水目标。
                if (cup.IsEmpty()) return;
                _selected = cup;
                cup.DoSelect();
                return;
            }

            if (_selected.Pouring) return;

            if (_selected == cup)
            {
                cup.DoUnSelect();
                _selected = null;
                return;
            }

            if (CheckPour(_selected, cup))
            {
                var from = _selected;
                _selected = null;
                PourRoutine(from, cup);
            }
            else
            {
                //UIManager.Instance.OpenNotice2(cup.IsFull() ? FacadeLanguage.GetText("10091") : FacadeLanguage.GetText("10092"));
                _selected.DoUnSelect();
                _selected = cup;
                cup.DoSelect();
            }
        }

        private void OnUnlockPocket(Pocket pocket)
        {
            if (_pocketColors.Count <= 0)
            {
                UIManager.Instance.OpenNotice2(FacadeLanguage.GetText("10097"));
                return;
            }

            FacadeAd.PlayInterAd(EAdSource.UnlockPocket, (count) => { OnUnlockPocket2(pocket); }, (errMsg) => { OnUnlockPocket2(pocket); });
            //FacadeAd.PlayROIAdByWeight(EAdSource.UnlockPocket, (count) => { OnUnlockPocket2(pocket); }, (errMsg) => { OnUnlockPocket2(pocket); }, () => { OnUnlockPocket2(pocket); }, GameDefines.WeightAdRange, GameDefines.AdWeight);
        }

        private void OnUnlockPocket2(Pocket pocket)
        {
            pocket.Init(false, _pocketColors.Count > 0 ? _pocketColors[0] : 0);
            if (_pocketColors.Count > 0) _pocketColors.RemoveAt(0);
            BeginCheckPack();
        }

        /// <summary>
        /// 检查两个水瓶顶部颜色对不对
        /// </summary>
        /// <param name="from">要倒水的水瓶</param>
        /// <param name="to">被倒水的水瓶</param>
        /// <returns>能不能倒</returns>
        private bool CheckPour(Bottle from, Bottle to)
        {
            if (from.Pouring || to.Pouring) return false;
            if (from.IsEmpty()) return false;
            if (!to.IsEmpty())
            {
                if (to.IsFull()) return false;
                if (from.GetTopColorId() != to.GetTopColorId()) return false;
            }

            return true;
        }

        #region 水瓶倒水动画流程

        /// <summary>
        /// 水瓶倒水动画
        /// </summary>
        /// <param name="from"></param>
        /// <param name="to"></param>
        private void PourRoutine(Bottle from, Bottle to)
        {
            Game.Instance.StartCoroutine(PourRoutine2(from, to));
        }

        private IEnumerator PourRoutine2(Bottle from, Bottle to)
        {
            var color = from.GetTopColorId();
            var pourNum = Mathf.Min(from.GetLayerPourWater(), to.GetLayerAddWater());
            if (pourNum <= 0)
                yield break;

            pourAction = new PourActionRecord
            {
                fromId = from.GetId(),
                toId = to.GetId(),
                colorId = color,
                num = pourNum
            };
            FacadeGamePlay.AbleProp2Btn(true);

            var dir = from.transform.localPosition.x > to.transform.localPosition.x ? 1 : -1;
            var pourPos = to.transform.localPosition + new Vector3(dir < 0 ? 20 : -20, 0, 0);

            var targetStartHeight = GameConstants.WaterMaxY[Mathf.Clamp(to.Data.colors.Count, 0, GameConstants.WaterMaxY.Length - 1)];
            var streamEndRootY = to.GetWaterSurfaceRootY(targetStartHeight);

            Coroutine waterInRoutine = null;
            yield return from.WaterOut(color, pourNum, dir, pourPos, streamEndRootY, () =>
            {
                waterInRoutine = Game.Instance.StartCoroutine(to.WaterIn(color, pourNum));
            });
            if (waterInRoutine != null)
                yield return waterInRoutine;

            if (from == null || to == null) yield break;

            if (to.IsCollect())
            {
                pourAction = null;
                FacadeGamePlay.AbleProp2Btn(false);
                FacadePlayer.AddPlayerExp(1 + curLevelIndex / 3);
                to.RefreshVisual();
                Game.Instance.StartCoroutine(CollectAndPackRoutine(to));
            }
        }

        private IEnumerator CollectAndPackRoutine(Bottle cup)
        {
            if (cup == null) yield break;
            yield return cup.DoCollected();
            CheckUnlockCup(cup.GetTopColorId());
            RegisterFullCup(cup);
            BeginCheckPack();
        }

        void RegisterFullCup(Bottle cup)
        {
            if (cup == null || !cup.IsCollect()) return;
            var color = cup.GetTopColorId();
            var id = cup.GetId();
            if (!_pendingFullCupsByColor.TryGetValue(color, out var queue))
            {
                queue = new Queue<int>();
                _pendingFullCupsByColor[color] = queue;
            }

            foreach (var existing in queue)
            {
                if (existing == id) return;
            }

            queue.Enqueue(id);
        }

        /// <summary>对齐 Cocos：装袋在后台进行，不阻塞其他水瓶操作。</summary>
        void BeginCheckPack()
        {
            Game.Instance.StartCoroutine(CheckPack());
        }

        private IEnumerator CheckPack()
        {
            if (_isCheckingPack) yield break;
            _isCheckingPack = true;
            try
            {
                while (true)
                {
                    List<PackPair2> batch = BatchCheckPack();
                    if (batch.Count == 0) break;
                    for (var i = 0; i < batch.Count; i++)
                    {
                        var item = batch[i];
                        Game.Instance.StartCoroutine(HandlePack(item.cup, item.pocket, i * 0.2f));
                    }
                }
            }
            finally
            {
                _isCheckingPack = false;
            }

            Game.Instance.StartCoroutine(WaitPackCompleteAndFinalize());
        }

        private IEnumerator WaitPackCompleteAndFinalize()
        {
            if (_packCompleteCheckRunning) yield break;
            _packCompleteCheckRunning = true;
            try
            {
                do
                {
                    while (_pendingPackOps > 0)
                        yield return null;
                    yield return null;
                } while (_pendingPackOps > 0 || _isCheckingPack);

                if (_collected >= _needCollect)
                    yield return WinRoutine();
                else
                    CheckOpenLuckySpin();

                LoopPlayCongratulationEffect(false);
            }
            finally
            {
                _packCompleteCheckRunning = false;
            }
        }

        /// <summary>
        /// 检测是否应该弹幸运转盘
        /// </summary>
        private void CheckOpenLuckySpin()
        {
            if (curLevelIndex > 3)
                LSCount++;

            if (LSCount == GameDefines.SpinCount)
            {
                if (!GameDefines.ifIAA)
                {
                    UIManager.Instance.OpenAsync<UILuckySpin>(EUIType.EUILuckySpin);
                }
                LSCount = 0;
            }

            
        }

        /// <summary>
        /// 检测是否应该弹幸运奖励
        /// </summary>
        private void CheckOpenLuckyReward()
        {
            if (curLevelIndex > 3)
            {
                LRPauseTimer.ReStart();
                if (LRTimer.STimerState == STimerState.Pause)
                {
                    LRTimer.Start();
                }
            }

            if (LRBool)
            {
                LRBool = false;
                //LRTimer.ReStart();
                UIManager.Instance.OpenAsync<UILuckyReward>(EUIType.EUILuckyReward);
            }
        }

        private void ReStartLRTimer()
        {
            LRTimer.ReStart();
            LRPauseTimer.Stop();
        }

        private List<PackPair2> BatchCheckPack()
        {
            var result = new List<PackPair2>();
            foreach (Transform child in FacadeGamePlay.GetPockets())
            {
                var pocket = child.GetComponent<Pocket>();
                if (pocket == null || pocket.IsLocked || pocket.PackColorId <= 0) continue;
                if (_reservedPockets.Contains(pocket)) continue;
                if (!_pendingFullCupsByColor.TryGetValue(pocket.PackColorId, out var queue) || queue.Count == 0)
                    continue;

                Bottle cup = null;
                while (queue.Count > 0)
                {
                    var cupId = queue.Dequeue();
                    if (!cups.TryGetValue(cupId, out var c) || c == null || !c.IsCollect())
                        continue;
                    cup = c;
                    break;
                }

                if (cup != null)
                {
                    _reservedPockets.Add(pocket);
                    result.Add(new PackPair2 { cup = cup, pocket = pocket });
                }
            }

            return result;
        }

        private IEnumerator HandlePack(Bottle cup, Pocket pocket, float delay)
        {
            if (cup == null || pocket == null)
            {
                if (pocket != null) _reservedPockets.Remove(pocket);
                yield break;
            }
            _pendingPackOps++;

            if (delay > 0f)
                yield return new WaitForSeconds(delay);

            var id = cup.GetId();
            var packedColor = cup.GetTopColorId();

            //SpineService.ClearEffects(cup.transform);
            if (shadows.TryGetValue(id, out var shadow) && shadow != null)
            {
                GameObject.Destroy(shadow.gameObject);
                shadows.Remove(id);
            }

            cup.transform.SetAsLastSibling();
            Vector3 worldTarget = pocket.transform.position;
            Vector3 localTarget = cup.transform.parent.InverseTransformPoint(worldTarget) + new Vector3(0, 100f, 0);
            yield return TweenHelper.MoveLocal(cup.transform, localTarget, 0.2f);

            GameObject.Destroy(cup.gameObject);
            cups[id] = null;
            foreach (var slot in curLevelData)
            {
                if (slot.id == id)
                {
                    ResetSlotAsPacked(slot);
                    break;
                }
            }

            AddFlyMoney(pocket.transform);
            _collected++;
            FacadeGamePlay.AbleProp3Btn(GetEmptySlotId() != null);
            SchedulePocketPack(pocket, packedColor);
        }

        void SchedulePocketPack(Pocket pocket, int packedColor)
        {
            if (!_pocketAnimQueues.TryGetValue(pocket, out var queue))
            {
                queue = new Queue<int>();
                _pocketAnimQueues[pocket] = queue;
            }

            queue.Enqueue(packedColor);
            if (_pocketAnimProcessing.Contains(pocket)) return;
            Game.Instance.StartCoroutine(ProcessPocketPackQueue(pocket));
        }

        private IEnumerator ProcessPocketPackQueue(Pocket pocket)
        {
            _pocketAnimProcessing.Add(pocket);
            while (_pocketAnimQueues.TryGetValue(pocket, out var queue) && queue.Count > 0)
            {
                var packedColor = queue.Dequeue();
                yield return pocket.OnPocketAction(packedColor);
                RefillPocket(pocket);
                _pendingPackOps--;
            }

            _pocketAnimProcessing.Remove(pocket);
        }

        private void AddFlyMoney(Transform startPos)
        {
            float moneyCount = GameDefines.ifIAA ? GameDefines.IAA_Elimination_Money : GameDefines.Elimination_Money;
            FacadePlayer.AddMoney(moneyCount);
            FacadeEffect.PlayFlyMoney(startPos, GameDefines.FlyMoney_FlyMoneyCount, moneyCount, () => { FacadeGamePlay.SetCurMoneyShow(); });
        }

        static void ResetSlotAsPacked(CupData slot)
        {
            if (slot == null) return;
            slot.colors.Clear();
            slot.whNums = 0;
            slot.whMask = 0;
            slot.isVideo = 0;
            slot.isLock = 0;
            slot.lockColor = 0;
            slot.lockNums = 0;
            slot.isNull = 1;
        }

        private IEnumerator WinRoutine()
        {
            status = GameStatus.Win;
            foreach (var kv in cups)
                if (kv.Value != null)
                    yield return kv.Value.DisappearEmpty();

            yield return new WaitForSeconds(0.4f);

            LRTimer.Stop();
            LRPauseTimer.Stop();

            NetworkModule.Instance.GetNetworkInitInfo2(() => 
            {
                UIManager.Instance.OpenAsync<UILevelCompleted>(EUIType.EUILevelCompleted, UIOpenType.None, null, curLevelIndex);
                FacadePlayer.AddLevel(1);
                Game.Instance.UILoadingWaiting.gameObject.SetActive(false);
                Game.Instance.UILoadingWaiting.StopTextAnim();
            }, () =>
            {
                Game.Instance.UILoadingWaiting.gameObject.SetActive(true);
                Game.Instance.UILoadingWaiting.StartTextAnim();
            });
        }

        /// <summary>任意水瓶倒满（4 层同色）时减少锁瓶次数；与口袋装袋无关。</summary>
        private void CheckUnlockCup(int fullBottleColor)
        {
            foreach (var kv in cups)
            {
                var cup = kv.Value;
                if (cup == null || !cup.IsLock()) continue;
                var lockColor = cup.GetLockColor();
                if (lockColor != 0 && lockColor != fullBottleColor) continue;
                cup.SetLockNum(cup.GetLockNum() - 1);
            }
        }


        void RefillPocket(Pocket pocket)
        {
            var next = _pocketColors.Count > 0 ? _pocketColors[0] : 0;
            if (_pocketColors.Count > 0) _pocketColors.RemoveAt(0);
            pocket.SetColor(next);
            _reservedPockets.Remove(pocket);
            BeginCheckPack();
        }

        #endregion

        /// <summary>
        /// 重新开始
        /// </summary>
        private void RePlay()
        {
            StartLevel();
        }

        #region 下三功能

        /// <summary>
        /// 刷新功能（开启刷新检测）
        /// </summary>
        private void Func_Porp1()
        {
            //status = GameStatus.UsingProp;
            //_shuffleMode = true;
            FacadeGamePlay.SetShuffleTipShow(true);

            var any = false;
            foreach (var kv in cups)
            {
                var cup = kv.Value;
                if (cup == null || cup.IsUnShuffle()) continue;
                any = true;
                cup.StartShuffleShake();
                AddShuffleEffect(cup);
            }

            if (!any) return;
            status = GameStatus.UsingProp;
            _shuffleMode = true;
            _shuffleAnimating = false;
            FacadeGamePlay.SetShuffleTipShow(true);
        }

        private void AddShuffleEffect(Bottle cup)
        {
            if (FacadeGamePlay.GetCupPart() == null || cup == null) return;
            var anchor = new GameObject("ShuffleFxAnchor", typeof(RectTransform));
            var rt = anchor.GetComponent<RectTransform>();
            rt.SetParent(FacadeGamePlay.GetCupPart(), false);
            rt.localPosition = cup.transform.localPosition + new Vector3(0f, -GameConstants.HalfBottleHeight - 110f, 0f);
            rt.localScale = Vector3.one * 0.8f;
            // 插在对应瓶子之前绘制，光环在瓶身/水体下层（对齐 Cocos：杯子层盖住 effectFront 光环）
            rt.SetSiblingIndex(cup.transform.GetSiblingIndex());

            //SpineService.PlayEffect(rt, Vector3.zero, "xuan_zhong", "idle", loop: true);
            GameObject RefreshEffectPath =  GameObject.Instantiate(ResourceMod.Instance.SyncLoad<GameObject>(GameDefines.RefreshEffectPath), rt);

            _shuffleFxObjects.Add(anchor);
            _shuffleFxByCupId[cup.GetId()] = anchor;
        }

        /// <summary>
        /// 刷新功能具体执行逻辑
        /// </summary>
        /// <param name="cup"></param>
        private IEnumerator Func_Prop1_Func(Bottle cup)
        {
            /*
            //if (cup.IsEmpty() || cup.IsLock()) return;

            //func1timer = 0;
            //FacadeGamePlay.SetShuffleTipShow(false);

            //STimerManager.Instance.CreateSTimer(GameDefines.RefreshATime, GameDefines.RefreshLCount, true, true, () => 
            //{
            //    List<int> colors = cup.Data.colors;
            //    if (colors.Count >= 2)
            //        (colors[0], colors[^1]) = (colors[^1], colors[0]);
            //    cup.RefreshVisual();

            //    func1timer++;
            //    if (func1timer > GameDefines.RefreshLCount)
            //        EndShuffleMode();
            //});
            */

            if (cup == null || cup.IsUnShuffle()) yield break;
            _shuffleAnimating = true;
            var colors = cup.Data.colors;
            for (var step = 0; step < 20; step++)
            {
                if (!_shuffleMode)
                {
                    _shuffleAnimating = false;
                    yield break;
                }

                if (colors.Count == 2)
                    (colors[0], colors[1]) = (colors[1], colors[0]);
                else
                    ShuffleColors(colors);
                cup.RefreshVisual();
                yield return new WaitForSeconds(0.02f);
            }

            if (!_shuffleMode)
            {
                _shuffleAnimating = false;
                yield break;
            }

            var topBefore = colors.Count > 0 ? colors[^1] : 0;
            ShuffleColors(colors);
            var diffIdx = -1;
            for (var i = 0; i < colors.Count; i++)
            {
                if (colors[i] == topBefore) continue;
                diffIdx = i;
                break;
            }

            if (diffIdx >= 0)
            {
                var moved = colors[diffIdx];
                colors.RemoveAt(diffIdx);
                colors.Add(moved);
            }

            cup.RefreshVisual();
            _shuffleAnimating = false;

            canReduceProp1Count = true;
            EndShuffleMode();
        }

        static void ShuffleColors(List<int> colors)
        {
            for (var i = colors.Count - 1; i > 0; i--)
            {
                var j = UnityEngine.Random.Range(0, i + 1);
                (colors[i], colors[j]) = (colors[j], colors[i]);
            }
        }

        /// <summary>
        /// 结束刷新功能
        /// </summary>
        void EndShuffleMode()
        {
            foreach (var kv in cups)
                kv.Value?.DoUnShuffle();
            ClearShuffleEffects();
            _shuffleMode = false;
            _shuffleAnimating = false;
            status = GameStatus.Gaming;
            FacadeGamePlay.SetShuffleTipShow(false);

            _shuffleMode = false;
            status = GameStatus.Gaming;

            if(canReduceProp1Count)
            {
                canReduceProp1Count = false;
                FacadePlayer.AddProp1Num(-1);
                FacadeGamePlay.SetProp1CountShow();
                UIManager.Instance.OpenNotice(FacadeLanguage.GetText("10094"));
            }
        }

        void ClearShuffleEffects()
        {
            foreach (var go in _shuffleFxObjects)
            {
                if (go != null) GameObject.Destroy(go);
            }

            _shuffleFxObjects.Clear();
            _shuffleFxByCupId.Clear();
        }

        /// <summary>
        /// 回退功能
        /// </summary>
        private void Func_Porp2()
        {
            if (pourAction == null) return;
            if (!cups.TryGetValue(pourAction.fromId, out var from) || from == null) return;
            if (!cups.TryGetValue(pourAction.toId, out var to) || to == null) return;

            var num = pourAction.num;
            var color = pourAction.colorId;
            for (var i = 0; i < num; i++)
                if (to.Data.colors.Count > 0)
                    to.Data.colors.RemoveAt(to.Data.colors.Count - 1);
            CupWhLayerUtility.OnRemovedTopLayers(to.Data);
            for (var i = 0; i < num; i++)
                from.Data.colors.Add(color);

            STimerManager.Instance.CreateSDelay(0.05f, () => 
            {
                from.RefreshVisual();
                to.RefreshVisual();
                pourAction = null;
            });
            //hud?.SetUndoGray(true);
            FacadeGamePlay.AbleProp2Btn(false);
        }

        /// <summary>
        /// 添加瓶子功能
        /// </summary>
        private void Func_Porp3()
        {
            var id = GetEmptySlotId();
            if (id == null) return;
            var slot = curLevelData[id.Value];
            ResetSlotAsEmptyCup(slot);
            var pos = new Vector3(slot.position.x, slot.position.y + GameConstants.HalfBottleHeight, 0);
            var bottle = Bottle.Create(FacadeGamePlay.GetCupPart());
            if (bottle == null) return;
            bottle.transform.localPosition = pos;
            bottle.Init(slot, OnCupClick);
            var shadow = Bottle.CreateShadow(FacadeGamePlay.GetCupPartShadow());
            if (shadow == null)
            {
                GameObject.Destroy(bottle.gameObject);
            }
            shadow.transform.localPosition = pos + new Vector3(
                GameConstants.BottleShadowDiffX,
                -GameConstants.BottleHeight + GameConstants.BottleShadowDiffY, 0);
            bottle.BindShadow(shadow);
            cups[slot.id] = bottle;
            shadows[slot.id] = shadow;
            //SpineService.PlayEffect(bottle.transform, Vector3.zero, "bao_xing", "bao");
            bottle.PlayProp3Effect();
            FacadeGamePlay.AbleProp3Btn(GetEmptySlotId() != null);
        }

        private int? GetEmptySlotId()
        {
            foreach (var d in curLevelData)
            {
                if (cups.TryGetValue(d.id, out var cup) && cup == null)
                    return d.id;
            }

            return null;
        }

        private void ResetSlotAsEmptyCup(CupData slot)
        {
            if (slot == null) return;
            slot.colors.Clear();
            slot.whNums = 0;
            slot.whMask = 0;
            slot.isVideo = 0;
            slot.isLock = 0;
            slot.lockColor = 0;
            slot.lockNums = 0;
            slot.isNull = 0;
        }

        #endregion

        private string GetLevelProgress()
        {
            return (_collected * 1f / _needCollect * 100).ToString("F2");
        }

        private void LoopPlayCongratulationEffect(bool b)
        {
            if (b)
            {
                FacadeEffect.PlayCongratulationEffect?.Invoke();
                if (LCTime == null)
                {
                    LCTime = STimerManager.Instance.CreateSTimer(GameDefines.LPCETime, -1, true, false, () =>
                    {
                        FacadeEffect.PlayCongratulationEffect?.Invoke();
                    });
                }
                else
                    LCTime.ReStart();
            }
            else
            {
                if (LCTime != null)
                    LCTime.Stop();
            }
        }

        private void ShowUITip()
        {
            if(curLevelIndex == GameDefines.NGPLevel1 && showUITip1)
            {
                showUITip1 = false;
                SPlayerPrefs.SetBool(PlayerPrefDefines.showUITip1, showUITip1);
                SPlayerPrefs.Save();

                UIManager.Instance.OpenAsync<UINewGamePlay>(EUIType.EUINewGamePlay);
            }
            else if(curLevelIndex == GameDefines.NGPLevel2 && showUITip2)
            {
                showUITip2 = false;
                SPlayerPrefs.SetBool(PlayerPrefDefines.showUITip2, showUITip2);
                SPlayerPrefs.Save();

                UIManager.Instance.OpenAsync<UINewGamePlay>(EUIType.EUINewGamePlay);
            }
            else if(curLevelIndex == GameDefines.NGPLevel3 && showUITip3)
            {
                showUITip3 = false;
                SPlayerPrefs.SetBool(PlayerPrefDefines.showUITip3, showUITip3);
                SPlayerPrefs.Save();

                UIManager.Instance.OpenAsync<UINewGamePlay>(EUIType.EUINewGamePlay);
            }
        }


        protected override void OnDispose()
        {
            FacadeRemove();
        }
    }
}
