using System;
using cfg;
using cfg.item;
using UnityEngine.UI;
using XrCode;

public static class PayoutStepTaskHelper
{
    private const string DailyDateFormat = "yyyy-MM-dd";

    public static string TodayKey => DateTime.Now.ToString(DailyDateFormat);

    public static ConfPayoutStep GetStepConfig(int stepSn)
    {
        return ConfigModule.Instance.Tables.TBPayoutStep.GetOrDefault(stepSn);
    }

    public static ConfPayoutTier GetTierConfig(int tierId)
    {
        return ConfigModule.Instance.Tables.TBPayoutTier.GetOrDefault(tierId);
    }

    public static float GetTierAmount(int tierId)
    {
        var tier = GetTierConfig(tierId);
        return tier?.TargetAmount ?? 0f;
    }

    public static void EnsureDailyReset(PayoutEntryData entry)
    {
        entry.ResetDailyIfNeeded(TodayKey);
    }

    public static bool IsCountdownFinished(PayoutEntryData entry)
    {
        if (!entry.IsStarted) return false;
        return GetNowTimestamp() >= entry.stepEndTimestamp;
    }

    public static long GetRemainSeconds(PayoutEntryData entry)
    {
        if (!entry.IsStarted) return 0;
        return Math.Max(0, entry.stepEndTimestamp - GetNowTimestamp());
    }

    public static int GetStepDayCount(ConfPayoutStep step)
    {
        return Math.Max(1, step.WaitHours / 24);
    }

    public static bool IsDailyLevelDone(PayoutEntryData entry, ConfPayoutStep step)
    {
        if (step.DailyLevelTarget <= 0) return true;
        EnsureDailyReset(entry);
        return entry.dailyLevelProgress >= step.DailyLevelTarget;
    }

    public static bool IsMultiDayDailySatisfied(PayoutEntryData entry, ConfPayoutStep step)
    {
        int needDays = GetStepDayCount(step);
        if (step.DailyLevelTarget <= 0 || needDays <= 1) return true;
        return entry.dailyCompletedCount >= needDays;
    }

    public static bool IsMainTaskDone(PayoutEntryData entry, ConfPayoutStep step, float tierAmount)
    {
        switch (step.TaskType)
        {
            case EPayoutTaskType.Ad:
                return entry.taskProgress >= step.TaskTarget;
            case EPayoutTaskType.Level:
            {
                bool levelDone = entry.taskProgress >= step.TaskTarget;
                bool onlineDone = step.OnlineMinutes <= 0 || entry.onlineSecondsToday >= step.OnlineMinutes * 60;
                return levelDone && onlineDone;
            }
            case EPayoutTaskType.CheckIn:
                return entry.accumProgress >= step.TaskTarget && IsDailyLevelDone(entry, step);
            case EPayoutTaskType.BankReview:
                return entry.accumProgress >= step.TaskTarget;
            case EPayoutTaskType.Online:
                return entry.taskProgress >= step.TaskTarget && entry.onlineSecondsToday >= step.OnlineMinutes * 60;
            case EPayoutTaskType.Queue:
                return entry.accumProgress >= step.TaskTarget;
            case EPayoutTaskType.DailyLevel:
                return IsDailyLevelDone(entry, step);
            case EPayoutTaskType.Amount:
                return FacadePlayer.GetMoney() >= tierAmount;
            default:
                return true;
        }
    }

    public static bool CanContinue(PayoutEntryData entry, float tierAmount)
    {
        if (!entry.IsStarted) return false;
        var step = GetStepConfig(entry.curStepSn);
        if (step == null) return false;
        if (!IsCountdownFinished(entry)) return false;
        if (!IsMainTaskDone(entry, step, tierAmount)) return false;
        if (!IsDailyLevelDone(entry, step)) return false;
        if (!IsMultiDayDailySatisfied(entry, step)) return false;
        return true;
    }

    public static void OnDailyLevelProgress(PayoutEntryData entry, ConfPayoutStep step, int add = 1)
    {
        EnsureDailyReset(entry);
        entry.dailyLevelProgress += add;
        if (step.DailyLevelTarget > 0 && entry.dailyLevelProgress >= step.DailyLevelTarget)
        {
            MarkDailyCompleted(entry);
        }

        if (step.TaskType == EPayoutTaskType.Level || step.TaskType == EPayoutTaskType.DailyLevel)
        {
            entry.taskProgress += add;
        }

        if (step.TaskType == EPayoutTaskType.CheckIn && entry.dailyLevelProgress >= step.DailyLevelTarget)
        {
            TryAdvanceCheckIn(entry, step);
        }

        if (step.TaskType == EPayoutTaskType.BankReview && entry.dailyLevelProgress >= step.DailyLevelTarget)
        {
            TryAdvanceBankReview(entry, step);
        }

        if (step.TaskType == EPayoutTaskType.Queue && entry.dailyLevelProgress >= step.DailyLevelTarget)
        {
            TryAdvanceQueue(entry, step);
        }
    }

    public static void OnAdWatched(PayoutEntryData entry, ConfPayoutStep step)
    {
        if (step.TaskType == EPayoutTaskType.Ad)
            entry.taskProgress++;
    }

    public static void OnOnlineSeconds(PayoutEntryData entry, ConfPayoutStep step, int seconds)
    {
        EnsureDailyReset(entry);
        entry.onlineSecondsToday += seconds;
        if (step.TaskType == EPayoutTaskType.Online)
        {
            entry.taskProgress = entry.onlineSecondsToday;
        }
    }

    private static void TryAdvanceCheckIn(PayoutEntryData entry, ConfPayoutStep step)
    {
        string dayKey = $"PayoutCheckIn_{entry.channel}_{entry.tierId}_{TodayKey}";
        if (SPlayerPrefs.GetInt(dayKey, 0) == 1) return;
        SPlayerPrefs.SetInt(dayKey, 1);
        entry.accumProgress = Math.Min(entry.accumProgress + 1, step.TaskTarget);
    }

    private static void TryAdvanceBankReview(PayoutEntryData entry, ConfPayoutStep step)
    {
        string dayKey = $"PayoutBank_{entry.channel}_{entry.tierId}_{TodayKey}";
        if (SPlayerPrefs.GetInt(dayKey, 0) == 1) return;
        SPlayerPrefs.SetInt(dayKey, 1);
        entry.accumProgress = Math.Min(entry.accumProgress + 1, step.TaskTarget);
    }

    private static void TryAdvanceQueue(PayoutEntryData entry, ConfPayoutStep step)
    {
        string dayKey = $"PayoutQueue_{entry.channel}_{entry.tierId}_{TodayKey}";
        if (SPlayerPrefs.GetInt(dayKey, 0) == 1) return;
        SPlayerPrefs.SetInt(dayKey, 1);
        entry.accumProgress = Math.Min(entry.accumProgress + 1, step.TaskTarget);
    }

    public static void ResetStepTask(PayoutEntryData entry)
    {
        entry.taskProgress = 0;
        entry.dailyCompletedCount = 0;
        entry.onlineSecondsToday = 0;
        EnsureDailyReset(entry);
        entry.dailyLevelProgress = 0;
    }

    public static void BeginStep(PayoutEntryData entry, ConfPayoutStep step)
    {
        entry.curStepSn = step.Sn;
        entry.stepStartTimestamp = GetNowTimestamp();
        entry.stepEndTimestamp = entry.stepStartTimestamp + step.WaitHours * 3600L;
        ResetStepTask(entry);
    }

    public static PayoutStepDisplayData BuildDisplay(PayoutEntryData entry, float tierAmount)
    {
        var step = GetStepConfig(entry.curStepSn);
        var prevStep = GetStepConfig(entry.curStepSn - 1);
        var display = new PayoutStepDisplayData
        {
            CanContinue = CanContinue(entry, tierAmount),
            IsTerminal = step != null && step.IfTerminal,
            RemainSeconds = GetRemainSeconds(entry)
        };

        if (step == null) return display;

        display.Title = FormatLang(step.TitleLangId);
        display.PrevTask = FormatPrevTask(entry, prevStep, tierAmount, step);
        display.CurTask = FormatCurTask(entry, step, tierAmount);
        display.Explain = FormatExplain(entry, step, tierAmount);
        display.Finish = FormatFinish(entry, step, tierAmount);
        display.CurStepDone = display.CanContinue;
        display.PrevStepDone = true;
        display.ShowFinishBanner = entry.IsStarted;
        return display;
    }

    private static string FormatPrevTask(PayoutEntryData entry, ConfPayoutStep prevStep, float tierAmount, ConfPayoutStep curStep)
    {
        string template = FacadeLanguage.GetText(curStep.PrevTaskLangId.ToString());
        // 步骤 1：上一步为达成档位提现金额（仅显示档位金额）
        if (entry.curStepSn <= 1)
            return SafeFormat(template, FacadePayType.RegionalChange(tierAmount));

        if (prevStep == null)
            return template;

        // 步骤 2+：上一步展示「上一步骤已完成的主任务」，文案取当前步 prevTaskLangId，数值取上一步配置
        return FormatTaskByStep(entry, prevStep, tierAmount, curStep.PrevTaskLangId, true);
    }

    private static string FormatCurTask(PayoutEntryData entry, ConfPayoutStep step, float tierAmount)
    {
        return FormatTaskByStep(entry, step, tierAmount, step.CurTaskLangId, false);
    }

    private static string FormatTaskByStep(PayoutEntryData entry, ConfPayoutStep step, float tierAmount, int langId, bool useCompletedValues)
    {
        string template = FacadeLanguage.GetText(langId.ToString());
        switch (step.TaskType)
        {
            case EPayoutTaskType.Ad:
                return SafeFormat(template,
                    useCompletedValues ? step.TaskTarget : entry.taskProgress, step.TaskTarget);
            case EPayoutTaskType.Level:
            case EPayoutTaskType.DailyLevel:
                return SafeFormat(template,
                    useCompletedValues ? step.TaskTarget : entry.taskProgress, step.TaskTarget);
            case EPayoutTaskType.CheckIn:
                return SafeFormat(template,
                    useCompletedValues ? step.TaskTarget : entry.accumProgress, step.TaskTarget);
            case EPayoutTaskType.BankReview:
            case EPayoutTaskType.Queue:
                return SafeFormat(template,
                    useCompletedValues ? step.TaskTarget : entry.accumProgress, step.TaskTarget);
            case EPayoutTaskType.Amount:
                return SafeFormat(template,
                    FacadePayType.RegionalChange((float)FacadePlayer.GetMoney()),
                    FacadePayType.RegionalChange(tierAmount));
            default:
                return SafeFormat(template,
                    useCompletedValues ? step.TaskTarget : entry.taskProgress, step.TaskTarget);
        }
    }

    private static string FormatExplain(PayoutEntryData entry, ConfPayoutStep step, float tierAmount)
    {
        string template = FacadeLanguage.GetText(step.ExplainLangId.ToString());
        switch (step.TaskType)
        {
            case EPayoutTaskType.Ad:
                return SafeFormat(template, step.TaskTarget);
            case EPayoutTaskType.Level:
            case EPayoutTaskType.DailyLevel:
                return SafeFormat(template, step.TaskTarget);
            case EPayoutTaskType.CheckIn:
                return SafeFormat(template,
                    Math.Max(0, step.DailyLevelTarget - entry.dailyLevelProgress));
            case EPayoutTaskType.BankReview:
                return SafeFormat(template, step.TaskTarget,
                    Math.Max(0, step.DailyLevelTarget - entry.dailyLevelProgress));
            case EPayoutTaskType.Online:
                return SafeFormat(template, step.TaskTarget);
            case EPayoutTaskType.Queue:
                return SafeFormat(template,
                    Math.Max(0, step.DailyLevelTarget - entry.dailyLevelProgress));
            default:
                return template;
        }
    }

    private static string FormatFinish(PayoutEntryData entry, ConfPayoutStep step, float tierAmount)
    {
        string template = FacadeLanguage.GetText(step.FinishLangId.ToString());
        if (step.FinishLangId == 10238)
            return SafeFormat(template, FacadePayType.RegionalChange(tierAmount));
        return template;
    }

    private static string SafeFormat(string template, params object[] args)
    {
        if (string.IsNullOrEmpty(template) || args == null || args.Length == 0)
            return template ?? string.Empty;
        if (template.IndexOf('{') < 0)
            return template;
        return string.Format(template, args);
    }

    private static string FormatLang(int langId) => FacadeLanguage.GetText(langId.ToString());

    private static void MarkDailyCompleted(PayoutEntryData entry)
    {
        string dayKey = $"PayoutDailyDone_{entry.channel}_{entry.tierId}_{TodayKey}";
        if (SPlayerPrefs.GetInt(dayKey, 0) == 1) return;
        SPlayerPrefs.SetInt(dayKey, 1);
        entry.dailyCompletedCount++;
        SPlayerPrefs.Save();
    }

    public static long GetNowTimestamp() => DateTimeOffset.Now.ToUnixTimeSeconds();

    public static string FormatRemainTime(long seconds)
    {
        var span = TimeSpan.FromSeconds(Math.Max(0, seconds));
        if (span.TotalHours >= 1)
            return $"{(int)span.TotalHours:D2}:{span.Minutes:D2}:{span.Seconds:D2}";
        return $"{span.Minutes:D2}:{span.Seconds:D2}";
    }

    /// <summary>
    /// 列表项倒计时：大于 24 小时显示「n Day HH:MM:SS」，否则仅显示「HH:MM:SS」。
    /// </summary>
    public static string FormatListCountdown(long seconds)
    {
        seconds = Math.Max(0, seconds);
        const long daySeconds = 86400;

        if (seconds > daySeconds)
        {
            long days = seconds / daySeconds;
            long remainder = seconds % daySeconds;
            int hours = (int)(remainder / 3600);
            int minutes = (int)((remainder % 3600) / 60);
            int secs = (int)(remainder % 60);
            return $"{days} Day {hours:D2}:{minutes:D2}:{secs:D2}";
        }

        var span = TimeSpan.FromSeconds(seconds);
        return $"{(int)span.TotalHours:D2}:{span.Minutes:D2}:{span.Seconds:D2}";
    }
}
