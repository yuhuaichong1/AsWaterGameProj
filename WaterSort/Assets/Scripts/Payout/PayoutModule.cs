using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using cfg;
using UnityEngine;
using XrCode;

namespace XrCode
{
    public class PayoutModule : BaseModule
    {
        private readonly Dictionary<string, PayoutEntryData> entries = new Dictionary<string, PayoutEntryData>();
        private float onlineTick;
        private PayoutEntryKey? gmFocusKey;

        protected override void OnLoad()
        {
            RegisterFacade();
            LoadData();
            FacadeAd.OnRewardAdReceivedReward += OnRewardAdReceived;
            FacadeEvent.AddEventListener(PayoutEventTypes.MONEY_UPDATED, OnMoneyUpdatedEvent);
            RegisetUpdateObj();
        }

        protected override void OnDispose()
        {
            FacadeAd.OnRewardAdReceivedReward -= OnRewardAdReceived;
            FacadeEvent.RemoveEventListener(PayoutEventTypes.MONEY_UPDATED, OnMoneyUpdatedEvent);
            UnregisterFacade();
        }

        protected override void OnUpdate()
        {
            if (!GameDefines.UsePayoutV2) return;
            onlineTick += Time.deltaTime;
            if (onlineTick < 1f) return;
            int seconds = (int)onlineTick;
            onlineTick -= seconds;
            foreach (var entry in entries.Values)
            {
                if (!entry.IsStarted) continue;
                var step = PayoutStepTaskHelper.GetStepConfig(entry.curStepSn);
                if (step == null) continue;
                PayoutStepTaskHelper.OnOnlineSeconds(entry, step, seconds);
            }
            SaveData();
        }

        private void RegisterFacade()
        {
            FacadePayout.GetEntry = GetEntry;
            FacadePayout.HasActiveEntry = HasActiveEntry;
            FacadePayout.CanStart = CanStart;
            FacadePayout.TryStart = TryStart;
            FacadePayout.CanContinue = CanContinue;
            FacadePayout.ContinueStep = ContinueStep;
            FacadePayout.GetStepDisplay = GetStepDisplay;
            FacadePayout.GetRemainSeconds = GetRemainSeconds;
            FacadePayout.GetTierAmount = key => PayoutStepTaskHelper.GetTierAmount(key.TierId);
            FacadePayout.GetTierList = GetTierList;
            FacadePayout.OpenProgressPanel = OpenProgressPanel;
            FacadePayout.EnsureStartedAndOpen = EnsureStartedAndOpen;
            FacadePayout.HasBoundAccount = HasBoundAccount;
            FacadePayout.NotifyMoneyUpdated = NotifyMoneyUpdated;
            FacadePayout.GM_SkipCountdown = GM_SkipCountdown;
            FacadePayout.GM_CompleteDailyTask = GM_CompleteDailyTask;
            FacadePayout.GM_JumpToStep = GM_JumpToStep;
            FacadePayout.GM_CompleteCurrentStep = GM_CompleteCurrentStep;
            FacadePayout.GM_AdvanceToNextStep = GM_AdvanceToNextStep;
        }

        private void UnregisterFacade()
        {
            FacadePayout.GetEntry = null;
            FacadePayout.HasActiveEntry = null;
            FacadePayout.CanStart = null;
            FacadePayout.TryStart = null;
            FacadePayout.CanContinue = null;
            FacadePayout.ContinueStep = null;
            FacadePayout.GetStepDisplay = null;
            FacadePayout.GetRemainSeconds = null;
            FacadePayout.GetTierAmount = null;
            FacadePayout.GetTierList = null;
            FacadePayout.OpenProgressPanel = null;
            FacadePayout.EnsureStartedAndOpen = null;
            FacadePayout.HasBoundAccount = null;
            FacadePayout.NotifyMoneyUpdated = null;
            FacadePayout.GM_SkipCountdown = null;
            FacadePayout.GM_CompleteDailyTask = null;
            FacadePayout.GM_JumpToStep = null;
            FacadePayout.GM_CompleteCurrentStep = null;
            FacadePayout.GM_AdvanceToNextStep = null;
        }

        public void OnLevelPassed(int count = 1)
        {
            if (!GameDefines.UsePayoutV2) return;
            bool changed = false;
            foreach (var entry in entries.Values)
            {
                if (!entry.IsStarted) continue;
                var step = PayoutStepTaskHelper.GetStepConfig(entry.curStepSn);
                if (step == null) continue;
                PayoutStepTaskHelper.OnDailyLevelProgress(entry, step, count);
                changed = true;
            }
            if (changed)
            {
                SaveData();
                DispatchEntryUpdated();
            }
        }

        private void OnRewardAdReceived(string platform, double revenue, double ecpm, string precision, int rewardAmount)
        {
            if (!GameDefines.UsePayoutV2) return;
            bool changed = false;
            foreach (var entry in entries.Values)
            {
                if (!entry.IsStarted) continue;
                var step = PayoutStepTaskHelper.GetStepConfig(entry.curStepSn);
                if (step == null) continue;
                PayoutStepTaskHelper.OnAdWatched(entry, step);
                changed = true;
            }
            if (changed)
            {
                SaveData();
                DispatchEntryUpdated();
            }
        }

        private void OnMoneyUpdatedEvent(EventStruct evt)
        {
            DispatchEntryUpdated();
        }

        public void NotifyMoneyUpdated()
        {
            if (!GameDefines.UsePayoutV2) return;
            FacadeEvent.DispatchEvent(PayoutEventTypes.MONEY_UPDATED, null);
        }

        private PayoutEntryData GetOrCreateEntry(PayoutEntryKey key)
        {
            if (!entries.TryGetValue(key.StorageKey, out var entry))
            {
                entry = new PayoutEntryData
                {
                    channel = (int)key.Channel,
                    tierId = key.TierId
                };
                entries[key.StorageKey] = entry;
            }
            return entry;
        }

        private PayoutEntryData GetEntry(PayoutEntryKey key)
        {
            entries.TryGetValue(key.StorageKey, out var entry);
            return entry;
        }

        private bool HasActiveEntry(PayoutEntryKey key)
        {
            var entry = GetEntry(key);
            return entry != null && entry.IsStarted;
        }

        private bool CanStart(PayoutEntryKey key)
        {
            if (GameDefines.PayoutSkipAmountCheck) return true;
            float amount = PayoutStepTaskHelper.GetTierAmount(key.TierId);
            return FacadePlayer.GetMoney() >= amount && HasBoundAccount();
        }

        private bool StartEntry(PayoutEntryKey key)
        {
            var entry = GetOrCreateEntry(key);
            if (entry.IsStarted) return true;

            var firstStep = PayoutStepTaskHelper.GetStepConfig(1);
            if (firstStep == null) return false;

            FacadeWithdraw.SetPayType(key.Channel);
            PayoutStepTaskHelper.BeginStep(entry, firstStep);
            SaveData();
            DispatchStepChanged(key);
            return true;
        }

        private bool TryStart(PayoutEntryKey key)
        {
            if (!CanStart(key)) return false;
            return StartEntry(key);
        }

        private bool EnsureStartedAndOpen(PayoutEntryKey key)
        {
            if (!StartEntry(key)) return false;
            SetGmFocusKey(key);
            OpenProgressPanel(key);
            return true;
        }

        private bool CanContinue(PayoutEntryKey key)
        {
            var entry = GetEntry(key);
            if (entry == null || !entry.IsStarted) return false;
            return PayoutStepTaskHelper.CanContinue(entry, PayoutStepTaskHelper.GetTierAmount(key.TierId));
        }

        private void ContinueStep(PayoutEntryKey key)
        {
            var entry = GetEntry(key);
            if (entry == null || !CanContinue(key)) return;

            var step = PayoutStepTaskHelper.GetStepConfig(entry.curStepSn);
            if (step == null) return;

            if (step.IfTerminal)
            {
                PayoutStepTaskHelper.BeginStep(entry, step);
            }
            else
            {
                var next = PayoutStepTaskHelper.GetStepConfig(entry.curStepSn + 1);
                if (next == null) return;
                PayoutStepTaskHelper.BeginStep(entry, next);
            }

            SaveData();
            DispatchStepChanged(key);
        }

        private PayoutStepDisplayData GetStepDisplay(PayoutEntryKey key)
        {
            var entry = GetEntry(key);
            if (entry == null || !entry.IsStarted)
            {
                return new PayoutStepDisplayData();
            }
            return PayoutStepTaskHelper.BuildDisplay(entry, PayoutStepTaskHelper.GetTierAmount(key.TierId));
        }

        private long GetRemainSeconds(PayoutEntryKey key)
        {
            var entry = GetEntry(key);
            if (entry == null) return 0;
            return PayoutStepTaskHelper.GetRemainSeconds(entry);
        }

        private List<ConfPayoutTier> GetTierList()
        {
            return ConfigModule.Instance.Tables.TBPayoutTier.DataList
                .OrderBy(t => t.SortOrder)
                .ToList();
        }

        private void OpenProgressPanel(PayoutEntryKey key)
        {
            UIManager.Instance.OpenAsync<UIProgressPanel>(EUIType.EUIProgressPanel, UIOpenType.None, null, key);
        }

        private bool HasBoundAccount()
        {
            return FacadeWithdraw.GetPayType() != EPayType.None
                && !string.IsNullOrEmpty(FacadeWithdraw.GetWName())
                && !string.IsNullOrEmpty(FacadeWithdraw.GetWPhoneOrEmail());
        }

        private void DispatchEntryUpdated()
        {
            FacadeEvent.DispatchEvent(PayoutEventTypes.ENTRY_UPDATED, null);
        }

        private void DispatchStepChanged(PayoutEntryKey key)
        {
            FacadeEvent.DispatchEvent(PayoutEventTypes.STEP_CHANGED, key);
            DispatchEntryUpdated();
        }

        private void LoadData()
        {
            entries.Clear();
            string json = SPlayerPrefs.GetString(PlayerPrefDefines.payoutEntries, string.Empty);
            if (string.IsNullOrEmpty(json)) return;

            var wrapper = JsonUtility.FromJson<PayoutEntryWrapper>(json);
            if (wrapper?.items == null || wrapper.items.Length == 0) return;
            foreach (var item in wrapper.items)
            {
                if (item == null) continue;
                entries[item.Key.StorageKey] = item;
            }
        }

        private void SaveData()
        {
            var wrapper = new PayoutEntryWrapper();
            var list = new List<PayoutEntryData>(entries.Values);
            wrapper.items = list.ToArray();
            SPlayerPrefs.SetString(PlayerPrefDefines.payoutEntries, JsonUtility.ToJson(wrapper));
            SPlayerPrefs.Save();
        }

        #region GM

        private PayoutEntryKey GetGmFocusKey()
        {
            if (gmFocusKey.HasValue) return gmFocusKey.Value;
            foreach (var pair in entries)
            {
                if (pair.Value.IsStarted)
                {
                    return pair.Value.Key;
                }
            }
            return new PayoutEntryKey(FacadeWithdraw.GetPayType(), 1);
        }

        private void GM_SkipCountdown()
        {
            var key = GetGmFocusKey();
            var entry = GetOrCreateEntry(key);
            if (!entry.IsStarted) return;
            entry.stepEndTimestamp = PayoutStepTaskHelper.GetNowTimestamp();
            SaveData();
            DispatchEntryUpdated();
        }

        private void GM_CompleteDailyTask()
        {
            var key = GetGmFocusKey();
            var entry = GetOrCreateEntry(key);
            if (!entry.IsStarted) return;
            var step = PayoutStepTaskHelper.GetStepConfig(entry.curStepSn);
            if (step == null) return;
            if (step.TaskType == cfg.item.EPayoutTaskType.Ad)
                entry.taskProgress = step.TaskTarget;
            else if (step.TaskType == cfg.item.EPayoutTaskType.Level || step.TaskType == cfg.item.EPayoutTaskType.DailyLevel)
                entry.taskProgress = step.TaskTarget;
            else if (step.TaskType == cfg.item.EPayoutTaskType.Online)
            {
                entry.onlineSecondsToday = step.OnlineMinutes * 60;
                entry.taskProgress = entry.onlineSecondsToday;
            }
            else if (step.TaskType == cfg.item.EPayoutTaskType.CheckIn || step.TaskType == cfg.item.EPayoutTaskType.BankReview || step.TaskType == cfg.item.EPayoutTaskType.Queue)
                entry.accumProgress = step.TaskTarget;

            if (step.DailyLevelTarget > 0)
                entry.dailyLevelProgress = step.DailyLevelTarget;
            entry.dailyCompletedCount = PayoutStepTaskHelper.GetStepDayCount(step);
            SaveData();
            DispatchEntryUpdated();
        }

        private void GM_JumpToStep(int stepSn)
        {
            var key = GetGmFocusKey();
            var entry = GetOrCreateEntry(key);
            var step = PayoutStepTaskHelper.GetStepConfig(stepSn);
            if (step == null) return;
            PayoutStepTaskHelper.BeginStep(entry, step);
            SaveData();
            DispatchStepChanged(key);
        }

        private void GM_CompleteCurrentStep()
        {
            GM_SkipCountdown();
            GM_CompleteDailyTask();
        }

        private void GM_AdvanceToNextStep()
        {
            if (!GameDefines.ifDebug) return;

            var key = GetGmFocusKey();
            var entry = GetOrCreateEntry(key);
            if (!entry.IsStarted && !StartEntry(key))
                return;

            entry = GetOrCreateEntry(key);
            var step = PayoutStepTaskHelper.GetStepConfig(entry.curStepSn);
            if (step == null) return;

            if (step.IfTerminal)
            {
                PayoutStepTaskHelper.BeginStep(entry, step);
            }
            else
            {
                var next = PayoutStepTaskHelper.GetStepConfig(entry.curStepSn + 1);
                if (next == null) return;
                PayoutStepTaskHelper.BeginStep(entry, next);
            }

            SaveData();
            DispatchStepChanged(key);
            D.Log($"[GM] Payout 已进入步骤 {entry.curStepSn}");
        }

        public void SetGmFocusKey(PayoutEntryKey key)
        {
            gmFocusKey = key;
        }

        #endregion

        [Serializable]
        private class PayoutEntryWrapper
        {
            public PayoutEntryData[] items;
        }
    }
}
