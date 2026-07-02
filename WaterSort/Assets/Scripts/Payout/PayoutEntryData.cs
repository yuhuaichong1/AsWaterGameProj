using System;

[Serializable]
public class PayoutEntryData
{
    public int channel;
    public int tierId;
    public int curStepSn;
    public long stepEndTimestamp;
    public long stepStartTimestamp;
    public int taskProgress;
    public int accumProgress;
    public int dailyLevelProgress;
    public int onlineSecondsToday;
    /// <summary>当前步骤累计在线秒数（跨天不清零，仅换步时重置）。</summary>
    public int stepOnlineSeconds;
    public string lastDailyDate = string.Empty;
    public int dailyCompletedCount;
    /// <summary>列表项是否展示任务态（Cash Out / Continue 成功后立即为 true）。</summary>
    public bool panelAcknowledged;

    public PayoutEntryKey Key => new PayoutEntryKey((EPayType)channel, tierId);

    public bool IsStarted => curStepSn > 0;

    public void ResetDailyIfNeeded(string today)
    {
        if (lastDailyDate == today) return;
        lastDailyDate = today;
        dailyLevelProgress = 0;
        onlineSecondsToday = 0;
    }
}
