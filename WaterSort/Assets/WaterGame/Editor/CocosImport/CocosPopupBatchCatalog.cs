#if UNITY_EDITOR
using System.Collections.Generic;

namespace AsGame.Editor.CocosImport
{
    /// <summary>批量导入清单（已排除微信/抖音/快手渠道弹窗）。</summary>
    public static class CocosPopupBatchCatalog
    {
        public readonly struct Entry
        {
            public readonly string PrefabName;
            public readonly string RelativeCocosPath;
            public readonly string Category;
            public readonly string UnityScript;
            public readonly bool SkipGm;

            public Entry(string name, string relPath, string category, string script = null, bool skipGm = true)
            {
                PrefabName = name;
                RelativeCocosPath = relPath;
                Category = category;
                UnityScript = script;
                SkipGm = skipGm;
            }
        }

        public static IReadOnlyList<Entry> All => new List<Entry>
        {
            // —— 主弹窗 ——
            new("SettingPopup", "SettingPopup/Prefab/SettingPopup.prefab", "弹窗", "SettingPopupView"),
            new("SuccessPopup", "SuccessPopup/Prefab/SuccessPopup.prefab", "弹窗", "SuccessPopupView"),
            new("NewPlayPopup", "NewPlayPopup/Prefab/NewPlayPopup.prefab", "弹窗", "NewPlayPopupView"),
            new("RankPopup", "RankPopup/Prefab/RankPopup.prefab", "弹窗", "RankPopupView"),
            new("CollectPopup", "CollectPopup/Prefab/CollectPopup.prefab", "弹窗", "CollectPopupView"),
            new("GetCollectPopup", "GetCollectPopup/Prefab/GetCollectPopup.prefab", "弹窗", "GetCollectPopupView"),
            new("RecoverHeartPopup", "RecoverHeartPopup/Prefab/RecoverHeartPopup.prefab", "弹窗", "RecoverHeartPopupView"),
            new("GetHeartPopup", "RecoverHeartPopup/Prefab/GetHeartPopup.prefab", "弹窗", "GetHeartPopupView"),
            new("DailyHeartPopup", "RecoverHeartPopup/Prefab/DailyHeartPopup.prefab", "弹窗", "DailyHeartPopupView"),
            new("FeedbackPopup", "FeedbackPopup/Prefab/FeedbackPopup.prefab", "弹窗", "FeedbackPopupView"),
            new("ClickPopup", "ClickPopup/Prefab/ClickPopup.prefab", "系统", null, skipGm: false),

            // —— 列表项 / 子预制 ——
            new("RankItem", "RankPopup/Prefab/RankItem.prefab", "子项"),
            new("RankUpItem", "SuccessPopup/Prefab/RankUpItem.prefab", "子项"),
            new("CollectItem", "CollectPopup/Prefab/CollectItem.prefab", "子项"),
            new("GetCollectItem", "GetCollectPopup/Prefab/GetCollectItem.prefab", "子项"),
            new("HeartItem", "Res/Prefab/HeartItem.prefab", "子项"),
        };

        public static IReadOnlyList<Entry> ExcludedGooglePlay => new List<Entry>
        {
            new("WXCollectionPopup", "WXCollectionPopup/Prefab/WXCollectionPopup.prefab", "渠道(已跳过)"),
            new("TTCollectionPopup", "(无资源包)", "渠道(已跳过)"),
            new("KSCollectionPopup", "(无资源包)", "渠道(已跳过)"),
        };
    }
}
#endif
