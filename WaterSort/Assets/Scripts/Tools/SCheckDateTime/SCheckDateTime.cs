using System;
using UnityEngine;
using XrCode;

public class SCheckDateTime : Singleton<SCheckDateTime>, ILoad, IDispose
{
    /// <summary>
    /// 距离上一次的时间
    /// </summary>
    /// <param name="key">查找值</param>
    /// <param name="ifUpdate">是否更新登记时间</param>
    /// <returns>距离上一次时间</returns>
    public double SinceLastTime(string key, bool ifUpdate = true)
    {
        if(string.IsNullOrEmpty(key))
        {
            D.Error($"the key '{key}' is null or empty");
            return 0;
        }

        double value = 0;
        DateTime lastTime = SPlayerPrefs.GetDateTime(key);

        if (lastTime == default(DateTime)) 
        {
            if (ifUpdate)
                SPlayerPrefs.SetDateTime(key, DateTime.Now);
            else
                return 0;
        }
        else
        {
            TimeSpan tSpan = DateTime.Now - lastTime;
            value = tSpan.TotalSeconds;

            if (ifUpdate)
                SPlayerPrefs.SetDateTime(key, DateTime.Now);
        }

        return value;
    }

    /// <summary>
    /// 是否已是第二天
    /// </summary>
    /// <param name="key">查找值</param>
    /// <param name="ifUpdate">是否更新登记时间</param>
    /// <returns>是否跨天</returns>
    public bool IfNextDay(string key, bool ifUpdate = true)
    {
        if (string.IsNullOrEmpty(key))
        {
            D.Error($"the key '{key}' is null or empty");
            return false;
        }

        bool value = false;
        DateTime lastTime = SPlayerPrefs.GetDateTime(key);

        if (lastTime == default(DateTime))
        { 
            if(ifUpdate) 
                SPlayerPrefs.SetDateTime(key, DateTime.Now);
            else 
                return false;
        }
        else
        {
            value = DateTime.Now.Date > lastTime; 

            if (ifUpdate)
                SPlayerPrefs.SetDateTime(key, DateTime.Now);
        }

        return value;
    }

    /// <summary>
    /// 是否已经过去了指定的时间
    /// </summary>
    /// <param name="key">查找值</param>
    /// <param name="time">指定时间</param>
    /// <param name="ifUpdate">是否更新登记时间</param>
    /// <returns>是否已经过去了指定的时间</returns>
    public bool IfPassAmountTime(string key, double time, bool ifUpdate = true) 
    {
        if (string.IsNullOrEmpty(key))
        {
            D.Error($"the key '{key}' is null or empty");
            return false;
        }

        double timelength = SinceLastTime(key, ifUpdate);

        return timelength >= time;
    }

    public void Load()
    {
        
    }

    public void Dispose()
    {
        
    }
}
