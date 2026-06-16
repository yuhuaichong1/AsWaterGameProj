using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using AsGame.Ads;
using AsGame.Core;
using AsGame.Data;
using AsGame.Events;
using AsGame.Spine;
using AsGame.UI;
using XrCode;

namespace AsGame.Water
{
    public class GameController : MonoBehaviour
    {
        [SerializeField] RectTransform cupRoot;
        [SerializeField] RectTransform shadowRoot;
        [SerializeField] RectTransform pocketRoot;
        [SerializeField] RectTransform contentRoot;
        [SerializeField] GameHudController hud;

        readonly Dictionary<int, Bottle> _cups = new();
        readonly Dictionary<int, Bottle> _shadows = new();
        readonly List<int> _pocketColors = new();
        List<CupData> _levelData = new();
        PourActionRecord _pourAction;
        Bottle _selected;
        GameStatus _status = GameStatus.None;
        int _levelIndex;
        int _needCollect;
        int _collected;
        bool _shuffleMode;
        bool _shuffleAnimating;
        bool _isCheckingPack;
        int _pendingPackOps;
        bool _packCompleteCheckRunning;
        readonly Dictionary<Pocket, Queue<int>> _pocketAnimQueues = new();
        readonly HashSet<Pocket> _pocketAnimProcessing = new();
        readonly List<GameObject> _shuffleFxObjects = new();
        readonly Dictionary<int, GameObject> _shuffleFxByCupId = new();
        readonly Dictionary<int, Queue<int>> _pendingFullCupsByColor = new();
        Sprite _bottleSprite;
        Sprite _shadowSprite;

        void OnEnable()
        {
            EventBus.Subscribe(GameEvents.UseProp, OnUseProp);
            EventBus.Subscribe(GameEvents.Restart, OnRestartEvent);
            EventBus.Subscribe(GameEvents.NextLevel, OnNextLevelEvent);
            EventBus.Subscribe(GameEvents.ShuffleEnd, OnShuffleEnd);
        }

        void OnDisable()
        {
            EventBus.Unsubscribe(GameEvents.UseProp, OnUseProp);
            EventBus.Unsubscribe(GameEvents.Restart, OnRestartEvent);
            EventBus.Unsubscribe(GameEvents.NextLevel, OnNextLevelEvent);
            EventBus.Unsubscribe(GameEvents.ShuffleEnd, OnShuffleEnd);
        }

        void OnRestartEvent(object _) => StartCoroutine(RestartLevel());
        void OnNextLevelEvent(object _) => StartCoroutine(NextLevel());

        void Start()
        {
            _bottleSprite = GameResourceLoader.LoadSprite("Sprites/Bottle/img_8");
            _shadowSprite = GameResourceLoader.LoadSprite("Sprites/Bottle/img_9");
            if (_shadowSprite == null)
                _shadowSprite = GameResourceLoader.LoadSprite("Sprites/Bottle/sgsg");
            if (_bottleSprite == null)
                _bottleSprite = GameResourceLoader.LoadSprite("Sprites/Bottle/img_2");
            StartCoroutine(StartLevel());
        }

        IEnumerator StartLevel()
        {
            _levelIndex = GameSaveData.CurrentLevel;
            _levelData = LevelConfigLoader.LoadLevel(_levelIndex);
            BuildPocketColors();
            ClearBoard();
            _pourAction = null;
            _selected = null;
            _shuffleMode = false;
            ClearShuffleEffects();

            hud?.RefreshLevel();
            hud?.RefreshHeart();
            hud?.SetUndoGray(true);
            hud?.SetAddBottleGray(true);
            hud?.SetShuffleGray(false);
            hud?.HideShuffleTip();

            CheckNewPlayUnlock();

            if (contentRoot != null)
            {
                contentRoot.localScale = Vector3.one * 1.2f;
                yield return TweenHelper.ToFloat(1.2f, 1f, 0.3f, s => contentRoot.localScale = Vector3.one * s);
            }

            yield return GenerateCups();
            yield return GeneratePockets();
            foreach (var kv in _cups)
                if (kv.Value != null && kv.Value.IsCollect())
                    RegisterFullCup(kv.Value);
            RefreshAddBottleButton();
            yield return CheckPack();
            _status = GameStatus.Gaming;
        }

        void CheckNewPlayUnlock()
        {
            for (var i = 0; i < GameConstants.NewPlayUnlockLevels.Length; i++)
            {
                var lv = GameConstants.NewPlayUnlockLevels[i];
                if (_levelIndex == lv && !GameSaveData.IsNewPlayUnlocked(i + 1))
                {
                    GameSaveData.SetNewPlayUnlocked(i + 1);
                    PopupManager.Instance.ShowAtOnce(new PopupContext
                    {
                        Type = PopupType.NewPlay,
                        Payload = i + 1
                    });
                }
            }
        }

        void BuildPocketColors()
        {
            _needCollect = 0;
            _collected = 0;
            foreach (var cup in _levelData)
            {
                if (cup == null || cup.isNull != 0 || cup.isVideo != 0 || cup.colors == null)
                    continue;
                _needCollect += cup.colors.Count;
            }

            _needCollect /= 4;
            LevelPocketColorPlanner.BuildPocketColors(
                _levelData, _levelIndex, _pocketColors, useUnityRandom: true);
        }

        void ClearBoard()
        {
            _pendingFullCupsByColor.Clear();
            _isCheckingPack = false;
            _pendingPackOps = 0;
            _packCompleteCheckRunning = false;
            _pocketAnimQueues.Clear();
            _pocketAnimProcessing.Clear();
            foreach (var kv in _cups.ToList())
                if (kv.Value != null) Destroy(kv.Value.gameObject);
            _cups.Clear();
            _shadows.Clear();
            foreach (Transform c in cupRoot) Destroy(c.gameObject);
            foreach (Transform c in shadowRoot) Destroy(c.gameObject);
            foreach (Transform c in pocketRoot) Destroy(c.gameObject);
        }

        IEnumerator GenerateCups()
        {
            foreach (var data in _levelData)
            {
                if (data.isNull != 0)
                {
                    _cups[data.id] = null;
                    continue;
                }

                var pos = new Vector3(data.position.x, data.position.y + GameConstants.HalfBottleHeight, 0);
                var bottle = Bottle.Create(cupRoot);
                if (bottle == null)
                {
                    Debug.LogError($"[GameController] 创建瓶子失败 id={data.id} pos={data.position}");
                    continue;
                }

                bottle.transform.localPosition = pos;
                bottle.Init(data, OnCupClick);
                var shadow = Bottle.CreateShadow(shadowRoot);
                if (shadow == null)
                {
                    Debug.LogError($"[GameController] 创建阴影失败 id={data.id}");
                    Destroy(bottle.gameObject);
                    continue;
                }

                shadow.transform.localPosition = pos + new Vector3(
                    GameConstants.BottleShadowDiffX,
                    -GameConstants.BottleHeight + GameConstants.BottleShadowDiffY, 0);
                bottle.BindShadow(shadow);
                _cups[data.id] = bottle;
                _shadows[data.id] = shadow;

                if (data.isVideo != 0)
                {
                    bottle.transform.SetAsLastSibling();
                    if (!bottle.gameObject.activeInHierarchy)
                        Debug.LogWarning($"[GameController] 广告瓶未激活 id={data.id} pos={data.position}");
                }
            }

            yield return null;
        }

        IEnumerator GeneratePockets()
        {
            for (var i = 0; i < 4; i++)
            {
                var locked = i > 1;
                var color = locked ? 0 : (_pocketColors.Count > 0 ? _pocketColors[0] : 0);
                if (!locked && _pocketColors.Count > 0) _pocketColors.RemoveAt(0);
                var pocket = Pocket.Create(pocketRoot, locked, OnUnlockPocket);
                var rt = (RectTransform)pocket.transform;
                rt.anchoredPosition = new Vector2(new[] { -262.5f, -87.5f, 87.5f, 262.5f }[i], -100f);
                pocket.Init(locked, color, OnUnlockPocket);
            }

            yield return null;
        }

        void OnUnlockPocket(Pocket pocket)
        {
            AdsService.ShowRewarded(RewardAdPlacement.UnlockBag, ok =>
            {
                if (!ok) return;
                pocket.Init(false, _pocketColors.Count > 0 ? _pocketColors[0] : 0);
                if (_pocketColors.Count > 0) _pocketColors.RemoveAt(0);
                BeginCheckPack();
            });
        }

        void OnCupClick(Bottle cup)
        {
            if (cup.IsVideo())
            {
                AdsService.ShowRewarded(RewardAdPlacement.UnlockBottle, ok =>
                {
                    if (!ok) return;
                    cup.UnlockVideo();
                    SpineService.PlayEffect(cup.transform, Vector3.zero, "bao_xing", "bao");
                });
                return;
            }

            if (_status == GameStatus.UsingProp && _shuffleMode)
            {
                if (_shuffleAnimating) return;
                if (cup.IsUnShuffle())
                {
                    ToastService.Show("该瓶子无法打乱");
                    return;
                }

                StartCoroutine(ShuffleCupRoutine(cup));
                return;
            }

            if (_status != GameStatus.Gaming) return;
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
                StartCoroutine(PourRoutine(from, cup));
            }
            else
            {
                ToastService.Show(cup.IsFull() ? "瓶子已满啦!" : "最上面一层颜色相同才可以倒入");
                _selected.DoUnSelect();
                _selected = cup;
                cup.DoSelect();
            }
        }

        bool CheckPour(Bottle from, Bottle to)
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

        IEnumerator PourRoutine(Bottle from, Bottle to)
        {
            var color = from.GetTopColorId();
            var pourNum = Mathf.Min(from.GetLayerPourWater(), to.GetLayerAddWater());
            if (pourNum <= 0)
                yield break;

            _pourAction = new PourActionRecord
            {
                fromId = from.GetId(),
                toId = to.GetId(),
                colorId = color,
                num = pourNum
            };
            hud?.SetUndoGray(false);

            var dir = from.transform.localPosition.x > to.transform.localPosition.x ? 1 : -1;
            var pourPos = to.transform.localPosition + new Vector3(dir < 0 ? 20 : -20, 0, 0);

            var targetStartHeight = GameConstants.WaterMaxY[Mathf.Clamp(to.Data.colors.Count, 0, GameConstants.WaterMaxY.Length - 1)];
            var streamEndRootY = to.transform.localPosition.y - 238f + targetStartHeight;

            Coroutine waterInRoutine = null;
            yield return from.WaterOut(color, pourNum, dir, pourPos, streamEndRootY, () =>
            {
                waterInRoutine = StartCoroutine(to.WaterIn(color, pourNum));
            });
            if (waterInRoutine != null)
                yield return waterInRoutine;

            if (to.IsCollect())
            {
                RemoveShuffleEffectForCup(to.GetId());
                _pourAction = null;
                hud?.SetUndoGray(true);
                to.RefreshVisual();
                StartCoroutine(CollectAndPackRoutine(to));
            }
        }

        IEnumerator CollectAndPackRoutine(Bottle cup)
        {
            if (cup == null) yield break;
            yield return cup.DoCollected();
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

        void BeginCheckPack()
        {
            StartCoroutine(CheckPack());
        }

        /// <summary>对齐 Cocos checkPack / batchHandlePack：口袋颜色与待收集满瓶匹配时批量装袋。</summary>
        IEnumerator CheckPack()
        {
            if (_isCheckingPack) yield break;
            _isCheckingPack = true;
            try
            {
                while (true)
                {
                    var batch = BatchCheckPack();
                    if (batch.Count == 0) break;
                    for (var i = 0; i < batch.Count; i++)
                    {
                        var item = batch[i];
                        StartCoroutine(HandlePack(item.cup, item.pocket, i * 0.2f));
                    }
                }
            }
            finally
            {
                _isCheckingPack = false;
            }

            yield return WaitPackCompleteAndFinalize();
        }

        IEnumerator WaitPackCompleteAndFinalize()
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
            }
            finally
            {
                _packCompleteCheckRunning = false;
            }
        }

        struct PackPair
        {
            public Bottle cup;
            public Pocket pocket;
        }

        List<PackPair> BatchCheckPack()
        {
            var result = new List<PackPair>();
            foreach (Transform child in pocketRoot)
            {
                var pocket = child.GetComponent<Pocket>();
                if (pocket == null || pocket.IsLocked || pocket.PackColorId <= 0) continue;
                if (!_pendingFullCupsByColor.TryGetValue(pocket.PackColorId, out var queue) || queue.Count == 0)
                    continue;

                Bottle cup = null;
                while (queue.Count > 0)
                {
                    var cupId = queue.Dequeue();
                    if (!_cups.TryGetValue(cupId, out var c) || c == null || !c.IsCollect())
                        continue;
                    cup = c;
                    break;
                }

                if (cup != null)
                    result.Add(new PackPair { cup = cup, pocket = pocket });
            }

            return result;
        }

        IEnumerator HandlePack(Bottle cup, Pocket pocket, float delay)
        {
            if (cup == null || pocket == null) yield break;
            _pendingPackOps++;

            if (delay > 0f)
                yield return new WaitForSeconds(delay);

            var id = cup.GetId();
            var packedColor = cup.GetTopColorId();

            SpineService.ClearEffects(cup.transform);
            if (_shadows.TryGetValue(id, out var shadow) && shadow != null)
            {
                Destroy(shadow.gameObject);
                _shadows.Remove(id);
            }

            cup.transform.SetAsLastSibling();
            var target = GetPackMoveTargetLocal(pocket);
            yield return TweenHelper.MoveLocal(cup.transform, target, 0.2f);

            Destroy(cup.gameObject);
            _cups[id] = null;
            foreach (var slot in _levelData)
            {
                if (slot.id == id)
                {
                    ResetSlotAsPacked(slot);
                    break;
                }
            }

            RefreshAddBottleButton();
            _collected++;
            CheckUnlockCup(packedColor);
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
            StartCoroutine(ProcessPocketPackQueue(pocket));
        }

        IEnumerator ProcessPocketPackQueue(Pocket pocket)
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

        /// <summary>将口袋位置换算到 cupRoot 本地坐标（对齐 Cocos getTargetLocalPosAtNode(pocket, cupMgr)）。</summary>
        Vector3 GetPackMoveTargetLocal(Pocket pocket)
        {
            if (cupRoot == null || pocket == null)
                return Vector3.zero;
            var world = pocket.transform.position;
            var local = cupRoot.InverseTransformPoint(world);
            return local + Vector3.up * (GameConstants.HalfBottleHeight + 100f);
        }

        void CheckUnlockCup(int packedColor)
        {
            foreach (var kv in _cups)
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
            BeginCheckPack();
        }

        void OnUseProp(object payload)
        {
            if (_status != GameStatus.Gaming) return;
            if (payload is not GameProp prop) return;

            switch (prop)
            {
                case GameProp.Shuffle:
                    if (!BeginShuffleMode())
                        ToastService.Show("没有可打乱的水瓶");
                    break;
                case GameProp.Undo:
                    if (!TryUndo())
                    {
                        ToastService.Show("暂无可撤回的操作");
                        break;
                    }

                    StartCoroutine(UndoRoutine());
                    break;
                case GameProp.AddBottle:
                    if (HandleAddBottle())
                        ConsumeProp(prop);
                    else
                        ToastService.Show("没有空位添加瓶子");
                    break;
            }
        }

        void OnShuffleEnd(object payload)
        {
            var success = payload is bool b && b;
            FinishShuffleMode(success);
        }

        bool BeginShuffleMode()
        {
            ClearShuffleEffects();
            var any = false;
            foreach (var kv in _cups)
            {
                var cup = kv.Value;
                if (cup == null || cup.IsUnShuffle()) continue;
                any = true;
                cup.StartShuffleShake();
                AddShuffleEffect(cup);
            }

            if (!any) return false;
            _status = GameStatus.UsingProp;
            _shuffleMode = true;
            _shuffleAnimating = false;
            hud?.ShowShuffleTip();
            return true;
        }

        void AddShuffleEffect(Bottle cup)
        {
            if (cupRoot == null || cup == null) return;
            var anchor = new GameObject("ShuffleFxAnchor", typeof(RectTransform));
            var rt = anchor.GetComponent<RectTransform>();
            rt.SetParent(cupRoot, false);
            rt.localPosition = cup.transform.localPosition + new Vector3(0f, -GameConstants.HalfBottleHeight - 110f, 0f);
            rt.localScale = Vector3.one * 0.8f;
            // 插在对应瓶子之前绘制，光环在瓶身/水体下层（对齐 Cocos：杯子层盖住 effectFront 光环）
            rt.SetSiblingIndex(cup.transform.GetSiblingIndex());
            // 对齐 Cocos Spine_Shuffle：xuan_zhong / idle（spine-unity SkeletonGraphic）
            //SpineService.PlayEffect(rt, Vector3.zero, "xuan_zhong", "idle", loop: true);
            _shuffleFxObjects.Add(anchor);
            _shuffleFxByCupId[cup.GetId()] = anchor;
        }

        void RemoveShuffleEffectForCup(int cupId)
        {
            if (!_shuffleFxByCupId.TryGetValue(cupId, out var anchor)) return;
            _shuffleFxByCupId.Remove(cupId);
            _shuffleFxObjects.Remove(anchor);
            if (anchor != null) Destroy(anchor);
        }

        void ClearShuffleEffects()
        {
            foreach (var go in _shuffleFxObjects)
            {
                if (go != null) Destroy(go);
            }

            _shuffleFxObjects.Clear();
            _shuffleFxByCupId.Clear();
        }

        void FinishShuffleMode(bool consumeProp)
        {
            foreach (var kv in _cups)
                kv.Value?.DoUnShuffle();
            ClearShuffleEffects();
            _shuffleMode = false;
            _shuffleAnimating = false;
            _status = GameStatus.Gaming;
            hud?.HideShuffleTip();
            if (consumeProp)
            {
                ConsumeProp(GameProp.Shuffle);
                ToastService.Show("打乱成功!!!");
            }
        }

        IEnumerator ShuffleCupRoutine(Bottle cup)
        {
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
            EventBus.Publish(GameEvents.ShuffleEnd, true);
        }

        static void ShuffleColors(List<int> colors)
        {
            for (var i = colors.Count - 1; i > 0; i--)
            {
                var j = Random.Range(0, i + 1);
                (colors[i], colors[j]) = (colors[j], colors[i]);
            }
        }

        bool TryUndo()
        {
            if (_pourAction == null) return false;
            if (!_cups.TryGetValue(_pourAction.fromId, out var from) || from == null) return false;
            if (!_cups.TryGetValue(_pourAction.toId, out var to) || to == null) return false;
            return true;
        }

        IEnumerator UndoRoutine()
        {
            if (_pourAction == null) yield break;
            if (!_cups.TryGetValue(_pourAction.fromId, out var from) || from == null) yield break;
            if (!_cups.TryGetValue(_pourAction.toId, out var to) || to == null) yield break;

            var num = _pourAction.num;
            var color = _pourAction.colorId;
            for (var i = 0; i < num; i++)
                if (to.Data.colors.Count > 0)
                    to.Data.colors.RemoveAt(to.Data.colors.Count - 1);
            for (var i = 0; i < num; i++)
                from.Data.colors.Add(color);

            yield return TweenHelper.Delay(0.05f, null);
            from.RefreshVisual();
            to.RefreshVisual();
            _pourAction = null;
            hud?.SetUndoGray(true);
            ConsumeProp(GameProp.Undo);
        }

        bool HandleAddBottle()
        {
            var id = GetEmptySlotId();
            if (id == null) return false;
            var slot = _levelData[id.Value];
            ResetSlotAsEmptyCup(slot);
            var pos = new Vector3(slot.position.x, slot.position.y + GameConstants.HalfBottleHeight, 0);
            var bottle = Bottle.Create(cupRoot);
            if (bottle == null) return false;
            bottle.transform.localPosition = pos;
            bottle.Init(slot, OnCupClick);
            var shadow = Bottle.CreateShadow(shadowRoot);
            if (shadow == null)
            {
                Destroy(bottle.gameObject);
                return false;
            }
            shadow.transform.localPosition = pos + new Vector3(
                GameConstants.BottleShadowDiffX,
                -GameConstants.BottleHeight + GameConstants.BottleShadowDiffY, 0);
            bottle.BindShadow(shadow);
            _cups[slot.id] = bottle;
            _shadows[slot.id] = shadow;
            SpineService.PlayEffect(bottle.transform, Vector3.zero, "bao_xing", "bao");
            RefreshAddBottleButton();
            return true;
        }

        void RefreshAddBottleButton() => hud?.SetAddBottleGray(GetEmptySlotId() == null);

        /// <summary>对齐 Cocos CupComp.resetData：打包后槽位标记为空且清空水层等。</summary>
        static void ResetSlotAsPacked(CupData slot)
        {
            if (slot == null) return;
            slot.colors.Clear();
            slot.whNums = 0;
            slot.isVideo = 0;
            slot.isLock = 0;
            slot.lockColor = 0;
            slot.lockNums = 0;
            slot.isNull = 1;
        }

        /// <summary>加瓶时写入空瓶数据（仅保留 id / position）。</summary>
        static void ResetSlotAsEmptyCup(CupData slot)
        {
            if (slot == null) return;
            slot.colors.Clear();
            slot.whNums = 0;
            slot.isVideo = 0;
            slot.isLock = 0;
            slot.lockColor = 0;
            slot.lockNums = 0;
            slot.isNull = 0;
        }

        int? GetEmptySlotId()
        {
            foreach (var d in _levelData)
            {
                if (_cups.TryGetValue(d.id, out var cup) && cup == null)
                    return d.id;
            }

            return null;
        }

        static void ConsumeProp(GameProp prop)
        {
            var n = GameSaveData.GetPropCount(prop);
            if (n > 0)
                GameSaveData.SetPropCount(prop, n - 1);
            EventBus.Publish(GameEvents.UpdateProp);
        }

        IEnumerator WinRoutine()
        {
            _status = GameStatus.Win;
            foreach (var kv in _cups)
                if (kv.Value != null)
                    yield return kv.Value.DisappearEmpty();

            yield return new WaitForSeconds(0.4f);
            ShowWinPopup();
        }

        void ShowWinPopup()
        {
            GameSaveData.CurrentLevel++;
            if (ShouldShowCollect())
            {
                var entry = CollectCatalog.Drinks[Mathf.Clamp(GameSaveData.CollectLevel, 0, CollectCatalog.Drinks.Count - 1)];
                GameSaveData.CollectLevel++;
                PopupManager.Instance.ShowAtOnce(new PopupContext
                {
                    Type = PopupType.GetCollect,
                    Payload = entry
                });
            }
            else
            {
                PopupManager.Instance.ShowAtOnce(new PopupContext { Type = PopupType.Success });
            }
        }

        static bool ShouldShowCollect() =>
            GameSaveData.CurrentLevel % 2 == 0 && GameSaveData.CollectLevel < CollectCatalog.Drinks.Count;

        IEnumerator NextLevel() => StartLevel();

        IEnumerator RestartLevel() => StartLevel();

        void OnDestroy() => PopupManager.Instance?.ClearAll();
    }
}
