using UnityEngine;
using UnityEditor;
using YRTT;
using System.IO;
using Newtonsoft.Json.Linq;

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
        _view.Close();
        _view = null;
    }

    private void OnGUI()
    {
        Init();
        DrawBuildApp();
        EditorUtility.SetDirty(YRTTConfig.Instance);
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
        YRTTConfig.Instance.serverUrl = DrawTextField("业务接口域名", YRTTConfig.Instance.serverUrl);
        YRTTConfig.Instance.statUrl = DrawTextField("日志上报域名", YRTTConfig.Instance.statUrl);
        YRTTConfig.Instance.privacyPolicyUri = DrawTextField("隐私政策地址", YRTTConfig.Instance.privacyPolicyUri);
        YRTTConfig.Instance.termsOfServiceUri = DrawTextField("用户协议地址", YRTTConfig.Instance.termsOfServiceUri);
        EditorGUILayout.EndVertical();

        Label("广告配置");
        EditorGUILayout.BeginVertical("frameBox");
        YRTTConfig.Instance.maxAppKey = DrawTextField("Max SDK Key", YRTTConfig.Instance.maxAppKey);
        YRTTConfig.Instance.maxAdReviewKey = DrawTextField("Max AdReview Key", YRTTConfig.Instance.maxAdReviewKey);
        YRTTConfig.Instance.videoUnitId = DrawTextField("激励视频 ID", YRTTConfig.Instance.videoUnitId);
        YRTTConfig.Instance.insUnitId = DrawTextField("插屏 ID", YRTTConfig.Instance.insUnitId);
        YRTTConfig.Instance.bannerUnitId = DrawTextField("Banner ID", YRTTConfig.Instance.bannerUnitId);
        YRTTConfig.Instance.openUnitId = DrawTextField("开屏 ID", YRTTConfig.Instance.openUnitId);
        YRTTConfig.Instance.admobId = DrawTextField("Admob appID", YRTTConfig.Instance.admobId);
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
            YRTTConfig.Instance.localValueConfig = GUILayout.TextField(YRTTConfig.Instance.localValueConfig);
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
            // 将JSON字符串解析为JObject对象
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
    /// 加密文本框
    /// </summary>
    /// <param name="title"></param>
    /// <param name="content"></param>
    /// <returns></returns>
    private string DrawEntryTextField(string title, string content)
    {
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label(title, GUILayout.MinWidth(80), GUILayout.ExpandWidth(false));
        GUIStyle textFieldStyle = new GUIStyle(GUI.skin.textField);
        textFieldStyle.normal.textColor = new Color(0.5f, 0.5f, 1);
        textFieldStyle.focused.textColor = new Color(0.5f, 0.5f, 1);
        content = GUILayout.TextField(content.DecrypData(), textFieldStyle);
        EditorGUILayout.EndHorizontal();
        return content.EncryData();
    }

    private string DrawTextField(string title, string content)
    {
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label(title, GUILayout.MinWidth(80), GUILayout.ExpandWidth(false));
        content = GUILayout.TextField(content);
        EditorGUILayout.EndHorizontal();
        return content;
    }

    private bool DrawBoolField(string title, bool content, string warn = "")
    {
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label(title, GUILayout.MinWidth(80), GUILayout.ExpandWidth(false));
        content = EditorGUILayout.Toggle(content);
        if (content)
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
        style.normal.textColor = Color.gray;
        style.fontSize = 14;
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

    public bool isInit;

    private void Init()
    {
        if (isInit)
            return;
        isInit = true;
    }
}