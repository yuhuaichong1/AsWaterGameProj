using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using YRTT;
using UnityEditor;
using UnityEngine;

namespace YRTT
{
    [System.Serializable]
    public class YRTTConfig : ScriptableObject
    {
        public const string sdk_Version_Name = "6.12.2.1";
        public string serverUrl = string.Empty;
        public string statUrl = string.Empty;
        public string appKey = string.Empty;
        public string appSecret = string.Empty;
        public string umkAppId = string.Empty;

        public string afDevKey = string.Empty;
        public string iosAppId = string.Empty;
        public string oneLinkID = string.Empty;

        // 新增 GMS 认证配置
        public string cloudProjectNumber = string.Empty;

        public bool useLocalValueConfig = false;
        public bool forceLocalValueConfig = false;
        public string localValueConfig = string.Empty;

        public string maxAppKey = "";
        public string maxAdReviewKey = "";

        // 新增通用广告字段（保留原来的 max 字段以兼容）
        public string adAppKey = "";
        public string adAppID = "";
        public string selectedAdNetwork = "Max";

        public string insUnitId = "";
        public string videoUnitId = "";
        public string bannerUnitId = "";
        public string openUnitId = "";
        public string admobId = "";

        public bool debug = false;
        public bool printLog = false;

        // 新增隐私政策和服务条款字段
        public string privacyPolicyUri = "";
        public string termsOfServiceUri = "";

        public override string ToString()
        {
            return
              $"{nameof(serverUrl)}: {ServerUrl},\n" +
              $"{nameof(statUrl)}: {StatUrl}, \n" +
              $"{nameof(appKey)}: {appKey}, \n" +
              $"{nameof(appSecret)}: {appSecret}, \n" +
              $"{nameof(umkAppId)}: {umkAppId}, \n" +
              $"{nameof(afDevKey)}: {afDevKey}, \n" +
              $"{nameof(iosAppId)}: {iosAppId}, \n" +
              $"{nameof(oneLinkID)}: {oneLinkID}, \n" +
              $"{nameof(cloudProjectNumber)}: {cloudProjectNumber}, \n" +
              $"{nameof(maxAppKey)}: {MaxAppKey}, \n" +
              $"{nameof(adAppKey)}: {adAppKey}, \n" +
              $"{nameof(adAppID)}: {adAppID}, \n" +
              $"{nameof(maxAdReviewKey)}: {maxAdReviewKey}, \n" +
              $"{nameof(insUnitId)}: {insUnitId}, \n" +
              $"{nameof(videoUnitId)}: {videoUnitId}, \n" +
              $"{nameof(bannerUnitId)}: {bannerUnitId}, \n" +
              $"{nameof(openUnitId)}: {openUnitId}, \n" +
              $"{nameof(admobId)}: {admobId}, \n" +
              $"{nameof(privacyPolicyUri)}: {privacyPolicyUri}, \n" +
              $"{nameof(termsOfServiceUri)}: {termsOfServiceUri}, \n" +
              $"{nameof(debug)}: {debug}, \n" +
              $"{nameof(printLog)}: {printLog}, \n" +
              $"{nameof(selectedAdNetwork)}: {selectedAdNetwork}";
        }

#if UNITY_EDITOR

        public string ToJson()
        {
            var obj = new JObject
            {
                [nameof(appKey)] = appKey ?? string.Empty,
                [nameof(appSecret)] = appSecret ?? string.Empty,
                [nameof(umkAppId)] = umkAppId ?? string.Empty,
                [nameof(afDevKey)] = afDevKey ?? string.Empty,
                [nameof(iosAppId)] = iosAppId ?? string.Empty,
                [nameof(oneLinkID)] = oneLinkID ?? string.Empty,
                [nameof(cloudProjectNumber)] = cloudProjectNumber ?? string.Empty,
                [nameof(serverUrl)] = serverUrl ?? string.Empty,
                [nameof(statUrl)] = statUrl ?? string.Empty,
                [nameof(privacyPolicyUri)] = privacyPolicyUri ?? string.Empty,
                [nameof(termsOfServiceUri)] = termsOfServiceUri ?? string.Empty,
                [nameof(adAppKey)] = adAppKey ?? string.Empty,
                [nameof(adAppID)] = adAppID ?? string.Empty,
                [nameof(selectedAdNetwork)] = selectedAdNetwork ?? string.Empty,
                [nameof(maxAppKey)] = maxAppKey ?? string.Empty,
                [nameof(maxAdReviewKey)] = maxAdReviewKey ?? string.Empty,
                [nameof(videoUnitId)] = videoUnitId ?? string.Empty,
                [nameof(insUnitId)] = insUnitId ?? string.Empty,
                [nameof(bannerUnitId)] = bannerUnitId ?? string.Empty,
                [nameof(openUnitId)] = openUnitId ?? string.Empty,
                [nameof(admobId)] = admobId ?? string.Empty,
                ["packageName"] = PlayerSettings.applicationIdentifier ?? string.Empty
            };
            // 返回压缩后的 JSON（没有空格、换行）
            return obj.ToString(Formatting.None);
        }

#endif

        public bool InitByJson(string json)
        {
            try
            {
                var cf = JsonConvert.DeserializeObject<YRTTConfig>(json);
                Instance.appKey = cf.appKey;
                Instance.appSecret = cf.appSecret;
                Instance.umkAppId = cf.umkAppId;
                Instance.afDevKey = cf.afDevKey;
                Instance.iosAppId = cf.iosAppId;
                Instance.oneLinkID = cf.oneLinkID;
                Instance.cloudProjectNumber = cf.cloudProjectNumber;
                Instance.serverUrl = cf.serverUrl;
                Instance.statUrl = cf.statUrl;
                Instance.privacyPolicyUri = cf.privacyPolicyUri;
                Instance.termsOfServiceUri = cf.termsOfServiceUri;

                // 广告相关：adAppKey 优先使用，若为空则回退到老字段 maxAppKey（兼容）
                Instance.adAppKey = string.IsNullOrEmpty(cf.adAppKey) ? cf.maxAppKey : cf.adAppKey;
                Instance.adAppID = cf.adAppID;
                Instance.selectedAdNetwork = string.IsNullOrEmpty(cf.selectedAdNetwork) ? "Max" : cf.selectedAdNetwork;

                Instance.maxAppKey = cf.maxAppKey;
                Instance.videoUnitId = cf.videoUnitId;
                Instance.insUnitId = cf.insUnitId;
                Instance.bannerUnitId = cf.bannerUnitId;
                Instance.openUnitId = cf.openUnitId;
                Instance.admobId = cf.admobId;
                Instance.maxAdReviewKey = cf.maxAdReviewKey;
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static YRTTConfig m_Instance;

        public static YRTTConfig Instance
        {
            get
            {
                if (m_Instance != null) return m_Instance;
                m_Instance = AssetUtils.GetScriptableObject<YRTTConfig>(typeof(YRTTConfig).Name, "Assets/Resources", false, false);
                return m_Instance;
            }
            set
            {
                m_Instance = value;
            }
        }

        public static string AppKey
        {
            get => Instance.appKey;
        }

        public static string ServerUrl
        {
            get
            {
                if (!Instance.serverUrl.StartsWith("http"))
                {
                    return "https://" + Instance.serverUrl;
                }
                return Instance.serverUrl;
            }
        }

        public static string StatUrl
        {
            get
            {
                if (!Instance.statUrl.StartsWith("http"))
                {
                    return "https://" + Instance.statUrl;
                }
                return Instance.statUrl;
            }
        }

        public static string PrivacyPolicyUri
        {
            get => Instance.privacyPolicyUri.Trim();
        }

        public static string TermsOfServiceUri
        {
            get => Instance.termsOfServiceUri.Trim();
        }

        public static string AppSecret
        {
            get => Instance.appSecret.Trim();
        }

        public static string UmkAppId
        {
            get => Instance.umkAppId.Trim();
        }

        public static string AfDevKey
        {
            get => Instance.afDevKey.Trim();
        }

        public static string IOSAppId
        {
            get => Instance.iosAppId.Trim();
        }

        public static string OneLinkID
        {
            get => Instance.oneLinkID.Trim();
        }

        public static string CloudProjectNumber
        {
            get => Instance.cloudProjectNumber.Trim();
        }

        public static string MaxAppKey
        {
            get => Instance.maxAppKey.Trim();
        }

        public static string MaxAdReviewKey
        {
            get => Instance.maxAdReviewKey.Trim();
        }

        public static string AdAppKey
        {
            get => Instance.adAppKey.Trim();
        }

        public static string AdAppID
        {
            get => Instance.adAppID.Trim();
        }

        public static string VideoUnitId
        {
            get => Instance.videoUnitId.Trim();
        }

        public static string InsUnitId
        {
            get => Instance.insUnitId.Trim();
        }

        public static string BannerUnitId
        {
            get => Instance.bannerUnitId.Trim();
        }

        public static string OpenUnitId
        {
            get => Instance.openUnitId.Trim();
        }

        public static string AdmobId
        {
            get => Instance.admobId.Trim();
        }

        public static bool IsDebug
        {
            get => Instance.debug;
        }

        public static bool IsPrintLog
        {
            get => Instance.printLog;
        }

        public static bool UseLocalValueConfig
        {
            get => Instance.useLocalValueConfig;
        }

        public static bool ForceLocalValueConfig
        {
            get => Instance.forceLocalValueConfig;
        }

        public static string LocalValueConfig
        {
            get => Instance.localValueConfig;
        }

        public static string SDKVersionName
        {
            get => sdk_Version_Name;
        }

        public static string SelectedAdNetworkName
        {
            get => Instance.selectedAdNetwork;
        }
    }
}