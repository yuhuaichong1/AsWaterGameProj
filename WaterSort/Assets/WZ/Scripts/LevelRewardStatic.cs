namespace WZSDK
{
    using System.Collections.Generic;

    public static class LevelRewardStatic
    {
        public class RewardItem
        {
            public int type;
            public float amount;
        }

        public static List<RewardItem> GetRewards(int level)
        {
            List<RewardItem> rewards = UserDataManager.Instance.GetLevelRewards(level);
            if (rewards != null && rewards.Count > 0)
            {
                return rewards;
            }

            return new List<RewardItem>
            {
                new RewardItem
                {
                    type = 0,
                    amount = 10
                }
            };
        }
    }
}
