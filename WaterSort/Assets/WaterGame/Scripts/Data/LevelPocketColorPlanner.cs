using System.Collections.Generic;
using UnityEngine;

namespace AsGame.Data
{
    /// <summary>
    /// 生成长桌口袋目标颜色。初始开放口袋会优先包含“锁瓶外已凑够 4 层”的颜色，避免开局目标色随机成死局。
    /// </summary>
    public static class LevelPocketColorPlanner
    {
        const int InitialOpenPocketCount = 2;

        public static void BuildPocketColors(
            IList<CupData> cups,
            int levelIndex,
            List<int> output,
            bool useUnityRandom)
        {
            output.Clear();
            if (cups == null)
                return;

            var colorCount = new Dictionary<int, int>();
            foreach (var cup in cups)
            {
                if (cup == null || cup.isNull != 0 || cup.isVideo != 0 || cup.colors == null)
                    continue;

                foreach (var c in cup.colors)
                {
                    if (c <= 0) continue;
                    if (!colorCount.ContainsKey(c)) colorCount[c] = 0;
                    colorCount[c]++;
                    if (colorCount[c] % 4 == 0)
                    {
                        output.Add(c);
                        colorCount[c] = 0;
                    }
                }
            }

            if (levelIndex != 1)
            {
                if (useUnityRandom)
                    ShuffleWithUnityRandom(output);
                else
                    Shuffle(output, levelIndex * 7919 + 17);
            }

            EnsureInitialOpenPocketHasOutsideCompletableColor(cups, output);
        }

        public static List<int> GetOutsideCompletableColors(IList<CupData> cups)
        {
            var outsideCounts = new Dictionary<int, int>();
            var result = new List<int>();
            if (cups == null)
                return result;

            foreach (var cup in cups)
            {
                if (cup == null || cup.isNull != 0 || cup.isVideo != 0 || cup.isLock != 0 || cup.colors == null)
                    continue;

                foreach (var color in cup.colors)
                {
                    if (color <= 0) continue;
                    if (!outsideCounts.ContainsKey(color)) outsideCounts[color] = 0;
                    outsideCounts[color]++;
                }
            }

            foreach (var kv in outsideCounts)
            {
                if (kv.Value >= 4)
                    result.Add(kv.Key);
            }

            result.Sort();
            return result;
        }

        public static bool HasOutsideCompletableColor(IList<CupData> cups) =>
            GetOutsideCompletableColors(cups).Count > 0;

        static void EnsureInitialOpenPocketHasOutsideCompletableColor(
            IList<CupData> cups,
            List<int> pocketColors)
        {
            if (pocketColors == null || pocketColors.Count == 0)
                return;

            var outsideCompletable = GetOutsideCompletableColors(cups);
            if (outsideCompletable.Count == 0)
                return;

            var preferredColors = BuildPreferredInitialColors(cups, outsideCompletable);
            var openCount = Mathf.Min(InitialOpenPocketCount, pocketColors.Count);
            for (var i = 0; i < openCount; i++)
            {
                if (preferredColors.Contains(pocketColors[i]))
                    return;
            }

            for (var i = openCount; i < pocketColors.Count; i++)
            {
                if (!preferredColors.Contains(pocketColors[i]))
                    continue;

                (pocketColors[0], pocketColors[i]) = (pocketColors[i], pocketColors[0]);
                return;
            }
        }

        static List<int> BuildPreferredInitialColors(
            IList<CupData> cups,
            List<int> outsideCompletable)
        {
            var preferred = new List<int>();
            if (cups != null)
            {
                foreach (var cup in cups)
                {
                    if (cup == null || cup.isLock == 0 || cup.lockNums <= 0 || cup.lockColor <= 0)
                        continue;
                    if (outsideCompletable.Contains(cup.lockColor) && !preferred.Contains(cup.lockColor))
                        preferred.Add(cup.lockColor);
                }
            }

            return preferred.Count > 0 ? preferred : outsideCompletable;
        }

        static void ShuffleWithUnityRandom<T>(IList<T> list)
        {
            for (var i = list.Count - 1; i > 0; i--)
            {
                var j = Random.Range(0, i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }

        static void Shuffle<T>(IList<T> list, int seed)
        {
            var rng = new System.Random(seed);
            for (var i = list.Count - 1; i > 0; i--)
            {
                var j = rng.Next(i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }
    }
}
