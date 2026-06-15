using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using AsGame.Core;
using AsGame.Data;
using UnityEditor;
using UnityEngine;

namespace AsGame.Editor.LevelEditor
{
    /// <summary>按 LevelDesignV1.json（优化版v1 表）批量生成拆关 JSON。</summary>
    public static class LevelBatchGeneratorFromSheet
    {
        const int MaxAttemptsPerLevel = 512;

        static readonly Vector2 NullSlotSlot = new(280f, -540f);
        static readonly Vector2 SecondNullSlotSlot = new(220f, -540f);

        [MenuItem("WaterGame/关卡/按优化版v1表生成前60关", false, 105)]
        public static void GenerateFromMenu()
        {
            if (!EditorUtility.DisplayDialog(
                    "按优化版v1生成关卡",
                    "将读取 LevelDesignV1.json，覆盖 level_1.json ~ level_60.json。\n\n" +
                    "瓶子按局内预览网格居中摆放（N≤6 第3排；6<N≤12 第2/4排；12<N≤18 第1/3/5排）。\n" +
                    "空瓶/空槽/广告瓶不参与水层随机；锁瓶层数按表内 lockLayers。\n\n是否继续？",
                    "生成",
                    "取消"))
                return;

            var report = GenerateRange(1, 60, overwrite: true);
            LevelConfigLoader.InvalidateCache();
            LevelDesignSheetLoader.InvalidateCache();
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("生成完成", report, "确定");
        }

        public static string GenerateRange(int fromLevel, int toLevel, bool overwrite)
        {
            LevelDesignSheetLoader.InvalidateCache();
            var sb = new StringBuilder();
            var ok = 0;
            var fail = new List<string>();

            Directory.CreateDirectory(ProjectPaths.LevelsSplitAbsolute);

            for (var level = fromLevel; level <= toLevel; level++)
            {
                if (!TryGetEntry(level, out var entry))
                {
                    fail.Add($"第 {level} 关：LevelDesignV1.json 中无配置");
                    continue;
                }

                if (!TryGenerateLevel(entry, out var cups, out var error))
                {
                    fail.Add($"第 {level} 关：{error}");
                    continue;
                }

                if (!overwrite && File.Exists(ProjectPaths.GetSplitLevelAbsolute(level)))
                {
                    fail.Add($"第 {level} 关：文件已存在");
                    continue;
                }

                if (!LevelConfigLoader.SaveSplitLevel(level, cups))
                {
                    fail.Add($"第 {level} 关：写入失败");
                    continue;
                }

                ok++;
            }

            sb.AppendLine($"按优化版v1生成：成功 {ok} 关（{fromLevel}~{toLevel}），失败 {fail.Count} 关。");
            if (fail.Count > 0)
            {
                sb.AppendLine("失败明细：");
                foreach (var line in fail.Take(25))
                    sb.AppendLine("· " + line);
            }

            return sb.ToString().TrimEnd();
        }

        static bool TryGetEntry(int level, out LevelDesignSheetEntry entry)
        {
            entry = null;
            if (!LevelDesignSheetLoader.TryGetEntry(level, out var e))
                return false;
            entry = e;
            return true;
        }

        public static bool TryGenerateLevel(LevelDesignSheetEntry spec, out List<CupData> cups, out string error)
        {
            cups = null;
            error = null;
            var rng = new System.Random(spec.level * 10007 + 17);

            for (var attempt = 0; attempt < MaxAttemptsPerLevel; attempt++)
            {
                var tryRng = attempt == 0 ? rng : new System.Random(spec.level * 7919 + attempt * 131);
                if (!TryBuildLevel(spec, tryRng, out cups, out error))
                    continue;
                if (ValidateGenerated(spec, cups, out error))
                    return true;
            }

            cups = null;
            error ??= "多次随机仍未通过校验";
            return false;
        }

        static bool TryBuildLevel(LevelDesignSheetEntry spec, System.Random rng, out List<CupData> cups, out string error)
        {
            cups = new List<CupData>();
            error = null;

            var gridCount = spec.regular + spec.empty + spec.lockCup + spec.ad;
            var positions = LevelGridPlacement.BuildCupPositions(gridCount);
            if (positions.Count < gridCount)
            {
                error = $"网格位不足（需 {gridCount}，可用 {positions.Count}）";
                return false;
            }

            var idx = 0;
            for (var i = 0; i < spec.lockCup; i++)
            {
                var cup = CreateCupAt(positions[idx++]);
                CupSlotKindUtility.SetKind(cup, CupSlotKind.锁瓶);
                cup.lockNums = spec.level <= 10 ? 1 : (spec.level <= 40 ? 1 : 2);
                cup.lockColor = 0;
                cups.Add(cup);
            }

            for (var i = 0; i < spec.regular; i++)
                cups.Add(CreateCupAt(positions[idx++]));

            for (var i = 0; i < spec.empty; i++)
                AddEmptyBottle(cups, positions[idx++]);

            if (spec.ad > 0)
            {
                var ad = CreateCupAt(positions[idx++]);
                CupSlotKindUtility.SetKind(ad, CupSlotKind.广告瓶);
                cups.Add(ad);
            }

            for (var s = 0; s < spec.slot; s++)
            {
                var slotPos = s == 0 ? NullSlotSlot : SecondNullSlotSlot;
                var slot = CreateCupAt(ClampPos(slotPos));
                CupSlotKindUtility.SetKind(slot, CupSlotKind.空槽);
                cups.Add(slot);
            }

            var lockLayerList = new List<int>();
            for (var i = 0; i < spec.lockCup; i++)
                lockLayerList.Add(Mathf.Clamp(spec.lockLayers, 1, GameConstants.WaterMaxCount));

            var difficulty = spec.level <= 2
                ? WaterRefreshDifficulty.超简单
                : spec.level <= 40 ? WaterRefreshDifficulty.简单 : WaterRefreshDifficulty.中等;

            if (!LevelWaterRandomizer.TryRefresh(
                    cups, spec.colors, spec.layers, difficulty, lockLayerList, out error))
                return false;

            return true;
        }

        static bool ValidateGenerated(LevelDesignSheetEntry spec, List<CupData> cups, out string error)
        {
            error = null;
            var validation = LevelWaterValidator.Validate(cups, spec.level);
            if (!validation.IsValid)
            {
                error = validation.Errors.FirstOrDefault() ?? "校验失败";
                return false;
            }

            if (validation.RegularCupCount != spec.regular)
            {
                error = $"普通瓶数量应为 {spec.regular}（当前 {validation.RegularCupCount}）";
                return false;
            }

            if (validation.EmptyBottleCount != spec.empty)
            {
                error = $"空瓶数量应为 {spec.empty}（当前 {validation.EmptyBottleCount}）";
                return false;
            }

            if (validation.LockCupCount != spec.lockCup)
            {
                error = $"锁瓶数量应为 {spec.lockCup}（当前 {validation.LockCupCount}）";
                return false;
            }

            if (validation.VideoCupCount != spec.ad)
            {
                error = $"广告瓶数量应为 {spec.ad}（当前 {validation.VideoCupCount}）";
                return false;
            }

            if (validation.EmptySlotCupCount != spec.slot)
            {
                error = $"空槽数量应为 {spec.slot}（当前 {validation.EmptySlotCupCount}）";
                return false;
            }

            if (validation.ColorCount != spec.colors)
            {
                error = $"颜色种类应为 {spec.colors}（当前 {validation.ColorCount}）";
                return false;
            }

            if (validation.TotalLayers != spec.layers)
            {
                error = $"水层总数应为 {spec.layers}（当前 {validation.TotalLayers}）";
                return false;
            }

            if (spec.lockCup > 0)
            {
                var expectedLockLayers = spec.lockLayers;
                foreach (var cup in cups)
                {
                    if (cup.isLock == 0) continue;
                    var n = cup.colors?.Count ?? 0;
                    if (n != expectedLockLayers)
                    {
                        error = $"锁瓶水层应为 {expectedLockLayers}（当前 {n}）";
                        return false;
                    }
                }
            }

            foreach (var cup in cups)
            {
                if (cup.isLock != 0 || cup.isEmptyCup != 0 || cup.isNull != 0 || cup.isVideo != 0)
                    continue;
                var n = cup.colors?.Count ?? 0;
                if (n <= 0)
                {
                    error = "存在未含水的普通瓶（空瓶应使用 isEmptyCup，不应留空普通瓶）";
                    return false;
                }
            }

            if (!ValidateLayout(cups, out error))
                return false;

            var metrics = LevelWaterDifficultyAnalyzer.Analyze(cups, spec.level);
            if (!LevelWaterCheckPolicy.IsStepsAcceptable(metrics))
            {
                error = metrics.IsSolvable
                    ? $"最少步数 {metrics.MinSolveSteps} 超过上限 {LevelWaterCheckPolicy.MaxAllowedMinSteps}"
                    : metrics.SolveNote ?? $"未在 {LevelWaterCheckPolicy.MaxAllowedMinSteps} 步内找到解";
                return false;
            }

            return true;
        }

        static bool ValidateLayout(IList<CupData> cups, out string error)
        {
            error = null;
            const float minDist = 80f;
            var visible = cups.Where(c => c != null && c.isNull == 0).ToList();

            for (var i = 0; i < visible.Count; i++)
            {
                for (var j = i + 1; j < visible.Count; j++)
                {
                    if (Vector2.Distance(visible[i].position, visible[j].position) < minDist)
                    {
                        error = "瓶子间距过近";
                        return false;
                    }
                }
            }

            return true;
        }

        static CupData CreateCupAt(Vector2 pos) => new()
        {
            position = pos,
            colors = new List<int>()
        };

        static void AddEmptyBottle(List<CupData> cups, Vector2 pos)
        {
            var cup = CreateCupAt(ClampPos(pos));
            CupSlotKindUtility.SetKind(cup, CupSlotKind.空瓶);
            cups.Add(cup);
        }

        static Vector2 ClampPos(Vector2 p) =>
            GameplayCupSpace.ClampCupPosition(p, GameConstants.BottleWidth, GameConstants.BottleHeight);
    }
}
