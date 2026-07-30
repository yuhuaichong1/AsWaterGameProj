using System.Xml;
using System.Text;
using System.IO;
using System.Xml.Linq;
using UnityEngine;
using System.Linq;
using YRTT;

/// <summary>
/// YRTTAndroidManifest 继承自 XmlDocument，封装了对 AndroidManifest.xml 的常用操作
/// </summary>
internal class YRTTAndroidManifest : XmlDocument
{
    private const string AndroidXmlNamespace = "http://schemas.android.com/apk/res/android";
    private const string AndroidXmlToolsNamespace = "http://schemas.android.com/tools";

    private string path;
    private XmlNamespaceManager nameSpaceManager;
    private readonly XmlElement ManifestElement;
    private readonly XmlElement ApplicationElement;

    public YRTTAndroidManifest(string path)
    {
        this.path = path;
        using (var reader = new XmlTextReader(path))
        {
            reader.Read();
            Load(reader);
        }
        nameSpaceManager = new XmlNamespaceManager(NameTable);
        nameSpaceManager.AddNamespace("android", AndroidXmlNamespace);
        ManifestElement = SelectSingleNode("/manifest") as XmlElement;
        ApplicationElement = SelectSingleNode("/manifest/application") as XmlElement;
    }

    public string Save() => SaveAs(path);

    public string SaveAs(string path)
    {
        using (var writer = new XmlTextWriter(path, new UTF8Encoding(false)))
        {
            writer.Formatting = Formatting.Indented;
            Save(writer);
        }
        return path;
    }

    /// <summary>
    /// 通用设置属性方法
    /// </summary>
    private bool SetElementAttribute(XmlElement element, string attrName, string value, string ns = AndroidXmlNamespace)
    {
        if (element.GetAttribute(attrName, ns) != value)
        {
            element.SetAttribute(attrName, ns, value);
            return true;
        }
        return false;
    }

    /// <summary>
    /// 通用设置 tools:replace 属性方法
    /// </summary>
    private bool SetToolsReplace(XmlElement element, string replaceValue)
    {
        if (!element.HasAttribute("tools:replace"))
        {
            element.SetAttribute("replace", AndroidXmlToolsNamespace, replaceValue);
            return true;
        }
        return false;
    }

    /// <summary>
    /// 获取带有启动意图的 activity 节点
    /// </summary>
    internal XmlNode GetActivityWithLaunchIntent()
    {
        return SelectSingleNode(
            "/manifest/application/activity[intent-filter/action/@android:name='android.intent.action.MAIN' and "
            + "intent-filter/category/@android:name='android.intent.category.LAUNCHER']",
            nameSpaceManager);
    }

    /// <summary>
    /// 设置 application 的 usesCleartextTraffic 属性为 true，允许应用访问明文 HTTP 流量。
    /// 同时添加 tools:replace 属性，确保该属性在合并 manifest 时可被替换。
    /// </summary>
    internal bool SetUsesCleartextTraffic()
    {
        bool changed = false;
        changed |= SetElementAttribute(ApplicationElement, "usesCleartextTraffic", "true");
        changed |= SetToolsReplace(ApplicationElement, "android:usesCleartextTraffic");
        return changed;
    }

    /// <summary>
    /// 设置 application 和主 activity 的 hardwareAccelerated 属性为 true，启用硬件加速以提升渲染性能。
    /// </summary>
    internal bool SetHardwareAccelerated()
    {
        bool changed = false;
        var activity = GetActivityWithLaunchIntent() as XmlElement;
        changed |= SetElementAttribute(ApplicationElement, "hardwareAccelerated", "true");
        if (activity != null)
            changed |= SetElementAttribute(activity, "hardwareAccelerated", "true");
        return changed;
    }

    /// <summary>
    /// 设置主 activity 的 launchMode 为 standard，确保每次启动 activity 都会创建新的实例。
    /// 并移除名为 unityplayer.UnityActivity 的 meta-data 节点。
    /// </summary>
    internal bool SetLaunchModeStandard()
    {
        bool changed = false;
        var activity = GetActivityWithLaunchIntent() as XmlElement;
        if (activity != null)
        {
            changed |= SetElementAttribute(activity, "launchMode", "standard");
            XmlNodeList metaNodes = activity.GetElementsByTagName("meta-data");
            for (int i = metaNodes.Count - 1; i >= 0; i--)
            {
                if (metaNodes[i].Attributes[0].Value.Equals("unityplayer.UnityActivity"))
                {
                    activity.RemoveChild(metaNodes[i]);
                    changed = true;
                }
            }
        }
        return changed;
    }

    /// <summary>
    /// 设置主 activity 的 launchMode 为 singleTask，确保任务栈中只有一个该 activity 实例。
    /// </summary>
    internal bool SetLaunchModeSingleTask()
    {
        var activity = GetActivityWithLaunchIntent() as XmlElement;
        return activity != null && SetElementAttribute(activity, "launchMode", "singleTask");
    }

    /// <summary>
    /// 设置主 activity 的 exported 属性为 true，允许外部应用通过 Intent 启动该 activity。
    /// </summary>
    internal bool SetLuacherActivityExport()
    {
        var activity = GetActivityWithLaunchIntent() as XmlElement;
        return activity != null && SetElementAttribute(activity, "exported", "true");
    }

    /// <summary>
    /// 设置主 activity 的 theme 属性为 @style/UnityThemeSelector，指定自定义主题样式。
    /// </summary>
    internal bool SetTheme()
    {
        var activity = GetActivityWithLaunchIntent() as XmlElement;
        return activity != null && SetElementAttribute(activity, "theme", "@style/UnityThemeSelector");
    }

    /// <summary>
    /// 通用添加权限方法
    /// </summary>
    private bool AddPermission(string permissionName)
    {
        if (SelectNodes($"/manifest/uses-permission[@android:name='{permissionName}']", nameSpaceManager).Count == 0)
        {
            var elem = CreateElement("uses-permission");
            elem.SetAttribute("name", AndroidXmlNamespace, permissionName);
            ManifestElement.AppendChild(elem);
            return true;
        }
        return false;
    }

    /// <summary>
    /// 添加 READ_EXTERNAL_STORAGE 权限，允许应用读取外部存储。
    /// </summary>
    internal bool AddReadExternalStoragePermission() =>
        AddPermission("android.permission.READ_EXTERNAL_STORAGE");

    /// <summary>
    /// 添加 WRITE_EXTERNAL_STORAGE 权限，允许应用写入外部存储。
    /// </summary>
    internal bool AddWriteExternalStoragePermission() =>
        AddPermission("android.permission.WRITE_EXTERNAL_STORAGE");

    /// <summary>
    /// 添加 ACCESS_FINE_LOCATION 权限，允许应用获取精确的位置信息。
    /// </summary>
    internal bool AddAccessFineLocationPermission() =>
        AddPermission("android.permission.ACCESS_FINE_LOCATION");

    /// <summary>
    /// 向 <application> 节点添加 Google Ads Application ID 的 meta-data
    /// </summary>
    internal bool AddGoogleAdsApplicationIdMetaData()
    {
        string metaDataName = "com.google.android.gms.ads.APPLICATION_ID";
        string metaDataValue = YRTTConfig.AdmobId;
        try
        {
            // 检查是否已存在该 meta-data
            var existing = ApplicationElement.SelectSingleNode(
                $"meta-data[@android:name='{metaDataName}']",
                nameSpaceManager) as XmlElement;

            if (existing != null)
            {
                Debug.Log($"已存在 Google Ads Application ID");
                // 已存在则更新 value
                bool updated = SetElementAttribute(existing, "value", metaDataValue);
                if (updated) Debug.Log($"更新 Google Ads Application ID: {metaDataValue}");
                return updated;
            }
            else
            {
                Debug.Log($"不存在 Google Ads Application ID，准备添加: {metaDataValue}");
                // 不存在则添加
                var metaDataElem = CreateElement("meta-data");
                metaDataElem.SetAttribute("name", AndroidXmlNamespace, metaDataName);
                metaDataElem.SetAttribute("value", AndroidXmlNamespace, metaDataValue);
                ApplicationElement.AppendChild(metaDataElem);
                Debug.Log($"添加 Google Ads Application ID: {metaDataValue}");
                return true;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"添加 Google Ads Application ID 失败: {e.Message}");
            return false;
        }
    }

    #region 添加BigoActivity

    internal void AddBigoActivity()
    {
        string manifestPath = path;
        if (File.Exists(manifestPath))
        {
            XDocument xmlDoc = XDocument.Load(manifestPath);
            XElement manifestElement = xmlDoc.Root;
            XElement applicationElement = manifestElement.Element("application");

            if (applicationElement != null)
            {
                AddActivityIfNotExists(xmlDoc, applicationElement, "sg.bigo.ads.ad.splash.AdSplashActivity", "portrait", true);
                AddActivityIfNotExists(xmlDoc, applicationElement, "sg.bigo.ads.ad.splash.LandscapeAdSplashActivity", "landscape", true);
                xmlDoc.Save(manifestPath);
                Debug.Log("AndroidManifest.xml has been updated.");
            }
            else
            {
                Debug.LogError("<application> element not found in AndroidManifest.xml");
            }
        }
        else
        {
            Debug.LogError("AndroidManifest.xml not found at " + manifestPath);
        }
    }

    private bool ActivityExists(XDocument xmlDoc, string activityName)
    {
        XNamespace ns = AndroidXmlNamespace;
        return xmlDoc.Descendants("activity")
            .FirstOrDefault(e => string.Equals(e.Attribute(ns + "name")?.Value, activityName)) != null;
    }

    private void AddActivityIfNotExists(XDocument xmlDoc, XElement elemApplication, string activityName, string orientation, bool replaceTheme)
    {
        XNamespace ns = AndroidXmlNamespace;
        XNamespace toolsNs = AndroidXmlToolsNamespace;
        if (!ActivityExists(xmlDoc, activityName))
        {
            XElement activityElement = new XElement("activity",
                new XAttribute(ns + "name", activityName),
                new XAttribute(ns + "screenOrientation", orientation),
                new XAttribute(ns + "theme", "@style/UnityThemeSelector.Translucent"));
            if (replaceTheme)
            {
                activityElement.SetAttributeValue(toolsNs + "replace", "android:theme");
            }
            elemApplication.Add(activityElement);
            Debug.Log($"Added new activity: {activityName}");
        }
        else
        {
            Debug.Log($"Activity '{activityName}' already exists in the manifest.");
        }
    }

    #endregion
}
