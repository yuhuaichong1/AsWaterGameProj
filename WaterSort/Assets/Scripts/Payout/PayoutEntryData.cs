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
    public string lastDailyDate = string.Empty;
    public int dailyCompletedCount;
    /// <summary>玩家已在步骤页点击 Continue 关闭过（列表切换为任务文案展示）。</summary>
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
