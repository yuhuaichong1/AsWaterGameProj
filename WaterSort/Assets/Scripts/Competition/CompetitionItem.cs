using System;

namespace XrCode
{
    public class CompetitionItem
    {
        public bool ifOnce;//是否在执行最终值后便不在竞争
        public int maxCount;//最多判别次数
        public int curCount;//当前判断次数
        public int curPriority;//当前优先等级
        public object finalValue;//最终值
        public Action<object> finalAction;//最终回调

        public void ReSet()
        {
            curCount = 0;
            curPriority = 0;
            finalValue = null;
        }

        public void SetInfo(int maxCount, Action<object> finalAction, bool ifOnce = true)
        {
            this.maxCount = maxCount;
            this.finalAction = finalAction;
            this.ifOnce = true;
        }

        public void SetFinalValue(object value, int level)
        {
            if(ifOnce && curCount >= maxCount)
                return;

            if(level >= curPriority)
            {
                curPriority = level;
                finalValue = value;
            }

            curCount++;
            if (curCount >= maxCount)
            {
                finalAction?.Invoke(finalValue);
            }
        }

        public void SkipCompetition()
        {
            if (ifOnce && curCount >= maxCount)
                return;

            curCount++;
            if (curCount >= maxCount)
            {
                finalAction?.Invoke(finalValue);
            }
        }
    }
}

