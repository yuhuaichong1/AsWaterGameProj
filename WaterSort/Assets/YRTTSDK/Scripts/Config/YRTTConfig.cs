using Newtonsoft.Json;
using YRTT;
using UnityEditor;
using UnityEngine;

namespace YRTT
{
    [System.Serializable]
    public class YRTTConfig : ScriptableObject
    {
        public const string sdk_Version_Name = "6.10.5.1";
        public string serverUrl = string.Empty;
        public string statUrl = string.Empty;
        public string appKey = string.Empty;
        public string appSecret = string.Empty;
        public string umkAppId = string.Empty;

        public string afDevKey = string.Empty;
        public string iosAppId = string.Empty;
        public string oneLinkID = string.Empty;

        public bool useLocalValueConfig = false;
        public bool forceLocalValueConfig = false;
        public string localValueConfig = string.Empty;

        public string maxAppKey = "";
        public string maxAdReviewKey = "";
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
              $"{nameof(maxAppKey)}: {MaxAppKey}, \n" +
              $"{nameof(maxAdReviewKey)}: {maxAdReviewKey}, \n" +
              $"{nameof(insUnitId)}: {insUnitId}, \n" +
              $"{nameof(videoUnitId)}: {videoUnitId}, \n" +
              $"{nameof(bannerUnitId)}: {bannerUnitId}, \n" +
              $"{nameof(openUnitId)}: {openUnitId}, \n" +
              $"{nameof(admobId)}: {admobId}, \n" +
              $"{nameof(privacyPolicyUri)}: {privacyPolicyUri}, \n" +
              $"{nameof(termsOfServiceUri)}: {termsOfServiceUri}, \n" +
              $"{nameof(debug)}: {debug}, \n" +
              $"{nameof(printLog)}: {printLog}";
        }

#if UNITY_EDITOR

        public string ToJson()
        {
            return
                 "{" +
               $"\"{nameof(appKey)}\": \"{appKey}\", \n" +
               $"\"{nameof(appSecret)}\": \"{appSecret}\", \n" +
               $"\"{nameof(umkAppId)}\":\"{umkAppId}\", \n" +
               $"\"{nameof(afDevKey)}\":\"{afDevKey}\", \n" +
               $"\"{nameof(iosAppId)}\":\"{iosAppId}\", \n" +
               $"\"{nameof(oneLinkID)}\": \"{oneLinkID}\", \n" +
               $"\"{nameof(serverUrl)}\": \"{serverUrl}\",\n" +
               $"\"{nameof(statUrl)}\": \"{statUrl}\", \n" +
               $"\"{nameof(privacyPolicyUri)}\": \"{privacyPolicyUri}\", \n" +
               $"\"{nameof(termsOfServiceUri)}\": \"{termsOfServiceUri}\", \n" +
               $"\"{nameof(maxAppKey)}\":\"{maxAppKey}\", \n" +
               $"\"{nameof(maxAdReviewKey)}\":\"{maxAdReviewKey}\", \n" +
               $"\"{nameof(videoUnitId)}\": \"{videoUnitId}\", \n" +
               $"\"{nameof(insUnitId)}\": \"{insUnitId}\", \n" +
               $"\"{nameof(bannerUnitId)}\": \"{bannerUnitId}\", \n" +
               $"\"{nameof(openUnitId)}\": \"{openUnitId}\",\n" +
               $"\"{nameof(admobId)}\": \"{admobId}\",\n" +
               $"\"{"packageName"}\": \"{PlayerSettings.applicationIdentifier}\",\n" +
                 "}";
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
                Instance.serverUrl = cf.serverUrl;
                Instance.statUrl = cf.statUrl;
                Instance.privacyPolicyUri = cf.privacyPolicyUri;
                Instance.termsOfServiceUri = cf.termsOfServiceUri;
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

        public static string MaxAppKey
        {
            get => Instance.maxAppKey.Trim();
        }
        public static string MaxAdReviewKey
        {
            get => Instance.maxAdReviewKey.Trim();
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
    }
}