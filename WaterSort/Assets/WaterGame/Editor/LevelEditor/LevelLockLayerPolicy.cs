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

        /// <summary>刷新水层时写入锁瓶的目标层数。</summary>
        public static int ResolveRefreshLockLayers(int levelIndex, CupData lockCup)
        {
            var mandatory = GetMandatoryLockLayers(levelIndex);
            if (mandatory > 0)
                return mandatory;

            var existing = lockCup?.colors?.Count ?? 0;
            if (existing > 0)
                return Mathf.Clamp(existing, 1, MaxCapacity);

            if (levelIndex is >= 3 and <= 5)
                return 2;
            if (levelIndex is >= 6 and <= 10)
                return 3;
            return MaxCapacity;
        }

        public static List<int> BuildLockLayerCounts(IList<CupData> cups, int levelIndex)
        {
            var list = new List<int>();
            if (cups == null)
                return list;

            foreach (var cup in cups)
            {
                if (cup == null || cup.isLock == 0)
                    continue;
                list.Add(ResolveRefreshLockLayers(levelIndex, cup));
            }

            return list;
        }
    }
}
