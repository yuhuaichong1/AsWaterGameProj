using System;
using System.Collections.Generic;
using System.Text;
using cfg;
using UnityEngine;
using XrCode;

public static class FacadePayout
{
    public static Func<PayoutEntryKey, PayoutEntryData> GetEntry;
    public static Func<PayoutEntryKey, bool> HasActiveEntry;
    public static Func<PayoutEntryKey, bool> CanStart;
    public static Func<PayoutEntryKey, bool> TryStart;
    public static Func<PayoutEntryKey, bool> CanContinue;
    public static Action<PayoutEntryKey> ContinueStep;
    public static Func<PayoutEntryKey, PayoutStepDisplayData> GetStepDisplay;
    public static Func<PayoutEntryKey, long> GetRemainSeconds;
    public static Func<PayoutEntryKey, float> GetTierAmount;
    public static Func<List<ConfPayoutTier>> GetTierList;
    public static Action<PayoutEntryKey> OpenProgressPanel;
    public static Func<PayoutEntryKey, bool> EnsureStartedAndOpen;
    public static Func<bool> HasBoundAccount;
    public static Action NotifyMoneyUpdated;

    public static Action GM_SkipCountdown;
    public static Action GM_CompleteDailyTask;
    public static Action<int> GM_JumpToStep;
    public static Action GM_CompleteCurrentStep;
    public static Action GM_AdvanceToNextStep;
}
