using System;
using System.Collections.Generic;
using System.Linq;
using cfg;
using UnityEngine;
using UnityEngine.UI;

namespace XrCode
{
    public class PayoutModule : BaseModule
    {
        private readonly Dictionary<string, PayoutEntryData> entries = new Dictionary<string, PayoutEntryData>();
        private readonly Dictionary<string, bool> continueStateCache = new Dictionary<string, bool>();
        private readonly Dictionary<string, long> remainSecondsCache = new Dictionary<string, long>();
        private float onlineTick;
        private PayoutEntryKey? gmFocusKey;

        private float curTipTarget;
        private int curTipTargetId;

        private Dictionary<int, ConfMoneyInterval> MIData;
        private List<float> TargetInterval;

        protected override void OnLoad()
        {
            RegisterFacade();
            LoadData();

            FacadeEvent.AddEventListener(PayoutEventTypes.MONEY_UPDATED, OnMoneyUpdatedEvent);
            RegisetUpdateObj();
            MoneyIntervalInit();
        }

        protected override void OnDispose()
        {

            FacadeEvent.RemoveEventListener(PayoutEventTypes.MONEY_UPDATED, OnMoneyUpdatedEvent);
            UnregisterFacade();
        }

        protected override void OnUpdate()
        {
            if (!GameDefines.UsePayoutV2) return;
            onlineTick += Time.unscaledDeltaTime;
            if (onlineTick < 1f) return;
            int seconds = (int)onlineTick;
            onlineTick -= seconds;

            bool dataChanged = false;
            bool continueStateChanged = false;
            foreach (var pair in entries)
            {
                var entry = pair.Value;
                if (!entry.IsStarted) continue;
                var step = PayoutStepTaskHelper.GetStepConfig(entry.curStepSn);
                if (step == null) continue;

                bool wasCanContinue = continueStateCache.TryGetValue(pair.Key, out bool cached) && cached;
                PayoutStepTaskHelper.OnOnlineSeconds(entry, step, seconds);
                dataChanged = true;

                float tierAmount = PayoutStepTaskHelper.GetTierAmount(entry.tierId);
                bool nowCanContinue = PayoutStepTaskHelper.CanContinue(entry, tierAmount);
                continueStateCache[pair.Key] = nowCanContinue;

                long remain = PayoutStepTaskHelper.GetRemainSeconds(entry);
                bool countdownJustEnded = remainSecondsCache.TryGetValue(pair.Key, out long prevRemain)
                    && prevRemain > 0
                    && remain <= 0;
                remainSecondsCache[pair.Key] = remain;

                if (wasCanContinue != nowCanContinue || countdownJustEnded)
                    continueStateChanged = true;
            }

            if (dataChanged)
                SaveData();
            if (continueStateChanged)
                DispatchEntryUpdated();
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
            FacadePayout.MarkPanelAcknowledged = MarkPanelAcknowledged;
            FacadePayout.HasBoundAccount = HasBoundAccount;
            FacadePayout.NotifyMoneyUpdated = NotifyMoneyUpdated;
            FacadePayout.GM_SkipCountdown = GM_SkipCountdown;
            FacadePayout.GM_ShortenStepTimeMinutes = GM_ShortenStepTimeMinutes;
            FacadePayout.GM_JumpToStep = GM_JumpToStep;
            FacadePayout.IfShowTip = IfShowTip;
            FacadePayout.SetCMDText = SetCMDText;
            FacadePayout.GetLuckyRewardAmount = GetLuckyRewardAmount;
            FacadePayout.GetLevelComplatedAmount = GetLevelComplatedAmount;
            FacadePayout.GetLuckySpinAmount = GetLuckySpinAmount;
            FacadePayout.GetEliminationAmount += GetEliminationAmount;
            FacadePayout.GetAdBottleAmount += GetAdBottleAmount;
            FacadePayout.AddWithdrawOrder_Ad += AddWithdrawOrder;
            FacadePayout.AddWithdrawOrder_Level += AddWithdrawOrder_Level;
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
            FacadePayout.MarkPanelAcknowledged = null;
            FacadePayout.HasBoundAccount = null;
            FacadePayout.NotifyMoneyUpdated = null;
            FacadePayout.GM_SkipCountdown = null;
            FacadePayout.GM_ShortenStepTimeMinutes = null;
            FacadePayout.GM_JumpToStep = null;
            FacadePayout.IfShowTip = null;
            FacadePayout.SetCMDText = null;
            FacadePayout.GetLuckyRewardAmount = null;
            FacadePayout.GetLevelComplatedAmount = null;
            FacadePayout.GetLuckySpinAmount = null;
            FacadePayout.GetEliminationAmount = null;
            FacadePayout.GetAdBottleAmount = null;
            FacadePayout.AddWithdrawOrder_Ad = null;
            FacadePayout.AddWithdrawOrder_Level = null;
        }

        public void AddWithdrawOrder_Level(int count = 1)
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
                RefreshStepCaches();
                DispatchEntryUpdated();
            }
        }

        private void AddWithdrawOrder()
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
                RefreshStepCaches();
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
            float amount = PayoutStepTaskHelper.GetTierAmount(key.TierId);
            if (FacadePlayer.GetMoney() < amount)
                return false;

            if (GameDefines.PayoutSkipAmountCheck)
                return true;

            return HasBoundAccount();
        }

        private bool StartEntry(PayoutEntryKey key)
        {
            var entry = GetOrCreateEntry(key);
            if (entry.IsStarted)
            {
                AcknowledgeListItem(entry);
                return true;
            }

            if (!CanStart(key)) return false;

            float amount = PayoutStepTaskHelper.GetTierAmount(key.TierId);
            if (FacadePlayer.GetMoney() < amount)
                return false;

            FacadePlayer.AddMoney(-amount);
            FacadeGamePlay.SetCurMoneyShow?.Invoke();
            NotifyMoneyUpdated();

            var firstStep = PayoutStepTaskHelper.GetStepConfig(1);
            if (firstStep == null) return false;

            FacadeWithdraw.SetPayType(key.Channel);
            PayoutStepTaskHelper.BeginStep(entry, firstStep);
            entry.panelAcknowledged = true;
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

        private void MarkPanelAcknowledged(PayoutEntryKey key)
        {
            var entry = GetEntry(key);
            if (entry == null || !entry.IsStarted) return;
            AcknowledgeListItem(entry);
        }

        /// <summary>列表项进入任务展示态（Cash Out / Continue 后立即生效，不依赖关闭进度面板）。</summary>
        private void AcknowledgeListItem(PayoutEntryData entry)
        {
            if (entry == null || !entry.IsStarted || entry.panelAcknowledged) return;
            entry.panelAcknowledged = true;
            SaveData();
            DispatchEntryUpdated();
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

            entry.panelAcknowledged = true;
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
            RefreshStepCaches();
            FacadeEvent.DispatchEvent(PayoutEventTypes.STEP_CHANGED, key);
            DispatchEntryUpdated();
        }

        private void LoadData()
        {
            curTipTargetId = SPlayerPrefs.GetInt(PlayerPrefDefines.curTipTargetId, 1);
            curTipTarget = SPlayerPrefs.GetFloat(PlayerPrefDefines.curTipTarget, PayoutStepTaskHelper.GetTierAmount(curTipTargetId));

            entries.Clear();
            string json = SPlayerPrefs.GetString(PlayerPrefDefines.payoutEntries, string.Empty);
            if (string.IsNullOrEmpty(json)) return;

            PayoutEntryWrapper wrapper = JsonUtility.FromJson<PayoutEntryWrapper>(json);
            if (wrapper?.items == null || wrapper.items.Length == 0) return;
            bool repairedAck = false;
            foreach (PayoutEntryData item in wrapper.items)
            {
                if (item == null) continue;
                // 兼容旧存档：已开始但未确认面板的订单，直接视为列表已刷新
                if (item.IsStarted && !item.panelAcknowledged)
                {
                    item.panelAcknowledged = true;
                    repairedAck = true;
                }

                entries[item.Key.StorageKey] = item;
            }
            if (repairedAck)
                SaveData();
            RefreshStepCaches();
        }

        private void RefreshRemainSecondsCache()
        {
            remainSecondsCache.Clear();
            foreach (var pair in entries)
            {
                var entry = pair.Value;
                if (!entry.IsStarted) continue;
                remainSecondsCache[pair.Key] = PayoutStepTaskHelper.GetRemainSeconds(entry);
            }
        }

        private void RefreshContinueStateCache()
        {
            continueStateCache.Clear();
            foreach (var pair in entries)
            {
                var entry = pair.Value;
                if (!entry.IsStarted) continue;
                float tierAmount = PayoutStepTaskHelper.GetTierAmount(entry.tierId);
                continueStateCache[pair.Key] = PayoutStepTaskHelper.CanContinue(entry, tierAmount);
            }
        }

        private void RefreshStepCaches()
        {
            RefreshContinueStateCache();
            RefreshRemainSecondsCache();
        }

        private void SaveData()
        {
            var wrapper = new PayoutEntryWrapper();
            var list = new List<PayoutEntryData>(entries.Values);
            wrapper.items = list.ToArray();
            SPlayerPrefs.SetString(PlayerPrefDefines.payoutEntries, JsonUtility.ToJson(wrapper));
            SPlayerPrefs.Save();

            SetCMDText();
        }

        private void IfShowTip(double money)
        {
            if (curTipTarget == 0)
                return;

            money = (float)money;
            float tempTarget = curTipTarget;
            if (money >= curTipTarget)
            {
                Sprite icon = FacadePayType.GetPayItems()[0].picture;
                foreach (PayoutEntryData item in entries.Values)
                {
                    float itemTarget = PayoutStepTaskHelper.GetTierAmount(item.tierId);
                    if (itemTarget == curTipTarget)
                    {
                        icon = FacadePayType.GetPayItemPicture((EPayType)item.channel);
                        break;
                    }
                }

                curTipTargetId += 1;
                curTipTarget = PayoutStepTaskHelper.GetTierAmount(curTipTargetId);
                SPlayerPrefs.SetInt(PlayerPrefDefines.curTipTargetId, curTipTargetId);
                SPlayerPrefs.SetFloat(PlayerPrefDefines.curTipTarget, curTipTarget);
                SPlayerPrefs.Save();

                UIManager.Instance.OpenAsync<UIWithdrawTip>(EUIType.EUIWithdrawTip, UIOpenType.None, null, tempTarget, icon);
            }
        }

        private void SetCMDText()
        {
            List<PayoutEntryData> peds = entries.Values.ToList();
            peds.Reverse();
            for(int i = 0; i < peds.Count; i++)
            {
                PayoutEntryKey entryKey = peds[i].Key;
                if (!CanContinue(entryKey))
                {
                    FacadeGamePlay.SetWithdrawalTip?.Invoke(GetStepDisplay(entryKey).CurTask);
                    return;
                }
            }

            FacadeGamePlay.SetWithdrawalTip?.Invoke(string.Format(FacadeLanguage.GetText("10236"), curTipTarget - (float)FacadePlayer.GetMoney(), curTipTarget));
        }

        private void MoneyIntervalInit()
        {
            MIData = ConfigModule.Instance.Tables.TBMoneyInterval.DataMap;
            TargetInterval = new List<float>();
            foreach (ConfMoneyInterval item in MIData.Values)
            {
                TargetInterval.Add(item.MoneyMax);
            }
            TargetInterval.Add(0);
        }

        private float GetLuckyRewardAmount()
        {
            int id = GetMIDataId();
            return UnityEngine.Random.Range(MIData[id].LRMin, MIData[id].LRMax);
        }

        private float GetLevelComplatedAmount()
        {
            int id = GetMIDataId();
            return UnityEngine.Random.Range(MIData[id].LSMin, MIData[id].LSMin);
        }

        private float GetLuckySpinAmount()
        {
            int id = GetMIDataId();
            return MIData[id].LSReward;
        }

        private float GetEliminationAmount()
        {
            int id = GetMIDataId();
            return UnityEngine.Random.Range(MIData[id].SEMin, MIData[id].SEMax);
        }

        private float GetAdBottleAmount()
        {
            int id = GetMIDataId();
            return UnityEngine.Random.Range(MIData[id].AdBottleMin, MIData[id].AdBottleMax);
        }

        private int GetMIDataId()
        {
            float remain = curTipTarget - (float)FacadePlayer.GetMoney();
            if (remain < 0)
                remain = 0;
            int id = TargetInterval.GetRangeIndex(remain);
            id = MIData.Count - id - 1;

            if(id == -1) id = MIData.Count - 1;
            //if (id == -1) id = 0;
            return id;
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
            RefreshStepCaches();
            DispatchEntryUpdated();
        }

        private void GM_ShortenStepTimeMinutes(int minutes)
        {
            if (minutes <= 0) return;

            var key = GetGmFocusKey();
            var entry = GetOrCreateEntry(key);
            if (!entry.IsStarted) return;

            long now = PayoutStepTaskHelper.GetNowTimestamp();
            entry.stepEndTimestamp = Math.Max(now, entry.stepEndTimestamp - minutes * 60L);
            SaveData();
            RefreshStepCaches();
            DispatchEntryUpdated();
            D.Log($"[GM] Payout 倒计时已缩短 {minutes} 分钟");
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
