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
                    "Assets/AssetBundleLocal/Json/Levels/level_N.json\n\n" +
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

        [MenuItem("WaterGame/关卡/校验全部拆关 JSON", false, 120)]
        public static void ValidateAllSplitLevels()
        {
            var entries = LevelBatchChecker.CheckAllOnDisk(showProgress: true);
            LevelBatchChecker.LogDetailedReport(entries);
            EditorUtility.DisplayDialog(
                "全部校验",
                LevelBatchChecker.FormatSummaryForStatusBar(entries) + "\n\n逐关明细已输出到 Console 窗口。",
                "确定");
        }

        [MenuItem("WaterGame/关卡/导出全部检查报告", false, 121)]
        public static void ExportAllCheckReport()
        {
            var entries = LevelBatchChecker.CheckAllOnDisk(showProgress: true);
            if (entries.Count == 0)
            {
                EditorUtility.DisplayDialog("导出报告", "未找到任何关卡 JSON。", "确定");
                return;
            }

            LevelBatchChecker.LogDetailedReport(entries);
            LevelBatchChecker.TryExportReportWithDialog(entries);
        }
    }
}
