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

        public static int CountConfiguredHiddenLayers(CupData cup)
        {
            if (cup == null)
                return 0;

            var mask = GetMask(cup);
            var layers = cup.colors?.Count ?? 0;
            var count = 0;
            for (var i = 0; i < layers && i < MaxLayers; i++)
            {
                if ((mask & (1 << i)) != 0)
                    count++;
            }

            return count;
        }

        /// <summary>该层在关卡数据中是否勾选为问号（不考虑局内遮挡）。</summary>
        public static bool IsConfiguredHidden(CupData cup, int layer) =>
            cup != null && layer >= 0 && layer < MaxLayers && (GetMask(cup) & (1 << layer)) != 0;

        public static bool HasHiddenLayers(CupData cup)
        {
            var count = cup.colors?.Count ?? 0;
            for (var i = 0; i < count; i++)
            {
                if (IsLayerHidden(cup, i))
                    return true;
            }

            return false;
        }

        public static int CountHiddenLayers(CupData cup)
        {
            var count = 0;
            var layers = cup.colors?.Count ?? 0;
            for (var i = 0; i < layers; i++)
            {
                if (IsLayerHidden(cup, i))
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

        /// <summary>顶层水倒出后：裁剪无效位并揭开新顶层（上方已无水遮挡）。</summary>
        public static void OnRemovedTopLayers(CupData cup)
        {
            if (cup == null) return;
            var count = cup.colors?.Count ?? 0;
            cup.whMask = NormalizeMaskAfterTopRemoved(GetMask(cup), count);
            SyncLegacyWhNums(cup);
        }

        /// <summary>按当前层数裁剪掩码并揭开顶层，供运行时与求解器共用。</summary>
        public static int NormalizeMaskAfterTopRemoved(int mask, int layerCount)
        {
            mask &= LayerCountMask(layerCount);
            if (layerCount > 0)
                mask &= ~(1 << (layerCount - 1));
            return mask;
        }

        /// <summary>层是否仍显示为问号：掩码标记且上方还有水层遮挡。</summary>
        public static bool IsLayerHidden(CupData cup, int layer)
        {
            if (cup == null || layer < 0 || layer >= MaxLayers)
                return false;
            if ((GetMask(cup) & (1 << layer)) == 0)
                return false;

            var count = cup.colors?.Count ?? 0;
            return layer < count - 1;
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
