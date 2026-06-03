using AsGame.Ads;
using AsGame.Core;
using AsGame.Data;
using AsGame.Events;
using AsGame.Spine;
using AsGame.Water;
using Newtonsoft.Json.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace XrCode
{
    public partial class GamePlayModule : BaseModule
    {
        private int curLevelIndex;
        private List<CupData> curLevelData;
        private Dictionary<string, List<CupData>> allLevelData;
        private Dictionary<int, Bottle> cups;
        private Dictionary<int, Bottle> shadows;
        private GameStatus status;
        private bool _shuffleMode;//是否处于刷新状态
        private Bottle _selected;//被选中的瓶子
        private PourActionRecord pourAction;//上一次的操作结果
        Dictionary<int, Queue<int>> _pendingFullCupsByColor;
        private bool _isCheckingPack;
        private int _collected;
        private int _needCollect;
        private List<int> _pocketColors;

        protected override void OnLoad()
        {
            curLevelData = new List<CupData>();
            allLevelData = new Dictionary<string, List<CupData>>();
            cups = new Dictionary<int, Bottle>();
            shadows = new Dictionary<int, Bottle>();
            _pocketColors = new List<int>();

            FacadeAdd();

            LoadData();
            InitGame();
        }

        #region Facade

        /// <summary>
        /// 添加Facade接口
        /// </summary>
        private void FacadeAdd()
        {
            FacadeGamePlay.CreateLevel += CreateLevel;
            FacadeGamePlay.Func_Porp1 += Func_Porp1;
            FacadeGamePlay.Func_Porp2 += Func_Porp2;
            FacadeGamePlay.Func_Porp3 += Func_Porp3;
            FacadeGamePlay.RePlay += RePlay;
            FacadeGamePlay.GetCurLevelProgress += GetCurLevelProgress;
            FacadeGamePlay.GetStatus += GetStatus;
        }

        /// <summary>
        /// 去除Facade接口
        /// </summary>
        private void FacadeRemove()
        {
            FacadeGamePlay.CreateLevel += CreateLevel;
            FacadeGamePlay.Func_Porp1 += Func_Porp1;
            FacadeGamePlay.Func_Porp2 += Func_Porp2;
            FacadeGamePlay.Func_Porp3 += Func_Porp3;
            FacadeGamePlay.RePlay += RePlay;
            FacadeGamePlay.GetCurLevelProgress -= GetCurLevelProgress;
            FacadeGamePlay.GetStatus += GetStatus;
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
        private void CreateLevel()
        {
            curLevelIndex = FacadePlayer.GetLevel();
            GetLevelData(curLevelIndex);
            GenerateCups();
            GeneratePockets();
            foreach (var kv in cups)
                if (kv.Value != null && kv.Value.IsCollect())
                    RegisterFullCup(kv.Value);
            CheckPack();

            status = GameStatus.Gaming;
        }

        /// <summary>
        /// 获取当前关卡数据
        /// </summary>
        /// <param name="levelIndex">关卡id</param>
        private void GetLevelData(int levelIndex)
        {
            
            if (allLevelData.Count == 0)
            {
                GetAllLevelData();
            }

            if (allLevelData.TryGetValue($"level_{levelIndex}", out List<CupData> levelData))
            {
                curLevelData = levelData;
            }
            else
            {
                D.Error($"level_{levelIndex} is not exist");
            }
        }

        /// <summary>
        /// 获取所有关卡数据
        /// </summary>
        private void GetAllLevelData()
        {
            TextAsset ta = ResourceMod.Instance.SyncLoad<TextAsset>(GameDefines.LevelsDataPath);
            string jsonContent = ta.text;
            JObject root = JObject.Parse(jsonContent);

            foreach (var property in root.Properties())
            {
                string levelName = property.Name;  // "level_1", "level_2", ...
                JArray levelArray = (JArray)property.Value;

                var cupList = new List<CupData>();
                int id = 0;

                foreach (var item in levelArray)
                {
                    var cupData = new CupData();
                    cupData.id = id++;

                    // 解析位置 { "x": -120, "y": -194 }
                    JObject positionObj = (JObject)item[0];
                    float x = positionObj.Value<float>("x");
                    float y = positionObj.Value<float>("y");
                    cupData.position = new Vector2(x, y);

                    // 解析颜色数组
                    JArray colorsArray = (JArray)item[1];
                    cupData.colors = new List<int>();
                    foreach (var color in colorsArray)
                    {
                        cupData.colors.Add(color.Value<int>());
                    }

                    // 解析后续字段
                    cupData.whNums = item[2].Value<int>();
                    cupData.isVideo = item[3].Value<int>();
                    cupData.isLock = item[4].Value<int>();
                    cupData.lockColor = item[5].Value<int>();
                    cupData.lockNums = item[6].Value<int>();
                    cupData.isNull = 0;

                    cupList.Add(cupData);
                }

                allLevelData.Add(levelName, cupList);
            }
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
                bottle.transform.localPosition = pos;
                bottle.Init(data, OnCupClick);
            }
        }

        /// <summary>
        /// 生成饮料
        /// </summary>
        private void GeneratePockets()
        {

        }

        void OnCupClick(Bottle cup)
        {
            if (cup.IsVideo())
            {
                AdsService.ShowRewarded(RewardAdPlacement.UnlockBottle, ok =>
                {
                    if (!ok) return;
                    cup.UnlockVideo();
                    FacadeEffect.PlayDrinkFinish();
                });
                return;
            }

            if (status == GameStatus.UsingProp && _shuffleMode)
            {
                Func_Prop1_Func(cup);
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
                //StartCoroutine(PourRoutine(from, cup));
            }
            else
            {
                ToastService.Show(cup.IsFull() ? "瓶子已满啦!" : "最上面一层颜色相同才可以倒入");
                _selected.DoUnSelect();
                _selected = cup;
                cup.DoSelect();
            }
        }

        /// <summary>
        /// 检查两个水瓶顶部颜色对不对
        /// </summary>
        /// <param name="from">要倒水的水瓶</param>
        /// <param name="to">被倒水的水瓶</param>
        /// <returns>能不能倒</returns>
        private bool CheckPour(Bottle from, Bottle to)
        {
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
            status = GameStatus.Moving;
            try
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
                //hud?.SetUndoGray(false);

                var dir = from.transform.localPosition.x > to.transform.localPosition.x ? 1 : -1;
                var pourPos = to.transform.localPosition + new Vector3(dir < 0 ? 20 : -20, 0, 0);

                var targetStartHeight = GameConstants.WaterMaxY[Mathf.Clamp(to.Data.colors.Count, 0, GameConstants.WaterMaxY.Length - 1)];
                var streamEndRootY = to.transform.localPosition.y - 238f + targetStartHeight;

                Coroutine waterInRoutine = null;
                yield return from.WaterOut(color, pourNum, dir, pourPos, streamEndRootY, () =>
                {
                    waterInRoutine = Game.Instance.StartCoroutine(to.WaterIn(color, pourNum));
                });
                if (waterInRoutine != null)
                    yield return waterInRoutine;

                if (to.IsCollect())
                {
                    pourAction = null;
                    //hud?.SetUndoGray(true);
                    yield return to.DoCollected();
                    RegisterFullCup(to);
                    yield return CheckPack();
                }
            }
            finally
            {
                if (status == GameStatus.Moving)
                    status = GameStatus.Gaming;
            }
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

        private IEnumerator CheckPack()
        {
            if (_isCheckingPack) yield break;
            _isCheckingPack = true;
            bool restoreGaming = status == GameStatus.Gaming;
            if (restoreGaming)
                status = GameStatus.Moving;
            try
            {
                while (true)
                {
                    List<PackPair2> batch = BatchCheckPack();
                    if (batch.Count == 0) break;
                    for (var i = 0; i < batch.Count; i++)
                    {
                        var item = batch[i];
                        yield return HandlePack(item.cup, item.pocket, i * 0.2f);
                    }
                }

                if (_collected >= _needCollect)
                    yield return WinRoutine();
            }
            finally
            {
                _isCheckingPack = false;
                if (restoreGaming && status != GameStatus.Win)
                    status = GameStatus.Gaming;
            }
        }

        private List<PackPair2> BatchCheckPack()
        {
            var result = new List<PackPair2>();
            foreach (Transform child in FacadeGamePlay.GetPockets())
            {
                var pocket = child.GetComponent<Pocket>();
                if (pocket == null || pocket.IsLocked || pocket.PackColorId <= 0) continue;
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
                    result.Add(new PackPair2 { cup = cup, pocket = pocket });
            }

            return result;
        }

        private IEnumerator HandlePack(Bottle cup, Pocket pocket, float delay)
        {
            if (cup == null || pocket == null) yield break;
            if (delay > 0f)
                yield return new WaitForSeconds(delay);

            var target = pocket.transform.localPosition + Vector3.up * 100f;
            yield return TweenHelper.MoveLocal(cup.transform, target, 0.2f);
            yield return pocket.OnPocketAction();
            SpineService.PlayEffect(pocket.transform, Vector3.zero, "he_cheng_1", "zhuang");

            var id = cup.GetId();
            var packedColor = cup.GetTopColorId();
            if (shadows.TryGetValue(id, out var shadow) && shadow != null)
                GameObject.Destroy(shadow.gameObject);
            shadows.Remove(id);
            GameObject.Destroy(cup.gameObject);
            cups[id] = null;

            _collected++;
            CheckUnlockCup(packedColor);
            RefillPocket(pocket);
        }

        private IEnumerator WinRoutine()
        {
            status = GameStatus.Win;
            foreach (var kv in cups)
                if (kv.Value != null)
                    yield return kv.Value.DisappearEmpty();

            yield return new WaitForSeconds(0.4f);
            UIManager.Instance.OpenSync<UILevelCompleted>(EUIType.EUILevelCompleted);
        }

        private void CheckUnlockCup(int packedColor)
        {
            foreach (var kv in cups)
            {
                var cup = kv.Value;
                if (cup == null || !cup.IsLock()) continue;
                var lockColor = cup.GetLockColor();
                if (lockColor != 0 && lockColor != packedColor) continue;
                cup.SetLockNum(cup.GetLockNum() - 1);
            }
        }


        void RefillPocket(Pocket pocket)
        {
            var next = _pocketColors.Count > 0 ? _pocketColors[0] : 0;
            if (_pocketColors.Count > 0) _pocketColors.RemoveAt(0);
            pocket.SetColor(next);
        }

        #endregion

        /// <summary>
        /// 重新开始
        /// </summary>
        private void RePlay()
        {

        }

        /// <summary>
        /// 获取当前关卡进度
        /// </summary>
        private float GetCurLevelProgress()
        {
            return 0;
        }

        #region 下三功能

        /// <summary>
        /// 刷新功能（开启刷新检测）
        /// </summary>
        private void Func_Porp1()
        {
            status = GameStatus.UsingProp;
            _shuffleMode = true;
        }

        /// <summary>
        /// 刷新功能具体执行逻辑
        /// </summary>
        /// <param name="cup"></param>
        private void Func_Prop1_Func(Bottle cup)
        {
            if (cup.IsEmpty() || cup.IsLock()) return;

            STimerManager.Instance.CreateSTimer(GameDefines.RefreshATime, GameDefines.RefreshLCount, true, true, () => 
            {
                var colors = cup.Data.colors;
                if (colors.Count >= 2)
                    (colors[0], colors[^1]) = (colors[^1], colors[0]);
                cup.RefreshVisual();
            });

            EndShuffleMode();
        }

        /// <summary>
        /// 结束刷新功能
        /// </summary>
        void EndShuffleMode()
        {
            _shuffleMode = false;
            status = GameStatus.Gaming;
            //hud?.HideShuffleTip();
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
            for (var i = 0; i < num; i++)
                from.Data.colors.Add(color);

            STimerManager.Instance.CreateSDelay(0.05f, () => 
            {
                from.RefreshVisual();
                to.RefreshVisual();
                pourAction = null;
            });
            //hud?.SetUndoGray(true);
        }

        /// <summary>
        /// 添加瓶子功能
        /// </summary>
        private void Func_Porp3()
        {
            EventBus.Publish(GameEvents.UpdateProp);
        }

        #endregion

        protected override void OnDispose()
        {
            FacadeRemove();
        }
    }
}
