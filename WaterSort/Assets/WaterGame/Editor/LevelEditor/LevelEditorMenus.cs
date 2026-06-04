using AsGame.Data;
using UnityEditor;
using UnityEngine;

namespace AsGame.Editor.LevelEditor
{
    public static class LevelEditorMenus
    {
        [MenuItem("WaterGame/关卡编辑器", false, 10)]
        public static void OpenLevelEditor()
        {
            LevelEditorWindow.ShowWindow();
        }

        [MenuItem("WaterGame/关卡/从 ConfTotal 拆分为单关 JSON", false, 100)]
        public static void SplitConfTotal()
        {
            if (!EditorUtility.DisplayDialog(
                    "拆分 ConfTotal",
                    "将把 ConfTotal.json 中所有关卡导出到：\n" +
                    "Assets/WaterGame/Resources/Levels/Split/level_N.json\n\n" +
                    "已有同名文件会被覆盖。是否继续？",
                    "拆分",
                    "取消"))
                return;

            var count = LevelConfigLoader.ExportConfTotalToSplit();
            EditorUtility.DisplayDialog("完成", $"已导出 {count} 个关卡。", "确定");
        }

        [MenuItem("WaterGame/关卡/清除运行时关卡缓存", false, 101)]
        public static void ClearCache()
        {
            LevelConfigLoader.InvalidateCache();
            Debug.Log("[LevelEditor] 已清除 LevelConfigLoader 缓存。");
        }
    }
}
