#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace AsGame.Editor.CocosImport
{
    public static class CocosImportSettings
    {
        const string CocosRootKey = "WaterSort_CocosProjectRoot";
        const string ImportRootKey = "WaterSort_CocosImportRoot";

        public static string CocosProjectRoot
        {
            get => EditorPrefs.GetString(CocosRootKey,
                @"d:\BaiduNetdiskDownload\水排序\KaiGeDaNaoMen2.4.13js");
            set => EditorPrefs.SetString(CocosRootKey, value);
        }

        public static string UnityImportRoot
        {
            get => EditorPrefs.GetString(ImportRootKey, "Assets/ImportedFromCocos");
            set => EditorPrefs.SetString(ImportRootKey, value);
        }
    }

    public class CocosImportSettingsWindow : EditorWindow
    {
        [MenuItem("AsGame/Cocos Import/Settings...")]
        public static void Open()
        {
            GetWindow<CocosImportSettingsWindow>("Cocos Import");
        }

        void OnGUI()
        {
            EditorGUILayout.LabelField("Cocos 工程根目录（含 assets 文件夹）", EditorStyles.boldLabel);
            CocosImportSettings.CocosProjectRoot = EditorGUILayout.TextField(CocosImportSettings.CocosProjectRoot);
            CocosImportSettings.UnityImportRoot = EditorGUILayout.TextField("Unity 导入根目录", CocosImportSettings.UnityImportRoot);
            EditorGUILayout.HelpBox(
                "从 Cocos 的 .prefab（JSON）读取节点名、Label 文本、Sprite UUID，\n" +
                "自动复制贴图并生成 Unity Prefab，无需手写 YAML。",
                MessageType.Info);
        }
    }
}
#endif
