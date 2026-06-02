#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using AsGame.UI.PrefabGen;

namespace AsGame.Editor
{
    public static class UIPrefabGenerator
    {
        const string MenuRoot = "AsGame/UI Prefab Generator/";

        [MenuItem(MenuRoot + "Generate All", false, 0)]
        public static void GenerateAll()
        {
            var types = TypeCache.GetTypesWithAttribute<UIPrefabAssetAttribute>()
                .Where(t => typeof(MonoBehaviour).IsAssignableFrom(t))
                .ToArray();

            if (types.Length == 0)
            {
                EditorUtility.DisplayDialog("UI Prefab Generator",
                    "未找到带 [UIPrefabAsset] 的 MonoBehaviour 类。", "确定");
                return;
            }

            var ok = 0;
            foreach (var type in types)
            {
                if (Generate(type, false)) ok++;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[UIPrefabGenerator] 完成 {ok}/{types.Length} 个 Prefab。");
        }

        [MenuItem(MenuRoot + "Generate Selected Script", false, 1)]
        public static void GenerateSelected()
        {
            var mono = Selection.activeObject as MonoScript;
            if (mono == null)
            {
                EditorUtility.DisplayDialog("UI Prefab Generator", "请在 Project 中选中一个 UI 脚本 (.cs)。", "确定");
                return;
            }

            Generate(mono.GetClass(), true);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        public static bool Generate(Type viewType, bool showDialog)
        {
            if (viewType == null || !typeof(MonoBehaviour).IsAssignableFrom(viewType))
            {
                if (showDialog)
                    EditorUtility.DisplayDialog("UI Prefab Generator", "无效的脚本类型。", "确定");
                return false;
            }

            var attr = (UIPrefabAssetAttribute)Attribute.GetCustomAttribute(viewType, typeof(UIPrefabAssetAttribute));
            if (attr == null)
            {
                if (showDialog)
                    EditorUtility.DisplayDialog("UI Prefab Generator",
                        $"{viewType.Name} 缺少 [UIPrefabAsset(\"Assets/...\")] 特性。", "确定");
                return false;
            }

            if (!viewType.GetInterfaces().Contains(typeof(IUIPrefabBlueprint)))
            {
                Debug.LogWarning($"[UIPrefabGenerator] {viewType.Name} 未实现 IUIPrefabBlueprint，将只生成空壳 Prefab。");
            }

            var directory = Path.GetDirectoryName(attr.AssetPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            var root = BuildHierarchy(viewType, attr, out var content, out var blocker);
            try
            {
                if (typeof(IUIPrefabBlueprint).IsAssignableFrom(viewType))
                {
                    var view = (IUIPrefabBlueprint)root.GetComponent(viewType);
                    var ctx = new UIPrefabBuildContext(
                        root.GetComponent<RectTransform>(),
                        content,
                        blocker,
                        root.GetComponent<MonoBehaviour>());
                    view.BuildPrefabUI(ctx);
                }

                PrefabUtility.SaveAsPrefabAsset(root, attr.AssetPath, out var success);
                if (!success)
                {
                    Debug.LogError($"[UIPrefabGenerator] 保存失败: {attr.AssetPath}");
                    return false;
                }

                Debug.Log($"[UIPrefabGenerator] 已生成: {attr.AssetPath}");
                return true;
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        static GameObject BuildHierarchy(Type viewType, UIPrefabAssetAttribute attr,
            out RectTransform contentRt, out CanvasGroup blockerCg)
        {
            var root = new GameObject(viewType.Name, typeof(RectTransform), viewType);
            var rootRt = root.GetComponent<RectTransform>();
            StretchFull(rootRt);

            CanvasGroup blocker = null;
            if (attr.WithBlocker)
            {
                var blockerGo = new GameObject("Blocker", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
                blockerGo.transform.SetParent(root.transform, false);
                StretchFull(blockerGo.GetComponent<RectTransform>());
                blockerGo.GetComponent<Image>().color = new Color(0, 0, 0, 0.55f);
                blocker = blockerGo.GetComponent<CanvasGroup>();
            }

            var contentGo = new GameObject("Content", typeof(RectTransform), typeof(Image));
            contentGo.transform.SetParent(root.transform, false);
            contentRt = contentGo.GetComponent<RectTransform>();
            contentRt.anchorMin = contentRt.anchorMax = new Vector2(0.5f, 0.5f);
            contentRt.sizeDelta = new Vector2(attr.ContentWidth, attr.ContentHeight);
            contentGo.GetComponent<Image>().color = new Color(0.12f, 0.16f, 0.22f, 0.98f);

            blockerCg = blocker;
            return root;
        }

        static void StretchFull(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }
    }
}
#endif
