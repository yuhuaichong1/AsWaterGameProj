using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;

namespace WZSDK
{
    public static class WithdrawApiService
    {
        private const string EditorTotalAdCountPrefsKey = "td_total_ad_reward_count";
        private const int RequestTimeoutSeconds = 35;
        private const string FirstAdQueuedStatusText = "\u6392\u961f\u4e2d";
        private const string WalletReviewStatusText = "\u5ba1\u6838\u4e2d";
        private const string LuckyRewardConfigCachePrefsKey = "wallet_lucky_reward_config_cache";
        private const float DefaultRewardedAdWalletIncomeAmount = 1f;
        private const string WalletReportLogPrefix = "[WalletReport]";
        private const string PaymentNoticeLogPrefix = "[PaymentNotice]";
        private const string EditorServerTestLogPrefix = "[EditorServerTest]";

        private static readonly JsonSerializerSettings JsonSettings = new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore
        };

#if UNITY_EDITOR
        private static bool editorAfDeviceInitialized;
        private static string editorInitializedAfDeviceId;
        private static int editorInitializedAppIndex;
        private static int editorSyncedAdCount;
#endif

        public static UserWalletData GetCachedWallet()
        {
            string json = SPlayerPrefs.GetString(FacadePlayerPrefExtend.WalletBalanceCache, string.Empty);
            if (string.IsNullOrEmpty(json))
                return null;

            try
            {
                return JsonConvert.DeserializeObject<UserWalletData>(json);
            }
            catch (Exception e)
            {
                Debug.LogError($"钱包缓存解析失败: {e.Message}");
                return null;
            }
        }

        public static List<WithdrawOrderData> GetCachedOrders()
        {
            string json = SPlayerPrefs.GetString(FacadePlayerPrefExtend.WalletWithdrawOrdersCache, string.Empty);
            if (string.IsNullOrEmpty(json))
                return new List<WithdrawOrderData>();

            try
            {
                List<WithdrawOrderData> orders = JsonConvert.DeserializeObject<List<WithdrawOrderData>>(json);
                return orders ?? new List<WithdrawOrderData>();
            }
            catch (Exception e)
            {
                Debug.LogError($"提现订单缓存解析失败: {e.Message}");
                return new List<WithdrawOrderData>();
            }
        }

        public static bool TryGetFirstAdOrderCreateTime(out DateTimeOffset time)
        {
            time = DateTimeOffset.MinValue;

            List<WithdrawOrderData> orders = GetCachedOrders();
            if (orders == null || orders.Count == 0)
                return false;

            WithdrawOrderData order = orders.FirstOrDefault(item => item != null && item.IsFirstAdWithdraw && item.IsPending)
                ?? orders.FirstOrDefault(item => item != null && item.IsFirstAdWithdraw);

            return order != null && order.TryGetSortAnchorTime(out time);
        }

        public static double GetCachedWalletBalance()
        {
            UserWalletData wallet = GetCachedWallet();
            return wallet != null ? wallet.Balance : GameDefines.LuckyChestPlaceholderCoin;
        }

        public static LuckyRewardConfigData GetCachedLuckyRewardConfig()
        {
            string json = SPlayerPrefs.GetString(LuckyRewardConfigCachePrefsKey, string.Empty);
            if (string.IsNullOrEmpty(json))
                return null;

            try
            {
                return JsonConvert.DeserializeObject<LuckyRewardConfigData>(json);
            }
            catch (Exception e)
            {
                Debug.LogError($"幸运钱包奖励配置缓存解析失败: {e.Message}");
                return null;
            }
        }

        public static float GetCachedLuckySuccessRewardAmount(float fallbackAmount)
        {
            LuckyRewardConfigData config = GetCachedLuckyRewardConfig();
            double? rewardAmount = config != null
                ? (config.LuckyAmount ?? config.LuckySuccessRewardAmount)
                : GetCachedWallet()?.LuckySuccessRewardAmount;
            return ResolveLuckyRewardAmount(rewardAmount, fallbackAmount);
        }

        public static float GetCachedLuckySpinRewardAmount(float fallbackAmount)
        {
            LuckyRewardConfigData config = GetCachedLuckyRewardConfig();
            double? rewardAmount = config != null ? config.LuckySpinRewardAmount : GetCachedWallet()?.LuckySpinRewardAmount;
            return ResolveLuckyRewardAmount(rewardAmount, fallbackAmount);
        }

        public static string GetCachedPayPalTip()
        {
            return GetCachedWallet()?.PayPalAccountTip;
        }

        public static int? GetCachedPayPalAccountExists()
        {
            return GetCachedWallet()?.PayPalAccountExists;
        }

        public static void LogPaymentNotice(string source, string message, LogType logType = LogType.Warning)
        {
            if (string.IsNullOrEmpty(message))
                return;

            string context = string.IsNullOrEmpty(source) ? "Payment" : source;
            string formattedMessage = $"{PaymentNoticeLogPrefix} {context}: {message}";

            switch (logType)
            {
                case LogType.Error:
                case LogType.Exception:
                    Debug.LogError(formattedMessage);
                    break;
                case LogType.Log:
                    Debug.Log(formattedMessage);
                    break;
                default:
                    Debug.LogWarning(formattedMessage);
                    break;
            }
        }

        private static void LogEditorServerRequest(string requestType, string method, string url, string details = null)
        {
#if UNITY_EDITOR
            if (string.IsNullOrEmpty(url))
                return;

            string suffix = string.IsNullOrEmpty(details) ? string.Empty : $", {details}";
            Debug.Log($"{EditorServerTestLogPrefix} request {requestType}: method={method}, url={url}{suffix}");
#endif
        }

        private static void LogEditorServerResponse(string requestType, UnityWebRequest request, string responseText, bool success, string extra = null)
        {
#if UNITY_EDITOR
            if (request == null)
                return;

            string summary = TrimEditorResponseSummary(responseText);
            string suffix = string.IsNullOrEmpty(extra) ? string.Empty : $", {extra}";
            string message = $"{EditorServerTestLogPrefix} response {requestType}: method={request.method}, url={request.url}, success={success}, code={request.responseCode}{suffix}, body={summary}";
            if (success)
                Debug.Log(message);
            else
                Debug.LogWarning(message);
#endif
        }

        private static string TrimEditorResponseSummary(string responseText)
        {
#if UNITY_EDITOR
            if (string.IsNullOrEmpty(responseText))
                return "<empty>";

            const int MaxLength = 280;
            return responseText.Length <= MaxLength
                ? responseText
                : responseText.Substring(0, MaxLength) + "...";
#else
            return string.Empty;
#endif
        }

        public static void AddWalletIncome(float amount, bool showError = false)
        {
            AddWalletIncomeInternal(amount, null, null, showError);
        }

        public static void AddWalletIncome(float amount, string incomeTypeOverride, bool showError = false)
        {
            AddWalletIncomeInternal(amount, incomeTypeOverride, null, showError);
        }

        public static void AddWalletIncome(float amount, string incomeTypeOverride, int incomeLevelOverride, bool showError = false)
        {
            AddWalletIncomeInternal(amount, incomeTypeOverride, incomeLevelOverride, showError);
        }

        private static void AddWalletIncomeInternal(float amount, string incomeTypeOverride, int? incomeLevelOverride, bool showError = false)
        {
            if (amount <= 0f)
            {
                Debug.Log($"{WalletReportLogPrefix} skip wallet income report: amount={amount:F6}");
                return;
            }

            if (!CanReportWalletIncomeAtCurrentLevel())
            {
                Debug.Log($"{WalletReportLogPrefix} skip wallet income report: currentLv={ResolveCurrentLevelForWalletIncome()}, unlockLv={GameDefines.LuckyWalletUnlockLevel}, amount={amount:F6}, typeOverride={incomeTypeOverride ?? "AUTO"}");
                return;
            }

            GameDefines.LuckyChestPlaceholderCoin += amount;

            if (GameManagerWZ.instance == null)
            {
                Debug.Log($"{WalletReportLogPrefix} skip wallet income coroutine: GameManagerWZ is null, amount={amount:F6}, typeOverride={incomeTypeOverride ?? "AUTO"}, levelOverride={(incomeLevelOverride.HasValue ? incomeLevelOverride.Value.ToString() : "AUTO")}");
                return;
            }

            Debug.Log($"{WalletReportLogPrefix} queue wallet income report: amount={amount:F6}, typeOverride={incomeTypeOverride ?? "AUTO"}, levelOverride={(incomeLevelOverride.HasValue ? incomeLevelOverride.Value.ToString() : "AUTO")}, currentLv={ResolveCurrentLevelForWalletIncome()}");

            GameManagerWZ.instance.StartCoroutine(ReportWalletIncomeInternal(amount, wallet =>
            {
                CacheWallet(wallet);
                Debug.Log($"{WalletReportLogPrefix} wallet income report success: amount={amount:F6}, balance={wallet?.Balance ?? 0d:F6}, typeOverride={incomeTypeOverride ?? "AUTO"}");
            }, error =>
            {
                Debug.LogError($"钱包收益上报失败: {error}");
                Debug.LogWarning($"{WalletReportLogPrefix} wallet income report failed: amount={amount:F6}, typeOverride={incomeTypeOverride ?? "AUTO"}, error={error}");
                if (showError)
                    LogPaymentNotice("WalletIncome", error, LogType.Warning);
            }, WithdrawConstants.CurrencyUsd, incomeTypeOverride, incomeLevelOverride));
        }

 

        public static void ReportRewardedAdWalletIncomeIfNeeded(double adRevenue, bool showError = false)
        {
            if (!ShouldReportRewardedAdWalletIncome())
            {
                Debug.Log($"{WalletReportLogPrefix} skip rewarded ad wallet flow: currentLv={ResolveCurrentLevelForWalletIncome()}, unlockLv={GameDefines.LuckyWalletUnlockLevel}, adRevenue={adRevenue:F6}");
                return;
            }

            float cachedAmount = GetCachedLuckySuccessRewardAmount(DefaultRewardedAdWalletIncomeAmount);
            Debug.Log($"{WalletReportLogPrefix} rewarded ad wallet flow uses cached lucky amount: amount={cachedAmount:F6}, adRevenue={adRevenue:F6}");
            AddWalletIncome(cachedAmount, showError);
        }

        public static void ReportAfAdCountIncomeIfNeeded(double adRevenue)
        {
            ReportAfAdCountIncomeIfNeeded(adRevenue, 0f);
        }

        public static void ReportAfAdCountIncomeIfNeeded(double adRevenue, float incomeAmount)
        {
            if (!CanReportWalletIncomeAtCurrentLevel())
            {
                Debug.Log($"{WalletReportLogPrefix} skip af/ad/count report: currentLv={ResolveCurrentLevelForWalletIncome()}, unlockLv={GameDefines.LuckyWalletUnlockLevel}, adRevenue={adRevenue:F4}");
                return;
            }

            GameManagerWZ gameManager = GameManagerWZ.instance;
            if (gameManager == null)
            {
                Debug.Log($"{WalletReportLogPrefix} skip af/ad/count coroutine: GameManagerWZ is null, adRevenue={adRevenue:F4}");
                return;
            }

            Debug.Log($"{WalletReportLogPrefix} queue af/ad/count report: adRevenue={adRevenue:F4}, currentLv={ResolveCurrentLevelForWalletIncome()}");

            gameManager.StartCoroutine(PostAfAdCountIncomeCoroutine(adRevenue));
        }

        public static IEnumerator QueryLuckySuccessRewardAmount(float fallbackAmount, Action<float> onSuccess, Action<string> onError = null)
        {
            yield return QueryLuckyRewardAmount(config => config != null ? (config.LuckyAmount ?? config.LuckySuccessRewardAmount) : null, fallbackAmount, onSuccess, onError);
        }

        public static IEnumerator QueryLuckySpinRewardAmount(float fallbackAmount, Action<float> onSuccess, Action<string> onError = null)
        {
            yield return QueryLuckyRewardAmount(config => config != null ? config.LuckySpinRewardAmount : null, fallbackAmount, onSuccess, onError);
        }

        public static IEnumerator QueryLuckyRewardConfig(Action<LuckyRewardConfigData> onSuccess, Action<string> onError = null, string currency = WithdrawConstants.CurrencyUsd, int? luckyLevelOverride = null)
        {
            if (string.IsNullOrEmpty(GameDefines.LuckyRewardConfigApiPath))
            {
                onSuccess?.Invoke(GetCachedLuckyRewardConfig());
                yield break;
            }

            if (!TryBuildIdentity(out string afDeviceId, out int appIndex, out string identityError))
            {
                onError?.Invoke(identityError);
                yield break;
            }

            bool ensureSuccess = false;
            string ensureError = null;
            yield return EnsureEditorAfDeviceInitialized(afDeviceId, appIndex, success => ensureSuccess = success, error => ensureError = error);
            if (!ensureSuccess)
            {
                onError?.Invoke(ensureError);
                yield break;
            }

            string url = BuildApiUrl(GameDefines.LuckyRewardConfigApiPath, null);
            if (string.IsNullOrEmpty(url))
            {
                onError?.Invoke("鎻愮幇鎺ュ彛鍦板潃鏈厤缃?");
                yield break;
            }

            WWWForm form = new WWWForm();
            form.AddField("afDeviceId", afDeviceId);
            form.AddField("appIndex", appIndex);
            int luckyLevel = luckyLevelOverride ?? ResolveLuckyRewardConfigLevel();
            form.AddField("level", luckyLevel);
            Debug.Log($"Lucky reward config request: url={url}, afDeviceId={afDeviceId}, appIndex={appIndex}, level={luckyLevel}, currency={currency}");

            using (UnityWebRequest request = UnityWebRequest.Post(url, form))
            {
                request.timeout = RequestTimeoutSeconds;
                request.downloadHandler = new DownloadHandlerBuffer();
                LogEditorServerRequest("luckyRewardConfig", UnityWebRequest.kHttpVerbPOST, url,
                    $"afDeviceId={afDeviceId}, appIndex={appIndex}, level={luckyLevel}, currency={currency}");

                yield return request.SendWebRequest();
                string rawResponseText = request.downloadHandler != null ? request.downloadHandler.text : string.Empty;
                Debug.Log($"Lucky reward config raw response: {rawResponseText}");
                HandleResponse<LuckyRewardConfigData>(request, config =>
                {
                    CacheLuckyRewardConfig(config);
                    Debug.Log($"Lucky reward config success: luckyAmount={config?.LuckyAmount}, luckySuccessRewardAmount={config?.LuckySuccessRewardAmount}, luckySpinRewardAmount={config?.LuckySpinRewardAmount}, currency={config?.Currency}");
                    onSuccess?.Invoke(config);
                }, error =>
                {
                    Debug.LogWarning($"Lucky reward config failed: {error}");
                    onError?.Invoke(error);
                });
            }
        }

        public static IEnumerator QueryWalletBalance(Action<UserWalletData> onSuccess, Action<string> onError = null, string currency = WithdrawConstants.CurrencyUsd)
        {
            if (!TryBuildIdentity(out string afDeviceId, out int appIndex, out string identityError))
            {
                onError?.Invoke(identityError);
                yield break;
            }

            bool ensureSuccess = false;
            string ensureError = null;
            yield return EnsureEditorAfDeviceInitialized(afDeviceId, appIndex, success => ensureSuccess = success, error => ensureError = error);
            if (!ensureSuccess)
            {
                onError?.Invoke(ensureError);
                yield break;
            }

            WalletBalanceRequest requestBody = new WalletBalanceRequest
            {
                AfDeviceId = afDeviceId,
                AppIndex = appIndex,
                Currency = string.IsNullOrEmpty(currency) ? WithdrawConstants.CurrencyUsd : currency
            };

            yield return SendJsonRequest<UserWalletData>(UnityWebRequest.kHttpVerbPOST, GameDefines.WalletBalanceApiPath, requestBody, wallet =>
            {
                CacheWallet(wallet);
                onSuccess?.Invoke(wallet);
            }, onError);
        }

        private static IEnumerator QueryLuckyRewardAmount(Func<LuckyRewardConfigData, double?> selector, float fallbackAmount, Action<float> onSuccess, Action<string> onError = null)
        {
            float resolvedFallbackAmount = ResolveLuckyRewardAmount(null, fallbackAmount);
            bool successInvoked = false;

            yield return QueryLuckyRewardConfig(config =>
            {
                successInvoked = true;
                onSuccess?.Invoke(ResolveLuckyRewardAmount(selector?.Invoke(config), resolvedFallbackAmount));
            }, error =>
            {
                if (!successInvoked)
                    onSuccess?.Invoke(resolvedFallbackAmount);

                onError?.Invoke(error);
            });
        }

        private static IEnumerator PostAfAdCountIncomeCoroutine(double adRevenue)
        {
            if (!TryBuildIdentity(out string afDeviceId, out int appIndex, out string identityError))
            {
                Debug.LogWarning($"AF ad count report skipped: {identityError}");
                yield break;
            }

            bool ensureSuccess = false;
            string ensureError = null;
            yield return EnsureEditorAfDeviceInitialized(afDeviceId, appIndex, success => ensureSuccess = success, error => ensureError = error);
            if (!ensureSuccess)
            {
                Debug.LogWarning($"AF ad count report skipped: {ensureError}");
                yield break;
            }

            string url = BuildPushApiUrl(GameDefines.AfAdCountApiPath);
            if (string.IsNullOrEmpty(url))
            {
                Debug.LogWarning("AF ad count report skipped: api url is empty.");
                yield break;
            }

            WWWForm form = new WWWForm();
            form.AddField("afDeviceId", afDeviceId);
            form.AddField("appIndex", appIndex);
            string formattedAdRevenue = Math.Round(adRevenue, 4, MidpointRounding.AwayFromZero).ToString("F4", CultureInfo.InvariantCulture);
            form.AddField("adRevenue", formattedAdRevenue);
            form.AddField("incomeAmount", formattedAdRevenue);

            Debug.Log($"{WalletReportLogPrefix} send af/ad/count: appIndex={appIndex}, adRevenue={formattedAdRevenue}, incomeAmount={formattedAdRevenue}");

            using (UnityWebRequest request = UnityWebRequest.Post(url, form))
            {
                request.timeout = RequestTimeoutSeconds;
                LogEditorServerRequest("afAdCount", UnityWebRequest.kHttpVerbPOST, url,
                    $"afDeviceId={afDeviceId}, appIndex={appIndex}, adRevenue={formattedAdRevenue}, incomeAmount={formattedAdRevenue}");
                yield return request.SendWebRequest();

                string responseText = request.downloadHandler != null ? request.downloadHandler.text : string.Empty;
                if (request.result == UnityWebRequest.Result.Success)
                {
                    LogEditorServerResponse("afAdCount", request, responseText, true);
                    Debug.Log($"{WalletReportLogPrefix} af/ad/count success: appIndex={appIndex}, adRevenue={formattedAdRevenue}");
                    yield break;
                }

                LogEditorServerResponse("afAdCount", request, responseText, false, $"error={request.error}");
                Debug.LogWarning($"AF ad count report failed: {ExtractErrorMessage(responseText, request.error)}");
            }
        }

        public static IEnumerator ReportWalletIncome(double amount, Action<UserWalletData> onSuccess, Action<string> onError = null, string currency = WithdrawConstants.CurrencyUsd, string incomeTypeOverride = null)
        {
            yield return ReportWalletIncomeInternal(amount, onSuccess, onError, currency, incomeTypeOverride, null);
        }

        private static IEnumerator ReportWalletIncomeInternal(double amount, Action<UserWalletData> onSuccess, Action<string> onError, string currency, string incomeTypeOverride, int? incomeLevelOverride)
        {
            if (amount <= 0d)
            {
                onError?.Invoke("新增金额必须大于0");
                yield break;
            }

            if (!TryBuildIdentity(out string afDeviceId, out int appIndex, out string identityError))
            {
                onError?.Invoke(identityError);
                yield break;
            }

            bool ensureSuccess = false;
            string ensureError = null;
            yield return EnsureEditorAfDeviceInitialized(afDeviceId, appIndex, success => ensureSuccess = success, error => ensureError = error);
            if (!ensureSuccess)
            {
                onError?.Invoke(ensureError);
                yield break;
            }

            ResolveWalletIncomeRequestMetadata(incomeTypeOverride, incomeLevelOverride, out string incomeType, out int? incomeLevel);
            WalletIncomeRequest requestBody = new WalletIncomeRequest
            {
                AfDeviceId = afDeviceId,
                AppIndex = appIndex,
                Amount = RoundAmount(amount),
                Currency = string.IsNullOrEmpty(currency) ? WithdrawConstants.CurrencyUsd : currency,
                Type = incomeType,
                Level = incomeLevel
            };

            Debug.Log($"{WalletReportLogPrefix} send wallet/income: appIndex={appIndex}, amount={requestBody.Amount:F6}, type={requestBody.Type}, level={(requestBody.Level.HasValue ? requestBody.Level.Value.ToString() : "null")}, currency={requestBody.Currency}");

            yield return SendJsonRequest<UserWalletData>(UnityWebRequest.kHttpVerbPOST, GameDefines.WalletIncomeApiPath, requestBody, wallet =>
            {
                CacheWallet(wallet);
                Debug.Log($"{WalletReportLogPrefix} wallet/income success: amount={requestBody.Amount:F6}, type={requestBody.Type}, level={(requestBody.Level.HasValue ? requestBody.Level.Value.ToString() : "null")}, balance={wallet?.Balance ?? 0d:F6}");
                onSuccess?.Invoke(wallet);
            }, onError);
        }

        public static IEnumerator SubmitWithdraw(ClientWithdrawRequest requestBody, Action<WithdrawOrderData> onSuccess, Action<string> onError = null)
        {
            if (requestBody == null)
            {
                //onError?.Invoke("提现参数不能为空");
                yield break;
            }

            if (!TryBuildIdentity(out string afDeviceId, out int appIndex, out string identityError))
            {
                onError?.Invoke(identityError);
                yield break;
            }

            bool ensureSuccess = false;
            string ensureError = null;
            yield return EnsureEditorAfDeviceInitialized(afDeviceId, appIndex, success => ensureSuccess = success, error => ensureError = error);
            if (!ensureSuccess)
            {
                onError?.Invoke(ensureError);
                yield break;
            }

            bool ensureAdCountSuccess = false;
            string ensureAdCountError = null;
            yield return EnsureEditorAdCountInitializedForWithdraw(requestBody, afDeviceId, appIndex, success => ensureAdCountSuccess = success, error => ensureAdCountError = error);
            if (!ensureAdCountSuccess)
            {
                onError?.Invoke(ensureAdCountError);
                yield break;
            }

            requestBody.AfDeviceId = afDeviceId;
            requestBody.AppIndex = appIndex;
            if (string.IsNullOrEmpty(requestBody.Currency))
            {
                requestBody.Currency = WithdrawConstants.CurrencyUsd;
            }

            if (string.IsNullOrEmpty(requestBody.RecipientType))
            {
                requestBody.RecipientType = WithdrawConstants.RecipientTypeEmail;
            }

            if (requestBody.Amount.HasValue)
            {
                requestBody.Amount = RoundAmount(requestBody.Amount.Value);
            }

            yield return SendJsonRequest<WithdrawOrderData>(UnityWebRequest.kHttpVerbPOST, GameDefines.WalletWithdrawApiPath, requestBody, order =>
            {
                CacheOrder(order);
                onSuccess?.Invoke(order);
            }, onError);
        }

        public static IEnumerator QueryWithdrawOrders(Action<List<WithdrawOrderData>> onSuccess, Action<string> onError = null)
        {
            if (!TryBuildIdentity(out string afDeviceId, out int appIndex, out string identityError))
            {
                onError?.Invoke(identityError);
                yield break;
            }

            bool ensureSuccess = false;
            string ensureError = null;
            yield return EnsureEditorAfDeviceInitialized(afDeviceId, appIndex, success => ensureSuccess = success, error => ensureError = error);
            if (!ensureSuccess)
            {
                onError?.Invoke(ensureError);
                yield break;
            }

            Dictionary<string, string> query = new Dictionary<string, string>
            {
                { "afDeviceId", afDeviceId },
                { "appIndex", appIndex.ToString() }
            };

            yield return SendGetRequest<List<WithdrawOrderData>>(GameDefines.WalletWithdrawOrdersApiPath, query, orders =>
            {
                List<WithdrawOrderData> mergedOrders = MergeLocalQueuedOrders(orders);
                CacheOrders(mergedOrders);
                onSuccess?.Invoke(mergedOrders);
            }, onError);
        }

        public static IEnumerator QueryWithdrawOrder(string orderNo, Action<WithdrawOrderData> onSuccess, Action<string> onError = null)
        {
            if (string.IsNullOrEmpty(orderNo))
            {
                onError?.Invoke("orderNo不能为空");
                yield break;
            }

            WithdrawOrderData localQueuedOrder = GetCachedOrders()
                .FirstOrDefault(order => order != null
                    && order.ClientQueuedPlaceholder
                    && string.Equals(order.OrderNo, orderNo, StringComparison.OrdinalIgnoreCase));
            if (localQueuedOrder != null)
            {
                onSuccess?.Invoke(localQueuedOrder);
                yield break;
            }

            if (!TryBuildIdentity(out string afDeviceId, out int appIndex, out string identityError))
            {
                onError?.Invoke(identityError);
                yield break;
            }

            bool ensureSuccess = false;
            string ensureError = null;
            yield return EnsureEditorAfDeviceInitialized(afDeviceId, appIndex, success => ensureSuccess = success, error => ensureError = error);
            if (!ensureSuccess)
            {
                onError?.Invoke(ensureError);
                yield break;
            }

            Dictionary<string, string> query = new Dictionary<string, string>
            {
                { "afDeviceId", afDeviceId },
                { "appIndex", appIndex.ToString() },
                { "orderNo", orderNo }
            };

            yield return SendGetRequest<WithdrawOrderData>(GameDefines.WalletWithdrawOrderApiPath, query, order =>
            {
                CacheOrder(order);
                onSuccess?.Invoke(order);
            }, onError);
        }

        public static DataModel CreateWalletWithdrawDataModel()
        {
            GameManagerWZ gm = GameManagerWZ.instance;
            DataModel dataModel = new DataModel
            {
                Level = gm != null ? Mathf.Max(gm.currentLv - 1, 0) : 0,
                Money = GetCachedWalletBalance(),
                Num = gm != null && gm.currentStage > 0 ? gm.currentStage : 1,
                CloseType = 1,
                Special = DataModel.SpecialLuckyWalletWithdraw,
                Name = gm != null ? gm.getPlayerName() : string.Empty,
                Phone = gm != null ? gm.getPlayerPhone() : string.Empty,
                Email = gm != null ? gm.getPlayerEmail() : string.Empty,
                Index = 3,
                PayPalAccountExists = GetCachedPayPalAccountExists(),
                PayPalAccountTip = GetCachedPayPalTip()
            };
            return dataModel;
        }

        public static WithdrawOrderData CreateQueuedWalletOrder(double amount, string receiver = null)
        {
            double roundedAmount = RoundAmount(Math.Max(amount, 0d));
            string amountText = roundedAmount.ToString("F2", CultureInfo.InvariantCulture);
            string resolvedReceiver = !string.IsNullOrEmpty(receiver)
                ? receiver
                : (GameManagerWZ.instance != null ? GameManagerWZ.instance.getPlayerEmail() : string.Empty);
            string nowText = DateTimeOffset.Now.ToString("o", CultureInfo.InvariantCulture);

            WithdrawOrderData queuedOrder = new WithdrawOrderData
            {
                OrderNo = $"local-wallet-{DateTime.UtcNow:yyyyMMddHHmmssfff}",
                WithdrawType = WithdrawConstants.WithdrawTypeWallet,
                OrderStatus = 0,
                Receiver = resolvedReceiver,
                Amount = amountText,
                Currency = WithdrawConstants.CurrencyUsd,
                CreateTime = nowText,
                UpdateTime = nowText,
                ClientSortAnchorTime = nowText,
                ClientQueuedPlaceholder = true,
                LocalStatusText = WalletReviewStatusText
            };

            List<WithdrawOrderData> orders = GetCachedOrders();
            orders.Add(queuedOrder);
            CacheOrders(orders);
            return queuedOrder;
        }

        public static WithdrawOrderData EnsureQueuedFirstAdOrder(double amount)
        {
            double roundedAmount = RoundAmount(amount > 0d ? amount : GameDefines.AddCoin);
            string amountText = roundedAmount.ToString("F2", CultureInfo.InvariantCulture);
            string receiver = GameManagerWZ.instance != null ? GameManagerWZ.instance.getPlayerEmail() : string.Empty;
            string nowText = DateTimeOffset.Now.ToString("o", CultureInfo.InvariantCulture);

            List<WithdrawOrderData> orders = GetCachedOrders();
            WithdrawOrderData queuedOrder = orders.FirstOrDefault(order => order != null && order.ClientQueuedPlaceholder && order.IsFirstAdWithdraw);
            if (queuedOrder != null)
            {
                string existingSortAnchor = queuedOrder.GetSortAnchorTimeText();
                queuedOrder.Amount = amountText;
                queuedOrder.Receiver = receiver;
                queuedOrder.Currency = WithdrawConstants.CurrencyUsd;
                queuedOrder.LocalStatusText = "排队中";
                queuedOrder.UpdateTime = nowText;
                if (string.IsNullOrEmpty(queuedOrder.ClientSortAnchorTime))
                    queuedOrder.ClientSortAnchorTime = string.IsNullOrEmpty(existingSortAnchor) ? nowText : existingSortAnchor;
                queuedOrder.LocalStatusText = FirstAdQueuedStatusText;
                CacheOrders(orders);
                return queuedOrder;
            }

            WithdrawOrderData existingFirstAdOrder = orders.FirstOrDefault(order => order != null && order.IsFirstAdWithdraw && !order.ClientQueuedPlaceholder);
            if (existingFirstAdOrder != null)
                return existingFirstAdOrder;

            queuedOrder = new WithdrawOrderData
            {
                OrderNo = $"local-first-ad-{DateTime.UtcNow:yyyyMMddHHmmssfff}",
                WithdrawType = WithdrawConstants.WithdrawTypeFirstAd,
                OrderStatus = 0,
                Receiver = receiver,
                Amount = amountText,
                Currency = WithdrawConstants.CurrencyUsd,
                CreateTime = nowText,
                UpdateTime = nowText,
                ClientSortAnchorTime = nowText,
                ClientQueuedPlaceholder = true,
                LocalStatusText = "排队中",
            };

            queuedOrder.LocalStatusText = FirstAdQueuedStatusText;
            orders.Add(queuedOrder);
            CacheOrders(orders);
            return queuedOrder;
        }

        public static void MarkQueuedFirstAdOrderUnderReview()
        {
            List<WithdrawOrderData> orders = GetCachedOrders();
            WithdrawOrderData queuedOrder = orders.FirstOrDefault(order => order != null && order.ClientQueuedPlaceholder && order.IsFirstAdWithdraw);
            if (queuedOrder == null)
                return;

            string existingSortAnchor = queuedOrder.GetSortAnchorTimeText();
            queuedOrder.LocalStatusText = WalletReviewStatusText;
            queuedOrder.UpdateTime = DateTime.Now.ToString("o");
            if (string.IsNullOrEmpty(queuedOrder.ClientSortAnchorTime))
                queuedOrder.ClientSortAnchorTime = existingSortAnchor;
            CacheOrders(orders);
        }

        public static void ReplaceQueuedFirstAdOrder(WithdrawOrderData order)
        {
            if (order == null || string.IsNullOrEmpty(order.OrderNo))
                return;

            List<WithdrawOrderData> cachedOrders = GetCachedOrders();
            ApplyCachedSortAnchor(cachedOrders, order);

            List<WithdrawOrderData> orders = cachedOrders
                .Where(item => item != null && !(item.ClientQueuedPlaceholder && item.IsFirstAdWithdraw))
                .ToList();

            int existingIndex = orders.FindIndex(item => string.Equals(item.OrderNo, order.OrderNo, StringComparison.OrdinalIgnoreCase));
            if (existingIndex >= 0)
            {
                orders[existingIndex] = order;
            }
            else
            {
                orders.Add(order);
            }

            CacheOrders(orders);
        }

        public static void ReplaceQueuedWalletOrder(string localOrderNo, WithdrawOrderData order)
        {
            if (order == null || string.IsNullOrEmpty(order.OrderNo))
                return;

            List<WithdrawOrderData> cachedOrders = GetCachedOrders();
            ApplyCachedSortAnchor(cachedOrders, order, localOrderNo);

            List<WithdrawOrderData> orders = cachedOrders
                .Where(item => item != null
                    && !(item.ClientQueuedPlaceholder
                        && item.IsWalletWithdraw
                        && string.Equals(item.OrderNo, localOrderNo, StringComparison.OrdinalIgnoreCase)))
                .ToList();

            int existingIndex = orders.FindIndex(item => string.Equals(item.OrderNo, order.OrderNo, StringComparison.OrdinalIgnoreCase));
            if (existingIndex >= 0)
            {
                orders[existingIndex] = order;
            }
            else
            {
                orders.Add(order);
            }

            CacheOrders(orders);
        }

        public static void RemoveQueuedWalletOrder(string localOrderNo)
        {
            if (string.IsNullOrEmpty(localOrderNo))
                return;

            List<WithdrawOrderData> orders = GetCachedOrders()
                .Where(item => item != null
                    && !(item.ClientQueuedPlaceholder
                        && item.IsWalletWithdraw
                        && string.Equals(item.OrderNo, localOrderNo, StringComparison.OrdinalIgnoreCase)))
                .ToList();

            CacheOrders(orders);
        }

        public static DataModel CreateRetryWithdrawDataModel(WithdrawOrderData order)
        {
            GameManagerWZ gm = GameManagerWZ.instance;
            DataModel dataModel = new DataModel
            {
                Level = gm != null ? Mathf.Max(gm.currentLv - 1, 0) : 0,
                Money = order != null ? order.GetAmountValue() : 0d,
                Num = gm != null && gm.currentStage > 0 ? gm.currentStage : 1,
                CloseType = 1,
                Special = order != null && order.IsWalletWithdraw ? DataModel.SpecialLuckyWalletWithdraw : DataModel.SpecialNone,
                Name = gm != null ? gm.getPlayerName() : string.Empty,
                Phone = gm != null ? gm.getPlayerPhone() : string.Empty,
                Email = order != null ? order.Receiver : (gm != null ? gm.getPlayerEmail() : string.Empty),
                Index = 3,
                OrderNo = order != null ? order.OrderNo : string.Empty,
                Receiver = order != null ? order.Receiver : string.Empty,
                WithdrawType = order != null ? order.WithdrawType : string.Empty,
                OrderStatus = order != null ? order.OrderStatus : 0,
                RetryWithdraw = true,
                PayPalAccountExists = order != null && order.RequiresReceiverInput ? 0 : GetCachedPayPalAccountExists(),
                PayPalAccountTip = order != null && order.RequiresReceiverInput
                    ? "当前PayPal收款账号不存在，请重新填写正确的PayPal邮箱"
                    : GetCachedPayPalTip()
            };
            return dataModel;
        }

        private static IEnumerator SendJsonRequest<T>(string method, string path, object body, Action<T> onSuccess, Action<string> onError)
        {
            string url = BuildApiUrl(path, null);
            if (string.IsNullOrEmpty(url))
            {
                onError?.Invoke("提现接口地址未配置");
                yield break;
            }

            byte[] bodyRaw = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(body, JsonSettings));
            using (UnityWebRequest request = new UnityWebRequest(url, method))
            {
                request.timeout = RequestTimeoutSeconds;
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");
                LogEditorServerRequest("json", method, url, $"body={Encoding.UTF8.GetString(bodyRaw)}");

                yield return request.SendWebRequest();
                HandleResponse(request, onSuccess, onError);
            }
        }

        private static IEnumerator SendGetRequest<T>(string path, Dictionary<string, string> query, Action<T> onSuccess, Action<string> onError)
        {
            string url = BuildApiUrl(path, query);
            if (string.IsNullOrEmpty(url))
            {
                onError?.Invoke("提现接口地址未配置");
                yield break;
            }

            using (UnityWebRequest request = UnityWebRequest.Get(url))
            {
                request.timeout = RequestTimeoutSeconds;
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");
                LogEditorServerRequest("get", UnityWebRequest.kHttpVerbGET, url,
                    query == null || query.Count == 0
                        ? "query=<none>"
                        : $"query={string.Join("&", query.Select(pair => $"{pair.Key}={pair.Value}"))}");

                yield return request.SendWebRequest();
                HandleResponse(request, onSuccess, onError);
            }
        }

        private static void HandleResponse<T>(UnityWebRequest request, Action<T> onSuccess, Action<string> onError)
        {
            string responseText = request.downloadHandler != null ? request.downloadHandler.text : string.Empty;
            if (request.result != UnityWebRequest.Result.Success)
            {
                LogEditorServerResponse("api", request, responseText, false, $"error={request.error}");
                onError?.Invoke(ExtractErrorMessage(responseText, request.error));
                return;
            }

            try
            {
                WithdrawApiResponse<T> response = JsonConvert.DeserializeObject<WithdrawApiResponse<T>>(responseText);
                if (response == null)
                {
                    LogEditorServerResponse("api", request, responseText, false, "error=response null");
                    onError?.Invoke("提现接口返回为空");
                    return;
                }

                if (response.Code != 200)
                {
                    LogEditorServerResponse("api", request, responseText, false, $"apiCode={response.Code}, msg={response.Msg}");
                    onError?.Invoke(string.IsNullOrEmpty(response.Msg) ? "提现接口请求失败" : response.Msg);
                    return;
                }

                LogEditorServerResponse("api", request, responseText, true, $"apiCode={response.Code}");
                onSuccess?.Invoke(response.Data);
            }
            catch (Exception e)
            {
                LogEditorServerResponse("api", request, responseText, false, $"parseError={e.Message}");
                Debug.LogError($"提现接口解析失败: {e.Message}\n{responseText}");
                onError?.Invoke("提现接口解析失败");
            }
        }

        private static bool TryBuildIdentity(out string afDeviceId, out int appIndex, out string error)
        {
            afDeviceId = ResolveAfDeviceId();
            appIndex = GameDefines.appIndex;
            error = null;

            if (string.IsNullOrEmpty(afDeviceId))
            {
                error = "AppsFlyer ID未就绪，请稍后重试";
                return false;
            }

            if (string.IsNullOrEmpty(afDeviceId))
            {
                error = "afDeviceId不能为空";
                return false;
            }

            if (appIndex <= 0)
            {
                error = "appIndex不能为空";
                return false;
            }

            return true;
        }

        private static int ResolveLuckyRewardConfigLevel()
        {
            GameManagerWZ gm = GameManagerWZ.instance;
            if (gm != null)
                return Mathf.Max(gm.currentLv - 1, 0);

            int currentLevel = PlayerPrefs.GetInt(FacadePlayerPrefExtend.currentLevel, 1);
            return Mathf.Max(currentLevel - 1, 0);
        }

        private static void ResolveWalletIncomeRequestMetadata(string incomeTypeOverride, int? incomeLevelOverride, out string incomeType, out int? incomeLevel)
        {
            incomeLevel = incomeLevelOverride ?? ResolveWalletIncomeRequestLevel();

            if (string.Equals(incomeTypeOverride, WithdrawConstants.WalletIncomeTypeNormal, StringComparison.OrdinalIgnoreCase))
            {
                incomeType = WithdrawConstants.WalletIncomeTypeNormal;
                return;
            }

            if (string.Equals(incomeTypeOverride, WithdrawConstants.WalletIncomeTypeLucky, StringComparison.OrdinalIgnoreCase))
            {
                incomeType = WithdrawConstants.WalletIncomeTypeLucky;
                if (incomeLevelOverride.HasValue)
                    return;

                if (TryResolveLuckyWalletIncomeLevel(out int forcedLuckyLevel))
                    incomeLevel = forcedLuckyLevel;

                return;
            }

            incomeType = WithdrawConstants.WalletIncomeTypeNormal;

            if (!TryResolveLuckyWalletIncomeLevel(out int luckyLevel))
                return;

            incomeType = WithdrawConstants.WalletIncomeTypeLucky;
            incomeLevel = luckyLevel;
        }

        private static bool ShouldReportRewardedAdWalletIncome()
        {
            if (!CanReportWalletIncomeAtCurrentLevel())
                return false;

            return UIManager.instance == null || !UIManager.instance.HasActiveView(EUIType.LuckySuccessView);
        }

        private static bool CanReportWalletIncomeAtCurrentLevel()
        {
            return ResolveCurrentLevelForWalletIncome() >= GameDefines.LuckyWalletUnlockLevel;
        }

        private static int ResolveCurrentLevelForWalletIncome()
        {
            GameManagerWZ gm = GameManagerWZ.instance;
            if (gm != null)
                return Mathf.Max(gm.currentLv, 0);

            return Mathf.Max(PlayerPrefs.GetInt(FacadePlayerPrefExtend.currentLevel, 1), 0);
        }

        private static int ResolveWalletIncomeRequestLevel()
        {
            GameManagerWZ gm = GameManagerWZ.instance;
            if (gm == null)
                return Mathf.Max(PlayerPrefs.GetInt(FacadePlayerPrefExtend.currentLevel, 1), 0);

            bool isLuckyCompletionFlow = UIManager.instance != null
                && UIManager.instance.HasActiveView(EUIType.LuckySuccessView);

            if (isLuckyCompletionFlow)
                return Mathf.Max(gm.currentLv - 1, 0);

            return Mathf.Max(gm.currentLv, 0);
        }

        private static bool TryResolveLuckyWalletIncomeLevel(out int level)
        {
            level = 0;
            UserDataManager userDataManager = UserDataManager.Instance;
            if (userDataManager == null)
                return false;

            GameManagerWZ gm = GameManagerWZ.instance;
            if (gm == null)
            {
                int savedLevel = PlayerPrefs.GetInt(FacadePlayerPrefExtend.currentLevel, 1);
                return TryUseLuckyLevel(savedLevel, userDataManager, out level);
            }

            bool isLuckyCompletionFlow = UIManager.instance != null
                && UIManager.instance.HasActiveView(EUIType.LuckySuccessView);

            if (isLuckyCompletionFlow)
            {
                int completedLevel = Mathf.Max(gm.currentLv - 1, 0);
                return TryUseLuckyLevel(completedLevel, userDataManager, out level);
            }

            int currentLevel = Mathf.Max(gm.currentLv, 0);
            return TryUseLuckyLevel(currentLevel, userDataManager, out level);
        }

        private static bool TryUseLuckyLevel(int level, UserDataManager userDataManager, out int resolvedLevel)
        {
            resolvedLevel = 0;
            if (level <= 0 || userDataManager == null || !userDataManager.IsLuckyRewardLevel(level))
                return false;

            resolvedLevel = level;
            return true;
        }

        private static string ResolveAfDeviceId()
        {
            return AppsFlyerIdResolver.ResolveCurrentId();
        }

        private static IEnumerator EnsureEditorAfDeviceInitialized(string afDeviceId, int appIndex, Action<bool> onComplete, Action<string> onError = null)
        {
#if UNITY_EDITOR
            if (string.IsNullOrEmpty(afDeviceId))
            {
                onError?.Invoke("AppsFlyer ID未就绪，请稍后重试");
                onComplete?.Invoke(false);
                yield break;
            }

            if (appIndex <= 0)
            {
                onError?.Invoke("appIndex不能为空");
                onComplete?.Invoke(false);
                yield break;
            }

            if (editorAfDeviceInitialized
                && string.Equals(editorInitializedAfDeviceId, afDeviceId, StringComparison.Ordinal)
                && editorInitializedAppIndex == appIndex)
            {
                onComplete?.Invoke(true);
                yield break;
            }

            string url = BuildAfInitUrl();
            if (string.IsNullOrEmpty(url))
            {
                onError?.Invoke("AF初始化地址未配置");
                onComplete?.Invoke(false);
                yield break;
            }

            WWWForm form = new WWWForm();
            form.AddField("afDeviceId", afDeviceId);
            form.AddField("appIndex", appIndex);
            form.AddField("statusName", GameDefines.AfInitStatusName);

            using (UnityWebRequest request = UnityWebRequest.Post(url, form))
            {
                request.timeout = RequestTimeoutSeconds;
                LogEditorServerRequest("afInit", UnityWebRequest.kHttpVerbPOST, url,
                    $"afDeviceId={afDeviceId}, appIndex={appIndex}, statusName={GameDefines.AfInitStatusName}");
                yield return request.SendWebRequest();

                string responseText = request.downloadHandler != null ? request.downloadHandler.text : string.Empty;
                if (request.result != UnityWebRequest.Result.Success)
                {
                    LogEditorServerResponse("afInit", request, responseText, false, $"error={request.error}");
                    onError?.Invoke(ExtractErrorMessage(responseText, request.error));
                    onComplete?.Invoke(false);
                    yield break;
                }

                if (!IsSuccessfulAfInitResponse(responseText, out string apiError))
                {
                    LogEditorServerResponse("afInit", request, responseText, false, $"apiError={apiError}");
                    onError?.Invoke(apiError);
                    onComplete?.Invoke(false);
                    yield break;
                }

                if (!string.Equals(editorInitializedAfDeviceId, afDeviceId, StringComparison.Ordinal)
                    || editorInitializedAppIndex != appIndex)
                {
                    editorSyncedAdCount = 0;
                }

                editorAfDeviceInitialized = true;
                editorInitializedAfDeviceId = afDeviceId;
                editorInitializedAppIndex = appIndex;
                LogEditorServerResponse("afInit", request, responseText, true);
                Debug.Log($"Editor AF device initialized: {afDeviceId}, appIndex={appIndex}");
                onComplete?.Invoke(true);
            }
#else
            onComplete?.Invoke(true);
            yield break;
#endif
        }

        private static string BuildAfInitUrl()
        {
            string baseUrl = !string.IsNullOrEmpty(GameDefines.PushApiBaseUrl)
                ? GameDefines.PushApiBaseUrl
                : GameDefines.WithdrawApiBaseUrl;

            if (string.IsNullOrEmpty(baseUrl))
            {
                return string.Empty;
            }

            try
            {
                UriBuilder builder = new UriBuilder(baseUrl)
                {
                    Path = GameDefines.AfInitApiPath.TrimStart('/'),
                    Query = string.Empty
                };
                return builder.Uri.ToString();
            }
            catch (Exception e)
            {
                Debug.LogError($"AF初始化地址生成失败: {e.Message}");
                return string.Empty;
            }
        }

        private static bool IsSuccessfulAfInitResponse(string responseText, out string error)
        {
            error = null;
            if (string.IsNullOrEmpty(responseText))
            {
                return true;
            }

            try
            {
                WithdrawApiResponse<object> response = JsonConvert.DeserializeObject<WithdrawApiResponse<object>>(responseText);
                if (response == null || response.Code == 0 || response.Code == 200)
                {
                    return true;
                }

                error = string.IsNullOrEmpty(response.Msg) ? "AF设备初始化失败" : response.Msg;
                return false;
            }
            catch
            {
                return true;
            }
        }

        private static IEnumerator EnsureEditorAdCountInitializedForWithdraw(ClientWithdrawRequest requestBody, string afDeviceId, int appIndex, Action<bool> onComplete, Action<string> onError = null)
        {
#if UNITY_EDITOR
            if (requestBody == null
                || !string.Equals(requestBody.WithdrawType, WithdrawConstants.WithdrawTypeFirstAd, StringComparison.OrdinalIgnoreCase))
            {
                onComplete?.Invoke(true);
                yield break;
            }

            int localAdCount = GetEditorLocalAdCount();
            if (localAdCount <= 0)
            {
                onComplete?.Invoke(true);
                yield break;
            }

            if (editorSyncedAdCount >= localAdCount)
            {
                onComplete?.Invoke(true);
                yield break;
            }

            string url = BuildPushApiUrl(GameDefines.AfAdCountApiPath);
            if (string.IsNullOrEmpty(url))
            {
                onError?.Invoke("广告次数上报地址未配置");
                onComplete?.Invoke(false);
                yield break;
            }

            int pendingCount = localAdCount - editorSyncedAdCount;
            for (int i = 0; i < pendingCount; i++)
            {
                WWWForm form = new WWWForm();
                form.AddField("afDeviceId", afDeviceId);
                form.AddField("appIndex", appIndex);
                form.AddField("adRevenue", 0f.ToString("F6", CultureInfo.InvariantCulture));
                form.AddField("incomeAmount", 0f.ToString("F6", CultureInfo.InvariantCulture));

                using (UnityWebRequest request = UnityWebRequest.Post(url, form))
                {
                    request.timeout = RequestTimeoutSeconds;
                    yield return request.SendWebRequest();

                    string responseText = request.downloadHandler != null ? request.downloadHandler.text : string.Empty;
                    if (request.result != UnityWebRequest.Result.Success)
                    {
                        onError?.Invoke(ExtractErrorMessage(responseText, request.error));
                        onComplete?.Invoke(false);
                        yield break;
                    }
                }
            }

            editorSyncedAdCount = localAdCount;
            Debug.Log($"Editor ad count synced for withdraw: {editorSyncedAdCount}");
            onComplete?.Invoke(true);
#else
            onComplete?.Invoke(true);
            yield break;
#endif
        }

        private static int GetEditorLocalAdCount()
        {
            int totalAdCount = PlayerPrefs.GetInt(EditorTotalAdCountPrefsKey, 0);
            int stageAdCount = 0;
            if (GameManagerWZ.instance != null)
            {
                stageAdCount = Mathf.Max(GameManagerWZ.instance.GetStageAdWatchCount(), 0);
            }

            return Math.Max(totalAdCount, stageAdCount);
        }

        private static float ResolveLuckyRewardAmount(double? rewardAmount, float fallbackAmount)
        {
            if (rewardAmount.HasValue && rewardAmount.Value > 0d)
                return (float)RoundAmount(rewardAmount.Value);

            return Mathf.Max(fallbackAmount, 0f);
        }

        private static string BuildPushApiUrl(string path)
        {
            string baseUrl = !string.IsNullOrEmpty(GameDefines.PushApiBaseUrl)
                ? GameDefines.PushApiBaseUrl
                : GameDefines.WithdrawApiBaseUrl;

            if (string.IsNullOrEmpty(baseUrl))
            {
                return string.Empty;
            }

            try
            {
                UriBuilder builder = new UriBuilder(baseUrl)
                {
                    Path = path.TrimStart('/'),
                    Query = string.Empty
                };
                return builder.Uri.ToString();
            }
            catch (Exception e)
            {
                Debug.LogError($"推送接口地址生成失败: {e.Message}");
                return string.Empty;
            }
        }

        private static string BuildApiUrl(string path, Dictionary<string, string> query)
        {
            string baseUrl = !string.IsNullOrEmpty(GameDefines.WithdrawApiBaseUrl)
                ? GameDefines.WithdrawApiBaseUrl
                : GameDefines.PushApiBaseUrl;

            if (string.IsNullOrEmpty(baseUrl))
            {
                return string.Empty;
            }

            try
            {
                UriBuilder builder = new UriBuilder(baseUrl)
                {
                    Path = path.TrimStart('/')
                };

                if (query != null && query.Count > 0)
                {
                    StringBuilder sb = new StringBuilder();
                    foreach (KeyValuePair<string, string> pair in query)
                    {
                        if (string.IsNullOrEmpty(pair.Key))
                            continue;

                        if (sb.Length > 0)
                            sb.Append('&');

                        sb.Append(Uri.EscapeDataString(pair.Key));
                        sb.Append('=');
                        sb.Append(Uri.EscapeDataString(pair.Value ?? string.Empty));
                    }

                    builder.Query = sb.ToString();
                }
                else
                {
                    builder.Query = string.Empty;
                }

                return builder.Uri.ToString();
            }
            catch (Exception e)
            {
                Debug.LogError($"提现接口地址生成失败: {e.Message}");
                return string.Empty;
            }
        }

        private static string ExtractErrorMessage(string responseText, string fallback)
        {
            if (!string.IsNullOrEmpty(responseText))
            {
                try
                {
                    WithdrawApiResponse<object> response = JsonConvert.DeserializeObject<WithdrawApiResponse<object>>(responseText);
                    if (response != null && !string.IsNullOrEmpty(response.Msg))
                    {
                        return response.Msg;
                    }
                }
                catch
                {
                }
            }

            return string.IsNullOrEmpty(fallback) ? "网络请求失败" : fallback;
        }

        private static List<WithdrawOrderData> MergeLocalQueuedOrders(List<WithdrawOrderData> serverOrders)
        {
            List<WithdrawOrderData> mergedOrders = SortOrders(serverOrders);
            List<WithdrawOrderData> cachedOrders = GetCachedOrders();
            bool hasRealFirstAdOrder = mergedOrders.Any(order => order != null && order.IsFirstAdWithdraw && !order.ClientQueuedPlaceholder);

            for (int i = 0; i < mergedOrders.Count; i++)
            {
                ApplyCachedSortAnchor(cachedOrders, mergedOrders[i]);
            }

            foreach (WithdrawOrderData cachedOrder in cachedOrders)
            {
                if (cachedOrder == null || !cachedOrder.ClientQueuedPlaceholder)
                    continue;

                if (cachedOrder.IsFirstAdWithdraw && hasRealFirstAdOrder)
                    continue;

                if (mergedOrders.Any(order => IsMatchingRealOrderForPlaceholder(cachedOrder, order)))
                    continue;

                if (mergedOrders.Any(order => string.Equals(order.OrderNo, cachedOrder.OrderNo, StringComparison.OrdinalIgnoreCase)))
                    continue;

                mergedOrders.Add(cachedOrder);
            }

            return SortOrders(mergedOrders);
        }

        private static void CacheWallet(UserWalletData wallet)
        {
            if (wallet == null)
                return;

            SPlayerPrefs.SetString(FacadePlayerPrefExtend.WalletBalanceCache, JsonConvert.SerializeObject(wallet));
            GameDefines.LuckyChestPlaceholderCoin = (float)Math.Max(wallet.Balance, 0d);
            PlayerPrefs.Save();
        }

        private static void CacheLuckyRewardConfig(LuckyRewardConfigData config)
        {
            if (config == null)
                return;

            SPlayerPrefs.SetString(LuckyRewardConfigCachePrefsKey, JsonConvert.SerializeObject(config));
            PlayerPrefs.Save();
        }

        private static void CacheOrders(List<WithdrawOrderData> orders)
        {
            List<WithdrawOrderData> safeOrders = SortOrders(orders);
            SPlayerPrefs.SetString(FacadePlayerPrefExtend.WalletWithdrawOrdersCache, JsonConvert.SerializeObject(safeOrders));
            PlayerPrefs.Save();
        }

        private static void CacheOrder(WithdrawOrderData order)
        {
            if (order == null || string.IsNullOrEmpty(order.OrderNo))
                return;

            List<WithdrawOrderData> orders = GetCachedOrders();
            ApplyCachedSortAnchor(orders, order);
            int existingIndex = orders.FindIndex(item => string.Equals(item.OrderNo, order.OrderNo, StringComparison.OrdinalIgnoreCase));
            if (existingIndex >= 0)
            {
                orders[existingIndex] = order;
            }
            else
            {
                int matchingPlaceholderIndex = orders.FindIndex(item => IsMatchingRealOrderForPlaceholder(item, order));
                if (matchingPlaceholderIndex >= 0)
                    orders[matchingPlaceholderIndex] = order;
                else
                    orders.Add(order);
            }

            CacheOrders(orders);
        }

        private static List<WithdrawOrderData> SortOrders(List<WithdrawOrderData> orders)
        {
            return (orders ?? new List<WithdrawOrderData>())
                .Where(item => item != null)
                .OrderByDescending(GetOrderCreateTime)
                .ThenByDescending(item => item.GetBestTimeText())
                .ThenByDescending(item => item.OrderNo)
                .ToList();
        }

        private static void ApplyCachedSortAnchor(List<WithdrawOrderData> cachedOrders, WithdrawOrderData order, string localPlaceholderOrderNo = null)
        {
            if (cachedOrders == null || order == null)
                return;

            WithdrawOrderData sourceOrder = null;

            if (!string.IsNullOrEmpty(localPlaceholderOrderNo))
            {
                sourceOrder = cachedOrders.FirstOrDefault(item => item != null
                    && string.Equals(item.OrderNo, localPlaceholderOrderNo, StringComparison.OrdinalIgnoreCase));
            }

            if (sourceOrder == null)
            {
                sourceOrder = cachedOrders.FirstOrDefault(item => item != null
                    && string.Equals(item.OrderNo, order.OrderNo, StringComparison.OrdinalIgnoreCase));
            }

            if (sourceOrder == null)
            {
                sourceOrder = cachedOrders.FirstOrDefault(item => IsMatchingRealOrderForPlaceholder(item, order));
            }

            ApplySortAnchor(sourceOrder, order);
        }

        private static void ApplySortAnchor(WithdrawOrderData sourceOrder, WithdrawOrderData targetOrder)
        {
            if (sourceOrder == null || targetOrder == null)
                return;

            if (!string.IsNullOrEmpty(targetOrder.ClientSortAnchorTime))
                return;

            string anchorTimeText = sourceOrder.GetSortAnchorTimeText();
            if (!string.IsNullOrEmpty(anchorTimeText))
                targetOrder.ClientSortAnchorTime = anchorTimeText;
        }

        private static bool IsMatchingRealOrderForPlaceholder(WithdrawOrderData placeholderOrder, WithdrawOrderData realOrder)
        {
            if (placeholderOrder == null || realOrder == null)
                return false;

            if (!placeholderOrder.ClientQueuedPlaceholder || realOrder.ClientQueuedPlaceholder)
                return false;

            if (!string.Equals(placeholderOrder.WithdrawType, realOrder.WithdrawType, StringComparison.OrdinalIgnoreCase))
                return false;

            if (!string.IsNullOrEmpty(placeholderOrder.Currency)
                && !string.IsNullOrEmpty(realOrder.Currency)
                && !string.Equals(placeholderOrder.Currency, realOrder.Currency, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (!AreOrderAmountsClose(placeholderOrder, realOrder))
                return false;

            if (!string.IsNullOrEmpty(placeholderOrder.Receiver)
                && !string.IsNullOrEmpty(realOrder.Receiver)
                && !string.Equals(placeholderOrder.Receiver, realOrder.Receiver, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            return AreOrderTimesClose(placeholderOrder, realOrder, 2d);
        }

        private static bool AreOrderAmountsClose(WithdrawOrderData leftOrder, WithdrawOrderData rightOrder)
        {
            if (leftOrder == null || rightOrder == null)
                return false;

            return Math.Abs(leftOrder.GetAmountValue() - rightOrder.GetAmountValue()) < 0.0001d;
        }

        private static bool AreOrderTimesClose(WithdrawOrderData leftOrder, WithdrawOrderData rightOrder, double maxHoursDifference)
        {
            if (!TryGetComparableOrderTime(leftOrder, out DateTimeOffset leftTime)
                || !TryGetComparableOrderTime(rightOrder, out DateTimeOffset rightTime))
            {
                return false;
            }

            return Math.Abs((leftTime - rightTime).TotalHours) <= maxHoursDifference;
        }

        private static bool TryGetComparableOrderTime(WithdrawOrderData order, out DateTimeOffset time)
        {
            time = DateTimeOffset.MinValue;
            if (order == null)
                return false;

            string[] timeTexts =
            {
                order.CreateTime,
                order.TimeCreated,
                order.UpdateTime,
                order.SuccessTime
            };

            for (int i = 0; i < timeTexts.Length; i++)
            {
                if (DateTimeOffset.TryParse(timeTexts[i], CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out time))
                    return true;
            }

            return false;
        }

        private static DateTimeOffset GetOrderCreateTime(WithdrawOrderData order)
        {
            if (order == null)
                return DateTimeOffset.MinValue;

            if (order.TryGetSortAnchorTime(out DateTimeOffset time))
                return time;

            return DateTimeOffset.MinValue;
        }

        private static double RoundAmount(double amount)
        {
            return Math.Round(amount, 2, MidpointRounding.AwayFromZero);
        }
    }
}
