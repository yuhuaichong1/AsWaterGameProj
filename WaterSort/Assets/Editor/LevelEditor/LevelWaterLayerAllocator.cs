using System.Collections.Generic;
using System.Text;
using AsGame.Core;
using AsGame.Data;
using UnityEngine;
using XrCode;

namespace AsGame.Editor.LevelEditor
{
    /// <summary>刷新水层前：优先沿用各瓶已配置层数，再按水层总数补删至合法。</summary>
    public static class LevelWaterLayerAllocator
    {
        const int MaxCapacity = GameConstants.WaterMaxCount;

        sealed class Slot
        {
            public int CupIndex;
            public CupData Cup;
            public bool IsLock;
            public int Target;
            public bool WasConfigured;
            /// <summary>层数被关卡策略锁定（如前 10 关锁瓶层数），不参与补删调整。</summary>
            public bool IsFixed;
        }

        public static bool TryResolve(
            IList<CupData> cups,
            int totalLayers,
            int levelIndex,
            out int[] lockLayerTargets,
            out List<CupData> lockCups,
            out int[] regularLayerTargets,
            out List<CupData> regularCups,
            out List<string> adjustLog,
            out string error)
        {
            lockLayerTargets = null;
            regularLayerTargets = null;
            lockCups = new List<CupData>();
            regularCups = new List<CupData>();
            adjustLog = new List<string>();
            error = null;

            if (cups == null || cups.Count == 0)
            {
                error = "没有瓶子数据";
                return false;
            }

            var slots = new List<Slot>();
            for (var i = 0; i < cups.Count; i++)
            {
                var cup = cups[i];
                if (!LevelWaterAnalyzer.ParticipatesInRefresh(cup))
                    continue;

                var isLock = LevelWaterAnalyzer.IsLockCup(cup);
                var configured = cup.colors?.Count ?? 0;
                bool wasConfigured;
                bool isFixed;
                int target;
                if (isLock)
                {
                    // 锁瓶层数必须与检查器一致：有强制要求时锁定为该值，否则按策略解析。
                    var mandatory = LevelLockLayerPolicy.GetMandatoryLockLayers(levelIndex);
                    target = mandatory > 0
                        ? mandatory
                        : LevelLockLayerPolicy.ResolveRefreshLockLayers(levelIndex, cup);
                    isFixed = mandatory > 0;
                    wasConfigured = true;
                }
                else
                {
                    wasConfigured = configured > 0;
                    target = wasConfigured ? configured : 0;
                    isFixed = false;
                }

                if (target > MaxCapacity)
                {
                    error = $"#{i} {(isLock ? "锁瓶" : "普通瓶")} 配置 {target} 层，超过上限 {MaxCapacity}";
                    return false;
                }

                if (isLock)
                    lockCups.Add(cup);
                else
                    regularCups.Add(cup);

                slots.Add(new Slot
                {
                    CupIndex = i,
                    Cup = cup,
                    IsLock = isLock,
                    Target = target,
                    WasConfigured = wasConfigured,
                    IsFixed = isFixed
                });
            }

            if (slots.Count == 0)
            {
                error = "没有可参与随机的瓶子";
                return false;
            }

            var configuredSum = 0;
            foreach (var s in slots)
                configuredSum += s.Target;

            if (configuredSum != totalLayers)
            {
                adjustLog.Add(
                    $"参与瓶已配置水层合计 {configuredSum} 层，与刷新目标 {totalLayers} 层不一致，开始调整。");
                if (!AdjustToTotal(slots, totalLayers, levelIndex, adjustLog, out error))
                    return false;
            }

            if (!BalanceRegularNonEmpty(slots, adjustLog, out error))
                return false;

            lockLayerTargets = new int[lockCups.Count];
            regularLayerTargets = new int[regularCups.Count];
            var lockIdx = 0;
            var regularIdx = 0;
            foreach (var s in slots)
            {
                if (s.IsLock)
                    lockLayerTargets[lockIdx++] = s.Target;
                else
                    regularLayerTargets[regularIdx++] = s.Target;
            }

            return true;
        }

        static bool AdjustToTotal(
            List<Slot> slots,
            int totalLayers,
            int levelIndex,
            List<string> adjustLog,
            out string error)
        {
            error = null;
            var sum = SumTargets(slots);
            var delta = totalLayers - sum;

            if (delta > 0)
            {
                while (delta > 0)
                {
                    var slot = PickForIncrease(slots);
                    if (slot == null)
                    {
                        error = $"无法继续补充：已加满至 {SumTargets(slots)} 层，仍少于目标 {totalLayers} 层";
                        return false;
                    }

                    var old = slot.Target;
                    slot.Target++;
                    delta--;
                    adjustLog.Add(FormatLine(slot, old, slot.Target, "补充"));
                }
            }
            else if (delta < 0)
            {
                delta = -delta;
                while (delta > 0)
                {
                    var slot = PickForDecrease(slots, levelIndex);
                    if (slot == null)
                    {
                        error = $"无法继续删减：已减至 {SumTargets(slots)} 层，仍多于目标 {totalLayers} 层";
                        return false;
                    }

                    var old = slot.Target;
                    slot.Target--;
                    delta--;
                    adjustLog.Add(FormatLine(slot, old, slot.Target, "删减"));
                }
            }

            return true;
        }

        static bool BalanceRegularNonEmpty(List<Slot> slots, List<string> adjustLog, out string error)
        {
            error = null;
            var regularSum = 0;
            foreach (var s in slots)
            {
                if (!s.IsLock)
                    regularSum += s.Target;
            }

            if (regularSum <= 0)
                return true;

            while (true)
            {
                Slot empty = null;
                foreach (var s in slots)
                {
                    if (!s.IsLock && s.Target == 0)
                    {
                        empty = s;
                        break;
                    }
                }

                if (empty == null)
                    return true;

                Slot donor = null;
                var donorScore = int.MinValue;
                foreach (var s in slots)
                {
                    if (s.IsLock || s.Target <= 1)
                        continue;
                    var score = s.Target * 10 + (s.WasConfigured ? 0 : 5);
                    if (score <= donorScore)
                        continue;
                    donorScore = score;
                    donor = s;
                }

                if (donor == null)
                {
                    error = "普通瓶无法同时满足「均有水层」与当前层数分配";
                    return false;
                }

                var donorOld = donor.Target;
                donor.Target--;
                adjustLog.Add(FormatLine(donor, donorOld, donor.Target, "删减（平衡普通瓶水层）"));

                var emptyOld = empty.Target;
                empty.Target++;
                adjustLog.Add(FormatLine(empty, emptyOld, empty.Target, "补充（平衡普通瓶水层）"));
            }
        }

        static Slot PickForIncrease(List<Slot> slots)
        {
            Slot best = null;
            var bestScore = int.MaxValue;
            foreach (var s in slots)
            {
                if (s.IsFixed || s.Target >= MaxCapacity)
                    continue;

                var score = ScoreForIncrease(s);
                if (score >= bestScore)
                    continue;
                bestScore = score;
                best = s;
            }

            return best;
        }

        static int ScoreForIncrease(Slot s)
        {
            // 未配置的普通瓶优先接收补充，其次层数少的瓶
            if (!s.IsLock && !s.WasConfigured)
                return s.Target;
            if (!s.WasConfigured)
                return 100 + s.Target;
            return 200 + s.Target;
        }

        static Slot PickForDecrease(List<Slot> slots, int levelIndex)
        {
            Slot best = null;
            var bestScore = int.MinValue;
            foreach (var s in slots)
            {
                if (s.IsFixed)
                    continue;

                var min = MinTarget(s, levelIndex);
                if (s.Target <= min)
                    continue;

                var score = ScoreForDecrease(s);
                if (score <= bestScore)
                    continue;
                bestScore = score;
                best = s;
            }

            return best;
        }

        static int ScoreForDecrease(Slot s)
        {
            // 未配置或后补的层优先删减：层数多的先减；同层数时未配置优先
            var score = s.Target * 10;
            if (!s.WasConfigured)
                score += 5;
            return score;
        }

        static int MinTarget(Slot s, int levelIndex)
        {
            if (!s.IsLock)
                return 0;

            var mandatory = LevelLockLayerPolicy.GetMandatoryLockLayers(levelIndex);
            return mandatory > 0 ? mandatory : 1;
        }

        static int SumTargets(List<Slot> slots)
        {
            var sum = 0;
            foreach (var s in slots)
                sum += s.Target;
            return sum;
        }

        static string FormatLine(Slot s, int oldCount, int newCount, string action)
        {
            var kind = s.IsLock ? "锁瓶" : "普通瓶";
            return $"{action}：#{s.CupIndex} {kind} {oldCount} → {newCount} 层";
        }

        public static string FormatAdjustLog(IReadOnlyList<string> lines)
        {
            if (lines == null || lines.Count == 0)
                return string.Empty;

            var sb = new StringBuilder();
            sb.AppendLine("【层数调整】");
            foreach (var line in lines)
                sb.AppendLine("· " + line);
            return sb.ToString().TrimEnd();
        }
    }
}
