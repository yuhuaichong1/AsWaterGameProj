using System;

namespace AsGame.UI.PrefabGen
{
    /// <summary>
    /// 标记可由编辑器工具生成 Prefab 的 UI 脚本。
    /// path 为工程内相对路径，例如 Assets/WaterGame/Resources/Prefabs/UI/Popups/SettingPopup.prefab
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class UIPrefabAssetAttribute : Attribute
    {
        public string AssetPath { get; }
        public bool WithBlocker { get; }
        public float ContentWidth { get; }
        public float ContentHeight { get; }

        public UIPrefabAssetAttribute(string assetPath, bool withBlocker = true,
            float contentWidth = 620f, float contentHeight = 720f)
        {
            AssetPath = assetPath;
            WithBlocker = withBlocker;
            ContentWidth = contentWidth;
            ContentHeight = contentHeight;
        }
    }
}
