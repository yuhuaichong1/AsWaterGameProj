using System.Collections.Generic;
using XrCode;

namespace AsGame.Data
{
    /// <summary>
    /// 锁瓶解锁可行性：彩色锁须在锁外保留足够解锁色；白锁须在锁外保留足够装袋次数。
    /// 刷新水层随机与关卡检查共用。
    /// </summary>
    public static class LockUnlockFeasibility
    {
        public const int LayersPerPack = 4;

        /// <summary>各色在锁瓶内最多可放层数（全关该色总数 − 锁外必须保留层数）。</summary>
        public static Dictionary<int, int> ComputeMaxLayersAllowedInLocks(
            IList<CupData> lockCups, IReadOnlyDictionary<int, int> totalColorCounts)
        {
            var requiredOutside = ComputeRequiredOutsideLayersByColor(lockCups);
            var maxInLocks = new Dictionary<int, int>();
            if (totalColorCounts == null)
                return maxInLocks;

            foreach (var kv in totalColorCounts)
            {
                requiredOutside.TryGetValue(kv.Key, out var required);
                maxInLocks[kv.Key] = kv.Value - required;
                if (maxInLocks[kv.Key] < 0)
                    maxInLocks[kv.Key] = 0;
            }

            return maxInLocks;
        }

        /// <summary>彩色解锁：锁外至少保留 4×max(lockNums) 层该色（同色多锁共享装袋）。</summary>
        public static Dictionary<int, int> ComputeRequiredOutsideLayersByColor(IList<CupData> lockCups)
        {
            var maxPacksByColor = new Dictionary<int, int>();
            if (lockCups == null)
                return new Dictionary<int, int>();

            foreach (var cup in lockCups)
            {
                if (cup == null || cup.lockNums <= 0 || cup.lockColor <= 0)
                    continue;
                maxPacksByColor.TryGetValue(cup.lockColor, out var prev);
                if (cup.lockNums > prev)
                    maxPacksByColor[cup.lockColor] = cup.lockNums;
            }

            var result = new Dictionary<int, int>();
            foreach (var kv in maxPacksByColor)
                result[kv.Key] = kv.Value * LayersPerPack;
            return result;
        }

        /// <summary>白锁：至少需要的装袋次数（同色多锁共享一次装袋）。</summary>
        public static int ComputeRequiredWhiteLockPacks(IList<CupData> lockCups)
        {
            var maxPacks = 0;
            if (lockCups == null)
                return 0;

            foreach (var cup in lockCups)
            {
                if (cup == null || cup.lockNums <= 0 || cup.lockColor != 0)
                    continue;
                if (cup.lockNums > maxPacks)
                    maxPacks = cup.lockNums;
            }

            return maxPacks;
        }

        /// <summary>开局至少需要的有效装袋次数（白锁与各色彩锁取 max 与各色需求之和的较大逻辑）。</summary>
        public static int ComputeMinRequiredPackEvents(IList<CupData> lockCups)
        {
            var coloredDemand = 0;
            var maxPacksByColor = new Dictionary<int, int>();
            if (lockCups != null)
            {
                foreach (var cup in lockCups)
                {
                    if (cup == null || cup.lockNums <= 0 || cup.lockColor <= 0)
                        continue;
                    maxPacksByColor.TryGetValue(cup.lockColor, out var prev);
                    if (cup.lockNums > prev)
                        maxPacksByColor[cup.lockColor] = cup.lockNums;
                }
            }

            foreach (var kv in maxPacksByColor)
                coloredDemand += kv.Value;

            var whiteDemand = ComputeRequiredWhiteLockPacks(lockCups);
            return whiteDemand > coloredDemand ? whiteDemand : coloredDemand;
        }

        /// <summary>根据颜色池校验锁配置是否有可能满足解锁（刷新前快速失败）。</summary>
        public static bool TryValidatePoolAgainstLocks(
            IList<CupData> lockCups,
            IReadOnlyDictionary<int, int> totalColorCounts,
            out string error)
        {
            error = null;
            if (lockCups == null || lockCups.Count == 0 || totalColorCounts == null)
                return true;

            var requiredOutside = ComputeRequiredOutsideLayersByColor(lockCups);
            foreach (var kv in requiredOutside)
            {
                totalColorCounts.TryGetValue(kv.Key, out var total);
                if (total < kv.Value)
                {
                    error =
                        $"解锁色 {kv.Key} 全关仅 {total} 层，锁外至少需保留 {kv.Value} 层（彩色锁 lockNums 需求）";
                    return false;
                }
            }

            var minEvents = ComputeMinRequiredPackEvents(lockCups);
            if (minEvents <= 0)
                return true;

            var packCapacity = 0;
            foreach (var kv in totalColorCounts)
                packCapacity += kv.Value / LayersPerPack;

            if (packCapacity < minEvents)
            {
                error =
                    $"锁瓶需 {minEvents} 次装袋解锁，全关满瓶组仅 {packCapacity} 组，请提高水层总数或减少 lockNums";
                return false;
            }

            return true;
        }

        static void CollectOutsideColorCounts(IList<CupData> cups, out List<CupData> activeLocks, out Dictionary<int, int> outsideCounts)
        {
            activeLocks = new List<CupData>();
            outsideCounts = new Dictionary<int, int>();
            if (cups == null)
                return;

            foreach (var cup in cups)
            {
                if (cup == null) continue;
                var kind = CupSlotKindUtility.GetKind(cup);
                if (kind is CupSlotKind.空槽 or CupSlotKind.广告瓶 or CupSlotKind.空瓶)
                    continue;
                if (kind == CupSlotKind.锁瓶 && cup.lockNums > 0)
                {
                    activeLocks.Add(cup);
                    continue;
                }

                if (cup.colors == null) continue;
                foreach (var c in cup.colors)
                {
                    if (c <= 0) continue;
                    outsideCounts.TryGetValue(c, out var n);
                    outsideCounts[c] = n + 1;
                }
            }
        }

        /// <summary>开局锁瓶未参与时，外部能否满足全部解锁条件。</summary>
        public static bool HasUnlockInfeasibleAtStart(IList<CupData> cups)
        {
            CollectOutsideColorCounts(cups, out var lockCups, out var outsideCounts);
            if (lockCups.Count == 0)
                return false;

            var requiredOutside = ComputeRequiredOutsideLayersByColor(lockCups);
            foreach (var kv in requiredOutside)
            {
                outsideCounts.TryGetValue(kv.Key, out var have);
                if (have < kv.Value)
                    return true;
            }

            var minEvents = ComputeMinRequiredPackEvents(lockCups);
            if (minEvents <= 0)
                return false;

            var packCapacity = 0;
            foreach (var kv in outsideCounts)
                packCapacity += kv.Value / LayersPerPack;

            if (packCapacity < minEvents)
                return true;

            var canFirstPack = false;
            foreach (var kv in outsideCounts)
            {
                if (kv.Value >= LayersPerPack)
                {
                    canFirstPack = true;
                    break;
                }
            }

            return !canFirstPack;
        }
    }
}
