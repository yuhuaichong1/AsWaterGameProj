using System;
using System.Collections.Generic;
using AsGame.Core;
using AsGame.Data;
using UnityEngine;

namespace AsGame.Editor.LevelEditor
{
    public enum WaterRefreshDifficulty
    {
        超简单 = 0,
        简单 = 1,
        中等 = 2,
        困难 = 3,
        超困难 = 4
    }

    public static class LevelWaterAnalyzer
    {
        public static bool ParticipatesInRefresh(CupData cup) =>
            CupSlotKindUtility.ParticipatesInWaterRefresh(cup);

        public static bool IsLockCup(CupData cup) =>
            cup != null && ParticipatesInRefresh(cup) && cup.isLock != 0;

        public static int CountParticipating(IList<CupData> cups)
        {
            var n = 0;
            if (cups == null) return 0;
            foreach (var c in cups)
                if (ParticipatesInRefresh(c)) n++;
            return n;
        }

        public static int CountLockCups(IList<CupData> cups)
        {
            var n = 0;
            if (cups == null) return 0;
            foreach (var c in cups)
                if (IsLockCup(c)) n++;
            return n;
        }

        public static int SumParticipatingLayers(IList<CupData> cups)
        {
            var sum = 0;
            if (cups == null) return 0;
            foreach (var c in cups)
                if (ParticipatesInRefresh(c))
                    sum += c.colors?.Count ?? 0;
            return sum;
        }
    }

    public static class LevelWaterRandomizer
    {
        const int MaxCapacity = GameConstants.WaterMaxCount;
        const int MaxAttempts = 128;

        public static string GetDifficultyDisplayName(WaterRefreshDifficulty d) => d switch
        {
            WaterRefreshDifficulty.超简单 => "超简单",
            WaterRefreshDifficulty.简单 => "简单",
            WaterRefreshDifficulty.中等 => "中等",
            WaterRefreshDifficulty.困难 => "困难",
            WaterRefreshDifficulty.超困难 => "超困难",
            _ => d.ToString()
        };

        /// <summary>根据普通瓶数量、颜色种数、水层总数给出推荐难度。</summary>
        public static WaterRefreshDifficulty RecommendDifficulty(
            int normalCupCount, int colorCount, int totalLayers, int lockCupCount = 0)
        {
            if (normalCupCount <= 0 || totalLayers <= 0)
                return WaterRefreshDifficulty.超简单;

            var regular = Mathf.Max(1, normalCupCount - lockCupCount);
            var density = totalLayers / (float)(normalCupCount * MaxCapacity);
            var colorPressure = colorCount / (float)regular;

            if (colorCount <= 3 && totalLayers <= 16 && density <= 0.55f)
                return WaterRefreshDifficulty.超简单;
            if (colorCount <= 4 && density <= 0.65f && colorPressure <= 0.9f)
                return WaterRefreshDifficulty.简单;
            if (colorCount <= 6 && density <= 0.8f)
                return WaterRefreshDifficulty.中等;
            if (colorCount <= 7 && density <= 0.92f)
                return WaterRefreshDifficulty.困难;
            return WaterRefreshDifficulty.超困难;
        }

        public static bool TryRefresh(
            IList<CupData> cups,
            int colorCount,
            int totalLayers,
            WaterRefreshDifficulty difficulty,
            out string error)
        {
            error = null;
            if (cups == null || cups.Count == 0)
            {
                error = "当前关卡没有瓶子数据";
                return false;
            }

            if (totalLayers <= 0 || totalLayers % 4 != 0)
            {
                error = "水层总数必须为大于 0 的 4 的倍数";
                return false;
            }

            colorCount = Mathf.Clamp(colorCount, 1, 8);
            if (colorCount * 4 != totalLayers)
            {
                error = $"颜色种类×4 应等于水层总数（当前 {colorCount}×4≠{totalLayers}）";
                return false;
            }

            var participating = new List<CupData>();
            var lockCups = new List<CupData>();
            var regularCups = new List<CupData>();
            foreach (var c in cups)
            {
                if (!LevelWaterAnalyzer.ParticipatesInRefresh(c)) continue;
                participating.Add(c);
                if (LevelWaterAnalyzer.IsLockCup(c))
                    lockCups.Add(c);
                else
                    regularCups.Add(c);
            }

            if (participating.Count == 0)
            {
                error = "没有可参与随机的瓶子（普通瓶/锁瓶；空槽、广告瓶、空瓶不参与）";
                return false;
            }

            var lockLayers = lockCups.Count * MaxCapacity;
            if (lockLayers > totalLayers)
            {
                error = $"锁瓶需要 {lockLayers} 层水，超过水层总数 {totalLayers}";
                return false;
            }

            var regularLayers = totalLayers - lockLayers;
            if (regularCups.Count == 0 && regularLayers > 0)
            {
                error = "仅有锁瓶时，水层总数应等于锁瓶数×4";
                return false;
            }

            if (regularCups.Count > 0)
            {
                var regularCapacity = regularCups.Count * MaxCapacity;
                if (regularLayers > regularCapacity)
                {
                    error =
                        $"普通瓶最多容纳 {regularCapacity} 层水（{regularCups.Count} 个普通瓶×4），" +
                        $"当前需分配 {regularLayers} 层（水层总数 {totalLayers}，锁瓶占 {lockLayers} 层）。" +
                        "请减少水层总数、增加普通瓶，或减少锁瓶/空瓶。";
                    return false;
                }

                var minRegularCups = Mathf.CeilToInt(regularLayers / (float)MaxCapacity);
                if (regularCups.Count < minRegularCups)
                {
                    error =
                        $"至少需要 {minRegularCups} 个普通瓶才能放下 {regularLayers} 层水，当前只有 {regularCups.Count} 个。" +
                        "请增加普通瓶、减少空瓶/锁瓶占用，或降低水层总数。";
                    return false;
                }
            }

            var rng = new System.Random();
            for (var attempt = 0; attempt < MaxAttempts; attempt++)
            {
                if (TryBuildOnce(cups, colorCount, totalLayers, difficulty, lockCups, regularCups, regularLayers, rng))
                    return true;
            }

            error = "随机失败：请调整水层总数、颜色数或普通瓶/锁瓶数量后重试";
            return false;
        }

        static bool TryBuildOnce(
            IList<CupData> cups,
            int colorCount,
            int totalLayers,
            WaterRefreshDifficulty difficulty,
            List<CupData> lockCups,
            List<CupData> regularCups,
            int regularLayers,
            System.Random rng)
        {
            var pool = BuildColorPool(colorCount);
            Shuffle(pool, rng);

            foreach (var cup in lockCups)
            {
                if (!TryFillLockCup(cup, ref pool, rng))
                    return false;
            }

            if (regularCups.Count == 0)
                return pool.Count == 0;

            // 普通瓶分配只从 pool 读取，不会 Remove；锁瓶才会清空 pool
            return TryFillRegularCups(regularCups, regularLayers, pool, difficulty, rng);
        }

        static List<int> BuildColorPool(int colorCount)
        {
            var pool = new List<int>(colorCount * 4);
            for (var c = 1; c <= colorCount; c++)
                for (var i = 0; i < 4; i++)
                    pool.Add(c);
            return pool;
        }

        static bool TryFillLockCup(CupData cup, ref List<int> pool, System.Random rng)
        {
            cup.colors ??= new List<int>();
            cup.colors.Clear();

            if (cup.lockColor > 0)
            {
                if (CountInPool(pool, cup.lockColor) < 4)
                    return false;
                RemoveFromPool(pool, cup.lockColor, 4);
                for (var i = 0; i < 4; i++)
                    cup.colors.Add(cup.lockColor);
                cup.whNums = 0;
                return true;
            }

            if (pool.Count < 4) return false;
            for (var layer = 0; layer < 4; layer++)
            {
                var idx = rng.Next(pool.Count);
                cup.colors.Add(pool[idx]);
                pool.RemoveAt(idx);
            }

            cup.whNums = 0;
            return cup.colors.Count == 4;
        }

        static bool TryFillRegularCups(
            List<CupData> regularCups,
            int regularLayers,
            List<int> pool,
            WaterRefreshDifficulty difficulty,
            System.Random rng)
        {
            var cupCount = regularCups.Count;
            var minFillCups = Mathf.Max(1, Mathf.CeilToInt(regularLayers / (float)MaxCapacity));
            var emptyTarget = GetEmptyCupTarget(cupCount, regularLayers, difficulty, rng);
            emptyTarget = Mathf.Min(emptyTarget, Mathf.Max(0, cupCount - minFillCups));
            var fillCupCount = cupCount - emptyTarget;
            if (fillCupCount < 0) fillCupCount = 0;
            if (regularLayers > fillCupCount * MaxCapacity)
                return false;
            if (regularLayers > 0 && fillCupCount > 0 && regularLayers < fillCupCount)
                return false;
            if (regularLayers == 0 && fillCupCount > 0)
                fillCupCount = 0;

            var layerCounts = new int[cupCount];
            for (var i = 0; i < cupCount; i++)
                layerCounts[i] = 0;

            var fillIndices = new List<int>();
            for (var i = 0; i < cupCount; i++)
                fillIndices.Add(i);
            Shuffle(fillIndices, rng);
            fillIndices = fillIndices.GetRange(0, Mathf.Max(0, fillCupCount));

            if (!DistributeLayerCounts(layerCounts, fillIndices, regularLayers, difficulty, rng))
                return false;

            var virtuals = new VirtualBottle[cupCount];
            for (var i = 0; i < cupCount; i++)
                virtuals[i] = new VirtualBottle(layerCounts[i]);

            if (!DealTokensToVirtuals(virtuals, pool, rng))
                return false;

            var steps = GetShuffleSteps(difficulty, rng);
            if (!ScrambleVirtuals(virtuals, steps, difficulty, rng))
                return false;

            for (var i = 0; i < cupCount; i++)
            {
                var cup = regularCups[i];
                cup.colors ??= new List<int>();
                cup.colors.Clear();
                cup.colors.AddRange(virtuals[i].Layers);
                cup.whNums = PickWhNums(difficulty, cup.colors.Count, rng);
            }

            return true;
        }

        static int GetEmptyCupTarget(int cupCount, int totalLayers, WaterRefreshDifficulty difficulty, System.Random rng)
        {
            if (cupCount <= 1) return 0;

            var minFill = Mathf.Max(1, Mathf.CeilToInt(totalLayers / (float)MaxCapacity));
            var maxEmpty = Mathf.Max(0, cupCount - minFill);

            if (maxEmpty == 0)
                return 0;

            var ratio = difficulty switch
            {
                WaterRefreshDifficulty.超简单 => 0.42f,
                WaterRefreshDifficulty.简单 => 0.32f,
                WaterRefreshDifficulty.中等 => 0.22f,
                WaterRefreshDifficulty.困难 => 0.12f,
                _ => 0.06f
            };
            var target = Mathf.RoundToInt(cupCount * ratio);
            target = Mathf.Clamp(target, 0, maxEmpty);
            if (target == 0 && maxEmpty > 0 && difficulty <= WaterRefreshDifficulty.简单)
                target = 1;
            if (difficulty == WaterRefreshDifficulty.超困难 && cupCount > 2)
                target = Mathf.Min(target, Mathf.Max(0, cupCount / 6));
            return target;
        }

        static bool DistributeLayerCounts(
            int[] layerCounts,
            List<int> fillIndices,
            int totalLayers,
            WaterRefreshDifficulty difficulty,
            System.Random rng)
        {
            if (totalLayers == 0)
                return true;
            var n = fillIndices.Count;
            if (n == 0)
                return false;

            var minTotal = n;
            var maxTotal = n * MaxCapacity;
            if (totalLayers < minTotal || totalLayers > maxTotal)
                return false;

            foreach (var cup in fillIndices)
                layerCounts[cup] = 1;

            var remaining = totalLayers - n;
            var preferHigh = difficulty >= WaterRefreshDifficulty.中等;
            var guard = 0;
            while (remaining > 0 && guard++ < 512)
            {
                var cup = fillIndices[rng.Next(n)];
                if (layerCounts[cup] >= MaxCapacity) continue;

                var add = 1;
                if (preferHigh && layerCounts[cup] < MaxCapacity - 1 && rng.NextDouble() < 0.35f)
                    add = Mathf.Min(remaining, 2, MaxCapacity - layerCounts[cup]);
                layerCounts[cup] += add;
                remaining -= add;
            }

            return remaining == 0;
        }

        static bool DealTokensToVirtuals(VirtualBottle[] virtuals, List<int> pool, System.Random rng)
        {
            Shuffle(pool, rng);
            var idx = 0;
            foreach (var v in virtuals)
            {
                for (var i = 0; i < v.TargetCount; i++)
                {
                    if (idx >= pool.Count) return false;
                    v.Layers.Add(pool[idx++]);
                }
            }

            return idx == pool.Count;
        }

        static bool ScrambleVirtuals(
            VirtualBottle[] virtuals,
            int steps,
            WaterRefreshDifficulty difficulty,
            System.Random rng)
        {
            var maxColorsPerBottle = difficulty switch
            {
                WaterRefreshDifficulty.超简单 => 2,
                WaterRefreshDifficulty.简单 => 3,
                WaterRefreshDifficulty.中等 => 4,
                _ => MaxCapacity
            };

            for (var s = 0; s < steps; s++)
            {
                var from = rng.Next(virtuals.Length);
                var to = rng.Next(virtuals.Length);
                if (from == to) continue;
                TryPour(virtuals[from], virtuals[to]);
            }

            if (difficulty <= WaterRefreshDifficulty.困难)
                return true;

            foreach (var v in virtuals)
            {
                if (v.Layers.Count == 0) continue;
                if (CountDistinct(v.Layers) <= maxColorsPerBottle) continue;
                TryBreakupBottle(v, maxColorsPerBottle, virtuals, rng);
            }

            return true;
        }

        static bool TryBreakupBottle(VirtualBottle target, int maxColors, VirtualBottle[] all, System.Random rng)
        {
            for (var attempt = 0; attempt < 12; attempt++)
            {
                var other = all[rng.Next(all.Length)];
                if (other == target || other.Layers.Count >= MaxCapacity) continue;
                if (TryPour(target, other) && CountDistinct(target.Layers) <= maxColors)
                    return true;
            }

            return CountDistinct(target.Layers) <= maxColors;
        }

        static int GetShuffleSteps(WaterRefreshDifficulty difficulty, System.Random rng)
        {
            var (min, max) = difficulty switch
            {
                WaterRefreshDifficulty.超简单 => (5, 16),
                WaterRefreshDifficulty.简单 => (10, 31),
                WaterRefreshDifficulty.中等 => (20, 51),
                WaterRefreshDifficulty.困难 => (40, 71),
                _ => (70, 151)
            };
            return rng.Next(min, max);
        }

        static int PickWhNums(WaterRefreshDifficulty difficulty, int layerCount, System.Random rng)
        {
            if (layerCount <= 1) return 0;
            return difficulty switch
            {
                WaterRefreshDifficulty.超简单 or WaterRefreshDifficulty.简单 => 0,
                WaterRefreshDifficulty.中等 => rng.NextDouble() < 0.2 ? rng.Next(1, Mathf.Min(2, layerCount) + 1) : 0,
                WaterRefreshDifficulty.困难 => rng.NextDouble() < 0.4 ? rng.Next(1, Mathf.Min(3, layerCount) + 1) : 0,
                _ => rng.NextDouble() < 0.5 ? rng.Next(1, Mathf.Min(3, layerCount) + 1) : 0
            };
        }

        static bool TryPour(VirtualBottle from, VirtualBottle to)
        {
            if (from.Layers.Count == 0 || to.Layers.Count >= MaxCapacity)
                return false;
            var color = from.Layers[^1];
            if (to.Layers.Count > 0 && to.Layers[^1] != color)
                return false;

            var run = 1;
            for (var i = from.Layers.Count - 2; i >= 0; i--)
            {
                if (from.Layers[i] != color) break;
                run++;
            }

            var space = MaxCapacity - to.Layers.Count;
            var move = Mathf.Min(run, space);
            if (move <= 0) return false;

            for (var m = 0; m < move; m++)
            {
                to.Layers.Add(from.Layers[^1]);
                from.Layers.RemoveAt(from.Layers.Count - 1);
            }

            return true;
        }

        static int CountDistinct(List<int> layers)
        {
            var set = new HashSet<int>();
            foreach (var c in layers) set.Add(c);
            return set.Count;
        }

        static int CountInPool(List<int> pool, int color)
        {
            var n = 0;
            foreach (var c in pool)
                if (c == color) n++;
            return n;
        }

        static void RemoveFromPool(List<int> pool, int color, int count)
        {
            for (var removed = 0; removed < count;)
            {
                for (var i = 0; i < pool.Count; i++)
                {
                    if (pool[i] != color) continue;
                    pool.RemoveAt(i);
                    removed++;
                    break;
                }
            }
        }

        static bool TryTakeOne(List<int> pool, int color, out int taken)
        {
            taken = color;
            for (var i = 0; i < pool.Count; i++)
            {
                if (pool[i] != color) continue;
                taken = pool[i];
                pool.RemoveAt(i);
                return true;
            }

            return false;
        }

        static void Shuffle<T>(IList<T> list, System.Random rng)
        {
            for (var i = list.Count - 1; i > 0; i--)
            {
                var j = rng.Next(i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }

        sealed class VirtualBottle
        {
            public readonly int TargetCount;
            public readonly List<int> Layers = new();

            public VirtualBottle(int targetCount) => TargetCount = targetCount;
        }
    }
}
