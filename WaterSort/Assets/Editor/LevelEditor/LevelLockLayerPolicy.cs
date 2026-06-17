using System.Collections.Generic;
using AsGame.Core;
using AsGame.Data;
using UnityEngine;
using XrCode;

namespace AsGame.Editor.LevelEditor
{
    /// <summary>锁瓶目标层数：设计表、关卡号规则与刷新/校验共用。</summary>
    public static class LevelLockLayerPolicy
    {
        const int MaxCapacity = GameConstants.WaterMaxCount;

        /// <summary>关卡对锁瓶层数的硬性要求；无要求时返回 -1。</summary>
        public static int GetMandatoryLockLayers(int levelIndex)
        {
            var fromSheet = LevelDesignSheetLoader.TryGetExpectedLockLayers(levelIndex);
            if (fromSheet > 0)
                return fromSheet;

            if (levelIndex is >= 3 and <= 5)
                return 2;
            if (levelIndex is >= 6 and <= 10)
                return 3;
            return -1;
        }

        /// <summary>刷新/校验时锁瓶允许的最大层数（受关卡策略与检查器约束）。</summary>
        public static int GetMaxAllowedLockLayers(int levelIndex, CupData lockCup = null)
        {
            var mandatory = GetMandatoryLockLayers(levelIndex);
            if (mandatory > 0)
                return mandatory;

            // 与 LevelWaterValidator.ValidateLockCupLayersForLevel 一致：11~40 关建议不超过 3 层
            if (levelIndex is >= 11 and <= 40)
                return 3;

            return MaxCapacity;
        }

        /// <summary>刷新水层时写入锁瓶的目标层数（尽量满瓶，留出更多普通瓶空位）。</summary>
        public static int ResolveRefreshLockLayers(int levelIndex, CupData lockCup) =>
            GetMaxAllowedLockLayers(levelIndex, lockCup);

        public static List<int> BuildLockLayerCounts(IList<CupData> cups, int levelIndex)
        {
            var list = new List<int>();
            if (cups == null)
                return list;

            foreach (var cup in cups)
            {
                if (cup == null || cup.isLock == 0)
                    continue;
                var layers = cup.colors?.Count ?? 0;
                list.Add(Mathf.Clamp(layers > 0 ? layers : 1, 1, MaxCapacity));
            }

            return list;
        }
    }
}
