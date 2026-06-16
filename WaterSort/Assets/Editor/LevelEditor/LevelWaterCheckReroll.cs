using System;
using System.Collections.Generic;
using AsGame.Data;
using UnityEngine;
using XrCode;

namespace AsGame.Editor.LevelEditor
{
    public sealed class LevelCheckRerollResult
    {
        public bool Success;
        public int RerollCount;
        public LevelWaterValidationResult Validation;
        public LevelWaterDifficultyMetrics Metrics;
        public string Message;
    }

    /// <summary>检查关卡；不通过或最少步数 &gt; 150 时重新随机水层（保留瓶位与类型）。</summary>
    public static class LevelWaterCheckReroll
    {
        public static LevelCheckRerollResult CheckAndReroll(
            List<CupData> cups,
            int levelIndex,
            int colorCount,
            int totalLayers,
            WaterRefreshDifficulty difficulty,
            bool saveToDisk = false,
            int maxRerollAttempts = -1,
            Action<int, int> onAttempt = null)
        {
            var result = new LevelCheckRerollResult();
            if (cups == null || cups.Count == 0)
            {
                result.Message = "没有瓶子数据";
                return result;
            }

            if (maxRerollAttempts <= 0)
                maxRerollAttempts = LevelWaterCheckPolicy.MaxRerollAttempts;

            var rng = new System.Random(levelIndex * 10007 + 17);
            for (var attempt = 0; attempt <= maxRerollAttempts; attempt++)
            {
                onAttempt?.Invoke(attempt, maxRerollAttempts);

                result.Validation = LevelWaterValidator.ValidateStructure(cups, levelIndex);
                var useFastSearch = attempt < maxRerollAttempts;
                result.Metrics = LevelWaterDifficultyAnalyzer.AnalyzeSolvability(
                    cups, levelIndex, result.Validation, useFastSearch);

                if (IsAcceptable(result.Validation, result.Metrics))
                {
                    if (useFastSearch && result.Validation.IsValid)
                    {
                        result.Metrics = LevelWaterDifficultyAnalyzer.AnalyzeSolvability(
                            cups, levelIndex, result.Validation, fastSearch: false);
                        if (!IsAcceptable(result.Validation, result.Metrics))
                        {
                            if (attempt >= maxRerollAttempts)
                                break;
                            if (!TryRerollAfterFail(cups, levelIndex, colorCount, totalLayers, difficulty, attempt, rng, out result.Message))
                                return result;
                            continue;
                        }
                    }

                    result.Success = true;
                    result.RerollCount = attempt;
                    result.Message = FormatSuccessMessage(levelIndex, attempt, result.Metrics);
                    if (saveToDisk && levelIndex > 0)
                        LevelConfigLoader.SaveSplitLevel(levelIndex, cups);
                    return result;
                }

                if (attempt >= maxRerollAttempts)
                    break;

                if (!TryRerollAfterFail(cups, levelIndex, colorCount, totalLayers, difficulty, attempt, rng, out result.Message))
                    return result;
            }

            result.Success = false;
            result.RerollCount = maxRerollAttempts;
            result.Message = FormatFailMessage(levelIndex, maxRerollAttempts, result.Validation, result.Metrics);
            return result;
        }

        static bool TryRerollAfterFail(
            IList<CupData> cups,
            int levelIndex,
            int colorCount,
            int totalLayers,
            WaterRefreshDifficulty difficulty,
            int attempt,
            System.Random rng,
            out string message)
        {
            message = null;
            var tryRng = attempt == 0 ? rng : new System.Random(levelIndex * 7919 + attempt * 131);
            if (TryRerollWater(cups, levelIndex, colorCount, totalLayers, difficulty, tryRng, out var error))
                return true;

            message = $"第 {attempt + 1} 次重随机失败：{error}";
            return false;
        }

        static bool IsAcceptable(LevelWaterValidationResult validation, LevelWaterDifficultyMetrics metrics) =>
            validation != null && validation.IsValid && LevelWaterCheckPolicy.IsStepsAcceptable(metrics);

        public static bool TryRerollWater(
            IList<CupData> cups,
            int levelIndex,
            int colorCount,
            int totalLayers,
            WaterRefreshDifficulty difficulty,
            System.Random rng,
            out string error)
        {
            error = null;
            var lockLayerCounts = LevelLockLayerPolicy.BuildLockLayerCounts(cups, levelIndex);

            if (!LevelWaterRandomizer.TryRefresh(
                    cups, colorCount, totalLayers, difficulty, lockLayerCounts, out error, levelIndex))
                return false;

            return true;
        }

        public static bool TryResolveRefreshParams(
            IList<CupData> cups,
            int levelIndex,
            int fallbackColorCount,
            int fallbackTotalLayers,
            WaterRefreshDifficulty fallbackDifficulty,
            out int colorCount,
            out int totalLayers,
            out WaterRefreshDifficulty difficulty)
        {
            colorCount = fallbackColorCount;
            totalLayers = fallbackTotalLayers;
            difficulty = fallbackDifficulty;

            if (LevelDesignSheetLoader.TryGetEntry(levelIndex, out var entry))
            {
                colorCount = entry.colors;
                totalLayers = entry.layers;
                difficulty = entry.level <= 2
                    ? WaterRefreshDifficulty.超简单
                    : entry.level <= 40 ? WaterRefreshDifficulty.简单 : WaterRefreshDifficulty.中等;
                return true;
            }

            if (fallbackColorCount > 0 && fallbackTotalLayers > 0)
                return true;

            var validation = LevelWaterValidator.ValidateStructure(cups, levelIndex);
            if (!validation.IsValid || validation.TotalLayers <= 0)
                return false;

            colorCount = validation.ColorCount;
            totalLayers = validation.TotalLayers;
            difficulty = LevelWaterRandomizer.RecommendDifficulty(
                validation.ParticipatingCupCount,
                validation.ColorCount,
                validation.TotalLayers,
                validation.LockCupCount);
            return true;
        }

        static string FormatSuccessMessage(int levelIndex, int rerollCount, LevelWaterDifficultyMetrics metrics)
        {
            var prefix = levelIndex > 0 ? $"第 {levelIndex} 关" : "当前关卡";
            var rerollNote = rerollCount > 0 ? $"（已重随机 {rerollCount} 次）" : "";
            return $"{prefix} 检查通过{rerollNote}；最少步数 {metrics.MinStepsLabel}（上限 {LevelWaterCheckPolicy.MaxAllowedMinSteps}）。";
        }

        static string FormatFailMessage(
            int levelIndex,
            int maxAttempts,
            LevelWaterValidationResult validation,
            LevelWaterDifficultyMetrics metrics)
        {
            var prefix = levelIndex > 0 ? $"第 {levelIndex} 关" : "当前关卡";
            if (validation != null && !validation.IsValid && validation.Errors.Count > 0)
                return $"{prefix} 在 {maxAttempts} 次重随机后仍未通过：{validation.Errors[0]}";

            if (!metrics.IsSolvable)
                return $"{prefix} 在 {maxAttempts} 次重随机后仍不可解：{metrics.SolveNote}";

            if (metrics.MinSolveSteps > LevelWaterCheckPolicy.MaxAllowedMinSteps)
                return $"{prefix} 在 {maxAttempts} 次重随机后最少步数仍 > {LevelWaterCheckPolicy.MaxAllowedMinSteps}（当前 {metrics.MinSolveSteps}）。";

            return $"{prefix} 检查失败。";
        }
    }
}
