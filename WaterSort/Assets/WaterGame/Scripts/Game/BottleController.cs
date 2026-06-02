using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using AsGame.Core;
using AsGame.Data;
using AsGame.Spine;
using AsGame.UI;
using Spine.Unity;

namespace AsGame.Water
{
    /// <summary>Cocos CupComp 核心逻辑移植，倒水/选中均使用补间动画。</summary>
    public class BottleController : MonoBehaviour
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

        CupData _data;
        Action<BottleController> _onClick;
        Vector3 _baseLocalPos;
        bool _pouring;
        BottleController _shadow;
        int _prevWhNums;
        float _streamEndRootY;
        static Dictionary<string, Sprite> _splashSprites;
        static SkeletonDataAsset _splashSkeletonData;
        static Material _splashGraphicMaterial;
        static Material _splashScreenGraphicMaterial;
        static Sprite _cachedAdIconSprite;

        const float StreamMouthX = -21f;
        const float StreamMouthY = -22f;
        /// <summary>与 sh_SkeletonData.asset 一致；Spine 像素经 scale×referencePixelsPerUnit 映射到 UI 像素。</summary>
        const float SplashSkeletonScale = 0.01f;
        /// <summary>对齐 Cocos Cup.prefab 中 zd 相对 effect 节点的 Y 偏移。</summary>
        const float SelectWaterFxSurfaceOffsetY = 19f;
        const float SelectWaterFxDuration = 1.3f;

        public bool Pouring => _pouring;
        public CupData Data => _data;

        public void BindShadow(BottleController shadow)
        {
            _shadow = shadow;
            shadow?.SetBaseLocalPosition(shadow.transform.localPosition);
        }

        public void SetBaseLocalPosition(Vector3 pos) => _baseLocalPos = pos;

        public void Init(CupData data, Action<BottleController> onClick)
        {
            EnsureHierarchyRefs();
            EnsureWaterVisual();
            _data = data.Clone();
            _onClick = onClick;
            _pouring = false;
            _baseLocalPos = transform.localPosition;
            _prevWhNums = _data.whNums;
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
                var fxTr = content.Find("selectFx");
                if (fxTr != null) selectFxAnchor = fxTr;
            }
            if (waterVisual == null)
                waterVisual = content.GetComponentInChildren<BottleWaterVisual>(true);
        }

        static Sprite GetAdIconSprite()
        {
            if (_cachedAdIconSprite == null)
                _cachedAdIconSprite = GameResourceLoader.LoadSprite("Sprites/Bottle/icon_6");
            return _cachedAdIconSprite;
        }

        /// <summary>对齐 Cocos Cup.ad/icon：广告图标挂在 ad/icon 上。</summary>
        void SetupVideoAdVisual()
        {
            if (!IsVideo() || adNode == null) return;

            adNode.SetActive(true);
            adNode.transform.SetAsLastSibling();

            // 父节点 Image 无 sprite 时会画白块，禁用后只用子节点 icon 显示
            var parentImg = adNode.GetComponent<Image>();
            if (parentImg != null)
                parentImg.enabled = false;

            var iconTr = adNode.transform.Find("icon");
            if (iconTr == null)
            {
                var iconGo = new GameObject("icon", typeof(RectTransform), typeof(Image));
                iconGo.transform.SetParent(adNode.transform, false);
                iconTr = iconGo.transform;
                var rt = iconTr as RectTransform;
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = Vector2.zero;
            }

            var iconImg = iconTr.GetComponent<Image>() ?? iconTr.gameObject.AddComponent<Image>();
            iconImg.raycastTarget = false;
            var adSp = GetAdIconSprite();
            if (adSp != null)
            {
                iconImg.sprite = adSp;
                iconImg.SetNativeSize();
                iconImg.color = Color.white;
            }
            else
                Debug.LogWarning("[BottleController] 未加载到 Sprites/Bottle/icon_6，广告瓶图标为空。");
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
            RefreshVisual();
        }

        public int GetTopColorId() => _data.colors.Count > 0 ? _data.colors[^1] : 0;

        public bool IsCollect()
        {
            if (_data == null || _data.colors.Count != GameConstants.WaterMaxCount) return false;
            var top = GetTopColorId();
            foreach (var c in _data.colors)
                if (c != top) return false;
            return _data.whNums <= 0;
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
            _data.whNums > 0 && !IsEmpty() && _data.colors.Count > 1 && index < _data.whNums;

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
            AudioManager.Instance?.PlaySfx("BottleUnlock");
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
            SpineService.PlayEffect(transform, Vector3.zero, "bao_xing", "bao");
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
            StartCoroutine(TweenHelper.MoveLocal(transform, _baseLocalPos + Vector3.up * 20f, 0.2f));
            if (lightBg != null) lightBg.gameObject.SetActive(true);
            PlaySelectWaterFx();
            if (_shadow != null)
                StartCoroutine(_shadow.FadeShadow(0.7f, Vector3.right * 60f, 0.08f));
            AudioManager.Instance?.PlaySfx("BottleUp");
        }

        public void DoUnSelect()
        {
            StopAllCoroutines();
            StartCoroutine(TweenHelper.MoveLocal(transform, _baseLocalPos, 0.2f));
            if (lightBg != null) lightBg.gameObject.SetActive(false);
            if (selectFxAnchor != null)
                SpineService.ClearEffects(selectFxAnchor);
            if (_shadow != null)
                StartCoroutine(_shadow.ResetShadow(0.2f));
            AudioManager.Instance?.PlaySfx("BottleUp");
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
            if (selectFxAnchor != null)
                SpineService.ClearEffects(selectFxAnchor);
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

            AudioManager.Instance?.PlaySfx("BottleMove");
            yield return TweenHelper.MoveLocal(transform, BottlePourMath.PourPosition(pourAnchor, midAngle, dir), moveDuration);

            yield return AnimateAngle(0f, midAngle, moveDuration * 0.5f, dir, pourAnchor, color, false);
            onPourReach?.Invoke();

            yield return AnimateAngle(midAngle, endAngle, pourDuration, dir, pourAnchor, color, true);

            if (streamNode != null) streamNode.SetActive(false);
            for (var i = 0; i < num; i++)
                if (_data.colors.Count > 0)
                    _data.colors.RemoveAt(_data.colors.Count - 1);

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
            AudioManager.Instance?.PlaySfx("PourWater1");
            var splashHost = CreateSplashHost(color, startHeight);
            yield return TweenHelper.ToFloat(startHeight, endHeight, duration, h =>
            {
                UpdateWaterHeight(h);
                if (splashHost != null)
                {
                    splashHost.transform.localPosition = new Vector3(0, -238f + h, 0);
                }
            });

            if (splashHost != null)
            {
                var splashGraphic = splashHost.GetComponentInChildren<SkeletonGraphic>();
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

        /// <summary>对应 Cocos sdEff：接水时在水面位置播放水花特效，随倒水进度由小变大。</summary>
        GameObject CreateSplashHost(int color, float surfaceHeight)
        {
            // water_parent 底边在 content 局部 y = -238，水面在其上 surfaceHeight 处。
            var host = new GameObject("SplashHost", typeof(RectTransform), typeof(CanvasGroup));
            host.transform.SetParent(content != null ? content : transform, false);
            host.transform.localPosition = new Vector3(0, -238f + surfaceHeight + 8f, 0);
            host.transform.localScale = Vector3.one;
            if (TryCreateSpineSplash(host.transform, color))
                return host;

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
            if (_data == null || _prevWhNums <= 0 || _data.whNums >= _prevWhNums) return;
            var y = -GameConstants.HalfBottleHeight + (_data.whNums - 2) * GameConstants.GridHeight;
            SpineService.PlayEffect(transform, new Vector3(0, y, 0), "wht", "animation2");
            _prevWhNums = _data.whNums;
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
                var targetHeight = CalculateStreamHeight();
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
            var parent = transform.parent;
            var origin = parent.InverseTransformPoint(streamNode.transform.position);
            return Mathf.Clamp(origin.y - _streamEndRootY + 8f, 40f, 260f);
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
            var oldWh = _prevWhNums;
            _data.whNums = Mathf.Min(_data.whNums, Mathf.Max(0, _data.colors.Count - 1));
            var revealIndex = oldWh > 0 && _data.whNums < oldWh ? _data.whNums : -1;

            for (var i = _data.colors.Count - 1; i >= 0; i--)
            {
                if (bodies == null || i >= bodies.Length || bodies[i] == null) continue;
                if (!GameConstants.GameColorData.TryGetValue(_data.colors[i], out var pair)) continue;

                var isHidden = i < _data.whNums;
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
                    if (i == revealIndex)
                        StartCoroutine(RevealWaterLayer(layer, top, pair));
                    else
                    {
                        layer.color = pair.Base;
                        if (top != null) top.color = pair.Top;
                    }
                }
            }

            _prevWhNums = _data.whNums;
            waterVisual.RefreshTopMeniscus(_data.colors.Count);
            waterVisual.SyncWhPositions();
            SyncSelectFxAnchorPosition();
            SyncSelectFxTint();
        }

        /// <summary>选中时在水面播放 shui/huang 晃动特效（对齐 Cocos zdEff）。</summary>
        void PlaySelectWaterFx()
        {
            if (selectFxAnchor == null || _data == null || _data.colors.Count == 0) return;
            SyncSelectFxAnchorPosition();
            if (!TryGetTopWaterSurfaceColor(out var color)) return;
            SpineService.ClearEffects(selectFxAnchor);
            SpineService.PlayEffect(selectFxAnchor, Vector3.zero, "shui", "huang", loop: false, tint: color,
                duration: SelectWaterFxDuration);
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

        public IEnumerator DoCollected()
        {
            AudioManager.Instance?.PlaySfx("BottleCollected");
            SpineService.PlayEffect(transform, new Vector3(0, -GameConstants.HalfBottleHeight, 0), "xuan_zhong", "guang");
            yield return new WaitForSeconds(0.5f);
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

        public void ApplyBottleSprite(Sprite bottleSprite)
        {
            if (bottleBg != null && bottleSprite != null)
            {
                bottleBg.sprite = bottleSprite;
                bottleBg.SetNativeSize();
            }

            var btn = GetComponent<Button>();
            if (btn != null)
            {
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(OnPointerClick);
            }
        }

        public static BottleController Create(Transform parent, Sprite bottleSprite)
        {
            var prefab = Resources.Load<GameObject>(PrefabPaths.Bottle);
            if (prefab == null)
            {
                Debug.LogWarning("[BottleController] Missing Resources/" + PrefabPaths.Bottle + ", using legacy build.");
                return CreateLegacy(parent, bottleSprite);
            }

            var go = UnityEngine.Object.Instantiate(prefab, parent);
            go.name = "Bottle";
            var rt = go.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.sizeDelta = new Vector2(GameConstants.BottleWidth, GameConstants.BottleHeight);
                rt.pivot = new Vector2(0.5f, 1f);
            }

            var ctrl = go.GetComponent<BottleController>();
            ctrl.EnsureHierarchyRefs();
            ctrl.EnsureWaterVisual();
            ctrl.ApplyBottleSprite(bottleSprite);
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

        public static BottleController CreateLegacy(Transform parent, Sprite bottleSprite)
        {
            var root = new GameObject("Bottle", typeof(RectTransform), typeof(CanvasGroup), typeof(BottleController));
            root.transform.SetParent(parent, false);
            var rt = root.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(GameConstants.BottleWidth, GameConstants.BottleHeight);
            rt.pivot = new Vector2(0.5f, 1f);

            var ctrl = root.GetComponent<BottleController>();

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
            bgImg.sprite = bottleSprite;
            bgImg.raycastTarget = false;
            if (bottleSprite != null) bgImg.SetNativeSize();
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

        public static BottleController CreateShadow(Transform parent, Sprite shadowSprite)
        {
            var prefab = Resources.Load<GameObject>(PrefabPaths.BottleShadow);
            if (prefab != null)
            {
                var go = UnityEngine.Object.Instantiate(prefab, parent);
                go.name = "Shadow";
                var ctrl = go.GetComponent<BottleController>();
                ctrl.ApplyShadowSprite(shadowSprite);
                return ctrl;
            }

            return CreateShadowLegacy(parent, shadowSprite);
        }

        public void ApplyShadowSprite(Sprite shadowSprite)
        {
            var img = transform.Find("img")?.GetComponent<Image>();
            if (img == null) return;
            if (shadowSprite != null)
            {
                img.sprite = shadowSprite;
                img.type = Image.Type.Simple;
                img.preserveAspect = false;
                img.color = Color.white;
            }
            else
                img.color = new Color(0f, 0f, 0f, 0.25f);

            shadowGroup = GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();
            shadowGroup.alpha = 1f;
        }

        public static BottleController CreateShadowLegacy(Transform parent, Sprite shadowSprite)
        {
            // 对齐 Cocos Shadow.prefab：197×135，anchor (0.24, 0.9)，img_9 原色显示。
            const float shadowWidth = 197f;
            const float shadowHeight = 135f;
            var shadowPivot = new Vector2(0.24f, 0.9f);

            var go = new GameObject("Shadow", typeof(RectTransform), typeof(CanvasGroup), typeof(BottleController));
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
            if (shadowSprite != null)
            {
                img.sprite = shadowSprite;
                img.type = Image.Type.Simple;
                img.preserveAspect = false;
                img.color = Color.white;
            }
            else
                img.color = new Color(0f, 0f, 0f, 0.25f);

            var ctrl = go.GetComponent<BottleController>();
            ctrl.shadowGroup = go.GetComponent<CanvasGroup>();
            ctrl.shadowGroup.alpha = 1f;
            return ctrl;
        }
    }
}
