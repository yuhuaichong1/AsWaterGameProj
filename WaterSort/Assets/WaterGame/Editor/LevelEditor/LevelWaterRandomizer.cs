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
            out string error) =>
            TryRefresh(cups, colorCount, totalLayers, difficulty, null, out error);

        /// <param name="lockLayerCounts">与锁瓶一一对应的目标层数；null 则每个锁瓶 4 层。</param>
        public static bool TryRefresh(
            IList<CupData> cups,
            int colorCount,
            int totalLayers,
            WaterRefreshDifficulty difficulty,
            IList<int> lockLayerCounts,
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

            var perLockLayers = ResolveLockLayerCounts(lockCups, lockLayerCounts, out var lockLayersError);
            if (perLockLayers == null)
            {
                error = lockLayersError;
                return false;
            }

            var lockLayers = 0;
            foreach (var n in perLockLayers)
                lockLayers += n;
            if (lockLayers > totalLayers)
            {
                error = $"锁瓶需要 {lockLayers} 层水，超过水层总数 {totalLayers}";
                return false;
            }

            var regularLayers = totalLayers - lockLayers;
            if (regularCups.Count == 0 && regularLayers > 0)
            {
                error = "除锁瓶外还有水层，但没有普通瓶可承载";
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
                if (TryBuildOnce(cups, colorCount, totalLayers, difficulty, lockCups, regularCups, regularLayers,
                        perLockLayers, rng))
                    return true;
            }

            error = "随机失败：请调整水层总数、颜色数或普通瓶/锁瓶数量后重试";
            return false;
        }

        static int[] ResolveLockLayerCounts(
            List<CupData> lockCups,
            IList<int> lockLayerCounts,
            out string error)
        {
            error = null;
            if (lockCups.Count == 0)
                return Array.Empty<int>();

            if (lockLayerCounts == null || lockLayerCounts.Count == 0)
            {
                var full = new int[lockCups.Count];
                for (var i = 0; i < full.Length; i++)
                    full[i] = MaxCapacity;
                return full;
            }

            if (lockLayerCounts.Count != lockCups.Count)
            {
                error = $"锁瓶层数配置数量（{lockLayerCounts.Count}）与锁瓶数（{lockCups.Count}）不一致";
                return null;
            }

            var resolved = new int[lockCups.Count];
            for (var i = 0; i < resolved.Length; i++)
            {
                resolved[i] = lockLayerCounts[i];
                if (resolved[i] < 1 || resolved[i] > MaxCapacity)
                {
                    error = $"锁瓶 #{i} 目标层数 {resolved[i]} 无效（应为 1~{MaxCapacity}）";
                    return null;
                }
            }

            return resolved;
        }

        static bool TryBuildOnce(
            IList<CupData> cups,
            int colorCount,
            int totalLayers,
            WaterRefreshDifficulty difficulty,
            List<CupData> lockCups,
            List<CupData> regularCups,
            int regularLayers,
            int[] lockLayerCounts,
            System.Random rng)
        {
            var pool = BuildColorPool(colorCount);
            Shuffle(pool, rng);

            for (var i = 0; i < lockCups.Count; i++)
            {
                if (!TryFillLockCup(lockCups[i], ref pool, rng, lockLayerCounts[i]))
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

        static bool TryFillLockCup(CupData cup, ref List<int> pool, System.Random rng, int layerCount)
        {
            layerCount = Mathf.Clamp(layerCount, 1, MaxCapacity);
            cup.colors ??= new List<int>();
            cup.colors.Clear();

            if (cup.lockColor > 0)
            {
                if (CountInPool(pool, cup.lockColor) < layerCount)
                    return false;
                RemoveFromPool(pool, cup.lockColor, layerCount);
                for (var i = 0; i < layerCount; i++)
                    cup.colors.Add(cup.lockColor);
                cup.whNums = 0;
                return true;
            }

            if (pool.Count < layerCount) return false;
            for (var layer = 0; layer < layerCount; layer++)
            {
                var idx = rng.Next(pool.Count);
                cup.colors.Add(pool[idx]);
                pool.RemoveAt(idx);
            }

            cup.whNums = 0;
            return cup.colors.Count == layerCount;
        }

        static bool TryFillRegularCups(
            List<CupData> regularCups,
            int regularLayers,
            List<int> pool,
            WaterRefreshDifficulty difficulty,
            System.Random rng)
        {
            var cupCount = regularCups.Count;
            if (regularLayers == 0)
            {
                foreach (var cup in regularCups)
                {
                    cup.colors ??= new List<int>();
                    cup.colors.Clear();
                    cup.whNums = 0;
                }

                return true;
            }

            if (cupCount == 0 || regularLayers > cupCount * MaxCapacity)
                return false;

            var layerCounts = DistributeEvenLayerCounts(regularLayers, cupCount);
            var virtuals = new VirtualBottle[cupCount];
            for (var i = 0; i < cupCount; i++)
                virtuals[i] = new VirtualBottle(layerCounts[i]);

            if (!TryDealColorsEvenly(virtuals, pool, rng))
                return false;

            var steps = GetShuffleSteps(difficulty, rng);
            if (!ScrambleVirtuals(virtuals, steps, difficulty, rng))
                return false;

            for (var i = 0; i < cupCount; i++)
            {
                if (!IsValidRegularBottleState(virtuals[i].Layers))
                    return false;

                var cup = regularCups[i];
                cup.colors ??= new List<int>();
                cup.colors.Clear();
                cup.colors.AddRange(virtuals[i].Layers);
                cup.whNums = PickWhNums(difficulty, cup.colors.Count, rng);
            }

            return true;
        }

        /// <summary>普通瓶水层在参与随机的瓶子间尽量均匀（差值不超过 1）。</summary>
        static int[] DistributeEvenLayerCounts(int totalLayers, int cupCount)
        {
            var result = new int[cupCount];
            if (cupCount <= 0) return result;

            var baseCount = totalLayers / cupCount;
            var remainder = totalLayers % cupCount;
            for (var i = 0; i < cupCount; i++)
                result[i] = baseCount + (i < remainder ? 1 : 0);
            return result;
        }

        /// <summary>按颜色交错 + 轮询分配到各普通瓶，避免同色集中导致满瓶完成态。</summary>
        static bool TryDealColorsEvenly(VirtualBottle[] virtuals, List<int> pool, System.Random rng)
        {
            var stream = BuildInterleavedColorStream(pool, rng);
            if (stream.Count != pool.Count)
                return false;

            var cupCount = virtuals.Length;
            var order = new List<int>(cupCount);
            for (var i = 0; i < cupCount; i++)
                order.Add(i);
            Shuffle(order, rng);

            var filled = new int[cupCount];
            var cursor = 0;
            foreach (var color in stream)
            {
                var placed = false;
                for (var t = 0; t < cupCount; t++)
                {
                    var ci = order[(cursor + t) % cupCount];
                    if (filled[ci] >= virtuals[ci].TargetCount) continue;
                    virtuals[ci].Layers.Add(color);
                    filled[ci]++;
                    cursor = (cursor + t + 1) % cupCount;
                    placed = true;
                    break;
                }

                if (!placed)
                    return false;
            }

            for (var i = 0; i < cupCount; i++)
            {
                if (filled[i] != virtuals[i].TargetCount)
                    return false;
            }

            return true;
        }

        static List<int> BuildInterleavedColorStream(List<int> pool, System.Random rng)
        {
            var buckets = new Dictionary<int, List<int>>();
            foreach (var c in pool)
            {
                if (!buckets.TryGetValue(c, out var list))
                {
                    list = new List<int>();
                    buckets[c] = list;
                }

                list.Add(c);
            }

            var colors = new List<int>(buckets.Keys);
            Shuffle(colors, rng);

            var stream = new List<int>(pool.Count);
            var guard = 0;
            while (stream.Count < pool.Count && guard++ < pool.Count * 8)
            {
                var progressed = false;
                Shuffle(colors, rng);
                foreach (var color in colors)
                {
                    if (buckets[color].Count == 0) continue;
                    stream.Add(buckets[color][0]);
                    buckets[color].RemoveAt(0);
                    progressed = true;
                }

                if (!progressed) break;
            }

            return stream;
        }

        static bool IsValidRegularBottleState(List<int> layers)
        {
            if (layers == null || layers.Count == 0)
                return false;
            if (IsUniformFullBottle(layers))
                return false;
            if (layers.Count == MaxCapacity && CountDistinct(layers) < 2)
                return false;
            return true;
        }

        static bool IsUniformFullBottle(IList<int> layers)
        {
            if (layers == null || layers.Count != MaxCapacity)
                return false;
            var first = layers[0];
            if (first < 1 || first > 8) return false;
            for (var i = 1; i < layers.Count; i++)
                if (layers[i] != first) return false;
            return true;
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
