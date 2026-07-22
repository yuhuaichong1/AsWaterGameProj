using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Networking;

namespace WZSDK
{
    public class NetworkFrame : Singleton<NetworkFrame>, ILoad, IDispose
    {
        private const string ServerConfigLogPrefix = "[ServerConfig]";
        private const string ServerResponseCacheKey = "server_config_raw";
        private const string ServerResponseTimeCacheKey = "server_config_time";
        private const int MaxServerRequestAttempts = 3;
        private const float ServerRetryDelaySeconds = 1f;
        private const string LocalSettingsResourcePath = "LocalSettings/LocalSettings";
        private bool loginSuccessed;
        private float calcTime = 0;
        private Action<bool, bool, bool> onRequestComplete; // bool iaaValue, bool isVersionMatch, bool forcedUpload

        public void Load() { }

        public void Dispose() => Debug.Log("~NetworkManager was destroy");

        public static string GetCachedServerResponse()
        {
            return SPlayerPrefs.GetString(ServerResponseCacheKey, string.Empty);
        }

        public static string GetCachedServerResponseTime()
        {
            return SPlayerPrefs.GetString(ServerResponseTimeCacheKey, string.Empty);
        }

        /// <summary>
        /// 获取服务器数据，解析iaa值和版本配置，成功后关闭tips并执行回调
        /// </summary>
        /// <param name="url">请求地址</param>
        /// <param name="onComplete">回调函数，参数：iaa值, 版本是否匹配</param>
        public void FetchServerData(string url, Action<bool, bool, bool> onComplete)
        {
            onRequestComplete = onComplete;
            string currentVersion = GetCurrentVersion();
            GameManagerWZ.instance.StartCoroutine(FetchDataCoroutine(url, currentVersion));
        }

        /// <summary>
        /// 获取当前应用版本号
        /// </summary>
        private string GetCurrentVersion()
        {
            string version = Application.version;
            return version;
        }

        private IEnumerator FetchDataCoroutine(string url, string currentVersion)
        {
            for (int attempt = 1; attempt <= MaxServerRequestAttempts; attempt++)
            {
                using (UnityWebRequest webReq = UnityWebRequest.Get(url))
                {
                    webReq.timeout = 16;
                    webReq.SetRequestHeader("Content-Type", "application/json");

                    yield return webReq.SendWebRequest();

                    if (webReq.result == UnityWebRequest.Result.Success)
                    {
                        string jsonData = webReq.downloadHandler.text;
                        CacheServerResponse(jsonData);
                        var (iaaValue, isVersionMatch, forcedUpload) = ParseServerResponse(jsonData, currentVersion);
                        onRequestComplete?.Invoke(iaaValue, isVersionMatch, forcedUpload);
                        yield break;
                    }

                    Debug.LogError($"请求失败：{webReq.error}");
                    Debug.LogWarning($"{ServerConfigLogPrefix} 请求失败({attempt}/{MaxServerRequestAttempts}): url={url}");
                }

                if (attempt < MaxServerRequestAttempts)
                {
                    yield return new WaitForSecondsRealtime(ServerRetryDelaySeconds);
                }
            }

            Debug.LogWarning($"{ServerConfigLogPrefix} 服务器请求连续失败 {MaxServerRequestAttempts} 次，回退本地配置: Resources/{LocalSettingsResourcePath}.json");
            var localResult = ParseLocalSettingsResponse(currentVersion);
            onRequestComplete?.Invoke(localResult.iaaValue, localResult.isVersionMatch, localResult.forcedUpload);
        }

        private void CacheServerResponse(string jsonData)
        {
            if (string.IsNullOrEmpty(jsonData))
                return;

            SPlayerPrefs.SetString(ServerResponseCacheKey, jsonData);
            SPlayerPrefs.SetString(ServerResponseTimeCacheKey, DateTimeOffset.UtcNow.ToString("o", CultureInfo.InvariantCulture));
            PlayerPrefs.Save();
            Debug.Log($"{ServerConfigLogPrefix} 已缓存服务器消息");
        }

        private (bool iaaValue, bool isVersionMatch, bool forcedUpload) ParseServerResponse(string jsonData, string currentVersion)
        {
            return ParseConfigResponse(jsonData, currentVersion, false, "server");
        }

        private (bool iaaValue, bool isVersionMatch, bool forcedUpload) ParseLocalSettingsResponse(string currentVersion)
        {
            TextAsset localSettingsAsset = Resources.Load<TextAsset>(LocalSettingsResourcePath);
            if (localSettingsAsset == null || string.IsNullOrEmpty(localSettingsAsset.text))
            {
                Debug.LogError($"{ServerConfigLogPrefix} 本地配置不存在: Resources/{LocalSettingsResourcePath}.json");
                return (false, false, false);
            }

            return ParseConfigResponse(localSettingsAsset.text, currentVersion, true, "local");
        }

        private (bool iaaValue, bool isVersionMatch, bool forcedUpload) ParseConfigResponse(string jsonData, string currentVersion, bool defaultIaaValue, string source)
        {
            try
            {
                if (string.IsNullOrEmpty(jsonData))
                {
                    Debug.LogWarning($"{ServerConfigLogPrefix} {source} 响应为空");
                    return (false, false, false);
                }

                string extText = jsonData;
                bool iaaValue = defaultIaaValue;
                JObject rootObject = JObject.Parse(jsonData);

                if (rootObject.TryGetValue("data", StringComparison.OrdinalIgnoreCase, out JToken dataToken)
                    && TryParseBoolToken(dataToken, out bool parsedIaaValue))
                {
                    iaaValue = parsedIaaValue;
                }

                if (rootObject.TryGetValue("ext", StringComparison.OrdinalIgnoreCase, out JToken extToken) && extToken != null)
                {
                    extText = extToken.Type == JTokenType.String
                        ? extToken.ToString()
                        : extToken.ToString(Newtonsoft.Json.Formatting.None);
                }

                ServerExtConfig config = ServerExtConfig.Parse(extText);
                if (config == null)
                {
                    Debug.Log($"{ServerConfigLogPrefix} {source} 未携带 ext 配置，iaa={iaaValue}");
                    return (iaaValue, false, false);
                }

                Debug.Log($"{ServerConfigLogPrefix} {source} ext keys: {string.Join(",", config.Keys)}");

                bool forcedUpload = config.GetBool("forcedUpload");
                ApplyAdShowConfig(config);
                ApplyWheelRewardConfig(config);
                ApplyLuckyRewardConfig(config);
                ApplyLevelMapConfig(config);
                bool isVersionMatch = ResolveVersionMatch(config.GetString("needChanceVersion"), currentVersion);

                Debug.Log($"{ServerConfigLogPrefix} {source} 应用完成: iaa={iaaValue}, forcedUpload={forcedUpload}, walletShow={GameDefines.LuckyWalletUnlockLevel}, luckyLevelCount={GameDefines.ServerLuckyRewardLevels.Count}, wheelReward={GameDefines.WheelReward:F4}, levelMapCount={GameDefines.LevelMapConfig.Count}, versionMatch={isVersionMatch}");
                return (iaaValue, isVersionMatch, forcedUpload);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"{ServerConfigLogPrefix} {source} 解析失败: {e.Message}");
                return (false, false, false);
            }
        }

        private static bool TryParseBoolToken(JToken token, out bool value)
        {
            value = false;
            if (token == null || token.Type == JTokenType.Null)
                return false;

            if (token.Type == JTokenType.Boolean)
            {
                value = token.Value<bool>();
                return true;
            }

            if (bool.TryParse(token.ToString(), out bool parsedBool))
            {
                value = parsedBool;
                return true;
            }

            if (int.TryParse(token.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsedInt))
            {
                value = parsedInt != 0;
                return true;
            }

            return false;
        }

        private void ApplyAdShowConfig(ServerExtConfig config)
        {
            if (!config.TryGetObject(out AdShowConfig[] adShowConfigs, "adShow") || adShowConfigs == null)
                return;

            GameDefines.LevelTimerConfig.Clear();
            for (int i = 0; i < adShowConfigs.Length; i++)
            {
                AdShowConfig adShowConfig = adShowConfigs[i];
                if (adShowConfig == null || adShowConfig.level <= 0)
                    continue;

                GameDefines.LevelTimerConfig[adShowConfig.level] = adShowConfig.time;
            }

            Debug.Log($"{ServerConfigLogPrefix} 加载 adShow 配置: {GameDefines.LevelTimerConfig.Count} 条");
        }

        private void ApplyWheelRewardConfig(ServerExtConfig config)
        {
            if (!config.TryGetFloat(out float wheelReward, "WheelReward", "wheelReward"))
                return;

            GameDefines.WheelReward = wheelReward;
            Debug.Log($"{ServerConfigLogPrefix} WheelReward={wheelReward:F4}");
        }

        private void ApplyLuckyRewardConfig(ServerExtConfig config)
        {
            bool configChanged = false;

            if (config.TryGetInt(out int walletShowLevel, "walletShow", "WalletShow") && walletShowLevel > 0)
            {
                GameDefines.LuckyWalletUnlockLevel = walletShowLevel;
                configChanged = true;
                Debug.Log($"{ServerConfigLogPrefix} walletShow={walletShowLevel}");
            }

            if (config.TryGetIntList(out List<int> luckyLevels, "luckyLevel", "LuckyLevel"))
            {
                GameDefines.ApplyServerLuckyRewardLevels(luckyLevels);
                configChanged = true;
                Debug.Log($"{ServerConfigLogPrefix} luckyLevel={string.Join(",", GameDefines.ServerLuckyRewardLevels)}");
            }

            if (!configChanged)
                return;

            UserDataManager.Instance?.RefreshLuckyRewardConfig();
        }

        private void ApplyLevelMapConfig(ServerExtConfig config)
        {
            GameDefines.LevelMapConfig.Clear();
            if (!config.TryGetObject(out Dictionary<string, int> levelMap, "levelMap") || levelMap == null)
                return;

            foreach (KeyValuePair<string, int> pair in levelMap)
            {
                if (int.TryParse(pair.Key, out int level) && pair.Value > 0)
                {
                    GameDefines.LevelMapConfig[level] = pair.Value;
                }
            }

            Debug.Log($"{ServerConfigLogPrefix} 加载 levelMap 配置: {GameDefines.LevelMapConfig.Count} 条");
        }

        private bool ResolveVersionMatch(string targetVersionsText, string currentVersion)
        {
            if (string.IsNullOrEmpty(targetVersionsText))
                return false;

            string[] targetVersions = targetVersionsText.Split(',');
            for (int i = 0; i < targetVersions.Length; i++)
            {
                if (CompareVersion(currentVersion.Trim(), targetVersions[i].Trim()))
                {
                    Debug.Log($"{ServerConfigLogPrefix} 版本匹配成功: current={currentVersion}, target={targetVersionsText}");
                    return true;
                }
            }

            Debug.Log($"{ServerConfigLogPrefix} 版本未匹配: current={currentVersion}, target={targetVersionsText}");
            return false;
        }

        /// <summary>
        /// 版本号比较
        /// </summary>
        /// <param name="currentVersion">当前版本</param>
        /// <param name="targetVersion">目标版本</param>
        /// <returns>是否匹配</returns>
        private bool CompareVersion(string currentVersion, string targetVersion)
        {

            if (currentVersion == targetVersion)
            {
                return true;
            }
            return false;
        }

        [Serializable]
        private class JsonData
        {
            public bool data;
            public string ext;
        }

        [Serializable]
        private class AdShowConfig
        {
            public int level;
            public float time;
        }

        private sealed class ServerExtConfig
        {
            private readonly Dictionary<string, JToken> tokenMap;

            private ServerExtConfig(Dictionary<string, JToken> tokenMap)
            {
                this.tokenMap = tokenMap ?? new Dictionary<string, JToken>(StringComparer.OrdinalIgnoreCase);
            }

            public IEnumerable<string> Keys => tokenMap.Keys;

            public static ServerExtConfig Parse(string rawExt)
            {
                if (string.IsNullOrEmpty(rawExt))
                    return null;

                string cleanExt = rawExt.Replace("\\\"", "\"").Trim('"');
                JObject extObject = JObject.Parse(cleanExt);
                Dictionary<string, JToken> valueMap = new Dictionary<string, JToken>(StringComparer.OrdinalIgnoreCase);
                foreach (JProperty property in extObject.Properties())
                {
                    if (property == null || string.IsNullOrEmpty(property.Name))
                        continue;

                    valueMap[property.Name] = property.Value;
                }

                return new ServerExtConfig(valueMap);
            }

            public bool GetBool(params string[] propertyNames)
            {
                if (!TryGetToken(out JToken token, propertyNames))
                    return false;

                if (token.Type == JTokenType.Boolean)
                    return token.Value<bool>();

                if (bool.TryParse(token.ToString(), out bool parsedBool))
                    return parsedBool;

                if (int.TryParse(token.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsedInt))
                    return parsedInt != 0;

                return false;
            }

            public string GetString(params string[] propertyNames)
            {
                return TryGetToken(out JToken token, propertyNames) ? token.ToString() : string.Empty;
            }

            public bool TryGetFloat(out float value, params string[] propertyNames)
            {
                value = 0f;
                if (!TryGetToken(out JToken token, propertyNames))
                    return false;

                if (token.Type == JTokenType.Float || token.Type == JTokenType.Integer)
                {
                    value = token.Value<float>();
                    return true;
                }

                return float.TryParse(token.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out value);
            }

            public bool TryGetInt(out int value, params string[] propertyNames)
            {
                value = 0;
                if (!TryGetToken(out JToken token, propertyNames))
                    return false;

                if (token.Type == JTokenType.Integer)
                {
                    value = token.Value<int>();
                    return true;
                }

                return int.TryParse(token.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
            }

            public bool TryGetIntList(out List<int> values, params string[] propertyNames)
            {
                values = null;
                if (!TryGetToken(out JToken token, propertyNames) || token.Type != JTokenType.Array)
                    return false;

                List<int> parsedValues = new List<int>();
                foreach (JToken item in token.Children())
                {
                    if (item == null || item.Type == JTokenType.Null)
                        continue;

                    if (item.Type == JTokenType.Integer)
                    {
                        parsedValues.Add(item.Value<int>());
                        continue;
                    }

                    if (int.TryParse(item.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsedValue))
                    {
                        parsedValues.Add(parsedValue);
                    }
                }

                if (parsedValues.Count <= 0)
                    return false;

                values = parsedValues;
                return true;
            }

            public bool TryGetObject<T>(out T value, params string[] propertyNames)
            {
                value = default(T);
                if (!TryGetToken(out JToken token, propertyNames))
                    return false;

                try
                {
                    value = token.ToObject<T>();
                    return true;
                }
                catch
                {
                    return false;
                }
            }

            private bool TryGetToken(out JToken token, params string[] propertyNames)
            {
                token = null;
                if (propertyNames == null)
                    return false;

                for (int i = 0; i < propertyNames.Length; i++)
                {
                    string propertyName = propertyNames[i];
                    if (string.IsNullOrEmpty(propertyName))
                        continue;

                    if (!tokenMap.TryGetValue(propertyName, out JToken foundToken) || foundToken == null || foundToken.Type == JTokenType.Null)
                        continue;

                    token = foundToken;
                    return true;
                }

                return false;
            }
        }
    }
}
