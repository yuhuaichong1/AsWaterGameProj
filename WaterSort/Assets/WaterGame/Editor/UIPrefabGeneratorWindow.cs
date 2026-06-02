#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEngine;
using AsGame.UI.PrefabGen;

namespace AsGame.Editor
{
    public class UIPrefabGeneratorWindow : EditorWindow
    {
        Vector2 _scroll;

        [MenuItem("AsGame/UI Prefab Generator/Window...", false, 20)]
        public static void Open()
        {
            GetWindow<UIPrefabGeneratorWindow>("UI Prefab Gen");
        }

        void OnGUI()
        {
            EditorGUILayout.HelpBox(
                "为带 [UIPrefabAsset] 且实现 IUIPrefabBlueprint 的 UI 脚本生成 Prefab。\n" +
                "在脚本里描述节点结构，Editor 会创建层级并 SaveAsPrefabAsset。",
                MessageType.Info);

            if (GUILayout.Button("生成全部", GUILayout.Height(32)))
                UIPrefabGenerator.GenerateAll();

            EditorGUILayout.Space(8);
            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            var types = TypeCache.GetTypesWithAttribute<UIPrefabAssetAttribute>()
                .Where(t => typeof(MonoBehaviour).IsAssignableFrom(t))
                .OrderBy(t => t.Name);

            foreach (var type in types)
            {
                var attr = (UIPrefabAssetAttribute)System.Attribute.GetCustomAttribute(type, typeof(UIPrefabAssetAttribute));
                var hasBlueprint = typeof(IUIPrefabBlueprint).IsAssignableFrom(type);
                EditorGUILayout.BeginHorizontal("box");
                EditorGUILayout.LabelField(type.Name, EditorStyles.boldLabel, GUILayout.Width(180));
                EditorGUILayout.LabelField(hasBlueprint ? "蓝图 ✓" : "仅空壳", GUILayout.Width(60));
                if (GUILayout.Button("生成", GUILayout.Width(60)))
                    UIPrefabGenerator.Generate(type, true);
                EditorGUILayout.EndHorizontal();
                if (attr != null)
                    EditorGUILayout.LabelField(attr.AssetPath, EditorStyles.miniLabel);
            }

            EditorGUILayout.EndScrollView();
        }
    }
}
#endif
