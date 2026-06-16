using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using AsGame.Core;
using AsGame.Data;
using UnityEditor;
using UnityEngine;

namespace AsGame.Editor.LevelEditor
{
    /// <summary>「全部检查」逐关汇总：难度、瓶型数量、颜色/水层、均层、最少步数。</summary>
    public sealed class LevelBatchCheckEntry
    {
        public int LevelIndex;
        public bool IsValid;
        public WaterRefreshDifficulty Difficulty;
        public int RegularCupCount;
        public int LockCupCount;
        public int EmptyBottleCount;
        public int EmptySlotCupCount;
        public int VideoCupCount;
        public int ColorCount;
        public int TotalLayers;
        public LevelWaterDifficultyMetrics Metrics;
        public LevelWaterValidationResult Validation;
        public int RerollCount;

        public int WaterBottleTotal => RegularCupCount + LockCupCount + EmptyBottleCount;

        public float AvgLayersPerWaterBottle =>
            WaterBottleTotal > 0 ? TotalLayers / (float)WaterBottleTotal : 0f;

        public string DifficultyLabel =>
            LevelWaterRandomizer.GetDifficultyDisplayName(Difficulty);

        public string MinStepsLabel => Metrics.MinStepsLabel;
    }

    public static class LevelBatchChecker
    {
        public static List<LevelBatchCheckEntry> CheckAllOnDisk(
            bool showProgress = true,
            bool rerollOnFail = false,
            string progressTitle = "全部检查")
        {
            var entries = new List<LevelBatchCheckEntry>();
            var dir = ProjectPaths.LevelsSplitAbsolute;
            if (!Directory.Exists(dir))
                return entries;

            var indices = new List<int>();
            foreach (var file in Directory.GetFiles(dir, "level_*.json"))
            {
                var m = Regex.Match(Path.GetFileNameWithoutExtension(file), @"^level_(\d+)$");
                if (m.Success)
                    indices.Add(int.Parse(m.Groups[1].Value));
            }

            indices.Sort();
            for (var i = 0; i < indices.Count; i++)
            {
                var level = indices[i];
                if (showProgress)
                {
                    var action = rerollOnFail ? "修复" : "检查";
                    if (EditorUtility.DisplayCancelableProgressBar(
                            progressTitle,
                            $"正在{action}第 {level} 关（{i + 1}/{indices.Count}）…",
                            indices.Count <= 1 ? 1f : i / (float)indices.Count))
                    {
                        EditorUtility.ClearProgressBar();
                        break;
                    }
                }

                entries.Add(rerollOnFail ? CheckLevelWithReroll(level) : CheckLevel(level));
            }

            if (showProgress)
                EditorUtility.ClearProgressBar();

            return entries;
        }

        public static LevelBatchCheckEntry CheckLevelWithReroll(int levelIndex, bool saveToDisk = true)
        {
            var cups = LevelConfigLoader.LoadSplitLevelFromDisk(levelIndex);
            var structure = LevelWaterValidator.ValidateStructure(cups, levelIndex);
            var metrics = LevelWaterDifficultyAnalyzer.AnalyzeSolvability(
                cups, levelIndex, structure, fastSearch: false);
            if (structure.IsValid && LevelWaterCheckPolicy.IsStepsAcceptable(metrics))
                return BuildEntry(levelIndex, structure, metrics, true, 0);

            LevelWaterCheckReroll.TryResolveRefreshParams(
                cups, levelIndex, 0, 0, WaterRefreshDifficulty.简单,
                out var colorCount, out var totalLayers, out var difficulty);

            var maxAttempts = LevelWaterCheckPolicy.MaxRerollAttemptsBatch;
            var reroll = LevelWaterCheckReroll.CheckAndReroll(
                cups, levelIndex, colorCount, totalLayers, difficulty, saveToDisk,
                maxAttempts,
                (attempt, max) =>
                {
                    if (attempt <= 0)
                        return;
                    EditorUtility.DisplayProgressBar(
                        "自动修复",
                        $"第 {levelIndex} 关重随机中（{attempt}/{max}）…",
                        0f);
                });

            if (!reroll.Success)
                AppendSolvabilityErrors(reroll.Validation, reroll.Metrics);

            return BuildEntry(levelIndex, reroll.Validation, reroll.Metrics, reroll.Success, reroll.RerollCount);
        }

        public static LevelBatchCheckEntry CheckLevel(int levelIndex)
        {
            var cups = LevelConfigLoader.LoadSplitLevelFromDisk(levelIndex);
            var validation = LevelWaterValidator.ValidateStructure(cups, levelIndex);
            var metrics = LevelWaterDifficultyAnalyzer.AnalyzeSolvability(
                cups, levelIndex, validation, fastSearch: false);
            AppendSolvabilityErrors(validation, metrics);
            var isValid = validation.IsValid && LevelWaterCheckPolicy.IsStepsAcceptable(metrics);
            return BuildEntry(levelIndex, validation, metrics, isValid, 0);
        }

        static void AppendSolvabilityErrors(
            LevelWaterValidationResult validation,
            LevelWaterDifficultyMetrics metrics)
        {
            if (validation == null || !validation.IsValid || LevelWaterCheckPolicy.IsStepsAcceptable(metrics))
                return;

            if (!string.IsNullOrEmpty(metrics.SolveNote) &&
                !validation.Errors.Contains(metrics.SolveNote))
                validation.Errors.Add(metrics.SolveNote);
            else if (metrics.MinSolveSteps > LevelWaterCheckPolicy.MaxAllowedMinSteps)
                validation.Errors.Add(
                    $"最少步数 {metrics.MinSolveSteps} 超过上限 {LevelWaterCheckPolicy.MaxAllowedMinSteps}");
            else if (!metrics.IsSolvable)
                validation.Errors.Add("关卡不可解或未在步数上限内找到解");
        }

        static LevelBatchCheckEntry BuildEntry(
            int levelIndex,
            LevelWaterValidationResult validation,
            LevelWaterDifficultyMetrics metrics,
            bool isValid,
            int rerollCount)
        {
            validation ??= new LevelWaterValidationResult { LevelIndex = levelIndex };
            if (validation.LevelIndex <= 0)
                validation.LevelIndex = levelIndex;

            var entry = new LevelBatchCheckEntry
            {
                LevelIndex = levelIndex,
                IsValid = isValid,
                Validation = validation,
                Metrics = metrics,
                RerollCount = rerollCount,
                RegularCupCount = validation.RegularCupCount,
                LockCupCount = validation.LockCupCount,
                EmptyBottleCount = validation.EmptyBottleCount,
                EmptySlotCupCount = validation.EmptySlotCupCount,
                VideoCupCount = validation.VideoCupCount,
                ColorCount = validation.ColorCount,
                TotalLayers = validation.TotalLayers
            };

            entry.Difficulty = validation.IsValid
                ? LevelWaterRandomizer.RecommendDifficulty(
                    validation.ParticipatingCupCount,
                    validation.ColorCount,
                    validation.TotalLayers,
                    validation.LockCupCount)
                : metrics.Difficulty;

            return entry;
        }

        public static string FormatFullLog(
            IReadOnlyList<LevelBatchCheckEntry> entries,
            string operationTitle = "全部检查",
            bool includeRerollSummary = false)
        {
            if (entries == null || entries.Count == 0)
                return "未找到任何关卡 JSON（Assets/AssetBundleLocal/Json/Levels/level_*.json）。";

            var sb = new StringBuilder();
            sb.AppendLine(FormatSummaryForStatusBar(entries, operationTitle));
            if (includeRerollSummary)
            {
                var rerolled = entries.Count(e => e.RerollCount > 0);
                var fixedCount = entries.Count(e => e.RerollCount > 0 && e.IsValid);
                if (rerolled > 0)
                    sb.AppendLine($"已重随机 {rerolled} 关，其中 {fixedCount} 关修复成功并写回 JSON。");
                else
                    sb.AppendLine("无需重随机，所有关卡均已符合要求。");
            }

            sb.AppendLine("可点击「导出报告」保存 CSV。");
            sb.AppendLine();
            sb.Append(FormatDetailedReport(entries, operationTitle, includeRerollSummary));
            return sb.ToString().TrimEnd();
        }

        public static string FormatDetailedReport(
            IReadOnlyList<LevelBatchCheckEntry> entries,
            string operationTitle = "全部检查",
            bool showRerollColumn = false)
        {
            if (entries == null || entries.Count == 0)
                return "未找到任何关卡 JSON（Assets/AssetBundleLocal/Json/Levels/level_*.json）。";

            var valid = entries.Count(e => e.IsValid);
            var invalid = entries.Count - valid;
            var sb = new StringBuilder();
            sb.AppendLine($"{operationTitle}明细：共 {entries.Count} 关，通过 {valid} 关，未通过 {invalid} 关。");
            sb.AppendLine("均层/瓶 = 水层总数 ÷（普通瓶 + 锁瓶 + 空瓶）");
            sb.AppendLine();
            if (showRerollColumn)
            {
                sb.Append(
                    "关卡\t难度\t普通\t锁\t空瓶\t空槽\t广告\t颜色\t水层\t均层/瓶\t最少步数\t重随机\t状态");
            }
            else
            {
                sb.Append(
                    "关卡\t难度\t普通\t锁\t空瓶\t空槽\t广告\t颜色\t水层\t均层/瓶\t最少步数\t状态");
            }

            sb.AppendLine();

            foreach (var e in entries)
            {
                var status = e.IsValid ? "通过" : "失败";
                if (!e.IsValid && e.Validation?.Errors.Count > 0)
                    status = "失败:" + e.Validation.Errors[0];

                var avg = e.WaterBottleTotal > 0 ? e.AvgLayersPerWaterBottle.ToString("F2") : "—";
                var steps = e.Metrics.HasData && e.Metrics.ConfigValid ? e.MinStepsLabel : "—";

                if (showRerollColumn)
                {
                    sb.AppendLine(
                        $"{e.LevelIndex}\t{e.DifficultyLabel}\t" +
                        $"{e.RegularCupCount}\t{e.LockCupCount}\t{e.EmptyBottleCount}\t" +
                        $"{e.EmptySlotCupCount}\t{e.VideoCupCount}\t" +
                        $"{e.ColorCount}\t{e.TotalLayers}\t{avg}\t{steps}\t{e.RerollCount}\t{status}");
                }
                else
                {
                    sb.AppendLine(
                        $"{e.LevelIndex}\t{e.DifficultyLabel}\t" +
                        $"{e.RegularCupCount}\t{e.LockCupCount}\t{e.EmptyBottleCount}\t" +
                        $"{e.EmptySlotCupCount}\t{e.VideoCupCount}\t" +
                        $"{e.ColorCount}\t{e.TotalLayers}\t{avg}\t{steps}\t{status}");
                }
            }

            if (invalid > 0)
            {
                sb.AppendLine();
                sb.AppendLine("未通过关卡明细：");
                foreach (var e in entries.Where(x => !x.IsValid))
                {
                    sb.AppendLine($"· 第 {e.LevelIndex} 关");
                    if (e.Validation?.Errors.Count > 0)
                    {
                        foreach (var err in e.Validation.Errors)
                            sb.AppendLine("    " + err);
                    }
                    else if (!string.IsNullOrEmpty(e.Metrics.SolveNote))
                    {
                        sb.AppendLine("    " + e.Metrics.SolveNote);
                    }
                }
            }

            return sb.ToString().TrimEnd();
        }

        public static string FormatSummaryForStatusBar(
            IReadOnlyList<LevelBatchCheckEntry> entries,
            string operationTitle = "全部检查")
        {
            if (entries == null || entries.Count == 0)
                return "未找到任何关卡 JSON。";

            var valid = entries.Count(e => e.IsValid);
            return $"{operationTitle}完成：共 {entries.Count} 关，通过 {valid} 关，未通过 {entries.Count - valid} 关。";
        }

        public static void LogDetailedReport(IReadOnlyList<LevelBatchCheckEntry> entries, string operationTitle = "全部检查")
        {
            var text = FormatDetailedReport(entries, operationTitle);
            const int chunkSize = 15000;
            if (text.Length <= chunkSize)
            {
                Debug.Log(text);
                return;
            }

            Debug.Log("全部检查明细（分段输出）…");
            for (var offset = 0; offset < text.Length; offset += chunkSize)
            {
                var len = Math.Min(chunkSize, text.Length - offset);
                Debug.Log(text.Substring(offset, len));
            }
        }

        public static string FormatCsvReport(IReadOnlyList<LevelBatchCheckEntry> entries)
        {
            if (entries == null || entries.Count == 0)
                return string.Empty;

            var sb = new StringBuilder();
            sb.AppendLine(string.Join(",",
                "关卡", "难度", "普通瓶", "锁瓶", "空瓶", "空槽", "广告瓶",
                "颜色种数", "水层总数", "均层每瓶", "最少步数", "重随机次数", "状态"));

            foreach (var e in entries)
            {
                var status = e.IsValid ? "通过" : "失败";
                if (!e.IsValid && e.Validation?.Errors.Count > 0)
                    status = "失败:" + e.Validation.Errors[0].Replace("\"", "\"\"");

                var avg = e.WaterBottleTotal > 0 ? e.AvgLayersPerWaterBottle.ToString("F2") : "";
                var steps = e.IsValid ? e.MinStepsLabel : "";

                sb.AppendLine(string.Join(",",
                    e.LevelIndex,
                    Csv(e.DifficultyLabel),
                    e.RegularCupCount,
                    e.LockCupCount,
                    e.EmptyBottleCount,
                    e.EmptySlotCupCount,
                    e.VideoCupCount,
                    e.ColorCount,
                    e.TotalLayers,
                    Csv(avg),
                    Csv(steps),
                    e.RerollCount,
                    Csv(status)));
            }

            return sb.ToString();
        }

        static string Csv(string value)
        {
            if (string.IsNullOrEmpty(value))
                return "";
            if (value.IndexOfAny(new[] { ',', '"', '\n', '\r' }) >= 0)
                return "\"" + value.Replace("\"", "\"\"") + "\"";
            return value;
        }

        public static string GetDefaultExportFileName() =>
            $"level_check_report_{DateTime.Now:yyyyMMdd_HHmmss}.csv";

        public static bool TryExportReport(IReadOnlyList<LevelBatchCheckEntry> entries, string absolutePath)
        {
            if (entries == null || entries.Count == 0)
                return false;

            try
            {
                var dir = Path.GetDirectoryName(absolutePath);
                if (!string.IsNullOrEmpty(dir))
                    Directory.CreateDirectory(dir);

                var csv = FormatCsvReport(entries);
                File.WriteAllText(absolutePath, csv, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[LevelBatchChecker] 导出失败：{ex.Message}");
                return false;
            }
        }

        public static bool TryExportReportWithDialog(IReadOnlyList<LevelBatchCheckEntry> entries)
        {
            if (entries == null || entries.Count == 0)
            {
                EditorUtility.DisplayDialog("导出报告", "没有可导出的检查数据。", "确定");
                return false;
            }

            var defaultDir = Path.Combine(ProjectPaths.ContentRootAbsolute, "Editor", "Reports");
            Directory.CreateDirectory(defaultDir);
            var path = EditorUtility.SaveFilePanel(
                "导出全部检查报告",
                defaultDir,
                GetDefaultExportFileName(),
                "csv");

            if (string.IsNullOrEmpty(path))
                return false;

            if (!TryExportReport(entries, path))
            {
                EditorUtility.DisplayDialog("导出报告", "写入文件失败，请查看 Console。", "确定");
                return false;
            }

            EditorUtility.RevealInFinder(path);
            Debug.Log($"[LevelBatchChecker] 已导出报告 → {path}");
            return true;
        }
    }
}
