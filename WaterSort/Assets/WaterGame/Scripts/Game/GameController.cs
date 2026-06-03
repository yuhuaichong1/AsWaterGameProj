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
        bool _isCheckingPack;
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
            _pocketColors.Clear();
            _needCollect = 0;
            _collected = 0;
            var colorCount = new Dictionary<int, int>();
            foreach (var cup in _levelData)
            {
                if (cup.isNull != 0) continue;
                foreach (var c in cup.colors)
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
            if (_levelIndex != 1)
                Shuffle(_pocketColors);
        }

        static void Shuffle<T>(IList<T> list)
        {
            for (var i = list.Count - 1; i > 0; i--)
            {
                var j = Random.Range(0, i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }

        void ClearBoard()
        {
            _pendingFullCupsByColor.Clear();
            _isCheckingPack = false;
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
                var shadow = Bottle.CreateShadow(shadowRoot, _shadowSprite);
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
                StartCoroutine(ShuffleCup(cup));
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
            _status = GameStatus.Moving;
            try
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
                    _pourAction = null;
                    hud?.SetUndoGray(true);
                    yield return to.DoCollected();
                    RegisterFullCup(to);
                    yield return CheckPack();
                }
            }
            finally
            {
                if (_status == GameStatus.Moving)
                    _status = GameStatus.Gaming;
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

        /// <summary>对齐 Cocos checkPack / batchHandlePack：口袋颜色与待收集满瓶匹配时批量装袋。</summary>
        IEnumerator CheckPack()
        {
            if (_isCheckingPack) yield break;
            _isCheckingPack = true;
            var restoreGaming = _status == GameStatus.Gaming;
            if (restoreGaming)
                _status = GameStatus.Moving;
            try
            {
                while (true)
                {
                    var batch = BatchCheckPack();
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
                if (restoreGaming && _status != GameStatus.Win)
                    _status = GameStatus.Gaming;
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
            if (delay > 0f)
                yield return new WaitForSeconds(delay);

            var target = pocket.transform.localPosition + Vector3.up * 100f;
            yield return TweenHelper.MoveLocal(cup.transform, target, 0.2f);
            yield return pocket.OnPocketAction();
            SpineService.PlayEffect(pocket.transform, Vector3.zero, "he_cheng_1", "zhuang");

            var id = cup.GetId();
            var packedColor = cup.GetTopColorId();
            if (_shadows.TryGetValue(id, out var shadow) && shadow != null)
                Destroy(shadow.gameObject);
            _shadows.Remove(id);
            Destroy(cup.gameObject);
            _cups[id] = null;

            _collected++;
            CheckUnlockCup(packedColor);
            RefillPocket(pocket);
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
        }

        void OnUseProp(object payload)
        {
            if (_status != GameStatus.Gaming && _status != GameStatus.UsingProp) return;
            if (payload is not GameProp prop) return;

            switch (prop)
            {
                case GameProp.Shuffle:
                    _status = GameStatus.UsingProp;
                    _shuffleMode = true;
                    hud?.ShowShuffleTip();
                    ToastService.Show("点击要打乱的瓶子");
                    break;
                case GameProp.Undo:
                    if (_pourAction != null)
                        StartCoroutine(UndoRoutine());
                    break;
                case GameProp.AddBottle:
                    if (HandleAddBottle())
                        ConsumeProp(prop);
                    break;
            }
        }

        void OnShuffleEnd(object _) => EndShuffleMode();

        void EndShuffleMode()
        {
            _shuffleMode = false;
            _status = GameStatus.Gaming;
            hud?.HideShuffleTip();
        }

        IEnumerator ShuffleCup(Bottle cup)
        {
            if (cup.IsEmpty() || cup.IsLock()) yield break;
            for (var i = 0; i < 8; i++)
            {
                var colors = cup.Data.colors;
                if (colors.Count >= 2)
                    (colors[0], colors[^1]) = (colors[^1], colors[0]);
                cup.RefreshVisual();
                yield return new WaitForSeconds(0.03f);
            }

            ConsumeProp(GameProp.Shuffle);
            EndShuffleMode();
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
            if (slot.isNull == 0) return false;

            slot.isNull = 0;
            var pos = new Vector3(slot.position.x, slot.position.y + GameConstants.HalfBottleHeight, 0);
            var bottle = Bottle.Create(cupRoot);
            if (bottle == null) return false;
            bottle.transform.localPosition = pos;
            bottle.Init(slot, OnCupClick);
            var shadow = Bottle.CreateShadow(shadowRoot, _shadowSprite);
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
            hud?.SetAddBottleGray(true);
            return true;
        }

        int? GetEmptySlotId()
        {
            foreach (var d in _levelData)
                if (d.isNull != 0) return d.id;
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
