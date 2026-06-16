using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using AsGame.Core;
using AsGame.Data;
using UnityEditor;
using UnityEngine;
using XrCode;

namespace AsGame.Editor.LevelEditor
{
    /// <summary>按难度曲线批量生成拆关 JSON（1~161 关）。</summary>
    public static class LevelBatchGenerator
    {
        public const int MaxLevelCount = 161;
        const int MaxAttemptsPerLevel = 512;

        /// <summary>主网格：避开顶部口袋与底部道具栏（CupMgr 坐标，y 为瓶底）。</summary>
        static readonly Vector2[] MainGridSlots =
        {
            new(0f, 40f),
            new(-110f, 40f), new(110f, 40f),
            new(-220f, -50f), new(220f, -50f),
            new(0f, -140f),
            new(-110f, -230f), new(110f, -230f),
            new(-220f, -320f), new(220f, -320f),
            new(0f, -410f),
            new(-110f, -500f), new(110f, -500f),
            new(-220f, -500f), new(220f, -500f),
            new(0f, -590f), new(-110f, -590f),
        };

        static readonly Vector2 AdBottleSlot = new(-280f, -540f);
        static readonly Vector2 NullSlotSlot = new(280f, -540f);

        sealed class LevelGenSpec
        {
            public int Level;
            public int ColorCount;
            public int RegularCupCount;
            public int LockCupCount;
            public int EmptyBottleCount;
            public bool HasAdBottle;
            public bool HasNullSlot;
            public WaterRefreshDifficulty Difficulty;
            public int LockNums;
            public bool UseColoredLock;
            public List<int> LockLayerCounts = new();
        }

        [MenuItem("WaterGame/关卡/批量生成关卡 (1-99)", false, 111)]
        public static void GenerateLevels1To99FromMenu()
        {
            if (!EditorUtility.DisplayDialog(
                    "批量生成关卡 1-99",
                    "将覆盖 level_1.json ~ level_99.json。\n\n" +
                    "规则：第3关起难度「简单」；锁瓶前10关非满瓶；生成时校验可解性。\n\n是否继续？",
                    "生成",
                    "取消"))
                return;

            var report = GenerateAll(1, 99, overwrite: true);
            LevelConfigLoader.InvalidateCache();
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("批量生成完成", report, "确定");
        }

        public static void GenerateAllBatchMode()
        {
            var report = GenerateAll(1, MaxLevelCount, overwrite: true);
            Debug.Log(report);
            if (report.Contains("失败"))
                EditorApplication.Exit(1);
        }

        [MenuItem("WaterGame/关卡/批量生成全部关卡 (1-161)", false, 110)]
        public static void GenerateAllFromMenu()
        {
            if (!EditorUtility.DisplayDialog(
                    "批量生成关卡",
                    $"将覆盖 Assets/WaterGame/Resources/Levels/Split/ 下 level_1.json ~ level_{MaxLevelCount}.json。\n\n" +
                    "规则：难度由简到难（第 161 关不超过中等）；第 1 关教学 3 瓶 2 色；\n" +
                    "每关 1~2 空瓶；第 3 关起各 1 广告瓶 + 1 空槽；布局避开 UI 遮挡。\n\n是否继续？",
                    "生成",
                    "取消"))
                return;

            var report = GenerateAll(1, MaxLevelCount, overwrite: true);
            LevelConfigLoader.InvalidateCache();
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("批量生成完成", report, "确定");
        }

        public static string GenerateAll(int fromLevel, int toLevel, bool overwrite)
        {
            var sb = new StringBuilder();
            var ok = 0;
            var fail = new List<string>();

            Directory.CreateDirectory(ProjectPaths.LevelsSplitAbsolute);

            for (var level = fromLevel; level <= toLevel; level++)
            {
                if (!TryGenerateLevel(level, out var cups, out var error))
                {
                    fail.Add($"第 {level} 关：{error}");
                    continue;
                }

                if (!overwrite && File.Exists(ProjectPaths.GetSplitLevelAbsolute(level)))
                {
                    fail.Add($"第 {level} 关：文件已存在且未勾选覆盖");
                    continue;
                }

                if (!LevelConfigLoader.SaveSplitLevel(level, cups))
                {
                    fail.Add($"第 {level} 关：写入磁盘失败");
                    continue;
                }

                ok++;
            }

            RemoveExtraLevels(toLevel);

            sb.AppendLine($"生成完成：成功 {ok} 关（{fromLevel}~{toLevel}），失败 {fail.Count} 关。");
            if (fail.Count > 0)
            {
                sb.AppendLine("失败明细：");
                foreach (var line in fail.Take(20))
                    sb.AppendLine("· " + line);
                if (fail.Count > 20)
                    sb.AppendLine($"· … 另有 {fail.Count - 20} 关");
            }

            return sb.ToString().TrimEnd();
        }

        static void RemoveExtraLevels(int maxLevel)
        {
            if (!Directory.Exists(ProjectPaths.LevelsSplitAbsolute))
                return;

            foreach (var file in Directory.GetFiles(ProjectPaths.LevelsSplitAbsolute, "level_*.json"))
            {
                var name = Path.GetFileNameWithoutExtension(file);
                if (!name.StartsWith("level_", StringComparison.Ordinal))
                    continue;
                if (!int.TryParse(name.Substring("level_".Length), out var n))
                    continue;
                if (n > maxLevel)
                    File.Delete(file);
            }
        }

        public static bool TryGenerateLevel(int level, out List<CupData> cups, out string error)
        {
            cups = null;
            error = null;

            if (level == 1)
            {
                cups = BuildTutorialLevel1();
                return ValidateGenerated(level, cups, out error);
            }

            var spec = BuildSpec(level);
            var rng = new System.Random(level * 10007 + 13);

            for (var attempt = 0; attempt < MaxAttemptsPerLevel; attempt++)
            {
                var trySpec = attempt == 0 ? spec : PerturbSpec(spec, attempt, rng);
                if (!TryBuildLevel(trySpec, rng, out cups, out error))
                    continue;
                if (ValidateGenerated(level, cups, out error))
                    return true;
            }

            cups = null;
            error ??= "多次随机仍未通过校验/难度约束";
            return false;
        }

        static LevelGenSpec BuildSpec(int level)
        {
            var spec = new LevelGenSpec { Level = level };

            if (level <= 99)
            {
                spec.ColorCount = ComputeColorCountEarly(level);
                spec.Difficulty = level <= 2
                    ? WaterRefreshDifficulty.超简单
                    : WaterRefreshDifficulty.简单;
            }
            else
            {
                var t = (level - 1) / (float)(MaxLevelCount - 1);
                spec.ColorCount = Mathf.Clamp(2 + Mathf.FloorToInt(t * 4.01f), 2, 6);
                spec.Difficulty = level <= 70 ? WaterRefreshDifficulty.简单 : WaterRefreshDifficulty.中等;
            }

            spec.EmptyBottleCount = level <= 20 ? 1 : (level % 8 == 0 ? 2 : 1);
            spec.EmptyBottleCount = Mathf.Clamp(spec.EmptyBottleCount, 1, 2);

            spec.HasAdBottle = level >= 3;
            spec.HasNullSlot = level >= 3;

            spec.LockCupCount = ComputeLockCupCount(level);
            spec.LockNums = level <= 10 ? 1 : (level <= 40 ? 1 : 2);
            spec.UseColoredLock = level >= 100;
            spec.LockLayerCounts = BuildLockLayerCounts(level, spec.LockCupCount);

            var totalLayers = spec.ColorCount * 4;
            var lockLayersSum = 0;
            foreach (var n in spec.LockLayerCounts)
                lockLayersSum += n;
            var regularLayers = totalLayers - lockLayersSum;

            if (regularLayers <= 0)
                spec.RegularCupCount = 0;
            else
            {
                var minRegular = Mathf.CeilToInt(regularLayers / 4f);
                var extraRegular = level <= 2 ? 1 : 2 + (level - 3) / 2;
                spec.RegularCupCount = Mathf.Min(minRegular + extraRegular, 9);
                if (spec.RegularCupCount < minRegular)
                    spec.RegularCupCount = minRegular;
            }

            return spec;
        }

        static int ComputeColorCountEarly(int level)
        {
            if (level <= 2) return 2;
            if (level <= 40) return 2;
            if (level <= 70) return 3;
            if (level <= 85) return 4;
            if (level <= 95) return 5;
            return 6;
        }

        static int ComputeLockCupCount(int level)
        {
            if (level < 3) return 0;
            if (level <= 70) return 1;
            if (level <= 99) return level % 2 == 0 ? 2 : 1;
            if (level < 40) return level % 3 == 0 ? 2 : 1;
            return level % 2 == 0 ? 2 : 1;
        }

        static List<int> BuildLockLayerCounts(int level, int lockCupCount)
        {
            var list = new List<int>(lockCupCount);
            if (lockCupCount <= 0)
                return list;

            int perLock;
            if (level is >= 3 and <= 5)
                perLock = 2;
            else if (level is >= 6 and <= 10)
                perLock = 3;
            else if (level <= 40)
                perLock = 3;
            else
                perLock = MaxCapacity;

            for (var i = 0; i < lockCupCount; i++)
                list.Add(perLock);
            return list;
        }

        const int MaxCapacity = GameConstants.WaterMaxCount;

        static LevelGenSpec PerturbSpec(LevelGenSpec baseSpec, int attempt, System.Random rng)
        {
            var s = new LevelGenSpec
            {
                Level = baseSpec.Level,
                ColorCount = baseSpec.ColorCount,
                RegularCupCount = baseSpec.RegularCupCount,
                LockCupCount = baseSpec.LockCupCount,
                EmptyBottleCount = baseSpec.EmptyBottleCount,
                HasAdBottle = baseSpec.HasAdBottle,
                HasNullSlot = baseSpec.HasNullSlot,
                Difficulty = baseSpec.Difficulty,
                LockNums = baseSpec.LockNums,
                UseColoredLock = baseSpec.UseColoredLock,
                LockLayerCounts = new List<int>(baseSpec.LockLayerCounts)
            };

            if (attempt % 3 == 1 && s.RegularCupCount > 2)
                s.RegularCupCount--;
            else if (attempt % 3 == 2)
                s.RegularCupCount++;

            if (attempt > 24 && s.LockCupCount > 0 && rng.NextDouble() < 0.3)
                s.LockCupCount--;

            return s;
        }

        static List<CupData> BuildTutorialLevel1()
        {
            var cups = new List<CupData>();
            AddRegularCup(cups, new Vector2(-110f, -40f), new List<int> { 1, 1, 1, 2 });
            AddRegularCup(cups, new Vector2(110f, -40f), new List<int> { 2, 2, 2, 1 });
            AddEmptyBottle(cups, new Vector2(0f, -200f));
            return cups;
        }

        static bool TryBuildLevel(LevelGenSpec spec, System.Random rng, out List<CupData> cups, out string error)
        {
            cups = new List<CupData>();
            error = null;

            var slotCount = spec.RegularCupCount + spec.LockCupCount + spec.EmptyBottleCount;
            if (slotCount > MainGridSlots.Length)
            {
                error = $"网格位不足（需 {slotCount}，可用 {MainGridSlots.Length}）";
                return false;
            }

            var positions = PickGridPositions(slotCount, spec.Level, rng);
            if (positions.Count < slotCount)
            {
                error = $"网格位不足（需 {slotCount}，可用 {MainGridSlots.Length}）";
                return false;
            }

            var idx = 0;
            var lockColors = PickLockColors(spec, rng);
            for (var i = 0; i < spec.LockCupCount; i++)
            {
                var cup = CreateCupAt(positions[idx++]);
                CupSlotKindUtility.SetKind(cup, CupSlotKind.锁瓶);
                cup.lockNums = spec.LockNums;
                cup.lockColor = lockColors[i];
                cups.Add(cup);
            }

            for (var i = 0; i < spec.RegularCupCount; i++)
                cups.Add(CreateCupAt(positions[idx++]));

            for (var i = 0; i < spec.EmptyBottleCount; i++)
                AddEmptyBottle(cups, positions[idx++]);

            if (spec.HasAdBottle)
            {
                var ad = CreateCupAt(ClampPos(AdBottleSlot));
                CupSlotKindUtility.SetKind(ad, CupSlotKind.广告瓶);
                cups.Add(ad);
            }

            if (spec.HasNullSlot)
            {
                var slot = CreateCupAt(ClampPos(NullSlotSlot));
                CupSlotKindUtility.SetKind(slot, CupSlotKind.空槽);
                cups.Add(slot);
            }

            var totalLayers = spec.ColorCount * 4;
            if (!LevelWaterRandomizer.TryRefresh(
                    cups, spec.ColorCount, totalLayers, spec.Difficulty, spec.LockLayerCounts, out error))
                return false;

            return true;
        }

        static int[] PickLockColors(LevelGenSpec spec, System.Random rng)
        {
            var colors = new int[spec.LockCupCount];
            for (var i = 0; i < colors.Length; i++)
            {
                if (!spec.UseColoredLock)
                    colors[i] = 0;
                else
                    colors[i] = rng.Next(1, spec.ColorCount + 1);
            }

            return colors;
        }

        static List<Vector2> PickGridPositions(int count, int level, System.Random rng)
        {
            var indices = Enumerable.Range(0, MainGridSlots.Length).ToList();
            var seed = level * 7919 + 3;
            Shuffle(indices, new System.Random(seed));

            const float minDist = 95f;
            var picked = new List<Vector2>();
            foreach (var idx in indices)
            {
                if (picked.Count >= count)
                    break;
                var p = ClampPos(MainGridSlots[idx]);
                var ok = true;
                foreach (var q in picked)
                {
                    if (Vector2.Distance(p, q) < minDist)
                    {
                        ok = false;
                        break;
                    }
                }

                if (ok)
                    picked.Add(p);
            }

            if (picked.Count < count)
                return new List<Vector2>();

            picked.Sort((a, b) =>
            {
                var c = b.y.CompareTo(a.y);
                return c != 0 ? c : a.x.CompareTo(b.x);
            });
            return picked;
        }

        static CupData CreateCupAt(Vector2 pos)
        {
            return new CupData
            {
                position = pos,
                colors = new List<int>()
            };
        }

        static void AddRegularCup(List<CupData> cups, Vector2 pos, List<int> colors)
        {
            var cup = CreateCupAt(ClampPos(pos));
            cup.colors = new List<int>(colors);
            cups.Add(cup);
        }

        static void AddEmptyBottle(List<CupData> cups, Vector2 pos)
        {
            var cup = CreateCupAt(ClampPos(pos));
            CupSlotKindUtility.SetKind(cup, CupSlotKind.空瓶);
            cups.Add(cup);
        }

        static Vector2 ClampPos(Vector2 p) =>
            GameplayCupSpace.ClampCupPosition(p, GameConstants.BottleWidth, GameConstants.BottleHeight);

        static bool ValidateGenerated(int level, List<CupData> cups, out string error)
        {
            error = null;
            var validation = LevelWaterValidator.Validate(cups, level);
            if (!validation.IsValid)
            {
                error = validation.Errors.FirstOrDefault() ?? "校验失败";
                return false;
            }

            if (validation.EmptyBottleCount < 1 || validation.EmptyBottleCount > 2)
            {
                error = $"空瓶数量应为 1~2（当前 {validation.EmptyBottleCount}）";
                return false;
            }

            if (level >= 3)
            {
                if (validation.VideoCupCount != 1)
                {
                    error = $"第 {level} 关应有 1 个广告瓶（当前 {validation.VideoCupCount}）";
                    return false;
                }

                if (validation.EmptySlotCupCount != 1)
                {
                    error = $"第 {level} 关应有 1 个空槽（当前 {validation.EmptySlotCupCount}）";
                    return false;
                }
            }
            else if (validation.VideoCupCount != 0 || validation.EmptySlotCupCount != 0)
            {
                error = "前两关不应有广告瓶或空槽";
                return false;
            }

            if (level == 1)
            {
                if (cups.Count != 3)
                {
                    error = "第 1 关应为 3 个瓶子";
                    return false;
                }

                if (validation.ColorCount != 2)
                {
                    error = "第 1 关应为 2 种颜色";
                    return false;
                }
            }

            var recommended = LevelWaterRandomizer.RecommendDifficulty(
                validation.ParticipatingCupCount,
                validation.ColorCount,
                validation.TotalLayers,
                validation.LockCupCount);

            if (level > 2 && recommended > WaterRefreshDifficulty.中等)
            {
                error = $"推荐难度 {recommended} 超过中等";
                return false;
            }

            if (level >= MaxLevelCount - 5 && level < 100 && recommended < WaterRefreshDifficulty.简单)
            {
                error = $"末段关卡难度偏低（{recommended}）";
                return false;
            }

            if (!ValidateLayout(cups, out var layoutErr))
            {
                error = layoutErr;
                return false;
            }

            return true;
        }

        static bool ValidateLayout(IList<CupData> cups, out string error)
        {
            error = null;
            const float minDist = 82f;
            const float topAvoidY = 130f;
            const float bottomAvoidY = -560f;

            var visible = new List<CupData>();
            foreach (var c in cups)
            {
                if (c == null) continue;
                if (c.isNull != 0) continue;
                visible.Add(c);
            }

            foreach (var c in visible)
            {
                if (c.isVideo != 0) continue;
                if (c.position.y > topAvoidY)
                {
                    error = "布局过高，可能被顶部口袋 UI 遮挡";
                    return false;
                }
            }

            foreach (var c in visible)
            {
                if (c.position.y < bottomAvoidY && c.isVideo == 0 && c.isEmptyCup == 0)
                {
                    error = "布局过低，可能被底部 UI 遮挡";
                    return false;
                }
            }

            for (var i = 0; i < visible.Count; i++)
            {
                for (var j = i + 1; j < visible.Count; j++)
                {
                    var a = visible[i];
                    var b = visible[j];
                    if (a.isVideo != 0 || b.isVideo != 0) continue;
                    var dist = Vector2.Distance(a.position, b.position);
                    if (dist < minDist)
                    {
                        error = $"瓶子间距过近（{dist:F0}<{minDist}）";
                        return false;
                    }
                }
            }

            return true;
        }

        static void Shuffle<T>(IList<T> list, System.Random rng)
        {
            for (var i = list.Count - 1; i > 0; i--)
            {
                var j = rng.Next(i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }
    }
}
