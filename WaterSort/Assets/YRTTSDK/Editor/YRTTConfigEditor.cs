using UnityEngine;
using UnityEditor;
using YRTT;
using System.IO;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Collections.Generic;

/// <summary>
/// YRTT 配置编辑器（含每平台本地缓存与共享字段持久化）
/// - 本地缓存文件： Assets/YRTTConfigEditorCache.json
/// - 优先使用 YRTTConfig.Instance 中的数据作为当前界面展示来源；其它平台使用本地缓存回填
/// - admob 与 maxAdReviewKey 均为三平台共享
/// - 切平台时只切换“非共享字段”
/// - 修改共享字段时，只影响共享字段，不影响各平台自己的非共享字段
/// - 仅在配置实际变化且用户停止操作超过 1 秒后才写文件（debounce）
/// - 只有当窗口正在被使用/可见（有焦点或鼠标悬停）时才运行去抖检查，其他时间不消耗 update 回调
/// - 不修改 YRTTConfig 类，仅在本文件做兼容与持久化
/// </summary>
public class YRTTConfigEditor : EditorWindow
{
    [MenuItem("Tools/清除用户数据")]
    internal static void RemoveArchive()
    {
        PlayerPrefs.DeleteAll();
        string path = Application.persistentDataPath + "/upload.txt";
        if (File.Exists(path))
            File.Delete(path);
        PlayerPrefs.DeleteAll();
    }

    private static YRTTConfigEditor _view;
    private static readonly string[] AdOptions = new string[] { "Max", "Topon", "Hyperbid" };

    [MenuItem("Tools/配置 &G")]
    public static void ShowWin()
    {
        if (_view != null)
        {
            CloseView();
            return;
        }

        var win = GetWindow<YRTTConfigEditor>();
        _view = win;
        win.Show();
    }

    private static void CloseView()
    {
        if (_view != null)
        {
            _view.Close();
            _view = null;
        }
    }

    class NetworkAdConfig
    {
        public string adAppKey = "";
        public string adAppID = "";
        public string videoUnitId = "";
        public string insUnitId = "";
        public string openUnitId = "";
        public string bannerUnitId = "";

        // 仅作缓存镜像，真正共享规则由编辑器层统一处理
        public string admobId = "";
        public string maxAdReviewKey = "";

        public void CopyNonSharedFromYRTT()
        {
            adAppKey = YRTTConfig.Instance.adAppKey ?? "";
            adAppID = YRTTConfig.Instance.adAppID ?? "";
            videoUnitId = YRTTConfig.Instance.videoUnitId ?? "";
            insUnitId = YRTTConfig.Instance.insUnitId ?? "";
            openUnitId = YRTTConfig.Instance.openUnitId ?? "";
            bannerUnitId = YRTTConfig.Instance.bannerUnitId ?? "";
        }

        public void CopySharedSnapshotFromYRTT()
        {
            admobId = YRTTConfig.Instance.admobId ?? "";
            maxAdReviewKey = YRTTConfig.Instance.maxAdReviewKey ?? "";
        }

        public void ApplyNonSharedToYRTT(string networkName)
        {
            YRTTConfig.Instance.adAppKey = adAppKey ?? "";
            YRTTConfig.Instance.adAppID = networkName == "Max" ? "" : (adAppID ?? "");
            YRTTConfig.Instance.videoUnitId = videoUnitId ?? "";
            YRTTConfig.Instance.insUnitId = insUnitId ?? "";
            YRTTConfig.Instance.openUnitId = openUnitId ?? "";
            YRTTConfig.Instance.bannerUnitId = bannerUnitId ?? "";
        }
    }

    private Dictionary<string, NetworkAdConfig> networkConfigs = new Dictionary<string, NetworkAdConfig>();

    // 三平台共享字段
    private string sharedAdmobId = "";
    private string sharedMaxAdReviewKey = "";

    // UI 对比值，用于识别用户真实修改
    private string prevSharedAdmobId = null;
    private string prevSharedMaxAdReviewKey = null;

    private static string CacheFilePath => Path.Combine(Application.dataPath, "YRTTConfigEditorCache.json");

    // debounce
    private bool cacheDirty = false;
    private double lastChangeTime = 0.0;
    private const double debounceSeconds = 1.0;
    private bool saveScheduled = false;

    public bool isInit;

    private void EnsureNetworkCacheExists(string name)
    {
        if (string.IsNullOrEmpty(name)) return;
        if (!networkConfigs.ContainsKey(name))
            networkConfigs[name] = new NetworkAdConfig();
    }

    private void EnsureAllNetworkCachesExist()
    {
        foreach (var n in AdOptions)
            EnsureNetworkCacheExists(n);
    }

    private bool IsWindowBeingUsed()
    {
        return this != null && (hasFocus || EditorWindow.focusedWindow == this || EditorWindow.mouseOverWindow == this);
    }

    private void ScheduleSave()
    {
        cacheDirty = true;
        lastChangeTime = EditorApplication.timeSinceStartup;

        if (IsWindowBeingUsed() && !saveScheduled)
        {
            saveScheduled = true;
            EditorApplication.update += OnEditorUpdate;
        }
    }

    private void OnFocus()
    {
        if (cacheDirty && !saveScheduled && IsWindowBeingUsed())
        {
            lastChangeTime = EditorApplication.timeSinceStartup;
            saveScheduled = true;
            EditorApplication.update += OnEditorUpdate;
        }
    }

    private void OnLostFocus()
    {
        // 不主动保存，等再次活跃时继续 debounce
    }

    private void OnEditorUpdate()
    {
        if (!cacheDirty)
        {
            saveScheduled = false;
            EditorApplication.update -= OnEditorUpdate;
            return;
        }

        if (!IsWindowBeingUsed())
        {
            saveScheduled = false;
            EditorApplication.update -= OnEditorUpdate;
            return;
        }

        double now = EditorApplication.timeSinceStartup;
        if (now - lastChangeTime >= debounceSeconds)
        {
            SaveCacheToFile();
        }
    }

    private void SaveCacheToFile()
    {
        try
        {
            var obj = new JObject
            {
                ["sharedAdmobId"] = sharedAdmobId ?? "",
                ["sharedMaxAdReviewKey"] = sharedMaxAdReviewKey ?? ""
            };

            var nets = new JObject();
            foreach (var kv in networkConfigs)
            {
                var cfg = kv.Value;
                var n = new JObject
                {
                    ["adAppKey"] = cfg.adAppKey ?? "",
                    ["adAppID"] = cfg.adAppID ?? "",
                    ["videoUnitId"] = cfg.videoUnitId ?? "",
                    ["insUnitId"] = cfg.insUnitId ?? "",
                    ["openUnitId"] = cfg.openUnitId ?? "",
                    ["bannerUnitId"] = cfg.bannerUnitId ?? "",
                    ["admobId"] = cfg.admobId ?? "",
                    ["maxAdReviewKey"] = cfg.maxAdReviewKey ?? ""
                };
                nets[kv.Key] = n;
            }

            obj["networks"] = nets;
            File.WriteAllText(CacheFilePath, obj.ToString(Formatting.Indented));

            cacheDirty = false;
            saveScheduled = false;
            EditorApplication.update -= OnEditorUpdate;
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning("保存 YRTTConfigEditor 缓存失败: " + ex.Message);
        }
    }

    private void LoadCacheFromFile()
    {
        try
        {
            if (!File.Exists(CacheFilePath))
                return;

            string text = File.ReadAllText(CacheFilePath);
            if (string.IsNullOrEmpty(text))
                return;

            var obj = JObject.Parse(text);

            sharedAdmobId = obj.Value<string>("sharedAdmobId") ?? sharedAdmobId;
            sharedMaxAdReviewKey = obj.Value<string>("sharedMaxAdReviewKey") ?? sharedMaxAdReviewKey;

            var nets = obj["networks"] as JObject;
            if (nets == null) return;

            foreach (var prop in nets.Properties())
            {
                EnsureNetworkCacheExists(prop.Name);
                var nobj = prop.Value as JObject;
                if (nobj == null) continue;

                var cfg = networkConfigs[prop.Name];
                cfg.adAppKey = nobj.Value<string>("adAppKey") ?? cfg.adAppKey;
                cfg.adAppID = nobj.Value<string>("adAppID") ?? cfg.adAppID;
                cfg.videoUnitId = nobj.Value<string>("videoUnitId") ?? cfg.videoUnitId;
                cfg.insUnitId = nobj.Value<string>("insUnitId") ?? cfg.insUnitId;
                cfg.openUnitId = nobj.Value<string>("openUnitId") ?? cfg.openUnitId;
                cfg.bannerUnitId = nobj.Value<string>("bannerUnitId") ?? cfg.bannerUnitId;
                cfg.admobId = nobj.Value<string>("admobId") ?? cfg.admobId;
                cfg.maxAdReviewKey = nobj.Value<string>("maxAdReviewKey") ?? cfg.maxAdReviewKey;
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning("读取 YRTTConfigEditor 缓存失败: " + ex.Message);
        }
    }

    /// <summary>
    /// 保存当前平台的“非共享字段”
    /// </summary>
    private void SaveCurrentNetworkToCache(string networkName)
    {
        if (string.IsNullOrEmpty(networkName)) return;

        EnsureNetworkCacheExists(networkName);
        var cfg = networkConfigs[networkName];
        bool changed = false;

        string adAppKey = YRTTConfig.Instance.adAppKey ?? "";
        string adAppID = networkName == "Max" ? "" : (YRTTConfig.Instance.adAppID ?? "");
        string videoUnitId = YRTTConfig.Instance.videoUnitId ?? "";
        string insUnitId = YRTTConfig.Instance.insUnitId ?? "";
        string openUnitId = YRTTConfig.Instance.openUnitId ?? "";
        string bannerUnitId = YRTTConfig.Instance.bannerUnitId ?? "";

        if (cfg.adAppKey != adAppKey) { cfg.adAppKey = adAppKey; changed = true; }
        if (cfg.adAppID != adAppID) { cfg.adAppID = adAppID; changed = true; }
        if (cfg.videoUnitId != videoUnitId) { cfg.videoUnitId = videoUnitId; changed = true; }
        if (cfg.insUnitId != insUnitId) { cfg.insUnitId = insUnitId; changed = true; }
        if (cfg.openUnitId != openUnitId) { cfg.openUnitId = openUnitId; changed = true; }
        if (cfg.bannerUnitId != bannerUnitId) { cfg.bannerUnitId = bannerUnitId; changed = true; }

        if (changed)
            ScheduleSave();
    }

    /// <summary>
    /// 把当前共享字段写回运行态 YRTTConfig
    /// </summary>
    private void ApplySharedFieldsToYRTT()
    {
        YRTTConfig.Instance.admobId = sharedAdmobId ?? "";
        YRTTConfig.Instance.maxAdReviewKey = sharedMaxAdReviewKey ?? "";
    }

    /// <summary>
    /// 加载指定平台：
    /// - 仅应用该平台自己的非共享字段
    /// - 共享字段统一从 editor 层共享值回填
    /// </summary>
    private void LoadNetworkFromCache(string networkName)
    {
        if (string.IsNullOrEmpty(networkName)) return;

        EnsureNetworkCacheExists(networkName);
        networkConfigs[networkName].ApplyNonSharedToYRTT(networkName);
        ApplySharedFieldsToYRTT();
    }

    private void SyncSharedAdmobToAllCaches()
    {
        EnsureAllNetworkCachesExist();

        bool changed = false;
        string value = sharedAdmobId ?? "";

        foreach (var kv in networkConfigs)
        {
            if (kv.Value.admobId != value)
            {
                kv.Value.admobId = value;
                changed = true;
            }
        }

        if (changed)
            ScheduleSave();
    }

    private void SyncSharedMaxAdReviewToAllCaches()
    {
        EnsureAllNetworkCachesExist();

        bool changed = false;
        string value = sharedMaxAdReviewKey ?? "";

        foreach (var kv in networkConfigs)
        {
            if (kv.Value.maxAdReviewKey != value)
            {
                kv.Value.maxAdReviewKey = value;
                changed = true;
            }
        }

        if (changed)
            ScheduleSave();
    }

    private void SwitchAdNetwork(string newNetwork)
    {
        string oldNetwork = YRTTConfig.Instance.selectedAdNetwork;

        if (string.IsNullOrEmpty(newNetwork))
            return;

        if (oldNetwork == newNetwork)
            return;

        if (!string.IsNullOrEmpty(oldNetwork))
            SaveCurrentNetworkToCache(oldNetwork);

        YRTTConfig.Instance.selectedAdNetwork = newNetwork;
        LoadNetworkFromCache(newNetwork);

        prevSharedAdmobId = sharedAdmobId;
        prevSharedMaxAdReviewKey = sharedMaxAdReviewKey;
    }

    private void Init()
    {
        if (isInit) return;
        isInit = true;

        if (string.IsNullOrEmpty(YRTTConfig.Instance.selectedAdNetwork))
            YRTTConfig.Instance.selectedAdNetwork = "Max";

        EnsureAllNetworkCachesExist();
        LoadCacheFromFile();

        // 当前运行态 YRTTConfig 的共享值优先
        if (!string.IsNullOrEmpty(YRTTConfig.Instance.admobId))
            sharedAdmobId = YRTTConfig.Instance.admobId;

        if (!string.IsNullOrEmpty(YRTTConfig.Instance.maxAdReviewKey))
            sharedMaxAdReviewKey = YRTTConfig.Instance.maxAdReviewKey;

        string currentNetwork = YRTTConfig.Instance.selectedAdNetwork;
        EnsureNetworkCacheExists(currentNetwork);

        // 当前平台的非共享字段以 YRTTConfig 为准
        networkConfigs[currentNetwork].CopyNonSharedFromYRTT();
        networkConfigs[currentNetwork].CopySharedSnapshotFromYRTT();

        prevSharedAdmobId = sharedAdmobId;
        prevSharedMaxAdReviewKey = sharedMaxAdReviewKey;

        // 初始化后统一按当前平台规则刷新界面数据
        LoadNetworkFromCache(currentNetwork);
    }

    private void OnGUI()
    {
        Init();

        DrawBuildApp();

        EditorUtility.SetDirty(YRTTConfig.Instance);

        if (!string.IsNullOrEmpty(YRTTConfig.Instance.selectedAdNetwork))
            SaveCurrentNetworkToCache(YRTTConfig.Instance.selectedAdNetwork);

        if (cacheDirty && !saveScheduled && IsWindowBeingUsed())
        {
            lastChangeTime = EditorApplication.timeSinceStartup;
            saveScheduled = true;
            EditorApplication.update += OnEditorUpdate;
        }
    }

    private void DrawBuildApp()
    {
        Label("基础配置");
        EditorGUILayout.BeginVertical("frameBox");
        YRTTConfig.Instance.appKey = DrawTextField("App Key", YRTTConfig.Instance.appKey);
        YRTTConfig.Instance.appSecret = DrawTextField("App Secret", YRTTConfig.Instance.appSecret);
        YRTTConfig.Instance.umkAppId = DrawTextField("UMK ID", YRTTConfig.Instance.umkAppId);
        YRTTConfig.Instance.afDevKey = DrawTextField("Af Dev Key", YRTTConfig.Instance.afDevKey);
        YRTTConfig.Instance.iosAppId = DrawTextField("Ios App ID", YRTTConfig.Instance.iosAppId);
        YRTTConfig.Instance.oneLinkID = DrawTextField("OneLink ID", YRTTConfig.Instance.oneLinkID);
        YRTTConfig.Instance.cloudProjectNumber = DrawTextField("CloudProjectNumber", YRTTConfig.Instance.cloudProjectNumber);
        YRTTConfig.Instance.serverUrl = DrawTextField("业务接口域名", YRTTConfig.Instance.serverUrl);
        YRTTConfig.Instance.statUrl = DrawTextField("日志上报域名", YRTTConfig.Instance.statUrl);
        YRTTConfig.Instance.privacyPolicyUri = DrawTextField("隐私政策地址", YRTTConfig.Instance.privacyPolicyUri);
        YRTTConfig.Instance.termsOfServiceUri = DrawTextField("用户协议地址", YRTTConfig.Instance.termsOfServiceUri);
        EditorGUILayout.EndVertical();

        Label("广告配置（选择三种广告平台中的一种作为本地广告配置）");
        EditorGUILayout.BeginVertical("frameBox");

        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("广告平台", GUILayout.MinWidth(80), GUILayout.ExpandWidth(false));

        int sel = System.Array.IndexOf(AdOptions, YRTTConfig.Instance.selectedAdNetwork);
        if (sel < 0) sel = 0;

        int newSel = GUILayout.Toolbar(sel, AdOptions);
        string selectedNetwork = AdOptions[newSel];

        if (selectedNetwork != YRTTConfig.Instance.selectedAdNetwork)
        {
            SwitchAdNetwork(selectedNetwork);
        }

        EditorGUILayout.EndHorizontal();

        string checkAdName = YRTTConfig.Instance.selectedAdNetwork;

        YRTTConfig.Instance.adAppKey = DrawTextField(checkAdName + " SDK App Key", YRTTConfig.Instance.adAppKey);

        if (checkAdName != "Max")
        {
            YRTTConfig.Instance.adAppID = DrawTextField(checkAdName + " SDK App ID", YRTTConfig.Instance.adAppID);
        }
        else
        {
            YRTTConfig.Instance.adAppID = "";
        }

        YRTTConfig.Instance.videoUnitId = DrawTextField(checkAdName + " 激励视频 ID", YRTTConfig.Instance.videoUnitId);
        YRTTConfig.Instance.insUnitId = DrawTextField(checkAdName + " 插屏 ID", YRTTConfig.Instance.insUnitId);
        YRTTConfig.Instance.openUnitId = DrawTextField(checkAdName + " 开屏 ID", YRTTConfig.Instance.openUnitId);
        YRTTConfig.Instance.bannerUnitId = DrawTextField(checkAdName + " Banner ID", YRTTConfig.Instance.bannerUnitId);

        string beforeMaxReview = sharedMaxAdReviewKey;
        sharedMaxAdReviewKey = DrawTextField("Max AdReview Key", sharedMaxAdReviewKey);
        if (prevSharedMaxAdReviewKey == null) prevSharedMaxAdReviewKey = beforeMaxReview;

        if (sharedMaxAdReviewKey != prevSharedMaxAdReviewKey)
        {
            YRTTConfig.Instance.maxAdReviewKey = sharedMaxAdReviewKey ?? "";
            SyncSharedMaxAdReviewToAllCaches();
            prevSharedMaxAdReviewKey = sharedMaxAdReviewKey;
        }

        string beforeAdmob = sharedAdmobId;
        sharedAdmobId = DrawTextField("Admob appID", sharedAdmobId);
        if (prevSharedAdmobId == null) prevSharedAdmobId = beforeAdmob;

        if (sharedAdmobId != prevSharedAdmobId)
        {
            YRTTConfig.Instance.admobId = sharedAdmobId ?? "";
            SyncSharedAdmobToAllCaches();
            prevSharedAdmobId = sharedAdmobId;
        }

        EditorGUILayout.EndVertical();

        Label("测试相关");
        EditorGUILayout.BeginVertical("frameBox");
        YRTTConfig.Instance.debug = DrawBoolField("测试环境", YRTTConfig.Instance.debug, "正式发布时请取消勾选");
        YRTTConfig.Instance.printLog = DrawBoolField("打印日志", YRTTConfig.Instance.printLog, "正式发布时请取消勾选");
        EditorGUILayout.EndVertical();

        Label("配置");
        EditorGUILayout.BeginVertical("frameBox");
        YRTTConfig.Instance.useLocalValueConfig = DrawBoolField("使用配置", YRTTConfig.Instance.useLocalValueConfig);
        EditorGUILayout.Space(5);
        if (YRTTConfig.Instance.useLocalValueConfig)
        {
            YRTTConfig.Instance.forceLocalValueConfig = DrawBoolField("强制使用默认配置", YRTTConfig.Instance.forceLocalValueConfig);
            if (GUILayout.Button("粘贴配置"))
            {
                YRTTConfig.Instance.localValueConfig = GUIUtility.systemCopyBuffer;
            }
            YRTTConfig.Instance.localValueConfig = GUILayout.TextField(YRTTConfig.Instance.localValueConfig ?? "");
        }
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(20);
        EditorGUILayout.BeginVertical("frameBox");

        if (GUILayout.Button("导出到剪贴板"))
        {
            TextEditor te = new TextEditor();
            te.text = YRTTConfig.Instance.ToJson();
            te.SelectAll();
            te.Copy();
            ShowNotification(new GUIContent("成功"));
        }

        if (GUILayout.Button("从剪贴板导入"))
        {
            string json = GUIUtility.systemCopyBuffer;
            if (!json.StartsWith("{") || !json.EndsWith("}"))
            {
                ShowNotification(new GUIContent("剪贴板内不是Json"));
                return;
            }

            JObject jsonObj = JObject.Parse(json);
            if (jsonObj.ContainsKey("packageName"))
            {
                string packageName = (string)jsonObj["packageName"];
                PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, packageName);
                Debug.LogWarning($"设置包名：{packageName}");
            }

            bool b = YRTTConfig.Instance.InitByJson(json);
            if (b)
            {
                ShowNotification(new GUIContent("设置参数成功"));

                string currentNetwork = YRTTConfig.Instance.selectedAdNetwork;
                if (string.IsNullOrEmpty(currentNetwork))
                {
                    currentNetwork = "Max";
                    YRTTConfig.Instance.selectedAdNetwork = currentNetwork;
                }

                EnsureNetworkCacheExists(currentNetwork);

                // 当前平台非共享字段以导入后的 YRTT 为准
                networkConfigs[currentNetwork].CopyNonSharedFromYRTT();

                // 共享字段同步
                sharedAdmobId = YRTTConfig.Instance.admobId ?? "";
                sharedMaxAdReviewKey = YRTTConfig.Instance.maxAdReviewKey ?? "";

                ApplySharedFieldsToYRTT();
                SyncSharedAdmobToAllCaches();
                SyncSharedMaxAdReviewToAllCaches();

                prevSharedAdmobId = sharedAdmobId;
                prevSharedMaxAdReviewKey = sharedMaxAdReviewKey;

                ScheduleSave();
            }
            else
            {
                ShowNotification(new GUIContent("设置参数失败"));
            }
        }

        EditorGUILayout.EndVertical();

        GUI.Label(new Rect(10, position.height - 40, 400, 40), "@version:" + YRTTConfig.SDKVersionName);
    }

    /// <summary>
    /// 加密文本框（保留原有接口）
    /// </summary>
    private string DrawEntryTextField(string title, string content)
    {
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label(title, GUILayout.MinWidth(80), GUILayout.ExpandWidth(false));

        GUIStyle textFieldStyle = new GUIStyle(GUI.skin.textField);
        textFieldStyle.normal.textColor = new Color(0.5f, 0.5f, 1);
        textFieldStyle.focused.textColor = new Color(0.5f, 0.5f, 1);

        content = GUILayout.TextField((content ?? "").DecrypData(), textFieldStyle);

        EditorGUILayout.EndHorizontal();
        return content.EncryData();
    }

    private string DrawTextField(string title, string content)
    {
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label(title, GUILayout.MinWidth(80), GUILayout.ExpandWidth(false));
        content = GUILayout.TextField(content ?? "");
        EditorGUILayout.EndHorizontal();
        return content;
    }

    private bool DrawBoolField(string title, bool content, string warn = "")
    {
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label(title, GUILayout.MinWidth(80), GUILayout.ExpandWidth(false));
        content = EditorGUILayout.Toggle(content);
        if (content && !string.IsNullOrEmpty(warn))
        {
            LabelWarn(warn);
        }
        EditorGUILayout.EndHorizontal();
        return content;
    }

    private void Label(string content)
    {
        GUIStyle style = new GUIStyle();
        style.contentOffset = new Vector2(8, 0);
        style.normal.textColor = Color.white;
        style.fontSize = 14;
        style.fontStyle = FontStyle.Bold;
        style.padding = new RectOffset(0, 0, 3, 0);
        GUILayout.Label(content, style);
    }

    private void LabelWarn(string content)
    {
        GUIStyle style = new GUIStyle();
        style.contentOffset = new Vector2(8, 0);
        style.normal.textColor = Color.red;
        style.fontSize = 14;
        style.padding = new RectOffset(0, 0, 3, 0);
        GUILayout.Label(content, style);
    }

    private void OnDisable()
    {
        if (!string.IsNullOrEmpty(YRTTConfig.Instance.selectedAdNetwork))
            SaveCurrentNetworkToCache(YRTTConfig.Instance.selectedAdNetwork);

        if (cacheDirty)
            SaveCacheToFile();
    }
}