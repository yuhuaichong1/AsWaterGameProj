using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using AsGame.Core;
using AsGame.Spine;
using Spine.Unity;
using XrCode;
using Spine;

namespace AsGame.Water
{
    /// <summary>Cocos CupComp 核心逻辑移植，倒水/选中均使用补间动画。</summary>
    public class Bottle : MonoBehaviour
    {
        [SerializeField] BottleWaterVisual waterVisual;
        [SerializeField] RectTransform content;
        [SerializeField] Image bottleBg;
        [SerializeField] Image lightBg;
        [SerializeField] GameObject lockNode;
        [SerializeField] Text lockNumLabel;
        [SerializeField] Image lockColorImage;
        [SerializeField] GameObject adNode;
        [SerializeField] GameObject streamNode;
        [SerializeField] Image streamBody;
        [SerializeField] CanvasGroup shadowGroup;
        [SerializeField] Transform selectFxAnchor;
        [SerializeField] SkeletonGraphic finishEffect;
        [SerializeField] SkeletonGraphic selectEffect;
        [SerializeField] SkeletonGraphic unlockEffect;

        CupData _data;
        Action<Bottle> _onClick;
        Vector3 _baseLocalPos;
        bool _pouring;
        Bottle _shadow;
        int _prevWhNums;
        int _prevWhMask;
        float _streamEndRootY;
        static Dictionary<string, Sprite> _splashSprites;
        static SkeletonDataAsset _splashSkeletonData;
        static Material _splashGraphicMaterial;
        static Material _splashScreenGraphicMaterial;
        static Sprite _cachedAdIconSprite;
        static Sprite _cachedCollectCheckSprite;
        Image _collectMark;

        const float StreamMouthX = -21f;
        const float StreamMouthY = -22f;
        /// <summary>与 sh_SkeletonData.asset 一致；Spine 像素经 scale×referencePixelsPerUnit 映射到 UI 像素。</summary>
        const float SplashSkeletonScale = 0.01f;
        /// <summary>对齐 Cocos Cup.prefab 中 zd 相对 effect 节点的 Y 偏移。</summary>
        const float SelectWaterFxSurfaceOffsetY = 19f;
        const float SelectWaterFxDuration = 1.3f;

        public bool Pouring => _pouring;
        public CupData Data => _data;

        public void BindShadow(Bottle shadow)
        {
            _shadow = shadow;
            shadow?.SetBaseLocalPosition(shadow.transform.localPosition);
        }

        public void SetBaseLocalPosition(Vector3 pos) => _baseLocalPos = pos;

        public void Init(CupData data, Action<Bottle> onClick)
        {
            EnsureHierarchyRefs();
            EnsureWaterVisual();
            _data = data.Clone();
            _onClick = onClick;
            _pouring = false;
            _baseLocalPos = transform.localPosition;
            _prevWhNums = _data.whNums;
            _prevWhMask = CupWhLayerUtility.GetMask(_data);
            if (lightBg != null) lightBg.gameObject.SetActive(false);
            if (streamNode != null) streamNode.SetActive(false);
            EnsureRootVisible();
            RefreshVisual();
        }

        /// <summary>保证瓶身节点可见（避免只剩阴影）。</summary>
        void EnsureRootVisible()
        {
            gameObject.SetActive(true);
            var cg = GetComponent<CanvasGroup>();
            if (cg != null)
            {
                cg.alpha = 1f;
                cg.interactable = true;
                cg.blocksRaycasts = true;
            }

            if (content != null)
                content.gameObject.SetActive(true);
        }

        /// <summary>从 content 子节点重新绑定序列化引用（防止 Prefab 实例化后引用丢失）。</summary>
        void EnsureHierarchyRefs()
        {
            if (content == null)
                content = transform.Find("content") as RectTransform;
            if (content == null) return;

            if (bottleBg == null)
                bottleBg = content.Find("bg")?.GetComponent<Image>();
            if (lightBg == null)
                lightBg = content.Find("light_bg")?.GetComponent<Image>();
            if (adNode == null)
            {
                var adTr = content.Find("ad");
                if (adTr != null) adNode = adTr.gameObject;
            }
            if (lockNode == null)
            {
                var lockTr = content.Find("clock");
                if (lockTr != null) lockNode = lockTr.gameObject;
            }
            if (lockNumLabel == null && lockNode != null)
                lockNumLabel = lockNode.transform.Find("num")?.GetComponent<Text>();
            if (lockColorImage == null && lockNode != null)
                lockColorImage = lockNode.transform.Find("color")?.GetComponent<Image>();
            if (streamNode == null)
            {
                var streamTr = transform.Find("shuiZhu");
                if (streamTr != null) streamNode = streamTr.gameObject;
            }
            if (streamBody == null && streamNode != null)
                streamBody = streamNode.transform.Find("body")?.GetComponent<Image>();
            if (selectFxAnchor == null)
            {
                var fxTr = content != null ? content.Find("selectFx") : null;
                if (fxTr == null)
                    fxTr = transform.Find("content/selectFx") ?? transform.Find("selectFx");
                if (fxTr != null) selectFxAnchor = fxTr;
            }
            EnsureSelectEffect();
            if (waterVisual == null)
                waterVisual = content.GetComponentInChildren<BottleWaterVisual>(true);
        }

        /// <summary>运行时补齐选中水面特效引用（AB 旧预制体或 CreateLegacy 可能未序列化）。</summary>
        void EnsureSelectEffect()
        {
            if (selectEffect != null)
                return;

            if (selectFxAnchor != null)
                selectEffect = selectFxAnchor.GetComponentInChildren<SkeletonGraphic>(true);

            if (selectEffect == null)
            {
                var selectTr = transform.Find("content/selectFx/SelectEffect")
                               ?? transform.Find("selectFx/SelectEffect");
                if (selectTr != null)
                    selectEffect = selectTr.GetComponent<SkeletonGraphic>();
            }
        }

        /// <summary>对齐 Cocos Cup.ad/icon：广告图标挂在 ad/icon 上。</summary>
        void SetupVideoAdVisual()
        {
            if (!IsVideo() || adNode == null) return;

            adNode.SetActive(true);
            adNode.transform.SetAsLastSibling();
        }

        public int GetId() => _data?.id ?? -1;
        public bool IsEmpty() => _data != null && _data.colors.Count == 0;
        public bool IsFull() => _data != null && _data.colors.Count >= GameConstants.WaterMaxCount;
        public bool IsLock() => _data != null && _data.isLock != 0;
        public bool IsVideo() => _data != null && _data.isVideo != 0;
        public int GetLockNum() => _data?.lockNums ?? 0;
        public int GetLockColor() => _data?.lockColor ?? 0;

        public void UnlockVideo()
        {
            if (_data == null) return;
            _data.isVideo = 0;
            if (adNode != null) adNode.SetActive(false);
            PlayProp3Effect();
            RefreshVisual();
        }

        public int GetTopColorId() => _data.colors.Count > 0 ? _data.colors[^1] : 0;

        public bool IsCollect()
        {
            if (_data == null || _data.colors.Count != GameConstants.WaterMaxCount) return false;
            var top = GetTopColorId();
            foreach (var c in _data.colors)
                if (c != top) return false;
            return !CupWhLayerUtility.HasHiddenLayers(_data);
        }

        /// <summary>对齐 Cocos isUnShuffle：不参与打乱的瓶子。</summary>
        public bool IsUnShuffle() =>
            IsEmpty() || IsVideo() || IsSameColor() || IsLock() || IsOneWater() || IsCollect();

        public bool IsSameColor()
        {
            if (_data == null || _data.colors.Count == 0) return false;
            var top = GetTopColorId();
            foreach (var c in _data.colors)
                if (c != top) return false;
            return !CupWhLayerUtility.HasHiddenLayers(_data);
        }

        public bool IsOneWater() => _data != null && _data.colors.Count == 1;

        Coroutine _shuffleShakeCo;

        public void StartShuffleShake()
        {
            StopShuffleShake();
            _shuffleShakeCo = StartCoroutine(ShuffleShakeLoop());
        }

        public void StopShuffleShake()
        {
            if (_shuffleShakeCo != null)
            {
                StopCoroutine(_shuffleShakeCo);
                _shuffleShakeCo = null;
            }
        }

        public void DoUnShuffle()
        {
            StopShuffleShake();
            transform.localPosition = _baseLocalPos;
            if (_shadow != null)
                StartCoroutine(_shadow.ResetShadow(0.2f));
        }

        IEnumerator ShuffleShakeLoop()
        {
            while (true)
            {
                yield return TweenHelper.MoveLocal(transform, _baseLocalPos + Vector3.up * 10f, 0.2f);
                yield return TweenHelper.MoveLocal(transform, _baseLocalPos - Vector3.up * 10f, 0.2f);
                yield return new WaitForSeconds(0.2f);
            }
        }

        public int GetLayerPourWater()
        {
            var top = GetTopColorId();
            var count = 0;
            for (var i = _data.colors.Count - 1; i >= 0; i--)
            {
                if (_data.colors[i] != top) break;
                if (IsWhtLayer(i)) break;
                count++;
            }

            return count;
        }

        public int GetLayerAddWater() => GameConstants.WaterMaxCount - _data.colors.Count;

        bool IsWhtLayer(int index) =>
            _data != null && !IsEmpty() && _data.colors.Count > 1 &&
            CupWhLayerUtility.IsLayerHidden(_data, index);

        public void SetLockNum(int num)
        {
            if (_data == null) return;
            _data.lockNums = num;
            if (_data.lockNums <= 0)
                StartCoroutine(UnlockLockRoutine());
            else
                RefreshLockVisual();
        }

        IEnumerator UnlockLockRoutine()
        {
            if (lockNode == null) yield break;
            FacadeAudio.PlayEffect?.Invoke(EAudioType.EBottleUnlock);
            var rt = lockNode.GetComponent<RectTransform>();
            if (rt != null)
                yield return TweenHelper.ToVector3(rt.anchoredPosition3D,
                    rt.anchoredPosition3D + Vector3.up * (rt.sizeDelta.y * 0.5f), 0.2f,
                    v => rt.anchoredPosition3D = v);
            _data.isLock = 0;
            _data.lockNums = 0;
            _data.lockColor = 0;
            if (lockNode != null) lockNode.SetActive(false);
            if (bottleBg != null) bottleBg.gameObject.SetActive(true);
            // 对齐 Cocos setUnLock：解锁后显示水体并刷新颜色。
            if (waterVisual != null)
            {
                waterVisual.EnsureInitialized();
                if (waterVisual.WaterParent != null)
                    waterVisual.WaterParent.gameObject.SetActive(true);
            }
            InitWaterColor();
            ShowWaterItems();
            UpdateWaterHeight(GameConstants.WaterMaxY[Mathf.Clamp(_data.colors.Count, 0, GameConstants.WaterMaxY.Length - 1)]);
            //SpineService.PlayEffect(transform, Vector3.zero, "bao_xing", "bao");
            PlayProp3Effect();
        }

        public void OnPointerClick()
        {
            if (_pouring) return;
            if (IsVideo())
            {
                _onClick?.Invoke(this);
                return;
            }

            if (IsLock() || IsCollect()) return;
            _onClick?.Invoke(this);
        }

        public void DoSelect()
        {
            StopAllCoroutines();
            // 先启动上移，避免后续特效/音效异常（Console Error Pause）卡住协程导致“瓶子不飞”。
            StartCoroutine(TweenHelper.MoveLocal(transform, _baseLocalPos + Vector3.up * 20f, 0.2f));
            if (lightBg != null)
                lightBg.gameObject.SetActive(true);

            try
            {
                PlaySelectWaterFx();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Bottle] PlaySelectWaterFx failed: {e.Message}");
            }

            if (_shadow != null)
                StartCoroutine(_shadow.FadeShadow(0.7f, Vector3.right * 60f, 0.08f));
            FacadeAudio.PlayEffect?.Invoke(EAudioType.EBottleUp);
        }

        public void DoUnSelect()
        {
            StopAllCoroutines();
            StartCoroutine(TweenHelper.MoveLocal(transform, _baseLocalPos, 0.2f));
            if (lightBg != null)
                lightBg.gameObject.SetActive(false);
            try
            {
                EnsureSelectEffect();
                if (selectEffect != null)
                    selectEffect.gameObject.SetActive(false);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Bottle] Hide selectEffect failed: {e.Message}");
            }
            if (_shadow != null)
                StartCoroutine(_shadow.ResetShadow(0.2f));
            FacadeAudio.PlayEffect?.Invoke(EAudioType.EBottleUp);
        }

        IEnumerator FadeShadow(float alpha, Vector3 offset, float duration)
        {
            if (shadowGroup == null) yield break;
            var start = shadowGroup.transform.localPosition;
            var end = _baseLocalPos + offset;
            shadowGroup.alpha = alpha;
            yield return TweenHelper.ToVector3(start, end, duration, v => shadowGroup.transform.localPosition = v);
        }

        IEnumerator ResetShadow(float duration)
        {
            if (shadowGroup == null) yield break;
            var start = shadowGroup.transform.localPosition;
            yield return TweenHelper.ToVector3(start, _baseLocalPos, duration, v => shadowGroup.transform.localPosition = v);
            shadowGroup.alpha = 1f;
        }

        /// <summary>倒水出去（对应 waterOut），含移动、旋转、skew、水柱与回位。</summary>
        public IEnumerator WaterOut(int color, int num, int dir, Vector3 pourAnchor, float streamEndRootY, Action onPourReach)
        {
            _pouring = true;
            _streamEndRootY = streamEndRootY;
            var originalSiblingIndex = transform.GetSiblingIndex();
            transform.SetAsLastSibling();
            SetPourWaterMask(true);
            HideAllMeniscuses();
            if (lightBg != null) lightBg.gameObject.SetActive(false);
            try
            {
                EnsureSelectEffect();
                if (selectEffect != null)
                    selectEffect.gameObject.SetActive(false);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Bottle] Hide selectEffect on pour failed: {e.Message}");
            }
            if (streamNode != null) streamNode.SetActive(false);

            ApplyPourFlip(dir);

            if (_shadow != null && _shadow.shadowGroup != null)
                _shadow.shadowGroup.alpha = 0f;

            var startCount = _data.colors.Count;
            var endCount = startCount - num;
            var midAngle = new[] { 78f, 66f, 54f, 42f, 30f }[Mathf.Clamp(startCount - 1, 0, 4)];
            var endAngle = GameConstants.BottleAngles[Mathf.Clamp(endCount, 0, 4)];
            var pourDuration = num == 1 ? 0.2f : 0.17f * num;

            var moveDuration = Vector3.Distance(transform.localPosition, BottlePourMath.PourPosition(pourAnchor, midAngle, dir)) / 1200f;
            moveDuration = Mathf.Clamp(moveDuration, 0.15f, 1.2f);

            FacadeAudio.PlayEffect?.Invoke(EAudioType.EBottleMove);
            var canvasGroup = GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();
            canvasGroup.blocksRaycasts = false;
            yield return TweenHelper.MoveLocal(transform, BottlePourMath.PourPosition(pourAnchor, midAngle, dir), moveDuration);

            yield return AnimateAngle(0f, midAngle, moveDuration * 0.5f, dir, pourAnchor, color, false);
            onPourReach?.Invoke();

            yield return AnimateAngle(midAngle, endAngle, pourDuration, dir, pourAnchor, color, true);

            if (streamNode != null) streamNode.SetActive(false);
            for (var i = 0; i < num; i++)
                if (_data.colors.Count > 0)
                    _data.colors.RemoveAt(_data.colors.Count - 1);

            CupWhLayerUtility.RevealHiddenLayerUncoveredByPour(_data);

            ShowWaterItems();
            HideAllMeniscuses();
            yield return new WaitForSeconds(0.1f);

            yield return AnimateAngle(endAngle, 0f, moveDuration, dir, pourAnchor, color, false);
            ResetWaterLayoutAfterPour();

            if (_shadow != null)
                StartCoroutine(_shadow.ResetShadow(0.2f));

            yield return TweenHelper.MoveLocal(transform, _baseLocalPos, 0.1f + moveDuration);
            ResetPourFlip();
            SetPourWaterMask(false);
            InitWaterColor();
            transform.SetSiblingIndex(originalSiblingIndex);
            _pouring = false;
            GetComponent<CanvasGroup>().blocksRaycasts = true;
        }

        IEnumerator AnimateAngle(float from, float to, float duration, int dir, Vector3 pourAnchor, int color, bool showStream)
        {
            var streamShown = false;
            yield return TweenHelper.ToFloat(from, to, duration, angle =>
            {
                if (content != null)
                    content.localRotation = angle < 0.5f
                        ? Quaternion.identity
                        : Quaternion.Euler(0, 0, angle);
                waterVisual?.ApplyPourRotate(angle, _data.colors.Count);
                transform.localPosition = BottlePourMath.PourPosition(pourAnchor, angle, dir);

                if (showStream)
                {
                    if (!streamShown)
                    {
                        streamShown = true;
                        ShowStream(color, angle);
                    }
                    UpdateStreamAngle(angle);
                }
            });
        }

        void ApplyPourFlip(int dir)
        {
            var sx = dir >= 0 ? 1f : -1f;
            transform.localScale = new Vector3(sx, 1f, 1f);
            if (bottleBg != null)
                bottleBg.rectTransform.localScale = new Vector3(sx, 1f, 1f);
            if (waterVisual != null && waterVisual.WhParent != null)
                waterVisual.WhParent.localScale = new Vector3(sx, 1f, 1f);
        }

        void ResetPourFlip()
        {
            transform.localScale = Vector3.one;
            if (bottleBg != null) bottleBg.rectTransform.localScale = Vector3.one;
            if (waterVisual != null && waterVisual.WhParent != null)
                waterVisual.WhParent.localScale = Vector3.one;
            if (content != null) content.localRotation = Quaternion.identity;
        }

        void HideAllMeniscuses() => waterVisual?.SetMeniscusVisible(false);

        void SetStreamColor(int colorId)
        {
            if (!GameConstants.GameColorData.TryGetValue(colorId, out var pair)) return;
            if (streamBody != null)
                streamBody.color = pair.Base;
        }

        /// <summary>接水（对应 waterIn），液面高度补间。</summary>
        public IEnumerator WaterIn(int color, int num)
        {
            _pouring = true;
            var originalSiblingIndex = transform.GetSiblingIndex();
            transform.SetAsLastSibling();
            SetPourWaterMask(true);
            var startCount = _data.colors.Count;
            var startHeight = GameConstants.WaterMaxY[Mathf.Clamp(startCount, 0, GameConstants.WaterMaxY.Length - 1)];

            for (var i = 0; i < num && _data.colors.Count < GameConstants.WaterMaxCount; i++)
                _data.colors.Add(color);

            InitWaterColor();
            UpdateWaterHeight(startHeight);
            var endHeight = GameConstants.WaterMaxY[Mathf.Clamp(_data.colors.Count, 0, GameConstants.WaterMaxY.Length - 1)];
            var duration = 0.2f * num;

            yield return new WaitForSeconds(0.1f);
            FacadeAudio.PlayEffect?.Invoke(EAudioType.EPourWater1);
            var splashHost = CreateSplashHost(color, startHeight);
            yield return TweenHelper.ToFloat(startHeight, endHeight, duration, h =>
            {
                UpdateWaterHeight(h);
                if (splashHost != null)
                    splashHost.transform.localPosition = new Vector3(0, ResolveSplashSurfaceLocalY(h), 0);
            });

            if (splashHost != null)
            {
                SkeletonGraphic splashGraphic = splashHost.GetComponentInChildren<SkeletonGraphic>();
                if (splashGraphic != null && splashGraphic.material != null)
                    Destroy(splashGraphic.material);
                Destroy(splashHost);
            }
            yield return new WaitForSeconds(0.1f);
            InitWaterColor();
            UpdateWaterHeight(endHeight);
            SetPourWaterMask(false);
            TryPlayWhUnlockFx();
            transform.SetSiblingIndex(originalSiblingIndex);
            _pouring = false;
        }

        /// <summary>接水水花应贴在 water_parent 内液面高度，对齐 content 局部坐标。</summary>
        float ResolveSplashSurfaceLocalY(float surfaceHeight)
        {
            EnsureWaterVisual();
            EnsureHierarchyRefs();
            if (waterVisual?.WaterParent is RectTransform waterParentRt
                && content != null
                && waterParentRt.parent == content)
            {
                return waterParentRt.anchoredPosition.y + surfaceHeight;
            }

            // 与 BottleWaterVisual.WaterAreaY 保持一致的后备值
            return -244f + surfaceHeight;
        }

        /// <summary>液面在瓶子父节点（cupPart）局部坐标下的 Y，用于水柱终点计算。</summary>
        public float GetWaterSurfaceRootY(float surfaceHeight)
        {
            var rootParent = transform.parent;
            var surfaceAnchor = content != null ? content : transform;
            var surfaceLocal = new Vector3(0f, ResolveSplashSurfaceLocalY(surfaceHeight), 0f);
            if (rootParent == null)
                return surfaceLocal.y;

            var world = surfaceAnchor.TransformPoint(surfaceLocal);
            return rootParent.InverseTransformPoint(world).y;
        }

        /// <summary>对应 Cocos sdEff：接水时在水面位置播放水花特效，随倒水进度由小变大。</summary>
        GameObject CreateSplashHost(int color, float surfaceHeight)
        {
            GameObject host = new GameObject("SplashHost", typeof(RectTransform), typeof(CanvasGroup));
            host.transform.SetParent(content != null ? content : transform, false);
            host.transform.localPosition = new Vector3(0, ResolveSplashSurfaceLocalY(surfaceHeight), 0);
            host.transform.localScale = Vector3.one;

            SkeletonGraphic shuihuaEffect = GameObject.Instantiate(ResourceMod.Instance.SyncLoad<GameObject>(GameDefines.ShuihuaEffectPath), host.transform).GetComponent<SkeletonGraphic>();
            bool b = ColorUtility.TryParseHtmlString(GameConstants.GameColorData[color].ColorHex, out Color color2);
            ColorUtility.TryParseHtmlString(GameConstants.GameColorData[color].ColorTopHex, out Color color3);
            if (b)
            {
                Material customMat = new Material(shuihuaEffect.material);
                customMat.SetColor("_OutlineColor", color2);
                shuihuaEffect.color = color3;
                shuihuaEffect.material = customMat;
            }
            return host;

            //if (TryCreateSpineSplash(host.transform, color))
            //    return host;

            var ringSprite = GameResourceLoader.LoadSprite("Sprites/Bottle/img_6");
            for (var i = 0; i < 4; i++)
            {
                var ring = new GameObject("yuan" + i, typeof(RectTransform), typeof(Image));
                ring.transform.SetParent(host.transform, false);
                var ringRt = ring.GetComponent<RectTransform>();
                ringRt.anchoredPosition = new Vector2(0f, i * 4f);
                var ringImg = ring.GetComponent<Image>();
                ringImg.sprite = ringSprite;
                ringImg.raycastTarget = false;
                ringImg.color = new Color(1f, 1f, 1f, 0.75f);
                if (ringImg.sprite != null) ringImg.SetNativeSize();
            }
            StartCoroutine(AnimateSplashFx(host));
            return host;
        }

        bool TryCreateSpineSplash(Transform host, int color)
        {
            var skeletonData = GetSplashSkeletonData();
            if (skeletonData == null) return false;

            var go = new GameObject("SpineSplash", typeof(RectTransform));
            go.transform.SetParent(host, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.localScale = Vector3.one;

            var graphic = go.AddComponent<SkeletonGraphic>();
            graphic.skeletonDataAsset = skeletonData;
            graphic.material = CreateSplashMaterialInstance(color);
            graphic.startingAnimation = "shuihua";
            graphic.startingLoop = true;
            graphic.raycastTarget = false;
            graphic.Initialize(false);
            if (!graphic.IsValid)
            {
                Destroy(go);
                return false;
            }
            graphic.AnimationState.SetAnimation(0, "shuihua", true);
            graphic.MatchRectTransformWithBounds();

            if (GameConstants.GameColorData.TryGetValue(color, out var pair))
                graphic.color = pair.Top;

            return true;
        }

        static Material CreateSplashMaterialInstance(int color)
        {
            var mat = new Material(GetSplashScreenGraphicMaterial());
            EnsureStraightAlphaMaterial(mat);
            ConfigureSplashOutline(mat, color);
            return mat;
        }

        /// <summary>浅色水面时加深描边，避免 Screen 混合水花被同色液体淹没。</summary>
        static void ConfigureSplashOutline(Material mat, int color)
        {
            if (mat == null) return;

            mat.EnableKeyword("_USE8NEIGHBOURHOOD_ON");
            mat.SetFloat("_OutlineReferenceTexWidth", 392f);
            mat.SetFloat("_ThresholdEnd", 0.25f);
            mat.SetFloat("_OutlineSmoothness", 0.7f);
            mat.SetFloat("_OutlineMipLevel", 0f);

            var outlineColor = new Color(0.45f, 0.45f, 0.45f, 0.55f);
            var outlineWidth = 2.4f;

            if (GameConstants.GameColorData.TryGetValue(color, out var pair))
            {
                var lum = pair.Top.r * 0.299f + pair.Top.g * 0.587f + pair.Top.b * 0.114f;
                if (lum > 0.62f)
                {
                    outlineColor = new Color(0.38f, 0.38f, 0.38f, 0.62f);
                    outlineWidth = 2.8f;
                }
                else if (lum > 0.45f)
                {
                    outlineColor = new Color(0.42f, 0.42f, 0.42f, 0.58f);
                    outlineWidth = 2.6f;
                }
            }

            mat.SetColor("_OutlineColor", outlineColor);
            mat.SetFloat("_OutlineWidth", outlineWidth);
        }

        static SkeletonDataAsset GetSplashSkeletonData()
        {
            if (_splashSkeletonData != null) return _splashSkeletonData;

            var json = LoadSplashTextAsset("sh");
            if (json == null || SpineRuntimeCompat.IsSpine38Json(json.text))
                return null;

            var imported = Resources.Load<SkeletonDataAsset>("Spine/shui_hua/sh_SkeletonData");
            if (imported != null)
            {
                try
                {
                    if (imported.GetSkeletonData(true) != null)
                    {
                        _splashSkeletonData = imported;
                        return _splashSkeletonData;
                    }
                }
                catch
                {
                    imported.Clear();
                }
            }

            var importedAtlas = Resources.Load<SpineAtlasAsset>("Spine/shui_hua/sh_Atlas");
            if (importedAtlas != null)
            {
                _splashSkeletonData = SkeletonDataAsset.CreateRuntimeInstance(json, importedAtlas, true, SplashSkeletonScale);
                return _splashSkeletonData;
            }

            var atlasText = LoadSplashTextAsset("sh.atlas");
            var atlasSprite = GameResourceLoader.LoadSprite("Spine/shui_hua/sh");
            var tex = atlasSprite != null ? atlasSprite.texture : Resources.Load<Texture2D>("Spine/shui_hua/sh");
            if (atlasText == null || tex == null)
            {
                Debug.LogWarning($"Spine splash load failed. json:{json != null}, atlas:{atlasText != null}, tex:{tex != null}");
                return null;
            }

            tex.name = "sh";
            var atlasAsset = SpineAtlasAsset.CreateRuntimeInstance(
                atlasText,
                new[] { tex },
                GetSplashGraphicMaterial(),
                true);
            _splashSkeletonData = SkeletonDataAsset.CreateRuntimeInstance(json, atlasAsset, true, SplashSkeletonScale);
            return _splashSkeletonData;
        }

        static Material GetSplashScreenGraphicMaterial()
        {
            if (_splashScreenGraphicMaterial != null) return _splashScreenGraphicMaterial;

            _splashScreenGraphicMaterial = Resources.Load<Material>("Spine/shui_hua/sh_Material-Screen");
            if (_splashScreenGraphicMaterial != null)
            {
                EnsureStraightAlphaMaterial(_splashScreenGraphicMaterial);
                return _splashScreenGraphicMaterial;
            }

            var baseMat = GetSplashGraphicMaterial();
            var shader = Shader.Find("AsGame/Spine/SkeletonGraphic Screen Outline")
                         ?? Shader.Find("Spine/SkeletonGraphic Screen")
                         ?? Shader.Find("Spine/SkeletonGraphic");
            _splashScreenGraphicMaterial = new Material(shader)
            {
                mainTexture = baseMat != null ? baseMat.mainTexture : null
            };
            EnsureStraightAlphaMaterial(_splashScreenGraphicMaterial);
            return _splashScreenGraphicMaterial;
        }

        static void EnsureStraightAlphaMaterial(Material mat)
        {
            if (mat == null || !mat.HasProperty("_StraightAlphaInput")) return;
            mat.SetInt("_StraightAlphaInput", 1);
            mat.EnableKeyword("_STRAIGHT_ALPHA_INPUT");
        }

        static TextAsset LoadSplashTextAsset(string assetName)
        {
            var assets = Resources.LoadAll<TextAsset>("Spine/shui_hua");
            foreach (var asset in assets)
            {
                if (asset != null && asset.name == assetName)
                    return asset;
            }
            return Resources.Load<TextAsset>("Spine/shui_hua/" + assetName);
        }

        static Material GetSplashGraphicMaterial()
        {
            if (_splashGraphicMaterial != null)
            {
                EnsureStraightAlphaMaterial(_splashGraphicMaterial);
                return _splashGraphicMaterial;
            }
            _splashGraphicMaterial = Resources.Load<Material>("Spine/shui_hua/sh_Material");
            if (_splashGraphicMaterial != null)
            {
                EnsureStraightAlphaMaterial(_splashGraphicMaterial);
                return _splashGraphicMaterial;
            }

            var shader = Shader.Find("Spine/SkeletonGraphic") ?? Shader.Find("UI/Default");
            _splashGraphicMaterial = new Material(shader);
            EnsureStraightAlphaMaterial(_splashGraphicMaterial);
            return _splashGraphicMaterial;
        }

        IEnumerator AnimateSplashFx(GameObject host)
        {
            var elapsed = 0f;
            while (host != null)
            {
                elapsed += Time.deltaTime;
                for (var i = 0; i < 4; i++)
                {
                    var ring = host.transform.Find("yuan" + i);
                    if (ring == null) continue;
                    var phase = Mathf.Repeat(elapsed * 1.4f + i * 0.22f, 1f);
                    ring.localScale = new Vector3(
                        Mathf.Lerp(0.75f, 1.45f, phase),
                        Mathf.Lerp(0.45f, 0.95f, phase),
                        1f);
                    if (ring.TryGetComponent(out Image img))
                    {
                        var c = img.color;
                        c.a = Mathf.Lerp(0.78f, 0.12f, phase);
                        img.color = c;
                    }
                }

                yield return null;
            }
        }

        static void SetSplashSprite(Transform node, Dictionary<string, Sprite> sprites, string key)
        {
            if (node == null || !node.TryGetComponent(out Image img)) return;
            if (!sprites.TryGetValue(key, out var sp))
            {
                img.enabled = false;
                return;
            }
            img.sprite = sp;
            img.enabled = true;
            img.SetNativeSize();
        }

        static Dictionary<string, Sprite> LoadSplashSprites()
        {
            if (_splashSprites != null) return _splashSprites;
            _splashSprites = new Dictionary<string, Sprite>();
            var atlas = Resources.Load<TextAsset>("Spine/shui_hua/sh");
            var atlasSprite = GameResourceLoader.LoadSprite("Spine/shui_hua/sh");
            var tex = atlasSprite != null ? atlasSprite.texture : Resources.Load<Texture2D>("Spine/shui_hua/sh");
            if (atlas == null || tex == null) return _splashSprites;

            string current = null;
            var x = 0;
            var y = 0;
            var texH = tex.height;
            foreach (var raw in atlas.text.Split('\n'))
            {
                var line = raw.Trim();
                if (string.IsNullOrEmpty(line) || line.EndsWith(".png") || line.StartsWith("format:") ||
                    line.StartsWith("filter:") || line.StartsWith("repeat:"))
                    continue;
                if (!line.Contains(":"))
                {
                    current = line;
                    continue;
                }
                if (current == null) continue;
                if (line.StartsWith("xy:"))
                {
                    var p = line.Substring(3).Split(',');
                    x = int.Parse(p[0].Trim());
                    y = int.Parse(p[1].Trim());
                }
                else if (line.StartsWith("size:"))
                {
                    var p = line.Substring(5).Split(',');
                    var w = int.Parse(p[0].Trim());
                    var h = int.Parse(p[1].Trim());
                    var rect = new Rect(x, texH - y - h, w, h);
                    _splashSprites[current] = Sprite.Create(tex, rect, new Vector2(0.5f, 0.5f), 100f);
                    current = null;
                }
            }
            return _splashSprites;
        }

        void TryPlayWhUnlockFx()
        {
            if (_data == null) return;
            var currentMask = CupWhLayerUtility.GetMask(_data);
            if (_prevWhMask <= 0 || currentMask >= _prevWhMask) return;
            var revealedLayer = -1;
            for (var i = 0; i < CupWhLayerUtility.MaxLayers; i++)
            {
                var wasHidden = (_prevWhMask & (1 << i)) != 0;
                var isHidden = (currentMask & (1 << i)) != 0;
                if (wasHidden && !isHidden)
                {
                    revealedLayer = i;
                    break;
                }
            }

            if (revealedLayer >= 0)
            {
                var y = -GameConstants.HalfBottleHeight + (revealedLayer - 1) * GameConstants.GridHeight;
                //SpineService.PlayEffect(transform, new Vector3(0, y, 0), "wht", "animation2");
            }

            _prevWhNums = _data.whNums;
            _prevWhMask = currentMask;
        }

        void ShowStream(int colorId, float contentAngle)
        {
            if (streamNode == null) return;
            SetStreamColor(colorId);
            streamNode.SetActive(true);
            UpdateStreamAngle(contentAngle);
            if (streamBody != null)
            {
                var rt = streamBody.rectTransform;
                rt.sizeDelta = new Vector2(rt.sizeDelta.x, 0);
                float targetHeight = CalculateStreamHeight();
                StartCoroutine(TweenHelper.ToFloat(0f, targetHeight, 0.1f, h =>
                {
                    if (streamBody == null) return;
                    streamBody.rectTransform.sizeDelta = new Vector2(streamBody.rectTransform.sizeDelta.x, h);
                }));
            }
        }

        void UpdateStreamAngle(float contentAngle)
        {
            if (streamNode == null) return;
            UpdateStreamAnchor();
            streamNode.transform.localRotation = Quaternion.identity;
            if (streamBody != null && streamNode.activeSelf)
            {
                var h = CalculateStreamHeight();
                if (h > 0f)
                    streamBody.rectTransform.sizeDelta = new Vector2(streamBody.rectTransform.sizeDelta.x, h);
            }
        }

        void UpdateStreamAnchor()
        {
            if (streamNode == null || content == null || streamNode.transform.parent == null) return;
            var mouthWorld = content.TransformPoint(new Vector3(StreamMouthX, StreamMouthY, 0));
            streamNode.transform.localPosition = streamNode.transform.parent.InverseTransformPoint(mouthWorld);
        }

        float CalculateStreamHeight()
        {
            if (streamNode == null || transform.parent == null) return 180f;
            Transform parent = transform.parent;
            Vector3 origin = parent.InverseTransformPoint(streamNode.transform.position);
            return Mathf.Clamp(origin.y - _streamEndRootY + 8f, 40f, 420f);
        }

        void SetPourWaterMask(bool enabled)
        {
            waterVisual?.SetPourMask(enabled);
        }

        void UpdateWaterHeight(float totalHeight)
        {
            if (_data == null || waterVisual == null) return;
            waterVisual.ApplyIdleLayout(totalHeight, _data.colors.Count);
        }

        void ShowWaterItems()
        {
            waterVisual?.ShowWaterItems(_data?.colors.Count ?? 0);
        }

        void ResetWaterLayoutAfterPour()
        {
            if (content != null)
                content.localRotation = Quaternion.identity;
            UpdateWaterHeight(GameConstants.WaterMaxY[Mathf.Clamp(_data.colors.Count, 0, GameConstants.WaterMaxY.Length - 1)]);
        }

        void InitWaterColor()
        {
            if (_data == null || waterVisual == null) return;
            var bodies = waterVisual.Bodies;
            var whOverlays = waterVisual.WhOverlays;
            var oldMask = _prevWhMask;
            CupWhLayerUtility.ClampToLayerCount(_data);
            var newMask = CupWhLayerUtility.GetMask(_data);

            for (var i = _data.colors.Count - 1; i >= 0; i--)
            {
                if (bodies == null || i >= bodies.Length || bodies[i] == null) continue;
                if (!GameConstants.GameColorData.TryGetValue(_data.colors[i], out var pair)) continue;

                var isHidden = CupWhLayerUtility.IsLayerHidden(_data, i);
                var wasHidden = (oldMask & (1 << i)) != 0;
                var layer = bodies[i];
                var top = waterVisual.TopAt(i);
                var wh = whOverlays != null && i < whOverlays.Length ? whOverlays[i] : null;

                if (isHidden)
                {
                    layer.color = GameConstants.GameColorData[0].Base;
                    if (top != null) top.color = GameConstants.GameColorData[0].Base;
                    if (wh != null) wh.SetActive(true);
                }
                else
                {
                    if (wh != null) wh.SetActive(false);
                    if (wasHidden)
                        StartCoroutine(RevealWaterLayer(layer, top, pair));
                    else
                    {
                        layer.color = pair.Base;
                        if (top != null) top.color = pair.Top;
                    }
                }
            }

            _prevWhNums = _data.whNums;
            _prevWhMask = newMask;
            waterVisual.RefreshTopMeniscus(_data.colors.Count);
            waterVisual.SyncWhPositions();
            SyncSelectFxAnchorPosition();
            SyncSelectFxTint();
        }

        /// <summary>选中时在水面播放 shui/huang 晃动特效（对齐 Cocos zdEff）。</summary>
        void PlaySelectWaterFx()
        {
            if (selectFxAnchor == null || _data == null || _data.colors.Count == 0)
                return;

            SyncSelectFxAnchorPosition();
            EnsureSelectEffect();
            if (selectEffect == null || selectEffect.skeletonDataAsset == null)
                return;

            selectEffect.gameObject.SetActive(true);
            if (selectEffect.AnimationState == null)
                selectEffect.Initialize(true);

            var state = selectEffect.AnimationState;
            if (state == null)
                return;

            // 缺动画名时 SetAnimation 可能抛错，不影响瓶子上移。
            if (state.Data?.SkeletonData?.FindAnimation("huang") != null)
                state.SetAnimation(0, "huang", false);

            SyncSelectFxTint();
        }

        void SyncSelectFxAnchorPosition()
        {
            if (selectFxAnchor == null || _data == null || _data.colors.Count == 0) return;
            EnsureWaterVisual();
            var waterParent = waterVisual?.WaterParent;
            if (waterParent == null) return;

            var surfaceY = GameConstants.WaterMaxY[
                Mathf.Clamp(_data.colors.Count, 0, GameConstants.WaterMaxY.Length - 1)];

            if (selectFxAnchor.parent != waterParent)
                selectFxAnchor.SetParent(waterParent, false);

            if (selectFxAnchor is RectTransform rt)
            {
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = new Vector2(0f, surfaceY - SelectWaterFxSurfaceOffsetY);
                rt.localScale = Vector3.one;
                rt.SetAsLastSibling();
            }
        }

        void SyncSelectFxTint()
        {
            if (selectFxAnchor == null || !TryGetTopWaterSurfaceColor(out var color)) return;
            foreach (Transform child in selectFxAnchor)
            {
                var graphic = child.GetComponentInChildren<SkeletonGraphic>(true);
                if (graphic != null)
                {
                    graphic.color = color;
                    continue;
                }

                var img = child.GetComponentInChildren<Image>(true);
                if (img != null)
                    img.color = color;
            }
        }

        bool TryGetTopWaterSurfaceColor(out Color color)
        {
            color = Color.white;
            if (_data == null || _data.colors.Count == 0) return false;
            if (!GameConstants.GameColorData.TryGetValue(_data.colors[^1], out var pair)) return false;
            color = pair.Top;
            return true;
        }

        IEnumerator RevealWaterLayer(Image body, Image top, ColorPair pair)
        {
            body.color = GameConstants.GameColorData[0].Base;
            if (top != null) top.color = GameConstants.GameColorData[0].Base;
            yield return TweenHelper.ToFloat(0f, 1f, 0.2f, t =>
            {
                body.color = Color.Lerp(GameConstants.GameColorData[0].Base, pair.Base, t);
                if (top != null)
                    top.color = Color.Lerp(GameConstants.GameColorData[0].Base, pair.Top, t);
            });
        }

        public void RefreshVisual()
        {
            if (_data == null) return;
            EnsureHierarchyRefs();
            EnsureRootVisible();
            RefreshLockVisual();
            if (IsVideo())
                SetupVideoAdVisual();
            else if (adNode != null)
                adNode.SetActive(false);

            if (bottleBg != null)
            {
                bottleBg.gameObject.SetActive(!IsLock());
                if (!IsLock())
                {
                    bottleBg.color = Color.white;
                    if (bottleBg.sprite == null)
                    {
                        var sp = GameResourceLoader.LoadSprite("Sprites/Bottle/img_8")
                               ?? GameResourceLoader.LoadSprite("Sprites/Bottle/img_2");
                        if (sp != null)
                        {
                            bottleBg.sprite = sp;
                            bottleBg.SetNativeSize();
                        }
                    }
                }
            }
            if (waterVisual != null)
            {
                waterVisual.EnsureInitialized();
                if (waterVisual.WaterParent != null)
                    waterVisual.WaterParent.gameObject.SetActive(!IsLock());
            }
            InitWaterColor();
            ShowWaterItems();
            var totalH = GameConstants.WaterMaxY[Mathf.Clamp(_data.colors.Count, 0, GameConstants.WaterMaxY.Length - 1)];
            UpdateWaterHeight(totalH);
            waterVisual?.SyncWhPositions();
            SyncSelectFxAnchorPosition();
            RefreshCollectMark();
        }

        void RefreshCollectMark()
        {
            if (!IsCollect())
            {
                if (_collectMark != null)
                    _collectMark.gameObject.SetActive(false);
                return;
            }

            EnsureCollectMark();
            _collectMark.gameObject.SetActive(true);
            _collectMark.transform.SetAsLastSibling();
        }

        void EnsureCollectMark()
        {
            if (_collectMark != null) return;

            var parent = content != null ? content : transform;
            var go = new GameObject("collectMark", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(0f, -80f);
            rt.sizeDelta = new Vector2(72f, 72f);

            _collectMark = go.GetComponent<Image>();
            _collectMark.raycastTarget = false;
            if (_cachedCollectCheckSprite == null)
                _cachedCollectCheckSprite = ResourceMod.Instance.SyncLoad<Sprite>("UI/GamePlay/icon_check.png");
            if (_cachedCollectCheckSprite != null)
            {
                _collectMark.sprite = _cachedCollectCheckSprite;
                _collectMark.SetNativeSize();
            }
        }

        void RefreshLockVisual()
        {
            if (lockNode == null) return;
            var show = IsLock();
            lockNode.SetActive(show);
            if (!show) return;
            lockNode.transform.SetAsLastSibling();

            // 对齐 Cocos setLock：白色标签 lockColor=0，彩色标签 lockColor>0
            if (lockNumLabel != null)
            {
                lockNumLabel.text = _data.lockNums > 0 ? _data.lockNums.ToString() : "";
                lockNumLabel.color = _data.lockColor > 0 ? Color.white : Color.black;
            }

            if (lockColorImage != null)
            {
                var tagSp = GameResourceLoader.LoadSprite("Sprites/Bottle/img_11");
                if (tagSp != null)
                {
                    lockColorImage.sprite = tagSp;
                    lockColorImage.type = Image.Type.Sliced;
                    lockColorImage.SetNativeSize();
                }

                if (_data.lockColor > 0 &&
                    GameConstants.GameColorData.TryGetValue(_data.lockColor, out var pair))
                    lockColorImage.color = pair.Base;
                else
                    lockColorImage.color = Color.white;
            }
        }

        /// <summary>满瓶收集：Cocos Spine_Collection = he_cheng_2 / guang（双轨交叉流光）。</summary>
        public IEnumerator DoCollected()
        {
            try
            {
                FacadeAudio.PlayEffect?.Invoke(EAudioType.EBottleCollected);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Bottle] DoCollected audio failed: {e.Message}");
            }

            var finished = false;
            EnsureFinishEffect();

            if (finishEffect != null && finishEffect.skeletonDataAsset != null)
            {
                try
                {
                    finishEffect.gameObject.SetActive(true);
                    if (finishEffect.AnimationState == null)
                        finishEffect.Initialize(true);
                    TrackEntry trackEntry = finishEffect.AnimationState?.SetAnimation(0, "guang", false);
                    if (trackEntry != null)
                        trackEntry.Complete += _ => { finished = true; };
                    else
                        finished = true;
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[Bottle] DoCollected effect failed: {e.Message}");
                    finished = true;
                }
            }
            else
            {
                finished = true;
            }

            var elapsed = 0f;
            while (!finished && elapsed < 2f)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            yield return new WaitForSeconds(0.2f);
        }

        void EnsureFinishEffect()
        {
            if (finishEffect != null)
                return;

            var finishTr = transform.Find("content/finishEffect")
                            ?? transform.Find("finishEffect");
            if (finishTr == null)
                finishTr = FindChildUtility.FindChild(transform, "finishEffect")?.transform;
            if (finishTr != null)
                finishEffect = finishTr.GetComponent<SkeletonGraphic>();
        }

        public IEnumerator DisappearEmpty()
        {
            if (IsVideo()) yield break;
            if (!IsEmpty()) yield break;
            var cg = GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();
            yield return TweenHelper.FadeCanvasGroup(cg, 0f, 0.2f);
            gameObject.SetActive(false);
            if (_shadow != null)
                _shadow.gameObject.SetActive(false);
        }

        public void ApplyBottleSprite()
        {
            //if (bottleBg != null && bottleSprite != null)
            //{
            //    bottleBg.sprite = bottleSprite;
            //    bottleBg.SetNativeSize();
            //}

            var btn = GetComponent<Button>();
            if (btn != null)
            {
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(OnPointerClick);
            }
        }

        public static Bottle Create(Transform parent)
        {
            //GameObject prefab = Resources.Load<GameObject>(PrefabPaths.Bottle);
            GameObject prefab = ResourceMod.Instance.SyncLoad<GameObject>(GameDefines.BottlePath);
            if (prefab == null)
            {
                return CreateLegacy(parent);
            }

            GameObject go = UnityEngine.Object.Instantiate(prefab, parent);
            go.name = "Bottle";
            var rt = go.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.sizeDelta = new Vector2(GameConstants.BottleWidth, GameConstants.BottleHeight);
                rt.pivot = new Vector2(0.5f, 0.5f);
            }

            var ctrl = go.GetComponent<Bottle>();
            ctrl.EnsureHierarchyRefs();
            ctrl.EnsureWaterVisual();
            ctrl.ApplyBottleSprite();
            return ctrl;
        }

        void Awake()
        {
            EnsureHierarchyRefs();
            EnsureWaterVisual();
        }

        /// <summary>确保水体组件已绑定（Prefab 或代码创建）。</summary>
        public void EnsureWaterVisual()
        {
            if (waterVisual == null && content != null)
            {
                waterVisual = content.GetComponentInChildren<BottleWaterVisual>(true);
                if (waterVisual == null)
                {
                    var waterSprite = GameResourceLoader.LoadSprite("Sprites/Bottle/img_7")
                                      ?? GameResourceLoader.LoadSprite("Sprites/Bottle/rect");
                    var topSprite = GameResourceLoader.LoadSprite("Sprites/Bottle/img_6");
                    var whSprite = GameResourceLoader.LoadSprite("Sprites/Bottle/icon_8");
                    waterVisual = BottleWaterVisual.Build(content, waterSprite, topSprite, whSprite);
                }
            }

            waterVisual?.EnsureInitialized();
        }

        public static Bottle CreateLegacy(Transform parent)
        {
            var root = new GameObject("Bottle", typeof(RectTransform), typeof(CanvasGroup), typeof(Bottle));
            root.transform.SetParent(parent, false);
            var rt = root.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(GameConstants.BottleWidth, GameConstants.BottleHeight);
            rt.pivot = new Vector2(0.5f, 1f);

            var ctrl = root.GetComponent<Bottle>();

            var contentGo = new GameObject("content", typeof(RectTransform));
            contentGo.transform.SetParent(root.transform, false);
            var contentRt = contentGo.GetComponent<RectTransform>();
            contentRt.anchorMin = contentRt.anchorMax = new Vector2(0.5f, 1f);
            contentRt.pivot = new Vector2(0.5f, 1f);
            contentRt.sizeDelta = rt.sizeDelta;
            ctrl.content = contentRt;

            var waterSprite = GameResourceLoader.LoadSprite("Sprites/Bottle/img_7")
                              ?? GameResourceLoader.LoadSprite("Sprites/Bottle/rect");
            var topSprite = GameResourceLoader.LoadSprite("Sprites/Bottle/img_6");
            var whSprite = GameResourceLoader.LoadSprite("Sprites/Bottle/icon_8");
            ctrl.waterVisual = BottleWaterVisual.Build(contentGo.transform, waterSprite, topSprite, whSprite);

            var lightGo = new GameObject("light_bg", typeof(RectTransform), typeof(Image));
            lightGo.transform.SetParent(contentGo.transform, false);
            var lightRt = lightGo.GetComponent<RectTransform>();
            lightRt.anchorMin = lightRt.anchorMax = new Vector2(0.5f, 1f);
            lightRt.pivot = new Vector2(0.5f, 1f);
            lightRt.anchoredPosition = new Vector2(0, 18.375f);
            var lightSp = GameResourceLoader.LoadSprite("Sprites/Bottle/img_12")
                          ?? GameResourceLoader.LoadSprite("Sprites/Bottle/img_10");
            var lightImg = lightGo.GetComponent<Image>();
            if (lightSp != null)
            {
                lightImg.sprite = lightSp;
                lightImg.SetNativeSize();
            }
            lightImg.color = Color.white;
            lightImg.raycastTarget = false;
            ctrl.lightBg = lightImg;
            lightGo.SetActive(false);

            var bgGo = new GameObject("bg", typeof(RectTransform), typeof(Image));
            bgGo.transform.SetParent(contentGo.transform, false);
            // 玻璃 bg 在水之上、光泽 light_bg 之下（与 Cocos 渲染顺序一致）。
            lightGo.transform.SetAsLastSibling();
            var bgRt = bgGo.GetComponent<RectTransform>();
            bgRt.anchorMin = bgRt.anchorMax = new Vector2(0.5f, 1f);
            bgRt.pivot = new Vector2(0.5f, 1f);
            var bgImg = bgGo.GetComponent<Image>();
            //bgImg.sprite = bottleSprite;
            bgImg.raycastTarget = false;
            //if (bottleSprite != null) bgImg.SetNativeSize();
            ctrl.bottleBg = bgImg;

            var streamGo = new GameObject("shuiZhu", typeof(RectTransform));
            streamGo.transform.SetParent(root.transform, false);
            var streamRt = streamGo.GetComponent<RectTransform>();
            streamRt.anchorMin = streamRt.anchorMax = new Vector2(0.5f, 1f);
            streamRt.pivot = new Vector2(0.5f, 1f);
            streamRt.anchoredPosition = new Vector2(StreamMouthX, StreamMouthY);
            ctrl.streamNode = streamGo;
            var bodyStream = new GameObject("body", typeof(RectTransform), typeof(Image));
            bodyStream.transform.SetParent(streamGo.transform, false);
            var bodyStreamRt = bodyStream.GetComponent<RectTransform>();
            bodyStreamRt.sizeDelta = new Vector2(8, 0);
            bodyStreamRt.pivot = new Vector2(0.5f, 1f);
            ctrl.streamBody = bodyStream.GetComponent<Image>();
            var streamSp = GameResourceLoader.LoadSprite("Sprites/Bottle/rect");
            if (streamSp != null)
            {
                ctrl.streamBody.sprite = streamSp;
                ctrl.streamBody.type = Image.Type.Sliced;
            }
            ctrl.streamBody.color = Color.white;
            ctrl.streamBody.raycastTarget = false;
            streamGo.SetActive(false);

            var lockGo = new GameObject("clock", typeof(RectTransform));
            lockGo.transform.SetParent(contentGo.transform, false);
            var lockRt = lockGo.GetComponent<RectTransform>();
            lockRt.anchorMin = lockRt.anchorMax = new Vector2(0.5f, 1f);
            lockRt.pivot = new Vector2(0.5f, 0f);
            lockRt.anchoredPosition = new Vector2(0, -GameConstants.HalfBottleHeight);
            lockRt.sizeDelta = rt.sizeDelta;
            var lockCupSp = GameResourceLoader.LoadSprite("Sprites/Bottle/img_10");
            if (lockCupSp != null)
            {
                var cupImgGo = new GameObject("cup", typeof(RectTransform), typeof(Image));
                cupImgGo.transform.SetParent(lockGo.transform, false);
                var cupImg = cupImgGo.GetComponent<Image>();
                cupImg.sprite = lockCupSp;
                cupImg.SetNativeSize();
                cupImg.color = new Color(1, 1, 1, 0.85f);
            }
            var colorGo = new GameObject("color", typeof(RectTransform), typeof(Image));
            colorGo.transform.SetParent(lockGo.transform, false);
            var colorRt = colorGo.GetComponent<RectTransform>();
            colorRt.anchorMin = colorRt.anchorMax = new Vector2(0.5f, 0.5f);
            colorRt.pivot = new Vector2(0.5f, 0.5f);
            colorRt.anchoredPosition = new Vector2(0f, -37.5f);
            colorRt.sizeDelta = new Vector2(88f, 104f);
            ctrl.lockColorImage = colorGo.GetComponent<Image>();
            var numGo = new GameObject("num", typeof(RectTransform), typeof(Text));
            numGo.transform.SetParent(lockGo.transform, false);
            var numRt = numGo.GetComponent<RectTransform>();
            numRt.anchorMin = numRt.anchorMax = new Vector2(0.5f, 0.5f);
            numRt.pivot = new Vector2(0.5f, 0.5f);
            numRt.anchoredPosition = new Vector2(0f, -40.872f);
            numRt.sizeDelta = new Vector2(60f, 40f);
            ctrl.lockNumLabel = numGo.GetComponent<Text>();
            ctrl.lockNumLabel.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            ctrl.lockNumLabel.fontSize = 28;
            ctrl.lockNumLabel.fontStyle = FontStyle.Bold;
            ctrl.lockNumLabel.alignment = TextAnchor.MiddleCenter;
            ctrl.lockNumLabel.color = Color.black;
            ctrl.lockNode = lockGo;
            lockGo.SetActive(false);

            var eggBaseGo = new GameObject("img_13", typeof(RectTransform), typeof(Image));
            eggBaseGo.transform.SetParent(contentGo.transform, false);
            eggBaseGo.transform.SetAsFirstSibling();
            var eggRt = eggBaseGo.GetComponent<RectTransform>();
            eggRt.anchorMin = eggRt.anchorMax = new Vector2(0.5f, 1f);
            eggRt.pivot = new Vector2(0.5f, 1f);
            eggRt.anchoredPosition = new Vector2(0f, -220.91f);
            var eggSp = GameResourceLoader.LoadSprite("Sprites/Bottle/img_13");
            if (eggSp != null)
            {
                var eggImg = eggBaseGo.GetComponent<Image>();
                eggImg.sprite = eggSp;
                eggImg.SetNativeSize();
                eggImg.raycastTarget = false;
            }

            ctrl.adNode = new GameObject("ad", typeof(RectTransform), typeof(Image));
            ctrl.adNode.transform.SetParent(contentGo.transform, false);
            var adRt = ctrl.adNode.GetComponent<RectTransform>();
            adRt.anchoredPosition = new Vector2(0, -GameConstants.HalfBottleHeight);
            ctrl.adNode.SetActive(false);

            var fxGo = new GameObject("selectFx", typeof(RectTransform));
            fxGo.transform.SetParent(ctrl.waterVisual.WaterParent, false);
            ctrl.selectFxAnchor = fxGo.transform;

            // 透明点击区：扩大可点范围，避免瓶身 Image 关闭射线检测后点不中
            var hitImg = root.AddComponent<Image>();
            hitImg.color = new Color(1f, 1f, 1f, 0.001f);
            hitImg.raycastTarget = true;
            rt.sizeDelta = new Vector2(GameConstants.BottleWidth + 24f, GameConstants.BottleHeight + 20f);

            var click = root.AddComponent<Button>();
            click.transition = Selectable.Transition.None;
            click.targetGraphic = hitImg;
            click.onClick.AddListener(ctrl.OnPointerClick);

            return ctrl;
        }

        static void StretchRect(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }

        public static Bottle CreateShadow(Transform parent)
        {
            //var prefab = Resources.Load<GameObject>(PrefabPaths.BottleShadow);
            GameObject prefab = ResourceMod.Instance.SyncLoad<GameObject>(GameDefines.BottleShadowPath);
            if (prefab != null)
            {
                var go = UnityEngine.Object.Instantiate(prefab, parent);
                go.name = "Shadow";
                var ctrl = go.GetComponent<Bottle>();
                ctrl.ApplyShadowSprite();
                return ctrl;
            }

            return CreateShadowLegacy(parent);
        }

        public void ApplyShadowSprite()
        {
            var img = transform.Find("img")?.GetComponent<Image>();
            if (img == null) return;
            //img.color = new Color(0f, 0f, 0f, 0.25f);
            img.color = Color.white;

            shadowGroup = GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();
            shadowGroup.alpha = 1f;
        }

        public static Bottle CreateShadowLegacy(Transform parent)
        {
            // 对齐 Cocos Shadow.prefab：197×135，anchor (0.24, 0.9)，img_9 原色显示。
            const float shadowWidth = 197f;
            const float shadowHeight = 135f;
            var shadowPivot = new Vector2(0.24f, 0.9f);

            var go = new GameObject("Shadow", typeof(RectTransform), typeof(CanvasGroup), typeof(Bottle));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.pivot = shadowPivot;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(shadowWidth, shadowHeight);

            var imgGo = new GameObject("img", typeof(RectTransform), typeof(Image));
            imgGo.transform.SetParent(go.transform, false);
            StretchRect(imgGo.GetComponent<RectTransform>());
            var img = imgGo.GetComponent<Image>();
            img.raycastTarget = false;
            img.preserveAspect = false;
            img.color = Color.white;
            //img.color = new Color(0f, 0f, 0f, 0.25f);

            var ctrl = go.GetComponent<Bottle>();
            ctrl.shadowGroup = go.GetComponent<CanvasGroup>();
            ctrl.shadowGroup.alpha = 1f;
            return ctrl;
        }

        public void PlayProp3Effect()
        {
            if (unlockEffect == null || unlockEffect.skeletonDataAsset == null)
                return;

            try
            {
                unlockEffect.gameObject.SetActive(true);
                if (unlockEffect.AnimationState == null)
                    unlockEffect.Initialize(true);
                if (unlockEffect.AnimationState == null)
                    return;

                unlockEffect.AnimationState.SetAnimation(0, "bao", false);
                unlockEffect.AnimationState.Complete += _ =>
                {
                    if (unlockEffect != null)
                        unlockEffect.gameObject.SetActive(false);
                };
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Bottle] PlayProp3Effect failed: {e.Message}");
            }
        }
    }
}
