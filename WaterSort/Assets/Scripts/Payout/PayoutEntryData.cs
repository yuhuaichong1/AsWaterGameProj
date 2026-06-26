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
