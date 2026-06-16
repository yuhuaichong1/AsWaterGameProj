using System.Collections.Generic;
using AsGame.Data;
using UnityEngine;
using XrCode;

namespace AsGame.Editor.LevelEditor
{
    /// <summary>当前关卡盘面难度与最少完成步数（编辑器估算）。</summary>
    public struct LevelWaterDifficultyMetrics
    {
        public bool HasData;
        public bool ConfigValid;
        public WaterRefreshDifficulty Difficulty;
        public int MinSolveSteps;
        /// <summary>0=已求得；&gt;0 表示搜索达到上限仍未找到解。</summary>
        public int SearchLimitSteps;
        public bool IsSolvable;
        public string SolveNote;

        public string DifficultyLabel =>
            HasData ? LevelWaterRandomizer.GetDifficultyDisplayName(Difficulty) : "—";

        public string MinStepsLabel
        {
            get
            {
                if (!HasData) return "—";
                if (!ConfigValid) return "—（配置无效）";
                if (!IsSolvable)
                    return string.IsNullOrEmpty(SolveNote) ? "不可解" : SolveNote;
                if (SearchLimitSteps > 0)
                    return $">{SearchLimitSteps}";
                return MinSolveSteps.ToString();
            }
        }
    }

    public static class LevelWaterDifficultyAnalyzer
    {
        public static LevelWaterDifficultyMetrics Analyze(IList<CupData> cups, int levelIndex = 0) =>
            AnalyzeSolvability(cups, levelIndex, LevelWaterValidator.ValidateStructure(cups, levelIndex), fastSearch: false);

        /// <summary>在已有结构校验结果上求解；<paramref name="fastSearch"/> 用于重随机循环内快速判定。</summary>
        public static LevelWaterDifficultyMetrics AnalyzeSolvability(
            IList<CupData> cups,
            int levelIndex,
            LevelWaterValidationResult structure,
            bool fastSearch = false)
        {
            var metrics = new LevelWaterDifficultyMetrics();
            if (cups == null || cups.Count == 0)
                return metrics;

            metrics.HasData = true;
            metrics.ConfigValid = structure != null && structure.IsValid;
            if (structure == null)
                return metrics;

            metrics.Difficulty = LevelWaterRandomizer.RecommendDifficulty(
                structure.ParticipatingCupCount,
                structure.ColorCount,
                structure.TotalLayers,
                structure.LockCupCount);

            if (!structure.IsValid)
            {
                metrics.IsSolvable = false;
                metrics.SolveNote = "请先修正检查错误";
                return metrics;
            }

            if (LevelWaterValidator.HasLockDeadlockAtStart(cups))
            {
                metrics.IsSolvable = false;
                metrics.SolveNote = "关卡不可解：锁瓶内颜色无法在不解锁的情况下先完成装袋（死锁）";
                return metrics;
            }

            var maxVisited = fastSearch ? LevelWaterCheckPolicy.FastSearchMaxVisited : 0;
            var solve = LevelWaterSolver.TryFindMinSteps(
                cups, levelIndex, LevelWaterCheckPolicy.MaxAllowedMinSteps, maxVisited);
            metrics.IsSolvable = solve.IsSolvable;
            metrics.MinSolveSteps = solve.MinSteps;
            metrics.SearchLimitSteps = solve.SearchLimitSteps;
            metrics.SolveNote = solve.Message;
            return metrics;
        }
    }
}
