using UnityEngine;

namespace XrCode
{
    /// <summary>
    /// 问号层：whMask 按位表示 L0..L3 是否隐藏；whNums 保留旧关卡连续隐藏语义（L0 起连续 n 层）。
    /// </summary>
    public static class CupWhLayerUtility
    {
        public const int MaxLayers = 4;

        public static int LayerCountMask(int layerCount) =>
            layerCount <= 0 ? 0 : (1 << Mathf.Min(layerCount, MaxLayers)) - 1;

        public static int GetMask(CupData cup)
        {
            if (cup == null) return 0;
            var layerMask = LayerCountMask(cup.colors?.Count ?? 0);
            if (cup.whMask != 0)
                return cup.whMask & layerMask;
            if (cup.whNums > 0)
            {
                var n = Mathf.Clamp(cup.whNums, 0, MaxLayers);
                return n > 0 ? (1 << n) - 1 : 0;
            }

            return 0;
        }

        public static bool IsLayerHidden(CupData cup, int layer) =>
            cup != null && layer >= 0 && layer < MaxLayers && (GetMask(cup) & (1 << layer)) != 0;

        public static bool HasHiddenLayers(CupData cup) => GetMask(cup) != 0;

        public static int CountHiddenLayers(CupData cup)
        {
            var mask = GetMask(cup);
            var count = 0;
            for (var i = 0; i < MaxLayers; i++)
            {
                if ((mask & (1 << i)) != 0)
                    count++;
            }

            return count;
        }

        public static void SetLayerHidden(CupData cup, int layer, bool hidden)
        {
            if (cup == null || layer < 0 || layer >= MaxLayers) return;

            var mask = GetMask(cup);
            cup.whMask = hidden ? (mask | (1 << layer)) : (mask & ~(1 << layer));
            cup.whMask &= LayerCountMask(cup.colors?.Count ?? 0);
            SyncLegacyWhNums(cup);
        }

        public static void ClearHiddenLayers(CupData cup)
        {
            if (cup == null) return;
            cup.whMask = 0;
            cup.whNums = 0;
        }

        public static void ClampToLayerCount(CupData cup)
        {
            if (cup == null) return;
            cup.whMask = GetMask(cup) & LayerCountMask(cup.colors?.Count ?? 0);
            SyncLegacyWhNums(cup);
        }

        /// <summary>倒出上层后，若新的顶层仍为问号层则揭开颜色。</summary>
        public static void RevealHiddenLayerUncoveredByPour(CupData cup)
        {
            if (cup == null || cup.colors == null || cup.colors.Count == 0) return;
            var top = cup.colors.Count - 1;
            if (!IsLayerHidden(cup, top)) return;
            SetLayerHidden(cup, top, false);
        }

        /// <summary>连续 L0..n-1 时同步 whNums；非连续时 whNums=0，以 whMask 为准。</summary>
        public static void SyncLegacyWhNums(CupData cup)
        {
            if (cup == null) return;
            var mask = cup.whMask;
            if (mask == 0)
            {
                cup.whNums = 0;
                return;
            }

            var n = 0;
            for (var i = 0; i < MaxLayers; i++)
            {
                if ((mask & (1 << i)) == 0) break;
                n++;
            }

            cup.whNums = mask == ((1 << n) - 1) ? n : 0;
        }

        /// <summary>保存关卡前：将有效掩码写入 whMask，并同步兼容字段 whNums。</summary>
        public static void NormalizeForSave(CupData cup)
        {
            if (cup == null) return;
            cup.whMask = GetMask(cup);
            cup.whMask &= LayerCountMask(cup.colors?.Count ?? 0);
            SyncLegacyWhNums(cup);
        }
    }
}
