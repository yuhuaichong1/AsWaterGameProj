using System;
using System.Collections.Generic;
using System.Globalization;
using Newtonsoft.Json;

namespace WZSDK
{
    [Serializable]
    public class WithdrawApiResponse<T>
    {
        [JsonProperty("code")]
        public int Code { get; set; }

        [JsonProperty("msg")]
        public string Msg { get; set; }

        [JsonProperty("data")]
        public T Data { get; set; }

        [JsonProperty("ext")]
        public string Ext { get; set; }
    }

    [Serializable]
    public class WalletBalanceRequest
    {
        [JsonProperty("afDeviceId")]
        public string AfDeviceId { get; set; }

        [JsonProperty("appIndex")]
        public int AppIndex { get; set; }

        [JsonProperty("currency")]
        public string Currency { get; set; }
    }

    [Serializable]
    public class WalletIncomeRequest
    {
        [JsonProperty("afDeviceId")]
        public string AfDeviceId { get; set; }

        [JsonProperty("appIndex")]
        public int AppIndex { get; set; }

        [JsonProperty("amount")]
        public double Amount { get; set; }

        [JsonProperty("currency")]
        public string Currency { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("level", NullValueHandling = NullValueHandling.Ignore)]
        public int? Level { get; set; }
    }

    [Serializable]
    public class LuckyRewardConfigRequest
    {
        [JsonProperty("afDeviceId")]
        public string AfDeviceId { get; set; }

        [JsonProperty("appIndex")]
        public int AppIndex { get; set; }

        [JsonProperty("currency")]
        public string Currency { get; set; }
    }

    [Serializable]
    public class LuckyRewardConfigData
    {
        [JsonProperty("luckyAmount")]
        public double? LuckyAmount { get; set; }

        [JsonProperty("luckySuccessRewardAmount")]
        public double? LuckySuccessRewardAmount { get; set; }

        [JsonProperty("luckySpinRewardAmount")]
        public double? LuckySpinRewardAmount { get; set; }

        [JsonProperty("currency")]
        public string Currency { get; set; }
    }

    [Serializable]
    public class ClientWithdrawRequest
    {
        [JsonProperty("orderNo", NullValueHandling = NullValueHandling.Ignore)]
        public string OrderNo { get; set; }

        [JsonProperty("afDeviceId")]
        public string AfDeviceId { get; set; }

        [JsonProperty("appIndex")]
        public int AppIndex { get; set; }

        [JsonProperty("withdrawType", NullValueHandling = NullValueHandling.Ignore)]
        public string WithdrawType { get; set; }

        [JsonProperty("receiver", NullValueHandling = NullValueHandling.Ignore)]
        public string Receiver { get; set; }

        [JsonProperty("amount", NullValueHandling = NullValueHandling.Ignore)]
        public double? Amount { get; set; }

        [JsonProperty("currency", NullValueHandling = NullValueHandling.Ignore)]
        public string Currency { get; set; }

        [JsonProperty("note", NullValueHandling = NullValueHandling.Ignore)]
        public string Note { get; set; }

        [JsonProperty("emailSubject", NullValueHandling = NullValueHandling.Ignore)]
        public string EmailSubject { get; set; }

        [JsonProperty("recipientType", NullValueHandling = NullValueHandling.Ignore)]
        public string RecipientType { get; set; }
    }

    [Serializable]
    public class UserWalletData
    {
        [JsonProperty("id")]
        public long Id { get; set; }

        [JsonProperty("afDeviceId")]
        public string AfDeviceId { get; set; }

        [JsonProperty("appIndex")]
        public int AppIndex { get; set; }

        [JsonProperty("balance")]
        public double Balance { get; set; }

        [JsonProperty("frozenBalance")]
        public double FrozenBalance { get; set; }

        [JsonProperty("totalIncomeAmount")]
        public double TotalIncomeAmount { get; set; }

        [JsonProperty("totalWithdrawAmount")]
        public double TotalWithdrawAmount { get; set; }

        [JsonProperty("currency")]
        public string Currency { get; set; }

        [JsonProperty("status")]
        public int Status { get; set; }

        [JsonProperty("payPalAccountExists")]
        public int? PayPalAccountExists { get; set; }

        [JsonProperty("payPalAccountTip")]
        public string PayPalAccountTip { get; set; }

        [JsonProperty("luckySuccessRewardAmount", NullValueHandling = NullValueHandling.Ignore)]
        public double? LuckySuccessRewardAmount { get; set; }

        [JsonProperty("luckySpinRewardAmount", NullValueHandling = NullValueHandling.Ignore)]
        public double? LuckySpinRewardAmount { get; set; }

        [JsonProperty("createTime")]
        public string CreateTime { get; set; }

        [JsonProperty("updateTime")]
        public string UpdateTime { get; set; }
    }

    [Serializable]
    public class WithdrawOrderData
    {
        [JsonProperty("orderNo")]
        public string OrderNo { get; set; }

        [JsonProperty("withdrawType")]
        public string WithdrawType { get; set; }

        [JsonProperty("orderStatus")]
        public int OrderStatus { get; set; }

        [JsonProperty("walletFrozen")]
        public int WalletFrozen { get; set; }

        [JsonProperty("senderBatchId")]
        public string SenderBatchId { get; set; }

        [JsonProperty("senderItemId")]
        public string SenderItemId { get; set; }

        [JsonProperty("payoutBatchId")]
        public string PayoutBatchId { get; set; }

        [JsonProperty("batchStatus")]
        public string BatchStatus { get; set; }

        [JsonProperty("timeCreated")]
        public string TimeCreated { get; set; }

        [JsonProperty("payoutItemId")]
        public string PayoutItemId { get; set; }

        [JsonProperty("transactionId")]
        public string TransactionId { get; set; }

        [JsonProperty("transactionStatus")]
        public string TransactionStatus { get; set; }

        [JsonProperty("receiver")]
        public string Receiver { get; set; }

        [JsonProperty("amount")]
        public string Amount { get; set; }

        [JsonProperty("currency")]
        public string Currency { get; set; }

        [JsonProperty("errorName")]
        public string ErrorName { get; set; }

        [JsonProperty("errorMessage")]
        public string ErrorMessage { get; set; }

        [JsonProperty("lastQueryTime")]
        public string LastQueryTime { get; set; }

        [JsonProperty("successTime")]
        public string SuccessTime { get; set; }

        [JsonProperty("createTime")]
        public string CreateTime { get; set; }

        [JsonProperty("updateTime")]
        public string UpdateTime { get; set; }

        [JsonProperty("clientQueuedPlaceholder", NullValueHandling = NullValueHandling.Ignore)]
        public bool ClientQueuedPlaceholder { get; set; }

        [JsonProperty("localStatusText", NullValueHandling = NullValueHandling.Ignore)]
        public string LocalStatusText { get; set; }

        [JsonProperty("clientSortAnchorTime", NullValueHandling = NullValueHandling.Ignore)]
        public string ClientSortAnchorTime { get; set; }

        public bool IsFirstAdWithdraw =>
            string.Equals(WithdrawType, WithdrawConstants.WithdrawTypeFirstAd, StringComparison.OrdinalIgnoreCase);

        public bool IsWalletWithdraw =>
            string.Equals(WithdrawType, WithdrawConstants.WithdrawTypeWallet, StringComparison.OrdinalIgnoreCase);

        public bool CanClientRetry => OrderStatus == 2 || OrderStatus == 4;

        public bool RequiresNewReceiver => OrderStatus == 2;

        public bool RequiresReceiverInput
        {
            get
            {
                if (OrderStatus == 2)
                    return true;

                if (!string.IsNullOrEmpty(ErrorMessage))
                {
                    if (ErrorMessage.Contains("PayPal收款账号不能为空")
                        || ErrorMessage.Contains("PayPal收款账号不存在"))
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        public bool IsPending => ClientQueuedPlaceholder || OrderStatus == 0 || OrderStatus == 3;

        public double GetAmountValue()
        {
            if (double.TryParse(Amount, NumberStyles.Float, CultureInfo.InvariantCulture, out double value))
            {
                return value;
            }

            return 0d;
        }

        public string GetSortAnchorTimeText()
        {
            if (!string.IsNullOrEmpty(ClientSortAnchorTime))
                return ClientSortAnchorTime;

            if (!string.IsNullOrEmpty(CreateTime))
                return CreateTime;

            if (!string.IsNullOrEmpty(TimeCreated))
                return TimeCreated;

            if (!string.IsNullOrEmpty(UpdateTime))
                return UpdateTime;

            return SuccessTime;
        }

        public bool TryGetSortAnchorTime(out DateTimeOffset time)
        {
            time = DateTimeOffset.MinValue;

            string[] timeTexts =
            {
                ClientSortAnchorTime,
                CreateTime,
                TimeCreated,
                UpdateTime,
                SuccessTime
            };

            for (int i = 0; i < timeTexts.Length; i++)
            {
                if (DateTimeOffset.TryParse(timeTexts[i], CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out time))
                    return true;
            }

            return false;
        }

        public string GetBestTimeText()
        {
            if (!string.IsNullOrEmpty(SuccessTime))
                return SuccessTime;

            if (!string.IsNullOrEmpty(UpdateTime))
                return UpdateTime;

            if (!string.IsNullOrEmpty(CreateTime))
                return CreateTime;

            return TimeCreated;
        }

        public string GetStatusText()
        {
            if (!string.IsNullOrEmpty(LocalStatusText))
                return LocalStatusText;

            switch (OrderStatus)
            {
                case 0: return "\u672a\u6253\u6b3e";
                case 1: return "\u6253\u6b3e\u6210\u529f";
                case 2: return "\u6536\u6b3e\u8d26\u6237\u4e0d\u5b58\u5728";
                case 3: return "\u6253\u6b3e\u4e2d";
                case 4: return "\u63d0\u73b0\u5931\u8d25";
                case 5: return "PayPal\u4f59\u989d\u4e0d\u8db3";
                default: return "\u672a\u77e5\u72b6\u6001";
            }
        }
    }

    public static class WithdrawConstants
    {
        public const string CurrencyUsd = "USD";
        public const string WalletIncomeTypeNormal = "NORMAL";
        public const string WalletIncomeTypeLucky = "LUCKY";
        public const string WithdrawTypeFirstAd = "FIRST_AD";
        public const string WithdrawTypeWallet = "WALLET";
        public const string RecipientTypeEmail = "EMAIL";
    }
}
