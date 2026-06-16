namespace AsGame.Editor.LevelEditor
{
    /// <summary>关卡检查与最少步数策略。</summary>
    public static class LevelWaterCheckPolicy
    {
        /// <summary>单关允许的最大最少完成步数（倒水次数）。</summary>
        public const int MaxAllowedMinSteps = 150;

        /// <summary>单关「检查」最多重随机次数。</summary>
        public const int MaxRerollAttempts = 48;

        /// <summary>「全部检查」逐关重随机上限（避免 60 关 × 多次 BFS 卡死编辑器）。</summary>
        public const int MaxRerollAttemptsBatch = 32;

        /// <summary>重随机循环内快速步数搜索的状态上限（仅判定是否在 150 步内有解）。</summary>
        public const int FastSearchMaxVisited = 12000;

        public static bool NeedsWaterReroll(LevelWaterValidationResult validation, LevelWaterDifficultyMetrics metrics)
        {
            if (validation == null || !validation.IsValid)
                return true;
            if (!metrics.IsSolvable)
                return true;
            if (metrics.MinSolveSteps > MaxAllowedMinSteps)
                return true;
            return false;
        }

        public static bool IsStepsAcceptable(LevelWaterDifficultyMetrics metrics) =>
            metrics.IsSolvable && metrics.MinSolveSteps <= MaxAllowedMinSteps;
    }
}
