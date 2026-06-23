using UnityEngine;
using UnityEngine.UI;
using AsGame.Core;
using AsGame.UI;
using XrCode;
using AsGame;

/// <summary>
/// 对齐 Cocos Cup.prefab：water_parent(Mask) / water1..4(rect+top)。
///
/// Cocos 关键结构（决定视觉）：
/// - cc.Layout(纵向) 按各层 node.height 从底向上堆叠（这里用手动堆叠替代）。
/// - 底层 water1 的 rect 仅顶对齐、高度=层高，不溢出。
/// - 上层 water2/3/4 的 rect 上下拉伸且向下溢出 24px(_bottom:-24)，盖住下层液面，
///   使同色水无缝连续、异色处用 img_7 圆角底形成弧形分界。
/// - 每层都有 top(img_6 椭圆)液面，但只有最顶层露出，其余被上层 rect 覆盖。
/// - 闲置不开 Mask；倒水时开 Mask 并把底层 rect 拉高到 500 填满。
/// </summary>
public class BottleWaterVisual : MonoBehaviour
{
    const float WaterAreaY = -244f;
    const float WaterAreaH = 248f;
    const float WaterWidth = 90f;
    const float MeniscusHeight = 43f;
    const float UpperOverflow = 24f; // 上层 rect 向下溢出量（Cocos _bottom:-24）

    RectTransform _layoutRoot;
    RectTransform[] _layers;
    RectTransform[] _bodyRects;
    Image[] _bodies;
    Image[] _tops;
    UISkewGraphic[] _skews;
    float[] _heights;
    float[] _offsetX;
    Mask _mask;
    Image _maskImage;
    bool _pourMaskEnabled;

    public RectTransform WaterParent { get; private set; }
    public RectTransform WhParent { get; private set; }
    public Image[] Bodies => _bodies;
    public GameObject[] WhOverlays { get; private set; }

    void Awake() => EnsureInitialized();

    /// <summary>Prefab 实例化后绑定层级引用（Build 仅在代码创建时调用）。</summary>
    public bool EnsureInitialized()
    {
        if (_layers != null && _layers.Length > 0 && _layers[0] != null)
            return true;

        WaterParent = transform as RectTransform;
        _mask = GetComponent<Mask>();
        _maskImage = GetComponent<Image>();

        _layoutRoot = transform.Find("ly") as RectTransform;
        WhParent = transform.Find("wh") as RectTransform;
        if (_layoutRoot == null)
            return false;

        var n = GameConstants.WaterMaxCount;
        _layers = new RectTransform[n];
        _bodyRects = new RectTransform[n];
        _bodies = new Image[n];
        _tops = new Image[n];
        _skews = new UISkewGraphic[n];
        _heights = new float[n];
        _offsetX = new float[n];
        WhOverlays = new GameObject[n];

        for (var i = 0; i < n; i++)
        {
            var layer = _layoutRoot.Find("water" + (i + 1)) as RectTransform;
            _layers[i] = layer;
            if (layer == null) continue;

            var rectTf = layer.Find("rect") as RectTransform;
            _bodyRects[i] = rectTf;
            _bodies[i] = rectTf != null ? rectTf.GetComponent<Image>() : null;
            _skews[i] = rectTf != null ? rectTf.GetComponent<UISkewGraphic>() : null;
            var topTf = layer.Find("top");
            _tops[i] = topTf != null ? topTf.GetComponent<Image>() : null;
            _heights[i] = i == 0 ? GameConstants.GridOneHeight : GameConstants.GridHeight;
            _offsetX[i] = 0f;
        }

        if (WhParent != null)
        {
            for (var i = 0; i < n; i++)
            {
                var wh = WhParent.Find("wh" + (i + 1));
                WhOverlays[i] = wh != null ? wh.gameObject : null;
            }
        }

        return _layers[0] != null;
    }

    public Image TopAt(int index) =>
        _tops != null && index >= 0 && index < _tops.Length ? _tops[index] : null;

    static float Overflow(int index) => index == 0 ? 0f : UpperOverflow;

    public static BottleWaterVisual Build(Transform content, Sprite bodySprite, Sprite topSprite, Sprite whSprite)
    {
        // water_parent 用 Mask(模板)而非 RectMask2D：RectMask2D 不支持旋转，倒水时会把水裁没。
        var waterParentGo = new GameObject("water_parent", typeof(RectTransform), typeof(Image), typeof(Mask), typeof(BottleWaterVisual));
        waterParentGo.transform.SetParent(content, false);
        var waterParentRt = waterParentGo.GetComponent<RectTransform>();
        waterParentRt.anchorMin = waterParentRt.anchorMax = new Vector2(0.5f, 1f);
        waterParentRt.pivot = new Vector2(0.5f, 0f);
        waterParentRt.anchoredPosition = new Vector2(0, WaterAreaY);
        waterParentRt.sizeDelta = new Vector2(WaterWidth, WaterAreaH);

        var maskImg = waterParentGo.GetComponent<Image>();
        var maskSprite = GameResourceLoader.LoadSprite("Sprites/Bottle/sgsg2");
        if (maskSprite != null) maskImg.sprite = maskSprite;
        maskImg.raycastTarget = false;
        maskImg.enabled = false;
        var mask = waterParentGo.GetComponent<Mask>();
        mask.showMaskGraphic = false;
        mask.enabled = false;

        var lyGo = new GameObject("ly", typeof(RectTransform));
        lyGo.transform.SetParent(waterParentGo.transform, false);
        var lyRt = lyGo.GetComponent<RectTransform>();
        Stretch(lyRt);

        var whParentGo = new GameObject("wh", typeof(RectTransform));
        whParentGo.transform.SetParent(waterParentGo.transform, false);
        var whParentRt = whParentGo.GetComponent<RectTransform>();
        Stretch(whParentRt);

        var visual = waterParentGo.GetComponent<BottleWaterVisual>();
        visual.WaterParent = waterParentRt;
        visual.WhParent = whParentRt;
        visual._layoutRoot = lyRt;
        visual._mask = mask;
        visual._maskImage = maskImg;
        visual._layers = new RectTransform[GameConstants.WaterMaxCount];
        visual._bodyRects = new RectTransform[GameConstants.WaterMaxCount];
        visual._bodies = new Image[GameConstants.WaterMaxCount];
        visual._tops = new Image[GameConstants.WaterMaxCount];
        visual._skews = new UISkewGraphic[GameConstants.WaterMaxCount];
        visual._heights = new float[GameConstants.WaterMaxCount];
        visual._offsetX = new float[GameConstants.WaterMaxCount];
        visual.WhOverlays = new GameObject[GameConstants.WaterMaxCount];

        for (var i = 0; i < GameConstants.WaterMaxCount; i++)
        {
            var defaultH = i == 0 ? GameConstants.GridOneHeight : GameConstants.GridHeight;

            // 水层节点：锚定底部，position.y 为堆叠累计值，height 为该层高度。
            var layerGo = new GameObject("water" + (i + 1), typeof(RectTransform));
            layerGo.transform.SetParent(lyRt, false);
            var layerRt = layerGo.GetComponent<RectTransform>();
            layerRt.anchorMin = layerRt.anchorMax = new Vector2(0.5f, 0f);
            layerRt.pivot = new Vector2(0.5f, 0f);
            layerRt.sizeDelta = new Vector2(WaterWidth, defaultH);
            visual._layers[i] = layerRt;

            // rect 水体：img_7 九宫格(底 24 圆角)，顶对齐该层顶边、向下延伸 (层高+溢出)。
            var bodyGo = new GameObject("rect", typeof(RectTransform), typeof(Image), typeof(UISkewGraphic));
            bodyGo.transform.SetParent(layerGo.transform, false);
            var bodyRt = bodyGo.GetComponent<RectTransform>();
            bodyRt.anchorMin = new Vector2(0, 1);
            bodyRt.anchorMax = new Vector2(1, 1);
            bodyRt.pivot = new Vector2(0.5f, 1f);
            bodyRt.anchoredPosition = Vector2.zero;
            bodyRt.sizeDelta = new Vector2(0, defaultH + Overflow(i));
            var bodyImg = bodyGo.GetComponent<Image>();
            if (bodySprite != null)
            {
                bodyImg.sprite = bodySprite;
                bodyImg.type = Image.Type.Sliced;
            }
            bodyImg.color = Color.white;
            bodyImg.raycastTarget = false;
            visual._bodyRects[i] = bodyRt;
            visual._bodies[i] = bodyImg;
            visual._skews[i] = bodyGo.GetComponent<UISkewGraphic>();

            // top 液面：img_6 椭圆，中心压在该层顶边（Cocos Widget _top=-21.5）。
            if (topSprite != null)
            {
                var topGo = new GameObject("top", typeof(RectTransform), typeof(Image));
                topGo.transform.SetParent(layerGo.transform, false);
                var topRt = topGo.GetComponent<RectTransform>();
                topRt.anchorMin = topRt.anchorMax = new Vector2(0.5f, 1f);
                topRt.pivot = new Vector2(0.5f, 0.5f);
                topRt.sizeDelta = new Vector2(WaterWidth, MeniscusHeight);
                topRt.anchoredPosition = Vector2.zero;
                var topImg = topGo.GetComponent<Image>();
                topImg.sprite = topSprite;
                topImg.color = Color.white;
                topImg.raycastTarget = false;
                visual._tops[i] = topImg;
            }

            var whGo = new GameObject("wh" + (i + 1), typeof(RectTransform), typeof(Image));
            whGo.transform.SetParent(whParentRt, false);
            var whRt = whGo.GetComponent<RectTransform>();
            whRt.anchorMin = whRt.anchorMax = new Vector2(0.5f, 0f);
            whRt.pivot = new Vector2(0.5f, 0.5f);
            whRt.anchoredPosition = new Vector2(0, GameConstants.WaterMaxY[i] + defaultH * 0.5f);
            whRt.sizeDelta = new Vector2(14, 23);
            var whImg = whGo.GetComponent<Image>();
            if (whSprite != null)
            {
                whImg.sprite = whSprite;
                whImg.SetNativeSize();
            }
            else
            {
                whImg.color = Color.white;
                whRt.sizeDelta = new Vector2(28f, 36f);
            }
            whImg.raycastTarget = false;
            whGo.SetActive(false);
            visual.WhOverlays[i] = whGo;
        }

        return visual;
    }

    static void Stretch(RectTransform child)
    {
        child.anchorMin = Vector2.zero;
        child.anchorMax = Vector2.one;
        child.offsetMin = Vector2.zero;
        child.offsetMax = Vector2.zero;
    }

    /// <summary>Cocos updateWaterHeight：按 WaterMaxY 计算各层高度并堆叠。</summary>
    public void ApplyIdleLayout(float totalHeight, int colorCount)
    {
        for (var i = 0; i < GameConstants.WaterMaxCount; i++)
        {
            var maxH = i == 0 ? GameConstants.GridOneHeight : GameConstants.GridHeight;
            var h = Mathf.Clamp(totalHeight - GameConstants.WaterMaxY[i], 0, maxH);
            var active = h > 0.01f && i < colorCount;
            _layers[i].gameObject.SetActive(active);
            _offsetX[i] = 0f;
            _skews[i]?.SetSkewY(0f);
            if (!active)
            {
                _heights[i] = 0f;
                continue;
            }
            SetLayerHeight(i, h);
        }
        StackLayers();
        RefreshTopMeniscus(colorCount);
        SyncWhPositions();
    }

    /// <summary>Cocos showWaterItems：仅按颜色数显隐各层，不刷新液面弧（倒水倾斜时弧面会错位）。</summary>
    public void ShowWaterItems(int colorCount)
    {
        for (var i = 0; i < GameConstants.WaterMaxCount; i++)
            _layers[i].gameObject.SetActive(i < colorCount);
        StackLayers();
    }

    /// <summary>只显示最顶层水的液面弧，其余被上层水体覆盖（与 Cocos 一致）。</summary>
    public void RefreshTopMeniscus(int colorCount)
    {
        if (_tops == null) return;
        var topIndex = -1;
        for (var i = 0; i < GameConstants.WaterMaxCount; i++)
        {
            if (_layers[i] != null && _layers[i].gameObject.activeSelf && _heights[i] > 0.01f)
                topIndex = i;
        }
        for (var i = 0; i < GameConstants.WaterMaxCount; i++)
            if (_tops[i] != null)
                _tops[i].gameObject.SetActive(i == topIndex);
    }

    /// <summary>Cocos rotateTo：改各层 height + skewY + x 偏移，再堆叠。</summary>
    public void ApplyPourRotate(float angleDeg, int visibleCount)
    {
        if (angleDeg < 0.5f)
        {
            foreach (var skew in _skews)
                skew?.SetSkewY(0f);
            return;
        }

        var totalH = 4f * GameConstants.GridHeight + 20f;
        var h3 = totalH / 4f - 3f;
        var h2 = totalH / 3f - 6f;
        var h1 = totalH / 2f - 9f;
        var h0 = totalH / 2f + 60f;

        var lowHeight = angleDeg < 30f
            ? GameConstants.GridHeight + 4f * Mathf.Clamp01(angleDeg / 15f)
            : 0f;

        for (var i = 0; i < GameConstants.WaterMaxCount; i++)
        {
            var active = i < visibleCount;
            if (angleDeg >= GameConstants.BottleAngles[3] && i == 3) active = false;
            if (angleDeg >= GameConstants.BottleAngles[2] && i == 2) active = false;
            if (angleDeg >= GameConstants.BottleAngles[1] && i == 1) active = false;
            _layers[i].gameObject.SetActive(active);
            if (!active)
            {
                _heights[i] = 0f;
                continue;
            }

            _skews[i]?.SetSkewY(Mathf.Clamp(-angleDeg, -86f, 0f));

            float height;
            var offsetX = 0f;
            if (angleDeg <= 30f)
                height = lowHeight;
            else if (angleDeg <= GameConstants.BottleAngles[3])
            {
                var from = GameConstants.BottleAngles[3];
                var to = GameConstants.BottleAngles[4];
                var span = from - to;
                height = i == 3
                    ? h3 * Mathf.Clamp01((from - angleDeg) / span)
                    : Mathf.Lerp(h3, h2, Mathf.Clamp01((angleDeg - to) / span));
            }
            else if (angleDeg <= GameConstants.BottleAngles[2])
            {
                var from = GameConstants.BottleAngles[2];
                var to = GameConstants.BottleAngles[3];
                var span = from - to;
                height = i == 2
                    ? h2 * Mathf.Clamp01((from - angleDeg) / span)
                    : Mathf.Lerp(h2, h1, Mathf.Clamp01((angleDeg - to) / span));
            }
            else if (angleDeg <= GameConstants.BottleAngles[1])
            {
                var from = GameConstants.BottleAngles[1];
                var to = GameConstants.BottleAngles[2];
                var span = from - to;
                height = i == 1
                    ? h1 * Mathf.Clamp01((from - angleDeg) / span)
                    : Mathf.Lerp(h1, h0, Mathf.Clamp01((angleDeg - to) / span));
            }
            else
            {
                var from = GameConstants.BottleAngles[0];
                var to = GameConstants.BottleAngles[1];
                var span = from - to;
                height = h0 * Mathf.Clamp01((from - angleDeg) / span);
                if (angleDeg > 84f)
                    offsetX = (angleDeg - 84f) / 5.5f * -40f;
            }

            SetLayerHeight(i, Mathf.Max(height, 0.01f));
            _offsetX[i] = offsetX;
        }

        StackLayers();
        SetMeniscusVisible(false);
    }

    void SetLayerHeight(int index, float height)
    {
        _heights[index] = height;
        _layers[index].sizeDelta = new Vector2(WaterWidth, height);
        if (_bodyRects[index] != null)
            _bodyRects[index].sizeDelta = new Vector2(0,
                _pourMaskEnabled && index == 0 ? 500f : height + Overflow(index));
    }

    void StackLayers()
    {
        var y = 0f;
        for (var i = 0; i < GameConstants.WaterMaxCount; i++)
        {
            if (_layers[i] == null || !_layers[i].gameObject.activeSelf) continue;
            _layers[i].anchoredPosition = new Vector2(_offsetX[i], y);
            y += _heights[i];
        }
    }

    public void SetMeniscusVisible(bool visible)
    {
        if (_tops == null) return;
        foreach (var top in _tops)
            if (top != null) top.gameObject.SetActive(visible);
    }

    public void SetPourMask(bool enabled)
    {
        _pourMaskEnabled = enabled;
        // 模板遮罩支持旋转：开启时 Image 写模板(不显图)、Mask 裁剪子节点；关闭时都禁用，不裁剪。
        if (_maskImage != null) _maskImage.enabled = enabled;
        if (_mask != null) _mask.enabled = enabled;
        // Cocos：开启遮罩时把底层 rect 拉高到 500，确保倒水时水体填满被裁区域。
        if (_bodyRects != null && _bodyRects.Length > 0 && _bodyRects[0] != null)
            _bodyRects[0].sizeDelta = new Vector2(0, enabled ? 500f : _heights[0] + Overflow(0));
    }

    public void SyncWhPositions()
    {
        if (WhOverlays == null || _layers == null) return;
        var y = 0f;
        for (var i = 0; i < GameConstants.WaterMaxCount; i++)
        {
            if (_layers[i] == null || !_layers[i].gameObject.activeSelf) continue;
            if (WhOverlays[i] != null)
            {
                //var whRt = WhOverlays[i].GetComponent<RectTransform>();
                //whRt.anchoredPosition = new Vector2(0, y + _heights[i] * 0.5f);
                //whRt.SetAsLastSibling();
            }
            y += _heights[i];
        }
    }
}

